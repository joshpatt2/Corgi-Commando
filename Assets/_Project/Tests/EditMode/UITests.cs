using System;
using NUnit.Framework;
using UnityEngine;
using CorgiCommando.UI;

namespace CorgiCommando.Tests.EditMode
{
    /// <summary>
    /// Edit Mode tests for UI components — HUD, combo counter, boss banner.
    /// Tests verify state tracking. Visual rendering is Play Mode / manual.
    /// </summary>
    [TestFixture]
    public class UITests
    {
        [Test]
        public void ComboCounterUI_SetComboCount_UpdatesDisplay()
        {
            // Arrange
            var go = new GameObject("ComboCounter");
            var counter = go.AddComponent<ComboCounterUI>();

            // Act
            counter.SetComboCount(3);

            // Assert
            Assert.AreEqual(3, counter.DisplayedComboCount);
            Assert.IsTrue(counter.IsVisible);

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void ComboCounterUI_FadeOut_HidesCounter()
        {
            // Arrange
            var go = new GameObject("ComboCounter");
            var counter = go.AddComponent<ComboCounterUI>();
            counter.SetComboCount(5);

            // Act
            counter.FadeOut();

            // Assert — after fade, counter should be hidden
            // Design intent: FadeOut triggers an animation. For test purposes,
            // we check the final state. Implementation may use a coroutine.
            Assert.IsFalse(counter.IsVisible);

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void BossBannerUI_Show_DisplaysBossInfo()
        {
            // Arrange
            var go = new GameObject("BossBanner");
            var banner = go.AddComponent<BossBannerUI>();

            // Act
            banner.Show("WHISKERBOT-9000", 200, 200);

            // Assert
            Assert.IsTrue(banner.IsVisible);
            Assert.AreEqual("WHISKERBOT-9000", banner.BossName);

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void BossBannerUI_Hide_RemovesBanner()
        {
            // Arrange
            var go = new GameObject("BossBanner");
            var banner = go.AddComponent<BossBannerUI>();
            banner.Show("WHISKERBOT-9000", 200, 200);

            // Act
            banner.Hide();

            // Assert
            Assert.IsFalse(banner.IsVisible);

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void HUDController_TogglePause_SetsIsPaused()
        {
            // Arrange
            var go = new GameObject("HUD");
            var hud = go.AddComponent<HUDController>();

            // Act
            hud.TogglePause();

            // Assert
            Assert.IsTrue(hud.IsPaused);

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void HUDController_TogglePauseTwice_Unpauses()
        {
            // Arrange
            var go = new GameObject("HUD");
            var hud = go.AddComponent<HUDController>();

            // Act
            hud.TogglePause(); // pause
            hud.TogglePause(); // unpause

            // Assert
            Assert.IsFalse(hud.IsPaused);

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void HUDController_IsSafeAreaApplied_DefaultsFalse()
        {
            // Arrange
            var go = new GameObject("HUD");
            var hud = go.AddComponent<HUDController>();

            // Assert — safe area not applied until ApplySafeArea() is called
            Assert.IsFalse(hud.IsSafeAreaApplied());

            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
