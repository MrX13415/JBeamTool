using System.Text;
using BeamNG.JBeam;

namespace JBeamTool;

public static partial class CommandHandlers
{
    public static void Run_PriceList(CommandArgs args)
    {
        Console.WriteLine();
        Console.WriteLine($"Part price list.");
        Console.WriteLine();

        string sourceDir = args.Positional.Length > 0 ? args.Positional[0] : Directory.GetCurrentDirectory();
        string? targetFile = args.Positional.Length > 1 ? args.Positional[1] : null;

        if (!Directory.Exists(sourceDir))
        {
            Console.WriteLine($"Source directory does not exist: {sourceDir}");
            return;
        }

        sourceDir = Path.GetFullPath(sourceDir);
        Console.WriteLine($"Source: {sourceDir}");
        
        if (targetFile != null) 
        {
            targetFile = Path.GetFullPath(targetFile);
            if (Directory.Exists(targetFile))
                targetFile = Path.Combine(targetFile, "pricelist.txt");
            Console.WriteLine($"Target: {targetFile}");
        }

        Console.WriteLine();
        Console.WriteLine("Loading JBeam files...");
        JBeamLoader.InitializeLoader(sourceDir);
        var jbeams = JBeamFile.LoadFiles(sourceDir);
        Console.WriteLine();
        
        string pricelistText = GetPriceList(jbeams, args.Tree);
        
        if (targetFile != null)
            File.WriteAllText(targetFile, pricelistText);
        else
            Console.Write(pricelistText);
    }

    private static string GetPriceList(List<JBeamFile> jbeams, bool showTree)
    {
        if (showTree)
        {
            JBeamFile.PartsTree partsTree = new(jbeams);
            JBeamFile.PartsTree.TreePrinter.Config config = new()
            {
                LabelCallback = (node, isRef) =>
                {
                    if (isRef) return "";
                    if (node.IsSlot)
                        return $"[{node.Slot?.Name}]" ?? "[n/a]";

                    var info = node.Part?.Information;
                    if (info == null)
                        return $"<{node.Part?.Identifier}>" ?? "<n/a>";

                    var value = string.IsNullOrEmpty(info.Value) ? "n/a" : $"${info.Value}";
                    return $" {value,-5} {info.Name}";
                }
            };
            return new JBeamFile.PartsTree.TreePrinter(config).Print(partsTree.Roots);
        }

        StringBuilder sb = new();
        foreach (var jbeam in jbeams)
        {
            sb.AppendLine($"File: {Path.GetFileName(jbeam.FilePath)}");
            foreach (var part in jbeam.Parts)
            {
                var value = part.Information?.Value ?? "unknown";
                var name = part.Information?.Name ?? "unknown";
                sb.AppendLine($"   {part.Identifier}: ${value} - {name}");
            }
        }
        return sb.ToString();
    }
}
