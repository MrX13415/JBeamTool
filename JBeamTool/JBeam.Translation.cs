using Newtonsoft.Json.Linq;
using System.Text;

partial class JBeam
{
    public record TranslationKey(string Key, string Value)
    {
        public const string InformationName = "ui.vehicleconfig.information.name.";    
        public const string SlotDescription = "ui.vehicleconfig.slot.description.";
        public const string VariableTitle = "ui.vehicleconfig.variables.title.";
        public const string VariableCategory = "ui.vehicleconfig.variables.category.";
        public const string VariableDescription = "ui.vehicleconfig.variables.description.";

        public static TranslationKey New(string prefix, string str)
        {
            var test1 = JsonEscape("\"lol\"");
            var test2 = JsonEscape("A\nB");

            return new TranslationKey($"{prefix}{JsonEscape(str)}", JsonEscape(str));
        }

        public static readonly Dictionary<string, char> JsonEscapeMap = new()
        {
            [@""""] = '"',
            [@"\\"] = '\\',
            [@"\b"] = '\b',
            [@"\f"] = '\f',
            [@"\n"] = '\n',
            [@"\r"] = '\r',
            [@"\t"] = '\t'
        };
        public static string JsonEscape(string str)
        { 
            if (str == null)
                return "";

            // Escape tokens found, assume string is already escaped.
            // Notice: This does not check \uXXXX sequences!
            if (JsonEscapeMap.Any(kv => str.Contains(kv.Key)))
                return str;

            var sb = new StringBuilder(str.Length + 10);
            for(int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                
                var token = JsonEscapeMap.FirstOrDefault(kv => kv.Value == c);
                if (token.Key != null)
                    sb.Append(token.Key);
                else if (c < 0x20)
                    sb.Append("u").Append(((int)c).ToString("x4"));
                else
                    sb.Append(c);
            }

            return sb.ToString();
        }

        public static List<TranslationKey> LoadReference()
        {
            return LoadReference(new List<string> {                 
                Path.Combine(BeamNG.TranslationsFolder),  // official game translations
                Path.Combine(BeamNG.UnpackedModsFolder)   // Search for translations in unpacked mods
            });
        }
        public static List<TranslationKey> LoadReference(List<string> dirs)
        {
            List<TranslationKey> keys = new();
            foreach (var dir in dirs)
            {
                if (!Directory.Exists(dir))
                {
                    Console.WriteLine($"WARNING: Reference translation directory does not exist: {dir}");
                    continue;
                }
                var files = Directory.EnumerateFiles(dir, @"*.json", SearchOption.AllDirectories)
                    .Where(f => f.Contains(BeamNG.TranslationsRelativePath))
                    .Where(f => f.Contains("en-US")); // English is the reference language. Keys must be here.

                foreach (var file in files)
                {
                    var content = File.ReadAllText(file);
                    var json = JObject.Parse(content);
                    var fileKeys = json.Properties().Select(p => (key: p.Name, value: p.Value.ToString())).ToArray();
                    keys.AddRange(fileKeys.Select(k => new TranslationKey(k.key, k.value)));
                }
            }
            return keys;
        }
  
        public static List<TranslationKey> MissingKeys(List<TranslationKey> keys)
        {
            List<TranslationKey> refKeys = LoadReference();
            List<TranslationKey> missingKeys = new();
            foreach (var key in keys)
            {
                if (!refKeys.Any(k => k.Key == key.Key))
                {
                    missingKeys.Add(key);
                }
            }
            return missingKeys;
        }

    }
}
