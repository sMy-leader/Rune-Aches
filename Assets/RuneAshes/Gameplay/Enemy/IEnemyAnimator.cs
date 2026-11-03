namespace RuneAshes.Gameplay
{
    /// <summary>
    /// 敌人表现适配器：EnemyBrain 只依赖这组动画语义，不依赖具体 Controller 的参数名。
    /// </summary>
    public interface IEnemyAnimator
    {
        void SetLocomotion(float value01);
        void PlayAttack();
        void PlayHit();
        void PlayDeath();
        void PlayExplode();
        bool IsInAttackState();
        float CurrentNormalizedTime();
        float CurrentAttackImpactNormalized();
    }
}
