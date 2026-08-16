
void Print(string message = "")
{
    Console.WriteLine(message);
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

    JBeam.OnLoadFile += (jbeam) => {
        var relativePath = Path.GetRelativePath(sourceDir, jbeam.FilePath);
        Print($"File: {relativePath} [{jbeam.Parts.Count} parts, {jbeam.GetTranslationKeys().Count()} translation keys]");
        foreach (var warning in jbeam.GetWarnings())
            Print($"   Warning: {warning}");
    };

    var jbeams = JBeam.LoadPath(sourceDir);

/*
    Print("");
    Print("Parts:");
    foreach (var jbeam in jbeams)
    {
        var relativePath = Path.GetRelativePath(sourceDir, jbeam.FilePath);
        Print($"File: {relativePath}");
        foreach (var part in jbeam.Parts)
        {
            var partUIName = part.Information?.Name ?? "(unnamed)";
            Print($"   Part: '{partUIName}' [{part.Name}]");
            if (part.Slots != null)
            {
                foreach (var slot in part.Slots.Slots)
                {
                    Print($"      Slot: '{slot.Description}' [{slot.Name}]");
                }
            }
            if (part.Variables != null)
            {
                foreach (var variable in part.Variables.Variables)
                {
                    Print($"      Variable: '{variable.Description}' ({variable.Name})");
                }
            }
            if (part.Flexbodies != null)
            {
                foreach (var flexbody in part.Flexbodies.Flexbodies)
                {
                    List<string> details = new();
                    if (flexbody.Groups.Count > 0) 
                        details.Add($"Groups: {string.Join(", ", flexbody.Groups)}");
                    if (flexbody.Json != null) 
                        details.Add("Json: {...}");

                    Print($"      Flexbody: '{flexbody.Mesh}' ({string.Join(", ", details)})");
                }
            }
        }
    }
    */

    /*
    Print("");
    Print("Parts:");
    foreach (var jbeam in jbeams)
    {
        var relativePath = Path.GetRelativePath(sourceDir, jbeam.FilePath);
        Print($"File: {relativePath}");
        Print(jbeam.Root.ToString(3));
    }*/

    Print("");
    Print("Missing Translation Keys:");
    var translationKeys = JBeam.TranslationKey.MissingKeys(jbeams);
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