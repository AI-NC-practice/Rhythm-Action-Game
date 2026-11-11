# Run a Blender Python script headless (optional tool; skipped if Blender is not installed).
# Usage:
#   powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/blender-run.ps1 -Check
#   powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/blender-run.ps1 -Script render_preview --in Assets/Art/Models/x.glb --out harness/reports/preview/x.png
# Script arguments (--in, --out, ...) go after -Script and are passed to the Python script as-is.
# Scripts live in harness/scripts/blender/<name>.py
# Exit codes: 0 = ok, 1 = script failed, 4 = Blender not installed
# 일반 스크립트로 둔다(고급 함수 속성을 쓰면 "--in"이 공통 매개변수 -InformationAction으로 해석된다). 나머지 인자는 $args로 받는다.
param([switch]$Check, [string]$Script = '')
$ScriptArgs = @($args)
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path

function Find-Blender {
    $cfg = Join-Path $root 'harness\project.json'
    if (Test-Path $cfg) {
        $p = (Get-Content $cfg -Raw -Encoding UTF8 | ConvertFrom-Json).blender_exe
        if ($p -and (Test-Path $p)) { return $p }
    }
    $cmd = Get-Command blender -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    $found = Get-ChildItem "$env:ProgramFiles\Blender Foundation" -Recurse -Filter blender.exe -ErrorAction SilentlyContinue |
        Sort-Object FullName -Descending | Select-Object -First 1
    if ($found) { return $found.FullName }
    return $null
}

$blender = Find-Blender
if (-not $blender) {
    Write-Output 'NO_BLENDER: Blender가 설치되어 있지 않다. 이 도구는 선택 사항이므로 건너뛴다. (설치: https://www.blender.org/download/ , 또는 harness/project.json 의 blender_exe 에 경로)'
    exit 4
}
if ($Check) {
    $ver = & $blender --version 2>$null | Select-Object -First 1
    Write-Output "OK: $ver ($blender)"
    exit 0
}

$py = Join-Path $root "harness\scripts\blender\$Script.py"
if (-not (Test-Path $py)) { Write-Output "INVALID: $py 가 없다."; exit 1 }
$log = Join-Path $root 'harness\reports\blender.log'
Push-Location $root
try {
    & $blender --background --factory-startup --python $py -- @ScriptArgs *> $log
    $code = $LASTEXITCODE
} finally { Pop-Location }
$out = Get-Content $log -Encoding UTF8 | Where-Object { $_ -match '^\[(OK|FAIL|INFO)\]|Traceback|Error' }
$out | ForEach-Object { Write-Output $_ }
if ($code -ne 0 -or ($out -match '^\[FAIL\]|Traceback')) { Write-Output "FAILED. Full log: $log"; exit 1 }
exit 0
