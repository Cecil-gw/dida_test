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
using System.Windows.Media;
using KotlinModels;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Eisenhower;
using ticktick_WPF.Views.Undo;

namespace ticktick_WPF.Views.Widget;

public class MatrixWidget : WidgetBaseUserControl, IToastShowWindow, IComponentConnector
{
	internal Grid UndoToastGrid;

	internal MatrixContainer MatrixContainer;

	internal Border NotProBorder;

	private bool _contentLoaded;

	public string ThemeId => base.Model.ThemeId;

	public MatrixWidget(WidgetViewModel model)
		: base(model)
	{
		base.DataContext = model;
		InitializeComponent();
		InitEvents(model);
		base.Loaded += OnLoaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		MatrixContainer.LoadTask();
		LockWidget(base.Model.IsLocked);
		CheckProEnable();
	}

	private void InitEvents(WidgetViewModel model)
	{
		MatrixContainer.SetWidgetMode(model);
		MatrixContainer.MoreAction += OnMoreAction;
		MatrixContainer.SyncGrid.MouseLeftButtonUp += OnSyncClick;
		MatrixContainer.DragPanel.PreviewMouseLeftButtonDown += base.OnDragBarMouseDown;
		MatrixContainer.DragPanel.MouseMove += base.OnDragMove;
		MatrixContainer.UnLockGrid.MouseLeftButtonUp += base.OnUnlockWidgetClick;
		MatrixContainer.Background = Brushes.Transparent;
	}

	internal override void OnOpacityChange(float opacity)
	{
		MatrixContainer.SetQuadrantBackOpacity((base.Model.ViewThemeId == "dark") ? 1f : opacity);
	}

	internal override void OnThemeChange(string themeKey)
	{
		MatrixContainer.SetQuadrantBackOpacity((base.Model.ViewThemeId == "dark") ? 1f : base.Model.Opacity);
	}

	internal override void LockWidget(bool isLocked)
	{
		base.LockWidget(isLocked);
		MatrixContainer.SetLocked(isLocked);
	}

	internal override void OnMoreAction(object sender, WidgetMoreAction e)
	{
		if (e == WidgetMoreAction.Sync)
		{
			OnSyncClick(null, null);
		}
		else
		{
			base.OnMoreAction(sender, e);
		}
	}

	internal override void CloseWidget()
	{
		MatrixWidgetHelper.CloseWidget();
	}

	private void OnSyncClick(object sender, MouseButtonEventArgs e)
	{
		MatrixContainer.MorePopup.IsOpen = false;
		MatrixContainer.BeginSyncStory();
		if (!Utils.IsNetworkAvailable())
		{
			TryToastString(null, Utils.GetString("NoNetwork"));
		}
		SyncManager.Sync(1);
	}

	private void OnGridWidgetSizeChanged(object sender, SizeChangedEventArgs e)
	{
	}

	private void OnRenewClick(object sender, RoutedEventArgs e)
	{
		Utils.StartUpgrade("matrix_widget");
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		MatrixWidgetHelper.CloseWidget();
	}

	public void Reload()
	{
		MatrixContainer.LoadTask();
	}

	public async void TaskDeleted(string taskId)
	{
		TaskModel task = await TaskDao.GetTaskById(taskId);
		if (task != null)
		{
			task.deleted = ((!TaskService.IsEmptyTask(task)) ? 1 : 2);
			task.modifiedTime = DateTime.Now;
			await TaskService.UpdateTaskOnDeletedChanged(task);
			UndoToast undoToast = new UndoToast();
			undoToast.InitTaskUndo(taskId, task.title, task.deleted == 2);
			ShowUndoToast(undoToast);
		}
	}

	public void TryToastString(object sender, string e)
	{
		WindowToastHelper.ToastString(UndoToastGrid, e, 400.0);
	}

	public async Task<bool> BatchDeleteTask(List<TaskModel> tasks)
	{
		return await TaskService.BatchDeleteTasks(tasks, showUndo: true, 1, UndoToastGrid);
	}

	public async void TaskComplete(CloseUndoToast undo)
	{
		WindowToastHelper.ShowAndHideToast(UndoToastGrid, undo);
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

	public void ToastDeleteRecUndo(List<TaskDeleteRecurrenceUndoEntity> undoModels)
	{
		if (undoModels != null && undoModels.Any())
		{
			UndoToast undoToast = new UndoToast();
			undoToast.InitTaskUndo(undoModels);
			ShowUndoToast(undoToast);
		}
	}

	public void Toast(FrameworkElement uiElement)
	{
		WindowToastHelper.ShowAndHideToast(UndoToastGrid, uiElement);
	}

	public void ToastMoveProjectControl(string projectId, string taskName = null, MoveToastType moveType = MoveToastType.Move)
	{
	}

	public async Task ShowUndoToast(UndoToast undoToast)
	{
		WindowToastHelper.ShowAndHideToast(UndoToastGrid, undoToast);
	}

	internal override bool IsEditing()
	{
		return MatrixContainer.InOperation;
	}

	public void CheckProEnable()
	{
		MatrixContainer.SetBlur();
		NotProBorder.Visibility = (UserDao.IsPro() ? Visibility.Collapsed : Visibility.Visible);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/widget/matrixwidget.xaml", UriKind.Relative);
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
			MatrixContainer = (MatrixContainer)target;
			break;
		case 3:
			NotProBorder = (Border)target;
			break;
		case 4:
			((Button)target).Click += OnCancelClick;
			break;
		case 5:
			((Button)target).Click += OnRenewClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
