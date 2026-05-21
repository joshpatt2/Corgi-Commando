using System;

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
            throw new NotImplementedException();
        }

        public override void Tick(float deltaTime)
        {
            throw new NotImplementedException();
        }
    }
}
