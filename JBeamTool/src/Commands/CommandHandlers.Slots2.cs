using BeamNG.JBeam;

namespace JBeamTool;

public static partial class CommandHandlers
{
    public static void Run_Slots2(CommandArgs args)
    {
        Console.WriteLine();
        Console.WriteLine($"Convert to Slots2 format.");
        Console.WriteLine();

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
            var convertedParts = jbeam.ConvertFormat_Slots2();
            
            if (convertedParts.Count == 0)
                continue;
                
            Console.WriteLine($"File: {relativePath}");
            foreach (var partName in convertedParts)
                Console.WriteLine($"   Part: {partName}");

            var newFile = Path.GetFullPath(Path.Combine(targetDir, relativePath));
            Directory.CreateDirectory(Path.GetDirectoryName(newFile)!);
            File.WriteAllText(newFile, jbeam.FileContent);
        }
    }
}
