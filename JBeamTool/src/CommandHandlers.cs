using System.Text;
using System.Text.Json;
using BeamNG.JBeam;

namespace JBeamTool;

public static class CommandHandlers
{
    private static readonly JsonSerializerOptions IndentedJsonOptions = new() { WriteIndented = true };
    private static readonly JsonSerializerOptions CompactJsonOptions = new() { WriteIndented = false };

    public static void Run_Keys(CommandArgs args)
    {
        string langCode = "en-US";
        string sourceDir = Directory.GetCurrentDirectory();
        bool apply = Environment.GetCommandLineArgs().Contains("--apply", StringComparer.OrdinalIgnoreCase);
        
        var positional = args.Positional.ToList();
        
        Console.WriteLine();
        Console.WriteLine($"Report missing translation keys.");
        if (apply)
            Console.WriteLine("Apply mode enabled: missing keys will be written into localization JSON files.");
        Console.WriteLine();

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
            var errorPayload = new { ok = false, sourceDir, languageCode = langCode, error = "Source directory does not exist." };
            if (args.Json)
                Console.WriteLine(JsonSerializer.Serialize(errorPayload, IndentedJsonOptions));
            else
                Console.WriteLine($"Source directory does not exist: {sourceDir}");
            Environment.ExitCode = 1;
            return;
        }

        sourceDir = Path.GetFullPath(sourceDir);
        var keyList = JBeamFile.LoadFiles(sourceDir);
        var translationKeys = JBeamFile.TranslationKey.MissingKeys(langCode, keyList);
        translationKeys.Sort((a, b) => string.Compare(a.Key, b.Key));

        var distinctKeys = translationKeys.Distinct().ToList();
        var filesUpdated = new List<string>();
        
        if (apply)
            filesUpdated = JBeamFile.TranslationKey.ApplyMissingKeys(langCode, sourceDir, distinctKeys);

        var resultPayload = new
        {
            ok = distinctKeys.Count == 0,
            languageCode = JBeamFile.TranslationKey.NormalizeLanguageCode(langCode),
            sourceDir,
            fileCount = keyList.Count,
            missingKeyCount = distinctKeys.Count,
            missingKeys = distinctKeys.Select(k => new { key = k.Key, value = k.Value }).ToList(),
            applied = apply,
            updatedFiles = filesUpdated,
            updatedFileCount = filesUpdated.Count
        };

        if (args.Json)
        {
            Console.WriteLine(JsonSerializer.Serialize(resultPayload, IndentedJsonOptions));
            Environment.ExitCode = resultPayload.ok ? 0 : 1;
            return;
        }

        Console.WriteLine($"Source: {sourceDir}");
        Console.WriteLine($"Language Code: {resultPayload.languageCode}");
        Console.WriteLine();
        Console.WriteLine("Loading JBeam files...");
        JBeamLoader.InitializeLoader(sourceDir);
        Console.WriteLine();
        Console.WriteLine($"Missing Translation Keys in '{resultPayload.languageCode}':");
        
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
            var errorPayload = new { ok = false, strict = args.Strict, path = sourcePath, error = "Path does not exist." };
            if (args.Json)
                Console.WriteLine(JsonSerializer.Serialize(errorPayload, IndentedJsonOptions));
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
            var emptyPayload = new { ok = false, strict = args.Strict, path = Path.GetFullPath(sourcePath), fileCount = 0, error = "No .jbeam files were found." };
            if (args.Json)
                Console.WriteLine(JsonSerializer.Serialize(emptyPayload, IndentedJsonOptions));
            else
                Console.WriteLine("No .jbeam files were found.");
            Environment.ExitCode = 1;
            return;
        }

        var results = new List<object>();
        var ok = true;

        foreach (var file in files)
        {
            var jbeam = new JBeamFile(file);
            var loaded = jbeam.LoadFile();
            var warnings = jbeam.GetWarnings().ToList();
            var fileOk = loaded && (!args.Strict || warnings.Count == 0);
            if (!fileOk) ok = false;

            results.Add(new
            {
                file = jbeam.FilePath,
                ok = fileOk,
                parts = jbeam.Parts.Count,
                sanity = jbeam.Sanity,
                warnings,
                translationKeyCount = jbeam.GetTranslationKeys().Count()
            });
        }

        if (args.Strict)
            ok = results.All(r => (bool)r.GetType().GetProperty("ok")!.GetValue(r)!);

        var verifyPayload = new
        {
            ok,
            strict = args.Strict,
            fileCount = files.Length,
            resultCount = results.Count,
            results,
            warnings = results.SelectMany(r => ((IEnumerable<string>)r.GetType().GetProperty("warnings")!.GetValue(r)!)).ToList()
        };

        if (args.Json)
        {
            Console.WriteLine(JsonSerializer.Serialize(verifyPayload, IndentedJsonOptions));
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
                var file = (string)result.GetType().GetProperty("file")!.GetValue(result)!;
                var fileOk = (bool)result.GetType().GetProperty("ok")!.GetValue(result)!;
                var partCount = (int)result.GetType().GetProperty("parts")!.GetValue(result)!;
                var warningList = (IEnumerable<string>)result.GetType().GetProperty("warnings")!.GetValue(result)!;
                var warningCount = warningList.Count();
                var sanity = (bool)result.GetType().GetProperty("sanity")!.GetValue(result)!;

                var relativePath = Path.GetRelativePath(Path.GetDirectoryName(Path.GetFullPath(sourcePath)) ?? Path.GetFullPath(sourcePath), file);
                Console.WriteLine($"{relativePath}: {(fileOk ? "OK" : "FAILED")} [parts={partCount}, sanity={sanity}, warnings={warningCount}]");
                foreach (var warning in warningList)
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
                var errorPayload = new { ok = false, file = targetPath, error = "No .jbeam files found in directory." };
                if (args.Json)
                    Console.WriteLine(JsonSerializer.Serialize(errorPayload, IndentedJsonOptions));
                else
                    Console.WriteLine($"No .jbeam files found in directory: {targetPath}");
                Environment.ExitCode = 1;
                return;
            }
            targetPath = firstFile;
        }

        if (!File.Exists(targetPath))
        {
            var errorPayload = new { ok = false, file = targetPath, error = "File does not exist." };
            if (args.Json)
                Console.WriteLine(JsonSerializer.Serialize(errorPayload, IndentedJsonOptions));
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

        var infoPayload = new
        {
            ok = loaded,
            file = jbeam.FilePath,
            parts = jbeam.Parts.Count,
            sanity = jbeam.Sanity,
            warningCount = warnings.Count,
            warnings,
            sectionCounts,
            partsSummary = jbeam.Parts.Select(p => new
            {
                id = p.Identifier,
                name = p.Information?.Name ?? "",
                value = p.Information?.Value ?? "",
                authors = p.Information?.Authors?.ToArray() ?? Array.Empty<string>(),
                slotType = p.SlotType?.Items ?? new List<string>(),
                slots = p.Slots?.Items.Select(s => s.Name).ToArray() ?? Array.Empty<string>()
            }).ToList()
        };

        if (args.Json)
        {
            Console.WriteLine(JsonSerializer.Serialize(infoPayload, IndentedJsonOptions));
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

            foreach (var part in infoPayload.partsSummary)
            {
                Console.WriteLine($"Part: {part.id} | Name: {part.name} | Value: {part.value}");
                if (part.slotType.Any())
                    Console.WriteLine($"   Slot types: {string.Join(", ", part.slotType)}");
                if (part.slots.Any())
                    Console.WriteLine($"   Slots: {string.Join(", ", part.slots)}");
            }
        }

        Environment.ExitCode = loaded ? 0 : 1;
    }

    public static void Run_Ast(CommandArgs args)
    {
        var sourcePath = args.Positional.FirstOrDefault() ?? Directory.GetCurrentDirectory();

        if (!File.Exists(sourcePath) && !Directory.Exists(sourcePath))
        {
            var errorPayload = new { ok = false, path = sourcePath, error = "Path does not exist." };
            if (args.Json)
                Console.WriteLine(JsonSerializer.Serialize(errorPayload, IndentedJsonOptions));
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
            var errorPayload = new { ok = false, path = Path.GetFullPath(sourcePath), error = "No .jbeam files were found." };
            if (args.Json)
                Console.WriteLine(JsonSerializer.Serialize(errorPayload, IndentedJsonOptions));
            else
                Console.WriteLine("No .jbeam files were found.");
            Environment.ExitCode = 1;
            return;
        }

        var summaries = new List<object>();
        foreach (var file in files)
        {
            var jbeam = new JBeamFile(file);
            var loaded = jbeam.LoadFile();
            var parts = jbeam.Parts.Select(part => new
            {
                id = part.Identifier,
                sections = part.Sections.Select(section => new
                {
                    type = section.Type.ToString(),
                    identifier = section.Identifier,
                    properties = section.Node.Value is ObjectNode sectionObject
                        ? sectionObject.Properties.Select(entry => new { key = entry.Name, valueType = entry.Value.GetType().Name }).ToList<object>()
                        : new List<object>()
                }).ToList(),
                node = AstDescriber.DescribeNode(part.Object())
            }).ToList();

            var entry = new
            {
                ok = loaded,
                file = jbeam.FilePath,
                partCount = jbeam.Parts.Count,
                sanity = jbeam.Sanity,
                warnings = jbeam.GetWarnings().ToList(),
                root = AstDescriber.DescribeNode(jbeam.Root),
                parts
            };

            summaries.Add(entry);
        }

        var payloadSummary = new
        {
            ok = summaries.All(s => (bool)s.GetType().GetProperty("ok")!.GetValue(s)!),
            fileCount = files.Length,
            files = summaries
        };

        if (args.Json)
        {
            Console.WriteLine(JsonSerializer.Serialize(payloadSummary, IndentedJsonOptions));
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine("JBeam AST summary");
            Console.WriteLine();

            foreach (var summary in summaries)
            {
                var file = (string)summary.GetType().GetProperty("file")!.GetValue(summary)!;
                var partCount = (int)summary.GetType().GetProperty("partCount")!.GetValue(summary)!;
                var sanity = (bool)summary.GetType().GetProperty("sanity")!.GetValue(summary)!;
                var warnings = (List<string>)summary.GetType().GetProperty("warnings")!.GetValue(summary)!;

                Console.WriteLine($"File: {file}");
                Console.WriteLine($"  Parts: {partCount}");
                Console.WriteLine($"  Sanity: {sanity}");
                if (warnings.Count > 0)
                {
                    Console.WriteLine("  Warnings:");
                    foreach (var warning in warnings)
                        Console.WriteLine($"    - {warning}");
                }

                var root = summary.GetType().GetProperty("root")!.GetValue(summary)!;
                var rootText = JsonSerializer.Serialize(root, CompactJsonOptions);
                Console.WriteLine($"  Root: {rootText}");
            }
        }

        Environment.ExitCode = payloadSummary.ok ? 0 : 1;
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
        bool showTree = Environment.GetCommandLineArgs().Any(s => s.Equals("-tree", StringComparison.OrdinalIgnoreCase));

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
        
        string pricelistText = GetPriceList(jbeams, showTree);
        
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
