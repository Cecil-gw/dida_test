using System;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TickTickUtils.Lang;

namespace TickTickUtils;

public static class LogUtils
{
	private const int BufferSize = 4096;

	private static readonly SemaphoreSlim Locker = new SemaphoreSlim(1, 1);

	private static readonly TimeSpan DelayTime = TimeSpan.FromSeconds(1.0);

	private static readonly Stopwatch FlushWatch = Stopwatch.StartNew();

	private const int FlushTimeout = 3000;

	private static string _fileName;

	private static FileStream _fileStream;

	private static volatile bool _needClose;

	public static async Task WriteLineAsync(string line, bool flush = false)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(line + Environment.NewLine);
		await Locker.WaitAsync();
		FileStream fs;
		try
		{
			fs = OpenStream();
		}
		finally
		{
			Locker.Release();
		}
		try
		{
			await fs.WriteAsync(bytes, 0, bytes.Length);
			if (flush || FlushWatch.ElapsedMilliseconds > 3000)
			{
				FlushWatch.Restart();
				await fs.FlushAsync();
			}
		}
		catch (Exception ex)
		{
			UtilConsole.WriteLine("LogUtils.WriteLineAsync() ex: " + ex);
			_needClose = true;
		}
	}

	private static FileStream OpenStream()
	{
		FileStream fileStream = _fileStream;
		string text = $"{AppPaths.DataDir}{DateTime.Now:yyyy'-'MM'-'dd}_log.txt";
		if (fileStream != null)
		{
			if (!_needClose && string.Compare(text, _fileName, StringComparison.Ordinal) <= 0)
			{
				return fileStream;
			}
			UtilConsole.Info("LogUtils.OpenStream() roll needClose=" + _needClose + " delay=" + DelayTime.ToString() + ", logfile: " + _fileName + " => " + text);
			DisposeAsync(fileStream, DelayTime);
			_needClose = false;
			_fileStream = null;
			_fileName = null;
		}
		UtilConsole.Info("UtilLog open logfile: " + text);
		string directoryName = Path.GetDirectoryName(text);
		if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
		{
			Directory.CreateDirectory(directoryName);
		}
		fileStream = new FileStream(text, FileMode.Append, FileSystemRights.AppendData | FileSystemRights.Synchronize, FileShare.ReadWrite, 4096, FileOptions.None);
		_fileName = text;
		_fileStream = fileStream;
		return fileStream;
	}

	private static async void DisposeAsync(FileStream fs, TimeSpan delay)
	{
		await Task.Delay(delay);
		Dispose(fs);
	}

	private static void Dispose(FileStream fs)
	{
		try
		{
			fs?.Dispose();
		}
		catch (Exception ex)
		{
			UtilConsole.Warn("UtilLog ignore ex: " + ex);
		}
	}

	public static void AddDebugLog(string msg, string file)
	{
		try
		{
			string s = msg + Environment.NewLine;
			byte[] myByte = Encoding.UTF8.GetBytes(s);
			string url = AppPaths.DataDir + file + ".txt";
			new Thread((ThreadStart)delegate
			{
				try
				{
					using FileStream fileStream = new FileStream(url, FileMode.Append, FileSystemRights.AppendData | FileSystemRights.Synchronize, FileShare.ReadWrite, 4096, FileOptions.None);
					fileStream.Write(myByte, 0, myByte.Length);
				}
				catch (Exception)
				{
				}
			}).Start();
		}
		catch (Exception)
		{
		}
	}

	public static void AddPerfDebugLog(string msg)
	{
		AddDebugLog(msg, "performance");
	}

	public static void AddSyncDebugLog(string msg)
	{
		AddDebugLog(msg, "tick_debug_log");
	}

	public static void AddSyncLog(string msg)
	{
		AddDebugLog(msg, "sync_log");
	}
}
