using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Shapes;

namespace ticktick_WPF.Views.Update;

public class DownloadProgressBar : UserControl, IComponentConnector
{
	private int _progress;

	internal Rectangle Indicator;

	private bool _contentLoaded;

	public int Progress
	{
		get
		{
			return _progress;
		}
		set
		{
			_progress = value;
			NotifyProgressChanged(_progress);
		}
	}

	public DownloadProgressBar()
	{
		InitializeComponent();
	}

	private void NotifyProgressChanged(int progress)
	{
		Indicator.Width = (int)((double)progress * base.ActualWidth / 100.0);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/update/downloadprogressbar.xaml", UriKind.Relative);
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
			Indicator = (Rectangle)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
