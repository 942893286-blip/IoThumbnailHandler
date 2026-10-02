# IoThumbnailHandler

Windows Explorer **thumbnail provider** for BrickLink Studio `.io` files.

A `.io` file is a ZIP archive that normally embeds a rendered preview named
`thumbnail.png`. Windows has no built-in handler for this format, so Explorer
shows a generic icon. This project is a small Shell extension that extracts the
embedded preview and displays it as the file's thumbnail.

![platform: Windows](https://img.shields.io/badge/platform-Windows%2010%2F11-0078D4)
![runtime: .NET Framework 4.8](https://img.shields.io/badge/runtime-.NET%20Framework%204.8-512BD4)
![license: MIT](https://img.shields.io/badge/license-MIT-green)

---

## Features

- Shows **each file's own** `thumbnail.png` (not one shared icon)
- Implements `IInitializeWithStream`, so the original `.io` file is **never locked**
- Works with files on local disks, network shares and other Shell namespaces
- High-quality bicubic scaling with correct alpha (transparency) handling
- Fully scriptable install / uninstall (PowerShell, no third-party tools needed)
- Zero external dependencies — only the .NET Framework / GDI+ (included in Windows)

## How it works

```
Explorer enumerates a folder and finds a .io file
        |
        v
Shell looks up  .io\ShellEx\{E357FCCD-...}   (IThumbnailProvider slot)
        |
        v
Loads IoThumbnailHandler.dll inside a surrogate (dllhost.exe)
        |
        v
IInitializeWithStream.Initialize(fileStream)
        |-- treat the stream as a ZIP, read thumbnail.png
        v
IThumbnailProvider.GetThumbnail(cx)
        |-- decode, scale to cx with alpha, return HBITMAP
        v
Explorer renders (and caches) the thumbnail
```

The handler is a classic unmanaged COM server hosted by `mscoree.dll`; it is
**not** a BepInEx plugin and does not run inside Studio. It is loaded only by
the Shell (typically inside `dllhost.exe`).

## Requirements

- Windows 10 / 11 x64
- [.NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework/net48)
  (preinstalled on up-to-date Windows 10/11)
- Administrator rights for installation (COM registration is under `HKLM`)
- To build: the [.NET SDK](https://dotnet.microsoft.com/download) (6.0 or newer)

## Repository layout

```
IoThumbnailHandler/
├─ src/IoThumbnailHandler/
│  ├─ IoThumbnailHandler.csproj
│  ├─ IoThumbnailProvider.cs   # COM class: extract + render
│  └─ ShellInterop.cs          # IThumbnailProvider / IInitializeWithStream
├─ scripts/
│  ├─ Install.ps1
│  └─ Uninstall.ps1
└─ IoThumbnailHandler.sln
```

## Build

```powershell
dotnet build -c Release
```

Output: `src\IoThumbnailHandler\bin\Release\net48\IoThumbnailHandler.dll`

The project also compiles on Linux/macOS build agents (reference assemblies are
restored automatically on non-Windows), although the resulting DLL only runs
on Windows.

## Install

1. Build the DLL (above).
2. Open PowerShell **as Administrator**, go to the `scripts` folder and run:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install.ps1
```

If the DLL is somewhere else:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install.ps1 -DllPath "C:\path\to\IoThumbnailHandler.dll"
```

3. Open a folder containing `.io` files and switch to Medium/Large icons.

Alternative with RegAsm (dev machines):

```powershell
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe /codebase IoThumbnailHandler.dll
```

## Uninstall

```powershell
powershell -ExecutionPolicy Bypass -File .\Uninstall.ps1
```

or

```powershell
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe /u IoThumbnailHandler.dll
```

## Troubleshooting

**Explorer still shows the generic icon**

- Press **F5** to refresh the folder.
- Thumbnails are cached. Clear the cache (close Explorer first):
  delete `%LocalAppData%\Microsoft\Windows\Explorer\thumbcache_*.db`,
  then restart Explorer / sign out and back in.
- Make sure the view is set to show thumbnails (View → Medium/Large icons).

**The handler is never called**

- It must be registered under **HKLM**; per-user (`HKCU`) activation of an
  `mscoree`-hosted server typically fails with `0x80070002`.
- The assembly must have an explicit version (`1.0.0.0`) matching the
  `Assembly` value used in registration.
- The `IInitializeWithStream` interface IID is
  `{B824B49D-22AC-4161-AC8A-9916E8FA3F7F}`. A single wrong character makes the
  Shell reject the handler with `E_NOINTERFACE`.

**Some files show the icon, others the thumbnail**

Files whose archive lacks a valid `thumbnail.png` (or whose PNG is corrupt)
intentionally fall back to the type's default icon.

## The .io format

`.io` is a ZIP archive typically containing:

| Entry            | Purpose                          |
| ---------------- | -------------------------------- |
| `thumbnail.png`  | Rendered preview (used here)     |
| `model.ldr` / `modelv2.ldr` | LDraw scene           |
| `model.lxfml`    | Studio model markup              |
| `model.ins`      | Instruction maker data           |
| `.info`          | Archive metadata                 |

## License

[MIT](LICENSE).

## Acknowledgements

- Microsoft Learn — [IThumbnailProvider](https://learn.microsoft.com/windows/win32/api/thumbcache/nn-thumbcache-ithumbnailprovider)
  and [IInitializeWithStream](https://learn.microsoft.com/windows/win32/api/propsys/nn-propsys-iinitializewithstream)
- [SharpShell](https://github.com/dwmkerr/sharpshell) — reference Shell-extension framework used during development
