using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace ticktick_WPF.Views.Print;

public class TimePointLine : UserControl, IComponentConnector
{
	internal TextBlock TimeText;

	internal Grid BottomLine;

	private bool _contentLoaded;

	public TimePointLine(string timeText, bool showBottom = true)
	{
		InitializeComponent();
		TimeText.Text = timeText;
		if (!showBottom)
		{
			BottomLine.Visibility = Visibility.Collapsed;
			TimeText.Visibility = Visibility.Collapsed;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/print/timepointline.xaml", UriKind.Relative);
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
			TimeText = (TextBlock)target;
			break;
		case 2:
			BottomLine = (Grid)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
