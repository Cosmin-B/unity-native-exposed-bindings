using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ExposedBindings
{
    /// <summary>
    /// Checks Unity version compatibility for the internal bindings.
    /// The Cecil processor auto-detects internal API signatures at build time,
    /// so the DLL must be rebuilt via ProcessAssembly.sh when crossing major
    /// version boundaries (e.g. 6000.0 -> 6000.3).
    /// </summary>
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public static class UnityVersionChecker
    {
        // Minimum supported version — anything older is unsupported.
        private const string MIN_UNITY_VERSION = "6000.0.31f1";
        private const int MIN_MAJOR = 6000;
        private const int MIN_MINOR = 0;
        private const int MIN_PATCH = 31;

        // Maximum version the Cecil processor has been validated against.
        // Bump this when verifying a new Unity release.
        private const string MAX_VALIDATED_VERSION = "6000.3.99f1";
        private const int MAX_MAJOR = 6000;
        private const int MAX_MINOR = 3;

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
                               $"Minimum supported: {MIN_UNITY_VERSION}.");
                return;
            }

            // Check if version is older than minimum
            if (major < MIN_MAJOR ||
                (major == MIN_MAJOR && minor < MIN_MINOR) ||
                (major == MIN_MAJOR && minor == MIN_MINOR && patch < MIN_PATCH))
            {
                Debug.LogError($"[ExposedBindings] Unity version {currentVersion} is older than the minimum supported version {MIN_UNITY_VERSION}. " +
                             "The internal bindings may not work correctly.");
                return;
            }

            // Warn if version is beyond validated range
            if (major > MAX_MAJOR ||
                (major == MAX_MAJOR && minor > MAX_MINOR))
            {
                Debug.LogWarning($"[ExposedBindings] Unity {currentVersion} is newer than the last validated version ({MAX_VALIDATED_VERSION}). " +
                               "Internal signatures may have changed — rebuild with ProcessAssembly.sh against your Unity install " +
                               "and test thoroughly.");
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

            // Unity version format: "6000.0.31f1" or "6000.3.2f1"
            var parts = versionString.Split('.');
            if (parts.Length < 3)
                return false;

            if (!int.TryParse(parts[0], out major))
                return false;

            if (!int.TryParse(parts[1], out minor))
                return false;

            // Extract patch number (remove 'f1', 'b1', 'a1' suffix)
            var patchStr = parts[2];
            var suffixIndex = patchStr.IndexOfAny(new[] { 'f', 'b', 'a' });
            if (suffixIndex > 0)
            {
                patchStr = patchStr.Substring(0, suffixIndex);
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
                    MinVersion = MIN_UNITY_VERSION,
                    MaxValidatedVersion = MAX_VALIDATED_VERSION,
                    Message = "Could not parse Unity version"
                };
            }

            bool isOlder = major < MIN_MAJOR ||
                          (major == MIN_MAJOR && minor < MIN_MINOR) ||
                          (major == MIN_MAJOR && minor == MIN_MINOR && patch < MIN_PATCH);

            bool isBeyondValidated = major > MAX_MAJOR ||
                                     (major == MAX_MAJOR && minor > MAX_MINOR);

            bool isInRange = !isOlder && !isBeyondValidated;

            return new VersionCompatibility
            {
                IsCompatible = !isOlder,
                IsInValidatedRange = isInRange,
                IsBeyondValidated = isBeyondValidated,
                CurrentVersion = currentVersion,
                MinVersion = MIN_UNITY_VERSION,
                MaxValidatedVersion = MAX_VALIDATED_VERSION,
                Message = isOlder ? "Unity version is older than minimum supported version" :
                         isBeyondValidated ? "Unity version is newer than last validated version — rebuild recommended" :
                         "Unity version is within validated range"
            };
        }

        /// <summary>
        /// Version compatibility information.
        /// </summary>
        public struct VersionCompatibility
        {
            public bool IsCompatible;
            public bool IsInValidatedRange;
            public bool IsBeyondValidated;
            public string CurrentVersion;
            public string MinVersion;
            public string MaxValidatedVersion;
            public string Message;
        }
    }
}
