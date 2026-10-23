using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace TimingShow.Diagnostics
{
    public static class PerfStatsRecorder
    {
        public sealed class DrawerStats
        {
            public string Name = "";
            public long Frames;
            public long UpdateFrames;
            public long TextFrames;
            public long MeshFrames;
            public double UpdateMsSum;
            public double TextMsSum;
            public double MeshMsSum;
            public double MeshMsMax;
            public long VertexSum;
            public int VertexMax;

            public float LastUpdateMs;
            public float LastTextMs;
            public float LastMeshMs;
            public int LastVertices;
            public int LastRenderPoints;
            public int LastUpdateFrameId = -1;
            public int LastTextFrameId = -1;
            public int LastMeshFrameId = -1;
            public int MeshReportedFrameId = -1;
        }

        private struct FrameSample
        {
            public float TimeSec;
            public float FrameMs;
            public float HudMs;
            public int Points;
            public int RenderPoints;
            public float ScatterMeshMs;
            public float ScatterTextMs;
            public float ScatterUpdateMs;
            public int ScatterVerts;
            public bool MeshRan;
            public bool TextRan;
            public bool UpdateRan;
            public float TotalMeshMs;
            public int TotalVerts;
            public int DrawersDrawn;
            public int Hits;
            public float HitMs;
        }

        private const int FrameCap = 400000;
        private const string ScatterDrawerName = "Timing";

        private static readonly int[] BucketUpper = { 50, 100, 150, 200, 300, 400, 600, 800, 1200, 2000, 5000, int.MaxValue };

        private static readonly Stopwatch Clock = new Stopwatch();
        private static readonly List<FrameSample> Samples = new List<FrameSample>(16384);
        private static readonly long[] BucketFrames = new long[BucketUpper.Length];
        private static readonly double[] BucketMs = new double[BucketUpper.Length];
        private static readonly Dictionary<string, DrawerStats> Drawers = new Dictionary<string, DrawerStats>();

        private static bool _active;
        private static int _frameId;
        private static string _levelPath = "";
        private static string _songName = "";
        private static string _startedUtc = "";
        private static long _frames;
        private static double _frameMsSum;
        private static double _hudMsSum;
        private static double _hudMsMax;
        private static int _pointsMax;
        private static int _pointsLast;

        private static readonly Stopwatch HitClock = new Stopwatch();
        private static int _hitDepth;
        private static long _hitCount;
        private static double _hitMsSum;
        private static double _hitMsThisFrame;
        private static int _hitsThisFrame;

        public static bool IsRecording { get { return _active; } }
        public static int CurrentFrameId { get { return _frameId; } }

        public static void HitBegin()
        {
            if (!_active) return;
            if (_hitDepth == 0) HitClock.Restart();
            _hitDepth++;
        }

        public static void HitEnd()
        {
            if (!_active) return;
            if (_hitDepth <= 0) return;
            _hitDepth--;
            if (_hitDepth != 0) return;
            HitClock.Stop();
            double ms = HitClock.Elapsed.TotalMilliseconds;
            _hitCount++;
            _hitMsSum += ms;
            _hitMsThisFrame += ms;
            _hitsThisFrame++;
        }

        public static DrawerStats GetDrawer(string name)
        {
            DrawerStats d;
            if (!Drawers.TryGetValue(name, out d))
            {
                d = new DrawerStats();
                d.Name = name;
                Drawers[name] = d;
            }
            return d;
        }

        public static void StartSession(string levelPath, string songName)
        {
            Reset();
            _active = true;
            _levelPath = levelPath ?? "";
            _songName = songName ?? "";
            _startedUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            Clock.Restart();
        }

        public static int BeginFrame()
        {
            if (!_active) return _frameId;
            _frameId++;
            return _frameId;
        }

        public static void EndFrame(int frameId, float deltaMs, float hudMs)
        {
            if (!_active || frameId != _frameId) return;

            if (deltaMs < 0f) deltaMs = 0f;
            if (hudMs < 0f) hudMs = 0f;

            int points = 0;
            try
            {
                if (ModContext.TimingScatterSamples != null) points = ModContext.TimingScatterSamples.Count;
            }
            catch { }

            _frames++;
            _frameMsSum += deltaMs;
            _hudMsSum += hudMs;
            if (hudMs > _hudMsMax) _hudMsMax = hudMs;
            _pointsLast = points;
            if (points > _pointsMax) _pointsMax = points;

            int b = BucketIndex(points);
            BucketFrames[b]++;
            BucketMs[b] += deltaMs;

            FrameSample s = new FrameSample();
            s.TimeSec = (float)Clock.Elapsed.TotalSeconds;
            s.FrameMs = deltaMs;
            s.HudMs = hudMs;
            s.Points = points;
            
            float meshTotal = 0f;
            int vtxTotal = 0;
            int drawn = 0;
            foreach (KeyValuePair<string, DrawerStats> kv in Drawers)
            {
                DrawerStats d = kv.Value;
                bool updateRan = d.LastUpdateFrameId == frameId;
                bool textRan = d.LastTextFrameId == frameId;
                bool meshRan = (d.LastMeshFrameId == frameId || d.LastMeshFrameId == frameId - 1) &&
                               d.LastMeshFrameId != d.MeshReportedFrameId;
                if (meshRan) d.MeshReportedFrameId = d.LastMeshFrameId;
                if (!updateRan && !textRan && !meshRan) continue;

                d.Frames++;
                if (updateRan) { d.UpdateFrames++; d.UpdateMsSum += d.LastUpdateMs; }
                if (textRan) { d.TextFrames++; d.TextMsSum += d.LastTextMs; }
                if (meshRan)
                {
                    d.MeshFrames++;
                    d.MeshMsSum += d.LastMeshMs;
                    if (d.LastMeshMs > d.MeshMsMax) d.MeshMsMax = d.LastMeshMs;
                    d.VertexSum += d.LastVertices;
                    if (d.LastVertices > d.VertexMax) d.VertexMax = d.LastVertices;
                    meshTotal += d.LastMeshMs;
                    vtxTotal += d.LastVertices;
                }
                drawn++;

                if (kv.Key != ScatterDrawerName) continue;
                s.UpdateRan = updateRan;
                s.TextRan = textRan;
                s.MeshRan = meshRan;
                if (updateRan) s.ScatterUpdateMs = d.LastUpdateMs;
                if (textRan) s.ScatterTextMs = d.LastTextMs;
                if (meshRan)
                {
                    s.ScatterMeshMs = d.LastMeshMs;
                    s.ScatterVerts = d.LastVertices;
                    s.RenderPoints = d.LastRenderPoints;
                }
            }
            s.TotalMeshMs = meshTotal;
            s.TotalVerts = vtxTotal;
            s.DrawersDrawn = drawn;
            s.Hits = _hitsThisFrame;
            s.HitMs = (float)_hitMsThisFrame;
            _hitsThisFrame = 0;
            _hitMsThisFrame = 0.0;

            if (Samples.Count < FrameCap) Samples.Add(s);
        }

        public static void EndSession(string result)
        {
            if (!_active) return;
            _active = false;
            Clock.Stop();

            try
            {
                WriteReport(result ?? "unknown");
            }
            catch (Exception e)
            {
                if (ModContext.Logger != null) ModContext.Logger.Error("PerfLog write failed: " + e.Message);
            }

            Reset();
        }

        private static void Reset()
        {
            _active = false;
            _frameId = 0;
            Samples.Clear();
            Drawers.Clear();
            Array.Clear(BucketFrames, 0, BucketFrames.Length);
            Array.Clear(BucketMs, 0, BucketMs.Length);
            _frames = 0;
            _frameMsSum = 0;
            _hudMsSum = 0;
            _hudMsMax = 0;
            _pointsMax = 0;
            _pointsLast = 0;
            _hitDepth = 0;
            _hitCount = 0;
            _hitMsSum = 0.0;
            _hitMsThisFrame = 0.0;
            _hitsThisFrame = 0;
            _levelPath = "";
            _songName = "";
            _startedUtc = "";
        }

        private static int BucketIndex(int points)
        {
            for (int i = 0; i < BucketUpper.Length; i++)
            {
                if (points <= BucketUpper[i]) return i;
            }
            return BucketUpper.Length - 1;
        }

        private static string BucketLabel(int i)
        {
            if (i == 0) return "0-" + BucketUpper[0];
            if (i >= BucketUpper.Length - 1) return BucketUpper[BucketUpper.Length - 2] + "+";
            return (BucketUpper[i - 1] + 1) + "-" + BucketUpper[i];
        }

        #region report

        private static void WriteReport(string result)
        {
            Settings settings = ModContext.Settings;
            string root = settings != null ? settings.LogDirectory : null;
            if (string.IsNullOrEmpty(root)) return;

            string dir = Path.Combine(root, "PerfLogs");
            Directory.CreateDirectory(dir);

            string safeSong = SanitizeFileName(_songName.Length > 0 ? _songName : "level");
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            string basePath = Path.Combine(dir, "perf_" + safeSong + "_" + stamp);

            UTF8Encoding utf8 = new UTF8Encoding(false);
            File.WriteAllText(basePath + ".txt", BuildTextReport(result), utf8);
            File.WriteAllText(basePath + ".csv", BuildCsv(), utf8);

            if (ModContext.Logger != null) ModContext.Logger.Log("Perf report written: " + basePath + ".txt");
        }

        private static string BuildTextReport(string result)
        {
            StringBuilder sb = new StringBuilder();
            CultureInfo inv = CultureInfo.InvariantCulture;

            Settings settings = ModContext.Settings;

            double durationSec = Clock.Elapsed.TotalSeconds;
            double avgMs = _frames > 0 ? _frameMsSum / _frames : 0.0;
            double avgFps = avgMs > 0.0001 ? 1000.0 / avgMs : 0.0;

            sb.AppendLine("TimingShow Performance Report");
            sb.AppendLine("==============================");
            sb.AppendLine("Song        : " + _songName);
            sb.AppendLine("Level       : " + _levelPath);
            sb.AppendLine("Result      : " + result);
            sb.AppendLine("Started(UTC): " + _startedUtc);
            sb.AppendLine("Duration    : " + durationSec.ToString("F2", inv) + " s");
            sb.AppendLine("Frames      : " + _frames);
            sb.AppendLine();

            sb.AppendLine("--- Frame rate ---");
            sb.AppendLine("Average     : " + avgFps.ToString("F1", inv) + " FPS (" + avgMs.ToString("F2", inv) + " ms)");
            if (Samples.Count > 0)
            {
                float[] sorted = new float[Samples.Count];
                for (int i = 0; i < Samples.Count; i++) sorted[i] = Samples[i].FrameMs;
                Array.Sort(sorted);
                double p50 = Percentile(sorted, 0.50);
                double p95 = Percentile(sorted, 0.95);
                double p99 = Percentile(sorted, 0.99);
                double worst = sorted[sorted.Length - 1];
                double low1 = WorstSlice(sorted, 0.01);
                sb.AppendLine("Median(p50) : " + Fps(p50) + " FPS (" + p50.ToString("F2", inv) + " ms)");
                sb.AppendLine("p95         : " + Fps(p95) + " FPS (" + p95.ToString("F2", inv) + " ms)");
                sb.AppendLine("p99         : " + Fps(p99) + " FPS (" + p99.ToString("F2", inv) + " ms)");
                sb.AppendLine("Worst frame : " + Fps(worst) + " FPS (" + worst.ToString("F2", inv) + " ms)");
                sb.AppendLine("1% low      : " + Fps(low1) + " FPS (" + low1.ToString("F2", inv) + " ms, avg of slowest 1%)");
            }
            sb.AppendLine();

            sb.AppendLine("--- TimingShow CPU cost per frame ---");
            double hudAvg = _frames > 0 ? _hudMsSum / _frames : 0.0;
            double hudPct = avgMs > 0.0001 ? hudAvg / avgMs * 100.0 : 0.0;
            sb.AppendLine("HUDMan.Update avg : " + hudAvg.ToString("F2", inv) + " ms (" + hudPct.ToString("F1", inv) + "% of frame)");
            sb.AppendLine("HUDMan.Update max : " + _hudMsMax.ToString("F2", inv) + " ms");

            // Only frames in which the metric ran contribute to its average; frames that carried a
            // previous reading forward are excluded (N is reported so the sample size is explicit).
            long meshRanFrames = 0, textRanFrames = 0, updRanFrames = 0;
            double meshSum = 0.0, textSum = 0.0, updSum = 0.0;
            double meshMax = 0.0;
            long vtxSum = 0;
            long stableFrames = 0, stableMeshRan = 0;
            double stableFrameSum = 0.0, stableMeshSum = 0.0, stableMeshMax = 0.0;
            long stableVtxSum = 0;
            for (int i = 0; i < Samples.Count; i++)
            {
                FrameSample fs = Samples[i];
                bool stable = fs.TimeSec >= 1.0f;
                if (fs.MeshRan)
                {
                    meshRanFrames++;
                    meshSum += fs.TotalMeshMs;
                    vtxSum += fs.TotalVerts;
                    if (fs.TotalMeshMs > meshMax) meshMax = fs.TotalMeshMs;
                }
                if (fs.TextRan) { textRanFrames++; textSum += fs.ScatterTextMs; }
                if (fs.UpdateRan) { updRanFrames++; updSum += fs.ScatterUpdateMs; }
                if (stable)
                {
                    stableFrames++;
                    stableFrameSum += fs.FrameMs;
                }
                if (stable && fs.MeshRan)
                {
                    stableMeshRan++;
                    stableMeshSum += fs.TotalMeshMs;
                    stableVtxSum += fs.TotalVerts;
                    if (fs.TotalMeshMs > stableMeshMax) stableMeshMax = fs.TotalMeshMs;
                }
            }
            int n = Samples.Count > 0 ? Samples.Count : 1;
            double meshAvg = meshRanFrames > 0 ? meshSum / meshRanFrames : 0.0;
            double meshPct = avgMs > 0.0001 ? meshAvg / avgMs * 100.0 : 0.0;
            sb.AppendLine("Drawer mesh avg   : " + meshAvg.ToString("F3", inv) + " ms (" + meshPct.ToString("F1", inv) + "% of frame), max " + meshMax.ToString("F3", inv) + " ms, ran " + meshRanFrames.ToString(inv) + "/" + n.ToString(inv) + " frames");
            sb.AppendLine("Drawer text avg   : " + (textRanFrames > 0 ? textSum / textRanFrames : 0.0).ToString("F3", inv) + " ms, ran " + textRanFrames.ToString(inv) + "/" + n.ToString(inv) + " frames");
            sb.AppendLine("Drawer update avg : " + (updRanFrames > 0 ? updSum / updRanFrames : 0.0).ToString("F3", inv) + " ms, ran " + updRanFrames.ToString(inv) + "/" + n.ToString(inv) + " frames");
            sb.AppendLine("Drawer verts avg  : " + (meshRanFrames > 0 ? ((double)vtxSum / meshRanFrames) : 0.0).ToString("F0", inv) + " (over ran frames)");
            if (stableFrames > 0)
            {
                double sAvgMs = stableFrameSum / stableFrames;
                sb.AppendLine("Stable t>=1s      : " + Fps(sAvgMs) + " FPS (" + sAvgMs.ToString("F2", inv) + " ms), mesh avg " +
                              (stableMeshRan > 0 ? (stableMeshSum / stableMeshRan) : 0.0).ToString("F3", inv) + " ms over " +
                              stableMeshRan.ToString(inv) + " ran frames, max " + stableMeshMax.ToString("F3", inv) + " ms, verts avg " +
                              (stableMeshRan > 0 ? ((double)stableVtxSum / stableMeshRan) : 0.0).ToString("F0", inv) + ", frames " + stableFrames.ToString(inv));
            }

            double hitAvg = _frames > 0 ? _hitMsSum / _frames : 0.0;
            double hitPct = avgMs > 0.0001 ? hitAvg / avgMs * 100.0 : 0.0;
            sb.AppendLine("Hit handler avg   : " + hitAvg.ToString("F3", inv) + " ms/frame (" + hitPct.ToString("F1", inv) + "% of frame)");
            sb.AppendLine("Hits              : " + _hitCount.ToString(inv) + " total, " + ((double)_hitCount / n).ToString("F2", inv) + " per frame, " + (_hitCount > 0 ? (_hitMsSum / _hitCount) : 0.0).ToString("F4", inv) + " ms each");
            sb.AppendLine();

            sb.AppendLine("--- Frame rate vs scatter point count ---");
            sb.AppendLine("Max points seen : " + _pointsMax);
            sb.AppendLine(Pad("Points", 12) + Pad("Frames", 10) + Pad("AvgFPS", 10) + "AvgMs");
            for (int i = 0; i < BucketUpper.Length; i++)
            {
                if (BucketFrames[i] == 0) continue;
                double bm = BucketMs[i] / BucketFrames[i];
                sb.AppendLine(Pad(BucketLabel(i), 12) + Pad(BucketFrames[i].ToString(inv), 10) + Pad(Fps(bm), 10) + bm.ToString("F2", inv));
            }
            sb.AppendLine();

            // Averages cover only the frames in which that metric actually executed; N is the
            // number of such frames. Frames that merely carried over a previous reading are
            // excluded, so these means are not diluted by idle frames.
            sb.AppendLine("--- Drawer cost vs scatter point count (bucketed by source sample count) ---");
            sb.AppendLine(Pad("Points", 12) + Pad("MeshN", 8) + Pad("MeshAvg", 10) + Pad("MeshP95", 10) +
                           Pad("VtxP50", 9) + Pad("VtxP95", 9) + Pad("VtxMax", 9) +
                           Pad("TextN", 8) + Pad("TextAvg", 10) + Pad("UpdN", 8) + "UpdAvg");
            for (int i = 0; i < BucketUpper.Length; i++)
            {
                List<float> mesh = new List<float>();
                List<float> vtx = new List<float>();
                List<float> text = new List<float>();
                List<float> upd = new List<float>();
                for (int j = 0; j < Samples.Count; j++)
                {
                    FrameSample fs = Samples[j];
                    if (BucketIndex(fs.Points) != i) continue;
                    if (fs.MeshRan) { mesh.Add(fs.TotalMeshMs); vtx.Add(fs.ScatterVerts); }
                    if (fs.TextRan) text.Add(fs.ScatterTextMs);
                    if (fs.UpdateRan) upd.Add(fs.ScatterUpdateMs);
                }
                if (mesh.Count == 0 && text.Count == 0 && upd.Count == 0) continue;

                float[] meshA = SortedCopy(mesh);
                float[] vtxA = SortedCopy(vtx);
                float[] textA = SortedCopy(text);
                float[] updA = SortedCopy(upd);

                sb.AppendLine(
                    Pad(BucketLabel(i), 12) + Pad(meshA.Length.ToString(inv), 8) + Pad(Mean(meshA).ToString("F3", inv), 10) +
                    Pad(Percentile(meshA, 0.95).ToString("F3", inv), 10) +
                    Pad(Percentile(vtxA, 0.50).ToString("F0", inv), 9) + Pad(Percentile(vtxA, 0.95).ToString("F0", inv), 9) +
                    Pad(vtxA.Length > 0 ? vtxA[vtxA.Length - 1].ToString("F0", inv) : "-", 9) +
                    Pad(textA.Length.ToString(inv), 8) + Pad(Mean(textA).ToString("F3", inv), 10) +
                    Pad(updA.Length.ToString(inv), 8) + Mean(updA).ToString("F3", inv));
            }
            sb.AppendLine();

            // Actual drawn-primitive counts, as opposed to the source sample count in 'Points'.
            // Stable-phase figures exclude the first second after recording starts.
            long meshFrames = 0, stableMeshFrames = 0;
            List<float> rpAll = new List<float>();
            List<float> rpStable = new List<float>();
            List<float> vxAll = new List<float>();
            List<float> vxStable = new List<float>();
            for (int j = 0; j < Samples.Count; j++)
            {
                FrameSample fs = Samples[j];
                if (!fs.MeshRan) continue;
                meshFrames++;
                rpAll.Add(fs.RenderPoints);
                vxAll.Add(fs.ScatterVerts);
                if (fs.TimeSec >= 1.0f)
                {
                    stableMeshFrames++;
                    rpStable.Add(fs.RenderPoints);
                    vxStable.Add(fs.ScatterVerts);
                }
            }
            float[] rpAllA = SortedCopy(rpAll);
            float[] vxAllA = SortedCopy(vxAll);
            float[] rpStableA = SortedCopy(rpStable);
            float[] vxStableA = SortedCopy(vxStable);
            sb.AppendLine("--- Scatter rendered geometry (frames where the mesh executed) ---");
            sb.AppendLine("Mesh ran          : " + meshFrames.ToString(inv) + " / " + Samples.Count.ToString(inv) +
                           " frames (" + Pct(meshFrames, Samples.Count) + "%), stable t>=1s: " +
                           stableMeshFrames.ToString(inv) + " (" + Pct(stableMeshFrames, Samples.Count) + "%)");
            AppendStatLine(sb, "render_points/all", rpAllA, inv);
            AppendStatLine(sb, "render_points/1s+", rpStableA, inv);
            AppendStatLine(sb, "vertices/all", vxAllA, inv);
            AppendStatLine(sb, "vertices/1s+", vxStableA, inv);
            sb.AppendLine();

            if (Samples.Count > 0)
            {
                sb.AppendLine("--- Frame time vs note rate (rolling 1s window) ---");
                sb.AppendLine(Pad("Notes/s", 12) + Pad("Frames", 10) + Pad("AvgFPS", 10) + "AvgMs");
                int[] rateUpper = { 8, 16, 24, 32, 48, 64, 96, int.MaxValue };
                long[] rf = new long[rateUpper.Length];
                double[] rm = new double[rateUpper.Length];
                int tail = 0;
                int cum = 0;
                for (int i = 0; i < Samples.Count; i++)
                {
                    cum += Samples[i].Hits;
                    while (tail < i && Samples[i].TimeSec - Samples[tail].TimeSec > 1.0f)
                    {
                        cum -= Samples[tail].Hits;
                        tail++;
                    }
                    double span = Samples[i].TimeSec - Samples[tail].TimeSec;
                    double rate = span > 0.2 ? cum / span : 0.0;
                    int b = 0;
                    while (b < rateUpper.Length - 1 && rate > rateUpper[b]) b++;
                    rf[b]++;
                    rm[b] += Samples[i].FrameMs;
                }
                for (int i = 0; i < rateUpper.Length; i++)
                {
                    if (rf[i] == 0) continue;
                    double avg = rm[i] / rf[i];
                    string label = i == 0
                        ? "0-" + rateUpper[0]
                        : (i >= rateUpper.Length - 1 ? rateUpper[rateUpper.Length - 2] + "+" : (rateUpper[i - 1] + 1) + "-" + rateUpper[i]);
                    sb.AppendLine(Pad(label, 12) + Pad(rf[i].ToString(inv), 10) + Pad(Fps(avg), 10) + avg.ToString("F2", inv));
                }
                sb.AppendLine();
            }

            sb.AppendLine("--- Graph drawers ---");
            sb.AppendLine("(each metric averaged over its own executing frames)");
            sb.AppendLine(Pad("Drawer", 20) + Pad("Frames", 9) + Pad("UpdN", 7) + Pad("UpdAvg", 10) +
                           Pad("TextN", 7) + Pad("TextAvg", 10) + Pad("MeshN", 7) + Pad("MeshAvg", 10) +
                           Pad("MeshMax", 10) + Pad("VtxAvg", 9) + "VtxMax");
            foreach (KeyValuePair<string, DrawerStats> kv in Drawers)
            {
                DrawerStats d = kv.Value;
                if (d.Frames <= 0) continue;
                double u = d.UpdateFrames > 0 ? d.UpdateMsSum / d.UpdateFrames : 0.0;
                double t = d.TextFrames > 0 ? d.TextMsSum / d.TextFrames : 0.0;
                double m = d.MeshFrames > 0 ? d.MeshMsSum / d.MeshFrames : 0.0;
                double v = d.MeshFrames > 0 ? (double)d.VertexSum / d.MeshFrames : 0.0;
                sb.AppendLine(
                    Pad(d.Name, 20) + Pad(d.Frames.ToString(inv), 9) +
                    Pad(d.UpdateFrames.ToString(inv), 7) + Pad(u.ToString("F3", inv), 10) +
                    Pad(d.TextFrames.ToString(inv), 7) + Pad(t.ToString("F3", inv), 10) +
                    Pad(d.MeshFrames.ToString(inv), 7) + Pad(m.ToString("F3", inv), 10) +
                    Pad(d.MeshMsMax.ToString("F3", inv), 10) +
                    Pad(v.ToString("F1", inv), 9) + d.VertexMax.ToString(inv));
            }
            sb.AppendLine();

            sb.AppendLine("--- Settings snapshot ---");
            if (settings != null)
            {
                sb.AppendLine("ShowTimingScatter         : " + settings.ShowTimingScatter);
                sb.AppendLine("AutoScroll                : " + settings.TimingScatter_AutoScroll);
                sb.AppendLine("UseJudgeColor             : " + settings.TimingScatter_UseJudgeColor);
                sb.AppendLine("SampleCount               : " + settings.TimingScatter_SampleCount);
                sb.AppendLine("MaxRenderPoints           : " + settings.TimingScatter_MaxRenderPoints);
                sb.AppendLine("PointSize                 : " + settings.TimingScatter_PointSize.ToString("F2", inv));
                sb.AppendLine("Scale                     : " + settings.TimingScatter_Scale.ToString("F3", inv));
                sb.AppendLine("Width x Height            : " + settings.TimingScatter_Width.ToString("F1", inv) + " x " + settings.TimingScatter_Height.ToString("F1", inv));
                sb.AppendLine("ShowAvgLine               : " + settings.TimingScatter_ShowAvgLine);
                sb.AppendLine("ShowZeroLine              : " + settings.TimingScatter_ShowZeroLine);
                sb.AppendLine("ShowBorder                : " + settings.TimingScatter_ShowBorder);
                sb.AppendLine("ShowJudgeBands            : " + settings.TimingScatter_ShowJudgeBands);
                sb.AppendLine("ShowXpBand                : " + settings.TimingScatter_ShowXpBand);
                sb.AppendLine("IgnoreOutliers            : " + settings.TimingScatter_IgnoreOutliers);
                sb.AppendLine("ShowPerfInfo              : " + settings.TimingScatter_ShowPerfInfo);
                sb.AppendLine("UseHitAxis                : " + settings.TimingScatter_UseHitAxis);
                sb.AppendLine("LogGraph_Enabled          : " + settings.LogGraph_Enabled);
            }

            return sb.ToString();
        }

        private static string BuildCsv()
        {
            StringBuilder sb = new StringBuilder();
            CultureInfo inv = CultureInfo.InvariantCulture;
            // Cost cells are left empty when that metric did not execute on the frame, so a zero
            // is never confused with a carried-over reading; the matching *_ran flag disambiguates.
            sb.AppendLine("time_s,frame_ms,fps,hud_ms,points,scatter_render_points,scatter_mesh_ms,scatter_text_ms,scatter_update_ms,scatter_vtx,scatter_mesh_ran,scatter_text_ran,scatter_update_ran,mesh_total_ms,vtx_total,drawers,hits,hit_ms");
            for (int i = 0; i < Samples.Count; i++)
            {
                FrameSample s = Samples[i];
                double fps = s.FrameMs > 0.0001f ? 1000.0 / s.FrameMs : 0.0;
                sb.Append(s.TimeSec.ToString("F3", inv)).Append(',')
                  .Append(s.FrameMs.ToString("F3", inv)).Append(',')
                  .Append(fps.ToString("F1", inv)).Append(',')
                  .Append(s.HudMs.ToString("F3", inv)).Append(',')
                  .Append(s.Points.ToString(inv)).Append(',')
                  .Append(s.MeshRan ? s.RenderPoints.ToString(inv) : "").Append(',')
                  .Append(s.MeshRan ? s.ScatterMeshMs.ToString("F3", inv) : "").Append(',')
                  .Append(s.TextRan ? s.ScatterTextMs.ToString("F3", inv) : "").Append(',')
                  .Append(s.UpdateRan ? s.ScatterUpdateMs.ToString("F3", inv) : "").Append(',')
                  .Append(s.MeshRan ? s.ScatterVerts.ToString(inv) : "").Append(',')
                  .Append(s.MeshRan ? "1" : "0").Append(',')
                  .Append(s.TextRan ? "1" : "0").Append(',')
                  .Append(s.UpdateRan ? "1" : "0").Append(',')
                  .Append(s.TotalMeshMs.ToString("F3", inv)).Append(',')
                  .Append(s.TotalVerts.ToString(inv)).Append(',')
                  .Append(s.DrawersDrawn.ToString(inv)).Append(',')
                  .Append(s.Hits.ToString(inv)).Append(',')
                  .Append(s.HitMs.ToString("F3", inv)).Append('\n');
            }
            return sb.ToString();
        }

        private static double Mean(float[] values)
        {
            if (values == null || values.Length == 0) return 0.0;
            double sum = 0.0;
            for (int i = 0; i < values.Length; i++) sum += values[i];
            return sum / values.Length;
        }

        private static float[] SortedCopy(List<float> values)
        {
            float[] a = values.ToArray();
            System.Array.Sort(a);
            return a;
        }

        private static string Pct(long part, long total)
        {
            if (total <= 0) return "0.0";
            return (100.0 * part / total).ToString("F1", CultureInfo.InvariantCulture);
        }

        private static void AppendStatLine(StringBuilder sb, string label, float[] sorted, CultureInfo inv)
        {
            if (sorted.Length == 0)
            {
                sb.AppendLine(Pad(label, 18) + "no samples");
                return;
            }
            sb.AppendLine(Pad(label, 18) +
                          "P50 " + Pad(Percentile(sorted, 0.50).ToString("F0", inv), 7) +
                          "P95 " + Pad(Percentile(sorted, 0.95).ToString("F0", inv), 7) +
                          "P99 " + Pad(Percentile(sorted, 0.99).ToString("F0", inv), 7) +
                          "avg " + Pad(Mean(sorted).ToString("F1", inv), 8) +
                          "max " + sorted[sorted.Length - 1].ToString("F0", inv));
        }

        private static double Percentile(float[] sorted, double q)
        {
            if (sorted.Length == 0) return 0.0;
            int idx = (int)Math.Round((sorted.Length - 1) * q);
            if (idx < 0) idx = 0;
            if (idx >= sorted.Length) idx = sorted.Length - 1;
            return sorted[idx];
        }

        private static double WorstSlice(float[] sorted, double fraction)
        {
            if (sorted.Length == 0) return 0.0;
            int n = (int)Math.Ceiling(sorted.Length * fraction);
            if (n < 1) n = 1;
            double sum = 0.0;
            for (int i = sorted.Length - n; i < sorted.Length; i++) sum += sorted[i];
            return sum / n;
        }

        private static string Fps(double ms)
        {
            if (ms <= 0.0001) return "0.0";
            return (1000.0 / ms).ToString("F1", CultureInfo.InvariantCulture);
        }

        private static string Pad(string s, int width)
        {
            if (s.Length >= width) return s + " ";
            return s.PadRight(width);
        }

        private static string SanitizeFileName(string name)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            StringBuilder sb = new StringBuilder(name.Length);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                bool bad = c == ' ';
                if (!bad)
                {
                    for (int j = 0; j < invalid.Length; j++)
                    {
                        if (invalid[j] == c) { bad = true; break; }
                    }
                }
                sb.Append(bad ? '_' : c);
            }
            string result = sb.ToString();
            return result.Length > 0 ? result : "level";
        }

        #endregion
    }
}
