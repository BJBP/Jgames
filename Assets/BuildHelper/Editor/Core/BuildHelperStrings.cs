using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BuildHelper.Editor.Core {
    internal static class BuildHelperStrings {
        /// <summary>
        /// Path to <i>git</i> executable
        /// </summary>
        public const string GIT_EXEC_PATH = "git";
        /// <summary>
        /// A branch of which are builds release version of application.
        /// Build version generated from this branch will have pretty number format.
        /// </summary>
        /// <seealso cref="GetBuildVersion"/>
        public const string RELEASE_BRANCH = "master";
        /// <summary>
        /// Minor version prefix for develop builds.
        /// </summary>
        /// <seealso cref="GetBuildVersion"/>
        public const string PREFIX_DEVELOP = "d-";

#region Locations
        /// <summary>
        /// Returns root path for current project or full path for specified relative path.
        /// </summary>
        /// <param name="path">Path relative to the project root. Default is <i>null</i>.
        /// Can not start with the "/" character.</param>
        /// <returns>Root path for current project + specified relative path</returns>
        public static string ProjRoot(string path = null) {
            var root = Directory.GetParent(Application.dataPath).FullName;
            if (path != null) {
                root += "/" + path;
            }
            return root;
        }

        /// <summary>
        /// Generates path to builds. Has 2 options:
        /// <list type="bullet">
        ///     <item>
        ///         <description>If result path is the file: <br/><![CDATA[ 
        ///         <builds_root>/<product_name> <version>[.<target>]/<product_name>[.<specifyName>].<extension>
        ///         ]]> <br/> e.g. <b>/Builds/my_app 2.1.234/my_app.exe</b> or<br/>
        ///         <b>/Builds/my_app 2.1.234.Android/my_app.ARMv7.apk</b>
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>If result path is the directory: <br/><![CDATA[ 
        ///         <builds_root>/<product_name> <version>[.<target>][.<specifyName>]
        ///         ]]> <br/> e.g. <b>/Builds/my_app 2.1.234.iOS</b>
        ///         </description>
        ///     </item>
        /// </list> Where <i>builds_root</i> - build location defined in Unity Build Settings window,
        /// <i>product_name</i> - Product name from Player Settings, 
        /// <i>product_name</i> - file extension depending on the target. 
        /// If user not define build location for current target, folder panel will open.
        /// </summary>
        /// <param name="target">Target of build</param>
        /// <param name="version">Version of build</param>
        /// <param name="specifyTarget">Default is <i>true</i>. If <i>true</i> then 
        /// name of target will be present in the returned path.</param>
        /// <param name="specifyName">Optional</param>
        /// <returns>Generated path to builds</returns>
        /// <exception cref="OperationCanceledException">User closed folder panel.</exception>
        public static string GetBuildPath(BuildTarget target, string version, bool specifyTarget = true, string specifyName = null) {
            var pathBuild = EditorUserBuildSettings.GetBuildLocation(target);
            var fileExtension = BuildLocationFileExtension(target);
            if (string.IsNullOrEmpty(pathBuild)) {
                var title = string.Format("Select folder for {0} builds for this project", target);
                pathBuild = EditorUtility.OpenFolderPanel(title, "", "");
                if (pathBuild == null) {
                    throw new OperationCanceledException("User cancel build");
                }
                if (fileExtension != null) {
                    pathBuild += "/" + PlayerSettings.productName + fileExtension;
                }
                EditorUserBuildSettings.SetBuildLocation(target, pathBuild);
            }
            
            var targetStr = specifyTarget ? "." + BuiltTargetToPrettyString(target) : "";
            var specifyExt = string.IsNullOrEmpty(specifyName) ? "" : "." + specifyName; 
            if (fileExtension != null) {
                var pathSeparators = new char[] {'/', '\\'};
                var subLength = Math.Min(Math.Max(0, pathBuild.LastIndexOfAny(pathSeparators)), pathBuild.Length);
                pathBuild = pathBuild.Substring(0, subLength).TrimEnd(pathSeparators);
                return string.Format("{0}/{1} {2}{3}/{1}{4}{5}", 
                    pathBuild, PlayerSettings.productName, version, targetStr, specifyExt, fileExtension);
            } else {
                return string.Format("{0}/{1} {2}{3}{4}", 
                    pathBuild, PlayerSettings.productName, version, targetStr, specifyExt);
            }
        }
#endregion
        
#region Versions
        /// <summary>
        /// Generate version of build for current git repository state.<br/>
        /// Format: <![CDATA[<major>.<minor>]]><br/>
        /// There is two type of minor versions:
        /// <list type="bullet">
        ///     <item>
        ///         <description>Minor version is pretty number, e.g. "2.1.234". 
        ///         Returned if current branch is equal to <see cref="RELEASE_BRANCH"/></description>
        ///     </item>
        ///     <item>
        ///         <description>Minor version is revision id, e.g. "2.1.d-1685c5a". 
        ///         Returned if current branch is <b>not</b> equal to <see cref="RELEASE_BRANCH"/></description>
        ///     </item>
        /// </list>
        /// Major version is a Version in Unity Player Settings (<see cref="PlayerSettings.bundleVersion"/>).
        /// </summary>
        /// <seealso cref="GitRequest.Revision"/>
        /// <returns>Ganerated version</returns>
        public static string GetBuildVersion() {
            var branch = GitRequest.CurrentBranch();
            string version;
            if (branch == RELEASE_BRANCH) {
                version = GitRequest.Revision(true);
            } else {
                Debug.LogWarningFormat("Build development version from '{0}'", branch);
                version = PREFIX_DEVELOP + GitRequest.Revision(false);
            }
            return PlayerSettings.bundleVersion + "." + version;
        }

        /// <summary>
        /// Generate bundle number of build for Android.<br/>
        /// Format: <![CDATA[<revision><branchId><targetId>]]><br/>
        /// Where <i>revision</i> - parsed number from <see cref="GitRequest.Revision"/>;
        /// <i>branchId</i> - 0 if current branch is equal to <see cref="RELEASE_BRANCH"/>, otherwise 1;
        /// <i>targetId</i> - number equivalent of <i>targetDevice</i>.<br/>
        /// Example return: "23403" - revision is 234, release branch, Android target - ARMv7.
        /// </summary>
        /// <param name="targetDevice">Android target device</param>
        /// <returns>Generated bundle number</returns>
        public static int GenBundleNumber(AndroidTargetDevice targetDevice) {
            return GenBundleNumber((int) targetDevice);
        }
        
        /// <summary>
        /// Generate bundle number of build for Android and iOS.<br/>
        /// Format: <![CDATA[<revision><branchId><targetId>]]><br/>
        /// Where <i>revision</i> - parsed number from <see cref="GitRequest.Revision"/>;
        /// <i>branchId</i> - 0 if current branch is equal to <see cref="RELEASE_BRANCH"/>, otherwise 1.<br/>
        /// Example return: "23401" - revision is 234, release branch, targetId is 1.
        /// </summary>
        /// <param name="targetId">Default is 0. Target id for distinguish between builds for different 
        /// targets on one platform (e.g. ARMv7 and x86 on Android).</param>
        /// <returns>Generated bundle number</returns>
        public static int GenBundleNumber(int targetId = 0) {
            var rev = GitRequest.Revision(true);
            int branchId =  GitRequest.CurrentBranch() == RELEASE_BRANCH ? 0 : 1;
            return int.Parse(string.Format("{0}{1}{2}", rev, branchId, targetId));
        }
#endregion

#region Strings by targets
        /// <summary>
        /// Get pretty name of specified build target.
        /// e.g. "Win64", "WSA", "Linux"... 
        /// </summary>
        /// <param name="target">Build target</param>
        /// <returns>Pretty name of build target.</returns>
        public static string BuiltTargetToPrettyString(BuildTarget target) {
            switch (target) {
                case BuildTarget.StandaloneWindows:
                    return "Win86";
                case BuildTarget.StandaloneWindows64:
                    return "Win64";
                case BuildTarget.WSAPlayer:
                    return "WSA";
                case BuildTarget.StandaloneLinux:
                    return "Linux";
                case BuildTarget.StandaloneLinux64:
                    return "Linux64";
                case BuildTarget.StandaloneLinuxUniversal:
                    return "Linux-Uni";
                default:
                    return target.ToString();
            }
        }
        
        /// <summary>
        /// Get build location file extension for specified build target.
        /// e.g. ".exe" for <i>StandaloneWindows</i>, ".apk" for <i>Android</i>,
        /// <i>null</i> for <i>iOS</i>...
        /// </summary>
        /// <param name="target">Build target</param>
        /// <returns><i>String</i> if build location for specified target is file, 
        /// otherwise <i>null</i></returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// File extension for specified target is not defined.</exception>
        public static string BuildLocationFileExtension(BuildTarget target) {
            switch (target) {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return ".exe";
                case BuildTarget.StandaloneLinux:
                    return ".x86";
                case BuildTarget.StandaloneLinux64:
                case BuildTarget.StandaloneLinuxUniversal:
                    return ".x86-64";
                case BuildTarget.Android:
                    return ".apk";
                    
                case BuildTarget.StandaloneOSXIntel:
                case BuildTarget.StandaloneOSXIntel64:
                case BuildTarget.StandaloneOSXUniversal:
                case BuildTarget.WebGL:
                case BuildTarget.iOS:
                case BuildTarget.WSAPlayer:
                    return null;
                    
                default:
                    throw new ArgumentOutOfRangeException("target", target, 
                        "Specify whether export file extension for target " + target);
            }
        }
#endregion
    }
}