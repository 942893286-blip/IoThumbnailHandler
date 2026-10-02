using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace IoThumbnailHandler;

/// <summary>
/// Windows Shell interop definitions for the per-stream thumbnail handler.
/// </summary>
internal static class ShellInterop
{
	/// <summary>IThumbnailProvider shell extension slot (HKCR\...\ShellEx\{E357...}).</summary>
	public const string ThumbnailProviderIid = "E357FCCD-A995-4576-B01F-234630154E96";

	/// <summary>Legacy IExtractImage slot (optional, for very old hosts).</summary>
	public const string LegacyExtractImageIid = "BB2E617C-0920-11D1-9A0B-00C04FC2D6C1";
}

/// <summary>
/// Exposes an IStream-based initialization so the Shell never has to open
/// (and lock) the file itself. Works with any storage (local, network, zip folders).
/// IID: B824B49D-22AC-4161-AC8A-9916E8FA3F7F
/// </summary>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("B824B49D-22AC-4161-AC8A-9916E8FA3F7F")]
internal interface IInitializeWithStream
{
	[PreserveSig]
	int Initialize(IStream pstream, uint grfMode);
}

/// <summary>
/// Receives the thumbnail bitmap.
/// IID: E357FCCD-A995-4576-B01F-234630154E96
/// </summary>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("E357FCCD-A995-4576-B01F-234630154E96")]
internal interface IThumbnailProvider
{
	[PreserveSig]
	int GetThumbnail(uint cx, out IntPtr hBitmap, out WtsAlphaType pdwFlags);
}

internal enum WtsAlphaType : uint
{
	Unknown = 0,
	Rgb = 1,
	Alpha = 2
}
