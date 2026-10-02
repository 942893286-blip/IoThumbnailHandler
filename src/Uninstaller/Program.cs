using System;

namespace IoThumbnailHandler;

internal static class UninstallProgram
{
	private static int Main()
	{
		try
		{
			Registration.Uninstall();
			Console.WriteLine();
			Console.WriteLine("卸载完成，缩略图扩展的注册表项已清除。");
			Console.WriteLine("磁盘上的 DLL / EXE 文件不会被删除，可手工删除整个文件夹。");
			Pause();
			return 0;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine("卸载失败：");
			Console.Error.WriteLine(ex);
			Pause();
			return 1;
		}
	}

	private static void Pause()
	{
		try
		{
			Console.WriteLine();
			Console.WriteLine("按任意键关闭...");
			Console.ReadKey();
		}
		catch
		{
		}
	}
}
