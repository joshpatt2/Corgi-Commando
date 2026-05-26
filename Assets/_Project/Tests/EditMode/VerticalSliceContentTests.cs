using NUnit.Framework;
using UnityEditor;
using System.Linq;
using CorgiCommando.Data;

namespace CorgiCommando.Tests.EditMode
{
    [TestFixture]
    public class VerticalSliceContentTests
    {
        private const string SargePath = "Assets/_Project/Data/Corgis/Sarge.asset";
        private const string FeralCatPath = "Assets/_Project/Data/Enemies/FeralCat.asset";
        private const string RaccoonBanditPath = "Assets/_Project/Data/Enemies/RaccoonBandit.asset";
        private const string SprinklerTurretPath = "Assets/_Project/Data/Enemies/SprinklerTurret.asset";
        private const string BackyardWavePath = "Assets/_Project/Data/Waves/BackyardWave1.asset";

        private static readonly string[] AttackPaths =
        {
            "Assets/_Project/Data/Attacks/Player/Sarge_Punch1.asset",
            "Assets/_Project/Data/Attacks/Player/Sarge_Punch2.asset",
            "Assets/_Project/Data/Attacks/Player/Sarge_Kick.asset",
            "Assets/_Project/Data/Attacks/Player/Sarge_BarkShockwave.asset",
            "Assets/_Project/Data/Attacks/Enemies/Cat_Swipe.asset",
            "Assets/_Project/Data/Attacks/Enemies/Raccoon_Slash.asset",
            "Assets/_Project/Data/Attacks/Enemies/Sprinkler_WaterShot.asset"
        };

        [Test]
        public void VerticalSliceContent_SargeAsset_LoadsAndHasComboChain()
        {
            var sarge = AssetDatabase.LoadAssetAtPath<CorgiData>(SargePath);

            Assert.That(sarge, Is.Not.Null);
            Assert.That(sarge.maxHP, Is.GreaterThan(0));
            Assert.That(sarge.walkSpeed, Is.GreaterThan(0f));
            Assert.That(sarge.depthSpeed, Is.GreaterThan(0f));
            Assert.That(sarge.jumpForce, Is.GreaterThan(0f));
            Assert.That(sarge.maxSpecialMeter, Is.GreaterThan(0f));
            Assert.That(sarge.specialGainPerHit, Is.GreaterThan(0f));
            Assert.That(sarge.specialDecayRate, Is.GreaterThanOrEqualTo(0f));
            Assert.That(sarge.comboChain, Is.Not.Null);
            Assert.That(sarge.comboChain.Length, Is.EqualTo(3));
            Assert.That(sarge.comboChain[0], Is.Not.Null);
            Assert.That(sarge.comboChain[1], Is.Not.Null);
            Assert.That(sarge.comboChain[2], Is.Not.Null);
        }

        [Test]
        public void VerticalSliceContent_SargeAsset_HasSpecialAttack()
        {
            var sarge = AssetDatabase.LoadAssetAtPath<CorgiData>(SargePath);

            Assert.That(sarge, Is.Not.Null);
            Assert.That(sarge.specialAttack, Is.Not.Null);
        }

        [Test]
        public void VerticalSliceContent_FeralCatAsset_HasPrimaryAttackWired()
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(FeralCatPath);

            Assert.That(enemy, Is.Not.Null);
            Assert.That(enemy.primaryAttack, Is.Not.Null);
        }

        [Test]
        public void VerticalSliceContent_RaccoonBanditAsset_HasPrimaryAttackWired()
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(RaccoonBanditPath);

            Assert.That(enemy, Is.Not.Null);
            Assert.That(enemy.primaryAttack, Is.Not.Null);
        }

        [Test]
        public void VerticalSliceContent_SprinklerTurretAsset_HasPrimaryAttackWired()
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(SprinklerTurretPath);

            Assert.That(enemy, Is.Not.Null);
            Assert.That(enemy.primaryAttack, Is.Not.Null);
        }

        [Test]
        public void VerticalSliceContent_BackyardWave1_HasNonEmptyWaves()
        {
            var waveData = AssetDatabase.LoadAssetAtPath<WaveData>(BackyardWavePath);

            Assert.That(waveData, Is.Not.Null);
            Assert.That(waveData.waves, Is.Not.Null);
            Assert.That(waveData.waves.Length, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void WaveData_BackyardWave1_W1_HasTwoCatsPlusReinforcement()
        {
            var waveData = AssetDatabase.LoadAssetAtPath<WaveData>(BackyardWavePath);
            Assert.That(waveData, Is.Not.Null);
            Assert.That(waveData.waves, Is.Not.Null);
            Assert.That(waveData.waves.Length, Is.GreaterThanOrEqualTo(1));
            var wave1 = waveData.waves[0];

            Assert.That(wave1.spawnGroups.Sum(g => g.count), Is.EqualTo(3));
            Assert.That(CountByPreset(wave1, EnemyBehaviorPreset.FeralCat), Is.EqualTo(3));
            Assert.That(wave1.spawnGroups.Where(g => g.spawnTrigger == SpawnTrigger.OnWaveStart).Sum(g => g.count), Is.EqualTo(2));
            Assert.That(wave1.spawnGroups.Where(g => g.spawnTrigger == SpawnTrigger.OnLowHP).Sum(g => g.count), Is.EqualTo(1));
            Assert.That(wave1.spawnGroups.Count(g => g.spawnTrigger == SpawnTrigger.OnWaveStart && g.enemyData.behaviorPreset == EnemyBehaviorPreset.FeralCat), Is.EqualTo(1));
            Assert.That(wave1.spawnGroups.Count(g => g.spawnTrigger == SpawnTrigger.OnLowHP && g.enemyData.behaviorPreset == EnemyBehaviorPreset.FeralCat), Is.EqualTo(1));
            Assert.That(wave1.spawnGroups.Any(g => g.spawnTrigger == SpawnTrigger.OnLowHP && g.lowHpThresholdNormalized > 0f), Is.True);
        }

        [Test]
        public void WaveData_BackyardWave1_W2_HasSpecCompositions()
        {
            var waveData = AssetDatabase.LoadAssetAtPath<WaveData>(BackyardWavePath);
            Assert.That(waveData, Is.Not.Null);
            Assert.That(waveData.waves, Is.Not.Null);
            Assert.That(waveData.waves.Length, Is.GreaterThanOrEqualTo(2));
            var wave2 = waveData.waves[1];

            Assert.That(CountByPreset(wave2, EnemyBehaviorPreset.FeralCat), Is.EqualTo(2));
            Assert.That(CountByPreset(wave2, EnemyBehaviorPreset.RaccoonBandit), Is.EqualTo(1));
            Assert.That(CountByPreset(wave2, EnemyBehaviorPreset.SprinklerTurret), Is.EqualTo(1));
        }

        [Test]
        public void WaveData_BackyardWave1_W3_HasSpecCompositionsAndEnvWeapons()
        {
            var waveData = AssetDatabase.LoadAssetAtPath<WaveData>(BackyardWavePath);
            Assert.That(waveData, Is.Not.Null);
            Assert.That(waveData.waves, Is.Not.Null);
            Assert.That(waveData.waves.Length, Is.GreaterThanOrEqualTo(3));
            var wave3 = waveData.waves[2];

            Assert.That(CountByPreset(wave3, EnemyBehaviorPreset.FeralCat), Is.EqualTo(3));
            Assert.That(CountByPreset(wave3, EnemyBehaviorPreset.RaccoonBandit), Is.EqualTo(2));
            Assert.That(CountByPreset(wave3, EnemyBehaviorPreset.SprinklerTurret), Is.EqualTo(1));
            Assert.That(wave3.environmentalWeaponsEnabled, Is.True);
        }

        [Test]
        public void VerticalSliceContent_AllAttackDataAssets_HaveNonZeroDamage()
        {
            foreach (var attackPath in AttackPaths)
            {
                var attack = AssetDatabase.LoadAssetAtPath<AttackData>(attackPath);
                Assert.That(attack, Is.Not.Null, $"Expected attack asset at path: {attackPath}");
                Assert.That(attack.damage, Is.GreaterThan(0), $"Expected non-zero damage for {attackPath}");
            }
        }

        private static int CountByPreset(WaveEntry wave, EnemyBehaviorPreset behaviorPreset)
        {
            return wave.spawnGroups
                .Where(g => g.enemyData != null && g.enemyData.behaviorPreset == behaviorPreset)
                .Sum(g => g.count);
        }
    }
}
