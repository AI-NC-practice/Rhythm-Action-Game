# PreToolUse hook (mcp__varco-3d__.*): keep VARCO 3D calls inside this game's flow and within budget.
#  - editing/running tools must pass flowId equal to harness/asset_specs.json defaults.flow_id
#  - deprecated run_workflow is blocked (use run_flow)
#  - run_flow is counted in harness/varco-usage.log and blocked after defaults.max_run_flow_calls
# Read-only tools (get_*, list_*) and upload_image are always allowed.
# Exit 2 = block the tool call; stderr is sent back to Claude.
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
# 입력을 못 읽으면 막는다 (fail-closed). 조용히 허용하면 run_flow가 예산 로그에 남지 않을 수 있다.
try { $j = [Console]::In.ReadToEnd() | ConvertFrom-Json }
catch { [Console]::Error.WriteLine('varco-guard: could not read hook input; blocked to keep the run budget accurate. Retry the call.'); exit 2 }
$tool = "$($j.tool_name)" -replace '^mcp__varco-3d__', ''

function Block([string]$msg) { [Console]::Error.WriteLine($msg); exit 2 }

if ($tool -match '^(get_|list_)' -or $tool -eq 'upload_image') { exit 0 }
if ($tool -eq 'run_workflow') { Block 'run_workflow is deprecated. Use run_flow with flowId.' }

$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$specs = Get-Content (Join-Path $root 'harness\asset_specs.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$flowId = $specs.defaults.flow_id
if (-not $flowId) {
    Block 'No dedicated VARCO flow is configured (asset_specs.json defaults.flow_id is null). Stop and ask the user to create one; see art_refs/README.md. Never use the existing Untitled flow.'
}
$given = "$($j.tool_input.flowId)"
if ($given -ne $flowId) {
    Block "varco-3d $tool must pass flowId '$flowId' (got '$given'). Other flows belong to the user's other work."
}

if ($tool -eq 'run_flow') {
    $log = Join-Path $root 'harness\varco-usage.log'
    $used = 0
    if (Test-Path $log) {
        # 로그를 못 읽으면 사용 횟수를 알 수 없으므로 막는다 (0으로 보고 예산 검사를 건너뛰지 않게)
        try { $used = @(Get-Content $log -ErrorAction Stop | Where-Object { $_ -match '\S' }).Count }
        catch { Block 'varco-guard: could not read harness/varco-usage.log to check the run budget, so the run was blocked. Retry.' }
    }
    $max = [int]$specs.defaults.max_run_flow_calls
    if ($used -ge $max) {
        Block "VARCO run budget reached ($used/$max run_flow calls). Stop and report to the user; they can raise defaults.max_run_flow_calls."
    }
    $scope = (@($j.tool_input.scope) -join ',')
    # 로그 쓰기에 실패하면 실행을 막는다 (기록되지 않은 실행이 생기지 않게). 일시적 잠금에 대비해 몇 번 다시 시도한다.
    $line = "{0}`trun_flow`tscope={1}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $scope
    $ok = $false
    for ($i = 0; $i -lt 5 -and -not $ok; $i++) {
        try { Add-Content -Path $log -Value $line -Encoding UTF8 -ErrorAction Stop; $ok = $true }
        catch { Start-Sleep -Milliseconds 200 }
    }
    if (-not $ok) { Block 'varco-guard: could not write harness/varco-usage.log, so the run was blocked. Retry.' }
}
exit 0
