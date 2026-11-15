using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using NHotkey.Wpf;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Config;
using ticktick_WPF.Views.Lock;

namespace ticktick_WPF.Views.Setting;

public class LockSettings : UserControl, IComponentConnector
{
	private string _cacheShortcut;

	private AppLockModel _model;

	internal CheckBox StartOnWindowsCheckBox;

	internal Grid StartToTrayGrid;

	internal CheckBox StartOnWindowsMiniCheckBox;

	internal TextBlock SetPasswordButton;

	internal TextBlock ClearPasswordButton;

	internal StackPanel LockSettingsPanel;

	internal CheckBox MinLockCheckBox;

	internal CheckBox LockAfterCheckBox;

	internal CountSpinner CountSpinner;

	internal CheckBox LockWidgetCheckBox;

	internal HotkeyControl LockHotkey;

	private bool _contentLoaded;

	public LockSettings()
	{
		InitializeComponent();
		InitData();
		base.Unloaded += OnUnloaded;
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		SaveLock();
	}

	private async void InitData()
	{
		_model = (await AppLockDao.GetLockConfig()) ?? new AppLockModel
		{
			LockInterval = 5,
			MinLock = true
		};
		base.DataContext = new LockViewModel(_model);
		_cacheShortcut = LocalSettings.Settings.LockShortcut;
		await InitPasswordButton();
		LockHotkey.SetHotkey(_cacheShortcut);
		LockHotkey.HotkeyChanged += OnHotKeyChanged;
		LockHotkey.HotkeyClear += OnHotKeyClear;
		LockHotkey.SetNormalMode();
		StartOnWindowsCheckBox.IsChecked = ShortcutUtils.IsShortcutExit();
		if (!StartOnWindowsCheckBox.IsChecked.Value)
		{
			StartToTrayGrid.Visibility = Visibility.Collapsed;
		}
		StartOnWindowsMiniCheckBox.IsChecked = (LocalSettings.Settings.Common.ShowWhenStart == 0 && !string.IsNullOrWhiteSpace(ShortcutUtils.GetShutcutArgs())) || LocalSettings.Settings.Common.ShowWhenStart == 2;
	}

	private void StartOnWindowsCheckBoxPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		Mouse.Capture(null);
		CheckBox obj = (CheckBox)sender;
		CheckBox obj2 = (CheckBox)sender;
		obj.IsChecked = ((obj2 == null) ? ((bool?)null) : (!obj2.IsChecked));
		e.Handled = true;
		CheckBox obj3 = (CheckBox)sender;
		if (obj3 != null && obj3.IsChecked == true)
		{
			if (ShortcutUtils.CreatShortCut("", "-hide"))
			{
				StartToTrayGrid.Visibility = Visibility.Visible;
			}
		}
		else
		{
			StartToTrayGrid.Visibility = Visibility.Collapsed;
			ShortcutUtils.DeleteShutCut("", "滴答清单");
			ShortcutUtils.DeleteShutCut("", "TickTick");
		}
	}

	private void StartOnWindowsMiniCheckBoxPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		Mouse.Capture(null);
		CheckBox obj = (CheckBox)sender;
		CheckBox obj2 = (CheckBox)sender;
		obj.IsChecked = ((obj2 == null) ? ((bool?)null) : (!obj2.IsChecked));
		e.Handled = true;
		CommonSettings common = LocalSettings.Settings.Common;
		CheckBox obj3 = (CheckBox)sender;
		common.ShowWhenStart = ((obj3 == null || obj3.IsChecked != true) ? 1 : 2);
		LocalSettings.Settings.Save();
	}

	private void OnHotKeyClear(object sender, EventArgs e)
	{
		_cacheShortcut = string.Empty;
		LocalSettings.Settings.LockShortcut = string.Empty;
		Utils.FindParent<SettingDialog>(this)?.SetChanged();
		HotkeyManager.Current.Remove("LockShortcut");
	}

	private void OnHotKeyChanged(object sender, EventArgs e)
	{
		_cacheShortcut = LockHotkey.CurrentHotkey.ToString().Replace(" ", "");
		LocalSettings.Settings.LockShortcut = _cacheShortcut;
		Utils.FindParent<SettingDialog>(this)?.SetChanged();
		HotkeyManager.Current.Remove("LockShortcut");
		HotKeyUtils.ResignHotKey("LockShortcut");
	}

	private async Task InitPasswordButton()
	{
		string value = await AppLockCache.GetLockPassword();
		SetPasswordButton.Text = Utils.GetString((!string.IsNullOrEmpty(value)) ? "ChangePassword" : "SetPassword");
		ClearPasswordButton.Visibility = (string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible);
		SetLockPanelEnabled(!string.IsNullOrEmpty(value));
	}

	private void SetLockPanelEnabled(bool isEnabled)
	{
		LockSettingsPanel.Visibility = ((!isEnabled) ? Visibility.Collapsed : Visibility.Visible);
	}

	public async Task SaveLock()
	{
		AppLockModel model = new AppLockModel
		{
			MinLock = (MinLockCheckBox.IsChecked == true),
			LockAfter = (LockAfterCheckBox.IsChecked == true),
			LockWidget = (LockWidgetCheckBox.IsChecked == true),
			LockInterval = CountSpinner.Count
		};
		if (_model == null || !_model.Equal(model))
		{
			_model = model;
			await AppLockDao.SaveLockConfig(model);
		}
	}

	private async void SetPasswordClick(object sender, MouseButtonEventArgs e)
	{
		SetPasswordWindow setPasswordWindow = new SetPasswordWindow((!string.IsNullOrEmpty(await AppLockCache.GetLockPassword())) ? SetPasswordMode.Modify : SetPasswordMode.Init);
		setPasswordWindow.Owner = Window.GetWindow(this);
		setPasswordWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
		setPasswordWindow.ShowDialog();
		await InitPasswordButton();
	}

	private async void ClearPasswordClick(object sender, MouseButtonEventArgs e)
	{
		SetPasswordWindow setPasswordWindow = new SetPasswordWindow(SetPasswordMode.Clear);
		setPasswordWindow.Owner = Window.GetWindow(this);
		setPasswordWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
		setPasswordWindow.ShowDialog();
		await InitPasswordButton();
	}

	private void OnLockMinClick(object sender, MouseButtonEventArgs e)
	{
		MinLockCheckBox.IsChecked = !MinLockCheckBox.IsChecked;
		SaveLock();
	}

	private void OnAutoLockClick(object sender, MouseButtonEventArgs e)
	{
		LockAfterCheckBox.IsChecked = !LockAfterCheckBox.IsChecked;
		SaveLock();
	}

	private void OnLockWidgetClick(object sender, MouseButtonEventArgs e)
	{
		LockWidgetCheckBox.IsChecked = !LockWidgetCheckBox.IsChecked;
		SaveLock();
	}

	private void OnShortCutClick(object sender, MouseButtonEventArgs e)
	{
		LockHotkey.TryFocus();
		e.Handled = true;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/setting/locksettings.xaml", UriKind.Relative);
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
			StartOnWindowsCheckBox = (CheckBox)target;
			StartOnWindowsCheckBox.PreviewMouseLeftButtonUp += StartOnWindowsCheckBoxPreviewMouseLeftButtonUp;
			break;
		case 2:
			StartToTrayGrid = (Grid)target;
			break;
		case 3:
			StartOnWindowsMiniCheckBox = (CheckBox)target;
			StartOnWindowsMiniCheckBox.PreviewMouseLeftButtonUp += StartOnWindowsMiniCheckBoxPreviewMouseLeftButtonUp;
			break;
		case 4:
			SetPasswordButton = (TextBlock)target;
			SetPasswordButton.MouseLeftButtonUp += SetPasswordClick;
			break;
		case 5:
			ClearPasswordButton = (TextBlock)target;
			ClearPasswordButton.MouseLeftButtonUp += ClearPasswordClick;
			break;
		case 6:
			LockSettingsPanel = (StackPanel)target;
			break;
		case 7:
			MinLockCheckBox = (CheckBox)target;
			break;
		case 8:
			((TextBlock)target).MouseLeftButtonUp += OnLockMinClick;
			break;
		case 9:
			LockAfterCheckBox = (CheckBox)target;
			break;
		case 10:
			((TextBlock)target).MouseLeftButtonUp += OnAutoLockClick;
			break;
		case 11:
			CountSpinner = (CountSpinner)target;
			break;
		case 12:
			((TextBlock)target).MouseLeftButtonUp += OnAutoLockClick;
			break;
		case 13:
			LockWidgetCheckBox = (CheckBox)target;
			break;
		case 14:
			((TextBlock)target).MouseLeftButtonUp += OnLockWidgetClick;
			break;
		case 15:
			LockHotkey = (HotkeyControl)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
