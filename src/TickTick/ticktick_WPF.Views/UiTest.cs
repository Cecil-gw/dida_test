using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace ticktick_WPF.Views;

public class UiTest : UserControl, IComponentConnector
{
	internal PathFigure OutsidePath;

	internal LineSegment OutsideLine;

	internal ArcSegment OutsideArc;

	internal PathFigure InsidePath;

	internal LineSegment InsideLine;

	internal ArcSegment InsideArc;

	private bool _contentLoaded;

	public UiTest()
	{
		InitializeComponent();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/uitest.xaml", UriKind.Relative);
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
			OutsidePath = (PathFigure)target;
			break;
		case 2:
			OutsideLine = (LineSegment)target;
			break;
		case 3:
			OutsideArc = (ArcSegment)target;
			break;
		case 4:
			InsidePath = (PathFigure)target;
			break;
		case 5:
			InsideLine = (LineSegment)target;
			break;
		case 6:
			InsideArc = (ArcSegment)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
