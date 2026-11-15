using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using TickTickUtils;

namespace ticktick_WPF.Views.Subscribe;

public class WeChatDetailWindow : MyWindow, IOkCancelWindow, IComponentConnector
{
	internal Border DisplayImage;

	internal TextBlock TitleTextBlock;

	internal TextBlock ContentTextBlock;

	internal Grid HoverGrid;

	internal Border HoverButton;

	internal TextBlock HoverText;

	internal Border HoverTooltip;

	internal Image QRImage;

	internal TextBlock HelpText;

	internal Button CloseButton;

	private bool _contentLoaded;

	public WeChatDetailWindow()
	{
		InitializeComponent();
	}

	public WeChatDetailWindow(bool isHelper)
	{
		InitializeComponent();
		TitleTextBlock.Text = (isHelper ? "微信小助手" : "微信公众号");
		HoverText.Text = (isHelper ? "添加小助手" : "关注公众号");
		ContentTextBlock.Text = (isHelper ? "在微信中添加「微信小助手」，即可实现“聊天时转发消息创建任务”" : "关注滴答公众号，像聊天一样在公众号中发送文字、语音或图片，快速创建任务。也可以开启微信提醒，不再错过任何一个任务。");
		string text = (isHelper ? "WeChatHelper.png" : "WeChatOfficialAccount.png");
		DisplayImage.Background = new ImageBrush
		{
			ImageSource = ImageUtils.GetResourceImage("pack://application:,,,/Assets/ImageSource/" + text)
		};
		QRImage.Source = ImageUtils.GetResourceImage("pack://application:,,,/Assets/ImageSource/" + (isHelper ? "WeChatHelperQR.png" : "WeChatOfficialAccountQR.png"));
		if (isHelper)
		{
			HelpText.Visibility = Visibility.Visible;
		}
	}

	private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (!CloseButton.IsMouseOver && e.LeftButton == MouseButtonState.Pressed)
		{
			DragMove();
			e.Handled = false;
		}
	}

	private void CloseButton_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	public void OnCancel()
	{
		Close();
	}

	public void Ok()
	{
		Close();
	}

	private void OnMouseMove(object sender, MouseEventArgs e)
	{
		if (!HoverTooltip.IsMouseOver && !HoverGrid.IsMouseOver)
		{
			HoverTooltip.Visibility = Visibility.Collapsed;
		}
		else
		{
			HoverTooltip.Visibility = Visibility.Visible;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/subscribe/wechatdetailwindow.xaml", UriKind.Relative);
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
			DisplayImage = (Border)target;
			break;
		case 2:
			TitleTextBlock = (TextBlock)target;
			break;
		case 3:
			ContentTextBlock = (TextBlock)target;
			break;
		case 4:
			HoverGrid = (Grid)target;
			break;
		case 5:
			HoverButton = (Border)target;
			break;
		case 6:
			HoverText = (TextBlock)target;
			break;
		case 7:
			HoverTooltip = (Border)target;
			break;
		case 8:
			QRImage = (Image)target;
			break;
		case 9:
			HelpText = (TextBlock)target;
			break;
		case 10:
			CloseButton = (Button)target;
			CloseButton.Click += CloseButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
