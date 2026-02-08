param(
    [string]$ProfileName = "Outhard",
    [string]$ModName = "IggyTheMad-AlternateStart",
    [string]$CsprojPath = "",
    [string]$DllName = "AlternateStart.dll",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$OutDir = ""
)

$ErrorActionPreference = "Stop"

function Write-Info($msg) { Write-Host $msg }
function Write-Warn($msg) { Write-Warning $msg }
function Write-Fail($msg) { Write-Error $msg }

$scriptRoot = $PSScriptRoot
$profilesRoot = $scriptRoot

$profilePath = Join-Path $profilesRoot $ProfileName
if (-not (Test-Path $profilePath)) {
    Write-Fail "Profile not found: $profilePath"
}

$pluginPath = Join-Path $profilePath (Join-Path "BepInEx" (Join-Path "plugins" $ModName))
if (-not (Test-Path $pluginPath)) {
    Write-Fail "Mod folder not found: $pluginPath"
}

if ([string]::IsNullOrWhiteSpace($CsprojPath)) {
    $defaultProject = Join-Path $pluginPath (Join-Path "decompiled" "AlternateStart.csproj")
    $CsprojPath = $defaultProject
}

if (-not (Test-Path $CsprojPath)) {
    Write-Fail "Project file not found: $CsprojPath"
}

if ([string]::IsNullOrWhiteSpace($OutDir)) {
    $OutDir = $pluginPath
}

if (-not (Test-Path $OutDir)) {
    New-Item -ItemType Directory -Path $OutDir -Force | Out-Null
}

$msbuildCmd = Get-Command msbuild -ErrorAction SilentlyContinue
if (-not $msbuildCmd) {
    Write-Fail "msbuild not found on PATH. Install Visual Studio Build Tools or add msbuild to PATH."
}

Write-Info "Using profile: $ProfileName"
Write-Info "Mod folder: $pluginPath"
Write-Info "Project: $CsprojPath"
Write-Info "Configuration: $Configuration"
Write-Info "Output: $OutDir"

Write-Info "Building..."
& msbuild "$CsprojPath" /t:Build /p:Configuration=$Configuration /p:OutputPath="$OutDir" /p:GenerateFullPaths=true
if ($LASTEXITCODE -ne 0) {
    Write-Fail "Build failed."
}

$builtDllPath = Join-Path $OutDir $DllName
if (-not (Test-Path $builtDllPath)) {
    Write-Fail "Built DLL not found at $builtDllPath"
}

$targetDllPath = Join-Path $pluginPath $DllName
Copy-Item -Path $builtDllPath -Destination $targetDllPath -Force
Write-Info "Replaced DLL: $targetDllPath"

Write-Info "Build complete."
