param(
    [Parameter(Mandatory = $true)]
    [string]$RimWorldDir,

    [Parameter(Mandatory = $true)]
    [string]$HarmonyDll
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "Source\SharedColonyClient\SharedColonyClient.csproj"
$output = Join-Path $repoRoot "1.6\Assemblies"

if (-not (Test-Path (Join-Path $RimWorldDir "RimWorldWin64_Data\Managed\Assembly-CSharp.dll"))) {
    throw "RimWorldDir does not contain RimWorldWin64_Data\Managed\Assembly-CSharp.dll"
}

if (-not (Test-Path $HarmonyDll)) {
    throw "HarmonyDll was not found: $HarmonyDll"
}

dotnet build $project -c Release `
    -p:RimWorldDir="$RimWorldDir" `
    -p:HarmonyDll="$HarmonyDll"

New-Item -ItemType Directory -Force -Path $output | Out-Null
Copy-Item (Join-Path $repoRoot "Source\SharedColonyClient\bin\Release\net472\RWTSharedColony.dll") $output -Force
Write-Host "Built 1.6\Assemblies\RWTSharedColony.dll"
