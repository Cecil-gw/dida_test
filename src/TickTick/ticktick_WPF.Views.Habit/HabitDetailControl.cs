using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using TickTickUtils;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.MainListView;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Pomo;

namespace ticktick_WPF.Views.Habit;

public class HabitDetailControl : UserControl, IComponentConnector
{
	private string _habitId;

	private DateTime _pivotDate = DateTime.Today;

	public bool IsIndependent;

	private static ConcurrentDictionary<string, DateTime> _pullRemoteDict = new ConcurrentDictionary<string, DateTime>();

	private bool _useCardStyle = true;

	private bool _useDefaultThemeGrayBackground;

	private bool _isInLightSlideOutHost;

	internal Grid OuterContainer;

	internal HoverIconButton BackGrid;

	internal TextBlock ArchivedText;

	internal ContentControl MoreImage;

	internal EscPopup MorePopup;

	internal ScrollViewer Scroller;

	internal Grid Container;

	internal HabitStatisticsView StatisticsView;

	internal Border CycleBorder;

	internal HabitCompletedCyclesControl CompletedCyclesControl;

	internal HabitMonthGrid HabitMonthControl;

	internal HabitCheckinChartControl DailyGoalsControl;

	internal HabitLogControl LogControl;

	private bool _contentLoaded;

	public event EventHandler HideDetail;

	internal void SetSlideOutHost(bool isSlideOut)
	{
		_isInLightSlideOutHost = isSlideOut;
		StatisticsView.SetSlideOutHost(isSlideOut);
		DataChangedNotifier_InterfaceStyleChanged(this, EventArgs.Empty);
	}

	public HabitDetailControl()
	{
		InitializeComponent();
		BackGrid.SetIconColor("BaseColorOpacity60");
		base.Loaded += delegate
		{
			BindEvents();
		};
		base.Unloaded += delegate
		{
			UnbindEvents();
		};
	}

	private void BindEvents()
	{
		DataChangedNotifier.HabitCheckInChanged += OnCheckInChanged;
		DataChangedNotifier.HabitSkip += OnSkiped;
		DataChangedNotifier.HabitsChanged += OnHabitChanged;
		DataChangedNotifier.HabitLogChanged += ReloadHabit;
		DataChangedNotifier.HabitsSyncDone += ReloadHabit;
		DataChangedNotifier.InterfaceStyleChanged += DataChangedNotifier_InterfaceStyleChanged;
		DataChangedNotifier.ThemeModeChanged += DataChangedNotifier_InterfaceStyleChanged;
		HabitMonthControl.PivotDateChanged += OnDateChanged;
	}

	private void UnbindEvents()
	{
		DataChangedNotifier.HabitCheckInChanged -= OnCheckInChanged;
		DataChangedNotifier.HabitSkip -= OnSkiped;
		DataChangedNotifier.HabitsChanged -= OnHabitChanged;
		DataChangedNotifier.HabitLogChanged -= ReloadHabit;
		DataChangedNotifier.HabitsSyncDone -= ReloadHabit;
		DataChangedNotifier.InterfaceStyleChanged -= DataChangedNotifier_InterfaceStyleChanged;
		DataChangedNotifier.ThemeModeChanged -= DataChangedNotifier_InterfaceStyleChanged;
		HabitMonthControl.PivotDateChanged -= OnDateChanged;
		HabitMonthControl.Dispose();
		CompletedCyclesControl.Dispose();
	}

	private void DataChangedNotifier_InterfaceStyleChanged(object sender, EventArgs e)
	{
		if (base.DataContext is HabitDetailViewModel habitDetailViewModel)
		{
			habitDetailViewModel.IsShowGrayBack = ShouldShowGrayBackground();
		}
	}

	private bool ShouldShowGrayBackground()
	{
		if (!_isInLightSlideOutHost || ThemeUtil.IsDark())
		{
			if (LocalSettings.Settings.ThemeId == "White")
			{
				if (!_useDefaultThemeGrayBackground)
				{
					return LocalSettings.Settings.ExtraSettings.InterfaceStyle == InterfaceStyle.Card;
				}
				return true;
			}
			return false;
		}
		return true;
	}

	private void OnHabitChanged(object sender, EventArgs e)
	{
		Load(_habitId, _pivotDate, pullRemote: false, force: true, _useCardStyle, _useDefaultThemeGrayBackground);
	}

	private void OnCheckInChanged(object sender, HabitCheckInModel checkIn)
	{
		if (base.Visibility == Visibility.Visible && checkIn.HabitId == _habitId)
		{
			Load(_habitId, _pivotDate, pullRemote: false, force: true, _useCardStyle, _useDefaultThemeGrayBackground);
		}
	}

	private void OnSkiped(object sender, string habiteId)
	{
		if (base.Visibility == Visibility.Visible && habiteId == _habitId)
		{
			Load(_habitId, _pivotDate, pullRemote: false, force: true, _useCardStyle, _useDefaultThemeGrayBackground);
		}
	}

	private void OnDateChanged(object sender, DateTime pivotDate)
	{
		if (!string.IsNullOrEmpty(_habitId))
		{
			Load(_habitId, pivotDate, pullRemote: false, force: true, _useCardStyle, _useDefaultThemeGrayBackground);
		}
	}

	private void ReloadHabit(object sender, EventArgs e)
	{
		Reload();
	}

	public void Reload()
	{
		Load(_habitId, _pivotDate, pullRemote: false, force: true, _useCardStyle, _useDefaultThemeGrayBackground);
	}

	public void Load(string habitId, DateTime pivotDate, bool pullRemote = false, bool force = true, bool useCardStyle = true, bool useDefaultThemeGrayBackground = false)
	{
		ThreadUtil.DetachedRunOnUiThread(async delegate
		{
			if (force || string.IsNullOrEmpty(_habitId) || string.IsNullOrEmpty(habitId) || !(_habitId != habitId))
			{
				_habitId = habitId;
				_pivotDate = pivotDate;
				HabitModel habit = await HabitDao.GetHabitById(habitId);
				if (habit != null)
				{
					_useCardStyle = useCardStyle;
					_useDefaultThemeGrayBackground = useDefaultThemeGrayBackground;
					CycleBorder.IsHitTestVisible = habit.CompletedCyclesList != null && habit.CompletedCyclesList.Count > 1;
					UserActCollectUtils.AddClickEvent("habit", "check_in_style", (habit.Style == 2) ? "habit_icon" : "checkmark");
					SetArchiveView(habit.Status == 1);
					DateTime startDate = pivotDate.AddDays(pivotDate.Day * -1 + 1);
					DateTime endDate = startDate.AddMonths(1).AddDays(-1.0);
					HabitDetailViewModel vm = new HabitDetailViewModel(habit)
					{
						PivotDate = pivotDate,
						UseCardStyle = useCardStyle
					};
					int startDateStamp = ticktick_WPF.Util.DateUtils.GetDateNum(startDate);
					int endDateStamp = ticktick_WPF.Util.DateUtils.GetDateNum(endDate);
					int startDatExtraStamp = ticktick_WPF.Util.DateUtils.GetDateNum(startDate.AddDays(-14.0));
					int endDateExtraStamp = ticktick_WPF.Util.DateUtils.GetDateNum(endDate.AddDays(14.0));
					List<HabitCheckInModel> checkIns = await HabitCheckInDao.GetHabitCheckInsByHabitId(habitId);
					List<HabitCheckInModel> monthCheckIns = new List<HabitCheckInModel>();
					List<HabitCheckInModel> monthCheckInsExtra = new List<HabitCheckInModel>();
					foreach (HabitCheckInModel item in checkIns)
					{
						int num = int.Parse(item.CheckinStamp);
						if (num >= startDateStamp && num <= endDateStamp)
						{
							monthCheckIns.Add(item);
							monthCheckInsExtra.Add(item);
						}
						else if (num >= startDatExtraStamp && num <= endDateExtraStamp)
						{
							monthCheckInsExtra.Add(item);
						}
					}
					CompletedCyclesControl.Habit = habit;
					CompletedCyclesControl.Visibility = ((!(habit.TargetDays > 0)) ? Visibility.Collapsed : Visibility.Visible);
					List<HabitRecordModel> records = await HabitRecordDao.GetHabitRecordsByHabitId(habit.Id);
					await HabitMonthControl.Load(vm, pivotDate, monthCheckInsExtra);
					StatisticsView.Load(habit, startDate, endDate, checkIns, records, vm.UseCardStyle, _useDefaultThemeGrayBackground);
					LoadDailyGoals(habit, startDate, endDate, monthCheckIns);
					LogControl.Load(habitId, startDate, endDate, monthCheckIns, records);
					vm.IsShowGrayBack = ShouldShowGrayBackground();
					base.DataContext = vm;
				}
				if (pullRemote && !string.IsNullOrEmpty(habit?.Id) && (!_pullRemoteDict.TryGetValue(habit.Id, out var value) || !((DateTime.Now - value).TotalMinutes < 2.0)) && await HabitService.PullRemoteCheckinAndRecordData(habit))
				{
					Load(_habitId, _pivotDate, pullRemote: false, force: true, _useCardStyle, _useDefaultThemeGrayBackground);
					_pullRemoteDict[habit.Id] = DateTime.Now;
				}
			}
		});
	}

	public void ManuallyRecord(DateTime time)
	{
		HabitMonthControl.ManuallyRecord(time, placeMouse: false);
	}

	private void SetArchiveView(bool isArchive)
	{
		LogControl.OnShowEditChanged(!isArchive);
		HabitMonthControl.DayCells.IsEnabled = !isArchive;
		ArchivedText.Visibility = ((!isArchive) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void LoadDailyGoals(HabitModel habit, DateTime startDate, DateTime endDate, List<HabitCheckInModel> monthCheckIns)
	{
		if (!habit.IsBooleanType())
		{
			DailyGoalsControl.Load(habit, startDate, endDate, monthCheckIns);
			DailyGoalsControl.Visibility = Visibility.Visible;
		}
		else
		{
			DailyGoalsControl.Visibility = Visibility.Collapsed;
		}
	}

	public void ScrollToTop()
	{
		Scroller.ScrollToTop();
	}

	private void OnBackClick(object sender, MouseButtonEventArgs e)
	{
		HabitContainer habitContainer = Utils.FindParent<HabitContainer>(this);
		if (habitContainer != null)
		{
			habitContainer.TryHideDetail();
			return;
		}
		ListViewContainer listViewContainer = Utils.FindParent<ListViewContainer>(this);
		if (listViewContainer != null)
		{
			listViewContainer.TryExtractDetail();
		}
		else
		{
			this.HideDetail?.Invoke(this, EventArgs.Empty);
		}
	}

	private async void OnMoreClick(object sender, MouseButtonEventArgs e)
	{
		HabitModel habit = await HabitDao.GetHabitById(_habitId);
		if (habit == null)
		{
			return;
		}
		List<OperationItemViewModel> typeList;
		if (habit.Status != 1)
		{
			typeList = new List<OperationItemViewModel>
			{
				new OperationItemViewModel(ActionType.EditHabit),
				new OperationItemViewModel(ActionType.Archive),
				new OperationItemViewModel(ActionType.Delete)
			};
			typeList.Insert(1, new OperationItemViewModel(ActionType.EditStyle)
			{
				Selectable = false,
				SubActions = new List<OperationItemViewModel>
				{
					new OperationItemViewModel(ActionType.Checkmark)
					{
						Selected = (habit.Style != 2)
					},
					new OperationItemViewModel(ActionType.HabitIcon)
					{
						Selected = (habit.Style == 2)
					}
				}
			});
			if (LocalSettings.Settings.EnableFocus)
			{
				HabitCheckInModel habitCheckInModel = await HabitCheckInDao.GetHabitCheckInByHabitIdAndStamp(_habitId, DateTime.Today.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
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
		OperationDialog operationDialog = new OperationDialog(habit.Id, typeList, MorePopup);
		operationDialog.Operated += OnOptionClick;
		operationDialog.Show();
	}

	private void OnOptionClick(object sender, KeyValuePair<string, ActionType> e)
	{
		switch (e.Value)
		{
		case ActionType.EditHabit:
			ShowEditWindow(e.Key);
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
			UserActCollectUtils.AddClickEvent("focus", "start_from", "habit_detail");
			TickFocusManager.TryStartFocusHabit(e.Key, isPomo: true);
			break;
		case ActionType.StartTiming:
			UserActCollectUtils.AddClickEvent("focus", "start_from", "habit_detail");
			TickFocusManager.TryStartFocusHabit(e.Key, isPomo: false);
			break;
		case ActionType.HabitIcon:
			ChangeHabitCheckStyle(e.Key, 2);
			break;
		case ActionType.Checkmark:
			ChangeHabitCheckStyle(e.Key, 1);
			break;
		}
	}

	private async void OnDeleteClick(string habitId)
	{
		if (new CustomerDialog(Utils.GetString("DeleteHabit"), Utils.GetString("DeleteHabitMakeSure"), Utils.GetString("Delete"), Utils.GetString("Cancel"), Window.GetWindow(this)).ShowDialog() == true)
		{
			UserActCollectUtils.AddClickEvent("habit", "detail_om", "delete");
			this.HideDetail?.Invoke(this, null);
			await HabitDao.DeleteHabit(habitId);
		}
	}

	private async void ChangeHabitCheckStyle(string habitId, int style)
	{
		if (!HabitMonthControl.AnyChecked())
		{
			Utils.Toast(Utils.GetString("Switched"));
		}
		await HabitDao.ChangeHabitCheckStyle(habitId, style);
		DataChangedNotifier.NotifyHabitsChanged();
		HabitSyncService.CommitHabits();
	}

	private async void ChangeHabitArchiveStatus(string habitId, bool isArchive)
	{
		if (isArchive || await HabitUtils.CheckHabitLimit())
		{
			Utils.Toast(Utils.GetString(isArchive ? "Archived" : "Recovered"));
			UserActCollectUtils.AddClickEvent("habit", "detail_om", isArchive ? "archive" : "restore");
			this.HideDetail?.Invoke(this, null);
			await HabitDao.ChangeHabitArchiveStatus(habitId, isArchive);
			DataChangedNotifier.NotifyHabitsChanged();
			HabitSyncService.CommitHabits();
		}
	}

	private async void ShowEditWindow(string habitId)
	{
		HabitModel habitModel = await HabitDao.GetHabitById(habitId);
		if (habitModel != null)
		{
			UserActCollectUtils.AddClickEvent("habit", "detail_om", "edit");
			AddOrEditHabitDialog addOrEditHabitDialog = new AddOrEditHabitDialog(habitModel);
			addOrEditHabitDialog.Owner = Window.GetWindow(this);
			addOrEditHabitDialog.ShowDialog();
		}
	}

	public string GetHabitId()
	{
		return _habitId;
	}

	public void ClearEvent()
	{
		this.HideDetail = null;
	}

	public void ShowBackMenu()
	{
		BackGrid.Visibility = Visibility.Visible;
	}

	public void HideBackMenu()
	{
		BackGrid.Visibility = Visibility.Collapsed;
	}

	private void OnCompletedCircleClick(object sender, MouseButtonEventArgs e)
	{
		CompletedCyclesControl.ShowCompletedCircle();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/habit/habitdetailcontrol.xaml", UriKind.Relative);
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
			OuterContainer = (Grid)target;
			break;
		case 2:
			BackGrid = (HoverIconButton)target;
			break;
		case 3:
			ArchivedText = (TextBlock)target;
			break;
		case 4:
			MoreImage = (ContentControl)target;
			MoreImage.MouseLeftButtonUp += OnMoreClick;
			break;
		case 5:
			MorePopup = (EscPopup)target;
			break;
		case 6:
			Scroller = (ScrollViewer)target;
			break;
		case 7:
			Container = (Grid)target;
			break;
		case 8:
			StatisticsView = (HabitStatisticsView)target;
			break;
		case 9:
			CycleBorder = (Border)target;
			break;
		case 10:
			CompletedCyclesControl = (HabitCompletedCyclesControl)target;
			break;
		case 11:
			HabitMonthControl = (HabitMonthGrid)target;
			break;
		case 12:
			DailyGoalsControl = (HabitCheckinChartControl)target;
			break;
		case 13:
			LogControl = (HabitLogControl)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
