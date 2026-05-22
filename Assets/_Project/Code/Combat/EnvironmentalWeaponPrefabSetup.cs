using CorgiCommando.Data;
using UnityEngine;

namespace CorgiCommando.Combat
{
    /// <summary>
    /// Authoring helper for environmental weapon prefabs.
    /// </summary>
    public class EnvironmentalWeaponPrefabSetup : MonoBehaviour
    {
        [SerializeField] private AttackData _swingAttackData;
        [SerializeField] private AttackData _throwAttackData;
        [SerializeField] private int _maxUses = 3;
        [SerializeField] private Color _placeholderColor = Color.yellow;

        private void Awake()
        {
            var weapon = GetComponent<EnvironmentalWeaponEntity>();
            if (weapon != null)
            {
                weapon.Initialize(_swingAttackData, _throwAttackData, Mathf.Max(1, _maxUses));
            }

            var sprite = GetComponent<SpriteRenderer>();
            if (sprite != null)
            {
                sprite.color = _placeholderColor;
            }
        }
    }
}
