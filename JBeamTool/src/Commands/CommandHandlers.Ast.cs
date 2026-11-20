using System.Text.Json;
using BeamNG.JBeam;

namespace JBeamTool;

public static partial class CommandHandlers
{
    public static void Run_Ast(CommandArgs args)
    {
        var sourcePath = args.Positional.FirstOrDefault() ?? Directory.GetCurrentDirectory();

        if (!File.Exists(sourcePath) && !Directory.Exists(sourcePath))
        {
            var errorPayload = new AstError(false, sourcePath, "Path does not exist.");
            if (args.Json)
                Console.WriteLine(JsonSerializer.Serialize(errorPayload, AppJsonSerializationContext.Default.AstError));
            else
                Console.WriteLine($"Path does not exist: {sourcePath}");
            Environment.ExitCode = 1;
            return;
        }

        var files = File.Exists(sourcePath)
            ? new[] { Path.GetFullPath(sourcePath) }
            : JBeamFile.EnumerateFiles(Path.GetFullPath(sourcePath)).ToArray();

        if (files.Length == 0)
        {
            var errorPayload = new AstError(false, Path.GetFullPath(sourcePath), "No .jbeam files were found.");
            if (args.Json)
                Console.WriteLine(JsonSerializer.Serialize(errorPayload, AppJsonSerializationContext.Default.AstError));
            else
                Console.WriteLine("No .jbeam files were found.");
            Environment.ExitCode = 1;
            return;
        }

        var summaries = new List<AstFile>();
        foreach (var file in files)
        {
            var jbeam = new JBeamFile(file);
            var loaded = jbeam.LoadFile();
            var parts = jbeam.Parts.Select(part => new AstPart(part.Identifier,
                part.Sections.Select(section => new AstSection(section.Type.ToString(), section.Identifier,
                    section.Node.Value is ObjectNode sectionObject
                        ? sectionObject.Properties.Select(entry => new AstProperty(entry.Name, entry.Value.GetType().Name)).ToList()
                        : [])).ToList(), AstDescriber.DescribeNode(part.Object()))).ToList();

            summaries.Add(new AstFile(loaded, jbeam.FilePath, jbeam.Parts.Count, jbeam.Sanity,
                jbeam.GetWarnings().ToList(), AstDescriber.DescribeNode(jbeam.Root), parts));
        }

        var payloadSummary = new AstResponse(summaries.All(summary => summary.Ok), files.Length, summaries);

        if (args.Json)
        {
            Console.WriteLine(JsonSerializer.Serialize(payloadSummary, AppJsonSerializationContext.Default.AstResponse));
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine("JBeam AST summary");
            Console.WriteLine();

            foreach (var summary in summaries)
            {
                Console.WriteLine($"File: {summary.File}");
                Console.WriteLine($"  Parts: {summary.PartCount}");
                Console.WriteLine($"  Sanity: {summary.Sanity}");
                if (summary.Warnings.Count > 0)
                {
                    Console.WriteLine("  Warnings:");
                    foreach (var warning in summary.Warnings)
                        Console.WriteLine($"    - {warning}");
                }

                Console.WriteLine($"  Root: {summary.Root.ToJsonString()}");
            }
        }

        Environment.ExitCode = payloadSummary.Ok ? 0 : 1;
    }
}
