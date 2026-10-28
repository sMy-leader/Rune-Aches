// ─────────────────────────────────────────────────────────────────────────────
// 【定位】W2 的临时验证工具，不是正式 UI。
// 【它验证什么】按 1/2/3 切换三条 4 槽预设链 → RuneCaster 重编译 → HUD 显示能耗/槽位/错误。
//              用来证明"改链真的会改变战斗行为"，也是 W3 三选一 UI 的临时替身。
// 【W3 会被替换】正式升级界面走 uGUI（RuneAshes.UI 程序集），本文件保留为调试面板。
// 【为什么用 OnGUI】灰盒期不需要 Canvas/Prefab/字体资源，几十行就能出信息，删掉也无成本。
// ─────────────────────────────────────────────────────────────────────────────
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RuneAshes.Gameplay
{
    /// <summary>
    /// 灰盒调试 HUD：1/2/3 切换三条 4 槽预设链，验证"改链 → 行为变化"。
    /// 正式三选一 UI 走 uGUI，W3 接入经验/升级后替换本组件。
    /// </summary>
    public sealed class DebugChainHud : MonoBehaviour
    {
        /// <summary>
        /// 三条预设对应三种验证目标：持续输出、爆发清群、单体穿透。
        /// [坑] 这里硬编码是为了 W2 快速验证；W3 接装配 UI 后应改成从存档/三选一结果生成。
        /// </summary>
        private static readonly ChainBlueprint[] Presets =
        {
            new ChainBlueprint
            {
                TriggerId = "T1",
                EffectId = "E1",
                ModifierIds = new List<string> { "M2", "M4" }
            },
            new ChainBlueprint
            {
                TriggerId = "T1",
                EffectId = "E2",
                ModifierIds = new List<string> { "M1", "M6" }
            },
            new ChainBlueprint
            {
                TriggerId = "T1",
                EffectId = "E1",
                ModifierIds = new List<string> { "M3", "M7" }
            }
        };

        private static readonly string[] PresetNames = { "弹射燃烧", "分裂爆炸", "穿透追踪" };

        private RuneCaster _caster;
        private int _presetIndex;

        private void Awake()
        {
            _caster = GetComponent<RuneCaster>();
        }

        private void Start()
        {
            if (_caster != null)
            {
                ApplyPreset(0);
            }
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                ApplyPreset(0);
            }
            else if (keyboard.digit2Key.wasPressedThisFrame)
            {
                ApplyPreset(1);
            }
            else if (keyboard.digit3Key.wasPressedThisFrame)
            {
                ApplyPreset(2);
            }
        }

        /// <summary>
        /// 切预设 = 换链。这是 W2 唯一会调用 RuneCaster.Configure 的地方，
        /// 也是"三选一"逻辑的最小替身：真实版本会把玩家选择写进蓝图再走同一个入口。
        /// </summary>
        public void ApplyPreset(int index)
        {
            if (_caster == null || index < 0 || index >= Presets.Length)
            {
                return;
            }

            _presetIndex = index;
            var preset = Presets[index];
            _caster.Configure(preset.TriggerId, preset.EffectId, preset.ModifierIds.ToArray());
        }

        private void OnGUI()
        {
            if (_caster == null)
            {
                return;
            }

            var chain = _caster.Chain;
            GUI.Box(new Rect(12f, 12f, 430f, 104f), string.Empty);

            if (chain == null)
            {
                GUI.Label(new Rect(22f, 18f, 410f, 24f), "Chain: <null>");
                return;
            }

            var line = "Chain " + (_presetIndex + 1) + ": " +
                       (chain.Trigger != null ? chain.Trigger.Name : "?") + " -> " +
                       (chain.Effect != null ? chain.Effect.Name : "?");

            for (var i = 0; i < chain.Modifiers.Count; i++)
            {
                line += " -> " + chain.Modifiers[i].Name;
            }

            GUI.Label(new Rect(22f, 18f, 410f, 24f), line);
            GUI.Label(new Rect(22f, 42f, 410f, 24f),
                "能量 " + chain.EnergyCost + "/" + RuneRules.SliceEnergyLimit +
                "   槽位 " + chain.SlotCount + "/" + RuneRules.SliceSlotLimit +
                (chain.IsValid ? "   OK" : "   INVALID"));

            var hint = string.Empty;
            for (var i = 0; i < PresetNames.Length; i++)
            {
                hint += "[" + (i + 1) + "] " + PresetNames[i] + "   ";
            }

            GUI.Label(new Rect(22f, 66f, 410f, 24f), hint);

            if (!chain.IsValid && chain.Errors.Count > 0)
            {
                GUI.Label(new Rect(22f, 88f, 410f, 24f), chain.Errors[0]);
            }
        }
    }
}
