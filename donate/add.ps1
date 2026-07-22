# WinCraft 捐赠记录追加脚本
# 用法:
#   双击 add.bat 或右键 add.ps1 → PowerShell 运行
#   终端:  .\add.ps1
#   命令行: .\add.ps1 -Name "支持者" -Amount 5 -Platform wechat

param(
    [string]$Name,
    [string]$Amount,
    [string]$Platform,
    [string]$Message = "",
    [string]$Currency = "CNY",
    [string]$Date = "",
    [switch]$Commit
)

$ErrorActionPreference = 'Stop'
try {

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$CsvPath = Join-Path $ScriptDir "donate.csv"

# ---- 工具函数 ----

function ConvertTo-CsvField([string]$Value) {
    if ($Value -match '[,"\r\n]') {
        return '"' + ($Value -replace '"', '""') + '"'
    }
    return $Value
}

function Get-DonationTimestamp {
    return (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
}

function Add-DonationRecord($Record) {
    $line = @(
        (ConvertTo-CsvField $Record.Date),
        (ConvertTo-CsvField $Record.Name),
        (ConvertTo-CsvField $Record.Amount),
        (ConvertTo-CsvField $Record.Message),
        (ConvertTo-CsvField $Record.Platform),
        (ConvertTo-CsvField $Record.Currency)
    ) -join ","

    $lines = [System.Collections.Generic.List[string]]@(Get-Content -Path $CsvPath -Encoding UTF8)
    # 按时间从新到旧找到插入位置（跳过 header 行）
    $insertPos = 1
    $newDate = $Record.Date -replace '\D', ''  # 2026-07-22 12:00:00 → 20260722120000
    for ($i = 1; $i -lt $lines.Count; $i++) {
        $existingDate = ($lines[$i] -split ',')[0] -replace '\D', ''
        if ($existingDate -and [long]$existingDate -le [long]$newDate) {
            $insertPos = $i
            break
        }
        $insertPos = $i + 1
    }
    $lines.Insert($insertPos, $line)

    $content = ($lines -join "`r`n").TrimEnd() + "`r`n"
    [System.IO.File]::WriteAllText($CsvPath, $content, [System.Text.UTF8Encoding]::new($false))
}

function Test-ValidInput($Record) {
    if (-not $Record.Name) { Write-Host "  ✗ 用户名不能为空" -ForegroundColor Red; return $false }
    if ($Record.Amount -as [double] -le 0) { Write-Host "  ✗ 金额必须大于 0" -ForegroundColor Red; return $false }
    $validPlatforms = @("wechat", "alipay", "qq")
    if ($Record.Platform -notin $validPlatforms) { Write-Host "  ✗ 平台无效" -ForegroundColor Red; return $false }
    if ($Record.Currency -notmatch '^[A-Z]{3}$') { Write-Host "  ✗ 币种格式无效" -ForegroundColor Red; return $false }
    return $true
}

# 带方向键的选项选择器
function Select-Option {
    param(
        [string]$Label,
        [System.Collections.IDictionary]$Options,
        [string]$DefaultKey
    )
    $keys = @($Options.Keys)
    $idx = [array]::IndexOf($keys, $DefaultKey)
    if ($idx -lt 0) { $idx = 0 }

    $top = [Console]::CursorTop
    while ($true) {
        [Console]::SetCursorPosition(0, $top)
        # 清行
        Write-Host (" " * [Console]::WindowWidth) -NoNewline
        [Console]::SetCursorPosition(0, $top)

        Write-Host "$Label " -NoNewline
        for ($i = 0; $i -lt $keys.Count; $i++) {
            $fg = if ($i -eq $idx) { "White" } else { "DarkGray" }
            $prefix = if ($i -eq $idx) { "▶ " } else { "  " }
            Write-Host "$prefix" -NoNewline
            Write-Host "$($Options[$keys[$i]])" -ForegroundColor $fg -NoNewline
            Write-Host "  " -NoNewline
        }
        Write-Host ""

        $key = [Console]::ReadKey($true)
        switch ($key.Key) {
            LeftArrow  { $idx = [Math]::Max(0, $idx - 1) }
            RightArrow { $idx = [Math]::Min($keys.Count - 1, $idx + 1) }
            Enter      { return $keys[$idx] }
            D1 { if ($keys.Count -ge 1) { return $keys[0] } }
            D2 { if ($keys.Count -ge 2) { return $keys[1] } }
            D3 { if ($keys.Count -ge 3) { return $keys[2] } }
            Escape     { return $null }
        }
    }
}

# 确认提示（Y/N）
function Confirm-YesNo {
    param([string]$Prompt)
    $top = [Console]::CursorTop
    while ($true) {
        [Console]::SetCursorPosition(0, $top)
        Write-Host (" " * [Console]::WindowWidth) -NoNewline
        [Console]::SetCursorPosition(0, $top)
        Write-Host "$Prompt " -NoNewline
        Write-Host "[▶ 确认]  " -ForegroundColor White -NoNewline
        Write-Host "[  取消]  " -ForegroundColor DarkGray -NoNewline
        Write-Host "Enter/← → 选择" -ForegroundColor DarkGray

        $key = [Console]::ReadKey($true)
        switch ($key.Key) {
            Enter     { Write-Host ""; return $true }
            RightArrow { Write-Host ""; return $false }
            LeftArrow  { Write-Host ""; return $true }
            Escape    { Write-Host ""; return $false }
        }
    }
}

# 检测是否从资源管理器右键启动（窗口会自动关闭）
function Test-LaunchedFromExplorer {
    try {
        $parentPid = (Get-CimInstance -ClassName Win32_Process -Filter "ProcessId = $pid" -Property ParentProcessId).ParentProcessId
        return (Get-Process -Id $parentPid -ErrorAction Stop).ProcessName -eq 'explorer'
    } catch {
        return $false
    }
}

# Git 提交：上条=同描述则 amend，否则新 commit
function Sync-GitCommit {
    $targetMsg = "docs: update the list of donations"
    try {
        Push-Location $ScriptDir
        if (-not (git status --porcelain donate.csv)) {
            Write-Host "  (无变更，跳过提交)" -ForegroundColor DarkGray
            return
        }
        $lastMsg = git log -1 --format=%s
        git add donate.csv
        if ($lastMsg -eq $targetMsg) {
            git commit --amend -m $targetMsg --no-verify
            Write-Host "  ✓ 已压入上条提交: $targetMsg" -ForegroundColor Green
        } else {
            git commit -m $targetMsg --no-verify
            Write-Host "  ✓ 已创建提交: $targetMsg" -ForegroundColor Green
        }
    } catch {
        Write-Host "  ⚠ Git 提交失败: $_" -ForegroundColor DarkYellow
    } finally {
        Pop-Location
    }
}

# ---- 命令行模式 ----

if ($Name -and $Amount -and $Platform) {
    $exitCode = 0
    $Record = @{
        Date     = if ($Date) { $Date } else { Get-DonationTimestamp }
        Name     = $Name
        Amount   = $Amount
        Message  = $Message
        Platform = $Platform.ToLower()
        Currency = $Currency.ToUpper()
    }
    if (Test-ValidInput $Record) {
        Add-DonationRecord $Record
        Write-Host "✓ 已追加: $($Record.Date) | $($Record.Name) | $($Record.Amount) $($Record.Currency) | $($Record.Platform)" -ForegroundColor Green
        if ($Commit) { Sync-GitCommit }
    } else {
        $exitCode = 1
    }
    if (Test-LaunchedFromExplorer) { Read-Host "按 Enter 退出" }
    exit $exitCode
}

# ---- 交互模式 ----

Clear-Host
Write-Host ""
Write-Host "  ═══ WinCraft 捐赠记录 ═══" -ForegroundColor Cyan
Write-Host ""

$platforms = [ordered]@{
    wechat = "微信"
    alipay = "支付宝"
    qq     = "QQ"
}
$currencies = [ordered]@{
    CNY = "CNY"
    USD = "USD"
    EUR = "EUR"
}

$lastPlatform = "wechat"
$lastCurrency = "CNY"
$added = 0

while ($true) {
    # 收集输入
    Write-Host "  用户名 " -NoNewline
    $inputName = Read-Host
    if (-not $inputName.Trim()) {
        if ($added -eq 0) { Write-Host "  再见~" -ForegroundColor DarkGray; break }
        break
    }

    $validAmount = $false
    do {
        Write-Host "  金额   " -NoNewline
        $inputAmount = Read-Host
        if ($inputAmount -as [double] -le 0) {
            Write-Host "  ✗ 金额必须大于 0" -ForegroundColor Red
        } else {
            $validAmount = $true
        }
    } while (-not $validAmount)

    Write-Host ""
    Write-Host "  选择平台 (← → 切换, 数字键直选, Enter 确认):" -ForegroundColor DarkGray
    $platform = Select-Option "  平台" $platforms $lastPlatform
    if (-not $platform) { break }

    Write-Host ""
    Write-Host "  选择币种 (← → 切换, 数字键直选, Enter 确认):" -ForegroundColor DarkGray
    $currency = Select-Option "  币种" $currencies $lastCurrency
    if (-not $currency) { break }

    Write-Host ""
    $defaultTs = Get-DonationTimestamp
    Write-Host "  时间   (Enter 使用当前时间: $defaultTs) " -NoNewline
    $inputDate = Read-Host
    $ts = if ($inputDate.Trim()) { $inputDate.Trim() } else { $defaultTs }

    Write-Host ""
    Write-Host "  留言 (可选) " -NoNewline
    $inputMessage = Read-Host

    $lastPlatform = $platform
    $lastCurrency = $currency

    # 预览确认
    Write-Host ""
    Write-Host "  ┌ 预览 ──────────────────────────┐" -ForegroundColor DarkGray
    Write-Host "  │ $ts" -ForegroundColor DarkGray
    Write-Host "  │ $inputName  |  ¥$inputAmount $currency  |  $($platforms[$platform])" -ForegroundColor White
    if ($inputMessage) {
        Write-Host "  │ 留言: $inputMessage" -ForegroundColor DarkGray
    }
    Write-Host "  └────────────────────────────────┘" -ForegroundColor DarkGray
    Write-Host ""

    if (Confirm-YesNo "  确认写入 donate.csv？") {
        $Record = @{
            Date     = $ts
            Name     = $inputName.Trim()
            Amount   = $inputAmount
            Message  = $inputMessage.Trim()
            Platform = $platform
            Currency = $currency
        }
        Add-DonationRecord $Record
        Write-Host "  ✓ 已写入！" -ForegroundColor Green
        $added++
    } else {
        Write-Host "  已取消。" -ForegroundColor DarkGray
    }
    Write-Host ""
    Write-Host "  ─────────────────────────────────" -ForegroundColor DarkGray
    Write-Host ""
}

if ($added -gt 0) {
    Write-Host "  共追加 $added 条记录。" -ForegroundColor Cyan
    Write-Host ""
    $doCommit = if ($Commit) { $true } else { Confirm-YesNo "  是否提交到 Git？" }
    if ($doCommit) { Sync-GitCommit }
} else {
    Write-Host "  没有追加任何记录。" -ForegroundColor DarkGray
}
} catch {
    Write-Host "`n  ✗ 出错了: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "  文件: $CsvPath" -ForegroundColor DarkGray
    Write-Host "  请确认 donate.csv 与此脚本在同一目录。" -ForegroundColor DarkGray
}
if (Test-LaunchedFromExplorer) { Read-Host "按 Enter 退出" }
