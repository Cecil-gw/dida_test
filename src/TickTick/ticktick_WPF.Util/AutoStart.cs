using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace ticktick_WPF.Util;

internal class AutoStart
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

		void GetIconLocation([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);

		void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);

		void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);

		void Resolve(IntPtr hwnd, int fFlags);

		void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
	}

	private const string QuickName = "VisionProSystem";

	private string systemStartPath => Environment.GetFolderPath(Environment.SpecialFolder.Startup);

	private string appAllPath
	{
		get
		{
			string text = Process.GetCurrentProcess().MainModule.FileName;
			if (text.Contains(".vshost"))
			{
				text = text.Replace(".vshost", "");
			}
			return text;
		}
	}

	private string desktopPath => Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

	public void SetMeAutoStart(bool autostart = true, bool desktop = true)
	{
		if (autostart)
		{
			List<string> quickFromFolder = GetQuickFromFolder(systemStartPath, appAllPath);
			if (quickFromFolder.Count >= 2)
			{
				for (int i = 1; i < quickFromFolder.Count; i++)
				{
					DeleteFile(quickFromFolder[i]);
				}
			}
			else if (quickFromFolder.Count < 1)
			{
				CreatShortcut(systemStartPath, "VisionProSystem", appAllPath, "Alley@NOVO");
			}
		}
		else
		{
			List<string> quickFromFolder2 = GetQuickFromFolder(systemStartPath, appAllPath);
			if (quickFromFolder2.Count > 0)
			{
				for (int j = 0; j < quickFromFolder2.Count; j++)
				{
					DeleteFile(quickFromFolder2[j]);
				}
			}
		}
		if (desktop)
		{
			CreateDesktopQuick(desktopPath, "VisionProSystem", appAllPath);
		}
	}

	private List<string> GetQuickFromFolder(string directory, string targetPath)
	{
		List<string> list = new List<string>();
		list.Clear();
		string[] files = Directory.GetFiles(directory, "*.lnk");
		if (files == null || files.Length < 1)
		{
			return list;
		}
		for (int i = 0; i < files.Length; i++)
		{
			if (GetAppPathFromQuick(files[i]) == targetPath)
			{
				list.Add(files[i]);
			}
		}
		return list;
	}

	private string GetAppPathFromQuick(string shortcutPath)
	{
		string pszFileName = Path.Combine(desktopPath, "VisionProSystem.lnk");
		IShellLink shellLink = (IShellLink)new ShellLink();
		IPersistFile persistFile = (IPersistFile)shellLink;
		try
		{
			persistFile.Load(pszFileName, 2);
		}
		catch (Exception)
		{
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder(256);
		shellLink.GetPath(stringBuilder, stringBuilder.Capacity, IntPtr.Zero, 2);
		return stringBuilder.ToString();
	}

	private void DeleteFile(string path)
	{
		if (File.GetAttributes(path) == FileAttributes.Directory)
		{
			Directory.Delete(path, recursive: true);
		}
		else
		{
			File.Delete(path);
		}
	}

	private void CreateDesktopQuick(string DesktopPath = "", string quickName = "VisionProSystem", string AppPath = "")
	{
		DesktopPath = desktopPath;
		AppPath = appAllPath;
		if (GetQuickFromFolder(desktopPath, AppPath).Count < 1)
		{
			CreatShortcut(desktopPath, quickName, AppPath, "Alley@NOVO");
		}
	}

	private void CreatShortcut(string desktopPath, string quickname, string apppath, string description)
	{
		IShellLink obj = (IShellLink)new ShellLink();
		obj.SetDescription(description);
		obj.SetPath(apppath);
		IPersistFile obj2 = (IPersistFile)obj;
		string text = Path.Combine(desktopPath, quickname + ".lnk");
		if (File.Exists(text))
		{
			File.Delete(text);
		}
		obj2.Save(text, fRemember: false);
	}
}
