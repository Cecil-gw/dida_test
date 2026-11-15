using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Util.Files;

public class FileUtils
{
	public static bool FileEmptyOrNotExists(string path)
	{
		if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
		{
			if (File.Exists(path))
			{
				return new FileInfo(path).Length == 0;
			}
			return false;
		}
		return true;
	}

	public static DateTime FileModifyTime(string path)
	{
		if (!FileExists(path))
		{
			return default(DateTime);
		}
		return new FileInfo(path).LastWriteTime;
	}

	public static bool FileExists(string path)
	{
		return File.Exists(path);
	}

	public static bool FileInFolder(string path, string folder)
	{
		if (string.IsNullOrEmpty(path) || string.IsNullOrWhiteSpace(path))
		{
			return false;
		}
		if (string.IsNullOrEmpty(folder) || string.IsNullOrWhiteSpace(folder))
		{
			return false;
		}
		return path.StartsWith(folder);
	}

	public static bool FolderExists(string path)
	{
		return Directory.Exists(path);
	}

	public static double FileSize(string path)
	{
		if (!File.Exists(path))
		{
			return 0.0;
		}
		return (float)new FileInfo(path).Length / 1024f / 1024f;
	}

	public static bool FileOverSize(string path)
	{
		long userLimit = Utils.GetUserLimit(Constants.LimitKind.AttachmentSize);
		if (File.Exists(path))
		{
			return new FileInfo(path).Length > userLimit;
		}
		return false;
	}

	public static bool CountdownFileOverSize(string path)
	{
		if (File.Exists(path))
		{
			return new FileInfo(path).Length > 20480000;
		}
		return false;
	}

	public static void CollectFileSize(string path)
	{
		UserActCollectUtils.AddClickEvent("task_detail", "upload_attachment_size", FileSize(path).ToString("F2"));
	}

	public static string TrimFileName(string fileName, int maxLength)
	{
		if (fileName.Length <= maxLength)
		{
			return fileName;
		}
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
		if (fileNameWithoutExtension.Length == fileName.Length)
		{
			return fileName.Substring(0, maxLength);
		}
		string text = fileName.Substring(fileNameWithoutExtension.Length);
		return fileName.Substring(0, maxLength - text.Length) + text;
	}

	public static void ResetDbVersionFile()
	{
		string version = Utils.GetVersion();
		List<string> list = Directory.GetFiles(AppPaths.DataDir, "dbversion*.txt").ToList();
		if (list.Count == 0)
		{
			try
			{
				File.WriteAllText(AppPaths.DataDir + "dbversion" + version + ".txt", string.Empty);
				return;
			}
			catch (Exception ex)
			{
				UtilLog.Error("ResetDbVersionFile WriteAllText Error:" + ex.Message);
				return;
			}
		}
		string sourceFileName = list[0];
		try
		{
			File.Move(sourceFileName, AppPaths.DataDir + "dbversion" + version + ".txt");
		}
		catch (Exception ex2)
		{
			UtilLog.Error("ResetDbVersionFile FileMove Error:" + ex2.Message);
		}
	}

	public static double GetDbTxtVersion()
	{
		try
		{
			string[] files = Directory.GetFiles(AppPaths.DataDir, "dbversion*.txt");
			for (int i = 0; i < files.Length; i++)
			{
				if (double.TryParse(Path.GetFileNameWithoutExtension(files[i]).Replace("dbversion", ""), out var result))
				{
					return result;
				}
			}
		}
		catch (Exception ex)
		{
			UtilLog.Error("GetDbTxtVersion Error:" + ex.Message);
		}
		return 0.0;
	}

	public static string ToValidFileName(string filename)
	{
		if (string.IsNullOrEmpty(filename))
		{
			return filename;
		}
		string directoryName = Path.GetDirectoryName(filename);
		filename = filename.Substring(directoryName?.Length ?? 0);
		Regex regex = new Regex("[\\:\\*\\?\\|]+");
		return directoryName + regex.Replace(filename, "_");
	}

	public static List<string> GetPastFiles()
	{
		List<string> list = new List<string>();
		StringEnumerator enumerator = Clipboard.GetFileDropList().GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				string current = enumerator.Current;
				list.Add(current);
			}
			return list;
		}
		finally
		{
			if (enumerator is IDisposable disposable)
			{
				disposable.Dispose();
			}
		}
	}

	public static string SavePasteFile(string file, string name)
	{
		name = TrimFileName(name, 220 - AppPaths.ImageDir.Length);
		string text = AppPaths.ImageDir + name;
		if (File.Exists(text))
		{
			text = AppPaths.ImageDir + Path.GetFileNameWithoutExtension(name) + Utils.GetGuid() + Path.GetExtension(name);
		}
		if (File.Exists(file))
		{
			File.Copy(file, text, overwrite: true);
		}
		return text;
	}

	public static async Task<bool> TryDownloadBackground(string name)
	{
		if (!Directory.Exists(AppPaths.ThemeDir))
		{
			Directory.CreateDirectory(AppPaths.ThemeDir);
		}
		string localPath = AppPaths.ThemeDir + name;
		if (File.Exists(localPath) && new FileInfo(localPath).Length > 0)
		{
			return true;
		}
		string address = "https://" + BaseUrl.GetPullDomain() + "/windows/theme/background/" + name;
		if (Utils.IsNetworkAvailable())
		{
			WebClient downloader = new WebClient();
			try
			{
				await downloader.DownloadFileTaskAsync(address, localPath);
				downloader.Dispose();
				return File.Exists(localPath) && new FileInfo(localPath).Length > 0;
			}
			catch (Exception)
			{
				return false;
			}
			finally
			{
				if (File.Exists(localPath))
				{
					try
					{
						FileInfo fileInfo = new FileInfo(localPath);
						if (fileInfo.Length == 0L)
						{
							fileInfo.Attributes = FileAttributes.Normal;
							fileInfo.Delete();
						}
					}
					catch (Exception)
					{
						UtilLog.Info("TryDownloadBackground error " + name);
					}
				}
			}
		}
		return false;
	}
}
