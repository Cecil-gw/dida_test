using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using TickTickUtils;
using ticktick_WPF.Service;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Update;

public class NewVersionDetailWindow : Window, IComponentConnector
{
	public static bool ForceUpdating;

	private static NewVersionDetailWindow _instance;

	internal CheckBox IsForceCheckBox;

	internal TextBlock VersionInfoText;

	internal Border CloseButton;

	private bool _contentLoaded;

	private bool closeByDownload { get; set; }

	private bool loaded { get; set; }

	public NewVersionDetailWindow(NewVersonViewModel model, bool forceUpdate = false, string forceText = null)
	{
		InitializeComponent();
		base.DataContext = model;
		VersionInfoText.Text = string.Format(Utils.GetString("CurrentLastestVersion"), model.OldVersion, model.NewVersion);
		if (forceUpdate || (model.ForceVersions != null && model.ForceVersions.Contains(Utils.GetVersion())))
		{
			ForceUpdating = true;
			CloseButton.Visibility = Visibility.Collapsed;
			VersionInfoText.Text = forceText ?? string.Format(Utils.GetString("VersionErrorDesc"), "V" + model.NewVersion);
			IsForceCheckBox.IsChecked = true;
			_instance?.Close();
		}
		else
		{
			_instance = this;
		}
	}

	protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
	{
		if (e.Source == this)
		{
			base.OnMouseLeftButtonDown(e);
			DragMove();
		}
	}

	private void Window_Loaded(object sender, RoutedEventArgs e)
	{
		loaded = true;
	}

	private void Window_Closing(object sender, CancelEventArgs e)
	{
		App.Window?.LeftTabBar.SetUpgradeDisplay(0);
	}

	private void AutoUpdateCheckBox_Checked(object sender, RoutedEventArgs e)
	{
		_ = loaded;
	}

	private void CloseButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		Close();
	}

	private void ViewNewsButton_Click(object sender, RoutedEventArgs e)
	{
		Utils.TryProcessStartUrl(Utils.IsDida() ? "https://help.dida365.com/articles/6950689674532749312" : "https://help.ticktick.com/articles/7082552170989486080");
	}

	private void OnInstallClick(object sender, RoutedEventArgs e)
	{
		closeByDownload = true;
		Close();
		if (base.DataContext == null || !(base.DataContext is NewVersonViewModel newVersonViewModel))
		{
			return;
		}
		string text = AppPaths.PackageDir + newVersonViewModel.DownloadPath.Split('/').Last();
		try
		{
			long valueOrDefault = ((!newVersonViewModel.DownloadPath.Contains("x64")) ? newVersonViewModel.SizeModel?.x86 : newVersonViewModel.SizeModel?.x64).GetValueOrDefault();
			if (File.Exists(text) && new FileInfo(text).Length >= valueOrDefault)
			{
				Process.Start(text);
				if (ForceUpdating)
				{
					App.Instance.Shutdown();
				}
				return;
			}
			DownloadWindow downloadWindow = new DownloadWindow(newVersonViewModel.DownloadPath, valueOrDefault, AppUpdateFromType.Notification);
			if (ForceUpdating)
			{
				downloadWindow.SetForceUpdate();
				downloadWindow.ShowDialog();
			}
			else
			{
				downloadWindow.Show();
			}
		}
		catch (Exception)
		{
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/update/newversiondetailwindow.xaml", UriKind.Relative);
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
			((NewVersionDetailWindow)target).Loaded += Window_Loaded;
			((NewVersionDetailWindow)target).Closing += Window_Closing;
			break;
		case 2:
			IsForceCheckBox = (CheckBox)target;
			break;
		case 3:
			VersionInfoText = (TextBlock)target;
			break;
		case 4:
			((Button)target).Click += ViewNewsButton_Click;
			break;
		case 5:
			((Button)target).Click += OnInstallClick;
			break;
		case 6:
			CloseButton = (Border)target;
			CloseButton.MouseLeftButtonUp += CloseButton_MouseLeftButtonUp;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
