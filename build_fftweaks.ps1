param(
    [string]$ProjectPath = "",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$OutDir = ""
)

$ErrorActionPreference = "Stop"

function Write-Info($msg) { Write-Host $msg }
function Write-Fail($msg) { Write-Error $msg }

$scriptRoot = $PSScriptRoot
$defaultProject = Join-Path $scriptRoot "Outhard\BepInEx\plugins\FFTweaks-Outhard\FFTweaks-Outhard.csproj"

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = $defaultProject
}

if (-not (Test-Path $ProjectPath)) {
    Write-Fail "Project file not found: $ProjectPath"
}

$projectDir = Split-Path -Parent $ProjectPath
if ([string]::IsNullOrWhiteSpace($OutDir)) {
    $OutDir = $projectDir
}

$dotnetCmd = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnetCmd) {
    Write-Fail "dotnet not found on PATH. Install the .NET SDK to continue."
}

Write-Info "Project: $ProjectPath"
Write-Info "Configuration: $Configuration"
Write-Info "Output: $OutDir"

Write-Info "Building..."
& $dotnetCmd.Source build "$ProjectPath" -c $Configuration "/p:OutputPath=$OutDir" "/p:GenerateFullPaths=true"
if ($LASTEXITCODE -ne 0) {
    Write-Fail "Build failed."
}

$expectedDll = Join-Path $OutDir "FFTweaks-Outhard.dll"
if (-not (Test-Path $expectedDll)) {
    Write-Fail "Built DLL not found at $expectedDll"
}

Write-Info "Build complete: $expectedDll"
