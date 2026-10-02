using System;
using System.IO;

namespace IoThumbnailHandler;

internal static class InstallProgram
{
	private static int Main(string[] args)
	{
		try
		{
			string dll = args.Length > 0
				? args[0]
				: Path.Combine(AppContext.BaseDirectory, "IoThumbnailHandler.dll");

			if (!File.Exists(dll))
			{
				Console.Error.WriteLine("未找到 IoThumbnailHandler.dll：");
				Console.Error.WriteLine(dll);
				Console.Error.WriteLine("请把本程序与 IoThumbnailHandler.dll 放在同一文件夹。");
				Pause();
				return 1;
			}

			Registration.Install(dll);
			Console.WriteLine();
			Console.WriteLine("安装完成。");
			Console.WriteLine("打开包含 .io 文件的文件夹，切换到“大图标”即可看到各自的预览图。");
			Console.WriteLine("若没有变化：按 F5 刷新，或注销 Windows 后重新登录。");
			Pause();
			return 0;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine("安装失败：");
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
