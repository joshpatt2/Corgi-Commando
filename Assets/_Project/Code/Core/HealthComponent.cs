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
            Owner = owner;
        }

        public void OnDetach()
        {
            Owner = null;
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || IsDead)
            {
                return;
            }

            var previousHP = CurrentHP;
            CurrentHP = Math.Max(0, CurrentHP - amount);
            var appliedDamage = previousHP - CurrentHP;
            OnDamaged?.Invoke(appliedDamage);

            if (CurrentHP == 0 && !IsDead)
            {
                IsDead = true;
                OnDied?.Invoke();
            }
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || IsDead)
            {
                return;
            }

            CurrentHP = Math.Min(MaxHP, CurrentHP + amount);
        }

        public void ResetToMax()
        {
            CurrentHP = MaxHP;
            IsDead = false;
        }
    }
}
