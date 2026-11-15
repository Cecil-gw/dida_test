using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Upgrade;

public class ManageProSubscribeWindow : Window, IComponentConnector
{
	internal ManageProSubscribeWindow Root;

	internal Border CloseButton;

	private bool _contentLoaded;

	private ManageProSubscribeViewModel viewModel { get; set; }

	public ManageProSubscribeWindow(List<SubscribeInfoModel> list)
	{
		InitializeComponent();
		base.DataContext = (viewModel = new ManageProSubscribeViewModel(list));
		ManageProSubscribeViewModel manageProSubscribeViewModel2 = viewModel;
		manageProSubscribeViewModel2.OnClose = (Action)Delegate.Combine(manageProSubscribeViewModel2.OnClose, new Action(OnClose));
	}

	private void OnClose()
	{
		Close();
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private void OnCloseClick(object sender, MouseButtonEventArgs e)
	{
		Close();
	}

	private void Root_Closing(object sender, CancelEventArgs e)
	{
		if (viewModel != null)
		{
			ManageProSubscribeViewModel manageProSubscribeViewModel = viewModel;
			manageProSubscribeViewModel.OnClose = (Action)Delegate.Remove(manageProSubscribeViewModel.OnClose, new Action(OnClose));
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/upgrade/manageprosubscribewindow.xaml", UriKind.Relative);
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
			Root = (ManageProSubscribeWindow)target;
			Root.Closing += Root_Closing;
			break;
		case 2:
			CloseButton = (Border)target;
			CloseButton.MouseLeftButtonUp += OnCloseClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
