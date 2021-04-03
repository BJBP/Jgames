using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Debug = UnityEngine.Debug;

namespace BuildHelper.Editor.Core {
    /// <summary>
    /// Provides request to adb (Android Debug Bridge).
    /// Before using this, make sure you have set the path to the Android SDK in Unity Preferences.
    /// </summary>
    public static class AdbRequest {
        private const string _EDITOR_ANDROID_SDK = "AndroidSdkRoot";
        private const string _TITLE_INSTALLING = "Installing app...";
        private const string _INSTALLING_PROGRESS_MATCH = @"\[\s*(\d{1,2})%\]\s*";
        private const string _ERROR_MATCH = @"(adb: error:\s+|Failure\s+\[.*\])";
        private const string _DEVICE_MATCH = @"(.*.\w+)\s+device\b";

        /// <summary>
        /// Install apk to default Android device.
        /// Installation is performed in the background thread.
        /// ProgressBar is displayed while the installation is in progress.
        /// </summary>
        /// <param name="path">Path to apk</param>
        /// <param name="OnDone">Callback thats invoked after request is done.
        /// Bool argument means success or not.</param>
        /// <seealso cref="InstallToDevice(string,string,System.Action{bool})"/>
        public static void InstallToDevice(string path, Action<bool> OnDone) {
            InstallToDevice(path, null, OnDone);
        }

        /// <summary>
        /// Install apk to default Android device.
        /// Installation is performed in the background thread.
        /// ProgressBar is displayed while the installation is in progress.
        /// </summary>
        /// <param name="path">Path to apk</param>
        /// <param name="deviceId">Id of device on which will be installed.
        ///  To get ids of all devices use <see cref="GetDevices"/></param>
        /// <param name="OnDone">Callback thats invoked after request is done.
        /// Bool argument means success or not.</param>
        /// <seealso cref="InstallToDevice(string,System.Action{bool})"/>
        public static void InstallToDevice(string path, string deviceId, Action<bool> OnDone) {
            var request = CreateRequestAdb(string.Format("{0} install -r \"{1}\"", ForDevicePart(deviceId), path));
            request.ExecuteAsync(
                OnExited: success => {
                    ProcessingWindow.Close();
                    if (OnDone != null) OnDone(success);
                },
                OnOutput: status => {
                    if (IsWasError(status)) {
                        request.Abort();
                        ProcessingWindow.Close();
                    } else {
                        ProcessingWindow.Update(status, _INSTALLING_PROGRESS_MATCH);
                    }
                },
                OnError: error => !error.StartsWith("success", true, CultureInfo.InvariantCulture));
            ProcessingWindow.Show(_TITLE_INSTALLING, request.Abort);
        }

        /// <summary>
        /// Run specified package on default Android device.
        /// </summary>
        /// <param name="package">Package name, e.g. "com.darkkon.unity_helper"</param>
        /// <see cref="RunOnDevice(string,string)"/>
        public static void RunOnDevice(string package) {
            RunOnDevice(package, null);
        }

        /// <summary>
        /// Run specified package on default Android device.
        /// </summary>
        /// <param name="package">Package name, e.g. "com.darkkon.unity_helper"</param>
        /// <param name="deviceId">Id of device on which will be installed.
        ///  To get ids of all devices use <see cref="GetDevices"/></param>
        /// <seealso cref="RunOnDevice(string)"/>
        public static void RunOnDevice(string package, string deviceId) {
            Debug.Log("Running apk...");
            CreateRequestAdb(string.Format("{0} shell monkey -p {1} -c android.intent.category.LAUNCHER 1 ",
                ForDevicePart(deviceId), package))
            .Execute();
        }

        /// <summary>
        /// Return ids of all android devices.
        /// </summary>
        /// <returns>string ids</returns>
        /// <seealso cref="InstallToDevice(string,string,System.Action{bool})"/>
        /// <seealso cref="RunOnDevice(string,string)"/>
        public static List<string> GetDevices() {
            var output = CreateRequestAdb("devices").Execute();
            var devices = new List<string>();
            foreach (Match match in Regex.Matches(output, _DEVICE_MATCH)) {
                devices.Add(match.Groups[1].Value);
            }
            return devices;
        }

        private static string ForDevicePart(string deviceId) {
            if (deviceId == null)
                return "";
            return string.Format("-s \"{0}\"", deviceId);
        }

        private static bool IsWasError(string output) {
            var match = Regex.Match(output, _ERROR_MATCH, RegexOptions.IgnoreCase);
            if (match.Success) {
                Debug.LogError(output);
                return true;
            }
            return false;
        }

        private static ProgramRequest CreateRequestAdb(string args = "") {
            var androidSDK = UnityEditor.EditorPrefs.GetString(_EDITOR_ANDROID_SDK);
            var adbPath = string.IsNullOrEmpty(androidSDK) 
                ? "adb"
                : androidSDK + "/platform-tools/adb";
            return new ProgramRequest(adbPath, args);
        }
    }
}