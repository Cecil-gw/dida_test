using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using TickTickUtils;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Subscribe;

public class NotionFeatureWindow : MyWindow, IComponentConnector
{
	internal Border DisplayImage;

	internal TextBlock TitleTextBlock;

	internal TextBlock ContentTextBlock;

	internal Button IntegrationButton;

	internal TextBlock GuideText;

	internal Button CloseButton;

	private bool _contentLoaded;

	public NotionFeatureWindow()
	{
		InitializeComponent();
		SetImage();
		ContentTextBlock.Text = string.Format(Utils.GetString("IntegrationWithNotionDesc"), Utils.GetAppName());
	}

	private async Task SetImage()
	{
		string text = ProChecker.SetImage(ProType.IntegrationWithNotion);
		string path = AppPaths.UpgradeProDir + "\\" + text;
		if (!File.Exists(path))
		{
			await ProChecker.TryDownloadBackground(text);
		}
		DisplayImage.Background = new ImageBrush
		{
			ImageSource = ImageUtils.GetImageByUrl(path, -1)
		};
	}

	private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (!CloseButton.IsMouseOver && !IntegrationButton.IsMouseOver && !GuideText.IsMouseOver && e.LeftButton == MouseButtonState.Pressed)
		{
			DragMove();
			e.Handled = false;
		}
	}

	private void CloseButton_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void OnIntegrationClick(object sender, RoutedEventArgs e)
	{
		Utils.TryProcessToNotionConnect(null);
	}

	private async void OnGuideClick(object sender, MouseButtonEventArgs e)
	{
		bool flag = await Communicator.NeedShowNotionPage();
		Utils.TryProcessStartUrlWithToken("/connect/notion/guide?channel=windows&lang=" + Utils.GetLanguage().ToLower() + "&hideBind=" + (flag ? "false" : "true"));
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/subscribe/notionfeaturewindow.xaml", UriKind.Relative);
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
			IntegrationButton = (Button)target;
			IntegrationButton.Click += OnIntegrationClick;
			break;
		case 5:
			GuideText = (TextBlock)target;
			GuideText.MouseLeftButtonUp += OnGuideClick;
			break;
		case 6:
			CloseButton = (Button)target;
			CloseButton.Click += CloseButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
