
using System.Text;
using BeamNG.JBeam;

void Print(string message = "")
{
    Console.WriteLine(message);
}


void InitJBeam(string sourceDir)
{
    JBeamFile.OnLoadFile += (jbeam) => {
        var relativePath = Path.GetRelativePath(sourceDir, jbeam.FilePath);
        Print($"   {relativePath} [{jbeam.Parts.Count} parts, {jbeam.GetTranslationKeys().Count()} translation keys]");
        foreach (var warning in jbeam.GetWarnings())
            Print($"      Warning: {warning}");
    };
}

void Run_Keys()
{
    Print();
    Print($"Report missing translation keys.");
    Print();

    var args = Environment.GetCommandLineArgs().Skip(2);
    string langCode = "en-US";
    string sourceDir = Directory.GetCurrentDirectory();
    if (args.Count() > 0 && BeamNG.App.BeamNGFolder.IsLanguageCode(args.First()))
    {
        langCode = args.First();
        args = args.Skip(1);      
    }
    if (args.Count() > 0)
    {
        sourceDir = args.First();
        args = args.Skip(1);
    }

    if (!Directory.Exists(sourceDir))
    {
        Print($"Source directory does not exist: {sourceDir}");
        return;
    }

    sourceDir = Path.GetFullPath(sourceDir);
    Print($"Source: {sourceDir}");
    Print($"Language Code: {langCode}");
    Print();

    Print("Loading JBeam files...");
    InitJBeam(sourceDir);
    var jbeams = JBeamFile.LoadFiles(sourceDir);
    Print();

    Print($"Missing Translation Keys in '{langCode}':");
    var translationKeys = JBeamFile.TranslationKey.MissingKeys(langCode, jbeams);
    translationKeys.Sort((a, b) => string.Compare(a.Key, b.Key));
    foreach (var (key, value) in translationKeys.Distinct())
    {
        Print($"   \"{key}\": \"{value}\",");
    }
}

void Run_Dump()
{
    Print();
    Print($"Dump JBeam files into a folder.");
    Print();

    var args = Environment.GetCommandLineArgs();
    string sourceDir = args.Length > 2 ? args[2] : Directory.GetCurrentDirectory();
    string targetDir = args.Length > 3 ? args[3] : Directory.GetCurrentDirectory();

    if (!Directory.Exists(sourceDir))
    {
        Print($"Source directory does not exist: {sourceDir}");
        return;
    }

    sourceDir = Path.GetFullPath(sourceDir);
    targetDir = Path.GetFullPath(targetDir);
    Print($"Source: {sourceDir}");
    Print($"Target: {targetDir}");
    Print();

    Print("Loading JBeam files...");
    InitJBeam(sourceDir);
    var jbeams = JBeamFile.LoadFiles(sourceDir);
    Print();

    foreach (var jbeam in jbeams)
    {
        var relativePath = Path.GetRelativePath(sourceDir, jbeam.FilePath);
        Print($"File: {relativePath}");

        var content = jbeam.Root.ToString();
        var newFile = Path.GetFullPath(Path.Combine(targetDir, relativePath));
        Directory.CreateDirectory(Path.GetDirectoryName(newFile)!);
        File.WriteAllText(newFile, content);
    }
}

void Run_Slots2()
{
    Print();
    Print($"Convert to Slots2 format.");
    Print();

    var args = Environment.GetCommandLineArgs();
    string sourceDir = args.Length > 2 ? args[2] : Directory.GetCurrentDirectory();
    string targetDir = args.Length > 3 ? args[3] : Directory.GetCurrentDirectory();

    if (!Directory.Exists(sourceDir))
    {
        Print($"Source directory does not exist: {sourceDir}");
        return;
    }

    sourceDir = Path.GetFullPath(sourceDir);
    targetDir = Path.GetFullPath(targetDir);
    Print($"Source: {sourceDir}");
    Print($"Target: {targetDir}");
    Print();

    Print("Loading JBeam files...");
    InitJBeam(sourceDir);
    var jbeams = JBeamFile.LoadFiles(sourceDir);
    Print();
    
    foreach (var jbeam in jbeams)
    {
        var relativePath = Path.GetRelativePath(sourceDir, jbeam.FilePath);

        var convertedParts = jbeam.ConvertFormat_Slots2();
        if (convertedParts.Count == 0)
            continue;
            
        Print($"File: {relativePath}");
        foreach (var partName in convertedParts)
        {
            Print($"   Part: {partName}");
        }

        var newFile = Path.GetFullPath(Path.Combine(targetDir, relativePath));
        Directory.CreateDirectory(Path.GetDirectoryName(newFile)!);
        File.WriteAllText(newFile, jbeam.FileContent);
    }
}

void Run_PriceList()
{
    Print();
    Print($"Part price list.");
    Print();

    bool IsSwitch(string arg) => arg.StartsWith("-") || arg.StartsWith("/");

    var args = Environment.GetCommandLineArgs();
    var switches = args.Where(IsSwitch).ToList();
    args = args.Where(arg => !IsSwitch(arg)).ToArray();
    string sourceDir = args.Length > 2 ? args[2] : Directory.GetCurrentDirectory();
    string? targetFile = args.Length > 3 ? args[3] : null;

    bool showTree = switches.Any(s => s.Equals("-tree", StringComparison.OrdinalIgnoreCase) || s.Equals("/tree", StringComparison.OrdinalIgnoreCase));

    if (!Directory.Exists(sourceDir))
    {
        Print($"Source directory does not exist: {sourceDir}");
        return;
    }

    sourceDir = Path.GetFullPath(sourceDir);
    Print($"Source: {sourceDir}");
    if (targetFile != null) 
    {
        targetFile = Path.GetFullPath(targetFile);
        if (Directory.Exists(targetFile))
            targetFile = Path.Combine(targetFile, "pricelist.txt");
        Print($"Target: {targetFile}");
    }

    Print();

    Print("Loading JBeam files...");
    InitJBeam(sourceDir);
    var jbeams = JBeamFile.LoadFiles(sourceDir);
    Print();
    
    string GetTree()
    {
        JBeamFile.PartsTree partsTree = new(jbeams);
        JBeamFile.PartsTree.TreePrinter.Config config = new()
        {
            LabelCallback = (node, isRef) =>
            {
                if (isRef) return "";
                else
                {
                    if (node.IsSlot)
                        return $"[{node.Slot?.Name}]" ?? "[n/a]";

                    var info = node.Part?.Information;
                    if (info == null)
                        return $"<{node.Part?.Identifier}>" ?? "<n/a>";

                    var value = string.IsNullOrEmpty(info.Value) ? "n/a" : $"${info.Value}";
                    return $" {value,-5} {info.Name}";
                }
            }
        };
        return new JBeamFile.PartsTree.TreePrinter(config).Print(partsTree.Roots);
    }
    string GetList()
    {
        StringBuilder sb = new();
        foreach (var jbeam in jbeams)
        {
            var relativePath = Path.GetRelativePath(sourceDir, jbeam.FilePath);

            sb.AppendLine($"File: {relativePath}");
            foreach (var part in jbeam.Parts)
            {
                var info = part.Information;
                if (info == null)
                {
                    sb.AppendLine($"   Part: {part.Identifier} - No information section.");
                    continue;
                }

                var authors = string.Join(", ", info.Authors);
                var value = string.IsNullOrEmpty(info.Value) ? "<n/a>" : $"${info.Value}";
                sb.AppendLine($"    {value,-5} - Part: {info.Name}");
            }            
        }

        return sb.ToString();
    }

    string content = showTree ? GetTree() : GetList();
    if (targetFile == null)
    {
        Print(content);
        
    }
    else
    {
        Print("Writing to file...");
        File.WriteAllText(targetFile, content);
    }
}

void Run()
{
    var args = Environment.GetCommandLineArgs();
    string command = args.Length > 1 ? args[1] : "";
    
    switch (command.ToLower().Trim())
    {
        case "keys":
            Run_Keys();
            break;

        case "slots2":
            Run_Slots2();
            break;

        case "dump":
            Run_Dump();
            break;

        case "pricelist":
            Run_PriceList();
            break;

        default:
            Print("Usage: JBeamTool.exe <command> <args>");
            Print();
            Print("Commands:");
            Print("   keys [lang] <sourceDir> - Show missing translation keys.");
            Print("   slots2 <sourceDir> <targetDir> - Convert JBeam files to Slots2 format.");
            Print("   dump <sourceDir> <targetDir> - Dump parsed JBeam files into the given folder.");
            Print("   pricelist [-tree] <sourceDir> [targetFile] - Show part price list.");
            break;
    }

}

Run();