using System;
using System.Diagnostics;

namespace TimingShow.Diagnostics
{
    public enum PerfPhase
    {
        DataFilter = 0,
        DataBounds,
        DataOutliers,
        DataRobustBounds,
        DataAverage,
        DataRender,
        DrawGrid,
        DrawReference,
        DrawBands,
        DrawAverageLine,
        DrawPoints,
        DrawSeries,
        DrawOutOfRange,
        DrawDetail,
        Count
    }

    public sealed class PerfHistogram
    {
        private const int BucketCount = 40;
        private readonly long[] _buckets = new long[BucketCount];

        public long Count { get; private set; }
        public double Total { get; private set; }
        public double Max { get; private set; }
        public double Min { get; private set; }

        public PerfHistogram()
        {
            Min = double.MaxValue;
        }

        public double Mean
        {
            get { return Count > 0 ? Total / Count : 0.0; }
        }

        public void Add(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0.0) return;

            Count++;
            Total += value;
            if (value > Max) Max = value;
            if (value < Min) Min = value;
            _buckets[BucketIndex(value * 1000.0)]++;
        }

        public void Reset()
        {
            Array.Clear(_buckets, 0, _buckets.Length);
            Count = 0;
            Total = 0.0;
            Max = 0.0;
            Min = double.MaxValue;
        }

        public double Percentile(double q)
        {
            if (Count <= 0) return 0.0;
            if (q <= 0.0) return 0.0;
            if (q > 1.0) q = 1.0;

            long target = (long)Math.Ceiling(Count * q);
            if (target < 1) target = 1;

            long cumulative = 0;
            for (int i = 0; i < BucketCount; i++)
            {
                cumulative += _buckets[i];
                if (cumulative < target) continue;
                if (i == 0) return 0.001;
                return Math.Pow(2.0, i) / 1000.0;
            }
            return Max;
        }

        private static int BucketIndex(double value)
        {
            if (value < 1.0) return 0;

            long bits = BitConverter.DoubleToInt64Bits(value);
            int exponent = (int)((bits >> 52) & 0x7FF) - 1023;
            if (exponent < 0) return 0;

            int index = exponent + 1;
            if (index >= BucketCount) index = BucketCount - 1;
            return index;
        }
    }

    public static class PerfPhases
    {
        public static readonly PerfHistogram[] Stats = CreateStats();
        public static bool Enabled;

        private static PerfHistogram[] CreateStats()
        {
            PerfHistogram[] stats = new PerfHistogram[(int)PerfPhase.Count];
            for (int i = 0; i < stats.Length; i++) stats[i] = new PerfHistogram();
            return stats;
        }

        public static long Mark()
        {
            if (!Enabled) return -1L;
            return Stopwatch.GetTimestamp();
        }

        public static double Since(long mark)
        {
            if (mark < 0L) return 0.0;

            long now = Stopwatch.GetTimestamp();
            if (now <= mark) return 0.0;
            return (now - mark) * 1000.0 / Stopwatch.Frequency;
        }

        public static void Record(PerfPhase phase, double ms)
        {
            if (!Enabled) return;

            int index = (int)phase;
            if (index < 0 || index >= Stats.Length) return;
            Stats[index].Add(ms);
        }

        public static void Reset()
        {
            Enabled = false;
            for (int i = 0; i < Stats.Length; i++) Stats[i].Reset();
        }
    }
}
