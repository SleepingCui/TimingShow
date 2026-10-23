using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace TimingShow.GeometryTests
{
    internal static class Program
    {
        private const double Eps = 0.01;

        private static int _checks;
        private static int _hardFailures;
        private static int _labelClippedByContainer;
        private static int _labelCases;
        private static int _gridCases;
        private static int _colorCases;
        private static int _colorClampedLabels;
        private static int _chromeCases;
        private static readonly List<string> Failures = new List<string>();
        private static readonly List<string> Clipped = new List<string>();

        private static void Main(string[] args)
        {
            string root = FindRepoRoot();
            string libs = Path.Combine(root, "libs");
            string dll = Path.Combine(root, "TimingShow", "bin", "Release", "TimingShow.dll");

            bool dump = args.Length >= 4 && args[0] == "--dump";

            Console.WriteLine("repo   : " + root);
            Console.WriteLine("libs   : " + libs);
            Console.WriteLine("target : " + dll);

            if (!File.Exists(dll))
            {
                Console.Error.WriteLine("Cannot find " + dll + "; build TimingShow (Release) first.");
                Environment.Exit(2);
            }

            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                string simple = new AssemblyName(e.Name).Name;
                string candidate = Path.Combine(libs, simple + ".dll");
                return File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
            };

            Assembly asm = Assembly.LoadFrom(dll);
            Type window = asm.GetType("TimingShow.HUD.LogGraphWindow.LogGraphWindow", throwOnError: true);

            MethodInfo contentCore = Require(window, "ContentRectCore");
            MethodInfo containerCore = Require(window, "GraphContainerRectCore");
            MethodInfo rectsCore = Require(window, "GraphRectsCore");

            if (window.GetMethod("PlotRect", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public) != null)
            {
                _hardFailures++;
                Failures.Add("LogGraphWindow still exposes a standalone static PlotRect() entry point (two layout implementations exist)");
            }
            if (window.GetMethod("ResolvedPlotRect", BindingFlags.Instance | BindingFlags.NonPublic) == null)
            {
                _hardFailures++;
                Failures.Add("LogGraphWindow is missing ResolvedPlotRect() (the single accessor for this frame's layout result)");
            }
            _checks += 2;

            if (dump)
            {
                DumpCase(contentCore, containerCore, rectsCore,
                    double.Parse(args[1]), double.Parse(args[2]), double.Parse(args[3]),
                    args.Length > 4 ? double.Parse(args[4]) : 64.0,
                    args.Length > 5 ? double.Parse(args[5]) : 0.0);
                return;
            }

            double[] windowWidths = { 480, 600, 900, 1440, 1920, 2560, 700, 480, 1920 };
            double[] windowHeights = { 260, 340, 520, 670, 1080, 1080, 1400, 1080, 260 };
            double[] scales = { 0.75, 1.0, 1.25, 1.5, 2.0, 3.0 };
            double[] labelWidths = { 40, 64, 120, 200 };
            double[] footers = { 0, 20, 40 };

            int cases = 0;

            for (int i = 0; i < windowWidths.Length; i++)
            {
                double w = windowWidths[i];
                double h = windowHeights[i];

                foreach (double scale in scales)
                foreach (double labelWidth in labelWidths)
                foreach (double footer in footers)
                {
                    cases++;
                    RunCase(contentCore, containerCore, rectsCore, w, h, scale, labelWidth, footer);
                }
            }

            RunGridChecks(asm);
            RunColorPanelChecks(asm);
            RunChromeChecks(asm);

            Console.WriteLine();
            Console.WriteLine("cases             : " + cases);
            Console.WriteLine("assertions        : " + _checks);
            Console.WriteLine("hard failures     : " + _hardFailures);
            Console.WriteLine("labels clipped    : " + _labelClippedByContainer + " / " + _labelCases + " (inside the window but outside graphContainer, entering the RectMask2D clip region)");
            Console.WriteLine("grid line ratios  : " + _gridCases + " sets (all inside the open interval (0,1))");
            Console.WriteLine("color item geom   : " + _colorCases + " sets (extreme combinations clamping label width to 1px: " + _colorClampedLabels + ")");
            Console.WriteLine("window chrome     : " + _chromeCases + " window/scale combinations (header, sidebar, stats rows, view toggle, clamp)");

            if (Failures.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Failure details (at most 25):");
                foreach (string line in Failures.Take(25)) Console.WriteLine("  - " + line);
            }

            if (Clipped.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Cases clipped by the container (at most 10, grouped by window size):");
                foreach (string line in Clipped) Console.WriteLine("  - " + line);
            }

            Console.WriteLine();
            Console.WriteLine(_hardFailures == 0 ? "Result: all hard assertions passed." : "Result: hard failures exist, see above.");
            Environment.Exit(_hardFailures == 0 ? 0 : 1);
        }

        private static void RunGridChecks(Assembly asm)
        {
            Type drawer = asm.GetType("TimingShow.HUD.ScatterDrawerBase.ScatterDrawerBase", throwOnError: true);
            MethodInfo fractions = Require(drawer, "GridLineFractions");

            foreach (int divisions in new[] { -5, 0, 1, 2, 3, 4, 5, 10, 20, 50, 100 })
            {
                int expected = Clamp(divisions, 2, 20) - 1;
                float[] values = (float[])fractions.Invoke(null, new object[] { divisions });
                string id = "grid divisions " + divisions;

                _gridCases++;
                Check(values.Length == expected, id + " : grid line count " + values.Length + " != " + expected);

                for (int i = 0; i < values.Length; i++)
                {
                    Check(values[i] > 0f && values[i] < 1f, id + " : grid line ratio out of range " + values[i]);
                    if (i > 0) Check(values[i] > values[i - 1], id + " : grid line ratios not increasing (" + values[i - 1] + " -> " + values[i] + ")");
                }
            }
        }

        private static void RunColorPanelChecks(Assembly asm)
        {
            Type ui = asm.GetType("TimingShow.HUD.LogGraphSettingsUI.LogGraphSettingsUI", throwOnError: true);
            MethodInfo resetRect = Require(ui, "ColorResetRect");
            MethodInfo labelWidth = Require(ui, "ColorChipLabelWidth");
            MethodInfo swatchSize = Require(ui, "ColorSwatchSize");

            double[] scales = { 0.75, 1.0, 1.25, 1.5, 2.0, 3.0 };
            double[] cellWidths = { 80, 140, 240, 400, 560 };
            double[] chipHeights = { 24, 32, 40, 52, 60 };

            Type rectType = resetRect.GetParameters()[0].ParameterType;

            foreach (double scale in scales)
            foreach (double cellWidth in cellWidths)
            foreach (double chipHeight in chipHeights)
            {
                string id = string.Format("color item {0}x{1} UiScale {2}", cellWidth, chipHeight, scale);
                BoxF chip = new BoxF(0, 0, cellWidth, chipHeight);
                object chipRect = MakeRect(rectType, chip);

                BoxF reset = Box(resetRect.Invoke(null, new object[] { chipRect, (float)scale }));
                double swatch = Convert.ToDouble(swatchSize.Invoke(null, new object[] { (float)chipHeight, (float)scale }));
                double text = Convert.ToDouble(labelWidth.Invoke(null, new object[] { chipRect, (float)scale }));

                _colorCases++;
                Check(chip.Contains(reset, Eps), id + " : reset button escapes the swatch " + reset + " / " + chip);
                Check(reset.Width >= 1 - Eps && reset.Height >= 1 - Eps, id + " : reset button size is invalid " + reset);
                Check(reset.Width <= cellWidth + Eps, id + " : reset button is wider than the swatch " + reset);
                Check(swatch >= 6 - Eps && swatch <= chipHeight + Eps, id + " : swatch size is invalid " + swatch);

                if (cellWidth >= 140 && chipHeight >= 32)
                {
                    Check(text >= 1 - Eps, id + " : label text width clamped below 1px " + text);
                }
                else if (text < 1)
                {
                    _colorClampedLabels++;
                }
            }
        }

        private static void RunChromeChecks(Assembly asm)
        {
            Type window = asm.GetType("TimingShow.HUD.LogGraphWindow.LogGraphWindow", throwOnError: true);
            Type settingsType = asm.GetType("TimingShow.Settings", throwOnError: true);
            Type modContext = asm.GetType("TimingShow.ModContext", throwOnError: true);

            FieldInfo settingsField = modContext.GetField("Settings", BindingFlags.Static | BindingFlags.Public);
            FieldInfo scaleField = settingsType.GetField("LogGraph_UIScale", BindingFlags.Instance | BindingFlags.Public);
            object settings = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(settingsType);

            MethodInfo contentCore = Require(window, "ContentRectCore");
            MethodInfo clampSize = Require(window, "ClampSize");
            MethodInfo clampPosition = Require(window, "ClampPosition");
            MethodInfo contentWidth = Require(window, "ContentWidth");
            MethodInfo contentHeight = Require(window, "ContentHeight");
            MethodInfo headerRect = Require(window, "HeaderRect");
            MethodInfo statsColumnWidth = Require(window, "StatsColumnWidth");
            MethodInfo settingsRect = Require(window, "SettingsRect");
            MethodInfo sidebarRect = Require(window, "SidebarRect");
            MethodInfo statsRowMetrics = Require(window, "StatsRowMetrics");
            MethodInfo viewToggleFits = Require(window, "ViewToggleFits");
            MethodInfo viewToggleRects = Require(window, "ViewToggleRects");
            MethodInfo hitViewToggle = Require(window, "HitViewToggle");
            MethodInfo metaReserve = Require(window, "MetaReserveWidth");

            Type vectorType = clampSize.GetParameters()[0].ParameterType;

            double[] scales = { 0.75, 1.0, 1.25, 1.5, 2.0, 3.0 };
            double[] widths = { 480, 600, 900, 1440, 1920, 2560 };
            double[] heights = { 260, 340, 520, 670, 1080, 1080 };
            double[] screenWidths = { 640, 1280, 1920, 3840 };
            double[] screenHeights = { 480, 720, 1080, 2160 };

            foreach (double scale in scales)
            {
                scaleField.SetValue(settings, (float)scale);
                settingsField.SetValue(null, settings);
                double clampedScale = Math.Min(3.0, Math.Max(0.75, scale));

                double contentInset = PropF(window, "ContentInset");
                double headerHeight = PropF(window, "HeaderHeight");
                double statsMinWidth = PropF(window, "StatsMinWidth");
                double statsPanelWidth = PropF(window, "StatsPanelWidth");
                double titleLeft = PropF(window, "TitleLeftPadding");

                Check(Math.Abs(PropF(window, "UiScale") - clampedScale) < 1e-4,
                    "chrome UiScale " + scale + " : UiScale does not reflect LogGraph_UIScale");

                for (int i = 0; i < widths.Length; i++)
                {
                    double w = widths[i];
                    double h = heights[i];
                    string id = string.Format("chrome {0}x{1} UiScale {2}", w, h, scale);
                    _chromeCases++;

                    BoxF windowBox = new BoxF(0, 0, w, h);
                    BoxF content = Box(contentCore.Invoke(null, new object[] { (float)w, (float)h, (float)clampedScale }));

                    double cw = CallF(contentWidth, (float)w);
                    double ch = CallF(contentHeight, (float)h);
                    Check(Math.Abs(cw - content.Width) < Eps, id + " : ContentWidth disagrees with ContentRectCore " + cw + " vs " + content.Width);
                    Check(Math.Abs(ch - content.Height) < Eps, id + " : ContentHeight disagrees with ContentRectCore " + ch + " vs " + content.Height);
                    Check(Math.Abs(cw - Math.Max(0, w - contentInset * 2)) < Eps, id + " : ContentWidth formula drifted " + cw);
                    Check(Math.Abs(ch - Math.Max(0, h - headerHeight - contentInset * 2)) < Eps, id + " : ContentHeight formula drifted " + ch);
                    Check(windowBox.Contains(content, Eps), id + " : content escapes window " + content);

                    BoxF header = Box(headerRect.Invoke(null, new object[] { (float)w, (float)h }));
                    Check(header.X0 >= -Eps && header.X1 <= w + Eps, id + " : headerRect escapes window horizontally " + header);
                    Check(header.Y0 >= -Eps && header.Y1 <= h + Eps, id + " : headerRect escapes window vertically " + header);
                    Check(Math.Abs(header.Height - headerHeight) < Eps, id + " : headerRect height != HeaderHeight " + header);
                    Check(header.Y0 >= content.Y1 - Eps, id + " : headerRect overlaps content " + header + " / " + content);

                    double statsWidth = CallF(statsColumnWidth, (float)w);
                    Check(statsWidth >= statsMinWidth - Eps, id + " : StatsColumnWidth below StatsMinWidth " + statsWidth);
                    Check(statsWidth <= statsPanelWidth + Eps, id + " : StatsColumnWidth above StatsPanelWidth " + statsWidth);
                    double quarter = Math.Max(statsMinWidth, cw * 0.25);
                    Check(statsWidth <= quarter + Eps, id + " : StatsColumnWidth above max(content*0.25, min) " + statsWidth);

                    BoxF sidebar = Box(sidebarRect.Invoke(null, new object[] { (float)w, (float)h }));
                    BoxF settingsBox = Box(settingsRect.Invoke(null, new object[] { (float)w, (float)h }));
                    Check(Math.Abs(sidebar.Width - statsWidth) < Eps, id + " : SidebarRect width != StatsColumnWidth " + sidebar);
                    Check(Math.Abs(sidebar.Height - ch) < Eps, id + " : SidebarRect height != ContentHeight " + sidebar);
                    Check(content.Contains(sidebar, Eps), id + " : SidebarRect escapes content " + sidebar + " / " + content);
                    Check(content.Contains(settingsBox, Eps), id + " : SettingsRect escapes content " + settingsBox + " / " + content);
                    Check(settingsBox.Width >= 1 - Eps, id + " : SettingsRect width invalid " + settingsBox);
                    Check(settingsBox.X1 <= sidebar.X0 + Eps, id + " : SettingsRect overlaps SidebarRect " + settingsBox + " / " + sidebar);

                    object[] metrics = { (float)ch, 0f, 0f, 0f, 0f, 0f };
                    statsRowMetrics.Invoke(null, metrics);
                    double mHeader = Convert.ToDouble(metrics[1]);
                    double mRowsTop = Convert.ToDouble(metrics[2]);
                    double mRow = Convert.ToDouble(metrics[3]);
                    double mLabel = Convert.ToDouble(metrics[4]);
                    double mValue = Convert.ToDouble(metrics[5]);
                    Check(mHeader >= 1 - Eps, id + " : stats header height invalid " + mHeader);
                    Check(Math.Abs(mRowsTop - mHeader) < Eps, id + " : stats rowsTop != headerHeight " + mRowsTop);
                    Check(mRow >= 1 - Eps, id + " : stats row height invalid " + mRow);
                    Check(mLabel >= -Eps && mLabel <= mRow + Eps, id + " : stats label height invalid " + mLabel + " / row " + mRow);
                    Check(mValue >= 1 - Eps, id + " : stats value height invalid " + mValue);
                    Check(mValue >= mRow - mLabel - Eps, id + " : stats value+label shorter than row " + mValue + "+" + mLabel + " / row " + mRow);
                    Check(mRowsTop + 4 * mRow <= ch + Eps, id + " : stats rows overflow content height " + (mRowsTop + 4 * mRow) + " / " + ch);

                    bool fits = CallBool(viewToggleFits, (float)w);
                    object sizeVec = MakeVector(vectorType, w, h);
                    object[] rectsArgs = { sizeVec, null, null };
                    viewToggleRects.Invoke(null, rectsArgs);
                    BoxF toggleScatter = Box(rectsArgs[1]);
                    BoxF toggleDistribution = Box(rectsArgs[2]);
                    bool toggleNonEmpty = toggleScatter.Width > 1;

                    Check(fits == toggleNonEmpty, id + " : ViewToggleFits disagrees with ViewToggleRects (" + fits + " vs " + toggleNonEmpty + ")");

                    if (fits && toggleNonEmpty)
                    {
                        Check(toggleScatter.X0 >= titleLeft - Eps, id + " : view toggle escapes left edge " + toggleScatter);
                        Check(toggleDistribution.X1 <= w + Eps, id + " : view toggle escapes right edge " + toggleDistribution);
                        Check(Math.Abs(toggleDistribution.X0 - toggleScatter.X1) < Eps, id + " : view toggle items are not adjacent");
                        Check(Math.Abs(toggleDistribution.Width - toggleScatter.Width) < Eps, id + " : view toggle item widths differ");
                        Check(toggleScatter.Height > 0 && Math.Abs(toggleScatter.Height - toggleDistribution.Height) < Eps, id + " : view toggle item height invalid");
                        Check(toggleScatter.Y0 >= -Eps && toggleScatter.Y1 <= h + Eps, id + " : view toggle escapes vertically " + toggleScatter);

                        object scatterCenter = MakeVector(vectorType, toggleScatter.X0 + toggleScatter.Width * 0.5, toggleScatter.Y0 + toggleScatter.Height * 0.5);
                        object distributionCenter = MakeVector(vectorType, toggleDistribution.X0 + toggleDistribution.Width * 0.5, toggleDistribution.Y0 + toggleDistribution.Height * 0.5);
                        object outside = MakeVector(vectorType, 1.0, 1.0);

                        object[] hitArgs = { scatterCenter, sizeVec, false };
                        bool hit = Convert.ToBoolean(hitViewToggle.Invoke(null, hitArgs));
                        Check(hit && !Convert.ToBoolean(hitArgs[2]), id + " : scatter toggle center does not hit the scatter item");
                        hitArgs = new object[] { distributionCenter, sizeVec, false };
                        hit = Convert.ToBoolean(hitViewToggle.Invoke(null, hitArgs));
                        Check(hit && Convert.ToBoolean(hitArgs[2]), id + " : distribution toggle center does not hit the distribution item");
                        hitArgs = new object[] { outside, sizeVec, false };
                        hit = Convert.ToBoolean(hitViewToggle.Invoke(null, hitArgs));
                        Check(!hit, id + " : empty corner wrongly hits a view toggle");
                    }

                    foreach (double screenW in screenWidths)
                    foreach (double screenH in screenHeights)
                    foreach (double inW in new[] { 100.0, 480.0, 1920.0, 4000.0 })
                    foreach (double inH in new[] { 100.0, 260.0, 1080.0, 3000.0 })
                    {
                        object clamped = clampSize.Invoke(null, new object[] { MakeVector(vectorType, inW, inH), (float)screenW, (float)screenH });
                        double ox = VecX(clamped);
                        double oy = VecY(clamped);
                        Check(ox >= 480 - Eps, id + " : ClampSize width below minimum " + ox);
                        Check(oy >= 260 - Eps, id + " : ClampSize height below minimum " + oy);
                        Check(ox <= Math.Max(480, Math.Min(1920, screenW)) + Eps, id + " : ClampSize width above maximum " + ox);
                        Check(oy <= Math.Max(260, Math.Min(1080, screenH)) + Eps, id + " : ClampSize height above maximum " + oy);

                        object pos = clampPosition.Invoke(null, new object[]
                        {
                            MakeVector(vectorType, -50, screenH + 50),
                            MakeVector(vectorType, ox, oy),
                            (float)screenW, (float)screenH,
                        });
                        double px = VecX(pos);
                        double py = VecY(pos);
                        Check(px >= -Eps && px <= Math.Max(0, screenW - ox) + Eps, id + " : ClampPosition x out of range " + px);
                        Check(py >= -Eps && py <= Math.Max(0, screenH - oy) + Eps, id + " : ClampPosition y out of range " + py);
                    }

                    Check(Math.Abs(CallF(metaReserve, (float)w)) < 1e-4,
                        id + " : MetaReserveWidth should be 0 without a window instance");
                }

                bool previous = false;
                bool seenFit = false;
                foreach (double probe in new[] { 200.0, 480.0, 600.0, 800.0, 1000.0, 1200.0, 1600.0, 1920.0, 2560.0, 3840.0 })
                {
                    bool current = CallBool(viewToggleFits, (float)probe);
                    Check(!(previous && !current), "chrome UiScale " + scale + " : ViewToggleFits is not monotonic at width " + probe);
                    previous = current;
                    seenFit |= current;
                }
                Check(seenFit, "chrome UiScale " + scale + " : ViewToggleFits never fits even at 3840px");
            }
        }

        private static double PropF(Type type, string name)
        {
            PropertyInfo p = type.GetProperty(name, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (p == null) throw new InvalidOperationException("missing static property " + name);
            return Convert.ToDouble(p.GetValue(null));
        }

        private static double CallF(MethodInfo m, params object[] args) => Convert.ToDouble(m.Invoke(null, args));

        private static bool CallBool(MethodInfo m, params object[] args) => Convert.ToBoolean(m.Invoke(null, args));

        private static object MakeVector(Type vectorType, double x, double y) =>
            Activator.CreateInstance(vectorType, (float)x, (float)y);

        private static double VecX(object v) => VecComponent(v, "x");

        private static double VecY(object v) => VecComponent(v, "y");

        private static double VecComponent(object v, string name)
        {
            Type t = v.GetType();
            PropertyInfo p = t.GetProperty(name);
            if (p != null) return Convert.ToDouble(p.GetValue(v));
            FieldInfo f = t.GetField(name);
            if (f == null) throw new InvalidOperationException("vector has no component " + name);
            return Convert.ToDouble(f.GetValue(v));
        }

        private static object MakeRect(Type rectType, BoxF box)
        {
            return Activator.CreateInstance(rectType, (float)box.X, (float)box.Y, (float)box.W, (float)box.H);
        }

        private static BoxF Call(MethodInfo m, double w, double h, double scale)
        {
            return Box(m.Invoke(null, new object[] { (float)w, (float)h, (float)scale }));
        }

        private static BoxF[] CallRects(MethodInfo m, double w, double h, double labelWidth, double footer, double scale)
        {
            object[] args = { (float)w, (float)h, (float)labelWidth, (float)footer, (float)scale, null, null, null, null, null, null };
            m.Invoke(null, args);
            return new[] { Box(args[5]), Box(args[6]), Box(args[7]), Box(args[8]), Box(args[9]), Box(args[10]) };
        }

        private static MethodInfo Require(Type type, string name)
        {
            MethodInfo m = type.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (m == null) throw new InvalidOperationException("missing static method " + name + " (pure-function layout entry point)");
            return m;
        }

        private static void RunCase(MethodInfo contentCore, MethodInfo containerCore, MethodInfo rectsCore,
            double w, double h, double scale, double labelWidth, double footer)
        {
            string id = string.Format("window {0}x{1} UiScale {2} labelWidth {3} footer {4}", w, h, scale, labelWidth, footer);

            BoxF window = new BoxF(0, 0, w, h);
            BoxF content = Box(contentCore.Invoke(null, new object[] { (float)w, (float)h, (float)scale }));
            BoxF container = Box(containerCore.Invoke(null, new object[] { (float)w, (float)h, (float)scale }));

            object[] args = { (float)w, (float)h, (float)labelWidth, (float)footer, (float)scale, null, null, null, null, null, null };
            rectsCore.Invoke(null, args);
            BoxF outContainer = Box(args[5]);
            BoxF yAxis = Box(args[6]);
            BoxF plot = Box(args[7]);
            BoxF xAxis = Box(args[8]);
            BoxF topGutter = Box(args[9]);
            BoxF rightBand = Box(args[10]);

            Check(window.Contains(content, Eps), id + " : content is not contained by window " + content + " / " + window);
            Check(window.Contains(container, Eps), id + " : container is not contained by window " + container + " / " + window);
            Check(content.Contains(container, Eps), id + " : container is not contained by content " + container + " / " + content);
            Check(!container.Equals(window), id + " : container == window (chart border coincides with window border)");
            Check(outContainer.Equals(container), id + " : GraphRectsCore container differs from GraphContainerRectCore");

            foreach (KeyValuePair<string, BoxF> pair in new[]
            {
                new KeyValuePair<string, BoxF>("yAxis", yAxis),
                new KeyValuePair<string, BoxF>("plot", plot),
                new KeyValuePair<string, BoxF>("xAxis", xAxis),
                new KeyValuePair<string, BoxF>("topGutter", topGutter),
                new KeyValuePair<string, BoxF>("rightBand", rightBand),
            })
            {
                Check(container.Contains(pair.Value, Eps), id + " : " + pair.Key + " is not contained by container " + pair.Value + " / " + container);
                Check(window.Contains(pair.Value, Eps), id + " : " + pair.Key + " is not contained by window " + pair.Value + " / " + window);
            }

            Check(plot.Contains(yAxis, Eps), id + " : yAxis is not inside the plot area " + yAxis + " / " + plot);
            Check(plot.Contains(xAxis, Eps), id + " : xAxis is not inside the plot area " + xAxis + " / " + plot);
            Check(plot.Contains(topGutter, Eps), id + " : topGutter is not inside the plot area " + topGutter + " / " + plot);
            Check(plot.Contains(rightBand, Eps), id + " : rightBand is not inside the plot area " + rightBand + " / " + plot);
            Check(yAxis.X1 <= plot.X0 + plot.Width * 0.5 + Eps, id + " : yAxis crosses the plot mid-line");
            Check(xAxis.Y1 <= plot.Y0 + plot.Height * 0.5 + Eps, id + " : xAxis crosses the plot mid-line");
            Check(topGutter.Y0 >= plot.Y1 - plot.Height * 0.5 - Eps, id + " : topGutter crosses the plot mid-line");
            Check(rightBand.X0 >= plot.X1 - plot.Width * 0.5 - Eps, id + " : rightBand crosses the plot mid-line");
            Check(plot.Width >= 1 - Eps && plot.Height >= 1 - Eps, id + " : plot size is invalid " + plot);
            Check(!plot.Equals(container), id + " : plot == container");
            Check(!plot.Equals(window), id + " : plot == window");


            MirrorLabels(id, window, container, plot, yAxis, xAxis, topGutter, rightBand, scale, labelWidth, footer);
        }

        private static void MirrorLabels(string id, BoxF window, BoxF container, BoxF plot,
            BoxF yAxis, BoxF xAxis, BoxF topGutter, BoxF rightBand, double scale, double measuredY, double footer)
        {
            _labelCases++;

            double gap = 6.0 * Math.Max(0.01, scale);
            double bandLeft = yAxis.Width;
            double bandBottom = xAxis.Height;
            double bandRight = rightBand.Width;

            int yFont = Clamp((int)Math.Round(12 * scale), 8, 32);
            double yBoxWidth = measuredY;
            if (bandLeft > 1) yBoxWidth = Math.Min(yBoxWidth, Math.Max(1, bandLeft - gap));
            double yDefaultHeight = 24.0 * (yFont / 12.0);
            double maxHeight = Math.Max(1, plot.Height);
            double topHeight = Math.Min(yDefaultHeight, maxHeight);
            double bottomHeight = Math.Min(yDefaultHeight, maxHeight);
            double midHeight = Math.Min(yDefaultHeight, maxHeight);
            double topCenter = Math.Max(plot.Y1 - topHeight / 2, plot.Y0 + plot.Height * 0.5);
            double bottomCenter = Math.Min(plot.Y0 + bottomHeight / 2, plot.Y0 + plot.Height * 0.5);

            List<BoxF> labels = new List<BoxF>
            {
                new BoxF(plot.X0 + gap, topCenter - topHeight / 2, yBoxWidth, topHeight),
                new BoxF(plot.X0 + gap, plot.Y0 + plot.Height * 0.5 - midHeight / 2, yBoxWidth, midHeight),
                new BoxF(plot.X0 + gap, bottomCenter - bottomHeight / 2, yBoxWidth, bottomHeight),
            };

            int xFont = Clamp((int)Math.Round(11 * scale), 8, 32);
            double xLabelHeight = 16.0 * (xFont / 12.0);
            double axisY = 3.0 * scale;
            if (bandBottom > 1) axisY = Math.Min(axisY, Math.Max(0, bandBottom - xLabelHeight));
            double rowLeft = bandLeft > 1 ? Math.Min(bandLeft, plot.Width * 0.5) : 2 * scale;
            double rowRight = plot.Width - 2 * scale;
            double xMaxWidth = Math.Max(1, (rowRight - rowLeft) * 0.5);
            double xWidth = Math.Min(measuredY * 2.2, xMaxWidth);
            double xHeight = xLabelHeight;
            if (bandBottom > 1) xHeight = Math.Min(xHeight, Math.Max(1, bandBottom - axisY));
            labels.Add(new BoxF(plot.X0 + rowLeft, plot.Y0 + axisY, xWidth, xHeight));
            labels.Add(new BoxF(plot.X1 - 2 * scale - xWidth, plot.Y0 + axisY, xWidth, xHeight));

            int avgFont = Clamp((int)Math.Round(12 * scale), 8, 32);
            double avgWidth = Math.Max(24.0, measuredY * 1.6);
            double ratioWidth = Math.Max(24.0, plot.Width * 0.28);
            if (avgWidth > ratioWidth) avgWidth = ratioWidth;
            if (bandRight > 1)
            {
                double bandWidth = Math.Max(24.0, bandRight - 6 * scale);
                if (avgWidth > bandWidth) avgWidth = bandWidth;
            }
            avgWidth = Math.Min(avgWidth, Math.Max(1, plot.Width - 6 * scale));
            double avgHeight = Math.Min(24.0 * (avgFont / 12.0), Math.Max(1, plot.Height));
            double avgHalf = avgHeight * 0.5;
            double avgCenter = Math.Min(Math.Max(plot.Y0 + plot.Height * 0.5, plot.Y0 + avgHalf), Math.Max(plot.Y0 + avgHalf, plot.Y1 - avgHalf));
            labels.Add(new BoxF(plot.X1 - 6 * scale - avgWidth, avgCenter - avgHeight / 2, avgWidth, avgHeight));

            LastLabels.Clear();
            LastLabels.AddRange(labels);

            string[] labelNames = { "Y-top", "Y-mid", "Y-bottom", "X-start", "X-end", "Avg" };
            for (int i = 0; i < labels.Count; i++)
            {
                BoxF label = labels[i];
                string labelName = i < labelNames.Length ? labelNames[i] : ("#" + i);
                Check(plot.Contains(label, Eps), id + " : label " + labelName + " escapes the plot area " + label + " / " + plot);
                Check(window.Contains(label, Eps), id + " : label " + labelName + " escapes window " + label + " / " + window);

                if (!container.Contains(label, Eps))
                {
                    _labelClippedByContainer++;
                    if (Clipped.Count < 10) Clipped.Add(id + " label " + labelName + " " + label + " outside container " + container);
                }
            }
        }

        private static readonly List<BoxF> LastLabels = new List<BoxF>();

        private static void DumpCase(MethodInfo contentCore, MethodInfo containerCore, MethodInfo rectsCore,
            double w, double h, double scale, double labelWidth, double footer)
        {
            BoxF content = Call(contentCore, w, h, scale);
            BoxF containerCoreRect = Call(containerCore, w, h, scale);
            BoxF[] rects = CallRects(rectsCore, w, h, labelWidth, footer, scale);
            Console.WriteLine();
            Console.WriteLine("window " + w + "x" + h + "  UiScale " + scale + "  labelWidth " + labelWidth + "  footer " + footer);
            Console.WriteLine("  window           " + new BoxF(0, 0, w, h));
            Console.WriteLine("  contentRect      " + content);
            Console.WriteLine("  container(=core) " + containerCoreRect);
            Console.WriteLine("  graphRects.cont  " + rects[0]);
            Console.WriteLine("  yAxisRect        " + rects[1]);
            Console.WriteLine("  plotRect         " + rects[2]);
            Console.WriteLine("  xAxisRect        " + rects[3]);
            Console.WriteLine("  topGutterRect    " + rects[4]);
            Console.WriteLine("  rightBandRect    " + rects[5]);
            MirrorLabels("dump", new BoxF(0, 0, w, h), rects[0], rects[2], rects[1], rects[3], rects[4], rects[5], scale, labelWidth, footer);
            string[] names = { "Y-top", "Y-mid", "Y-bottom", "X-start", "X-end", "Avg" };
            for (int i = 0; i < LastLabels.Count && i < names.Length; i++)
                Console.WriteLine("  " + names[i].PadRight(9) + "       " + LastLabels[i]);
        }

        private static void Check(bool ok, string message)
        {
            _checks++;
            if (ok) return;
            _hardFailures++;
            if (Failures.Count < 200) Failures.Add(message);
        }

        private static int Clamp(int value, int min, int max) => value < min ? min : (value > max ? max : value);

        private static BoxF Box(object rect)
        {
            if (rect == null) throw new ArgumentNullException(nameof(rect));
            Type t = rect.GetType();
            double x = Convert.ToDouble(t.GetProperty("x").GetValue(rect));
            double y = Convert.ToDouble(t.GetProperty("y").GetValue(rect));
            double w = Convert.ToDouble(t.GetProperty("width").GetValue(rect));
            double h = Convert.ToDouble(t.GetProperty("height").GetValue(rect));
            return new BoxF(x, y, w, h);
        }

        private static string FindRepoRoot()
        {
            string dir = AppContext.BaseDirectory;
            for (int i = 0; i < 10 && !string.IsNullOrEmpty(dir); i++)
            {
                if (Directory.Exists(Path.Combine(dir, "libs")) && Directory.Exists(Path.Combine(dir, "TimingShow"))) return dir;
                dir = Path.GetDirectoryName(dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            }

            throw new InvalidOperationException("Cannot locate the repository root (it must contain libs/ and TimingShow/)");
        }

        private readonly struct BoxF
        {
            public readonly double X;
            public readonly double Y;
            public readonly double W;
            public readonly double H;

            public BoxF(double x, double y, double w, double h)
            {
                X = x;
                Y = y;
                W = w;
                H = h;
            }

            public double X0 => X;
            public double Y0 => Y;
            public double X1 => X + W;
            public double Y1 => Y + H;
            public double Width => W;
            public double Height => H;

            public bool Contains(BoxF other, double eps) =>
                other.X0 >= X0 - eps && other.Y0 >= Y0 - eps && other.X1 <= X1 + eps && other.Y1 <= Y1 + eps;

            public bool Equals(BoxF other) =>
                Math.Abs(X - other.X) < Eps && Math.Abs(Y - other.Y) < Eps &&
                Math.Abs(W - other.W) < Eps && Math.Abs(H - other.H) < Eps;

            public override string ToString() =>
                string.Format("[{0:F2},{1:F2}]-[{2:F2},{3:F2}]", X0, Y0, X1, Y1);
        }
    }
}
