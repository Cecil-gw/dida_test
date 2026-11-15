using System;
using System.Runtime.InteropServices;

namespace KotlinModels;

public class TTCalendar
{
	public int year;

	public int month;

	public int day;

	public int hour;

	public int minute;

	public int second;

	public int milliSecond;

	public string zone;

	public TTCalendar()
	{
	}

	public TTCalendar(DateTime date, string zone)
	{
		year = date.Year;
		month = date.Month;
		day = date.Day;
		hour = date.Hour;
		minute = date.Minute;
		second = date.Second;
		milliSecond = date.Millisecond;
		this.zone = zone;
	}

	public TTCalendar(DateTime date, IntPtr zone)
	{
		year = date.Year;
		month = date.Month;
		day = date.Day;
		hour = date.Hour;
		minute = date.Minute;
		second = date.Second;
		milliSecond = date.Millisecond;
		this.zone = Marshal.PtrToStringAnsi(zone);
	}

	public override string ToString()
	{
		if (year == 0)
		{
			return "0";
		}
		return year + "/" + month + "/" + day + "    " + hour + ":" + minute + "   " + second + "    " + zone;
	}
}
