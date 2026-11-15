using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using TickTickUtils;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Update;

public class NewVersionReminderWindow : Window, IComponentConnector
{
	private bool hasBeenShown;

	private Action onShown;

	internal System.Windows.Controls.CheckBox OtherLangCheckBox;

	internal System.Windows.Controls.CheckBox NeedExpandCheckBox;

	internal Grid MainGrid;

	internal Grid TitleGrid;

	internal TextBlock UpdateDescribeText;

	internal Border CloseButton;

	private bool _contentLoaded;

	private bool loaded { get; set; }

	private bool closeByDownload { get; set; }

	public static bool ForceUpdating { get; set; }

	private static NewVersionReminderWindow _instance { get; set; }

	private string newVersionNum { get; set; }

	public NewVersionReminderWindow(NewVersonViewModel model, bool forceUpdate = false, string forceText = null)
	{
		InitializeComponent();
		base.DataContext = model;
		newVersionNum = model.UpdateModel.versionNum.ToString();
		if (forceUpdate || (model.ForceVersions != null && model.ForceVersions.Contains(Utils.GetVersion())))
		{
			ForceUpdating = true;
			CloseButton.Visibility = Visibility.Collapsed;
			_instance?.Close();
		}
		else
		{
			_instance = this;
		}
		UpdateDescribeText.Text = string.Format(Utils.GetString("UpdateLatestVersionExperience"), "V" + model.NewVersion);
		OtherLangCheckBox.IsChecked = !Utils.IsCn();
		MainWindow window = App.Window;
		window.OnModuleSwitch = (Action<DisplayModule>)Delegate.Combine(window.OnModuleSwitch, new Action<DisplayModule>(OnModuleSwitch));
		App.Window.LocationChanged += Owner_LocationChanged;
		App.Window.SizeChanged += Owner_SizeChanged;
		App.Window.IsVisibleChanged += Owner_IsVisibleChanged;
		App.Window.StateChanged += Owner_StateChanged;
	}

	private void Window_Closing(object sender, CancelEventArgs e)
	{
		if (App.Window != null)
		{
			App.Window.LocationChanged -= Owner_LocationChanged;
			App.Window.SizeChanged -= Owner_SizeChanged;
			App.Window.IsVisibleChanged -= Owner_IsVisibleChanged;
			App.Window.StateChanged -= Owner_StateChanged;
		}
		MainWindow window = App.Window;
		window.OnModuleSwitch = (Action<DisplayModule>)Delegate.Remove(window.OnModuleSwitch, new Action<DisplayModule>(OnModuleSwitch));
		App.Window?.LeftTabBar?.SetUpgradeDisplay(0);
		MainWindow.NewVersionReminderWindow = null;
		if (!closeByDownload)
		{
			LocalSettings.Settings.ExtraSettings.LastUpdateRedDotNewVersion = newVersionNum;
			LocalSettings.Settings.ExtraSettings.LastUpdateRedDotVersion = Utils.GetVersion();
			LocalSettings.Settings.Save();
		}
	}

	public void OnModuleSwitch(DisplayModule module)
	{
		UpdateVisibility();
	}

	public void UpdateVisibility(Action onShown = null)
	{
		if (onShown != null)
		{
			if (hasBeenShown)
			{
				onShown();
			}
			else
			{
				this.onShown = onShown;
			}
		}
		if (LocalSettings.Settings.MainWindowDisplayModule == 0)
		{
			MainWindow window = App.Window;
			if (window != null && window.IsVisible && App.Window.WindowState != WindowState.Minimized)
			{
				Show();
				if (base.IsVisible)
				{
					hasBeenShown = true;
					Action action = this.onShown;
					this.onShown = null;
					action?.Invoke();
				}
				return;
			}
		}
		Hide();
	}

	private void AutoUpdateCheckBox_Checked(object sender, RoutedEventArgs e)
	{
		_ = loaded;
	}

	private void CloseButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		Close();
	}

	private async void OnInstallClick(object sender, RoutedEventArgs e)
	{
		closeByDownload = true;
		Close();
		if (base.DataContext == null || !(base.DataContext is NewVersonViewModel newVersonViewModel))
		{
			return;
		}
		string package = AppPaths.PackageDir + newVersonViewModel.DownloadPath.Split('/').Last();
		try
		{
			long valueOrDefault = ((!newVersonViewModel.DownloadPath.Contains("x64")) ? newVersonViewModel.SizeModel?.x86 : newVersonViewModel.SizeModel?.x64).GetValueOrDefault();
			if (File.Exists(package) && new FileInfo(package).Length >= valueOrDefault)
			{
				await UpgradeAppService.SaveUpdateFrom(AppUpdateFromType.Notification);
				Process.Start(package);
				if (ForceUpdating)
				{
					App.Instance.Shutdown();
				}
			}
			else
			{
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
		}
		catch (Exception)
		{
		}
	}

	private void Window_Loaded(object sender, RoutedEventArgs e)
	{
		loaded = true;
		UpdateReminderPosition();
		UpdateVisibility();
	}

	private void Owner_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		UpdateVisibility();
	}

	private void Owner_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		UpdateReminderPosition();
	}

	private void Owner_LocationChanged(object sender, EventArgs e)
	{
		UpdateReminderPosition();
	}

	private async void Owner_StateChanged(object sender, EventArgs e)
	{
		UpdateReminderPosition();
		UpdateVisibility();
	}

	private void UpdateReminderPosition()
	{
		if (base.Owner != null)
		{
			Rect windowRectangle = GetWindowRectangle(base.Owner);
			base.Left = windowRectangle.Left + 48.0;
			base.Top = windowRectangle.Top + windowRectangle.Height - base.Height + 2.0;
		}
	}

	public static Rect GetWindowRectangle(Window w)
	{
		if (w.WindowState == WindowState.Maximized)
		{
			Rectangle workingArea = Screen.FromHandle(new WindowInteropHelper(w).Handle).WorkingArea;
			double pixelsPerDip = VisualTreeHelper.GetDpi(w).PixelsPerDip;
			return new Rect((double)workingArea.Left / pixelsPerDip, (double)workingArea.Top / pixelsPerDip, (double)workingArea.Width / pixelsPerDip, (double)workingArea.Height / pixelsPerDip);
		}
		return new Rect((int)w.Left, (int)w.Top, (int)w.ActualWidth, (int)w.ActualHeight);
	}

	private void Window_ContentRendered(object sender, EventArgs e)
	{
		bool value = MainGrid.ActualWidth - TitleGrid.ActualWidth < 24.0;
		NeedExpandCheckBox.IsChecked = value;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/update/newversionreminderwindow.xaml", UriKind.Relative);
			System.Windows.Application.LoadComponent(this, resourceLocator);
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
			((NewVersionReminderWindow)target).Closing += Window_Closing;
			((NewVersionReminderWindow)target).Loaded += Window_Loaded;
			((NewVersionReminderWindow)target).ContentRendered += Window_ContentRendered;
			break;
		case 2:
			OtherLangCheckBox = (System.Windows.Controls.CheckBox)target;
			break;
		case 3:
			NeedExpandCheckBox = (System.Windows.Controls.CheckBox)target;
			break;
		case 4:
			MainGrid = (Grid)target;
			break;
		case 5:
			TitleGrid = (Grid)target;
			break;
		case 6:
			UpdateDescribeText = (TextBlock)target;
			break;
		case 7:
			((System.Windows.Controls.Button)target).Click += OnInstallClick;
			break;
		case 8:
			CloseButton = (Border)target;
			CloseButton.MouseLeftButtonUp += CloseButton_MouseLeftButtonUp;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
