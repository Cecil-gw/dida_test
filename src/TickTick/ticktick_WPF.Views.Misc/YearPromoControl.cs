using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using ticktick_WPF.Properties;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc;

public class YearPromoControl : Border, IComponentConnector
{
	private string _url;

	internal YearPromoControl YearPromoBorder;

	internal Image YearPromoImage;

	private bool _contentLoaded;

	public YearPromoControl()
	{
		InitializeComponent();
	}

	private void OnCloseYearPromoClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		base.Visibility = Visibility.Collapsed;
		Settings settings = Settings.Default;
		settings.YearPromoClosedUsers = settings.YearPromoClosedUsers + ";" + LocalSettings.Settings.LoginUserId;
		Settings.Default.Save();
	}

	private void OnYearPromoClick(object sender, MouseButtonEventArgs e)
	{
		if (!string.IsNullOrEmpty(_url))
		{
			UserActCollectUtils.AddClickEvent("2021_report", "click", "");
			Utils.TryProcessStartUrl(_url);
		}
	}

	public void SetImageAndUrl(BitmapImage bitmapImage, string url)
	{
		YearPromoImage.Source = bitmapImage;
		_url = url;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/misc/yearpromocontrol.xaml", UriKind.Relative);
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
			YearPromoBorder = (YearPromoControl)target;
			YearPromoBorder.MouseLeftButtonUp += OnYearPromoClick;
			break;
		case 2:
			YearPromoImage = (Image)target;
			break;
		case 3:
			((Border)target).MouseLeftButtonUp += OnCloseYearPromoClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
