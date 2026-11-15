using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.ReminderTime;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Time;

namespace ticktick_WPF.Views;

public class SetReminderControl : UserControl, ITabControl, IComponentConnector
{
	private readonly DateTime _date;

	private readonly bool _isAllDay;

	private List<string> _recentTriggers = new List<string>();

	private bool _withNull;

	private int _tabIndex = -1;

	private readonly bool _isSpan;

	private int? _initialAnnoyingAlert;

	private readonly bool _initialAnnoyingAlertChecked;

	private readonly bool _showAnnoyingAlert;

	internal Grid AnnoyingAlertGrid;

	internal CheckBox AnnoyingAlertCheckBox;

	internal Button SaveButton;

	internal Button CancelButton;

	internal Grid Container;

	internal UpDownSelectListView ReminderlistView;

	internal EscPopup CustomChooseRemindPopup;

	private bool _contentLoaded;

	public event EventHandler OnCancel;

	public event EventHandler<ReminderSelectedEventArgs> OnSelected;

	public SetReminderControl(bool isAllDay, IEnumerable<TaskReminderModel> reminders, DateTime date, bool withNull = true, int tabIndex = -1, bool isSpan = false, int? annoyingAlert = null, DisplayModule displayModule = DisplayModule.Task, bool showAnnoyingAlert = true)
	{
		InitializeComponent();
		ReminderlistView.ItemTemplateSelector = new ReminderItemTemplateSelector();
		_isAllDay = isAllDay;
		_date = date;
		_withNull = withNull;
		_tabIndex = tabIndex;
		_isSpan = isSpan;
		_showAnnoyingAlert = showAnnoyingAlert;
		AnnoyingAlertGrid.Visibility = ((!_showAnnoyingAlert) ? Visibility.Collapsed : Visibility.Visible);
		if (_showAnnoyingAlert && UserDao.IsPro())
		{
			_initialAnnoyingAlert = annoyingAlert;
			_initialAnnoyingAlertChecked = AnnoyingReminderManager.ShouldUseAnnoyingAlert(annoyingAlert, displayModule);
			AnnoyingAlertCheckBox.IsChecked = _initialAnnoyingAlertChecked;
		}
		InitData(reminders);
	}

	private int? GetAnnoyingAlert()
	{
		if (!_showAnnoyingAlert || !UserDao.IsPro())
		{
			return null;
		}
		bool valueOrDefault = AnnoyingAlertCheckBox.IsChecked == true;
		if (valueOrDefault == _initialAnnoyingAlertChecked)
		{
			return _initialAnnoyingAlert;
		}
		return valueOrDefault ? 1 : 0;
	}

	private void InitData(IEnumerable<TaskReminderModel> reminders)
	{
		if (_isAllDay)
		{
			InitAllDayData(reminders.Select((TaskReminderModel reminder) => reminder.trigger).ToList(), withToday: true, _isSpan);
		}
		else
		{
			InitTimeData(reminders.Select((TaskReminderModel reminder) => reminder.trigger).ToList(), _isSpan);
		}
	}

	private void InitTimeData(IEnumerable<string> reminders, bool showEnd = false)
	{
		List<AdvanceDateModel> list = AdvanceDateModel.BuildTimeDayModels(showEnd);
		if (!_withNull)
		{
			list.RemoveAt(0);
		}
		List<string> list2 = reminders.ToList();
		list[0].Selected = !list2.ToList().Any() && _withNull;
		foreach (string trigger in list2)
		{
			if (_isSpan || !trigger.Contains("END"))
			{
				AdvanceDateModel advanceDateModel = list.FirstOrDefault((AdvanceDateModel m) => EqualRule(m.Trigger, trigger));
				if (advanceDateModel == null)
				{
					list.Add(new AdvanceDateModel(trigger, isAllDay: false)
					{
						Selected = true
					});
				}
				else
				{
					advanceDateModel.Selected = true;
				}
			}
		}
		List<string> list3 = LocalSettings.Settings.TimeCustomRemind.Split(';').ToList();
		list3.Remove("");
		_recentTriggers = list3.Except(list2).ToList();
		if (_recentTriggers.Count > 0)
		{
			list.Add(new AdvanceDateModel
			{
				Order = 199990
			});
			list.Add(new AdvanceDateModel(isEmpty: true, isCustom: false)
			{
				IsEnable = false,
				IsAllDay = true,
				DisplayText = Utils.GetString("RecentUsed"),
				Order = 200000
			});
			AdvanceDateModel advanceDateModel2 = new AdvanceDateModel(_recentTriggers[0], isAllDay: false);
			advanceDateModel2.Order += 200000;
			list.Add(advanceDateModel2);
			if (_recentTriggers.Count > 1)
			{
				AdvanceDateModel advanceDateModel3 = new AdvanceDateModel(_recentTriggers[1], isAllDay: false);
				advanceDateModel3.Order += 200000;
				list.Add(advanceDateModel3);
			}
		}
		list = list.OrderBy((AdvanceDateModel model) => model.Order).ToList();
		if (_tabIndex == 0)
		{
			list[0].HoverSelected = true;
		}
		ReminderlistView.ItemsSource = list;
	}

	private void InitAllDayData(IEnumerable<string> reminders, bool withToday = true, bool showEnd = false)
	{
		List<AdvanceDateModel> list = AdvanceDateModel.BuildAllDayModels(withToday, showEnd);
		if (!_withNull)
		{
			list.RemoveAt(0);
		}
		List<string> list2 = reminders.ToList();
		list[0].Selected = !list2.ToList().Any() && _withNull;
		foreach (string trigger in list2)
		{
			AdvanceDateModel advanceDateModel = list.FirstOrDefault((AdvanceDateModel m) => EqualRule(m.Trigger, trigger));
			if (advanceDateModel == null)
			{
				list.Add(new AdvanceDateModel(trigger, isAllDay: true)
				{
					Selected = true
				});
			}
			else
			{
				advanceDateModel.Selected = true;
			}
		}
		List<string> list3 = LocalSettings.Settings.AllDayCustomRemind.Split(';').ToList();
		list3.Remove("");
		_recentTriggers = list3.Except(list2).ToList();
		if (_recentTriggers.Count > 0)
		{
			list.Add(new AdvanceDateModel
			{
				Order = 199990
			});
			list.Add(new AdvanceDateModel(isEmpty: true, isCustom: true)
			{
				IsEnable = false,
				IsAllDay = true,
				DisplayText = Utils.GetString("RecentUsed"),
				Order = 200000
			});
			AdvanceDateModel advanceDateModel2 = new AdvanceDateModel(_recentTriggers[0], isAllDay: true);
			advanceDateModel2.Order += 200000;
			list.Add(advanceDateModel2);
			if (_recentTriggers.Count > 1)
			{
				AdvanceDateModel advanceDateModel3 = new AdvanceDateModel(_recentTriggers[1], isAllDay: true);
				advanceDateModel3.Order += 200000;
				list.Add(advanceDateModel3);
			}
		}
		list = list.OrderBy((AdvanceDateModel model) => model.Order).ToList();
		if (_tabIndex == 0)
		{
			list[0].HoverSelected = true;
		}
		ReminderlistView.ItemsSource = list;
	}

	private static bool EqualRule(string rule1, string rule2)
	{
		Regex regex = new Regex("(-)?P((\\d{0,})Y)?((\\d{0,})M)?((\\d{0,})W)?((\\d{0,})D)?T?((\\d{0,})H)?((\\d{0,})M)?((\\d{0,})S)?");
		if (!string.IsNullOrEmpty(rule1) && !string.IsNullOrEmpty(rule2))
		{
			bool flag = rule1.Contains("END");
			bool flag2 = rule2.Contains("END");
			Match match = regex.Match(rule1);
			Match match2 = regex.Match(rule2);
			if (match.Success && match2.Success)
			{
				bool flag3 = match.Groups[1].ToString() == "-";
				int.TryParse(match.Groups[7].ToString(), out var result);
				int.TryParse(match.Groups[9].ToString(), out var result2);
				int.TryParse(match.Groups[11].ToString(), out var result3);
				int.TryParse(match.Groups[13].ToString(), out var result4);
				bool flag4 = match2.Groups[1].ToString() == "-";
				int.TryParse(match2.Groups[7].ToString(), out var result5);
				int.TryParse(match2.Groups[9].ToString(), out var result6);
				int.TryParse(match2.Groups[11].ToString(), out var result7);
				int.TryParse(match2.Groups[13].ToString(), out var result8);
				result4 = result * 10080 + result2 * 1440 + result3 * 60 + result4;
				result8 = result5 * 10080 + result6 * 1440 + result7 * 60 + result8;
				if (flag == flag2 && ((result4 == result8 && flag3 == flag4) || (result4 == 0 && result8 == 0)))
				{
					return true;
				}
			}
		}
		return false;
	}

	private void OnReminderClick(bool enter, UpDownSelectViewModel e)
	{
		if (e is AdvanceDateModel reminder)
		{
			SelectReminder(reminder);
			if (_withNull)
			{
				SetEmptyToggled();
			}
		}
	}

	private bool CheckOutOfLimits(int add = 0)
	{
		int num = GetSelectedReminder().Count + add;
		if (!UserDao.IsPro())
		{
			if (num > 2)
			{
				ShowUpgradeProDialog();
				return true;
			}
		}
		else if (num > 5)
		{
			ShowProLimitDialog();
			return true;
		}
		return false;
	}

	private void SelectReminder(AdvanceDateModel reminder)
	{
		if (reminder == null || !reminder.IsEnable)
		{
			return;
		}
		if (reminder.IsEmpty && ReminderlistView.ItemsSource is List<AdvanceDateModel> list)
		{
			list.ForEach(delegate(AdvanceDateModel model)
			{
				model.Selected = false;
			});
		}
		if (reminder.IsCustom)
		{
			reminder.Selected = false;
			if (!CheckOutOfLimits(1))
			{
				ShowCustomReminder();
			}
		}
		else if (reminder.Selected)
		{
			CheckOutOfLimits();
		}
	}

	private void SetEmptyToggled()
	{
		if (ReminderlistView.ItemsSource is List<AdvanceDateModel> list)
		{
			int count = GetSelectedReminder().Count;
			list[0].Selected = count == 0;
		}
	}

	private async void ShowProLimitDialog()
	{
		this.OnCancel?.Invoke(this, null);
		TaskDetailPopup.TryCloseWindow();
		await Task.Delay(100);
		long userLimit = Utils.GetUserLimit(Constants.LimitKind.ReminderNumber);
		new CustomerDialog(Utils.GetString("LimitTips"), string.Format(Utils.GetString("ProRemindCountLimit"), userLimit), MessageBoxButton.OK, (Utils.GetToastWindow() is FrameworkElement dependencyObject) ? Window.GetWindow(dependencyObject) : null).ShowDialog();
	}

	private static async void ShowUpgradeProDialog()
	{
		TaskDetailPopup.TryCloseWindow();
		await Task.Delay(50);
		ProChecker.ShowUpgradeDialog(ProType.MoreReminders);
	}

	private async void ShowCustomReminder()
	{
		if (!_isAllDay)
		{
			ShowCustomTimeReminderDialog();
		}
		else
		{
			ShowAllDayReminderDialog();
		}
	}

	private async void ShowAllDayReminderDialog()
	{
		PopupStateManager.SetCanOpenTimePopup(canOpen: true, delay: true);
		await Task.Delay(50);
		CustomAllDayReminderControl customAllDayReminderControl = new CustomAllDayReminderControl(new AdvanceDateModel("-PT15H", isAllDay: true)
		{
			Date = _date.Date
		}, CustomChooseRemindPopup, _tabIndex == 0);
		customAllDayReminderControl.Save += OnSaveClick;
		customAllDayReminderControl.Show();
	}

	private void OnSaveClick(object sender, string rule)
	{
		List<string> reminders = OnReminderSelected(rule);
		StoreCustomAllDayReminder(rule);
		this.OnSelected?.Invoke(this, new ReminderSelectedEventArgs(reminders, GetAnnoyingAlert()));
	}

	private void StoreCustomAllDayReminder(string rule)
	{
		if (!new List<string> { "TRIGGER:P0DT9H0M0S", "TRIGGER:-P0DT15H0M0S", "TRIGGER:-P2DT15H0M0S", "TRIGGER:-P6DT15H0M0S" }.Contains(rule))
		{
			List<string> list = LocalSettings.Settings.AllDayCustomRemind.Split(';').ToList();
			list.Remove("");
			if (list.Contains(rule))
			{
				list.Remove(rule);
			}
			StringBuilder stringBuilder = new StringBuilder(rule);
			for (int i = 0; i < ((list.Count > 6) ? 6 : list.Count); i++)
			{
				stringBuilder.Append(";").Append(list[i]);
			}
			LocalSettings.Settings.AllDayCustomRemind = stringBuilder.ToString();
			stringBuilder.Clear();
		}
	}

	private void StoreCustomTimeReminder(string rule)
	{
		List<string> list = LocalSettings.Settings.TimeCustomRemind.Split(';').ToList();
		list.Remove("");
		if (list.Contains(rule))
		{
			list.Remove(rule);
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (!new List<string> { "TRIGGER:-PT0S", "TRIGGER:-PT5M", "TRIGGER:-PT30M", "TRIGGER:-PT60M", "TRIGGER:-PT1440M" }.Contains(rule))
		{
			stringBuilder.Append(rule);
		}
		for (int i = 0; i < ((list.Count > 6) ? 6 : list.Count); i++)
		{
			stringBuilder.Append(";").Append(list[i]);
		}
		LocalSettings.Settings.TimeCustomRemind = stringBuilder.ToString();
		stringBuilder.Clear();
	}

	private List<string> OnReminderSelected(string rule)
	{
		List<string> selectedReminder = GetSelectedReminder();
		if (!selectedReminder.Contains(rule))
		{
			selectedReminder.Add(rule);
		}
		return selectedReminder;
	}

	private async void ShowCustomTimeReminderDialog()
	{
		await Task.Delay(50);
		CustomReminderControl customReminderControl = new CustomReminderControl(_date);
		customReminderControl.OnCancel += delegate
		{
			CustomChooseRemindPopup.IsOpen = false;
		};
		customReminderControl.OnSave += delegate(object e, string value)
		{
			List<string> reminders = OnReminderSelected(value);
			StoreCustomTimeReminder(value);
			this.OnSelected?.Invoke(this, new ReminderSelectedEventArgs(reminders, GetAnnoyingAlert()));
			CustomChooseRemindPopup.IsOpen = false;
		};
		CustomChooseRemindPopup.Child = customReminderControl;
		CustomChooseRemindPopup.IsOpen = true;
	}

	private void CancelClick(object sender, RoutedEventArgs e)
	{
		this.OnCancel?.Invoke(this, null);
	}

	private void SaveClick(object sender, RoutedEventArgs e)
	{
		List<string> selectedReminder = GetSelectedReminder();
		for (int num = _recentTriggers.Count - 1; num >= 0; num--)
		{
			if (selectedReminder.Contains(_recentTriggers[num]))
			{
				if (_isAllDay)
				{
					StoreCustomAllDayReminder(_recentTriggers[num]);
				}
				else
				{
					StoreCustomTimeReminder(_recentTriggers[num]);
				}
			}
		}
		this.OnSelected?.Invoke(this, new ReminderSelectedEventArgs(selectedReminder, selectedReminder.Any() ? GetAnnoyingAlert() : new int?(-1)));
	}

	private List<string> GetSelectedReminder()
	{
		return (from AdvanceDateModel m in ReminderlistView.Items
			where m.Selected && !string.IsNullOrEmpty(m.Trigger)
			orderby m.Order
			select m.Trigger).ToList();
	}

	public bool HandleTab(bool shift)
	{
		if (!CustomChooseRemindPopup.IsOpen)
		{
			_tabIndex += 3 + ((!shift) ? 1 : (-1));
			_tabIndex %= 3;
			UiUtils.SetSaveButtonTabSelected(SaveButton, _tabIndex == 1);
			UiUtils.SetCancelButtonTabSelected(CancelButton, _tabIndex == 2);
			return true;
		}
		return CustomChooseRemindPopup.HandleTab(shift);
	}

	public bool HandleEnter()
	{
		EnterSelect();
		return true;
	}

	public bool HandleEsc()
	{
		if (CustomChooseRemindPopup.IsOpen)
		{
			CustomChooseRemindPopup.HandleEsc();
			return true;
		}
		return false;
	}

	public bool UpDownSelect(bool isUp)
	{
		if (!CustomChooseRemindPopup.IsOpen)
		{
			ReminderlistView.UpDownSelect(isUp);
			return true;
		}
		return CustomChooseRemindPopup.HandleUpDown(isUp);
	}

	public bool LeftRightSelect(bool isLeft)
	{
		return false;
	}

	private void EnterSelect()
	{
		switch (_tabIndex)
		{
		case 1:
			SaveClick(null, null);
			return;
		case 2:
			this.OnCancel?.Invoke(this, null);
			return;
		}
		if (!CustomChooseRemindPopup.IsOpen)
		{
			ReminderlistView.HandleEnter();
		}
	}

	private void AnnoyingAlertCheckBox_Click(object sender, RoutedEventArgs e)
	{
		if (AnnoyingAlertCheckBox.IsChecked == true && ProChecker.CheckPro(ProType.ConstantReminder) && LocalSettings.Settings.ExtraSettings.ConstantReminderTipCount < 3)
		{
			LocalSettings.Settings.ExtraSettings.ConstantReminderTipCount++;
			LocalSettings.Settings.Save();
			Utils.Toast(Utils.GetString("ConstantReminderToast"));
		}
	}

	private void AnnoyingAlertCheckBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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
			Uri resourceLocator = new Uri("/TickTick;component/views/setremindercontrol.xaml", UriKind.Relative);
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
			AnnoyingAlertGrid = (Grid)target;
			break;
		case 2:
			AnnoyingAlertCheckBox = (CheckBox)target;
			AnnoyingAlertCheckBox.PreviewMouseLeftButtonDown += AnnoyingAlertCheckBox_PreviewMouseLeftButtonDown;
			AnnoyingAlertCheckBox.Click += AnnoyingAlertCheckBox_Click;
			break;
		case 3:
			SaveButton = (Button)target;
			SaveButton.Click += SaveClick;
			break;
		case 4:
			CancelButton = (Button)target;
			CancelButton.Click += CancelClick;
			break;
		case 5:
			Container = (Grid)target;
			break;
		case 6:
			ReminderlistView = (UpDownSelectListView)target;
			break;
		case 7:
			CustomChooseRemindPopup = (EscPopup)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
