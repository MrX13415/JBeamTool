# JBeamTool

A powerful command-line utility for validating, inspecting, and managing BeamNG JBeam files. This tool provides structured validation, localization key checking, and AST inspection for JBeam vehicle definition files.

## Overview

JBeamTool is a C# .NET application designed for developers working with BeamNG vehicle mods. It offers:

- **File Validation**: Verify JBeam syntax and structural integrity
- **Localization Management**: Find and update missing translation keys
- **AST Inspection**: Access the parsed syntax tree programmatically
- **JSON Output**: Automation-friendly structured output format
- **Strict Mode**: Treat warnings as errors for quality assurance

## Features

### Validate JBeam Files
Comprehensive validation of JBeam files or entire directories with detailed error reporting.

### Localization Key Management
Automatically detect missing translation keys and optionally update locale files.

### JBeam AST Analysis
Access the parsed abstract syntax tree for detailed inspection and analysis.

### Automation-Ready Output
JSON format output for integration with CI/CD pipelines and other automation tools.

## Requirements

- .NET 10.0 or later
- Newtonsoft.Json 13.0.4 or later

## Installation

### Building from Source

```bash
# Clone the repository
git clone https://github.com/MrX13415/JBeamTool.git
cd JBeamTool

# Build the project
dotnet build JBeamTool/JBeamTool.csproj

# Or build in Release mode
dotnet build -c Release JBeamTool/JBeamTool.csproj
```

### Output Location

The compiled executable will be located at:
```
JBeamTool/bin/Debug/net10.0/JBeamTool.exe     (Debug build)
JBeamTool/bin/Release/net10.0/JBeamTool.exe   (Release build)
```

## Usage

### Command: `verify`

Validate a JBeam file or directory for syntax errors and structural integrity.

```bash
JBeamTool.exe verify <path> [options]
```

**Options:**
- `--json` - Output results in JSON format for automation
- `--strict` - Treat warnings as errors (exit code 1 if warnings found)

**Examples:**

```powershell
# Validate a single file
.\JBeamTool\bin\Debug\net10.0\JBeamTool.exe verify "C:\mods\vehicle\part.jbeam" --json --strict

# Validate an entire directory
.\JBeamTool\bin\Debug\net10.0\JBeamTool.exe verify "C:\mods\vehicle\parts" --json --strict
```

**Output:**
- File count
- Number of parts
- Warning count and details
- Sanity status
- Overall pass/fail status

---

### Command: `info`

Inspect a single JBeam file and retrieve its metadata without full validation.

```bash
JBeamTool.exe info <file.jbeam> [options]
```

**Options:**
- `--json` - Output results in JSON format

**Examples:**

```powershell
.\JBeamTool\bin\Debug\net10.0\JBeamTool.exe info "C:\mods\vehicle\engine.jbeam" --json
```

**Output:**
- File path
- Load status
- Part count and names
- Sanity status
- Warnings
- Section information
- Per-part metadata

---

### Command: `keys`

Find and manage missing translation keys in JBeam files.

```bash
JBeamTool.exe keys [options] [languageCode] <sourceDir>
```

**Options:**
- `--json` - Output results in JSON format
- `--apply` - Automatically write missing keys to locale JSON files
- `languageCode` - Language code (e.g., `en-US`, `de-DE`). Defaults to `en-US`

**Examples:**

```powershell
# Report missing keys for English
.\JBeamTool\bin\Debug\net10.0\JBeamTool.exe keys --json "C:\mods\vehicle"

# Report and apply missing keys for German
.\JBeamTool\bin\Debug\net10.0\JBeamTool.exe keys --json --apply de-DE "C:\mods\vehicle"

# Report missing keys for a specific language
.\JBeamTool\bin\Debug\net10.0\JBeamTool.exe keys en-US "C:\mods\vehicle\parts"
```

**Workflow:**

1. Run the command with your mod directory
2. Review the output for missing keys
3. Use `--apply` flag to automatically update locale files
4. Verify the changes with `verify` command

**Behavior:**
- Defaults to `en-US` if no language code specified
- Creates `locales/en-US/generated_missing_translations.json` if no locale file exists
- Skips dynamic template values like `{{ ... | translate }}`
- Writes English values as placeholders for new locale entries

---

### Command: `ast`

Expose the parsed JBeam abstract syntax tree (AST) for detailed structural analysis.

```bash
JBeamTool.exe ast <path> [options]
```

**Options:**
- `--json` - Output AST in JSON format (recommended)

**Examples:**

```powershell
.\JBeamTool\bin\Debug\net10.0\JBeamTool.exe ast "C:\mods\vehicle\part.jbeam" --json
```

**Output:**
- Full parsed object graph
- Nested node structure
- Raw AST representation

This command is useful when you need programmatic access to the JBeam structure.

---

## Common Workflows

### Validate Before and After Edits

This is the **recommended workflow** for all JBeam modifications:

```powershell
# 1. Validate before editing
.\JBeamTool\bin\Debug\net10.0\JBeamTool.exe verify "C:\mods\vehicle\part.jbeam" --json --strict

# 2. Make your changes

# 3. Validate after editing
.\JBeamTool\bin\Debug\net10.0\JBeamTool.exe verify "C:\mods\vehicle\part.jbeam" --json --strict

# 4. Update localization
.\JBeamTool\bin\Debug\net10.0\JBeamTool.exe keys --json --apply en-US "C:\mods\vehicle"
```

### Create a Wrapper Script

For convenience, create a wrapper script in your repository:

**File: `build/jbeam-verify.ps1`**

```powershell
# build/jbeam-verify.ps1
param(
    [Parameter(ValueFromRemainingArguments=$true)]
    $Arguments
)

$root = Split-Path -Parent $PSScriptRoot
$binary = Join-Path $root "JBeamTool\bin\Debug\net10.0\JBeamTool.exe"

& $binary verify $Arguments --json --strict
```

Then use it like:

```powershell
./build/jbeam-verify.ps1 "C:\path\to\part.jbeam"
```

### CI/CD Integration

Since JBeamTool outputs JSON by default with `--json`, it integrates easily into automation:

```bash
# Check exit code for pass/fail
JBeamTool.exe verify "vehicle/" --json --strict
if ($LASTEXITCODE -ne 0) {
    Write-Host "JBeam validation failed!"
    exit 1
}
```

---

## Output Interpretation

### Exit Codes

- `0` - Success (validation passed or no issues found)
- `1` - Failure (validation failed or warnings found in strict mode)

### JSON Response Structure

All commands support `--json` output with the following structure:

```json
{
  "ok": true,
  "sourceDir": "C:\\mods\\vehicle",
  "fileCount": 5,
  "partCount": 42,
  "warnings": [],
  "sanity": true
}
```

**Fields:**
- `ok` - Boolean indicating overall success
- `sourceDir` - Input directory or file path
- `fileCount` - Number of files processed
- `partCount` - Total parts found
- `warnings` - Array of warning messages
- `sanity` - Structural integrity status

### Strict Mode

When using `--strict`:
- **Warnings are treated as errors**
- Exit code is 1 if any warnings are found
- Useful for quality gates in automation

---

## Project Structure

```
JBeamTool/
├── JBeamTool/
│   ├── src/
│   │   ├── Program.cs          # Main CLI entry point
│   │   ├── BeamNG/
│   │   │   ├── App/            # BeamNG application utilities
│   │   │   └── JBeam/          # JBeam file parsing and validation
│   │   └── Steam.cs            # Steam integration
│   ├── JBeamTool.csproj        # Project configuration
│   ├── AGENTS.md               # Agent workflow documentation
│   └── Properties/             # Project properties
├── build/                      # Build output and scripts
├── .gitignore                  # Git ignore rules
├── .vscode/                    # VS Code configuration
└── README.md                   # This file
```

---

## Development

### Prerequisites

- .NET 10.0 SDK
- Visual Studio Code or Visual Studio 2022+

### Building

```bash
# Debug build
dotnet build JBeamTool/JBeamTool.csproj

# Release build
dotnet build -c Release JBeamTool/JBeamTool.csproj
```

### Running

```bash
# From source
dotnet run --project JBeamTool/JBeamTool.csproj -- verify "path/to/file.jbeam" --json --strict

# From built binary
.\JBeamTool\bin\Debug\net10.0\JBeamTool.exe verify "path/to/file.jbeam" --json --strict
```

### Dependencies

- **Newtonsoft.Json** (13.0.4) - JSON serialization and deserialization

---

## Documentation

For detailed agent workflow instructions, see [AGENTS.md](JBeamTool/AGENTS.md).

---

## License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

---

## Troubleshooting

### "Source directory does not exist"

Verify the path you're passing to the tool:
```powershell
Test-Path "C:\path\to\directory"
```

### "JBeam validation failed" but file looks correct

Use strict mode and check for warnings:
```powershell
.\JBeamTool\bin\Debug\net10.0\JBeamTool.exe verify "file.jbeam" --json --strict
```

### Missing localization keys not found

Ensure the language code is correct and the mod directory structure is valid:
```powershell
.\JBeamTool\bin\Debug\net10.0\JBeamTool.exe keys en-US "C:\mods\vehicle" --json
```

---

## Contributing

For contributions and development workflows, refer to the [AGENTS.md](JBeamTool/AGENTS.md) documentation.

---

## Support

For issues, questions, or feature requests, please use the GitHub issues tracker for this repository.
