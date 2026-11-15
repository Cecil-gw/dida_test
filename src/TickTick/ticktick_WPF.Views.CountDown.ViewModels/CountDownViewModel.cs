using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.KotlinUtils;
using ticktick_WPF.Views.CountDown.Infrastructure;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Views.CountDown.ViewModels;

public class CountDownViewModel : ObservableObject
{
	public enum FromSource
	{
		MainContainer,
		StylePreview,
		Calendar,
		Kanban,
		Widget,
		WidgetStylePreview
	}

	private string _title;

	private bool _isShowRemark;

	private string _remarkText;

	private string _dateText;

	private string _dateHoverText;

	private string _dayOfWeek;

	private Brush _iconBackground;

	private Brush _countColor;

	private Brush _titleColor;

	private Brush _descColor;

	private ImageBrush _backgroundImage;

	private ImageSource _icon;

	private bool _isPinned;

	private bool _isShowTimerMode;

	private bool _hasUnit;

	private CountdownModel _model;

	private double _scaleRatio = 1.0;

	private double _scaleRatioExtra = 1.0;

	private bool _showToday;

	private string _presetImageKey = string.Empty;

	private string _customImagePath = string.Empty;

	private CountDownType _type;

	private Rect _clipRect;

	private Thickness _daysDisplayMargin;

	private float _widgetOpacity = 0.7f;

	private string[] _styleColors;

	private string _iconText;

	private bool _showActionButton = true;

	private string _styleType;

	private bool _showIconText;

	public bool IgnoreRatio;

	private ICommand switchFormatCommand;

	private ICommand editRemarkCommand;

	public Action OnShowRemarkDetail { get; set; }

	public double ImageWidth { get; private set; }

	public double ImageHeight { get; private set; }

	public bool IsTwoColorStyle
	{
		get
		{
			if (!(_styleType == "cartoon") && !(_styleType == "two_color_card"))
			{
				return _styleType == "dopamine";
			}
			return true;
		}
	}

	public bool IsProStyle
	{
		get
		{
			if (!(_styleType == "cartoon") && !(_styleType == "perpetual") && !(_styleType == "two_color_card"))
			{
				return _styleType == "dopamine";
			}
			return true;
		}
	}

	public string[] StyleColors
	{
		get
		{
			return _styleColors;
		}
		set
		{
			SetProperty(ref _styleColors, value, "StyleColors");
		}
	}

	public string IconText
	{
		get
		{
			return _iconText;
		}
		set
		{
			SetProperty(ref _iconText, value, "IconText");
		}
	}

	public bool ShowIconText
	{
		get
		{
			return _showIconText;
		}
		set
		{
			SetProperty(ref _showIconText, value, "ShowIconText");
		}
	}

	public bool ShowActionButton
	{
		get
		{
			return _showActionButton;
		}
		set
		{
			SetProperty(ref _showActionButton, value, "ShowActionButton");
		}
	}

	public bool ShowToday
	{
		get
		{
			return _showToday;
		}
		set
		{
			SetProperty(ref _showToday, value, "ShowToday");
		}
	}

	public double ScaleRatio
	{
		get
		{
			return _scaleRatio;
		}
		set
		{
			SetProperty(ref _scaleRatio, value, "ScaleRatio");
			UpdateDaysScale();
		}
	}

	public double ScaleRatioExtra
	{
		get
		{
			return _scaleRatioExtra;
		}
		set
		{
			SetProperty(ref _scaleRatioExtra, value, "ScaleRatioExtra");
		}
	}

	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			SetProperty(ref _title, value, "Title");
		}
	}

	public ObservableCollection<DateDisplayData> DateDisplays { get; set; } = new ObservableCollection<DateDisplayData>();

	public bool HasUnit
	{
		get
		{
			return _hasUnit;
		}
		set
		{
			SetProperty(ref _hasUnit, value, "HasUnit");
		}
	}

	public Thickness DaysDisplayMargin
	{
		get
		{
			return _daysDisplayMargin;
		}
		set
		{
			SetProperty(ref _daysDisplayMargin, value, "DaysDisplayMargin");
		}
	}

	public string DateText
	{
		get
		{
			return _dateText;
		}
		set
		{
			SetProperty(ref _dateText, value, "DateText");
		}
	}

	public string DateHoverText
	{
		get
		{
			return _dateHoverText;
		}
		set
		{
			SetProperty(ref _dateHoverText, value, "DateHoverText");
		}
	}

	public string RemarkText
	{
		get
		{
			return _remarkText;
		}
		set
		{
			SetProperty(ref _remarkText, value, "RemarkText");
		}
	}

	public bool IsShowRemark
	{
		get
		{
			return _isShowRemark;
		}
		set
		{
			SetProperty(ref _isShowRemark, value, "IsShowRemark");
		}
	}

	public string DayOfWeek
	{
		get
		{
			return _dayOfWeek;
		}
		set
		{
			SetProperty(ref _dayOfWeek, value, "DayOfWeek");
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

	public Brush CountColor
	{
		get
		{
			return _countColor;
		}
		set
		{
			SetProperty(ref _countColor, value, "CountColor");
		}
	}

	public Brush TitleColor
	{
		get
		{
			return _titleColor;
		}
		set
		{
			SetProperty(ref _titleColor, value, "TitleColor");
		}
	}

	public Brush DescColor
	{
		get
		{
			return _descColor;
		}
		set
		{
			SetProperty(ref _descColor, value, "DescColor");
		}
	}

	public ImageBrush BackgroundImage
	{
		get
		{
			return _backgroundImage;
		}
		set
		{
			SetProperty(ref _backgroundImage, value, "BackgroundImage");
		}
	}

	public Rect ClipRect
	{
		get
		{
			return _clipRect;
		}
		set
		{
			SetProperty(ref _clipRect, value, "ClipRect");
			LoadImageBackground();
		}
	}

	public string CustomImagePath
	{
		get
		{
			return _customImagePath;
		}
		set
		{
			SetProperty(ref _customImagePath, value, "CustomImagePath");
			LoadImageBackground();
		}
	}

	public string PresetImageKey
	{
		get
		{
			return _presetImageKey;
		}
		set
		{
			SetProperty(ref _presetImageKey, value, "PresetImageKey");
			LoadImageBackground();
		}
	}

	public bool IsShowTimerMode
	{
		get
		{
			return _isShowTimerMode;
		}
		set
		{
			SetProperty(ref _isShowTimerMode, value, "IsShowTimerMode");
		}
	}

	public DisplayTimerMode DisplayTimerMode
	{
		get
		{
			if (Model == null)
			{
				return DisplayTimerMode.CountDown;
			}
			FromSource displayFromSource = DisplayFromSource;
			if ((uint)(displayFromSource - 2) <= 1u)
			{
				return DisplayTimerMode.CountDown;
			}
			return Model.TimerMode;
		}
	}

	public ImageSource Icon
	{
		get
		{
			return _icon;
		}
		set
		{
			SetProperty(ref _icon, value, "Icon");
		}
	}

	public bool IsPinned
	{
		get
		{
			return _isPinned;
		}
		set
		{
			SetProperty(ref _isPinned, value, "IsPinned");
		}
	}

	public string StyleType
	{
		get
		{
			return _styleType;
		}
		set
		{
			string styleType = _styleType;
			SetProperty(ref _styleType, value, "StyleType");
			if (!string.IsNullOrEmpty(styleType))
			{
				UpdateFromModel(_model, ignoreStyle: true);
			}
		}
	}

	public bool IsArchived
	{
		get
		{
			CountdownModel model = Model;
			if (model == null)
			{
				return false;
			}
			return model.Status == 1;
		}
	}

	private DateTime? DisplayDate { get; }

	public bool HasDisplayDate => DisplayFromSource == FromSource.Calendar;

	public bool IsStylePreview
	{
		get
		{
			if (DisplayFromSource != FromSource.StylePreview)
			{
				return DisplayFromSource == FromSource.WidgetStylePreview;
			}
			return true;
		}
	}

	public bool IsWidget
	{
		get
		{
			if (DisplayFromSource != FromSource.Widget)
			{
				return DisplayFromSource == FromSource.WidgetStylePreview;
			}
			return true;
		}
	}

	public FromSource DisplayFromSource { get; set; }

	public CountdownWidgetModel.DisplayDescType WidgetDescType { get; set; }

	public float WidgetOpacity
	{
		get
		{
			return _widgetOpacity;
		}
		set
		{
			SetProperty(ref _widgetOpacity, value, "WidgetOpacity");
		}
	}

	public string WidgetId { get; set; }

	public int DayNumLength { get; set; }

	public ICommand SwitchFormatCommand
	{
		get
		{
			if (switchFormatCommand != null)
			{
				return switchFormatCommand;
			}
			return new AsyncRelayCommand(OnSwitchFormat);
		}
	}

	public ICommand EditRemarkCommand
	{
		get
		{
			if (editRemarkCommand != null)
			{
				return editRemarkCommand;
			}
			return new AsyncRelayCommand(OnEditRemark);
		}
	}

	public CountdownModel Model
	{
		get
		{
			return _model;
		}
		set
		{
			SetProperty(ref _model, value, "Model");
		}
	}

	private static Dictionary<string, BitmapImage> _imageCache { get; set; } = new Dictionary<string, BitmapImage>();

	public string GetPresetImageKeyWithDefault()
	{
		string result = PresetImageKey;
		if (string.IsNullOrEmpty(PresetImageKey))
		{
			result = _model.Type switch
			{
				CountDownType.Birthday => "birthday", 
				CountDownType.Holiday => "holiday", 
				CountDownType.Anniversary => "anniversary", 
				_ => "countdown", 
			};
		}
		return result;
	}

	public CountDownViewModel()
	{
	}

	public CountDownViewModel(CountdownModel model, FromSource source = FromSource.MainContainer)
	{
		Model = model;
		DisplayFromSource = source;
		UpdateFromModel(model);
	}

	public CountDownViewModel(CountdownModel model, DateTime displayDate)
	{
		Model = model;
		DisplayDate = displayDate;
		DisplayFromSource = FromSource.Calendar;
		UpdateFromModel(model);
	}

	public CountDownViewModel(CountdownModel model, string styleType, bool isWidget)
	{
		if (isWidget)
		{
			DisplayFromSource = FromSource.WidgetStylePreview;
		}
		else
		{
			DisplayFromSource = FromSource.StylePreview;
		}
		StyleType = styleType;
		Model = model;
		UpdateFromModel(model, ignoreStyle: true);
	}

	private string FormatDate(DateTime date, DateTextFormat format)
	{
		if ((uint)(format - 1) <= 1u)
		{
			LunarUtils.ChineseCalendar chineseCalendar = new LunarUtils.ChineseCalendar(date);
			string text = (chineseCalendar.IsChineseLeapMonth ? "闰" : string.Empty) + DateUtils.GetChineseMonthName(chineseCalendar.ChineseMonth) + DateUtils.GetChineseDayName(chineseCalendar.ChineseDay);
			if (format == DateTextFormat.Lunar)
			{
				text = text + " (" + DateUtils.FormatYearMonthDay(date, DateFormatLen.Middle) + ")";
			}
			return text;
		}
		return DateUtils.FormatYearMonthDay(date, DateFormatLen.Middle) ?? "";
	}

	public void UpdateFromModel(CountdownModel model, bool ignoreStyle = false)
	{
		if (model == null)
		{
			return;
		}
		Model = model;
		_type = model.Type;
		Title = model.Name;
		RemarkText = Utils.GetFirstLine(model.Remark);
		WidgetDescType = model.WidgetDescType;
		DateTime dateTime = (DisplayDate.HasValue ? DisplayDate.Value : model.GetTargetDate(DisplayTimerMode));
		if (model.CheckActualCanShowAge(DisplayTimerMode))
		{
			Title += _model.GetAge(dateTime);
		}
		if (!ignoreStyle)
		{
			StyleType = model.Style;
		}
		StyleColors = model.StyleColors;
		int dayOptionDeltaDays = GetDayOptionDeltaDays(dateTime, DateTime.Today);
		DateTextFormat format = DateTextFormat.Gregorian;
		if (model.ShowCalendarType == ShowCalendarType.Lunar)
		{
			format = ((!(StyleType == "cartoon") && !IsWidget) ? DateTextFormat.Lunar : DateTextFormat.OnlyLunar);
		}
		if (IsWidget)
		{
			CountdownWidgetModel.DisplayDescType widgetDescType = WidgetDescType;
			if (widgetDescType == CountdownWidgetModel.DisplayDescType.UntilOrSince || widgetDescType != CountdownWidgetModel.DisplayDescType.Detail)
			{
				if (dayOptionDeltaDays == 0)
				{
					DateText = FormatDate(dateTime, format) ?? "";
				}
				else if (dayOptionDeltaDays > 0)
				{
					DateText = ((Math.Abs(dayOptionDeltaDays) == 1) ? Utils.GetString("DayLeftWidget") : Utils.GetString("DaysLeftWidget"));
				}
				else
				{
					DateText = ((Math.Abs(dayOptionDeltaDays) == 1) ? Utils.GetString("DaySinceWidget") : Utils.GetString("DaysSinceWidget"));
				}
			}
			else
			{
				DateText = FormatDate(dateTime, format) ?? "";
			}
		}
		else if (dayOptionDeltaDays == 0)
		{
			DateText = FormatDate(dateTime, format) ?? "";
		}
		else if (dayOptionDeltaDays > 0)
		{
			DateText = string.Format(Utils.GetString("days_until_xx"), FormatDate(dateTime, format));
		}
		else
		{
			DateText = string.Format(Utils.GetString("days_since_xx"), FormatDate(dateTime, format));
		}
		DayOfWeek = DateUtils.GetWeekTextByWeekDay((int)dateTime.DayOfWeek);
		IsShowTimerMode = false;
		DateTime dateAsDateTime = model.GetDateAsDateTime();
		if (dateAsDateTime < DateTime.Today && !model.IgnoreYear && DisplayFromSource != FromSource.Calendar && !IsStylePreview && !string.IsNullOrEmpty(model.RepeatFlag) && !model.RepeatFlag.Contains("NONE"))
		{
			switch (DisplayTimerMode)
			{
			case DisplayTimerMode.CountDown:
			{
				int num = -GetDayOptionDeltaDays(dateAsDateTime, DateTime.Today, DisplayTimerMode.CountUp);
				if (num >= 0)
				{
					if (num > 1)
					{
						DateHoverText = string.Format(Utils.GetString("xx_days_since"), num, FormatDate(dateAsDateTime, DateTextFormat.Gregorian));
					}
					else if (num == 1)
					{
						DateHoverText = string.Format(Utils.GetString("1_day_since"), FormatDate(dateAsDateTime, DateTextFormat.Gregorian));
					}
				}
				break;
			}
			case DisplayTimerMode.CountUp:
			{
				DateTime countdownRepeatNextDate = RepeatCalculator.GetCountdownRepeatNextDate(model.RepeatFlag, dateAsDateTime);
				int dayOptionDeltaDays2 = GetDayOptionDeltaDays(countdownRepeatNextDate, DateTime.Today, DisplayTimerMode.CountDown);
				if (dayOptionDeltaDays2 >= 0)
				{
					if (dayOptionDeltaDays2 > 1)
					{
						DateHoverText = string.Format(Utils.GetString("xx_days_until"), dayOptionDeltaDays2, FormatDate(countdownRepeatNextDate, DateTextFormat.Gregorian));
					}
					else if (dayOptionDeltaDays2 == 1)
					{
						DateHoverText = string.Format(Utils.GetString("xx_day_until"), 1, FormatDate(countdownRepeatNextDate, DateTextFormat.Gregorian));
					}
					else
					{
						DateHoverText = string.Format(Utils.GetString("xx_day_until"), 0, FormatDate(countdownRepeatNextDate, DateTextFormat.Gregorian));
					}
				}
				break;
			}
			}
			IsShowTimerMode = true;
		}
		DaysToFormat(dateTime, DateTime.Today, model.DateDisplayFormatType);
		try
		{
			if (string.IsNullOrEmpty(model.Color))
			{
				IconBackground = new SolidColorBrush(Colors.Transparent);
			}
			else
			{
				IconBackground = (Brush)new BrushConverter().ConvertFrom(model.Color);
			}
		}
		catch
		{
			IconBackground = new SolidColorBrush(Colors.Transparent);
		}
		IsPinned = model != null && model.pinnedTimeValue != null && model?.pinnedTimeValue != "-1";
		Icon = CountdownService.GetIcon(model.IconRes, model?.Color);
		if (model?.BackgroundObject != null)
		{
			BackgroundModel backgroundObject = model.BackgroundObject;
			if (backgroundObject.Id != null && backgroundObject.Id.Contains("preset_"))
			{
				_presetImageKey = backgroundObject.Id.Replace("preset_", "");
			}
			if (backgroundObject.Landscape != null)
			{
				_clipRect = new Rect(new Point(backgroundObject.Landscape.Left, backgroundObject.Landscape.Top), new Point(backgroundObject.Landscape.Right, backgroundObject.Landscape.Bottom));
			}
			else
			{
				_clipRect = default(Rect);
			}
		}
		_customImagePath = model?.LocalImagePath;
		LoadImageBackground();
	}

	private async Task OnSwitchFormat(object param)
	{
		if (IsStylePreview)
		{
			return;
		}
		DateTime today = DateTime.Today;
		DateTime dateTime = (DisplayDate.HasValue ? DisplayDate.Value : _model.GetTargetDate(DisplayTimerMode));
		if (Math.Abs(GetDayOptionDeltaDays(dateTime, today)) < 7)
		{
			return;
		}
		try
		{
			(int, int, int) value = ((Model.ShowCalendarType != ShowCalendarType.Gregorian) ? LunarUtils.ChineseCalendar.ComputeLunarYearMonthDay(today, dateTime) : CalculateDateDifferenceNatural(today, dateTime));
			if (value.Item2 < 1 && value.Item1 < 1)
			{
				switch (_model.DateDisplayFormatType)
				{
				case DateDisplayFormatType.Day:
				case DateDisplayFormatType.Year:
					_model.DateDisplayFormatType = DateDisplayFormatType.Week;
					break;
				case DateDisplayFormatType.Week:
					_model.DateDisplayFormatType = DateDisplayFormatType.Day;
					break;
				}
			}
			else
			{
				switch (_model.DateDisplayFormatType)
				{
				case DateDisplayFormatType.Day:
					_model.DateDisplayFormatType = DateDisplayFormatType.Year;
					break;
				case DateDisplayFormatType.Year:
					_model.DateDisplayFormatType = DateDisplayFormatType.Week;
					break;
				case DateDisplayFormatType.Week:
					_model.DateDisplayFormatType = DateDisplayFormatType.Day;
					break;
				}
			}
			DaysToFormat(dateTime, today, _model.DateDisplayFormatType, value);
		}
		catch (Exception ex)
		{
			UtilLog.Error(ex);
			return;
		}
		if (DisplayFromSource == FromSource.Widget)
		{
			CountdownWidgetsHelper.SaveCountdownDateFormat(WidgetId, _model.DateDisplayFormatType);
		}
		else
		{
			await CountdownDao.UpdateCountdown(_model, notify: false);
		}
	}

	public static int GetFullNaturalMonths(DateTime startDate, DateTime endDate)
	{
		if (startDate > endDate)
		{
			DateTime dateTime = startDate;
			startDate = endDate;
			endDate = dateTime;
		}
		if (endDate < startDate)
		{
			return 0;
		}
		int num = endDate.Year - startDate.Year;
		int num2 = endDate.Month - startDate.Month;
		int num3 = num * 12 + num2;
		if (endDate.Day < startDate.Day && new DateTime(endDate.Year, endDate.Month, 1).AddMonths(1).AddDays(-1.0).Day >= startDate.Day)
		{
			num3--;
		}
		return Math.Max(num3, 0);
	}

	public static (int year, int month, int day) CalculateDateDifferenceNatural(DateTime startDate, DateTime endDate)
	{
		if (startDate > endDate)
		{
			DateTime dateTime = startDate;
			startDate = endDate;
			endDate = dateTime;
		}
		int num = GetFullNaturalMonths(startDate, endDate);
		DateTime dateTime2 = startDate.AddMonths(num);
		while (dateTime2 > endDate)
		{
			num--;
			dateTime2 = startDate.AddMonths(num);
		}
		int num2 = num / 12;
		if (num2 >= 1)
		{
			DateTime dateTime3 = startDate.AddYears(num2);
			num -= num2 * 12;
			dateTime2 = dateTime3.AddMonths(num);
		}
		else
		{
			num %= 12;
		}
		int days = (endDate - dateTime2).Days;
		return (year: num2, month: num, day: days);
	}

	public static (int year, int month, int day) CalculateDateDifference(DateTime startDate, DateTime endDate)
	{
		if (startDate > endDate)
		{
			DateTime dateTime = startDate;
			startDate = endDate;
			endDate = dateTime;
		}
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		DateTime dateTime2 = endDate;
		while (dateTime2.AddYears(-1) >= startDate)
		{
			dateTime2 = dateTime2.AddYears(-1);
			num++;
		}
		dateTime2 = endDate.AddYears(-num);
		while (dateTime2.AddMonths(-1) >= startDate)
		{
			dateTime2 = dateTime2.AddMonths(-1);
			num2++;
		}
		dateTime2 = endDate.AddYears(-num).AddMonths(-num2);
		while (dateTime2.AddDays(-1.0) >= startDate)
		{
			dateTime2 = dateTime2.AddDays(-1.0);
			num3++;
		}
		return (year: num, month: num2, day: num3);
	}

	private int GetDayOptionDeltaDays(DateTime targetDate, DateTime currentDate, DisplayTimerMode? timerMode = null)
	{
		if (!timerMode.HasValue)
		{
			FromSource displayFromSource = DisplayFromSource;
			if ((uint)(displayFromSource - 2) <= 1u && !(targetDate >= currentDate))
			{
				timerMode = DisplayTimerMode.CountUp;
			}
		}
		return Model.GetDaysRemaining(targetDate, currentDate, timerMode ?? DisplayTimerMode);
	}

	public bool CanShowSwitchHint()
	{
		if (HasUnit)
		{
			return false;
		}
		DateTime targetDate = (DisplayDate.HasValue ? DisplayDate.Value : _model.GetTargetDate(DisplayTimerMode));
		return Math.Abs(GetDayOptionDeltaDays(targetDate, DateTime.Today)) > 7;
	}

	private void DaysToFormat(DateTime targetDate, DateTime currentDate, DateDisplayFormatType format, (int year, int month, int day)? delta = null)
	{
		int num = 44;
		int num2 = 18;
		int num3 = Math.Abs(GetDayOptionDeltaDays(targetDate, currentDate));
		DateDisplays.Clear();
		if (num3 == 0)
		{
			bool bold = false;
			switch (StyleType)
			{
			case "cartoon":
			case "two_color_card":
			case "dopamine":
				bold = true;
				break;
			}
			num = GetNumberLengthFontSize(-1);
			DateDisplays.Add(new DateDisplayData(Utils.GetString("Today"), null, num, 0.0, bold));
		}
		else
		{
			try
			{
				switch (format)
				{
				case DateDisplayFormatType.Day:
					num = GetNumberLengthFontSize(0);
					DateDisplays.Add(new DateDisplayData(num3.ToString(), null, num, 0.0));
					break;
				case DateDisplayFormatType.Week:
				{
					_ = string.Empty;
					int num6 = num3 / 7;
					int num7 = num3 % 7;
					if (num6 <= 0 && num7 <= 0)
					{
						num7 = num3;
					}
					int num8 = 0;
					if (num6 > 0)
					{
						num8 += num6.ToString().Length;
					}
					if (num7 > 0)
					{
						num8 += num7.ToString().Length;
					}
					if (num6 <= 0)
					{
						num8 = 0;
					}
					num = GetNumberLengthFontSize(num8);
					if (num6 > 0)
					{
						DateDisplays.Add(new DateDisplayData(num6.ToString(), Utils.GetString("ShortWeekUnit"), num, num2));
					}
					if (num7 > 0)
					{
						if (num6 <= 0)
						{
							DateDisplays.Add(new DateDisplayData(num7.ToString(), null, num, num2));
						}
						else
						{
							DateDisplays.Add(new DateDisplayData(num7.ToString(), Utils.GetString("ShortDayUnit"), num, num2));
						}
					}
					break;
				}
				case DateDisplayFormatType.Year:
				{
					if (!delta.HasValue)
					{
						targetDate = Model.GetDayOptionTargetDate(targetDate);
						delta = ((Model.ShowCalendarType != ShowCalendarType.Gregorian) ? new(int, int, int)?(LunarUtils.ChineseCalendar.ComputeLunarYearMonthDay(currentDate, targetDate)) : new(int, int, int)?(CalculateDateDifferenceNatural(currentDate, targetDate)));
					}
					int item = delta.Value.year;
					int item2 = delta.Value.month;
					int num4 = delta.Value.day;
					if (item <= 0 && item2 <= 0 && num4 <= 0)
					{
						num4 = num3;
					}
					int num5 = 0;
					if (item > 0)
					{
						num5 += item.ToString().Length;
					}
					if (item2 > 0)
					{
						num5 += item2.ToString().Length;
					}
					if (num4 > 0)
					{
						num5 += num4.ToString().Length;
					}
					if (item <= 0 && item2 <= 0)
					{
						num5 = 0;
					}
					num = GetNumberLengthFontSize(num5);
					if (item > 0)
					{
						DateDisplays.Add(new DateDisplayData(item.ToString(), Utils.GetString("ShortYearUnit"), num, num2));
					}
					if (item2 > 0)
					{
						DateDisplays.Add(new DateDisplayData(item2.ToString(), Utils.GetString("ShortMonthUnit"), num, num2));
					}
					if (num4 > 0)
					{
						if (item <= 0 && item2 <= 0)
						{
							DateDisplays.Add(new DateDisplayData(num4.ToString(), null, num, num2));
						}
						else
						{
							DateDisplays.Add(new DateDisplayData(num4.ToString(), Utils.GetString("ShortDayUnit"), num, num2));
						}
					}
					break;
				}
				}
			}
			catch (Exception ex)
			{
				UtilLog.Error(ex);
				num = GetNumberLengthFontSize(0);
				DateDisplays.Add(new DateDisplayData(num3.ToString(), null, num, 0.0));
			}
		}
		UpdateDaysScale();
		if (string.IsNullOrEmpty(DateDisplays.LastOrDefault()?.Unit))
		{
			DaysDisplayMargin = default(Thickness);
		}
		else if (format != DateDisplayFormatType.Day && DaysDisplayMargin.Right == 0.0)
		{
			Thickness? thickness = DateDisplays.FirstOrDefault()?.UnitMargin;
			if (thickness.HasValue)
			{
				DaysDisplayMargin = new Thickness(0.0, 0.0, 0.0 - thickness.Value.Right, 0.0);
			}
		}
		HasUnit = DateDisplays.Any((DateDisplayData x) => !string.IsNullOrEmpty(x.Unit));
	}

	public int GetNumberLengthFontSize(int length)
	{
		DayNumLength = length;
		switch (length)
		{
		case -1:
			return 44;
		case 0:
		case 1:
			switch (StyleType)
			{
			case "normal":
			case "calendar":
			case "fullscreen_image":
			case "polaroid":
				return 55;
			case "cartoon":
				return 50;
			default:
				return 53;
			}
		case 2:
		case 3:
			switch (StyleType)
			{
			case "normal":
			case "calendar":
			case "fullscreen_image":
			case "polaroid":
				return 46;
			case "cartoon":
				return 40;
			default:
				return 44;
			}
		case 4:
			switch (StyleType)
			{
			case "normal":
			case "calendar":
			case "fullscreen_image":
			case "polaroid":
				return 42;
			case "cartoon":
				return 35;
			default:
				return 40;
			}
		case 5:
			switch (StyleType)
			{
			case "normal":
			case "calendar":
			case "fullscreen_image":
			case "polaroid":
				return 42;
			case "cartoon":
				return 32;
			default:
				return 40;
			}
		default:
			switch (StyleType)
			{
			case "normal":
			case "calendar":
			case "fullscreen_image":
			case "polaroid":
				return 40;
			case "perpetual":
				return 32;
			case "cartoon":
				return 26;
			default:
				return 38;
			}
		}
	}

	private int GetNumberLengthFontSizeWidget(int length, bool withoutUnit = false)
	{
		if (withoutUnit)
		{
			switch (length)
			{
			case -1:
				switch (StyleType)
				{
				default:
					return 30;
				case "polaroid":
				case "fullscreen_image":
					return 28;
				case "dopamine":
				case "perpetual":
				case "cartoon":
					return 34;
				case "two_color_card":
					return 32;
				}
			case 1:
			case 2:
			case 3:
				switch (StyleType)
				{
				case "fullscreen_image":
				case "polaroid":
					return 34;
				case "perpetual":
					return 48;
				default:
					return 42;
				}
			case 4:
			case 5:
				switch (StyleType)
				{
				case "fullscreen_image":
				case "polaroid":
					return 34;
				case "perpetual":
					return 42;
				default:
					return 36;
				}
			default:
				if (StyleType == "perpetual")
				{
					return 32;
				}
				return 36;
			}
		}
		switch (length)
		{
		case 1:
		case 2:
		case 3:
		case 4:
			switch (StyleType)
			{
			case "normal":
			case "calendar":
				return 36;
			case "cartoon":
				return 28;
			case "two_color_card":
				return 32;
			default:
				return 34;
			}
		case 5:
			switch (StyleType)
			{
			case "polaroid":
			case "fullscreen_image":
				return 22;
			case "perpetual":
				return 28;
			case "cartoon":
				return 20;
			default:
				return 24;
			}
		default:
			switch (StyleType)
			{
			case "polaroid":
			case "fullscreen_image":
			case "cartoon":
				return 18;
			case "perpetual":
				return 22;
			default:
				return 20;
			}
		}
	}

	private void UpdateDaysScale()
	{
		try
		{
			bool isPreviewMini = false;
			if ((DisplayFromSource == FromSource.StylePreview || DisplayFromSource == FromSource.WidgetStylePreview) && ScaleRatio <= 0.4)
			{
				isPreviewMini = true;
			}
			bool isEn = Utils.IsEn();
			foreach (DateDisplayData dateDisplay in DateDisplays)
			{
				dateDisplay.UpdateScale(ScaleRatio, isPreviewMini, isEn);
			}
		}
		catch
		{
		}
	}

	private BitmapImage GetCustomBitmap()
	{
		using FileStream streamSource = new FileStream(_customImagePath, FileMode.Open, FileAccess.Read);
		BitmapImage bitmapImage = new BitmapImage();
		bitmapImage.BeginInit();
		bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
		bitmapImage.StreamSource = streamSource;
		bitmapImage.EndInit();
		bitmapImage.Freeze();
		return bitmapImage;
	}

	private Rect GetBackgroundViewBox(bool isPresetImage, double pixelWidth, double pixelHeight)
	{
		if (_clipRect.Width > 0.0 && _clipRect.Height > 0.0 && _clipRect.Width > 1.0)
		{
			return new Rect(_clipRect.X / pixelWidth, _clipRect.Y / pixelHeight, _clipRect.Width / pixelWidth, _clipRect.Height / pixelHeight);
		}
		if (isPresetImage)
		{
			return new Rect(new Point(0.0, 0.23), new Point(1.0, 0.58));
		}
		double num = 1.3333333333333333;
		double num2 = pixelWidth / pixelHeight;
		double x = 0.0;
		double y = 0.0;
		double width = 1.0;
		double height = 1.0;
		if (num2 > num)
		{
			double num3 = num / num2;
			width = num3;
			x = (1.0 - num3) / 2.0;
		}
		else
		{
			double num4 = num2 / num;
			height = num4;
			y = (1.0 - num4) / 2.0;
		}
		return new Rect(x, y, width, height);
	}

	public void DisposeImage()
	{
		if (BackgroundImage != null)
		{
			if (BackgroundImage.ImageSource is BitmapImage { IsFrozen: false } bitmapImage)
			{
				bitmapImage.StreamSource?.Dispose();
				bitmapImage.UriSource = null;
			}
			BackgroundImage.ImageSource = null;
			BackgroundImage = null;
		}
	}

	private static Size GetImageSize(string path)
	{
		using FileStream bitmapStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
		BitmapDecoder bitmapDecoder = BitmapDecoder.Create(bitmapStream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
		return new Size(bitmapDecoder.Frames[0].PixelWidth, bitmapDecoder.Frames[0].PixelHeight);
	}

	public BitmapSource LoadCroppedImage(string filePath, Int32Rect cropRect, int? targetWidth)
	{
		BitmapFrame bitmapFrame = null;
		using (FileStream bitmapStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
		{
			BitmapDecoder bitmapDecoder = BitmapDecoder.Create(bitmapStream, BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile | BitmapCreateOptions.IgnoreImageCache, BitmapCacheOption.OnLoad);
			if (bitmapDecoder.Frames.Count == 0)
			{
				throw new InvalidOperationException("No frames found in the image");
			}
			bitmapFrame = bitmapDecoder.Frames[0];
			if (bitmapFrame.CanFreeze)
			{
				bitmapFrame.Freeze();
			}
		}
		if (cropRect.IsEmpty || cropRect.Width <= 0 || cropRect.Height <= 0)
		{
			return bitmapFrame;
		}
		int num = Math.Max(0, Math.Min(cropRect.X, bitmapFrame.PixelWidth - 1));
		int num2 = Math.Max(0, Math.Min(cropRect.Y, bitmapFrame.PixelHeight - 1));
		int width = Math.Max(1, Math.Min(cropRect.Width, bitmapFrame.PixelWidth - num));
		int height = Math.Max(1, Math.Min(cropRect.Height, bitmapFrame.PixelHeight - num2));
		CroppedBitmap croppedBitmap = new CroppedBitmap(bitmapFrame, new Int32Rect(num, num2, width, height));
		bitmapFrame = null;
		WriteableBitmap writeableBitmap = null;
		if (targetWidth.HasValue)
		{
			double num3 = (double)targetWidth.Value / (double)croppedBitmap.PixelWidth;
			writeableBitmap = new WriteableBitmap(new TransformedBitmap(croppedBitmap, new ScaleTransform(num3, num3)));
		}
		else
		{
			writeableBitmap = new WriteableBitmap(croppedBitmap);
		}
		croppedBitmap = null;
		if (writeableBitmap.CanFreeze)
		{
			writeableBitmap.Freeze();
		}
		return writeableBitmap;
	}

	private async Task LoadImageBackground()
	{
		if (StyleType != "fullscreen_image" && StyleType != "polaroid")
		{
			return;
		}
		if (!string.IsNullOrEmpty(_customImagePath))
		{
			try
			{
				double num = 330.0;
				switch (DisplayFromSource)
				{
				case FromSource.MainContainer:
					num = 300.0;
					break;
				case FromSource.StylePreview:
				case FromSource.WidgetStylePreview:
					num = 206.0;
					break;
				case FromSource.Calendar:
				case FromSource.Kanban:
					num = 330.0;
					break;
				}
				num *= VisualTreeHelper.GetDpi(App.Window).PixelsPerDip;
				Size imageSize = GetImageSize(_customImagePath);
				ImageWidth = imageSize.Width;
				ImageHeight = imageSize.Height;
				Rect backgroundViewBox = GetBackgroundViewBox(isPresetImage: false, imageSize.Width, imageSize.Height);
				num = Math.Min(num, imageSize.Width);
				num *= ((imageSize.Height > imageSize.Width) ? 0.8 : 0.7);
				BitmapSource image = LoadCroppedImage(_customImagePath, new Int32Rect
				{
					X = (int)(backgroundViewBox.X * ImageWidth),
					Y = (int)(backgroundViewBox.Y * ImageHeight),
					Width = (int)(backgroundViewBox.Width * ImageWidth),
					Height = (int)(backgroundViewBox.Height * ImageHeight)
				}, (int)num);
				BackgroundImage = new ImageBrush(image)
				{
					Stretch = Stretch.UniformToFill
				};
				return;
			}
			catch (Exception)
			{
				LoadPresetImage();
				return;
			}
		}
		LoadPresetImage();
	}

	private void LoadPresetImage()
	{
		try
		{
			string text = _presetImageKey;
			if (string.IsNullOrEmpty(_presetImageKey))
			{
				text = _type switch
				{
					CountDownType.Birthday => "birthday", 
					CountDownType.Holiday => "holiday", 
					CountDownType.Anniversary => "anniversary", 
					_ => "countdown", 
				};
			}
			BitmapImage cacheImage = GetCacheImage("pack://application:,,,/Assets/CountDown/preset_" + text + ".jpg");
			BackgroundImage = new ImageBrush(cacheImage)
			{
				Stretch = Stretch.UniformToFill,
				Viewbox = GetBackgroundViewBox(isPresetImage: true, cacheImage.PixelWidth, cacheImage.PixelHeight)
			};
			ImageWidth = cacheImage.Width;
			ImageHeight = cacheImage.Height;
		}
		catch (Exception ex)
		{
			Console.WriteLine("加载预设图片失败: " + ex.Message);
			BitmapImage cacheImage2 = GetCacheImage("pack://application:,,,/Assets/CountDown/preset_countdown.jpg");
			BackgroundImage = new ImageBrush(cacheImage2)
			{
				Stretch = Stretch.UniformToFill,
				Viewbox = new Rect(new Point(0.0, 0.23), new Point(1.0, 0.58))
			};
		}
	}

	private BitmapImage GetCacheImage(string path)
	{
		if (!_imageCache.TryGetValue(path, out var value))
		{
			BitmapImage bitmapImage = new BitmapImage();
			bitmapImage.BeginInit();
			bitmapImage.UriSource = new Uri(path);
			bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
			bitmapImage.EndInit();
			bitmapImage.Freeze();
			_imageCache[path] = bitmapImage;
			return bitmapImage;
		}
		return value;
	}

	public void SwitchShowDateOrRemark(bool showRemark = false)
	{
		if (DisplayFromSource == FromSource.MainContainer)
		{
			if (showRemark && !string.IsNullOrEmpty(Model.Remark))
			{
				RemarkText = Utils.GetFirstLine(Model.Remark);
				IsShowRemark = true;
			}
			else
			{
				IsShowRemark = false;
			}
		}
	}

	public async Task OnEditRemark(object param)
	{
		OnShowRemarkDetail?.Invoke();
	}
}
