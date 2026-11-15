using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using ticktick_WPF.Dal;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Util.DateParser;

public static class DateParser
{
	public static bool CanParse;

	static DateParser()
	{
		CanParse = true;
		try
		{
			Parser.setIsUsOrUkDateFormat(DateUtils.GetSysDateFormatType() != DateFormatType.DMY);
		}
		catch (Exception)
		{
		}
	}

	public static IPaserDueDate Parse(string content, DateTime? baseDate, bool recognizeDuration)
	{
		if (CanParse)
		{
			try
			{
				Parser.setStartDay((int)(LocalSettings.Settings.WeekStartFromType + 1));
				Parser.setIs24DateFormat(LocalSettings.Settings.TimeFormat == "24Hour");
				string s = baseDate?.ToString("yyyy-MM-dd'T'HH:mm:ss", App.Ci) ?? string.Empty;
				IntPtr intPtr = Marshal.StringToCoTaskMemAnsi(content);
				IntPtr intPtr2 = Marshal.StringToCoTaskMemAnsi(s);
				string name = Thread.CurrentThread.CurrentCulture.Name;
				IntPtr intPtr3 = Marshal.StringToCoTaskMemAnsi(name);
				IntPtr ptr = Parser.parse(intPtr, Encoding.Default.GetByteCount(content), intPtr2, Encoding.Default.GetByteCount(s), recognizeDuration, intPtr3, Encoding.Default.GetByteCount(name));
				string text = Marshal.PtrToStringAnsi(ptr);
				ParseDueDate parseDueDate = ((text == null) ? null : JsonConvert.DeserializeObject<ParseDueDate>(text));
				if (parseDueDate != null)
				{
					parseDueDate.Text = content;
				}
				if (content.Contains("法定节假日"))
				{
					if (parseDueDate == null)
					{
						parseDueDate = new ParseDueDate();
					}
					parseDueDate.startDate = DateTime.Today;
					parseDueDate.repeatFlag = "RRULE:FREQ=DAILY;INTERVAL=1;TT_SKIP=OFFICIAL_WORKDAY";
					parseDueDate.recognizeStrings.Add("法定节假日");
				}
				Marshal.FreeCoTaskMem(intPtr);
				Marshal.FreeCoTaskMem(intPtr2);
				Marshal.FreeCoTaskMem(intPtr3);
				Parser.free_parse_result(ptr);
				return parseDueDate;
			}
			catch (Exception ex)
			{
				if (ex.Message.Contains("DllNotFoundException"))
				{
					CanParse = false;
				}
			}
		}
		return OldDateParser.Parse(content, baseDate ?? DateTime.Now);
	}

	public static IPaserDueDate Parse(string content, DateTime? baseDate)
	{
		return Parse(content, baseDate, UserDao.IsPro());
	}

	private static string StringToUTF8(string str)
	{
		try
		{
			byte[] bytes = Encoding.Unicode.GetBytes(str);
			byte[] bytes2 = Encoding.Convert(Encoding.Unicode, Encoding.UTF8, bytes);
			return Encoding.UTF8.GetString(bytes2);
		}
		catch (Exception)
		{
			return str;
		}
	}
}
