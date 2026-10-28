using System;
using RuneAshes.Data;

namespace RuneAshes.Gameplay
{
    /// <summary>
    /// 效果原语注册表。所有符文共享这里的 5 个原语，不为单个符文写分支逻辑。
    /// </summary>
    public sealed class RuneEffectFactory
    {
        public IRuneEffect Create(RuneDef effect)
        {
            if (effect == null)
            {
                throw new ArgumentNullException("effect");
            }

            switch (effect.Id)
            {
                case "E1":
                    return new DirectShotEffect(effect);
                case "E2":
                    return new SpreadShotEffect(effect);
                case "E3":
                    return new OrbitingOrbEffect(effect);
                case "E4":
                    return new MineEffect(effect);
                case "E5":
                    return new DashSlashEffect(effect);
                default:
                    throw new NotSupportedException("No effect primitive registered for '" + effect.Id + "'.");
            }
        }
    }
}
