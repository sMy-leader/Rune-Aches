// ─────────────────────────────────────────────────────────────────────────────
// 【链路】CompiledChain ──> RuneCaster（本文件）──> IRuneEffect.Execute()
//        ──> IRuneWorld.SpawnProjectile / DamageArea / …
// 【职责】运行时驱动：决定"什么时候施法"，并组装一次施法的上下文（RuneEffectContext）。
//        它不知道 E1 是弹还是 E5 是斩击——那是效果原语的事，这里只做触发与预算。
// 【W2 现状】只实现 T1 定时触发；T2 击杀 / T3 命中 / T4 冲刺 / T5 受伤 / T6 光环
//           要等 W3 的事件总线。接入时把事件翻译成"调用 Cast()"即可，本类不用重写。
// ─────────────────────────────────────────────────────────────────────────────
using System;
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

        /// <summary>
        /// 本局的随机种子。施法序号会与它混合，保证"同种子 + 同操作 = 同结果"，
        /// 为将来的种子系统/回放/每日挑战留门。
        /// </summary>
        [SerializeField] private uint _runSeed = 20261028u;

        /// <summary>
        /// 是否自动开火。默认 true（幸存者类基线）；F / 手柄 Y 切换。
        /// 关闭后按住鼠标左键 / RT 仍会按 T1 间隔手动施法，松开即停。
        /// </summary>
        [SerializeField] private bool _autoFire = true;

        private IInputSource _input;

        private CompiledChain _chain;
        private IRuneEffect _effect;
        private IRuneWorld _world;
        private PlayerController _player;
        private ModifierSet _modifiers;
        private ProcBudget _procBudget;
        private float _timer;
        private int _castCounter;

        /// <summary>当前链的编译结果。HUD/测试通过它读能耗、槽位与错误。</summary>
        public CompiledChain Chain
        {
            get { return _chain; }
        }

        /// <summary>
        /// 一次施法成功后触发。动画/音效/统计订阅它，不要每帧轮询 _castCounter。
        /// [注意] 被 ProcBudget 丢弃的施法不会触发；只有真正执行了效果才发。
        /// </summary>
        public event Action CastPerformed;

        /// <summary>当前是否自动开火。HUD 读它显示 ON/OFF。</summary>
        public bool AutoFireEnabled
        {
            get { return _autoFire; }
        }

        /// <summary>切换自动开火。关闭后按住 Fire（鼠标左键 / RT）仍可按 T1 间隔手动施法。</summary>
        public void ToggleAutoFire()
        {
            _autoFire = !_autoFire;
        }

        public void SetAutoFire(bool enabled)
        {
            _autoFire = enabled;
        }

        /// <summary>
        /// 换链入口：三选一 UI、预设切换、分享码导入最终都走这里。
        /// 它会立即重新编译，所以下一帧开火用的就是新链。
        /// </summary>
        public void Configure(string triggerId, string effectId, string[] modifierIds)
        {
            _triggerId = triggerId;
            _effectId = effectId;
            _modifierIds = modifierIds ?? new string[0];
            Rebuild();
        }

        private void Awake()
        {
            // IRuneWorld 就在玩家身上（由场景构建器挂载）；找不到就全场景找一次，
            // 让测试场景/手动搭的场景也能跑。
            _player = GetComponent<PlayerController>();
            _input = GetComponent<IInputSource>();
            _world = GetComponent<UnityRuneWorld>();
            if (_world == null)
            {
                _world = FindFirstObjectByType<UnityRuneWorld>();
            }

            Rebuild();
        }

        /// <summary>
        /// 编译当前链，并创建"效果原语 + 修饰参数 + 全局预算"三件运行时依赖。
        /// [坑] W2 每次换链都会 new TableProvider + LoadAll（重建字典）。
        ///      换链是低频操作所以能接受；W3 应把 provider 提到启动时注入，
        ///      否则三选一频繁预览时会白白分配。
        /// </summary>
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

            // 自动开火切换对当前所有链都生效；先处理输入，再决定是否推进 T1 计时。
            if (_input != null && _input.ToggleFirePressed)
            {
                _autoFire = !_autoFire;
            }

            // W2 只有 T1（定时）。其他触发器会在事件到来时直接调用 Cast()，
            // 不走这里的每帧轮询。
            if (_chain.Trigger.Id != "T1")
            {
                return;
            }

            // 预算窗口必须每帧推进，否则窗口永远不重置，链会"开局 64 次后就永久哑火"。
            _procBudget.Tick(Time.deltaTime);

            // 自动开火关闭时：不按键就完全停手；按住 Fire 仍按 T1 间隔手动施法。
            var wantsFire = _autoFire || (_input != null && _input.FireHeld);
            if (!wantsFire)
            {
                // 清零而不是保留：恢复开火时不会立刻补发一発。
                _timer = 0f;
                return;
            }

            // 0.05s 是硬下限：表里填 0 或负数时，退化成每帧施法会直接打爆帧率。
            var interval = Mathf.Max(0.05f, _chain.Trigger.GetFloat("interval", 0.8f));
            _timer += Time.deltaTime;
            if (_timer >= interval)
            {
                // 用减法而不是清零：掉帧时能补发，长期节奏不会越走越慢。
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
        /// <summary>
        /// 真正执行一次施法。调用前必须已经有合法链与效果实例。
        /// 顺序很关键：先过全局预算，再构造 Context，最后交给效果原语。
        /// </summary>
        public void Cast(Vector3 direction)
        {
            if (_chain == null || _effect == null || _world == null)
            {
                return;
            }

            _castCounter++;

            // 预算挡的是"整次施法"，不是单颗弹丸：
            // 一次 E2 散射仍然只占 1 次 Proc，否则散射类构筑会被预算误杀。
            if (_procBudget != null && !_procBudget.TryConsume())
            {
                return;
            }

            // CastId 让同一次施法派生的所有弹丸/爆炸共享身份，用于击杀归属与"同帧合并"。
            // Depth=0 表示这是玩家直接施法，之后由事件派生的才 +1。
            var meta = new CastMeta
            {
                ChainId = 1,
                CastId = _castCounter,
                Depth = 0,
                Seed = Hash(_runSeed, (uint)_castCounter)
            };

            // 发射点抬高 0.9m：与灰盒靶子的受击高度对齐，也避免弹丸出生就贴地。
            var context = new RuneEffectContext
            {
                Origin = transform.position + Vector3.up * 0.9f,
                Direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward,
                // 效果伤害从表里读；将来等级强化就乘在这里，效果代码不用改。
                BaseValue = _chain.Effect.BaseValue,
                ChainDepth = 0,
                Seed = meta.Seed,
                Meta = meta,
                Modifiers = _modifiers,
                World = _world
            };

            _effect.Execute(ref context);

            if (CastPerformed != null)
            {
                CastPerformed();
            }
        }

        /// <summary>
        /// 把 runSeed 与施法序号混合成种子。用哈希而不是直接相加，
        /// 是为了让相邻施法的随机序列不相关（否则同种子回放时弹道会整排重复）。
        /// </summary>
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
