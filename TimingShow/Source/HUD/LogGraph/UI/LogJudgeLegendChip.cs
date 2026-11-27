using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TimingShow.HUD
{
    public class LogJudgeLegendChip : MaskableGraphic, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        internal const float HeightBase = 22f;
        internal const float PaddingBase = 10f;
        internal const float GapBase = 3f;

        private const float DotSizeBase = 6.5f;
        private const float TextGapBase = 6f;
        private const float FontSizeBase = 11f;
        private const float RadiusBase = 8f;

        private static readonly Color LabelOnColor = LogGraphTheme.TextMain;
        private static readonly Color LabelOffColor = LogGraphTheme.TextFaint;
        private static readonly Color FillColor = LogGraphTheme.SurfaceRaised;
        private static readonly Color FillHoverColor = LogGraphTheme.Rgb(78, 85, 116);

        private TextMeshProUGUI _label;
        private System.Action _onClick;
        private Color _swatch = Color.white;
        private bool _checked = true;
        private bool _hovered;
        private float _scale = 1f;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = true;
            material = defaultMaterial;
            color = Color.white;
            useLegacyMeshGeneration = false;
        }

        internal void Initialize(TMP_FontAsset font, Color swatch, string label, System.Action onClick)
        {
            _swatch = swatch;
            _onClick = onClick;

            GameObject go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(transform, false);

            _label = go.AddComponent<TextMeshProUGUI>();
            _label.font = font != null ? font : TMP_Settings.defaultFontAsset;
            _label.text = label ?? string.Empty;
            _label.color = LabelOnColor;
            _label.alignment = TextAlignmentOptions.MidlineLeft;
            _label.raycastTarget = false;
            _label.textWrappingMode = TextWrappingModes.NoWrap;
            _label.overflowMode = TextOverflowModes.Overflow;

            ApplyLabelLayout();
        }

        internal void SetFont(TMP_FontAsset font)
        {
            if (_label == null || font == null || _label.font == font) return;
            _label.font = font;
        }

        internal void SetLabel(string label)
        {
            if (_label == null || label == null || _label.text == label) return;
            _label.text = label;
        }

        internal void SetChecked(bool value)
        {
            if (_checked == value) return;
            _checked = value;
            ApplyLabelColor();
            SetVerticesDirty();
        }

        internal void SetScale(float scale)
        {
            float clamped = Mathf.Max(0.01f, scale);
            if (Mathf.Approximately(_scale, clamped)) return;
            _scale = clamped;
            ApplyLabelLayout();
            SetVerticesDirty();
        }

        internal void RefreshLabel()
        {
            if (_label == null) return;
            _label.ForceMeshUpdate();
        }

        internal float MeasureWidth()
        {
            float inset = (PaddingBase + DotSizeBase + TextGapBase) * _scale;
            return Mathf.Ceil(inset + MeasureLabelWidth() + PaddingBase * _scale);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
            if (_onClick != null) _onClick();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            SetVerticesDirty();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect = rectTransform != null ? rectTransform.rect : new Rect(0f, 0f, 0f, 0f);
            float w = rect.width;
            float h = rect.height;
            if (w <= 0f || h <= 0f) return;

            float corner = Mathf.Min(RadiusBase * _scale, Mathf.Min(w, h) * 0.5f);
            Color fill = _checked
                ? (_hovered ? FillHoverColor : FillColor)
                : (_hovered ? LogGraphTheme.WithAlpha(Color.white, 0.05f) : new Color(0f, 0f, 0f, 0f));
            if (fill.a > 0f) LogRoundedRect.DrawFilled(vh, rect, corner, fill, 8);

            float dot = DotSizeBase * _scale;
            Vector2 center = new Vector2(rect.xMin + PaddingBase * _scale + dot * 0.5f, rect.center.y);
            if (_checked)
            {
                Color dotColor = _hovered ? LogGraphTheme.Mix(_swatch, Color.white, 0.15f) : _swatch;
                LogGraphTheme.DrawCircle(vh, center, dot * 0.5f, dotColor, 14);
            }
            else
            {
                LogGraphTheme.DrawRing(vh, center, dot * 0.5f, Mathf.Max(1f, 1.6f * _scale), LogGraphTheme.TextFaint, 16);
            }
        }

        private void ApplyLabelLayout()
        {
            if (_label == null) return;

            float scale = _scale;
            _label.fontSize = Mathf.Clamp(Mathf.RoundToInt(FontSizeBase * scale), 8, 32);
            _label.alignment = TextAlignmentOptions.MidlineLeft;

            float inset = (PaddingBase + DotSizeBase + TextGapBase) * scale;
            RectTransform rt = _label.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, 1f);
            rt.offsetMax = new Vector2(-PaddingBase * scale, -1f);

            ApplyLabelColor();
        }

        private void ApplyLabelColor()
        {
            if (_label == null) return;
            Color wanted = _checked ? LabelOnColor : LabelOffColor;
            if (_label.color != wanted) _label.color = wanted;
        }

        private float MeasureLabelWidth()
        {
            if (_label == null) return 0f;

            string content = _label.text ?? string.Empty;
            float fontSize = Mathf.Max(1f, _label.fontSize);
            float estimate = Mathf.Max(1f, content.Length * fontSize * 0.62f);
            try
            {
                Vector2 preferred = _label.GetPreferredValues(content);
                if (preferred.x > 0.5f && preferred.x < 16384f && !float.IsNaN(preferred.x)) return preferred.x;
            }
            catch (System.Exception)
            {
            }

            return estimate;
        }
    }
}
