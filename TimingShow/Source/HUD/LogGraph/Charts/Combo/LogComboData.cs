using System;
using System.Collections.Generic;

namespace TimingShow.HUD
{
    internal sealed class ComboSegment
    {
        public int Start;
        public int End;
        public int Length;
        public bool IsRun;
    }

    internal sealed class ComboData
    {
        private const int MaxStoredSegments = 200000;

        public List<ComboSegment> Segments = new List<ComboSegment>();
        public int HitCount;
        public int RunCount;
        public int LongestRun;
        public int MaxRunLength;
        public double AverageRun;
        public bool HasData;

        public static ComboData Compute(List<TimingScatterSample> samples, int playerId)
        {
            ComboData data = new ComboData();
            if (samples == null) return data;

            int count = 0;
            for (int i = 0; i < samples.Count; i++)
            {
                TimingScatterSample sample = samples[i];
                if (!sample.HasJudge) continue;
                if (playerId >= 0 && sample.PlayerId != playerId) continue;
                count++;
            }

            data.HitCount = count;
            if (count == 0) return data;

            List<int> runStarts = new List<int>();
            List<int> runEnds = new List<int>();

            int ordinal = 0;
            int runStart = -1;
            for (int i = 0; i < samples.Count; i++)
            {
                TimingScatterSample sample = samples[i];
                if (!sample.HasJudge) continue;
                if (playerId >= 0 && sample.PlayerId != playerId) continue;

                bool keepsStreak = HitMarginCompat.IsPerfectFamily(sample.Judge) || sample.Judge == HitMan.Auto;
                if (keepsStreak)
                {
                    if (runStart < 0) runStart = ordinal;
                }
                else if (runStart >= 0)
                {
                    runStarts.Add(runStart);
                    runEnds.Add(ordinal);
                    runStart = -1;
                }

                ordinal++;
            }

            if (runStart >= 0)
            {
                runStarts.Add(runStart);
                runEnds.Add(count);
            }

            int totalRunLength = 0;
            int cursor = 0;
            for (int i = 0; i < runStarts.Count; i++)
            {
                int start = runStarts[i];
                int end = runEnds[i];
                if (start > cursor) AddSegment(data, cursor, start, false);
                int length = end - start;
                AddSegment(data, start, end, true);
                cursor = end;

                totalRunLength += length;
                if (length > data.LongestRun) data.LongestRun = length;
            }

            if (cursor < count) AddSegment(data, cursor, count, false);

            data.RunCount = runStarts.Count;
            data.MaxRunLength = data.LongestRun;
            data.AverageRun = data.RunCount > 0 ? (double)totalRunLength / data.RunCount : 0.0;
            data.HasData = true;
            return data;
        }

        private static void AddSegment(ComboData data, int start, int end, bool isRun)
        {
            if (data.Segments.Count >= MaxStoredSegments) return;
            data.Segments.Add(CreateSegment(start, end, isRun));
        }

        private static ComboSegment CreateSegment(int start, int end, bool isRun)
        {
            ComboSegment segment = new ComboSegment();
            segment.Start = start;
            segment.End = end;
            segment.Length = end - start;
            segment.IsRun = isRun;
            return segment;
        }
    }
}
