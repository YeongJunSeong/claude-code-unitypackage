using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using UnityEngine;

namespace ClaudeCode.Editor.Terminal
{
    public class CommandResult
    {
        public int ExitCode;
        public string Output;
        public string Error;
        public bool TimedOut;
    }

    public class CommandExecutor : IDisposable
    {
        readonly string _workingDirectory;
        Process _process;
        volatile bool _disposed;

        readonly ConcurrentQueue<string> _outputLines = new ConcurrentQueue<string>();
        bool _isRunning;

        public bool IsRunning => _isRunning;
        public event Action<string> OnOutputLine;
        public event Action<CommandResult> OnComplete;

        public CommandExecutor(string workingDirectory)
        {
            _workingDirectory = workingDirectory;
        }

        public bool Execute(string command, int timeoutMs = 30000)
        {
            if (_isRunning) return false;
            _isRunning = true;

            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            var psi = new ProcessStartInfo
            {
                FileName = isWindows ? "cmd.exe" : "/bin/bash",
                Arguments = isWindows ? $"/c {command}" : $"-c \"{command.Replace("\"", "\\\"")}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = _workingDirectory,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            try
            {
                _process = Process.Start(psi);
                if (_process == null)
                {
                    _isRunning = false;
                    return false;
                }

                var thread = new Thread(() => RunProcess(timeoutMs)) { IsBackground = true };
                thread.Start();
                return true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[ClaudeCode] Command failed: {e.Message}");
                _isRunning = false;
                return false;
            }
        }

        void RunProcess(int timeoutMs)
        {
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            bool timedOut = false;

            try
            {
                while (!_process.StandardOutput.EndOfStream)
                {
                    var line = _process.StandardOutput.ReadLine();
                    if (line != null)
                    {
                        stdout.AppendLine(line);
                        _outputLines.Enqueue(line);
                    }
                }

                stderr.Append(_process.StandardError.ReadToEnd());

                if (!_process.WaitForExit(timeoutMs))
                {
                    timedOut = true;
                    try { _process.Kill(); } catch { }
                }
            }
            catch (Exception e)
            {
                if (!_disposed)
                    stderr.AppendLine(e.Message);
            }

            _isRunning = false;

            var result = new CommandResult
            {
                ExitCode = timedOut ? -1 : _process?.ExitCode ?? -1,
                Output = stdout.ToString(),
                Error = stderr.ToString(),
                TimedOut = timedOut
            };

            OnComplete?.Invoke(result);
        }

        public void ProcessQueue()
        {
            while (_outputLines.TryDequeue(out var line))
                OnOutputLine?.Invoke(line);
        }

        public void Kill()
        {
            if (_process != null && !_process.HasExited)
            {
                try { _process.Kill(); } catch { }
            }
        }

        public void Dispose()
        {
            _disposed = true;
            Kill();
            _process?.Dispose();
        }
    }
}
