namespace RuneAshes.Gameplay
{
    /// <summary>
    /// 一条链上所有修饰折算后的效果参数。用 struct 传给弹丸/地雷/环绕球，
    /// 避免运行时反复查 RuneDef。
    /// </summary>
    public struct ModifierSet
    {
        public int SplitCount;
        public float SplitInherit;
        public int Bounces;
        public float BounceFalloff;
        public int Pierce;
        public float TurnRate;
        public float BurnDps;
        public float BurnDuration;
        public float ExplosionRadius;
        public float ExplosionDamage;

        public bool HasAny
        {
            get
            {
                return SplitCount > 0 || Bounces > 0 || Pierce > 0 || TurnRate > 0f ||
                       BurnDps > 0f || ExplosionRadius > 0f;
            }
        }
    }
}
