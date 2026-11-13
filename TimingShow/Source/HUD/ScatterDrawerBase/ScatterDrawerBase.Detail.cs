using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TimingShow.HUD.ScatterDrawerBase
{
    public abstract partial class ScatterDrawerBase
    {
        private const float HintShowSeconds = 8f;
        private const float DetailOffset = 14f;
        private const float DetailPanelPadding = 5f;
        private const float DetailAccentWidth = 2f;
        private const float DetailMinHitRadius = 6f;
        private const int DetailLineCount = 4;
        private const float DetailPanelWidth = 190f;
        private const float DetailPanelWideWidth = 210f;
        private const float DetailAnimDuration = 0.12f;
        private const float DetailRiseDistance = 5f;
        private const float DetailCornerRadius = 7f;
        private static readonly Color DetailPanelTopColor = new Color(0.085f, 0.095f, 0.13f, 0.95f);
        private static readonly Color DetailPanelBottomColor = new Color(0.025f, 0.03f, 0.045f, 0.96f);
        private static readonly Color DetailPanelBorderColor = new Color(1f, 1f, 1f, 0.10f);
        private static readonly Color DetailShadowColor = new Color(0f, 0f, 0f, 0.45f);

        private TMP_Text _detailText;
        private TMP_Text _hintText;

        private bool _detailVisible;
        private int _hoverSourceIndex = -1;
        private int _detailShownIndex = -1;
        private bool _detailDetailsShown;
        private int _detailDataVersion = int.MinValue;
        private Vector2 _detailPanelPosition;
        private float _detailPanelWidth;
        private float _detailPanelHeight;
        private float _detailPad = DetailPanelPadding;
        private float _detailAlpha;
        private int _detailFontSize;
        private float _detailLineHeight;
        private int _detailLineCount = DetailLineCount;
        private Color _detailAccentColor = Color.white;
        private float _hintHideTime;

        private void UpdateHintText()
        {
            if (_hintText == null) return;

            bool visible = DetailInteractive && Time.unscaledTime < _hintHideTime;
            if (_hintText.gameObject.activeSelf != visible) _hintText.gameObject.SetActive(visible);
            if (!visible) return;

            Settings settings = ModContext.Settings;
            float scale = Mathf.Max(0.01f, Scale);
            float w = rectTransform.rect.width;
            int fontSize = Mathf.Clamp(Mathf.RoundToInt(10 * scale), 8, 32);
            Color color = ScatterAxisTextColor;
            color.a *= 0.6f;

            string content = i18n.T("Hint_GraphGesture");
            if (_hintText.text != content) _hintText.text = content;
            if (_hintText.fontSize != fontSize) _hintText.fontSize = fontSize;
            if (_hintText.color != color) _hintText.color = color;

            RectTransform rt = _hintText.rectTransform;
            Vector2 position;
            Vector2 pivot;
            Vector2 size = new Vector2(280f * (fontSize / 12f), 16f * (fontSize / 12f));
            if (HintInFooter)
            {
                _hintText.alignment = TextAlignmentOptions.Top;
                position = new Vector2(w * 0.5f, -3f * scale);
                pivot = new Vector2(0.5f, 1f);
                size.x = Mathf.Min(size.x, Mathf.Max(40f, w * 0.8f));
            }
            else
            {
                _hintText.alignment = TextAlignmentOptions.Bottom;
                position = new Vector2(w * 0.5f, 3f * scale);
                pivot = new Vector2(0.5f, 0f);
            }

            if (rt.pivot != pivot) rt.pivot = pivot;
            if (rt.anchoredPosition != position) rt.anchoredPosition = position;
            if (rt.sizeDelta != size) rt.sizeDelta = size;
        }

        private int FindNearestRenderPoint(Vector2 localMouse, float w, float h)
        {
            Settings settings = ModContext.Settings;
            float scale = Mathf.Max(0.01f, Scale);
            float pointRadius = Mathf.Max(0.5f, (ScatterPointSize) * scale * 0.5f);
            float hitRadius = Mathf.Max(pointRadius, DetailMinHitRadius);
            float bestDistanceSqr = hitRadius * hitRadius;
            int best = -1;
            for (int i = 0; i < _renderPoints.Count; i++)
            {
                RenderPoint point = _renderPoints[i];
                if (!TryGetPointLocalPosition(point, w, h, out Vector2 center)) continue;

                float distanceSqr = (center - localMouse).sqrMagnitude;
                if (distanceSqr > bestDistanceSqr) continue;

                bestDistanceSqr = distanceSqr;
                best = point.SourceIndex;
            }

            return best;
        }

        private void RefreshDetailPanel()
        {
            int index = _hoverSourceIndex;
            if (index >= 0 && !IsRenderableSourceIndex(index)) index = -1;

            if (index < 0)
            {
                HideDetailPanel();
                return;
            }

            float scale = Mathf.Max(0.01f, Scale);
            int fontSize = Mathf.Clamp(Mathf.RoundToInt(12 * scale), 8, 32);
            float pad = DetailPanelPadding * (fontSize / 12f);
            bool showDetails = ScatterShowHitDetails;
            float panelWidth = (showDetails ? DetailPanelWideWidth : DetailPanelWidth) * (fontSize / 12f) + pad * 2f;
            float textWidth = panelWidth - pad * 2f;

            if (index != _detailShownIndex || showDetails != _detailDetailsShown || fontSize != _detailFontSize || _detailText.text.Length == 0)
                RebuildDetailText(index, showDetails, fontSize, textWidth);

            float lineHeight = _detailLineHeight > 0f ? _detailLineHeight : fontSize * 1.45f;
            float panelHeight = lineHeight * Mathf.Max(1, _detailLineCount) + pad * 2f + 0.5f;

            Vector2 panelPosition = ComputeDetailPanelPosition(index, panelWidth, panelHeight);
            bool moved = (panelPosition - _detailPanelPosition).sqrMagnitude > 0.25f;
            if (_detailVisible && !moved && _detailPanelWidth == panelWidth && _detailPanelHeight == panelHeight) return;

            _detailPanelPosition = panelPosition;
            _detailPanelWidth = panelWidth;
            _detailPanelHeight = panelHeight;

            _detailPad = pad;

            RectTransform rt = _detailText.rectTransform;
            rt.pivot = Vector2.zero;
            rt.sizeDelta = new Vector2(panelWidth - pad * 2f, panelHeight - pad * 2f);

            bool appearing = _detailAlpha <= 0.001f;
            _detailVisible = true;
            if (!_detailText.gameObject.activeSelf) _detailText.gameObject.SetActive(true);
            if (appearing) _detailAlpha = 0f;
            ApplyDetailVisuals();
            SetVerticesDirty();
        }

        private void HideDetailPanel()
        {
            _detailShownIndex = -1;
            if (!_detailVisible) return;

            _detailVisible = false;
            SetVerticesDirty();
        }

        private void ClearDetailInteraction()
        {
            _hoverSourceIndex = -1;
            _detailShownIndex = -1;
            _detailVisible = false;
            _detailAlpha = 0f;
            if (_detailText != null && _detailText.gameObject.activeSelf) _detailText.gameObject.SetActive(false);
            SetVerticesDirty();
        }

        private bool IsRenderableSourceIndex(int index)
        {
            List<TimingScatterSample> samples = ScatterSamples;
            if (index < 0 || index >= samples.Count || !samples[index].HasJudge) return false;

            for (int i = 0; i < _renderPoints.Count; i++)
                if (_renderPoints[i].SourceIndex == index) return true;

            return false;
        }

        private Vector2 ComputeDetailPanelPosition(int index, float panelWidth, float panelHeight)
        {
            Rect rect = rectTransform.rect;
            Vector2 anchor = new Vector2(DetailOffset, DetailOffset);

            for (int i = 0; i < _renderPoints.Count; i++)
            {
                RenderPoint point = _renderPoints[i];
                if (point.SourceIndex != index) continue;

                if (TryGetPointLocalPosition(point, rect.width, rect.height, out Vector2 pointPosition))
                    anchor = pointPosition;
                break;
            }

            // Keep the panel inside the plot rect; clamping against the screen alone
            // let it run under the graph window frame when the window sits mid-screen.
            float margin = 2f * Mathf.Max(0.01f, Scale);
            float x = anchor.x + DetailOffset;
            float y = anchor.y + DetailOffset;

            if (x + panelWidth > rect.xMax - margin) x = anchor.x - DetailOffset - panelWidth;
            if (y + panelHeight > rect.yMax - margin) y = anchor.y - DetailOffset - panelHeight;

            float minX = rect.xMin + margin;
            float minY = rect.yMin + margin;
            float maxX = Mathf.Max(minX, rect.xMax - panelWidth - margin);
            float maxY = Mathf.Max(minY, rect.yMax - panelHeight - margin);

            return new Vector2(Mathf.Clamp(x, minX, maxX), Mathf.Clamp(y, minY, maxY));
        }

        private void DrawDetailPanel(VertexHelper vh, float scale)
        {
            if (_detailAlpha <= 0.001f) return;

            float w = _detailPanelWidth;
            float h = _detailPanelHeight;
            if (w <= 0f || h <= 0f) return;

            float rise = (1f - _detailAlpha) * DetailRiseDistance * scale;
            Rect rect = new Rect(_detailPanelPosition.x, _detailPanelPosition.y - rise, w, h);
            float radius = DetailCornerRadius * scale;

            LogRoundedRect.DrawShadow(vh, rect, radius, 5f * scale, new Vector2(0f, -1.5f * scale), WithDetailAlpha(DetailShadowColor), 4);
            LogRoundedRect.DrawFilled(vh, rect, radius, WithDetailAlpha(DetailPanelTopColor), WithDetailAlpha(DetailPanelBottomColor), 10, 1f);
            LogRoundedRect.DrawBorder(vh, rect, radius, Mathf.Max(0.5f, scale), WithDetailAlpha(DetailPanelBorderColor), WithDetailAlpha(DetailPanelBorderColor), 10, 1f);

            float accentWidth = Mathf.Max(1.5f, DetailAccentWidth * scale);
            float inset = 2.5f * scale;
            Rect accent = new Rect(rect.xMin + inset, rect.yMin + inset, accentWidth, Mathf.Max(0f, rect.height - inset * 2f));
            LogRoundedRect.DrawFilled(vh, accent, accentWidth * 0.5f, WithDetailAlpha(_detailAccentColor), WithDetailAlpha(_detailAccentColor), 8, 1f);
        }

        private void StepDetailFade()
        {
            float target = _detailVisible ? 1f : 0f;
            if (Mathf.Approximately(_detailAlpha, target)) return;

            _detailAlpha = Mathf.MoveTowards(_detailAlpha, target, Time.unscaledDeltaTime / DetailAnimDuration);
            ApplyDetailVisuals();
            SetVerticesDirty();
        }

        private void ApplyDetailVisuals()
        {
            if (_detailText == null) return;

            _detailText.alpha = _detailAlpha;
            float rise = (1f - _detailAlpha) * DetailRiseDistance * Mathf.Max(0.01f, Scale);
            _detailText.rectTransform.anchoredPosition = new Vector2(_detailPanelPosition.x + _detailPad, _detailPanelPosition.y + _detailPad - rise);
        }

        private void RebuildDetailText(int index, bool showDetails, int fontSize, float textWidth)
        {
            if (!_detailText.gameObject.activeSelf) _detailText.gameObject.SetActive(true);

            _detailShownIndex = index;
            _detailDetailsShown = showDetails;
            _detailFontSize = fontSize;

            _detailText.fontSize = fontSize;
            _detailText.color = ScatterAxisTextColor;
            _detailText.alignment = TextAlignmentOptions.TopLeft;

            string body = BuildDetailText(index);
            _detailText.text = body;
            _detailText.rectTransform.sizeDelta = new Vector2(textWidth, fontSize * 8f);
            _detailText.ForceMeshUpdate();

            int fallbackLines = 1;
            for (int i = 0; i < body.Length; i++) if (body[i] == '\n') fallbackLines++;

            TMP_TextInfo info = _detailText.textInfo;
            _detailLineCount = info != null && info.lineCount > 0 ? info.lineCount : fallbackLines;
            if (info != null && info.lineInfo != null && info.lineInfo.Length > 0 && info.lineInfo[0].lineHeight > 0f)
                _detailLineHeight = info.lineInfo[0].lineHeight;
        }

        private Color WithDetailAlpha(Color color)
        {
            color.a *= _detailAlpha;
            return color;
        }

        private string BuildDetailText(int index)
        {
            TimingScatterSample sample = ScatterSamples[index];
            _detailAccentColor = JColors.GetColor(sample.Judge, sample.IsXPerfect, true);

            string timing = string.Format("{0:+0.00;-0.00;0.00} ms", sample.OffsetMs);
            string time = sample.TimeMs < 0f
                ? i18n.T("Detail_TimeUnavailable")
                : FormatDetailTime(sample.TimeMs) + " (" + sample.TimeMs.ToString() + " ms)";

            string body = string.Format(
                "{0} #{1}\n{2}: {3}\n{4}: {5}\n{6}: <color=#{7}>{8}</color>",
                i18n.T("Detail_Hit"), index + 1,
                i18n.T("Detail_Timing"), timing,
                i18n.T("Detail_Time"), time,
                i18n.T("Detail_Judge"), ColorUtility.ToHtmlStringRGB(_detailAccentColor), FormatDetailJudge(sample));

            if (ScatterPlayerCount > 1)
            {
                body += "\n" + string.Format("{0}: <color=#{1}>{2}</color>",
                    i18n.T("Detail_Player"),
                    ColorUtility.ToHtmlStringRGB(Coop.PlayerColor(sample.PlayerId)),
                    Coop.Label(sample.PlayerId));
            }

            if (ScatterShowHitDetails)
            {
                if (sample.HasFloorId)
                {
                    body += "\n" + i18n.T("Detail_Floor") + ": "
                        + (sample.FloorIdEstimated ? "~" : "") + "#" + sample.FloorId.ToString(CultureInfo.InvariantCulture);
                }

                if (sample.HasAngle)
                {
                    body += "\n" + i18n.T("Detail_Angle") + ": "
                        + sample.AngleDeg.ToString("0.###", CultureInfo.InvariantCulture) + "°";
                }

                if (sample.HasHitParams)
                {
                    body += "\n" + string.Format("BPM: {0} ({1}×{2}×{3})",
                        FormatDetailNumber(sample.Bpm * sample.Speed * sample.Pitch, "0.#"),
                        FormatDetailNumber(sample.Bpm, "0.##"),
                        FormatDetailNumber(sample.Speed, "0.##"),
                        FormatDetailNumber(sample.Pitch, "0.##"));
                }

                if (sample.MarginScale > 0f && (sample.MarginScale < 0.9999f || sample.MarginScale > 1.0001f))
                {
                    body += "\n" + i18n.T("Detail_MarginScale") + ": "
                        + sample.MarginScale.ToString("0.###", CultureInfo.InvariantCulture) + "×";
                }

                if (sample.HasOrbitDirection)
                {
                    body += "\n" + i18n.T("Detail_Orbit") + ": "
                        + i18n.T(sample.OrbitClockwise ? "Detail_OrbitClockwise" : "Detail_OrbitCounterClockwise");
                }
            }

            return body;
        }

        private static string FormatDetailNumber(float value, string format)
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }

        private static string FormatDetailTime(float timeMs)
        {
            int totalMs = Mathf.Max(0, Mathf.RoundToInt(timeMs));
            int minutes = totalMs / 60000;
            int seconds = (totalMs / 1000) % 60;
            int millis = totalMs % 1000;
            return string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, millis);
        }

        private static string FormatDetailJudge(TimingScatterSample sample)
        {
            if (sample.IsXPerfect && !HitMarginCompat.IsGame34 && HitMarginCompat.IsPerfectFamily(sample.Judge))
                return i18n.T("Toggle_XPerfect");

            switch (sample.Judge)
            {
                case HitMan.TooEarly: return i18n.T("Toggle_TooEarly");
                case HitMan.VeryEarly: return i18n.T("Toggle_VeryEarly");
                case HitMan.EarlyPerfect: return i18n.T("Toggle_EarlyPerfect");
                case HitMan.PerfectMinus: return i18n.T(HitMarginCompat.IsNormalPerfect(sample.Judge) ? "Toggle_Perfect" : "Toggle_PerfectMinus");
                case HitMan.XPerfect: return i18n.T("Toggle_XPerfect");
                case HitMan.PerfectPlus: return i18n.T("Toggle_PerfectPlus");
                case HitMan.LatePerfect: return i18n.T("Toggle_LatePerfect");
                case HitMan.VeryLate: return i18n.T("Toggle_VeryLate");
                case HitMan.TooLate: return i18n.T("Toggle_TooLate");
                case HitMan.Multipress: return i18n.T("Toggle_Multipress");
                case HitMan.FailMiss: return i18n.T("Toggle_FailMiss");
                case HitMan.FailOverload: return i18n.T("Toggle_FailOverload");
                case HitMan.OverPress: return i18n.T("Toggle_OverPress");
                case HitMan.Auto: return i18n.T("Toggle_Auto");
                case HitMan.Midspin: return i18n.T("Judge_Midspin");
                case HitMan.FailedFloor: return i18n.T("Judge_FailedFloor");
                default: return i18n.T("Judge_Unknown");
            }
        }
    }
}
