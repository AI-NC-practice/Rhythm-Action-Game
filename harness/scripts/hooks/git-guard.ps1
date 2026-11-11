# PreToolUse hook (Bash|PowerShell): guard git commit and git push.
#  - git push with --force / -f / --force-with-lease / +refspec  -> blocked
#  - git add and git commit in one command                         -> blocked (stage first, so the staged content can be checked)
#  - git commit: staged file over 20MB (no Git LFS)                -> blocked
#  - git commit: staged secret-looking files or text               -> blocked
# Exit 2 = block the tool call; stderr is sent back to Claude.
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
try { $j = [Console]::In.ReadToEnd() | ConvertFrom-Json } catch { exit 0 }
$cmd = "$($j.tool_input.command)"
if ($cmd -notmatch '\bgit\b') { exit 0 }

function Block([string]$msg) { [Console]::Error.WriteLine($msg); exit 2 }

if ($cmd -match '\bgit\s+push\b') {
    # push 명령 부분만, 대소문자를 구분해서(-cmatch) 검사한다.
    # 그렇지 않으면 같은 줄의 "git commit -F 파일"이 push -f로 오인되어 막힌다.
    $pushPart = ($cmd -split '\bgit\s+push\b', 2)[1]
    $pushPart = ($pushPart -split '&&|\|\||;|\|')[0]
    if ($pushPart -cmatch '--force|--force-with-lease|\s-f(\s|$)|\s\+[\w/]') {
        Block 'Force push is not allowed in this project. If the push was rejected, stop and report to the user.'
    }
    exit 0
}

if ($cmd -notmatch '\bgit\s+commit\b') { exit 0 }
if ($cmd -match '\bgit\s+add\b') { Block 'Run "git add" as a separate command first, then "git commit". The guard checks the staged content before the commit.' }
if ($cmd -match '\s-a(\s|$)|--all|\s-am\s') { Block 'Do not use "git commit -a". Stage with "git add -A" first.' }

$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
Push-Location $root
try {
    $problems = New-Object System.Collections.Generic.List[string]
    $files = @(git -c core.quotepath=false diff --cached --name-only --diff-filter=ACMR)
    foreach ($f in $files) {
        if (-not $f) { continue }
        if ($f -match '(^|/)\.env(\..*)?$|\.pem$|\.key$|\.p12$|\.pfx$|id_rsa') { $problems.Add("secret-like file staged: $f") }
        $p = Join-Path $root $f
        if (Test-Path $p) {
            $mb = (Get-Item $p).Length / 1MB
            if ($mb -gt 20) { $problems.Add(("file too large for GitHub without LFS ({0:N1} MB > 20 MB): {1}" -f $mb, $f)) }
        }
    }

    $patterns = @(
        'ghp_[A-Za-z0-9]{36}', 'gho_[A-Za-z0-9]{36}', 'github_pat_[A-Za-z0-9_]{40,}',
        'sk-[A-Za-z0-9_\-]{20,}', 'AKIA[0-9A-Z]{16}',
        '-----BEGIN [A-Z ]*PRIVATE KEY-----',
        '(?i)(api[_-]?key|openapi_key|secret|access[_-]?token|password)\s*["'']?\s*[:=]\s*["'']?[A-Za-z0-9_\-\.]{16,}',
        '(?i)bearer\s+[A-Za-z0-9_\-\.=]{24,}'
    )
    $added = git -c core.quotepath=false diff --cached -U0 --no-color | Where-Object { $_ -match '^\+' -and $_ -notmatch '^\+\+\+' }
    foreach ($line in $added) {
        foreach ($pat in $patterns) {
            if ($line -match $pat) {
                $snippet = $line.Substring(0, [Math]::Min(60, $line.Length))
                $problems.Add("secret-like text staged (pattern $pat): $snippet...")
                break
            }
        }
        if ($problems.Count -ge 10) { break }
    }

    if ($problems.Count -gt 0) {
        Block ("Commit blocked by git-guard:`n - " + ($problems -join "`n - ") + "`nUnstage or fix these (git restore --staged <file>), then commit again. Never commit keys or tokens.")
    }
} finally { Pop-Location }
exit 0
