using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TimingShow
{
    // Settings.json 按字段名前缀分组：HUD_x 写成 HUD: { x: ... }。旧版扁平文件读取时自动兼容。
    public class SettingsJsonConverter : JsonConverter
    {
        private const string RootValueKey = "Show";

        private sealed class Group
        {
            public readonly string Name;
            public readonly List<FieldInfo> Fields = new List<FieldInfo>();
            public FieldInfo Root;

            public Group(string name)
            {
                Name = name;
            }
        }

        private static readonly List<FieldInfo> RootFields = new List<FieldInfo>();
        private static readonly List<Group> Groups = new List<Group>();
        private static readonly Dictionary<string, Group> GroupsByName = new Dictionary<string, Group>(StringComparer.OrdinalIgnoreCase);
        private static readonly FieldInfo[] AllFields;

        static SettingsJsonConverter()
        {
            List<FieldInfo> fields = new List<FieldInfo>();
            foreach (FieldInfo field in typeof(Settings).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.IsStatic || field.IsLiteral || field.IsInitOnly) continue;
                fields.Add(field);
                int separator = field.Name.IndexOf('_');
                if (separator <= 0)
                {
                    RootFields.Add(field);
                    continue;
                }

                string name = field.Name.Substring(0, separator);
                Group group;
                if (!GroupsByName.TryGetValue(name, out group))
                {
                    group = new Group(name);
                    GroupsByName[name] = group;
                    Groups.Add(group);
                }
                group.Fields.Add(field);
            }

            foreach (FieldInfo field in RootFields.ToArray())
            {
                Group group;
                if (!GroupsByName.TryGetValue(field.Name, out group)) continue;
                group.Root = field;
                RootFields.Remove(field);
            }

            AllFields = fields.ToArray();
        }

        public static bool HasGroupedLayout(string json)
        {
            try
            {
                JObject root = JObject.Parse(json);
                foreach (Group group in Groups)
                {
                    if (root[group.Name] is JObject) return true;
                }
                return false;
            }
            catch (Exception)
            {
                return true;
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Settings);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Settings settings = (Settings)value;
            writer.WriteStartObject();
            foreach (FieldInfo field in RootFields)
            {
                writer.WritePropertyName(field.Name);
                serializer.Serialize(writer, field.GetValue(settings));
            }
            foreach (Group group in Groups)
            {
                writer.WritePropertyName(group.Name);
                writer.WriteStartObject();
                if (group.Root != null)
                {
                    writer.WritePropertyName(RootValueKey);
                    serializer.Serialize(writer, group.Root.GetValue(settings));
                }
                foreach (FieldInfo field in group.Fields)
                {
                    writer.WritePropertyName(field.Name.Substring(group.Name.Length + 1));
                    serializer.Serialize(writer, field.GetValue(settings));
                }
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;

            JObject json = JObject.Load(reader);
            Dictionary<string, JToken> values = new Dictionary<string, JToken>(StringComparer.OrdinalIgnoreCase);
            foreach (JProperty property in json.Properties())
            {
                Group group;
                JObject nested = property.Value as JObject;
                if (nested != null && GroupsByName.TryGetValue(property.Name, out group))
                {
                    foreach (JProperty child in nested.Properties())
                    {
                        if (group.Root != null && string.Equals(child.Name, RootValueKey, StringComparison.OrdinalIgnoreCase))
                            values[group.Root.Name] = child.Value;
                        else
                            values[group.Name + "_" + child.Name] = child.Value;
                    }
                    continue;
                }
                values[property.Name] = property.Value;
            }

            Settings settings = new Settings();
            foreach (FieldInfo field in AllFields)
            {
                JToken token;
                if (!values.TryGetValue(field.Name, out token) || token.Type == JTokenType.Null) continue;
                field.SetValue(settings, token.ToObject(field.FieldType, serializer));
            }
            return settings;
        }
    }
}
