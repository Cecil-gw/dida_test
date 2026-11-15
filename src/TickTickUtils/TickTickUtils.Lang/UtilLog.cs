using System;
using System.Threading;
using System.Threading.Tasks;

namespace TickTickUtils.Lang;

public static class UtilLog
{
	private const string TimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffzzz";

	private static LogLevel LogLevel { get; } = LogLevel.Info;

	public static void Debug(string format, params object[] arg)
	{
		Debug(string.Format(format, arg));
	}

	public static void Debug(string msg)
	{
		Log(LogLevel.Debug, msg);
	}

	public static void Admin(string format, params object[] arg)
	{
		Admin(string.Format(format, arg));
	}

	public static void Admin(string msg)
	{
		if (GlobalSettings.IsAdmin)
		{
			Log(LogLevel.Admin, msg);
		}
	}

	public static void Info(string format, params object[] arg)
	{
		Info(string.Format(format, arg));
	}

	public static void Info(string msg)
	{
		Log(LogLevel.Info, msg);
	}

	public static void Trace(string msg)
	{
		Log(LogLevel.Trace, msg);
	}

	public static void Warn(string msg)
	{
		Log(LogLevel.Warn, msg);
	}

	public static void Error(string msg)
	{
		Log(LogLevel.Error, msg);
	}

	public static void Error(Exception ex)
	{
		Log(LogLevel.Error, ExceptionUtils.BuildExceptionMessage(ex));
	}

	private static void Log(LogLevel level, string msg)
	{
		if (LogLevel < level)
		{
			return;
		}
		string line = Env(level) + msg;
		WriteLineAsync(line).ContinueWith(delegate(Task t)
		{
			if (t.IsFaulted)
			{
				UtilConsole.Error("UtilLog.Log() line=" + line + ", ex: " + t.Exception);
			}
		});
	}

	private static Task WriteLineAsync(string line)
	{
		return Task.Run(() => LogUtils.WriteLineAsync(line));
	}

	internal static string Env(LogLevel level)
	{
		return string.Format("{0} {1,5} [{2},{3}] ", DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffzzz"), Enum.GetName(typeof(LogLevel), level), Thread.CurrentThread.ManagedThreadId, Task.CurrentId);
	}
}
