using System.Collections.Generic;
using RuneAshes.Data;

namespace RuneAshes.Gameplay
{
    /// <summary>
    /// 把 RuneDef 列表折算成 ModifierSet。修饰只影响参数，不改变效果原语类型。
    /// </summary>
    public sealed class RuneModifierFactory
    {
        public ModifierSet Build(IReadOnlyList<RuneDef> modifiers)
        {
            var set = new ModifierSet();
            if (modifiers == null)
            {
                return set;
            }

            for (var i = 0; i < modifiers.Count; i++)
            {
                var modifier = modifiers[i];
                if (modifier == null)
                {
                    continue;
                }

                switch (modifier.Id)
                {
                    case "M1":
                        set.SplitCount = modifier.GetInt("count", 3);
                        set.SplitInherit = modifier.GetFloat("inherit", 0.6f);
                        break;
                    case "M2":
                        set.Bounces = modifier.GetInt("bounces", 2);
                        set.BounceFalloff = modifier.GetFloat("falloff", 0.2f);
                        break;
                    case "M3":
                        set.Pierce = modifier.GetInt("pierce", 2);
                        break;
                    case "M4":
                        set.BurnDps = modifier.GetFloat("dps", 4f);
                        set.BurnDuration = modifier.GetFloat("duration", 3f);
                        break;
                    case "M6":
                        set.ExplosionRadius = modifier.GetFloat("radius", 2.5f);
                        set.ExplosionDamage = modifier.BaseValue;
                        break;
                    case "M7":
                        set.TurnRate = modifier.GetFloat("turnrate", 180f);
                        break;
                }
            }

            return set;
        }
    }
}
