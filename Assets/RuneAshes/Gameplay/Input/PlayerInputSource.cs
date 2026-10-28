using UnityEngine;
using UnityEngine.InputSystem;

namespace RuneAshes.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PlayerInputSource : MonoBehaviour, IInputSource
    {
        [SerializeField] private InputActionAsset _actions;
        [SerializeField] private string _mapName = "Player";

        private InputAction _move;
        private InputAction _aim;
        private InputAction _fire;
        private InputAction _dash;
        private InputAction _pause;

        public Vector2 Move
        {
            get { return _move != null ? _move.ReadValue<Vector2>() : Vector2.zero; }
        }

        public Vector2 Aim
        {
            get { return _aim != null ? _aim.ReadValue<Vector2>() : Vector2.zero; }
        }

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
