using CorgiCommando.Core;
using CorgiCommando.Testing;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CorgiCommando.Tests.EditMode
{
    [TestFixture]
    public class PlaytestScriptTests
    {
        private const string BackyardWalkToArenaPath = "Assets/_Project/Data/PlaytestScripts/BackyardWalkToArena.asset";

        [Test]
        public void PlaytestScript_RoundTrips_LoadFromAssetMatches()
        {
            var script = AssetDatabase.LoadAssetAtPath<PlaytestScript>(BackyardWalkToArenaPath);

            Assert.That(script, Is.Not.Null);
            Assert.That(script.entries, Is.Not.Null);
            Assert.That(script.entries.Count, Is.EqualTo(2));
            Assert.That(script.entries[0].action, Is.EqualTo(InputAction.MoveRight));
            Assert.That(script.entries[0].timestamp, Is.EqualTo(0f).Within(0.001f));
            Assert.That(script.entries[0].axisValue, Is.EqualTo(Vector2.right));
            Assert.That(script.entries[1].action, Is.EqualTo(InputAction.MoveRight));
            Assert.That(script.entries[1].timestamp, Is.EqualTo(3f).Within(0.001f));
            Assert.That(script.entries[1].axisValue, Is.EqualTo(Vector2.right));
        }
    }
}
