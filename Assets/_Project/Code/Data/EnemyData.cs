using UnityEngine;

namespace CorgiCommando.Data
{
    /// <summary>
    /// ScriptableObject defining an enemy type's stats, behavior preset, and drop table.
    /// One asset per enemy type (FeralCat, RaccoonBandit, SprinklerTurret, Roomba, Whiskerbot).
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "CorgiCommando/EnemyData")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Enemy type name for debugging")]
        public string enemyName;

        [Tooltip("Placeholder color for the solid-color primitive")]
        public Color placeholderColor = Color.grey;

        [Header("Stats")]
        [Tooltip("Maximum hit points")]
        public int maxHP = 30;

        [Tooltip("Movement speed in units/sec")]
        public float moveSpeed = 3f;

        [Tooltip("Z-depth movement speed")]
        public float depthSpeed = 2f;

        [Header("Combat")]
        [Tooltip("Attack data for this enemy's primary attack")]
        public AttackData primaryAttack;

        [Tooltip("Aggro range — distance at which enemy transitions from Idle to Chase")]
        public float aggroRange = 8f;

        [Tooltip("Attack range — distance at which enemy can start attacking")]
        public float attackRange = 1.5f;

        [Header("Behavior")]
        [Tooltip("Behavior preset — maps to FSM type selection")]
        public EnemyBehaviorPreset behaviorPreset;

        [Header("Drops")]
        [Tooltip("Base Treats dropped on death")]
        public int baseTreatsDrop = 5;

        [Tooltip("Bonus Treats if killed by combo finisher")]
        public int comboFinisherBonus = 3;

        [Header("Boss")]
        [Tooltip("HP for the pilot entity ejected at Phase 3 (boss enemies only)")]
        public int pilotMaxHP = 100;
    }

    /// <summary>
    /// Behavior preset enum for selecting the correct FSM.
    /// </summary>
    public enum EnemyBehaviorPreset
    {
        FeralCat,
        RaccoonBandit,
        SprinklerTurret,
        Roomba,
        Boss
    }
}
