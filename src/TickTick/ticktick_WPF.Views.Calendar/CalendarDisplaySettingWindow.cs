using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Calendar;

public class CalendarDisplaySettingWindow : Window, IComponentConnector
{
	private const string List = "list";

	private new const string Tag = "tag";

	private const string Priority = "priority";

	internal Grid ColorGrid;

	internal TextBlock SelectedStyle;

	internal Grid ShowWeekGrid;

	internal CheckBox ShowWeekend;

	internal CheckBox ShowComplete;

	internal CheckBox ShowSubtask;

	internal CheckBox ShowRecycle;

	internal Grid ShowHabitPanel;

	internal CheckBox ShowHabit;

	internal Grid ShowFocusPanel;

	internal CheckBox ShowFocus;

	internal Grid ShowCountdownPanel;

	internal CheckBox ShowCountdown;

	internal Grid ShowCoursePanel;

	internal CheckBox ShowCourse;

	internal CheckBox EnableTimeZoneCheckBox;

	internal TextBlock TimeZoneSetText;

	private bool _contentLoaded;

	public CalendarDisplaySettingWindow()
	{
		InitializeComponent();
		ShowComplete.IsChecked = LocalSettings.Settings.ShowCompletedInCal;
		ShowSubtask.IsChecked = LocalSettings.Settings.ShowCheckListInCal;
		ShowRecycle.IsChecked = LocalSettings.Settings.ShowRepeatCircles;
		ShowFocus.IsChecked = LocalSettings.Settings.ShowFocusRecord;
		if (LocalSettings.Settings.ShowHabit)
		{
			ShowHabitPanel.Visibility = Visibility.Visible;
			ShowHabit.IsChecked = LocalSettings.Settings.HabitInCal;
		}
		if (LocalSettings.Settings.EnableCountDownDays)
		{
			ShowCountdownPanel.Visibility = Visibility.Visible;
			ShowCountdown.IsChecked = LocalSettings.Settings.ExtraSettings.ShowCountdownInCal;
		}
		TimeTableModel timeTable = LocalSettings.Settings.UserPreference.TimeTable;
		if (timeTable != null && timeTable.isEnabled)
		{
			ShowCoursePanel.Visibility = Visibility.Visible;
			ShowCourse.IsChecked = LocalSettings.Settings.UserPreference.TimeTable.displayInCalendar;
		}
		ShowWeekend.IsChecked = LocalSettings.Settings.ShowCalWeekend;
		EnableTimeZoneCheckBox.IsChecked = LocalSettings.Settings.EnableCalendarTimeZone;
		TimeZoneSetText.Visibility = ((!EnableTimeZoneCheckBox.IsChecked.Value) ? Visibility.Collapsed : Visibility.Visible);
		base.InputBindings.Add(new KeyBinding(WindowCommands.EscCommand, new KeyGesture(Key.Escape))
		{
			CommandParameter = this
		});
		base.Loaded += SetEvents;
		base.Closing += delegate
		{
			base.Owner?.Activate();
		};
	}

	private void SetEvents(object sender, RoutedEventArgs e)
	{
		ShowComplete.Checked += OnShowCompletedChanged;
		ShowComplete.Unchecked += OnShowCompletedChanged;
		ShowSubtask.Checked += OnShowCheckListChanged;
		ShowSubtask.Unchecked += OnShowCheckListChanged;
		ShowRecycle.Checked += OnShowRepeatChanged;
		ShowRecycle.Unchecked += OnShowRepeatChanged;
		ShowFocus.Checked += OnShowFocusChanged;
		ShowFocus.Unchecked += OnShowFocusChanged;
		ShowHabit.Checked += OnShowHabitChanged;
		ShowHabit.Unchecked += OnShowHabitChanged;
		ShowWeekend.Checked += OnShowWeekendChanged;
		ShowWeekend.Unchecked += OnShowWeekendChanged;
		ShowCourse.Checked += OnShowCourseChanged;
		ShowCourse.Unchecked += OnShowCourseChanged;
		ShowCountdown.Checked += OnShowCountdownChanged;
		ShowCountdown.Unchecked += OnShowCountdownChanged;
	}

	private void OnShowWeekendChanged(object sender, RoutedEventArgs e)
	{
		Mouse.Capture(null);
		e.Handled = true;
		if (sender is CheckBox checkBox)
		{
			LocalSettings.Settings.ShowCalWeekend = checkBox.IsChecked == true;
			LocalSettings.Settings.Save();
		}
	}

	private void OnShowCountdownChanged(object sender, RoutedEventArgs e)
	{
		Mouse.Capture(null);
		e.Handled = true;
		if (sender is CheckBox checkBox)
		{
			LocalSettings.Settings.ExtraSettings.ShowCountdownInCal = checkBox.IsChecked == true;
			LocalSettings.Settings.Save();
		}
	}

	private void OnShowHabitChanged(object sender, RoutedEventArgs e)
	{
		Mouse.Capture(null);
		e.Handled = true;
		if (sender is CheckBox checkBox)
		{
			LocalSettings.Settings.HabitInCal = checkBox.IsChecked == true;
			LocalSettings.Settings.Save();
			DataChangedNotifier.NotifyCalendarConfigChanged(colorChanged: false);
			HabitService.PushHabitSettings();
			UserActCollectUtils.AddClickEvent("calendar", "view_options", LocalSettings.Settings.HabitInCal ? "show_habit" : "hide_habit");
		}
	}

	private void OnShowFocusChanged(object sender, RoutedEventArgs e)
	{
		Mouse.Capture(null);
		e.Handled = true;
		if (sender is CheckBox checkBox)
		{
			LocalSettings.Settings.ShowFocusRecord = checkBox.IsChecked == true;
			LocalSettings.Settings.Save();
			DataChangedNotifier.NotifyCalendarConfigChanged(colorChanged: false);
			UserActCollectUtils.AddClickEvent("calendar", "view_options", LocalSettings.Settings.ShowFocusRecord ? "show_focus" : "hide_focus");
		}
	}

	private void OnShowFocusClick(object sender, MouseButtonEventArgs e)
	{
		ShowFocus.IsChecked = !ShowFocus.IsChecked;
	}

	private void OnShowRecycleClick(object sender, MouseButtonEventArgs e)
	{
		ShowRecycle.IsChecked = !ShowRecycle.IsChecked;
	}

	private void OnShowCompleteClick(object sender, MouseButtonEventArgs e)
	{
		ShowComplete.IsChecked = !ShowComplete.IsChecked;
	}

	private void OnShowSubtaskClick(object sender, MouseButtonEventArgs e)
	{
		ShowSubtask.IsChecked = !ShowSubtask.IsChecked;
	}

	private void OnShowWeekendClick(object sender, MouseButtonEventArgs e)
	{
		ShowWeekend.IsChecked = !ShowWeekend.IsChecked;
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private void OnShowCompletedChanged(object sender, RoutedEventArgs e)
	{
		Mouse.Capture(null);
		e.Handled = true;
		if (sender is CheckBox checkBox)
		{
			LocalSettings.Settings.ShowCompletedInCal = checkBox.IsChecked == true;
			LocalSettings.Settings.Save();
			UserActCollectUtils.AddClickEvent("calendar", "view_options", LocalSettings.Settings.ShowCompletedInCal ? "show_completed" : "hide_completed");
			DataChangedNotifier.NotifyCalendarConfigChanged(colorChanged: false);
		}
	}

	private void OnShowCheckListChanged(object sender, RoutedEventArgs e)
	{
		Mouse.Capture(null);
		e.Handled = true;
		if (sender is CheckBox checkBox)
		{
			LocalSettings.Settings.ShowCheckListInCal = checkBox.IsChecked == true;
			LocalSettings.Settings.Save();
			UserActCollectUtils.AddClickEvent("calendar", "view_options", LocalSettings.Settings.ShowCheckListInCal ? "show_check_item" : "hide_check_item");
			DataChangedNotifier.NotifyCalendarConfigChanged(colorChanged: false);
		}
	}

	private void OnShowRepeatChanged(object sender, RoutedEventArgs e)
	{
		Mouse.Capture(null);
		e.Handled = true;
		if (sender is CheckBox checkBox)
		{
			LocalSettings.Settings.ShowRepeatCircles = checkBox.IsChecked == true;
			LocalSettings.Settings.Save();
			UserActCollectUtils.AddClickEvent("calendar", "view_options", LocalSettings.Settings.ShowRepeatCircles ? "show_repeats" : "hide_repeats");
			DataChangedNotifier.NotifyCalendarConfigChanged(colorChanged: false);
		}
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		base.Owner.Activate();
		SettingsHelper.PushLocalSettings();
		SettingsHelper.PushLocalPreference();
		base.OnClosing(e);
	}

	private void OnShowHabitClick(object sender, MouseButtonEventArgs e)
	{
		ShowHabit.IsChecked = !ShowHabit.IsChecked;
	}

	private async void OnShowCountdownClick(object sender, MouseButtonEventArgs e)
	{
		Mouse.Capture(null);
		ShowCountdown.IsChecked = !ShowCountdown.IsChecked;
		DataChangedNotifier.NotifyCalendarConfigChanged(colorChanged: false);
	}

	private async void OnShowCourseClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		CourseScheduleModel courseScheduleModel = await ScheduleDao.GetScheduleByIdAsync(LocalSettings.Settings.UserPreference.TimeTable.currentTimetableId);
		if (ShowCourse.IsChecked != true && (courseScheduleModel == null || !courseScheduleModel.AllTimeSet()))
		{
			UtilLog.Info($"ShowCourseClick ,schedule is null = {courseScheduleModel == null}");
			new CustomerDialog(Utils.GetString("ShowCourse"), Utils.GetString("ShowCourseMessage"), Utils.GetString("GotIt"), string.Empty, this).ShowDialog();
		}
		else
		{
			UserActCollectUtils.AddClickEvent("calendar", "view_options", (ShowCourse.IsChecked == true) ? "show_course" : "hide_course");
			ShowCourse.IsChecked = !ShowCourse.IsChecked;
		}
	}

	private void OnShowCourseChanged(object sender, RoutedEventArgs e)
	{
		Mouse.Capture(null);
		e.Handled = true;
		if (sender is CheckBox checkBox)
		{
			LocalSettings.Settings.UserPreference.TimeTable.displayInCalendar = checkBox.IsChecked == true;
			LocalSettings.Settings.UserPreference.TimeTable.mtime = Utils.GetNowTimeStampInMills();
			LocalSettings.Settings.Save(force: true);
			DataChangedNotifier.OnScheduleChanged();
		}
	}

	private void OnTimeZoneSetClick(object sender, MouseButtonEventArgs e)
	{
		CalendarTimeZoneConfigWindow calendarTimeZoneConfigWindow = new CalendarTimeZoneConfigWindow();
		calendarTimeZoneConfigWindow.Owner = this;
		calendarTimeZoneConfigWindow.Closed += delegate
		{
			Activate();
		};
		calendarTimeZoneConfigWindow.ShowDialog();
	}

	private async void OnStyleClick(object sender, MouseButtonEventArgs e)
	{
		CalendarStyleSelectWindow window = new CalendarStyleSelectWindow
		{
			Owner = base.Owner
		};
		Close();
		await Task.Delay(50);
		window.ShowDialog();
	}

	private async void OnColorClick(object sender, MouseButtonEventArgs e)
	{
		CalendarColorConfigWindow window = new CalendarColorConfigWindow
		{
			Owner = base.Owner
		};
		Close();
		await Task.Delay(50);
		window.ShowDialog();
	}

	private void OnEnableTimeZoneClick(object sender, MouseButtonEventArgs e)
	{
		Mouse.Capture(null);
		e.Handled = true;
		EnableTimeZoneCheckBox.IsChecked = !EnableTimeZoneCheckBox.IsChecked;
		LocalSettings.Settings.SetEnableCalendarTimeZone(EnableTimeZoneCheckBox.IsChecked == true);
		TimeZoneSetText.Visibility = ((EnableTimeZoneCheckBox.IsChecked != true) ? Visibility.Collapsed : Visibility.Visible);
		GlobalEventManager.NotifyTimeLineTimeZonesChangedChanged(countChanged: true);
		LocalSettings.Settings.Save(force: true);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/calendar/calendardisplaysettingwindow.xaml", UriKind.Relative);
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
			ColorGrid = (Grid)target;
			ColorGrid.MouseLeftButtonUp += OnColorClick;
			break;
		case 2:
			((Grid)target).MouseLeftButtonUp += OnStyleClick;
			break;
		case 3:
			SelectedStyle = (TextBlock)target;
			break;
		case 4:
			ShowWeekGrid = (Grid)target;
			break;
		case 5:
			ShowWeekend = (CheckBox)target;
			ShowWeekend.PreviewMouseLeftButtonUp += OnShowWeekendClick;
			break;
		case 6:
			ShowComplete = (CheckBox)target;
			ShowComplete.PreviewMouseLeftButtonUp += OnShowCompleteClick;
			break;
		case 7:
			ShowSubtask = (CheckBox)target;
			ShowSubtask.PreviewMouseLeftButtonUp += OnShowSubtaskClick;
			break;
		case 8:
			ShowRecycle = (CheckBox)target;
			ShowRecycle.PreviewMouseLeftButtonUp += OnShowRecycleClick;
			break;
		case 9:
			ShowHabitPanel = (Grid)target;
			break;
		case 10:
			ShowHabit = (CheckBox)target;
			ShowHabit.PreviewMouseLeftButtonUp += OnShowHabitClick;
			break;
		case 11:
			ShowFocusPanel = (Grid)target;
			break;
		case 12:
			ShowFocus = (CheckBox)target;
			ShowFocus.PreviewMouseLeftButtonUp += OnShowFocusClick;
			break;
		case 13:
			ShowCountdownPanel = (Grid)target;
			break;
		case 14:
			ShowCountdown = (CheckBox)target;
			ShowCountdown.PreviewMouseLeftButtonUp += OnShowCountdownClick;
			break;
		case 15:
			ShowCoursePanel = (Grid)target;
			break;
		case 16:
			ShowCourse = (CheckBox)target;
			ShowCourse.PreviewMouseLeftButtonUp += OnShowCourseClick;
			break;
		case 17:
			EnableTimeZoneCheckBox = (CheckBox)target;
			EnableTimeZoneCheckBox.PreviewMouseLeftButtonUp += OnEnableTimeZoneClick;
			break;
		case 18:
			TimeZoneSetText = (TextBlock)target;
			TimeZoneSetText.MouseLeftButtonUp += OnTimeZoneSetClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
