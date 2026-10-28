namespace RuneAshes.Gameplay
{
    public enum DamageType
    {
        Direct,
        Dot,
        Explosion
    }

    /// <summary>
    /// 伤害结算的最小单位。struct + 只读字段，热路径零分配。
    /// </summary>
    public struct DamageEvent
    {
        public int SourceChainId;
        public int CastId;
        public int Depth;
        public float Amount;
        public DamageType Type;
        public bool IgnoreShield;

        public DamageEvent(CastMeta meta, float amount, DamageType type, bool ignoreShield = false)
        {
            SourceChainId = meta.ChainId;
            CastId = meta.CastId;
            Depth = meta.Depth;
            Amount = amount;
            Type = type;
            IgnoreShield = ignoreShield;
        }
    }
}
