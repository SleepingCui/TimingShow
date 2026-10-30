using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TimingShow.HUD.LogGraphWindow
{
    public sealed class LogRangeField : MaskableGraphic, IPointerClickHandler
    {
        private const int MaxLength = 9;
        private const float CaretBlinkSeconds = 0.5f;
        private const float TextInset = 4f;

        private static readonly Color IdleColor = new Color(1f, 1f, 1f, 0.10f);
        private static readonly Color EditColor = new Color(1f, 1f, 1f, 0.20f);
        private static readonly Color TextColor = new Color(0.94f, 0.96f, 1f, 0.96f);
        private static readonly Color CaretColor = new Color(1f, 1f, 1f, 0.85f);

        private TMP_Text _text;
        private RectTransform _textRect;
        private System.Action<string> _onChanged;
        private string _value = "";
        private bool _editing;
        private bool _caretVisible;
        private float _caretTimer;

        public bool IsEditing => _editing;

        public string Value => _value;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = true;
            material = defaultMaterial;
            color = Color.white;
            useLegacyMeshGeneration = false;
        }

        internal void Initialize(TMP_FontAsset font, float fontSize, System.Action<string> onChanged)
        {
            _onChanged = onChanged;

            GameObject go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(transform, false);

            _text = go.AddComponent<TextMeshProUGUI>();
            _text.font = font != null ? font : TMP_Settings.defaultFontAsset;
            _text.fontSize = fontSize;
            _text.color = TextColor;
            _text.alignment = TextAlignmentOptions.MidlineLeft;
            _text.raycastTarget = false;
            _text.textWrappingMode = TextWrappingModes.NoWrap;
            _text.overflowMode = TextOverflowModes.Overflow;

            _textRect = _text.rectTransform;
            _textRect.anchorMin = Vector2.zero;
            _textRect.anchorMax = Vector2.one;
            _textRect.offsetMin = new Vector2(TextInset, 0f);
            _textRect.offsetMax = new Vector2(-TextInset, 0f);
        }

        internal void SetFont(TMP_FontAsset font)
        {
            if (_text == null || font == null || _text.font == font) return;
            _text.font = font;
            _text.SetVerticesDirty();
        }

        internal void SetFontSize(float size)
        {
            if (_text == null) return;
            float clamped = Mathf.Max(8f, size);
            if (Mathf.Approximately(_text.fontSize, clamped)) return;
            _text.fontSize = clamped;
        }

        internal void SetValueSilent(string value)
        {
            string next = value ?? "";
            if (_value == next) return;
            _value = next;
            if (_text != null) _text.text = _value;
            SetVerticesDirty();
        }

        internal void EndEdit()
        {
            if (!_editing) return;
            _editing = false;
            SetVerticesDirty();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
            if (_editing) return;

            _editing = true;
            _caretVisible = true;
            _caretTimer = 0f;
            SetVerticesDirty();
        }

        internal void Tick()
        {
            if (!_editing) return;

            bool changed = false;
            string typed = Input.inputString;
            for (int i = 0; i < typed.Length; i++)
            {
                char c = typed[i];
                if (c == '\b')
                {
                    if (_value.Length > 0)
                    {
                        _value = _value.Substring(0, _value.Length - 1);
                        changed = true;
                    }
                }
                else if (c == '\n' || c == '\r')
                {
                    _editing = false;
                }
                else if (c >= '0' && c <= '9')
                {
                    if (_value.Length < MaxLength)
                    {
                        _value += c;
                        changed = true;
                    }
                }
                else if (c == '-' && _value.Length == 0)
                {
                    _value = "-";
                    changed = true;
                }
                else if (c == '.' && _value.IndexOf('.') < 0 && _value.Length < MaxLength)
                {
                    _value += c;
                    changed = true;
                }
            }

            if (Input.GetKeyDown(KeyCode.Backspace) && typed.IndexOf('\b') < 0 && _value.Length > 0)
            {
                _value = _value.Substring(0, _value.Length - 1);
                changed = true;
            }

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                _editing = false;
            }

            if (changed)
            {
                if (_text != null) _text.text = _value;
                if (_onChanged != null) _onChanged(_value);
            }

            _caretTimer += Time.unscaledDeltaTime;
            if (_caretTimer >= CaretBlinkSeconds)
            {
                _caretTimer = 0f;
                _caretVisible = !_caretVisible;
            }

            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect = rectTransform != null ? rectTransform.rect : new Rect(0f, 0f, 0f, 0f);
            if (rect.width <= 0f || rect.height <= 0f) return;

            float corner = Mathf.Min(LogRoundedRect.ControlRadius, Mathf.Min(rect.width, rect.height) * 0.5f);
            LogRoundedRect.DrawFilled(vh, rect, corner, _editing ? EditColor : IdleColor, 6);

            if (!_editing || !_caretVisible || _text == null) return;

            float textWidth = _text.GetPreferredValues(_value).x;
            float caretX = Mathf.Min(rect.xMax - TextInset, rect.xMin + TextInset + textWidth);
            float half = Mathf.Max(0.75f, rect.height * 0.06f);
            float yMin = rect.yMin + rect.height * 0.22f;
            float yMax = rect.yMax - rect.height * 0.22f;

            int start = vh.currentVertCount;
            vh.AddVert(new Vector3(caretX - half, yMin), CaretColor, Vector2.zero);
            vh.AddVert(new Vector3(caretX - half, yMax), CaretColor, Vector2.zero);
            vh.AddVert(new Vector3(caretX + half, yMax), CaretColor, Vector2.zero);
            vh.AddVert(new Vector3(caretX + half, yMin), CaretColor, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }

    public sealed class LogRangeBar : MaskableGraphic
    {
        private static readonly Color BarColor = new Color(1f, 1f, 1f, 0.045f);
        private static readonly Color BorderColor = new Color(1f, 1f, 1f, 0.10f);

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = true;
            material = defaultMaterial;
            color = Color.white;
            useLegacyMeshGeneration = false;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect = rectTransform != null ? rectTransform.rect : new Rect(0f, 0f, 0f, 0f);
            if (rect.width <= 0f || rect.height <= 0f) return;

            float corner = Mathf.Min(LogRoundedRect.PanelRadius, Mathf.Min(rect.width, rect.height) * 0.5f);
            LogRoundedRect.DrawFilled(vh, rect, corner, BarColor, 8);
            LogRoundedRect.DrawBorder(vh, rect, corner, 1f, BorderColor, 8);
        }
    }
}
