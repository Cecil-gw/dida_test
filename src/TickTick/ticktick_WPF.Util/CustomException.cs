using System;

namespace ticktick_WPF.Util;

public class CustomException
{
	public class CalendarExpiredException : Exception
	{
	}

	public class CalendarBindException : Exception
	{
		public CalendarBindException(string message)
			: base(message)
		{
		}
	}
}
