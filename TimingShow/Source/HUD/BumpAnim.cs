using System;

namespace TimingShow.HUD
{

    public sealed class BumpAnim
    {
        public const double DurationMs = 500.0;
        public const float PeakRatio = 0.28f;
        
        private static readonly float[] _expTable = BuildTable();
        private double _elapsedMs = DurationMs;
        public bool IsAnimating => _elapsedMs < DurationMs;

    
        public float Ease
        {
            get
            {
                if (_elapsedMs >= DurationMs) return 0f;
                int idx = (int)(_elapsedMs / DurationMs * 30.0 + 0.5);
                return idx >= _expTable.Length ? 0f : _expTable[idx];
            }
        }

        public void Trigger()
        {
            _elapsedMs = 0.0;
        }

        public void Stop()
        {
            _elapsedMs = DurationMs;
        }

        public void Advance(double deltaMs)
        {
            if (_elapsedMs >= DurationMs) return;
            _elapsedMs += deltaMs;
            if (_elapsedMs > DurationMs) _elapsedMs = DurationMs;
        }
        
        public static int Scale(int baseSize, float ease)
        {
            if (ease <= 0f || baseSize <= 0) return baseSize;
            int extra = (int)(baseSize * PeakRatio * ease + 0.5f);
            return extra <= 0 ? baseSize : baseSize + extra;
        }

        private static float[] BuildTable()
        {
            var table = new float[31];
            for (int i = 0; i < table.Length; i++)
            {
                float p = i / 30f;
                table[i] = p >= 1f ? 0f : (float)Math.Pow(2.0, -10.0 * p);
            }
            return table;
        }
    }
    
    public static class HitBump
    {
        public static readonly BumpAnim Timing = new BumpAnim();
        public static readonly BumpAnim Avg = new BumpAnim();
        public static readonly BumpAnim UR = new BumpAnim();
        public static readonly BumpAnim Ratio = new BumpAnim();
        public static readonly BumpAnim Title = new BumpAnim();
        
        public static void Tick(double deltaMs)
        {
            Timing.Advance(deltaMs);
            Avg.Advance(deltaMs);
            UR.Advance(deltaMs);
            Ratio.Advance(deltaMs);
            Title.Advance(deltaMs);
        }

        public static void StopAll()
        {
            Timing.Stop();
            Avg.Stop();
            UR.Stop();
            Ratio.Stop();
            Title.Stop();
        }
    }
    
    public static class BumpTrigger
    {
        private static string _timing;
        private static string _avg;
        private static string _ur;
        private static string _ratio;
        private static string _title;

        public static void Timing(string value) => Fire(HitBump.Timing, ref _timing, value);

        public static void Avg(string value) => Fire(HitBump.Avg, ref _avg, value);

        public static void UR(string value) => Fire(HitBump.UR, ref _ur, value);

        public static void Ratio(string value) => Fire(HitBump.Ratio, ref _ratio, value);

        public static void Title(string value) => Fire(HitBump.Title, ref _title, value);

        public static void Reset()
        {
            _timing = null;
            _avg = null;
            _ur = null;
            _ratio = null;
            _title = null;
        }

        private static void Fire(BumpAnim anim, ref string last, string value)
        {
            if (value == null || value == last) return;
            last = value;
            anim.Trigger();
        }
    }
}
