
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

    var args = Environment.GetCommandLineArgs();
    string sourceDir = args.Length > 2 ? args[2] : Directory.GetCurrentDirectory();

    if (!Directory.Exists(sourceDir))
    {
        Print($"Source directory does not exist: {sourceDir}");
        return;
    }

    sourceDir = Path.GetFullPath(sourceDir);
    Print($"Source: {sourceDir}");
    Print();

    Print("Loading JBeam files...");
    InitJBeam(sourceDir);
    var jbeams = JBeamFile.LoadFiles(sourceDir);
    Print();

    Print("Missing Translation Keys:");
    var translationKeys = JBeamFile.TranslationKey.MissingKeys(jbeams);
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

    var args = Environment.GetCommandLineArgs();
    string sourceDir = args.Length > 2 ? args[2] : Directory.GetCurrentDirectory();

    if (!Directory.Exists(sourceDir))
    {
        Print($"Source directory does not exist: {sourceDir}");
        return;
    }

    sourceDir = Path.GetFullPath(sourceDir);
    Print($"Source: {sourceDir}");
    Print();

    Print("Loading JBeam files...");
    InitJBeam(sourceDir);
    var jbeams = JBeamFile.LoadFiles(sourceDir);
    Print();
    
    foreach (var jbeam in jbeams)
    {
        var relativePath = Path.GetRelativePath(sourceDir, jbeam.FilePath);

        Print($"File: {relativePath}");
        foreach (var part in jbeam.Parts)
        {
            var info = part.Information;
            if (info == null)
            {
                Print($"   Part: {part.Identifier} - No information section.");
                continue;
            }

            var authors = string.Join(", ", info.Authors);
            var value = string.IsNullOrEmpty(info.Value) ? "<n/a>" : $"${info.Value}";
            Print($"    {value,-5} - Part: {info.Name} (by {authors})");
        }
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
            Print("   keys [sourceDir] - Show missing translation keys.");
            Print("   slots2 [sourceDir] [targetDir] - Convert JBeam files to Slots2 format.");
            Print("   dump [sourceDir] [targetDir] - Dump parsed JBeam files into the given folder.");
            Print("   pricelist [sourceDir] - Show part price list.");
            break;
    }

}

Run();