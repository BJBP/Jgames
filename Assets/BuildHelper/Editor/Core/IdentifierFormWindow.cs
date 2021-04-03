using System;
using UnityEditor;
using UnityEngine;

namespace BuildHelper.Editor.Core {
    /// <summary>
    /// A window in which you can change the <i>App Identifier</i> 
    /// for a given build target or for all build targets at once.
    /// </summary>
    public class IdentifierFormWindow : EditorWindow {
        private const string _TITLE = "App Identifier";
        private const string _NOT_VALID_BUNDLE = "com.Company.ProductName";
        private string _identifier = "";
        private BuildTargetGroup _buildTarget;
        private bool _forAll;
        private bool _forAllForce;
        private Action _callback;
        
        /// <summary>
        /// Show window for change <i>App Identifier</i> for all build targets.
        /// </summary>
        /// <param name="callback">Invoked after window is closed</param>
        /// <seealso cref="ShowWindow(BuildTargetGroup, System.Action)"/>
        public static void ShowWindow(Action callback = null) {
            var wnd = CreateWindow(BuildTargetGroup.Standalone, callback);
            wnd._forAllForce = true;
        }
        
        /// <summary>
        /// Show window for change <i>App Identifier</i> for specified build target.
        /// </summary>
        /// <param name="buildTarget">Build target</param>
        /// <param name="callback">Invoked after window is closed</param>
        /// <seealso cref="ShowWindow(System.Action)"/>
        public static void ShowWindow(BuildTargetGroup buildTarget, Action callback = null) {
            CreateWindow(buildTarget, callback);
        }
        
        /// <summary>
        /// if <i>App Identifier</i> for specified target is not setted 
        /// (empty or equals to "com.Company.ProductName")
        /// then show window for change <i>App Identifier</i>. 
        /// Else just invoke <i>callback</i>.
        /// </summary>
        /// <param name="buildTarget">Build target</param>
        /// <param name="callback">Invoked after window is closed
        ///  or <i>App Identifier</i> passed the test immediately.</param>
        /// <seealso cref="ShowWindow(BuildTargetGroup, System.Action)"/>
        public static void ShowIfNeedChange(BuildTargetGroup buildTarget, Action callback = null) {
            var identifier = PlayerSettings.GetApplicationIdentifier(buildTarget);
            if (string.IsNullOrEmpty(identifier) || identifier == _NOT_VALID_BUNDLE) {
                CreateWindow(buildTarget, callback);
            } else {
                if (callback != null)
                    callback();
            }
        }

        private static IdentifierFormWindow CreateWindow(BuildTargetGroup buildTarget, Action callback) {
            var wnd = GetWindow<IdentifierFormWindow>(_TITLE);
            wnd._identifier = PlayerSettings.GetApplicationIdentifier(buildTarget);
            wnd._buildTarget = buildTarget;
            wnd._callback = callback;
            return wnd;
        }

        private void OnDestroy() {
            if (_callback != null)
                _callback();
        }

        private void OnGUI() {
            EditorGUILayout.Space();
            _identifier = EditorGUILayout.TextField("Identifier", _identifier);
            if (!_forAllForce)
                _forAll = EditorGUILayout.Toggle("All build targets", _forAll);
            if (GUILayout.Button("Save for " + WichTargets())) {
                if (Save())
                    Close();
            }
        }

        private string WichTargets() {
            if (IsForAll())
                return " all";
            return _buildTarget.ToString();
        }

        private bool Save() {
            try {
                BuildTime.SaveSettingsToRestore();
                if (!IsForAll()) {
                    PlayerSettings.SetApplicationIdentifier(_buildTarget, _identifier);
                } else {
                    foreach (BuildTargetGroup target in Enum.GetValues(typeof(BuildTargetGroup))) {
                        PlayerSettings.SetApplicationIdentifier(target, _identifier);
                    }
                }
                BuildTime.AcceptChangedSettings();
                return true;
            } catch (Exception e) {
                BuildTime.RestoreSettings();
                Debug.LogError(e);
            }
            return false;
        }
        
        private bool IsForAll() {
            return _forAll || _forAllForce;
        }
    }
}