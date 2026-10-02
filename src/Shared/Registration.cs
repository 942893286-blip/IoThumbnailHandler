using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace IoThumbnailHandler;

/// <summary>
/// Registry-based install/uninstall logic, shared by the install and uninstall exes.
/// Kept as linked source so each exe is fully self-contained.
/// </summary>
internal static class Registration
{
	public const string Clsid = "{A7E14C32-5D2E-4F9B-8C6A-2B71D904E6F1}";
	public const string ClassName = "IoThumbnailHandler.IoThumbnailProvider";
	public const string Assembly = "IoThumbnailHandler, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
	public const string ProviderIid = "{E357FCCD-A995-4576-B01F-234630154E96}";
	public const string LegacyIid = "{BB2E617C-0920-11D1-9A0B-00C04FC2D6C1}";
	public const string Category = "{62C8FE65-4EB0-442F-A837-4B39CF4F2DD2}";

	public static void Install(string dllPath)
	{
		if (!File.Exists(dllPath))
		{
			throw new FileNotFoundException("IoThumbnailHandler.dll not found", dllPath);
		}
		string codeBase = new Uri(Path.GetFullPath(dllPath)).AbsoluteUri;

		// 1) COM server under HKLM (mscoree host)
		using (RegistryKey clsidKey = Registry.LocalMachine.CreateSubKey(@"Software\Classes\CLSID\" + Clsid))
		{
			clsidKey.SetValue("", "Studio IO Thumbnail Provider");
		}
		using (RegistryKey inproc = Registry.LocalMachine.CreateSubKey(
			@"Software\Classes\CLSID\" + Clsid + @"\InprocServer32"))
		{
			inproc.SetValue("", "mscoree.dll");
			inproc.SetValue("ThreadingModel", "Both");
			inproc.SetValue("Class", ClassName);
			inproc.SetValue("Assembly", Registration.Assembly);
			inproc.SetValue("RuntimeVersion", "v4.0.30319");
			inproc.SetValue("CodeBase", codeBase);
		}
		using (RegistryKey progid = Registry.LocalMachine.CreateSubKey(
			@"Software\Classes\CLSID\" + Clsid + @"\ProgID"))
		{
			progid.SetValue("", ClassName);
		}
		Registry.LocalMachine.CreateSubKey(
			@"Software\Classes\CLSID\" + Clsid + @"\Implemented Categories\" + Category).Dispose();

		// 2) Bind handler to .io in both HKLM and HKCU
		foreach (RegistryKey classes in new[] { Registry.LocalMachine.OpenSubKey("Software", writable: true),
												Registry.CurrentUser.OpenSubKey("Software", writable: true) })
		{
			using (classes)
			using (RegistryKey p = classes.CreateSubKey(@"Classes\.io\ShellEx\" + ProviderIid))
			{
				p.SetValue("", Clsid);
			}
			using (RegistryKey l = classes.CreateSubKey(@"Classes\.io\ShellEx\" + LegacyIid))
			{
				l.SetValue("", Clsid);
			}
		}

		// 3) Approved list
		using (RegistryKey approved = Registry.LocalMachine.CreateSubKey(
			@"SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved"))
		{
			approved.SetValue(Clsid, "Studio IO Thumbnail Provider");
		}

		NotifyShell();
	}

	public static void Uninstall()
	{
		Registry.LocalMachine.DeleteSubKeyTree(@"Software\Classes\CLSID\" + Clsid, throwOnMissingSubKey: false);

		foreach (RegistryKey classes in new[] { Registry.LocalMachine.OpenSubKey("Software", writable: true),
												Registry.CurrentUser.OpenSubKey("Software", writable: true) })
		{
			using (classes)
			{
				classes.DeleteSubKeyTree(@"Classes\.io\ShellEx\" + ProviderIid, throwOnMissingSubKey: false);
				classes.DeleteSubKeyTree(@"Classes\.io\ShellEx\" + LegacyIid, throwOnMissingSubKey: false);
			}
		}

		using (RegistryKey approved = Registry.LocalMachine.OpenSubKey(
			@"SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved", writable: true))
		{
			approved?.DeleteValue(Clsid, throwOnMissingValue: false);
		}

		NotifyShell();
	}

	private static void NotifyShell()
	{
		// SHCNE_ASSOCCHANGED
		SHChangeNotify(0x08000000, 0, IntPtr.Zero, IntPtr.Zero);
	}

	[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
	private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
}
