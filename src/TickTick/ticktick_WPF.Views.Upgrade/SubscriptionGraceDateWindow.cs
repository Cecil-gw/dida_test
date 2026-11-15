using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Upgrade;

public class SubscriptionGraceDateWindow : MyWindow, IComponentConnector
{
	private readonly string _type;

	internal TextBlock ContentTextBlock;

	internal Run GraceDateRun;

	internal Run LearnMoreRun;

	internal Button GotItButton;

	internal Button CloseButton;

	private bool _contentLoaded;

	public SubscriptionGraceDateWindow(string type, int day)
	{
		InitializeComponent();
		_type = type;
		GraceDateRun.Text = string.Format(Utils.GetString("SubscriptionGraceDateMessage"), type, day) + ((Utils.IsCn() || Utils.IsJp()) ? "" : " ");
	}

	private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (!CloseButton.IsMouseOver && !GotItButton.IsMouseOver && !LearnMoreRun.IsMouseOver && e.LeftButton == MouseButtonState.Pressed)
		{
			DragMove();
			e.Handled = false;
		}
	}

	private void OnLearnMoreClick(object sender, MouseButtonEventArgs e)
	{
		Utils.GotoPaySubscriptionHelp(_type == "Apple");
		Close();
	}

	private void OnGotItClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void CloseButton_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/upgrade/subscriptiongracedatewindow.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	internal Delegate _CreateDelegate(Type delegateType, string handler)
	{
		return Delegate.CreateDelegate(delegateType, this, handler);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			ContentTextBlock = (TextBlock)target;
			break;
		case 2:
			GraceDateRun = (Run)target;
			GraceDateRun.MouseLeftButtonUp += OnLearnMoreClick;
			break;
		case 3:
			LearnMoreRun = (Run)target;
			LearnMoreRun.MouseLeftButtonUp += OnLearnMoreClick;
			break;
		case 4:
			GotItButton = (Button)target;
			GotItButton.Click += OnGotItClick;
			break;
		case 5:
			CloseButton = (Button)target;
			CloseButton.Click += CloseButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
