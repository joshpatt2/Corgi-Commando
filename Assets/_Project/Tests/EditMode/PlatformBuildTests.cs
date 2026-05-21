using System;
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
    }
}
