using System.Collections;
using System.Reflection;
using CorgiCommando.Camera;
using CorgiCommando.Core;
using CorgiCommando.Data;
using CorgiCommando.Player;
using CorgiCommando.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CorgiCommando.Tests.PlayMode
{
    [TestFixture]
    public class SceneBootstrapPlayModeTests
    {
        private static readonly BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnityTest]
        public IEnumerator SceneBootstrap_PressMoveRight_CorgiTranslates()
        {
            var root = new GameObject("SceneBootstrap");
            var bootstrap = root.AddComponent<SceneBootstrap>();
            root.AddComponent<SpawnManager>();
            root.AddComponent<GroupTargetCamera>();
            root.AddComponent<ArenaCameraLock>();

            var waveData = ScriptableObject.CreateInstance<WaveData>();
            waveData.waves = new WaveEntry[0];
            SetPrivateField(bootstrap, "_waveData", waveData);

            var attack = ScriptableObject.CreateInstance<AttackData>();
            attack.attackName = "TestAttack";

            var corgiData = ScriptableObject.CreateInstance<CorgiData>();
            corgiData.corgiName = "Sarge";
            corgiData.maxHP = 100;
            corgiData.walkSpeed = 5f;
            corgiData.depthSpeed = 3f;
            corgiData.jumpForce = 10f;
            corgiData.comboChain = new[] { attack };
            corgiData.maxSpecialMeter = 100f;
            corgiData.specialCost = 100f;

            var playerGo = new GameObject("P1");
            playerGo.AddComponent<KinematicMovementController>();
            var player = playerGo.AddComponent<CorgiController>();
            var inputBuffer = new InputBuffer();
            player.Initialize(corgiData, inputBuffer, 0);

            yield return null; // Start SceneBootstrap

            float startX = player.transform.position.x;
            inputBuffer.RecordInput(InputAction.MoveRight, Time.time, new Vector2(1f, 0f));

            // First PlayMode update can have Time.deltaTime == 0, and KinematicMovementController.Tick
            // applies translation as velocity * deltaTime. Retry a few ticks to observe real movement.
            for (int retryCount = 0; retryCount < 3 && player.transform.position.x <= startX; retryCount++)
            {
                yield return null;
            }

            Assert.Greater(player.transform.position.x, startX);

            UnityEngine.Object.Destroy(root);
            UnityEngine.Object.Destroy(playerGo);
            UnityEngine.Object.Destroy(waveData);
            UnityEngine.Object.Destroy(attack);
            UnityEngine.Object.Destroy(corgiData);
        }

        [UnityTest]
        public IEnumerator Coop_P2JoinsMidWave_SpawnsAndAddsCameraTarget()
        {
            var root = new GameObject("SceneBootstrap");
            var bootstrap = root.AddComponent<SceneBootstrap>();
            root.AddComponent<SpawnManager>();
            var camera = root.AddComponent<GroupTargetCamera>();
            root.AddComponent<ArenaCameraLock>();
            var hudGo = new GameObject("HUD");
            hudGo.AddComponent<HUDController>();

            var waveData = ScriptableObject.CreateInstance<WaveData>();
            waveData.waves = new WaveEntry[0];
            SetPrivateField(bootstrap, "_waveData", waveData);

            var attack = ScriptableObject.CreateInstance<AttackData>();
            attack.attackName = "TestAttack";

            var corgiData = ScriptableObject.CreateInstance<CorgiData>();
            corgiData.corgiName = "Sarge";
            corgiData.maxHP = 100;
            corgiData.walkSpeed = 5f;
            corgiData.depthSpeed = 3f;
            corgiData.jumpForce = 10f;
            corgiData.comboChain = new[] { attack };
            corgiData.maxSpecialMeter = 100f;
            corgiData.specialCost = 100f;

            var playerGo = new GameObject("P1");
            playerGo.AddComponent<KinematicMovementController>();
            var player = playerGo.AddComponent<CorgiController>();
            var inputBuffer = new InputBuffer();
            player.Initialize(corgiData, inputBuffer, 0);

            yield return null;

            Assert.IsTrue(bootstrap.TryDropInPlayerTwo());
            yield return null;

            Assert.AreEqual(2, bootstrap.RunState.ActivePlayerCount);
            Assert.NotNull(bootstrap.PlayerTwo);
            Assert.Greater(camera.GetPlayerDistance(), 0f);
            Assert.IsTrue(hudGo.GetComponent<HUDController>().IsPlayerStripVisible(1));

            UnityEngine.Object.Destroy(root);
            UnityEngine.Object.Destroy(playerGo);
            UnityEngine.Object.Destroy(hudGo);
            UnityEngine.Object.Destroy(waveData);
            UnityEngine.Object.Destroy(attack);
            UnityEngine.Object.Destroy(corgiData);
        }

        [UnityTest]
        public IEnumerator Coop_P2DropsOut_CleansUpCleanly()
        {
            var root = new GameObject("SceneBootstrap");
            var bootstrap = root.AddComponent<SceneBootstrap>();
            root.AddComponent<SpawnManager>();
            var camera = root.AddComponent<GroupTargetCamera>();
            root.AddComponent<ArenaCameraLock>();
            var hudGo = new GameObject("HUD");
            var hud = hudGo.AddComponent<HUDController>();

            var waveData = ScriptableObject.CreateInstance<WaveData>();
            waveData.waves = new WaveEntry[0];
            SetPrivateField(bootstrap, "_waveData", waveData);

            var attack = ScriptableObject.CreateInstance<AttackData>();
            attack.attackName = "TestAttack";

            var corgiData = ScriptableObject.CreateInstance<CorgiData>();
            corgiData.corgiName = "Sarge";
            corgiData.maxHP = 100;
            corgiData.walkSpeed = 5f;
            corgiData.depthSpeed = 3f;
            corgiData.jumpForce = 10f;
            corgiData.comboChain = new[] { attack };
            corgiData.maxSpecialMeter = 100f;
            corgiData.specialCost = 100f;

            var playerGo = new GameObject("P1");
            playerGo.AddComponent<KinematicMovementController>();
            var player = playerGo.AddComponent<CorgiController>();
            var inputBuffer = new InputBuffer();
            player.Initialize(corgiData, inputBuffer, 0);

            yield return null;
            Assert.IsTrue(bootstrap.TryDropInPlayerTwo());
            yield return null;

            bootstrap.DropOutPlayerTwo();
            yield return null;

            Assert.AreEqual(1, bootstrap.RunState.ActivePlayerCount);
            Assert.IsNull(bootstrap.PlayerTwo);
            Assert.AreEqual(0f, camera.GetPlayerDistance(), 0.001f);
            Assert.IsFalse(hud.IsPlayerStripVisible(1));

            UnityEngine.Object.Destroy(root);
            UnityEngine.Object.Destroy(playerGo);
            UnityEngine.Object.Destroy(hudGo);
            UnityEngine.Object.Destroy(waveData);
            UnityEngine.Object.Destroy(attack);
            UnityEngine.Object.Destroy(corgiData);
        }

        [UnityTest]
        public IEnumerator Coop_P2Rejoins_AfterDropout_Works()
        {
            var root = new GameObject("SceneBootstrap");
            var bootstrap = root.AddComponent<SceneBootstrap>();
            root.AddComponent<SpawnManager>();
            root.AddComponent<GroupTargetCamera>();
            root.AddComponent<ArenaCameraLock>();

            var waveData = ScriptableObject.CreateInstance<WaveData>();
            waveData.waves = new WaveEntry[0];
            SetPrivateField(bootstrap, "_waveData", waveData);

            var attack = ScriptableObject.CreateInstance<AttackData>();
            attack.attackName = "TestAttack";

            var corgiData = ScriptableObject.CreateInstance<CorgiData>();
            corgiData.corgiName = "Sarge";
            corgiData.maxHP = 100;
            corgiData.walkSpeed = 5f;
            corgiData.depthSpeed = 3f;
            corgiData.jumpForce = 10f;
            corgiData.comboChain = new[] { attack };
            corgiData.maxSpecialMeter = 100f;
            corgiData.specialCost = 100f;

            var playerGo = new GameObject("P1");
            playerGo.AddComponent<KinematicMovementController>();
            var player = playerGo.AddComponent<CorgiController>();
            var inputBuffer = new InputBuffer();
            player.Initialize(corgiData, inputBuffer, 0);

            yield return null;

            Assert.IsTrue(bootstrap.TryDropInPlayerTwo());
            yield return null;
            bootstrap.DropOutPlayerTwo();
            yield return null;
            Assert.IsTrue(bootstrap.TryDropInPlayerTwo());
            yield return null;

            Assert.AreEqual(2, bootstrap.RunState.ActivePlayerCount);
            Assert.NotNull(bootstrap.PlayerTwo);

            UnityEngine.Object.Destroy(root);
            UnityEngine.Object.Destroy(playerGo);
            UnityEngine.Object.Destroy(waveData);
            UnityEngine.Object.Destroy(attack);
            UnityEngine.Object.Destroy(corgiData);
        }

        private static void SetPrivateField<T>(object instance, string fieldName, T value)
        {
            var field = instance.GetType().GetField(fieldName, InstancePrivate);
            Assert.NotNull(field, $"Expected field '{fieldName}' to exist.");
            field.SetValue(instance, value);
        }
    }
}
