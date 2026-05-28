using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Collections.Concurrent;

namespace ClaudeCode.Editor.Core
{
    public class LoginProcess : IDisposable
    {
        Process _process;
        Thread _readThread;
        volatile bool _disposed;
        readonly ConcurrentQueue<string> _outputLines = new ConcurrentQueue<string>();
        string _detectedUrl;

        public bool IsRunning => _process != null && !_process.HasExited;
        public string DetectedUrl => _detectedUrl;

        public event Action<string> OnOutputLine;
        public event Action<string> OnUrlDetected;
        public event Action<int> OnExited;

        public bool Start(string cliPath)
        {
            var psi = new ProcessStartInfo
            {
                FileName = cliPath,
                Arguments = "auth login",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            try
            {
                _process = Process.Start(psi);
                if (_process == null) return false;

                _readThread = new Thread(ReadLoop) { IsBackground = true };
                _readThread.Start();
                return true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[ClaudeCode] Login start failed: {e.Message}");
                return false;
            }
        }

        void ReadLoop()
        {
            try
            {
                var charBuf = new char[1];
                var lineBuf = new StringBuilder();
                while (!_disposed && _process != null && !_process.HasExited)
                {
                    int n = _process.StandardOutput.Read(charBuf, 0, 1);
                    if (n <= 0) break;
                    char c = charBuf[0];
                    if (c == '\n' || c == '\r')
                    {
                        if (lineBuf.Length > 0)
                        {
                            var line = lineBuf.ToString();
                            lineBuf.Clear();
                            ProcessLine(line);
                        }
                    }
                    else
                    {
                        lineBuf.Append(c);
                    }
                }

                if (lineBuf.Length > 0)
                    ProcessLine(lineBuf.ToString());

                int exitCode = _process?.ExitCode ?? -1;
                _outputLines.Enqueue($"[exited:{exitCode}]");
            }
            catch (Exception e)
            {
                if (!_disposed)
                    _outputLines.Enqueue($"[error: {e.Message}]");
            }
        }

        void ProcessLine(string line)
        {
            _outputLines.Enqueue(line);

            if (_detectedUrl == null)
            {
                var match = Regex.Match(line, @"https?://[^\s]+");
                if (match.Success)
                    _detectedUrl = match.Value;
            }
        }

        public void ProcessQueue()
        {
            while (_outputLines.TryDequeue(out var line))
            {
                if (line.StartsWith("[exited:"))
                {
                    int code = -1;
                    var m = Regex.Match(line, @"\[exited:(-?\d+)\]");
                    if (m.Success) int.TryParse(m.Groups[1].Value, out code);
                    OnExited?.Invoke(code);
                    continue;
                }

                OnOutputLine?.Invoke(line);

                if (_detectedUrl != null && line.Contains(_detectedUrl))
                    OnUrlDetected?.Invoke(_detectedUrl);
            }
        }

        public bool SubmitCode(string code)
        {
            if (!IsRunning) return false;
            try
            {
                var bytes = Encoding.UTF8.GetBytes(code + "\r\n");
                _process.StandardInput.BaseStream.Write(bytes, 0, bytes.Length);
                _process.StandardInput.BaseStream.Flush();
                _process.StandardInput.Flush();
                return true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"[ClaudeCode] Submit failed: {e.Message}");
                return false;
            }
        }

        public void Cancel()
        {
            if (_process != null && !_process.HasExited)
            {
                try { _process.Kill(); } catch { }
            }
        }

        public void Dispose()
        {
            _disposed = true;
            Cancel();
            try { _process?.Dispose(); } catch { }
            _process = null;
        }
    }
}
