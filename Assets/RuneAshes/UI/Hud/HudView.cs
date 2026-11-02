// ─────────────────────────────────────────────────────────────────────────────
// 【链路】PlayerHealth / ExperienceSystem / RunProgression / WaveSpawner
//        ──> HudView（本文件，只读展示）
// 【职责】把本局状态显示出来：HP、经验、等级、计时、波次、链摘要、开火模式。
// 【占位】条/面板都是纯色 Image；正式 UI 到位后只替换 Sprite。
// ─────────────────────────────────────────────────────────────────────────────
using RuneAshes.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RuneAshes.UI
{
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] private Canvas _hudCanvas;

        private Image _hpFill;
        private Image _xpFill;
        private Slider _hpSlider;
        private Slider _xpSlider;
        private TMP_Text _hpText;
        private TMP_Text _xpText;
        private TMP_Text _levelText;
        private TMP_Text _timerText;
        private TMP_Text _waveText;
        private TMP_Text _chainText;
        private TMP_Text _fireText;
        private TMP_Text _fpsText;
        private Canvas _canvas;

        private PlayerHealth _health;
        private ExperienceSystem _experience;
        private RuneCaster _caster;
        private RunProgression _progression;
        private WaveSpawner _waves;
        private RunStats _stats;

        private float _startTime;
        private float _chainRefresh;

        private void Awake()
        {
            _hudCanvas = _hudCanvas != null ? _hudCanvas : FindHudCanvas();
            if (_hudCanvas != null)
            {
                _canvas = _hudCanvas;
                BindExisting();
                return;
            }

            _canvas = UiTheme.CreateCanvas("HudCanvas", 0);
            Build();
        }

        private static Canvas FindHudCanvas()
        {
            var root = GameObject.Find("HudRoot");
            return root != null ? root.GetComponent<Canvas>() : null;
        }

        private void BindExisting()
        {
            var root = _hudCanvas.transform;
            _hpSlider = FindComponent<Slider>(root, "HP_Bar");
            _xpSlider = FindComponent<Slider>(root, "XP_Bar");
            _hpFill = FindComponent<Image>(root, "HP_Bar/Fill");
            _xpFill = FindComponent<Image>(root, "XP_Bar/Slider/FillArea/Fill");
            _hpText = FindComponent<TMP_Text>(root, "HP_Bar/HP_Text");
            _xpText = FindComponent<TMP_Text>(root, "XP_Bar/XP_Text");
            _levelText = FindComponent<TMP_Text>(root, "XP_Bar/LevelBadge/Level_Text");
            _timerText = FindComponent<TMP_Text>(root, "Timer");
            _waveText = FindComponent<TMP_Text>(root, "Wave");
            _chainText = FindComponent<TMP_Text>(root, "Chain");
            _fireText = FindComponent<TMP_Text>(root, "Fire");
            _fpsText = FindComponent<TMP_Text>(root, "FPS");
        }

        private static T FindComponent<T>(Transform root, string path) where T : Component
        {
            var child = root.Find(path);
            return child != null ? child.GetComponent<T>() : null;
        }

        private void Start()
        {
            _health = FindFirstObjectByType<PlayerHealth>();
            _experience = FindFirstObjectByType<ExperienceSystem>();
            _caster = FindFirstObjectByType<RuneCaster>();
            _progression = FindFirstObjectByType<RunProgression>();
            _waves = FindFirstObjectByType<WaveSpawner>();
            _stats = FindFirstObjectByType<RunStats>();
            _startTime = Time.time;
        }

        private void Build()
        {
            var parent = _canvas != null ? _canvas.transform : transform;
            var catalog = UiTheme.Catalog;

            // HP 条（左上）：优先使用正式 prefab，缺失时回退纯色条。
            Transform hpRoot;
            var hpPrefab = catalog != null ? catalog.HealthBarPrefab : null;
            if (hpPrefab != null)
            {
                var hpBar = Instantiate(hpPrefab, parent, false);
                var hpRect = hpBar.GetComponent<RectTransform>();
                hpRect.anchorMin = hpRect.anchorMax = new Vector2(0f, 1f);
                hpRect.pivot = new Vector2(0f, 1f);
                hpRect.sizeDelta = new Vector2(360f, 56f);
                hpRect.anchoredPosition = new Vector2(24f, -24f);
                _hpSlider = hpBar.GetComponent<Slider>();
                if (_hpSlider == null)
                {
                    _hpSlider = hpBar.GetComponentInChildren<Slider>();
                }

                var fill = hpBar.transform.Find("Fill");
                _hpFill = fill != null ? fill.GetComponent<Image>() : null;
                hpRoot = hpBar.transform;
            }
            else
            {
                var hpBg = UiTheme.CreateImage(parent, "HP_Bg", UiTheme.Bg,
                    new Vector2(340f, 28f), Vector2.zero);
                Anchor(hpBg.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(24f, -24f));
                _hpFill = UiTheme.CreateImage(hpBg.transform, "HP_Fill", UiTheme.Danger,
                    new Vector2(336f, 24f), Vector2.zero);
                _hpFill.type = Image.Type.Filled;
                _hpFill.fillMethod = Image.FillMethod.Horizontal;
                _hpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
                _hpFill.fillAmount = 1f;
                hpRoot = hpBg.transform;
            }

            _hpText = UiTheme.CreateText(hpRoot, "HP_Text", "HP 100 / 100", 16,
                UiTheme.TextColor, TextAnchor.MiddleCenter, new Vector2(336f, 28f), Vector2.zero);

            // 经验条（底部中央）
            Transform xpRoot;
            var xpPrefab = catalog != null ? catalog.ExpBarPrefab : null;
            if (xpPrefab != null)
            {
                var xpBar = Instantiate(xpPrefab, parent, false);
                var xpRect = xpBar.GetComponent<RectTransform>();
                xpRect.anchorMin = xpRect.anchorMax = new Vector2(0.5f, 0f);
                xpRect.pivot = new Vector2(0.5f, 0f);
                xpRect.sizeDelta = new Vector2(520f, 96f);
                xpRect.anchoredPosition = new Vector2(0f, 24f);
                _xpSlider = xpBar.GetComponent<Slider>();
                if (_xpSlider == null)
                {
                    _xpSlider = xpBar.GetComponentInChildren<Slider>();
                }

                var fill = xpBar.transform.Find("Slider/FillArea/Fill");
                _xpFill = fill != null ? fill.GetComponent<Image>() : null;
                xpRoot = xpBar.transform;
            }
            else
            {
                var xpBg = UiTheme.CreateImage(parent, "XP_Bg", UiTheme.Bg,
                    new Vector2(520f, 16f), Vector2.zero);
                Anchor(xpBg.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 24f));
                _xpFill = UiTheme.CreateImage(xpBg.transform, "XP_Fill", UiTheme.Cyan,
                    new Vector2(516f, 12f), Vector2.zero);
                _xpFill.type = Image.Type.Filled;
                _xpFill.fillMethod = Image.FillMethod.Horizontal;
                _xpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
                _xpFill.fillAmount = 0f;
                xpRoot = xpBg.transform;
            }

            _xpText = UiTheme.CreateText(xpRoot, "XP_Text", "0 / 14", 22, UiTheme.Amber,
                TextAnchor.MiddleCenter, new Vector2(300f, 40f), new Vector2(31f, -18f));

            // 计时 / 波次（顶部中央）
            _timerText = UiTheme.CreateText(parent, "Timer", "00:00", 30,
                UiTheme.TextColor, TextAnchor.MiddleCenter, new Vector2(300f, 40f), Vector2.zero);
            Anchor(_timerText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -12f));

            _waveText = UiTheme.CreateText(parent, "Wave", "波次 0   存活 0", 16,
                UiTheme.Dim, TextAnchor.MiddleCenter, new Vector2(500f, 26f), Vector2.zero);
            Anchor(_waveText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -56f));

            // 链摘要 / 开火模式（右下）
            _chainText = UiTheme.CreateText(parent, "Chain", "—", 14,
                UiTheme.Cyan, TextAnchor.MiddleRight, new Vector2(620f, 28f), Vector2.zero);
            Anchor(_chainText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-24f, 58f));

            _fireText = UiTheme.CreateText(parent, "Fire", "开火：自动（F / Y）", 14,
                UiTheme.Amber, TextAnchor.MiddleRight, new Vector2(620f, 24f), Vector2.zero);
            Anchor(_fireText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-24f, 30f));

            _fpsText = UiTheme.CreateText(parent, "FPS", "FPS 60", 14,
                UiTheme.Dim, TextAnchor.MiddleRight, new Vector2(200f, 26f), Vector2.zero);
            Anchor(_fpsText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-24f, -16f));
        }

        private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
        }

        private void Update()
        {
            if (_health != null)
            {
                var max = Mathf.Max(1f, _health.MaxHealth);
                var ratio = Mathf.Clamp01(_health.Health / max);
                if (_hpSlider != null)
                {
                    _hpSlider.value = ratio;
                }
                else if (_hpFill != null)
                {
                    _hpFill.fillAmount = ratio;
                }

                _hpText.text = "HP " + Mathf.CeilToInt(_health.Health) + " / " + Mathf.CeilToInt(max);
            }

            if (_experience != null)
            {
                if (_xpSlider != null)
                {
                    _xpSlider.value = _experience.Progress01;
                }
                else if (_xpFill != null)
                {
                    _xpFill.fillAmount = _experience.Progress01;
                }

                _xpText.text = _experience.CurrentXp + " / " + _experience.XpToNext;
                if (_levelText != null)
                {
                    _levelText.text = _experience.Level.ToString();
                }
            }

            var elapsed = Mathf.Max(0f, Time.time - _startTime);
            var minutes = Mathf.FloorToInt(elapsed / 60f);
            var seconds = Mathf.FloorToInt(elapsed % 60f);
            _timerText.text = minutes.ToString("00") + ":" + seconds.ToString("00");

            if (_waves != null)
            {
                _waveText.text = "波次 " + _waves.WaveIndex + "   存活 " + _waves.AliveCount +
                                 "   击杀 " + (_stats != null ? _stats.Kills : 0);
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

            var fps = 1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime);
            _fpsText.text = "FPS " + Mathf.RoundToInt(fps);
        }
    }
}
