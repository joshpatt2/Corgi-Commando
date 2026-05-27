using System.Collections;
using System.IO;
using System.Reflection;
using CorgiCommando.Camera;
using CorgiCommando.Core;
using CorgiCommando.Data;
using CorgiCommando.Player;
using CorgiCommando.Testing;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CorgiCommando.Tests.PlayMode
{
    [TestFixture]
    public class SceneBootstrapPlayModeTests
    {
        private static readonly BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
        private const float MovementObservationDelaySeconds = 0.02f;

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
            Assert.AreEqual(new Vector2(1f, 0f), inputBuffer.GetMoveAxis());

            yield return new WaitForSeconds(MovementObservationDelaySeconds);

            Assert.Greater(player.transform.position.x, startX);

            UnityEngine.Object.Destroy(root);
            UnityEngine.Object.Destroy(playerGo);
            UnityEngine.Object.Destroy(waveData);
            UnityEngine.Object.Destroy(attack);
            UnityEngine.Object.Destroy(corgiData);
        }

        [UnityTest]
        public IEnumerator PlaytestMetrics_InputLifecycle_RecordAndConsume_BothLogged()
        {
            PlaytestMetrics.Reset();
            PlaytestMetrics.IsRecording = true;

            var playerGo = new GameObject("P1");
            playerGo.AddComponent<KinematicMovementController>();
            var player = playerGo.AddComponent<CorgiController>();

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

            var inputBuffer = new InputBuffer();
            player.Initialize(corgiData, inputBuffer, 0);

            float timestamp = Time.time;
            inputBuffer.RecordInput(InputAction.MoveRight, timestamp, new Vector2(1f, 0f));
            player.Tick(1f / 60f);

            string path = Path.Combine(Path.GetTempPath(), "playtest_metrics_input_lifecycle.json");
            PlaytestMetrics.WriteReport(path);
            var report = JsonUtility.FromJson<PlaytestMetrics.PlaytestReport>(File.ReadAllText(path));

            bool recordedFound = false;
            for (int i = 0; i < report.inputRecorded.Count; i++)
            {
                if (report.inputRecorded[i].action == InputAction.MoveRight)
                {
                    recordedFound = true;
                    break;
                }
            }

            bool consumedFound = false;
            for (int i = 0; i < report.inputConsumed.Count; i++)
            {
                if (report.inputConsumed[i].action == InputAction.MoveRight &&
                    report.inputConsumed[i].consumerComponentId == nameof(KinematicMovementController))
                {
                    consumedFound = true;
                    break;
                }
            }

            Assert.IsTrue(recordedFound, "Expected MoveRight to be logged in inputRecorded.");
            Assert.IsTrue(consumedFound, "Expected MoveRight to be logged as consumed by KinematicMovementController.");

            UnityEngine.Object.Destroy(playerGo);
            UnityEngine.Object.Destroy(attack);
            UnityEngine.Object.Destroy(corgiData);
            PlaytestMetrics.IsRecording = false;
            PlaytestMetrics.Reset();
            yield return null;
        }

        private static void SetPrivateField<T>(object instance, string fieldName, T value)
        {
            var field = instance.GetType().GetField(fieldName, InstancePrivate);
            Assert.NotNull(field, $"Expected field '{fieldName}' to exist.");
            field.SetValue(instance, value);
        }
    }
}
