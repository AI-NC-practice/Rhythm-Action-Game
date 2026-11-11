# Run a static C# editor method in Unity batch mode (scene building, player builds).
# Usage: powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/unity-exec.ps1 -Method <Namespace>.EditorTools.SceneBuilder.BuildAll
param([Parameter(Mandatory = $true)][string]$Method)
. "$PSScriptRoot\common.ps1"
Assert-UnityClosed

$log = Join-Path $ReportDir 'exec.log'
Remove-Item $log -ErrorAction SilentlyContinue
Write-Output "Executing $Method in Unity batch mode..."
$code = Invoke-Unity -UnityArgs @('-quit', '-executeMethod', $Method) -LogFile $log

$errs = Get-CompileErrors -LogFile $log
if ($errs.Count -gt 0) {
    Write-Output "COMPILE ERRORS:"
    $errs | Select-Object -First 30 | ForEach-Object { Write-Output "  $_" }
    exit 1
}
# 실제 예외 줄만 잡는다 ("XxxException: 메시지" 형식). 빌드 리포트의 파일 목록(...Exception.cs)은 실패가 아니다.
$failPattern = '(?m)^\s*(\w+\.)*\w*Exception(:| \(|$)|executeMethod method .* could not be found|Aborting batchmode due to failure'
$exc = Select-String -Path $log -Pattern $failPattern | Select-Object -First 10
if ($code -ne 0 -or $exc) {
    Write-Output "FAILED (unity exit code $code). Relevant log lines:"
    $exc | ForEach-Object { Write-Output "  $($_.Line.Trim())" }
    Write-Output "Full log: $log"
    exit 1
}
Write-Output "OK: $Method finished. Log: $log"
exit 0
