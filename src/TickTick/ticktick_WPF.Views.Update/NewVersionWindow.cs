using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Update;

public class NewVersionWindow : Window, IComponentConnector
{
	public static bool ForceUpdating;

	private static NewVersionWindow _instance;

	internal TextBlock DescText;

	internal Button SkipButton;

	internal Border CloseButton;

	private bool _contentLoaded;

	public NewVersionWindow()
	{
		InitializeComponent();
	}

	public NewVersionWindow(NewVersonViewModel model, bool forceUpdate = false, string forceText = null)
	{
		InitializeComponent();
		base.DataContext = model;
		if (forceUpdate || (model.ForceVersions != null && model.ForceVersions.Contains(Utils.GetVersion())))
		{
			ForceUpdating = true;
			CloseButton.Visibility = Visibility.Collapsed;
			SkipButton.Visibility = Visibility.Collapsed;
			DescText.Inlines.Clear();
			DescText.Text = forceText ?? string.Format(Utils.GetString("VersionErrorDesc"), model.NewVersion);
			_instance?.Close();
		}
		else
		{
			_instance = this;
		}
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		if (object.Equals(_instance, this))
		{
			_instance = null;
		}
		App.Window?.LeftTabBar.SetUpgradeDisplay(0);
		base.OnClosing(e);
	}

	private void OnSkipClick(object sender, RoutedEventArgs e)
	{
		if (base.DataContext != null && base.DataContext is NewVersonViewModel newVersonViewModel)
		{
			string skipVersion = LocalSettings.Settings.SkipVersion;
			if (string.IsNullOrEmpty(skipVersion))
			{
				LocalSettings.Settings.SkipVersion = newVersonViewModel.NewVersion;
			}
			else
			{
				LocalSettings.Settings.SkipVersion = skipVersion + ";" + newVersonViewModel.NewVersion;
			}
			Close();
		}
	}

	private void OnInstallClick(object sender, RoutedEventArgs e)
	{
	}

	private void OnCloseClick(object sender, MouseButtonEventArgs e)
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
			Uri resourceLocator = new Uri("/TickTick;component/views/update/newversionwindow.xaml", UriKind.Relative);
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
			DescText = (TextBlock)target;
			break;
		case 2:
			SkipButton = (Button)target;
			SkipButton.Click += OnSkipClick;
			break;
		case 3:
			((Button)target).Click += OnInstallClick;
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
