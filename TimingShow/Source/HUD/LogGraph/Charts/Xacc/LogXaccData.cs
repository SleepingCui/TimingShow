using System.Collections.Generic;

namespace TimingShow.HUD
{
    internal sealed class XaccSeries
    {
        public int PlayerId;
        public int HitCount;
        public int ValidCount;
        public float MinValue;
        public float MaxValue;
        public bool HasData;
        public float[] Values;
        public float[] RenderX;
        public float[] RenderY;
    }

    internal sealed class XaccData
    {
        internal const int MaxRenderPointsPerSeries = 2048;

        public XaccSeries[] Series;
        public int PlayerCount;
        public int MaxHitCount;
        public float GlobalMin;
        public float GlobalMax;
        public bool HasData;

        public static XaccData Compute(List<TimingScatterSample> samples, int playerCount)
        {
            XaccData data = new XaccData();
            if (playerCount < 1) playerCount = 1;
            if (playerCount > Players.MaxPlayers) playerCount = Players.MaxPlayers;

            data.PlayerCount = playerCount;
            data.Series = new XaccSeries[playerCount];
            for (int p = 0; p < playerCount; p++)
            {
                XaccSeries series = new XaccSeries();
                series.PlayerId = p;
                data.Series[p] = series;
            }

            if (samples == null || samples.Count == 0) return data;

            int[] hitCounts = new int[playerCount];
            for (int i = 0; i < samples.Count; i++)
            {
                TimingScatterSample sample = samples[i];
                if (!sample.HasJudge) continue;

                int playerId = sample.PlayerId;
                if (playerId < 0 || playerId >= playerCount) playerId = 0;
                hitCounts[playerId]++;
            }

            float[][] rawValues = new float[playerCount][];
            for (int p = 0; p < playerCount; p++)
            {
                XaccSeries series = data.Series[p];
                series.HitCount = hitCounts[p];
                if (hitCounts[p] <= 0) continue;

                float[] values = new float[hitCounts[p]];
                for (int i = 0; i < values.Length; i++) values[i] = float.NaN;
                rawValues[p] = values;
            }

            double[] weightedSums = new double[playerCount];
            int[] countedHits = new int[playerCount];
            int[] ordinals = new int[playerCount];
            bool hasGlobalValue = false;

            for (int i = 0; i < samples.Count; i++)
            {
                TimingScatterSample sample = samples[i];
                if (!sample.HasJudge) continue;

                int playerId = sample.PlayerId;
                if (playerId < 0 || playerId >= playerCount) playerId = 0;

                int ordinal = ordinals[playerId]++;
                float[] values = rawValues[playerId];
                if (values == null || ordinal >= values.Length) continue;

                float? weight = WeightOf(sample.Judge);
                if (!weight.HasValue) continue;

                weightedSums[playerId] += weight.Value;
                countedHits[playerId]++;

                float value = (float)(weightedSums[playerId] / countedHits[playerId] * 100.0);
                values[ordinal] = value;

                XaccSeries series = data.Series[playerId];
                if (!series.HasData)
                {
                    series.HasData = true;
                    series.MinValue = value;
                    series.MaxValue = value;
                }
                else
                {
                    if (value < series.MinValue) series.MinValue = value;
                    if (value > series.MaxValue) series.MaxValue = value;
                }
                series.ValidCount++;

                if (!hasGlobalValue)
                {
                    data.GlobalMin = value;
                    data.GlobalMax = value;
                    hasGlobalValue = true;
                }
                else
                {
                    if (value < data.GlobalMin) data.GlobalMin = value;
                    if (value > data.GlobalMax) data.GlobalMax = value;
                }

                data.HasData = true;
            }

            for (int p = 0; p < playerCount; p++)
            {
                XaccSeries series = data.Series[p];
                if (series.HitCount > data.MaxHitCount) data.MaxHitCount = series.HitCount;
                if (rawValues[p] == null) continue;

                series.Values = rawValues[p];
                BuildRenderPoints(rawValues[p], out series.RenderX, out series.RenderY);
            }

            return data;
        }

        internal static float? WeightOf(HitMan judge)
        {
            switch (judge)
            {
                case HitMan.TooEarly: return 0.2f;
                case HitMan.VeryEarly: return 0.4f;
                case HitMan.EarlyPerfect: return 0.75f;
                case HitMan.PerfectMinus: return 1.0f;
                case HitMan.XPerfect: return 1.0f;
                case HitMan.PerfectPlus: return 1.0f;
                case HitMan.LatePerfect: return 0.75f;
                case HitMan.VeryLate: return 0.4f;
                case HitMan.TooLate: return 0.4f;
                case HitMan.FailMiss: return 0.0f;
                case HitMan.FailOverload: return 0.0f;
                case HitMan.OverPress: return 0.0f;
                case HitMan.FailedFloor: return 0.0f;
                case HitMan.Auto: return 1.0f;
                default: return null;
            }
        }

        private static void BuildRenderPoints(float[] values, out float[] renderX, out float[] renderY)
        {
            int count = values.Length;
            if (count <= MaxRenderPointsPerSeries)
            {
                renderX = new float[count];
                renderY = new float[count];
                for (int i = 0; i < count; i++)
                {
                    renderX[i] = i + 1;
                    renderY[i] = values[i];
                }
                return;
            }

            int bucketCount = MaxRenderPointsPerSeries / 4;
            int bucketSize = (count + bucketCount - 1) / bucketCount;
            List<float> xs = new List<float>(bucketCount * 4);
            List<float> ys = new List<float>(bucketCount * 4);
            int[] candidates = new int[4];
            int[] ordered = new int[4];

            for (int start = 0; start < count; start += bucketSize)
            {
                int end = start + bucketSize;
                if (end > count) end = count;

                int firstIndex = -1;
                int lastIndex = -1;
                int minIndex = -1;
                int maxIndex = -1;
                float minValue = float.MaxValue;
                float maxValue = float.MinValue;

                for (int i = start; i < end; i++)
                {
                    float value = values[i];
                    if (float.IsNaN(value)) continue;

                    if (firstIndex < 0) firstIndex = i;
                    lastIndex = i;
                    if (value < minValue)
                    {
                        minValue = value;
                        minIndex = i;
                    }
                    if (value > maxValue)
                    {
                        maxValue = value;
                        maxIndex = i;
                    }
                }

                if (firstIndex < 0)
                {
                    xs.Add(start + (end - start) * 0.5f + 0.5f);
                    ys.Add(float.NaN);
                    continue;
                }

                candidates[0] = firstIndex;
                candidates[1] = minIndex;
                candidates[2] = maxIndex;
                candidates[3] = lastIndex;

                int orderedCount = 0;
                for (int i = 0; i < candidates.Length; i++)
                {
                    int index = candidates[i];
                    if (index < 0) continue;

                    int insertAt = orderedCount;
                    while (insertAt > 0 && ordered[insertAt - 1] > index)
                    {
                        ordered[insertAt] = ordered[insertAt - 1];
                        insertAt--;
                    }
                    if (insertAt > 0 && ordered[insertAt - 1] == index) continue;
                    if (insertAt < orderedCount && ordered[insertAt] == index) continue;
                    ordered[insertAt] = index;
                    orderedCount++;
                }

                for (int i = 0; i < orderedCount; i++)
                {
                    int index = ordered[i];
                    xs.Add(index + 1);
                    ys.Add(values[index]);
                }
            }

            renderX = xs.ToArray();
            renderY = ys.ToArray();
        }
    }
}
