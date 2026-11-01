using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TimingShow.HUD.LogGraphWindow
{
    public sealed class LogRangeField : MaskableGraphic, IPointerDownHandler, IPointerUpHandler,
        IPointerClickHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private const int MaxLength = 9;
        private const float TextInset = 4f;
        private const float CaretWidth = 1.5f;
        private const float CaretBlinkSeconds = 0.53f;
        private const float RepeatDelay = 0.45f;
        private const float RepeatInterval = 0.045f;
        private const float RingThickness = 1.25f;
        private const int UndoLimit = 32;

        private static readonly Color IdleTopColor = new Color(1f, 1f, 1f, 0.10f);
        private static readonly Color IdleBottomColor = new Color(1f, 1f, 1f, 0.055f);
        private static readonly Color HoverTopColor = new Color(1f, 1f, 1f, 0.15f);
        private static readonly Color HoverBottomColor = new Color(1f, 1f, 1f, 0.09f);
        private static readonly Color EditTopColor = new Color(1f, 1f, 1f, 0.20f);
        private static readonly Color EditBottomColor = new Color(1f, 1f, 1f, 0.12f);
        private static readonly Color HoverBorderColor = new Color(1f, 1f, 1f, 0.14f);
        private static readonly Color FocusBorderColor = new Color(0.30f, 0.76f, 1f, 0.62f);
        private static readonly Color TextColor = new Color(0.94f, 0.96f, 1f, 0.96f);
        private static readonly Color HintColor = new Color(1f, 1f, 1f, 0.26f);
        private static readonly Color CaretColor = new Color(1f, 1f, 1f, 0.92f);
        private static readonly Color SelectionColor = new Color(0.30f, 0.76f, 1f, 0.32f);

        private struct EditState
        {
            public string Value;
            public int Caret;
        }

        private readonly List<EditState> _undo = new List<EditState>();
        private readonly List<EditState> _redo = new List<EditState>();

        private TMP_Text _text;
        private TMP_Text _hint;
        private RectTransform _textRect;
        private System.Action<string> _onChanged;
        private System.Action<LogRangeField> _onCommit;
        private string _value = "";
        private string _valueAtFocus = "";
        private bool _editing;
        private bool _hover;
        private bool _caretVisible;
        private bool _dragSelecting;
        private float _caretTimer;
        private float _scroll;
        private float _appliedScroll;
        private float _textWidth;
        private float _caretX;
        private float _selectionStartX;
        private float _selectionEndX;
        private float _lastWidth = -1f;
        private float _lastHeight = -1f;
        private int _caret;
        private int _selectionAnchor;
        private KeyCode _holdKey = KeyCode.None;
        private float _holdTimer;

        public bool IsEditing { get { return _editing; } }

        public string Value { get { return _value; } }

        private bool HasSelection { get { return _caret != _selectionAnchor; } }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = true;
            material = defaultMaterial;
            color = Color.white;
            useLegacyMeshGeneration = false;
        }

        internal void Initialize(TMP_FontAsset font, float fontSize, string hint, System.Action<string> onChanged,
            System.Action<LogRangeField> onCommit)
        {
            _onChanged = onChanged;
            _onCommit = onCommit;

            GameObject clipGo = new GameObject("TextClip", typeof(RectTransform));
            clipGo.transform.SetParent(transform, false);

            RectTransform clipRect = (RectTransform)clipGo.transform;
            clipRect.anchorMin = Vector2.zero;
            clipRect.anchorMax = Vector2.one;
            clipRect.offsetMin = new Vector2(TextInset, 0f);
            clipRect.offsetMax = new Vector2(-TextInset, 0f);
            clipGo.AddComponent<RectMask2D>();

            _text = CreateLabel("Value", font, fontSize, TextColor, clipRect);
            _text.text = _value;
            _hint = CreateLabel("Hint", font, fontSize, HintColor, clipRect);
            _hint.text = hint ?? "";
            LogGraphFont.EnsureGlyphs(_hint.font, _hint.text);

            _textRect = _text.rectTransform;
            UpdateHintVisibility();
        }

        private static TMP_Text CreateLabel(string name, TMP_FontAsset font, float fontSize, Color color,
            RectTransform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.font = font != null ? font : TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return text;
        }

        internal void SetFont(TMP_FontAsset font)
        {
            if (font == null) return;
            SetLabelFont(_text, font);
            SetLabelFont(_hint, font);
            if (_text != null) _text.ForceMeshUpdate();
            UpdateScroll();
        }

        private static void SetLabelFont(TMP_Text label, TMP_FontAsset font)
        {
            if (label == null || label.font == font) return;
            label.font = font;
            label.SetVerticesDirty();
        }

        internal void SetFontSize(float size)
        {
            float clamped = Mathf.Max(8f, size);
            SetLabelFontSize(_text, clamped);
            SetLabelFontSize(_hint, clamped);
            if (_text != null) _text.ForceMeshUpdate();
            UpdateScroll();
        }

        private static void SetLabelFontSize(TMP_Text label, float size)
        {
            if (label == null || Mathf.Approximately(label.fontSize, size)) return;
            label.fontSize = size;
            label.SetVerticesDirty();
        }

        internal void SetValueSilent(string value)
        {
            string next = value ?? "";
            if (_value == next) return;

            _value = next;
            _caret = _value.Length;
            _selectionAnchor = _caret;
            if (_text != null)
            {
                _text.text = _value;
                _text.ForceMeshUpdate();
            }

            UpdateHintVisibility();
            UpdateScroll();
            SetVerticesDirty();
        }

        internal void BeginEdit()
        {
            if (_editing) return;

            _editing = true;
            _valueAtFocus = _value;
            _caret = _value.Length;
            _selectionAnchor = _caret;
            _dragSelecting = false;
            _holdKey = KeyCode.None;
            MarkCaretActive();
            UpdateScroll();
            SetVerticesDirty();
        }

        internal void FocusSelectAll()
        {
            BeginEdit();
            SelectAll();
        }

        internal void EndEdit()
        {
            if (!_editing) return;

            _editing = false;
            _dragSelecting = false;
            _selectionAnchor = _caret;
            SetVerticesDirty();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;

            bool extend = _editing && IsShiftHeld();
            if (!_editing) BeginEdit();
            _dragSelecting = true;
            _caret = IndexAt(eventData != null ? eventData.position : (Vector2)Input.mousePosition);
            if (!extend) _selectionAnchor = _caret;
            MarkCaretActive();
            UpdateScroll();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
            if (!_editing || !_dragSelecting) return;

            _caret = IndexAt(eventData != null ? eventData.position : (Vector2)Input.mousePosition);
            MarkCaretActive();
            UpdateScroll();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _dragSelecting = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
            if (eventData == null || eventData.clickCount < 2) return;

            SelectAll();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_hover) return;
            _hover = true;
            SetVerticesDirty();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_hover) return;
            _hover = false;
            SetVerticesDirty();
        }

        internal void Tick()
        {
            if (!_editing) return;

            if (_holdKey != KeyCode.None && !Input.GetKey(_holdKey)) _holdKey = KeyCode.None;

            bool dirty = false;
            bool ctrl = IsControlHeld();
            bool shift = IsShiftHeld();
            bool backspaceTyped = false;
            bool enterTyped = false;

            string typed = Input.inputString;
            for (int i = 0; i < typed.Length; i++)
            {
                char c = typed[i];
                if (c == '\b')
                {
                    backspaceTyped = true;
                    dirty |= DeleteBackward(ctrl);
                }
                else if (c == '\n' || c == '\r')
                {
                    enterTyped = true;
                }
                else if (c >= ' ' && !ctrl)
                {
                    dirty |= ReplaceSelection(c);
                }
            }

            if (ctrl)
            {
                if (Input.GetKeyDown(KeyCode.A)) SelectAll();
                if (Input.GetKeyDown(KeyCode.C)) CopySelection();
                if (Input.GetKeyDown(KeyCode.X)) dirty |= CutSelection();
                if (Input.GetKeyDown(KeyCode.V)) dirty |= PasteClipboard();
                if (Input.GetKeyDown(KeyCode.Z)) dirty |= UndoStep(shift);
                if (Input.GetKeyDown(KeyCode.Y)) dirty |= UndoStep(true);
            }

            if (!backspaceTyped && Hold(KeyCode.Backspace)) dirty |= DeleteBackward(ctrl);
            if (Hold(KeyCode.Delete)) dirty |= DeleteForward(ctrl);

            if (Hold(KeyCode.LeftArrow)) MoveCaret(-1, shift);
            else if (Hold(KeyCode.RightArrow)) MoveCaret(1, shift);
            if (Input.GetKeyDown(KeyCode.Home)) MoveCaretTo(0, shift);
            if (Input.GetKeyDown(KeyCode.End)) MoveCaretTo(_value.Length, shift);

            if (dirty) ApplyValue();
            else UpdateScroll();

            if (enterTyped || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)
                || Input.GetKeyDown(KeyCode.Tab))
            {
                Submit();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelEdit();
                return;
            }

            Rect rect = rectTransform != null ? rectTransform.rect : new Rect(0f, 0f, 0f, 0f);
            if (!Mathf.Approximately(rect.width, _lastWidth) || !Mathf.Approximately(rect.height, _lastHeight))
            {
                _lastWidth = rect.width;
                _lastHeight = rect.height;
                UpdateScroll();
                dirty = true;
            }

            _caretTimer += Time.unscaledDeltaTime;
            if (_caretTimer >= CaretBlinkSeconds)
            {
                _caretTimer -= CaretBlinkSeconds;
                _caretVisible = !_caretVisible;
                dirty = true;
            }

            if (dirty) SetVerticesDirty();
        }

        private void Submit()
        {
            EndEdit();
            if (_onCommit != null) _onCommit(this);
        }

        private void CancelEdit()
        {
            _editing = false;
            _dragSelecting = false;
            _value = _valueAtFocus;
            _caret = _value.Length;
            _selectionAnchor = _caret;
            _undo.Clear();
            _redo.Clear();
            ApplyValue();
        }

        private void ApplyValue()
        {
            if (_text != null)
            {
                _text.text = _value;
                _text.ForceMeshUpdate();
            }

            UpdateHintVisibility();
            MarkCaretActive();
            UpdateScroll();
            SetVerticesDirty();
            if (_onChanged != null) _onChanged(_value);
        }

        private void UpdateHintVisibility()
        {
            if (_hint == null) return;
            bool visible = _value.Length == 0;
            if (_hint.gameObject.activeSelf != visible) _hint.gameObject.SetActive(visible);
        }

        private void MarkCaretActive()
        {
            _caretVisible = true;
            _caretTimer = 0f;
        }

        private void SelectAll()
        {
            _selectionAnchor = 0;
            _caret = _value.Length;
            MarkCaretActive();
            UpdateScroll();
            SetVerticesDirty();
        }

        private bool ReplaceSelection(char c)
        {
            int start = SelectionStart();
            int end = SelectionEnd();
            string candidate = _value.Substring(0, start) + c + _value.Substring(end);
            if (candidate == _value || !IsAcceptable(candidate)) return false;

            RecordUndo();
            _value = candidate;
            _caret = start + 1;
            _selectionAnchor = _caret;
            return true;
        }

        private bool ReplaceSelection(string text)
        {
            int start = SelectionStart();
            int end = SelectionEnd();
            string candidate = _value.Substring(0, start) + text + _value.Substring(end);
            if (candidate == _value || !IsAcceptable(candidate)) return false;

            RecordUndo();
            _value = candidate;
            _caret = start + text.Length;
            _selectionAnchor = _caret;
            return true;
        }

        private bool DeleteBackward(bool whole)
        {
            if (HasSelection) return RemoveSelection();

            if (whole)
            {
                if (_caret <= 0) return false;
                RecordUndo();
                _value = _value.Substring(_caret);
                _caret = 0;
                _selectionAnchor = 0;
                return true;
            }

            if (_caret <= 0) return false;
            RecordUndo();
            _value = _value.Remove(_caret - 1, 1);
            _caret--;
            _selectionAnchor = _caret;
            return true;
        }

        private bool DeleteForward(bool whole)
        {
            if (HasSelection) return RemoveSelection();

            if (whole)
            {
                if (_caret >= _value.Length) return false;
                RecordUndo();
                _value = _value.Substring(0, _caret);
                _selectionAnchor = _caret;
                return true;
            }

            if (_caret >= _value.Length) return false;
            RecordUndo();
            _value = _value.Remove(_caret, 1);
            _selectionAnchor = _caret;
            return true;
        }

        private bool RemoveSelection()
        {
            if (!HasSelection) return false;

            RecordUndo();
            int start = SelectionStart();
            _value = _value.Remove(start, SelectionEnd() - start);
            _caret = start;
            _selectionAnchor = start;
            return true;
        }

        private void CopySelection()
        {
            if (!HasSelection) return;
            GUIUtility.systemCopyBuffer = _value.Substring(SelectionStart(), SelectionEnd() - SelectionStart());
        }

        private bool CutSelection()
        {
            if (!HasSelection) return false;
            CopySelection();
            return RemoveSelection();
        }

        private bool PasteClipboard()
        {
            string buffer = GUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(buffer)) return false;

            int room = MaxLength - (_value.Length - (SelectionEnd() - SelectionStart()));
            if (room <= 0) return false;

            StringBuilder filtered = new StringBuilder(buffer.Length);
            for (int i = 0; i < buffer.Length && filtered.Length < room; i++)
            {
                char c = buffer[i];
                if ((c >= '0' && c <= '9') || c == '.' || c == '-') filtered.Append(c);
            }

            return filtered.Length > 0 && ReplaceSelection(filtered.ToString());
        }

        private void RecordUndo()
        {
            _undo.Add(new EditState { Value = _value, Caret = _caret });
            if (_undo.Count > UndoLimit) _undo.RemoveAt(0);
            _redo.Clear();
        }

        private bool UndoStep(bool redo)
        {
            List<EditState> source = redo ? _redo : _undo;
            List<EditState> target = redo ? _undo : _redo;
            if (source.Count == 0) return false;

            int last = source.Count - 1;
            EditState state = source[last];
            source.RemoveAt(last);
            target.Add(new EditState { Value = _value, Caret = _caret });

            _value = state.Value;
            _caret = Mathf.Clamp(state.Caret, 0, _value.Length);
            _selectionAnchor = _caret;
            return true;
        }

        private void MoveCaret(int direction, bool extend)
        {
            if (!extend && HasSelection)
            {
                MoveCaretTo(direction < 0 ? SelectionStart() : SelectionEnd(), false);
                return;
            }

            MoveCaretTo(_caret + direction, extend);
        }

        private void MoveCaretTo(int index, bool extend)
        {
            _caret = Mathf.Clamp(index, 0, _value.Length);
            if (!extend) _selectionAnchor = _caret;
            MarkCaretActive();
            UpdateScroll();
            SetVerticesDirty();
        }

        private bool Hold(KeyCode key)
        {
            if (Input.GetKeyDown(key))
            {
                _holdKey = key;
                _holdTimer = RepeatDelay;
                return true;
            }

            if (_holdKey != key || !Input.GetKey(key)) return false;

            _holdTimer -= Time.unscaledDeltaTime;
            if (_holdTimer > 0f) return false;

            _holdTimer += RepeatInterval;
            return true;
        }

        private static bool IsControlHeld()
        {
            return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)
                || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
        }

        private static bool IsShiftHeld()
        {
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        }

        private static bool IsAcceptable(string text)
        {
            if (text.Length > MaxLength) return false;

            int dots = 0;
            int minuses = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c >= '0' && c <= '9') continue;
                if (c == '.')
                {
                    if (++dots > 1) return false;
                    continue;
                }
                if (c == '-')
                {
                    if (++minuses > 1 || i != 0) return false;
                    continue;
                }
                return false;
            }

            return true;
        }

        private int SelectionStart() { return Mathf.Min(_caret, _selectionAnchor); }

        private int SelectionEnd() { return Mathf.Max(_caret, _selectionAnchor); }

        private int IndexAt(Vector2 screenPosition)
        {
            Vector2 local;
            if (rectTransform == null
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPosition, null, out local))
                return _caret;

            Rect rect = rectTransform.rect;
            float x = local.x - rect.xMin - TextInset + _scroll;

            int best = 0;
            float bestDistance = float.MaxValue;
            for (int i = 0; i <= _value.Length; i++)
            {
                float distance = Mathf.Abs(CaretOffset(i) - x);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = i;
            }

            return best;
        }

        private float CaretOffset(int index)
        {
            TMP_TextInfo info = _text != null ? _text.textInfo : null;
            if (info == null || info.characterCount == 0) return 0f;

            int count = Mathf.Clamp(index, 0, info.characterCount);
            float offset = 0f;
            for (int i = 0; i < count; i++) offset += info.characterInfo[i].xAdvance;
            return offset;
        }

        private float TextOffsetWidth()
        {
            TMP_TextInfo info = _text != null ? _text.textInfo : null;
            if (info == null || info.characterCount == 0) return 0f;

            float offset = 0f;
            for (int i = 0; i < info.characterCount; i++) offset += info.characterInfo[i].xAdvance;
            return offset;
        }

        private void UpdateScroll()
        {
            if (_text == null) return;

            _textWidth = TextOffsetWidth();
            _caretX = CaretOffset(_caret);
            if (HasSelection)
            {
                _selectionStartX = CaretOffset(SelectionStart());
                _selectionEndX = CaretOffset(SelectionEnd());
            }

            Rect rect = rectTransform != null ? rectTransform.rect : new Rect(0f, 0f, 0f, 0f);
            float available = rect.width - TextInset * 2f;
            float maxScroll = Mathf.Max(0f, _textWidth + CaretWidth - available);
            if (available <= 0f || maxScroll <= 0f)
            {
                _scroll = 0f;
            }
            else
            {
                if (_caretX - _scroll > available) _scroll = _caretX - available;
                else if (_caretX - _scroll < 0f) _scroll = _caretX;
                _scroll = Mathf.Clamp(_scroll, 0f, maxScroll);
            }

            if (_textRect == null || Mathf.Approximately(_scroll, _appliedScroll)) return;

            _appliedScroll = _scroll;
            _textRect.offsetMin = new Vector2(-_scroll, 0f);
            _textRect.offsetMax = new Vector2(-_scroll, 0f);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect = rectTransform != null ? rectTransform.rect : new Rect(0f, 0f, 0f, 0f);
            if (rect.width <= 0f || rect.height <= 0f) return;

            float corner = Mathf.Min(LogRoundedRect.ControlRadius, Mathf.Min(rect.width, rect.height) * 0.5f);
            Color top = _editing ? EditTopColor : (_hover ? HoverTopColor : IdleTopColor);
            Color bottom = _editing ? EditBottomColor : (_hover ? HoverBottomColor : IdleBottomColor);
            LogRoundedRect.DrawFilled(vh, rect, corner, top, bottom, 8, 1f);

            if (_editing)
            {
                LogRoundedRect.DrawBorder(vh, rect, corner, RingThickness, FocusBorderColor, FocusBorderColor, 8, 1f);
            }
            else if (_hover)
            {
                LogRoundedRect.DrawBorder(vh, rect, corner, 1f, HoverBorderColor, HoverBorderColor, 8, 1f);
            }

            if (!_editing) return;

            if (HasSelection)
            {
                float left = Mathf.Clamp(TextInset + _selectionStartX - _scroll, TextInset, rect.width - TextInset);
                float right = Mathf.Clamp(TextInset + _selectionEndX - _scroll, TextInset, rect.width - TextInset);
                if (right > left)
                {
                    Rect selection = new Rect(rect.xMin + left, rect.yMin + rect.height * 0.16f,
                        right - left, rect.height * 0.68f);
                    LogRoundedRect.DrawFilled(vh, selection, Mathf.Min(1.5f, selection.height * 0.3f),
                        SelectionColor, 4);
                }
                return;
            }

            if (!_caretVisible) return;

            float caretX = Mathf.Clamp(TextInset + _caretX - _scroll, TextInset, rect.width - TextInset);
            Rect caret = new Rect(rect.xMin + caretX - CaretWidth * 0.5f, rect.yMin + rect.height * 0.22f,
                CaretWidth, rect.height * 0.56f);
            LogRoundedRect.DrawFilled(vh, caret, CaretWidth * 0.5f, CaretColor, 4);
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
