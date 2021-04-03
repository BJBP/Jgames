using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BuildHelper.Editor.Core {
    /// <summary>
    /// Window for select android device
    /// </summary>
    public class AndroidDevicesWindow : EditorWindow {
        private List<string> _devices;
        private Action<string> _onSelected;
        private Action _onCanceled;
        private bool _wasSelected;
        
        /// <summary>
        /// If only one device is connected, <i>OnSelected</i> is called immediately.
        ///  Otherwise, the device selection window is displayed.
        /// </summary>
        /// <param name="OnSelected">Calling if device is selected.
        ///  Argument is device id.</param>
        /// <param name="OnCanceled">Calling if user canceled select.</param>
        public static void ShowIfNeedSelect(Action<string> OnSelected, Action OnCanceled = null) {
            var devices = AdbRequest.GetDevices();
            if (devices.Count == 1)
                OnSelected(devices[0]);
            else {
                var wnd = GetWindow<AndroidDevicesWindow>("Devices");
                wnd._devices = devices;
                wnd._onSelected = OnSelected;
                wnd._onCanceled = OnCanceled;
            }
        }
        
        private void OnDestroy() {
            if (!_wasSelected && _onCanceled != null) {
                _onCanceled();
            }
        }

        private void OnGUI() {
            if (_devices.Count > 0) {
                EditorGUILayout.LabelField("Select Android device:");
                foreach (var device in _devices) {
                    if (GUILayout.Button(device)) {
                        _wasSelected = true;
                        Close();
                        _onSelected(device);
                    }
                }
            } else {
                EditorGUILayout.LabelField("No devices connected", EditorStyles.helpBox);
                if (GUILayout.Button("Try again")) {
                    _devices = AdbRequest.GetDevices();
                }
            }
            
            EditorGUILayout.Space();
            if (GUILayout.Button("Cancel")) {
                Close();
            }
        }
    }
}