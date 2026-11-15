namespace TickTickUtils.Lang;

public class UtilConsole
{
	public static void Info(string format, params object[] arg)
	{
		Info(string.Format(format, arg));
	}

	public static void Trace(string msg)
	{
	}

	public static void Debug(string msg)
	{
	}

	public static void Info(string msg)
	{
		Log(LogLevel.Info, msg);
	}

	public static void Warn(string msg)
	{
		Log(LogLevel.Warn, msg);
	}

	public static void Error(string msg)
	{
		Log(LogLevel.Error, msg);
	}

	public static void WriteLine(string msg)
	{
		try
		{
		}
		catch
		{
		}
	}

	private static void Log(LogLevel level, string msg)
	{
		WriteLine(UtilLog.Env(level) + msg);
	}
}
