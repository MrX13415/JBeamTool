$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$binary = Join-Path $root 'JBeamTool\bin\Debug\net10.0\win-x64\publish\JBeamTool.exe'

if (-not (Test-Path $binary)) {
    throw "JBeamTool binary not found at: $binary. Build the project first with 'dotnet publish -c Debug'."
}

$argsList = @($args)
if ($argsList.Count -eq 0) {
    & $binary verify --json --strict
}
else {
    & $binary verify $argsList --json --strict
}

exit $LASTEXITCODE
