using System;

namespace CorgiCommando.Core
{
    /// <summary>
    /// Concrete health component. Manages HP, fires damage/death events.
    /// </summary>
    public class HealthComponent : IHealthComponent
    {
        public Entity Owner { get; private set; }
        public int CurrentHP { get; private set; }
        public int MaxHP { get; private set; }
        public bool IsDead { get; private set; }

        public event Action<int> OnDamaged;
        public event Action OnDied;

        public HealthComponent(int maxHP)
        {
            MaxHP = maxHP;
            CurrentHP = maxHP;
            IsDead = false;
        }

        public void OnAttach(Entity owner)
        {
            throw new NotImplementedException();
        }

        public void OnDetach()
        {
            throw new NotImplementedException();
        }

        public void TakeDamage(int amount)
        {
            throw new NotImplementedException();
        }

        public void Heal(int amount)
        {
            throw new NotImplementedException();
        }

        public void ResetToMax()
        {
            throw new NotImplementedException();
        }
    }
}
