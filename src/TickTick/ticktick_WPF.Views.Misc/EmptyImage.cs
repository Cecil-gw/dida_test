using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Shapes;

namespace ticktick_WPF.Views.Misc;

public class EmptyImage : Viewbox, IComponentConnector
{
	internal Image Image;

	internal Path Path;

	private bool _contentLoaded;

	public EmptyImage()
	{
		InitializeComponent();
		base.Width = 200.0;
		base.Height = 200.0;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/misc/emptyimage.xaml", UriKind.Relative);
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
			Image = (Image)target;
			break;
		case 2:
			Path = (Path)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
