using System;
using System.Collections;
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
    public class PlaytestBotTests
    {
        private static readonly BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string BackyardWalkToArenaPath = "Assets/_Project/Data/PlaytestScripts/BackyardWalkToArena.asset";
        private const float MinimumExpectedDistance = 4f;

        [UnityTest]
        public IEnumerator PlaytestBot_DrivesMoveRight_PlayerTranslatesAtLeast4Units()
        {
            var root = new GameObject("SceneBootstrap");
            var bootstrap = root.AddComponent<SceneBootstrap>();
            root.AddComponent<SpawnManager>();
            root.AddComponent<GroupTargetCamera>();
            root.AddComponent<ArenaCameraLock>();
            var playtestBot = root.AddComponent<PlaytestBot>();

            var waveData = ScriptableObject.CreateInstance<WaveData>();
            waveData.waves = Array.Empty<WaveEntry>();
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

            float startX = player.transform.position.x;
            PlaytestScript script = LoadAssetAtPath<PlaytestScript>(BackyardWalkToArenaPath);
            Assert.NotNull(script, $"Expected playtest script asset at path: {BackyardWalkToArenaPath}");

            yield return playtestBot.Play(inputBuffer, script);
            yield return null;

            float movedDistance = player.transform.position.x - startX;
            Assert.GreaterOrEqual(movedDistance, MinimumExpectedDistance);

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

        private static T LoadAssetAtPath<T>(string path) where T : UnityEngine.Object
        {
            Type assetDatabaseType = Type.GetType("UnityEditor.AssetDatabase, UnityEditor");
            Assert.NotNull(assetDatabaseType, "UnityEditor.AssetDatabase type should be available in PlayMode tests.");

            var loadMethod = assetDatabaseType.GetMethod(
                "LoadAssetAtPath",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(string), typeof(Type) },
                modifiers: null);

            Assert.NotNull(loadMethod, "Expected UnityEditor.AssetDatabase.LoadAssetAtPath(string, Type) to exist.");
            return loadMethod.Invoke(null, new object[] { path, typeof(T) }) as T;
        }
    }
}
