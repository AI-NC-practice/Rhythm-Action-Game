# Compile the project and run EditMode + PlayMode tests in Unity batch mode.
# Usage: powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/unity-verify.ps1 [-Platform EditMode|PlayMode|All] [-Filter <test name regex>]
#   -Filter: run only matching tests (quick check while working; the full run without -Filter is what counts)
# Exit codes: 0 = all passed, 1 = compile error or test failure, 3 = Unity Editor is open
param([ValidateSet('EditMode', 'PlayMode', 'All')][string]$Platform = 'All', [string]$Filter = '')
. "$PSScriptRoot\common.ps1"
Assert-UnityClosed

$platforms = if ($Platform -eq 'All') { @('EditMode', 'PlayMode') } else { @($Platform) }
$summary = New-Object System.Collections.Generic.List[string]
$summary.Add("# Unity verify report")
$summary.Add("time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
$ok = $true

foreach ($pf in $platforms) {
    $log = Join-Path $ReportDir "$($pf.ToLower()).log"
    $xml = Join-Path $ReportDir "$($pf.ToLower())-results.xml"
    Remove-Item $log, $xml -ErrorAction SilentlyContinue

    Write-Output "Running $pf tests (this takes 1-3 minutes)..."
    $uargs = @('-runTests', '-testPlatform', $pf, '-testResults', "`"$xml`"")
    if ($Filter -ne '') { $uargs += @('-testFilter', "`"$Filter`"") }
    $code = Invoke-Unity -UnityArgs $uargs -LogFile $log

    $errs = Get-CompileErrors -LogFile $log
    if ($errs.Count -gt 0) {
        $ok = $false
        $summary.Add("")
        $summary.Add("## $pf : COMPILE ERRORS ($($errs.Count))")
        $errs | Select-Object -First 30 | ForEach-Object { $summary.Add("- $_") }
        break
    }

    if (-not (Test-Path $xml)) {
        $ok = $false
        $summary.Add("")
        $summary.Add("## $pf : NO RESULT FILE (unity exit code $code). See $log")
        Get-Content $log -Tail 25 | ForEach-Object { $summary.Add("    $_") }
        continue
    }

    [xml]$r = Get-Content $xml -Raw -Encoding UTF8
    $run = $r.'test-run'
    $summary.Add("")
    $summary.Add("## $pf : total=$($run.total) passed=$($run.passed) failed=$($run.failed) skipped=$($run.skipped) result=$($run.result)")
    if ([int]$run.failed -gt 0) {
        $ok = $false
        foreach ($tc in $r.SelectNodes("//test-case[@result='Failed']")) {
            $msg = ''
            if ($tc.failure -and $tc.failure.message) { $msg = ($tc.failure.message.InnerText -replace '\s+', ' ').Trim() }
            $summary.Add("- FAIL $($tc.fullname): $msg")
        }
    }
    foreach ($tc in $r.SelectNodes("//test-case")) {
        $summary.Add("  - [$($tc.result)] $($tc.fullname)")
    }
}

$summary.Add("")
$summary.Add($(if ($ok) { 'OVERALL: PASS' } else { 'OVERALL: FAIL' }))
$text = $summary -join "`n"
Set-Content -Path (Join-Path $ReportDir 'verify-latest.md') -Value $text -Encoding UTF8
Write-Output $text
if ($ok) { exit 0 } else { exit 1 }
