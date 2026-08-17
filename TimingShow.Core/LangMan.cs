using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace TimingShow
{
    public static class LangMan
    {
        private static Dictionary<string, Dictionary<string, string>> _langData = new Dictionary<string, Dictionary<string, string>>();
        public static IEnumerable<string> AvailableLanguages => _langData.Keys;
        public static void LoadLanguages(string modPath)
        {
            string jsonPath = Path.Combine(modPath, "lang.json");
            try
            {
                if (File.Exists(jsonPath))
                {
                    _langData = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(jsonPath, System.Text.Encoding.UTF8));
                    ModContext.Logger.Log("Languages loaded successfully");
                }
                else
                {
                    ModContext.Logger.Error("lang.json missing!");
                    _langData = new Dictionary<string, Dictionary<string, string>>();
                }
            }
            catch (Exception e)
            {
                ModContext.Logger.Error($"Failed to load lang.json: {e.Message}");
            }
        }

        public static string T(string key)
        {
            string curLang = ModContext.Settings?.Language ?? "English";

            if (_langData.TryGetValue(curLang, out var langDict) && langDict.TryGetValue(key, out string text))
            {
                return text;
            }
            if (_langData.TryGetValue("English", out var enDict) && enDict.TryGetValue(key, out string enText))
            {
                return enText;
            }
            return key;
        }
    }
}
