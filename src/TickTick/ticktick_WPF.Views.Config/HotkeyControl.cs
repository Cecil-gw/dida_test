using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Config;

public class HotkeyControl : UserControl, IComponentConnector
{
	internal HotkeyControl Root;

	internal TextBox HotkeyTextBox;

	internal TextBlock HotKeyTextBlock;

	internal Grid DeleteGrid;

	private bool _contentLoaded;

	public HotkeyModel CurrentHotkey { get; private set; }

	public event EventHandler HotkeyChanged;

	public event EventHandler HotkeyClear;

	protected virtual void OnHotkeyChanged()
	{
		this.HotkeyChanged?.Invoke(this, EventArgs.Empty);
	}

	public HotkeyControl()
	{
		InitializeComponent();
	}

	private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (e.NewValue is ShortcutViewModel { Editable: not false } shortcutViewModel)
		{
			SetHotkey(shortcutViewModel.Shortcut);
		}
	}

	public void SetNormalMode()
	{
		HotKeyTextBlock.TextAlignment = TextAlignment.Center;
		HotKeyTextBlock.HorizontalAlignment = HorizontalAlignment.Center;
	}

	private void TbHotkey_OnPreviewKeyDown(object sender, KeyEventArgs e)
	{
		e.Handled = true;
		if ((e.Key == Key.System && (e.SystemKey == Key.LeftAlt || e.SystemKey == Key.RightAlt)) || e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl || e.Key == Key.LeftShift || e.Key == Key.RightShift || e.Key == Key.LeftAlt || e.Key == Key.RightAlt || e.Key == Key.LWin || e.Key == Key.RWin)
		{
			return;
		}
		SpecialKeyState specialKeyState = GlobalHotkey.Instance.CheckModifiers();
		if (!specialKeyState.AltPressed && !specialKeyState.CtrlPressed && !specialKeyState.WinPressed)
		{
			if (e.Key == Key.Back || e.Key == Key.Delete)
			{
				SetHotkey(new HotkeyModel(alt: false, shift: false, win: false, ctrl: false, Key.None));
			}
			if (base.DataContext is ShortcutViewModel { EnableOneWord: false } && (e.Key > Key.F12 || e.Key < Key.F1))
			{
				return;
			}
		}
		Key key = ((e.Key == Key.System) ? e.SystemKey : e.Key);
		if ((specialKeyState.AltPressed && key >= Key.NumPad0 && key <= Key.NumPad9) || (specialKeyState.CtrlPressed && key == Key.Return))
		{
			return;
		}
		HotkeyModel hotkeyModel = new HotkeyModel(specialKeyState.AltPressed, specialKeyState.ShiftPressed, specialKeyState.WinPressed, specialKeyState.CtrlPressed, key);
		if (hotkeyModel.ToString() == HotkeyTextBox.Text)
		{
			return;
		}
		if (HotKeyExisted(hotkeyModel.ToRealString().Replace(" ", "")))
		{
			TryToastExist();
			return;
		}
		base.Dispatcher.InvokeAsync((Func<Task>)async delegate
		{
			await Task.Delay(500);
			SetHotkey(hotkeyModel);
		});
	}

	private void TryToastExist()
	{
		SettingDialog settingDialog = Utils.FindParent<SettingDialog>(this);
		if (settingDialog != null)
		{
			settingDialog.Toast(Utils.GetString("ExistedShortcut"));
		}
		else
		{
			Utils.Toast(Utils.GetString("ExistedShortcut"));
		}
	}

	private bool HotKeyExisted(string hotkey)
	{
		if (!(LocalSettings.Settings.ShortcutAddTask == hotkey) && !(LocalSettings.Settings.ShortcutOpenOrClose == hotkey) && !(LocalSettings.Settings.ShortcutPin == hotkey) && !(LocalSettings.Settings.LockShortcut == hotkey) && !(LocalSettings.Settings.PomoShortcut == hotkey))
		{
			return LocalSettings.Settings.ShortCutModel.ExistKey(hotkey);
		}
		return true;
	}

	private void SetHotkey(HotkeyModel keyModel, bool init = false)
	{
		CurrentHotkey = keyModel;
		bool flag = CurrentHotkey.ToString() == "Back" || string.IsNullOrEmpty(CurrentHotkey.ToString());
		ShortcutViewModel shortcutViewModel = base.DataContext as ShortcutViewModel;
		if (shortcutViewModel == null)
		{
			string text = keyModel.ToString();
			shortcutViewModel = (ShortcutViewModel)(base.DataContext = new ShortcutViewModel
			{
				OriginHotKey = (string.IsNullOrEmpty(text) ? Utils.GetString("SetShortcut") : text)
			});
		}
		if (!init)
		{
			shortcutViewModel.NewHotKey = (flag ? string.Empty : CurrentHotkey.ToString());
			shortcutViewModel.OriginHotKey = (flag ? Utils.GetString("SetShortcut") : CurrentHotkey.ToString());
		}
		HotkeyTextBox.Select(HotkeyTextBox.Text.Length, 0);
		if (flag)
		{
			this.HotkeyClear?.Invoke(this, null);
		}
		else if (!init)
		{
			OnHotkeyChanged();
		}
	}

	public void SetHotkey(string keyStr)
	{
		SetHotkey(new HotkeyModel(keyStr), init: true);
	}

	public void TryFocus()
	{
		if (!HotkeyTextBox.IsFocused)
		{
			if (base.DataContext is ShortcutViewModel shortcutViewModel)
			{
				shortcutViewModel.NewHotKey = string.Empty;
			}
			HotkeyTextBox.Focus();
			HotkeyTextBox.SelectAll();
		}
	}

	private void OnDeleteMouseDown(object sender, MouseButtonEventArgs e)
	{
		SetHotkey(new HotkeyModel(string.Empty));
		e.Handled = true;
	}

	private void TryFocusInputBox(object sender, MouseButtonEventArgs e)
	{
		HotkeyTextBox.Visibility = Visibility.Visible;
		HotkeyTextBox.Focus();
		e.Handled = true;
	}

	private void OnKeyUp(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Escape)
		{
			Keyboard.ClearFocus();
			e.Handled = true;
		}
	}

	private void OnLostFocus(object sender, RoutedEventArgs e)
	{
		HotkeyTextBox.Visibility = Visibility.Collapsed;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/config/hotkeycontrol.xaml", UriKind.Relative);
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
			Root = (HotkeyControl)target;
			break;
		case 2:
			((Grid)target).DataContextChanged += OnDataContextChanged;
			break;
		case 3:
			HotkeyTextBox = (TextBox)target;
			HotkeyTextBox.PreviewKeyDown += TbHotkey_OnPreviewKeyDown;
			HotkeyTextBox.PreviewKeyUp += OnKeyUp;
			HotkeyTextBox.LostFocus += OnLostFocus;
			break;
		case 4:
			((Border)target).MouseLeftButtonUp += TryFocusInputBox;
			break;
		case 5:
			HotKeyTextBlock = (TextBlock)target;
			break;
		case 6:
			DeleteGrid = (Grid)target;
			DeleteGrid.PreviewMouseLeftButtonDown += OnDeleteMouseDown;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
