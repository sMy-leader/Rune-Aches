// ─────────────────────────────────────────────────────────────────────────────
// 【链路】PlayerHealth.Died ──> SettlementScreen（本文件）
// 【职责】死亡后暂停并显示本局结算：存活时间、击杀、等级、构筑链、再来一局。
// 【占位】面板/按钮仍是纯色 uGUI；正式 UI 素材到位后只替换 Sprite。
// ─────────────────────────────────────────────────────────────────────────────
using RuneAshes.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RuneAshes.UI
{
    public sealed class SettlementScreen : MonoBehaviour
    {
        private Canvas _canvas;
        private Text _statsText;
        private Text _chainText;

        private PlayerHealth _health;
        private RunStats _stats;
        private ExperienceSystem _experience;
        private RunProgression _progression;

        private bool _showing;

        private void Awake()
        {
            _canvas = UiTheme.CreateCanvas("SettlementCanvas", 200);
            Build();
            _canvas.gameObject.SetActive(false);
        }

        private void Start()
        {
            _health = FindFirstObjectByType<PlayerHealth>();
            _stats = FindFirstObjectByType<RunStats>();
            _experience = FindFirstObjectByType<ExperienceSystem>();
            _progression = FindFirstObjectByType<RunProgression>();

            if (_health != null)
            {
                _health.Died += OnDied;
            }
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.Died -= OnDied;
            }
        }

        private void Build()
        {
            var overlay = UiTheme.CreateImage(_canvas.transform, "Overlay",
                new Color(0f, 0f, 0f, 0.80f), new Vector2(1920f, 1080f), Vector2.zero, true);
            var panel = UiTheme.CreateImage(overlay.transform, "Panel", UiTheme.Bg,
                new Vector2(1100f, 640f), Vector2.zero);

            UiTheme.CreateImage(panel.transform, "TopLine", UiTheme.Danger,
                new Vector2(1100f, 4f), new Vector2(0f, 318f));

            UiTheme.CreateText(panel.transform, "Title", "本局结束", 44, UiTheme.Danger,
                TextAnchor.MiddleCenter, new Vector2(900f, 64f), new Vector2(0f, 245f));

            _statsText = UiTheme.CreateText(panel.transform, "Stats", "—", 22, UiTheme.TextColor,
                TextAnchor.UpperLeft, new Vector2(900f, 180f), new Vector2(0f, 60f));

            _chainText = UiTheme.CreateText(panel.transform, "Chain", "—", 18, UiTheme.Cyan,
                TextAnchor.MiddleCenter, new Vector2(900f, 60f), new Vector2(0f, -80f));

            var buttonImage = UiTheme.CreateImage(panel.transform, "Restart", UiTheme.Amber,
                new Vector2(280f, 68f), new Vector2(0f, -235f), true);
            UiTheme.CreateText(buttonImage.transform, "Label", "再来一局", 24, UiTheme.Bg,
                TextAnchor.MiddleCenter, new Vector2(280f, 68f), Vector2.zero);

            var button = buttonImage.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(Restart);
        }

        private void OnDied()
        {
            if (_showing)
            {
                return;
            }

            _showing = true;
            Time.timeScale = 0f;
            _canvas.gameObject.SetActive(true);
            Refresh();
        }

        private void Refresh()
        {
            var elapsed = _stats != null ? _stats.Elapsed : 0f;
            var minutes = Mathf.FloorToInt(elapsed / 60f);
            var seconds = Mathf.FloorToInt(elapsed % 60f);
            var kills = _stats != null ? _stats.Kills : 0;
            var level = _experience != null ? _experience.Level : 1;

            _statsText.text =
                "存活时间    " + minutes.ToString("00") + ":" + seconds.ToString("00") + "\n\n" +
                "击杀        " + kills + "\n\n" +
                "等级        Lv." + level;

            _chainText.text = _progression != null ? "构筑    " + _progression.Summary() : string.Empty;
        }

        private void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
