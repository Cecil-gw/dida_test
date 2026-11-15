using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views;

public class UnhandledExceptionWindow : Window, IComponentConnector
{
	private const string MailToTemplate = "mailto:{0}?subject={1}&body={2}";

	private static string _exceptionMessage = "";

	private bool _needDownLoad;

	private readonly bool _restart;

	private bool _closingForNativeFallback;

	internal TextBlock ExceptionTitle;

	internal TextBox ExceptionMessageTextBlock;

	internal Button sendButton;

	internal Button exitButton;

	private bool _contentLoaded;

	public UnhandledExceptionWindow()
	{
		InitializeComponent();
	}

	public UnhandledExceptionWindow(Exception e, bool restart = false)
	{
		base.Topmost = true;
		InitializeComponent();
		_exceptionMessage = ExceptionUtils.BuildExceptionMessage(e);
		UtilLog.Error(_exceptionMessage);
		ExceptionMessageTextBlock.Text = _exceptionMessage;
		base.Title = Utils.GetString("ProgramUnhandledException");
		_restart = restart;
		sendButton.Content = Utils.GetString("Restart");
		if (NeedsDownload(e))
		{
			sendButton.Content = Utils.GetString("GoToDownload");
			exitButton.Content = Utils.GetString("SendLog");
			ExceptionTitle.Text = Utils.GetString("RedownloadMessage");
			_needDownLoad = true;
		}
	}

	public override async void OnApplyTemplate()
	{
		if (LocalSettings.Settings != null)
		{
			try
			{
				await LocalSettings.Settings.Save();
			}
			catch
			{
			}
		}
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	internal static bool NeedsDownload(Exception e)
	{
		string text = (e.InnerException?.ToString() ?? string.Empty) + e.Message;
		if (text.Contains("Version=") && text.Contains("Culture="))
		{
			return text.Contains("PublicKeyToken=");
		}
		return false;
	}

	private void sendButton_Click(object sender, RoutedEventArgs e)
	{
		HandlePrimaryAction(_needDownLoad, _restart);
	}

	internal static void HandlePrimaryAction(bool needDownload, bool restart)
	{
		if (needDownload)
		{
			Utils.TryProcessStartUrl("https://" + BaseUrl.Domain + "/about/windows");
		}
		else if (restart)
		{
			App.Instance.Restart();
		}
		else
		{
			OpenFeedback();
		}
	}

	private static void OpenFeedback()
	{
		try
		{
			int dotNetReleaseKey = Utils.GetDotNetReleaseKey();
			UtilLog.Info("db size : " + new FileInfo(AppPaths.TickTickDbPath).Length);
			UtilLog.Info("dotnet releaseKey : " + dotNetReleaseKey);
			UtilLog.Info("taskCount : " + TaskCache.LocalTaskViewModels.Count);
		}
		catch (Exception)
		{
		}
		NavigateWebBrowserWindow navigateWebBrowserWindow = new NavigateWebBrowserWindow("/v3/tickets/");
		navigateWebBrowserWindow.Topmost = true;
		navigateWebBrowserWindow.Closed += async delegate
		{
			await LocalSettings.Settings.Save();
			Application.Current?.Shutdown();
		};
		navigateWebBrowserWindow.Show();
	}

	private async void exitButton_Click(object sender, RoutedEventArgs e)
	{
		await HandleSecondaryAction(_needDownLoad);
	}

	internal static async Task HandleSecondaryAction(bool needDownload)
	{
		if (needDownload)
		{
			OpenFeedback();
			return;
		}
		await LocalSettings.Settings.Save();
		Application.Current?.Shutdown();
	}

	internal void CloseForNativeFallback()
	{
		_closingForNativeFallback = true;
		Close();
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		if (!_closingForNativeFallback)
		{
			Application.Current?.Shutdown();
		}
		base.OnClosing(e);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/unhandledexceptionwindow.xaml", UriKind.Relative);
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
			ExceptionTitle = (TextBlock)target;
			break;
		case 2:
			ExceptionMessageTextBlock = (TextBox)target;
			break;
		case 3:
			sendButton = (Button)target;
			sendButton.Click += sendButton_Click;
			break;
		case 4:
			exitButton = (Button)target;
			exitButton.Click += exitButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
