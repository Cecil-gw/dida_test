using System;

namespace ticktick_WPF.Util;

public class ChineseCalendarException : Exception
{
	public ChineseCalendarException(string msg)
		: base(msg)
	{
	}
}
