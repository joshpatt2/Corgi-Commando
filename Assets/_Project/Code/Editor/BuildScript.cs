using UnityEditor;
using UnityEngine;
using System.IO;

namespace CorgiCommando.Editor
{
    /// <summary>
    /// CLI build scripts for macOS and iOS targets.
    /// Called via: Unity -batchmode -executeMethod CorgiCommando.Editor.BuildScript.BuildMacOS
    /// </summary>
    public static class BuildScript
    {
        private static readonly string[] Scenes = new string[]
        {
            "Assets/_Project/Scenes/Level_Backyard.unity"
        };

        public static void BuildMacOS()
        {
            string buildPath = Path.Combine("Builds", "macOS", "CorgiCommando.app");
            Directory.CreateDirectory(Path.GetDirectoryName(buildPath));

            var options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = buildPath,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.LogError($"macOS build failed: {report.summary.totalErrors} errors");
                EditorApplication.Exit(1);
            }
            else
            {
                Debug.Log($"macOS build succeeded: {buildPath}");
                EditorApplication.Exit(0);
            }
        }

        public static void BuildIOS()
        {
            string buildPath = Path.Combine("Builds", "iOS");
            Directory.CreateDirectory(buildPath);

            var options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = buildPath,
                target = BuildTarget.iOS,
                options = BuildOptions.None
            };

            // iOS-specific settings
            PlayerSettings.iOS.targetOSVersionString = "13.0";
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.appleEnableAutomaticSigning = false;

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.LogError($"iOS build failed: {report.summary.totalErrors} errors");
                EditorApplication.Exit(1);
            }
            else
            {
                Debug.Log($"iOS build succeeded: {buildPath}");
                EditorApplication.Exit(0);
            }
        }

        public static void RunEditModeTests()
        {
            // Tests are run via: Unity -batchmode -runTests -testPlatform EditMode
            // This method exists for documentation purposes.
            Debug.Log("Use -runTests -testPlatform EditMode instead");
        }
    }
}
