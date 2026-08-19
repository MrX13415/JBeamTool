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

        public static TranslationKey New(string prefix, string str)
        {
            return new TranslationKey($"{prefix}{str.JsonEscape()}", str.JsonEscape());
        }

        public static List<TranslationKey> LoadReference()
        {
            return LoadReference(new List<string> {                 
                Path.Combine(BeamNGFolder.Translations),  // official game translations
                Path.Combine(BeamNGFolder.UnpackedMods)   // Search for translations in unpacked mods
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
                    .Where(f => f.Contains(BeamNGFolder.TranslationsFolderName))
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
        public static List<TranslationKey> MissingKeys(List<JBeamFile> jBeams)
        {
            var keys = jBeams.SelectMany(j => j.GetTranslationKeys()).ToList();
            return MissingKeys(keys);
        }

    }
}
