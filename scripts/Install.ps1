# ============================================================================
# Install.ps1 - Register IoThumbnailHandler with Windows Explorer
# Right-click PowerShell -> "Run as Administrator", then:
#   powershell -ExecutionPolicy Bypass -File .\Install.ps1
# Optional: -DllPath "X:\path\to\IoThumbnailHandler.dll"
# ============================================================================
#Requires -RunAsAdministrator
[CmdletBinding()]
param(
    [string]$DllPath
)

$ErrorActionPreference = 'Stop'

$Clsid       = '{A7E14C32-5D2E-4F9B-8C6A-2B71D904E6F1}'
$ClassName    = 'IoThumbnailHandler.IoThumbnailProvider'
$Assembly     = 'IoThumbnailHandler, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
$ProviderIid  = '{E357FCCD-A995-4576-B01F-234630154E96}'
$LegacyIid    = '{BB2E617C-0920-11D1-9A0B-00C04FC2D6C1}'
$Category     = '{62C8FE65-4EB0-442F-A837-4B39CF4F2DD2}'

if (-not $DllPath) {
    $DllPath = Join-Path $PSScriptRoot '..\src\IoThumbnailHandler\bin\Release\net48\IoThumbnailHandler.dll'
}
$DllPath = (Resolve-Path $DllPath).Path
if (-not (Test-Path $DllPath)) { throw "DLL not found: $DllPath (build it first: dotnet build -c Release)" }

$CodeBase = 'file:///' + ($DllPath -replace '\\', '/')

function Write-Key($Path, $Name, $Value, $Kind = 'String') {
    New-Item -Path $Path -Force | Out-Null
    Set-ItemProperty -Path $Path -Name $Name -Value $Value -Type $Kind
}

# ---- 1) COM server (HKLM, mscoree host) -----------------------------------
Write-Key "HKLM:\Software\Classes\CLSID\$Clsid" '' 'Studio IO Thumbnail Provider'
$inproc = "HKLM:\Software\Classes\CLSID\$Clsid\InprocServer32"
Write-Key $inproc ''                'mscoree.dll'
Write-Key $inproc 'ThreadingModel'  'Both'
Write-Key $inproc 'Class'           $ClassName
Write-Key $inproc 'Assembly'        $Assembly
Write-Key $inproc 'RuntimeVersion'  'v4.0.30319'
Write-Key $inproc 'CodeBase'        $CodeBase
Write-Key "HKLM:\Software\Classes\CLSID\$Clsid\ProgID" '' $ClassName
New-Item -Path "HKLM:\Software\Classes\CLSID\$Clsid\Implemented Categories\$Category" -Force | Out-Null

# ---- 2) Bind the handler to the .io extension (HKLM + HKCU) ---------------
foreach ($hive in 'HKLM:\Software\Classes', 'HKCU:\Software\Classes') {
    Write-Key "$hive\.io\ShellEx\$ProviderIid" '' $Clsid
    Write-Key "$hive\.io\ShellEx\$LegacyIid"   '' $Clsid
}

# ---- 3) Mark the extension as approved ------------------------------------
$approved = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved'
Write-Key $approved $Clsid 'Studio IO Thumbnail Provider'

# ---- 4) Notify the Shell that associations changed ------------------------
$src = @"
using System;
using System.Runtime.InteropServices;
public static class ShellNotify {
    [DllImport("shell32.dll")]
    public static extern void SHChangeNotify(uint e, uint f, IntPtr a, IntPtr b);
}
"@
Add-Type -TypeDefinition $src
[ShellNotify]::SHChangeNotify(0x08000000, 0, [IntPtr]::Zero, [IntPtr]::Zero)

Write-Host ''
Write-Host 'Installed. Open a folder with .io files and switch to large icons.' -ForegroundColor Green
Write-Host 'If icons do not change: press F5, sign out/in, or clear the thumbnail cache.'
Write-Host 'If .io opens with another program, set the "open with" association separately.'
