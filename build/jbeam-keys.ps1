$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$binary = Join-Path $root 'JBeamTool\bin\Debug\net10.0\JBeamTool.exe'

if (-not (Test-Path $binary)) {
    throw "JBeamTool binary not found at: $binary. Build the project first."
}

& $binary keys $args --json --apply

exit $LASTEXITCODE