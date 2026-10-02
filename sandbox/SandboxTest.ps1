# ============================================================================
# SandboxTest.ps1 - runs INSIDE Windows Sandbox (auto-started by the .wsb)
# 1) installs IoThumbnailHandler,  2) lets you visually verify thumbnails,
# 3) uninstalls,                  4) verifies registry rollback.
# ============================================================================
$ErrorActionPreference = 'Continue'

$Clsid      = '{A7E14C32-5D2E-4F9B-8C6A-2B71D904E6F1}'
$Provider   = '{E357FCCD-A995-4576-B01F-234630154E96}'
$desktop    = 'C:\Users\WDAGUtilityAccount\Desktop'
$handlerDir = 'C:\Handler'
$ioDir      = Join-Path $desktop 'io-test'

Write-Host '=== IoThumbnailHandler sandbox test ===' -ForegroundColor Cyan

# ---- prepare ---------------------------------------------------------------
New-Item -ItemType Directory -Force -Path $handlerDir | Out-Null
New-Item -ItemType Directory -Force -Path $ioDir | Out-Null

Copy-Item 'C:\Test\payload\*' $handlerDir -Recurse -Force
if (Test-Path 'C:\Test\testfiles') {
    Copy-Item 'C:\Test\testfiles\*.io' $ioDir -Force -ErrorAction SilentlyContinue
}

$ioCount = (Get-ChildItem $ioDir -Filter *.io -ErrorAction SilentlyContinue).Count
if ($ioCount -eq 0) {
    Write-Host 'No .io files found in testfiles - visual check will be skipped.' -ForegroundColor Yellow
}

# ---- 1) install ------------------------------------------------------------
Write-Host "`n[1/4] Installing..." -ForegroundColor Cyan
$p = Start-Process (Join-Path $handlerDir 'IoThumbnailHandler_Install.exe') `
    -Verb RunAs -Wait -PassThru
Write-Host ("Installer exit code: {0}" -f $p.ExitCode)

# ---- 2) verify registry ----------------------------------------------------
Write-Host "`n[2/4] Verifying registry..." -ForegroundColor Cyan
$checks = [ordered]@{
    'CLSID default'        = (Get-Item "HKLM:\Software\Classes\CLSID\$Clsid" -ErrorAction SilentlyContinue).GetValue('')
    'InprocServer32'       = (Get-Item "HKLM:\Software\Classes\CLSID\$Clsid\InprocServer32" -ErrorAction SilentlyContinue).GetValue('')
    'CodeBase'             = (Get-Item "HKLM:\Software\Classes\CLSID\$Clsid\InprocServer32" -ErrorAction SilentlyContinue).GetValue('CodeBase')
    '.io binding (HKLM)'   = (Get-Item "HKLM:\Software\Classes\.io\ShellEx\$Provider" -ErrorAction SilentlyContinue).GetValue('')
    '.io binding (HKCU)'   = (Get-Item "HKCU:\Software\Classes\.io\ShellEx\$Provider" -ErrorAction SilentlyContinue).GetValue('')
}
$checks.GetEnumerator() | ForEach-Object { Write-Host ('  {0,-22} = {1}' -f $_.Key, $_.Value) }

# ---- 3) visual check -------------------------------------------------------
if ($ioCount -gt 0) {
    Write-Host "`n[3/4] Opening test folder..." -ForegroundColor Cyan
    Start-Process explorer.exe $ioDir
    Write-Host '  In the Explorer window press Ctrl+Shift+2 (Large icons).' -ForegroundColor Yellow
    Write-Host '  Confirm every .io shows ITS OWN rendered preview.' -ForegroundColor Yellow
    Read-Host "`n  Press Enter here when you have confirmed the thumbnails"
} else {
    Write-Host "`n[3/4] Visual check skipped." -ForegroundColor Yellow
}

# ---- 4) uninstall + verify rollback ---------------------------------------
Write-Host "`n[4/4] Uninstalling..." -ForegroundColor Cyan
$u = Start-Process (Join-Path $handlerDir 'IoThumbnailHandler_Uninstall.exe') `
    -Verb RunAs -Wait -PassThru
Write-Host ("Uninstaller exit code: {0}" -f $u.ExitCode)

Start-Sleep 1
$leftovers = @(
    "HKLM:\Software\Classes\CLSID\$Clsid"
    "HKLM:\Software\Classes\.io\ShellEx\$Provider"
    "HKCU:\Software\Classes\.io\ShellEx\$Provider"
) | Where-Object { Test-Path $_ }

Write-Host "`n=== RESULT ===" -ForegroundColor Cyan
if ($leftovers.Count -eq 0) {
    Write-Host 'Registry fully cleaned. .io files now fall back to the default icon.' -ForegroundColor Green
} else {
    Write-Host 'Leftover keys found:' -ForegroundColor Red
    $leftovers | ForEach-Object { Write-Host "  $_" }
}
if ($ioCount -gt 0) {
    Start-Process explorer.exe $ioDir
    Write-Host 'Explorer reopened - press F5 to see the fallback icons.'
}
Write-Host "`nTest finished. You can close the sandbox window (nothing is saved)." -ForegroundColor Cyan
Read-Host 'Press Enter to close'
