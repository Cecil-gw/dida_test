using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using TickTickUtils;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Update;

public class VersionNewsWindow : Window, IComponentConnector
{
	internal Border Border1;

	internal Border CloseButton;

	private bool _contentLoaded;

	private EpicUpdateModel viewModel { get; set; }

	public VersionNewsWindow(ReleaseNoteDetailModel detailModel)
	{
		InitializeComponent();
		base.DataContext = (viewModel = detailModel.Epic);
		DataChangedNotifier.IsDarkChanged += OnIsDarkChanged;
	}

	protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
	{
		if (e.Source == this)
		{
			base.OnMouseLeftButtonDown(e);
			DragMove();
		}
	}

	private void OnCloseClick(object sender, MouseButtonEventArgs e)
	{
		Close();
	}

	private void OpenLinkButton_Click(object sender, RoutedEventArgs e)
	{
		if (!string.IsNullOrEmpty(viewModel?.Link?.Url))
		{
			if (viewModel?.Link?.OpenType == "app")
			{
				if (viewModel.Link.Url == "ai")
				{
					App.NavigateAI();
				}
			}
			else
			{
				Utils.TryProcessStartUrl(viewModel.Link.Url);
			}
		}
		Close();
	}

	private void Window_Closing(object sender, CancelEventArgs e)
	{
		DataChangedNotifier.IsDarkChanged -= OnIsDarkChanged;
	}

	private void OnIsDarkChanged(object sender, EventArgs e)
	{
		string imageFilePath = Path.Combine(AppPaths.ImageDir, "epic_image_temp.png");
		string text = Path.Combine(AppPaths.ImageDir, "epic_image_dark_temp.png");
		if (ThemeUtil.IsDark() && File.Exists(text))
		{
			viewModel.ImageFilePath = text;
		}
		else
		{
			viewModel.ImageFilePath = imageFilePath;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/update/versionnewswindow.xaml", UriKind.Relative);
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
			((VersionNewsWindow)target).Closing += Window_Closing;
			break;
		case 2:
			Border1 = (Border)target;
			break;
		case 3:
			((Button)target).Click += OpenLinkButton_Click;
			break;
		case 4:
			CloseButton = (Border)target;
			CloseButton.MouseLeftButtonUp += OnCloseClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
