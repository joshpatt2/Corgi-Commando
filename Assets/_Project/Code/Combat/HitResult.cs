using UnityEngine;
using CorgiCommando.Core;
using CorgiCommando.Data;

namespace CorgiCommando.Combat
{
    /// <summary>
    /// Result of a hit resolution by CombatSystem.
    /// Contains all data needed for feedback (VFX, SFX, hitstop).
    /// </summary>
    public struct HitResult
    {
        /// <summary>Whether the hit connected (target in Z-band, hurtbox enabled).</summary>
        public bool DidHit;

        /// <summary>The entity that performed the attack.</summary>
        public Entity Attacker;

        /// <summary>The entity that was hit (null if whiff).</summary>
        public Entity Target;

        /// <summary>Damage dealt after any modifiers.</summary>
        public int DamageDealt;

        /// <summary>Knockback impulse applied to target.</summary>
        public Vector3 KnockbackApplied;

        /// <summary>Hitstop duration in frames.</summary>
        public int HitstopFrames;

        /// <summary>Hit type for VFX color coding.</summary>
        public HitType HitType;

        /// <summary>Screen shake intensity.</summary>
        public float ScreenShakeIntensity;

        /// <summary>World position where the hit occurred (for VFX spawn).</summary>
        public Vector3 HitPosition;
    }
}
