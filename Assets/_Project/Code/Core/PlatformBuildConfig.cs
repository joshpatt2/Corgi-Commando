using UnityEngine;
using CorgiCommando.Data;

namespace CorgiCommando.Core
{
    /// <summary>
    /// Build configuration manager. Single source of truth for platform-specific
    /// settings. Both macOS and iOS targets read from this.
    /// </summary>
    public static class PlatformBuildConfig
    {
        private const string RequiredIOSBackend = "IL2CPP";
        private const string RequiredIOSArchitecture = "ARM64";
        private const string RequiredMinimumIOSVersion = "13.0";

        /// <summary>
        /// Validates runtime platform requirements and required PlatformSettings values.
        /// Checks platform support, optional landscape lock, safe area validity, and iOS required settings.
        /// </summary>
        /// <returns>True if all settings are valid for the current target.</returns>
        public static bool ValidateCurrentTarget()
        {
            if (!IsIOS() && !IsMacOS())
            {
                return false;
            }

            var settings = Resources.Load<PlatformSettings>("PlatformSettings");
            if (settings == null)
            {
                return false;
            }

            if (settings.landscapeLocked && !IsLandscapeLocked())
            {
                return false;
            }

            var safeArea = GetSafeArea();
            if (safeArea.width <= 0f || safeArea.height <= 0f)
            {
                return false;
            }

            if (IsIOS())
            {
                return settings.iOSScriptingBackend == RequiredIOSBackend
                    && settings.iOSArchitecture == RequiredIOSArchitecture
                    && settings.minimumiOSVersion == RequiredMinimumIOSVersion;
            }

            return true;
        }

        /// <summary>
        /// Returns whether the current platform is iOS.
        /// </summary>
        public static bool IsIOS()
        {
            return Application.platform == RuntimePlatform.IPhonePlayer;
        }

        /// <summary>
        /// Returns whether the current platform is macOS.
        /// </summary>
        public static bool IsMacOS()
        {
            return Application.platform == RuntimePlatform.OSXPlayer
                || Application.platform == RuntimePlatform.OSXEditor;
        }

        /// <summary>
        /// Returns whether orientation is locked to landscape.
        /// </summary>
        public static bool IsLandscapeLocked()
        {
            return Screen.orientation == ScreenOrientation.LandscapeLeft
                || Screen.orientation == ScreenOrientation.LandscapeRight;
        }

        /// <summary>
        /// Returns the Screen.safeArea as a normalized rect.
        /// Used by UI to respect iOS notch/home indicator.
        /// </summary>
        public static Rect GetSafeArea()
        {
            var fallbackRect = new Rect(
                0f,
                0f,
                Mathf.Max(1f, Screen.width),
                Mathf.Max(1f, Screen.height));

            if (!IsIOS())
            {
                return fallbackRect;
            }

            var safeArea = Screen.safeArea;
            if (safeArea.width <= 0f || safeArea.height <= 0f)
            {
                return fallbackRect;
            }

            return safeArea;
        }
    }
}
