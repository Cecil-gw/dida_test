using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Service;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Update;

public class DownloadWindow : Window, IComponentConnector
{
	private WebClient _downloader;

	private readonly string _downloadPath;

	private long _pkgSize;

	private bool _forceUpdate;

	private AppUpdateFromType? _appUpdateFromType;

	internal TextBlock IndicatorText;

	internal DownloadProgressBar ProgressBar;

	internal Button CancalBtn;

	internal Button InstallBtn;

	internal Border CloseButton;

	private bool _contentLoaded;

	public DownloadWindow(string downloadPath, long size, AppUpdateFromType appUpdateFromType)
	{
		InitializeComponent();
		_downloadPath = downloadPath;
		_pkgSize = size;
		_appUpdateFromType = appUpdateFromType;
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private void OnWindowLoaded(object sender, RoutedEventArgs e)
	{
		StartDownload();
	}

	private async void StartDownload()
	{
		if (Utils.IsNetworkAvailable())
		{
			_downloader = new WebClient();
			if (!Directory.Exists(AppPaths.PackageDir))
			{
				Directory.CreateDirectory(AppPaths.PackageDir);
			}
			try
			{
				_downloader.DownloadProgressChanged += OnDownloadChanged;
				_downloader.DownloadFileCompleted += OnDownloadCompleted;
				await _downloader.DownloadFileTaskAsync(_downloadPath, AppPaths.PackageDir + GetPackageName());
				NotifyDownloadFinished();
			}
			catch (Exception ex)
			{
				UtilLog.Error("download failed:" + ex);
				MessageBox.Show(Utils.GetString("DownloadFailed"));
				OnCancelClick(null, null);
			}
		}
	}

	private void OnDownloadCompleted(object sender, AsyncCompletedEventArgs e)
	{
		if (e.Cancelled)
		{
			DeleteBrokenPackage();
		}
	}

	private void OnDownloadChanged(object sender, DownloadProgressChangedEventArgs e)
	{
		ProgressBar.Progress = e.ProgressPercentage;
	}

	private string GetPackageName()
	{
		return _downloadPath.Split('/').Last();
	}

	private void NotifyDownloadFinished()
	{
		CancalBtn.Visibility = Visibility.Collapsed;
		InstallBtn.Visibility = Visibility.Visible;
		IndicatorText.Text = Utils.GetString("ReadyToInstall");
	}

	private async void OnCancelClick(object sender, RoutedEventArgs e)
	{
		CancalBtn.Content = Utils.GetString("Canceling");
		CancalBtn.IsEnabled = false;
		_downloader?.CancelAsync();
		await Task.Delay(1000);
		Close();
		if (_forceUpdate)
		{
			App.Instance.Shutdown();
		}
	}

	private void DeleteBrokenPackage()
	{
		string path = AppPaths.PackageDir + GetPackageName();
		if (File.Exists(path))
		{
			try
			{
				File.Delete(path);
			}
			catch (Exception)
			{
			}
		}
	}

	private async void OnInstallClick(object sender, RoutedEventArgs e)
	{
		UtilLog.Info("DownloadWindow.OnInstallClick");
		if (_appUpdateFromType.HasValue)
		{
			await UpgradeAppService.SaveUpdateFrom(_appUpdateFromType.Value);
		}
		string text = AppPaths.PackageDir + GetPackageName();
		if (File.Exists(text))
		{
			try
			{
				Process.Start(text);
			}
			catch (Exception ex)
			{
				UtilLog.Info("DownloadWindow.OnInstallClick " + ex.Message);
			}
		}
		await Task.Delay(1000);
		Close();
		UtilLog.Info("DownloadWindow Close " + _forceUpdate);
		if (_forceUpdate)
		{
			App.Instance.Shutdown();
		}
	}

	public void SetForceUpdate()
	{
		_forceUpdate = true;
	}

	private async void CloseButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (_downloader != null)
		{
			_downloader.CancelAsync();
			await Task.Delay(1000);
		}
		Close();
		if (_forceUpdate)
		{
			App.Instance.Shutdown();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/update/downloadwindow.xaml", UriKind.Relative);
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
			((DownloadWindow)target).Loaded += OnWindowLoaded;
			break;
		case 2:
			IndicatorText = (TextBlock)target;
			break;
		case 3:
			ProgressBar = (DownloadProgressBar)target;
			break;
		case 4:
			CancalBtn = (Button)target;
			CancalBtn.Click += OnCancelClick;
			break;
		case 5:
			InstallBtn = (Button)target;
			InstallBtn.Click += OnInstallClick;
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
