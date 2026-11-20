using System.Text.Json;
using System.Text.Json.Serialization;
using BeamNG.App;

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

        public static string NormalizeLanguageCode(string langCode)
        {
            var normalized = (langCode ?? "en-US").Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                normalized = "en-US";

            normalized = normalized.Replace("_", "-");

            if (normalized.Equals("en", StringComparison.OrdinalIgnoreCase))
                return "en-US";

            return normalized;
        }

        public static IEnumerable<string> FindLanguageJsonFiles(string langCode, string sourceDir)
        {
            if (string.IsNullOrWhiteSpace(sourceDir) || !Directory.Exists(sourceDir))
                return Array.Empty<string>();

            var normalizedLang = NormalizeLanguageCode(langCode).ToLowerInvariant();
            var aliasLang = normalizedLang.Replace("-", "_");

            var matches = Directory.EnumerateFiles(sourceDir, "*.json", SearchOption.AllDirectories)
                .Where(path =>
                {
                    var fileLower = path.ToLowerInvariant();
                    var folderSegments = Path.GetDirectoryName(path)?.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Select(s => s.ToLowerInvariant()).ToArray() ?? Array.Empty<string>();

                    var langInPath = folderSegments.Any(segment =>
                        segment == normalizedLang || segment == aliasLang || segment == normalizedLang.Replace("-", "") || segment == aliasLang.Replace("_", ""));

                    if (!langInPath)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(path);
                        if (fileName.Equals(normalizedLang, StringComparison.OrdinalIgnoreCase) || fileName.Equals(aliasLang, StringComparison.OrdinalIgnoreCase))
                            return true;

                        if (fileName.StartsWith(normalizedLang, StringComparison.OrdinalIgnoreCase) || fileName.StartsWith(aliasLang, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }

                    return langInPath && (fileLower.Contains("locales") || fileLower.Contains("translations") || fileLower.Contains("locale"));
                })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return matches.Count > 0 ? matches : Array.Empty<string>();
        }

        public static List<string> ApplyMissingKeys(string langCode, string sourceDir, List<TranslationKey> missingKeys)
        {
            var appliedFiles = new List<string>();
            if (missingKeys.Count == 0)
                return appliedFiles;

            var localeFiles = FindLanguageJsonFiles(langCode, sourceDir).ToList();
            if (localeFiles.Count == 0)
            {
                var fallbackDir = Path.Combine(sourceDir, "locales", NormalizeLanguageCode(langCode));
                Directory.CreateDirectory(fallbackDir);
                var fallbackFile = Path.Combine(fallbackDir, "generated_missing_translations.json");
                
                var dict = missingKeys.Distinct().ToDictionary(k => k.Key, k => k.Value);
                var json = JsonSerializer.Serialize(dict, LocalizationJsonContext.Default.StringEntries);
                File.WriteAllText(fallbackFile, json);
                appliedFiles.Add(fallbackFile);
                return appliedFiles;
            }

            foreach (var localeFile in localeFiles)
            {
                try
                {
                    var content = File.ReadAllText(localeFile);
                    var dict = JsonSerializer.Deserialize(content, LocalizationJsonContext.Default.ElementEntries);
                    
                    if (dict == null)
                        continue;

                    var addedAny = false;
                    foreach (var missingKey in missingKeys.Distinct())
                    {
                        if (dict.ContainsKey(missingKey.Key))
                            continue;

                        dict[missingKey.Key] = JsonSerializer.SerializeToElement(missingKey.Value, LocalizationJsonContext.Default.String);
                        addedAny = true;
                    }

                    if (!addedAny)
                        continue;

                    var json = JsonSerializer.Serialize(dict, LocalizationJsonContext.Default.ElementEntries);
                    File.WriteAllText(localeFile, json);
                    appliedFiles.Add(localeFile);
                }
                catch (Exception)
                {
                    continue;
                }
            }

            return appliedFiles;
        }

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
                    Console.Error.WriteLine($"WARNING: Reference translation directory does not exist: {dir}");
                    continue;
                }
                var files = Directory.EnumerateFiles(dir, @"*.json", SearchOption.AllDirectories)
                    .Where(f => f.Contains(BeamNGFolder.TranslationsFolderName))
                    .Where(f => f.Contains(langCode));

                foreach (var file in files)
                {
                    try
                    {
                        var content = File.ReadAllText(file);
                        var json = JsonSerializer.Deserialize(content, LocalizationJsonContext.Default.StringEntries);
                        
                        if (json != null)
                        {
                            foreach (var kvp in json)
                                keys.Add(new TranslationKey(kvp.Key, kvp.Value));
                        }
                    }
                    catch (Exception)
                    {
                        continue;
                    }
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

[JsonSourceGenerationOptions(ReadCommentHandling = JsonCommentHandling.Skip, WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<string, string>), TypeInfoPropertyName = "StringEntries")]
[JsonSerializable(typeof(Dictionary<string, JsonElement>), TypeInfoPropertyName = "ElementEntries")]
internal partial class LocalizationJsonContext : JsonSerializerContext;
