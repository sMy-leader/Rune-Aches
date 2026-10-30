using NUnit.Framework;
using RuneAshes.Framework;
using UnityEngine;

namespace RuneAshes.Tests
{
    public sealed class TimeServiceTests
    {
        [Test]
        public void PauseAndHitStop_AreLayered()
        {
            var go = new GameObject("time_test");
            var time = go.AddComponent<UnityTime>();
            time.Reset();

            time.RequestHitStop(10f, 0.05f);
            Assert.AreEqual(0.05f, time.EffectiveScale, 0.0001f);

            time.PushPause();
            Assert.AreEqual(0f, time.EffectiveScale);
            Assert.IsTrue(time.IsPaused);

            // 两层暂停：退出一层仍应保持暂停。
            time.PushPause();
            time.PopPause();
            Assert.AreEqual(0f, time.EffectiveScale);

            time.PopPause();
            Assert.AreEqual(0.05f, time.EffectiveScale, 0.0001f);

            time.ClearHitStop();
            Assert.AreEqual(1f, time.EffectiveScale, 0.0001f);

            time.Reset();
            Object.DestroyImmediate(go);
        }

        [Test]
        public void HitStopExpiredDuringPause_DoesNotLeakSlowMotion()
        {
            var go = new GameObject("time_test_2");
            var time = go.AddComponent<UnityTime>();
            time.Reset();

            // 模拟用户报告的 bug：顿帧 0.05，随后升级暂停，顿帧在暂停期间过期。
            time.RequestHitStop(0.05f, 0.05f);
            time.PushPause();
            Assert.AreEqual(0f, time.EffectiveScale);

            time.ClearHitStop();
            time.PopPause();

            Assert.AreEqual(1f, time.EffectiveScale, 0.0001f);
            time.Reset();
            Object.DestroyImmediate(go);
        }
    }
}
