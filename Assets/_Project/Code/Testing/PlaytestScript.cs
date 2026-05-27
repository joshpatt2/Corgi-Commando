using System;
using System.Collections.Generic;
using CorgiCommando.Core;
using UnityEngine;

namespace CorgiCommando.Testing
{
    [CreateAssetMenu(fileName = "NewPlaytestScript", menuName = "CorgiCommando/Testing/PlaytestScript")]
    public class PlaytestScript : ScriptableObject
    {
        public List<PlaytestEntry> entries = new List<PlaytestEntry>();
    }

    [Serializable]
    public struct PlaytestEntry
    {
        public InputAction action;
        public float timestamp;
        public Vector2 axisValue;
    }
}
