using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using TickTickUtils;
using XamlAnimatedGif;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views;

public class FeatureInfoWindow : MyWindow, IOkCancelWindow, IComponentConnector
{
	internal Border DisplayImage;

	internal Image GifImage;

	internal PathFigure InsidePath;

	internal Button CommandButton;

	private bool _contentLoaded;

	public FeatureInfoWindow()
	{
		InitializeComponent();
		base.Loaded += OnLoaded;
	}

	public void Ok()
	{
		Close();
	}

	public void OnCancel()
	{
		Close();
	}

	private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed)
		{
			DragMove();
			e.Handled = false;
		}
	}

	private void CloseButton_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	internal static async Task Show(string title, string info1, string info2, string name, string cTitle, Action act, bool isGif)
	{
		_ = string.Empty;
		name += ((!isGif) ? ".png" : ".gif");
		string text = ((!isGif) ? AppPaths.UpgradeProDir : AppPaths.ResourceDir);
		string imagePath = text + name;
		string remotePath = "https://" + BaseUrl.GetPullDomain() + "/windows/static/" + name;
		await IOUtils.CheckResourceExist(text, name, remotePath);
		FeatureInfoWindowViewModel dataContext = new FeatureInfoWindowViewModel(title, info1, info2, imagePath, cTitle, act);
		FeatureInfoWindow featureInfoWindow = new FeatureInfoWindow();
		featureInfoWindow.Owner = App.Window;
		featureInfoWindow.DataContext = dataContext;
		featureInfoWindow.ShowDialog();
	}

	private async void OnLoaded(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is FeatureInfoWindowViewModel featureInfoWindowViewModel)
		{
			GifImage.SetValue(AnimationBehavior.SourceUriProperty, new Uri(featureInfoWindowViewModel.GifPath));
		}
	}

	private void OnCommandClick(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is FeatureInfoWindowViewModel featureInfoWindowViewModel)
		{
			featureInfoWindowViewModel.DoAction();
		}
		Close();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/featureinfowindow.xaml", UriKind.Relative);
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
			GifImage = (Image)target;
			break;
		case 3:
			InsidePath = (PathFigure)target;
			break;
		case 4:
			CommandButton = (Button)target;
			CommandButton.Click += OnCommandClick;
			break;
		case 5:
			((Button)target).Click += CloseButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
