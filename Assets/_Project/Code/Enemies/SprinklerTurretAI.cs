namespace CorgiCommando.Enemies
{
    /// <summary>
    /// Sprinkler Turret AI. Fixed position, fires telegraphed water bursts on interval.
    /// Teaches player timing and dodging environmental hazards.
    /// Does not use Chase state — stays in Idle/Attack cycle.
    /// </summary>
    public class SprinklerTurretAI : EnemyAI
    {
        private float _cooldownTimer;
        private float _telegraphTimer;

        /// <summary>Time in seconds between attacks.</summary>
        public float FireInterval { get; set; } = 2.0f;

        /// <summary>Time in seconds the telegraph shows before firing.</summary>
        public float TelegraphDuration { get; set; } = 0.5f;

        /// <summary>Whether the turret is currently in its telegraph phase.</summary>
        public bool IsTelegraphing { get; private set; }

        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);

            if (CurrentState == EnemyState.Dead)
            {
                return;
            }

            if (IsTelegraphing)
            {
                _telegraphTimer += deltaTime;
                if (_telegraphTimer >= TelegraphDuration)
                {
                    IsTelegraphing = false;
                    _telegraphTimer = 0f;
                    TransitionTo(EnemyState.Attack);
                }

                return;
            }

            if (CurrentState == EnemyState.Attack)
            {
                TransitionTo(EnemyState.Idle);
                _cooldownTimer = 0f;
                return;
            }

            _cooldownTimer += deltaTime;
            if (_cooldownTimer >= FireInterval)
            {
                IsTelegraphing = true;
                _telegraphTimer = 0f;
            }
        }
    }
}
