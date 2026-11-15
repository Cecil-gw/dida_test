using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using Emoji.Wpf;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.ReminderTime;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Filter;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Config;

public class ReminderConfig : UserControl, IComponentConnector
{
	private static readonly List<DisplayModule> DefaultAnnoyingReminderTypes = new List<DisplayModule>
	{
		DisplayModule.Task,
		DisplayModule.Habit,
		DisplayModule.CountDown
	};

	private string _email;

	private bool _verifiedEmail;

	private bool _isEmailPopupOpen;

	private List<string> _emailNotificationEntityTypes;

	internal CustomSimpleComboBox ShowReminderComboBox;

	internal CustomSimpleComboBox ReminderDetailComboBox;

	internal Image ProIcon;

	internal CheckBox AnnoyingReminderCheckbox;

	internal MultiSelectComboBox ReminderSourceCombo;

	internal Grid ReminderRingGrid;

	internal CustomSimpleComboBox RemindSoundComboBox;

	internal CustomSimpleComboBox CompletionSoundComboBox;

	internal Grid VolumePanel;

	internal Slider Slider;

	internal Border EmailReminderGrid;

	internal CheckBox EmailReminderCheckBox;

	internal StackPanel EmailPanel;

	internal TextBlock EmailText;

	internal Path VerifiedIcon;

	internal StackPanel NotificationItemPanel;

	internal EmjTextBlock EmailNotificationType;

	internal EscPopup ListPopup;

	private bool _contentLoaded;

	private bool loaded { get; set; }

	public ReminderConfig()
	{
		InitializeComponent();
		InitData();
		if (Utils.IsWindows7())
		{
			VolumePanel.Visibility = Visibility.Collapsed;
			return;
		}
		Slider.Value = LocalSettings.Settings.ExtraSettings.ReminderVolume / 10.0;
		Slider.ToolTip = (int)(Slider.Value * 10.0) + "%";
		Slider.ValueChanged -= OnSlideValueChanged;
		Slider.ValueChanged += OnSlideValueChanged;
	}

	private void UserControl_Loaded(object sender, RoutedEventArgs e)
	{
		loaded = true;
	}

	private void InitData()
	{
		InitShowReminder();
		InitEmailReminder();
		InitRemindSound();
		InitRemindDetail();
		InitCompletionSound();
		InitReminderSource();
	}

	private void InitReminderSource()
	{
		if (UserDao.IsPro())
		{
			AnnoyingReminderCheckbox.IsChecked = LocalSettings.Settings.UserPreference?.Reminder?.annoyingAlertEnabled == true;
			List<DisplayModule> effectiveRemindTypes = AnnoyingReminderManager.GetEffectiveRemindTypes(LocalSettings.Settings.UserPreference?.Reminder);
			ReminderSourceCombo.Init(BuildReminderSourceItems(effectiveRemindTypes));
		}
	}

	private static List<DataComboBoxViewModel> BuildReminderSourceItems(List<DisplayModule> reminderTypes)
	{
		List<DataComboBoxViewModel> list = new List<DataComboBoxViewModel>();
		foreach (DisplayModule item in new List<DisplayModule>
		{
			DisplayModule.Task,
			DisplayModule.Habit,
			DisplayModule.CountDown
		})
		{
			string text = null;
			switch (item)
			{
			case DisplayModule.Task:
				text = Utils.GetString("task_plural");
				break;
			case DisplayModule.Habit:
				text = Utils.GetString("Habits");
				break;
			case DisplayModule.CountDown:
				text = Utils.GetString("Countdowns");
				break;
			}
			list.Add(new DataComboBoxViewModel(item, text)
			{
				Selected = (reminderTypes?.Contains(item) ?? false)
			});
		}
		return list;
	}

	private async void InitEmailReminder()
	{
		EmailReminderGrid.Visibility = Visibility.Collapsed;
		_emailNotificationEntityTypes = LocalSettings.Settings.UserPreference.GeneralConfig?.emailRemindItems ?? new List<string> { "task" };
		UserInfoModel userInfoModel = await UserManager.GetUserInfo();
		_email = ((userInfoModel == null || userInfoModel.fakedEmail) ? string.Empty : userInfoModel.username);
		_verifiedEmail = userInfoModel?.verifiedEmail ?? false;
		EmailText.Text = (string.IsNullOrEmpty(_email) ? Utils.GetString("SetupEmail") : _email);
		VerifiedIcon.Visibility = (_verifiedEmail ? Visibility.Collapsed : Visibility.Visible);
		EmailReminderCheckBox.IsChecked = UserDao.IsPro() && (LocalSettings.Settings.UserPreference.GeneralConfig?.emailRemindEnabled ?? false);
		EmailPanel.Visibility = (string.IsNullOrEmpty(_email) ? Visibility.Collapsed : Visibility.Visible);
		EmailNotificationType.Text = ConvertNotificationDisplayText(_emailNotificationEntityTypes);
	}

	private void ShowReminderSelectionChanged(object sender, SimpleComboBoxViewModel e)
	{
		if (ShowReminderComboBox.ItemsSource.Count == 3)
		{
			if (ShowReminderComboBox.SelectedIndex == 1 && !SystemToastUtils.CheckSystemToastEnable())
			{
				ShowReminderComboBox.SelectedIndex = 0;
			}
			LocalSettings.Settings.ShowReminderInClient = ShowReminderComboBox.SelectedIndex == 0;
			LocalSettings.Settings.ShowReminder = ShowReminderComboBox.SelectedIndex != 2;
		}
		else
		{
			LocalSettings.Settings.ShowReminder = ShowReminderComboBox.SelectedIndex == 0;
		}
	}

	private void InitShowReminder()
	{
		List<string> itemsSource = new List<string>
		{
			Utils.GetString("ReminderMeOnClient"),
			Utils.GetString("ReminderMeOnSystem"),
			Utils.GetString("DoNotReminderMe")
		};
		bool flag = false;
		if (!Utils.IsWindows7())
		{
			bool flag2 = SystemToastUtils.CheckSystemToastEnable();
			flag = !flag2;
			if (LocalSettings.Settings.ShowReminder && !LocalSettings.Settings.ShowReminderInClient && !flag2)
			{
				LocalSettings.Settings.ShowReminderInClient = true;
			}
		}
		else
		{
			flag = true;
			LocalSettings.Settings.ShowReminderInClient = true;
		}
		ShowReminderComboBox.SelectedIndex = (LocalSettings.Settings.ShowReminder ? ((!LocalSettings.Settings.ShowReminderInClient) ? 1 : 0) : 2);
		ShowReminderComboBox.ItemsSource = itemsSource;
		if (flag)
		{
			ShowReminderComboBox.Items[1].IsEnable = false;
		}
	}

	private void InitCompletionSound()
	{
		CompletionSoundComboBox.ItemsSource = new List<string>
		{
			Utils.GetString("none"),
			Utils.GetString("Jingle"),
			Utils.GetString("Drip"),
			Utils.GetString("Knock"),
			Utils.GetString("Spiral")
		};
		Enum.TryParse<CompletionSound>(LocalSettings.Settings.CompletionSound, out var result);
		CompletionSoundComboBox.SelectedIndex = (int)result;
	}

	private void InitRemindSound()
	{
		RemindSoundComboBox.ItemsSource = new List<string>
		{
			Utils.GetString("none"),
			Utils.GetString("Default"),
			Utils.GetString("MusicBoxSound"),
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
		if (Enum.TryParse<RemindSound>(LocalSettings.Settings.ExtraSettings.RemindSound, out var result))
		{
			RemindSoundComboBox.SelectedIndex = (int)result;
		}
		else
		{
			RemindSoundComboBox.SelectedIndex = (LocalSettings.Settings.EnableRingtone ? 1 : 0);
		}
	}

	private void OnRemindSoundChanged(object sender, SimpleComboBoxViewModel e)
	{
		LocalSettings.Settings.ExtraSettings.RemindSound = ((RemindSound)RemindSoundComboBox.SelectedIndex/*cast due to .constrained prefix*/).ToString();
		RemindSoundPlayer.PlayTaskRemindSound(isTry: true);
	}

	private void InitRemindDetail()
	{
		ReminderDetailComboBox.ItemsSource = new List<string>
		{
			Utils.GetString("Always"),
			Utils.GetString("WhenUnlocked"),
			Utils.GetString("Never")
		};
		ReminderDetailComboBox.SelectedIndex = LocalSettings.Settings.ExtraSettings.RemindDetail;
	}

	private void OnRemindDetailChanged(object sender, SimpleComboBoxViewModel e)
	{
		LocalSettings.Settings.ExtraSettings.RemindDetail = ReminderDetailComboBox.SelectedIndex;
	}

	private void OnCompletionSoundChanged(object sender, SimpleComboBoxViewModel e)
	{
		LocalSettings.Settings.CompletionSound = ((CompletionSound)CompletionSoundComboBox.SelectedIndex/*cast due to .constrained prefix*/).ToString();
		Utils.PlayCompletionSound();
	}

	private async void EmailReminderClick(object sender, MouseButtonEventArgs e)
	{
		Mouse.Capture(null);
		e.Handled = true;
		if (!ProChecker.CheckPro(ProType.EmailReminder))
		{
			return;
		}
		if (string.IsNullOrEmpty(_email) || !_verifiedEmail)
		{
			if (new CustomerDialog(Utils.GetString(string.IsNullOrEmpty(_email) ? "NoEmail" : "NotVerified"), Utils.GetString(string.IsNullOrEmpty(_email) ? "NotSetEmailCantReminder" : "NotVerifiedCantReminder"), Utils.GetString(string.IsNullOrEmpty(_email) ? "GotoSet" : "GotoVerify"), Utils.GetString("Cancel"))
			{
				Owner = Window.GetWindow(this)
			}.ShowDialog() == true)
			{
				if (string.IsNullOrEmpty(_email))
				{
					GotoSetEmail();
					return;
				}
				await Communicator.ResentVerifyEmail();
				Utils.FindParent<SettingDialog>(this)?.Toast(Utils.GetString("SendEmailSucceeded"));
			}
		}
		else
		{
			EmailReminderCheckBox.IsChecked = EmailReminderCheckBox.IsChecked != true;
			LocalSettings.Settings.SetEmailReminder(EmailReminderCheckBox.IsChecked == true);
			SettingsHelper.PushLocalPreference();
		}
	}

	private static void GotoSetEmail()
	{
		Utils.TryProcessStartUrlWithToken("/webapp/#settings/profile");
	}

	private void OnTaskEditClick(object sender, MouseButtonEventArgs e)
	{
		if (ListPopup.IsOpen || _isEmailPopupOpen)
		{
			ListPopup.IsOpen = false;
			return;
		}
		NotificationEntityTypeEditDialog notificationEntityTypeEditDialog = new NotificationEntityTypeEditDialog(_emailNotificationEntityTypes);
		ListPopup.Child = notificationEntityTypeEditDialog;
		ListPopup.IsOpen = true;
		_isEmailPopupOpen = true;
		notificationEntityTypeEditDialog.OnSelectedTypesChanged += delegate(object o, List<string> list)
		{
			_emailNotificationEntityTypes = list;
		};
		notificationEntityTypeEditDialog.OnSave += delegate
		{
			if (_emailNotificationEntityTypes.Count > 0)
			{
				EmailNotificationType.Text = ConvertNotificationDisplayText(_emailNotificationEntityTypes);
				LocalSettings.Settings.SetEmailRemindItems(_emailNotificationEntityTypes.Distinct().ToList());
				SettingsHelper.PushLocalPreference();
			}
			ListPopup.IsOpen = false;
		};
		notificationEntityTypeEditDialog.OnCancel += delegate
		{
			ListPopup.IsOpen = false;
		};
		ListPopup.Closed -= OnListPopupClosed;
		ListPopup.Closed += OnListPopupClosed;
	}

	private async void OnListPopupClosed(object sender, EventArgs e)
	{
		await Task.Delay(100);
		_isEmailPopupOpen = false;
	}

	private static string ConvertNotificationDisplayText(List<string> keys)
	{
		List<string> list = new List<string>();
		foreach (string key in keys)
		{
			if (!(key == "task"))
			{
				if (key == "habit")
				{
					list.Add(Utils.GetString("Habits"));
				}
			}
			else
			{
				list.Add(Utils.GetString("task_plural"));
			}
		}
		return string.Join(", ", list.Distinct().ToList());
	}

	private void OnSlideValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
	{
		LocalSettings.Settings.ExtraSettings.ReminderVolume = e.NewValue * 10.0;
		Slider.ToolTip = (int)(e.NewValue * 10.0) + "%";
		RemindSoundPlayer.OnVolumeChanged();
	}

	private void ReminderSourceCombo_ItemSelected(object sender, List<DataComboBoxViewModel> e)
	{
		if (ProChecker.CheckPro(ProType.ConstantReminder))
		{
			LocalSettings.Settings.SaveAnnoyingReminder(LocalSettings.Settings?.UserPreference?.Reminder?.annoyingAlertEnabled == true, e.Select((DataComboBoxViewModel x) => (DisplayModule)x.Data).ToList());
		}
	}

	private void AnnoyingReminderCheckbox_Click(object sender, RoutedEventArgs e)
	{
		if (!ProChecker.CheckPro(ProType.ConstantReminder))
		{
			AnnoyingReminderCheckbox.IsChecked = false;
			return;
		}
		List<DisplayModule> list = LocalSettings.Settings?.UserPreference?.Reminder?.RemindTypes;
		if (AnnoyingReminderCheckbox.IsChecked == true && list == null)
		{
			list = DefaultAnnoyingReminderTypes.ToList();
			ReminderSourceCombo.Init(BuildReminderSourceItems(list));
		}
		LocalSettings.Settings.SaveAnnoyingReminder(AnnoyingReminderCheckbox.IsChecked == true, list);
	}

	private void AnnoyingReminderCheckbox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (!ProChecker.CheckPro(ProType.ConstantReminder))
		{
			e.Handled = true;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/config/reminderconfig.xaml", UriKind.Relative);
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
			((ReminderConfig)target).Loaded += UserControl_Loaded;
			break;
		case 2:
			ShowReminderComboBox = (CustomSimpleComboBox)target;
			break;
		case 3:
			ReminderDetailComboBox = (CustomSimpleComboBox)target;
			break;
		case 4:
			ProIcon = (Image)target;
			break;
		case 5:
			AnnoyingReminderCheckbox = (CheckBox)target;
			AnnoyingReminderCheckbox.PreviewMouseLeftButtonDown += AnnoyingReminderCheckbox_PreviewMouseLeftButtonDown;
			AnnoyingReminderCheckbox.Click += AnnoyingReminderCheckbox_Click;
			break;
		case 6:
			ReminderSourceCombo = (MultiSelectComboBox)target;
			break;
		case 7:
			ReminderRingGrid = (Grid)target;
			break;
		case 8:
			RemindSoundComboBox = (CustomSimpleComboBox)target;
			break;
		case 9:
			CompletionSoundComboBox = (CustomSimpleComboBox)target;
			break;
		case 10:
			VolumePanel = (Grid)target;
			break;
		case 11:
			Slider = (Slider)target;
			break;
		case 12:
			EmailReminderGrid = (Border)target;
			break;
		case 13:
			EmailReminderCheckBox = (CheckBox)target;
			EmailReminderCheckBox.PreviewMouseLeftButtonUp += EmailReminderClick;
			break;
		case 14:
			EmailPanel = (StackPanel)target;
			break;
		case 15:
			EmailText = (TextBlock)target;
			break;
		case 16:
			VerifiedIcon = (Path)target;
			break;
		case 17:
			NotificationItemPanel = (StackPanel)target;
			break;
		case 18:
			((Grid)target).PreviewMouseLeftButtonUp += OnTaskEditClick;
			break;
		case 19:
			EmailNotificationType = (EmjTextBlock)target;
			break;
		case 20:
			ListPopup = (EscPopup)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
