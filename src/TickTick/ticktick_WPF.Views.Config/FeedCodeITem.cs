using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Config;

public class FeedCodeITem : UserControl, IComponentConnector
{
	internal Grid Container;

	private bool _contentLoaded;

	public FeedCodeITem()
	{
		InitializeComponent();
	}

	private void OnCopyClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is ProjectFeedViewModel projectFeedViewModel)
		{
			try
			{
				Clipboard.SetText(projectFeedViewModel.Url);
				Utils.FindParent<GetTickUrlLinkDialog>(this)?.Toast(Utils.GetString("Copied"));
			}
			catch (Exception)
			{
			}
		}
	}

	private void OnRemoveClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is ProjectFeedViewModel vm)
		{
			Utils.FindParent<GetTickUrlLinkDialog>(this)?.RemoveFeedItem(vm);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/config/feedcodeitem.xaml", UriKind.Relative);
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
			Container = (Grid)target;
			break;
		case 2:
			((TextBlock)target).MouseLeftButtonUp += OnCopyClick;
			break;
		case 3:
			((TextBlock)target).MouseLeftButtonUp += OnRemoveClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
