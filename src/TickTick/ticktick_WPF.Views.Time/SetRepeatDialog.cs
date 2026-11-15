using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media;
using Ical.Net;
using Ical.Net.DataTypes;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.KotlinUtils;
using ticktick_WPF.Views.CustomControl;

namespace ticktick_WPF.Views.Time;

public class SetRepeatDialog : UserControl, ITabControl, IComponentConnector
{
	private const string None = "None";

	private const string Daily = "Daily";

	private const string Weekly = "Weekly";

	private const string Monthly = "Monthly";

	private const string Yearly = "Yearly";

	private const string Lunar = "Lunar";

	private const string LunarYearly = "LunarYearly";

	private const string LunarMonthly = "LunarMonthly";

	private const string OfficialWorkingDays = "OfficialWorkingDays";

	private const string WorkingDays = "WorkingDays";

	private const string Holidays = "Holidays";

	private const string Weekend = "Weekend";

	private const string Custom = "Custom";

	private const string Ebbinghaus = "Ebbinghaus";

	private const string OfficialHolidays = "OfficalHolidays";

	private bool _calendarMode;

	private readonly bool _countdownMode;

	private readonly bool _lunarMode;

	private readonly Popup _popup;

	private RepeatExtra _repeat;

	internal ContentControl ListContent;

	internal Grid CustomContainer;

	internal CustomRepeatDialog CustomRepeatDialog;

	private bool _contentLoaded;

	private bool isNewRrule { get; set; } = true;

	public bool IsShowing
	{
		get
		{
			if (_popup != null)
			{
				return _popup.IsOpen;
			}
			return false;
		}
	}

	public event EventHandler<RepeatExtra> RepeatSelect;

	public event EventHandler PopupClosed;

	public event EventHandler PopupOpened;

	public SetRepeatDialog(Popup popup, RepeatExtra repeat, bool calenderMode = false, bool countdownMode = false, bool lunarMode = false)
	{
		InitializeComponent();
		_popup = popup;
		_repeat = repeat;
		_calendarMode = calenderMode;
		_lunarMode = lunarMode;
		_countdownMode = countdownMode;
		_popup.Child = this;
		_popup.Closed -= OnPopupClosed;
		_popup.Closed += OnPopupClosed;
		SetButtonSelected(repeat, calenderMode);
	}

	public void Reset(RepeatExtra repeatExtra, bool calender = false)
	{
		_repeat = repeatExtra;
		_calendarMode = calender;
		SetButtonSelected(repeatExtra, calender);
		ListContent.Visibility = Visibility.Visible;
		CustomContainer.Visibility = Visibility.Collapsed;
	}

	private void SetButtonSelected(RepeatExtra repeatExtra, bool calendarMode = false)
	{
		if (repeatExtra.RepeatFlag == null)
		{
			repeatExtra.RepeatFlag = string.Empty;
		}
		DateTime dateTime = (Utils.IsEmptyDate(_repeat.DefaultDate) ? DateTime.Today : _repeat.DefaultDate);
		bool flag = string.IsNullOrEmpty(repeatExtra.RepeatFlag);
		bool flag2 = repeatExtra.RepeatFrom == "2";
		if (_countdownMode && RepeatUtils.CheckIfCountdownCustom(repeatExtra.RepeatFlag, dateTime))
		{
			flag2 = false;
		}
		RecurrenceModel recurrenceModel = RecurrenceModel.GetRecurrenceModel(repeatExtra.RepeatFlag);
		bool flag3 = flag2 && recurrenceModel.IsEquals(RepeatUtils.WorkingDaysRrule);
		bool flag4 = flag2 && recurrenceModel.IsEquals(RepeatUtils.WeekendRrule);
		if (!flag3 && flag2 && !flag && !flag4)
		{
			switch (recurrenceModel.Frequency)
			{
			case FrequencyType.Daily:
				if (recurrenceModel.Interval > 1)
				{
					flag2 = false;
				}
				break;
			case FrequencyType.Weekly:
			{
				if (recurrenceModel.Interval > 1)
				{
					flag2 = false;
				}
				List<WeekDay> byDay = recurrenceModel.ByDay;
				if (byDay != null && byDay.Count > 1)
				{
					flag2 = false;
				}
				break;
			}
			case FrequencyType.Monthly:
			{
				if (recurrenceModel.Interval > 1)
				{
					flag2 = false;
				}
				List<int> byWeekNo = recurrenceModel.ByWeekNo;
				if (byWeekNo != null && byWeekNo.Count > 0)
				{
					flag2 = false;
				}
				List<WeekDay> byDay2 = recurrenceModel.ByDay;
				if (byDay2 != null && byDay2.Count > 0)
				{
					flag2 = false;
				}
				break;
			}
			case FrequencyType.Yearly:
				if (recurrenceModel.Interval > 1)
				{
					flag2 = false;
				}
				break;
			}
		}
		List<CustomMenuItemViewModel> list = new List<CustomMenuItemViewModel>();
		if (_countdownMode)
		{
			list.Add(new CustomMenuItemViewModel((object)"None", Utils.GetString("none"), (Geometry)null, "")
			{
				Selected = flag
			});
		}
		list.Add(new CustomMenuItemViewModel((object)"Daily", Utils.GetString("EveryDay"), (Geometry)null, "")
		{
			Selected = (!flag && flag2 && repeatExtra.RepeatFlag.Contains("DAILY") && !repeatExtra.RepeatFlag.Contains("WEEKEND") && !repeatExtra.RepeatFlag.Contains("OFFICIAL_WORKDAY"))
		});
		bool flag5 = !flag3 && !flag && flag2 && repeatExtra.RepeatFlag.Contains("WEEKLY") && !flag4;
		string text;
		if (recurrenceModel.ByDay.Any() && flag5)
		{
			isNewRrule = true;
			DateTime today = DateTime.Today;
			IEnumerable<string> values = recurrenceModel.ByDay.Select(delegate(WeekDay x)
			{
				int num3 = (7 + x.DayOfWeek - today.DayOfWeek) % 7;
				return today.AddDays(num3).ToString("ddd", App.Ci);
			});
			text = string.Join(Utils.IsZhCn() ? "、" : ",", values);
		}
		else
		{
			text = dateTime.ToString("ddd", App.Ci);
		}
		list.Add(new CustomMenuItemViewModel((object)"Weekly", Utils.GetString("EveryWeek"), (Geometry)null, "")
		{
			Selected = flag5,
			UIContent = GetDescContent("(" + text + ")", flag5)
		});
		if (!_lunarMode)
		{
			bool flag6 = !flag && flag2 && repeatExtra.RepeatFlag.Contains("MONTHLY") && repeatExtra.RepeatFlag.Contains("RRULE");
			string text2;
			if (recurrenceModel.ByMonthDay.Any() && flag6)
			{
				IEnumerable<string> values2 = from x in recurrenceModel.ByMonthDay
					select DateUtils.FormatDay(x) into x
					where !string.IsNullOrEmpty(x)
					select x;
				text2 = string.Join(Utils.IsZhCn() ? "、" : ",", values2);
			}
			else
			{
				text2 = DateUtils.FormatDay(dateTime);
			}
			list.Add(new CustomMenuItemViewModel((object)"Monthly", Utils.GetString("EveryMonth"), (Geometry)null, "")
			{
				Selected = flag6,
				UIContent = GetDescContent("(" + text2 + ")", flag6)
			});
			bool flag7 = !flag && (flag2 || (repeatExtra.RepeatFlag.Contains("BYMONTH") && !repeatExtra.RepeatFlag.Contains("DAY"))) && repeatExtra.RepeatFlag.Contains("YEARLY") && repeatExtra.RepeatFlag.Contains("RRULE");
			string text3;
			if (recurrenceModel.ByMonthDay.Any() && recurrenceModel.ByMonth.Any() && flag7)
			{
				isNewRrule = true;
				List<string> list2 = new List<string>();
				foreach (int item in recurrenceModel.ByMonth)
				{
					foreach (int item2 in recurrenceModel.ByMonthDay)
					{
						try
						{
							DateTime date = new DateTime(2024, item, item2);
							list2.Add(DateUtils.FormatMonthDay(date, DateFormatLen.Middle));
						}
						catch (ArgumentOutOfRangeException)
						{
						}
					}
				}
				text3 = string.Join(Utils.IsZhCn() ? "、" : ",", list2);
			}
			else
			{
				text3 = DateUtils.FormatMonthDay(dateTime, DateFormatLen.Middle);
			}
			list.Add(new CustomMenuItemViewModel((object)"Yearly", Utils.GetString("EveryYear"), (Geometry)null, "")
			{
				Selected = flag7,
				UIContent = GetDescContent("(" + text3 + ")", flag7)
			});
			goto IL_0662;
		}
		int num;
		int num2;
		if (flag2)
		{
			num = (repeatExtra.RepeatFlag.Contains("LUNAR") ? 1 : 0);
			if (num != 0)
			{
				num2 = (repeatExtra.RepeatFlag.Contains("MONTHLY") ? 1 : 0);
				goto IL_059e;
			}
		}
		else
		{
			num = 0;
		}
		num2 = 0;
		goto IL_059e;
		IL_059e:
		bool flag8 = (byte)num2 != 0;
		bool flag9 = num != 0 && !flag8;
		DateTime currentOrNextRepeatDate = RepeatCalculator.GetCurrentOrNextRepeatDate(repeatExtra.RepeatFlag, dateTime);
		list.Add(new CustomMenuItemViewModel((object)"LunarMonthly", Utils.GetString("EveryMonth"), (Geometry)null, "")
		{
			Selected = flag8,
			UIContent = GetDescContent("(" + DateUtils.GetLunarDay(flag8 ? currentOrNextRepeatDate : dateTime) + ")", flag8)
		});
		list.Add(new CustomMenuItemViewModel((object)"LunarYearly", Utils.GetString("EveryYear"), (Geometry)null, "")
		{
			Selected = flag9,
			UIContent = GetDescContent("(" + DateUtils.GetLunarMonthDay(flag9 ? currentOrNextRepeatDate : dateTime) + ")", flag9)
		});
		goto IL_0662;
		IL_0662:
		if (!calendarMode && !_countdownMode)
		{
			list.Add(new CustomMenuItemViewModel(null));
			bool flag10 = flag3 || (!flag && flag2 && repeatExtra.RepeatFlag.Contains("TT_SKIP=WEEKEND"));
			bool flag11 = !flag && flag2 && repeatExtra.RepeatFlag.Contains("TT_SKIP=HOLIDAY,WEEKEND");
			bool flag12 = !flag && flag2 && repeatExtra.RepeatFlag.Contains("TT_SKIP=OFFICIAL_WORKDAY");
			list.Add(new CustomMenuItemViewModel((object)"WorkingDays", flag10 ? Utils.GetString("EveryWeekday") : (flag11 ? Utils.GetString("OfficialWorkingDays") : Utils.GetString("weekdays")), (Geometry)null, "")
			{
				Selected = (flag10 || flag11),
				ShowSelectedIcon = false,
				SubActions = new List<CustomMenuItemViewModel>
				{
					new CustomMenuItemViewModel((object)"WorkingDays", Utils.GetString("EveryWeekday"), (Geometry)null, "")
					{
						Selected = flag10,
						UIContent = GetDescContent("(" + Utils.GetString("MonToFri") + ")", flag10)
					},
					new CustomMenuItemViewModel((object)"OfficialWorkingDays", Utils.GetString("OfficialWorkingDays"), (Geometry)null, "")
					{
						Selected = flag11
					}
				}
			});
			list.Add(new CustomMenuItemViewModel((object)"Holidays", flag4 ? Utils.GetString("every_weekend") : (flag12 ? Utils.GetString("official_holidays") : Utils.GetString("holidays")), (Geometry)null, "")
			{
				Selected = (flag4 || flag12),
				ShowSelectedIcon = false,
				SubActions = new List<CustomMenuItemViewModel>
				{
					new CustomMenuItemViewModel((object)"Weekend", Utils.GetString("every_weekend"), (Geometry)null, "")
					{
						Selected = flag4
					},
					new CustomMenuItemViewModel((object)"OfficalHolidays", Utils.GetString("official_holidays"), (Geometry)null, "")
					{
						Selected = flag12
					}
				}
			});
			bool flag13 = flag2 && repeatExtra.RepeatFlag.Contains("LUNAR");
			bool flag14 = flag13 && repeatExtra.RepeatFlag.Contains("MONTHLY");
			bool flag15 = flag13 && !flag14;
			DateTime currentOrNextRepeatDate2 = RepeatCalculator.GetCurrentOrNextRepeatDate(repeatExtra.RepeatFlag, dateTime);
			list.Add(new CustomMenuItemViewModel((object)"Lunar", Utils.GetString((!flag13) ? "LunarRepeat" : (flag14 ? "LunarMonthly" : "EveryLunarYear")), (Geometry)null, "")
			{
				Selected = flag13,
				UIContent = (flag13 ? GetDescContent("(" + (flag14 ? DateUtils.GetLunarDay(flag14 ? currentOrNextRepeatDate2 : dateTime) : DateUtils.GetLunarMonthDay(flag15 ? currentOrNextRepeatDate2 : dateTime)) + ")", selected: true) : null),
				ShowSelectedIcon = false,
				SubActions = new List<CustomMenuItemViewModel>
				{
					new CustomMenuItemViewModel((object)"LunarMonthly", Utils.GetString("LunarMonthly"), (Geometry)null, "")
					{
						Selected = flag14,
						UIContent = GetDescContent("(" + DateUtils.GetLunarDay(flag14 ? currentOrNextRepeatDate2 : dateTime) + ")", flag14)
					},
					new CustomMenuItemViewModel((object)"LunarYearly", Utils.GetString("EveryLunarYear"), (Geometry)null, "")
					{
						Selected = flag15,
						UIContent = GetDescContent("(" + DateUtils.GetLunarMonthDay(flag15 ? currentOrNextRepeatDate2 : dateTime) + ")", flag15)
					}
				}
			});
			list.Add(new CustomMenuItemViewModel((object)"Ebbinghaus", Utils.GetString("Ebbinghaus"), (Geometry)null, "")
			{
				Selected = (!flag && repeatExtra.RepeatFlag.Contains("FORGETTINGCURVE"))
			});
		}
		list.Add(new CustomMenuItemViewModel(null));
		list.Add(new CustomMenuItemViewModel((object)"Custom", Utils.GetString("Custom"), (Geometry)null, "")
		{
			Selected = (!flag && !flag2 && !repeatExtra.RepeatFlag.Contains("FORGETTINGCURVE"))
		});
		CustomMenuList customMenuList = new CustomMenuList();
		customMenuList.InitItems(list);
		(list.FirstOrDefault((CustomMenuItemViewModel i) => i.Selected) ?? list[0]).HoverSelected = true;
		ListContent.Content = customMenuList;
		customMenuList.Operated += delegate(object sender, object o)
		{
			OnItemSelected(o.ToString());
		};
		customMenuList.Closed += delegate
		{
			ClosePopup();
		};
		static TextBlock GetDescContent(string desc, bool selected)
		{
			TextBlock textBlock = new TextBlock();
			textBlock.Margin = new Thickness(0.0, 0.0, 8.0, 0.0);
			textBlock.HorizontalAlignment = HorizontalAlignment.Left;
			textBlock.VerticalAlignment = VerticalAlignment.Center;
			textBlock.SetResourceReference(TextBlock.FontSizeProperty, "Font12");
			textBlock.SetResourceReference(TextBlock.ForegroundProperty, selected ? "TextAccentColor" : "BaseColorOpacity40");
			textBlock.Text = desc;
			return textBlock;
		}
	}

	private void OnPopupClosed(object sender, EventArgs e)
	{
		this.PopupClosed?.Invoke(sender, e);
	}

	private void OnItemSelected(string val)
	{
		RecurrencePattern recurrencePattern = new RecurrencePattern
		{
			Interval = 1
		};
		switch (val)
		{
		case "Daily":
			recurrencePattern.Frequency = FrequencyType.Daily;
			break;
		case "Weekly":
			recurrencePattern.Frequency = FrequencyType.Weekly;
			if (isNewRrule)
			{
				DateTime dateTime3 = (Utils.IsEmptyDate(_repeat.DefaultDate) ? DateTime.Today : _repeat.DefaultDate);
				recurrencePattern.ByDay = new List<WeekDay>
				{
					new WeekDay(dateTime3.DayOfWeek)
				};
			}
			break;
		case "Monthly":
			if (isNewRrule)
			{
				DateTime dateTime2 = (Utils.IsEmptyDate(_repeat.DefaultDate) ? DateTime.Today : _repeat.DefaultDate);
				recurrencePattern.ByMonthDay = new List<int> { dateTime2.Day };
			}
			recurrencePattern.Frequency = FrequencyType.Monthly;
			break;
		case "Yearly":
			recurrencePattern.Frequency = FrequencyType.Yearly;
			if (isNewRrule)
			{
				DateTime dateTime = (Utils.IsEmptyDate(_repeat.DefaultDate) ? DateTime.Today : _repeat.DefaultDate);
				recurrencePattern.ByMonth = new List<int> { dateTime.Month };
				recurrencePattern.ByMonthDay = new List<int> { dateTime.Day };
			}
			break;
		}
		foreach (CalendarConvertType value in Enum.GetValues(typeof(CalendarConvertType)))
		{
			if (value != CalendarConvertType.Unknown)
			{
				if (val == value.ToString() + "Monthly")
				{
					this.RepeatSelect?.Invoke(this, GetSubRepeatFlag(value, isMonthly: true));
					val = "subCalendar";
					break;
				}
				if (val == value.ToString() + "Yearly")
				{
					this.RepeatSelect?.Invoke(this, GetSubRepeatFlag(value, isMonthly: false));
					val = "subCalendar";
					break;
				}
			}
		}
		switch (val)
		{
		case "Custom":
			ShowCustomRepeatDialog(enter: true);
			return;
		case "Ebbinghaus":
			this.RepeatSelect?.Invoke(this, GetEbbinghausRepeatFlag());
			break;
		case "Daily":
			this.RepeatSelect?.Invoke(this, new RepeatExtra
			{
				RepeatFlag = "RRULE:FREQ=DAILY",
				RepeatFrom = "2"
			});
			break;
		case "OfficalHolidays":
			this.RepeatSelect?.Invoke(this, new RepeatExtra
			{
				RepeatFlag = "RRULE:FREQ=DAILY;INTERVAL=1;TT_SKIP=OFFICIAL_WORKDAY",
				RepeatFrom = "2"
			});
			break;
		case "WorkingDays":
			this.RepeatSelect?.Invoke(this, new RepeatExtra
			{
				RepeatFlag = "RRULE:FREQ=WEEKLY;INTERVAL=1;BYDAY=WE,MO,TU,TH,FR",
				RepeatFrom = "2"
			});
			break;
		case "Weekend":
			this.RepeatSelect?.Invoke(this, new RepeatExtra
			{
				RepeatFlag = "RRULE:FREQ=WEEKLY;INTERVAL=1;BYDAY=SA,SU",
				RepeatFrom = "2"
			});
			break;
		case "OfficialWorkingDays":
			this.RepeatSelect?.Invoke(this, new RepeatExtra
			{
				RepeatFlag = "RRULE:FREQ=DAILY;INTERVAL=1;TT_SKIP=HOLIDAY,WEEKEND",
				RepeatFrom = "2"
			});
			break;
		default:
		{
			string repeatFlag = string.Empty;
			string text = RecurrenceModel.Normalize(recurrencePattern);
			if (text != "FREQ:NONE")
			{
				repeatFlag = "RRULE:" + text;
			}
			this.RepeatSelect?.Invoke(this, new RepeatExtra
			{
				RepeatFlag = repeatFlag,
				RepeatFrom = "2"
			});
			break;
		}
		case "subCalendar":
			break;
		}
		ClosePopup();
	}

	private RepeatExtra GetEbbinghausRepeatFlag()
	{
		return new RepeatExtra
		{
			RepeatFrom = "0",
			RepeatFlag = RepeatUtils.GetEbbinghausRepeatFlag()
		};
	}

	private RepeatExtra GetSubRepeatFlag(CalendarConvertType type, bool isMonthly)
	{
		return new RepeatExtra
		{
			RepeatFrom = "2",
			RepeatFlag = RepeatUtils.GetSubCalendarRepeatFlag(type, isMonthly, _repeat.DefaultDate)
		};
	}

	private void ShowCustomRepeatDialog(bool enter)
	{
		RepeatFromType repeatType = RepeatUtils.GetRepeatType(_repeat.RepeatFrom, _repeat.RepeatFlag);
		CustomRepeatDialog.Init(_popup, repeatType, _repeat.RepeatFlag, _calendarMode, _repeat.DefaultDate, _countdownMode, _lunarMode);
		CustomRepeatDialog.SetTabEnter(enter);
		CustomRepeatDialog.OnCustomRepeatSet += delegate(string customRepeatFrom, string repeatFlag)
		{
			_repeat.RepeatFrom = customRepeatFrom;
			_repeat.RepeatFlag = repeatFlag;
			this.RepeatSelect?.Invoke(CustomRepeatDialog, new RepeatExtra
			{
				RepeatFrom = customRepeatFrom,
				RepeatFlag = repeatFlag
			});
		};
		ListContent.Visibility = Visibility.Collapsed;
		CustomContainer.Visibility = Visibility.Visible;
	}

	public void ClosePopup()
	{
		_popup.IsOpen = false;
	}

	public ITabControl GetTabControlChild()
	{
		if (ListContent.Visibility == Visibility.Visible && ListContent.Content is CustomMenuList result)
		{
			return result;
		}
		return CustomRepeatDialog;
	}

	public bool HandleTab(bool shift)
	{
		return GetTabControlChild().HandleTab(shift);
	}

	public bool HandleEnter()
	{
		return GetTabControlChild().HandleEnter();
	}

	public bool HandleEsc()
	{
		return GetTabControlChild().HandleEsc();
	}

	public bool UpDownSelect(bool isUp)
	{
		return GetTabControlChild().UpDownSelect(isUp);
	}

	public bool LeftRightSelect(bool isLeft)
	{
		return GetTabControlChild().LeftRightSelect(isLeft);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/time/setrepeatdialog.xaml", UriKind.Relative);
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
			ListContent = (ContentControl)target;
			break;
		case 2:
			CustomContainer = (Grid)target;
			break;
		case 3:
			CustomRepeatDialog = (CustomRepeatDialog)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
