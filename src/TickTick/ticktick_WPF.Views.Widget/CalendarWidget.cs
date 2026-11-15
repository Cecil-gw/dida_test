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
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using KotlinModels;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.TouchPad;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Undo;

namespace ticktick_WPF.Views.Widget;

public class CalendarWidget : WidgetBaseUserControl, IToastShowWindow, IComponentConnector
{
	private const int WM_MOUSEHWHEEL = 526;

	internal Grid UndoToastGrid;

	internal CalendarControl CalendarControl;

	internal Border PremiumBorder;

	internal Button UpdateNowButton;

	private bool _contentLoaded;

	public string ThemeId => base.Model.ThemeId;

	public CalendarWidget(WidgetViewModel model)
		: base(model)
	{
		base.DataContext = model;
		InitializeComponent();
		base.Loaded += OnWindowLoaded;
		InitEvents();
	}

	public void TryHideToast()
	{
		if (UndoToastGrid.Children.Count > 0)
		{
			if (UndoToastGrid.Children[0] is UndoToast undoToast)
			{
				undoToast.OnFinished();
			}
			UndoToastGrid.Children[0].Visibility = Visibility.Collapsed;
		}
	}

	internal void CheckCalendarProEnable()
	{
		base.Dispatcher.Invoke(delegate
		{
			CalendarControl.ShowProToast();
			PremiumBorder.Visibility = ((UserDao.IsPro() || !ABTestManager.UsePremiumV2CalendarProStyle()) ? Visibility.Collapsed : Visibility.Visible);
		});
	}

	public void TryToastString(object obj, string toastString)
	{
		WindowToastHelper.ToastString(UndoToastGrid, toastString);
	}

	public async Task<bool> BatchDeleteTask(List<TaskModel> tasks)
	{
		CalendarControl.GetFocus();
		return await TaskService.BatchDeleteTasks(tasks, showUndo: true, 1, UndoToastGrid);
	}

	public void TaskComplete(CloseUndoToast undo)
	{
		WindowToastHelper.ShowAndHideToast(UndoToastGrid, undo);
		CalendarControl.GetFocus();
	}

	public async void TaskDeleted(string taskId)
	{
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		if (task != null)
		{
			task.deleted = 1;
			task.modifiedTime = DateTime.Now;
			await TaskService.UpdateTaskOnDeletedChanged(task);
			UndoToast undoToast = new UndoToast();
			undoToast.InitTaskUndo(taskId, task.title);
			WindowToastHelper.ShowAndHideToast(UndoToastGrid, undoToast);
		}
		CalendarControl.GetFocus();
	}

	public void ToastDeleteRecUndo(List<TaskDeleteRecurrenceUndoEntity> undoModels)
	{
		if (undoModels != null && undoModels.Any())
		{
			UndoToast undoToast = new UndoToast();
			undoToast.InitTaskUndo(undoModels);
			ShowUndoToast(undoToast);
		}
	}

	public async Task ShowUndoToast(UndoToast undoToast)
	{
		WindowToastHelper.ShowAndHideToast(UndoToastGrid, undoToast);
		CalendarControl.GetFocus();
	}

	public void Toast(FrameworkElement uiElement)
	{
		WindowToastHelper.ShowAndHideToast(UndoToastGrid, uiElement);
	}

	public void ToastMoveProjectControl(string projectId, string taskName = null, MoveToastType moveType = MoveToastType.Move)
	{
		WindowToastHelper.ShowAndHideToast(UndoToastGrid, new MoveToastControl(projectId, App.Window, taskName, moveType));
	}

	private void InitEvents()
	{
		CalendarControl.HeadView.MoreButton.Visibility = Visibility.Visible;
		CalendarControl.HeadView.MoreAction += OnMoreAction;
		CalendarControl.HeadView.DragPanel.PreviewMouseLeftButtonDown += base.OnDragBarMouseDown;
		CalendarControl.HeadView.DragPanel.MouseMove += base.OnDragMove;
		CalendarControl.HeadView.DragPanel.Cursor = Cursors.SizeAll;
		CalendarControl.HeadView.Height = 56.0;
		CalendarControl.HeadView.FirstRow.Height = new GridLength(46.0);
		CalendarControl.ArrangePanel.SwitchTitle.BackBorder.Opacity = 0.2;
		CalendarControl.HeadView.UnlockButton.Click += base.OnUnlockWidgetClick;
		CalendarControl.HeadView.LockedSyncButton.Click += OnLockedSyncClick;
		CalendarControl.Background = Brushes.Transparent;
		CalendarControl.CalendarSide.DayPicker.Margin = new Thickness(10.0, 8.0, 10.0, 12.0);
		CalendarControl.ArrangePanel.TopGrid.Margin = new Thickness(16.0, 21.0, 12.0, 6.0);
	}

	private void OnWindowLoaded(object sender, RoutedEventArgs e)
	{
		base.Resources["MonthDayTextFontSize"] = 12.0;
		base.Resources["MonthDayTextMargin"] = new Thickness(2.0, 6.0, 0.0, 0.0);
		base.Resources["WeekDayTextMargin"] = new Thickness(4.0, 40.0, 0.0, 0.0);
		CalendarControl.GotoDate(null);
		CheckCalendarProEnable();
		PresentationSource presentationSource = PresentationSource.FromVisual(this);
		if (presentationSource != null)
		{
			((HwndSource)presentationSource).AddHook(Hook);
			if (TouchPadHelper.TouchpadHelper.Exists())
			{
				TouchPadHelper.TouchpadHelper.RegisterInput(((HwndSource)presentationSource).Handle);
			}
		}
		LockWidget(base.Model.IsLocked);
	}

	private void SyncTask()
	{
		CalendarControl.HeadView.BeginSyncStory();
		if (!Utils.IsNetworkAvailable())
		{
			TryToastString(null, Utils.GetString("NoNetwork"));
		}
		SyncManager.Sync(1);
	}

	internal override void OnMoreAction(object sender, WidgetMoreAction e)
	{
		if (e == WidgetMoreAction.Sync)
		{
			SyncTask();
		}
		else
		{
			base.OnMoreAction(sender, e);
		}
	}

	internal override void LockWidget(bool isLocked)
	{
		CalendarControl.HeadView.DragPanel.IsEnabled = !isLocked;
		CalendarControl.HeadView.LockedOptionGrid.Visibility = ((!isLocked) ? Visibility.Collapsed : Visibility.Visible);
		CalendarControl.HeadView.UnlockOptionGrid.Visibility = (isLocked ? Visibility.Collapsed : Visibility.Visible);
		CalendarControl.HeadView.AddGrid.Visibility = (isLocked ? Visibility.Collapsed : Visibility.Visible);
		base.LockWidget(isLocked);
		CalendarControl.IsLocked = isLocked;
	}

	private void OnLockedSyncClick(object sender, RoutedEventArgs e)
	{
		SyncTask();
	}

	internal override void CloseWidget()
	{
		CalendarWidgetHelper.CloseWidget();
	}

	private IntPtr Hook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
	{
		switch (msg)
		{
		case 526:
		{
			int num = 0;
			num = LOWORD(wParam);
			OnMouseTilt(num);
			return (IntPtr)1;
		}
		case 255:
		{
			TouchpadContact[] array = TouchPadHelper.TouchpadHelper.ParseInput(lParam);
			if (array != null && array.Any((TouchpadContact c) => c.ContactId == 1))
			{
				CalendarControl.OnDoubleFingerTouch();
			}
			break;
		}
		}
		return IntPtr.Zero;
	}

	private static int HIWORD(IntPtr ptr)
	{
		return (ptr.ToInt32() >> 16) & 0xFFFF;
	}

	private static int LOWORD(IntPtr ptr)
	{
		return ((int)ptr.ToInt64() >> 16) % 256;
	}

	private void OnMouseTilt(int offset)
	{
		CalendarControl.OnTouchScroll(offset);
	}

	internal override bool IsEditing()
	{
		return CalendarControl.IsEditting();
	}

	public void TryUndo()
	{
		if (UndoToastGrid.Children.Count > 0 && UndoToastGrid.Children[0] is UndoToast { Visibility: Visibility.Visible } undoToast)
		{
			undoToast.OnUndo();
		}
	}

	private void OnRenewClick(object sender, RoutedEventArgs e)
	{
		Utils.StartUpgrade("calendar_view");
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		CalendarWidgetHelper.CloseWidget();
	}

	public void Reload()
	{
		CalendarControl.Reload(force: true, reloadArrange: true, reloadFilter: true, delay: false, resetDatePickerColor: true);
	}

	private void OnUpgradeClick(object sender, MouseButtonEventArgs e)
	{
		Utils.StartUpgrade("calendar_widget", Window.GetWindow(this));
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/widget/calendarwidget.xaml", UriKind.Relative);
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
			UndoToastGrid = (Grid)target;
			break;
		case 2:
			CalendarControl = (CalendarControl)target;
			break;
		case 3:
			PremiumBorder = (Border)target;
			break;
		case 4:
			UpdateNowButton = (Button)target;
			UpdateNowButton.PreviewMouseLeftButtonUp += OnUpgradeClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
