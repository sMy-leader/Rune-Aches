// ─────────────────────────────────────────────────────────────────────────────
// 【链路】PlayerHealth / ExperienceSystem / RunProgression / WaveSpawner
//        ──> HudView（本文件，只读展示）
// 【职责】把本局状态显示出来：HP、经验、等级、计时、波次、链摘要、开火模式。
// 【占位】条/面板都是纯色 Image；正式 UI 到位后只替换 Sprite。
// ─────────────────────────────────────────────────────────────────────────────
using RuneAshes.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace RuneAshes.UI
{
    public sealed class HudView : MonoBehaviour
    {
        private Image _hpFill;
        private Image _xpFill;
        private Text _hpText;
        private Text _xpText;
        private Text _timerText;
        private Text _waveText;
        private Text _chainText;
        private Text _fireText;
        private Canvas _canvas;

        private PlayerHealth _health;
        private ExperienceSystem _experience;
        private RuneCaster _caster;
        private RunProgression _progression;
        private WaveSpawner _waves;

        private float _startTime;
        private float _chainRefresh;

        private void Awake()
        {
            _canvas = UiTheme.CreateCanvas("HudCanvas", 0);
            Build();
        }

        private void Start()
        {
            _health = FindFirstObjectByType<PlayerHealth>();
            _experience = FindFirstObjectByType<ExperienceSystem>();
            _caster = FindFirstObjectByType<RuneCaster>();
            _progression = FindFirstObjectByType<RunProgression>();
            _waves = FindFirstObjectByType<WaveSpawner>();
            _startTime = Time.time;
        }

        private void Build()
        {
            var parent = _canvas != null ? _canvas.transform : transform;

            // HP 条（左上）
            var hpBg = UiTheme.CreateImage(parent, "HP_Bg", UiTheme.Bg,
                new Vector2(340f, 28f), new Vector2(-760f, 470f));
            _hpFill = UiTheme.CreateImage(hpBg.transform, "HP_Fill", UiTheme.Danger,
                new Vector2(336f, 24f), Vector2.zero);
            _hpFill.type = Image.Type.Filled;
            _hpFill.fillMethod = Image.FillMethod.Horizontal;
            _hpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _hpFill.fillAmount = 1f;
            _hpText = UiTheme.CreateText(hpBg.transform, "HP_Text", "HP 100 / 100", 16,
                UiTheme.TextColor, TextAnchor.MiddleCenter, new Vector2(336f, 28f), Vector2.zero);

            // 经验条（底部中央）
            var xpBg = UiTheme.CreateImage(parent, "XP_Bg", UiTheme.Bg,
                new Vector2(520f, 16f), new Vector2(0f, -505f));
            _xpFill = UiTheme.CreateImage(xpBg.transform, "XP_Fill", UiTheme.Cyan,
                new Vector2(516f, 12f), Vector2.zero);
            _xpFill.type = Image.Type.Filled;
            _xpFill.fillMethod = Image.FillMethod.Horizontal;
            _xpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _xpFill.fillAmount = 0f;
            _xpText = UiTheme.CreateText(parent, "XP_Text", "Lv.1   0 / 14", 16,
                UiTheme.TextColor, TextAnchor.MiddleCenter, new Vector2(520f, 24f), new Vector2(0f, -478f));

            // 计时 / 波次（顶部中央）
            _timerText = UiTheme.CreateText(parent, "Timer", "00:00", 30,
                UiTheme.TextColor, TextAnchor.MiddleCenter, new Vector2(300f, 40f), new Vector2(0f, 505f));
            _waveText = UiTheme.CreateText(parent, "Wave", "波次 0   存活 0", 16,
                UiTheme.Dim, TextAnchor.MiddleCenter, new Vector2(500f, 26f), new Vector2(0f, 465f));

            // 链摘要 / 开火模式（右下）
            _chainText = UiTheme.CreateText(parent, "Chain", "—", 14,
                UiTheme.Cyan, TextAnchor.MiddleRight, new Vector2(620f, 28f), new Vector2(620f, -455f));
            _fireText = UiTheme.CreateText(parent, "Fire", "开火：自动（F / Y）", 14,
                UiTheme.Amber, TextAnchor.MiddleRight, new Vector2(620f, 24f), new Vector2(620f, -485f));
        }

        private void Update()
        {
            if (_health != null)
            {
                var max = Mathf.Max(1f, _health.MaxHealth);
                _hpFill.fillAmount = Mathf.Clamp01(_health.Health / max);
                _hpText.text = "HP " + Mathf.CeilToInt(_health.Health) + " / " + Mathf.CeilToInt(max);
            }

            if (_experience != null)
            {
                _xpFill.fillAmount = _experience.Progress01;
                _xpText.text = "Lv." + _experience.Level + "   " + _experience.CurrentXp + " / " +
                               _experience.XpToNext;
            }

            var elapsed = Mathf.Max(0f, Time.time - _startTime);
            var minutes = Mathf.FloorToInt(elapsed / 60f);
            var seconds = Mathf.FloorToInt(elapsed % 60f);
            _timerText.text = minutes.ToString("00") + ":" + seconds.ToString("00");

            if (_waves != null)
            {
                _waveText.text = "波次 " + _waves.WaveIndex + "   存活 " + _waves.AliveCount;
            }

            _chainRefresh -= Time.deltaTime;
            if (_chainRefresh <= 0f)
            {
                _chainRefresh = 0.25f;
                if (_progression != null)
                {
                    _chainText.text = _progression.Summary();
                }
            }

            if (_caster != null)
            {
                _fireText.text = _caster.AutoFireEnabled
                    ? "开火：自动（F / Y 切换）"
                    : "开火：手动（按住左键 / RT）";
            }
        }
    }
}
