using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Habit;

public class HabitCheckInWindow : EscPopup, IComponentConnector
{
	public static bool PopupShowing;

	private readonly HabitModel _habit;

	private readonly string _stamp;

	private readonly int _status;

	private HabitAction _action = HabitAction.None;

	private double _amount;

	private IToastShowWindow _toastWindow;

	private DateTime _date;

	internal Border BaseBar;

	internal StackPanel ActionPanel;

	internal Popup ManuallyCheckInPopup;

	internal ManualRecordCheckinControl CheckInControl;

	internal Grid AutoCheckInGrid;

	internal Grid AutoCheckInGridWithReset;

	internal Grid ManualCheckInGrid;

	internal Button ManualCheckInButton;

	internal Button CheckInButton;

	internal Button ResetButton;

	private bool _contentLoaded;

	public event EventHandler OnAction;

	public HabitCheckInWindow()
	{
		InitializeComponent();
		base.Closed += async delegate
		{
			PopupShowing = false;
			PopupStateManager.OnViewPopupClosed();
			await OnCheckInAction();
			this.OnAction?.Invoke(this, null);
		};
	}

	public HabitCheckInWindow(HabitModel habit, int status, DateTime? startDate, IToastShowWindow toastWindow = null)
	{
		InitializeComponent();
		_habit = habit;
		_toastWindow = toastWindow;
		_status = status;
		_date = startDate ?? DateTime.Today;
		if (startDate.HasValue)
		{
			_stamp = startDate.Value.ToString("yyyyMMdd", App.Ci);
		}
		base.Closed += async delegate
		{
			PopupStateManager.OnViewPopupClosed();
			await OnCheckInAction();
			this.OnAction?.Invoke(this, null);
		};
	}

	private async Task InitData()
	{
		HabitDetailViewModel vm = new HabitDetailViewModel(_habit)
		{
			PivotDate = _date,
			Status = _status
		};
		InitOperationButton(vm.Status, vm.Type, vm.Step, vm.Goal);
		HabitCheckInModel habitCheckInModel = await HabitCheckInDao.GetHabitCheckInsByHabitIdAndStamp(_habit.Id, _stamp);
		if (habitCheckInModel != null)
		{
			vm.Value = habitCheckInModel.GetDisplayValue();
			vm.CompletedRatio = (int)(habitCheckInModel.GetDisplayValue() * 1.0 / _habit.Goal * 1.0 * 100.0);
		}
		base.DataContext = vm;
	}

	private void InitOperationButton(int status, string type, double step, double goal)
	{
		switch (HabitUtils.GetHabitCheckInType(status, type, step, goal))
		{
		case HabitCheckInType.BoolCompleted:
		case HabitCheckInType.CompletedAllCompleted:
			ResetButton.Visibility = Visibility.Visible;
			break;
		case HabitCheckInType.BoolUnCompleted:
		case HabitCheckInType.CompletedAllUnCompleted:
			CheckInButton.Visibility = Visibility.Visible;
			break;
		case HabitCheckInType.AutoCompleted:
			AutoCheckInGridWithReset.Visibility = Visibility.Visible;
			break;
		case HabitCheckInType.AutoUncompleted:
			AutoCheckInGrid.Visibility = Visibility.Visible;
			break;
		case HabitCheckInType.ManualCompleted:
			ManualCheckInGrid.Visibility = Visibility.Visible;
			break;
		case HabitCheckInType.ManualUnCompleted:
			ManualCheckInButton.Visibility = Visibility.Visible;
			break;
		}
	}

	private async Task OnCheckInAction()
	{
		if (base.DataContext != null)
		{
			object dataContext = base.DataContext;
			if (dataContext is HabitDetailViewModel model)
			{
				bool checkRecord = model.Status == 0;
				HabitModel habitModel = ((_action != HabitAction.None) ? (await HabitDao.GetHabitById(model.Id)) : null);
				HabitModel habitModel2 = habitModel;
				HabitCheckInType? habitCheckInType = ((habitModel2 == null) ? ((HabitCheckInType?)null) : new HabitCheckInType?(HabitUtils.GetHabitCheckInType(0, habitModel2.Type, habitModel2.Step, habitModel2.Goal)));
				switch (_action)
				{
				case HabitAction.CheckIn:
					if (!IsCompleteAction(habitCheckInType))
					{
						NavigateToLatestCheckIn(habitModel2);
						break;
					}
					Utils.PlayCompletionSound();
					await HabitService.CheckInHabit(habitModel2.Id, DateTime.Today, -1.0, increment: true, checkShowRecord: true, 0, _toastWindow, null, HabitAction.CheckIn);
					break;
				case HabitAction.Reset:
					if (habitModel2 != null)
					{
						await HabitService.ResetCheckInHabit(habitModel2.Id, model.PivotDate, habitModel2.Type);
					}
					break;
				case HabitAction.ManualRecord:
					if (!IsRealRecordAction(habitCheckInType))
					{
						NavigateToLatestCheckIn(habitModel2);
						break;
					}
					_toastWindow?.TryToastString(null, "+" + _amount + " " + HabitUtils.GetUnitText(habitModel2.Unit));
					await CheckInRealHabit(model, habitModel2, _amount, HabitAction.ManualRecord, checkRecord);
					break;
				case HabitAction.AutoRecord:
					if (habitCheckInType != HabitCheckInType.AutoCompleted && habitCheckInType != HabitCheckInType.AutoUncompleted)
					{
						NavigateToLatestCheckIn(habitModel2);
						break;
					}
					_toastWindow?.TryToastString(null, "+" + habitModel2.Step + " " + HabitUtils.GetUnitText(habitModel2.Unit));
					await CheckInRealHabit(model, habitModel2, habitModel2.Step, HabitAction.AutoRecord, checkRecord);
					break;
				}
			}
		}
		IToastShowWindow toastWindow = _toastWindow;
		if (toastWindow is Window window)
		{
			await Task.Delay(10);
			if (!PopupStateManager.IsViewPopOpened())
			{
				window.Activate();
			}
		}
	}

	private static bool IsCompleteAction(HabitCheckInType? checkInType)
	{
		if (checkInType != HabitCheckInType.BoolCompleted && checkInType != HabitCheckInType.BoolUnCompleted && checkInType != HabitCheckInType.CompletedAllCompleted)
		{
			return checkInType == HabitCheckInType.CompletedAllUnCompleted;
		}
		return true;
	}

	private static bool IsRealRecordAction(HabitCheckInType? checkInType)
	{
		if (checkInType != HabitCheckInType.AutoCompleted && checkInType != HabitCheckInType.AutoUncompleted && checkInType != HabitCheckInType.ManualCompleted)
		{
			return checkInType == HabitCheckInType.ManualUnCompleted;
		}
		return true;
	}

	private static void NavigateToLatestCheckIn(HabitModel habit)
	{
		if (habit != null)
		{
			App.NavigateHabit(habit.Id, showRecord: true);
		}
	}

	private async Task CheckInRealHabit(HabitDetailViewModel model, HabitModel habit, double step, HabitAction action, bool checkLog = true)
	{
		Utils.PlayCompletionSound();
		model.Habit = habit;
		model.Type = habit.GetSafeType();
		model.Unit = HabitUtils.GetUnitText(habit.Unit);
		model.Goal = habit.Goal;
		model.Step = habit.Step;
		if (model.TodayCheckIn == null)
		{
			HabitCheckInModel todayCheckIn = new HabitCheckInModel
			{
				Goal = habit.Goal,
				Value = step
			};
			model.TodayCheckIn = todayCheckIn;
		}
		else
		{
			model.TodayCheckIn.Goal = habit.Goal;
			model.TodayCheckIn.Value = model.TodayCheckIn.GetDisplayValue() + step;
			model.TodayCheckIn = model.TodayCheckIn;
		}
		DateTime pivotDate = model.PivotDate;
		await HabitService.CheckInHabit(habit.Id, pivotDate, step, increment: true, checkLog, 0, _toastWindow, null, action);
		Task.Run(delegate
		{
			base.Dispatcher.Invoke((Func<Task>)async delegate
			{
				await Task.Delay(1000);
				if (model.TodayCheckIn.Value < model.Goal)
				{
					model.Status = 0;
				}
			});
		});
	}

	public async void Show(UIElement target, double targetWidth, double addHeight, bool byMouse, Point point = default(Point))
	{
		await InitData();
		PopupStateManager.OnViewPopupOpened();
		base.PlacementTarget = target;
		TaskPopupArgs popupLocation = PopupLocationCalculator.GetPopupLocation(target, targetWidth, base.Width, byMouse, addHeight);
		if (point != default(Point))
		{
			base.Placement = PlacementMode.RelativePoint;
			base.HorizontalOffset = point.X;
			base.VerticalOffset = point.Y;
		}
		else if (!popupLocation.ByMouse)
		{
			base.Placement = (popupLocation.IsRight ? PlacementMode.Right : PlacementMode.Left);
			base.HorizontalOffset = ((!popupLocation.IsRight) ? 6 : 0);
			base.VerticalOffset = -4.0;
		}
		else
		{
			base.Placement = PlacementMode.Mouse;
		}
		base.IsOpen = true;
		await Task.Delay(200);
		if (base.IsOpen)
		{
			PopupShowing = true;
			PopupStateManager.OnViewPopupOpened();
		}
	}

	private void OnAutoCheckInClick(object sender, RoutedEventArgs e)
	{
		_action = HabitAction.AutoRecord;
		base.IsOpen = false;
	}

	private void OnCheckInClick(object sender, RoutedEventArgs e)
	{
		_action = HabitAction.CheckIn;
		base.IsOpen = false;
	}

	private void OnResetClick(object sender, RoutedEventArgs e)
	{
		_action = HabitAction.Reset;
		base.IsOpen = false;
	}

	private void OnManualCheckInClick(object sender, RoutedEventArgs e)
	{
		ManuallyRecordCheckIn();
	}

	private void ManuallyRecordCheckIn()
	{
		ManuallyCheckInPopup.IsOpen = true;
		CheckInControl.Init(_habit.Unit);
		CheckInControl.Cancel -= OnCheckInPopupCancel;
		CheckInControl.Cancel += OnCheckInPopupCancel;
		CheckInControl.Save -= OnCheckInPopupSave;
		CheckInControl.Save += OnCheckInPopupSave;
	}

	private void OnCheckInPopupSave(object sender, double amount)
	{
		ManuallyCheckInPopup.IsOpen = false;
		_amount = amount;
		_action = HabitAction.ManualRecord;
		base.IsOpen = false;
	}

	private void OnCheckInPopupCancel(object sender, EventArgs e)
	{
		ManuallyCheckInPopup.IsOpen = false;
	}

	private void OnWindowKeyDown(object sender, KeyEventArgs e)
	{
		Key key = e.Key;
		if ((key == Key.Up || key == Key.Down) && base.PlacementTarget is IShowTaskDetailWindow showTaskDetailWindow)
		{
			showTaskDetailWindow.OnDetailKeyUp(e.Key);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/habit/habitcheckinwindow.xaml", UriKind.Relative);
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
			BaseBar = (Border)target;
			break;
		case 2:
			ActionPanel = (StackPanel)target;
			break;
		case 3:
			ManuallyCheckInPopup = (Popup)target;
			break;
		case 4:
			CheckInControl = (ManualRecordCheckinControl)target;
			break;
		case 5:
			AutoCheckInGrid = (Grid)target;
			break;
		case 6:
			((Button)target).Click += OnAutoCheckInClick;
			break;
		case 7:
			((Button)target).Click += OnManualCheckInClick;
			break;
		case 8:
			AutoCheckInGridWithReset = (Grid)target;
			break;
		case 9:
			((Button)target).Click += OnAutoCheckInClick;
			break;
		case 10:
			((Button)target).Click += OnManualCheckInClick;
			break;
		case 11:
			((Button)target).Click += OnResetClick;
			break;
		case 12:
			ManualCheckInGrid = (Grid)target;
			break;
		case 13:
			((Button)target).Click += OnManualCheckInClick;
			break;
		case 14:
			((Button)target).Click += OnResetClick;
			break;
		case 15:
			ManualCheckInButton = (Button)target;
			ManualCheckInButton.Click += OnManualCheckInClick;
			break;
		case 16:
			CheckInButton = (Button)target;
			CheckInButton.Click += OnCheckInClick;
			break;
		case 17:
			ResetButton = (Button)target;
			ResetButton.Click += OnResetClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
