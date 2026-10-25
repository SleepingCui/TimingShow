using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace TimingShow.HUD
{

    internal static class LogGraphFont
    {
        private const string BundledFontResource = "TimingShow.Assets.SourceHanSansSC-Regular.otf";
        private const string BundledFontFileName = "SourceHanSansSC-Regular.otf";
        private const string KoreanFontResource = "TimingShow.Assets.GmarketSansMedium.otf";
        private const string KoreanFontFileName = "GmarketSansMedium.otf";
        private const string BundledLicenseResource = "TimingShow.Assets.OFL.txt";
        private const string BundledLicenseFileName = "OFL.txt";
        private const string KoreanLanguage = "한국어";

        private const string CjkProbe = "字";
        private const string HangulProbe = "한";
        private const string LatinProbe = "Aa0";

        private sealed class Candidate
        {
            internal readonly string Family;
            internal readonly string Probe;

            internal Candidate(string family, string probe)
            {
                Family = family;
                Probe = probe;
            }
        }
        
        private static readonly Candidate[] CjkCandidates =
        {
            new Candidate("Microsoft YaHei UI", CjkProbe),
            new Candidate("Microsoft YaHei", CjkProbe),
            new Candidate("SimHei", CjkProbe),
            new Candidate("SimSun", CjkProbe)
        };

        private static readonly Candidate[] HangulCandidates =
        {
            new Candidate("Malgun Gothic", HangulProbe)
        };

        private static readonly Candidate[] LatinCandidates =
        {
            new Candidate("Segoe UI", LatinProbe),
            new Candidate("Arial", LatinProbe),
            new Candidate("Tahoma", LatinProbe),
            new Candidate("Verdana", LatinProbe),
            new Candidate("Liberation Sans", LatinProbe)
        };

        private static readonly Dictionary<string, TMP_FontAsset> Cache = new Dictionary<string, TMP_FontAsset>();
        private static TMP_FontAsset _primary;
        private static Font _bundledSource;
        private static bool _resolved;

        internal static TMP_FontAsset Resolve()
        {
            if (_resolved) return _primary;

            _resolved = true;

            TMP_FontAsset bundled = TryLoadBundledFont();
            TMP_FontAsset latin = FirstAvailable(LatinCandidates);
            TMP_FontAsset cjk = FirstAvailable(CjkCandidates);
            TMP_FontAsset hangul = FirstAvailable(HangulCandidates);

            _primary = bundled != null
                ? bundled
                : (latin != null ? latin : (cjk != null ? cjk : hangul));
            if (_primary != null)
            {
                PreloadSettingsGlyphs(_primary);
                AddFallback(_primary, cjk);
                AddFallback(_primary, hangul);
                AddFallback(_primary, latin);
                ModContext.Logger?.Log("Log graph font: "
                    + (bundled != null ? "bundled '" + bundled.name + "'" : "system font '" + _primary.name + "'"));
            }
            else
            {
                ModContext.Logger?.Log("Log graph font: no usable font found");
            }

            return _primary;
        }

        internal static TMP_FontAsset ResolveSettingsFont()
        {
            return Resolve();
        }

        private static void PreloadSettingsGlyphs(TMP_FontAsset asset)
        {
            if (asset == null) return;

            string[] keys =
            {
                "LogGraph_SettingsTitle", "LogGraph_TabBasic", "LogGraph_TabColors", "LogGraph_Apply",
                "LogGraph_FontHint", "LogGraph_UiScale", "LogGraph_CornerScale", "Toggle_IgnoreOutliers",
                "Toggle_UseHitAxis", "Toggle_UseJudgeColor",
                "Toggle_DistinguishPlayers", "Toggle_ShowZeroLine", "Toggle_ShowAvgLine", "Toggle_ShowGrid",
                "Toggle_ShowJudgeBands", "Toggle_ShowXpBand", "Toggle_CustomFont", "Label_PointSize",
                "Label_MaxRenderPoints", "Label_BandThresholdBpm",
                "Toggle_ShowBorder", "Label_GridColor", "Label_PointColor", "Label_ZeroLineColor", "Label_AvgLineColor",
                "Label_AxisTextColor", "Label_BandPerfectColor", "Label_BandElPerfectColor",
                "Label_BandEarlyLateColor", "Label_BandXpColor", "Btn_ResetColor"
            };

            System.Text.StringBuilder characters = new System.Text.StringBuilder("0123456789.-+%: ");
            for (int i = 0; i < keys.Length; i++) characters.Append(i18n.T(keys[i]));

            if (!asset.TryAddCharacters(characters.ToString(), out string missing)
                || !string.IsNullOrEmpty(missing))
            {
                ModContext.Logger?.Log("Log graph font: settings glyph preload incomplete: " + missing);
            }
        }

        private static TMP_FontAsset TryLoadBundledFont()
        {
            bool korean = IsKoreanUi();
            string fontPath = EnsureBundledAssets(
                korean ? KoreanFontResource : BundledFontResource,
                korean ? KoreanFontFileName : BundledFontFileName);
            if (string.IsNullOrEmpty(fontPath) || !File.Exists(fontPath)) return null;

            try
            {
                Font source = new Font(fontPath);
                if (source == null)
                {
                    ModContext.Logger?.Error("Log graph font: failed to create Font from " + fontPath);
                    return null;
                }

                TMP_FontAsset asset = CreateAsset(source);
                if (asset == null)
                {
                    ModContext.Logger?.Error("Log graph font: TMP_FontAsset.CreateFontAsset failed for bundled font");
                    return null;
                }

                asset.name = korean ? "TimingShow_Bundled_GmarketSans" : "TimingShow_Bundled_SourceHanSansSC";
                asset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                asset.isMultiAtlasTexturesEnabled = true;

                _bundledSource = source;

                string probe = korean ? HangulProbe : (CjkProbe + HangulProbe + LatinProbe);
                if (!asset.TryAddCharacters(probe, out string missing) || !string.IsNullOrEmpty(missing))
                {
                    ModContext.Logger?.Log("Log graph font: bundled font missing probe glyphs '" + probe + "': " + missing);
                }

                ModContext.Logger?.Log("Log graph font: loaded bundled '" + _bundledSource.name
                    + "' (" + asset.atlasWidth + "x" + asset.atlasHeight + ")");
                return asset;
            }
            catch (Exception e)
            {
                ModContext.Logger?.Error("Log graph font: bundled font load failed: " + e.Message);
                return null;
            }
        }

        private static bool IsKoreanUi()
        {
            Settings settings = ModContext.Settings;
            return settings != null && settings.Language == KoreanLanguage;
        }

        private static string EnsureBundledAssets(string fontResource, string fontFileName)
        {
            string assetsDir = CacheDirectory();
            string fontPath = Path.Combine(assetsDir, fontFileName);
            string licensePath = Path.Combine(assetsDir, BundledLicenseFileName);

            try
            {
                Directory.CreateDirectory(assetsDir);
            }
            catch (Exception e)
            {
                ModContext.Logger?.Error("Log graph font: cannot create cache directory: " + e.Message);
                return null;
            }

            ExtractIfChanged(fontResource, fontPath);
            ExtractIfChanged(BundledLicenseResource, licensePath);
            return fontPath;
        }

        private static string CacheDirectory()
        {
            string root = null;
            try
            {
                root = Application.temporaryCachePath;
            }
            catch (Exception)
            {
            }

            if (string.IsNullOrEmpty(root)) root = Path.GetTempPath();
            return Path.Combine(root, "TimingShow");
        }

        private static void ExtractIfChanged(string resourceName, string path)
        {
            try
            {
                Assembly asm = typeof(LogGraphFont).Assembly;
                using (Stream resource = asm.GetManifestResourceStream(resourceName))
                {
                    if (resource == null)
                    {
                        ModContext.Logger?.Error("Log graph font: embedded resource missing: " + resourceName);
                        return;
                    }

                    string tmp = path + ".tmp";
                    using (Stream decoded = new DeflateStream(resource, CompressionMode.Decompress))
                    using (Stream output = File.Create(tmp))
                    {
                        decoded.CopyTo(output);
                    }

                    if (SameContent(tmp, path))
                    {
                        File.Delete(tmp);
                        return;
                    }

                    if (File.Exists(path)) File.Delete(path);
                    File.Move(tmp, path);
                }
            }
            catch (Exception e)
            {
                ModContext.Logger?.Error("Log graph font: failed to extract '" + path + "': " + e.Message);
            }
        }

        private static bool SameContent(string leftPath, string rightPath)
        {
            try
            {
                FileInfo right = new FileInfo(rightPath);
                if (!right.Exists) return false;

                byte[] left = File.ReadAllBytes(leftPath);
                byte[] existing = File.ReadAllBytes(rightPath);
                if (left.Length != existing.Length) return false;

                for (int i = 0; i < left.Length; i++)
                {
                    if (left[i] != existing[i]) return false;
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }


        private static TMP_FontAsset FirstAvailable(Candidate[] candidates)
        {
            for (int i = 0; i < candidates.Length; i++)
            {
                TMP_FontAsset asset = CreateDynamic(candidates[i].Family, candidates[i].Probe);
                if (asset != null) return asset;
            }

            return null;
        }

        private static TMP_FontAsset CreateDynamic(string familyName, string probe)
        {
            if (string.IsNullOrEmpty(familyName)) return null;
            if (Cache.TryGetValue(familyName, out TMP_FontAsset cached)) return cached;

            TMP_FontAsset asset = null;

            try
            {
                Font source = Font.CreateDynamicFontFromOSFont(familyName, SamplingPointSize);
                if (source != null)
                {
                    source.name = familyName;
                    asset = CreateAsset(source);
                }

                if (asset != null)
                {
                    asset.name = "TimingShow_LogGraphFont_" + familyName;
                    asset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                    asset.isMultiAtlasTexturesEnabled = true;

                    if (!string.IsNullOrEmpty(probe) &&
                        (!asset.TryAddCharacters(probe, out string missing) || !string.IsNullOrEmpty(missing)))
                    {
                        if (Application.isPlaying) UnityEngine.Object.Destroy(asset);
                        else UnityEngine.Object.DestroyImmediate(asset);
                        asset = null;
                    }
                }
            }
            catch (Exception e)
            {
                ModContext.Logger?.Log("Log graph font: create '" + familyName + "' failed: " + e.Message);
                asset = null;
            }

            if (asset != null && Application.isPlaying) UnityEngine.Object.DontDestroyOnLoad(asset);

            Cache[familyName] = asset;
            return asset;
        }

        private static void AddFallback(TMP_FontAsset asset, TMP_FontAsset fallback)
        {
            if (asset == null || fallback == null) return;
            if (ReferenceEquals(asset, fallback)) return;

            if (asset.fallbackFontAssetTable == null)
            {
                asset.fallbackFontAssetTable = new List<TMP_FontAsset>();
            }

            if (!asset.fallbackFontAssetTable.Contains(fallback))
            {
                asset.fallbackFontAssetTable.Add(fallback);
            }
        }
        
        private const int SamplingPointSize = 48;
        private const int AtlasPadding = 6;
        private const int AtlasWidth = 2048;
        private const int AtlasHeight = 2048;

        private static TMP_FontAsset CreateAsset(Font source)
        {
            TMP_FontAsset asset = TryCreateAssetWithAtlas(source);
            if (asset != null) return asset;
            return TMP_FontAsset.CreateFontAsset(source);
        }

        private static TMP_FontAsset TryCreateAssetWithAtlas(Font source)
        {
            try
            {
                MethodInfo[] methods = typeof(TMP_FontAsset).GetMethods(BindingFlags.Public | BindingFlags.Static);
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];
                    if (method.Name != "CreateFontAsset") continue;

                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length != 8 || parameters[0].ParameterType != typeof(Font)) continue;
                    
                    object renderMode = Enum.ToObject(parameters[3].ParameterType, 0x1045);
                    object result = method.Invoke(null, new object[]
                    {
                        source, SamplingPointSize, AtlasPadding, renderMode,
                        AtlasWidth, AtlasHeight, AtlasPopulationMode.Dynamic, true
                    });

                    TMP_FontAsset asset = result as TMP_FontAsset;
                    if (asset != null) return asset;
                }
            }
            catch (Exception e)
            {
                ModContext.Logger?.Log("Log graph font: custom atlas creation failed: " + e.Message);
            }

            return null;
        }
    }
}
