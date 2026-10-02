using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.Win32;

namespace IoThumbnailHandler;

/// <summary>
/// Windows Explorer thumbnail provider for BrickLink Studio ".io" files.
/// <para>
/// A .io file is a ZIP archive that normally contains a "thumbnail.png".
/// This class implements <see cref="IInitializeWithStream"/> (the Shell hands us
/// the file stream) and <see cref="IThumbnailProvider"/> (we return the bitmap).
/// </para>
/// </summary>
[ComVisible(true)]
[Guid("A7E14C32-5D2E-4F9B-8C6A-2B71D904E6F1")]
[ClassInterface(ClassInterfaceType.None)]
[ProgId("IoThumbnailHandler.IoThumbnailProvider")]
public sealed class IoThumbnailProvider : IInitializeWithStream, IThumbnailProvider
{
	private const int E_FAIL = unchecked((int)0x80004005);
	private const int S_OK = 0;

	private byte[] _pngBytes;
	private string _error;

	// -----------------------------------------------------------------------
	// IInitializeWithStream
	// -----------------------------------------------------------------------

	int IInitializeWithStream.Initialize(IStream stream, uint grfMode)
	{
		_pngBytes = null;
		_error = null;

		if (stream == null)
		{
			_error = "stream is null";
			return S_OK; // GetThumbnail will report failure; keeps activation simple
		}

		try
		{
			byte[] all = ReadAllFromStream(stream);
			_pngBytes = ExtractThumbnail(all);
			if (_pngBytes == null)
			{
				_error = "thumbnail.png not found in io archive";
			}
		}
		catch (Exception ex)
		{
			_error = ex.Message;
		}

		return S_OK;
	}

	// -----------------------------------------------------------------------
	// IThumbnailProvider
	// -----------------------------------------------------------------------

	int IThumbnailProvider.GetThumbnail(uint cx, out IntPtr hBitmap, out WtsAlphaType pdwFlags)
	{
		hBitmap = IntPtr.Zero;
		pdwFlags = WtsAlphaType.Unknown;

		if (_pngBytes == null)
		{
			// Shell falls back to the file type's DefaultIcon
			return E_FAIL;
		}

		Bitmap canvas = null;
		try
		{
			using (MemoryStream srcMs = new MemoryStream(_pngBytes))
			using (Bitmap orig = new Bitmap(srcMs))
			{
				int w = orig.Width;
				int h = orig.Height;
				double scale = Math.Min((double)cx / w, (double)cx / h);
				int nw = Math.Max(1, (int)Math.Round(w * scale));
				int nh = Math.Max(1, (int)Math.Round(h * scale));

				canvas = new Bitmap((int)cx, (int)cx, PixelFormat.Format32bppArgb);
				using (Graphics g = Graphics.FromImage(canvas))
				{
					g.Clear(Color.Transparent);
					g.InterpolationMode = InterpolationMode.HighQualityBicubic;
					g.PixelOffsetMode = PixelOffsetMode.HighQuality;
					g.SmoothingMode = SmoothingMode.HighQuality;
					g.CompositingQuality = CompositingQuality.HighQuality;

					int x = ((int)cx - nw) / 2;
					int y = ((int)cx - nh) / 2;
					g.DrawImage(orig, new Rectangle(x, y, nw, nh), 0, 0, w, h, GraphicsUnit.Pixel);
				}
			}

			PremultiplyAlpha(canvas);

			// Ownership of the HBITMAP is transferred to the Shell; do not delete it
			hBitmap = canvas.GetHbitmap(Color.Transparent);
			pdwFlags = WtsAlphaType.Alpha;
			return S_OK;
		}
		catch
		{
			if (hBitmap != IntPtr.Zero)
			{
				DeleteObject(hBitmap);
				hBitmap = IntPtr.Zero;
			}
			return E_FAIL;
		}
		finally
		{
			canvas?.Dispose();
		}
	}

	// -----------------------------------------------------------------------
	// Helpers
	// -----------------------------------------------------------------------

	private static byte[] ReadAllFromStream(IStream stream)
	{
		byte[] buffer = new byte[8192];
		IntPtr pcbRead = Marshal.AllocHGlobal(sizeof(int));
		try
		{
			using (MemoryStream ms = new MemoryStream())
			{
				while (true)
				{
					stream.Read(buffer, buffer.Length, pcbRead);
					int n = Marshal.ReadInt32(pcbRead);
					if (n <= 0)
					{
						break;
					}
					ms.Write(buffer, 0, n);
					if (n < buffer.Length)
					{
						break;
					}
				}
				return ms.ToArray();
			}
		}
		finally
		{
			Marshal.FreeHGlobal(pcbRead);
		}
	}

	private static byte[] ExtractThumbnail(byte[] ioBytes)
	{
		using (MemoryStream ms = new MemoryStream(ioBytes, writable: false))
		using (ZipArchive zip = new ZipArchive(ms, ZipArchiveMode.Read))
		{
			ZipArchiveEntry entry = zip.Entries.FirstOrDefault(e =>
				e.FullName.Trim('/').Equals("thumbnail.png", StringComparison.OrdinalIgnoreCase));
			if (entry == null)
			{
				return null;
			}
			using (Stream es = entry.Open())
			using (MemoryStream outMs = new MemoryStream())
			{
				es.CopyTo(outMs);
				return outMs.ToArray();
			}
		}
	}

	/// <summary>
	/// GDI's per-pixel alpha must be premultiplied; GetHbitmap does not do this.
	/// </summary>
	private static void PremultiplyAlpha(Bitmap bitmap)
	{
		BitmapData data = bitmap.LockBits(
			new Rectangle(0, 0, bitmap.Width, bitmap.Height),
			ImageLockMode.ReadWrite,
			PixelFormat.Format32bppArgb);
		try
		{
			int byteCount = Math.Abs(data.Stride) * bitmap.Height;
			byte[] pixels = new byte[byteCount];
			Marshal.Copy(data.Scan0, pixels, 0, byteCount);
			for (int i = 0; i < byteCount; i += 4)
			{
				byte a = pixels[i + 3];
				if (a == 0)
				{
					pixels[i] = 0;
					pixels[i + 1] = 0;
					pixels[i + 2] = 0;
				}
				else if (a != 255)
				{
					pixels[i] = (byte)(pixels[i] * a / 255);
					pixels[i + 1] = (byte)(pixels[i + 1] * a / 255);
					pixels[i + 2] = (byte)(pixels[i + 2] * a / 255);
				}
			}
			Marshal.Copy(pixels, 0, data.Scan0, byteCount);
		}
		finally
		{
			bitmap.UnlockBits(data);
		}
	}

	[DllImport("gdi32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool DeleteObject(IntPtr hObject);

	// -----------------------------------------------------------------------
	// Optional RegAsm hooks: "RegAsm IoThumbnailHandler.dll" also binds .io
	// The main flow uses scripts\Install.ps1 (or regsvr-style registration).
	// -----------------------------------------------------------------------

	[ComRegisterFunction]
	public static void Register(Type type)
	{
		string clsid = type.GUID.ToString("B").ToUpperInvariant();
		using (RegistryKey io = Registry.ClassesRoot.CreateSubKey(
			@".io\ShellEx\" + ShellInterop.ThumbnailProviderIid))
		{
			io.SetValue("", clsid);
		}
	}

	[ComUnregisterFunction]
	public static void Unregister(Type type)
	{
		try
		{
			Registry.ClassesRoot.DeleteSubKey(
				@".io\ShellEx\" + ShellInterop.ThumbnailProviderIid,
				throwOnMissingSubKey: false);
		}
		catch
		{
		}
	}
}
