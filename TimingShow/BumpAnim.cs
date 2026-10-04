using System;

namespace TimingShow
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
        public static readonly BumpAnim UR = new BumpAnim();
        public static readonly BumpAnim Ratio = new BumpAnim();
        public static readonly BumpAnim Title = new BumpAnim();
        
        public static void OnHit()
        {
            Timing.Trigger();
            UR.Trigger();
            Ratio.Trigger();
            Title.Trigger();
        }
        
        public static void Tick(double deltaMs)
        {
            Timing.Advance(deltaMs);
            UR.Advance(deltaMs);
            Ratio.Advance(deltaMs);
            Title.Advance(deltaMs);
        }

        public static void StopAll()
        {
            Timing.Stop();
            UR.Stop();
            Ratio.Stop();
            Title.Stop();
        }
    }
}
