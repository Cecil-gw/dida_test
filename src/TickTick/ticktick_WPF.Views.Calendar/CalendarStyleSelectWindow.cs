using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Calendar;

public class CalendarStyleSelectWindow : Window, IComponentConnector
{
	internal Path TaskIcon1;

	internal Path EventIcon1;

	internal RadioButton ModernTaskRadioButton;

	internal Path TaskIcon2;

	internal Path EventIcon2;

	internal RadioButton ClassicTaskRadioButton;

	internal RadioButton StandardYearRadioButton;

	internal RadioButton HeatYearRadioButton;

	private bool _contentLoaded;

	private bool loaded { get; set; }

	public event EventHandler SelectedChanged;

	public CalendarStyleSelectWindow()
	{
		InitializeComponent();
		CalendarConfig obj = LocalSettings.Settings?.UserPreference?.CalendarConfig;
		if (LocalSettings.Settings.ShowHeatMap)
		{
			HeatYearRadioButton.IsChecked = true;
		}
		else
		{
			StandardYearRadioButton.IsChecked = true;
		}
		if (obj?.ItemStyleType != CalendarConfig.CalendarItemStyle.Modern)
		{
			ClassicTaskRadioButton.IsChecked = true;
		}
		else
		{
			ModernTaskRadioButton.IsChecked = true;
		}
		DataChangedNotifier.CalendarShowIconChanged += DataChangedNotifier_CalendarShowIconChanged;
		DataChangedNotifier_CalendarShowIconChanged(null, null);
	}

	private void DataChangedNotifier_CalendarShowIconChanged(object sender, EventArgs e)
	{
		Path taskIcon = TaskIcon1;
		Visibility visibility = (TaskIcon2.Visibility = ((!LocalSettings.Settings.IsShowCalendarIcon(DisplayType.Task)) ? Visibility.Collapsed : Visibility.Visible));
		taskIcon.Visibility = visibility;
		Path eventIcon = EventIcon1;
		visibility = (EventIcon2.Visibility = ((!LocalSettings.Settings.IsShowCalendarIcon(DisplayType.Event)) ? Visibility.Collapsed : Visibility.Visible));
		eventIcon.Visibility = visibility;
	}

	private void Window_Loaded(object sender, RoutedEventArgs e)
	{
		loaded = true;
	}

	private void Window_Closing(object sender, CancelEventArgs e)
	{
		SaveCurrentSetting(saveAndPush: true);
		DataChangedNotifier.CalendarShowIconChanged -= DataChangedNotifier_CalendarShowIconChanged;
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private void SaveCurrentSetting(bool saveAndPush)
	{
		CalendarConfig calendarConfig = LocalSettings.Settings?.UserPreference?.CalendarConfig ?? new CalendarConfig();
		calendarConfig.Heatmap = HeatYearRadioButton.IsChecked.Value;
		calendarConfig.ItemStyleType = ((ModernTaskRadioButton.IsChecked != true) ? CalendarConfig.CalendarItemStyle.Classic : CalendarConfig.CalendarItemStyle.Modern);
		LocalSettings.Settings.SaveCalendarStyleConfig(calendarConfig, saveAndPush);
	}

	private void IconsGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		CalendarItemIconVisWindow calendarItemIconVisWindow = new CalendarItemIconVisWindow();
		calendarItemIconVisWindow.Owner = this;
		calendarItemIconVisWindow.ShowDialog();
	}

	private void OnModernTaskClick(object sender, MouseButtonEventArgs e)
	{
		ModernTaskRadioButton.IsChecked = true;
		LocalSettings settings = LocalSettings.Settings;
		if (settings == null || settings.UserPreference?.CalendarConfig?.ItemStyleType != CalendarConfig.CalendarItemStyle.Modern)
		{
			SaveCurrentSetting(saveAndPush: false);
			DataChangedNotifier.NotifyCalendarTaskbarStyleChanged();
		}
	}

	private void OnClassicTaskClick(object sender, MouseButtonEventArgs e)
	{
		ClassicTaskRadioButton.IsChecked = true;
		LocalSettings settings = LocalSettings.Settings;
		if (settings == null || settings.UserPreference?.CalendarConfig?.ItemStyleType != CalendarConfig.CalendarItemStyle.Classic)
		{
			SaveCurrentSetting(saveAndPush: false);
			DataChangedNotifier.NotifyCalendarTaskbarStyleChanged();
		}
	}

	private void OnStandardYearClick(object sender, MouseButtonEventArgs e)
	{
		StandardYearRadioButton.IsChecked = true;
		LocalSettings settings = LocalSettings.Settings;
		if (settings == null || settings.UserPreference?.CalendarConfig?.Heatmap != false)
		{
			SaveCurrentSetting(saveAndPush: false);
			DataChangedNotifier.NotifyCalendarYearHeatMapChanged(show: false);
		}
	}

	private void OnHeatYearClick(object sender, MouseButtonEventArgs e)
	{
		HeatYearRadioButton.IsChecked = true;
		LocalSettings settings = LocalSettings.Settings;
		if (settings == null || settings.UserPreference?.CalendarConfig?.Heatmap != true)
		{
			SaveCurrentSetting(saveAndPush: false);
			DataChangedNotifier.NotifyCalendarYearHeatMapChanged(show: true);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/calendar/calendarstyleselectwindow.xaml", UriKind.Relative);
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
			((CalendarStyleSelectWindow)target).Loaded += Window_Loaded;
			((CalendarStyleSelectWindow)target).Closing += Window_Closing;
			break;
		case 2:
			((StackPanel)target).MouseLeftButtonUp += OnModernTaskClick;
			break;
		case 3:
			TaskIcon1 = (Path)target;
			break;
		case 4:
			EventIcon1 = (Path)target;
			break;
		case 5:
			ModernTaskRadioButton = (RadioButton)target;
			break;
		case 6:
			((StackPanel)target).MouseLeftButtonUp += OnClassicTaskClick;
			break;
		case 7:
			TaskIcon2 = (Path)target;
			break;
		case 8:
			EventIcon2 = (Path)target;
			break;
		case 9:
			ClassicTaskRadioButton = (RadioButton)target;
			break;
		case 10:
			((Grid)target).MouseLeftButtonUp += IconsGrid_MouseLeftButtonUp;
			break;
		case 11:
			((StackPanel)target).MouseLeftButtonUp += OnStandardYearClick;
			break;
		case 12:
			StandardYearRadioButton = (RadioButton)target;
			break;
		case 13:
			((StackPanel)target).MouseLeftButtonUp += OnHeatYearClick;
			break;
		case 14:
			HeatYearRadioButton = (RadioButton)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
