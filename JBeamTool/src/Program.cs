
using System.Text;
using BeamNG.JBeam;
using Newtonsoft.Json;

void Print(string message = "")
{
    Console.WriteLine(message);
}

(string[] Positional, bool Json, bool Strict) ParseCommandArgs(string[] args)
{
    var positional = new List<string>();
    bool json = false;
    bool strict = false;

    foreach (var arg in args)
    {
        if (!arg.StartsWith('-') && !arg.StartsWith('/'))
        {
            positional.Add(arg);
            continue;
        }

        switch (arg.Trim().ToLowerInvariant())
        {
            case "--json":
            case "-json":
            case "/json":
                json = true;
                break;
            case "--strict":
            case "-strict":
            case "/strict":
                strict = true;
                break;
            default:
                positional.Add(arg);
                break;
        }
    }

    return (positional.ToArray(), json, strict);
}

void InitJBeam(string sourceDir)
{
    JBeamFile.OnLoadFile += (jbeam) => {
        var relativePath = Path.GetRelativePath(sourceDir, jbeam.FilePath);
        Print($"   {relativePath} [{jbeam.Parts.Count} parts, {jbeam.GetTranslationKeys().Count()} translation keys]");
        foreach (var warning in jbeam.GetWarnings())
            Print($"      Warning: {warning}");
    };
}

void Run_Keys()
{
    var args = Environment.GetCommandLineArgs().Skip(2).ToArray();
    var json = args.Any(arg => arg.Equals("--json", StringComparison.OrdinalIgnoreCase)
        || arg.Equals("-json", StringComparison.OrdinalIgnoreCase)
        || arg.Equals("/json", StringComparison.OrdinalIgnoreCase));
    var apply = args.Any(arg => arg.Equals("--apply", StringComparison.OrdinalIgnoreCase)
        || arg.Equals("-apply", StringComparison.OrdinalIgnoreCase)
        || arg.Equals("/apply", StringComparison.OrdinalIgnoreCase));
    var positional = args.Where(arg => !arg.StartsWith("-") && !arg.StartsWith("/")).ToList();
    string langCode = "en-US";
    string sourceDir = Directory.GetCurrentDirectory();

    Print();
    Print($"Report missing translation keys.");
    if (apply)
        Print("Apply mode enabled: missing keys will be written into localization JSON files.");
    Print();

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
        if (json)
        {
            var missingSourcePayload = new { ok = false, sourceDir, languageCode = langCode, error = "Source directory does not exist." };
            Print(JsonConvert.SerializeObject(missingSourcePayload, Formatting.Indented));
        }
        else
        {
            Print($"Source directory does not exist: {sourceDir}");
        }
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
    {
        filesUpdated = JBeamFile.TranslationKey.ApplyMissingKeys(langCode, sourceDir, distinctKeys);
    }

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

    if (json)
    {
        Print(JsonConvert.SerializeObject(resultPayload, Formatting.Indented));
        Environment.ExitCode = resultPayload.ok ? 0 : 1;
        return;
    }

    Print($"Source: {sourceDir}");
    Print($"Language Code: {resultPayload.languageCode}");
    Print();

    Print("Loading JBeam files...");
    InitJBeam(sourceDir);
    Print();

    Print($"Missing Translation Keys in '{resultPayload.languageCode}':");
    foreach (var (key, value) in distinctKeys)
    {
        Print($"   \"{key}\": \"{value}\",");
    }

    if (apply)
    {
        Print();
        if (filesUpdated.Count == 0)
            Print("No localization files were updated.");
        else
        {
            Print($"Updated {filesUpdated.Count} localization file(s):");
            foreach (var file in filesUpdated)
                Print($"   {file}");
        }
    }
}

void Run_Verify()
{
    var args = ParseCommandArgs(Environment.GetCommandLineArgs().Skip(2).ToArray());
    var sourcePath = args.Positional.FirstOrDefault() ?? Directory.GetCurrentDirectory();
    var json = args.Json;
    var strict = args.Strict;

    if (!File.Exists(sourcePath) && !Directory.Exists(sourcePath))
    {
        var verifyErrorPayload = new {
            ok = false,
            strict,
            path = sourcePath,
            error = "Path does not exist."
        };

        if (json) Print(JsonConvert.SerializeObject(verifyErrorPayload, Formatting.Indented));
        else
        {
            Print($"Source path does not exist: {sourcePath}");
        }

        Environment.ExitCode = 1;
        return;
    }

    var files = File.Exists(sourcePath)
        ? new[] { Path.GetFullPath(sourcePath) }
        : JBeamFile.EnumerateFiles(Path.GetFullPath(sourcePath)).ToArray();

    if (files.Length == 0)
    {
        var verifyEmptyPayload = new {
            ok = false,
            strict,
            path = Path.GetFullPath(sourcePath),
            fileCount = 0,
            error = "No .jbeam files were found."
        };

        if (json) Print(JsonConvert.SerializeObject(verifyEmptyPayload, Formatting.Indented));
        else Print("No .jbeam files were found.");

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
        var fileOk = loaded && (!strict || warnings.Count == 0);
        if (!fileOk)
            ok = false;

        results.Add(new
        {
            file = jbeam.FilePath,
            ok = fileOk,
            parts = jbeam.Parts.Count,
            sanity = jbeam.Sanity,
            warnings = warnings,
            translationKeyCount = jbeam.GetTranslationKeys().Count()
        });
    }

    if (strict)
        ok = results.All(r => (bool)r.GetType().GetProperty("ok")!.GetValue(r)!);

    var verifyPayload = new
    {
        ok,
        strict,
        fileCount = files.Length,
        resultCount = results.Count,
        results,
        warnings = results.SelectMany(r => ((IEnumerable<string>)r.GetType().GetProperty("warnings")!.GetValue(r)!)).ToList()
    };

    if (json)
    {
        Print(JsonConvert.SerializeObject(verifyPayload, Formatting.Indented));
    }
    else
    {
        Print();
        Print($"Verify JBeam files: {(ok ? "OK" : "FAILED")}");
        Print($"Path: {Path.GetFullPath(sourcePath)}");
        Print($"Files: {files.Length}");
        Print();

        foreach (var result in results)
        {
            var file = (string)result.GetType().GetProperty("file")!.GetValue(result)!;
            var fileOk = (bool)result.GetType().GetProperty("ok")!.GetValue(result)!;
            var partCount = (int)result.GetType().GetProperty("parts")!.GetValue(result)!;
            var warningList = (IEnumerable<string>)result.GetType().GetProperty("warnings")!.GetValue(result)!;
            var warningCount = warningList.Count();
            var sanity = (bool)result.GetType().GetProperty("sanity")!.GetValue(result)!;

            Print($"{Path.GetRelativePath(Path.GetDirectoryName(Path.GetFullPath(sourcePath)) ?? Path.GetFullPath(sourcePath), file)}: {(fileOk ? "OK" : "FAILED")} [parts={partCount}, sanity={sanity}, warnings={warningCount}]");
            foreach (var warning in warningList)
                Print($"   - {warning}");
        }
    }

    Environment.ExitCode = ok ? 0 : 1;
}

void Run_Info()
{
    var args = ParseCommandArgs(Environment.GetCommandLineArgs().Skip(2).ToArray());
    var sourcePath = args.Positional.FirstOrDefault() ?? Directory.GetCurrentDirectory();
    var json = args.Json;

    var targetPath = sourcePath;
    if (Directory.Exists(targetPath))
    {
        var firstFile = JBeamFile.EnumerateFiles(Path.GetFullPath(targetPath)).FirstOrDefault();
        if (firstFile == null)
        {
            var infoDirectoryPayload = new { ok = false, file = targetPath, error = "No .jbeam files found in directory." };
            if (json) Print(JsonConvert.SerializeObject(infoDirectoryPayload, Formatting.Indented));
            else Print($"No .jbeam files found in directory: {targetPath}");
            Environment.ExitCode = 1;
            return;
        }

        targetPath = firstFile;
    }

    if (!File.Exists(targetPath))
    {
        var infoMissingPayload = new { ok = false, file = targetPath, error = "File does not exist." };
        if (json) Print(JsonConvert.SerializeObject(infoMissingPayload, Formatting.Indented));
        else Print($"File does not exist: {targetPath}");
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
        warnings = warnings,
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

    if (json)
    {
        Print(JsonConvert.SerializeObject(infoPayload, Formatting.Indented));
    }
    else
    {
        Print();
        Print($"JBeam Info: {jbeam.FilePath}");
        Print($"Loaded: {(loaded ? "yes" : "no")}");
        Print($"Parts: {jbeam.Parts.Count}");
        Print($"Sanity: {jbeam.Sanity}");
        Print($"Warnings: {warnings.Count}");

        foreach (var warning in warnings)
            Print($"   - {warning}");

        if (sectionCounts.Count > 0)
        {
            Print("Sections:");
            foreach (var kv in sectionCounts)
                Print($"   - {kv.Key}: {kv.Value}");
        }

        foreach (var part in infoPayload.partsSummary)
        {
            Print($"Part: {part.id} | Name: {part.name} | Value: {part.value}");
            if (part.slotType.Any())
                Print($"   Slot types: {string.Join(", ", part.slotType)}");
            if (part.slots.Any())
                Print($"   Slots: {string.Join(", ", part.slots)}");
        }
    }

    Environment.ExitCode = loaded ? 0 : 1;
}

object? DescribeNode(Node node)
{
    if (node is ValueNode valueNode)
        return new { type = nameof(ValueNode), value = valueNode.Value };

    if (node is ArrayNode arrayNode)
        return new { type = nameof(ArrayNode), count = arrayNode.Items.Count, items = arrayNode.Items.Select(DescribeNode).ToList() };

    if (node is ObjectNode objectNode)
    {
        return new
        {
            type = nameof(ObjectNode),
            propertyCount = objectNode.Properties.Count,
            properties = objectNode.Properties.Select(entry => new
            {
                key = entry.Name,
                keyType = entry.Key.GetType().Name,
                valueType = entry.Value.GetType().Name,
                value = DescribeNode(entry.Value)
            }).ToList()
        };
    }

    if (node is CommentNode commentNode)
        return new { type = nameof(CommentNode), isBlock = commentNode.IsBlock, comment = commentNode.Comment };

    if (node is EmptyLineNode emptyLineNode)
        return new { type = nameof(EmptyLineNode), count = emptyLineNode.Count };

    return new { type = node.GetType().Name };
}

void Run_Ast()
{
    var args = ParseCommandArgs(Environment.GetCommandLineArgs().Skip(2).ToArray());
    var sourcePath = args.Positional.FirstOrDefault() ?? Directory.GetCurrentDirectory();
    var json = args.Json;

    if (!File.Exists(sourcePath) && !Directory.Exists(sourcePath))
    {
        var payload = new { ok = false, path = sourcePath, error = "Path does not exist." };
        if (json) Print(JsonConvert.SerializeObject(payload, Formatting.Indented));
        else Print($"Path does not exist: {sourcePath}");
        Environment.ExitCode = 1;
        return;
    }

    var files = File.Exists(sourcePath)
        ? new[] { Path.GetFullPath(sourcePath) }
        : JBeamFile.EnumerateFiles(Path.GetFullPath(sourcePath)).ToArray();

    if (files.Length == 0)
    {
        var payload = new { ok = false, path = Path.GetFullPath(sourcePath), error = "No .jbeam files were found." };
        if (json) Print(JsonConvert.SerializeObject(payload, Formatting.Indented));
        else Print("No .jbeam files were found.");
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
            node = DescribeNode(part.Object())
        }).ToList();

        var entry = new
        {
            ok = loaded,
            file = jbeam.FilePath,
            partCount = jbeam.Parts.Count,
            sanity = jbeam.Sanity,
            warnings = jbeam.GetWarnings().ToList(),
            root = DescribeNode(jbeam.Root),
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

    if (json)
    {
        Print(JsonConvert.SerializeObject(payloadSummary, Formatting.Indented));
    }
    else
    {
        Print();
        Print("JBeam AST summary");
        Print();

        foreach (var summary in summaries)
        {
            var file = (string)summary.GetType().GetProperty("file")!.GetValue(summary)!;
            var partCount = (int)summary.GetType().GetProperty("partCount")!.GetValue(summary)!;
            var sanity = (bool)summary.GetType().GetProperty("sanity")!.GetValue(summary)!;
            var warnings = (List<string>)summary.GetType().GetProperty("warnings")!.GetValue(summary)!;

            Print($"File: {file}");
            Print($"  Parts: {partCount}");
            Print($"  Sanity: {sanity}");
            if (warnings.Count > 0)
            {
                Print("  Warnings:");
                foreach (var warning in warnings)
                    Print($"    - {warning}");
            }

            var root = summary.GetType().GetProperty("root")!.GetValue(summary)!;
            var rootText = JsonConvert.SerializeObject(root, Formatting.None);
            Print($"  Root: {rootText}");
        }
    }

    Environment.ExitCode = payloadSummary.ok ? 0 : 1;
}

void Run_Dump()
{
    Print();
    Print($"Dump JBeam files into a folder.");
    Print();

    var args = Environment.GetCommandLineArgs();
    string sourceDir = args.Length > 2 ? args[2] : Directory.GetCurrentDirectory();
    string targetDir = args.Length > 3 ? args[3] : Directory.GetCurrentDirectory();

    if (!Directory.Exists(sourceDir))
    {
        Print($"Source directory does not exist: {sourceDir}");
        return;
    }

    sourceDir = Path.GetFullPath(sourceDir);
    targetDir = Path.GetFullPath(targetDir);
    Print($"Source: {sourceDir}");
    Print($"Target: {targetDir}");
    Print();

    Print("Loading JBeam files...");
    InitJBeam(sourceDir);
    var jbeams = JBeamFile.LoadFiles(sourceDir);
    Print();

    foreach (var jbeam in jbeams)
    {
        var relativePath = Path.GetRelativePath(sourceDir, jbeam.FilePath);
        Print($"File: {relativePath}");

        var content = jbeam.Root.ToString();
        var newFile = Path.GetFullPath(Path.Combine(targetDir, relativePath));
        Directory.CreateDirectory(Path.GetDirectoryName(newFile)!);
        File.WriteAllText(newFile, content);
    }
}

void Run_Slots2()
{
    Print();
    Print($"Convert to Slots2 format.");
    Print();

    var args = Environment.GetCommandLineArgs();
    string sourceDir = args.Length > 2 ? args[2] : Directory.GetCurrentDirectory();
    string targetDir = args.Length > 3 ? args[3] : Directory.GetCurrentDirectory();

    if (!Directory.Exists(sourceDir))
    {
        Print($"Source directory does not exist: {sourceDir}");
        return;
    }

    sourceDir = Path.GetFullPath(sourceDir);
    targetDir = Path.GetFullPath(targetDir);
    Print($"Source: {sourceDir}");
    Print($"Target: {targetDir}");
    Print();

    Print("Loading JBeam files...");
    InitJBeam(sourceDir);
    var jbeams = JBeamFile.LoadFiles(sourceDir);
    Print();
    
    foreach (var jbeam in jbeams)
    {
        var relativePath = Path.GetRelativePath(sourceDir, jbeam.FilePath);

        var convertedParts = jbeam.ConvertFormat_Slots2();
        if (convertedParts.Count == 0)
            continue;
            
        Print($"File: {relativePath}");
        foreach (var partName in convertedParts)
        {
            Print($"   Part: {partName}");
        }

        var newFile = Path.GetFullPath(Path.Combine(targetDir, relativePath));
        Directory.CreateDirectory(Path.GetDirectoryName(newFile)!);
        File.WriteAllText(newFile, jbeam.FileContent);
    }
}

void Run_PriceList()
{
    Print();
    Print($"Part price list.");
    Print();

    bool IsSwitch(string arg) => arg.StartsWith("-") || arg.StartsWith("/");

    var args = Environment.GetCommandLineArgs();
    var switches = args.Where(IsSwitch).ToList();
    args = args.Where(arg => !IsSwitch(arg)).ToArray();
    string sourceDir = args.Length > 2 ? args[2] : Directory.GetCurrentDirectory();
    string? targetFile = args.Length > 3 ? args[3] : null;

    bool showTree = switches.Any(s => s.Equals("-tree", StringComparison.OrdinalIgnoreCase) || s.Equals("/tree", StringComparison.OrdinalIgnoreCase));

    if (!Directory.Exists(sourceDir))
    {
        Print($"Source directory does not exist: {sourceDir}");
        return;
    }

    sourceDir = Path.GetFullPath(sourceDir);
    Print($"Source: {sourceDir}");
    if (targetFile != null) 
    {
        targetFile = Path.GetFullPath(targetFile);
        if (Directory.Exists(targetFile))
            targetFile = Path.Combine(targetFile, "pricelist.txt");
        Print($"Target: {targetFile}");
    }

    Print();

    Print("Loading JBeam files...");
    InitJBeam(sourceDir);
    var jbeams = JBeamFile.LoadFiles(sourceDir);
    Print();
    
    string GetTree()
    {
        JBeamFile.PartsTree partsTree = new(jbeams);
        JBeamFile.PartsTree.TreePrinter.Config config = new()
        {
            LabelCallback = (node, isRef) =>
            {
                if (isRef) return "";
                else
                {
                    if (node.IsSlot)
                        return $"[{node.Slot?.Name}]" ?? "[n/a]";

                    var info = node.Part?.Information;
                    if (info == null)
                        return $"<{node.Part?.Identifier}>" ?? "<n/a>";

                    var value = string.IsNullOrEmpty(info.Value) ? "n/a" : $"${info.Value}";
                    return $" {value,-5} {info.Name}";
                }
            }
        };
        return new JBeamFile.PartsTree.TreePrinter(config).Print(partsTree.Roots);
    }
    string GetList()
    {
        StringBuilder sb = new();
        foreach (var jbeam in jbeams)
        {
            var relativePath = Path.GetRelativePath(sourceDir, jbeam.FilePath);

            sb.AppendLine($"File: {relativePath}");
            foreach (var part in jbeam.Parts)
            {
                var info = part.Information;
                if (info == null)
                {
                    sb.AppendLine($"   Part: {part.Identifier} - No information section.");
                    continue;
                }

                var authors = string.Join(", ", info.Authors);
                var value = string.IsNullOrEmpty(info.Value) ? "<n/a>" : $"${info.Value}";
                sb.AppendLine($"    {value,-5} - Part: {info.Name}");
            }            
        }

        return sb.ToString();
    }

    string content = showTree ? GetTree() : GetList();
    if (targetFile == null)
    {
        Print(content);
        
    }
    else
    {
        Print("Writing to file...");
        File.WriteAllText(targetFile, content);
    }
}

void Run()
{
    var args = Environment.GetCommandLineArgs();
    string command = args.Length > 1 ? args[1] : "";
    
    switch (command.ToLower().Trim())
    {
        case "keys":
            Run_Keys();
            break;

        case "verify":
            Run_Verify();
            break;

        case "info":
            Run_Info();
            break;

        case "ast":
            Run_Ast();
            break;

        case "slots2":
            Run_Slots2();
            break;

        case "dump":
            Run_Dump();
            break;

        case "pricelist":
            Run_PriceList();
            break;

        default:
            Print("Usage: JBeamTool.exe <command> <args>");
            Print();
            Print("Commands:");
            Print("   keys [lang] <sourceDir> - Show missing translation keys.");
            Print("   verify [--json] [--strict] <sourceDir|file> - Validate JBeam files and report warnings.");
            Print("   info [--json] <file.jbeam|directory> - Show compact file metadata and warnings.");
            Print("   ast [--json] <file.jbeam|directory> - Expose the parsed JBeam AST in an AI-friendly structure.");
            Print("   slots2 <sourceDir> <targetDir> - Convert JBeam files to Slots2 format.");
            Print("   dump <sourceDir> <targetDir> - Dump parsed JBeam files into the given folder.");
            Print("   pricelist [-tree] <sourceDir> [targetFile] - Show part price list.");
            break;
    }

}

Run();