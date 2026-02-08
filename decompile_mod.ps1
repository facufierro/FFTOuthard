param(
    [string]$ProfileName = "Outhard",
    [string]$ModName = "IggyTheMad-AlternateStart",
    [string]$DllName = "AlternateStart.dll",
    [string]$OutDir = "",
    [string]$OutwardManagedPath = "D:\\Games\\Steam\\steamapps\\common\\Outward\\Outward_Defed\\Outward Definitive Edition_Data\\Managed"
)

$ErrorActionPreference = "Stop"

function Write-Info($msg) { Write-Host $msg }
function Write-Warn($msg) { Write-Warning $msg }
function Write-Fail($msg) { Write-Error $msg }

$scriptRoot = $PSScriptRoot
$profilesRoot = $scriptRoot

if (-not (Test-Path $profilesRoot)) {
    Write-Fail "Profiles root not found: $profilesRoot"
}

$profilePath = Join-Path $profilesRoot $ProfileName
if (-not (Test-Path $profilePath)) {
    Write-Fail "Profile not found: $profilePath"
}

$pluginPath = Join-Path $profilePath (Join-Path "BepInEx" (Join-Path "plugins" $ModName))
if (-not (Test-Path $pluginPath)) {
    Write-Fail "Mod folder not found: $pluginPath"
}

$dllPath = Join-Path $pluginPath $DllName
if (-not (Test-Path $dllPath)) {
    Write-Fail "DLL not found: $dllPath"
}

if (-not (Test-Path $OutwardManagedPath)) {
    Write-Fail "Outward Managed path not found: $OutwardManagedPath"
}

if ([string]::IsNullOrWhiteSpace($OutDir)) {
    $OutDir = Join-Path $pluginPath "decompiled"
}

if (-not (Test-Path $OutDir)) {
    New-Item -ItemType Directory -Path $OutDir -Force | Out-Null
}

Write-Info "Using profile: $ProfileName"
Write-Info "Mod folder: $pluginPath"
Write-Info "DLL: $dllPath"
Write-Info "Output: $OutDir"
Write-Info "References: $OutwardManagedPath"

$dotnetCmd = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnetCmd) {
    Write-Fail "dotnet not found on PATH. Install the .NET SDK to continue."
}

$toolList = & dotnet tool list -g
if ($LASTEXITCODE -ne 0) {
    Write-Fail "Unable to list dotnet tools."
}

$ilspyInstalled = $toolList | Select-String -Pattern "ilspycmd"
if (-not $ilspyInstalled) {
    Write-Info "Installing ilspycmd dotnet tool..."
    & dotnet tool install -g ilspycmd
    if ($LASTEXITCODE -ne 0) {
        Write-Fail "Failed to install ilspycmd."
    }
}

Write-Info "Decompiling..."
& ilspycmd -p -o "$OutDir" -r "$OutwardManagedPath" "$dllPath"
if ($LASTEXITCODE -ne 0) {
    Write-Fail "Decompilation failed."
}

Write-Info "Decompile complete."
