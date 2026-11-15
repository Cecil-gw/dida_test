using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Time;

public class DateTimeSelector : ContentControl, IComponentConnector
{
	private DateTime _time;

	internal TickDatePicker Calendar;

	internal TimeInputControl TimePointControl;

	internal Button SaveButton;

	internal Button CancelButton;

	private bool _contentLoaded;

	public event EventHandler<DateTime> TimeSelected;

	public event EventHandler Cancel;

	public DateTimeSelector(DateTime time, DateTime? maxDate = null, DateTime? minDate = null)
	{
		InitializeComponent();
		_time = time;
		Calendar.SetData(time, null, null, isStart: true, isMonthMode: false, maxDate, minDate);
		TimePointControl.SelectedTime = time;
		base.PreviewKeyUp += OnKeyUp;
	}

	private void OnKeyUp(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Escape && TimePointControl.ReminderPopup.IsOpen)
		{
			TimePointControl.HidePopup();
			e.Handled = true;
		}
	}

	private void OnSelectedDateChanged(object sender, DateTime e)
	{
		_time = DateUtils.SetDateOnly(_time, e);
	}

	private void OnTimeChanged(object sender, DateTime e)
	{
		_time = DateUtils.SetHourAndMinuteOnly(_time, e.Hour, e.Minute);
	}

	private void CancelClick(object sender, RoutedEventArgs e)
	{
		this.Cancel?.Invoke(this, null);
	}

	private void SaveClick(object sender, RoutedEventArgs e)
	{
		DateTime? time = TimePointControl.GetTime();
		if (time.HasValue)
		{
			_time = DateUtils.SetHourAndMinuteOnly(_time, time.Value.Hour, time.Value.Minute);
		}
		this.TimeSelected?.Invoke(this, _time);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/time/datetimeselector.xaml", UriKind.Relative);
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
			Calendar = (TickDatePicker)target;
			break;
		case 2:
			TimePointControl = (TimeInputControl)target;
			break;
		case 3:
			SaveButton = (Button)target;
			SaveButton.Click += SaveClick;
			break;
		case 4:
			CancelButton = (Button)target;
			CancelButton.Click += CancelClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
