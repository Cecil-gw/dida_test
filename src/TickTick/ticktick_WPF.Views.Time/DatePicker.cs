using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Time;

public class DatePicker : UserControl, IComponentConnector
{
	internal TextBlock DateText;

	internal EscPopup Popup;

	internal TickDatePicker Calendar;

	private bool _contentLoaded;

	public DateTime? SelectedDate
	{
		get
		{
			return Calendar.SelectedDate;
		}
		set
		{
			Calendar.SelectedDate = value;
			if (value.HasValue)
			{
				DateText.Text = value.Value.ToString("yyyy/MM/dd");
			}
		}
	}

	public DatePicker()
		: this(DateTime.Today)
	{
	}

	public DatePicker(DateTime selectedDate)
	{
		InitializeComponent();
		Calendar.SelectedDate = selectedDate;
		Calendar.SelectedDateChanged += delegate(object sender, DateTime date)
		{
			Popup.IsOpen = false;
			DateText.Text = date.ToString("yyyy/MM/dd");
		};
	}

	private void OnTextClick(object sender, MouseButtonEventArgs e)
	{
		Popup.IsOpen = !Popup.IsOpen;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/time/datepicker.xaml", UriKind.Relative);
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
			((Border)target).MouseLeftButtonUp += OnTextClick;
			break;
		case 2:
			DateText = (TextBlock)target;
			break;
		case 3:
			Popup = (EscPopup)target;
			break;
		case 4:
			Calendar = (TickDatePicker)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
