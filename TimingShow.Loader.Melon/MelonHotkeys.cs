using MelonLoader;
using UnityEngine;

namespace TimingShow.Melon
{
    public static class MelonHotkeys
    {
        private const string CategoryName = "TimingShow";
        private const string ConfigKeyIdentifier = "ConfigKey";
        private const KeyCode FallbackKey = KeyCode.F9;

        private static MelonPreferences_Entry<KeyCode> _configKey;

        public static void Initialize(Settings settings)
        {
            var category = MelonPreferences.CreateCategory(CategoryName, "TimingShow");
            string label = i18n.T("Label_ConfigKey").TrimEnd(' ', ':', '：');
            _configKey = category.CreateEntry(ConfigKeyIdentifier, settings.ConfigKey, label, i18n.T("Melon_ConfigKey_Desc"));
            _configKey.OnEntryValueChanged.Subscribe((oldKey, newKey) =>
            {
                if (newKey != KeyCode.None)
                    settings.ConfigKey = newKey;
            });
            Apply(settings);
            MelonPreferences.Save();
        }

        public static void Apply(Settings settings)
        {
            if (settings == null || _configKey == null) return;

            KeyCode key = _configKey.Value;
            if (key != KeyCode.None)
            {
                settings.ConfigKey = key;
                return;
            }

            settings.ConfigKey = FallbackKey;
            _configKey.Value = FallbackKey;
        }

        public static void SetConfigKey(KeyCode key)
        {
            if (_configKey == null) return;

            _configKey.Value = key;
            MelonPreferences.Save();
        }
    }
}
