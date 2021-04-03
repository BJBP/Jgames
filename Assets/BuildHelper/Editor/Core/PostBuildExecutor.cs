using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEngine;

namespace BuildHelper.Editor.Core {
    /// <summary>
    /// Use it if you need perform async tasks after build.
    /// </summary>
    /// <remarks>
    /// When Unity completes the build, all the assemblies are refreshed.
    /// During this, any objects used in asynchronous operations will be destroyed.
    /// To prevent this, this class keeps the information about the specified callback
    ///  and executes it after Unity completes refresh.
    /// </remarks>
    public class PostBuildExecutor : EditorWindow {
        private string _serialisedArg;
        private string _callbackClass;
        private string _callbackName;

        /// <summary>
        /// Serialize specified callback and its argument for perform after build complete.
        /// </summary>
        /// <param name="callback">Must be a static</param>
        /// <param name="arg">Argument for callback.</param>
        /// <typeparam name="T">Type of argument. Type must be a serializable.</typeparam>
        /// <exception cref="InvalidOperationException">Type is not serializable or callback is not static.</exception>
        public static void Make<T>(Action<T> callback, T arg) {
            if(!typeof(T).IsSerializable && !typeof(ISerializable).IsAssignableFrom(typeof(T)))
                throw new InvalidOperationException("A serializable Type of arg is required");
            if (!callback.Method.IsStatic)
                throw new InvalidOperationException("A method of callback must be static");
            if (callback.Method.DeclaringType == null)
                throw new InvalidOperationException("A method must have declaring type");
            
            var wnd = GetWindow<PostBuildExecutor>("Post build");
            wnd._callbackClass = callback.Method.DeclaringType.FullName;
            wnd._callbackName = callback.Method.Name;
            wnd._serialisedArg = Serialize(arg);
        }

        private void OnGUI() {
            ShowNotification(new GUIContent("Wait a bit..."));
        }

        private void Update() {
            if (EditorApplication.isUpdating || EditorApplication.isCompiling) return;
            Close();
            Invoke();
        }

        private void Invoke() {
            var arg = Deserialize(_serialisedArg);
            Type.GetType(_callbackClass).GetMethod(_callbackName).Invoke(null, new []{arg});
        }
        
        private static string Serialize(object obj) {
            using (var ms = new MemoryStream()) {
                new BinaryFormatter().Serialize(ms, obj);         
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        private static object Deserialize(string base64String) {
            var bytes = Convert.FromBase64String(base64String);
            using (var ms = new MemoryStream(bytes, 0, bytes.Length)) {
                ms.Write(bytes, 0, bytes.Length);
                ms.Position = 0;
                return new BinaryFormatter().Deserialize(ms);
            }
        }
    }
}