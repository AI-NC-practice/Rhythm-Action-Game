# Count triangles in a GLB file by reading its glTF JSON chunk, and check the file size (no Git LFS: keep files small).
# Usage: powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/check-glb.ps1 -Path Assets/Art/Models/key.glb [-Max 5000] [-MaxMb 15]
# Exit codes: 0 = within limits, 1 = over a limit or invalid file
param([Parameter(Mandatory = $true)][string]$Path, [int]$Max = 5000, [double]$MaxMb = 15)
. "$PSScriptRoot\common.ps1"

$full = if ([System.IO.Path]::IsPathRooted($Path)) { $Path } else { Join-Path $ProjectRoot $Path }
if (-not (Test-Path $full)) { Write-Output "INVALID: file not found: $Path"; exit 1 }

$bytes = [System.IO.File]::ReadAllBytes($full)
if ($bytes.Length -lt 20 -or [System.Text.Encoding]::ASCII.GetString($bytes, 0, 4) -ne 'glTF') {
    Write-Output "INVALID: not a GLB file: $Path"; exit 1
}
$jsonLen = [BitConverter]::ToUInt32($bytes, 12)
$json = [System.Text.Encoding]::UTF8.GetString($bytes, 20, $jsonLen) | ConvertFrom-Json

$tris = 0
foreach ($mesh in $json.meshes) {
    foreach ($prim in $mesh.primitives) {
        $mode = if ($null -ne $prim.mode) { [int]$prim.mode } else { 4 }
        if ($mode -ne 4) { continue }   # only TRIANGLES
        if ($null -ne $prim.indices) { $count = $json.accessors[[int]$prim.indices].count }
        else { $count = $json.accessors[[int]$prim.attributes.POSITION].count }
        $tris += [math]::Floor($count / 3)
    }
}
$images = if ($json.images) { @($json.images).Count } else { 0 }
$sizeMb = [math]::Round($bytes.Length / 1MB, 2)
$okTris = $tris -le $Max
$okSize = $sizeMb -le $MaxMb
$status = if ($okTris -and $okSize) { 'OK' } elseif (-not $okTris) { 'OVER_TRIANGLES' } else { 'OVER_SIZE' }
Write-Output "$status triangles=$tris max=$Max textures=$images size_mb=$sizeMb max_mb=$MaxMb file=$Path"
if ($okTris -and $okSize) { exit 0 } else { exit 1 }
