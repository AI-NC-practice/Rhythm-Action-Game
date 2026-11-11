# SessionStart hook: print a short task status so every new session starts with the current progress.
# Anything written to stdout is added to Claude's context.
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$cfgFile = Join-Path $root 'harness\project.json'
if (Test-Path $cfgFile) {
    $cfg = Get-Content $cfgFile -Raw -Encoding UTF8 | ConvertFrom-Json
    if (-not $cfg.setup_done) {
        Write-Output '[harness] New project from the harness template: setup is not done yet (harness/project.json setup_done=false).'
        Write-Output '[harness] Start with the project-setup skill: check the environment, fill project values, write the GDD together with the user, propose tasks, and lock the GDD only after the user confirms.'
        exit 0
    }
}
if (-not (Test-Path (Join-Path $root 'harness\gdd.lock'))) {
    Write-Output '[harness] GDD is not confirmed yet (no harness/gdd.lock). Finish planning with the user before /run-tasks.'
}
$tasksFile = Join-Path $root 'harness\tasks.json'
if (-not (Test-Path $tasksFile)) { exit 0 }
$tasks = (Get-Content $tasksFile -Raw -Encoding UTF8 | ConvertFrom-Json).tasks
$counts = $tasks | Group-Object status | ForEach-Object { "$($_.Name)=$($_.Count)" }
Write-Output "[harness] task status: $($counts -join ', ')"
foreach ($t in $tasks | Where-Object { $_.status -ne 'pending' -and $_.status -ne 'done' }) {
    Write-Output "[harness] $($t.id) is $($t.status): $($t.notes)"
}
$next = $tasks | Where-Object { $_.status -eq 'pending' } | Select-Object -First 1
if ($next) { Write-Output "[harness] next pending task: $($next.id)" }
if (Test-Path (Join-Path $root 'harness\.loop-active')) { Write-Output '[harness] run-tasks loop was active in a previous session. Ask the user before resuming.' }
exit 0
