# Download a generated model (from varco-3d get_output_downloads) into Assets/Art/Models/<id>.glb and check it.
# Usage: powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/download-asset.ps1 -Id mannequin -Url "<download url>"
# Exit codes: 0 = saved and within limits, 1 = download failed or over a limit
param(
    [Parameter(Mandatory = $true)][string]$Id,
    [Parameter(Mandatory = $true)][string]$Url
)
. "$PSScriptRoot\common.ps1"

$specs = Get-Content (Join-Path $ProjectRoot 'harness\asset_specs.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$spec = $specs.assets | Where-Object { $_.id -eq $Id }
if (-not $spec) { Write-Output "INVALID: unknown asset id '$Id'"; exit 1 }
if ($Url -notmatch '^https://') { Write-Output 'INVALID: URL must start with https://'; exit 1 }

$outDir = Join-Path $ProjectRoot 'Assets\Art\Models'
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }
$out = Join-Path $outDir "$Id.glb"
$tmp = "$out.part"
Remove-Item $tmp -ErrorAction SilentlyContinue

& "$env:SystemRoot\System32\curl.exe" -sS -L --fail -m 300 -o "$tmp" "$Url"
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $tmp)) {
    Remove-Item $tmp -ErrorAction SilentlyContinue
    Write-Output "DOWNLOAD_FAILED: curl exit $LASTEXITCODE (the URL may have expired; call get_output_downloads again)"
    exit 1
}
Move-Item -Force $tmp $out
Write-Output "Saved Assets/Art/Models/$Id.glb"

& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'check-glb.ps1') -Path $out `
    -Max ([int]$specs.defaults.max_triangles) -MaxMb ([double]$specs.defaults.max_file_mb)
exit $LASTEXITCODE
