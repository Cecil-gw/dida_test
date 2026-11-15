using System;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.CountDown.Views;

public class DatePickerPopup : UserControl, IComponentConnector, IStyleConnector
{
	public static readonly DependencyProperty IsGregorianCalendarProperty = DependencyProperty.Register("IsGregorianCalendar", typeof(bool), typeof(DatePickerPopup), new PropertyMetadata(true, OnIsGregorianCalendarChanged));

	public static readonly DependencyProperty CountdownTypeProperty = DependencyProperty.Register("CountdownType", typeof(CountDownType), typeof(DatePickerPopup), new PropertyMetadata(CountDownType.Countdown, OnCountdownTypeChanged));

	public static readonly DependencyProperty IgnoreYearProperty = DependencyProperty.Register("IgnoreYear", typeof(bool), typeof(DatePickerPopup), new PropertyMetadata(false, OnIgnoreYearChanged));

	public static readonly DependencyProperty SelectedDateProperty = DependencyProperty.Register("SelectedDate", typeof(DateTime), typeof(DatePickerPopup), new PropertyMetadata(DateTime.Today, OnSelectedDateChanged));

	private int _lastLunarLeapMonth = -1;

	private int _currentSelectedDayIndex;

	internal Grid TypeTab;

	internal RadioButton GregorianToggle;

	internal RadioButton LunarToggle;

	internal Grid ComboGrid;

	internal ComboBox YearComboBox;

	internal ComboBox MonthComboBox;

	internal ComboBox DayComboBox;

	internal TextBlock ErrorYearHint;

	internal CheckBox IgnoreYearCheckBox;

	internal Button ConfirmButton;

	internal Button CancelButton;

	private bool _contentLoaded;

	private ObservableCollection<DisplayNameValue<int>> years { get; set; } = new ObservableCollection<DisplayNameValue<int>>();

	private ObservableCollection<DisplayNameValue<(int month, bool isleap)>> months { get; set; } = new ObservableCollection<DisplayNameValue<(int, bool)>>();

	private ObservableCollection<DisplayNameValue<int>> days { get; set; } = new ObservableCollection<DisplayNameValue<int>>();

	public bool IsGregorianCalendar
	{
		get
		{
			return (bool)GetValue(IsGregorianCalendarProperty);
		}
		set
		{
			SetValue(IsGregorianCalendarProperty, value);
		}
	}

	public CountDownType CountdownType
	{
		get
		{
			return (CountDownType)GetValue(CountdownTypeProperty);
		}
		set
		{
			SetValue(CountdownTypeProperty, value);
		}
	}

	public bool IgnoreYear
	{
		get
		{
			return (bool)GetValue(IgnoreYearProperty);
		}
		set
		{
			SetValue(IgnoreYearProperty, value);
		}
	}

	public DateTime SelectedDate
	{
		get
		{
			return (DateTime)GetValue(SelectedDateProperty);
		}
		set
		{
			SetValue(SelectedDateProperty, value);
		}
	}

	public DateTime CurrentDate { get; set; }

	private DisplayNameValue<int> beforeInputYear { get; set; }

	private bool loaded { get; set; }

	private bool _isUpdating { get; set; }

	public event EventHandler<DateSelectedEventArgs> DateSelected;

	public event EventHandler SelectionCanceled;

	private static void OnIsGregorianCalendarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		((DatePickerPopup)d).UpdateCalendarType();
	}

	private static void OnCountdownTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		((DatePickerPopup)d).UpdateCountdownTypeSettings();
	}

	private static void OnIgnoreYearChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		((DatePickerPopup)d).UpdateYearVisibility();
	}

	private static void OnSelectedDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		((DatePickerPopup)d).UpdateSelection();
	}

	public DatePickerPopup()
	{
		InitializeComponent();
		YearComboBox.ItemsSource = years;
		MonthComboBox.ItemsSource = months;
		DayComboBox.ItemsSource = days;
		if (Utils.IsTickPackage())
		{
			TypeTab.Visibility = Visibility.Collapsed;
			UpdateComboColumn();
		}
		base.Loaded += async delegate
		{
			loaded = true;
			CurrentDate = SelectedDate;
			UpdateCountdownTypeSettings();
			UpdateCalendarType();
		};
	}

	private void UpdateComboColumn()
	{
		DateFormatType dateFormatType = DateFormatType.YMD;
		if (Utils.IsTickPackage())
		{
			dateFormatType = DateUtils.GetSysDateFormatType();
		}
		ColumnDefinitionCollection columnDefinitions = ComboGrid.ColumnDefinitions;
		switch (dateFormatType)
		{
		case DateFormatType.MDY:
			Grid.SetColumn(MonthComboBox, 0);
			Grid.SetColumn(DayComboBox, 1);
			Grid.SetColumn(YearComboBox, 2);
			if (IgnoreYear)
			{
				columnDefinitions[0].Width = new GridLength(1.0, GridUnitType.Star);
				columnDefinitions[1].Width = new GridLength(1.0, GridUnitType.Star);
				columnDefinitions[2].Width = new GridLength(0.0);
			}
			else
			{
				columnDefinitions[0].Width = new GridLength(3.0, GridUnitType.Star);
				columnDefinitions[1].Width = new GridLength(3.0, GridUnitType.Star);
				columnDefinitions[2].Width = new GridLength(4.0, GridUnitType.Star);
			}
			break;
		case DateFormatType.DMY:
			Grid.SetColumn(DayComboBox, 0);
			Grid.SetColumn(MonthComboBox, 1);
			Grid.SetColumn(YearComboBox, 2);
			if (IgnoreYear)
			{
				columnDefinitions[0].Width = new GridLength(1.0, GridUnitType.Star);
				columnDefinitions[1].Width = new GridLength(1.0, GridUnitType.Star);
				columnDefinitions[2].Width = new GridLength(0.0);
			}
			else
			{
				columnDefinitions[0].Width = new GridLength(3.0, GridUnitType.Star);
				columnDefinitions[1].Width = new GridLength(3.0, GridUnitType.Star);
				columnDefinitions[2].Width = new GridLength(4.0, GridUnitType.Star);
			}
			break;
		case DateFormatType.YMD:
			Grid.SetColumn(YearComboBox, 0);
			Grid.SetColumn(MonthComboBox, 1);
			Grid.SetColumn(DayComboBox, 2);
			if (IgnoreYear)
			{
				columnDefinitions[0].Width = new GridLength(0.0);
				columnDefinitions[1].Width = new GridLength(1.0, GridUnitType.Star);
				columnDefinitions[2].Width = new GridLength(1.0, GridUnitType.Star);
			}
			else
			{
				columnDefinitions[0].Width = new GridLength(4.0, GridUnitType.Star);
				columnDefinitions[1].Width = new GridLength(3.0, GridUnitType.Star);
				columnDefinitions[2].Width = new GridLength(3.0, GridUnitType.Star);
			}
			break;
		}
	}

	private async Task UpdateCalendarType()
	{
		if (!loaded || _isUpdating)
		{
			return;
		}
		_isUpdating = true;
		try
		{
			ErrorYearHint.Visibility = Visibility.Collapsed;
			GregorianToggle.IsChecked = IsGregorianCalendar;
			LunarToggle.IsChecked = !IsGregorianCalendar;
			IgnoreYearCheckBox.IsChecked = IgnoreYear;
			LoadYears();
			if (YearComboBox.SelectedItem == null)
			{
				if (IsGregorianCalendar)
				{
					SelectYearItem(CurrentDate.Year);
				}
				else
				{
					LunarUtils.ChineseCalendar chineseCalendar = new LunarUtils.ChineseCalendar(CurrentDate);
					SelectYearItem(chineseCalendar.ChineseYear);
				}
			}
			LoadMonths(IsGregorianCalendar);
			int year = CurrentDate.Year;
			int month = CurrentDate.Month;
			bool isLeapMonth = false;
			if (!IsGregorianCalendar)
			{
				LunarUtils.ChineseCalendar chineseCalendar2 = new LunarUtils.ChineseCalendar(CurrentDate);
				year = chineseCalendar2.ChineseYear;
				month = chineseCalendar2.ChineseMonth;
				isLeapMonth = chineseCalendar2.IsChineseLeapMonth;
			}
			LoadDays(year, month, IsGregorianCalendar, isLeapMonth);
			UpdateSelection();
			await Task.Delay(200);
			DisableInputMethod(YearComboBox);
			DisableInputMethod(MonthComboBox);
			DisableInputMethod(DayComboBox);
		}
		finally
		{
			_isUpdating = false;
		}
	}

	private void UpdateYearVisibility()
	{
		if (IgnoreYearCheckBox == null || YearComboBox == null)
		{
			return;
		}
		_isUpdating = true;
		try
		{
			IgnoreYearCheckBox.IsChecked = IgnoreYear;
			YearComboBox.Visibility = (IgnoreYear ? Visibility.Collapsed : Visibility.Visible);
			if (YearComboBox.Parent is Grid)
			{
				UpdateComboColumn();
			}
		}
		finally
		{
			_isUpdating = false;
		}
	}

	private void UpdateSelection()
	{
		if (!loaded)
		{
			return;
		}
		_isUpdating = true;
		try
		{
			if (IsGregorianCalendar)
			{
				SelectYearItem(CurrentDate.Year);
				SelectMonthItem(CurrentDate.Month, isLeapMonth: false);
				SelectDayItem(CurrentDate.Day);
				return;
			}
			LunarUtils.ChineseCalendar chineseCalendar = new LunarUtils.ChineseCalendar(CurrentDate);
			int chineseYear = chineseCalendar.ChineseYear;
			int chineseMonth = chineseCalendar.ChineseMonth;
			int chineseDay = chineseCalendar.ChineseDay;
			bool isChineseLeapMonth = chineseCalendar.IsChineseLeapMonth;
			_ = chineseCalendar.IsChineseLeapYear;
			SelectYearItem(chineseYear);
			SelectMonthItem(chineseMonth, isChineseLeapMonth);
			SelectDayItem(chineseDay);
		}
		finally
		{
			_isUpdating = false;
		}
	}

	private void LoadYears()
	{
		if (YearComboBox == null)
		{
			return;
		}
		years.Clear();
		int num = 2099;
		for (int i = 1910; i <= num; i++)
		{
			if (IsGregorianCalendar)
			{
				string arg = (Utils.IsCn() ? "年" : string.Empty);
				years.Add(new DisplayNameValue<int>
				{
					Name = $"{i}{arg}",
					Value = i
				});
			}
			else
			{
				string chineseYearName = DateUtils.GetChineseYearName(i);
				years.Add(new DisplayNameValue<int>
				{
					Name = $"{chineseYearName}({i})",
					Value = i
				});
			}
		}
	}

	private void LoadMonths(bool isGregorian)
	{
		if (MonthComboBox == null)
		{
			return;
		}
		months.Clear();
		for (int i = 1; i <= 12; i++)
		{
			if (isGregorian)
			{
				DateTime date = new DateTime(DateTime.Now.Year, i, 1);
				months.Add(new DisplayNameValue<(int, bool)>
				{
					Name = DateUtils.FormatShortMonth(date),
					Value = (i, false)
				});
			}
			else
			{
				string chineseMonthName = DateUtils.GetChineseMonthName(i);
				months.Add(new DisplayNameValue<(int, bool)>
				{
					Name = chineseMonthName,
					Value = (i, false)
				});
			}
		}
		if (isGregorian || IgnoreYear)
		{
			return;
		}
		int year = (IgnoreYear ? DateTime.Now.Year : ParseSelectedYear());
		_lastLunarLeapMonth = GetLeapMonthInYear(year);
		for (int j = 0; j < months.Count; j++)
		{
			if (months[j].Name.StartsWith("闰"))
			{
				months.RemoveAt(j);
				break;
			}
		}
		if (_lastLunarLeapMonth > 0)
		{
			string name = "闰" + DateUtils.GetChineseMonthName(_lastLunarLeapMonth);
			months.Insert(_lastLunarLeapMonth, new DisplayNameValue<(int, bool)>
			{
				Name = name,
				Value = (_lastLunarLeapMonth, true)
			});
		}
	}

	private void LoadDays(int year, int month, bool isGregorian, bool isLeapMonth)
	{
		if (DayComboBox == null)
		{
			return;
		}
		_currentSelectedDayIndex = DayComboBox.SelectedIndex;
		days.Clear();
		int num;
		if (IgnoreYear)
		{
			if (isGregorian)
			{
				switch (month)
				{
				case 2:
					num = 29;
					break;
				case 4:
				case 6:
				case 9:
				case 11:
					num = 30;
					break;
				default:
					num = 31;
					break;
				}
			}
			else
			{
				num = 30;
			}
		}
		else
		{
			num = GetDaysInMonth(year, month, isGregorian, isLeapMonth);
		}
		for (int i = 1; i <= num; i++)
		{
			if (isGregorian)
			{
				DateTime dateTime = new DateTime(DateTime.Now.Year, 1, i);
				days.Add(new DisplayNameValue<int>
				{
					Name = dateTime.ToString("dd", App.Ci) + (Utils.IsCn() ? "日" : ""),
					Value = i
				});
			}
			else
			{
				string chineseDayName = DateUtils.GetChineseDayName(i);
				days.Add(new DisplayNameValue<int>
				{
					Name = chineseDayName,
					Value = i
				});
			}
		}
	}

	private void UpdateCountdownTypeSettings()
	{
		if (IgnoreYearCheckBox == null)
		{
			return;
		}
		_isUpdating = true;
		try
		{
			switch (CountdownType)
			{
			case CountDownType.Holiday:
			case CountDownType.Birthday:
				IgnoreYearCheckBox.Visibility = Visibility.Visible;
				break;
			default:
				IgnoreYear = false;
				IgnoreYearCheckBox.Visibility = Visibility.Collapsed;
				break;
			}
			UpdateYearVisibility();
		}
		finally
		{
			_isUpdating = false;
		}
	}

	private string GetChineseDayName(int day)
	{
		switch (day)
		{
		case 10:
			return "初十";
		case 20:
			return "二十";
		case 30:
			return "三十";
		default:
		{
			string[] array = new string[4] { "", "十", "二十", "三十" };
			string[] array2 = new string[11]
			{
				"", "一", "二", "三", "四", "五", "六", "七", "八", "九",
				"十"
			};
			int num = day / 10;
			int num2 = day % 10;
			if (day <= 10)
			{
				return "初" + array2[day];
			}
			return array[num] + ((num2 > 0) ? array2[num2] : "");
		}
		}
	}

	private void SelectYearItem(int year)
	{
		if (YearComboBox != null)
		{
			DisplayNameValue<int> displayNameValue = years.FirstOrDefault((DisplayNameValue<int> x) => x.Value == year);
			YearComboBox.SelectedItem = displayNameValue;
			YearComboBox.Text = displayNameValue?.Name;
			TextBox textBox = FindComboTextBox(YearComboBox);
			if (textBox != null)
			{
				textBox.CaretIndex = textBox.Text.Length;
			}
		}
	}

	private void SelectMonthItem(int month, bool isLeapMonth)
	{
		if (MonthComboBox == null)
		{
			return;
		}
		(int month, bool) target = (month: month, !IgnoreYear && isLeapMonth);
		DisplayNameValue<(int, bool)> displayNameValue = months.FirstOrDefault(delegate(DisplayNameValue<(int month, bool isleap)> x)
		{
			(int, bool) value = x.Value;
			(int, bool) tuple = target;
			return value.Item1 == tuple.Item1 && value.Item2 == tuple.Item2;
		});
		if (displayNameValue == null && isLeapMonth)
		{
			target = (month: month, false);
			displayNameValue = months.FirstOrDefault(delegate(DisplayNameValue<(int month, bool isleap)> x)
			{
				(int, bool) value = x.Value;
				(int, bool) tuple = target;
				return value.Item1 == tuple.Item1 && value.Item2 == tuple.Item2;
			});
		}
		MonthComboBox.SelectedItem = displayNameValue;
		MonthComboBox.Text = displayNameValue?.Name;
		TextBox textBox = FindComboTextBox(MonthComboBox);
		if (textBox != null)
		{
			textBox.CaretIndex = textBox.Text.Length;
		}
	}

	private void SelectDayItem(int day)
	{
		if (DayComboBox != null)
		{
			DisplayNameValue<int> displayNameValue = days.FirstOrDefault((DisplayNameValue<int> x) => x.Value == day);
			DayComboBox.SelectedItem = displayNameValue;
			DayComboBox.Text = displayNameValue?.Name;
			TextBox textBox = FindComboTextBox(DayComboBox);
			if (textBox != null)
			{
				textBox.CaretIndex = textBox.Text.Length;
			}
		}
	}

	private int ParseSelectedYear()
	{
		if (YearComboBox == null || YearComboBox.SelectedItem == null)
		{
			return DateTime.Now.Year;
		}
		if (!(YearComboBox.SelectedItem is DisplayNameValue<int> displayNameValue))
		{
			return DateTime.Now.Year;
		}
		return displayNameValue.Value;
	}

	private (int Month, bool IsLeapMonth) ParseSelectedMonth()
	{
		if (MonthComboBox == null || MonthComboBox.SelectedItem == null)
		{
			return (Month: 1, IsLeapMonth: false);
		}
		if (!(MonthComboBox.SelectedItem is DisplayNameValue<(int, bool)> displayNameValue))
		{
			return (Month: 1, IsLeapMonth: false);
		}
		return displayNameValue.Value;
	}

	private int ParseSelectedDay()
	{
		if (DayComboBox == null || DayComboBox.SelectedItem == null)
		{
			return 1;
		}
		if (!(DayComboBox.SelectedItem is DisplayNameValue<int> displayNameValue))
		{
			return 1;
		}
		return displayNameValue.Value;
	}

	private DateTime GetSelectedDateTime()
	{
		if (IsGregorianCalendar)
		{
			int year = ParseSelectedYear();
			int item = ParseSelectedMonth().Month;
			int num = ParseSelectedDay();
			if (IgnoreYear && num == 29 && item == 2 && !IsLeapYear(year))
			{
				year = FindPreviousLeapYear(year);
			}
			int val = DateTime.DaysInMonth(year, item);
			num = Math.Min(num, val);
			try
			{
				return new DateTime(year, item, num);
			}
			catch
			{
				return DateTime.Today;
			}
		}
		int num2 = ParseSelectedDay();
		var (num3, flag) = ParseSelectedMonth();
		if (IgnoreYear)
		{
			int cy = FindValidLunarYearForDay(num3, num2, flag);
			try
			{
				return new LunarUtils.ChineseCalendar(cy, num3, num2, flag).Date;
			}
			catch
			{
				return DateTime.Today;
			}
		}
		int num4 = ParseSelectedYear();
		int daysInMonth = GetDaysInMonth(num4, num3, isGregorian: false);
		num2 = Math.Min(num2, daysInMonth);
		try
		{
			return new LunarUtils.ChineseCalendar(num4, num3, num2, flag).Date;
		}
		catch
		{
			return DateTime.Today;
		}
	}

	private bool IsLeapYear(int year)
	{
		if (year % 4 == 0 && (year % 100 != 0 || year % 400 == 0))
		{
			return true;
		}
		return false;
	}

	private int FindPreviousLeapYear(int year)
	{
		while (!IsLeapYear(--year))
		{
		}
		return year;
	}

	private int FindValidLunarYearForDay(int month, int day, bool isLeapMonth)
	{
		int chineseYear = new LunarUtils.ChineseCalendar(DateTime.Today).ChineseYear;
		int num = 200;
		int num2 = chineseYear;
		while (num-- > 0)
		{
			try
			{
				if (GetDaysInMonth(num2, month, isGregorian: false) >= day)
				{
					return num2;
				}
				num2--;
			}
			catch
			{
				num2--;
			}
		}
		return chineseYear;
	}

	private int GetDaysInMonth(int year, int month, bool isGregorian, bool isLeapMonth = false)
	{
		if (isGregorian)
		{
			return DateTime.DaysInMonth(year, month);
		}
		return LunarUtils.ChineseCalendar.GetMonthTotalDays(year, month, isLeapMonth);
	}

	private int GetLeapMonthInYear(int year)
	{
		if (new LunarUtils.ChineseCalendar(new DateTime(year, 3, 1)).IsChineseLeapYear)
		{
			for (int i = 0; i < 12; i++)
			{
				LunarUtils.ChineseCalendar chineseCalendar = new LunarUtils.ChineseCalendar(new DateTime(year, i + 1, 1));
				if (chineseCalendar.IsChineseLeapMonth)
				{
					return chineseCalendar.ChineseMonth;
				}
			}
		}
		return 0;
	}

	private bool IsLeapMonth(int month)
	{
		if (MonthComboBox != null && MonthComboBox.SelectedItem != null)
		{
			if (MonthComboBox.SelectedItem is DisplayNameValue<(int, bool)> displayNameValue)
			{
				return displayNameValue.Value.Item2;
			}
			return false;
		}
		return false;
	}

	private bool IsValidMonthInYear(int year, int month, bool isLeapMonth)
	{
		if (IsGregorianCalendar)
		{
			return true;
		}
		if (!isLeapMonth)
		{
			return true;
		}
		return GetLeapMonthInYear(year) == month;
	}

	private int GetNearestValidMonth(int year, int month, bool isLeapMonth)
	{
		if (!IsGregorianCalendar)
		{
		}
		return month;
	}

	private void GregorianToggle_Checked(object sender, RoutedEventArgs e)
	{
		if (_isUpdating || GregorianToggle.IsChecked != true)
		{
			return;
		}
		if (IgnoreYear)
		{
			int year = ParseSelectedYear();
			int item = ParseSelectedMonth().Month;
			int day = ParseSelectedDay();
			try
			{
				CurrentDate = new DateTime(year, item, day);
			}
			catch (Exception)
			{
				CurrentDate = new DateTime(year, item, 1).AddMonths(1).AddDays(-1.0);
			}
		}
		IsGregorianCalendar = true;
		UpdateCalendarType();
	}

	private void LunarToggle_Checked(object sender, RoutedEventArgs e)
	{
		if (_isUpdating || LunarToggle.IsChecked != true)
		{
			return;
		}
		if (IgnoreYear)
		{
			int num = ParseSelectedYear();
			(int Month, bool IsLeapMonth) tuple = ParseSelectedMonth();
			int item = tuple.Month;
			bool item2 = tuple.IsLeapMonth;
			int num2 = ParseSelectedDay();
			try
			{
				int monthTotalDays = LunarUtils.ChineseCalendar.GetMonthTotalDays(num, item, isLeapMonth: false);
				if (num2 > monthTotalDays)
				{
					num2 = monthTotalDays;
				}
				LunarUtils.ChineseCalendar chineseCalendar = new LunarUtils.ChineseCalendar(num, item, num2, item2);
				CurrentDate = chineseCalendar.Date;
			}
			catch
			{
				CurrentDate = DateTime.Today;
			}
		}
		IsGregorianCalendar = false;
		UpdateCalendarType();
	}

	private void IgnoreYearCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
	{
		if (!_isUpdating)
		{
			IgnoreYear = IgnoreYearCheckBox.IsChecked == true;
			UpdateYearVisibility();
			UpdateCalendarType();
		}
	}

	private void YearComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_isUpdating || YearComboBox.SelectedIndex < 0 || MonthComboBox.SelectedIndex < 0)
		{
			return;
		}
		CurrentDate = GetSelectedDateTime();
		_isUpdating = true;
		try
		{
			int year = ParseSelectedYear();
			var (month, isLeapMonth) = ParseSelectedMonth();
			if (!IsGregorianCalendar)
			{
				LoadMonths(IsGregorianCalendar);
			}
			if (!IsValidMonthInYear(year, month, isLeapMonth))
			{
				int nearestValidMonth = GetNearestValidMonth(year, month, isLeapMonth);
				SelectMonthItem(nearestValidMonth, isLeapMonth);
			}
			else
			{
				SelectMonthItem(month, isLeapMonth);
			}
			LoadDays(year, month, IsGregorianCalendar, isLeapMonth: false);
			int num = _currentSelectedDayIndex + 1;
			int daysInMonth = GetDaysInMonth(year, month, IsGregorianCalendar);
			if (num > daysInMonth)
			{
				DayComboBox.SelectedIndex = daysInMonth - 1;
			}
			else if (num <= daysInMonth && num > 0)
			{
				DayComboBox.SelectedIndex = num - 1;
			}
			else if (days.Count > 0)
			{
				DayComboBox.SelectedIndex = 0;
			}
		}
		finally
		{
			_isUpdating = false;
		}
	}

	private void MonthComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_isUpdating || MonthComboBox.SelectedIndex < 0)
		{
			return;
		}
		CurrentDate = GetSelectedDateTime();
		_isUpdating = true;
		try
		{
			int year = (IgnoreYear ? DateTime.Now.Year : ParseSelectedYear());
			(int Month, bool IsLeapMonth) tuple = ParseSelectedMonth();
			int item = tuple.Month;
			bool item2 = tuple.IsLeapMonth;
			int num = DayComboBox.SelectedIndex + 1;
			int daysInMonth = GetDaysInMonth(year, item, IsGregorianCalendar, item2);
			LoadDays(year, item, IsGregorianCalendar, item2);
			if (daysInMonth > 0 && num > daysInMonth)
			{
				DayComboBox.SelectedIndex = daysInMonth - 1;
			}
			else if (num <= daysInMonth && num > 0)
			{
				DayComboBox.SelectedIndex = num - 1;
			}
			else if (days.Count > 0)
			{
				DayComboBox.SelectedIndex = 0;
			}
		}
		finally
		{
			_isUpdating = false;
		}
	}

	private void DayComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (!_isUpdating)
		{
			CurrentDate = GetSelectedDateTime();
		}
	}

	private void CancelButton_Click(object sender, RoutedEventArgs e)
	{
		IgnoreYear = false;
		this.SelectionCanceled?.Invoke(this, EventArgs.Empty);
	}

	private void ConfirmButton_Click(object sender, RoutedEventArgs e)
	{
		Confirm();
	}

	private void Confirm()
	{
		if (ErrorYearHint.Visibility == Visibility.Collapsed)
		{
			DateTime selectedDate = (CurrentDate = GetSelectedDateTime());
			SelectedDate = selectedDate;
			DateSelectedEventArgs e = new DateSelectedEventArgs
			{
				Date = CurrentDate,
				IsGregorianCalendar = IsGregorianCalendar,
				IgnoreYear = IgnoreYear
			};
			this.DateSelected?.Invoke(this, e);
		}
	}

	private TextBox FindComboTextBox(ComboBox comboBox, bool applyTemplate = false)
	{
		if (!comboBox.IsEditable)
		{
			return null;
		}
		if (applyTemplate)
		{
			comboBox.ApplyTemplate();
		}
		return comboBox.Template.FindName("PART_EditableTextBox", comboBox) as TextBox;
	}

	private void DisableInputMethod(ComboBox comboBox)
	{
		TextBox textBox = FindComboTextBox(comboBox, applyTemplate: true);
		if (textBox != null)
		{
			InputMethod.SetIsInputMethodEnabled(textBox, value: false);
		}
	}

	private void ComboBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
	{
		(sender as ComboBox).IsDropDownOpen = false;
		bool handled = LimitComboBoxInput(sender, e);
		e.Handled = handled;
	}

	private bool LimitComboBoxInput(object sender, TextCompositionEventArgs e)
	{
		if (new Regex("[^0-9]+").IsMatch(e.Text))
		{
			return true;
		}
		ComboBox comboBox = sender as ComboBox;
		TextBox textBox = FindComboTextBox(comboBox);
		if (textBox == null)
		{
			return false;
		}
		string preText = GetPreText(textBox, e.Text);
		if (sender == MonthComboBox)
		{
			return !(comboBox.ItemsSource as ObservableCollection<DisplayNameValue<(int, bool)>>).Any((DisplayNameValue<(int month, bool)> x) => x.Value.month.ToString().Contains(preText));
		}
		return !(comboBox.ItemsSource as ObservableCollection<DisplayNameValue<int>>).Any((DisplayNameValue<int> x) => x.Name.Contains(preText));
	}

	private string GetPreText(TextBox textBox, string nextText)
	{
		int selectionLength = textBox.SelectionLength;
		if (selectionLength > 0)
		{
			string text = textBox.Text.Remove(textBox.SelectionStart, selectionLength);
			return text.Insert(textBox.SelectionStart, nextText);
		}
		return textBox.Text.Insert(textBox.CaretIndex, nextText);
	}

	private void YearComboBox_GotFocus(object sender, RoutedEventArgs e)
	{
		RecordYearBeforeInput(sender as ComboBox);
	}

	private void YearComboBox_DropDownOpened(object sender, EventArgs e)
	{
		RecordYearBeforeInput(sender as ComboBox);
	}

	private void RecordYearBeforeInput(ComboBox comboBox)
	{
		if (comboBox.IsEditable)
		{
			beforeInputYear = comboBox.SelectedItem as DisplayNameValue<int>;
			ErrorYearHint.Visibility = Visibility.Collapsed;
		}
	}

	private async void YearComboBox_KeyDown(object sender, KeyEventArgs e)
	{
		ComboBox comboBox = sender as ComboBox;
		if (comboBox.IsEditable && e.Key == Key.Return && CheckInputYear(comboBox))
		{
			MonthComboBox.Focus();
		}
	}

	private void YearComboBox_LostFocus(object sender, RoutedEventArgs e)
	{
		CheckInputYear(sender as ComboBox);
	}

	private bool CheckInputYear(ComboBox comboBox)
	{
		if (!comboBox.IsEditable)
		{
			return true;
		}
		string text = comboBox.Text;
		ErrorYearHint.Visibility = Visibility.Collapsed;
		if (text == null)
		{
			return false;
		}
		switch (text.Length)
		{
		case 1:
		case 3:
		{
			if (int.TryParse(text, out var result2) && result2 == 0 && beforeInputYear != null)
			{
				SelectYearItem(beforeInputYear.Value);
				return false;
			}
			ErrorYearHint.Visibility = Visibility.Visible;
			return false;
		}
		case 2:
		{
			if (!int.TryParse(text, out var result3))
			{
				return false;
			}
			int num = DateTime.Now.Year % 100;
			int num2 = DateTime.Now.Year / 100;
			string text2 = text;
			if (result3 >= 0 && result3 <= num + 10)
			{
				text2 = num2.ToString("D2") + result3.ToString("D2");
			}
			else
			{
				if (result3 <= num + 10 || result3 > 99)
				{
					return false;
				}
				text2 = (num2 - 1).ToString("D2") + result3.ToString("D2");
			}
			int targetValue = int.Parse(text2);
			DisplayNameValue<int> displayNameValue = years.FirstOrDefault((DisplayNameValue<int> x) => x.Value == targetValue);
			if (displayNameValue != null)
			{
				SelectYearItem(displayNameValue.Value);
				return true;
			}
			break;
		}
		case 4:
		{
			if (int.TryParse(text, out var result) && result == 0 && beforeInputYear != null)
			{
				SelectYearItem(beforeInputYear.Value);
				return false;
			}
			break;
		}
		}
		text = comboBox.Text;
		if (int.TryParse(text, out var numberValue))
		{
			DisplayNameValue<int> displayNameValue2 = years.FirstOrDefault((DisplayNameValue<int> x) => x.Value == numberValue);
			if (displayNameValue2 == null)
			{
				return false;
			}
			SelectYearItem(displayNameValue2.Value);
			return true;
		}
		DisplayNameValue<int> displayNameValue3 = years.FirstOrDefault((DisplayNameValue<int> x) => x.Name == text);
		if (displayNameValue3 == null)
		{
			return false;
		}
		SelectYearItem(displayNameValue3.Value);
		return true;
	}

	private void MonthComboBox_KeyDown(object sender, KeyEventArgs e)
	{
		ComboBox comboBox = sender as ComboBox;
		if (comboBox.IsEditable && e.Key == Key.Return && CheckInputMonth(comboBox))
		{
			DayComboBox.Focus();
		}
	}

	private void MonthComboBox_LostFocus(object sender, RoutedEventArgs e)
	{
		CheckInputMonth(sender as ComboBox);
	}

	private bool CheckInputMonth(ComboBox comboBox)
	{
		if (!comboBox.IsEditable)
		{
			return true;
		}
		string text = comboBox.Text;
		if (int.TryParse(text, out var number))
		{
			DisplayNameValue<(int, bool)> displayNameValue = months.FirstOrDefault((DisplayNameValue<(int month, bool isleap)> x) => x.Value.month == number);
			if (displayNameValue == null)
			{
				return false;
			}
			SelectMonthItem(displayNameValue.Value.Item1, displayNameValue.Value.Item2);
			return true;
		}
		DisplayNameValue<(int, bool)> displayNameValue2 = months.FirstOrDefault((DisplayNameValue<(int month, bool isleap)> x) => x.Name == text);
		if (displayNameValue2 == null)
		{
			return false;
		}
		SelectMonthItem(displayNameValue2.Value.Item1, displayNameValue2.Value.Item2);
		return true;
	}

	private bool CheckInputDay(ComboBox comboBox)
	{
		if (!comboBox.IsEditable)
		{
			return true;
		}
		string text = comboBox.Text;
		if (int.TryParse(text, out var number))
		{
			DisplayNameValue<int> displayNameValue = days.FirstOrDefault((DisplayNameValue<int> x) => x.Value == number);
			if (displayNameValue == null)
			{
				return false;
			}
			SelectDayItem(displayNameValue.Value);
			return true;
		}
		DisplayNameValue<int> displayNameValue2 = years.FirstOrDefault((DisplayNameValue<int> x) => x.Name == text);
		if (displayNameValue2 == null)
		{
			return false;
		}
		SelectDayItem(displayNameValue2.Value);
		return true;
	}

	private void DayComboBox_KeyDown(object sender, KeyEventArgs e)
	{
		ComboBox comboBox = sender as ComboBox;
		if (comboBox.IsEditable && e.Key == Key.Return)
		{
			CheckInputDay(comboBox);
		}
	}

	private void DayComboBox_LostFocus(object sender, RoutedEventArgs e)
	{
		CheckInputDay(sender as ComboBox);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/countdown/views/datepickerpopup.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			TypeTab = (Grid)target;
			break;
		case 2:
			GregorianToggle = (RadioButton)target;
			GregorianToggle.Checked += GregorianToggle_Checked;
			break;
		case 3:
			LunarToggle = (RadioButton)target;
			LunarToggle.Checked += LunarToggle_Checked;
			break;
		case 4:
			ComboGrid = (Grid)target;
			break;
		case 6:
			YearComboBox = (ComboBox)target;
			YearComboBox.DropDownOpened += YearComboBox_DropDownOpened;
			YearComboBox.KeyDown += YearComboBox_KeyDown;
			YearComboBox.LostFocus += YearComboBox_LostFocus;
			YearComboBox.GotFocus += YearComboBox_GotFocus;
			YearComboBox.SelectionChanged += YearComboBox_SelectionChanged;
			break;
		case 7:
			MonthComboBox = (ComboBox)target;
			MonthComboBox.LostFocus += MonthComboBox_LostFocus;
			MonthComboBox.KeyDown += MonthComboBox_KeyDown;
			MonthComboBox.SelectionChanged += MonthComboBox_SelectionChanged;
			break;
		case 8:
			DayComboBox = (ComboBox)target;
			DayComboBox.KeyDown += DayComboBox_KeyDown;
			DayComboBox.LostFocus += DayComboBox_LostFocus;
			DayComboBox.SelectionChanged += DayComboBox_SelectionChanged;
			break;
		case 9:
			ErrorYearHint = (TextBlock)target;
			break;
		case 10:
			IgnoreYearCheckBox = (CheckBox)target;
			IgnoreYearCheckBox.Checked += IgnoreYearCheckBox_CheckedChanged;
			IgnoreYearCheckBox.Unchecked += IgnoreYearCheckBox_CheckedChanged;
			break;
		case 11:
			ConfirmButton = (Button)target;
			ConfirmButton.Click += ConfirmButton_Click;
			break;
		case 12:
			CancelButton = (Button)target;
			CancelButton.Click += CancelButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 5)
		{
			EventSetter eventSetter = new EventSetter();
			eventSetter.Event = UIElement.PreviewTextInputEvent;
			eventSetter.Handler = new TextCompositionEventHandler(ComboBox_PreviewTextInput);
			((Style)target).Setters.Add(eventSetter);
		}
	}
}
