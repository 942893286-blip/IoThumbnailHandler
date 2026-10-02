# ============================================================================
# Enable-WindowsSandbox.ps1
# Run on the Windows host (the machine where you want Windows Sandbox).
# Right-click PowerShell -> Run as Administrator, then:
#   powershell -ExecutionPolicy Bypass -File .\Enable-WindowsSandbox.ps1
# Requires Windows 10/11 PRO, Enterprise or Education (not Home).
# ============================================================================
#Requires -RunAsAdministrator
[CmdletBinding()]
param()

$feature = 'Containers-DisposableClientVM'
$state = Get-WindowsOptionalFeature -Online -FeatureName $feature

if ($state.State -eq 'Enabled') {
    Write-Host 'Windows Sandbox is already enabled.' -ForegroundColor Green
    exit 0
}

Write-Host 'Enabling Windows Sandbox feature...'
Enable-WindowsOptionalFeature -Online -FeatureName $feature -All -NoRestart

Write-Host ''
Write-Host 'Done. You must RESTART the computer once.' -ForegroundColor Yellow
Write-Host 'After reboot, double-click IoThumbnailTest.wsb to run the handler test.'
$restart = Read-Host 'Restart now? (y/N)'
if ($restart -eq 'y' -or $restart -eq 'Y') {
    Restart-Computer
}
