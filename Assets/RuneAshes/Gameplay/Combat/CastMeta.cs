namespace RuneAshes.Gameplay
{
    /// <summary>
    /// 一次施法的身份信息。所有由它派生的弹丸/地雷/召唤物都带着它，
    /// 用于击杀归属、递归深度控制与确定性随机。
    /// </summary>
    public struct CastMeta
    {
        public int ChainId;
        public int CastId;
        public int Depth;
        public uint Seed;

        public CastMeta NextDepth()
        {
            return new CastMeta
            {
                ChainId = ChainId,
                CastId = CastId,
                Depth = Depth + 1,
                Seed = Seed
            };
        }
    }
}
