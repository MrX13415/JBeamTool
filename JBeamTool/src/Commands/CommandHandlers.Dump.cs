using BeamNG.JBeam;

namespace JBeamTool;

public static partial class CommandHandlers
{
    public static void Run_Dump(CommandArgs args)
    {
        Console.WriteLine();
        Console.WriteLine($"Dump JBeam files into a folder.");
        Console.WriteLine();

        var cmdArgs = Environment.GetCommandLineArgs();
        string sourceDir = args.Positional.Length > 0 ? args.Positional[0] : Directory.GetCurrentDirectory();
        string targetDir = args.Positional.Length > 1 ? args.Positional[1] : Directory.GetCurrentDirectory();

        if (!Directory.Exists(sourceDir))
        {
            Console.WriteLine($"Source directory does not exist: {sourceDir}");
            return;
        }

        sourceDir = Path.GetFullPath(sourceDir);
        targetDir = Path.GetFullPath(targetDir);
        Console.WriteLine($"Source: {sourceDir}");
        Console.WriteLine($"Target: {targetDir}");
        Console.WriteLine();

        Console.WriteLine("Loading JBeam files...");
        JBeamLoader.InitializeLoader(sourceDir);
        var jbeams = JBeamFile.LoadFiles(sourceDir);
        Console.WriteLine();

        foreach (var jbeam in jbeams)
        {
            var relativePath = Path.GetRelativePath(sourceDir, jbeam.FilePath);
            Console.WriteLine($"File: {relativePath}");

            var content = jbeam.Root.ToString();
            var newFile = Path.GetFullPath(Path.Combine(targetDir, relativePath));
            Directory.CreateDirectory(Path.GetDirectoryName(newFile)!);
            File.WriteAllText(newFile, content);
        }
    }
}
