using System.Text.RegularExpressions;

namespace ticktick_WPF.Util;

public static class NameUtils
{
	public static bool IsValidName(string exp, bool checkSpace = true)
	{
		if (checkSpace)
		{
			return !new Regex("\"| |#|:|\\*|\\?|>|<|\\||\\/|\\\\").IsMatch(exp);
		}
		return !new Regex("\"|#|:|\\*|\\?|>|<|\\||\\/|\\\\").IsMatch(exp);
	}

	public static bool IsValidNameNoCheckSharp(string exp, bool checkSpace = true)
	{
		if (checkSpace)
		{
			return !new Regex("\"| |:|\\*|\\?|>|<|\\||\\/|\\\\").IsMatch(exp);
		}
		return !new Regex("\"|:|\\*|\\?|>|<|\\||\\/|\\\\").IsMatch(exp);
	}

	public static bool IsValidColumnName(string exp)
	{
		return !new Regex("\"|#|:|\\*|\\?|>|<|\\||\\/|\\\\").IsMatch(exp);
	}
}
