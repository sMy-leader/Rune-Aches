// ─────────────────────────────────────────────────────────────────────────────
// 【链路】WaveSpawner（本文件）──> Instantiate(TrollTemplate)
//        ──> EnemyActor / EnemyBrain ──> UnityRuneWorld.Kill ──> ExperienceSystem
// 【职责】按固定间隔在玩家周围生成敌人，限制同屏数量。
// 【W3b 简化】不做波次表/精英词缀；先保证"杀怪→升级→继续杀"的循环能跑。
// 【W5】换成数据驱动波次 + Flow Field；本类可保留为生成器实现。
// ─────────────────────────────────────────────────────────────────────────────
using System.Collections.Generic;
using UnityEngine;

namespace RuneAshes.Gameplay
{
    public sealed class WaveSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject _enemyTemplate;
        [SerializeField] private float _spawnInterval = 10f;
        [SerializeField] private int _maxAlive = 3;
        [SerializeField] private float _spawnRadius = 12f;

        private readonly List<EnemyActor> _spawned = new List<EnemyActor>();
        private PlayerHealth _player;
        private float _timer;

        public int WaveIndex { get; private set; }

        public int AliveCount
        {
            get
            {
                Prune();
                return _spawned.Count;
            }
        }

        public GameObject EnemyTemplate
        {
            set { _enemyTemplate = value; }
        }

        private void Awake()
        {
            _player = FindFirstObjectByType<PlayerHealth>();
        }

        private void Start()
        {
            // 场景里已有的敌人也计入上限，避免开局就超过 maxAlive。
            var existing = FindObjectsByType<EnemyActor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (var i = 0; i < existing.Length; i++)
            {
                if (existing[i] != null && existing[i].IsAlive)
                {
                    _spawned.Add(existing[i]);
                }
            }

            _timer = 2f;
        }

        private void Update()
        {
            if (_player == null || !_player.IsAlive)
            {
                return;
            }

            Prune();
            _timer -= Time.deltaTime;

            if (_timer > 0f || _spawned.Count >= _maxAlive)
            {
                return;
            }

            _timer = _spawnInterval;
            Spawn();
        }

        private void Spawn()
        {
            if (_enemyTemplate == null)
            {
                Debug.LogError("[WaveSpawner] EnemyTemplate is not assigned.", this);
                return;
            }

            var angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            var offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _spawnRadius;
            var position = _player.transform.position + offset;
            position.y = 0.235f;

            // 模板是 inactive 的场景副本；实例化后立即激活，Awake/Start 会正常执行。
            var instance = Instantiate(_enemyTemplate, position, Quaternion.identity);
            instance.name = "Troll_Wave_" + (WaveIndex + 1);
            instance.SetActive(true);

            var actor = instance.GetComponent<EnemyActor>();
            if (actor == null)
            {
                Debug.LogError("[WaveSpawner] Template has no EnemyActor.", this);
                Destroy(instance);
                return;
            }

            _spawned.Add(actor);
            WaveIndex++;
        }
        /// <summary>
        /// 如果当前生成的敌人已经死亡->从列表里删除，清空内存
        /// </summary>
        private void Prune()
        {
            for (var i = _spawned.Count - 1; i >= 0; i--)
            {
                if (_spawned[i] == null || !_spawned[i].IsAlive)
                {
                    _spawned.RemoveAt(i);
                }
            }
        }
    }
}
