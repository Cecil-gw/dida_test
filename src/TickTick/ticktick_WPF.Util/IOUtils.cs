using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Resources;
using TickTickUtils.Lang;

namespace ticktick_WPF.Util;

public static class IOUtils
{
	public static string GetFileContentInAssemblyFold(string fileName)
	{
		string result = string.Empty;
		string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		if (directoryName != null)
		{
			result = GetFileContent(Path.Combine(directoryName, fileName));
		}
		return result;
	}

	private static string GetFileContent(string path)
	{
		string result = string.Empty;
		if (File.Exists(path))
		{
			StreamReader streamReader = new StreamReader(path);
			result = streamReader.ReadToEnd();
			streamReader.Close();
		}
		return result;
	}

	public static string GetFileContentInResourceFile(string filePath)
	{
		StreamResourceInfo resourceStream = Application.GetResourceStream(new Uri("pack://application:,,," + filePath));
		if (resourceStream != null)
		{
			using (StreamReader streamReader = new StreamReader(resourceStream.Stream, Encoding.UTF8))
			{
				return streamReader.ReadToEnd();
			}
		}
		return string.Empty;
	}

	public static async Task<bool> CheckResourceExist(string dir, string fileName, string remotePath)
	{
		if (!Directory.Exists(dir))
		{
			Directory.CreateDirectory(dir);
		}
		string localPath = dir + fileName;
		return await Utils.TryDownloadFile(remotePath, localPath);
	}

	public static async Task<bool> DownloadFile(string dir, string fileName, string remotePath)
	{
		if (!Directory.Exists(dir))
		{
			Directory.CreateDirectory(dir);
		}
		string localPath = dir + fileName;
		return await Utils.TryDownloadFile(remotePath, localPath, ignoreExist: true);
	}

	public static void DeleteFile(string path)
	{
		try
		{
			if (File.Exists(path))
			{
				FileInfo fileInfo = new FileInfo(path);
				fileInfo.Attributes = FileAttributes.Normal;
				fileInfo.Delete();
			}
		}
		catch (Exception ex)
		{
			UtilLog.Warn("DeleteFileError " + ex.Message);
		}
	}

	public static string GetFileName(string path)
	{
		return path?.Split('\\').Last().Split('/')
			.Last();
	}

	public static byte[] ReadBytes(Func<byte[], int, int, int> readFunc, int bufLen)
	{
		byte[] array = new byte[bufLen];
		using MemoryStream memoryStream = new MemoryStream();
		while (true)
		{
			int num = readFunc(array, 0, array.Length);
			if (num == 0)
			{
				break;
			}
			memoryStream.Write(array, 0, num);
		}
		return memoryStream.ToArray();
	}

	public static string ReadString(Func<byte[], int, int, int> readFunc, Encoding encoding = null)
	{
		byte[] array = ReadBytes(readFunc, 1024);
		encoding = encoding ?? Encoding.Unicode;
		return encoding.GetString(array, 0, array.Length);
	}
}
