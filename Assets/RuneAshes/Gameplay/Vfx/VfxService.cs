// ─────────────────────────────────────────────────────────────────────────────
// 【职责】游戏内 VFX 的稳定入口：代码只认 VfxId，不认具体 prefab 路径。
// 当前先保证正确性：每次 Play 直接实例化，按生命周期自动销毁。
// W5 性能阶段再接对象池；不要为了省一次 Instantiate 把反馈弄丢。
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;

namespace RuneAshes.Gameplay
{
    public static class VfxService
    {
        private static VfxCatalog _catalog;
        private static Transform _root;
        private static bool _loaded;

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
            var prefab = GetPrefab(id);
            if (prefab != null)
            {
                Spawn(prefab, parent, position, rotation, maxLifetime, false);
            }
        }

        /// <summary>
        /// 给弹丸/持续物体使用的挂接版本：作为 parent 的子对象，随父物体移动。
        /// 返回实例，调用方可在命中时提前 Destroy。
        /// </summary>
        public static GameObject PlayAttached(VfxId id, Transform parent, Vector3 localPosition,
            Quaternion localRotation, float maxLifetime)
        {
            var prefab = GetPrefab(id);
            return prefab == null
                ? null
                : Spawn(prefab, parent, localPosition, localRotation, maxLifetime, true);
        }

        private static GameObject Spawn(GameObject prefab, Transform parent, Vector3 position,
            Quaternion rotation, float maxLifetime, bool local)
        {
            if (_root == null)
            {
                _root = new GameObject("[VFX]").transform;
            }

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
                // One-shot 反馈统一关闭 loop；真实循环特效应走独立 API。
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

            Object.Destroy(go, maxLifetime > 0f ? maxLifetime : Mathf.Min(duration, 3f));
            return go;
        }

        private static GameObject GetPrefab(VfxId id)
        {
            var catalog = GetCatalog();
            return catalog != null ? catalog.Get(id) : null;
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
}
