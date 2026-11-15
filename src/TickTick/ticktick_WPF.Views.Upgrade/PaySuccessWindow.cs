using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Upgrade;

public class PaySuccessWindow : Window, IComponentConnector
{
	internal TextBlock ProInfoText;

	internal Button TickUpdateNowButton;

	private bool _contentLoaded;

	public PaySuccessWindow(DateTime endDate)
	{
		InitializeComponent();
		ProInfoText.Text = string.Format(Utils.GetString("payment_return_end_date"), DateUtils.FormatYearMonthDay(endDate, DateFormatLen.Short));
	}

	private void CloseButton_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void OnUseClick(object sender, MouseButtonEventArgs e)
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
			Uri resourceLocator = new Uri("/TickTick;component/views/upgrade/paysuccesswindow.xaml", UriKind.Relative);
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
			ProInfoText = (TextBlock)target;
			break;
		case 2:
			TickUpdateNowButton = (Button)target;
			TickUpdateNowButton.PreviewMouseLeftButtonUp += OnUseClick;
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
