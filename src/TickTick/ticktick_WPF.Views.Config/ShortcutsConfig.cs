using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Config;

public class ShortcutsConfig : UserControl, IComponentConnector
{
	private string _addTaskHotKeyString;

	private string _lockHotKeyString;

	private string _openOrCloseHotKeyString;

	private string _pinHotKeyString;

	internal StackPanel TitlePanel;

	internal Border HelpBorder;

	internal ListView ShortcutList;

	private bool _contentLoaded;

	public ShortcutsConfig(bool inSettings = true)
	{
		InitializeComponent();
		InitShortcuts();
		if (inSettings)
		{
			TitlePanel.Visibility = Visibility.Collapsed;
		}
		else
		{
			TitlePanel.Visibility = Visibility.Visible;
		}
		base.Loaded += delegate
		{
			ShortcutList.ScrollIntoView((ShortcutList.ItemsSource as IEnumerable<ShortcutViewModel>).FirstOrDefault());
		};
	}

	public void InitShortcuts()
	{
		ShortcutList.ItemsSource = ShortcutViewModel.BuildShortcuts();
	}

	private void OnClearShortcut(object sender, EventArgs e)
	{
		if (sender is HotkeyControl { DataContext: ShortcutViewModel dataContext } hotkeyControl)
		{
			dataContext.Shortcut = hotkeyControl.CurrentHotkey.ToString().Replace(" ", "");
			dataContext.ClearShortCut();
			SettingsHelper.ShortCutChanged = true;
		}
	}

	private void OnShortcutChanged(object sender, EventArgs e)
	{
		if (sender is HotkeyControl { DataContext: ShortcutViewModel dataContext } hotkeyControl)
		{
			dataContext.SetShortCut(hotkeyControl.CurrentHotkey.ToRealString().Replace(" ", ""));
		}
	}

	private void OnIndicatorClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is Grid { Tag: string tag } && LocalSettings.Settings[tag] is bool flag)
		{
			LocalSettings.Settings[tag] = !flag;
		}
	}

	private void ResetAllShortcut(object sender, RoutedEventArgs e)
	{
		LocalSettings.Settings.ShortcutAddTask = "Alt+Shift+A";
		LocalSettings.Settings.ShortcutOpenOrClose = "Ctrl+Shift+E";
		LocalSettings.Settings.ShortcutPin = "";
		LocalSettings.Settings.LockShortcut = "Ctrl+Shift+L";
		LocalSettings.Settings.PomoShortcut = "Ctrl+Alt+P";
		LocalSettings.Settings.CreateStickyShortCut = "";
		LocalSettings.Settings.ShowHideStickyShortCut = "";
		LocalSettings.Settings.ShortCutModel.Reset();
		InitShortcuts();
		Utils.FindParent<SettingDialog>(this)?.Toast(Utils.GetString("SettingsRestored"));
	}

	private void OnMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (Utils.GetMousePointElement<HotkeyControl>(e, ShortcutList) == null)
		{
			Window window = Window.GetWindow(this);
			if (window != null)
			{
				FocusManager.SetFocusedElement(window, window);
				Keyboard.Focus(window);
			}
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/config/shortcutsconfig.xaml", UriKind.Relative);
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
			TitlePanel = (StackPanel)target;
			break;
		case 2:
			HelpBorder = (Border)target;
			break;
		case 3:
			ShortcutList = (ListView)target;
			ShortcutList.MouseLeftButtonUp += OnMouseUp;
			break;
		case 4:
			((Button)target).Click += ResetAllShortcut;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
