using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
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
        public void BossBannerUI_UpdateHP_StoresValues()
        {
            // Arrange
            var go = new GameObject("BossBanner");
            go.AddComponent<RectTransform>();
            var banner = go.AddComponent<BossBannerUI>();
            banner.Show("WHISKERBOT-9000", 200, 200);

            // Act
            banner.UpdateHP(75, 120);

            // Assert
            Assert.AreEqual(75, banner.CurrentHP);
            Assert.AreEqual(120, banner.MaxHP);

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

        [Test]
        public void HUDController_UpdateHealthBar_StoresPerPlayerState()
        {
            // Arrange
            var go = new GameObject("HUD");
            var hud = go.AddComponent<HUDController>();

            // Act
            hud.UpdateHealthBar(0, 65, 100);
            hud.UpdateHealthBar(1, 42, 80);

            // Assert
            Assert.AreEqual(65, hud.GetCurrentHealth(0));
            Assert.AreEqual(100, hud.GetMaxHealth(0));
            Assert.AreEqual(42, hud.GetCurrentHealth(1));
            Assert.AreEqual(80, hud.GetMaxHealth(1));

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void HUDController_UpdateSpecialMeter_ClampsToMax()
        {
            // Arrange
            var go = new GameObject("HUD");
            var hud = go.AddComponent<HUDController>();

            // Act
            hud.UpdateSpecialMeter(0, 150f, 100f);

            // Assert
            Assert.AreEqual(100f, hud.GetCurrentSpecialMeter(0));
            Assert.AreEqual(100f, hud.GetMaxSpecialMeter(0));

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void HUDController_Awake_CreatesVisualElements()
        {
            // Arrange
            var go = new GameObject("HUD");
            go.AddComponent<HUDController>();

            // Assert
            Assert.NotNull(go.transform.Find("HUDCanvas/P1HUD"));
            Assert.NotNull(go.transform.Find("HUDCanvas/P1HUD/HealthBarBG"));
            Assert.NotNull(go.transform.Find("HUDCanvas/P1HUD/SpecialMeterBG"));
            Assert.NotNull(go.transform.Find("HUDCanvas/P2HUD"));
            Assert.NotNull(go.transform.Find("HUDCanvas/PauseMenuPanel"));
            Assert.NotNull(go.transform.Find("HUDCanvas/ComboCounter"));
            Assert.NotNull(go.transform.Find("HUDCanvas/BossBanner"));
            Assert.Greater(go.GetComponentsInChildren<Image>(true).Length, 5);
            Assert.NotNull(go.GetComponentInChildren<ComboCounterUI>(true));
            Assert.NotNull(go.GetComponentInChildren<BossBannerUI>(true));
            Assert.IsTrue(go.GetComponent<HUDController>().HasVisualHierarchy());
            Assert.NotNull(go.GetComponent<HUDController>().GetHealthFillImage(0));
            Assert.NotNull(go.GetComponent<HUDController>().GetSpecialMeterFillImage(0));

            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
