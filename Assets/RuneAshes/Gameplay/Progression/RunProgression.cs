// ─────────────────────────────────────────────────────────────────────────────
// 【链路】ExperienceSystem.LeveledUp ──> UpgradeScreen（UI）
//        ──> RunProgression.GetChoices() / ApplyChoice()
//        ──> RuneCaster.Configure() ──> CompiledChain
// 【职责】维护本局装了什么（1 触发器 + 1 效果 + 最多 2 修饰），
//        生成"装得上"的三选一候选，并把玩家选择写回 RuneCaster。
// 【为什么单独一层】三选一 UI 不应该知道 ChainCompiler 的能耗/槽位规则；
//        UI 只拿 RuneDef，合法性由这里用 ChainCompiler 预检。
// ─────────────────────────────────────────────────────────────────────────────
using System.Collections.Generic;
using RuneAshes.Data;
using RuneAshes.Framework;
using UnityEngine;

namespace RuneAshes.Gameplay
{
    public sealed class RunProgression : MonoBehaviour
    {
        /// <summary>4 槽 = 1 触发 + 1 效果 + 2 修饰。meta 扩槽后改这里。</summary>
        private const int MaxModifiers = 2;

        [SerializeField] private uint _seed = 20261029u;

        private RuneCaster _caster;
        private RuneTable _runes;
        private XorShiftRandom _random;

        private string _triggerId = "T1";
        private string _effectId = "E1";
        private readonly List<string> _modifierIds = new List<string>(MaxModifiers);

        public string TriggerId
        {
            get { return _triggerId; }
        }

        public string EffectId
        {
            get { return _effectId; }
        }

        public IReadOnlyList<string> ModifierIds
        {
            get { return _modifierIds; }
        }

        private void Awake()
        {
            _caster = GetComponent<RuneCaster>();

            var provider = new TableProvider();
            provider.LoadAll();
            _runes = provider.Runes;

            // 固定种子：同样的选择序列 → 同样的三选一，便于复现/测试。
            _random = new XorShiftRandom(_seed);
        }

        private void Start()
        {
            SyncFromCaster();
        }

        /// <summary>从 RuneCaster 当前链同步，场景初始化或外部改链后调用。</summary>
        public void SyncFromCaster()
        {
            if (_caster == null || _caster.Chain == null)
            {
                return;
            }

            var chain = _caster.Chain;
            if (chain.Trigger != null)
            {
                _triggerId = chain.Trigger.Id;
            }

            if (chain.Effect != null)
            {
                _effectId = chain.Effect.Id;
            }

            _modifierIds.Clear();
            for (var i = 0; i < chain.Modifiers.Count; i++)
            {
                _modifierIds.Add(chain.Modifiers[i].Id);
            }
        }

        /// <summary>
        /// 返回最多 count 个"装上后编译合法"的修饰候选。
        /// 槽位满时按替换最后一个修饰来预检；所以候选项永远可落地。
        /// </summary>
        public List<RuneDef> GetChoices(int count)
        {
            var result = new List<RuneDef>();
            if (_runes == null || count <= 0)
            {
                return result;
            }

            var candidates = new List<RuneDef>();
            foreach (var rune in _runes.ByCategory(RuneCategory.Modifier))
            {
                if (rune == null || _modifierIds.Contains(rune.Id))
                {
                    continue;
                }

                if (IsValidWith(rune.Id))
                {
                    candidates.Add(rune);
                }
            }

            // Fisher-Yates：候选顺序随机，但不改变合法性。
            for (var i = candidates.Count - 1; i > 0; i--)
            {
                var j = _random.NextInt(0, i + 1);
                var temp = candidates[i];
                candidates[i] = candidates[j];
                candidates[j] = temp;
            }

            var take = Mathf.Min(count, candidates.Count);
            for (var i = 0; i < take; i++)
            {
                result.Add(candidates[i]);
            }

            return result;
        }

        /// <summary>应用一个符文；失败会回滚到选择前的状态。</summary>
        public bool ApplyChoice(RuneDef rune)
        {
            if (rune == null)
            {
                return false;
            }

            var oldTrigger = _triggerId;
            var oldEffect = _effectId;
            var oldModifiers = new List<string>(_modifierIds);

            if (rune.Category == RuneCategory.Modifier)
            {
                if (_modifierIds.Count < MaxModifiers)
                {
                    _modifierIds.Add(rune.Id);
                }
                else if (_modifierIds.Count > 0)
                {
                    // 槽位满了：替换最后一个修饰，让玩家能换构筑。
                    _modifierIds[_modifierIds.Count - 1] = rune.Id;
                }
                else
                {
                    _modifierIds.Add(rune.Id);
                }
            }
            else if (rune.Category == RuneCategory.Effect)
            {
                _effectId = rune.Id;
            }
            else
            {
                _triggerId = rune.Id;
            }

            if (TryApply())
            {
                return true;
            }

            _triggerId = oldTrigger;
            _effectId = oldEffect;
            _modifierIds.Clear();
            _modifierIds.AddRange(oldModifiers);
            TryApply();
            return false;
        }

        public CompiledChain CompileCurrent()
        {
            if (_runes == null)
            {
                return null;
            }

            return new ChainCompiler(_runes).Compile(BuildBlueprint());
        }

        /// <summary>HUD 用的一行摘要。</summary>
        public string Summary()
        {
            var trigger = ResolveName(_triggerId);
            var effect = ResolveName(_effectId);

            var text = trigger + " > " + effect;
            for (var i = 0; i < _modifierIds.Count; i++)
            {
                text += (i == 0 ? " > " : " + ") + ResolveName(_modifierIds[i]);
            }

            return text;
        }

        private string ResolveName(string id)
        {
            RuneDef def;
            return _runes != null && _runes.TryGet(id, out def) ? def.Name : id;
        }

        private bool IsValidWith(string modifierId)
        {
            var modifiers = new List<string>(_modifierIds);
            if (modifiers.Count < MaxModifiers)
            {
                modifiers.Add(modifierId);
            }
            else if (modifiers.Count > 0)
            {
                modifiers[modifiers.Count - 1] = modifierId;
            }
            else
            {
                modifiers.Add(modifierId);
            }

            var chain = new ChainCompiler(_runes).Compile(new ChainBlueprint
            {
                TriggerId = _triggerId,
                EffectId = _effectId,
                ModifierIds = modifiers,
                SlotLimit = RuneRules.SliceSlotLimit,
                EnergyLimit = RuneRules.SliceEnergyLimit
            });

            return chain.IsValid;
        }

        private bool TryApply()
        {
            if (_caster == null)
            {
                return false;
            }

            var chain = CompileCurrent();
            if (chain == null || !chain.IsValid)
            {
                return false;
            }

            _caster.Configure(_triggerId, _effectId, _modifierIds.ToArray());
            return _caster.Chain != null && _caster.Chain.IsValid;
        }

        private ChainBlueprint BuildBlueprint()
        {
            return new ChainBlueprint
            {
                TriggerId = _triggerId,
                EffectId = _effectId,
                ModifierIds = new List<string>(_modifierIds),
                SlotLimit = RuneRules.SliceSlotLimit,
                EnergyLimit = RuneRules.SliceEnergyLimit
            };
        }
    }
}
