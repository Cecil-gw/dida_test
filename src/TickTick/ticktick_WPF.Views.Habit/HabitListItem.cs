using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using Emoji.Wpf;
using TickTickUtils;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Pomo;

namespace ticktick_WPF.Views.Habit;

public class HabitListItem : Grid, IComponentConnector
{
	private HabitListControl _listParent;

	private UIElement _clickElement;

	private bool _isInChecking;

	private bool _canShowPop;

	private HabitItemViewModel _model;

	internal HabitListItem Container;

	internal Border BackBorder;

	internal Grid IconGrid;

	internal Ellipse IconEllipse;

	internal EmjTextBlock IconText;

	internal Border IconHoverBorder;

	internal ContentControl DateCheckIn;

	internal ItemsControl CheckInItems;

	internal EmjTextBlock NameTextBlock;

	internal TextBlock TotalText;

	internal TextBlock CurrentText;

	private bool _contentLoaded;

	public HabitItemViewModel Model => base.DataContext as HabitItemViewModel;

	public HabitListItem()
	{
		InitializeComponent();
	}

	private void Container_Loaded(object sender, RoutedEventArgs e)
	{
		DataChangedNotifier.InterfaceStyleChanged += DataChangedNotifier_InterfaceStyleChanged;
		DataChangedNotifier.ThemeModeChanged += DataChangedNotifier_InterfaceStyleChanged;
		DataChangedNotifier_InterfaceStyleChanged(null, null);
	}

	private void Container_Unloaded(object sender, RoutedEventArgs e)
	{
		DataChangedNotifier.InterfaceStyleChanged -= DataChangedNotifier_InterfaceStyleChanged;
		DataChangedNotifier.ThemeModeChanged -= DataChangedNotifier_InterfaceStyleChanged;
	}

	private void DataChangedNotifier_InterfaceStyleChanged(object sender, EventArgs e)
	{
		switch (LocalSettings.Settings.ExtraSettings.InterfaceStyle)
		{
		case InterfaceStyle.Flat:
			BackBorder.SetResourceReference(Panel.BackgroundProperty, "CardColor");
			break;
		case InterfaceStyle.Card:
			if (LocalSettings.Settings.ThemeId == "White")
			{
				BackBorder.SetResourceReference(Panel.BackgroundProperty, "LightGrayCardColor");
			}
			else
			{
				BackBorder.SetResourceReference(Panel.BackgroundProperty, "CardColor");
			}
			break;
		}
	}

	private async void OnCheckItemClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		IconGrid.IsEnabled = false;
		try
		{
			if (_isInChecking)
			{
				return;
			}
			HabitItemViewModel model = Model;
			if (model != null)
			{
				_isInChecking = true;
				HabitDayCheckModel habitDayCheckModel = (model.ShowDateCheckIn ? model.DateCheckIn : model.WeekCheckIns.FirstOrDefault((HabitDayCheckModel w) => w.Date == DateTime.Today));
				if (habitDayCheckModel?.CheckInModel != null)
				{
					await CheckDayCheck(habitDayCheckModel, IconGrid);
				}
			}
		}
		finally
		{
			IconGrid.IsEnabled = true;
			_isInChecking = false;
		}
	}

	private async Task ResetHabit(HabitDayCheckModel dayCheck)
	{
		HabitItemViewModel model = Model;
		if (model != null)
		{
			if (dayCheck.IsBooleanHabit || Math.Abs(model.Habit.Step) < 0.001)
			{
				GetParent()?.ShowAddStepPop(_clickElement, Utils.GetString("Reseted"));
				dayCheck.CheckInModel.Value = 0.0;
				dayCheck.Percent = 0.0;
				dayCheck.Status = 0;
				await Task.Delay(10);
			}
			if (await HabitService.ResetCheckInHabit(model.Id, dayCheck.Date, model.Habit.Type, this))
			{
				dayCheck.SetData(null, model.Habit);
			}
		}
	}

	private async Task CheckHabit(HabitDayCheckModel dayCheck, HabitModel model)
	{
		if (model.IsBoolHabit())
		{
			await CheckInBooleanHabit(dayCheck);
			GetParent()?.ShowAddStepPop(_clickElement, Utils.GetString("Done"));
			return;
		}
		if (Math.Abs(model.Step + 1.0) <= 0.001)
		{
			GetParent()?.ManuallyRecordCheckIn(dayCheck, _clickElement);
			return;
		}
		HabitListControl parent = GetParent();
		if (parent != null)
		{
			HabitAction action = ((!(Math.Abs(model.Step) < 1E-05)) ? HabitAction.AutoRecord : HabitAction.CheckIn);
			await parent.CheckInRealHabit(dayCheck, model, model.Step, _clickElement, action);
		}
	}

	private async Task CheckInBooleanHabit(HabitDayCheckModel dayCheck)
	{
		HabitModel model = Model.Habit;
		if (dayCheck?.CheckInModel != null && model != null)
		{
			dayCheck.CheckInModel.Value = dayCheck.CheckInModel.Goal;
			dayCheck.Status = 2;
			Utils.PlayCompletionSound();
			await Task.Delay(16);
			await HabitService.CheckInHabit(model.Id, dayCheck.Date, -1.0, increment: true, checkShowRecord: true, 0, null, this, HabitAction.CheckIn);
		}
	}

	private HabitListControl GetParent()
	{
		_listParent = _listParent ?? Utils.FindParent<HabitListControl>(this);
		return _listParent;
	}

	private void OnItemSelected(object sender, MouseButtonEventArgs e)
	{
		if (!CheckInItems.IsMouseOver && !IconGrid.IsMouseOver && !DateCheckIn.IsMouseOver && base.DataContext is HabitItemViewModel habitItemViewModel)
		{
			GetParent()?.OnItemSelected(habitItemViewModel.Id);
		}
	}

	private async void ShowOptionPopup(object sender, MouseButtonEventArgs e)
	{
		if (CheckInItems.IsMouseOver || DateCheckIn.IsMouseOver)
		{
			return;
		}
		HabitItemViewModel model = Model;
		if (!_canShowPop || model?.Habit == null)
		{
			return;
		}
		_canShowPop = false;
		List<OperationItemViewModel> typeList;
		if (model.Habit.Status != 1)
		{
			typeList = new List<OperationItemViewModel>
			{
				new OperationItemViewModel(ActionType.EditHabit),
				new OperationItemViewModel(ActionType.Archive),
				new OperationItemViewModel(ActionType.Delete)
			};
			if (LocalSettings.Settings.EnableFocus)
			{
				HabitCheckInModel habitCheckInModel = await HabitCheckInDao.GetHabitCheckInByHabitIdAndStamp(model.Habit.Id, DateTime.Today.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
				if (habitCheckInModel == null || (!habitCheckInModel.IsComplete() && !habitCheckInModel.IsUnComplete()))
				{
					typeList.Insert(1, new OperationItemViewModel(ActionType.StartFocus)
					{
						Selectable = false,
						SubActions = new List<OperationItemViewModel>
						{
							new OperationItemViewModel(ActionType.StartPomo)
							{
								Enable = (!TickFocusManager.Working || LocalSettings.Settings.PomoType == FocusConstance.Focus)
							},
							new OperationItemViewModel(ActionType.StartTiming)
							{
								Enable = (!TickFocusManager.Working || LocalSettings.Settings.PomoType == FocusConstance.Timing)
							}
						}
					});
				}
			}
		}
		else
		{
			typeList = new List<OperationItemViewModel>
			{
				new OperationItemViewModel(ActionType.RecoverHabit),
				new OperationItemViewModel(ActionType.Delete)
			};
		}
		OperationDialog operationDialog = new OperationDialog(model.Habit.Id, typeList);
		operationDialog.SetPlaceTarget(this);
		operationDialog.Operated += OnOptionClick;
		operationDialog.Show();
	}

	private void OnOptionClick(object sender, KeyValuePair<string, ActionType> e)
	{
		switch (e.Value)
		{
		case ActionType.EditHabit:
			OnEditClick(e.Key);
			break;
		case ActionType.Archive:
			ChangeHabitArchiveStatus(e.Key, isArchive: true);
			break;
		case ActionType.RecoverHabit:
			ChangeHabitArchiveStatus(e.Key, isArchive: false);
			break;
		case ActionType.Delete:
			OnDeleteClick(e.Key);
			break;
		case ActionType.StartPomo:
			UserActCollectUtils.AddClickEvent("focus", "start_from", "cm_single_habit");
			TickFocusManager.TryStartFocusHabit(e.Key, isPomo: true);
			break;
		case ActionType.StartTiming:
			UserActCollectUtils.AddClickEvent("focus", "start_from", "cm_single_habit");
			TickFocusManager.TryStartFocusHabit(e.Key, isPomo: false);
			break;
		}
	}

	private async void OnEditClick(string habitId)
	{
		HabitModel habitModel = await HabitDao.GetHabitById(habitId);
		if (habitModel != null)
		{
			UserActCollectUtils.AddClickEvent("habit", "list_cm", "edit");
			AddOrEditHabitDialog addOrEditHabitDialog = new AddOrEditHabitDialog(habitModel);
			addOrEditHabitDialog.Owner = Window.GetWindow(this);
			addOrEditHabitDialog.ShowDialog();
		}
	}

	private async void ChangeHabitArchiveStatus(string habitId, bool isArchive)
	{
		if (isArchive || await HabitUtils.CheckHabitLimit())
		{
			Utils.Toast(Utils.GetString(isArchive ? "Archived" : "Recovered"));
			UserActCollectUtils.AddClickEvent("habit", "list_cm", isArchive ? "archive" : "restore");
			GetParent()?.RemoveItem(habitId);
			await Task.Delay(100);
			await HabitDao.ChangeHabitArchiveStatus(habitId, isArchive);
			DataChangedNotifier.NotifyHabitsChanged();
			HabitSyncService.CommitHabits();
		}
	}

	private async void OnDeleteClick(string habitId)
	{
		if (new CustomerDialog(Utils.GetString("DeleteHabit"), Utils.GetString("DeleteHabitMakeSure"), Utils.GetString("Delete"), Utils.GetString("Cancel"), Window.GetWindow(this)).ShowDialog() == true)
		{
			UserActCollectUtils.AddClickEvent("habit", "list_cm", "delete");
			GetParent()?.RemoveItem(habitId);
			await HabitDao.DeleteHabit(habitId);
		}
	}

	private void OnMouseRightDown(object sender, MouseButtonEventArgs e)
	{
		_canShowPop = true;
	}

	private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (base.DataContext is HabitItemViewModel habitItemViewModel)
		{
			_model = habitItemViewModel;
			IconHoverBorder.Cursor = (habitItemViewModel.Archived ? Cursors.No : Cursors.Hand);
			if (string.IsNullOrEmpty(habitItemViewModel.Color))
			{
				IconHoverBorder.SetResourceReference(Panel.BackgroundProperty, "BaseColorOpacity10");
				IconText.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryColor");
				IconEllipse.SetResourceReference(Shape.FillProperty, "PrimaryColor10");
			}
			else
			{
				IconHoverBorder.Background = ThemeUtil.GetColorInString("#20191919");
				IconText.Foreground = Brushes.White;
				IconEllipse.Fill = ThemeUtil.GetColorInString(habitItemViewModel.Color);
			}
			bool flag = LocalSettings.Settings.HabitItemDisplayMode == 1 || habitItemViewModel.Archived;
			double actualWidth = base.ActualWidth;
			HabitItemViewModel model = Model;
			double num = actualWidth - (double)((model != null && model.ShowDateCheckIn) ? 150 : (flag ? 100 : 340));
			NameTextBlock.MaxWidth = ((num > 0.0) ? num : 0.0);
		}
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (e.WidthChanged)
		{
			bool flag = LocalSettings.Settings.HabitItemDisplayMode == 1 || Model.Archived;
			double actualWidth = base.ActualWidth;
			HabitItemViewModel model = Model;
			double num = actualWidth - (double)((model != null && model.ShowDateCheckIn) ? 150 : (flag ? 100 : 340));
			num = ((num > 0.0) ? num : 0.0);
			NameTextBlock.MaxWidth = num;
			SetTotalTextAndCurrentText();
		}
	}

	private void SetTotalTextAndCurrentText()
	{
		HabitItemViewModel model = _model;
		if (base.ActualWidth > 0.0 && model != null)
		{
			if (LocalSettings.Settings.HabitItemDisplayMode == 0 || base.ActualWidth > (double)(model.ShowDateCheckIn ? 279 : 240))
			{
				TotalText.SetBinding(TextBlock.TextProperty, "TotalDays");
				CurrentText.SetBinding(TextBlock.TextProperty, "CurrentStreakDays");
			}
			else
			{
				TotalText.SetBinding(TextBlock.TextProperty, "ShortTotalDays");
				CurrentText.SetBinding(TextBlock.TextProperty, "ShortCurrentStreakDays");
			}
		}
	}

	public async Task CheckDayCheck(HabitDayCheckModel dayCheck, UIElement ui)
	{
		_clickElement = ui;
		HabitItemViewModel itemModel = Model;
		string text = itemModel?.Habit?.Id;
		if (string.IsNullOrEmpty(text) || dayCheck == null)
		{
			return;
		}
		Task<HabitModel> habitTask = HabitDao.GetHabitById(text);
		Task<HabitCheckInModel> checkInTask = HabitCheckInDao.GetHabitCheckInsByHabitIdAndStamp(text, dayCheck.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
		await Task.WhenAll(habitTask, checkInTask);
		HabitModel result = habitTask.Result;
		bool flag = itemModel.DateCheckIn == dayCheck || (itemModel.WeekCheckIns?.Contains(dayCheck) ?? false);
		if (result != null && Model == itemModel && flag)
		{
			itemModel.Habit = result;
			dayCheck.SetData(checkInTask.Result, result);
			if (dayCheck.Status == 1 || !(dayCheck.CheckInModel.Value < dayCheck.CheckInModel.Goal))
			{
				await ResetHabit(dayCheck);
			}
			else
			{
				UserActCollectUtils.AddClickEvent("habit", "view_action", (ui == IconGrid) ? "check_in_from_icon" : "check_in");
				await CheckHabit(dayCheck, result);
			}
			itemModel.UpdateTotalCheckDaysAndCurrentStreak();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/habit/habitlistitem.xaml", UriKind.Relative);
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
			Container = (HabitListItem)target;
			Container.DataContextChanged += OnDataContextChanged;
			Container.SizeChanged += OnSizeChanged;
			Container.PreviewMouseLeftButtonUp += OnItemSelected;
			Container.PreviewMouseRightButtonUp += ShowOptionPopup;
			Container.PreviewMouseRightButtonDown += OnMouseRightDown;
			Container.Loaded += Container_Loaded;
			Container.Unloaded += Container_Unloaded;
			break;
		case 2:
			BackBorder = (Border)target;
			break;
		case 3:
			IconGrid = (Grid)target;
			IconGrid.MouseLeftButtonUp += OnCheckItemClick;
			break;
		case 4:
			IconEllipse = (Ellipse)target;
			break;
		case 5:
			IconText = (EmjTextBlock)target;
			break;
		case 6:
			IconHoverBorder = (Border)target;
			break;
		case 7:
			DateCheckIn = (ContentControl)target;
			break;
		case 8:
			CheckInItems = (ItemsControl)target;
			break;
		case 9:
			NameTextBlock = (EmjTextBlock)target;
			break;
		case 10:
			TotalText = (TextBlock)target;
			break;
		case 11:
			CurrentText = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
