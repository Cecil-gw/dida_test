using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;

namespace ticktick_WPF.Views.Time;

public class AdvanceDateModel : UpDownSelectViewModel
{
	private int _advanceDays;

	private string _displayText;

	private int _hour;

	private int _minute;

	private string _trigger;

	public bool IsSplit { get; set; }

	public DateTime Date { get; set; }

	public bool IsAllDay { get; set; }

	public bool IsEmpty { get; set; }

	public bool IsCustom { get; set; }

	public int Order { get; set; }

	public string Trigger
	{
		get
		{
			return _trigger;
		}
		set
		{
			_trigger = value;
			OnPropertyChanged("Trigger");
		}
	}

	public int AdvanceDays
	{
		get
		{
			return _advanceDays;
		}
		set
		{
			_advanceDays = value;
			OnPropertyChanged("AdvanceDays");
		}
	}

	public int Hour
	{
		get
		{
			return _hour;
		}
		set
		{
			_hour = value;
			OnPropertyChanged("Hour");
		}
	}

	public int Minute
	{
		get
		{
			return _minute;
		}
		set
		{
			_minute = value;
			OnPropertyChanged("Minute");
		}
	}

	public string ExtraText { get; set; }

	public string DisplayText
	{
		get
		{
			return _displayText;
		}
		set
		{
			_displayText = value;
			OnPropertyChanged("DisplayText");
		}
	}

	public AdvanceDateModel()
	{
		IsSplit = true;
		base.IsEnable = false;
	}

	public AdvanceDateModel(string rule, bool isAllDay)
	{
		Trigger = rule;
		IsAllDay = isAllDay;
		if (rule == null)
		{
			return;
		}
		Match match = new Regex("(-)?P((\\d{0,})Y)?((\\d{0,})M)?((\\d{0,})W)?((\\d{0,})D)?T?((\\d{0,})H)?((\\d{0,})M)?((\\d{0,})S)?").Match(rule);
		if (match.Success)
		{
			bool num = match.Groups[1].ToString() == "-";
			int.TryParse(match.Groups[7].ToString(), out var result);
			int.TryParse(match.Groups[9].ToString(), out var result2);
			int.TryParse(match.Groups[11].ToString(), out var result3);
			int.TryParse(match.Groups[13].ToString(), out var result4);
			if (result > 0)
			{
				result2 += result * 7;
			}
			Order = result3 * 60 + result4;
			if (num && isAllDay)
			{
				result2++;
			}
			if (num)
			{
				Order = (result2 + 2) * 24 * 60 - Order;
				result3 *= -1;
				result4 *= -1;
			}
			if (!isAllDay)
			{
				Order = result2 * 1440 + (result3 * 60 + result4) * -1;
			}
			if (rule.Contains("END"))
			{
				Order = 199970;
			}
			DateTime dateTime = DateTime.Today.AddHours(result3).AddMinutes(result4);
			AdvanceDays = result2;
			Hour = dateTime.Hour;
			Minute = dateTime.Minute;
			string reminderText = ReminderUtils.GetReminderText(rule, isAllDay);
			int num2 = reminderText.IndexOf("(");
			DisplayText = reminderText.Substring(0, (num2 > 0) ? num2 : reminderText.Length);
			ExtraText = reminderText.Substring((num2 > 0) ? num2 : reminderText.Length);
		}
	}

	public AdvanceDateModel(bool isEmpty, bool isCustom)
	{
		IsCustom = isCustom;
		IsEmpty = isEmpty;
		DisplayText = ToString();
		if (isEmpty)
		{
			Order = int.MinValue;
		}
		else if (isCustom)
		{
			Order = 199980;
		}
	}

	public override string ToString()
	{
		if (IsSplit)
		{
			return null;
		}
		if (IsEmpty)
		{
			return Utils.GetString("none");
		}
		if (IsCustom)
		{
			return Utils.GetString("Custom");
		}
		return ReminderUtils.GetReminderText(Trigger, IsAllDay);
	}

	public static List<AdvanceDateModel> BuildAllDayModels(bool withToday, bool showEnd)
	{
		List<AdvanceDateModel> list = new List<AdvanceDateModel>
		{
			new AdvanceDateModel(isEmpty: true, isCustom: false)
			{
				IsAllDay = true
			}
		};
		if (withToday)
		{
			list.Add(new AdvanceDateModel("TRIGGER:P0DT9H0M0S", isAllDay: true));
		}
		List<AdvanceDateModel> list2 = new List<AdvanceDateModel>
		{
			new AdvanceDateModel("TRIGGER:-P0DT15H0M0S", isAllDay: true),
			new AdvanceDateModel("TRIGGER:-P1DT15H0M0S", isAllDay: true),
			new AdvanceDateModel("TRIGGER:-P2DT15H0M0S", isAllDay: true),
			new AdvanceDateModel("TRIGGER:-P6DT15H0M0S", isAllDay: true),
			new AdvanceDateModel(isEmpty: false, isCustom: true)
			{
				IsAllDay = true
			}
		};
		if (showEnd)
		{
			list2.Insert(4, new AdvanceDateModel("TRIGGER;RELATED=END:P0DT9H0M0S", isAllDay: true));
		}
		list.AddRange(list2);
		return list;
	}

	public static List<AdvanceDateModel> BuildTimeDayModels(bool showEnd = false)
	{
		List<AdvanceDateModel> list = new List<AdvanceDateModel>
		{
			new AdvanceDateModel(isEmpty: true, isCustom: false)
			{
				IsAllDay = false
			},
			new AdvanceDateModel("TRIGGER:-PT0S", isAllDay: false),
			new AdvanceDateModel("TRIGGER:-PT5M", isAllDay: false),
			new AdvanceDateModel("TRIGGER:-PT30M", isAllDay: false),
			new AdvanceDateModel("TRIGGER:-PT60M", isAllDay: false),
			new AdvanceDateModel("TRIGGER:-PT1440M", isAllDay: false),
			new AdvanceDateModel(isEmpty: false, isCustom: true)
			{
				IsAllDay = false
			}
		};
		if (showEnd)
		{
			list.Insert(6, new AdvanceDateModel("TRIGGER;RELATED=END:-PT0S", isAllDay: false));
		}
		return list;
	}

	public string ToRule()
	{
		return ToRule(AdvanceDays, Hour, Minute);
	}

	private static string ToRule(int day, int hour, int minute)
	{
		int num = day - 1;
		int num2 = 1440 - hour * 60 - minute;
		if (num < 0)
		{
			return "TRIGGER:P0DT" + hour + "H" + minute + "M0S";
		}
		int num3 = num2 / 60;
		int num4 = num2 - num3 * 60;
		if (num > 0 || num3 > 0 || minute > 0)
		{
			string text = "TRIGGER:-P" + num + "D";
			if (num3 > 0 || minute > 0)
			{
				text = text + "T" + num3 + "H" + num4 + "M0S";
			}
			return text;
		}
		return string.Empty;
	}
}
