using System;
using UnityEngine;
using CorgiCommando.Core;
using CorgiCommando.Data;
using CorgiCommando.Testing;

namespace CorgiCommando.Enemies
{
    /// <summary>
    /// WHISKERBOT-9000 boss controller. Three-phase mech fight.
    /// Phase 1 (100-75% HP): Stomp + claw swipes. Hit legs to stagger, then wail on cockpit.
    /// Phase 2 (75-35% HP): Fence-debris throws, homing yarn-ball missiles, sweeping laser pointer.
    /// Phase 3 (35-0% HP / pilot): Mech wrecked, Maine Coon pilot fight — separate entity.
    /// </summary>
    public class WhiskerbotController : BossEntity
    {
        /// <summary>HP percentage threshold for Phase 1 → Phase 2 transition.</summary>
        public const float Phase2Threshold = 0.75f;

        /// <summary>HP percentage threshold for Phase 2 → Phase 3 transition.</summary>
        public const float Phase3Threshold = 0.35f;

        /// <summary>Whether the laser pointer is currently active (Phase 2).</summary>
        public bool IsLaserActive { get; private set; }

        /// <summary>Whether the mech is destroyed and the pilot has ejected.</summary>
        public bool IsPilotEjected { get; private set; }

        /// <summary>Reference to the pilot entity (spawned at Phase 3).</summary>
        public Entity PilotEntity { get; private set; }

        /// <summary>Fired when the pilot ejects from the mech.</summary>
        public event Action OnPilotEjected;

        /// <summary>Fired when the laser pointer activates.</summary>
        public event Action OnLaserActivated;

        /// <summary>Fired when the laser pointer deactivates.</summary>
        public event Action OnLaserDeactivated;

        private EnemyData _data;

        /// <summary>
        /// Initializes the boss with its data.
        /// </summary>
        public void Initialize(EnemyData data)
        {
            _data = data;
            TotalPhases = 3;
            CurrentPhase = 1;
            AddEntityComponent(new HealthComponent(data.maxHP));
        }

        /// <summary>
        /// Checks HP percentage against phase thresholds.
        /// Transitions at 75% and 35%.
        /// </summary>
        public override void CheckPhaseTransition(int currentHP, int maxHP)
        {
            if (maxHP <= 0)
            {
                return;
            }

            float ratio = (float)currentHP / maxHP;

            if (CurrentPhase == 1 && ratio <= Phase2Threshold)
            {
                int oldPhase = CurrentPhase;
                TransitionToPhase(2);
                if (PlaytestMetrics.IsRecording)
                {
                    PlaytestMetrics.LogStateTransition($"{GetType().Name}:{GetInstanceID()}", oldPhase.ToString(), CurrentPhase.ToString());
                }
            }
            else if (CurrentPhase == 2 && ratio <= Phase3Threshold)
            {
                int oldPhase = CurrentPhase;
                TransitionToPhase(3);
                if (PlaytestMetrics.IsRecording)
                {
                    PlaytestMetrics.LogStateTransition($"{GetType().Name}:{GetInstanceID()}", oldPhase.ToString(), CurrentPhase.ToString());
                }
            }
        }

        /// <summary>
        /// Activates the laser pointer attack (Phase 2).
        /// The laser chases corgis — mechanically real risk (fight the urge).
        /// </summary>
        public void ActivateLaser()
        {
            IsLaserActive = true;
            OnLaserActivated?.Invoke();
        }

        /// <summary>
        /// Deactivates the laser pointer.
        /// </summary>
        public void DeactivateLaser()
        {
            IsLaserActive = false;
            OnLaserDeactivated?.Invoke();
        }

        /// <summary>
        /// Ejects the pilot. Creates a separate Entity for the 1v1 finish.
        /// Called at 0% mech HP.
        /// </summary>
        public void EjectPilot()
        {
            var pilotGo = new GameObject("Maine Coon_Pilot");
            pilotGo.transform.position = transform.position;
            var pilot = pilotGo.AddComponent<Entity>();
            int pilotHP = _data.pilotMaxHP;
            pilot.AddEntityComponent(new HealthComponent(pilotHP));
            PilotEntity = pilot;
            IsPilotEjected = true;
            OnPilotEjected?.Invoke();
        }

        /// <summary>
        /// Boss tick — runs phase-specific AI logic.
        /// </summary>
        public void Tick(float deltaTime)
        {
            // Intentionally empty: WHISKERBOT's behavior is driven by phase events and external UnityEvent scripts.
        }

        /// <summary>
        /// Resets boss state to a fresh phase-1 intro state for retry.
        /// </summary>
        public void ResetToPhase1()
        {
            CurrentPhase = 1;
            IsLaserActive = false;
            IsPilotEjected = false;

            if (PilotEntity != null)
            {
                Destroy(PilotEntity.gameObject);
                PilotEntity = null;
            }

            Revive();
        }

        public string GetBossName()
        {
            return _data != null ? _data.enemyName : gameObject.name;
        }
    }
}
