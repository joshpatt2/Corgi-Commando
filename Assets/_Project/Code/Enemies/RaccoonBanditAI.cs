namespace CorgiCommando.Enemies
{
    /// <summary>
    /// Raccoon Bandit AI. Grabs Treats (health currency) and runs.
    /// Teaches player movement, prioritization, urgency.
    /// Has unique Fleeing state when carrying stolen Treats.
    /// </summary>
    public class RaccoonBanditAI : EnemyAI
    {
        /// <summary>Whether this raccoon is currently carrying stolen Treats.</summary>
        public bool IsCarryingTreats { get; private set; }

        /// <summary>Amount of Treats stolen.</summary>
        public int StolenTreatsAmount { get; private set; }

        /// <summary>
        /// Steals Treats from a player and transitions to Fleeing state.
        /// </summary>
        public void StealTreats(int amount)
        {
            if (amount <= 0)
            {
                IsCarryingTreats = false;
                StolenTreatsAmount = 0;
                return;
            }

            IsCarryingTreats = true;
            StolenTreatsAmount = amount;
            TransitionTo(EnemyState.Fleeing);
        }

        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);
        }
    }
}
