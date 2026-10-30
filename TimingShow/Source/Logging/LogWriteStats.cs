using System;
using TimingShow.Diagnostics;

namespace TimingShow.Logging
{
    public static class LogWriteStats
    {
        public enum FlushReason
        {
            Header = 0,
            Report = 1,
            Close = 2,
            Manual = 3,
            Count = 4
        }

        public static readonly PerfHistogram HitUs = new PerfHistogram();
        public static readonly PerfHistogram FlushMs = new PerfHistogram();
        public static readonly long[] FlushByReason = new long[(int)FlushReason.Count];

        public static long Hits;
        public static long HitBytes;
        public static long WriteErrors;
        public static long FlushCount;
        public static double FlushTotalMs;
        public static double LastFlushMs;
        public static double FlushMaxMs;
        public static long FlushStalls;
        public static DateTime LastFlushUtc;
        public static long LastPersistedBytes;

        public static double CloseFlushMs;
        public static double CloseDisposeMs;
        public static bool CloseRecorded;

        public static double LastFlushAgeSec
        {
            get
            {
                if (LastFlushUtc == DateTime.MinValue) return 0.0;
                double age = (DateTime.UtcNow - LastFlushUtc).TotalSeconds;
                return age > 0.0 ? age : 0.0;
            }
        }

        public static void Reset()
        {
            HitUs.Reset();
            FlushMs.Reset();
            Array.Clear(FlushByReason, 0, FlushByReason.Length);
            Hits = 0;
            HitBytes = 0;
            WriteErrors = 0;
            FlushCount = 0;
            FlushTotalMs = 0.0;
            LastFlushMs = 0.0;
            FlushMaxMs = 0.0;
            FlushStalls = 0;
            LastFlushUtc = DateTime.MinValue;
            LastPersistedBytes = 0;
            CloseFlushMs = 0.0;
            CloseDisposeMs = 0.0;
            CloseRecorded = false;
        }

        public static void RecordHit(double us, int bytes)
        {
            Hits++;
            HitBytes += bytes;
            HitUs.Add(us);
        }

        public static void RecordFlush(FlushReason reason, double ms, long persistedBytes)
        {
            FlushCount++;
            FlushTotalMs += ms;
            LastFlushMs = ms;
            if (ms > FlushMaxMs) FlushMaxMs = ms;
            if (ms > 10.0) FlushStalls++;
            FlushMs.Add(ms);

            int index = (int)reason;
            if (index >= 0 && index < FlushByReason.Length) FlushByReason[index]++;

            LastFlushUtc = DateTime.UtcNow;
            LastPersistedBytes = persistedBytes;
        }

        public static void RecordClose(double flushMs, double disposeMs)
        {
            CloseFlushMs = flushMs;
            CloseDisposeMs = disposeMs;
            CloseRecorded = true;
        }
    }
}
