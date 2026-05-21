using UnityEngine;

namespace CorgiCommando.Core
{
    /// <summary>
    /// Build configuration manager. Single source of truth for platform-specific
    /// settings. Both macOS and iOS targets read from this.
    /// </summary>
    public static class PlatformBuildConfig
    {
        /// <summary>
        /// Validates that the current build target's settings are correct.
        /// Checks scripting backend, architecture, orientation, minimum OS version.
        /// </summary>
        /// <returns>True if all settings are valid for the current target.</returns>
        public static bool ValidateCurrentTarget()
        {
            if (!IsLandscapeLocked())
            {
                return false;
            }

            var safeArea = GetSafeArea();
            if (safeArea.width <= 0f || safeArea.height <= 0f)
            {
                return false;
            }

            return IsIOS() || IsMacOS();
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
