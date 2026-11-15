using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc;

public class ParallelGuideControl : UserControl, IComponentConnector
{
	internal Grid ParallelGuideGrid;

	internal Path Arrow;

	internal Path ArrowLine;

	private bool _contentLoaded;

	public ParallelGuideControl(bool isDark)
	{
		InitializeComponent();
		Arrow.Data = Geometry.Parse(isDark ? "M0,11 L10,0 L10,22 z" : "M0,9 L8,0 L8,18 z");
		ArrowLine.Data = Geometry.Parse(isDark ? "M9,0 L0,10 L9,20" : "M8,0 L0,9 L8,18");
	}

	private void OnCloseClick(object sender, MouseButtonEventArgs e)
	{
		Utils.FindParent<MainWindow>(this)?.CloseParallelGuide(openParallel: false);
	}

	private void OnEnableClick(object sender, RoutedEventArgs e)
	{
		Utils.FindParent<MainWindow>(this)?.CloseParallelGuide(openParallel: true);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/misc/parallelguidecontrol.xaml", UriKind.Relative);
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
			ParallelGuideGrid = (Grid)target;
			break;
		case 2:
			Arrow = (Path)target;
			break;
		case 3:
			ArrowLine = (Path)target;
			break;
		case 4:
			((Button)target).Click += OnEnableClick;
			break;
		case 5:
			((Border)target).MouseLeftButtonUp += OnCloseClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
