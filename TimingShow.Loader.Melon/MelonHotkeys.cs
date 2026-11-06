using System;
using MelonLoader;
using UnityEngine;

namespace TimingShow.Melon
{
    public static class MelonHotkeys
    {
        private const string CategoryName = "TimingShow";
        private const string ConfigKeyIdentifier = "ConfigKey";
        private const KeyCode FallbackKey = KeyCode.F9;

        private static MelonPreferences_Entry<string> _configKey;

        public static void Initialize(Settings settings)
        {
            var category = MelonPreferences.CreateCategory(CategoryName, "TimingShow");
            string label = i18n.T("Label_ConfigKey").TrimEnd(' ', ':', '：');
            _configKey = category.CreateEntry(ConfigKeyIdentifier, settings.ConfigKey.ToString(), label, i18n.T("Melon_ConfigKey_Desc"));
            Apply(settings);
            MelonPreferences.Save();
        }

        public static void Apply(Settings settings)
        {
            if (settings == null || _configKey == null) return;

            string raw = _configKey.Value;
            if (!string.IsNullOrWhiteSpace(raw) && Enum.TryParse(raw.Trim(), true, out KeyCode key) && key != KeyCode.None)
            {
                settings.ConfigKey = key;
                return;
            }

            ModContext.Logger?.Error("[TimingShow] Invalid ConfigKey \"" + raw + "\" in MelonPreferences, falling back to " + FallbackKey + ".");
            settings.ConfigKey = FallbackKey;
            _configKey.Value = FallbackKey.ToString();
        }

        public static void SetConfigKey(KeyCode key)
        {
            if (_configKey == null) return;

            _configKey.Value = key.ToString();
            MelonPreferences.Save();
        }
    }
}
