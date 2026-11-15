using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Entity.Calendar;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Calendar.Year;

public class YearCalendarMonthControl : UserControl, IComponentConnector
{
	internal YearCalendarMonthControl Root;

	private bool _contentLoaded;

	public YearCalendarMonthControl()
	{
		InitializeComponent();
	}

	private void OnMonthTitleClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement frameworkElement)
		{
			YearViewMonthData yearViewMonthData = frameworkElement.DataContext as YearViewMonthData;
			CalendarControl calendarControl = Utils.FindParent<CalendarControl>(this);
			if (calendarControl != null)
			{
				_ = calendarControl.HeadView.SelectedDate;
				DateTime date = new DateTime(yearViewMonthData.Year, yearViewMonthData.Month, 1);
				calendarControl.NavigateDate(date, CalendarViewType.Month);
			}
		}
	}

	private void OnDayClick(object sender, MouseButtonEventArgs e)
	{
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/calendar/year/yearcalendarmonthcontrol.xaml", UriKind.Relative);
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
			Root = (YearCalendarMonthControl)target;
			break;
		case 2:
			((Grid)target).PreviewMouseLeftButtonUp += OnMonthTitleClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
