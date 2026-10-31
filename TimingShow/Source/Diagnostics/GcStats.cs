using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TimingShow.Diagnostics
{
    public static class GcStats
    {
        public struct FrameGc
        {
            public int AllocBytes;
            public int HudAllocBytes;
            public byte Gen0Delta;
        }

        public sealed class ObjectCount
        {
            public string Name = "";
            public int First;
            public int Last;
            public int Max;
        }

        private static readonly List<ObjectCount> ObjectCounts = new List<ObjectCount>();
        private static readonly PerfHistogram AllocKb = new PerfHistogram();
        private static readonly Stopwatch ScanClock = new Stopwatch();

        private static Func<long> _allocProbe;
        private static bool _probeChecked;
        private static long _frameStartBytes;
        private static long _hudEndBytes;
        private static int _gen0Base;
        private static int _gen1Base;
        private static int _gen2Base;
        private static int _lastGen0;
        private static int _scanCount;
        private static double _scanMaxMs;
        private static double _scanLastMs;

        public static int ScanCount
        {
            get { return _scanCount; }
        }

        public static double ScanMaxMs
        {
            get { return _scanMaxMs; }
        }

        public static double ScanLastMs
        {
            get { return _scanLastMs; }
        }

        public static bool Available
        {
            get
            {
                EnsureProbe();
                return _allocProbe != null;
            }
        }

        public static string ProbeName
        {
            get { return _allocProbe != null ? "GC.GetAllocatedBytesForCurrentThread" : "GC.GetTotalMemory (approximate)"; }
        }

        public static long TotalAllocBytes { get; private set; }
        public static long MaxAllocBytes { get; private set; }
        public static int Gen0Collections { get; private set; }
        public static int Gen1Collections { get; private set; }
        public static int Gen2Collections { get; private set; }

        public static PerfHistogram AllocPerFrameKb
        {
            get { return AllocKb; }
        }

        public static List<ObjectCount> Objects
        {
            get { return ObjectCounts; }
        }

        public static void BeginSession()
        {
            EnsureProbe();
            _gen0Base = GC.CollectionCount(0);
            _gen1Base = GC.CollectionCount(1);
            _gen2Base = GC.CollectionCount(2);
            _lastGen0 = _gen0Base;
            CaptureObjects();
        }

        public static void BeginFrame()
        {
            if (!PerfStatsRecorder.IsRecording) return;

            _frameStartBytes = AllocatedBytes();
            _hudEndBytes = _frameStartBytes;
        }

        public static void MarkHudEnd()
        {
            if (!PerfStatsRecorder.IsRecording) return;
            _hudEndBytes = AllocatedBytes();
        }

        public static FrameGc CompleteFrame()
        {
            FrameGc frame = new FrameGc();

            long endBytes = AllocatedBytes();
            long alloc = endBytes - _frameStartBytes;
            long hudAlloc = _hudEndBytes - _frameStartBytes;
            if (alloc < 0) alloc = 0;
            if (hudAlloc < 0) hudAlloc = 0;
            if (hudAlloc > alloc) hudAlloc = alloc;

            frame.AllocBytes = ClampToInt(alloc);
            frame.HudAllocBytes = ClampToInt(hudAlloc);

            int gen0 = GC.CollectionCount(0);
            int delta = gen0 - _lastGen0;
            _lastGen0 = gen0;
            if (delta < 0) delta = 0;
            if (delta > 255) delta = 255;
            frame.Gen0Delta = (byte)delta;

            TotalAllocBytes += alloc;
            if (alloc > MaxAllocBytes) MaxAllocBytes = alloc;
            AllocKb.Add(alloc / 1024.0);

            Gen0Collections = gen0 - _gen0Base;
            Gen1Collections = GC.CollectionCount(1) - _gen1Base;
            Gen2Collections = GC.CollectionCount(2) - _gen2Base;

            return frame;
        }

        public static void CaptureObjects()
        {
            if (!PerfStatsRecorder.IsRecording) return;

            ScanClock.Restart();
            Capture<TMP_Text>("TMP_Text");
            Capture<Graphic>("Graphic");
            Capture<Canvas>("Canvas");
            Capture<Material>("Material");
            ScanClock.Stop();

            _scanCount++;
            _scanLastMs = ScanClock.Elapsed.TotalMilliseconds;
            if (_scanLastMs > _scanMaxMs) _scanMaxMs = _scanLastMs;
            if (_scanLastMs > 50.0)
                ModLog.UnityLog("Perf object scan took " + _scanLastMs.ToString("F1") + " ms");
        }

        public static void Reset()
        {
            ObjectCounts.Clear();
            AllocKb.Reset();
            _frameStartBytes = 0;
            _hudEndBytes = 0;
            _lastGen0 = 0;
            _gen0Base = 0;
            _gen1Base = 0;
            _gen2Base = 0;
            _scanCount = 0;
            _scanMaxMs = 0.0;
            _scanLastMs = 0.0;
            TotalAllocBytes = 0;
            MaxAllocBytes = 0;
            Gen0Collections = 0;
            Gen1Collections = 0;
            Gen2Collections = 0;
        }

        private static void Capture<T>(string name) where T : UnityEngine.Object
        {
            int count;
            try
            {
                count = Resources.FindObjectsOfTypeAll<T>().Length;
            }
            catch
            {
                return;
            }

            ObjectCount entry = null;
            for (int i = 0; i < ObjectCounts.Count; i++)
            {
                if (ObjectCounts[i].Name == name)
                {
                    entry = ObjectCounts[i];
                    break;
                }
            }

            if (entry == null)
            {
                entry = new ObjectCount();
                entry.Name = name;
                entry.First = count;
                ObjectCounts.Add(entry);
            }

            entry.Last = count;
            if (count > entry.Max) entry.Max = count;
        }

        private static void EnsureProbe()
        {
            if (_probeChecked) return;
            _probeChecked = true;

            try
            {
                MethodInfo method = typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
                if (method != null && method.ReturnType == typeof(long))
                    _allocProbe = (Func<long>)Delegate.CreateDelegate(typeof(Func<long>), method);
            }
            catch
            {
                _allocProbe = null;
            }
        }

        private static long AllocatedBytes()
        {
            EnsureProbe();
            if (_allocProbe != null) return _allocProbe();
            return GC.GetTotalMemory(false);
        }

        private static int ClampToInt(long value)
        {
            if (value > int.MaxValue) return int.MaxValue;
            if (value < 0) return 0;
            return (int)value;
        }
    }
}
