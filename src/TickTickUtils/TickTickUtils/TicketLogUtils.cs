using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TickTickUtils.Lang;

namespace TickTickUtils;

public static class TicketLogUtils
{
	public static string GetRecentLogs()
	{
		int num = 10485760;
		StringBuilder stringBuilder = new StringBuilder(num);
		for (int i = 0; i < 4; i++)
		{
			string text = DateTime.Today.AddDays(i * -1).ToString("yyyy-MM-dd");
			string text2 = text + "_log.txt";
			string text3 = AppPaths.DataDir + text2;
			if (File.Exists(text3))
			{
				UtilLog.Info("GetRecentLogs() pos: " + stringBuilder.Length + ", file: " + text3);
				stringBuilder.Append(ReadFile(text3));
				if (stringBuilder.Length > num)
				{
					return stringBuilder.ToString();
				}
			}
			string text4 = text + "_postlog.txt";
			string text5 = AppPaths.DataDir + text4;
			if (File.Exists(text5))
			{
				UtilLog.Info("GetRecentLogs() pos: " + stringBuilder.Length + ", file: " + text3);
				stringBuilder.Append(ReadFile(text5));
				if (stringBuilder.Length > num)
				{
					return stringBuilder.ToString();
				}
			}
		}
		return stringBuilder.ToString();
	}

	private static string ReadFile(string filePath)
	{
		if (File.Exists(filePath))
		{
			using (FileStream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				byte[] array = new byte[fileStream.Length];
				int num = (int)fileStream.Length;
				int num2 = 0;
				while (num > 0)
				{
					int num3 = fileStream.Read(array, num2, num);
					if (num3 == 0)
					{
						break;
					}
					num2 += num3;
					num -= num3;
				}
				return Encoding.UTF8.GetString(array);
			}
		}
		return string.Empty;
	}

	public static void RemoveTenDaysAgoLogs()
	{
		List<string> list = Directory.GetFiles(AppPaths.DataDir, "*_*log.txt").ToList();
		for (int i = 0; i < 10; i++)
		{
			string text = DateTime.Today.AddDays(i * -1).ToString("yyyy-MM-dd") + "_log.txt";
			string text2 = AppPaths.DataDir + text;
			if (File.Exists(text2))
			{
				list.Remove(text2);
			}
			string text3 = DateTime.Today.AddDays(i * -1).ToString("yyyy-MM-dd") + "_postlog.txt";
			string text4 = AppPaths.DataDir + text3;
			if (File.Exists(text4))
			{
				list.Remove(text4);
			}
		}
		foreach (string item in list)
		{
			if (File.Exists(item))
			{
				try
				{
					FileInfo fileInfo = new FileInfo(item);
					fileInfo.Attributes = FileAttributes.Normal;
					fileInfo.Delete();
				}
				catch
				{
				}
			}
		}
	}

	public static string GetLogString(string log)
	{
		return DateTime.Now.ToString("HH:mm:ss") + " - " + log;
	}

	public static List<string> GetUnPostLogs()
	{
		List<string> list = Directory.GetFiles(AppPaths.DataDir, "*_log.txt").ToList();
		string todayFile = DateTime.Today.ToString("yyyy-MM-dd") + "_log.txt";
		list.RemoveAll((string f) => f.EndsWith(todayFile));
		return list;
	}
}
