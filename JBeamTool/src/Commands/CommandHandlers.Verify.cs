using System.Text.Json;
using BeamNG.JBeam;

namespace JBeamTool;

public static partial class CommandHandlers
{
    public static void Run_Verify(CommandArgs args)
    {
        var sourcePath = args.Positional.FirstOrDefault() ?? Directory.GetCurrentDirectory();

        if (!File.Exists(sourcePath) && !Directory.Exists(sourcePath))
        {
            var errorPayload = new VerifyError(false, args.Strict, sourcePath, "Path does not exist.");
            if (args.Json)
                Console.WriteLine(JsonSerializer.Serialize(errorPayload, AppJsonSerializationContext.Default.VerifyError));
            else
                Console.WriteLine($"Source path does not exist: {sourcePath}");
            Environment.ExitCode = 1;
            return;
        }

        var files = File.Exists(sourcePath)
            ? new[] { Path.GetFullPath(sourcePath) }
            : JBeamFile.EnumerateFiles(Path.GetFullPath(sourcePath)).ToArray();

        if (files.Length == 0)
        {
            var emptyPayload = new VerifyError(false, args.Strict, Path.GetFullPath(sourcePath), "No .jbeam files were found.", 0);
            if (args.Json)
                Console.WriteLine(JsonSerializer.Serialize(emptyPayload, AppJsonSerializationContext.Default.VerifyError));
            else
                Console.WriteLine("No .jbeam files were found.");
            Environment.ExitCode = 1;
            return;
        }

        var results = new List<VerifyFileResult>();

        foreach (var file in files)
        {
            var jbeam = new JBeamFile(file);
            var loaded = jbeam.LoadFile();
            var warnings = jbeam.GetWarnings().ToList();
            var fileOk = loaded && (!args.Strict || warnings.Count == 0);
            results.Add(new VerifyFileResult(jbeam.FilePath, fileOk, jbeam.Parts.Count, jbeam.Sanity, warnings, jbeam.GetTranslationKeys().Count()));
        }

        var ok = results.All(result => result.Ok);
        var verifyPayload = new VerifyResponse(ok, args.Strict, files.Length, results.Count, results, results.SelectMany(result => result.Warnings).ToList());

        if (args.Json)
        {
            Console.WriteLine(JsonSerializer.Serialize(verifyPayload, AppJsonSerializationContext.Default.VerifyResponse));
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine($"Verify JBeam files: {(ok ? "OK" : "FAILED")}");
            Console.WriteLine($"Path: {Path.GetFullPath(sourcePath)}");
            Console.WriteLine($"Files: {files.Length}");
            Console.WriteLine();

            foreach (var result in results)
            {
                var relativePath = Path.GetRelativePath(Path.GetDirectoryName(Path.GetFullPath(sourcePath)) ?? Path.GetFullPath(sourcePath), result.File);
                Console.WriteLine($"{relativePath}: {(result.Ok ? "OK" : "FAILED")} [parts={result.Parts}, sanity={result.Sanity}, warnings={result.Warnings.Count}]");
                foreach (var warning in result.Warnings)
                    Console.WriteLine($"   - {warning}");
            }
        }

        Environment.ExitCode = ok ? 0 : 1;
    }
}
