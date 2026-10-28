namespace RuneAshes.Gameplay
{
    /// <summary>
    /// 可被附加持续效果的实体。与 IDamageable 分开，方便只做伤害的靶子。
    /// </summary>
    public interface IDebuffable
    {
        void ApplyBurn(float dps, float duration, CastMeta meta);

        void ApplySlow(float percent, float duration);
    }
}
