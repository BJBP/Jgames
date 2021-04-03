using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace BuildHelper.Editor.Core {
    /// <summary>
    /// Provades request to external programs.
    /// </summary>
    public class ProgramRequest {
        private bool _isAborted;
        private readonly Process _process;
        private StringBuilder _errorsAsync;
        private StringBuilder _outputAsync;
        private Action<bool> _onExited;
        private Action<string> _onOutput;
        private Func<string, bool> _onError;

        /// <summary>
        /// Create request for program located in specified <i>path</i>.
        /// Working directory for program sets to project root.   
        /// </summary>
        /// <param name="path">Path to external program</param>
        /// <param name="args">Comand line arguments</param>
        public ProgramRequest(string path, string args = "") {
            _process = new Process {
                StartInfo = {
                    FileName = path,
                    Arguments = args,
                    WorkingDirectory = BuildHelperStrings.ProjRoot(),
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };
        }

        /// <summary>
        /// Perform program request and return result.
        /// The calling thread is blocks while external program is works. 
        /// </summary>
        /// <exception cref="ExternalException">External program was failed</exception>
        /// <remarks>See more exceptions in <see cref="Process.Start()">System.Diagnostics.Process.Start</see></remarks>.
        /// <returns>Output of program</returns>
        /// <seealso cref="ExecuteAsync"/>
        public string Execute() {
            _process.Start();
            _process.WaitForExit();
            return HandleExited(_process.StandardOutput.ReadToEnd(),
                _process.StandardError.ReadToEnd());
        }

        /// <summary>
        /// Abort request. In this case, 
        /// it is considered that the request was not finished successfully.
        /// </summary>
        public void Abort() {
            _isAborted = true;
            try {
                _process.Kill();
            } catch (Exception) {
                // ignored
            }
        }

        /// <summary>
        /// Asynchronous program request. The calling thread is not blocks.
        /// </summary>
        /// <param name="OnExited">Invoked after external program is finished.
        ///  The first callback argument is the last output of program.
        ///  The second argument is <b>true</b> if request finished successfully.</param>
        /// <param name="OnOutput">Received output</param>
        /// <param name="OnError">Received error. 
        ///  If you want to stop the request with an error, return <b>true</b>.
        ///  If you return <b>false</b>, the request will continue execution.</param>
        /// <remarks>See exceptions in <see cref="Process.Start()">System.Diagnostics.Process.Start</see></remarks>
        /// <seealso cref="Execute"/>
        public void ExecuteAsync(Action<bool> OnExited = null, Action<string> OnOutput = null, 
            Func<string, bool> OnError = null) 
        {
            _onExited = OnExited;
            _onOutput = OnOutput;
            _onError = OnError;
            _errorsAsync = new StringBuilder();
            _outputAsync = new StringBuilder();
            bool isOutputClosed = false, isErrorsClosed = false;
            _process.OutputDataReceived += (sender, args) => {
                isOutputClosed = DataReceived(args, OnOutputDataReceived);
            };
            _process.ErrorDataReceived += (sender, args) => {
                isErrorsClosed = DataReceived(args, OnErrorDataReceived);
            };
            _process.EnableRaisingEvents = true;
            _process.Exited += (sender, args) => {
                WaitUntilAndDo(() => isOutputClosed && isErrorsClosed, HandleExitedAsync);
            };
            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }

        private static bool DataReceived(DataReceivedEventArgs args, Action<string> OnReceived) {
            if (args.Data != null) {
                OnReceived(args.Data);
                return false;
            }
            return true;
        }

        private void OnOutputDataReceived(string data) {
            _outputAsync.AppendLine(data);
            if (_onOutput != null)
                _onOutput(data);
        }

        private void OnErrorDataReceived(string data) {
            if (_onError == null || _onError(data)) {
                _errorsAsync.AppendLine(data);
                _process.Kill();
            }
        }

        private string HandleExited(string output, string error) {
            var exitCode = _process.ExitCode; 
            _process.Close();
            if (!_isAborted && exitCode != 0 || !string.IsNullOrEmpty(error)) {
                throw new ExternalException(string.Format("\nOutput:\n{0}\nError Output:\n{1}", output, error));
            }
            return output;
        }

        private void HandleExitedAsync() {
            bool success = false;
            try {
                HandleExited(_outputAsync.ToString(), _errorsAsync.ToString());
                success = !_isAborted;
            } catch (Exception e) {
                UnityEngine.Debug.LogError(e);
            } finally {
                if (_onExited != null)
                    MainThreadCallback.Push(() => _onExited(success));
            }
        }

        private static void WaitUntilAndDo(Func<bool> check, Action callbcak, int timeOut = 2000) {
            var stopwatch = Stopwatch.StartNew();
            new Thread(() => {
                while (!check() && stopwatch.ElapsedMilliseconds < timeOut) {
                    Thread.Sleep(50);
                }
                callbcak();
            }).Start();
        }
    }
}