using System;
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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns whether the current platform is iOS.
        /// </summary>
        public static bool IsIOS()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns whether the current platform is macOS.
        /// </summary>
        public static bool IsMacOS()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns whether orientation is locked to landscape.
        /// </summary>
        public static bool IsLandscapeLocked()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the Screen.safeArea as a normalized rect.
        /// Used by UI to respect iOS notch/home indicator.
        /// </summary>
        public static Rect GetSafeArea()
        {
            throw new NotImplementedException();
        }
    }
}
