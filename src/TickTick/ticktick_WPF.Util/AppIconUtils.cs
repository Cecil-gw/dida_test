using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using IWshRuntimeLibrary;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Util;

public class AppIconUtils
{
	[ComImport]
	[Guid("00021401-0000-0000-C000-000000000046")]
	internal class ShellLink
	{
	}

	[ComImport]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("000214F9-0000-0000-C000-000000000046")]
	internal interface IShellLink
	{
		void GetPath([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, IntPtr pfd, int fFlags);

		void GetIDList(out IntPtr ppidl);

		void SetIDList(IntPtr pidl);

		void GetDescription([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);

		void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);

		void GetWorkingDirectory([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);

		void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);

		void GetArguments([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);

		void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

		void GetHotkey(out short pwHotkey);

		void SetHotkey(short wHotkey);

		void GetShowCmd(out int piShowCmd);

		void SetShowCmd(int iShowCmd);

		void GetIconLocation([MarshalAs(UnmanagedType.LPWStr)] out StringBuilder pszIconPath, int cchIconPath, out int iIcon);

		void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);

		void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);

		void Resolve(IntPtr hwnd, int fFlags);

		void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
	}

	private const int MAX_PATH = 260;

	private const int CSIDL_COMMON_DESKTOPDIRECTORY = 25;

	public static async Task SetAppIcons(string key, bool checkDefault = false)
	{
		await Task.Yield();
		TryDeleteCommonProgramsLink();
		if (checkDefault && !string.IsNullOrEmpty(LocalSettings.Settings.Common.AppIconKey) && LocalSettings.Settings.Common.AppIconKey == AppIconKey.Default.ToString())
		{
			return;
		}
		if (string.IsNullOrEmpty(key))
		{
			key = AppIconKey.Default.ToString();
		}
		App.Instance.SetNotifyIcon(isLock: false);
		Task.Run(delegate
		{
			Utils.LogActionTimes(delegate
			{
				SetLinks(key);
			}, 2000, null, "SetLinks");
		});
		SystemToastUtils.SetLogoUrl();
	}

	public static void SetLinks(string key)
	{
		string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
		HashSet<string> checkedDir = new HashSet<string>();
		SetLnkIcons(folderPath, checkedDir, key);
		if (App.IsAdmin)
		{
			SetLnkIcons(GetAllUsersDesktopFolderPath(), checkedDir, key);
			SetLnkIcons(Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms), checkedDir, key);
		}
		SetLnkIcons(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), checkedDir, key, ignoreChildFold: true);
		SetLnkIcons(Environment.GetFolderPath(Environment.SpecialFolder.Startup), checkedDir, key);
	}

	private static int SetLnkIcons(string foldPath, HashSet<string> checkedDir, string key, bool ignoreChildFold = false, int limit = 200)
	{
		try
		{
			if (Directory.Exists(foldPath) && !checkedDir.Contains(foldPath))
			{
				checkedDir.Add(foldPath);
				string[] files = Directory.GetFiles(foldPath, "*.lnk");
				if (files.Length != 0)
				{
					string[] array = files;
					foreach (string text in array)
					{
						if (limit < 0)
						{
							return limit;
						}
						if (!Utils.IsWindows11())
						{
							IShellLink shellLink = (IShellLink)new CShellLink();
							IPersistFile persistFile = (IPersistFile)shellLink;
							persistFile.Load(text, 2);
							StringBuilder stringBuilder = new StringBuilder(1024);
							shellLink.GetPath(stringBuilder, stringBuilder.Capacity, IntPtr.Zero, 2);
							string text2 = stringBuilder.ToString();
							stringBuilder.Clear();
							if (text2.Contains(AppPaths.ExeDir))
							{
								UtilLog.Info("SetIconInWindows10");
								string pszIconPath = AppPaths.AppIconDir + key + ".ico";
								shellLink.SetIconLocation(pszIconPath, 0);
								persistFile.Save(text, fRemember: true);
							}
							Marshal.ReleaseComObject(shellLink);
							Marshal.ReleaseComObject(persistFile);
							limit--;
							continue;
						}
						WshShell wshShell = (WshShell)Activator.CreateInstance(Marshal.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8")));
						IWshShortcut wshShortcut = (IWshShortcut)(dynamic)wshShell.CreateShortcut(text);
						if (wshShortcut.TargetPath.Contains(AppPaths.ExeDir) || wshShortcut.FullName.Contains("滴答清单") || wshShortcut.FullName.Contains("TickTick"))
						{
							string text3 = AppPaths.AppIconDir + key + ".ico";
							if (!wshShortcut.IconLocation.Contains(text3))
							{
								wshShortcut.IconLocation = text3;
							}
							wshShortcut.Save();
						}
						limit--;
					}
				}
				if (!ignoreChildFold)
				{
					string[] directories = Directory.GetDirectories(foldPath);
					if (directories.Length != 0)
					{
						string[] array = directories;
						foreach (string foldPath2 in array)
						{
							if (limit < 0)
							{
								return limit;
							}
							limit = SetLnkIcons(foldPath2, checkedDir, key, ignoreChildFold: true, limit);
						}
					}
				}
			}
		}
		catch (Exception)
		{
			UtilLog.Info("SetLnkIcons" + foldPath);
		}
		return limit;
	}

	private static void TryDeleteCommonProgramsLink()
	{
		try
		{
			if (!string.IsNullOrEmpty(GetLnkPath(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu))))
			{
				string lnkPath = GetLnkPath(Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms), checkChild: true);
				if (File.Exists(lnkPath))
				{
					File.Delete(lnkPath);
				}
			}
		}
		catch (Exception ex)
		{
			UtilLog.Info("TryDeleteCommonProgramsLink   " + ex.Message);
		}
	}

	private static string GetLnkPath(string foldPath, bool checkChild = false)
	{
		if (Directory.Exists(foldPath))
		{
			string[] files = Directory.GetFiles(foldPath, "*.lnk");
			if (files.Length != 0)
			{
				string[] array = files;
				foreach (string text in array)
				{
					WshShell wshShell = (WshShell)Activator.CreateInstance(Marshal.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8")));
					if (((IWshShortcut)(dynamic)wshShell.CreateShortcut(text)).TargetPath.Contains(AppPaths.ExeDir))
					{
						return text;
					}
				}
			}
			if (checkChild)
			{
				string[] directories = Directory.GetDirectories(foldPath);
				if (directories.Length != 0)
				{
					string[] array = directories;
					foreach (string text2 in array)
					{
						string value = "滴答清单";
						if (!text2.Contains(value))
						{
							continue;
						}
						string[] files2 = Directory.GetFiles(text2, "*.lnk");
						if (files2.Length == 0)
						{
							continue;
						}
						string[] array2 = files2;
						foreach (string text3 in array2)
						{
							WshShell wshShell2 = (WshShell)Activator.CreateInstance(Marshal.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8")));
							if (((IWshShortcut)(dynamic)wshShell2.CreateShortcut(text3)).TargetPath.Contains(AppPaths.ExeDir))
							{
								return text3;
							}
						}
					}
				}
			}
		}
		return null;
	}

	[DllImport("shfolder.dll", CharSet = CharSet.Auto)]
	private static extern int SHGetFolderPath(IntPtr hwndOwner, int nFolder, IntPtr hToken, int dwFlags, StringBuilder lpszPath);

	public static string GetAllUsersDesktopFolderPath()
	{
		StringBuilder stringBuilder = new StringBuilder(260);
		SHGetFolderPath(IntPtr.Zero, 25, IntPtr.Zero, 0, stringBuilder);
		return stringBuilder.ToString();
	}

	public static void TrySetToastIcon()
	{
		string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
		string text = LocalSettings.Settings.Common.AppIconKey;
		if (string.IsNullOrEmpty(text))
		{
			text = AppIconKey.Default.ToString();
		}
		SetLnkIcons(folderPath, new HashSet<string>(), text, ignoreChildFold: true);
	}

	public static BitmapImage GetIconImage()
	{
		Enum.TryParse<AppIconKey>(LocalSettings.Settings.Common.AppIconKey, out var result);
		if (result == AppIconKey.Default)
		{
			return new BitmapImage(new Uri("pack://application:,,,/Resources/Default.ico"));
		}
		return new BitmapImage(new Uri("pack://application:,,,/Resources/" + result.ToString() + ".ico"));
	}
}
