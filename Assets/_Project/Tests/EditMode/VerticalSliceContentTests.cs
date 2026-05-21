using NUnit.Framework;
using UnityEditor;
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
            Assert.That(sarge.maxHP, Is.Not.EqualTo(100));
            Assert.That(sarge.walkSpeed, Is.Not.EqualTo(5f));
            Assert.That(sarge.depthSpeed, Is.Not.EqualTo(3f));
            Assert.That(sarge.jumpForce, Is.Not.EqualTo(10f));
            Assert.That(sarge.maxSpecialMeter, Is.Not.EqualTo(100f));
            Assert.That(sarge.specialGainPerHit, Is.Not.EqualTo(10f));
            Assert.That(sarge.specialDecayRate, Is.Not.EqualTo(5f));
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
        public void VerticalSliceContent_AllAttackDataAssets_HaveNonZeroDamage()
        {
            foreach (var attackPath in AttackPaths)
            {
                var attack = AssetDatabase.LoadAssetAtPath<AttackData>(attackPath);
                Assert.That(attack, Is.Not.Null, $"Expected attack asset at path: {attackPath}");
                Assert.That(attack.damage, Is.GreaterThan(0), $"Expected non-zero damage for {attackPath}");
            }
        }
    }
}
