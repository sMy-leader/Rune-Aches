// ─────────────────────────────────────────────────────────────────────────────
// 【链路】PlayerInputSource / 将来的网络输入 ──> IInputSource（本文件）
//        ──> PlayerController
// 【职责】输入抽象。玩法层只读这几个属性，不关心键鼠、手柄还是远端玩家。
// 【Move 与 Aim 的区别】Move 是"想往哪走"（世界 XZ 方向）；
//        Aim 是"想打哪"，但鼠标时是屏幕坐标、手柄时是摇杆向量，
//        所以消费方必须先看 IsGamepadActive 再解释它。
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;

namespace RuneAshes.Gameplay
{
    /// <summary>
    /// 输入抽象。NGO 联机时用网络输入实现同一接口，玩法层不感知来源。
    /// </summary>
    public interface IInputSource
    {
        /// <summary>移动输入（-1~1）。键盘 WASD 或手柄左摇杆。</summary>
        Vector2 Move { get; }

        /// <summary>鼠标：屏幕坐标；手柄：右摇杆方向。由消费方区分设备语义。</summary>
        Vector2 Aim { get; }

        /// <summary>当前瞄准输入是否来自手柄右摇杆（用于区分屏幕坐标与世界方向）。</summary>
        bool IsGamepadActive { get; }

        /// <summary>本帧按下开火。W2 自动开火，暂未消费。</summary>
        bool FirePressed { get; }

        /// <summary>开火键是否按住。W2 自动开火，暂未消费。</summary>
        bool FireHeld { get; }

        bool DashPressed { get; }

        bool PausePressed { get; }

        void Enable();

        void Disable();
    }
}
