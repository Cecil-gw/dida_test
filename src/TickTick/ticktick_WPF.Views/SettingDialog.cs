using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Config;
using ticktick_WPF.Views.QuickAdd;
using ticktick_WPF.Views.Setting;
using ticktick_WPF.Views.Theme;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Views;

public class SettingDialog : Window, IComponentConnector, IStyleConnector
{
	private readonly Dictionary<SettingsType, UserControl> _settingItems = new Dictionary<SettingsType, UserControl>();

	private static SettingDialog _instance;

	private bool _changed;

	private bool _needCheckNotion;

	internal SettingDialog SettingsWindow;

	internal ListView MenuListView;

	internal Grid SettingContainer;

	internal Border ToastBorder;

	internal TextBlock ToastText;

	private bool _contentLoaded;

	public static void ShowSettingDialog(SettingsType type = SettingsType.Account, Window owner = null)
	{
		if (type == SettingsType.Premium || type == SettingsType.Account)
		{
			type = SettingsType.More;
		}
		if (_instance == null)
		{
			_instance = new SettingDialog(type)
			{
				Owner = owner
			};
		}
		try
		{
			_instance.Show();
			_instance.Activate();
			_instance.ForceSelectType(type);
			_instance.SetMenuListViewIndex(type);
		}
		catch (Exception)
		{
			_instance = new SettingDialog(type)
			{
				Owner = owner
			};
			_instance.Show();
		}
		WindowNotifier.NotifySubWindowOpen(SubWindowType.Setting);
	}

	private void ForceSelectType(SettingsType type)
	{
		GetSettingControl(type, bySwitchClick: false);
	}

	public static void ShowTemplateSettingDialog(Window owner = null, TemplateKind kind = TemplateKind.Task, AddTaskViewModel addModel = null)
	{
		if (_instance == null)
		{
			_instance = new SettingDialog(SettingsType.More)
			{
				Owner = owner
			};
			if (owner == null)
			{
				_instance.Topmost = true;
			}
		}
		try
		{
			_instance.Show();
			_instance.Activate();
			_instance.SetTemplate(kind, addModel);
			_instance.SetMenuListViewIndex(SettingsType.More);
		}
		catch (Exception)
		{
			_instance = new SettingDialog(SettingsType.More)
			{
				Owner = owner
			};
			_instance.Show();
		}
	}

	private void SetTemplate(TemplateKind kind = TemplateKind.Task, AddTaskViewModel addModel = null)
	{
		MoreSettingsConfig moreSettingsConfig = new MoreSettingsConfig(kind, addModel);
		_settingItems[SettingsType.More] = moreSettingsConfig;
		SettingContainer.Children.Clear();
		SettingContainer.Children.Add(moreSettingsConfig);
		moreSettingsConfig.DelayScrollToTemplate();
	}

	public SettingDialog(SettingsType type = SettingsType.Account)
	{
		InitializeComponent();
		SettingsWindow.DataContext = new SettingViewModel(type);
		base.Closing += delegate
		{
			_instance = null;
			base.Owner?.Activate();
		};
		base.InputBindings.Add(new KeyBinding(WindowCommands.EscCommand, new KeyGesture(Key.Escape))
		{
			CommandParameter = this
		});
		App.Window.ReleaseLock();
	}

	protected override void OnPreviewKeyDown(KeyEventArgs e)
	{
		if (e.Key == Key.F12 && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
		{
			AboutInfo aboutInfo = SettingContainer.Children.OfType<AboutInfo>().FirstOrDefault();
			if (aboutInfo != null)
			{
				e.Handled = true;
				if (!e.IsRepeat)
				{
					aboutInfo.ShowAICompatibilityMenu();
				}
				return;
			}
		}
		base.OnPreviewKeyDown(e);
	}

	private async void SetMenuListViewIndex(SettingsType t)
	{
		await Task.Delay(50);
		if (!(MenuListView.ItemsSource is ObservableCollection<SettingMenuItem> observableCollection))
		{
			return;
		}
		foreach (SettingMenuItem item in observableCollection)
		{
			item.IsSelected = item.Type == t;
		}
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	protected override async void OnClosed(EventArgs e)
	{
		await SaveSettings();
		JumpHelper.InitJumpList();
		ProjectWidgetsHelper.Reload();
		base.OnClosed(e);
	}

	private async Task SaveSettings()
	{
		await LocalSettings.Settings.Save();
		if (_changed || _settingItems.ContainsKey(SettingsType.Shortcuts))
		{
			await SettingsHelper.PushLocalSettings();
		}
		AppLockModel appLockModel = await AppLockDao.GetLockConfig();
		if (appLockModel != null && appLockModel.LockAfter)
		{
			App.Window.TryLock();
		}
		else
		{
			App.Window.ReleaseLock();
		}
	}

	private void OnItemClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: SettingMenuItem dataContext })
		{
			SelectItem(dataContext);
		}
	}

	private void SelectItem(SettingMenuItem model)
	{
		if (!(MenuListView.ItemsSource is ObservableCollection<SettingMenuItem> observableCollection))
		{
			return;
		}
		foreach (SettingMenuItem item in observableCollection)
		{
			item.IsSelected = false;
		}
		model.IsSelected = true;
		if (_settingItems.TryGetValue(model.Type, out var value))
		{
			SettingContainer.Children.Clear();
			SettingContainer.Children.Add(value);
			if (value is ShortcutsConfig shortcutsConfig)
			{
				shortcutsConfig.InitShortcuts();
			}
		}
		else
		{
			GetSettingControl(model.Type, bySwitchClick: true);
		}
	}

	private void GetSettingControl(SettingsType selectedType, bool bySwitchClick)
	{
		UserControl userControl = null;
		if (!_settingItems.ContainsKey(selectedType))
		{
			switch (selectedType)
			{
			case SettingsType.Feature:
				userControl = new FeatureConfig();
				break;
			case SettingsType.Theme:
				userControl = new AppearanceControl();
				break;
			case SettingsType.SmartList:
				userControl = new SmartListConfig();
				break;
			case SettingsType.DateTime:
				userControl = new DateTimeConfig();
				break;
			case SettingsType.Notification:
				userControl = new ticktick_WPF.Views.Config.ReminderConfig();
				break;
			case SettingsType.More:
				userControl = new MoreSettingsConfig();
				break;
			case SettingsType.AIFeatures:
				userControl = new AIFeaturesConfig();
				break;
			case SettingsType.StickyNote:
				userControl = new StickyNoteConfig();
				break;
			case SettingsType.Share:
				userControl = new ShareConfig();
				break;
			case SettingsType.Widget:
				userControl = new WidgetInfo();
				break;
			case SettingsType.Premium:
				UserActCollectUtils.SendUpgradePromptData("settings");
				userControl = new PremiumConfig();
				break;
			case SettingsType.Account:
				userControl = new AccountInfo(this);
				break;
			case SettingsType.Shortcuts:
				userControl = new ShortcutsConfig();
				break;
			case SettingsType.Calendar:
				userControl = new SubscribeCalendar();
				break;
			case SettingsType.About:
				userControl = new AboutInfo(bySwitchClick);
				break;
			case SettingsType.Debug:
				userControl = new DebugSettings();
				break;
			}
			if (userControl != null && !_settingItems.ContainsKey(selectedType))
			{
				_settingItems.Add(selectedType, userControl);
				SettingContainer.Children.Clear();
				SettingContainer.Children.Add(userControl);
			}
		}
	}

	public void Toast(string toastText)
	{
		ToastBorder.Visibility = Visibility.Visible;
		ToastText.Text = toastText;
		Storyboard storyboard = (Storyboard)FindResource("ShowToast");
		ToastBorder.BeginStoryboard(storyboard);
	}

	private void OnToasted(object sender, EventArgs e)
	{
		ToastBorder.Visibility = Visibility.Collapsed;
	}

	private void OnBringIntoView(object sender, RequestBringIntoViewEventArgs e)
	{
		if (MenuListView.IsMouseOver)
		{
			e.Handled = true;
		}
	}

	public static void CloseInstance()
	{
		_instance?.Close();
	}

	public static bool SettingShow()
	{
		return _instance != null;
	}

	private void OnDragMove(object sender, MouseButtonEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed)
		{
			DragMove();
		}
	}

	public void SetChanged()
	{
		_changed = true;
	}

	public static bool TryToast(SettingsType type, string msg)
	{
		if (_instance != null && _instance.MenuListView.ItemsSource is ObservableCollection<SettingMenuItem> source)
		{
			SettingMenuItem settingMenuItem = source.FirstOrDefault((SettingMenuItem d) => d.IsSelected);
			if (settingMenuItem != null && settingMenuItem.Type == type)
			{
				try
				{
					_instance.Activate();
				}
				catch (Exception)
				{
				}
				_instance.Toast(msg);
				return true;
			}
		}
		return false;
	}

	public void CheckNotionChanged()
	{
		_needCheckNotion = true;
	}

	protected override void OnActivated(EventArgs e)
	{
		if (_needCheckNotion)
		{
			_needCheckNotion = false;
			CalendarService.PullCalendarConnects();
		}
		base.OnActivated(e);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/settingdialog.xaml", UriKind.Relative);
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
			SettingsWindow = (SettingDialog)target;
			break;
		case 3:
			((Storyboard)target).Completed += OnToasted;
			break;
		case 4:
			MenuListView = (ListView)target;
			break;
		case 6:
			SettingContainer = (Grid)target;
			break;
		case 7:
			ToastBorder = (Border)target;
			break;
		case 8:
			ToastText = (TextBlock)target;
			break;
		case 9:
			((Border)target).MouseLeftButtonDown += OnDragMove;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 2:
			((ItemsPresenter)target).RequestBringIntoView += OnBringIntoView;
			break;
		case 5:
			((Border)target).MouseLeftButtonUp += OnItemClick;
			break;
		}
	}
}
