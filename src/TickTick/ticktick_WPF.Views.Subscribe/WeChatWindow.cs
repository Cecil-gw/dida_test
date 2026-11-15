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
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Subscribe;

public class WeChatWindow : MyWindow, IOkCancelWindow, IComponentConnector
{
	internal Border DisplayImage;

	internal ImageBrush Brush;

	internal Button CloseButton;

	private bool _contentLoaded;

	public WeChatWindow()
	{
		InitializeComponent();
		Brush.ImageSource = ImageUtils.GetResourceImage("pack://application:,,,/Assets/ImageSource/" + (ThemeUtil.IsDark() ? "dark/" : "light/") + "WeChatWindow.png");
	}

	private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (!CloseButton.IsMouseOver && DisplayImage.IsMouseOver && e.LeftButton == MouseButtonState.Pressed)
		{
			DragMove();
			e.Handled = false;
		}
	}

	private void CloseButton_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void OnAddAccountClick(object sender, MouseButtonEventArgs e)
	{
		WeChatDetailWindow weChatDetailWindow = new WeChatDetailWindow(isHelper: false);
		weChatDetailWindow.Owner = Window.GetWindow(this);
		weChatDetailWindow.ShowDialog();
	}

	private void OnAddHelperClick(object sender, MouseButtonEventArgs e)
	{
		WeChatDetailWindow weChatDetailWindow = new WeChatDetailWindow(isHelper: true);
		weChatDetailWindow.Owner = Window.GetWindow(this);
		weChatDetailWindow.ShowDialog();
	}

	public void OnCancel()
	{
		Close();
	}

	public void Ok()
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
			Uri resourceLocator = new Uri("/TickTick;component/views/subscribe/wechatwindow.xaml", UriKind.Relative);
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
			Brush = (ImageBrush)target;
			break;
		case 3:
			((TextBlock)target).MouseLeftButtonUp += OnAddAccountClick;
			break;
		case 4:
			((TextBlock)target).MouseLeftButtonUp += OnAddHelperClick;
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
