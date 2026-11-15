using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.CountDown.Views;

public class HolidaySelectionWindow : Window, IComponentConnector, IStyleConnector
{
	private bool isTraditionalTabSelected = true;

	internal RadioButton TraditionalTab;

	internal RadioButton ModernTab;

	internal ItemsControl HolidaysItemsControl;

	internal Button btnSelectAll;

	internal Button btnCancel;

	internal Button btnConfirm;

	private bool _contentLoaded;

	public ObservableCollection<HolidayItem> TraditionalHolidays { get; private set; }

	public ObservableCollection<HolidayItem> ModernHolidays { get; private set; }

	public List<HolidayItem> SelectedHolidays { get; private set; }

	public HolidaySelectionWindow()
	{
		SelectedHolidays = new List<HolidayItem>();
		TraditionalHolidays = new ObservableCollection<HolidayItem>();
		ModernHolidays = new ObservableCollection<HolidayItem>();
		InitializeComponent();
		base.Loaded += HolidaySelectionWindow_Loaded;
	}

	private void HolidaySelectionWindow_Loaded(object sender, RoutedEventArgs e)
	{
		base.Loaded -= HolidaySelectionWindow_Loaded;
		InitializeHolidays();
		SafeUpdateHolidaysList();
		UpdateSelectAllButtonState();
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		base.Owner?.Activate();
		base.OnClosing(e);
	}

	private void InitializeHolidays()
	{
		TraditionalHolidays.Add(new HolidayItem("除夕", "大年三十", "pack://application:,,,/Assets/CountDown/Icons/countdown_chuxi.png", "#FFF9E3", 20240209, "LUNAR:FREQ=YEARLY;INTERVAL=1;BYMONTH=12;BYMONTHDAY=30"));
		TraditionalHolidays.Add(new HolidayItem("春节", "正月初一", "pack://application:,,,/Assets/CountDown/Icons/countdown_chunjie.png", "#FEE8E6", 20250129, "LUNAR:FREQ=YEARLY;INTERVAL=1;BYMONTH=1;BYMONTHDAY=1"));
		TraditionalHolidays.Add(new HolidayItem("元宵节", "正月十五", "pack://application:,,,/Assets/CountDown/Icons/countdown_yuanxiao.png", "#E8F7FF", 20250212, "LUNAR:FREQ=YEARLY;INTERVAL=1;BYMONTH=1;BYMONTHDAY=15"));
		TraditionalHolidays.Add(new HolidayItem("清明节", "4月5日", "pack://application:,,,/Assets/CountDown/Icons/countdown_qingming.png", "#E8F9ED", 20260405, "ERULE:NAME=CUSTOM;BYDATE=20240404,20250404,20260405,20270405,20280404,20290404,20300405,20310405,20320404,20330404,20340405,20350405,20360404,20370404,20380405,20390405,20400404,20410404,20420404,20430405,20440404,20450404,20460404,20470405,20480404,20490404,20500404,20510405,20520404,20530404,20540404,20550405,20560404,20570404,20580404,20590405,20600404,20610404,20620404,20630405,20640404,20650404,20660404,20670405,20680404,20690404,20700404,20710405,20720404,20730404,20740404,20750404,20760404,20770404,20780404,20790404,20800404,20810404,20820404,20830404,20840404,20850404,20860404,20870404,20880404,20890404,20900404,20910404,20920404,20930404,20940404,20950404,20960404,20970404,20980404,20990404"));
		TraditionalHolidays.Add(new HolidayItem("端午节", "五月初五", "pack://application:,,,/Assets/CountDown/Icons/countdown_duanwu.png", "#E5F5ED", 20250531, "LUNAR:FREQ=YEARLY;INTERVAL=1;BYMONTH=5;BYMONTHDAY=5"));
		TraditionalHolidays.Add(new HolidayItem("七夕节", "七月初七", "pack://application:,,,/Assets/CountDown/Icons/countdown_qixi.png", "#FEE6EF", 20250829, "LUNAR:FREQ=YEARLY;INTERVAL=1;BYMONTH=7;BYMONTHDAY=7"));
		TraditionalHolidays.Add(new HolidayItem("中元节", "七月十五", "pack://application:,,,/Assets/CountDown/Icons/countdown_zhongyuan.png", "#EEEFF6", 20250906, "LUNAR:FREQ=YEARLY;INTERVAL=1;BYMONTH=7;BYMONTHDAY=15"));
		TraditionalHolidays.Add(new HolidayItem("中秋节", "八月十五", "pack://application:,,,/Assets/CountDown/Icons/countdown_zhongqiu.png", "#E8F7FF", 20251006, "LUNAR:FREQ=YEARLY;INTERVAL=1;BYMONTH=8;BYMONTHDAY=15"));
		TraditionalHolidays.Add(new HolidayItem("重阳节", "九月初九", "pack://application:,,,/Assets/CountDown/Icons/countdown_chongyang.png", "#FFF4E1", 20251029, "LUNAR:FREQ=YEARLY;INTERVAL=1;BYMONTH=9;BYMONTHDAY=9"));
		TraditionalHolidays.Add(new HolidayItem("北小年", "腊月廿三", "pack://application:,,,/Assets/CountDown/Icons/countdown_beixiaonian.png", "#FEF5F4", 20260210, "LUNAR:FREQ=YEARLY;INTERVAL=1;BYMONTH=12;BYMONTHDAY=23"));
		TraditionalHolidays.Add(new HolidayItem("南小年", "腊月廿四", "pack://application:,,,/Assets/CountDown/Icons/countdown_nanxiaonian.png", "#FEF5F4", 20260211, "LUNAR:FREQ=YEARLY;INTERVAL=1;BYMONTH=12;BYMONTHDAY=24"));
		ModernHolidays.Add(new HolidayItem("元旦", "1月1日", "pack://application:,,,/Assets/CountDown/Icons/countdown_new_year_day.png", "#E8F7FF", 20260101, "RRULE:FREQ=YEARLY;INTERVAL=1;BYMONTH=1;BYMONTHDAY=1"));
		ModernHolidays.Add(new HolidayItem("情人节", "2月14日", "pack://application:,,,/Assets/CountDown/Icons/countdown_valentine_day.png", "#FEE6EF", 20260214, "RRULE:FREQ=YEARLY;INTERVAL=1;BYMONTH=2;BYMONTHDAY=14"));
		ModernHolidays.Add(new HolidayItem("妇女节", "3月8日", "pack://application:,,,/Assets/CountDown/Icons/countdown_women_day.png", "#FFF2F9", 20260308, "RRULE:FREQ=YEARLY;INTERVAL=1;BYMONTH=3;BYMONTHDAY=8"));
		ModernHolidays.Add(new HolidayItem("愚人节", "4月1日", "pack://application:,,,/Assets/CountDown/Icons/countdown_april_fool_day.png", "#EEF5FC", 20260401, "RRULE:FREQ=YEARLY;INTERVAL=1;BYMONTH=4;BYMONTHDAY=1"));
		ModernHolidays.Add(new HolidayItem("劳动节", "5月1日", "pack://application:,,,/Assets/CountDown/Icons/countdown_labor_day.png", "#FFF8E7", 20250501, "RRULE:FREQ=YEARLY;INTERVAL=1;BYMONTH=5;BYMONTHDAY=1"));
		ModernHolidays.Add(new HolidayItem("母亲节", "5月11日", "pack://application:,,,/Assets/CountDown/Icons/countdown_mother_day.png", "#FFF2F9", 20250511, "RRULE:FREQ=YEARLY;INTERVAL=1;BYMONTH=5;BYDAY=2SU"));
		ModernHolidays.Add(new HolidayItem("儿童节", "6月1日", "pack://application:,,,/Assets/CountDown/Icons/countdown_children_day.png", "#E6F9FB", 20250601, "RRULE:FREQ=YEARLY;INTERVAL=1;BYMONTH=6;BYMONTHDAY=1"));
		ModernHolidays.Add(new HolidayItem("父亲节", "6月15日", "pack://application:,,,/Assets/CountDown/Icons/countdown_father_day.png", "#EFF6FF", 20250615, "RRULE:FREQ=YEARLY;INTERVAL=1;BYMONTH=6;BYDAY=3SU"));
		ModernHolidays.Add(new HolidayItem("建党节", "7月1日", "pack://application:,,,/Assets/CountDown/Icons/countdown_party_founding_day.png", "#FEE8E6", 20250701, "RRULE:FREQ=YEARLY;INTERVAL=1;BYMONTH=7;BYMONTHDAY=1"));
		ModernHolidays.Add(new HolidayItem("建军节", "8月1日", "pack://application:,,,/Assets/CountDown/Icons/countdown_army_founding_day.png", "#FEE8E6", 20250801, "RRULE:FREQ=YEARLY;INTERVAL=1;BYMONTH=8;BYMONTHDAY=1"));
		ModernHolidays.Add(new HolidayItem("教师节", "9月10日", "pack://application:,,,/Assets/CountDown/Icons/countdown_teachers_day.png", "#F1F6FF", 20250910, "RRULE:FREQ=YEARLY;INTERVAL=1;BYMONTH=9;BYMONTHDAY=10"));
		ModernHolidays.Add(new HolidayItem("国庆节", "10月1日", "pack://application:,,,/Assets/CountDown/Icons/countdown_national_day.png", "#FEE8E6", 20251001, "RRULE:FREQ=YEARLY;INTERVAL=1;BYMONTH=10;BYMONTHDAY=1"));
		ModernHolidays.Add(new HolidayItem("万圣节", "10月31日", "pack://application:,,,/Assets/CountDown/Icons/countdown_halloween.png", "#FFF8E7", 20251031, "RRULE:FREQ=YEARLY;INTERVAL=1;BYMONTH=10;BYMONTHDAY=31"));
		ModernHolidays.Add(new HolidayItem("感恩节", "11月27日", "pack://application:,,,/Assets/CountDown/Icons/countdown_thanksgiving.png", "#FFF8E7", 20251127, "RRULE:FREQ=YEARLY;INTERVAL=1;BYMONTH=11;BYDAY=4TH"));
		ModernHolidays.Add(new HolidayItem("平安夜", "12月24日", "pack://application:,,,/Assets/CountDown/Icons/countdown_christmas_eve.png", "#F0F5FF", 20251224, "RRULE:FREQ=YEARLY;INTERVAL=1;BYMONTH=12;BYMONTHDAY=24"));
		ModernHolidays.Add(new HolidayItem("圣诞节", "12月25日", "pack://application:,,,/Assets/CountDown/Icons/countdown_christmas.png", "#F0F5FF", 20251225, "RRULE:FREQ=YEARLY;INTERVAL=1;BYMONTH=12;BYMONTHDAY=25"));
	}

	private void SelectAll_Click(object sender, RoutedEventArgs e)
	{
		ObservableCollection<HolidayItem> observableCollection = (isTraditionalTabSelected ? TraditionalHolidays : ModernHolidays);
		if (observableCollection != null)
		{
			bool isSelected = !AreAllSelected(observableCollection);
			SetAllSelected(observableCollection, isSelected);
			UpdateSelectAllButtonState();
		}
	}

	private void Cancel_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private async void Confirm_Click(object sender, RoutedEventArgs e)
	{
		SelectedHolidays.Clear();
		if (TraditionalHolidays != null)
		{
			foreach (HolidayItem item in TraditionalHolidays.Where((HolidayItem h) => h.IsSelected).ToList())
			{
				SelectedHolidays.Add(item);
			}
		}
		if (ModernHolidays != null)
		{
			foreach (HolidayItem item2 in ModernHolidays.Where((HolidayItem h) => h.IsSelected).ToList())
			{
				SelectedHolidays.Add(item2);
			}
		}
		List<CountdownModel> list = await CountdownDao.GetActiveCountdowns();
		long limitByKey = LimitCache.GetLimitByKey(Constants.LimitKind.CountdownNumber);
		if (!UserDao.IsPro())
		{
			if (list.Count + SelectedHolidays.Count > limitByKey)
			{
				ProChecker.ShowUpgradeDialog(Utils.IsDida() ? ProType.MoreAnniversaries : ProType.MoreCountdowns, Window.GetWindow(this));
				return;
			}
		}
		else if (list.Count + SelectedHolidays.Count > limitByKey)
		{
			new CustomerDialog(Utils.GetString("LimitTips"), string.Format(Utils.GetString(Utils.IsDida() ? "ProAnniversaryLimit" : "ProCountdownLimit"), limitByKey), MessageBoxButton.OK, Utils.GetToastWindow() as Window).ShowDialog();
			return;
		}
		foreach (HolidayItem item3 in SelectedHolidays.ToList())
		{
			CountdownModel defaultCountdown = CountdownModel.GetDefaultCountdown(CountDownType.Holiday);
			defaultCountdown.Name = item3.Name;
			defaultCountdown.Date = item3.TargetDate;
			defaultCountdown.RepeatFlag = item3.RepeatFlag;
			defaultCountdown.Type = CountDownType.Holiday;
			defaultCountdown.IgnoreYear = true;
			defaultCountdown.Reminders = new List<string> { "TRIGGER:P0DT9H0M0S", "TRIGGER:-P2DT15H0M0S" }.ToArray();
			defaultCountdown.IconRes = Path.GetFileNameWithoutExtension(item3.ImageSource);
			defaultCountdown.ShowCalendarType = ((!item3.RepeatFlag.Contains("LUNAR")) ? ShowCalendarType.Gregorian : ShowCalendarType.Lunar);
			await CountdownDao.CreateCountdown(defaultCountdown);
		}
		if (Window.GetWindow(base.Owner) is EditCountDownDialog editCountDownDialog)
		{
			editCountDownDialog.ChangedType = CountDownType.Holiday;
			editCountDownDialog.Close();
		}
		Close();
	}

	private void TabButton_Checked(object sender, RoutedEventArgs e)
	{
		if (sender == TraditionalTab)
		{
			isTraditionalTabSelected = true;
			foreach (HolidayItem modernHoliday in ModernHolidays)
			{
				modernHoliday.IsSelected = false;
			}
		}
		else if (sender == ModernTab)
		{
			isTraditionalTabSelected = false;
			foreach (HolidayItem traditionalHoliday in TraditionalHolidays)
			{
				traditionalHoliday.IsSelected = false;
			}
		}
		SafeUpdateHolidaysList();
		UpdateSelectAllButtonState();
	}

	private void SafeUpdateHolidaysList()
	{
		try
		{
			if (HolidaysItemsControl != null)
			{
				ObservableCollection<HolidayItem> observableCollection = (isTraditionalTabSelected ? TraditionalHolidays : ModernHolidays);
				if (observableCollection != null)
				{
					HolidaysItemsControl.ItemsSource = observableCollection;
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("Error in UpdateHolidaysList: " + ex.Message);
		}
	}

	private void UpdateSelectAllButtonState()
	{
		if (btnSelectAll != null)
		{
			ObservableCollection<HolidayItem> observableCollection = (isTraditionalTabSelected ? TraditionalHolidays : ModernHolidays);
			if (observableCollection != null)
			{
				btnSelectAll.Content = (AreAllSelected(observableCollection) ? Utils.GetString("DeselectAll") : Utils.GetString("SelectAll"));
			}
		}
	}

	private bool AreAllSelected(ObservableCollection<HolidayItem> holidays)
	{
		if (holidays == null || holidays.Count == 0)
		{
			return false;
		}
		foreach (HolidayItem holiday in holidays)
		{
			if (!holiday.IsSelected)
			{
				return false;
			}
		}
		return true;
	}

	private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		try
		{
			if (sender is Border { DataContext: HolidayItem dataContext })
			{
				dataContext.IsSelected = !dataContext.IsSelected;
				UpdateSelectAllButtonState();
				e.Handled = true;
			}
		}
		catch (Exception)
		{
		}
	}

	private void SetAllSelected(ObservableCollection<HolidayItem> holidays, bool isSelected)
	{
		if (holidays == null)
		{
			return;
		}
		foreach (HolidayItem holiday in holidays)
		{
			holiday.IsSelected = isSelected;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/countdown/views/holidayselectionwindow.xaml", UriKind.Relative);
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
			TraditionalTab = (RadioButton)target;
			TraditionalTab.Checked += TabButton_Checked;
			break;
		case 2:
			ModernTab = (RadioButton)target;
			ModernTab.Checked += TabButton_Checked;
			break;
		case 3:
			HolidaysItemsControl = (ItemsControl)target;
			break;
		case 5:
			btnSelectAll = (Button)target;
			btnSelectAll.Click += SelectAll_Click;
			break;
		case 6:
			btnCancel = (Button)target;
			btnCancel.Click += Cancel_Click;
			break;
		case 7:
			btnConfirm = (Button)target;
			btnConfirm.Click += Confirm_Click;
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
		if (connectionId == 4)
		{
			((Border)target).MouseLeftButtonUp += Grid_MouseLeftButtonDown;
		}
	}
}
