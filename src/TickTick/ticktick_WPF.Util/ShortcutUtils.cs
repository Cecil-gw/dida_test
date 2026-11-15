using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using IWshRuntimeLibrary;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Util;

public static class ShortcutUtils
{
	public static bool CreatShortCut(string path = "", string arg = "")
	{
		try
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				path = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
			}
			string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
			string location = Assembly.GetExecutingAssembly().Location;
			DeleteShutCut(path, "滴答清单");
			DeleteShutCut(path, "TickTick");
			string text = (Utils.IsDida() ? "滴答清单" : "TickTick");
			WshShell wshShell = (WshShell)Activator.CreateInstance(Marshal.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8")));
			IWshShortcut obj = (IWshShortcut)(dynamic)wshShell.CreateShortcut(path + "\\" + text + ".lnk");
			obj.TargetPath = location;
			obj.WorkingDirectory = baseDirectory;
			obj.WindowStyle = 1;
			obj.Description = text;
			obj.IconLocation = string.Concat(str1: string.IsNullOrEmpty(LocalSettings.Settings.Common.AppIconKey) ? AppIconKey.Default.ToString() : LocalSettings.Settings.Common.AppIconKey, str0: AppPaths.AppIconDir, str2: ".ico");
			obj.Arguments = arg;
			obj.Save();
			return true;
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
			return false;
		}
	}

	public static void DeleteShutCut(string path = "", string name = "")
	{
		try
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				path = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
			}
			if (File.Exists(path + "\\" + name + ".lnk"))
			{
				File.Delete(path + "\\" + name + ".lnk");
			}
		}
		catch (Exception)
		{
		}
	}

	public static string GetShutcutArgs(string path = "")
	{
		try
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				path = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
			}
			string text = "滴答清单";
			if (!File.Exists(path + "\\" + text + ".lnk"))
			{
				text = "TickTick";
				if (!File.Exists(path + "\\" + text + ".lnk"))
				{
					return "";
				}
			}
			WshShell wshShell = (WshShell)Activator.CreateInstance(Marshal.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8")));
			IWshShortcut wshShortcut = (IWshShortcut)(dynamic)wshShell.CreateShortcut(path + "\\" + text + ".lnk");
			if (wshShortcut != null)
			{
				return wshShortcut.Arguments;
			}
			return "";
		}
		catch (Exception)
		{
			return "";
		}
	}

	public static bool IsShortcutExit(string path = "")
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			path = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
		}
		string text = "滴答清单";
		if (!File.Exists(path + "\\" + text + ".lnk"))
		{
			text = "TickTick";
			if (!File.Exists(path + "\\" + text + ".lnk"))
			{
				return false;
			}
		}
		return true;
	}
}
