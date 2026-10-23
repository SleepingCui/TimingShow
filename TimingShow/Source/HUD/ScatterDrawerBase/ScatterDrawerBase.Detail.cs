using System.Collections.Generic;
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
        private static readonly Color DetailPanelBgColor = new Color(0f, 0f, 0f, 0.78f);

        private TMP_Text _detailText;
        private TMP_Text _hintText;

        private bool _detailVisible;
        private int _hoverSourceIndex = -1;
        private int _detailShownIndex = -1;
        private int _detailDataVersion = int.MinValue;
        private Vector2 _detailPanelPosition;
        private float _detailPanelWidth;
        private float _detailPanelHeight;
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

            Settings settings = ModContext.Settings;
            float scale = Mathf.Max(0.01f, Scale);
            int fontSize = Mathf.Clamp(Mathf.RoundToInt(12 * scale), 8, 32);
            float lineHeight = 16f * (fontSize / 12f);
            float pad = DetailPanelPadding * (fontSize / 12f);
            float panelWidth = 190f * (fontSize / 12f) + pad * 2f;
            int lineCount = ScatterPlayerCount > 1 ? DetailLineCount + 1 : DetailLineCount;
            float panelHeight = lineHeight * lineCount + pad * 2f;

            _detailText.fontSize = fontSize;
            _detailText.color = ScatterAxisTextColor;
            _detailText.alignment = TextAlignmentOptions.TopLeft;

            if (index != _detailShownIndex)
            {
                _detailShownIndex = index;
                _detailText.text = BuildDetailText(index);
            }

            Vector2 panelPosition = ComputeDetailPanelPosition(index, panelWidth, panelHeight);
            bool moved = (panelPosition - _detailPanelPosition).sqrMagnitude > 0.25f;
            if (_detailVisible && !moved && _detailPanelWidth == panelWidth && _detailPanelHeight == panelHeight) return;

            _detailPanelPosition = panelPosition;
            _detailPanelWidth = panelWidth;
            _detailPanelHeight = panelHeight;

            RectTransform rt = _detailText.rectTransform;
            rt.pivot = Vector2.zero;
            rt.anchoredPosition = new Vector2(panelPosition.x + pad, panelPosition.y + pad);
            rt.sizeDelta = new Vector2(panelWidth - pad * 2f, panelHeight - pad * 2f);

            _detailVisible = true;
            if (!_detailText.gameObject.activeSelf) _detailText.gameObject.SetActive(true);
            SetVerticesDirty();
        }

        private void HideDetailPanel()
        {
            _detailShownIndex = -1;
            if (!_detailVisible) return;

            _detailVisible = false;
            if (_detailText != null && _detailText.gameObject.activeSelf) _detailText.gameObject.SetActive(false);
            SetVerticesDirty();
        }

        private void ClearDetailInteraction()
        {
            _hoverSourceIndex = -1;
            HideDetailPanel();
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
            float w = rectTransform.rect.width;
            float h = rectTransform.rect.height;
            Vector2 panelPosition = new Vector2(DetailOffset, DetailOffset);

            for (int i = 0; i < _renderPoints.Count; i++)
            {
                RenderPoint point = _renderPoints[i];
                if (point.SourceIndex != index) continue;

                if (TryGetPointLocalPosition(point, w, h, out Vector2 pointPosition))
                    panelPosition = new Vector2(pointPosition.x + DetailOffset, pointPosition.y + DetailOffset);
                break;
            }

            float canvasScale = canvas != null ? canvas.scaleFactor : 1f;
            float screenWidth = panelWidth * canvasScale;
            float screenHeight = panelHeight * canvasScale;

            Vector3 corner = RectTransformUtility.WorldToScreenPoint(null, rectTransform.TransformPoint(panelPosition));
            float x = corner.x;
            float y = corner.y;

            if (x + screenWidth > Screen.width) x -= screenWidth + DetailOffset * 2f * canvasScale;
            if (y + screenHeight > Screen.height) y -= screenHeight + DetailOffset * 2f * canvasScale;

            x = Mathf.Clamp(x, 0f, Mathf.Max(0f, Screen.width - screenWidth));
            y = Mathf.Clamp(y, 0f, Mathf.Max(0f, Screen.height - screenHeight));

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, new Vector2(x, y), null, out Vector2 local))
                panelPosition = local;

            return panelPosition;
        }

        private void DrawDetailPanel(VertexHelper vh, float scale)
        {
            if (!_detailVisible) return;

            float w = _detailPanelWidth;
            float h = _detailPanelHeight;
            if (w <= 0f || h <= 0f) return;

            float x = _detailPanelPosition.x;
            float y = _detailPanelPosition.y;

            DrawQuad(vh, new Vector2(x, y), new Vector2(x + w, y + h), DetailPanelBgColor);
            DrawQuad(vh, new Vector2(x, y), new Vector2(x + Mathf.Max(1f, DetailAccentWidth * scale), y + h), _detailAccentColor);
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

            return body;
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
