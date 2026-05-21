using NUnit.Framework;
using UnityEngine;
using CorgiCommando.Core;
using CorgiCommando.Data;

namespace CorgiCommando.Tests.EditMode
{
    /// <summary>
    /// Tests for platform build configuration.
    /// Validates that settings are correct for macOS and iOS targets.
    /// </summary>
    [TestFixture]
    public class PlatformBuildTests
    {
        [TearDown]
        public void TearDown()
        {
            PlatformBuildConfig.ResetOrientationProvider();
        }

        [Test]
        public void PlatformSettings_LandscapeLocked_DefaultsTrue()
        {
            // Arrange
            var settings = ScriptableObject.CreateInstance<PlatformSettings>();

            // Assert
            Assert.IsTrue(settings.landscapeLocked);

            UnityEngine.Object.DestroyImmediate(settings);
        }

        [Test]
        public void PlatformSettings_iOS_RequiresIL2CPP()
        {
            // Arrange
            var settings = ScriptableObject.CreateInstance<PlatformSettings>();

            // Assert — iOS must use IL2CPP scripting backend
            Assert.AreEqual("IL2CPP", settings.iOSScriptingBackend);

            UnityEngine.Object.DestroyImmediate(settings);
        }

        [Test]
        public void PlatformSettings_iOS_RequiresARM64()
        {
            // Arrange
            var settings = ScriptableObject.CreateInstance<PlatformSettings>();

            // Assert
            Assert.AreEqual("ARM64", settings.iOSArchitecture);

            UnityEngine.Object.DestroyImmediate(settings);
        }

        [Test]
        public void PlatformSettings_iOS_MinimumVersion13()
        {
            // Arrange
            var settings = ScriptableObject.CreateInstance<PlatformSettings>();

            // Assert — minimum iOS 13 for Game Controller framework
            Assert.AreEqual("13.0", settings.minimumiOSVersion);

            UnityEngine.Object.DestroyImmediate(settings);
        }

        [Test]
        public void PlatformBuildConfig_GetSafeArea_ReturnsValidRect()
        {
            // Act
            var safeArea = PlatformBuildConfig.GetSafeArea();

            // Assert — safe area should have positive dimensions
            Assert.Greater(safeArea.width, 0f);
            Assert.Greater(safeArea.height, 0f);
        }

        [Test]
        public void PlatformSettings_ResourceAsset_HasRequiredDefaults()
        {
            // Act
            var settings = Resources.Load<PlatformSettings>("PlatformSettings");
            var defaults = ScriptableObject.CreateInstance<PlatformSettings>();

            try
            {
                // Assert
                Assert.IsNotNull(settings);
                Assert.AreEqual("IL2CPP", settings.iOSScriptingBackend);
                Assert.AreEqual("ARM64", settings.iOSArchitecture);
                Assert.AreEqual("13.0", settings.minimumiOSVersion);
                Assert.AreEqual(defaults.iOSScriptingBackend, settings.iOSScriptingBackend);
                Assert.AreEqual(defaults.iOSArchitecture, settings.iOSArchitecture);
                Assert.AreEqual(defaults.minimumiOSVersion, settings.minimumiOSVersion);
                Assert.AreEqual(defaults.landscapeLocked, settings.landscapeLocked);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(defaults);
            }
        }

        [Test]
        public void PlatformBuildConfig_IsIOS_MatchesRuntimePlatform()
        {
            Assert.AreEqual(
                Application.platform == RuntimePlatform.IPhonePlayer,
                PlatformBuildConfig.IsIOS());
        }

        [Test]
        public void PlatformBuildConfig_IsMacOS_MatchesRuntimePlatform()
        {
            Assert.AreEqual(
                Application.platform == RuntimePlatform.OSXPlayer
                    || Application.platform == RuntimePlatform.OSXEditor,
                PlatformBuildConfig.IsMacOS());
        }

        [Test]
        public void PlatformBuildConfig_IsLandscapeLocked_ReturnsTrueForLandscapeOrientations()
        {
            PlatformBuildConfig.SetOrientationProvider(() => ScreenOrientation.LandscapeLeft);
            Assert.IsTrue(PlatformBuildConfig.IsLandscapeLocked());

            PlatformBuildConfig.SetOrientationProvider(() => ScreenOrientation.LandscapeRight);
            Assert.IsTrue(PlatformBuildConfig.IsLandscapeLocked());

            PlatformBuildConfig.SetOrientationProvider(() => ScreenOrientation.Portrait);
            Assert.IsFalse(PlatformBuildConfig.IsLandscapeLocked());

            PlatformBuildConfig.SetOrientationProvider(() => ScreenOrientation.PortraitUpsideDown);
            Assert.IsFalse(PlatformBuildConfig.IsLandscapeLocked());
        }
    }
}
