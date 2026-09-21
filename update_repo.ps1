<#
  Pushes the current state of this repository and refreshes the GitHub release.

  Usage, from the repository folder:
      .\update_repo.cmd "what changed"
      .\update_repo.cmd "what changed" v1.20.0

  Without a tag the version from src\LivingSoldiers.csproj is used.
  Existing release assets with the same name are replaced.
#>
param(
  [string]$Message,
  [string]$Tag,
  [string[]]$Zips,
  [string]$ZipFolder = "C:\Program Files (x86)\Steam\steamapps\common\Sons Of The Forest",
  [switch]$NoRelease
)
$ErrorActionPreference = "Stop"
Set-Location (Split-Path -Parent $MyInvocation.MyCommand.Path)

function Step($t) { Write-Host ""; Write-Host "=== $t" -ForegroundColor Cyan }

if (-not (Test-Path ".git")) { Write-Host "Das hier ist kein Git-Repository." -ForegroundColor Red; exit 1 }
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
  Write-Host "Die GitHub CLI (gh) fehlt. Ohne sie geht nur der Push, kein Release." -ForegroundColor Yellow
  $NoRelease = $true
}

# ---- 1. Aenderungen einchecken ------------------------------------------
Step "Aenderungen"
$changes = git status --porcelain
if ($changes) {
  git status --short
  if (-not $Message) { $Message = Read-Host "`nCommit-Text" }
  if (-not $Message) { Write-Host "Ohne Text kein Commit." -ForegroundColor Red; exit 1 }
  git add -A
  git commit -m $Message
} else {
  Write-Host "Nichts geaendert, kein neuer Commit."
}

# ---- 2. Hochladen --------------------------------------------------------
Step "Push"
$branch = (git rev-parse --abbrev-ref HEAD).Trim()
git push origin $branch
if ($LASTEXITCODE -ne 0) { Write-Host "Push fehlgeschlagen." -ForegroundColor Red; exit 1 }

if ($NoRelease) { Write-Host ""; Write-Host "Fertig (ohne Release)." -ForegroundColor Green; exit 0 }

# ---- 3. Version bestimmen ------------------------------------------------
if (-not $Tag) {
  $csproj = Join-Path $PWD "src\LivingSoldiers.csproj"
  if (Test-Path $csproj) {
    $m = [regex]::Match((Get-Content $csproj -Raw), '<Version>([^<]+)</Version>')
    if ($m.Success) { $Tag = "v" + $m.Groups[1].Value }
  }
}
if (-not $Tag) { Write-Host "Keine Version gefunden, bitte als zweites Argument angeben." -ForegroundColor Red; exit 1 }

# ---- 4. Pakete suchen ----------------------------------------------------
Step "Release $Tag"
$v = $Tag.TrimStart('v')
if ($Zips -and $Zips.Count) {
  # Feste Pfade aus update_repo.cmd
  $missing = $Zips | Where-Object { -not (Test-Path $_) }
  foreach ($m in $missing) { Write-Host "Nicht gefunden: $m" -ForegroundColor Yellow }
  $zips = $Zips | Where-Object { Test-Path $_ }
} else {
  $zips = @(
    (Join-Path $ZipFolder "LivingSoldiers_v$v.zip"),
    (Join-Path $ZipFolder "LivingSoldiers_v${v}_Server.zip")
  ) | Where-Object { Test-Path $_ }
}

if ($zips.Count -eq 0) {
  Write-Host "Keine Pakete zu $Tag gefunden in:" -ForegroundColor Yellow
  Write-Host "  $ZipFolder"
  Write-Host "Das Release wird ohne Dateien angelegt bzw. nur der Text aktualisiert."
} else {
  foreach ($z in $zips) { Write-Host ("  " + (Split-Path $z -Leaf) + "  " + [math]::Round((Get-Item $z).Length/1MB,1) + " MB") }
}

# ---- 5. Release anlegen oder aktualisieren --------------------------------
$notes = if (Test-Path "RELEASE_NOTES.md") { @("--notes-file","RELEASE_NOTES.md") } else { @() }
gh release view $Tag 2>$null | Out-Null
if ($LASTEXITCODE -eq 0) {
  Write-Host "Release existiert, wird aktualisiert."
  if ($notes.Count) { gh release edit $Tag @notes }
  if ($zips.Count) { gh release upload $Tag @zips --clobber }
} else {
  Write-Host "Release wird neu angelegt."
  gh release create $Tag @zips --title $v @notes
}

Step "Fertig"
gh release view $Tag --json tagName,url,assets --jq '"\(.tagName)  \(.url)\n" + (.assets | map("  " + .name) | join("\n"))'
