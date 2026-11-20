namespace JBeamTool;

public static class UsageHelper
{
    public static void PrintHelp()
    {
        Console.WriteLine(AppInfo.FullTitle);
        Console.WriteLine();
        Console.WriteLine("USAGE:");
        Console.WriteLine("   JBeamTool.exe <command> [options] <arguments>");
        Console.WriteLine();
        Console.WriteLine("COMMANDS:");
        Console.WriteLine();
        Console.WriteLine("   verify [--json] [--strict] <path>");
        Console.WriteLine("      Validate JBeam files or directories for syntax errors and structural integrity.");
        Console.WriteLine("      Options:");
        Console.WriteLine("         --json   Output results in JSON format");
        Console.WriteLine("         --strict Treat warnings as errors (exit code 1 if warnings found)");
        Console.WriteLine();
        Console.WriteLine("   info [--json] <file.jbeam>");
        Console.WriteLine("      Retrieve metadata for a single JBeam file without full validation.");
        Console.WriteLine("      Options:");
        Console.WriteLine("         --json   Output results in JSON format");
        Console.WriteLine();
        Console.WriteLine("   keys [--json] [--apply] [langCode] <sourceDir>");
        Console.WriteLine("      Find and optionally update missing translation keys in JBeam files.");
        Console.WriteLine("      Options:");
        Console.WriteLine("         --json         Output results in JSON format");
        Console.WriteLine("         --apply        Write missing keys to locale JSON files");
        Console.WriteLine("         langCode       Language code (e.g., en-US, de-DE). Default: en-US");
        Console.WriteLine();
        Console.WriteLine("   ast [--json] <file.jbeam>");
        Console.WriteLine("      Expose the parsed JBeam abstract syntax tree for detailed structural analysis.");
        Console.WriteLine("      Options:");
        Console.WriteLine("         --json   Output AST in JSON format");
        Console.WriteLine();
        Console.WriteLine("   dump <sourceDir> <targetDir>");
        Console.WriteLine("      Dump parsed JBeam files into the specified target folder.");
        Console.WriteLine();
        Console.WriteLine("   slots2 <sourceDir> <targetDir>");
        Console.WriteLine("      Convert JBeam files to Slots2 format.");
        Console.WriteLine();
        Console.WriteLine("   pricelist [-tree] <sourceDir> [targetFile]");
        Console.WriteLine("      Generate a part price list from JBeam files.");
        Console.WriteLine("      Options:");
        Console.WriteLine("         -tree   Display price list in tree format");
        Console.WriteLine();
        Console.WriteLine("EXAMPLES:");
        Console.WriteLine("   JBeamTool.exe verify \"C:\\mods\\vehicle\\part.jbeam\" --json --strict");
        Console.WriteLine("   JBeamTool.exe keys en-US \"C:\\mods\\vehicle\" --json --apply");
        Console.WriteLine("   JBeamTool.exe info \"C:\\mods\\vehicle\\engine.jbeam\" --json");
        Console.WriteLine();
        Console.WriteLine("For more information, see README.md in the package directory.");
    }
}
