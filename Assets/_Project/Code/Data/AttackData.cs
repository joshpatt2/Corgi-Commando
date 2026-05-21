using UnityEngine;

namespace CorgiCommando.Data
{
    /// <summary>
    /// ScriptableObject defining per-attack frame data and hitbox.
    /// Data-driven: balancing is tunable without code changes.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAttackData", menuName = "CorgiCommando/AttackData")]
    public class AttackData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Attack name for debugging and UI")]
        public string attackName;

        [Header("Frame Data")]
        [Tooltip("Frames before the hitbox becomes active")]
        public int startupFrames = 3;

        [Tooltip("Frames the hitbox is active")]
        public int activeFrames = 4;

        [Tooltip("Frames after active before the character can act again")]
        public int recoveryFrames = 6;

        [Tooltip("Frames within which the next combo input is accepted (0 = no chaining)")]
        public int comboWindowFrames = 10;

        [Header("Damage & Knockback")]
        [Tooltip("Base damage dealt on hit")]
        public int damage = 10;

        [Tooltip("Knockback impulse applied to target on hit")]
        public Vector3 knockbackForce = new Vector3(3f, 1f, 0f);

        [Tooltip("Hitstun frames applied to the target")]
        public int hitstunFrames = 8;

        [Tooltip("Whether this attack causes knockdown")]
        public bool causesKnockdown = false;

        [Header("Hitbox")]
        [Tooltip("Hitbox rect in local space relative to the attacker")]
        public Rect hitboxRect = new Rect(0.5f, -0.25f, 1f, 0.5f);

        [Header("Feel")]
        [Tooltip("Hitstop duration in frames on contact (3-6 typical)")]
        public int hitstopFrames = 4;

        [Tooltip("Screen shake intensity on hit (0 = none)")]
        public float screenShakeIntensity = 0.1f;

        [Header("VFX")]
        [Tooltip("Hit type for VFX color coding (Light, Heavy, Special)")]
        public HitType hitType = HitType.Light;
    }

    /// <summary>
    /// Hit type classification for VFX color coding.
    /// White = light, Yellow = heavy, Magenta = special.
    /// </summary>
    public enum HitType
    {
        Light,
        Heavy,
        Special
    }
}
