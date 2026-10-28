using UnityEngine;

namespace RuneAshes.Gameplay
{
    /// <summary>
    /// 输入抽象。NGO 联机时用网络输入实现同一接口，玩法层不感知来源。
    /// </summary>
    public interface IInputSource
    {
        Vector2 Move { get; }

        /// <summary>鼠标：屏幕坐标；手柄：右摇杆方向。由消费方区分设备语义。</summary>
        Vector2 Aim { get; }

        /// <summary>当前瞄准输入是否来自手柄右摇杆（用于区分屏幕坐标与世界方向）。</summary>
        bool IsGamepadActive { get; }

        bool FirePressed { get; }

        bool FireHeld { get; }

        bool DashPressed { get; }

        bool PausePressed { get; }

        void Enable();

        void Disable();
    }
}
