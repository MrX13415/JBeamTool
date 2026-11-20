using System.Text.Json;
using BeamNG.JBeam;

namespace JBeamTool;

public static partial class CommandHandlers
{
    public static void Run_Keys(CommandArgs args)
    {
        string langCode = "en-US";
        string sourceDir = Directory.GetCurrentDirectory();
        bool apply = Environment.GetCommandLineArgs().Contains("--apply", StringComparer.OrdinalIgnoreCase);
        
        var positional = args.Positional.ToList();
        
        if (!args.Json)
        {
            Console.WriteLine();
            Console.WriteLine("Report missing translation keys.");
            if (apply)
                Console.WriteLine("Apply mode enabled: missing keys will be written into localization JSON files.");
            Console.WriteLine();
        }

        if (positional.Count > 0 && BeamNG.App.BeamNGFolder.IsLanguageCode(positional.First()))
        {
            langCode = positional.First();
            positional = positional.Skip(1).ToList();
        }
        if (positional.Count > 0)
        {
            sourceDir = positional.First();
            positional = positional.Skip(1).ToList();
        }

        if (!Directory.Exists(sourceDir))
        {
            var errorPayload = new KeysError(false, sourceDir, langCode, "Source directory does not exist.");
            if (args.Json)
                Console.WriteLine(JsonSerializer.Serialize(errorPayload, AppJsonSerializationContext.Default.KeysError));
            else
                Console.WriteLine($"Source directory does not exist: {sourceDir}");
            Environment.ExitCode = 1;
            return;
        }

        sourceDir = Path.GetFullPath(sourceDir);
        var languageCode = JBeamFile.TranslationKey.NormalizeLanguageCode(langCode);
        if (!args.Json)
        {
            Console.WriteLine($"Source: {sourceDir}");
            Console.WriteLine($"Language Code: {languageCode}");
            Console.WriteLine();
            Console.WriteLine("Loading JBeam files...");
            JBeamLoader.InitializeLoader(sourceDir);
        }
        var keyList = JBeamFile.LoadFiles(sourceDir);
        var translationKeys = JBeamFile.TranslationKey.MissingKeys(langCode, keyList);
        translationKeys.Sort((a, b) => string.Compare(a.Key, b.Key));

        var distinctKeys = translationKeys.Distinct().ToList();
        var filesUpdated = new List<string>();
        
        if (apply)
            filesUpdated = JBeamFile.TranslationKey.ApplyMissingKeys(langCode, sourceDir, distinctKeys);

        if (args.Json)
        {
            var resultPayload = new KeysResponse(distinctKeys.Count == 0, languageCode, sourceDir, keyList.Count,
                distinctKeys.Count, distinctKeys.Select(key => new MissingKey(key.Key, key.Value)).ToList(), apply, filesUpdated, filesUpdated.Count);
            Console.WriteLine(JsonSerializer.Serialize(resultPayload, AppJsonSerializationContext.Default.KeysResponse));
            Environment.ExitCode = resultPayload.Ok ? 0 : 1;
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"Missing Translation Keys in '{languageCode}':");
        
        foreach (var (key, value) in distinctKeys)
            Console.WriteLine($"   \"{key}\": \"{value}\",");

        if (apply)
        {
            Console.WriteLine();
            if (filesUpdated.Count == 0)
                Console.WriteLine("No localization files were updated.");
            else
            {
                Console.WriteLine($"Updated {filesUpdated.Count} localization file(s):");
                foreach (var file in filesUpdated)
                    Console.WriteLine($"   {file}");
            }
        }
    }
}
