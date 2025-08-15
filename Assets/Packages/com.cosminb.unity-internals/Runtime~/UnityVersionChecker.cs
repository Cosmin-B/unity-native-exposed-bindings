using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ExposedBindings
{
    /// <summary>
    /// Checks Unity version compatibility for the internal bindings.
    /// </summary>
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public static class UnityVersionChecker
    {
        private const string TARGET_UNITY_VERSION = "6000.0.31f1";
        private const int TARGET_MAJOR = 6000;
        private const int TARGET_MINOR = 0;
        private const int TARGET_PATCH = 31;
        
        static UnityVersionChecker()
        {
            CheckUnityVersion();
        }

        /// <summary>
        /// Checks if the current Unity version is compatible with the bindings.
        /// </summary>
        public static void CheckUnityVersion()
        {
            var currentVersion = Application.unityVersion;
            
            if (!ParseUnityVersion(currentVersion, out int major, out int minor, out int patch))
            {
                Debug.LogWarning($"[ExposedBindings] Could not parse Unity version: {currentVersion}. " +
                               $"This library was built for Unity {TARGET_UNITY_VERSION}.");
                return;
            }

            // Check if version is older than target
            if (major < TARGET_MAJOR || 
                (major == TARGET_MAJOR && minor < TARGET_MINOR) ||
                (major == TARGET_MAJOR && minor == TARGET_MINOR && patch < TARGET_PATCH))
            {
                Debug.LogError($"[ExposedBindings] Unity version {currentVersion} is older than the minimum supported version {TARGET_UNITY_VERSION}. " +
                             "The internal bindings may not work correctly.");
                return;
            }

            // Warn if version is newer
            if (major > TARGET_MAJOR || 
                (major == TARGET_MAJOR && minor > TARGET_MINOR) ||
                (major == TARGET_MAJOR && minor == TARGET_MINOR && patch > TARGET_PATCH))
            {
                Debug.LogWarning($"[ExposedBindings] This library was built for Unity {TARGET_UNITY_VERSION}, " +
                               $"but you are using {currentVersion}. " +
                               "Unity's internal signatures may have changed. " +
                               "Please test thoroughly and consider updating the bindings if issues occur.");
            }
        }

        /// <summary>
        /// Parses a Unity version string into major, minor, and patch components.
        /// </summary>
        private static bool ParseUnityVersion(string versionString, out int major, out int minor, out int patch)
        {
            major = 0;
            minor = 0;
            patch = 0;

            if (string.IsNullOrEmpty(versionString))
                return false;

            // Unity version format: "6000.0.31f1" or "2022.3.10f1"
            var parts = versionString.Split('.');
            if (parts.Length < 3)
                return false;

            if (!int.TryParse(parts[0], out major))
                return false;

            if (!int.TryParse(parts[1], out minor))
                return false;

            // Extract patch number (remove 'f1' suffix)
            var patchStr = parts[2];
            var fIndex = patchStr.IndexOf('f');
            if (fIndex > 0)
            {
                patchStr = patchStr.Substring(0, fIndex);
            }

            if (!int.TryParse(patchStr, out patch))
                return false;

            return true;
        }

        /// <summary>
        /// Gets version compatibility information.
        /// </summary>
        public static VersionCompatibility GetCompatibility()
        {
            var currentVersion = Application.unityVersion;
            
            if (!ParseUnityVersion(currentVersion, out int major, out int minor, out int patch))
            {
                return new VersionCompatibility
                {
                    IsCompatible = false,
                    CurrentVersion = currentVersion,
                    TargetVersion = TARGET_UNITY_VERSION,
                    Message = "Could not parse Unity version"
                };
            }

            bool isOlder = major < TARGET_MAJOR || 
                          (major == TARGET_MAJOR && minor < TARGET_MINOR) ||
                          (major == TARGET_MAJOR && minor == TARGET_MINOR && patch < TARGET_PATCH);

            bool isNewer = major > TARGET_MAJOR || 
                          (major == TARGET_MAJOR && minor > TARGET_MINOR) ||
                          (major == TARGET_MAJOR && minor == TARGET_MINOR && patch > TARGET_PATCH);

            bool isExactMatch = major == TARGET_MAJOR && minor == TARGET_MINOR && patch == TARGET_PATCH;

            return new VersionCompatibility
            {
                IsCompatible = !isOlder,
                IsExactMatch = isExactMatch,
                IsNewer = isNewer,
                CurrentVersion = currentVersion,
                TargetVersion = TARGET_UNITY_VERSION,
                Message = isOlder ? "Unity version is older than minimum supported version" :
                         isNewer ? "Unity version is newer than target version - internals may have changed" :
                         "Unity version matches target version"
            };
        }

        /// <summary>
        /// Version compatibility information.
        /// </summary>
        public struct VersionCompatibility
        {
            public bool IsCompatible;
            public bool IsExactMatch;
            public bool IsNewer;
            public string CurrentVersion;
            public string TargetVersion;
            public string Message;
        }
    }
}