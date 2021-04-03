using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEditor;

namespace BuildHelper.Editor.Core {
    /// <summary>
    /// Shows the progress of the operation.
    /// It is designed for work with background and main thread.
    /// </summary>
    /// <seealso cref="EditorUtility.DisplayProgressBar"/>
    public class ProcessingWindow {
        private const string _FIRST_STATUS = "Please, wait...";
        private static string _title;
        private static string _status;
        private static float _progress;
        private static Action _onCancel;
        private static bool _closed = true;
        
        /// <summary>
        /// Show progress bar with zero progress.
        /// </summary>
        /// <param name="title">Title of progress bar</param>
        /// <param name="OnCancel">Invoked if user canceled operation</param>
        public static void Show(string title, Action OnCancel = null) {
            _title = title;
            _progress = 0f;
            _onCancel = OnCancel;
            Update(_FIRST_STATUS);
            if (_closed) {
                _closed = false;
                MainThreadCallback.Push(UpdateProgress());
            }
        }

        /// <summary>
        /// Update status of operation.
        /// </summary>
        /// <param name="status">Status text that will be shown on progress bar</param>
        /// <seealso cref="Update(string, float)"/>
        /// <seealso cref="Update(string, string, float)"/>
        public static void Update(string status) {
            _status = status;
        }

        /// <summary>
        /// Update status and progress of operation. 
        /// </summary>
        /// <param name="status">Status text that will be shown on progress bar</param>
        /// <param name="patternProgress">Regular expression to extract progress value from the status. 
        /// The first group will be considered progress. If there is no match, progress will not be updated.
        /// See <see cref="Regex.Match(string, string)">Regex.Match</see>.</param>
        /// <param name="val100">The value taken as 100% of progress.</param>
        public static void Update(string status, string patternProgress, float val100 = 100f) {
            var match = Regex.Match(status, patternProgress);
            if (match.Success) {
                var group = match.Groups[match.Groups.Count > 1 ? 1 : 0].Value;
                float progress;
                if (float.TryParse(group, out progress)) {
                    _progress = progress / val100;
                }
            }
            Update(status);
        }

        /// <summary>
        /// Update status and progress of operation. 
        /// </summary>
        /// <param name="status">Status text that will be shown on progress bar</param>
        /// <param name="progress">Progress from 0 to 1</param>
        public static void Update(string status, float progress) {
            _progress = progress;
            Update(status);
        }

        /// <summary>
        /// Close progress bar
        /// </summary>
        public static void Close() {
            _closed = true;
            MainThreadCallback.Push(EditorUtility.ClearProgressBar);
        }

        private static IEnumerator UpdateProgress() {
            while (!_closed) {
                if (_onCancel != null) {
                    if (EditorUtility.DisplayCancelableProgressBar(_title, _status, _progress)) {
                        Close();
                        _onCancel();
                        _onCancel = null;
                        yield break;
                    }
                } else {
                    EditorUtility.DisplayProgressBar(_title, _status, _progress);
                }
                yield return null;
            }
        }
    }
}