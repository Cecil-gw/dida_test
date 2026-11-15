using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Calendar;

public class CalProToastBorder : UserControl, IComponentConnector
{
	private bool _contentLoaded;

	public CalProToastBorder()
	{
		InitializeComponent();
	}

	private void ProClick(object sender, MouseButtonEventArgs e)
	{
		Utils.FindParent<CalendarControl>(this)?.ProClick();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/calendar/calprotoastborder.xaml", UriKind.Relative);
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
			((StackPanel)target).MouseLeftButtonUp += ProClick;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
