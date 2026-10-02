# ============================================================================
# Uninstall.ps1 - Remove IoThumbnailHandler registration
# Run as Administrator:
#   powershell -ExecutionPolicy Bypass -File .\Uninstall.ps1
# ============================================================================
#Requires -RunAsAdministrator
[CmdletBinding()]
param()

$Clsid      = '{A7E14C32-5D2E-4F9B-8C6A-2B71D904E6F1}'
$ProviderIid = '{E357FCCD-A995-4576-B01F-234630154E96}'
$LegacyIid   = '{BB2E617C-0920-11D1-9A0B-00C04FC2D6C1}'

# COM server
Remove-Item "HKLM:\Software\Classes\CLSID\$Clsid" -Recurse -Force -ErrorAction SilentlyContinue

# Extension bindings
foreach ($hive in 'HKLM:\Software\Classes', 'HKCU:\Software\Classes') {
    Remove-Item "$hive\.io\ShellEx\$ProviderIid" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item "$hive\.io\ShellEx\$LegacyIid"   -Recurse -Force -ErrorAction SilentlyContinue
}

# Approved entry
$approved = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved'
Remove-ItemProperty -Path $approved -Name $Clsid -ErrorAction SilentlyContinue

# Notify Shell
$src = @"
using System;
using System.Runtime.InteropServices;
public static class ShellNotify2 {
    [DllImport("shell32.dll")]
    public static extern void SHChangeNotify(uint e, uint f, IntPtr a, IntPtr b);
}
"@
Add-Type -TypeDefinition $src
[ShellNotify2]::SHChangeNotify(0x08000000, 0, [IntPtr]::Zero, [IntPtr]::Zero)

Write-Host 'Unregistered. DLL files on disk are left untouched.' -ForegroundColor Green
