using System.Collections.Generic;

namespace RuneAshes.Gameplay
{
    /// <summary>
    /// 玩家编辑中的链。只存 ID 与等级，不存对象引用——分享码与存档都直接序列化它。
    /// </summary>
    public sealed class ChainBlueprint
    {
        public string TriggerId;
        public string EffectId;
        public List<string> ModifierIds = new List<string>();
        public int SlotLimit = RuneRules.SliceSlotLimit;
        public int EnergyLimit = RuneRules.SliceEnergyLimit;

        public ChainBlueprint Clone()
        {
            return new ChainBlueprint
            {
                TriggerId = TriggerId,
                EffectId = EffectId,
                ModifierIds = new List<string>(ModifierIds),
                SlotLimit = SlotLimit,
                EnergyLimit = EnergyLimit
            };
        }
    }
}
