using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using NHotkey.Wpf;
using TickTickUtils;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.Views.Config;
using ticktick_WPF.Views.CustomControl;

namespace ticktick_WPF.Views.Pomo;

public class PomoSettings : Window, IComponentConnector
{
	private const string KeyDailyPomoGoals = "DailyPomoGoals";

	public const string KeyPomoDuration = "PomoDuration";

	public const string KeyShortBreakDuration = "ShortBreakDuration";

	public const string KeyLongBreakDuration = "LongBreakDuration";

	private const string KeyLongBreakEvery = "LongBreakEvery";

	private const string KeyAutoPomoTimes = "AutoPomoTimes";

	public static readonly Dictionary<string, Tuple<int, int, int>> PomoSettingsData = new Dictionary<string, Tuple<int, int, int>>
	{
		{
			"DailyPomoGoals",
			new Tuple<int, int, int>(4, 1, 20)
		},
		{
			"PomoDuration",
			new Tuple<int, int, int>(25, 5, 180)
		},
		{
			"ShortBreakDuration",
			new Tuple<int, int, int>(5, 1, 60)
		},
		{
			"LongBreakDuration",
			new Tuple<int, int, int>(15, 1, 60)
		},
		{
			"LongBreakEvery",
			new Tuple<int, int, int>(4, 1, 60)
		},
		{
			"AutoPomoTimes",
			new Tuple<int, int, int>(4, 2, 10)
		}
	};

	private bool _changed;

	private bool _hotKeyChanged;

	private static PomoSettings _instance;

	internal ScrollViewer Scroller;

	internal StackPanel SettingsPanel;

	internal Path ProIcon;

	internal CheckBox AutoSyncCheckBox;

	internal TextBox PomoDurationText;

	internal TextBox ShortBreakDurationText;

	internal TextBox LongBreakDurationText;

	internal TextBox LongBreakEveryText;

	internal TextBlock PomoCount;

	internal CheckBox AutoNextPomo;

	internal CheckBox AutoBreak;

	internal Grid AutoTimesGrid;

	internal TextBox AutoPomoTimes;

	internal CustomSimpleComboBox FocusEndSoundComboBox;

	internal CustomSimpleComboBox BreakEndSoundComboBox;

	internal Image NormalImage;

	internal Grid DetailedSelect;

	internal Image CircleImage;

	internal Grid CircleSelect;

	internal Image MiniImage;

	internal Grid RectSelect;

	internal CustomSimpleComboBox MiniModeThemeComboBox;

	internal Slider OpacitySlider;

	internal CheckBox AutoShowWidget;

	internal HotkeyControl PomoHotkey;

	private bool _contentLoaded;

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	public static void ShowInstance(Window owner, bool scrollToMini = false, bool centerScreen = false)
	{
		if (_instance != null && _instance.Owner != owner)
		{
			_instance.Close();
			_instance = null;
		}
		if (_instance == null)
		{
			_instance = new PomoSettings
			{
				Owner = owner
			};
			if (centerScreen)
			{
				_instance.WindowStartupLocation = WindowStartupLocation.CenterScreen;
			}
			_instance.Closing += delegate
			{
				_instance = null;
			};
		}
		_instance.Show();
		if (scrollToMini)
		{
			_instance.Scroller.ScrollToBottom();
		}
	}

	public PomoSettings()
	{
		InitializeComponent();
		InitSettings();
		LoadRemoteAndInit();
		base.InputBindings.Add(new KeyBinding(WindowCommands.EscCommand, new KeyGesture(Key.Escape))
		{
			CommandParameter = this
		});
		PomoCount.Text = Utils.GetString("PomoCount")?.ToLower();
		base.Closed += OnClosed;
		base.Loaded += delegate
		{
			bool valueOrDefault = FindResource("IsDarkTheme") as bool? == true;
			bool flag = !Utils.IsZhCn();
			string text = "pack://application:,,,/Assets/ImageSource/" + (valueOrDefault ? "dark" : "light") + "/";
			NormalImage.Source = ImageUtils.GetResourceImage(text + "NormalFocus" + (flag ? "_en" : "_cn") + ".png", 0);
			MiniImage.Source = ImageUtils.GetResourceImage(text + "MiniFocus.png", 0);
			CircleImage.Source = ImageUtils.GetResourceImage(text + "CircleFocus.png", 0);
		};
	}

	private void OnClosed(object sender, EventArgs e)
	{
		if (_changed)
		{
			SyncRemoteSettings();
			TickFocusManager.ResetPomoSetting();
		}
		if (_hotKeyChanged)
		{
			SettingsHelper.PushLocalSettings();
		}
		RemindSoundPlayer.Stop();
		base.Owner?.Activate();
	}

	private async void LoadRemoteAndInit()
	{
		if (await LoadRemoteSettings())
		{
			InitSettings();
		}
	}

	public static async Task<bool> LoadRemoteSettings()
	{
		return ApplyRemoteSettings(isCurrentUser: LocalSettings.Settings.CaptureFocusSyncGuard(), config: await Communicator.GetRemotePomoConfig());
	}

	internal static bool ApplyRemoteSettings(PomoConfigModel config, Func<bool> isCurrentUser)
	{
		if (config == null || !string.IsNullOrEmpty(config.errorId))
		{
			return false;
		}
		return LocalSettings.Settings.ApplyFocusSyncResult(isCurrentUser, delegate
		{
			LocalSettings settings = LocalSettings.Settings;
			settings.ObserveSyncedFocusDuration((double)config.pomoDuration * 60.0);
			settings.PomoDuration = ((config.pomoDuration == 0) ? 25 : Math.Max(Constants.MinFocusDuration / 60, config.pomoDuration));
			settings.ShortBreakDuration = ((config.shortBreakDuration == 0) ? 5 : config.shortBreakDuration);
			settings.LongBreakDuration = ((config.longBreakDuration == 0) ? 15 : config.longBreakDuration);
			settings.ShowDailyDuration = config.focusDuration > 0;
			settings.DailyFocusDuration = (settings.ShowDailyDuration ? config.focusDuration : settings.DailyFocusDuration);
			settings.ShowDailyPomo = config.pomoGoal > 0;
			settings.DailyPomoGoals = (settings.ShowDailyPomo ? config.pomoGoal : settings.DailyPomoGoals);
			settings.LongBreakEvery = ((config.longBreakInterval == 0) ? 4 : config.longBreakInterval);
			settings.AutoBreak = config.autoBreak;
			settings.AutoNextPomo = config.autoPomo;
			settings.LightsOn = config.lightsOn;
		});
	}

	public static async void SyncRemoteSettings()
	{
		PomoConfigModel pomoConfigModel = new PomoConfigModel();
		LocalSettings settings = LocalSettings.Settings;
		pomoConfigModel.id = settings.LoginUserId;
		pomoConfigModel.pomoDuration = Math.Max(Constants.MinFocusDuration / 60, settings.PomoDuration);
		pomoConfigModel.shortBreakDuration = settings.ShortBreakDuration;
		pomoConfigModel.longBreakDuration = settings.LongBreakDuration;
		pomoConfigModel.pomoGoal = (settings.ShowDailyPomo ? settings.DailyPomoGoals : 0);
		pomoConfigModel.focusDuration = (settings.ShowDailyDuration ? settings.DailyFocusDuration : 0);
		pomoConfigModel.longBreakInterval = settings.LongBreakEvery;
		pomoConfigModel.autoBreak = settings.AutoBreak;
		pomoConfigModel.autoPomo = settings.AutoNextPomo;
		pomoConfigModel.lightsOn = settings.LightsOn;
		await Communicator.UpdateRemotePomoConfig(pomoConfigModel);
	}

	private void InitSettings()
	{
		SetPomoSetting("PomoDuration", PomoDurationText);
		SetPomoSetting("ShortBreakDuration", ShortBreakDurationText);
		SetPomoSetting("LongBreakDuration", LongBreakDurationText);
		SetPomoSetting("LongBreakEvery", LongBreakEveryText);
		SetPomoSetting("AutoPomoTimes", AutoPomoTimes);
		AutoNextPomo.IsChecked = LocalSettings.Settings.AutoNextPomo;
		AutoBreak.IsChecked = LocalSettings.Settings.AutoBreak;
		AutoShowWidget.IsChecked = LocalSettings.Settings.PomoLocalSetting.AutoShowWidget;
		PomoHotkey.SetHotkey(LocalSettings.Settings.PomoShortcut);
		PomoHotkey.HotkeyChanged += OnHotKeyChanged;
		PomoHotkey.HotkeyClear += OnHotKeyClear;
		PomoHotkey.SetNormalMode();
		if (LocalSettings.Settings.PomoWindowOpacity >= 0.0 && LocalSettings.Settings.PomoWindowOpacity <= 1.0)
		{
			OpacitySlider.Value = LocalSettings.Settings.PomoWindowOpacity * 10.0;
			OpacitySlider.ToolTip = (int)(OpacitySlider.Value * 10.0) + "%";
			OpacitySlider.ValueChanged -= OnSlideValueChanged;
			OpacitySlider.ValueChanged += OnSlideValueChanged;
		}
		InitFocusEndSound();
		InitBreakEndSound();
		InitMiniModeThemeComboBox();
		AutoTimesGrid.Visibility = ((!LocalSettings.Settings.AutoNextPomo || !LocalSettings.Settings.AutoBreak) ? Visibility.Collapsed : Visibility.Visible);
		AutoSyncCheckBox.IsChecked = LocalSettings.Settings.FocusKeepInSync;
		SetSelectedDisplayType();
	}

	private void SetSelectedDisplayType()
	{
		bool flag = LocalSettings.Settings.PomoLocalSetting.DisplayType == "Circle";
		bool flag2 = LocalSettings.Settings.PomoLocalSetting.DisplayType == "Mini";
		DetailedSelect.Visibility = ((flag || flag2) ? Visibility.Collapsed : Visibility.Visible);
		CircleSelect.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
		RectSelect.Visibility = ((!flag2) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void OnHotKeyClear(object sender, EventArgs e)
	{
		LocalSettings.Settings.PomoShortcut = string.Empty;
		HotkeyManager.Current.Remove("PomoShortcut");
		_hotKeyChanged = true;
	}

	private void OnHotKeyChanged(object sender, EventArgs e)
	{
		LocalSettings.Settings.PomoShortcut = PomoHotkey.CurrentHotkey.ToString().Replace(" ", "");
		HotkeyManager.Current.Remove("PomoShortcut");
		HotKeyUtils.ResignHotKey("PomoShortcut");
		_hotKeyChanged = true;
	}

	private void SetPomoSetting(string key, TextBox settingText)
	{
		settingText.TextChanged -= OnSettingsTextChanged;
		settingText.Text = LocalSettings.Settings[key].ToString();
		settingText.TextChanged += OnSettingsTextChanged;
	}

	private async void OnSettingsTextChanged(object sender, TextChangedEventArgs e)
	{
		TextBox textBox = (TextBox)sender;
		if (textBox.Tag == null)
		{
			return;
		}
		_changed = true;
		string key = textBox.Tag.ToString();
		if (PomoSettingsData.ContainsKey(key))
		{
			Tuple<int, int, int> tuple = PomoSettingsData[key];
			await Task.Delay(600);
			int.TryParse(textBox.Text, out var result);
			if (result > tuple.Item3)
			{
				textBox.Text = tuple.Item3.ToString();
				textBox.SelectAll();
			}
			if (result < tuple.Item2)
			{
				textBox.Text = tuple.Item2.ToString();
				textBox.SelectAll();
			}
			SavePomoSettings(key, textBox.Text);
		}
	}

	private static void SavePomoSettings(string key, string value)
	{
		if (int.TryParse(value, out var result))
		{
			LocalSettings.Settings[key] = result;
		}
	}

	private void OnSettingsKeyUp(object sender, KeyEventArgs e)
	{
		if (e.Key != Key.Up && e.Key != Key.Down)
		{
			return;
		}
		TextBox textBox = (TextBox)sender;
		if (textBox.Tag == null)
		{
			return;
		}
		string key = textBox.Tag.ToString();
		if (!PomoSettingsData.ContainsKey(key))
		{
			return;
		}
		Tuple<int, int, int> tuple = PomoSettingsData[key];
		textBox.TextChanged -= OnSettingsTextChanged;
		if (int.TryParse(textBox.Text, out var result))
		{
			int item = tuple.Item2;
			int item2 = tuple.Item3;
			int num = ((e.Key == Key.Up) ? (result + 1) : (result - 1));
			if (num >= item && num <= item2)
			{
				textBox.Text = num.ToString();
				textBox.SelectAll();
				SavePomoSettings(key, textBox.Text);
				_changed = true;
			}
		}
		textBox.TextChanged += OnSettingsTextChanged;
	}

	private void AutoSyncClick(object sender, RoutedEventArgs e)
	{
		if (!UserDao.IsPro())
		{
			ProChecker.CheckPro(ProType.FocusAutoSync, Window.GetWindow(this));
			Mouse.Capture(null);
			e.Handled = true;
			Close();
			return;
		}
		NotifyCheckBoxChanged(sender, e);
		bool flag = AutoSyncCheckBox.IsChecked.HasValue && AutoSyncCheckBox.IsChecked.Value;
		LocalSettings.Settings.SaveFocusKeepInSync(flag);
		if (flag)
		{
			UserActCollectUtils.AddClickEvent("focus", "focus_settings", "focus_sync");
			WebSocketService.CheckFocusSocket();
		}
	}

	private void AutoPomoClick(object sender, MouseButtonEventArgs e)
	{
		_changed = true;
		NotifyCheckBoxChanged(sender, e);
		LocalSettings.Settings.AutoNextPomo = AutoNextPomo.IsChecked.HasValue && AutoNextPomo.IsChecked.Value;
		AutoTimesGrid.Visibility = ((!LocalSettings.Settings.AutoNextPomo || !LocalSettings.Settings.AutoBreak) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void AutoBreakClick(object sender, MouseButtonEventArgs e)
	{
		_changed = true;
		NotifyCheckBoxChanged(sender, e);
		LocalSettings.Settings.AutoBreak = AutoBreak.IsChecked.HasValue && AutoBreak.IsChecked.Value;
		AutoTimesGrid.Visibility = ((!LocalSettings.Settings.AutoNextPomo || !LocalSettings.Settings.AutoBreak) ? Visibility.Collapsed : Visibility.Visible);
	}

	private static void NotifyCheckBoxChanged(object sender, RoutedEventArgs e)
	{
		Mouse.Capture(null);
		CheckBox obj = (CheckBox)sender;
		CheckBox obj2 = (CheckBox)sender;
		obj.IsChecked = ((obj2 == null) ? ((bool?)null) : (!obj2.IsChecked));
		e.Handled = true;
	}

	private void OnSlideValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
	{
		LocalSettings.Settings.PomoWindowOpacity = e.NewValue / 10.0;
		OpacitySlider.ToolTip = (int)(e.NewValue * 10.0) + "%";
		GlobalEventManager.NotifyPomoOpacityChanged(LocalSettings.Settings.PomoWindowOpacity);
	}

	private void OnShortCutClick(object sender, MouseButtonEventArgs e)
	{
		PomoHotkey.TryFocus();
		e.Handled = true;
	}

	private void OnThemeChecked(object sender, RoutedEventArgs e)
	{
		if (sender is RadioButton { Tag: string tag })
		{
			_ = tag != LocalSettings.Settings.PomoWindowTheme;
		}
	}

	private void InitFocusEndSound()
	{
		FocusEndSoundComboBox.ItemsSource = new List<string>
		{
			Utils.GetString("none"),
			Utils.GetString("Default"),
			Utils.GetString("HarpSound"),
			Utils.GetString("DrumSound"),
			Utils.GetString("BeepSound"),
			Utils.GetString("BlocksSound"),
			Utils.GetString("ChimesSound"),
			Utils.GetString("CrystalSound"),
			Utils.GetString("LadderSound"),
			Utils.GetString("LatticeSound"),
			Utils.GetString("LeapSound"),
			Utils.GetString("MatrixSound"),
			Utils.GetString("PulseSound"),
			Utils.GetString("Spiral")
		};
		if (Enum.TryParse<FocusRemindSound>(LocalSettings.Settings.PomoLocalSetting.FocusEndSound, out var result))
		{
			FocusEndSoundComboBox.SelectedIndex = (int)result;
		}
		else
		{
			FocusEndSoundComboBox.SelectedIndex = (LocalSettings.Settings.EnableRingtone ? 1 : 0);
		}
	}

	private void OnFocusEndSoundChanged(object sender, SimpleComboBoxViewModel e)
	{
		LocalSettings.Settings.PomoLocalSetting.FocusEndSound = ((FocusRemindSound)FocusEndSoundComboBox.SelectedIndex/*cast due to .constrained prefix*/).ToString();
		RemindSoundPlayer.PlayFocusRemindSound(isBreak: false, trial: true);
	}

	private void InitBreakEndSound()
	{
		BreakEndSoundComboBox.ItemsSource = new List<string>
		{
			Utils.GetString("none"),
			Utils.GetString("Default"),
			Utils.GetString("HarpSound"),
			Utils.GetString("DrumSound"),
			Utils.GetString("BeepSound"),
			Utils.GetString("BlocksSound"),
			Utils.GetString("ChimesSound"),
			Utils.GetString("CrystalSound"),
			Utils.GetString("LadderSound"),
			Utils.GetString("LatticeSound"),
			Utils.GetString("LeapSound"),
			Utils.GetString("MatrixSound"),
			Utils.GetString("PulseSound"),
			Utils.GetString("Spiral")
		};
		if (Enum.TryParse<FocusRemindSound>(LocalSettings.Settings.PomoLocalSetting.BreakEndSound, out var result))
		{
			BreakEndSoundComboBox.SelectedIndex = (int)result;
		}
		else
		{
			BreakEndSoundComboBox.SelectedIndex = (LocalSettings.Settings.EnableRingtone ? 1 : 0);
		}
	}

	private void OnBreakEndSoundChanged(object sender, SimpleComboBoxViewModel e)
	{
		LocalSettings.Settings.PomoLocalSetting.BreakEndSound = ((FocusRemindSound)BreakEndSoundComboBox.SelectedIndex/*cast due to .constrained prefix*/).ToString();
		RemindSoundPlayer.PlayFocusRemindSound(isBreak: true, trial: true);
	}

	private void AutoShowWidgetClick(object sender, MouseButtonEventArgs e)
	{
		NotifyCheckBoxChanged(sender, e);
		LocalSettings.Settings.PomoLocalSetting.AutoShowWidget = AutoShowWidget.IsChecked.HasValue && AutoShowWidget.IsChecked.Value;
	}

	private void InitMiniModeThemeComboBox()
	{
		MiniModeThemeComboBox.ItemsSource = new List<string>
		{
			Utils.GetString("DarkColor"),
			Utils.GetString("LightColor")
		};
		MiniModeThemeComboBox.SelectedIndex = ((!(LocalSettings.Settings.PomoWindowTheme == "dark")) ? 1 : 0);
	}

	private void OnMiniModeThemeChanged(object sender, SimpleComboBoxViewModel e)
	{
		LocalSettings.Settings.PomoWindowTheme = ((MiniModeThemeComboBox.SelectedIndex == 0) ? "dark" : "light");
	}

	private void OnDisplayTypeClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement { Tag: string tag } && tag != LocalSettings.Settings.PomoLocalSetting.DisplayType)
		{
			if (tag != "Normal" && !ProChecker.CheckPro(ProType.FocusMiniStyle, App.Window))
			{
				Close();
				return;
			}
			LocalSettings.Settings.PomoLocalSetting.DisplayType = tag;
			LocalSettings.Settings.Save(force: true);
			TickFocusManager.OnDisplayTypeChanged();
			SetSelectedDisplayType();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/pomo/pomosettings.xaml", UriKind.Relative);
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
			Scroller = (ScrollViewer)target;
			break;
		case 2:
			SettingsPanel = (StackPanel)target;
			break;
		case 3:
			ProIcon = (Path)target;
			break;
		case 4:
			AutoSyncCheckBox = (CheckBox)target;
			AutoSyncCheckBox.PreviewMouseLeftButtonUp += AutoSyncClick;
			break;
		case 5:
			PomoDurationText = (TextBox)target;
			PomoDurationText.KeyUp += OnSettingsKeyUp;
			PomoDurationText.TextChanged += OnSettingsTextChanged;
			break;
		case 6:
			ShortBreakDurationText = (TextBox)target;
			ShortBreakDurationText.KeyUp += OnSettingsKeyUp;
			ShortBreakDurationText.TextChanged += OnSettingsTextChanged;
			break;
		case 7:
			LongBreakDurationText = (TextBox)target;
			LongBreakDurationText.KeyUp += OnSettingsKeyUp;
			LongBreakDurationText.TextChanged += OnSettingsTextChanged;
			break;
		case 8:
			LongBreakEveryText = (TextBox)target;
			LongBreakEveryText.KeyUp += OnSettingsKeyUp;
			LongBreakEveryText.TextChanged += OnSettingsTextChanged;
			break;
		case 9:
			PomoCount = (TextBlock)target;
			break;
		case 10:
			AutoNextPomo = (CheckBox)target;
			AutoNextPomo.PreviewMouseLeftButtonUp += AutoPomoClick;
			break;
		case 11:
			AutoBreak = (CheckBox)target;
			AutoBreak.PreviewMouseLeftButtonUp += AutoBreakClick;
			break;
		case 12:
			AutoTimesGrid = (Grid)target;
			break;
		case 13:
			AutoPomoTimes = (TextBox)target;
			AutoPomoTimes.KeyUp += OnSettingsKeyUp;
			AutoPomoTimes.TextChanged += OnSettingsTextChanged;
			break;
		case 14:
			FocusEndSoundComboBox = (CustomSimpleComboBox)target;
			break;
		case 15:
			BreakEndSoundComboBox = (CustomSimpleComboBox)target;
			break;
		case 16:
			((Grid)target).MouseLeftButtonUp += OnDisplayTypeClick;
			break;
		case 17:
			NormalImage = (Image)target;
			break;
		case 18:
			DetailedSelect = (Grid)target;
			break;
		case 19:
			((Grid)target).MouseLeftButtonUp += OnDisplayTypeClick;
			break;
		case 20:
			CircleImage = (Image)target;
			break;
		case 21:
			CircleSelect = (Grid)target;
			break;
		case 22:
			((Grid)target).MouseLeftButtonUp += OnDisplayTypeClick;
			break;
		case 23:
			MiniImage = (Image)target;
			break;
		case 24:
			RectSelect = (Grid)target;
			break;
		case 25:
			MiniModeThemeComboBox = (CustomSimpleComboBox)target;
			break;
		case 26:
			OpacitySlider = (Slider)target;
			break;
		case 27:
			AutoShowWidget = (CheckBox)target;
			AutoShowWidget.PreviewMouseLeftButtonUp += AutoShowWidgetClick;
			break;
		case 28:
			PomoHotkey = (HotkeyControl)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
