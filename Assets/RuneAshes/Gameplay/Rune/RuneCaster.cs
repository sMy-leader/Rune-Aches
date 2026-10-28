using System.Collections.Generic;
using RuneAshes.Data;
using UnityEngine;

namespace RuneAshes.Gameplay
{
    /// <summary>
    /// 把链挂到玩家身上并驱动触发器。W2 先实现 T1 定时；
    /// T2/T3/T4/T5/T6 在 W3 接入事件总线后补齐。
    /// </summary>
    public sealed class RuneCaster : MonoBehaviour
    {
        [SerializeField] private string _triggerId = "T1";
        [SerializeField] private string _effectId = "E1";
        [SerializeField] private string[] _modifierIds = new string[0];
        [SerializeField] private uint _runSeed = 20261028u;

        private CompiledChain _chain;
        private IRuneEffect _effect;
        private IRuneWorld _world;
        private PlayerController _player;
        private ModifierSet _modifiers;
        private ProcBudget _procBudget;
        private float _timer;
        private int _castCounter;

        public CompiledChain Chain
        {
            get { return _chain; }
        }

        public void Configure(string triggerId, string effectId, string[] modifierIds)
        {
            _triggerId = triggerId;
            _effectId = effectId;
            _modifierIds = modifierIds ?? new string[0];
            Rebuild();
        }

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            _world = GetComponent<UnityRuneWorld>();
            if (_world == null)
            {
                _world = FindFirstObjectByType<UnityRuneWorld>();
            }

            Rebuild();
        }

        private void Rebuild()
        {
            var provider = new TableProvider();
            provider.LoadAll();
            var compiler = new ChainCompiler(provider.Runes);

            _chain = compiler.Compile(new ChainBlueprint
            {
                TriggerId = _triggerId,
                EffectId = _effectId,
                ModifierIds = new List<string>(_modifierIds ?? new string[0])
            });

            if (!_chain.IsValid)
            {
                Debug.LogError("[RuneCaster] Invalid chain: " + string.Join(" | ", _chain.Errors), this);
                _effect = null;
                return;
            }

            _effect = new RuneEffectFactory().Create(_chain.Effect);
            _modifiers = new RuneModifierFactory().Build(_chain.Modifiers);
            _procBudget = new ProcBudget(RuneRules.ProcBudgetPerTenthSecond, 0.1f);
        }

        private void Update()
        {
            if (_chain == null || _chain.Trigger == null || _effect == null || _world == null)
            {
                return;
            }

            if (_chain.Trigger.Id != "T1")
            {
                return;
            }

            _procBudget.Tick(Time.deltaTime);

            var interval = Mathf.Max(0.05f, _chain.Trigger.GetFloat("interval", 0.8f));
            _timer += Time.deltaTime;
            if (_timer >= interval)
            {
                _timer -= interval;
                Cast();
            }
        }

        public void Cast()
        {
            var direction = _player != null ? _player.AimDirection : transform.forward;
            Cast(direction);
        }

        /// <summary>指定方向施法：供 Play 验证与未来的网络/AI 输入使用。</summary>
        public void Cast(Vector3 direction)
        {
            if (_chain == null || _effect == null || _world == null)
            {
                return;
            }

            _castCounter++;
            if (_procBudget != null && !_procBudget.TryConsume())
            {
                return;
            }

            var meta = new CastMeta
            {
                ChainId = 1,
                CastId = _castCounter,
                Depth = 0,
                Seed = Hash(_runSeed, (uint)_castCounter)
            };

            var context = new RuneEffectContext
            {
                Origin = transform.position + Vector3.up * 0.9f,
                Direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward,
                BaseValue = _chain.Effect.BaseValue,
                ChainDepth = 0,
                Seed = meta.Seed,
                Meta = meta,
                Modifiers = _modifiers,
                World = _world
            };

            _effect.Execute(ref context);
        }

        private static uint Hash(uint seed, uint value)
        {
            var x = seed ^ (value + 0x9E3779B9u + (seed << 6) + (seed >> 2));
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            return x;
        }
    }
}
