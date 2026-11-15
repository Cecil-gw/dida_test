using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Agenda;
using ticktick_WPF.Views.MainListView.DetailView;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Detail;

public class TaskDetailBottomControl : Grid, IComponentConnector
{
	private readonly TaskDetailView _parent;

	private bool _inNavigate;

	private bool _moveProjectMouseDown;

	private bool _moreGridMouseDown;

	private string _switchTag;

	private bool _switchVisible;

	private bool _isPin;

	internal Grid AttendeeItemsPanel;

	internal AttendeeDisplayControl AttendeePanel;

	internal Grid TaskDetailUnit;

	internal Grid TaskDetailMoveGrid;

	internal EscPopup SetProjectPopup;

	internal Grid EditorIcon;

	internal Border EditIconSelectedBorder;

	internal HoverIconButton EditButton;

	internal HoverIconButton CommentGrid;

	internal HoverIconButton DeleteGrid;

	internal HoverIconButton MoreGrid;

	internal EscPopup MorePopup;

	internal ContentControl MoreContentContainer;

	internal Grid TaskDetailTrashGrid;

	internal Grid TrashRestoreGrid;

	internal HoverIconButton RemoveButton;

	private bool _contentLoaded;

	public TaskDetailBottomControl(TaskDetailView taskDetailControl)
	{
		InitializeComponent();
		_parent = taskDetailControl;
	}

	private void ShowAttendeeDialog(object sender, MouseButtonEventArgs e)
	{
		_parent?.ShowAttendeeDialog();
	}

	private void MoveProjectMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (_moveProjectMouseDown)
		{
			_moveProjectMouseDown = false;
			TaskDetailMoveGrid.MouseLeftButtonUp -= MoveProjectMouseUp;
			_parent?.OnMoveProject(SetProjectPopup);
			TaskDetailMoveGrid.MouseLeftButtonUp += MoveProjectMouseUp;
		}
	}

	private void PopupOpened(object sender, EventArgs e)
	{
		_parent?.PopupOpened(sender, e);
	}

	private void PopupClosed(object sender, EventArgs e)
	{
		_parent?.PopupClosed(sender, e);
		MoreContentContainer.Content = null;
	}

	private void OnEditorClick(object sender, MouseButtonEventArgs e)
	{
		_parent?.OnEditorClick();
	}

	private void OnCommentClick(object sender, MouseButtonEventArgs e)
	{
		if (MorePopup.IsOpen)
		{
			MorePopup.IsOpen = false;
		}
		_parent?.OnCommentClick();
		e.Handled = true;
	}

	private void OnDeleteMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (MorePopup.IsOpen)
		{
			MorePopup.IsOpen = false;
		}
		_parent?.OnDelete();
	}

	private async void MoreGridClick(object sender, MouseButtonEventArgs e)
	{
		if (!_moreGridMouseDown)
		{
			return;
		}
		_moreGridMouseDown = false;
		TaskDetailMoreContent taskDetailMoreContent = new TaskDetailMoreContent();
		taskDetailMoreContent.SetParent(_parent, MorePopup);
		MoreContentContainer.Content = taskDetailMoreContent;
		taskDetailMoreContent.SwitchTaskNoteItem.Tag = _switchTag;
		taskDetailMoreContent.SwitchTaskNoteItem.Visibility = ((!_switchVisible) ? Visibility.Collapsed : Visibility.Visible);
		taskDetailMoreContent.ConvertToEventItem.Visibility = Visibility.Collapsed;
		if (base.DataContext is TaskDetailViewModel taskDetailViewModel)
		{
			UserActCollectUtils.AddClickEvent("task_detail", "action", "three_dots");
			if (!taskDetailViewModel.IsNewAdd)
			{
				bool flag = taskDetailViewModel.Permission != "read" && taskDetailViewModel.Permission != "comment";
				bool flag2 = taskDetailViewModel.Kind == "NOTE";
				taskDetailMoreContent.SelectedIndex = -1;
				taskDetailMoreContent.CopyItem.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
				taskDetailMoreContent.NewUploadItem.Visibility = ((!flag || !taskDetailViewModel.IsOwner) ? Visibility.Collapsed : Visibility.Visible);
				taskDetailMoreContent.Print.Visibility = Visibility.Visible;
				taskDetailMoreContent.CopyLinkItem.Visibility = Visibility.Visible;
				taskDetailMoreContent.AddTemplate.Visibility = Visibility.Visible;
				taskDetailMoreContent.OMTopLine.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
				taskDetailMoreContent.AbandonedGrid.Visibility = ((!(!flag2 && flag)) ? Visibility.Collapsed : Visibility.Visible);
				taskDetailMoreContent.ActivityItem.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
				taskDetailMoreContent.ActivityItem.Text = Utils.GetString(flag2 ? "NoteActivities" : "TaskActivities");
				taskDetailMoreContent.PomoItem.Visibility = ((!(LocalSettings.Settings.EnableFocus && taskDetailViewModel.Status == 0 && flag)) ? Visibility.Collapsed : Visibility.Visible);
				taskDetailMoreContent.NewTagItem.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
				taskDetailMoreContent.NewInsetSummaryItem.Visibility = ((!(flag && flag2)) ? Visibility.Collapsed : Visibility.Visible);
				taskDetailMoreContent.FirstOpenTag = true;
				taskDetailMoreContent.PinButton.SetResourceReference(OptionCheckBox.IconProperty, _isPin ? "Ic.BatchEdit.CancelTopmost" : "Ic.BatchEdit.Topmost");
				taskDetailMoreContent.PinButton.SetResourceReference(OptionCheckBox.TextProperty, _isPin ? "Unpin" : "Pin");
				if (!flag)
				{
					taskDetailMoreContent.SwitchTaskNoteItem.Visibility = Visibility.Collapsed;
				}
				taskDetailMoreContent.DeletePanel.Visibility = ((!flag || _inNavigate) ? Visibility.Collapsed : Visibility.Visible);
				taskDetailMoreContent.StickyItem.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
				taskDetailMoreContent.SwitchTaskNoteItem.Text = (flag2 ? Utils.GetString("ConvertToTask") : Utils.GetString("ConvertToNote"));
				taskDetailMoreContent.SwitchTaskNoteItem.SetResourceReference(OptionCheckBox.IconProperty, flag2 ? "Ic.BatchEdit.ConvertToTask" : "Ic.BatchEdit.ConvertToNote");
				taskDetailMoreContent.ConvertToEventItem.Visibility = Visibility.Collapsed;
				if (TaskDao.GetTaskLevel(taskDetailViewModel.TaskId, taskDetailViewModel.ProjectId) >= 4 || flag2 || !flag)
				{
					taskDetailMoreContent.NewAddSubTaskItem.Visibility = Visibility.Collapsed;
				}
				else
				{
					taskDetailMoreContent.NewAddSubTaskItem.Visibility = Visibility.Visible;
				}
				taskDetailMoreContent.LinkParentTaskItem.Visibility = ((flag2 || !flag || !TaskCache.ExistSubTasks()) ? Visibility.Collapsed : Visibility.Visible);
				taskDetailMoreContent.PinButton.Visibility = ((!flag || taskDetailViewModel.InCal) ? Visibility.Collapsed : Visibility.Visible);
			}
			else
			{
				Grid newTagItem = taskDetailMoreContent.NewTagItem;
				Visibility visibility = (taskDetailMoreContent.UseTemplate.Visibility = Visibility.Visible);
				newTagItem.Visibility = visibility;
				taskDetailMoreContent.SwitchTaskNoteItem.Visibility = Visibility.Visible;
				taskDetailMoreContent.SwitchTaskNoteItem.Tag = null;
				taskDetailMoreContent.SwitchTaskNoteItem.Text = (taskDetailViewModel.IsNote ? Utils.GetString("ConvertToTask") : Utils.GetString("ConvertToNote"));
				taskDetailMoreContent.SwitchTaskNoteItem.SetResourceReference(OptionCheckBox.IconProperty, taskDetailViewModel.IsNote ? "Ic.TaskOM.ConvertToTask" : "Ic.TaskOM.ConvertToNote");
				taskDetailMoreContent.ConvertToEventItem.Visibility = ((!(_parent?.CanConvertNewTaskToEvent() ?? false)) ? Visibility.Collapsed : Visibility.Visible);
				OptionCheckBox newAddSubTaskItem = taskDetailMoreContent.NewAddSubTaskItem;
				OptionCheckBox linkParentTaskItem = taskDetailMoreContent.LinkParentTaskItem;
				OptionCheckBox newUploadItem = taskDetailMoreContent.NewUploadItem;
				OptionCheckBox pinButton = taskDetailMoreContent.PinButton;
				Grid abandonedGrid = taskDetailMoreContent.AbandonedGrid;
				OptionCheckBox newInsetSummaryItem = taskDetailMoreContent.NewInsetSummaryItem;
				Grid pomoItem = taskDetailMoreContent.PomoItem;
				Line oMTopLine = taskDetailMoreContent.OMTopLine;
				OptionCheckBox activityItem = taskDetailMoreContent.ActivityItem;
				OptionCheckBox addTemplate = taskDetailMoreContent.AddTemplate;
				OptionCheckBox copyItem = taskDetailMoreContent.CopyItem;
				OptionCheckBox copyLinkItem = taskDetailMoreContent.CopyLinkItem;
				OptionCheckBox stickyItem = taskDetailMoreContent.StickyItem;
				OptionCheckBox print = taskDetailMoreContent.Print;
				Visibility visibility3 = (taskDetailMoreContent.DeletePanel.Visibility = Visibility.Collapsed);
				Visibility visibility5 = (print.Visibility = visibility3);
				Visibility visibility7 = (stickyItem.Visibility = visibility5);
				Visibility visibility9 = (copyLinkItem.Visibility = visibility7);
				Visibility visibility11 = (copyItem.Visibility = visibility9);
				Visibility visibility13 = (addTemplate.Visibility = visibility11);
				Visibility visibility15 = (activityItem.Visibility = visibility13);
				Visibility visibility17 = (oMTopLine.Visibility = visibility15);
				Visibility visibility19 = (pomoItem.Visibility = visibility17);
				Visibility visibility21 = (newInsetSummaryItem.Visibility = visibility19);
				Visibility visibility23 = (abandonedGrid.Visibility = visibility21);
				Visibility visibility25 = (pinButton.Visibility = visibility23);
				Visibility visibility27 = (newUploadItem.Visibility = visibility25);
				visibility = (linkParentTaskItem.Visibility = visibility27);
				newAddSubTaskItem.Visibility = visibility;
			}
		}
		await Task.Delay(20);
		MorePopup.IsOpen = !MorePopup.IsOpen;
		Window window = Window.GetWindow(this);
		if (window != null)
		{
			FocusManager.SetFocusedElement(window, window);
		}
	}

	private void RestoreProjectMouseUp(object sender, MouseButtonEventArgs e)
	{
		_parent?.OnRestoreProject();
	}

	private void DeleteFromTrashMouseUp(object sender, MouseButtonEventArgs e)
	{
		_parent?.OnDeleteFromTrash();
	}

	public void SetSwitchTagAndVisible(string tag, bool visible)
	{
		_switchTag = tag;
		_switchVisible = visible;
	}

	public void SetItemVisible(string itemName, bool show)
	{
		UIElement uIElement = null;
		switch (itemName)
		{
		case "EditorIcon":
			uIElement = EditorIcon;
			break;
		case "CommentGrid":
			uIElement = CommentGrid;
			break;
		case "DeleteGrid":
			uIElement = DeleteGrid;
			break;
		case "EditIconSelectedBorder":
			uIElement = EditIconSelectedBorder;
			break;
		}
		if (uIElement != null)
		{
			uIElement.Visibility = ((!show) ? Visibility.Collapsed : Visibility.Visible);
		}
	}

	public async Task LoadAttendees(TaskDetailViewModel task, Constants.DetailMode mode)
	{
		AttendeeItemsPanel.Visibility = Visibility.Collapsed;
		if (!string.IsNullOrEmpty(task.AttendId))
		{
			await AttendeePanel.LoadData((mode == Constants.DetailMode.Page) ? base.ActualWidth : 340.0, task.AttendId, task.TaskId);
			if (!(await AttendeePanel.WaitingForAttend(task)))
			{
				AttendeeItemsPanel.Visibility = Visibility.Visible;
			}
		}
	}

	public void OnDataBind(TaskDetailViewModel task)
	{
		bool flag = task.Deleted == 1;
		bool flag2 = task.Permission != "comment" && task.Permission != "read";
		TaskDetailTrashGrid.Visibility = ((!(flag && flag2)) ? Visibility.Collapsed : Visibility.Visible);
		TaskDetailUnit.Visibility = (flag ? Visibility.Collapsed : Visibility.Visible);
		ProjectModel projectById = CacheManager.GetProjectById(task.ProjectId);
		if (projectById != null && !projectById.IsValid())
		{
			base.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
		}
		else
		{
			base.Visibility = Visibility.Visible;
			if (!flag2)
			{
				TaskDetailMoveGrid.Cursor = Cursors.No;
				RemoveButton.Cursor = Cursors.No;
				CommentGrid.Cursor = ((task.Permission == "read") ? Cursors.No : Cursors.Hand);
			}
			else
			{
				TaskDetailMoveGrid.Cursor = Cursors.Hand;
				RemoveButton.Cursor = Cursors.Hand;
				CommentGrid.Cursor = Cursors.Hand;
			}
			if (flag && task.TeamId == TeamService.GetTeamId())
			{
				RemoveButton.Visibility = Visibility.Collapsed;
			}
			else
			{
				RemoveButton.Visibility = Visibility.Visible;
			}
		}
		DeleteGrid.Visibility = Visibility.Collapsed;
	}

	public void SetNavigate()
	{
		_inNavigate = true;
		DeleteGrid.Visibility = Visibility.Collapsed;
		TrashRestoreGrid.Visibility = Visibility.Collapsed;
		RemoveButton.Visibility = Visibility.Collapsed;
	}

	public void SetPin(bool isPin)
	{
		_isPin = isPin;
	}

	private void MoveProjectMouseDown(object sender, MouseButtonEventArgs e)
	{
		_moveProjectMouseDown = true;
	}

	private void MoreGridDown(object sender, MouseButtonEventArgs e)
	{
		_moreGridMouseDown = true;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/detail/taskdetailbottomcontrol.xaml", UriKind.Relative);
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
			AttendeeItemsPanel = (Grid)target;
			AttendeeItemsPanel.MouseLeftButtonUp += ShowAttendeeDialog;
			break;
		case 2:
			AttendeePanel = (AttendeeDisplayControl)target;
			break;
		case 3:
			TaskDetailUnit = (Grid)target;
			break;
		case 4:
			TaskDetailMoveGrid = (Grid)target;
			TaskDetailMoveGrid.MouseLeftButtonUp += MoveProjectMouseUp;
			TaskDetailMoveGrid.PreviewMouseLeftButtonDown += MoveProjectMouseDown;
			break;
		case 5:
			SetProjectPopup = (EscPopup)target;
			break;
		case 6:
			EditorIcon = (Grid)target;
			EditorIcon.MouseLeftButtonUp += OnEditorClick;
			break;
		case 7:
			EditIconSelectedBorder = (Border)target;
			break;
		case 8:
			EditButton = (HoverIconButton)target;
			break;
		case 9:
			CommentGrid = (HoverIconButton)target;
			break;
		case 10:
			DeleteGrid = (HoverIconButton)target;
			break;
		case 11:
			MoreGrid = (HoverIconButton)target;
			break;
		case 12:
			MorePopup = (EscPopup)target;
			break;
		case 13:
			MoreContentContainer = (ContentControl)target;
			break;
		case 14:
			TaskDetailTrashGrid = (Grid)target;
			break;
		case 15:
			TrashRestoreGrid = (Grid)target;
			TrashRestoreGrid.MouseLeftButtonUp += RestoreProjectMouseUp;
			break;
		case 16:
			RemoveButton = (HoverIconButton)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
