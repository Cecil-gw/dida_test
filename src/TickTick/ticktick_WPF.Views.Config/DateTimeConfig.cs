using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;

namespace ticktick_WPF.Views.Config;

public class DateTimeConfig : UserControl, IComponentConnector
{
	internal CustomSimpleComboBox TimeFormatComboBox;

	internal CustomSimpleComboBox WeekStartFromComboBox;

	internal Grid LunarGrid;

	internal CheckBox LunarToggle;

	internal Grid AlternateCalendarGrid;

	internal CustomSimpleComboBox OtherCalendarComboBox;

	internal Grid ShowWeekGrid;

	internal CheckBox ShowWeekNumToggle;

	internal Grid HolidayGrid;

	internal CheckBox EnableHolidayToggle;

	private bool _contentLoaded;

	private Dictionary<string, DayOfWeek> weekStartNameDict { get; set; } = new Dictionary<string, DayOfWeek>();

	public DateTimeConfig()
	{
		InitializeComponent();
		InitData();
	}

	private void InitData()
	{
		InitTimeFormat();
		InitWeekStartFrom();
		InitLunarHolidayPanel();
		InitAlternativeCalendar();
	}

	private void InitAlternativeCalendar()
	{
		OtherCalendarComboBox.ItemsSource = CalendarDisplayInfo.GetCalendarDisplayNames();
		string valueByKey = CalendarDisplayInfo.GetValueByKey(LocalSettings.Settings.UserPreference.alternativeCalendar?.calendar);
		if (!string.IsNullOrEmpty(valueByKey))
		{
			OtherCalendarComboBox.SetSelectedValue(valueByKey);
		}
		else
		{
			OtherCalendarComboBox.SelectedIndex = 0;
		}
	}

	private void InitLunarHolidayPanel()
	{
		LunarGrid.Visibility = Visibility.Collapsed;
		HolidayGrid.Visibility = Visibility.Collapsed;
		if (Utils.IsShowHoliday())
		{
			HolidayGrid.Visibility = Visibility.Visible;
		}
		AlternateCalendarGrid.Visibility = Visibility.Collapsed;
		LunarGrid.Visibility = Visibility.Visible;
	}

	private void InitWeekStartFrom()
	{
		weekStartNameDict.Clear();
		List<string> list = new List<string>();
		int num = 0;
		int selectedIndex = -1;
		foreach (DayOfWeek value in Enum.GetValues(typeof(DayOfWeek)))
		{
			string text = Utils.GetString(value.ToString());
			list.Add(text);
			weekStartNameDict[text] = value;
			if (LocalSettings.Settings.WeekStartFromType == value)
			{
				selectedIndex = num;
			}
			num++;
		}
		WeekStartFromComboBox.ItemsSource = list;
		WeekStartFromComboBox.SelectedIndex = selectedIndex;
	}

	private void InitTimeFormat()
	{
		TimeFormatComboBox.ItemsSource = new List<string>
		{
			Utils.GetString("SystemDefault"),
			Utils.GetString("12Hour") + " (1:00 PM)",
			Utils.GetString("24Hour") + " (13:00)"
		};
		TimeFormatComboBox.SelectedIndex = ((LocalSettings.Settings.SettingsModel.TimeFormat == "24Hour") ? 2 : ((!(LocalSettings.Settings.SettingsModel.TimeFormat == "System")) ? 1 : 0));
	}

	private async void OnEnableLunarClick(object sender, MouseButtonEventArgs e)
	{
		Mouse.Capture(null);
		e.Handled = true;
		LocalSettings.Settings.EnableLunar = !LocalSettings.Settings.EnableLunar;
		Utils.FindParent<SettingDialog>(this)?.SetChanged();
	}

	private async void OnEnableHolidayClick(object sender, MouseButtonEventArgs e)
	{
		Mouse.Capture(null);
		e.Handled = true;
		LocalSettings.Settings.EnableHoliday = !LocalSettings.Settings.EnableHoliday;
		if (LocalSettings.Settings.EnableHoliday)
		{
			HolidayManager.ForcePullHolidays();
		}
		Utils.FindParent<SettingDialog>(this)?.SetChanged();
	}

	private async void OnShowWeekClick(object sender, MouseButtonEventArgs e)
	{
		Mouse.Capture(null);
		e.Handled = true;
		LocalSettings.Settings.ShowWeek = !LocalSettings.Settings.ShowWeek;
		Utils.FindParent<SettingDialog>(this)?.SetChanged();
	}

	private void EnableTimeZoneClick(object sender, MouseButtonEventArgs e)
	{
		Mouse.Capture(null);
		e.Handled = true;
		LocalSettings.Settings.EnableTimeZone = !LocalSettings.Settings.EnableTimeZone;
		Utils.FindParent<SettingDialog>(this)?.SetChanged();
	}

	private async void OtherCalendarSelectionChanged(object sender, SimpleComboBoxViewModel e)
	{
		string selectedValue = OtherCalendarComboBox.GetSelectedValue();
		LocalSettings.Settings.UserPreference.alternativeCalendar = new AlternativeCalendar
		{
			calendar = CalendarDisplayInfo.GetKeyByValue(selectedValue),
			mtime = Utils.GetNowTimeStampInMills()
		};
		LocalSettings.Settings.UserPreference.mtime = Utils.GetNowTimeStampInMills();
		LocalSettings.Settings.Save();
		LocalSettings.Settings.NotifyPropertyChanged("EnableLunar");
		await SettingsHelper.PushLocalPreference();
	}

	private void WeekStartFromSelectionChanged(object sender, SimpleComboBoxViewModel e)
	{
		string text = WeekStartFromComboBox.SelectedItem?.Title;
		if (!string.IsNullOrEmpty(text) && weekStartNameDict.TryGetValue(text, out var value))
		{
			LocalSettings.Settings.WeekStartFromType = value;
		}
		else
		{
			LocalSettings.Settings.WeekStartFromType = DayOfWeek.Sunday;
		}
		LocalSettings.Settings.Save();
		Utils.FindParent<SettingDialog>(this)?.SetChanged();
	}

	private void OnTimeFormatSelectionChanged(object sender, SimpleComboBoxViewModel e)
	{
		switch (TimeFormatComboBox.SelectedIndex)
		{
		case 0:
			LocalSettings.Settings.TimeFormat = "System";
			break;
		case 1:
			LocalSettings.Settings.TimeFormat = "12Hour";
			break;
		case 2:
			LocalSettings.Settings.TimeFormat = "24Hour";
			break;
		default:
			LocalSettings.Settings.TimeFormat = "System";
			break;
		}
		LocalSettings.Settings.Save();
		DataChangedNotifier.NotifyTimeFormatChanged();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/config/datetimeconfig.xaml", UriKind.Relative);
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
			TimeFormatComboBox = (CustomSimpleComboBox)target;
			break;
		case 2:
			WeekStartFromComboBox = (CustomSimpleComboBox)target;
			break;
		case 3:
			LunarGrid = (Grid)target;
			break;
		case 4:
			LunarToggle = (CheckBox)target;
			LunarToggle.PreviewMouseLeftButtonUp += OnEnableLunarClick;
			break;
		case 5:
			AlternateCalendarGrid = (Grid)target;
			break;
		case 6:
			OtherCalendarComboBox = (CustomSimpleComboBox)target;
			break;
		case 7:
			ShowWeekGrid = (Grid)target;
			break;
		case 8:
			ShowWeekNumToggle = (CheckBox)target;
			ShowWeekNumToggle.PreviewMouseLeftButtonUp += OnShowWeekClick;
			break;
		case 9:
			HolidayGrid = (Grid)target;
			break;
		case 10:
			EnableHolidayToggle = (CheckBox)target;
			EnableHolidayToggle.PreviewMouseLeftButtonUp += OnEnableHolidayClick;
			break;
		case 11:
			((CheckBox)target).PreviewMouseLeftButtonUp += EnableTimeZoneClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
