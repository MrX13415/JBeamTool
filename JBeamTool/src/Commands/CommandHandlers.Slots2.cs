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
        
        int countSlots = 0;         // Total parts with slot section
        int countSlots2 = 0;        // Total parts already in Slots2 format
        int countConverted = 0;     // Total converted parts
        int countFiles = 0;         // Total converted files

        foreach (var jbeam in jbeams)
        {
            var relativePath = Path.GetRelativePath(sourceDir, jbeam.FilePath);

            countSlots += jbeam.Parts.Count(p => p.Slots != null);
            countSlots2 += jbeam.Parts.Count(p => p.Slots?.IsTypeSlots2 is true);

            var convertedParts = jbeam.ConvertFormat_Slots2();
            if (convertedParts.Count == 0)
                continue;

            Console.WriteLine($"File: {relativePath}");
            foreach (var partName in convertedParts)
                Console.WriteLine($"   Part: {partName}");
            
            countConverted += convertedParts.Count;
            countFiles++;
            Console.WriteLine($"Converted {convertedParts.Count} parts in file: {relativePath}");

            var newFile = Path.GetFullPath(Path.Combine(targetDir, relativePath));
            Directory.CreateDirectory(Path.GetDirectoryName(newFile)!);
            File.WriteAllText(newFile, jbeam.FileContent);
        }

        int countLegacy = countSlots - countSlots2;     // Total parts with legacy slots

        if (countSlots == 0)
        {
            Console.WriteLine("Nothing to convert, no slots found.");
            return;
        }
        else if (countLegacy == 0)
        {
            Console.WriteLine("Nothing to convert, all slot sections are already in Slots2 format.");
            return;
        }

        if (countConverted < countLegacy)
            Console.WriteLine($"Warning: Not all slots were converted. Converted {countConverted} of {countLegacy} parts with legacy slots.");

        Console.WriteLine();
        Console.WriteLine($"Converted {countConverted} of {countSlots} parts with slot sections in {countFiles} files.");
    }
}
