using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using ticktick_WPF.Dal;
using ticktick_WPF.Entity.Binding;
using ticktick_WPF.Entity.Dto;
using ticktick_WPF.Entity.Enums;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels.AI;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Pomo.MainFocus;

namespace ticktick_WPF.Views.AI;

public class AIRecommendWindow : Window, IComponentConnector
{
	internal AIRecommendWindow Root;

	internal SmoothScrollViewer MainScrollViewer;

	internal SmoothScrollViewer DetailScrollViewer;

	internal EmptyView EmptyView;

	internal Grid ToastGrid;

	private bool _contentLoaded;

	public static AIRecommendWindow Window { get; set; }

	private List<string> effectIds { get; set; } = new List<string>();

	private AIRecommendAllGroup tempSnapShot { get; set; }

	private Debouncer snapShotDisposeDebounce { get; set; }

	private string lastPopupTask { get; set; }

	private bool loaded { get; set; }

	private AIRecommendViewModel viewModel { get; set; }

	public AIRecommendWindow()
	{
		InitializeComponent();
		base.DataContext = (viewModel = new AIRecommendViewModel());
		EmptyView.SetIconsSource(new EmptyIconsSourceModel("NormalEmpty", new List<int> { 1, 2, 3, 4, 6 }));
		snapShotDisposeDebounce = new Debouncer(5000, OnDeleteFinish);
		TasksChangeEventManager.AddHandler(TaskChangeNotifier.Notifier, OnTasksChanged);
		AIRecommendViewModel aIRecommendViewModel2 = viewModel;
		aIRecommendViewModel2.OnToast = (Action<string>)Delegate.Combine(aIRecommendViewModel2.OnToast, new Action<string>(OnToast));
		AIRecommendViewModel aIRecommendViewModel3 = viewModel;
		aIRecommendViewModel3.OnEnterDetail = (Action)Delegate.Combine(aIRecommendViewModel3.OnEnterDetail, new Action(OnEnterDetail));
		AIRecommendViewModel aIRecommendViewModel4 = viewModel;
		aIRecommendViewModel4.OnGenerating = (Action)Delegate.Combine(aIRecommendViewModel4.OnGenerating, new Action(OnGenerating));
		AIRecommendViewModel aIRecommendViewModel5 = viewModel;
		aIRecommendViewModel5.OnGenerated = (Action)Delegate.Combine(aIRecommendViewModel5.OnGenerated, new Action(OnGenerated));
		AIRecommendService.OnToast = (Action<string>)Delegate.Combine(AIRecommendService.OnToast, new Action<string>(OnToast));
		AIRecommendService.OnResult = (Action<List<AIRecommendPropertyDto>>)Delegate.Combine(AIRecommendService.OnResult, new Action<List<AIRecommendPropertyDto>>(OnAIResult));
	}

	private void Window_Loaded(object sender, RoutedEventArgs e)
	{
		WindowNotifier.NotifySubWindowOpen(SubWindowType.AIRecommend);
		UserActCollectUtils.AddClickEvent("ai_suggested_tasks", "suggested_tasks_page", "show");
		loaded = true;
		UpdateWindowPosition();
		if (base.Owner != null)
		{
			base.Owner.LocationChanged += Owner_LocationChanged;
			base.Owner.SizeChanged += Owner_SizeChanged;
			base.Owner.IsVisibleChanged += Owner_IsVisibleChanged;
			base.Owner.StateChanged += Owner_StateChanged;
		}
		MainWindow window = App.Window;
		window.OnModuleSwitch = (Action<DisplayModule>)Delegate.Combine(window.OnModuleSwitch, new Action<DisplayModule>(OnModuleSwitch));
		WindowNotifier.OnSubWindowOpen += WindowNotifier_OnSubWindowOpen;
	}

	private void Window_Closing(object sender, CancelEventArgs e)
	{
		if (base.Owner != null)
		{
			base.Owner.LocationChanged -= Owner_LocationChanged;
			base.Owner.SizeChanged -= Owner_SizeChanged;
			base.Owner.IsVisibleChanged -= Owner_IsVisibleChanged;
			base.Owner.StateChanged -= Owner_StateChanged;
			base.Owner.Activate();
		}
		TasksChangeEventManager.RemoveHandler(TaskChangeNotifier.Notifier, OnTasksChanged);
		AIRecommendViewModel aIRecommendViewModel = viewModel;
		aIRecommendViewModel.OnToast = (Action<string>)Delegate.Remove(aIRecommendViewModel.OnToast, new Action<string>(OnToast));
		AIRecommendViewModel aIRecommendViewModel2 = viewModel;
		aIRecommendViewModel2.OnEnterDetail = (Action)Delegate.Remove(aIRecommendViewModel2.OnEnterDetail, new Action(OnEnterDetail));
		AIRecommendViewModel aIRecommendViewModel3 = viewModel;
		aIRecommendViewModel3.OnGenerating = (Action)Delegate.Remove(aIRecommendViewModel3.OnGenerating, new Action(OnGenerating));
		AIRecommendViewModel aIRecommendViewModel4 = viewModel;
		aIRecommendViewModel4.OnGenerated = (Action)Delegate.Remove(aIRecommendViewModel4.OnGenerated, new Action(OnGenerated));
		AIRecommendService.OnToast = (Action<string>)Delegate.Remove(AIRecommendService.OnToast, new Action<string>(OnToast));
		AIRecommendService.OnResult = (Action<List<AIRecommendPropertyDto>>)Delegate.Remove(AIRecommendService.OnResult, new Action<List<AIRecommendPropertyDto>>(OnAIResult));
		MainWindow window = App.Window;
		window.OnModuleSwitch = (Action<DisplayModule>)Delegate.Remove(window.OnModuleSwitch, new Action<DisplayModule>(OnModuleSwitch));
		WindowNotifier.OnSubWindowOpen -= WindowNotifier_OnSubWindowOpen;
		Window = null;
	}

	private void Owner_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (!AppLockWindowManager.IsLocked)
		{
			base.Visibility = ((e.NewValue as bool? != true) ? Visibility.Hidden : Visibility.Visible);
		}
	}

	private void Owner_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		UpdateWindowPosition();
	}

	private void Owner_LocationChanged(object sender, EventArgs e)
	{
		UpdateWindowPosition();
	}

	private async void Owner_StateChanged(object sender, EventArgs e)
	{
		UpdateWindowPosition();
	}

	private void OnModuleSwitch(DisplayModule module)
	{
		if (module != DisplayModule.Task)
		{
			Close();
		}
	}

	private void WindowNotifier_OnSubWindowOpen(object sender, SubWindowType e)
	{
		if (e != SubWindowType.AIRecommend)
		{
			Close();
		}
	}

	private void UpdateWindowPosition()
	{
		if (base.Owner != null)
		{
			Rect windowRectangle = GetWindowRectangle(base.Owner);
			base.Left = windowRectangle.Left + windowRectangle.Width - base.ActualWidth;
			base.Top = windowRectangle.Top + windowRectangle.Height - base.ActualHeight;
		}
	}

	public static Rect GetWindowRectangle(Window w)
	{
		if (w.WindowState == WindowState.Maximized)
		{
			Rectangle workingArea = Screen.FromHandle(new WindowInteropHelper(w).Handle).WorkingArea;
			double pixelsPerDip = VisualTreeHelper.GetDpi(w).PixelsPerDip;
			return new Rect((double)workingArea.Left / pixelsPerDip, (double)workingArea.Top / pixelsPerDip, (double)workingArea.Width / pixelsPerDip, (double)workingArea.Height / pixelsPerDip);
		}
		return new Rect((int)w.Left, (int)w.Top, (int)w.ActualWidth, (int)w.ActualHeight);
	}

	private void OnEnterDetail()
	{
		DetailScrollViewer.ScrollToVerticalOffset(0.0);
		UserActCollectUtils.AddClickEvent("ai_suggested_tasks", "suggested_tasks_page", "section_detail_page");
	}

	private async void OnGenerating()
	{
		MainScrollViewer.ScrollToBottom();
	}

	private void OnGenerated()
	{
		if (MainScrollViewer.VerticalOffset + MainScrollViewer.ViewportHeight == MainScrollViewer.ExtentHeight)
		{
			MainScrollViewer.ScrollToVerticalOffset(MainScrollViewer.VerticalOffset - 0.5);
		}
	}

	private void OnToast(string msg)
	{
		WindowToastHelper.ToastString(ToastGrid, msg);
	}

	private void OnAIResult(List<AIRecommendPropertyDto> list)
	{
		viewModel.OnAIRecommendResult(list);
	}

	private async void OnTasksChanged(object sender, TasksChangeEventArgs e)
	{
		if (e.DeletedChangedIds.Any())
		{
			await UpdateFromNotify(e.DeletedChangedIds.Value);
		}
		if (e.UndoDeletedIds.Any() && effectIds.Any() && effectIds.Intersect(e.UndoDeletedIds.Value).Any())
		{
			if (tempSnapShot != null)
			{
				viewModel.RecoverFromSnapShot(tempSnapShot);
				tempSnapShot = null;
			}
			effectIds.Clear();
		}
		if (e.StatusChangedIds.Any())
		{
			if (effectIds.Any() && effectIds.Intersect(e.StatusChangedIds.Value).Any() && (await TaskDao.GetTasksByIds(effectIds)).All((TaskModel x) => x.status == 0))
			{
				if (tempSnapShot != null)
				{
					viewModel.RecoverFromSnapShot(tempSnapShot);
					tempSnapShot = null;
				}
				effectIds.Clear();
				return;
			}
			await UpdateFromNotify(e.StatusChangedIds.Value, (TaskModel x) => x.status != 0);
		}
		if (e.KindChangedIds.Any())
		{
			await UpdateFromNotify(e.KindChangedIds.Value, (TaskModel x) => x.kind == "NOTE", takeSnapShot: false);
		}
		if (e.DateChangedIds.Any())
		{
			await UpdateFromNotify(e.DateChangedIds.Value, null, takeSnapShot: false);
		}
	}

	private async Task UpdateFromNotify(IEnumerable<string> changeIds, Func<TaskModel, bool> where = null, bool takeSnapShot = true)
	{
		HashSet<string> allTaskIds = viewModel.GetAllTaskIds();
		IEnumerable<string> enumerable = changeIds.Intersect(allTaskIds);
		if (!enumerable.Any())
		{
			return;
		}
		List<TaskModel> list = await TaskDao.GetTasksByIds(enumerable);
		if (where != null)
		{
			list = list.Where(where).ToList();
		}
		if (!list.Any())
		{
			return;
		}
		if (takeSnapShot)
		{
			snapShotDisposeDebounce?.Trigger();
			effectIds = list.Select((TaskModel x) => x.id).ToList();
			tempSnapShot = viewModel.TakeSnapshot();
		}
		UpdateOrRemoveTaskInGroups(list, viewModel.DefaultGroups);
		UpdateOrRemoveTaskInGroups(list, viewModel.AIRecommendGroups);
		UpdateOrRemoveTaskInGroups(list, viewModel.DetailGroups);
	}

	private void UpdateOrRemoveTaskInGroups(List<TaskModel> tasks, IEnumerable<AIRecommendGroup> groups)
	{
		foreach (AIRecommendGroup group in groups)
		{
			foreach (AIRecommendItem item in group.Items)
			{
				TaskModel taskModel = tasks.FirstOrDefault((TaskModel x) => x.id == item.TaskId);
				if (taskModel != null)
				{
					ApplyTaskOrRemove(item, taskModel, AIRecommendRemoveBy.Notify);
				}
			}
		}
	}

	private void ApplyTaskOrRemove(AIRecommendItem item, TaskModel task, AIRecommendRemoveBy removeBy)
	{
		item.TaskInfo.Task = new AIRecommendTaskInfo.TaskInfo(task);
		if (!viewModel.ApplyTaskToItem(item.TaskInfo, item, needCheck: true))
		{
			Action completedHandler = null;
			completedHandler = delegate
			{
				AIRecommendItem aIRecommendItem2 = item;
				aIRecommendItem2.OnRemoveCompleted = (Action)Delegate.Remove(aIRecommendItem2.OnRemoveCompleted, completedHandler);
				viewModel.RemoveItem(item, removeBy);
			};
			AIRecommendItem aIRecommendItem = item;
			aIRecommendItem.OnRemoveCompleted = (Action)Delegate.Combine(aIRecommendItem.OnRemoveCompleted, completedHandler);
			item.OnRemoveAnimate?.Invoke(obj: true);
		}
	}

	private void OnDeleteFinish()
	{
		tempSnapShot = null;
		effectIds.Clear();
	}

	private void CloseButton_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void TaskItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (!(sender is FrameworkElement frameworkElement))
		{
			return;
		}
		AIRecommendItem data = frameworkElement.DataContext as AIRecommendItem;
		if (data == null || lastPopupTask == data.TaskId)
		{
			return;
		}
		lastPopupTask = data.TaskId;
		data.IsSelected = true;
		TaskDetailPopup taskDetailPopup = new TaskDetailPopup();
		MainWindow window = App.Window;
		if (window != null)
		{
			if (PopupStateManager.LastTarget == this)
			{
				data.IsSelected = false;
				return;
			}
			taskDetailPopup.DependentWindow = window;
		}
		taskDetailPopup.Disappear += async delegate
		{
			data.IsSelected = false;
			TaskModel task = await TaskDao.GetTaskById(data.TaskId);
			ApplyTaskOrRemove(data, task, AIRecommendRemoveBy.DetailChange);
			await Task.Delay(100);
			lastPopupTask = null;
		};
		taskDetailPopup.Show(data.TaskId, null, new TaskWindowDisplayArgs(frameworkElement, 0.0)
		{
			TargetWidth = -415.0
		});
		UserActCollectUtils.AddClickEvent("ai_suggested_tasks", "suggested_tasks_page", "task_detail");
	}

	private async void AIRecTaskItemControl_CheckClicked(object sender, RoutedEventArgs e)
	{
		if (!(sender is FrameworkElement frameworkElement))
		{
			return;
		}
		AIRecommendItem data = frameworkElement.DataContext as AIRecommendItem;
		if (data == null)
		{
			return;
		}
		AIRecommendAllGroup snapshot = viewModel.TakeSnapshot();
		data.IsCompleted = true;
		data.UpdateIcon(2);
		bool isRemoved = false;
		Action completedHandler = null;
		completedHandler = delegate
		{
			AIRecommendItem aIRecommendItem2 = data;
			aIRecommendItem2.OnRemoveCompleted = (Action)Delegate.Remove(aIRecommendItem2.OnRemoveCompleted, completedHandler);
			isRemoved = true;
			TaskService.SetTaskStatus(data.TaskId, 2);
			viewModel.RemoveItem(data, AIRecommendRemoveBy.Checked);
		};
		AIRecommendItem aIRecommendItem = data;
		aIRecommendItem.OnRemoveCompleted = (Action)Delegate.Combine(aIRecommendItem.OnRemoveCompleted, completedHandler);
		data.OnRemoveAnimate?.Invoke(obj: true);
		CustomUndoToast customUndoToast = new CustomUndoToast();
		customUndoToast.SetText(Utils.GetString("Completed"));
		customUndoToast.SetEvent(async delegate
		{
		}, async delegate
		{
			if (isRemoved)
			{
				await TaskService.SetTaskStatus(data.TaskId, 0);
			}
			else
			{
				data.OnRemoveAnimate?.Invoke(obj: false);
			}
			viewModel.RecoverFromSnapShot(snapshot);
		});
		WindowToastHelper.ShowAndHideToast(ToastGrid, customUndoToast);
		UserActCollectUtils.AddClickEvent("ai_suggested_tasks", "suggested_tasks_page", "complete_task");
	}

	private async void AIRecTaskItemControl_AddClicked(object sender, RoutedEventArgs e)
	{
		if (!(sender is FrameworkElement frameworkElement))
		{
			return;
		}
		AIRecommendItem data = frameworkElement.DataContext as AIRecommendItem;
		if (data != null)
		{
			Action completedHandler = null;
			completedHandler = delegate
			{
				AIRecommendItem aIRecommendItem2 = data;
				aIRecommendItem2.OnRemoveCompleted = (Action)Delegate.Remove(aIRecommendItem2.OnRemoveCompleted, completedHandler);
				viewModel.RemoveItem(data, AIRecommendRemoveBy.AddToToday);
			};
			AIRecommendItem aIRecommendItem = data;
			aIRecommendItem.OnRemoveCompleted = (Action)Delegate.Combine(aIRecommendItem.OnRemoveCompleted, completedHandler);
			data.OnRemoveAnimate?.Invoke(obj: true);
			await TaskService.SetDate(data.TaskId, DateTime.Today);
			WindowToastHelper.ToastString(ToastGrid, Utils.GetString("AIAddedToToday"));
			UserActCollectUtils.AddClickEvent("ai_suggested_tasks", "suggested_tasks_page", "add_to_today");
		}
	}

	private void HintStackPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		viewModel.IsShowHint = false;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/ai/airecommendwindow.xaml", UriKind.Relative);
			System.Windows.Application.LoadComponent(this, resourceLocator);
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
			Root = (AIRecommendWindow)target;
			Root.Loaded += Window_Loaded;
			Root.Closing += Window_Closing;
			break;
		case 2:
			((System.Windows.Controls.Button)target).Click += CloseButton_Click;
			break;
		case 3:
			MainScrollViewer = (SmoothScrollViewer)target;
			break;
		case 4:
			DetailScrollViewer = (SmoothScrollViewer)target;
			break;
		case 5:
			EmptyView = (EmptyView)target;
			break;
		case 6:
			ToastGrid = (Grid)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
