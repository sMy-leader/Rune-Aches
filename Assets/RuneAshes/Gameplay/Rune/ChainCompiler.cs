using System;
using System.Collections.Generic;
using RuneAshes.Data;

namespace RuneAshes.Gameplay
{
    /// <summary>
    /// 链编译器：一次收集全部错误，装配 UI 可以逐条显示"为什么装不上"。
    /// 纯逻辑，不依赖场景，可单测。
    /// </summary>
    public sealed class ChainCompiler
    {
        private readonly RuneTable _runes;

        public ChainCompiler(RuneTable runes)
        {
            if (runes == null)
            {
                throw new ArgumentNullException("runes");
            }

            _runes = runes;
        }

        public CompiledChain Compile(ChainBlueprint blueprint)
        {
            var errors = new List<string>();
            if (blueprint == null)
            {
                errors.Add("Chain blueprint is null.");
                return new CompiledChain(null, null, new List<RuneDef>(), 0, 0, errors);
            }

            RuneDef trigger = null;
            RuneDef effect = null;
            var modifiers = new List<RuneDef>();
            var seenModifiers = new HashSet<string>(StringComparer.Ordinal);
            var energy = 0;

            if (string.IsNullOrWhiteSpace(blueprint.TriggerId))
            {
                errors.Add("Trigger slot is empty.");
            }
            else if (!_runes.TryGet(blueprint.TriggerId, out trigger))
            {
                errors.Add("Unknown trigger '" + blueprint.TriggerId + "'.");
            }
            else if (trigger.Category != RuneCategory.Trigger)
            {
                errors.Add("'" + trigger.Id + "' is not a trigger.");
                trigger = null;
            }
            else
            {
                energy += trigger.EnergyCost;
            }

            if (string.IsNullOrWhiteSpace(blueprint.EffectId))
            {
                errors.Add("Effect slot is empty.");
            }
            else if (!_runes.TryGet(blueprint.EffectId, out effect))
            {
                errors.Add("Unknown effect '" + blueprint.EffectId + "'.");
            }
            else if (effect.Category != RuneCategory.Effect)
            {
                errors.Add("'" + effect.Id + "' is not an effect.");
                effect = null;
            }
            else
            {
                energy += effect.EnergyCost;
            }

            for (var i = 0; i < blueprint.ModifierIds.Count; i++)
            {
                var id = blueprint.ModifierIds[i];
                RuneDef modifier;
                if (string.IsNullOrWhiteSpace(id))
                {
                    errors.Add("Modifier slot " + (i + 1) + " is empty.");
                    continue;
                }

                if (!_runes.TryGet(id, out modifier))
                {
                    errors.Add("Unknown modifier '" + id + "'.");
                    continue;
                }

                if (modifier.Category != RuneCategory.Modifier)
                {
                    errors.Add("'" + modifier.Id + "' is not a modifier.");
                    continue;
                }

                if (!RuneRules.AllowDuplicateModifiers && !seenModifiers.Add(modifier.Id))
                {
                    errors.Add("Duplicate modifier '" + modifier.Id + "' (each modifier can appear once per chain).");
                    continue;
                }

                modifiers.Add(modifier);
                energy += modifier.EnergyCost;
            }

            var slots = (trigger != null ? 1 : 0) + (effect != null ? 1 : 0) + modifiers.Count;

            if (slots > blueprint.SlotLimit)
            {
                errors.Add("Slots " + slots + " exceed limit " + blueprint.SlotLimit + " (remove " +
                           (slots - blueprint.SlotLimit) + ").");
            }

            if (energy > blueprint.EnergyLimit)
            {
                errors.Add("Energy " + energy + " exceeds limit " + blueprint.EnergyLimit + " (remove " +
                           (energy - blueprint.EnergyLimit) + ").");
            }

            return new CompiledChain(trigger, effect, modifiers, energy, slots, errors);
        }
    }
}
