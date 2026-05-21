using UnityEngine;

namespace CorgiCommando.Data
{
    /// <summary>
    /// ScriptableObject defining a corgi character's stats and references.
    /// One asset per corgi (Sarge, Biscuit, Pixel, Duchess).
    /// Only Sarge is used for the prototype vertical slice.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCorgiData", menuName = "CorgiCommando/CorgiData")]
    public class CorgiData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Display name for UI")]
        public string corgiName;

        [Tooltip("Placeholder color for the solid-color rectangle")]
        public Color placeholderColor = new Color(1f, 0.5f, 0f); // orange

        [Header("Stats")]
        [Tooltip("Maximum hit points")]
        public int maxHP = 100;

        [Tooltip("Walk speed in units/sec on X axis")]
        public float walkSpeed = 5f;

        [Tooltip("Walk speed in units/sec on Z axis (depth)")]
        public float depthSpeed = 3f;

        [Tooltip("Jump force (initial Y velocity)")]
        public float jumpForce = 10f;

        [Header("Combat")]
        [Tooltip("Ordered list of attacks in the combo chain (Punch, Punch, Kick)")]
        public AttackData[] comboChain;

        [Tooltip("Special move attack data (Bark Shockwave for Sarge)")]
        public AttackData specialAttack;

        [Tooltip("Maximum special meter value")]
        public float maxSpecialMeter = 100f;

        // TODO: Should specials cost meter, or be on a cooldown timer? (open design question)
        [Tooltip("Special meter cost to use signature move")]
        public float specialCost = 100f;

        [Tooltip("Special meter decay rate per second when not attacking")]
        public float specialDecayRate = 5f;

        [Tooltip("Special meter gained per hit landed")]
        public float specialGainPerHit = 10f;
    }
}
