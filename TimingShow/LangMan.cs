using System;
using System.Collections.Generic;
using System.IO;

namespace TimingShow
{
    public static class LangMan
    {
        private static Dictionary<string, Dictionary<string, string>> LangData = new Dictionary<string, Dictionary<string, string>>();
        public static IEnumerable<string> AvailableLanguages => LangData.Keys;
        public static void LoadLanguages(string modPath)
        {
            string jsonPath = Path.Combine(modPath, "lang.json");
            try
            {
                if (File.Exists(jsonPath))
                {
                    LangData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(jsonPath, System.Text.Encoding.UTF8));
                    Main.Logger.Log("Languages loaded successfully");
                }
                else
                {
                    Main.Logger.Error("lang.json missing!");
                    LangData = new Dictionary<string, Dictionary<string, string>>();
                }
            }
            catch (Exception ex)
            {
                Main.Logger.Error($"Failed to load lang.json: {ex.Message}");
            }
        }

        public static string T(string key)
        {
            string curLang = Main.Settings?.Language ?? "English";

            if (LangData.TryGetValue(curLang, out var langDict) && langDict.TryGetValue(key, out string text))
            {
                return text;
            }
            if (LangData.TryGetValue("English", out var enDict) && enDict.TryGetValue(key, out string enText))
            {
                return enText;
            }
            return key;
        }
    }
}