# Run the built Windows player headless for a few seconds and scan its log for errors.
# Catches problems that only appear in builds (e.g. NavMeshAgent created before NavMesh data, missing shaders).
# Usage: powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/smoke-test-build.ps1 [-Exe Builds/Final/Game.exe] [-Seconds 12]
# Exit codes: 0 = ran and no problems found, 1 = crashed early or problems found
param([string]$Exe = '', [int]$Seconds = 12)
. "$PSScriptRoot\common.ps1"

if (-not $Exe) {
    $found = Get-ChildItem (Join-Path $ProjectRoot 'Builds') -Recurse -Filter *.exe -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -notmatch 'UnityCrashHandler' } | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $found) { Write-Output 'NO BUILD: Builds/ 아래에 exe가 없다.'; exit 1 }
    $Exe = $found.FullName
}
$Exe = (Resolve-Path $Exe).Path
$log = Join-Path $ReportDir 'player-smoke.log'
Remove-Item $log -ErrorAction SilentlyContinue

Write-Output "Running $Exe headless for $Seconds s..."
$p = Start-Process -FilePath $Exe -ArgumentList @('-batchmode', '-logFile', "`"$log`"") -PassThru -WindowStyle Hidden
Start-Sleep -Seconds $Seconds
$alive = -not $p.HasExited
if ($alive) { Stop-Process -Id $p.Id -Force }

$problems = @()
if (Test-Path $log) {
    $problems = @(Select-String -Path $log -Pattern '(?m)^\s*(\w+\.)*\w*Exception(:| \(|$)|Failed to create agent|Shader .* not supported|Missing|NullReference|error' |
        ForEach-Object { $_.Line.Trim() } | Select-Object -Unique -First 20)
}
if (-not $alive) { Write-Output "FAIL: player exited within $Seconds s (exit code $($p.ExitCode)). Log: $log" }
if ($problems.Count -gt 0) {
    Write-Output 'FAIL: problems in player log:'
    $problems | ForEach-Object { Write-Output "  $_" }
}
if ($alive -and $problems.Count -eq 0) { Write-Output "OK: player ran $Seconds s with a clean log. Log: $log"; exit 0 }
exit 1
