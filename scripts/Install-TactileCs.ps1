#Requires -Version 5.1
<#
.SYNOPSIS
    Downloads and extracts the TactileCs NuGet package to a local directory.

.DESCRIPTION
    Fetches the specified version (or latest stable) of TactileCs from NuGet.org,
    extracts the DLLs for the requested target framework, and places them in the
    installation directory.

.PARAMETER Version
    The NuGet package version to install. Use "latest" (default) to install the
    most recent stable release.

.PARAMETER TargetFramework
    The target framework moniker (TFM) whose DLLs should be extracted
    (e.g. "net8.0"). Defaults to "net8.0".

.PARAMETER InstallDir
    The directory into which the DLLs are placed. Defaults to "./lib/TactileCs".
    The directory is created if it does not already exist.

.EXAMPLE
    .\Install-TactileCs.ps1

    Installs the latest stable release to ./lib/TactileCs for net8.0.

.EXAMPLE
    .\Install-TactileCs.ps1 -Version 1.0.0 -TargetFramework net8.0 -InstallDir C:\Libs\TactileCs

    Installs version 1.0.0 to C:\Libs\TactileCs.
#>

[CmdletBinding()]
param(
    [string] $Version       = 'latest',
    [string] $TargetFramework = 'net8.0',
    [string] $InstallDir    = './lib/TactileCs'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ── 1. Resolve the concrete version ────────────────────────────────────────
if ($Version -eq 'latest') {
    Write-Verbose "Querying NuGet for the latest stable version of TactileCs..."
    $indexUrl  = 'https://api.nuget.org/v3-flatcontainer/tactilecs/index.json'
    $indexData = Invoke-RestMethod -Uri $indexUrl -UseBasicParsing
    $Version   = $indexData.versions | Select-Object -Last 1
    Write-Host "Resolved latest version: $Version"
} else {
    Write-Host "Using specified version: $Version"
}

# ── 2. Download the .nupkg ──────────────────────────────────────────────────
$nupkgUrl  = "https://api.nuget.org/v3-flatcontainer/tactilecs/$Version/tactilecs.$Version.nupkg"
$tmpNupkg  = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "tactilecs.$Version.nupkg")

Write-Host "Downloading $nupkgUrl ..."
Invoke-WebRequest -Uri $nupkgUrl -OutFile $tmpNupkg -UseBasicParsing

# ── 3. Extract DLLs for the requested TFM ──────────────────────────────────
$targetPath = "lib/$TargetFramework"
$absInstall = [System.IO.Path]::GetFullPath($InstallDir)

if (-not (Test-Path $absInstall)) {
    New-Item -ItemType Directory -Path $absInstall | Out-Null
}

Write-Host "Extracting '$targetPath/*.dll' to '$absInstall' ..."

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($tmpNupkg)
try {
    $extracted = 0
    foreach ($entry in $zip.Entries) {
        if ($entry.FullName -like "$targetPath/*.dll") {
            $dest = [System.IO.Path]::Combine($absInstall, $entry.Name)
            [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $dest, $true)
            Write-Verbose "  Extracted: $($entry.Name)"
            $extracted++
        }
    }
    if ($extracted -eq 0) {
        Write-Warning "No DLLs found for framework '$TargetFramework' inside the package."
    } else {
        Write-Host "Installed $extracted DLL(s) to '$absInstall'."
    }
}
finally {
    $zip.Dispose()
    Remove-Item $tmpNupkg -Force -ErrorAction SilentlyContinue
}

# ── 4. Report installed files ───────────────────────────────────────────────
Write-Host "`nInstalled files:"
Get-ChildItem -Path $absInstall -Filter '*.dll' | ForEach-Object { Write-Host "  $_" }

# Output the resolved version for scripts that capture it
return $Version
