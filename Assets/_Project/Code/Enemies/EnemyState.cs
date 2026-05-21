namespace CorgiCommando.Enemies
{
    /// <summary>
    /// Enemy FSM states. Lightweight: Idle → Chase → Attack → Stunned → Recover.
    /// </summary>
    public enum EnemyState
    {
        Idle,
        Chase,
        Attack,
        Stunned,
        Recover,
        Dead,
        /// <summary>Raccoon-specific: fleeing with stolen Treats.</summary>
        Fleeing
    }
}
