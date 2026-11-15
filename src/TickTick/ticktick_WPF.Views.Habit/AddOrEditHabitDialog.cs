using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using Emoji.Wpf;
using Ical.Net;
using Ical.Net.DataTypes;
using TickTickDao;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.ReminderTime;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Misc.ColorSelector;
using ticktick_WPF.Views.Time;

namespace ticktick_WPF.Views.Habit;

public class AddOrEditHabitDialog : Window, IOkCancelWindow, IComponentConnector
{
	private HabitModel _habit;

	private bool _isNew;

	private DateTime _popupClosedTime;

	private string _beforeSection;

	private bool _isTargetDayChanged;

	private int? _initialAnnoyingAlert;

	private bool _initialAnnoyingAlertChecked;

	private string _beforeTime;

	private List<HabitSectionModel> _allSection;

	internal Grid TitleGrid;

	internal Grid DragGrid;

	internal Grid IconGrid;

	internal Image IconImage;

	internal EscPopup SetIconPopup;

	internal SetHabitIconControl SetIconControl;

	internal TextBox HabitTitleBox;

	internal Border FrequencySelectBorder;

	internal TextBlock FreqText;

	internal EscPopup SetFrequencyPopup;

	internal SetHabitFrequencyControl SetFrequencyControl;

	internal Border GoalBorder;

	internal TextBlock GoalText;

	internal EscPopup SetGoalPopup;

	internal SetHabitGoalControl SetGoalControl;

	internal Border TargetDayBorder;

	internal TextBlock TargetDayTextBlock;

	internal EscPopup TargetDaySelectPopup;

	internal StackPanel TargetDayList;

	internal OptionCheckBox TargetDayCustomRadioButton;

	internal EscPopup TargetDayCustomPopup;

	internal TextBox TargetDayCustomInput;

	internal TextBlock TargetDayCustomInputDay;

	internal Border TargetStartDateBorder;

	internal TextBlock TargetStartDateTextBlock;

	internal EscPopup TargetStartDatePopup;

	internal TickDatePicker TargetStartDatePicker;

	internal Border SectionBorder;

	internal EmjTextBlock SectionNameTextBlock;

	internal EscPopup SectionPopup;

	internal ListView SectionList;

	internal EscPopup AddSectionPopup;

	internal EmojiEditor AddSectionInput;

	internal TextBlock AddSectionError;

	internal SetHabitReminderControl SetReminderControl;

	internal CheckBox AnnoyingAlertCheckBox;

	internal CheckBox RecordCheckBox;

	internal Button SaveButton;

	internal Grid ToastGrid;

	internal TextBlock ToastTextBlock;

	private bool _contentLoaded;

	public AddOrEditHabitDialog(string sectionId = null)
	{
		InitializeComponent();
		base.Title = Utils.GetString("AddHabit");
		_habit = HabitModel.GetDefaultHabit();
		_habit.SectionId = sectionId;
		_isNew = true;
	}

	public AddOrEditHabitDialog(HabitModel habit)
	{
		InitializeComponent();
		base.Title = Utils.GetString("EditHabit");
		_habit = habit;
	}

	private async void OnWindowLoaded(object sender, RoutedEventArgs e)
	{
		if (_habit.RepeatRule == null)
		{
			_habit.RepeatRule = "RRULE:FREQ=DAILY;INTERVAL=1";
		}
		_habit.Unit = HabitUtils.GetUnitText(_habit.Unit);
		HabitTitleBox.Text = _habit.Name;
		SetFreqText(_habit.RepeatRule);
		GoalText.Text = (_habit.IsBoolHabit() ? Utils.GetString("CheckInTheDay") : string.Format(Utils.GetString("DailyTimes"), _habit.Goal, _habit.Unit));
		RecordCheckBox.IsChecked = _habit.RecordEnable == true;
		SetRecordCheckBoxEvent();
		_popupClosedTime = DateTime.Now;
		SetReminderControl.Init(_habit.Reminder);
		if (UserDao.IsPro())
		{
			_initialAnnoyingAlert = _habit.AnnoyingAlert;
			_initialAnnoyingAlertChecked = AnnoyingReminderManager.ShouldUseAnnoyingAlert(_habit.AnnoyingAlert, DisplayModule.Habit);
			AnnoyingAlertCheckBox.IsChecked = _initialAnnoyingAlertChecked;
		}
		SetIcon();
		await Task.Delay(100);
		Activate();
		HabitTitleBox.Focus();
		if (_isNew)
		{
			HabitTitleBox.SelectAll();
		}
		else
		{
			HabitTitleBox.Select(HabitTitleBox.Text.Length, 0);
		}
		foreach (object child in TargetDayList.Children)
		{
			if (child is OptionCheckBox { Tag: string tag } optionCheckBox)
			{
				int result = -1;
				if (int.TryParse(tag, out result) && result > 0)
				{
					optionCheckBox.Text = string.Format(Utils.GetString("HabitCycleDay"), tag);
				}
			}
		}
		UpdateSelectedTargetDay();
		UpdateTargetStartDate();
		UpdateSections(_isNew);
	}

	private void SetRecordCheckBoxEvent()
	{
		RecordCheckBox.Checked += delegate
		{
			UserActCollectUtils.AddClickEvent("habit", "add_edit_habit", "show_auto_habit_log");
		};
		RecordCheckBox.Unchecked += delegate
		{
			UserActCollectUtils.AddClickEvent("habit", "add_edit_habit", "hide_auto_habit_log");
		};
	}

	private void OnTextChanged(object sender, TextChangedEventArgs e)
	{
		UpdateSaveButton();
	}

	private void UpdateSaveButton()
	{
		SaveButton.IsEnabled = !string.IsNullOrEmpty(HabitTitleBox.Text) || !string.IsNullOrEmpty(HabitTitleBox.Tag as string);
	}

	private void OpenFrequencyPopup(object sender, MouseButtonEventArgs e)
	{
		SetFrequencyControl.Init(_habit.RepeatRule);
		SetFrequencyPopup.IsOpen = true;
		HwndHelper.SetFocus(SetFrequencyPopup);
	}

	private void OnFreqSaved(object sender, string rule)
	{
		_habit.RepeatRule = rule;
		SetFreqText(rule);
	}

	private void SetFreqText(string rule)
	{
		if (rule == null)
		{
			FreqText.Text = Utils.GetString("EveryDay");
		}
		else if (rule.Contains("TT_TIMES"))
		{
			Match match = new Regex("TT_TIMES=(\\d{0,})").Match(rule);
			if (match.Success && int.TryParse(match.Groups[1].ToString(), out var result))
			{
				FreqText.Text = string.Format(Utils.GetString("TimesPerWeekText"), result, Utils.GetString((result > 1) ? "PublicDays" : "PublicDay"));
				return;
			}
			FreqText.Text = string.Format(Utils.GetString("TimesPerWeekText"), 3, Utils.GetString("PublicDays"));
			_habit.RepeatRule = "RRULE:FREQ=WEEKLY;INTERVAL=1;TT_TIMES=" + 3;
		}
		else
		{
			RecurrenceModel recurrenceModel = RecurrenceModel.GetRecurrenceModel(rule);
			bool flag = (recurrenceModel.Frequency == FrequencyType.Daily && recurrenceModel.Interval == 1) || IsWeekDaily(recurrenceModel);
			FreqText.Text = (flag ? Utils.GetString("EveryDay") : RRuleUtils.RRule2String("2", rule, null, showUntil: false));
		}
	}

	private bool IsWeekDaily(RecurrenceModel pattern)
	{
		if (pattern.Frequency == FrequencyType.Weekly)
		{
			List<DayOfWeek> dayOdWeeks = pattern.ByDay?.Select((WeekDay bd) => bd.DayOfWeek).ToList();
			if (dayOdWeeks != null)
			{
				return Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>().All((DayOfWeek value) => dayOdWeeks.Contains(value));
			}
			return true;
		}
		return false;
	}

	private void HideFreqPopup(object sender, EventArgs e)
	{
		SetFrequencyPopup.IsOpen = false;
	}

	private void OpenGoalPopup(object sender, MouseButtonEventArgs e)
	{
		SetGoalControl.Init(_habit);
		SetGoalPopup.IsOpen = true;
	}

	private void HideGoalPopup(object sender, EventArgs e)
	{
		SetGoalPopup.IsOpen = false;
	}

	private void OnGoalSaved(object sender, HabitGoalModel goal)
	{
		if (goal.Type == HabitType.Real.ToString())
		{
			_habit.Type = goal.Type;
			_habit.Goal = goal.Goal;
			_habit.Step = goal.Step;
			_habit.Unit = goal.Unit;
		}
		else
		{
			_habit.Type = HabitType.Boolean.ToString();
			_habit.Step = 1.0;
			_habit.Goal = 1.0;
			_habit.Unit = Utils.GetString("Count");
		}
		SetGoalPopup.IsOpen = false;
		GoalText.Text = (_habit.IsBoolHabit() ? Utils.GetString("CheckInTheDay") : string.Format(Utils.GetString("DailyTimes"), _habit.Goal, _habit.Unit));
	}

	private void OnSelecteIconClick(object sender, MouseButtonEventArgs e)
	{
		SetIconPopup.IsOpen = true;
		SetIconControl.Reset(_habit.IconRes, _habit.Color);
		HwndHelper.SetFocus(SetIconPopup);
	}

	private void SetIcon()
	{
		IconImage.Source = HabitService.GetIcon(_habit.IconRes, _habit.Color);
		HabitTitleBox.Tag = HabitUtils.GetHabitHintText(_habit.IconRes);
	}

	private void OnIconSelected(object sender, EventArgs e)
	{
		if (sender is SetHabitIconControl setHabitIconControl)
		{
			if (setHabitIconControl.IsIcon)
			{
				_habit.IconRes = setHabitIconControl.SelectedIcon;
				_habit.Color = setHabitIconControl.IconColor;
			}
			else
			{
				_habit.IconRes = "txt_" + setHabitIconControl.IconText;
				string text = setHabitIconControl.TextColor ?? "";
				if (_habit.Color != text)
				{
					_habit.Color = text;
					ColorSelector.TryAddClickEvent(text);
				}
			}
		}
		SetIconPopup.IsOpen = false;
		SetIcon();
		UpdateSaveButton();
	}

	private void HideIconPopup(object sender, EventArgs e)
	{
		SetIconPopup.IsOpen = false;
		UpdateSaveButton();
	}

	private void OnStoryCompleted(object sender, EventArgs e)
	{
		ToastGrid.Visibility = Visibility.Collapsed;
	}

	private void OnSaveClick(object sender, RoutedEventArgs e)
	{
		Save();
	}

	private async void Save()
	{
		_habit.RecordEnable = RecordCheckBox.IsChecked == true;
		if (string.IsNullOrEmpty(HabitTitleBox.Text))
		{
			_habit.Name = HabitTitleBox.Tag as string;
		}
		else
		{
			_habit.Name = HabitTitleBox.Text;
		}
		if (_isNew)
		{
			_habit.CreatedTime = DateTime.Now;
			_habit.SyncStatus = 0;
			HabitModel habit = _habit;
			habit.SortOrder = await HabitDao.GetNewHabitSortOrderBySectionId(_habit.SectionId);
		}
		else
		{
			_habit.SyncStatus = 1;
		}
		string reminder = SetReminderControl.GetReminderString();
		if (_habit.Reminder != reminder)
		{
			await ReminderDelayDao.DeleteByIdAsync(_habit.Id, "habit");
		}
		_habit.Reminder = reminder;
		if (UserDao.IsPro())
		{
			if (string.IsNullOrEmpty(reminder))
			{
				_habit.AnnoyingAlert = -1;
			}
			else
			{
				bool valueOrDefault = AnnoyingAlertCheckBox.IsChecked == true;
				_habit.AnnoyingAlert = ((valueOrDefault == _initialAnnoyingAlertChecked) ? _initialAnnoyingAlert : new int?(valueOrDefault ? 1 : 0));
			}
		}
		if (!_isTargetDayChanged || await CheckHabitTargetDay())
		{
			await HabitService.UpdateHabit(_habit);
			await TimerService.TryUpdateTimerIcon(_habit.Id, _habit.IconRes, _habit.Color);
			DataChangedNotifier.NotifyHabitsChanged();
			if (ABTestManager.IsNewRemindCalculate())
			{
				await HabitReminderCalculator.RecalHabitReminder(_habit.Id);
			}
			else
			{
				ReminderCalculator.AssembleReminders();
			}
			Close();
		}
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private void Toast(object sender, string text)
	{
		ToastGrid.Visibility = Visibility.Visible;
		ToastTextBlock.Text = text;
		((Storyboard)FindResource("ToastShowAndHide")).Begin();
	}

	public void OnCancel()
	{
		if (SetReminderControl.TimePopup.IsOpen)
		{
			SetReminderControl.TimePopup.IsOpen = false;
		}
		else
		{
			Close();
		}
	}

	public void Ok()
	{
		if (SetFrequencyPopup.IsOpen)
		{
			SetFrequencyControl.OnEnter();
		}
		else if (SetGoalPopup.IsOpen)
		{
			SetGoalControl.OnEnter();
		}
		else if (TargetStartDatePopup.IsOpen)
		{
			OnTargetStartDateOk(null, null);
		}
		else if (!TargetDaySelectPopup.IsOpen && !SectionPopup.IsOpen && !SetReminderControl.TimePopup.IsOpen && SaveButton.IsEnabled)
		{
			Save();
		}
	}

	public async Task<bool> CheckHabitTargetDay()
	{
		if (_habit.TargetDays <= 0)
		{
			return true;
		}
		HabitModel habit = _habit;
		habit.CompletedCyclesList = await HabitDao.GetHabitCompletedCycles(_habit);
		_habit.CompletedCycles = _habit.CompletedCyclesList.Count((CompletedCycle c) => c.isComplete);
		string content = string.Format(Utils.GetString("HabitComfirmEditTargetInfo"), GetFormatDateStr(), _habit.TargetDays);
		return new CustomerDialog(null, content, Utils.GetString("Continue"), Utils.GetString("Cancel")).ShowDialog() == true;
	}

	private async void UpdateSections(bool updateReminder = true)
	{
		_allSection = HabitSectionCache.GetSections();
		HabitSectionModel section = _allSection?.FirstOrDefault((HabitSectionModel s) => s.Id == _habit.SectionId);
		if (section != null)
		{
			string sectionName = SectionNameTextBlock.Text.ToString();
			SectionNameTextBlock.Text = section.DisplayName;
			string container = SetReminderControl.GetReminderString() ?? string.Empty;
			bool flag = string.IsNullOrEmpty(container);
			bool flag2 = !flag && !container.Contains(",");
			if ((!(flag && updateReminder) && (!(flag || flag2) || !_isNew) && (!flag2 || !string.IsNullOrEmpty(_habit.Reminder))) || container.Contains(","))
			{
				return;
			}
			string beforeSectionDefaultTime = await HabitService.GetSectionDefaultTime(sectionName);
			string text = await HabitService.GetSectionDefaultTime(section.DisplayName);
			if (!string.IsNullOrEmpty(text))
			{
				if (!new List<string> { "09:00", "13:00", "20:00", "" }.Contains(container) || (beforeSectionDefaultTime != _beforeTime && !string.IsNullOrEmpty(_beforeTime)))
				{
					_beforeTime = container.ToString();
					return;
				}
				SetReminderControl.Init(text);
				_beforeTime = SetReminderControl.GetReminderString() ?? string.Empty;
			}
		}
		else
		{
			SectionNameTextBlock.Text = Utils.GetString("HabitSectionOthers");
		}
	}

	private string GetFormatDateStr()
	{
		return DateUtils.FormatShortDate(_habit.GetStartDate());
	}

	private void UpdateTargetStartDate()
	{
		if (!HabitModel.IsValidTargetStartDate(DateUtils.ParseDateTime(_habit.TargetStartDate?.ToString())))
		{
			_habit.TargetStartDate = DateUtils.DateTimeToInt(_habit.GetStartDate());
		}
		TargetStartDateTextBlock.Text = GetFormatDateStr();
	}

	private void UpdateSelectedTargetDay()
	{
		if (_habit.TargetDays < 0 || !_habit.TargetDays.HasValue)
		{
			_habit.TargetDays = 0;
		}
		else if (_habit.TargetDays > 999)
		{
			_habit.TargetDays = 999;
		}
		TargetDayTextBlock.Text = ((_habit.TargetDays == 0) ? Utils.GetString("Forever") : string.Format(Utils.GetString("HabitCycleDay"), _habit.TargetDays));
		OptionCheckBox optionCheckBox = null;
		TargetDayCustomPopup.IsOpen = false;
		TargetDayCustomInput.Text = _habit.TargetDays.ToString();
		int num = TargetDayList.Children?.Count ?? 0;
		int i;
		for (i = 0; i < num; i++)
		{
			if (TargetDayList.Children[i] is OptionCheckBox { Tag: string tag } optionCheckBox2)
			{
				if (tag == _habit.TargetDays.ToString())
				{
					optionCheckBox2.Selected = true;
					break;
				}
				optionCheckBox2.Selected = false;
				if (tag == "-1")
				{
					optionCheckBox = optionCheckBox2;
				}
			}
		}
		if (i == num)
		{
			optionCheckBox.Selected = true;
		}
	}

	private void OpenTargetDayPopup(object sender, MouseButtonEventArgs e)
	{
		UpdateSelectedTargetDay();
		TargetDaySelectPopup.IsOpen = true;
	}

	private void OnTargetDayCustomInputTextChanged(object sender, TextChangedEventArgs e)
	{
		if (!(sender is TextBox textBox) || string.IsNullOrEmpty(textBox.Text))
		{
			return;
		}
		textBox.TextChanged -= OnTargetDayCustomInputTextChanged;
		int result = 1;
		if (int.TryParse(textBox.Text, out result))
		{
			if (result < 1)
			{
				result = 1;
			}
			if (result > 999)
			{
				result = 999;
			}
		}
		else
		{
			result = 1;
		}
		textBox.Text = result.ToString();
		textBox.CaretIndex = result.ToString().Length;
		TargetDayCustomInputDay.Text = ((result > 1) ? Utils.GetString("PublicDays") : Utils.GetString("PublicDay"));
		textBox.TextChanged += OnTargetDayCustomInputTextChanged;
	}

	private void OnTargetDayCustomInputPreviewTextInput(object sender, TextCompositionEventArgs e)
	{
		Regex regex = new Regex("[^0-9.-]+");
		e.Handled = regex.IsMatch(e.Text);
	}

	private void OpenTargetStartDatePopup(object sender, MouseButtonEventArgs e)
	{
		TargetStartDatePicker.SetData(_habit.GetStartDate());
		TargetStartDatePopup.IsOpen = true;
	}

	private void OnTargetDaySelectCustomOk(object sender, RoutedEventArgs e)
	{
		int result = 0;
		int.TryParse(TargetDayCustomInput.Text, out result);
		_habit.TargetDays = result;
		UpdateSelectedTargetDay();
		TargetDayCustomPopup.IsOpen = false;
		TargetDaySelectPopup.IsOpen = false;
	}

	private void OnPopupCancelClick(object sender, RoutedEventArgs e)
	{
		CloseAllPopup();
	}

	private void CloseAllPopup()
	{
		TargetDayCustomPopup.IsOpen = false;
		TargetDaySelectPopup.IsOpen = false;
		TargetStartDatePopup.IsOpen = false;
		AddSectionPopup.IsOpen = false;
		SectionPopup.IsOpen = false;
	}

	private void OnTargetStartDateOk(object sender, RoutedEventArgs e)
	{
		DateTime time = TargetStartDatePicker.SelectedDate ?? DateTime.Today;
		_habit.TargetStartDate = DateUtils.DateTimeToInt(time);
		UpdateTargetStartDate();
		TargetStartDatePopup.IsOpen = false;
	}

	private async void OnTargetDaySelectClick(object sender, MouseButtonEventArgs e)
	{
		_isTargetDayChanged = true;
		if (sender is OptionCheckBox { Tag: var tag } optionCheckBox)
		{
			if (tag?.ToString() == "-1")
			{
				TargetDaySelectPopup.IsOpen = false;
				TargetDayCustomPopup.IsOpen = true;
				TargetDayCustomInput.Text = string.Empty;
				await Task.Delay(100);
				TargetDayCustomInput.Focus();
				return;
			}
			int num = TargetDayList.Children?.Count ?? 0;
			int i = 0;
			int result = 0;
			if (optionCheckBox.Tag is string s)
			{
				if (!int.TryParse(s, out result))
				{
					return;
				}
				if (result > 0)
				{
					_habit.TargetDays = result;
				}
				else if (result == 0)
				{
					_habit.TargetDays = 0;
				}
			}
			for (; i < num; i++)
			{
				if (TargetDayList.Children[i] is OptionCheckBox { Selected: not false, Tag: string tag2 } optionCheckBox2)
				{
					if (!int.TryParse(tag2, out var result2))
					{
						return;
					}
					optionCheckBox2.Selected = result2 == result;
				}
			}
			UpdateSelectedTargetDay();
			TargetDaySelectPopup.IsOpen = false;
		}
		TargetDayCustomPopup.IsOpen = false;
	}

	private async void OpenSectionPopup(object sender, MouseButtonEventArgs e)
	{
		if (_allSection == null || _allSection.Count == 0)
		{
			await HabitService.InitSections();
			_allSection = await HabitService.GetHabitSections();
		}
		List<HabitSectionListViewModel> list = new List<HabitSectionListViewModel>();
		bool flag = false;
		foreach (HabitSectionModel item in _allSection)
		{
			HabitSectionListViewModel habitSectionListViewModel = new HabitSectionListViewModel
			{
				Selected = (_habit.SectionId == item.Id),
				Title = item.DisplayName,
				Section = item,
				SortOrder = item.SortOrder
			};
			list.Add(habitSectionListViewModel);
			flag = flag || habitSectionListViewModel.Selected;
		}
		list.Add(new HabitSectionListViewModel(HabitSectionModel.GetDefault())
		{
			Selected = !flag,
			Title = Utils.GetString("HabitSectionOthers"),
			IsOther = true
		});
		list.Sort((HabitSectionListViewModel a, HabitSectionListViewModel b) => a.SortOrder.CompareTo(b.SortOrder));
		list.Add(new HabitSectionListViewModel
		{
			Selected = false,
			Title = Utils.GetString("AddSection"),
			IsAdd = true
		});
		ItemsSourceHelper.SetItemsSource(SectionList, list);
		SectionPopup.IsOpen = true;
	}

	private async void OnSelectSectionClick(object sender, MouseButtonEventArgs e)
	{
		if (SectionList.ItemsSource is ObservableCollection<HabitSectionListViewModel> observableCollection)
		{
			foreach (HabitSectionListViewModel item in observableCollection)
			{
				item.Selected = false;
			}
		}
		if (!(sender is OptionCheckBox { DataContext: HabitSectionListViewModel dataContext }))
		{
			return;
		}
		dataContext.Selected = true;
		if (dataContext.IsAdd)
		{
			if (await HabitUtils.CheckSectionLimit())
			{
				AddSectionPopup.IsOpen = true;
				AddSectionInput.Text = string.Empty;
			}
			SectionPopup.IsOpen = true;
			return;
		}
		AddSectionPopup.IsOpen = false;
		if (SectionList.ItemsSource is ObservableCollection<HabitSectionListViewModel> observableCollection2)
		{
			foreach (HabitSectionListViewModel item2 in observableCollection2)
			{
				if (item2.Selected)
				{
					_habit.SectionId = item2.Section?.Id;
				}
			}
		}
		AddSectionPopup.IsOpen = false;
		SectionPopup.IsOpen = false;
		UpdateSections();
	}

	private async void OnAddSectionOkClicked(object sender, RoutedEventArgs e)
	{
		var (flag, text) = await HabitService.CheckSectionName(AddSectionInput.Text.Trim());
		if (flag)
		{
			HabitSectionModel model = new HabitSectionModel
			{
				Name = AddSectionInput.Text
			};
			await HabitService.AddSections(new List<HabitSectionModel> { model });
			_habit.SectionId = model.Id;
			AddSectionPopup.IsOpen = false;
			SectionPopup.IsOpen = false;
		}
		else
		{
			AddSectionError.Text = text;
			AddSectionError.Visibility = Visibility.Visible;
		}
		UpdateSections();
	}

	private void OnSectionOkClicked(object sender, RoutedEventArgs e)
	{
		if (SectionList.ItemsSource is ObservableCollection<HabitSectionListViewModel> observableCollection)
		{
			foreach (HabitSectionListViewModel item in observableCollection)
			{
				if (item.Selected)
				{
					_habit.SectionId = item.Section?.Id;
				}
			}
		}
		AddSectionPopup.IsOpen = false;
		SectionPopup.IsOpen = false;
		UpdateSections();
	}

	private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (!HabitTitleBox.IsMouseOver)
		{
			HabitTitleBox.TabIndex = -1;
			Keyboard.ClearFocus();
		}
		if (!SectionPopup.IsMouseOver && !AddSectionPopup.IsMouseOver && !TargetDayCustomPopup.IsMouseOver && !TargetDaySelectPopup.IsMouseOver && !TargetStartDatePopup.IsMouseOver)
		{
			CloseAllPopup();
		}
	}

	private async void OnAddSectionInputTextChanged(object sender, EventArgs e)
	{
		if (!NameUtils.IsValidColumnName(AddSectionInput.Text) && !string.IsNullOrEmpty(AddSectionInput.Text))
		{
			AddSectionError.Text = Utils.GetString("SectionNotValid");
			AddSectionError.Visibility = Visibility.Visible;
		}
		else
		{
			AddSectionError.Visibility = Visibility.Collapsed;
		}
	}

	[Obsolete]
	private void OnWindowLocationChanged(object sender, EventArgs e)
	{
		MethodInfo method = typeof(Popup).GetMethod("UpdatePosition", BindingFlags.Instance | BindingFlags.NonPublic);
		if (!(method == null))
		{
			if (AddSectionPopup.IsOpen)
			{
				method.Invoke(AddSectionPopup, null);
			}
			if (SectionPopup.IsOpen)
			{
				method.Invoke(SectionPopup, null);
			}
			if (TargetDaySelectPopup.IsOpen)
			{
				method.Invoke(TargetDaySelectPopup, null);
			}
			if (TargetDayCustomPopup.IsOpen)
			{
				method.Invoke(TargetDayCustomPopup, null);
			}
		}
	}

	private void OnCustomTargetKeyUp(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Return)
		{
			OnTargetDaySelectCustomOk(null, null);
			e.Handled = true;
		}
	}

	private void AnnoyingAlertCheckBox_Click(object sender, RoutedEventArgs e)
	{
		if (AnnoyingAlertCheckBox.IsChecked == true)
		{
			if (!ProChecker.CheckPro(ProType.ConstantReminder))
			{
				AnnoyingAlertCheckBox.IsChecked = false;
			}
			else if (LocalSettings.Settings.ExtraSettings.ConstantReminderTipCount < 3)
			{
				LocalSettings.Settings.ExtraSettings.ConstantReminderTipCount++;
				LocalSettings.Settings.Save();
				Utils.Toast(Utils.GetString("ConstantReminderToast"));
			}
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
			Uri resourceLocator = new Uri("/TickTick;component/views/habit/addoredithabitdialog.xaml", UriKind.Relative);
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
			((AddOrEditHabitDialog)target).Loaded += OnWindowLoaded;
			((AddOrEditHabitDialog)target).PreviewMouseLeftButtonDown += OnPreviewMouseDown;
			break;
		case 2:
			((Storyboard)target).Completed += OnStoryCompleted;
			break;
		case 3:
			TitleGrid = (Grid)target;
			break;
		case 4:
			DragGrid = (Grid)target;
			break;
		case 5:
			IconGrid = (Grid)target;
			IconGrid.MouseLeftButtonUp += OnSelecteIconClick;
			break;
		case 6:
			IconImage = (Image)target;
			break;
		case 7:
			SetIconPopup = (EscPopup)target;
			break;
		case 8:
			SetIconControl = (SetHabitIconControl)target;
			break;
		case 9:
			HabitTitleBox = (TextBox)target;
			HabitTitleBox.TextChanged += OnTextChanged;
			break;
		case 10:
			FrequencySelectBorder = (Border)target;
			FrequencySelectBorder.MouseLeftButtonUp += OpenFrequencyPopup;
			break;
		case 11:
			FreqText = (TextBlock)target;
			break;
		case 12:
			SetFrequencyPopup = (EscPopup)target;
			break;
		case 13:
			SetFrequencyControl = (SetHabitFrequencyControl)target;
			break;
		case 14:
			GoalBorder = (Border)target;
			GoalBorder.MouseLeftButtonUp += OpenGoalPopup;
			break;
		case 15:
			GoalText = (TextBlock)target;
			break;
		case 16:
			SetGoalPopup = (EscPopup)target;
			break;
		case 17:
			SetGoalControl = (SetHabitGoalControl)target;
			break;
		case 18:
			TargetDayBorder = (Border)target;
			TargetDayBorder.MouseLeftButtonUp += OpenTargetDayPopup;
			break;
		case 19:
			TargetDayTextBlock = (TextBlock)target;
			break;
		case 20:
			TargetDaySelectPopup = (EscPopup)target;
			break;
		case 21:
			TargetDayList = (StackPanel)target;
			break;
		case 22:
			TargetDayCustomRadioButton = (OptionCheckBox)target;
			break;
		case 23:
			TargetDayCustomPopup = (EscPopup)target;
			break;
		case 24:
			TargetDayCustomInput = (TextBox)target;
			TargetDayCustomInput.TextChanged += OnTargetDayCustomInputTextChanged;
			TargetDayCustomInput.PreviewTextInput += OnTargetDayCustomInputPreviewTextInput;
			TargetDayCustomInput.PreviewKeyUp += OnCustomTargetKeyUp;
			break;
		case 25:
			TargetDayCustomInputDay = (TextBlock)target;
			break;
		case 26:
			((Button)target).Click += OnTargetDaySelectCustomOk;
			break;
		case 27:
			((Button)target).Click += OnPopupCancelClick;
			break;
		case 28:
			TargetStartDateBorder = (Border)target;
			TargetStartDateBorder.MouseLeftButtonUp += OpenTargetStartDatePopup;
			break;
		case 29:
			TargetStartDateTextBlock = (TextBlock)target;
			break;
		case 30:
			TargetStartDatePopup = (EscPopup)target;
			break;
		case 31:
			TargetStartDatePicker = (TickDatePicker)target;
			break;
		case 32:
			((Button)target).Click += OnTargetStartDateOk;
			break;
		case 33:
			((Button)target).Click += OnPopupCancelClick;
			break;
		case 34:
			SectionBorder = (Border)target;
			SectionBorder.MouseLeftButtonUp += OpenSectionPopup;
			break;
		case 35:
			SectionNameTextBlock = (EmjTextBlock)target;
			break;
		case 36:
			SectionPopup = (EscPopup)target;
			break;
		case 37:
			SectionList = (ListView)target;
			break;
		case 38:
			AddSectionPopup = (EscPopup)target;
			break;
		case 39:
			AddSectionInput = (EmojiEditor)target;
			break;
		case 40:
			AddSectionError = (TextBlock)target;
			break;
		case 41:
			((Button)target).Click += OnAddSectionOkClicked;
			break;
		case 42:
			((Button)target).Click += OnPopupCancelClick;
			break;
		case 43:
			SetReminderControl = (SetHabitReminderControl)target;
			break;
		case 44:
			AnnoyingAlertCheckBox = (CheckBox)target;
			AnnoyingAlertCheckBox.PreviewMouseLeftButtonDown += AnnoyingAlertCheckBox_PreviewMouseLeftButtonDown;
			AnnoyingAlertCheckBox.Click += AnnoyingAlertCheckBox_Click;
			break;
		case 45:
			RecordCheckBox = (CheckBox)target;
			break;
		case 46:
			((Button)target).Click += OnCancelClick;
			break;
		case 47:
			SaveButton = (Button)target;
			SaveButton.Click += OnSaveClick;
			break;
		case 48:
			ToastGrid = (Grid)target;
			break;
		case 49:
			ToastTextBlock = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
