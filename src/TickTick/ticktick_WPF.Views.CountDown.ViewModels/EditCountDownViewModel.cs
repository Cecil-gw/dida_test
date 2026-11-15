using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Media;
using Ical.Net;
using TickTickUtils.Lang;
using ticktick_WPF.Converter;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CountDown.Infrastructure;

namespace ticktick_WPF.Views.CountDown.ViewModels;

public class EditCountDownViewModel : ObservableObject
{
	private string _name;

	private DateTime? _date;

	private DateTime? lastDate;

	private string _iconName;

	private Brush _iconBackground;

	private bool _ignoreYear;

	private List<string> _reminders;

	private string _repeatFlag;

	private int _typeOfSmartList;

	private bool _showInSmartList;

	private CountDownType _type = CountDownType.Holiday;

	private DayStartOption _daysOption;

	private bool _isShowDaysCalculateOption;

	private bool _isGregorianCalendar = true;

	private bool? _manualIgnoreYear;

	private int? _annoyingAlert;

	private DisplayTimerMode _displayTimerMode;

	private bool _isShowAge;

	private CountdownModel _originalModel { get; set; }

	public bool IsAdd => _originalModel == null;

	public CountDownType? originalType { get; set; }

	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			SetProperty(ref _name, value, "Name");
			OnPropertyChanged("IsConfirmButtonEnable");
		}
	}

	public DateTime? Date
	{
		get
		{
			return _date;
		}
		set
		{
			lastDate = _date;
			if (SetProperty(ref _date, value, "Date"))
			{
				OnPropertyChanged("DateText");
				OnPropertyChanged("IsShowTimerMode");
				OnPropertyChanged("IsConfirmButtonEnable");
				OnPropertyChanged("HasDateValue");
				OnPropertyChanged("IsShowDaysCalculateOption");
			}
		}
	}

	public bool HasDateValue => Date.HasValue;

	public bool IsConfirmButtonEnable
	{
		get
		{
			if (!string.IsNullOrEmpty(Name))
			{
				return Date.HasValue;
			}
			return false;
		}
	}

	public string IconName
	{
		get
		{
			return _iconName;
		}
		set
		{
			SetProperty(ref _iconName, value, "IconName");
		}
	}

	public Brush IconBackground
	{
		get
		{
			return _iconBackground;
		}
		set
		{
			SetProperty(ref _iconBackground, value, "IconBackground");
		}
	}

	public bool IgnoreYear
	{
		get
		{
			return _ignoreYear;
		}
		set
		{
			SetProperty(ref _ignoreYear, value, "IgnoreYear");
			if (value)
			{
				IsShowAge = false;
			}
			else
			{
				IsShowAge = IsShowAgeBeforeIgnoreYear;
			}
			OnPropertyChanged("DateText");
			OnPropertyChanged("IsShowTimerMode");
			OnPropertyChanged("IsShowDaysCalculateOption");
		}
	}

	public bool IsGregorianCalendar
	{
		get
		{
			return _isGregorianCalendar;
		}
		set
		{
			SetProperty(ref _isGregorianCalendar, value, "IsGregorianCalendar");
			OnPropertyChanged("DateText");
		}
	}

	public int TypeOfSmartList
	{
		get
		{
			return _typeOfSmartList;
		}
		set
		{
			SetProperty(ref _typeOfSmartList, value, "TypeOfSmartList");
		}
	}

	public string DateText
	{
		get
		{
			if (!Date.HasValue)
			{
				return Utils.GetString("SelectDate");
			}
			try
			{
				if (!IsGregorianCalendar)
				{
					return CountdownDateTextConverter.FormatLunarDate(Date.Value, IgnoreYear);
				}
				return CountdownDateTextConverter.FormatGregorianDate(Date.Value, IgnoreYear);
			}
			catch (Exception ex)
			{
				UtilLog.Error("EditCountDownViewModel DateText Error: " + ex.Message);
				return "Error Format";
			}
		}
	}

	public List<string> Reminders
	{
		get
		{
			return _reminders ?? (_reminders = new List<string>());
		}
		set
		{
			SetProperty(ref _reminders, value, "Reminders");
		}
	}

	public int? AnnoyingAlert
	{
		get
		{
			return _annoyingAlert;
		}
		set
		{
			SetProperty(ref _annoyingAlert, value, "AnnoyingAlert");
		}
	}

	public string RepeatFlag
	{
		get
		{
			return _repeatFlag;
		}
		set
		{
			SetProperty(ref _repeatFlag, value, "RepeatFlag");
			OnPropertyChanged("IsShowTimerMode");
			OnPropertyChanged("IsShowAgeOption");
			OnPropertyChanged("IsShowDaysCalculateOption");
		}
	}

	public bool ShowInSmartList
	{
		get
		{
			return _showInSmartList;
		}
		set
		{
			SetProperty(ref _showInSmartList, value, "ShowInSmartList");
		}
	}

	public CountDownType Type
	{
		get
		{
			return _type;
		}
		set
		{
			SetProperty(ref _type, value, "Type");
			OnPropertyChanged("IsShowAgeOption");
		}
	}

	public DayStartOption DaysOption
	{
		get
		{
			return _daysOption;
		}
		set
		{
			SetProperty(ref _daysOption, value, "DaysOption");
		}
	}

	public bool IsShowDaysCalculateOption
	{
		get
		{
			if (!IgnoreYear)
			{
				if (!string.IsNullOrEmpty(RepeatFlag) || !(Date <= DateTime.Today))
				{
					if (IsShowTimerMode)
					{
						return DisplayTimerMode == DisplayTimerMode.CountUp;
					}
					return false;
				}
				return true;
			}
			return false;
		}
	}

	public string WindowTitle => Utils.GetString(IsAdd ? "Add" : "Edit");

	public bool IsShowTimerMode
	{
		get
		{
			if (!Date.HasValue || Date > DateTime.Today)
			{
				return false;
			}
			if (IgnoreYear)
			{
				return false;
			}
			if (string.IsNullOrEmpty(RepeatFlag) || RepeatFlag.Contains("NONE"))
			{
				return false;
			}
			return true;
		}
	}

	public DisplayTimerMode DisplayTimerMode
	{
		get
		{
			return _displayTimerMode;
		}
		set
		{
			SetProperty(ref _displayTimerMode, value, "DisplayTimerMode");
			OnPropertyChanged("DisplayTimerModeText");
			OnPropertyChanged("IsShowAgeOption");
			OnPropertyChanged("IsShowDaysCalculateOption");
		}
	}

	public string DisplayTimerModeText => CountdownModel.GetTimerModeText(DisplayTimerMode);

	public bool IsShowAge
	{
		get
		{
			return _isShowAge;
		}
		set
		{
			SetProperty(ref _isShowAge, value, "IsShowAge");
			if (!IgnoreYear)
			{
				IsShowAgeBeforeIgnoreYear = IsShowAge;
			}
		}
	}

	public bool IsShowAgeOption
	{
		get
		{
			if ((DisplayTimerMode == DisplayTimerMode.CountDown || !IsShowTimerMode) && (!IsGregorianCalendar || !isCustomFlag) && Type == CountDownType.Birthday && !string.IsNullOrEmpty(RepeatFlag) && RepeatFlag.Contains("FREQ=YEARLY"))
			{
				if (RepeatFlag.Contains("INTERVAL"))
				{
					return RepeatFlag.Contains("INTERVAL=1");
				}
				return true;
			}
			return false;
		}
	}

	private bool isCustomFlag { get; set; }

	public bool? ManualIgnoreYear
	{
		get
		{
			return _manualIgnoreYear;
		}
		set
		{
			SetProperty(ref _manualIgnoreYear, value, "ManualIgnoreYear");
		}
	}

	public bool IsShowAgeBeforeIgnoreYear { get; set; }

	public bool IsChangeDate { get; set; }

	public EditCountDownViewModel(CountdownModel existingModel = null, CountDownType countdownType = CountDownType.Countdown)
	{
		_originalModel = existingModel;
		IsChangeDate = false;
		if (existingModel != null)
		{
			Name = existingModel.Name;
			Date = existingModel.GetDateAsDateTime();
			IsGregorianCalendar = existingModel.ShowCalendarType == ShowCalendarType.Gregorian;
			TypeOfSmartList = existingModel.TypeOfSmartList;
			IconName = (existingModel.IconRes ?? "countdown_countdown").Replace(".png", "");
			IgnoreYear = existingModel.IgnoreYear;
			RepeatFlag = existingModel.RepeatFlag;
			originalType = (Type = existingModel.Type);
			DisplayTimerMode = existingModel.TimerMode;
			IsShowAge = existingModel.ShowAge;
			DaysOption = existingModel.DaysOption;
			AnnoyingAlert = existingModel.AnnoyingAlert;
			if (existingModel.Reminders != null && existingModel.Reminders.Length != 0)
			{
				Reminders = new List<string>(existingModel.Reminders);
			}
			else
			{
				Reminders = new List<string>();
			}
			ShowInSmartList = existingModel.TypeOfSmartList != 9999;
			try
			{
				if (string.IsNullOrEmpty(existingModel.Color))
				{
					IconBackground = new SolidColorBrush(Colors.Transparent);
				}
				else
				{
					IconBackground = (Brush)new BrushConverter().ConvertFrom(existingModel.Color);
				}
			}
			catch
			{
				IconBackground = new SolidColorBrush(Colors.Transparent);
			}
		}
		else
		{
			Name = "";
			IconName = countdownType.GetDefaultIconName();
			if (_originalModel != null)
			{
				_originalModel.Type = countdownType;
			}
			Type = countdownType;
			IgnoreYear = Type == CountDownType.Holiday;
			DisplayTimerMode = DisplayTimerMode.CountDown;
			IsShowAge = false;
			Reminders = new List<string> { "TRIGGER:P0DT9H0M0S", "TRIGGER:-P2DT15H0M0S" };
			switch (countdownType)
			{
			case CountDownType.Birthday:
			{
				int month2 = DateTime.Today.Month;
				int day2 = DateTime.Today.Day;
				RepeatFlag = $"RRULE:FREQ=YEARLY;INTERVAL=1;BYMONTH={month2};BYMONTHDAY={day2}";
				break;
			}
			case CountDownType.Holiday:
				if (Utils.IsTickPackage())
				{
					int month = DateTime.Today.Month;
					int day = DateTime.Today.Day;
					RepeatFlag = $"RRULE:FREQ=YEARLY;INTERVAL=1;BYMONTH={month};BYMONTHDAY={day}";
				}
				else
				{
					LunarUtils.ChineseCalendar chineseCalendar = new LunarUtils.ChineseCalendar(DateTime.Today);
					RepeatFlag = $"LUNAR:FREQ=YEARLY;INTERVAL=1;BYMONTH={chineseCalendar.ChineseMonth};BYMONTHDAY={chineseCalendar.ChineseDay}";
				}
				break;
			}
			if (Type == CountDownType.Birthday && !Utils.IsTickPackage())
			{
				IsGregorianCalendar = false;
			}
			UpdateRepeatFlag();
			ShowInSmartList = true;
		}
		IsShowAgeBeforeIgnoreYear = IsShowAge;
	}

	public void UpdateType(CountDownType type)
	{
		Type = type;
		if (!ManualIgnoreYear.HasValue)
		{
			if (_originalModel == null)
			{
				switch (Type)
				{
				case CountDownType.Holiday:
					IgnoreYear = true;
					break;
				case CountDownType.Birthday:
					IgnoreYear = false;
					break;
				}
			}
			else
			{
				IgnoreYear = _originalModel.IgnoreYear;
			}
		}
		else
		{
			IgnoreYear = ManualIgnoreYear.Value;
		}
		CountDownType type2 = Type;
		if ((uint)(type2 - 3) <= 1u)
		{
			IgnoreYear = false;
		}
	}

	public CountdownModel ToModel()
	{
		string color = null;
		if (IconBackground is SolidColorBrush solidColorBrush)
		{
			color = ColorToHex1(solidColorBrush.Color);
		}
		Date = ((!Date.HasValue) ? new DateTime?(DateTime.Today) : Date);
		if (!IsShowTimerMode)
		{
			DisplayTimerMode = DisplayTimerMode.CountDown;
		}
		if (IsAdd)
		{
			return new CountdownModel
			{
				Name = Name,
				Date = int.Parse(Date.Value.ToString("yyyyMMdd")),
				Color = color,
				StyleColor = "#4285F4",
				IconRes = IconName,
				Style = _originalModel?.Style,
				Type = Type,
				IgnoreYear = IgnoreYear,
				RepeatFlag = AdjustRepeatFlag(Date.Value, RepeatFlag, _isGregorianCalendar),
				Reminders = (Reminders?.ToArray() ?? new string[0]),
				TimerMode = DisplayTimerMode,
				ShowAge = IsShowAge,
				ShowCalendarType = (IsGregorianCalendar ? ShowCalendarType.Gregorian : ShowCalendarType.Lunar),
				DaysOption = DaysOption,
				AnnoyingAlert = AnnoyingAlert
			};
		}
		IsChangeDate = _originalModel.Date != int.Parse(Date.Value.ToString("yyyyMMdd")) || _originalModel.IgnoreYear != IgnoreYear || _originalModel.TimerMode != DisplayTimerMode || _originalModel.RepeatFlag != AdjustRepeatFlag(Date.Value, RepeatFlag, _isGregorianCalendar);
		_originalModel.Name = Name;
		_originalModel.Date = int.Parse(Date.Value.ToString("yyyyMMdd"));
		_originalModel.Color = color;
		_originalModel.StyleColor = _originalModel?.StyleColor;
		_originalModel.IconRes = IconName;
		_originalModel.Style = _originalModel?.Style;
		_originalModel.Type = Type;
		_originalModel.IgnoreYear = IgnoreYear;
		_originalModel.RepeatFlag = AdjustRepeatFlag(Date.Value, RepeatFlag, _isGregorianCalendar);
		_originalModel.Reminders = Reminders?.ToArray() ?? new string[0];
		_originalModel.TimerMode = DisplayTimerMode;
		_originalModel.ShowAge = IsShowAge;
		_originalModel.ShowCalendarType = (IsGregorianCalendar ? ShowCalendarType.Gregorian : ShowCalendarType.Lunar);
		_originalModel.DaysOption = DaysOption;
		_originalModel.AnnoyingAlert = AnnoyingAlert;
		return _originalModel;
	}

	private string ColorToHex1(Color color)
	{
		return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
	}

	public void UpdateRepeatFlagByDate()
	{
		isCustomFlag = RepeatUtils.CheckIfCountdownCustom(RepeatFlag, Date ?? DateTime.Today);
		if (CheckIfCanSaveRepeatFlag())
		{
			UpdateRepeatFlag();
		}
	}

	public bool CheckIfCanSaveRepeatFlag()
	{
		bool flag = RepeatUtils.CheckIfCountdownCustom(RepeatFlag, Date ?? DateTime.Today, ignoreInterval: true);
		if (!IsGregorianCalendar && flag)
		{
			return false;
		}
		return true;
	}

	public void UpdateRepeatFlag(string repeatFlag = null)
	{
		repeatFlag = (string.IsNullOrEmpty(repeatFlag) ? RepeatFlag : repeatFlag);
		RepeatFlag = AdjustRepeatFlag(Date ?? DateTime.Today, repeatFlag, _isGregorianCalendar, lastDate ?? DateTime.Today);
		isCustomFlag = RepeatUtils.CheckIfCountdownCustom(RepeatFlag, Date ?? DateTime.Today);
		OnPropertyChanged("IsShowAgeOption");
	}

	public static string AdjustRepeatFlag(DateTime selectedDate, string repeatFlag, bool isGregorianCalendar, DateTime? lastDate = null)
	{
		if (string.IsNullOrEmpty(repeatFlag) || repeatFlag.Contains("NONE"))
		{
			return null;
		}
		if (isGregorianCalendar && repeatFlag.Contains("RRULE") && RepeatUtils.CheckIfCountdownCustom(repeatFlag, lastDate ?? selectedDate))
		{
			return repeatFlag;
		}
		int result;
		int num = ((!int.TryParse(Regex.Match(repeatFlag, "INTERVAL=(\\d+)").Groups[1]?.Value, out result)) ? 1 : result);
		if (repeatFlag.Contains("FREQ=DAILY"))
		{
			repeatFlag = $"RRULE:FREQ=DAILY;INTERVAL={num}";
		}
		else if (repeatFlag.Contains("FREQ=WEEKLY"))
		{
			repeatFlag = $"RRULE:FREQ=WEEKLY;INTERVAL={num}";
		}
		else if (!isGregorianCalendar)
		{
			LunarUtils.ChineseCalendar chineseCalendar = new LunarUtils.ChineseCalendar(selectedDate);
			if (repeatFlag.Contains("FREQ=MONTHLY"))
			{
				repeatFlag = $"LUNAR:FREQ=MONTHLY;INTERVAL={num};BYMONTHDAY={chineseCalendar.ChineseDay}";
			}
			if (repeatFlag.Contains("FREQ=YEARLY"))
			{
				repeatFlag = $"LUNAR:FREQ=YEARLY;INTERVAL={num};BYMONTH={chineseCalendar.ChineseMonth};BYMONTHDAY={chineseCalendar.ChineseDay}";
			}
		}
		else
		{
			if (repeatFlag.Contains("FREQ=MONTHLY"))
			{
				repeatFlag = $"RRULE:FREQ=MONTHLY;INTERVAL={num};BYMONTHDAY={selectedDate.Day}";
			}
			if (repeatFlag.Contains("FREQ=YEARLY"))
			{
				repeatFlag = $"RRULE:FREQ=YEARLY;INTERVAL={num};BYMONTH={selectedDate.Month};BYMONTHDAY={selectedDate.Day}";
			}
		}
		lastDate = selectedDate;
		return repeatFlag;
	}

	public void SetRepeatCollect()
	{
		string text = string.Empty;
		if (RepeatUtils.CheckIfCountdownCustom(RepeatFlag, Date ?? DateTime.Today))
		{
			text = (IsGregorianCalendar ? "custom" : "lunar_custom");
		}
		else
		{
			switch (RecurrenceModel.GetRecurrenceModel(RepeatFlag).Frequency)
			{
			case FrequencyType.Daily:
				text = (IsGregorianCalendar ? "daily" : "lunar_daily");
				break;
			case FrequencyType.Weekly:
				text = (IsGregorianCalendar ? "weekly" : "lunar_weekly");
				break;
			case FrequencyType.Monthly:
				text = (IsGregorianCalendar ? "monthly" : "lunar_monthly");
				break;
			case FrequencyType.Yearly:
				text = (IsGregorianCalendar ? "yearly" : "lunar_yearly");
				break;
			}
		}
		if (!string.IsNullOrEmpty(text))
		{
			UserActCollectUtils.AddClickEvent("countdown", "repeat", text);
		}
	}
}
