using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TimingShow.HUD.ScatterDrawerBase
{
    public abstract partial class ScatterDrawerBase
    {
        private const float LegendSwatchSize = 8f;
        private const float LegendGap = 5f;
        private const float LegendPadding = 4f;

        private struct LegendItem
        {
            public int PlayerId;
            public Rect Rect;
        }

        private readonly TMP_Text[] _legendTexts = new TMP_Text[4];
        private readonly LegendItem[] _legendItems = new LegendItem[4];
        private int _legendItemCount;
        private int _playerFilter = -1;

        public int SelectedPlayerId { get { return _playerFilter; } }

        public System.Action PlayerFilterChanged;

        protected virtual int ScatterPlayerCount { get { return 1; } }

        protected virtual bool ScatterDistinguishPlayers { get { return false; } }

        protected virtual bool AllowPlayerFilter { get { return false; } }

        protected bool ShowPlayerLegend
        {
            get { return ScatterDistinguishPlayers && AllowPlayerFilter && ScatterPlayerCount > 1; }
        }

        protected bool ColorPointsByPlayer
        {
            get { return ScatterDistinguishPlayers && ScatterPlayerCount > 1 && _playerFilter < 0; }
        }

        private void CreatePlayerLegendTexts()
        {
            for (int i = 0; i < _legendTexts.Length; i++)
            {
                TMP_Text text = CreateText("LegendLabel" + i, CurrentFontAsset, TextAnchor.MiddleLeft);
                text.gameObject.SetActive(false);
                _legendTexts[i] = text;
            }
        }

        private void ApplyPlayerLegendFont(TMP_FontAsset font)
        {
            for (int i = 0; i < _legendTexts.Length; i++) ApplyFontToText(_legendTexts[i], font);
        }

        private void TogglePlayerLegend(bool active)
        {
            bool show = active && ShowPlayerLegend;
            for (int i = 0; i < _legendTexts.Length; i++)
            {
                if (_legendTexts[i] == null) continue;
                bool want = show && i < _legendItemCount;
                if (_legendTexts[i].gameObject.activeSelf != want) _legendTexts[i].gameObject.SetActive(want);
            }
        }

        private void UpdatePlayerLegendLayout(float w, float h)
        {
            _legendItemCount = 0;

            if (!ShowPlayerLegend)
            {
                for (int i = 0; i < _legendTexts.Length; i++)
                {
                    if (_legendTexts[i] != null && _legendTexts[i].gameObject.activeSelf)
                        _legendTexts[i].gameObject.SetActive(false);
                }
                return;
            }

            int count = Mathf.Clamp(ScatterPlayerCount, 1, _legendTexts.Length);
            float scale = Mathf.Max(0.01f, Scale);
            int fontSize = Mathf.Clamp(Mathf.RoundToInt(11 * scale), 8, 32);
            float swatch = LegendSwatchSize * scale;
            float gap = LegendGap * scale;
            float itemHeight = Mathf.Max(swatch, fontSize * 1.4f);
            float x = LegendPadding * scale;
            float centerY = h - LegendPadding * scale - itemHeight * 0.5f;

            for (int i = 0; i < count; i++)
            {
                TMP_Text text = _legendTexts[i];
                string label = Coop.Label(i);
                float textWidth = text != null ? MeasureTextWidth(text, label, fontSize) : label.Length * fontSize * 0.62f;
                float itemWidth = swatch + gap * 0.5f + textWidth;

                LegendItem item = new LegendItem
                {
                    PlayerId = i,
                    Rect = new Rect(x, centerY - itemHeight * 0.5f, itemWidth, itemHeight)
                };
                _legendItems[i] = item;
                _legendItemCount = i + 1;

                if (text != null)
                {
                    text.fontSize = fontSize;
                    if (text.text != label) text.text = label;
                    Color color = Coop.PlayerColor(i);
                    if (_playerFilter >= 0 && _playerFilter != i) color.a *= 0.4f;
                    text.color = color;
                    text.alignment = TextAlignmentOptions.MidlineLeft;

                    RectTransform rt = text.rectTransform;
                    rt.pivot = new Vector2(0f, 0.5f);
                    rt.anchoredPosition = new Vector2(x + swatch + gap * 0.5f, centerY);
                    rt.sizeDelta = new Vector2(Mathf.Max(1f, textWidth), itemHeight);
                }

                x += itemWidth + gap;
            }

            TogglePlayerLegend(true);
        }

        private void DrawPlayerLegendSwatches(VertexHelper vh, float w, float h)
        {
            if (!ShowPlayerLegend || _legendItemCount <= 0) return;

            float scale = Mathf.Max(0.01f, Scale);
            float swatch = LegendSwatchSize * scale;

            for (int i = 0; i < _legendItemCount; i++)
            {
                LegendItem item = _legendItems[i];
                Color color = Coop.PlayerColor(item.PlayerId);
                if (_playerFilter >= 0 && _playerFilter != item.PlayerId) color.a *= 0.4f;

                float x0 = item.Rect.x;
                float y0 = item.Rect.center.y - swatch * 0.5f;
                DrawQuad(vh, new Vector2(x0, y0), new Vector2(x0 + swatch, y0 + swatch), color);

                if (_playerFilter == item.PlayerId)
                {
                    Color ring = Color.white;
                    ring.a = 0.9f;
                    float half = Mathf.Max(0.5f, 0.8f * scale);
                    DrawSegment(vh, new Vector2(x0, y0), new Vector2(x0 + swatch, y0), half, ring);
                    DrawSegment(vh, new Vector2(x0 + swatch, y0), new Vector2(x0 + swatch, y0 + swatch), half, ring);
                    DrawSegment(vh, new Vector2(x0 + swatch, y0 + swatch), new Vector2(x0, y0 + swatch), half, ring);
                    DrawSegment(vh, new Vector2(x0, y0 + swatch), new Vector2(x0, y0), half, ring);
                }
            }
        }

        private bool HitPlayerLegend(Vector2 local, out int playerId)
        {
            playerId = -1;
            if (!ShowPlayerLegend) return false;

            for (int i = 0; i < _legendItemCount; i++)
            {
                Rect rect = _legendItems[i].Rect;
                if (local.x >= rect.xMin && local.x <= rect.xMax && local.y >= rect.yMin && local.y <= rect.yMax)
                {
                    playerId = _legendItems[i].PlayerId;
                    return true;
                }
            }

            return false;
        }

        public bool TryPlayerLegendClick(Vector2 screenPosition)
        {
            if (!ShowPlayerLegend) return false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPosition, null, out Vector2 local)) return false;

            int playerId;
            if (!HitPlayerLegend(local, out playerId)) return false;

            TogglePlayerFilter(playerId);
            return true;
        }

        protected void ResetPlayerFilter()
        {
            _playerFilter = -1;
        }

        private void TogglePlayerFilter(int playerId)
        {
            _playerFilter = _playerFilter == playerId ? -1 : playerId;
            NotifyPlayerFilterChanged();
        }

        private void NotifyPlayerFilterChanged()
        {
            InvalidateDataCache();
            _hoverSourceIndex = -1;
            HideDetailPanel();
            UpdateData();
            UpdateTextLayoutAndValues();
            SetVerticesDirty();

            System.Action handler = PlayerFilterChanged;
            if (handler != null) handler();
        }
    }
}
