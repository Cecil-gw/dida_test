using System;

namespace TickTickUtils;

public class EnvironmentUtils
{
	public static bool IsWindows7()
	{
		if (Environment.OSVersion.Version.Major == 6)
		{
			return true;
		}
		return false;
	}

	public static bool IsWindows11()
	{
		int major = Environment.OSVersion.Version.Major;
		int build = Environment.OSVersion.Version.Build;
		if (major == 10)
		{
			return build >= 22000;
		}
		return false;
	}

	public static string GetWindowsVersion()
	{
		int major = Environment.OSVersion.Version.Major;
		int minor = Environment.OSVersion.Version.Minor;
		int build = Environment.OSVersion.Version.Build;
		if (major == 6)
		{
			switch (minor)
			{
			case 1:
				return "7";
			case 2:
				return "8";
			case 3:
				return "8.1";
			}
		}
		if (major == 10 && build >= 22000)
		{
			return "11";
		}
		if (major != 10)
		{
			return "unknown";
		}
		return "10";
	}
}
