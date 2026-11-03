// ─────────────────────────────────────────────────────────────────────────────
// 【职责】链编辑器静态原型的开关。当前只负责暂停/显示 prefab，
//        拖拽、槽位校验、真实装配留给下一阶段。
// ─────────────────────────────────────────────────────────────────────────────
using RuneAshes.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RuneAshes.UI
{
    public sealed class ChainEditorScreen : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;

        private bool _open;
        private bool _paused;

        private void Awake()
        {
            // 手工拖 prefab 时引用可能没序列化；按名字找 inactive Canvas。
            if (_canvas == null)
            {
                var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (var i = 0; i < canvases.Length; i++)
                {
                    if (canvases[i] != null && canvases[i].name == "ChainEditorRoot")
                    {
                        _canvas = canvases[i];
                        break;
                    }
                }
            }
        }

        private void Start()
        {
            if (_canvas != null)
            {
                _canvas.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || _canvas == null)
            {
                return;
            }

            if (keyboard.tabKey.wasPressedThisFrame)
            {
                Toggle();
            }
            else if (_open && keyboard.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        private void Toggle()
        {
            if (_open)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        private void Open()
        {
            _open = true;
            UiTheme.EnsureEventSystem();
            _canvas.gameObject.SetActive(true);

            if (GameTime.Current != null)
            {
                GameTime.Current.PushPause();
                _paused = true;
            }
        }

        private void Close()
        {
            _open = false;
            _canvas.gameObject.SetActive(false);

            if (_paused && GameTime.Current != null)
            {
                GameTime.Current.PopPause();
            }

            _paused = false;
        }

        private void OnDestroy()
        {
            if (_paused && GameTime.Current != null)
            {
                GameTime.Current.PopPause();
            }
        }
    }
}
