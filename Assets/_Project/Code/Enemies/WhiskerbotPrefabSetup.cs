using CorgiCommando.Data;
using UnityEngine;

namespace CorgiCommando.Enemies
{
    public class WhiskerbotPrefabSetup : MonoBehaviour
    {
        [SerializeField] private EnemyData _enemyData;

        public EnemyData EnemyData => _enemyData;

        private void Awake()
        {
            var boss = GetComponent<WhiskerbotController>();
            if (boss != null && _enemyData != null)
            {
                boss.Initialize(_enemyData);
            }

            var sprite = GetComponent<SpriteRenderer>();
            if (sprite != null && _enemyData != null)
            {
                sprite.color = _enemyData.placeholderColor;
            }
        }
    }
}
