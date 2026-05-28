using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using UnityEngine;

namespace ClaudeCode.Editor.Core
{
    public enum ProcessState
    {
        Idle,
        Running,
        Error
    }

    public class ClaudeProcess : IDisposable
    {
        readonly string _cliPath;
        readonly string _workingDirectory;
        readonly AuthManager _authManager;
        Process _process;
        Thread _readThread;
        volatile bool _disposed;

        readonly ConcurrentQueue<string> _outputLines = new ConcurrentQueue<string>();
        readonly ConcurrentQueue<string> _errorLines = new ConcurrentQueue<string>();

        public ProcessState State { get; private set; } = ProcessState.Idle;
        public event Action<string> OnOutputLine;
        public event Action<string> OnErrorLine;
        public event Action OnProcessExited;

        public ClaudeProcess(string cliPath, string workingDirectory, AuthManager authManager = null)
        {
            _cliPath = cliPath;
            _workingDirectory = workingDirectory;
            _authManager = authManager;
        }

        public bool Start(string prompt, string sessionId = null, bool resume = false, string[] additionalArgs = null)
        {
            if (State == ProcessState.Running)
                return false;

            var args = new StringBuilder();
            args.Append("-p --output-format stream-json --verbose --include-partial-messages");

            if (!string.IsNullOrEmpty(sessionId))
            {
                if (resume)
                    args.Append($" --resume \"{sessionId}\"");
                else
                    args.Append($" --session-id \"{sessionId}\"");
            }

            if (additionalArgs != null)
            {
                foreach (var arg in additionalArgs)
                    args.Append($" {arg}");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = _cliPath,
                Arguments = args.ToString(),
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = _workingDirectory,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            _authManager?.ApplyAuth(startInfo);

            try
            {
                _process = Process.Start(startInfo);
                if (_process == null)
                {
                    State = ProcessState.Error;
                    return false;
                }

                _process.EnableRaisingEvents = true;
                _process.Exited += HandleProcessExited;

                _process.StandardInput.WriteLine(prompt);
                _process.StandardInput.Close();

                _readThread = new Thread(ReadOutputLoop) { IsBackground = true };
                _readThread.Start();

                State = ProcessState.Running;
                return true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[ClaudeCode] Failed to start CLI: {e.Message}");
                State = ProcessState.Error;
                return false;
            }
        }

        void ReadOutputLoop()
        {
            try
            {
                while (!_disposed && _process != null && !_process.HasExited)
                {
                    var line = _process.StandardOutput.ReadLine();
                    if (line == null) break;
                    _outputLines.Enqueue(line);
                }

                while (_process != null && !_disposed)
                {
                    var line = _process.StandardOutput.ReadLine();
                    if (line == null) break;
                    _outputLines.Enqueue(line);
                }

                if (!_disposed && _process != null)
                {
                    var stderr = _process.StandardError.ReadToEnd();
                    if (!string.IsNullOrEmpty(stderr))
                    {
                        try
                        {
                            if (_process.HasExited && _process.ExitCode != 0)
                                _errorLines.Enqueue(stderr);
                            else
                                UnityEngine.Debug.Log($"[ClaudeCode] CLI stderr (non-fatal): {stderr}");
                        }
                        catch
                        {
                            _errorLines.Enqueue(stderr);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (!_disposed)
                    _errorLines.Enqueue(e.Message);
            }
        }

        const int MaxLinesPerFrame = 2;

        /// <summary>
        /// Call from Unity main thread (e.g., EditorApplication.update) to dispatch queued events.
        /// Throttled to MaxLinesPerFrame lines per call so UI can repaint between chunks.
        /// </summary>
        public void ProcessQueue()
        {
            int processed = 0;
            while (processed < MaxLinesPerFrame && _outputLines.TryDequeue(out var line))
            {
                OnOutputLine?.Invoke(line);
                processed++;
            }

            while (_errorLines.TryDequeue(out var error))
                OnErrorLine?.Invoke(error);
        }

        public void Kill()
        {
            if (_process != null && !_process.HasExited)
            {
                try { _process.Kill(); }
                catch (InvalidOperationException) { }
            }
        }

        void HandleProcessExited(object sender, EventArgs e)
        {
            State = ProcessState.Idle;
            OnProcessExited?.Invoke();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            Kill();
            _process?.Dispose();
            _process = null;
        }
    }
}
