#define WORKAROUND_ANDROID_UNITY_SHADER_COMPILER_BUG

using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using Debug = UnityEngine.Debug;

namespace BuildHelper.Editor.Core {
    /// <summary>
    /// This class processes events before and after all the build processes.
    /// It also saves and restores <i>ProjectSettings.asset</i> that have been programmatically
    /// changed for specific build configurations.
    /// </summary>
    [InitializeOnLoad]
    public class BuildTime : IPreprocessBuild, IPostprocessBuild {
        public int callbackOrder {
            get { return 0; }
        }

        /// <summary>
        /// Restore settings after UnityEditor crash.
        /// </summary>
        static BuildTime() {
            RestoreSettings();
        }

        private const string _SETTINGS_PATH = "ProjectSettings/ProjectSettings.asset";
        private const string _SETTINGS_TEMP_PATH = "ProjectSettings/ProjectSettings.temp";
        private static bool _settingsAlreadySaved;
        
        /// <summary>
        /// Implements <see cref="IPreprocessBuild.OnPreprocessBuild"/>.
        /// Is performed before all the build processes, saves <i>ProjectSettings.asset</i> and
        /// sets build version.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="path"></param>
        public void OnPreprocessBuild(BuildTarget target, string path) {
#if UNITY_EDITOR_LINUX && WORKAROUND_ANDROID_UNITY_SHADER_COMPILER_BUG
            if (target == BuildTarget.Android)
                KillUnityShaderCompiler();
#endif
            Debug.Log("Starting build to: " + path);
            SaveSettingsToRestore();
            PlayerSettings.bundleVersion = BuildHelperStrings.GetBuildVersion();
        }

        /// <summary>
        /// Implements <see cref="IPostprocessBuild.OnPostprocessBuild"/>.
        /// Is performed after all the build processes and restore <i>ProjectSettings.asset</i>.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="path"></param>
        public void OnPostprocessBuild(BuildTarget target, string path) {
            RestoreSettings();
        }

        /// <summary>
        /// Saves <i>ProjectSettings.asset</i> to temporary file for change and then restore.
        /// </summary>
        /// <seealso cref="RestoreSettings"/>
        /// <seealso cref="RestoreSettingsIfFailed"/>
        public static void SaveSettingsToRestore() {
            KeepKeystoreInfo();
            if (!_settingsAlreadySaved) {
                var settingsPath = BuildHelperStrings.ProjRoot(_SETTINGS_PATH);
                var settingsPathTemp = BuildHelperStrings.ProjRoot(_SETTINGS_TEMP_PATH);
                File.Copy(settingsPath, settingsPathTemp, true);
                _settingsAlreadySaved = true;
                Debug.Log("BuildTime: Project Settings saved");
            }
        }

        /// <summary>
        /// Saves changes of <i>ProjectSettings.asset</i> and delete temporary file.
        /// After this and until a new call to <see cref="SaveSettingsToRestore"/>,
        /// the call to <see cref="RestoreSettings"/> does not produce any effect.
        /// </summary>
        public static void AcceptChangedSettings() {
            var settingsPathTemp = BuildHelperStrings.ProjRoot(_SETTINGS_TEMP_PATH);
            if (File.Exists(settingsPathTemp)) {
                File.Delete(settingsPathTemp);
            }
            _settingsAlreadySaved = false;
            AssetDatabase.SaveAssets();
            Debug.Log("BuildTime: Project Settings accepted");
        }

        /// <summary>
        /// Restores the saved <i>ProjectSettings.asset</i> and refresh Unity asset database.
        /// If <i>ProjectSettings.asset</i> is not saved then nothing will happen.   
        /// </summary>
        /// <remarks>It is better to use <see cref="RestoreSettingsIfFailed"/> if there is no guarantee 
        /// that the build process will not break with the exception 
        /// and <i>RestoreSettings</i> is not executed.</remarks>
        /// <seealso cref="SaveSettingsToRestore"/>
        public static void RestoreSettings() {
            var settingsPath = BuildHelperStrings.ProjRoot(_SETTINGS_PATH);
            var settingsPathTemp = BuildHelperStrings.ProjRoot(_SETTINGS_TEMP_PATH);
            if (File.Exists(settingsPathTemp)) {
                File.Copy(settingsPathTemp, settingsPath, true);
                File.Delete(settingsPathTemp);
                Debug.Log("BuildTime: Project Settings restored");
                AssetDatabase.Refresh();
            }
            _settingsAlreadySaved = false;
            RestoreKeystoreInfo();
        }

        /// <summary>
        /// Perform specified action and safely restore <i>ProjectSettings.asset</i> 
        /// if action throws exception.
        /// </summary>
        /// <param name="buildAction">An action that can throw exception</param>
        /// <exception cref="Exception">Exception that thrown by an action</exception>
        /// <seealso cref="SaveSettingsToRestore"/>
        /// <seealso cref="RestoreSettings"/>
        public static void RestoreSettingsIfFailed(Action buildAction) {
            try {
                buildAction();
            } catch (Exception e) {
                RestoreSettings();
                throw e;
            }
        }

        /// <summary>
        /// Wrapper for <see cref="BuildPipeline.BuildPlayer(BuildPlayerOptions)"/>.
        /// </summary>
        /// <param name="options">Options for <i>BuildPlayer</i></param>
        /// <exception cref="BuildFailedException">Throw 
        /// if <see cref="BuildPipeline.BuildPlayer(BuildPlayerOptions)"/> return error</exception>
        public static void Build(BuildPlayerOptions options) {
            if (!string.IsNullOrEmpty(BuildPipeline.BuildPlayer(options))) {
                throw new BuildFailedException("");
            }
        }

#region Keep Keystore Info        
        private class KeystoreInfo {
            public string keystoreName { get; set; }
            public string keystorePass { get; set; }
            public string keyaliasName { get; set; }
            public string keyaliasPass { get; set; }
        }
        private static KeystoreInfo _keystoreInfo;

        private static void KeepKeystoreInfo() {
            if (_keystoreInfo == null) {
                _keystoreInfo = new KeystoreInfo();
            }
            CopyProperties<KeystoreInfo>(typeof(PlayerSettings.Android), _keystoreInfo);
        }

        private static void RestoreKeystoreInfo() {
            if (_keystoreInfo != null) {
                CopyProperties<KeystoreInfo>(_keystoreInfo, typeof(PlayerSettings.Android));
            }
        }
        
        private static void CopyProperties<T>(object from, object to) {
            var tFrom = DivideTypeObject(ref from);
            var tTo = DivideTypeObject(ref to);
             
            foreach (var field in typeof(T).GetProperties()) {
                var val = tFrom.GetProperty(field.Name).GetValue(from, null);
                tTo.GetProperty(field.Name).SetValue(to, val, null);
            }
        }

        private static Type DivideTypeObject(ref object obj) {
            Type type;
            if (obj is Type) {
                type = (Type) obj;
                obj = null;
            } else {
                type = obj.GetType();
            }
            return type;
        }
#endregion

        private static void KillUnityShaderCompiler() {
            foreach (var proc in Process.GetProcessesByName("UnityShaderCompiler")) {
                proc.Kill();
            }
        }
    }
}