// ─────────────────────────────────────────────────────────────────────────────
// 【职责】正式 UI 素材的运行时索引。美术资源在 Assets/Art/UI 下，这个资产只保存引用，
//        不复制图片；代码通过 UiTheme.Catalog 读取，避免把路径散落在各个界面。
// ─────────────────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuneAshes.UI
{
    [CreateAssetMenu(menuName = "Rune Ashes/UI Sprite Catalog", fileName = "UiSpriteCatalog")]
    public sealed class UiSpriteCatalog : ScriptableObject
    {
        [Serializable]
        public struct RuneIconEntry
        {
            public string Id;
            public Sprite Icon;
        }

        [SerializeField] private Sprite _panelMain;
        [SerializeField] private Sprite _chainPanel;
        [SerializeField] private Sprite _cardFrameCommon;
        [SerializeField] private GameObject _healthBarPrefab;
        [SerializeField] private GameObject _expBarPrefab;
        [SerializeField] private Sprite _cardFrameRare;
        [SerializeField] private Sprite _cardFrameFull;
        [SerializeField] private Sprite _cardFrameIntensify;
        [SerializeField] private Sprite _cardIconSlot;
        [SerializeField] private Sprite _buttonPrimary;
        [SerializeField] private Sprite _buttonSecondary;
        [SerializeField] private Sprite _chainSlotTrigger;
        [SerializeField] private Sprite _chainSlotEffect;
        [SerializeField] private Sprite _chainSlotModifier;
        [SerializeField] private Sprite _hudSlotCircle;
        [SerializeField] private Sprite _hudSlotSquare;
        [SerializeField] private RuneIconEntry[] _runeIcons = Array.Empty<RuneIconEntry>();

        public Sprite PanelMain { get { return _panelMain; } }
        public Sprite ChainPanel { get { return _chainPanel; } }
        public GameObject HealthBarPrefab { get { return _healthBarPrefab; } }
        public GameObject ExpBarPrefab { get { return _expBarPrefab; } }
        public Sprite CardFrameCommon { get { return _cardFrameCommon; } }
        public Sprite CardFrameRare { get { return _cardFrameRare; } }
        public Sprite CardFrameFull { get { return _cardFrameFull; } }
        public Sprite CardFrameIntensify { get { return _cardFrameIntensify; } }
        public Sprite CardIconSlot { get { return _cardIconSlot; } }
        public Sprite ButtonPrimary { get { return _buttonPrimary; } }
        public Sprite ButtonSecondary { get { return _buttonSecondary; } }
        public Sprite ChainSlotTrigger { get { return _chainSlotTrigger; } }
        public Sprite ChainSlotEffect { get { return _chainSlotEffect; } }
        public Sprite ChainSlotModifier { get { return _chainSlotModifier; } }
        public Sprite HudSlotCircle { get { return _hudSlotCircle; } }
        public Sprite HudSlotSquare { get { return _hudSlotSquare; } }

        public bool TryGetRuneIcon(string id, out Sprite sprite)
        {
            if (_runeIcons != null)
            {
                for (var i = 0; i < _runeIcons.Length; i++)
                {
                    if (string.Equals(_runeIcons[i].Id, id, StringComparison.Ordinal))
                    {
                        sprite = _runeIcons[i].Icon;
                        return sprite != null;
                    }
                }
            }

            sprite = null;
            return false;
        }

        public IReadOnlyList<RuneIconEntry> RuneIcons
        {
            get { return _runeIcons ?? Array.Empty<RuneIconEntry>(); }
        }
    }
}
