using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace BuildHelper.Editor.Core {
    /// <summary>
    /// Callback to main thread from any thread.
    /// </summary>
    public class MainThreadCallback {
        private readonly Queue<object> _mainThreadActions = new Queue<object>();
        private bool _mainThreadUpdateEnabled;
        private readonly object _actionsLock = new object();
        
        private static MainThreadCallback _instance;

        private static MainThreadCallback Instance {
            get {
                if (_instance == null)
                    _instance = new MainThreadCallback();
                return _instance;
            }
            set { _instance = value; }
        }
        
        /// <summary>
        /// Make callback to main thread from any thread.
        /// The action pushed to actions queue thats
        ///  will be preformed once on next update in main thread. 
        /// </summary>
        /// <param name="callback">Callback to main thread</param>
        /// <seealso cref="Push(IEnumerator)"/>
        public static void Push(Action callback) {
            Instance.PushInternal(callback);
        }
        
        /// <summary>
        /// Make coroutine thats will be executing in main thread. 
        /// </summary>
        /// <param name="callback">Main thread coroutine. 
        ///  The return value does not affect execution.
        ///  Therefore, it is better to call <i>yield return null</i> to wait for the next frame.</param>
        /// <seealso cref="Push(System.Action)"/>
        public static void Push(IEnumerator callback) {
            Instance.PushInternal(callback);
        }

        private void PushInternal(object callback) {
            lock (_actionsLock) {
                EnableMainThreadUpdate();
                _mainThreadActions.Enqueue(callback);
            }
        }

        private void MainThreadUpdate() {
            lock (_actionsLock) {
                if (_mainThreadActions.Count == 0)
                    DisableMainThreadUpdate();
                else {
                    var count = _mainThreadActions.Count;
                    for(int i = 0; i < count; ++i) {
                        var action = _mainThreadActions.Dequeue();
                        if (action is Action) {
                            ((Action) action)();
                        } else 
                        if (action is IEnumerator) {
                            var enumerator = (IEnumerator) action;
                            if (enumerator.MoveNext()) {
                                var res = enumerator.Current;
                                _mainThreadActions.Enqueue(action);
                            }
                        }
                    }
                }
            }
        }

        private void EnableMainThreadUpdate() {
            if (!_mainThreadUpdateEnabled) {
                EditorApplication.update += MainThreadUpdate;
                _mainThreadUpdateEnabled = true;
            }
        }

        private void DisableMainThreadUpdate() {
            if (_mainThreadUpdateEnabled) {
                EditorApplication.update -= MainThreadUpdate;
                _mainThreadUpdateEnabled = false;
            }
        }
    }
}