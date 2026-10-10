using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TimingShow.HUD
{
    
    internal static class LogGraphFont
    {
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
        private static bool _resolved;
        
        internal static TMP_FontAsset Resolve()
        {
            if (_resolved) return _primary;

            _resolved = true;

            TMP_FontAsset latin = FirstAvailable(LatinCandidates);
            TMP_FontAsset cjk = FirstAvailable(CjkCandidates);
            TMP_FontAsset hangul = FirstAvailable(HangulCandidates);

            _primary = latin != null ? latin : (cjk != null ? cjk : hangul);

            if (_primary != null)
            {
                AddFallback(_primary, cjk);
                AddFallback(_primary, hangul);
                AddFallback(_primary, latin);
                ModContext.Logger?.Log("Log graph font: system font '" + _primary.name + "'");
            }
            else
            {
                ModContext.Logger?.Log("Log graph font: no usable system font found");
            }

            return _primary;
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
                    asset = TMP_FontAsset.CreateFontAsset(source);
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
        
        private const int SamplingPointSize = 90;
    }
}
