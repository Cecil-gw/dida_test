using System;
using System.Runtime.InteropServices;

namespace ticktick_WPF.Util.DateParser;

public static class Parser
{
	[DllImport("SmartDateDLL.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern IntPtr parse(IntPtr content, int conLen, IntPtr defaultDateStr, int dateLen, bool isRecognizeDuratio, IntPtr localStr, int localLen);

	[DllImport("SmartDateDLL.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void setStartDay(int startDay);

	[DllImport("SmartDateDLL.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void setIsUsOrUkDateFormat(bool isUsOrUkDateFormat);

	[DllImport("SmartDateDLL.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void setDefaultTimeSecond(long defaultTime);

	[DllImport("SmartDateDLL.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void setIs24DateFormat(bool is24Format);

	[DllImport("SmartDateDLL.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void setCustomizeHour(int morningHour, int morningMin, int afternoonHour, int afternoonMin, int eveningHour, int eveningMin, int nightHour, int nightMin);

	[DllImport("SmartDateDLL.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void free_parse_result(IntPtr ptr);
}
