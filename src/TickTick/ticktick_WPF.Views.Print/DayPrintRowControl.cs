using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using ticktick_WPF.Views.Calendar;

namespace ticktick_WPF.Views.Print;

public class DayPrintRowControl : UserControl, IComponentConnector
{
	internal TextBlock LeftText;

	internal CalendarPrintCell TaskPrintCell;

	private bool _contentLoaded;

	public DayPrintRowControl()
	{
		InitializeComponent();
	}

	public DayPrintRowControl(string text, CalendarDisplayViewModel allDayTask)
	{
		InitializeComponent();
		if (text == null)
		{
			LeftText.Visibility = Visibility.Collapsed;
		}
		else
		{
			LeftText.Text = text;
		}
		if (allDayTask == null)
		{
			TaskPrintCell.Visibility = Visibility.Collapsed;
		}
		else
		{
			TaskPrintCell.DataContext = allDayTask;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/print/dayprintrowcontrol.xaml", UriKind.Relative);
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
			LeftText = (TextBlock)target;
			break;
		case 2:
			TaskPrintCell = (CalendarPrintCell)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
