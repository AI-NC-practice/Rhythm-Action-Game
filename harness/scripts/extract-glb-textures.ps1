# Extract the images embedded in a GLB next to a given folder, named base_color / normal / image_N.
# Needed because VARCO's FBX export references base_color.png and normal.png by name but does not embed them.
# Usage: powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/extract-glb-textures.ps1 -Glb <model.glb> -OutDir <folder next to the fbx>
param([Parameter(Mandatory = $true)][string]$Glb, [Parameter(Mandatory = $true)][string]$OutDir)
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$b = [IO.File]::ReadAllBytes((Resolve-Path $Glb))
if ([Text.Encoding]::ASCII.GetString($b, 0, 4) -ne 'glTF') { Write-Output 'INVALID: not a GLB file'; exit 1 }
$jsonLen = [BitConverter]::ToUInt32($b, 12)
$j = [Text.Encoding]::UTF8.GetString($b, 20, $jsonLen) | ConvertFrom-Json
$binStart = 20 + $jsonLen + 8
if (-not $j.images) { Write-Output 'NO_IMAGES: GLB has no embedded images'; exit 0 }
New-Item -ItemType Directory -Force $OutDir | Out-Null

# 첫 번째 머티리얼의 색/노멀 텍스처에 이름을 붙인다
$names = @{}
$m = $j.materials | Select-Object -First 1
if ($m -and $m.pbrMetallicRoughness.baseColorTexture) { $names[[int]$j.textures[$m.pbrMetallicRoughness.baseColorTexture.index].source] = 'base_color' }
if ($m -and $m.normalTexture) { $names[[int]$j.textures[$m.normalTexture.index].source] = 'normal' }

for ($i = 0; $i -lt $j.images.Count; $i++) {
    $img = $j.images[$i]
    if ($null -eq $img.bufferView) { continue }
    $bv = $j.bufferViews[$img.bufferView]
    $off = $binStart + [int]$bv.byteOffset
    $ext = if ($img.mimeType -eq 'image/jpeg') { 'jpg' } else { 'png' }
    $name = if ($names.ContainsKey($i)) { $names[$i] } else { "image_$i" }
    $out = Join-Path $OutDir "$name.$ext"
    [IO.File]::WriteAllBytes($out, $b[$off..($off + [int]$bv.byteLength - 1)])
    Write-Output ("{0} ({1:N0} KB)" -f $out, ($bv.byteLength / 1KB))
}
exit 0
