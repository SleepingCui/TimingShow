using UnityEngine;
using UnityEngine.UI;

namespace TimingShow.HUD.LogGraphWindow
{
    internal sealed class LogGraphPlotFrame : MaskableGraphic
    {
        private const float ShadeHeightBase = 20f;
        private const float ShadeAlpha = 0.30f;

        private static float UiScale
        {
            get
            {
                Settings settings = ModContext.Settings;
                return settings != null ? Mathf.Clamp(settings.LogGraph_UIScale, 0.75f, 3f) : 1.25f;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= 2f || rect.height <= 2f) return;

            float shadeHeight = Mathf.Min(rect.height * 0.45f, ShadeHeightBase * UiScale);
            LogRoundedRect.DrawFilled(vh, new Rect(rect.xMin, rect.yMax - shadeHeight, rect.width, shadeHeight),
                0f, new Color(0f, 0f, 0f, ShadeAlpha), new Color(0f, 0f, 0f, 0f), 2, 0f);

            LogRoundedRect.DrawBorder(vh, new Rect(rect.xMin + 0.5f, rect.yMin + 0.5f, rect.width - 1f, rect.height - 1f),
                0f, 1f, LogGraphTheme.HairStrong);
        }
    }
}
