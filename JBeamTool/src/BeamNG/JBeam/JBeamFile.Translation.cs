using BeamNG.App;
using Newtonsoft.Json.Linq;

namespace BeamNG.JBeam;

public partial class JBeamFile
{
    public IEnumerable<TranslationKey> GetTranslationKeys()
    {
        foreach (var part in Parts)
            foreach (var key in part.GetTranslationKeys())
                yield return key;
    }

    public record TranslationKey(string Key, string Value)
    {
        public const string InformationName = "ui.vehicleconfig.information.name.";    
        public const string SlotDescription = "ui.vehicleconfig.slot.description.";
        public const string VariableTitle = "ui.vehicleconfig.variables.title.";
        public const string VariableCategory = "ui.vehicleconfig.variables.category.";
        public const string VariableDescription = "ui.vehicleconfig.variables.description.";

        public static string NormalizeLookupText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return text
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\")
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t");
        }

        public static bool IsDynamicTranslation(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return text.Contains("{{", StringComparison.Ordinal) && text.Contains("}}", StringComparison.Ordinal)
                || text.Contains("| translate", StringComparison.OrdinalIgnoreCase)
                || text.Contains("|translate", StringComparison.OrdinalIgnoreCase);
        }

        public static TranslationKey New(string prefix, string str)
        {
            var normalized = NormalizeLookupText(str);
            return new TranslationKey($"{prefix}{normalized}", normalized);
        }

        public static List<TranslationKey> LoadReference(string langCode = "en-US")
        {
            return LoadReference(langCode, new List<string> {                 
                Path.Combine(BeamNGFolder.Translations),  // official game translations
                Path.Combine(BeamNGFolder.UnpackedMods)   // Search for translations in unpacked mods
            });
        }
        public static List<TranslationKey> LoadReference(string langCode, List<string> dirs)
        {
            if (string.IsNullOrEmpty(langCode))
                langCode = "en-US";

            List<TranslationKey> keys = new();
            foreach (var dir in dirs)
            {
                if (!Directory.Exists(dir))
                {
                    Console.WriteLine($"WARNING: Reference translation directory does not exist: {dir}");
                    continue;
                }
                var files = Directory.EnumerateFiles(dir, @"*.json", SearchOption.AllDirectories)
                    .Where(f => f.Contains(BeamNGFolder.TranslationsFolderName))
                    .Where(f => f.Contains(langCode)); // English is the reference language. Keys must be here.

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
  
        public static List<TranslationKey> MissingKeys(string langeCode, List<TranslationKey> keys)
        {
            List<TranslationKey> refKeys = LoadReference(langeCode);
            var referenceKeys = new HashSet<string>(refKeys.Select(k => NormalizeLookupText(k.Key)), StringComparer.Ordinal);
            List<TranslationKey> missingKeys = new();
            foreach (var key in keys)
            {
                var normalizedKey = NormalizeLookupText(key.Key);
                if (string.IsNullOrWhiteSpace(normalizedKey) || IsDynamicTranslation(normalizedKey))
                    continue;

                if (!referenceKeys.Contains(normalizedKey))
                {
                    missingKeys.Add(key);
                }
            }
            return missingKeys;
        }
        public static List<TranslationKey> MissingKeys(string langeCode, List<JBeamFile> jBeams)
        {
            var keys = jBeams.SelectMany(j => j.GetTranslationKeys()).ToList();
            return MissingKeys(langeCode, keys);
        }

    }
}
