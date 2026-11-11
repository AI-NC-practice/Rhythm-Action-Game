# Stop hook: while the run-tasks loop is active, keep Claude working until no actionable task remains.
# Loop mode is on only while harness/.loop-active exists (created by the run-tasks skill).
# Exit 0 = allow stop. Exit 2 = block stop; stderr is sent back to Claude as the reason.
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$root  = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$flag  = Join-Path $root 'harness\.loop-active'
$state = Join-Path $root 'harness\.stop-hook-state'
$tasksFile = Join-Path $root 'harness\tasks.json'

$inputJson = $null
try { $inputJson = (New-Object System.IO.StreamReader([Console]::OpenStandardInput(), [System.Text.Encoding]::UTF8)).ReadToEnd() | ConvertFrom-Json } catch { }
$hookActive = $false
if ($inputJson -and $inputJson.stop_hook_active) { $hookActive = $true }

if (-not (Test-Path $flag)) { exit 0 }            # not in loop mode: normal conversation
if (-not (Test-Path $tasksFile)) { exit 0 }

$raw = Get-Content $tasksFile -Raw -Encoding UTF8
$tasks = ($raw | ConvertFrom-Json).tasks

# Legitimate reasons to stop: waiting on the user, or a task failed.
$waiting = @($tasks | Where-Object { $_.status -in @('needs_user', 'failed', 'blocked') })
if ($waiting.Count -gt 0) { Remove-Item $flag, $state -ErrorAction SilentlyContinue; exit 0 }

$done = @($tasks | Where-Object { $_.status -eq 'done' } | ForEach-Object { $_.id })
$actionable = @($tasks | Where-Object {
    $_.status -eq 'in_progress' -or (
        $_.status -eq 'pending' -and @($_.depends_on | Where-Object { $done -notcontains $_ }).Count -eq 0)
})
if ($actionable.Count -eq 0) { Remove-Item $flag, $state -ErrorAction SilentlyContinue; exit 0 }

# Loop guard: if we already blocked once and tasks.json has not changed since, let Claude stop.
$md5 = [System.Security.Cryptography.MD5]::Create()
$hash = [BitConverter]::ToString($md5.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($raw)))
if ($hookActive -and (Test-Path $state) -and ((Get-Content $state -Raw).Trim() -eq $hash)) {
    Remove-Item $flag, $state -ErrorAction SilentlyContinue
    exit 0
}
Set-Content -Path $state -Value $hash -Encoding ASCII

$next = $actionable[0]
[Console]::Error.WriteLine("run-tasks loop is active and tasks remain. Next: $($next.id) '$($next.title)' (status: $($next.status)). Continue the run-tasks procedure. If you are truly blocked, set that task's status to needs_user or blocked with a note in tasks.json, then stop.")
exit 2
