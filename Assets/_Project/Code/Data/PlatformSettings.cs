using UnityEngine;

namespace CorgiCommando.Data
{
    /// <summary>
    /// ScriptableObject holding platform-specific build settings.
    /// Single source of truth — both macOS and iOS targets read from this.
    /// </summary>
    [CreateAssetMenu(fileName = "PlatformSettings", menuName = "CorgiCommando/PlatformSettings")]
    public class PlatformSettings : ScriptableObject
    {
        [Header("iOS")]
        [Tooltip("Minimum iOS version (13+)")]
        public string minimumiOSVersion = "13.0";

        [Tooltip("iOS scripting backend (must be IL2CPP)")]
        public string iOSScriptingBackend = "IL2CPP";

        [Tooltip("iOS target architecture (must be ARM64)")]
        public string iOSArchitecture = "ARM64";

        [Header("macOS")]
        [Tooltip("macOS scripting backend (Mono or IL2CPP)")]
        public string macOSScriptingBackend = "Mono";

        [Tooltip("macOS architecture (Universal — Apple Silicon + Intel)")]
        public string macOSArchitecture = "Universal";

        [Header("Shared")]
        [Tooltip("Lock orientation to landscape on all platforms")]
        public bool landscapeLocked = true;

        [Tooltip("Target frame rate")]
        public int targetFrameRate = 60;
    }
}
