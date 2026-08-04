


using System.Collections.Frozen;

void Print(string message = "")
{
    Console.WriteLine(message);
}

IEnumerable<string> GetJBeamFiles(string directory)
{
    return Directory.EnumerateFiles(directory, "*.jbeam", SearchOption.AllDirectories);
}

void Run_keys()
{
    var args = Environment.GetCommandLineArgs();
    string sourceDir = args.Length > 2 ? args[2] : Directory.GetCurrentDirectory();

    if (!Directory.Exists(sourceDir))
    {
        Print($"Source directory does not exist: {sourceDir}");
        return;
    }

    Print($"Source: {sourceDir}");

    List<JBeam.TranslationKey> translationKeys = new();

    var files = GetJBeamFiles(sourceDir);    
    foreach (var file in files)
    {
        var relativePath = Path.GetRelativePath(sourceDir, file);
        var content = File.ReadAllText(file);
        
        JBeam jBeam = new();
        try
        {
            jBeam.LoadFile(content);

            var part = jBeam.Parts.First();
            var information = part.Information;
            var slots = part.Slots;

        }
        catch (Exception ex)
        {
            Print($"Error parsing file: {file}");
            Print($"   {ex.Message}");
            Print($"   {ex.StackTrace}");
            continue;
        }

        var keys = jBeam.GetTranslationKeys().ToList();
        translationKeys.AddRange(keys);

        Print($"File: {relativePath} [{jBeam.Parts.Count} parts, {keys.Count} translation keys]");
    }

    translationKeys = JBeam.TranslationKey.MissingKeys(translationKeys);

    Print("Missing Translation Keys:");
    translationKeys.Sort((a, b) => string.Compare(a.Key, b.Key));
    foreach (var (key, value) in translationKeys.Distinct())
    {
        Print($"   \"{key}\": \"{value}\",");
    }
}

void Run()
{
    var args = Environment.GetCommandLineArgs();
    string command = args.Length > 1 ? args[1] : "";
    
    switch (command.ToLower().Trim())
    {
        case "keys":
            Run_keys();
            break;

        default:
            Print("Usage: JBeamTool.exe <command> <args>");
            Print();
            Print("Commands:");
            Print("   keys <sourceDir> - Show missing translation keys.");
            break;
    }

}

Run();