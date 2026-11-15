using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace ticktick_WPF.Views.Filter;

public class FilterPreviewControl : UserControl, IComponentConnector
{
	private bool _contentLoaded;

	public FilterPreviewControl()
	{
		InitializeComponent();
	}

	private void ClosePreview(object sender, MouseButtonEventArgs e)
	{
	}

	private void SaveFilter(object sender, RoutedEventArgs e)
	{
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/filter/filterpreviewcontrol.xaml", UriKind.Relative);
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
			((Grid)target).MouseLeftButtonUp += ClosePreview;
			break;
		case 2:
			((Button)target).Click += SaveFilter;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
