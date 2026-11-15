namespace ticktick_WPF.Util;

public class CharUtils
{
	public static bool IsAsciiSpec(char ch)
	{
		if ((ch < ' ' || ch >= 'A') && (ch <= 'Z' || ch >= 'a'))
		{
			if (ch > 'z')
			{
				return ch < '\u007f';
			}
			return false;
		}
		return true;
	}

	public static bool IsAbc(char ch)
	{
		if (ch < 'a' || ch > 'z')
		{
			if (ch >= 'A')
			{
				return ch <= 'Z';
			}
			return false;
		}
		return true;
	}
}
