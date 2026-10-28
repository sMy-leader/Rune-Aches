// ─────────────────────────────────────────────────────────────────────────────
// 【链路】InputActionAsset（.inputactions）──> PlayerInputSource（本文件）
//        ──> IInputSource ──> PlayerController
// 【职责】把 Unity Input System 的 Action 读成业务层能用的几个属性。
// 【为什么多一层 IInputSource】PlayerController 不直接依赖 Input System：
//        将来接 NGO（远端输入）、回放（录制输入）、AI（自动操作）时只换实现。
// 【配置】_actions 必须在 Inspector 里指定 RuneAshes.inputactions，
//        场景构建器（ArenaBlockoutBuilder）已经自动挂好；手搭场景时要自己拖。
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;
using UnityEngine.InputSystem;

namespace RuneAshes.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PlayerInputSource : MonoBehaviour, IInputSource
    {
        [SerializeField] private InputActionAsset _actions;

        /// <summary>Action Map 名称。改 .inputactions 里的 Map 名时这里要同步。</summary>
        [SerializeField] private string _mapName = "Player";

        private InputAction _move;
        private InputAction _aim;
        private InputAction _fire;
        private InputAction _dash;
        private InputAction _pause;
        private InputAction _toggleFire;

        public Vector2 Move
        {
            get { return _move != null ? _move.ReadValue<Vector2>() : Vector2.zero; }
        }

        /// <summary>
        /// 鼠标时是屏幕坐标，手柄时是右摇杆向量——由 PlayerController 根据 IsGamepadActive 解释。
        /// 同一个属性承载两种语义，是为了让上层只写一次"瞄准"逻辑。
        /// </summary>
        public Vector2 Aim
        {
            get { return _aim != null ? _aim.ReadValue<Vector2>() : Vector2.zero; }
        }

        /// <summary>
        /// 通过"最后一次产生输入的设备"判断当前是否手柄操作。
        /// [坑] 鼠标/手柄同时有输入时会来回切换，导致瞄准源抖动；
        ///      如果实机测试发现准星跳，改成"最后一次非零输入"或固定设备模式。
        /// </summary>
        public bool IsGamepadActive
        {
            get
            {
                if (_aim == null)
                {
                    return false;
                }

                var control = _aim.activeControl;
                return control != null && control.device is Gamepad;
            }
        }

        public bool FirePressed
        {
            get { return _fire != null && _fire.WasPressedThisFrame(); }
        }

        public bool FireHeld
        {
            get { return _fire != null && _fire.IsPressed(); }
        }

        /// <summary>F / 手柄 Y：切换自动开火。关闭后 FireHeld 用来手动施法。</summary>
        public bool ToggleFirePressed
        {
            get { return _toggleFire != null && _toggleFire.WasPressedThisFrame(); }
        }

        public bool DashPressed
        {
            get { return _dash != null && _dash.WasPressedThisFrame(); }
        }

        public bool PausePressed
        {
            get { return _pause != null && _pause.WasPressedThisFrame(); }
        }

        public void Enable()
        {
            if (_actions != null)
            {
                _actions.Enable();
            }
        }

        public void Disable()
        {
            if (_actions != null)
            {
                _actions.Disable();
            }
        }

        private void Awake()
        {
            Resolve();
        }

        private void OnEnable()
        {
            Enable();
        }

        private void OnDisable()
        {
            Disable();
        }

        private void Resolve()
        {
            if (_actions == null)
            {
                Debug.LogError("[Input] InputActionAsset is not assigned on " + name + ".", this);
                return;
            }

            _move = ResolveAction("Move");
            _aim = ResolveAction("Aim");
            _fire = ResolveAction("Fire");
            _toggleFire = ResolveAction("ToggleFire");
            _dash = ResolveAction("Dash");
            _pause = ResolveAction("Pause");
        }

        private InputAction ResolveAction(string actionName)
        {
            var action = _actions.FindAction(_mapName + "/" + actionName, false);
            if (action == null)
            {
                Debug.LogError("[Input] Missing action '" + _mapName + "/" + actionName + "'.", this);
            }

            return action;
        }
    }
}
