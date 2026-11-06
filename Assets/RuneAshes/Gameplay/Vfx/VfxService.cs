// ─────────────────────────────────────────────────────────────────────────────
// 【职责】游戏内 VFX 的稳定入口：代码只认 VfxId，不认具体 prefab 路径。
// 世界空间的一次性特效走池；弹丸/环绕球这类挂接特效走直接实例化，
// 避免池对象被父对象 Destroy 后污染池。
// ─────────────────────────────────────────────────────────────────────────────
using System.Collections.Generic;
using UnityEngine;

namespace RuneAshes.Gameplay
{
    public static class VfxService
    {
        /// <summary>Profiler 对照/过载保护用；关闭后不再生成新 VFX。</summary>
        public static bool Enabled = true;

        /// <summary>同屏活跃的池化 VFX 上限；超过后丢弃低优先级新请求。</summary>
        public static int MaxActive = 48;

        private static VfxCatalog _catalog;
        private static Transform _root;
        private static VfxPoolRuntime _runtime;
        private static bool _loaded;
        private static int _frame = -1;
        private static int _spawnedThisFrame;
        private const int MaxSpawnsPerFrame = 16;

        private static readonly Dictionary<GameObject, Stack<VfxPooledInstance>> Pools =
            new Dictionary<GameObject, Stack<VfxPooledInstance>>();

        public static void Play(VfxId id, Vector3 position)
        {
            Play(id, position, Quaternion.identity, null, 0f);
        }

        public static void Play(VfxId id, Vector3 position, Quaternion rotation)
        {
            Play(id, position, rotation, null, 0f);
        }

        public static void Play(VfxId id, Vector3 position, Quaternion rotation, Transform parent)
        {
            Play(id, position, rotation, parent, 0f);
        }

        public static void Play(VfxId id, Vector3 position, Quaternion rotation, float maxLifetime)
        {
            Play(id, position, rotation, null, maxLifetime);
        }

        public static void Play(VfxId id, Vector3 position, Quaternion rotation, Transform parent,
            float maxLifetime)
        {
            if (!Enabled || !ConsumeBudget())
            {
                return;
            }

            var prefab = GetPrefab(id);
            if (prefab == null)
            {
                return;
            }

            EnsureRoot();

            // 有父对象时不要池化：父对象可能被 Destroy，池会拿到失效实例。
            if (parent != null)
            {
                SpawnDirect(prefab, parent, position, rotation, maxLifetime, false);
                return;
            }

            if (_runtime.ActiveCount >= Mathf.Max(1, MaxActive))
            {
                return;
            }

            var instance = GetPooled(prefab);
            if (instance == null)
            {
                return;
            }

            instance.transform.SetParent(_root, false);
            instance.transform.position = position;
            instance.transform.rotation = rotation;
            _runtime.Register(instance);
            instance.Begin(maxLifetime);
        }

        /// <summary>
        /// 给弹丸/持续物体使用的挂接版本：作为 parent 的子对象，随父物体移动。
        /// 直接实例化并返回；调用方在命中时 Destroy。
        /// </summary>
        public static GameObject PlayAttached(VfxId id, Transform parent, Vector3 localPosition,
            Quaternion localRotation, float maxLifetime)
        {
            if (!Enabled)
            {
                return null;
            }

            var prefab = GetPrefab(id);
            return prefab == null
                ? null
                : SpawnDirect(prefab, parent, localPosition, localRotation, maxLifetime, true);
        }

        /// <summary>
        /// 将 VFX 根对象挂到场景中，避免被 Destroy 污染池。
        /// </summary>
        private static void EnsureRoot()
        {
            if (_root != null)
            {
                return;
            }

            var root = new GameObject("[VFX]");
            _root = root.transform;
            _runtime = root.AddComponent<VfxPoolRuntime>();
        }

        /// <summary>
        /// 从池中获取一个实例，若没有可用实例则创建一个新实例。
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns></returns>
        private static VfxPooledInstance GetPooled(GameObject prefab)
        {
            Stack<VfxPooledInstance> pool;
            if (!Pools.TryGetValue(prefab, out pool))
            {
                pool = new Stack<VfxPooledInstance>();
                Pools.Add(prefab, pool);
            }

            while (pool.Count > 0)
            {
                var pooled = pool.Pop();
                if (pooled != null)
                {
                    return pooled;
                }
            }

            var go = Object.Instantiate(prefab, _root);
            var instance = go.GetComponent<VfxPooledInstance>();
            if (instance == null)
            {
                instance = go.AddComponent<VfxPooledInstance>();
            }

            instance.Prefab = prefab;
            return instance;
        }

        internal static void Return(VfxPooledInstance instance)
        {
            if (instance == null || instance.Prefab == null)
            {
                return;
            }

            instance.StopAndHide();
            instance.transform.SetParent(_root, false);
            instance.transform.localPosition = Vector3.zero;
            instance.gameObject.SetActive(false);

            Stack<VfxPooledInstance> pool;
            if (!Pools.TryGetValue(instance.Prefab, out pool))
            {
                pool = new Stack<VfxPooledInstance>();
                Pools.Add(instance.Prefab, pool);
            }

            pool.Push(instance);//压栈，下次取出时是最近使用的实例，避免频繁创建销毁
        }

        private static GameObject SpawnDirect(GameObject prefab, Transform parent, Vector3 position,
            Quaternion rotation, float maxLifetime, bool local)
        {
            var go = Object.Instantiate(prefab, parent != null ? parent : _root);
            go.name = prefab.name + " (VFX)";
            if (local)
            {
                go.transform.localPosition = position;
                go.transform.localRotation = rotation;
            }
            else
            {
                go.transform.position = position;
                go.transform.rotation = rotation;
            }

            go.SetActive(true);
            var duration = ConfigureParticles(go);
            Object.Destroy(go, maxLifetime > 0f ? maxLifetime : Mathf.Min(duration, 3f));
            return go;
        }

        /// <summary>
        /// 确保所有粒子系统都不循环播放，计算整个特效的最大持续时间。
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        internal static float ConfigureParticles(GameObject go)
        {
            var systems = go.GetComponentsInChildren<ParticleSystem>(true);
            var duration = 0.2f;
            for (var i = 0; i < systems.Length; i++)
            {
                var system = systems[i];
                if (system == null)
                {
                    continue;
                }

                var main = system.main;
                main.loop = false;
                main.stopAction = ParticleSystemStopAction.None;
                system.Clear(true);
                system.Play(true);

                var lifetime = main.duration + main.startLifetime.constantMax;
                if (lifetime > duration)
                {
                    duration = lifetime;
                }
            }

            return duration;
        }

        private static GameObject GetPrefab(VfxId id)
        {
            var catalog = GetCatalog();
            return catalog != null ? catalog.Get(id) : null;
        }

        /// <summary>
        /// 为了避免在一帧内生成过多特效，限制每帧最多生成 MaxSpawnsPerFrame 个特效。
        /// </summary>
        /// <returns></returns>
        private static bool ConsumeBudget()
        {
            if (Time.frameCount != _frame)
            {
                _frame = Time.frameCount;
                _spawnedThisFrame = 0;
            }

            if (_spawnedThisFrame >= MaxSpawnsPerFrame)
            {
                return false;
            }

            _spawnedThisFrame++;
            return true;
        }

        private static VfxCatalog GetCatalog()
        {
            if (!_loaded)
            {
                _loaded = true;
                _catalog = Resources.Load<VfxCatalog>("VfxCatalog");
            }

            return _catalog;
        }
    }

    /// <summary>池对象：只保存自身状态，由 VfxPoolRuntime 统一 Tick。</summary>
    public sealed class VfxPooledInstance : MonoBehaviour
    {
        internal GameObject Prefab;

        private ParticleSystem[] _systems;
        private float _age;
        private float _duration;
        private bool _active;

        internal void Begin(float maxLifetime)
        {
            if (_systems == null)
            {
                _systems = GetComponentsInChildren<ParticleSystem>(true);
            }

            //根据粒子系统的持续时间计算整个特效的最大持续时间，避免特效被提前回收
            gameObject.SetActive(true);
            _duration = VfxService.ConfigureParticles(gameObject);
            _duration = maxLifetime > 0f ? maxLifetime : Mathf.Min(_duration, 3f);
            _age = 0f;
            _active = true;
        }

        internal bool Tick(float dt)
        {
            if (!_active)
            {
                return false;
            }

            _age += dt;
            if (_age < _duration)
            {
                return false;
            }

            _active = false;
            return true;
        }

        /// <summary>停止所有粒子系统并隐藏对象，准备回收。</summary>
        internal void StopAndHide()
        {
            _active = false;
            if (_systems == null)
            {
                return;
            }

            for (var i = 0; i < _systems.Length; i++)
            {
                if (_systems[i] != null)
                {
                    _systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }
    }

    /// <summary>单一管理器负责所有池实例的倒计时，避免每个特效各自 Update。</summary>
    public sealed class VfxPoolRuntime : MonoBehaviour
    {
        private readonly List<VfxPooledInstance> _active = new List<VfxPooledInstance>();

        internal int ActiveCount
        {
            get { return _active.Count; }
        }

        internal void Register(VfxPooledInstance instance)
        {
            if (!_active.Contains(instance))
            {
                _active.Add(instance);
            }
        }

        private void Update()
        {
            for (var i = _active.Count - 1; i >= 0; i--)
            {
                var instance = _active[i];
                if (instance == null)
                {
                    _active.RemoveAt(i);
                    continue;
                }

                if (instance.Tick(Time.deltaTime))
                {
                    _active.RemoveAt(i);
                    VfxService.Return(instance);
                }
            }
        }
    }
}
