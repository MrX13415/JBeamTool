using System.Text;
using System.Text.Json;
using BeamNG.JBeam;

namespace JBeamTool;

public static class CommandHandlers
{
    public static void Run_Keys(CommandArgs args)
    {
        string langCode = "en-US";
        string sourceDir = Directory.GetCurrentDirectory();
        bool apply = Environment.GetCommandLineArgs().Contains("--apply", StringComparer.OrdinalIgnoreCase);
        
        var positional = args.Positional.ToList();
        
        if (!args.Json)
        {
            Console.WriteLine();
            Console.WriteLine("Report missing translation keys.");
            if (apply)
                Console.WriteLine("Apply mode enabled: missing keys will be written into localization JSON files.");
            Console.WriteLine();
        }

        if (positional.Count > 0 && BeamNG.App.BeamNGFolder.IsLanguageCode(positional.First()))
        {
            langCode = positional.First();
            positional = positional.Skip(1).ToList();
        }
        if (positional.Count > 0)
        {
            sourceDir = positional.First();
            positional = positional.Skip(1).ToList();
        }

        if (!Directory.Exists(sourceDir))
        {
            var errorPayload = new KeysError(false, sourceDir, langCode, "Source directory does not exist.");
            if (args.Json)
                Console.WriteLine(JsonSerializer.Serialize(errorPayload, AppJsonSerializationContext.Default.KeysError));
            else
                Console.WriteLine($"Source directory does not exist: {sourceDir}");
            Environment.ExitCode = 1;
            return;
        }

        sourceDir = Path.GetFullPath(sourceDir);
        var languageCode = JBeamFile.TranslationKey.NormalizeLanguageCode(langCode);
        if (!args.Json)
        {
            Console.WriteLine($"Source: {sourceDir}");
            Console.WriteLine($"Language Code: {languageCode}");
            Console.WriteLine();
            Console.WriteLine("Loading JBeam files...");
            JBeamLoader.InitializeLoader(sourceDir);
        }
        var keyList = JBeamFile.LoadFiles(sourceDir);
        var translationKeys = JBeamFile.TranslationKey.MissingKeys(langCode, keyList);
        translationKeys.Sort((a, b) => string.Compare(a.Key, b.Key));

        var distinctKeys = translationKeys.Distinct().ToList();
        var filesUpdated = new List<string>();
        
        if (apply)
            filesUpdated = JBeamFile.TranslationKey.ApplyMissingKeys(langCode, sourceDir, distinctKeys);

        if (args.Json)
        {
            var resultPayload = new KeysResponse(distinctKeys.Count == 0, languageCode, sourceDir, keyList.Count,
                distinctKeys.Count, distinctKeys.Select(key => new MissingKey(key.Key, key.Value)).ToList(), apply, filesUpdated, filesUpdated.Count);
            Console.WriteLine(JsonSerializer.Serialize(resultPayload, AppJsonSerializationContext.Default.KeysResponse));
            Environment.ExitCode = resultPayload.Ok ? 0 : 1;
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"Missing Translation Keys in '{languageCode}':");
        
        foreach (var (key, value) in distinctKeys)
            Console.WriteLine($"   \"{key}\": \"{value}\",");

        if (apply)
        {
            Console.WriteLine();
            if (filesUpdated.Count == 0)
                Console.WriteLine("No localization files were updated.");
            else
            {
                Console.WriteLine($"Updated {filesUpdated.Count} localization file(s):");
                foreach (var file in filesUpdated)
                    Console.WriteLine($"   {file}");
            }
        }
    }

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
