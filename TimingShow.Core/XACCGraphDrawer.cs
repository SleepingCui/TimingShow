using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimingShow
{
    public class XACCGraphDrawer : GraphDrawerBase
    {
        private readonly List<float> _xaccRenderCache = new List<float>();

        protected override bool IsEnabled => ModContext.IsEnabled && ModContext.IsPlaying;
        protected override bool ShowGraph => ModContext.Settings.ShowXACCGraph;
        protected override float Scale => ModContext.Settings.XACCGraph_Scale;
        protected override float Width => ModContext.Settings.XACCGraph_Width;
        protected override float Height => ModContext.Settings.XACCGraph_Height;
        protected override float PosX => ModContext.Settings.XACCGraph_X;
        protected override float PosY => ModContext.Settings.XACCGraph_Y;

        protected override Color BgColor => ModContext.Settings.XACCGraph_BgColor;
        protected override Color GridColor => ModContext.Settings.XACCGraph_GridColor;
        protected override Color LineColor => ModContext.Settings.XACCGraph_LineColor;

        protected override int MaxPoints => ModContext.Settings.XACCGraph_MaxPoints > 0 ? ModContext.Settings.XACCGraph_MaxPoints : 250;
        public override string GraphName => "XACC";

        private float _cachedMinY;
        private float _cachedMaxY;

        protected override void UpdateData()
        {
            if (ModContext.FullXAccHistory == null || ModContext.FullXAccHistory.Count == 0)
            {
                _xaccRenderCache.Clear();
                return;
            }

            _xaccRenderCache.Clear();
            int total = ModContext.FullXAccHistory.Count;
            int targetPoints = MaxPoints;

            if (ModContext.IsLevelFinished)
            {
                if (total <= targetPoints)
                {
                    _xaccRenderCache.AddRange(ModContext.FullXAccHistory);
                }
                else
                {
                    float step = (float)(total - 1) / (targetPoints - 1);
                    for (int i = 0; i < targetPoints; i++)
                    {
                        int idx = Mathf.Clamp((int)(i * step), 0, total - 1);
                        _xaccRenderCache.Add(ModContext.FullXAccHistory[idx]);
                    }
                }
            }
            else
            {
                int startIdx = Math.Max(0, total - targetPoints);
                for (int i = startIdx; i < total; i++)
                {
                    _xaccRenderCache.Add(ModContext.FullXAccHistory[i]);
                }
            }

            int count = _xaccRenderCache.Count;
            if (count == 0) return;

            if (ModContext.IsLevelFinished)
            {
                _cachedMaxY = 100f;
                float minVal = 100f;
                for (int i = 0; i < count; i++)
                {
                    if (_xaccRenderCache[i] < minVal) minVal = _xaccRenderCache[i];
                }
                _cachedMinY = Mathf.Max(0f, Mathf.Floor(minVal - 1f));
                if (100f - minVal < 1.0f)
                {
                    _cachedMinY = Mathf.Max(0f, (float)Math.Floor(minVal * 10f) / 10f);
                }
            }
            else
            {
                float minVal = 100f;
                float maxVal = 0f;
                for (int i = 0; i < count; i++)
                {
                    float val = _xaccRenderCache[i];
                    if (val < minVal) minVal = val;
                    if (val > maxVal) maxVal = val;
                }

                if (Mathf.Abs(maxVal - minVal) < 0.2f)
                {
                    _cachedMinY = Mathf.Max(0f, minVal - 1.0f);
                    _cachedMaxY = Mathf.Min(100f, maxVal + 1.0f);
                }
                else
                {
                    _cachedMinY = Mathf.Max(0f, minVal - 1.5f);
                    _cachedMaxY = Mathf.Min(100f, maxVal + 1.5f);
                }
            }
        }

        protected override int GetDataCount() => _xaccRenderCache.Count;
        protected override float GetDataValue(int index) => _xaccRenderCache[index];
        protected override float GetMinY() => _cachedMinY;
        protected override float GetMaxY() => _cachedMaxY;
    }
}
