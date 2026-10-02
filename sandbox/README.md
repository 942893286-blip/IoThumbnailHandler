# Windows Sandbox test kit (Windows 沙盒测试包)

This folder lets you verify IoThumbnailHandler on a **completely clean Windows**
(Windows Sandbox), so it proves the handler works on anyone's computer.

## 0. Requirements

- Windows 10/11 **PRO / Enterprise / Education** (Windows Home cannot use Sandbox)
- Administrator rights

## 1. Enable Windows Sandbox (once)

Open PowerShell **as Administrator** in this folder:

```powershell
powershell -ExecutionPolicy Bypass -File .\Enable-WindowsSandbox.ps1
```

Restart when prompted. (Equivalent GUI path: Settings → Optional Features →
More Windows features → tick "Windows Sandbox".)

## 2. Prepare the test folder

Create this exact folder:

```
C:\Users\Administrator\Desktop\IoThumbSandboxTest
```

and put into it:

```
IoThumbSandboxTest\
├─ payload\
│  ├─ IoThumbnailHandler.dll
│  ├─ IoThumbnailHandler_Install.exe
│  └─ IoThumbnailHandler_Uninstall.exe
├─ testfiles\                 (drop 3-5 real .io files here)
│  └─ *.io
├─ SandboxTest.ps1
└─ IoThumbnailTest.wsb
```

(The three payload files come from the release zip; change the HostFolder path
inside the .wsb if your Windows user is not "Administrator".)

## 3. Run

Double-click **`IoThumbnailTest.wsb`**. The sandbox boots and automatically:

1. installs the handler (click Yes if a UAC prompt appears),
2. prints the registry verification,
3. opens the test folder — press **Ctrl+Shift+2** for large icons and confirm
   every `.io` shows its own preview,
4. press Enter in the PowerShell window → it uninstalls and verifies the
   registry is empty, reopens the folder (F5) so you can see the fallback icon.

Close the sandbox window afterwards; nothing is persisted.
