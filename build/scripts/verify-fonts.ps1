#Requires -Version 3.0
<#
Exit codes:
  0 = success
  1 = error (missing file, duplicate alias, unreadable/invalid font file, or TreatWarningsAsErrors)
  2 = success with warnings (e.g., undeclared FontFamily usages)
#>
[CmdletBinding()]
param(
  [Parameter(Mandatory=$true)][string]$Project,
  [Parameter()][string]$TargetFramework,
  [Parameter()][string]$Configuration,
  [switch]$TreatWarningsAsErrors
)

function Resolve-AbsolutePath([string]$BaseDir, [string]$RelativePath) {
  $rp = $RelativePath -replace '\\','/'  # tolerate mixed input
  return [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($BaseDir, $rp))
}

function Test-FontMagicBytes([string]$Path) {
  try {
    $fs = [System.IO.File]::Open($Path, 'Open', 'Read', 'ReadWrite')
    try {
      $buf = New-Object byte[] 4
      $read = $fs.Read($buf, 0, 4)
      if ($read -ne 4) { return $false }
      # Check for 00 01 00 00 (0x00010000)
      $val = [System.BitConverter]::ToUInt32(($buf[3],$buf[2],$buf[1],$buf[0]), 0) # big-endian interpret
      if ($val -eq 0x00010000) { return $true }
      # Check for ASCII 'OTTO', 'true', 'typ1'
      $str = [System.Text.Encoding]::ASCII.GetString($buf)
      if ($str -eq 'OTTO' -or $str -eq 'true' -or $str -eq 'typ1') { return $true }
      return $false
    } finally { $fs.Dispose() }
  } catch {
    return $false
  }
}

if (!(Test-Path -LiteralPath $Project)) {
  Write-Error "verify-fonts.ps1: Project file not found: $Project"
  exit 1
}

try {
  [xml]$xml = Get-Content -LiteralPath $Project
} catch {
  Write-Error "verify-fonts.ps1: Failed to read project XML: $($_.Exception.Message)"
  exit 1
}

# Collect <MauiFont Include="..."> and their Alias attributes
$mauiFonts = @()
$nodes = $xml.Project.ItemGroup | ForEach-Object { $_.MauiFont } | Where-Object { $_ -ne $null }
foreach ($n in $nodes) {
  $include = $n.Include
  $alias   = $n.Alias
  if ([string]::IsNullOrWhiteSpace($include) -or [string]::IsNullOrWhiteSpace($alias)) {
    Write-Error "verify-fonts.ps1: Found a <MauiFont> missing Include or Alias."
    exit 1
  }
  $mauiFonts += [pscustomobject]@{ Include=$include; Alias=$alias }
}

if ($mauiFonts.Count -eq 0) {
  Write-Error "verify-fonts.ps1: No <MauiFont> entries found. At least one font must be declared."
  exit 1
}

# Check duplicates in Alias
$dupes = $mauiFonts | Group-Object Alias | Where-Object { $_.Count -gt 1 }
if ($dupes) {
  $dupeList = ($dupes | ForEach-Object { "$( $_.Name) x$( $_.Count)" }) -join ', '
  Write-Error "verify-fonts.ps1: Duplicate Alias detected: $dupeList"
  exit 1
}

$projDir = [System.IO.Path]::GetDirectoryName([System.IO.Path]::GetFullPath($Project))
$missing = @()
$invalid = @()
foreach ($f in $mauiFonts) {
  $abs = Resolve-AbsolutePath $projDir $f.Include
  if (!(Test-Path -LiteralPath $abs)) {
    $missing += "$( $f.Include) (Alias=$( $f.Alias))"
    continue
  }
  if (-not (Test-FontMagicBytes -Path $abs)) {
    $invalid += "$( $f.Include) (Alias=$( $f.Alias))"
  }
}

if ($missing.Count -gt 0) {
  Write-Error "verify-fonts.ps1: Missing font files: $( $missing -join '; ')"
  exit 1
}
if ($invalid.Count -gt 0) {
  Write-Error "verify-fonts.ps1: Invalid or unreadable font files (expect TTF/OTF): $( $invalid -join '; ')"
  exit 1
}

# Soft check: find FontFamily uses not in alias set (warn only by default)
$aliases = $mauiFonts.Alias
$repoRoot = $projDir
$warns = @()

$files = Get-ChildItem -LiteralPath $repoRoot -Recurse -Include *.xaml,*.cs -File -ErrorAction SilentlyContinue
foreach ($file in $files) {
  $content = Get-Content -LiteralPath $file.FullName -Raw
  # XAML: FontFamily="Name" (static string only)
  $matches = Select-String -InputObject $content -Pattern 'FontFamily\s*=\s*"([^"]+)"' -AllMatches
  foreach ($m in $matches.Matches) {
    $name = $m.Groups[1].Value
    if ($aliases -notcontains $name) {
      $warns += "$( $file.FullName): $name"
    }
  }
  # C#: new FontFamily("Name") or WithFont("Name") simple cases
  $matchesCs = Select-String -InputObject $content -Pattern 'FontFamily\(\s*"([^"]+)"\s*\)|WithFont\(\s*"([^"]+)"\s*\)' -AllMatches
  foreach ($m in $matchesCs.Matches) {
    $name = ($m.Groups[1].Value, $m.Groups[2].Value) | Where-Object { $_ -ne "" } | Select-Object -First 1
    if ($name -and ($aliases -notcontains $name)) {
      $warns += "$( $file.FullName): $name"
    }
  }
}

if ($warns.Count -gt 0) {
  Write-Warning "verify-fonts.ps1: Found FontFamily values not declared as MAUI aliases:"
  $warns | ForEach-Object { Write-Warning "  $_" }
  if ($TreatWarningsAsErrors) {
    Write-Error "verify-fonts.ps1: TreatWarningsAsErrors is set; failing due to warnings."
    exit 1
  } else {
    exit 2
  }
}

Write-Host "verify-fonts.ps1: Fonts OK ($($mauiFonts.Count) declared)."
exit 0
