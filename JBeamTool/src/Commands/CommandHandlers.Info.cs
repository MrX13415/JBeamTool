using System.Text.Json;
using BeamNG.JBeam;

namespace JBeamTool;

public static partial class CommandHandlers
{
    public static void Run_Info(CommandArgs args)
    {
        var sourcePath = args.Positional.FirstOrDefault() ?? Directory.GetCurrentDirectory();
        var targetPath = sourcePath;

        if (Directory.Exists(targetPath))
        {
            var firstFile = JBeamFile.EnumerateFiles(Path.GetFullPath(targetPath)).FirstOrDefault();
            if (firstFile == null)
            {
                var errorPayload = new InfoError(false, targetPath, "No .jbeam files found in directory.");
                if (args.Json)
                    Console.WriteLine(JsonSerializer.Serialize(errorPayload, AppJsonSerializationContext.Default.InfoError));
                else
                    Console.WriteLine($"No .jbeam files found in directory: {targetPath}");
                Environment.ExitCode = 1;
                return;
            }
            targetPath = firstFile;
        }

        if (!File.Exists(targetPath))
        {
            var errorPayload = new InfoError(false, targetPath, "File does not exist.");
            if (args.Json)
                Console.WriteLine(JsonSerializer.Serialize(errorPayload, AppJsonSerializationContext.Default.InfoError));
            else
                Console.WriteLine($"File does not exist: {targetPath}");
            Environment.ExitCode = 1;
            return;
        }

        var jbeam = new JBeamFile(targetPath);
        var loaded = jbeam.LoadFile();
        var warnings = jbeam.GetWarnings().ToList();
        var sectionCounts = jbeam.Parts
            .SelectMany(p => p.Sections)
            .GroupBy(s => s.Type.ToString())
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.Count());

        var partsSummary = jbeam.Parts.Select(part => new InfoPart(part.Identifier, part.Information?.Name ?? "",
            part.Information?.Value ?? "", part.Information?.Authors.ToArray() ?? [],
            part.SlotType?.Items ?? [], part.Slots?.Items.Select(slot => slot.Name).ToArray() ?? [])).ToList();
        var infoPayload = new InfoResponse(loaded, jbeam.FilePath, jbeam.Parts.Count, jbeam.Sanity, warnings.Count, warnings, sectionCounts, partsSummary);

        if (args.Json)
        {
            Console.WriteLine(JsonSerializer.Serialize(infoPayload, AppJsonSerializationContext.Default.InfoResponse));
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine($"JBeam Info: {jbeam.FilePath}");
            Console.WriteLine($"Loaded: {(loaded ? "yes" : "no")}");
            Console.WriteLine($"Parts: {jbeam.Parts.Count}");
            Console.WriteLine($"Sanity: {jbeam.Sanity}");
            Console.WriteLine($"Warnings: {warnings.Count}");

            foreach (var warning in warnings)
                Console.WriteLine($"   - {warning}");

            if (sectionCounts.Count > 0)
            {
                Console.WriteLine("Sections:");
                foreach (var kv in sectionCounts)
                    Console.WriteLine($"   - {kv.Key}: {kv.Value}");
            }

            foreach (var part in partsSummary)
            {
                Console.WriteLine($"Part: {part.Id} | Name: {part.Name} | Value: {part.Value}");
                if (part.SlotType.Any())
                    Console.WriteLine($"   Slot types: {string.Join(", ", part.SlotType)}");
                if (part.Slots.Any())
                    Console.WriteLine($"   Slots: {string.Join(", ", part.Slots)}");
            }
        }

        Environment.ExitCode = loaded ? 0 : 1;
    }
}
