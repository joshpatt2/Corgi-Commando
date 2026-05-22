using System.Collections;
using System.Linq;
using CorgiCommando.Core;
using CorgiCommando.Data;
using CorgiCommando.Enemies;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CorgiCommando.Tests.PlayMode
{
    [TestFixture]
    public class SpawnManagerPlayModeTests
    {
        [UnityTest]
        public IEnumerator SpawnManager_SpawnCurrentWave_InstantiatesEnemiesAtPositions()
        {
            var managerGo = new GameObject("SpawnManager");
            var manager = managerGo.AddComponent<SpawnManager>();

            var enemyData = ScriptableObject.CreateInstance<EnemyData>();
            enemyData.enemyName = "FeralCat";
            enemyData.behaviorPreset = EnemyBehaviorPreset.FeralCat;
            enemyData.placeholderColor = Color.magenta;

            var waveData = ScriptableObject.CreateInstance<WaveData>();
            waveData.waves = new[]
            {
                new WaveEntry
                {
                    spawnGroups = new[]
                    {
                        new SpawnGroup
                        {
                            enemyData = enemyData,
                            count = 3,
                            spawnPosition = new Vector3(2f, 0f, 4f)
                        }
                    }
                }
            };

            manager.StartEncounter(waveData);
            manager.SpawnCurrentWave();

            yield return null;

            var enemies = Object.FindObjectsOfType<EnemyAI>();
            Assert.AreEqual(3, enemies.Length);

            float[] xPositions = enemies.Select(enemy => enemy.transform.position.x).OrderBy(x => x).ToArray();
            Assert.That(xPositions[0], Is.EqualTo(2f).Within(0.001f));
            Assert.That(xPositions[1], Is.EqualTo(3.5f).Within(0.001f));
            Assert.That(xPositions[2], Is.EqualTo(5f).Within(0.001f));
            Assert.AreEqual(3, manager.AliveEnemyCount);

            foreach (var enemy in enemies)
            {
                Assert.That(enemy.transform.position.z, Is.EqualTo(4f).Within(0.001f));
            }

            foreach (var enemy in enemies)
            {
                Object.Destroy(enemy.gameObject);
            }

            Object.Destroy(managerGo);
            Object.Destroy(waveData);
            Object.Destroy(enemyData);
        }
    }
}
