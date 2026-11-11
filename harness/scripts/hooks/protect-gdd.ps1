# PreToolUse hook: protect the design document docs/GDD.md once the user has confirmed it.
#  - Before confirmation (no harness/gdd.lock): Claude may draft and edit the GDD together with the user.
#  - After confirmation (harness/gdd.lock exists): edits are blocked. Only the user changes the design,
#    or the user deletes harness/gdd.lock to reopen it for joint editing.
# Exit 2 = block the tool call; stderr is sent back to Claude.
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
try { $j = [Console]::In.ReadToEnd() | ConvertFrom-Json }
catch { [Console]::Error.WriteLine('protect-gdd: could not read hook input; blocked. Retry the call.'); exit 2 }

$path = "$($j.tool_input.file_path)" -replace '\\', '/'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$lock = Join-Path $root 'harness\gdd.lock'

# 잠금 파일은 사용자가 채팅에서 "기획 확정"을 말했을 때만 만든다 (project-setup 스킬이 셸로 생성). 지우는 것은 사용자만.
if ($path -match '(^|/)harness/gdd\.lock$') {
    [Console]::Error.WriteLine('harness/gdd.lock must not be edited with file tools. Create it only after the user explicitly confirms the GDD in chat (project-setup skill step), and never delete it; only the user removes it.')
    exit 2
}
if ($path -match '(^|/)docs/GDD\.md$' -and (Test-Path $lock)) {
    [Console]::Error.WriteLine('docs/GDD.md is confirmed and locked (harness/gdd.lock). Do not edit it. If the design seems wrong or unclear, ask the user; they can change it or delete harness/gdd.lock to reopen it.')
    exit 2
}
exit 0
