using System;
using System.IO;

namespace IoThumbnailHandler;

internal static class InstallProgram
{
	private static int Main(string[] args)
	{
		try
		{
			string dllPath = args.Length > 0
				? args[0]
				: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IoThumbnailHandler.dll");

			Registration.Install(dllPath);

			Console.WriteLine();
			Console.WriteLine("IoThumbnailHandler installed successfully.");
			Console.WriteLine("Open a folder with .io files and switch to Medium/Large icons.");
			Console.WriteLine("If nothing changes, press F5 or clear the thumbnail cache.");
			return 0;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine();
			Console.Error.WriteLine("Installation failed: " + ex.Message);
			return 1;
		}
		finally
		{
			PauseIfInteractive();
		}
	}

	private static void PauseIfInteractive()
	{
		try
		{
			Console.WriteLine();
			Console.WriteLine("Press any key to close this window...");
			Console.ReadKey();
		}
		catch
		{
			// Non-interactive (scripted) run: nothing to wait for
		}
	}
}
