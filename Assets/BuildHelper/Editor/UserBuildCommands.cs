using System;
using BuildHelper.Editor.Core;
using UnityEditor;

namespace BuildHelper.Editor {
    public static class UserBuildCommands {
#region Build Menu
        [MenuItem("Build/Build Win64")]
        public static void BuildWin64ToPathWithVersion() {
            BuildTime.RestoreSettingsIfFailed(() => {
                var buildVersion = BuildHelperStrings.GetBuildVersion();
                var target = BuildTarget.StandaloneWindows64;
                var options = GetStandartPlayerOptions(target);
                options.locationPathName = BuildHelperStrings.GetBuildPath(target, buildVersion);
                BuildTime.Build(options);                
            });
        }
        
        [MenuItem("Build/Build Android ARMv7 and run on device")]
        public static void BuildAndroidARMv7AndRun() {
            IdentifierFormWindow.ShowIfNeedChange(BuildTargetGroup.Android, () =>
            AndroidDevicesWindow.ShowIfNeedSelect(adbDeviceId =>
            BuildTime.RestoreSettingsIfFailed(() => {
                var buildVersion = BuildHelperStrings.GetBuildVersion();
                var target = BuildTarget.Android;
                var options = GetStandartPlayerOptions(target);
                var path = BuildAndroidForDevice(AndroidTargetDevice.ARMv7, buildVersion, options);
                PostBuildExecutor.Make(InstallApkToDeviceAndRun, new InstallApkParams {
                    pathToApk = path,
                    adbDeviceId = adbDeviceId
                });
            })));
        }
        
        [MenuItem("Build/Build Android separate ARMv7 and x86")]
        public static void BuildAndroidToPathWithVersion() {
            IdentifierFormWindow.ShowIfNeedChange(BuildTargetGroup.Android, () =>
            BuildTime.RestoreSettingsIfFailed(() => {
                var buildVersion = BuildHelperStrings.GetBuildVersion();
                var target = BuildTarget.Android;
                var options = GetStandartPlayerOptions(target);
                BuildAndroidForDevice(AndroidTargetDevice.ARMv7, buildVersion, options);
                BuildAndroidForDevice(AndroidTargetDevice.x86, buildVersion, options);
            }));
        }
        
        [MenuItem("Build/Build iOS")]
        public static void BuildIOSToPathWithVersion() {
            IdentifierFormWindow.ShowIfNeedChange(BuildTargetGroup.iOS, () =>
            BuildTime.RestoreSettingsIfFailed(() => {
                var buildVersion = BuildHelperStrings.GetBuildVersion();
                var target = BuildTarget.iOS;
                var options = GetStandartPlayerOptions(target);
                BuildTime.SaveSettingsToRestore();
                PlayerSettings.iOS.buildNumber = BuildHelperStrings.GenBundleNumber().ToString();
                options.locationPathName = BuildHelperStrings.GetBuildPath(target, buildVersion);
                BuildTime.Build(options);
            }));
        }

        [MenuItem("Build/Build all from master branch")]
        public static void BuildAllFromMaster() {
            var branch = GitRequest.CurrentBranch();
            if (branch != BuildHelperStrings.RELEASE_BRANCH)
                GitRequest.Checkout(BuildHelperStrings.RELEASE_BRANCH);

            try {
                BuildWin64ToPathWithVersion();
                BuildAndroidToPathWithVersion();
                BuildIOSToPathWithVersion();
            } finally {
                if (branch != BuildHelperStrings.RELEASE_BRANCH)
                    GitRequest.Checkout(branch);
            }
        }
#endregion

#region Post build async operations
        /// <seealso cref="UserBuildCommands.InstallApkToDeviceAndRun"/>
        [Serializable]
        public class InstallApkParams {
            public string pathToApk;
            public string adbDeviceId;
        }

        /// <summary>
        /// Install apk on android device and run it.
        /// </summary>
        /// <param name="p">Install params.</param>
        public static void InstallApkToDeviceAndRun(InstallApkParams p) {
            var appIdentifier = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
            AdbRequest.InstallToDevice(p.pathToApk, p.adbDeviceId, OnDone: success => {
                if (success)
                    AdbRequest.RunOnDevice(appIdentifier, p.adbDeviceId);
            });
        }
#endregion

#region Utility functions
        /// <summary>
        /// Create default <i>BuildPlayerOptions</i> with specified target and return it.
        /// This options include all scenes that defined in Build Settings window.
        /// </summary>
        /// <param name="target">build target</param>
        /// <returns>new <i>BuildPlayerOptions</i></returns>
        private static BuildPlayerOptions GetStandartPlayerOptions(BuildTarget target) {
            return new BuildPlayerOptions {
                scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes),
                target = target,
                options = BuildOptions.None
            };
        }

        /// <summary>
        /// Build project for Android with specified AndroidTargetDevice.
        /// This function change PlayerSettings and restore it after build.
        /// Different devices will get different bundle version code.
        /// See: <see cref="BuildHelperStrings.GenBundleNumber(UnityEditor.AndroidTargetDevice)"/> 
        /// </summary>
        /// <param name="device">Android target device</param>
        /// <param name="buildVersion">Build version wich will be available by <i>Application.version</i></param>
        /// <param name="options">Build player options</param>
        /// <returns>Build path</returns>
        private static string BuildAndroidForDevice(AndroidTargetDevice device, string buildVersion, BuildPlayerOptions options) {
            BuildTime.SaveSettingsToRestore();
            PlayerSettings.Android.targetDevice = device;
            PlayerSettings.Android.bundleVersionCode = BuildHelperStrings.GenBundleNumber(device);
            var buildPath = BuildHelperStrings.GetBuildPath(BuildTarget.Android, buildVersion, specifyName: device.ToString());
            options.locationPathName = buildPath;
            BuildTime.Build(options);
            return buildPath;
        }
#endregion
    }
}