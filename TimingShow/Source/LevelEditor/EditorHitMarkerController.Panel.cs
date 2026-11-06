using System;
using TimingShow.HUD;
using UnityEngine;

namespace TimingShow.LevelEditor
{
    public sealed partial class EditorHitMarkerController
    {
        private HitOverlayPanel _panel;
        private HitOverlayFilter _filter = HitOverlayFilter.CreateDefault();
        private bool[] _searchMask;
        private bool _onlyMatches = true;
        private bool _hasSearchQuery;
        private int _pinnedIndex = -1;
        private int _filteredCount;
        private int _validCount;
        private int _xPerfectCount;
        private int[] _judgeCounts = new int[17];
        private bool _forceRebuild;
        private bool _clearRequested;

        internal EditorHitSnapshotData Snapshot
        {
            get { return _snapshot; }
        }

        internal bool[] ValidMask
        {
            get { return _valid; }
        }

        internal bool IsPreparing
        {
            get { return _phase == Phase.Preparing; }
        }

        internal int ValidCount
        {
            get { return _validCount; }
        }

        internal int FilteredCount
        {
            get { return _filteredCount; }
        }

        internal int XPerfectCount
        {
            get { return _xPerfectCount; }
        }

        internal int PlayerCount
        {
            get { return _snapshot != null ? Mathf.Max(1, _snapshot.PlayerCount) : 1; }
        }

        internal int[] JudgeCounts
        {
            get { return _judgeCounts; }
        }

        internal void RequestRebuild()
        {
            _forceRebuild = true;
        }

        internal void RequestClear()
        {
            _clearRequested = true;
        }

        internal void ApplyPanelState(HitOverlayFilter filter, bool[] searchMask, bool onlyMatches, int pinnedIndex, int queryFlag)
        {
            bool changed = !SameFilter(_filter, filter)
                || !ReferenceEquals(_searchMask, searchMask)
                || _onlyMatches != onlyMatches
                || _pinnedIndex != pinnedIndex
                || _hasSearchQuery != (queryFlag != 0);

            _filter = filter;
            _searchMask = searchMask;
            _onlyMatches = onlyMatches;
            _pinnedIndex = pinnedIndex;
            _hasSearchQuery = queryFlag != 0;

            if (changed)
            {
                RecomputeCounts();
                _forceRebuild = true;
            }
        }

        private static bool SameFilter(HitOverlayFilter a, HitOverlayFilter b)
        {
            return a.JudgeMask == b.JudgeMask
                && a.XpOnly == b.XpOnly
                && Mathf.Approximately(a.MinAbsOffset, b.MinAbsOffset)
                && Mathf.Approximately(a.MaxAbsOffset, b.MaxAbsOffset)
                && a.PlayerMask == b.PlayerMask;
        }

        internal bool PanelContainsMouse(Vector2 screenPoint)
        {
            return _panel != null && _panel.ContainsMouse(screenPoint);
        }

        private void TickPanel()
        {
            if (_panel == null) _panel = new HitOverlayPanel(this);
            _panel.Tick();
        }

        private void DisposePanel()
        {
            if (_panel == null) return;

            _panel.Dispose();
            _panel = null;
        }

        private void BeginCounts()
        {
            _validCount = 0;
            _xPerfectCount = 0;
            _filteredCount = 0;
            _judgeCounts = new int[17];
            _forceRebuild = false;
            _pinnedIndex = -1;
            _searchMask = null;
            _hasSearchQuery = false;
        }

        private void TrackPrepared(TimingScatterSample sample, bool valid)
        {
            if (!valid) return;

            _validCount++;
            if (sample.IsXPerfect) _xPerfectCount++;

            int index = (int)sample.Judge + 1;
            if (index >= 0 && index < _judgeCounts.Length) _judgeCounts[index]++;
        }

        private void RecomputeCounts()
        {
            _filteredCount = 0;
            if (_snapshot == null) return;

            for (int i = 0; i < _snapshot.Samples.Length; i++)
            {
                if (PassesView(i)) _filteredCount++;
            }
        }

        private bool PassesView(int index)
        {
            if (_snapshot == null || _valid == null || index >= _valid.Length || !_valid[index]) return false;
            if (!_filter.Matches(_snapshot.Samples[index])) return false;
            if (_onlyMatches && _searchMask != null && (index >= _searchMask.Length || !_searchMask[index])) return false;

            return true;
        }

        private bool PassesSearch(int index)
        {
            if (_searchMask == null) return true;
            return index < _searchMask.Length && _searchMask[index];
        }

        private static Color MarkerColor(TimingScatterSample sample, Settings settings)
        {
            int mode = settings != null ? settings.EditorHits_ColorMode : 0;
            Color color;
            if (mode == 1) color = HitOverlaySkin.OffsetColor(sample.OffsetMs);
            else if (mode == 2) color = HitOverlaySkin.PlayerColor(sample.PlayerId);
            else color = JColors.GetColor(sample.Judge, sample.IsXPerfect, true);

            float opacity = settings != null ? Mathf.Clamp(settings.EditorHits_MarkerOpacity, 0.1f, 1f) : 1f;
            color.a *= opacity;
            return color;
        }

        private void AppendMarker(Vector3 center, Vector3 right, Vector3 up, float radius, float halfWidth, Color color)
        {
            Settings settings = ModContext.Settings;
            int shape = settings != null ? settings.EditorHits_MarkerShape : 0;

            if (shape == 1)
            {
                AppendDot(center, right, up, radius, color);
                return;
            }

            if (shape == 2)
            {
                AppendBar(center, right, up, radius, halfWidth, color);
                AppendBar(center, up, right, radius, halfWidth, color);
                return;
            }

            AppendDashedCircle(center, right, up, radius, halfWidth, color);
        }

        private void AppendDot(Vector3 center, Vector3 right, Vector3 up, float radius, Color color)
        {
            const int segments = 14;
            float step = Mathf.PI * 2f / segments;
            float outer = radius + Mathf.Max(0.0001f, radius * 0.06f);

            for (int i = 0; i < segments; i++)
            {
                float angle0 = i * step;
                float angle1 = angle0 + step;
                int vertex = _vertices.Count;

                _vertices.Add(center);
                _vertices.Add(center + CircleOffset(right, up, outer, angle0));
                _vertices.Add(center + CircleOffset(right, up, outer, angle1));
                for (int k = 0; k < 3; k++) _colors.Add(color);

                _triangles.Add(vertex);
                _triangles.Add(vertex + 1);
                _triangles.Add(vertex + 2);
            }
        }

        private void AppendBar(Vector3 center, Vector3 axisA, Vector3 axisB, float halfA, float halfB, Color color)
        {
            int vertex = _vertices.Count;
            _vertices.Add(center - axisA * halfA - axisB * halfB);
            _vertices.Add(center - axisA * halfA + axisB * halfB);
            _vertices.Add(center + axisA * halfA + axisB * halfB);
            _vertices.Add(center + axisA * halfA - axisB * halfB);
            for (int i = 0; i < 4; i++) _colors.Add(color);

            _triangles.Add(vertex);
            _triangles.Add(vertex + 1);
            _triangles.Add(vertex + 2);
            _triangles.Add(vertex + 2);
            _triangles.Add(vertex + 3);
            _triangles.Add(vertex);
        }

        private void AppendHighlight(Vector3 center, Vector3 right, Vector3 up, float radius, float halfWidth, Color color)
        {
            const int segments = 24;
            float step = Mathf.PI * 2f / segments;
            float inner = Mathf.Max(0.0001f, radius - halfWidth);
            float outer = radius + halfWidth;

            for (int i = 0; i < segments; i++)
            {
                float angle0 = i * step;
                float angle1 = angle0 + step;
                int vertex = _vertices.Count;

                _vertices.Add(center + CircleOffset(right, up, outer, angle0));
                _vertices.Add(center + CircleOffset(right, up, inner, angle0));
                _vertices.Add(center + CircleOffset(right, up, outer, angle1));
                _vertices.Add(center + CircleOffset(right, up, inner, angle1));
                for (int k = 0; k < 4; k++) _colors.Add(color);

                _triangles.Add(vertex);
                _triangles.Add(vertex + 1);
                _triangles.Add(vertex + 2);
                _triangles.Add(vertex + 2);
                _triangles.Add(vertex + 1);
                _triangles.Add(vertex + 3);
            }
        }
    }
}
