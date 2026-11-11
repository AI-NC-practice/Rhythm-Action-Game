# Shared settings for harness scripts. Dot-source this file: . "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$ReportDir   = Join-Path $ProjectRoot 'harness\reports'
if (-not (Test-Path $ReportDir)) { New-Item -ItemType Directory -Path $ReportDir | Out-Null }

# Unity 에디터 경로 찾기 (우선순위)
#  1) harness/project.json 의 unity_editor
#  2) ProjectSettings/ProjectVersion.txt 버전으로 Unity Hub 기본 설치 경로
# 이 파일은 dot-source되므로 여기서 exit하지 않는다 (exit하면 이 파일만 끝나고 호출한 스크립트는 계속 돈다).
# 찾지 못하면 $UnityBlockReason에 이유를 남기고, Assert-UnityClosed가 호출한 스크립트에서 멈춘다.
$UnityBlockReason = $null
function Get-UnityExe {
    $cfgPath = Join-Path $ProjectRoot 'harness\project.json'
    if (Test-Path $cfgPath) {
        $cfg = Get-Content $cfgPath -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($cfg.unity_editor -and (Test-Path $cfg.unity_editor)) { return $cfg.unity_editor }
    }
    $verFile = Join-Path $ProjectRoot 'ProjectSettings\ProjectVersion.txt'
    if (Test-Path $verFile) {
        $ver = (Select-String -Path $verFile -Pattern '^m_EditorVersion:\s*(\S+)').Matches[0].Groups[1].Value
        foreach ($hub in @("$env:ProgramFiles\Unity\Hub\Editor", "${env:ProgramFiles(x86)}\Unity\Hub\Editor")) {
            $exe = Join-Path $hub "$ver\Editor\Unity.exe"
            if (Test-Path $exe) { return $exe }
        }
        $script:UnityBlockReason = "BLOCKED: Unity $ver 에디터를 찾지 못했다. Unity Hub로 설치하거나 harness/project.json 의 unity_editor 에 경로를 적을 것."
        return $null
    }
    $script:UnityBlockReason = 'BLOCKED: Unity 프로젝트가 아직 없다 (ProjectSettings/ProjectVersion.txt 없음). project-setup 스킬로 먼저 Unity 프로젝트를 준비할 것.'
    return $null
}
$UnityExe = Get-UnityExe

function Assert-UnityClosed {
    if (-not $UnityExe) { Write-Output $UnityBlockReason; exit 3 }
    # Unity Editor keeps Temp/UnityLockfile open while the project is loaded.
    $lock = Join-Path $ProjectRoot 'Temp\UnityLockfile'
    if (Test-Path $lock) {
        try {
            $fs = [System.IO.File]::Open($lock, 'Open', 'ReadWrite', 'None')
            $fs.Close()
        } catch {
            Write-Output 'BLOCKED: Unity Editor has this project open. Ask the user to close Unity, then retry.'
            exit 3
        }
    }
}

function Invoke-Unity {
    param([string[]]$UnityArgs, [string]$LogFile)
    $all = @('-batchmode', '-projectPath', "`"$ProjectRoot`"", '-logFile', "`"$LogFile`"") + $UnityArgs
    $p = Start-Process -FilePath $UnityExe -ArgumentList $all -Wait -PassThru -WindowStyle Hidden
    return $p.ExitCode
}

function Get-CompileErrors {
    param([string]$LogFile)
    if (-not (Test-Path $LogFile)) { return @() }
    $lines = Select-String -Path $LogFile -Pattern 'error CS\d{4}' | ForEach-Object { $_.Line.Trim() }
    return @($lines | Select-Object -Unique)
}
