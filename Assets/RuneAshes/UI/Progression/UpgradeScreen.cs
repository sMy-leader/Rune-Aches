// ─────────────────────────────────────────────────────────────────────────────
// 【链路】ExperienceSystem.LeveledUp ──> UpgradeScreen（本文件）
//        ──> RunProgression.GetChoices / ApplyChoice
// 【职责】升级时暂停游戏，显示三选一，选完恢复。它不判断"能不能装"，
//        候选由 RunProgression 预检过；这里只负责展示与输入。
// 【占位】正式卡牌/按钮/图标到位后，只替换 Sprite，不改布局与交互。
// ─────────────────────────────────────────────────────────────────────────────
using System.Collections.Generic;
using RuneAshes.Data;
using RuneAshes.Framework;
using RuneAshes.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RuneAshes.UI
{
    public sealed class UpgradeScreen : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private RectTransform _cardRoot;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _hint;

        private RunProgression _progression;
        private ExperienceSystem _experience;

        private readonly List<RuneDef> _choices = new List<RuneDef>();
        private readonly List<GameObject> _cards = new List<GameObject>();

        private bool _showing;
        private bool _paused;
        private int _pendingLevels;

        private void Awake()
        {
            if (_canvas != null)
            {
                BindExisting();
                _canvas.gameObject.SetActive(false);
                return;
            }

            _canvas = UiTheme.CreateCanvas("UpgradeCanvas", 100);
            BuildShell();
            _canvas.gameObject.SetActive(false);
        }

        private void BindExisting()
        {
            var panel = _canvas.transform.Find("Overlay/Panel");
            if (panel == null)
            {
                return;
            }

            _cardRoot = panel.Find("Cards") as RectTransform;
            _title = panel.Find("Title") != null ? panel.Find("Title").GetComponent<TMP_Text>() : null;
            _hint = panel.Find("Hint") != null ? panel.Find("Hint").GetComponent<TMP_Text>() : null;
        }

        private void Start()
        {
            _experience = FindFirstObjectByType<ExperienceSystem>();
            _progression = FindFirstObjectByType<RunProgression>();

            if (_experience != null)
            {
                _experience.LeveledUp += OnLeveledUp;
            }
        }

        private void OnDestroy()
        {
            if (_experience != null)
            {
                _experience.LeveledUp -= OnLeveledUp;
            }

            if (_paused && GameTime.Current != null)
            {
                GameTime.Current.PopPause();
            }
        }

        private void OnLeveledUp(int level)
        {
            _pendingLevels++;
            if (!_showing)
            {
                Show();
            }
        }

        private void BuildShell()
        {
            var overlay = UiTheme.CreateImage(_canvas.transform, "Overlay",
                new Color(0f, 0f, 0f, 0.72f), new Vector2(1920f, 1080f), Vector2.zero, true);

            var panelSprite = UiTheme.Catalog != null ? UiTheme.Catalog.PanelMain : null;
            var panel = UiTheme.CreateImage(overlay.transform, "Panel",
                panelSprite != null ? Color.white : UiTheme.Bg,
                new Vector2(1440f, 780f), Vector2.zero, sprite: panelSprite);

            UiTheme.CreateImage(panel.transform, "TopLine", UiTheme.Amber,
                new Vector2(1440f, 4f), new Vector2(0f, 388f));
            UiTheme.CreateImage(panel.transform, "BottomLine", UiTheme.Line,
                new Vector2(1440f, 2f), new Vector2(0f, -388f));

            _title = UiTheme.CreateText(panel.transform, "Title", "选择符文", 40,
                UiTheme.Amber, TextAnchor.MiddleCenter, new Vector2(900f, 60f), new Vector2(0f, 320f));
            _hint = UiTheme.CreateText(panel.transform, "Hint", "选择一张，装入当前符文链", 16,
                UiTheme.Dim, TextAnchor.MiddleCenter, new Vector2(900f, 30f), new Vector2(0f, 270f));

            var root = new GameObject("Cards", typeof(RectTransform));
            _cardRoot = root.GetComponent<RectTransform>();
            _cardRoot.SetParent(panel.transform, false);
            _cardRoot.anchorMin = _cardRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _cardRoot.sizeDelta = new Vector2(1200f, 480f);
            _cardRoot.anchoredPosition = new Vector2(0f, -20f);
        }

        private void Show()
        {
            if (_progression == null)
            {
                _pendingLevels = 0;
                return;
            }

            _showing = true;
            _pendingLevels = Mathf.Max(0, _pendingLevels - 1);

            if (GameTime.Current != null)
            {
                GameTime.Current.PushPause();//进入一层暂停->由UI控制
                _paused = true;
            }

            UiTheme.EnsureEventSystem();
            _canvas.gameObject.SetActive(true);

            BuildCards();
        }

        private void BuildCards()
        {
            ClearCards();
            _choices.Clear();
            _choices.AddRange(_progression.GetChoices(3));

            _title.text = _experience != null ? "升到 Lv." + _experience.Level + "：选择符文" : "选择符文";
            _hint.text = _choices.Count > 0
                ? "数字键 1/2/3 或点击卡牌；选择后继续战斗"
                : "没有可装配的符文，按继续回到战斗";

            if (_choices.Count == 0)
            {
                var buttonSprite = UiTheme.Catalog != null ? UiTheme.Catalog.ButtonPrimary : null;
                var continueImage = UiTheme.CreateImage(_cardRoot, "Continue",
                    buttonSprite != null ? Color.white : UiTheme.Amber,
                    new Vector2(240f, 64f), new Vector2(0f, -300f), true, buttonSprite, false, true);
                UiTheme.CreateText(continueImage.transform, "Label", "继续", 22,
                    buttonSprite != null ? UiTheme.TextColor : UiTheme.Bg,
                    TextAnchor.MiddleCenter, new Vector2(240f, 64f), Vector2.zero);

                var button = continueImage.gameObject.AddComponent<Button>();
                button.targetGraphic = continueImage;
                button.transition = Selectable.Transition.ColorTint;
                var colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = UiTheme.Amber;
                colors.pressedColor = Color.Lerp(UiTheme.Amber, Color.white, 0.35f);
                colors.selectedColor = UiTheme.Amber;
                button.colors = colors;
                button.onClick.AddListener(Close);
                _cards.Add(continueImage.gameObject);
                return;
            }

            for (var i = 0; i < _choices.Count; i++)
            {
                CreateCard(_choices[i], i, _choices.Count);
            }
        }

        private void CreateCard(RuneDef rune, int index, int total)
        {
            var categoryColor = UiTheme.CategoryColor(rune.Category);
            var x = (index - (total - 1) * 0.5f) * 380f;

            var catalog = UiTheme.Catalog;
            var frameSprite = GetFrameSprite(catalog, rune.Rarity);
            var card = UiTheme.CreateImage(_cardRoot, "Card_" + rune.Id,
                frameSprite != null ? Color.white : UiTheme.Slate,
                new Vector2(320f, 440f), new Vector2(x, 0f), true, frameSprite, true);

            UiTheme.CreateImage(card.transform, "TopStrip", categoryColor,
                new Vector2(320f, 6f), new Vector2(0f, 217f));

            var slotSprite = catalog != null ? catalog.CardIconSlot : null;
            var iconBg = UiTheme.CreateImage(card.transform, "IconBg",
                slotSprite != null ? Color.white : Color.Lerp(UiTheme.Slate, categoryColor, 0.28f),
                new Vector2(180f, 180f), new Vector2(0f, 90f), false, slotSprite, true);

            Sprite runeIcon;
            if (catalog != null && catalog.TryGetRuneIcon(rune.Id, out runeIcon))
            {
                UiTheme.CreateImage(iconBg.transform, "Icon", Color.white,
                    new Vector2(128f, 128f), Vector2.zero, false, runeIcon, true);
            }
            else
            {
                UiTheme.CreateText(iconBg.transform, "IconId", rune.Id, 44, categoryColor,
                    TextAnchor.MiddleCenter, new Vector2(180f, 180f), Vector2.zero);
            }

            UiTheme.CreateText(card.transform, "Name", rune.Name, 24, categoryColor,
                TextAnchor.MiddleCenter, new Vector2(300f, 34f), new Vector2(0f, -30f));

            UiTheme.CreateText(card.transform, "Desc", rune.Description, 15, UiTheme.TextColor,
                TextAnchor.UpperCenter, new Vector2(280f, 110f), new Vector2(0f, -105f));

            UiTheme.CreateText(card.transform, "Cost", "能耗 " + rune.EnergyCost, 14, UiTheme.Amber,
                TextAnchor.MiddleCenter, new Vector2(200f, 26f), new Vector2(0f, -195f));

            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = categoryColor;
            colors.pressedColor = Color.Lerp(categoryColor, Color.white, 0.35f);
            colors.selectedColor = categoryColor;
            button.colors = colors;

            var captured = index;
            button.onClick.AddListener(() => Select(captured));
            _cards.Add(card.gameObject);
        }

        private static Sprite GetFrameSprite(UiSpriteCatalog catalog, RuneRarity rarity)
        {
            if (catalog == null)
            {
                return null;
            }

            switch (rarity)
            {
                case RuneRarity.Rare:
                    return catalog.CardFrameRare;
                case RuneRarity.Intensify:
                    return catalog.CardFrameIntensify;
                case RuneRarity.Full:
                    return catalog.CardFrameFull;
                default:
                    return catalog.CardFrameCommon;
            }
        }

        private void Update()
        {
            if (!_showing)
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                Select(0);
            }
            else if (keyboard.digit2Key.wasPressedThisFrame)
            {
                Select(1);
            }
            else if (keyboard.digit3Key.wasPressedThisFrame)
            {
                Select(2);
            }
        }

        //选择卡牌->根据索引应用选择
        private void Select(int index)
        {
            if (!_showing || index < 0 || index >= _choices.Count)
            {
                return;
            }

            _progression.ApplyChoice(_choices[index]);
            Close();
        }

        private void Close()
        {
            _showing = false;
            _canvas.gameObject.SetActive(false);

            if (_paused && GameTime.Current != null)
            {
                GameTime.Current.PopPause();//取出一层暂停
            }

            _paused = false;

            if (_pendingLevels > 0)
            {
                Show();
            }
        }

        private void ClearCards()
        {
            for (var i = _cards.Count - 1; i >= 0; i--)
            {
                if (_cards[i] != null)
                {
                    Destroy(_cards[i]);
                }
            }

            _cards.Clear();
        }
    }
}
