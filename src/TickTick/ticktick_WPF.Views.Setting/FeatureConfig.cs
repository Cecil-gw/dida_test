using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Habit;
using ticktick_WPF.Views.Pomo;

namespace ticktick_WPF.Views.Setting;

public class FeatureConfig : UserControl, IComponentConnector
{
	private bool _requestAIFloatingEntryGuideOnClose;

	internal SmoothScrollViewer ScrollViewer;

	internal CheckBox CalendarCheckBox;

	internal CheckBox MatrixCheckBox;

	internal CheckBox HabitCheckBox;

	internal TextBlock HabitSetting;

	internal CheckBox FocusCheckBox;

	internal TextBlock FocusSetting;

	internal CheckBox CountDownCheckBox;

	internal TextBlock AITitle;

	internal CheckBox AICheckBox;

	internal TextBlock AIDescription;

	internal Border AIPreview;

	private bool _contentLoaded;

	public FeatureConfig()
	{
		InitializeComponent();
		HabitSetting.Visibility = ((!LocalSettings.Settings.ShowHabit) ? Visibility.Collapsed : Visibility.Visible);
		FocusSetting.Visibility = ((!LocalSettings.Settings.EnableFocus) ? Visibility.Collapsed : Visibility.Visible);
		MatrixCheckBox.IsChecked = LocalSettings.Settings.ShowMatrix;
		HabitCheckBox.IsChecked = LocalSettings.Settings.ShowHabit;
		FocusCheckBox.IsChecked = LocalSettings.Settings.EnableFocus;
		CalendarCheckBox.IsChecked = LocalSettings.Settings.EnableCalendar;
		CountDownCheckBox.IsChecked = LocalSettings.Settings.EnableCountDownDays;
		AICheckBox.IsChecked = LocalSettings.Settings.EnableAI;
		if (Utils.IsWindows7())
		{
			AITitle.Visibility = Visibility.Collapsed;
			AICheckBox.Visibility = Visibility.Collapsed;
			AIDescription.Visibility = Visibility.Collapsed;
			AIPreview.Visibility = Visibility.Collapsed;
		}
	}

	private void OnSettingTextClick(object sender, MouseButtonEventArgs e)
	{
		if (object.Equals(sender, HabitSetting))
		{
			HabitSettingWindow habitSettingWindow = new HabitSettingWindow();
			habitSettingWindow.Owner = Window.GetWindow(this);
			habitSettingWindow.ShowDialog();
		}
		else if (object.Equals(sender, FocusSetting))
		{
			PomoSettings.ShowInstance(Window.GetWindow(this));
		}
	}

	protected virtual void OnCheckBoxClick(object sender, MouseButtonEventArgs e)
	{
		Mouse.Capture(null);
		e.Handled = true;
		if (!(sender is CheckBox checkBox))
		{
			return;
		}
		checkBox.IsChecked = !checkBox.IsChecked;
		if (object.Equals(sender, HabitCheckBox))
		{
			LocalSettings.Settings.ShowOrHideTabBar("HABIT", checkBox.IsChecked == true);
			HabitSetting.Visibility = ((HabitCheckBox.IsChecked != true) ? Visibility.Collapsed : Visibility.Visible);
		}
		else if (object.Equals(sender, FocusCheckBox))
		{
			if (checkBox.IsChecked != true)
			{
				TickFocusManager.CloseFocus();
			}
			LocalSettings.Settings.PomoLocalSetting.PomoWindowLeft = -1;
			LocalSettings.Settings.PomoLocalSetting.PomoWindowTop = -1;
			LocalSettings.Settings.ShowOrHideTabBar("POMO", checkBox.IsChecked == true);
			FocusSetting.Visibility = ((FocusCheckBox.IsChecked != true) ? Visibility.Collapsed : Visibility.Visible);
		}
		else if (object.Equals(sender, MatrixCheckBox))
		{
			LocalSettings.Settings.ShowOrHideTabBar("MATRIX", checkBox.IsChecked == true);
		}
		else if (object.Equals(sender, CalendarCheckBox))
		{
			LocalSettings.Settings.ShowOrHideTabBar("CALENDAR", checkBox.IsChecked == true);
		}
		else if (object.Equals(sender, CountDownCheckBox))
		{
			LocalSettings.Settings.ShowOrHideTabBar("COUNTDOWN", checkBox.IsChecked == true);
			if (checkBox.IsChecked == true)
			{
				CountdownSyncService.SyncCountdowns();
			}
		}
		else
		{
			if (!object.Equals(sender, AICheckBox))
			{
				return;
			}
			bool valueOrDefault = checkBox.IsChecked == true;
			LocalSettings.Settings.ShowOrHideTabBar("AI_ASSISTANT", valueOrDefault);
			_requestAIFloatingEntryGuideOnClose = valueOrDefault;
			if (valueOrDefault)
			{
				Window window = Window.GetWindow(this);
				if (window == null)
				{
					MainWindowManager.Window?.RequestAIFloatingEntryGuide();
					return;
				}
				window.Closed -= OnSettingWindowClosed;
				window.Closed += OnSettingWindowClosed;
			}
		}
	}

	private void OnSettingWindowClosed(object sender, EventArgs e)
	{
		if (sender is Window window)
		{
			window.Closed -= OnSettingWindowClosed;
		}
		if (_requestAIFloatingEntryGuideOnClose)
		{
			_requestAIFloatingEntryGuideOnClose = false;
			MainWindowManager.Window?.RequestAIFloatingEntryGuide();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/setting/featureconfig.xaml", UriKind.Relative);
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
			ScrollViewer = (SmoothScrollViewer)target;
			break;
		case 2:
			CalendarCheckBox = (CheckBox)target;
			CalendarCheckBox.PreviewMouseLeftButtonUp += OnCheckBoxClick;
			break;
		case 3:
			MatrixCheckBox = (CheckBox)target;
			MatrixCheckBox.PreviewMouseLeftButtonUp += OnCheckBoxClick;
			break;
		case 4:
			HabitCheckBox = (CheckBox)target;
			HabitCheckBox.PreviewMouseLeftButtonUp += OnCheckBoxClick;
			break;
		case 5:
			HabitSetting = (TextBlock)target;
			HabitSetting.MouseLeftButtonUp += OnSettingTextClick;
			break;
		case 6:
			FocusCheckBox = (CheckBox)target;
			FocusCheckBox.PreviewMouseLeftButtonUp += OnCheckBoxClick;
			break;
		case 7:
			FocusSetting = (TextBlock)target;
			FocusSetting.MouseLeftButtonUp += OnSettingTextClick;
			break;
		case 8:
			CountDownCheckBox = (CheckBox)target;
			CountDownCheckBox.PreviewMouseLeftButtonUp += OnCheckBoxClick;
			break;
		case 9:
			AITitle = (TextBlock)target;
			break;
		case 10:
			AICheckBox = (CheckBox)target;
			AICheckBox.PreviewMouseLeftButtonUp += OnCheckBoxClick;
			break;
		case 11:
			AIDescription = (TextBlock)target;
			break;
		case 12:
			AIPreview = (Border)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
