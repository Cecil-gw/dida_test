using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace ticktick_WPF.Views.Time;

public class MonthPicker : UserControl, IComponentConnector
{
	private TickDatePicker _calendar;

	internal Grid MonthGrid;

	private bool _contentLoaded;

	public event EventHandler<DateTime> MonthSelected;

	public MonthPicker(DateTime date)
	{
		InitializeComponent();
		InitMonth(date);
	}

	private void InitMonth(DateTime date)
	{
		_calendar = new TickDatePicker(date, null, null, isStart: false, isMonthMode: true);
		_calendar.MonthSelected -= OnMonthSelected;
		_calendar.MonthSelected += OnMonthSelected;
		MonthGrid.Children.Add(_calendar);
		_calendar.NextOrLastGrid.Margin = new Thickness(0.0, 0.0, 4.0, 0.0);
	}

	private void OnMonthSelected(object sender, DateTime date)
	{
		this.MonthSelected?.Invoke(this, date);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/time/monthpicker.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 1)
		{
			MonthGrid = (Grid)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
