using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using Ical.Net;
using Ical.Net.DataTypes;
using ticktick_WPF.Dal;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Time;

namespace ticktick_WPF.Views;

public class CustomRepeatDialog : UserControl, ITabControl, IComponentConnector
{
	public delegate void CustomRepeatSetDelegate(string repeatFrom, string repeatFlag);

	private RecurrencePattern _pattern;

	private Popup _popup;

	private bool _calendarMode;

	private bool _countdownMode;

	private bool _lunarMode;

	private DateTime _startTime;

	private RepeatFromType _repeatFrom;

	private string _repeatFlag = string.Empty;

	private int _maxIntervalLimit = 365;

	private Dictionary<DateTime, bool> _showedDays = new Dictionary<DateTime, bool>();

	private ObservableCollection<ComboBoxViewModel> _repeatModeItems;

	private ObservableCollection<ComboBoxViewModel> _repeatUnitItems;

	private ObservableCollection<ComboBoxViewModel> _workdayItems;

	private ObservableCollection<ComboBoxViewModel> _monthByWeekNumItems;

	private ObservableCollection<ComboBoxViewModel> _monthByWeekDayItems;

	private string _flagCustom;

	private List<DateTime> _customDateList = new List<DateTime>();

	private static readonly string RepeatByDue = Utils.GetString("RepeatByDue");

	private static readonly string RepeatByComplete = Utils.GetString("RepeatByComplete");

	private static readonly string RepeatByCustom = Utils.GetString("RepeatByCustom");

	private static readonly string Day = Utils.GetString("PublicUpDay");

	private static readonly string Week = Utils.GetString("PublicWeek");

	private static readonly string Month = Utils.GetString("PublicMonth");

	private static readonly string Year = Utils.GetString("TimelineYear");

	private static readonly string FirstWorkday = Utils.GetString("FirstWorkday");

	private static readonly string LastWorkday = Utils.GetString("LastWorkday");

	private int _tabIndex;

	internal CustomComboBox RepeatModeComboBox;

	internal TextBlock RepeatByDateText;

	internal Image ProIcon;

	internal Grid CustomChooseRepeatUnitGrid;

	internal TextBox CustomChooseRepeatUnitTimePopupTextBox;

	internal TextBox EmptyBox;

	internal TextBlock CountdownRepeatDesc;

	internal CustomComboBox ChooseRepeatUnitComboBox;

	internal Grid CustomChooseRepeatUnitPopupGrid;

	internal WeekdaySelector WeekDaySelector;

	internal Grid CustomChooseRepeatUnitMonthPopupGrid;

	internal GroupTitle2 SwitchGroupTitles;

	internal Grid WorkdayGrid;

	internal CustomComboBox WorkdayCombox;

	internal Border YearMonthGrid;

	internal CustomComboBox YearMonthCombobox;

	internal MonthDaySelector MonthDaySelector;

	internal Grid CustomChooseRepeatUnitMonthByWeekPopupGridGrid;

	internal CustomComboBox CustomChooseRepeatUnitMonthByWeekNumPopupGridComboBox;

	internal CustomComboBox CustomChooseRepeatUnitMonthByWeekDayPopupGridComboBox;

	internal TickDatePicker CustomDaySelector;

	internal Border SkipHolidayGrid;

	internal CheckBox SkipHolidayCheckbox;

	internal Border SkipWeekendGrid;

	internal CheckBox SkipWeekendCheckbox;

	internal Button CancelButton;

	internal Button SaveButton;

	private bool _contentLoaded;

	private string _realRepeatFrom
	{
		get
		{
			if (_repeatFrom != RepeatFromType.CompleteTime)
			{
				return "0";
			}
			return "1";
		}
	}

	public event CustomRepeatSetDelegate OnCustomRepeatSet;

	public CustomRepeatDialog()
	{
		InitializeComponent();
		InitRepeatModeComboBox();
		InitRepeatUnitComboBox(_countdownMode);
		InitWorkdayComboBox();
		InitMonthByWeekComboBox();
	}

	private void InitMonthByWeekComboBox()
	{
		string text = Utils.GetString("TheFirst");
		string text2 = Utils.GetString("TheSecond");
		string text3 = Utils.GetString("TheThird");
		string text4 = Utils.GetString("TheFourth");
		string text5 = Utils.GetString("TheFifth");
		string text6 = Utils.GetString("TheLast");
		_monthByWeekNumItems = new ObservableCollection<ComboBoxViewModel>
		{
			new ComboBoxViewModel(text, text, 32.0)
			{
				Selected = true
			},
			new ComboBoxViewModel(text2, text2, 32.0),
			new ComboBoxViewModel(text3, text3, 32.0),
			new ComboBoxViewModel(text4, text4, 32.0),
			new ComboBoxViewModel(text5, text5, 32.0),
			new ComboBoxViewModel(text6, text6, 32.0)
		};
		CustomChooseRepeatUnitMonthByWeekNumPopupGridComboBox.Init(_monthByWeekNumItems, _monthByWeekNumItems[0]);
		string text7 = Utils.GetString("Sun");
		string text8 = Utils.GetString("Mon");
		string text9 = Utils.GetString("Tue");
		string text10 = Utils.GetString("Wed");
		string text11 = Utils.GetString("Thu");
		string text12 = Utils.GetString("Fri");
		string text13 = Utils.GetString("Sat");
		_monthByWeekDayItems = new ObservableCollection<ComboBoxViewModel>
		{
			new ComboBoxViewModel(text7, text7, 32.0)
			{
				Selected = true
			},
			new ComboBoxViewModel(text8, text8, 32.0),
			new ComboBoxViewModel(text9, text9, 32.0),
			new ComboBoxViewModel(text10, text10, 32.0),
			new ComboBoxViewModel(text11, text11, 32.0),
			new ComboBoxViewModel(text12, text12, 32.0),
			new ComboBoxViewModel(text13, text13, 32.0)
		};
		CustomChooseRepeatUnitMonthByWeekDayPopupGridComboBox.Init(_monthByWeekDayItems, _monthByWeekDayItems[0]);
	}

	private void TryInitYearMonthCombobox()
	{
		ObservableCollection<ComboBoxViewModel> items = YearMonthCombobox.Items;
		if (items == null || items.Count <= 0)
		{
			DateTime dateTime = DateTime.Today.AddMonths(1 - DateTime.Today.Month);
			string title = dateTime.ToString("MMM", App.Ci);
			string title2 = dateTime.AddMonths(1).ToString("MMM", App.Ci);
			string title3 = dateTime.AddMonths(2).ToString("MMM", App.Ci);
			string title4 = dateTime.AddMonths(3).ToString("MMM", App.Ci);
			string title5 = dateTime.AddMonths(4).ToString("MMM", App.Ci);
			string title6 = dateTime.AddMonths(5).ToString("MMM", App.Ci);
			string title7 = dateTime.AddMonths(6).ToString("MMM", App.Ci);
			string title8 = dateTime.AddMonths(7).ToString("MMM", App.Ci);
			string title9 = dateTime.AddMonths(8).ToString("MMM", App.Ci);
			string title10 = dateTime.AddMonths(9).ToString("MMM", App.Ci);
			string title11 = dateTime.AddMonths(10).ToString("MMM", App.Ci);
			string title12 = dateTime.AddMonths(11).ToString("MMM", App.Ci);
			ObservableCollection<ComboBoxViewModel> observableCollection = new ObservableCollection<ComboBoxViewModel>
			{
				new ComboBoxViewModel(1, title, 32.0)
				{
					Selected = true
				},
				new ComboBoxViewModel(2, title2, 32.0),
				new ComboBoxViewModel(3, title3, 32.0),
				new ComboBoxViewModel(4, title4, 32.0),
				new ComboBoxViewModel(5, title5, 32.0),
				new ComboBoxViewModel(6, title6, 32.0),
				new ComboBoxViewModel(7, title7, 32.0),
				new ComboBoxViewModel(8, title8, 32.0),
				new ComboBoxViewModel(9, title9, 32.0),
				new ComboBoxViewModel(10, title10, 32.0),
				new ComboBoxViewModel(11, title11, 32.0),
				new ComboBoxViewModel(12, title12, 32.0)
			};
			YearMonthCombobox.Init(observableCollection, observableCollection[0]);
		}
	}

	private void InitWorkdayComboBox()
	{
		_repeatUnitItems = new ObservableCollection<ComboBoxViewModel>
		{
			new ComboBoxViewModel(FirstWorkday, FirstWorkday, 32.0)
			{
				Selected = true
			},
			new ComboBoxViewModel(LastWorkday, LastWorkday, 32.0)
		};
		WorkdayCombox.Init(_repeatUnitItems, _repeatUnitItems[0]);
	}

	private void InitRepeatUnitComboBox(bool countdownMode)
	{
		_countdownMode = countdownMode;
		if (_countdownMode)
		{
			_repeatUnitItems = new ObservableCollection<ComboBoxViewModel>
			{
				new ComboBoxViewModel(Day, Day, 32.0),
				new ComboBoxViewModel(Week, Week, 32.0),
				new ComboBoxViewModel(Month, Month, 32.0),
				new ComboBoxViewModel(Year, Year, 32.0)
				{
					Selected = true
				}
			};
			ChooseRepeatUnitComboBox.Init(_repeatUnitItems, _repeatUnitItems[3]);
		}
		else
		{
			_repeatUnitItems = new ObservableCollection<ComboBoxViewModel>
			{
				new ComboBoxViewModel(Day, Day, 32.0),
				new ComboBoxViewModel(Week, Week, 32.0)
				{
					Selected = true
				},
				new ComboBoxViewModel(Month, Month, 32.0),
				new ComboBoxViewModel(Year, Year, 32.0)
			};
			ChooseRepeatUnitComboBox.Init(_repeatUnitItems, _repeatUnitItems[1]);
		}
	}

	private void InitRepeatModeComboBox()
	{
		_repeatModeItems = new ObservableCollection<ComboBoxViewModel>
		{
			new ComboBoxViewModel(RepeatByDue, RepeatByDue, 32.0)
			{
				Selected = true
			},
			new ComboBoxViewModel(RepeatByComplete, RepeatByComplete, 32.0),
			new ComboBoxViewModel(RepeatByCustom, RepeatByCustom, 32.0)
		};
		RepeatModeComboBox.Init(_repeatModeItems, _repeatModeItems[0]);
	}

	public void Init(Popup popup, RepeatFromType repeatFrom, string repeatFlag, bool calendarMode, DateTime? startTime, bool countdownMode = false, bool lunarMode = false)
	{
		_popup = popup;
		_countdownMode = countdownMode;
		_lunarMode = lunarMode;
		_repeatFrom = repeatFrom;
		if (_repeatFrom == RepeatFromType.Custom)
		{
			_flagCustom = repeatFlag;
		}
		else
		{
			_repeatFlag = repeatFlag;
			_flagCustom = string.Empty;
		}
		_pattern = RecurrenceModel.GetRecurrenceModel(repeatFlag);
		_calendarMode = calendarMode;
		_startTime = startTime ?? DateTime.Now;
		_startTime = _startTime.Date;
		_maxIntervalLimit = (countdownMode ? 999 : 365);
		MonthDaySelector.SelectedDays = new List<int> { DateTime.Today.Day };
		MonthDaySelector.SetMonth(-1);
		WeekDaySelector.SelectedDays = new List<DayOfWeek> { DateTime.Today.DayOfWeek };
		WeekDaySelector.NotifySelectedChanged();
		_showedDays.Clear();
		_customDateList = GetCustomRepeatDateList();
		WorkdayCombox.SetSelected(FirstWorkday);
		CustomChooseRepeatUnitMonthByWeekNumPopupGridComboBox.SetSelected(_monthByWeekNumItems[0].Value);
		CustomChooseRepeatUnitMonthByWeekDayPopupGridComboBox.SetSelected(_monthByWeekDayItems[0].Value);
		SkipHolidayCheckbox.IsChecked = false;
		SkipWeekendCheckbox.IsChecked = false;
		if (countdownMode)
		{
			if (!string.IsNullOrEmpty(repeatFlag) && !repeatFlag.Contains("NONE"))
			{
				if (repeatFlag.Contains("DAILY"))
				{
					ChooseRepeatUnitComboBox.SetSelected(Day);
				}
				if (repeatFlag.Contains("WEEKLY"))
				{
					ChooseRepeatUnitComboBox.SetSelected(Week);
				}
				if (repeatFlag.Contains("MONTHLY"))
				{
					ChooseRepeatUnitComboBox.SetSelected(Month);
				}
				if (repeatFlag.Contains("YEARLY"))
				{
					ChooseRepeatUnitComboBox.SetSelected(Year);
				}
				int result;
				int num = ((!int.TryParse(Regex.Match(repeatFlag, "INTERVAL=(\\d+)").Groups[1]?.Value, out result)) ? 1 : result);
				CustomChooseRepeatUnitTimePopupTextBox.Text = num.ToString();
				UpdateCountdownRepeatText();
			}
			else
			{
				ChooseRepeatUnitComboBox.SetSelected(Year);
				UpdateCountdownRepeatText();
			}
		}
		InitView();
		if (calendarMode || countdownMode)
		{
			if (calendarMode)
			{
				SwitchGroupTitles.Titles = "EachByDate|EachByWeek";
			}
			if (calendarMode || (countdownMode && _lunarMode))
			{
				SkipHolidayGrid.Visibility = Visibility.Collapsed;
				SkipWeekendGrid.Visibility = Visibility.Collapsed;
			}
			RepeatByDateText.Visibility = Visibility.Visible;
			RepeatModeComboBox.Visibility = Visibility.Collapsed;
			if (countdownMode)
			{
				RepeatByDateText.Text = Utils.GetString("CustomRepeat");
				if (!UserDao.IsPro())
				{
					ProIcon.Visibility = Visibility.Visible;
				}
				if (_lunarMode)
				{
					WeekDaySelector.Visibility = Visibility.Collapsed;
					CountdownRepeatDesc.Visibility = Visibility.Visible;
				}
			}
		}
		else
		{
			RepeatByDateText.Visibility = Visibility.Collapsed;
			RepeatModeComboBox.Visibility = Visibility.Visible;
		}
	}

	private void InitView()
	{
		CustomDaySelector.Visibility = Visibility.Collapsed;
		SkipHolidayGrid.Visibility = Visibility.Visible;
		SkipWeekendGrid.Visibility = Visibility.Visible;
		CustomChooseRepeatUnitGrid.Visibility = Visibility.Visible;
		ChooseRepeatUnitComboBox.Visibility = Visibility.Visible;
		if (!string.IsNullOrEmpty(_repeatFlag))
		{
			if (_repeatFlag.Contains("HOLIDAY"))
			{
				SkipHolidayCheckbox.IsChecked = true;
			}
			if (_repeatFlag.Contains("WEEKEND"))
			{
				SkipWeekendCheckbox.IsChecked = true;
			}
		}
		if (_pattern != null)
		{
			if (_pattern.Interval > 0)
			{
				CustomChooseRepeatUnitTimePopupTextBox.Text = _pattern.Interval.ToString();
			}
			switch (_pattern.Frequency)
			{
			case FrequencyType.Daily:
				SetRepeatUnitView(Day);
				ChooseRepeatUnitComboBox.SetSelected(Day);
				break;
			case FrequencyType.Weekly:
				SetRepeatUnitView(Week);
				ChooseRepeatUnitComboBox.SetSelected(Week);
				if (_pattern.ByDay == null || _pattern.ByDay.Count <= 0)
				{
					break;
				}
				WeekDaySelector.SelectedDays.Clear();
				foreach (WeekDay item in _pattern.ByDay)
				{
					WeekDaySelector.SelectedDays.Add(item.DayOfWeek);
				}
				WeekDaySelector.NotifySelectedChanged();
				break;
			case FrequencyType.Monthly:
				SetRepeatUnitView(Month);
				ChooseRepeatUnitComboBox.SetSelected(Month);
				SwitchGroupTitles.Titles = (_calendarMode ? "EachByDate|EachByWeek" : "EachByDate|EachByWeek|Workday");
				if (_pattern.ByMonthDay != null && _pattern.ByMonthDay.Count > 0)
				{
					SwitchGroupTitles.SetSelectedIndex(0);
					OnSelectDayClick(null, null);
					MonthDaySelector.Visibility = Visibility.Visible;
					MonthDaySelector.SelectedDays.Clear();
					foreach (int item2 in _pattern.ByMonthDay)
					{
						MonthDaySelector.SelectedDays.Add(item2);
					}
					MonthDaySelector.SetMonth(-1);
				}
				else if (_pattern.ByDay != null && _pattern.ByDay.Count == 1)
				{
					SwitchGroupTitles.SetSelectedIndex(1);
					OnSelectWeekClick(null, null);
					int offset = _pattern.ByDay[0].Offset;
					offset = ((offset <= 0) ? 5 : (offset - 1));
					MonthDaySelector.Visibility = Visibility.Collapsed;
					CustomChooseRepeatUnitMonthByWeekPopupGridGrid.Visibility = Visibility.Visible;
					CustomChooseRepeatUnitMonthByWeekNumPopupGridComboBox.SetSelected(_monthByWeekNumItems[offset].Value);
					CustomChooseRepeatUnitMonthByWeekDayPopupGridComboBox.SetSelected(_monthByWeekDayItems[(int)_pattern.ByDay[0].DayOfWeek].Value);
				}
				break;
			case FrequencyType.Yearly:
			{
				SetRepeatUnitView(Year);
				SwitchGroupTitles.Titles = "EachByDate|EachByWeek";
				ChooseRepeatUnitComboBox.SetSelected(Year);
				List<int> byMonth = _pattern.ByMonth;
				int num = Math.Max(1, Math.Min(12, (byMonth == null || byMonth.Count <= 0) ? 1 : _pattern.ByMonth.FirstOrDefault()));
				if (YearMonthCombobox != null)
				{
					YearMonthCombobox.SetSelected(num);
				}
				if (_pattern.ByMonthDay != null && _pattern.ByMonthDay.Count > 0)
				{
					SwitchGroupTitles.SetSelectedIndex(0);
					OnSelectDayClick(null, null);
					MonthDaySelector.Visibility = Visibility.Visible;
					CustomChooseRepeatUnitMonthByWeekPopupGridGrid.Visibility = Visibility.Collapsed;
					MonthDaySelector.SelectedDays.Clear();
					MonthDaySelector.SelectedDays.Add(_pattern.ByMonthDay[0]);
					YearMonthGrid.Visibility = Visibility.Collapsed;
					MonthDaySelector.SetMonth(num, batch: false);
				}
				else
				{
					SwitchGroupTitles.SetSelectedIndex(1);
					OnSelectWeekClick(null, null);
					YearMonthGrid.Visibility = Visibility.Visible;
					List<WeekDay> byDay = _pattern.ByDay;
					int num2 = ((byDay == null || byDay.Count <= 0) ? 1 : _pattern.ByDay[0].Offset);
					num2 = ((num2 <= 0) ? 5 : (num2 - 1));
					CustomChooseRepeatUnitMonthByWeekNumPopupGridComboBox.SetSelected(_monthByWeekNumItems[num2].Value);
					CustomComboBox customChooseRepeatUnitMonthByWeekDayPopupGridComboBox = CustomChooseRepeatUnitMonthByWeekDayPopupGridComboBox;
					ObservableCollection<ComboBoxViewModel> monthByWeekDayItems = _monthByWeekDayItems;
					List<WeekDay> byDay2 = _pattern.ByDay;
					customChooseRepeatUnitMonthByWeekDayPopupGridComboBox.SetSelected(monthByWeekDayItems[(int)((byDay2 != null && byDay2.Count > 0) ? _pattern.ByDay[0].DayOfWeek : DayOfWeek.Sunday)].Value);
				}
				break;
			}
			default:
				if (_countdownMode)
				{
					SetRepeatUnitView(Year);
					ChooseRepeatUnitComboBox.SetSelected(Year);
				}
				else
				{
					SetRepeatUnitView(Week);
					ChooseRepeatUnitComboBox.SetSelected(Week);
					OnSelectWeekClick(null, null);
				}
				break;
			}
		}
		if (RepeatUtils.IsMonthlyWorkday(_repeatFlag))
		{
			SwitchGroupTitles.SetSelectedIndex(2);
			WorkdayCombox.SetSelected(_repeatFlag.Contains("TT_WORKDAY=-1") ? LastWorkday : FirstWorkday);
			WorkdayGrid.Visibility = Visibility.Visible;
			MonthDaySelector.Visibility = Visibility.Collapsed;
			CustomChooseRepeatUnitMonthByWeekPopupGridGrid.Visibility = Visibility.Collapsed;
			SkipWeekendGrid.Visibility = Visibility.Collapsed;
			SkipHolidayGrid.Visibility = Visibility.Collapsed;
		}
		switch (_repeatFrom)
		{
		case RepeatFromType.Duedate:
			CustomChooseRepeatUnitPopupGrid.Visibility = Visibility.Visible;
			RepeatModeComboBox.SetSelected(RepeatByDue);
			break;
		case RepeatFromType.CompleteTime:
			CustomChooseRepeatUnitPopupGrid.Visibility = Visibility.Collapsed;
			RepeatModeComboBox.SetSelected(RepeatByComplete);
			break;
		case RepeatFromType.Custom:
			CustomChooseRepeatUnitPopupGrid.Visibility = Visibility.Collapsed;
			ChooseRepeatUnitComboBox.Visibility = Visibility.Collapsed;
			CustomChooseRepeatUnitGrid.Visibility = Visibility.Collapsed;
			SkipHolidayGrid.Visibility = Visibility.Collapsed;
			SkipWeekendGrid.Visibility = Visibility.Collapsed;
			CustomDaySelector.Visibility = Visibility.Visible;
			InitCustomData();
			RepeatModeComboBox.SetSelected(RepeatByCustom);
			break;
		}
		if (_calendarMode)
		{
			SkipHolidayGrid.Visibility = Visibility.Collapsed;
			SkipWeekendGrid.Visibility = Visibility.Collapsed;
		}
	}

	private void UserControlLoaded(object sender, RoutedEventArgs e)
	{
		InitView();
		CustomDaySelector.StartDateChanged += delegate
		{
			UpdateCustomData();
		};
	}

	private void InitCustomData()
	{
		if (string.IsNullOrEmpty(_flagCustom))
		{
			CustomDaySelector.SetSelectedDays(new List<DateTime>());
		}
		TickDatePicker customDaySelector = CustomDaySelector;
		DateTime? minDate = _startTime;
		customDaySelector.SetData(null, null, null, isStart: true, isMonthMode: false, null, minDate);
		UpdateCustomData();
		_customDateList = GetCustomRepeatDateList();
	}

	private List<DateTime> GetCustomRepeatDateList()
	{
		List<DateTime> list = new List<DateTime>();
		if (!string.IsNullOrEmpty(_flagCustom))
		{
			string[] array = _flagCustom.Split(';');
			foreach (string text in array)
			{
				if (text.StartsWith("BYDATE="))
				{
					string[] array2 = text.Substring(7).Split(',');
					foreach (string s in array2)
					{
						list.Add(DateTime.ParseExact(s, "yyyyMMdd", App.Ci));
					}
					return list;
				}
			}
		}
		return list;
	}

	private void UpdateCustomData()
	{
		List<DateTime> selectedDays = CustomDaySelector.GetSelectedDays();
		foreach (DateTime customDate in _customDateList)
		{
			if (!_showedDays.ContainsKey(customDate.Date))
			{
				selectedDays.Add(customDate.Date);
			}
			_showedDays[customDate] = true;
		}
		CustomDaySelector.SetSelectedDays(selectedDays);
	}

	private void OnSaveClick(object sender, RoutedEventArgs e)
	{
		_popup.IsOpen = false;
		if (_countdownMode && !ProChecker.CheckPro(ProType.CountdownCustomRepeat))
		{
			return;
		}
		RecurrencePattern recurrencePattern = new RecurrencePattern
		{
			Interval = int.Parse(CustomChooseRepeatUnitTimePopupTextBox.Text)
		};
		if (_repeatFrom == RepeatFromType.Custom)
		{
			List<DateTime> selectedDays = CustomDaySelector.GetSelectedDays();
			if (!selectedDays.Any())
			{
				this.OnCustomRepeatSet?.Invoke("2", string.Empty);
				return;
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("ERULE:NAME=CUSTOM;BYDATE=");
			selectedDays.Sort();
			foreach (DateTime item2 in selectedDays)
			{
				stringBuilder.Append(item2.ToString("yyyyMMdd"));
				stringBuilder.Append(",");
			}
			stringBuilder.Remove(stringBuilder.Length - 1, 1);
			this.OnCustomRepeatSet?.Invoke(_realRepeatFrom, stringBuilder.ToString());
			return;
		}
		if (ChooseRepeatUnitComboBox.SelectedItem.Value.Equals(Week))
		{
			recurrencePattern.Frequency = FrequencyType.Weekly;
			if (WeekDaySelector.SelectedDays.Count != 0 && _repeatFrom != RepeatFromType.CompleteTime)
			{
				List<WeekDay> list = new List<WeekDay>();
				foreach (DayOfWeek selectedDay in WeekDaySelector.SelectedDays)
				{
					list.Add(new WeekDay(selectedDay));
				}
				recurrencePattern.ByDay = list;
			}
		}
		else if (ChooseRepeatUnitComboBox.SelectedItem.Value.Equals(Day))
		{
			recurrencePattern.Frequency = FrequencyType.Daily;
		}
		else if (ChooseRepeatUnitComboBox.SelectedItem.Value.Equals(Month))
		{
			recurrencePattern.Frequency = FrequencyType.Monthly;
			if (MonthDaySelector.Visibility == Visibility.Visible || (_countdownMode && _lunarMode))
			{
				if (MonthDaySelector.SelectedDays.Count != 0 && _repeatFrom != RepeatFromType.CompleteTime)
				{
					List<int> byMonthDay = new List<int>(MonthDaySelector.SelectedDays);
					recurrencePattern.ByMonthDay = byMonthDay;
				}
			}
			else if (CustomChooseRepeatUnitMonthByWeekPopupGridGrid.Visibility == Visibility.Visible && _repeatFrom != RepeatFromType.CompleteTime)
			{
				List<WeekDay> weekDayList = GetWeekDayList();
				recurrencePattern.ByDay = weekDayList;
			}
			else if (WorkdayGrid.Visibility == Visibility.Visible)
			{
				string repeatFlag = "RRULE:FREQ=MONTHLY;INTERVAL=" + int.Parse(CustomChooseRepeatUnitTimePopupTextBox.Text) + ";BYMONTHDAY=1;TT_WORKDAY=1";
				if (WorkdayCombox.SelectedItem.Value.Equals(LastWorkday))
				{
					repeatFlag = "RRULE:FREQ=MONTHLY;INTERVAL=" + int.Parse(CustomChooseRepeatUnitTimePopupTextBox.Text) + ";BYMONTHDAY=1;TT_WORKDAY=-1";
				}
				this.OnCustomRepeatSet?.Invoke(_realRepeatFrom, repeatFlag);
				return;
			}
		}
		else if (ChooseRepeatUnitComboBox.SelectedItem.Value.Equals(Year))
		{
			recurrencePattern.Frequency = FrequencyType.Yearly;
			if (_repeatFrom != RepeatFromType.CompleteTime)
			{
				int num = 1;
				if (SwitchGroupTitles.GetSelectedIndex() == 1 && (!_countdownMode || !_lunarMode))
				{
					num = YearMonthCombobox.SelectedIndex + 1;
					List<WeekDay> weekDayList2 = GetWeekDayList();
					recurrencePattern.ByDay = weekDayList2;
				}
				else
				{
					int item = ((MonthDaySelector.SelectedDays.Count > 0) ? MonthDaySelector.SelectedDays[0] : _startTime.Day);
					num = ((MonthDaySelector.CurrentMonth <= 0) ? 1 : MonthDaySelector.CurrentMonth);
					recurrencePattern.ByMonthDay = new List<int> { item };
				}
				recurrencePattern.ByMonth = new List<int> { num };
			}
		}
		this.OnCustomRepeatSet?.Invoke(_realRepeatFrom, GetRepeatFlag(recurrencePattern));
	}

	private List<WeekDay> GetWeekDayList()
	{
		List<WeekDay> list = new List<WeekDay>();
		int num = 0;
		switch (_monthByWeekNumItems.IndexOf(CustomChooseRepeatUnitMonthByWeekNumPopupGridComboBox.SelectedItem))
		{
		case 0:
			num = 1;
			break;
		case 1:
			num = 2;
			break;
		case 2:
			num = 3;
			break;
		case 3:
			num = 4;
			break;
		case 4:
			num = 5;
			break;
		case 5:
			num = -1;
			break;
		}
		switch (_monthByWeekDayItems.IndexOf(CustomChooseRepeatUnitMonthByWeekDayPopupGridComboBox.SelectedItem))
		{
		case 0:
			list.Add(new WeekDay(DayOfWeek.Sunday, num));
			break;
		case 1:
			list.Add(new WeekDay(DayOfWeek.Monday, num));
			break;
		case 2:
			list.Add(new WeekDay(DayOfWeek.Tuesday, num));
			break;
		case 3:
			list.Add(new WeekDay(DayOfWeek.Wednesday, num));
			break;
		case 4:
			list.Add(new WeekDay(DayOfWeek.Thursday, num));
			break;
		case 5:
			list.Add(new WeekDay(DayOfWeek.Friday, num));
			break;
		case 6:
			list.Add(new WeekDay(DayOfWeek.Saturday, num));
			break;
		}
		return list;
	}

	private string GetRepeatFlag(RecurrencePattern pattern)
	{
		string text = RecurrenceModel.Normalize(pattern);
		bool num = SkipHolidayGrid.Visibility == Visibility.Visible && SkipHolidayCheckbox.IsChecked.HasValue && SkipHolidayCheckbox.IsChecked.Value;
		bool flag = SkipWeekendGrid.Visibility == Visibility.Visible && SkipWeekendCheckbox.IsChecked.HasValue && SkipWeekendCheckbox.IsChecked.Value;
		string text2 = string.Empty;
		if (num && !flag)
		{
			text2 = ";TT_SKIP=HOLIDAY";
		}
		if (!num && flag)
		{
			text2 = ";TT_SKIP=WEEKEND";
		}
		if (num && flag)
		{
			text2 = ";TT_SKIP=HOLIDAY,WEEKEND";
		}
		return "RRULE:" + text + text2;
	}

	private void OnSelectWeekClick(object sender, MouseButtonEventArgs e)
	{
		MonthDaySelector.Visibility = Visibility.Collapsed;
		CustomChooseRepeatUnitMonthByWeekPopupGridGrid.Visibility = Visibility.Visible;
		WorkdayGrid.Visibility = Visibility.Collapsed;
		SkipWeekendGrid.Visibility = Visibility.Collapsed;
		SkipHolidayGrid.Visibility = Visibility.Visible;
		SetWeekNum(_startTime);
		if (_calendarMode)
		{
			SkipHolidayGrid.Visibility = Visibility.Collapsed;
			SkipWeekendGrid.Visibility = Visibility.Collapsed;
		}
	}

	private void OnSelectDayClick(object sender, MouseButtonEventArgs e)
	{
		bool flag = ChooseRepeatUnitComboBox.SelectedItem.Value == Year;
		MonthDaySelector.Visibility = Visibility.Visible;
		CustomChooseRepeatUnitMonthByWeekPopupGridGrid.Visibility = Visibility.Collapsed;
		WorkdayGrid.Visibility = Visibility.Collapsed;
		SkipWeekendGrid.Visibility = (flag ? Visibility.Collapsed : Visibility.Visible);
		SkipHolidayGrid.Visibility = (flag ? Visibility.Collapsed : Visibility.Visible);
		if (_calendarMode)
		{
			SkipHolidayGrid.Visibility = Visibility.Collapsed;
			SkipWeekendGrid.Visibility = Visibility.Collapsed;
		}
	}

	private void OnSelectWorkdayClick(object sender, MouseButtonEventArgs e)
	{
		WorkdayGrid.Visibility = Visibility.Visible;
		MonthDaySelector.Visibility = Visibility.Collapsed;
		CustomChooseRepeatUnitMonthByWeekPopupGridGrid.Visibility = Visibility.Collapsed;
		SkipWeekendGrid.Visibility = Visibility.Collapsed;
		SkipHolidayGrid.Visibility = Visibility.Collapsed;
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		_popup.IsOpen = false;
	}

	private void RepeatUnitComboBoxSelected(object sender, ComboBoxViewModel e)
	{
		if (e != null && WeekDaySelector != null)
		{
			SetRepeatUnitView(e.Value);
		}
	}

	private void UpdateCountdownRepeatText()
	{
		string text = ((CustomChooseRepeatUnitTimePopupTextBox != null) ? CustomChooseRepeatUnitTimePopupTextBox.Text : "");
		object obj = ((ChooseRepeatUnitComboBox?.SelectedItem != null) ? ChooseRepeatUnitComboBox.SelectedItem.Value : "");
		string key = "repeat_every_d_days";
		string key2 = "repeat_every_1_day";
		if (obj.Equals(Day))
		{
			key = "repeat_every_d_days";
			key2 = "repeat_every_1_day";
		}
		else if (obj.Equals(Week))
		{
			key = "repeat_every_d_weeks";
			key2 = "repeat_every_1_week";
		}
		else if (obj.Equals(Month))
		{
			key = "repeat_every_d_months";
			key2 = "repeat_every_1_month";
		}
		else if (obj.Equals(Year))
		{
			key = "repeat_every_d_years";
			key2 = "repeat_every_1_year";
		}
		string text2 = string.Format(Utils.GetString(key), text);
		if (text == "1")
		{
			text2 = Utils.GetString(key2);
		}
		if (CountdownRepeatDesc != null)
		{
			CountdownRepeatDesc.Text = text2;
		}
	}

	private void SetRepeatUnitView(object val)
	{
		if (_countdownMode && _lunarMode)
		{
			UpdateCountdownRepeatText();
			return;
		}
		if (val.Equals(Day))
		{
			WeekDaySelector.Visibility = Visibility.Collapsed;
			CustomChooseRepeatUnitMonthPopupGrid.Visibility = Visibility.Collapsed;
			SkipWeekendGrid.Visibility = Visibility.Visible;
			SkipHolidayGrid.Visibility = Visibility.Visible;
		}
		else if (val.Equals(Week))
		{
			WeekDaySelector.Visibility = Visibility.Visible;
			CustomChooseRepeatUnitMonthPopupGrid.Visibility = Visibility.Collapsed;
			SkipWeekendGrid.Visibility = Visibility.Collapsed;
			SkipHolidayGrid.Visibility = Visibility.Visible;
		}
		else if (val.Equals(Month))
		{
			WeekDaySelector.Visibility = Visibility.Collapsed;
			CustomChooseRepeatUnitMonthPopupGrid.Visibility = Visibility.Visible;
			SkipWeekendGrid.Visibility = MonthDaySelector.Visibility;
			CustomChooseRepeatUnitMonthByWeekPopupGridGrid.Visibility = Visibility.Collapsed;
			SkipHolidayGrid.Visibility = ((WorkdayGrid.Visibility == Visibility.Visible) ? Visibility.Collapsed : Visibility.Visible);
			YearMonthGrid.Visibility = Visibility.Collapsed;
			SwitchGroupTitles.Visibility = Visibility.Visible;
			SwitchGroupTitles.Titles = (_calendarMode ? "EachByDate|EachByWeek" : "EachByDate|EachByWeek|Workday");
			switch (SwitchGroupTitles.GetSelectedIndex())
			{
			case 1:
				OnSelectWeekClick(null, null);
				break;
			case 2:
				OnSelectWorkdayClick(null, null);
				break;
			default:
				OnSelectDayClick(null, null);
				MonthDaySelector.SetMonth(-1);
				break;
			}
		}
		else if (val.Equals(Year))
		{
			TryInitYearMonthCombobox();
			WeekDaySelector.Visibility = Visibility.Collapsed;
			SwitchGroupTitles.Titles = "EachByDate|EachByWeek";
			CustomChooseRepeatUnitMonthPopupGrid.Visibility = Visibility.Visible;
			SwitchGroupTitles.Visibility = Visibility.Visible;
			MonthDaySelector.Visibility = Visibility.Collapsed;
			WorkdayGrid.Visibility = Visibility.Collapsed;
			YearMonthCombobox.SetSelected(_startTime.Month);
			if (SwitchGroupTitles.GetSelectedIndex() == 1)
			{
				OnSelectWeekClick(null, null);
				YearMonthGrid.Visibility = Visibility.Visible;
			}
			else
			{
				OnSelectDayClick(null, null);
				MonthDaySelector.SelectedDays = new List<int> { _startTime.Day };
				YearMonthGrid.Visibility = Visibility.Collapsed;
				MonthDaySelector.SetMonth(_startTime.Month, batch: false);
			}
		}
		if (_calendarMode)
		{
			SkipHolidayGrid.Visibility = Visibility.Collapsed;
			SkipWeekendGrid.Visibility = Visibility.Collapsed;
		}
	}

	private void SetWeekNum(DateTime date)
	{
		int i;
		for (i = 1; date.AddDays(i * -7).Month == date.Month; i++)
		{
		}
		CustomChooseRepeatUnitMonthByWeekNumPopupGridComboBox.SetSelected(i - 1);
		CustomChooseRepeatUnitMonthByWeekDayPopupGridComboBox.SetSelected(_monthByWeekDayItems[(int)date.DayOfWeek].Value);
	}

	private void customChooseRepeatUnitTimePopupTextBox_LostFocus(object sender, RoutedEventArgs e)
	{
		if (CustomChooseRepeatUnitTimePopupTextBox == null)
		{
			return;
		}
		try
		{
			if (int.Parse(CustomChooseRepeatUnitTimePopupTextBox.Text) == 0)
			{
				CustomChooseRepeatUnitTimePopupTextBox.Text = "1";
			}
		}
		catch (FormatException)
		{
			CustomChooseRepeatUnitTimePopupTextBox.Text = "1";
		}
	}

	private void RepeatModeSelected(object sender, ComboBoxViewModel model)
	{
		if (RepeatModeComboBox.SelectedItem != null && CustomChooseRepeatUnitPopupGrid != null)
		{
			if (model.Value.ToString() == Utils.GetString("RepeatByDue"))
			{
				_repeatFrom = RepeatFromType.Duedate;
			}
			else if (model.Value.ToString() == Utils.GetString("RepeatByComplete"))
			{
				_repeatFrom = RepeatFromType.CompleteTime;
			}
			else if (model.Value.ToString() == Utils.GetString("RepeatByCustom"))
			{
				_repeatFrom = RepeatFromType.Custom;
			}
			InitView();
		}
	}

	private void OnRepeatIntervalTextChanged(object sender, TextChangedEventArgs e)
	{
		if (!int.TryParse(CustomChooseRepeatUnitTimePopupTextBox.Text, out var result))
		{
			CustomChooseRepeatUnitTimePopupTextBox.Text = "1";
			CustomChooseRepeatUnitTimePopupTextBox.SelectAll();
		}
		else if (result > _maxIntervalLimit)
		{
			CustomChooseRepeatUnitTimePopupTextBox.Text = _maxIntervalLimit.ToString();
			CustomChooseRepeatUnitTimePopupTextBox.SelectAll();
		}
		UpdateCountdownRepeatText();
	}

	private void OnGroupTitleSelectedTitleChanged(object sender, GroupTitleViewModel e)
	{
		if (ChooseRepeatUnitComboBox.SelectedItem.Value == Year)
		{
			YearMonthGrid.Visibility = ((e.Index != 1) ? Visibility.Collapsed : Visibility.Visible);
		}
		if (e.Index == 1)
		{
			OnSelectWeekClick(null, null);
		}
		else if (e.Index == 2)
		{
			OnSelectWorkdayClick(null, null);
		}
		else
		{
			OnSelectDayClick(null, null);
		}
	}

	public bool HandleTab(bool shift)
	{
		_tabIndex += 14 + ((!shift) ? 1 : (-1));
		_tabIndex %= 14;
		HandleTabIndex(shift);
		return true;
	}

	public bool HandleEnter()
	{
		if (RepeatModeComboBox.TabSelected)
		{
			RepeatModeComboBox.HandleEnter();
		}
		if (ChooseRepeatUnitComboBox.TabSelected)
		{
			ChooseRepeatUnitComboBox.HandleEnter();
		}
		if (CustomChooseRepeatUnitMonthByWeekNumPopupGridComboBox.TabSelected)
		{
			CustomChooseRepeatUnitMonthByWeekNumPopupGridComboBox.HandleEnter();
		}
		if (CustomChooseRepeatUnitMonthByWeekDayPopupGridComboBox.TabSelected)
		{
			CustomChooseRepeatUnitMonthByWeekDayPopupGridComboBox.HandleEnter();
		}
		if (CustomDaySelector.Visibility == Visibility.Visible && _tabIndex == 1)
		{
			CustomDaySelector.SelectTabItem();
		}
		if (WorkdayCombox.TabSelected)
		{
			WorkdayCombox.HandleEnter();
		}
		if (SwitchGroupTitles.InTab())
		{
			SwitchGroupTitles.SelectTabItem();
		}
		WeekDaySelector.HandleEnter();
		MonthDaySelector.HandleEnter();
		switch (_tabIndex)
		{
		case 10:
			SkipHolidayCheckbox.IsChecked = SkipHolidayCheckbox.IsChecked != true;
			break;
		case 11:
			SkipWeekendCheckbox.IsChecked = SkipWeekendCheckbox.IsChecked != true;
			break;
		case 12:
			OnSaveClick(this, null);
			break;
		case 13:
			_popup.IsOpen = false;
			break;
		}
		return true;
	}

	public bool HandleEsc()
	{
		if (RepeatModeComboBox.IsOpen)
		{
			RepeatModeComboBox.Close();
			return true;
		}
		if (ChooseRepeatUnitComboBox.IsOpen)
		{
			ChooseRepeatUnitComboBox.Close();
			return true;
		}
		if (CustomChooseRepeatUnitMonthByWeekNumPopupGridComboBox.IsOpen)
		{
			CustomChooseRepeatUnitMonthByWeekNumPopupGridComboBox.Close();
			return true;
		}
		if (CustomChooseRepeatUnitMonthByWeekDayPopupGridComboBox.IsOpen)
		{
			CustomChooseRepeatUnitMonthByWeekDayPopupGridComboBox.Close();
			return true;
		}
		if (WorkdayCombox.IsOpen)
		{
			WorkdayCombox.Close();
			return true;
		}
		return false;
	}

	public bool UpDownSelect(bool isUp)
	{
		if (RepeatModeComboBox.IsOpen)
		{
			RepeatModeComboBox.UpDownSelect(isUp);
		}
		if (ChooseRepeatUnitComboBox.IsOpen)
		{
			ChooseRepeatUnitComboBox.UpDownSelect(isUp);
		}
		if (CustomChooseRepeatUnitMonthByWeekNumPopupGridComboBox.IsOpen)
		{
			CustomChooseRepeatUnitMonthByWeekNumPopupGridComboBox.UpDownSelect(isUp);
		}
		if (CustomChooseRepeatUnitMonthByWeekDayPopupGridComboBox.IsOpen)
		{
			CustomChooseRepeatUnitMonthByWeekDayPopupGridComboBox.UpDownSelect(isUp);
		}
		if (WorkdayCombox.IsOpen)
		{
			WorkdayCombox.UpDownSelect(isUp);
		}
		if (ChooseRepeatUnitComboBox.SelectedItem.Value.Equals(Month) && _tabIndex == 6 && MonthDaySelector.Visibility == Visibility.Visible)
		{
			MonthDaySelector.SetTabSelected(tab: true, isUp ? (-7) : 7);
		}
		if (CustomDaySelector.Visibility == Visibility.Visible && _tabIndex == 1)
		{
			CustomDaySelector.MoveTabSelectDate(isUp ? (-7) : 7);
		}
		return true;
	}

	public bool LeftRightSelect(bool isLeft)
	{
		if (ChooseRepeatUnitComboBox.SelectedItem.Value.Equals(Month) && _tabIndex == 6 && MonthDaySelector.Visibility == Visibility.Visible)
		{
			MonthDaySelector.SetTabSelected(tab: true, (!isLeft) ? 1 : (-1));
			return true;
		}
		if (CustomDaySelector.Visibility == Visibility.Visible && _tabIndex == 1)
		{
			CustomDaySelector.MoveTabSelectDate((!isLeft) ? 1 : (-1));
			return true;
		}
		return false;
	}

	private void HandleTabIndex(bool shift)
	{
		RepeatModeComboBox.TabSelected = _tabIndex == 0;
		if (ChooseRepeatUnitComboBox.Visibility == Visibility.Visible)
		{
			if (_tabIndex == 1)
			{
				CustomChooseRepeatUnitTimePopupTextBox.Focus();
			}
			else
			{
				EmptyBox.Focus();
			}
			ChooseRepeatUnitComboBox.TabSelected = _tabIndex == 2;
			if (ChooseRepeatUnitComboBox.SelectedItem.Value.Equals(Day))
			{
				SetDayTabStatus(shift);
			}
			else if (ChooseRepeatUnitComboBox.SelectedItem.Value.Equals(Week))
			{
				SetWeekTabStatus(shift);
			}
			else if (ChooseRepeatUnitComboBox.SelectedItem.Value.Equals(Month))
			{
				SetMonthTabStatus(shift);
			}
		}
		else
		{
			if (_tabIndex >= 2 && _tabIndex <= 11)
			{
				_tabIndex = (shift ? 1 : 12);
			}
			if (_tabIndex == 1)
			{
				CustomDaySelector.TabSelectCurrent();
			}
			else
			{
				CustomDaySelector.ClearTabSelected();
			}
			UiUtils.SetSaveButtonTabSelected(SaveButton, _tabIndex == 12);
			UiUtils.SetCancelButtonTabSelected(CancelButton, _tabIndex == 13);
		}
	}

	private void SetMonthTabStatus(bool shift)
	{
		if (CustomChooseRepeatUnitMonthPopupGrid.IsVisible)
		{
			SwitchGroupTitles.TabSelect(_tabIndex - 3);
			if (MonthDaySelector.Visibility == Visibility.Visible)
			{
				MonthDaySelector.SetTabSelected(_tabIndex == 6);
				if (_tabIndex >= 7 && _tabIndex <= 9)
				{
					_tabIndex = (shift ? 6 : 10);
					HandleTabIndex(shift);
				}
				else
				{
					SetBottomTabStatus(shift, 6);
				}
			}
			else if (WorkdayGrid.Visibility == Visibility.Visible)
			{
				WorkdayCombox.TabSelected = _tabIndex == 6;
				if (_tabIndex >= 7 && _tabIndex <= 9)
				{
					_tabIndex = (shift ? 6 : 10);
					HandleTabIndex(shift);
				}
				else
				{
					SetBottomTabStatus(shift, 6);
				}
			}
			else if (CustomChooseRepeatUnitMonthByWeekPopupGridGrid.Visibility == Visibility.Visible)
			{
				CustomChooseRepeatUnitMonthByWeekNumPopupGridComboBox.TabSelected = _tabIndex == 6;
				CustomChooseRepeatUnitMonthByWeekDayPopupGridComboBox.TabSelected = _tabIndex == 7;
				if (_tabIndex >= 8 && _tabIndex <= 9)
				{
					_tabIndex = (shift ? 7 : 10);
					HandleTabIndex(shift);
				}
				else
				{
					SetBottomTabStatus(shift, 7);
				}
			}
			else
			{
				SetDayTabStatus(shift);
			}
		}
		else
		{
			SetDayTabStatus(shift);
		}
	}

	private void SetWeekTabStatus(bool shift)
	{
		if (WeekDaySelector.IsVisible)
		{
			WeekDaySelector.SetTabIndex(_tabIndex - 3);
		}
		else if (_tabIndex >= 3 && _tabIndex <= 9)
		{
			_tabIndex = (shift ? 2 : 10);
			HandleTabIndex(shift);
			return;
		}
		SetBottomTabStatus(shift, WeekDaySelector.IsVisible ? 9 : 2);
	}

	private void SetDayTabStatus(bool shift)
	{
		if (_tabIndex >= 3 && _tabIndex <= 9)
		{
			_tabIndex = (shift ? 2 : 10);
			HandleTabIndex(shift);
		}
		else
		{
			SetBottomTabStatus(shift, 2);
		}
	}

	private void SetBottomTabStatus(bool shift, int start)
	{
		if (_tabIndex == 10 && SkipHolidayGrid.Visibility != Visibility.Visible)
		{
			_tabIndex = (shift ? start : 11);
			HandleTabIndex(shift);
			return;
		}
		if (_tabIndex == 11 && SkipWeekendGrid.Visibility != Visibility.Visible)
		{
			_tabIndex = (shift ? 10 : 12);
			HandleTabIndex(shift);
			return;
		}
		UiUtils.SetBorderTabSelected(SkipHolidayGrid, _tabIndex == 10);
		UiUtils.SetBorderTabSelected(SkipWeekendGrid, _tabIndex == 11);
		UiUtils.SetSaveButtonTabSelected(SaveButton, _tabIndex == 12);
		UiUtils.SetCancelButtonTabSelected(CancelButton, _tabIndex == 13);
	}

	public void SetTabEnter(bool enter)
	{
		if (enter)
		{
			_tabIndex = 0;
			RepeatModeComboBox.TabSelected = true;
		}
		else
		{
			_tabIndex = -1;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/customrepeatdialog.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	internal Delegate _CreateDelegate(Type delegateType, string handler)
	{
		return Delegate.CreateDelegate(delegateType, this, handler);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			((CustomRepeatDialog)target).Loaded += UserControlLoaded;
			break;
		case 2:
			RepeatModeComboBox = (CustomComboBox)target;
			break;
		case 3:
			RepeatByDateText = (TextBlock)target;
			break;
		case 4:
			ProIcon = (Image)target;
			break;
		case 5:
			CustomChooseRepeatUnitGrid = (Grid)target;
			break;
		case 6:
			CustomChooseRepeatUnitTimePopupTextBox = (TextBox)target;
			CustomChooseRepeatUnitTimePopupTextBox.TextChanged += OnRepeatIntervalTextChanged;
			CustomChooseRepeatUnitTimePopupTextBox.LostFocus += customChooseRepeatUnitTimePopupTextBox_LostFocus;
			break;
		case 7:
			EmptyBox = (TextBox)target;
			break;
		case 8:
			CountdownRepeatDesc = (TextBlock)target;
			break;
		case 9:
			ChooseRepeatUnitComboBox = (CustomComboBox)target;
			break;
		case 10:
			CustomChooseRepeatUnitPopupGrid = (Grid)target;
			break;
		case 11:
			WeekDaySelector = (WeekdaySelector)target;
			break;
		case 12:
			CustomChooseRepeatUnitMonthPopupGrid = (Grid)target;
			break;
		case 13:
			SwitchGroupTitles = (GroupTitle2)target;
			break;
		case 14:
			WorkdayGrid = (Grid)target;
			break;
		case 15:
			WorkdayCombox = (CustomComboBox)target;
			break;
		case 16:
			YearMonthGrid = (Border)target;
			break;
		case 17:
			YearMonthCombobox = (CustomComboBox)target;
			break;
		case 18:
			MonthDaySelector = (MonthDaySelector)target;
			break;
		case 19:
			CustomChooseRepeatUnitMonthByWeekPopupGridGrid = (Grid)target;
			break;
		case 20:
			CustomChooseRepeatUnitMonthByWeekNumPopupGridComboBox = (CustomComboBox)target;
			break;
		case 21:
			CustomChooseRepeatUnitMonthByWeekDayPopupGridComboBox = (CustomComboBox)target;
			break;
		case 22:
			CustomDaySelector = (TickDatePicker)target;
			break;
		case 23:
			SkipHolidayGrid = (Border)target;
			break;
		case 24:
			SkipHolidayCheckbox = (CheckBox)target;
			break;
		case 25:
			SkipWeekendGrid = (Border)target;
			break;
		case 26:
			SkipWeekendCheckbox = (CheckBox)target;
			break;
		case 27:
			CancelButton = (Button)target;
			CancelButton.Click += OnCancelClick;
			break;
		case 28:
			SaveButton = (Button)target;
			SaveButton.Click += OnSaveClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
