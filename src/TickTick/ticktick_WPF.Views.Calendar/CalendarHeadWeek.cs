using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Views.Calendar;

public class CalendarHeadWeek : UserControl, IComponentConnector
{
	public string DefaultViewType = "0";

	private string _currentViewType;

	private bool _loaded;

	private CalendarControl _calendarControl;

	private List<CalendarHeadWeekModel> _models = new List<CalendarHeadWeekModel>();

	internal Grid CalendarHeadWeekGrid;

	internal ColumnDefinition Column6;

	internal ColumnDefinition Column7;

	internal TextBlock Mon;

	internal TextBlock Tue;

	internal TextBlock Wed;

	internal TextBlock Thu;

	internal TextBlock Fri;

	internal TextBlock Sat;

	internal TextBlock Sun;

	internal TextBlock DayMode;

	private bool _contentLoaded;

	public CalendarHeadWeek()
	{
		InitializeComponent();
		base.Loaded += OnLoaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		base.Loaded -= OnLoaded;
		DateTime dateTime = DateTime.Today;
		while (dateTime.DayOfWeek != DayOfWeek.Monday)
		{
			dateTime = dateTime.AddDays(1.0);
		}
		Mon.Text = dateTime.ToString("ddd", App.Ci);
		Tue.Text = dateTime.AddDays(1.0).ToString("ddd", App.Ci);
		Wed.Text = dateTime.AddDays(2.0).ToString("ddd", App.Ci);
		Thu.Text = dateTime.AddDays(3.0).ToString("ddd", App.Ci);
		Fri.Text = dateTime.AddDays(4.0).ToString("ddd", App.Ci);
		Sat.Text = dateTime.AddDays(5.0).ToString("ddd", App.Ci);
		Sun.Text = dateTime.AddDays(6.0).ToString("ddd", App.Ci);
		_loaded = true;
		OnSwitchView(string.Empty, DefaultViewType);
	}

	public void OnSwitchView(string from, string to)
	{
		_currentViewType = to;
		UpdateViewModel();
	}

	public void UpdateViewModel()
	{
		if (_loaded)
		{
			_ = _currentViewType;
			Mon.Visibility = Visibility.Visible;
			Mon.SetValue(Grid.ColumnProperty, GetWeekDayColumn(1));
			Tue.SetValue(Grid.ColumnProperty, GetWeekDayColumn(2));
			Wed.SetValue(Grid.ColumnProperty, GetWeekDayColumn(3));
			Thu.SetValue(Grid.ColumnProperty, GetWeekDayColumn(4));
			Fri.SetValue(Grid.ColumnProperty, GetWeekDayColumn(5));
			Sat.SetValue(Grid.ColumnProperty, GetWeekDayColumn(6));
			Sun.SetValue(Grid.ColumnProperty, GetWeekDayColumn(7));
			DayMode.Visibility = Visibility.Collapsed;
			Column6.Width = (LocalSettings.Settings.ShowCalWeekend ? new GridLength(1.0, GridUnitType.Star) : new GridLength(0.0));
			Column7.Width = (LocalSettings.Settings.ShowCalWeekend ? new GridLength(1.0, GridUnitType.Star) : new GridLength(0.0));
			CalendarHeadWeekGrid.Margin = new Thickness((!(_currentViewType == "0")) ? 65 : 0, 0.0, 0.0, 0.0);
		}
	}

	private int GetWeekDayColumn(int dayOfWeek)
	{
		DayOfWeek dayOfWeek2 = LocalSettings.Settings.WeekStartFromType;
		if (!LocalSettings.Settings.ShowCalWeekend)
		{
			dayOfWeek2 = DayOfWeek.Monday;
		}
		dayOfWeek = (int)(dayOfWeek + (7 - dayOfWeek2));
		return dayOfWeek % 7;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/calendar/calendarheadweek.xaml", UriKind.Relative);
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
			CalendarHeadWeekGrid = (Grid)target;
			break;
		case 2:
			Column6 = (ColumnDefinition)target;
			break;
		case 3:
			Column7 = (ColumnDefinition)target;
			break;
		case 4:
			Mon = (TextBlock)target;
			break;
		case 5:
			Tue = (TextBlock)target;
			break;
		case 6:
			Wed = (TextBlock)target;
			break;
		case 7:
			Thu = (TextBlock)target;
			break;
		case 8:
			Fri = (TextBlock)target;
			break;
		case 9:
			Sat = (TextBlock)target;
			break;
		case 10:
			Sun = (TextBlock)target;
			break;
		case 11:
			DayMode = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
