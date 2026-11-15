using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Upgrade;

public class PayFailureWindow : Window, IComponentConnector
{
	private bool _contentLoaded;

	public PayFailureWindow()
	{
		InitializeComponent();
	}

	private void CloseButton_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void OnHelpClick(object sender, RoutedEventArgs routedEventArgs)
	{
		Close();
		Utils.TryProcessStartUrl("https://help.dida365.com/articles/6990517106034868224");
	}

	private void OnUpgradeAgainClick(object sender, RoutedEventArgs e)
	{
		Close();
		Utils.StartUpgrade("settings");
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/upgrade/payfailurewindow.xaml", UriKind.Relative);
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
			((Button)target).Click += OnUpgradeAgainClick;
			break;
		case 2:
			((Button)target).Click += OnHelpClick;
			break;
		case 3:
			((Button)target).Click += CloseButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
