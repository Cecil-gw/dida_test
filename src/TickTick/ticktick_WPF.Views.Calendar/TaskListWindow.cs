using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using ICSharpCode.AvalonEdit.Document;
using TickTickDao;
using TickTickModels;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.MainListView;
using ticktick_WPF.Views.MainListView.DetailView;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.QuickAdd;

namespace ticktick_WPF.Views.Calendar;

public class TaskListWindow : IndependentWindow, IListViewParent
{
	public static ConcurrentDictionary<string, TaskListWindow> Windows = new ConcurrentDictionary<string, TaskListWindow>();

	private TaskView _taskView;

	private Border _immersiveBorder;

	private Border _containBorder;

	private TaskListWindow(WindowModel windowModel)
		: base(windowModel)
	{
		Id = windowModel.Id;
		_immersiveBorder = new Border();
		Container.Children.Add(_immersiveBorder);
		_containBorder = new Border();
		Container.Children.Add(_containBorder);
		_taskView = new TaskView(showProjectMenu: false);
		base.MinWidth = 340.0;
		base.MinHeight = 400.0;
		_containBorder.Child = _taskView;
		base.Loaded += async delegate
		{
			_taskView.SetColumns();
			ProjectIdentity projectIdentity = ProjectIdentity.BuildProject(Id, getDefault: false);
			_taskView.OnProjectSelect(projectIdentity);
			((HwndSource)PresentationSource.FromVisual(this))?.AddHook(Hook);
			base.Title = projectIdentity?.GetDisplayTitle();
		};
		base.MouseLeftButtonUp += OnWindowMouseUp;
		base.MouseLeftButtonDown += OnWindowMouseDown;
		base.PreviewMouseLeftButtonDown += OnPreviewMouseDown;
		DataChangedNotifier.ViewModeChanged += OnProjectViewModeChanged;
		DataChangedNotifier.ProjectChanged += OnIdentityChanged;
		DataChangedNotifier.ProjectGroupChanged += OnIdentityChanged;
		DataChangedNotifier.TagChanged += OnIdentityChanged;
		DataChangedNotifier.FilterChanged += OnIdentityChanged;
	}

	private void OnIdentityChanged(object sender, object e)
	{
		ProjectIdentity projectIdentity = ProjectIdentity.BuildProject(Id, getDefault: false);
		try
		{
			base.Title = projectIdentity?.GetDisplayTitle();
		}
		catch (Exception)
		{
			base.Title = Utils.GetString("Task");
		}
	}

	private IntPtr Hook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
	{
		if (msg == 526)
		{
			int num = 0;
			num = LOWORD(wParam);
			OnMouseTilt(num);
			return (IntPtr)1;
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
		_taskView?.OnTouchScroll(offset);
	}

	protected override void ClearCopiedTaskSelectionOnWindowMinimized()
	{
		_taskView?.ClearCopiedTaskSelectionOnWindowMinimized();
	}

	private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
	{
		_taskView?.TryFoldDetailOnMouseDown(e);
	}

	private void OnWindowMouseDown(object sender, MouseButtonEventArgs e)
	{
		_mouseDown = true;
	}

	private async void OnWindowMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (!_mouseDown)
		{
			return;
		}
		_mouseDown = false;
		if (!(_taskView?.DetailMouseOver() ?? false) && Utils.GetMousePointVisibleItem<QuickAddView>(e, this) == null && Utils.GetMousePointVisibleItem<TextBox>(e, this) == null)
		{
			await Task.Delay(100);
			if (base.IsActive && !e.Handled)
			{
				TryFocus();
			}
		}
	}

	public async Task TryFocus()
	{
		await Task.Delay(10);
		Keyboard.ClearFocus();
		if (!PopupStateManager.CanShowAddPopup())
		{
			await Task.Delay(300);
			if (!PopupStateManager.CanShowAddPopup())
			{
				return;
			}
		}
		if (base.IsActive)
		{
			FocusManager.SetFocusedElement(this, this);
			Keyboard.Focus(this);
		}
	}

	private void OnProjectViewModeChanged(object sender, ProjectIdentity e)
	{
		if (e.Id == _taskView?.ProjectIdentity?.Id)
		{
			_taskView?.OnProjectSelect(ProjectIdentity.BuildProject(Id, getDefault: false));
		}
	}

	public static async Task ShowSavedWindows()
	{
		foreach (WindowModel item in await WindowModelDao.GetAllAsync())
		{
			if (!item.Closed && item.Id.Contains(":"))
			{
				if (ProjectIdentity.BuildProject(item.Id, getDefault: false) == null)
				{
					break;
				}
				if (Windows.ContainsKey(item.Id))
				{
					TaskListWindow taskListWindow = Windows[item.Id];
					taskListWindow.Show();
					taskListWindow.Activate();
					taskListWindow.Reload();
				}
				else
				{
					TaskListWindow taskListWindow2 = new TaskListWindow(item);
					taskListWindow2.Show();
					taskListWindow2.SetBackBrush();
					Windows.TryAdd(item.Id, taskListWindow2);
				}
			}
		}
	}

	public static async Task ShowWindow(string projectId, bool force = true, Window win = null)
	{
		ProjectIdentity identity = ProjectIdentity.BuildProject(projectId, getDefault: false);
		if (identity == null)
		{
			return;
		}
		WindowModel windowModel = await WindowModelDao.GetModelByIdAsync(projectId);
		if (Windows.ContainsKey(projectId))
		{
			TaskListWindow taskListWindow = Windows[projectId];
			taskListWindow.Show();
			taskListWindow.Activate();
			taskListWindow.Reload();
			return;
		}
		IndependentWindow.AddShowEvent(GetEventName(identity));
		if (windowModel != null)
		{
			await WindowModelDao.OpenWindow(windowModel);
		}
		else
		{
			windowModel = await IndependentWindow.GetNewWindowModel(projectId, isProject: true, null, win);
		}
		TaskListWindow taskListWindow2 = new TaskListWindow(windowModel);
		taskListWindow2.Show();
		taskListWindow2.SetBackBrush();
		Windows.TryAdd(projectId, taskListWindow2);
	}

	private static string GetEventName(ProjectIdentity identity)
	{
		if (!(identity is WeekProjectIdentity))
		{
			if (!(identity is AssignToMeProjectIdentity))
			{
				if (!(identity is AbandonedProjectIdentity))
				{
					if (!(identity is CompletedProjectIdentity))
					{
						if (!(identity is TrashProjectIdentity))
						{
							if (!(identity is AllProjectIdentity))
							{
								if (!(identity is TodayProjectIdentity))
								{
									if (!(identity is TomorrowProjectIdentity))
									{
										if (!(identity is NormalProjectIdentity normalProjectIdentity))
										{
											if (!(identity is TagProjectIdentity))
											{
												if (!(identity is GroupProjectIdentity))
												{
													if (!(identity is FilterProjectIdentity))
													{
														if (identity is SubscribeCalendarProjectIdentity || identity is BindAccountCalendarProjectIdentity)
														{
															return "events";
														}
														return null;
													}
													return "filter";
												}
												return "folder";
											}
											return "tag";
										}
										ProjectModel project = normalProjectIdentity.Project;
										if (project == null || !project.Isinbox)
										{
											return "list";
										}
										return "inbox";
									}
									return "tomorrow";
								}
								return "today";
							}
							return "all";
						}
						return "trash";
					}
					return "completed";
				}
				return "won_t_do";
			}
			return "assign_to_me";
		}
		return "n7d";
	}

	public bool CanShowPopup()
	{
		if (_taskView.IsFoldColumn)
		{
			return LocalSettings.Settings.ExtraSettings.TaskDetailLayout == "Popup";
		}
		return false;
	}

	private void Reload()
	{
		_taskView?.ReloadView();
	}

	protected override void OnSizeChanged(SizeChangedEventArgs e)
	{
		if (e.NewSize.Width > 800.0)
		{
			_taskView?.ExpandDetail();
			_taskView?.SetListColumnSpan(1, 320.0, 312.0);
		}
		else
		{
			_taskView?.FoldDetail();
			_taskView?.SetListColumnSpan(2, 0.0, 0.0);
		}
		if (Math.Abs(e.NewSize.Width - e.PreviousSize.Width) > 3.0 && base.ActualWidth > 0.0)
		{
			_taskView?.ResetColumnWidth(base.ActualWidth);
		}
	}

	protected override void OnClosing()
	{
		DataChangedNotifier.ViewModeChanged -= OnProjectViewModeChanged;
		Container.Children.Clear();
		Windows.TryRemove(Id, out var _);
	}

	public override void Print()
	{
		_taskView?.TryPrint(isDetail: false);
	}

	public override void SwitchViewMode(string mode)
	{
		_taskView?.ProjectIdentity?.SwitchViewMode(mode);
	}

	public double GetListContainerWidth()
	{
		return base.ActualWidth;
	}

	public static async void OpenOrCloseWindow(string id, Window w)
	{
		if (!string.IsNullOrEmpty(id))
		{
			if (Windows.TryRemove(id, out var value))
			{
				value.Close();
			}
			else if (IndependentWindow.CheckCount(w))
			{
				await Task.Delay(100);
				ShowWindow(id, force: true, w);
			}
		}
	}

	public override void BatchSetPriorityCommand(int priority)
	{
		_taskView?.TryBatchSetPriority(priority);
	}

	public override void BatchSetDateCommand(DateTime? date)
	{
		_taskView?.TryBatchSetDate(date);
	}

	public override void BatchPinTaskCommand()
	{
		_taskView?.BatchPinTask();
	}

	public override void BatchOpenStickyCommand()
	{
		_taskView?.BatchOpenSticky();
	}

	public override void BatchDeleteCommand()
	{
		_taskView?.BatchDeleteTask();
	}

	public static void CloseAllWindow()
	{
		foreach (TaskListWindow item in Windows.Values.ToList())
		{
			item.Close();
		}
	}

	public void ExitImmersiveMode()
	{
		if (_immersiveBorder.Visibility == Visibility.Visible)
		{
			TaskDetailImmerseView source = (_immersiveBorder.Child as ImmersiveContent)?.TaskDetail;
			_taskView?.GetTaskDetail()?.CopyEditorStateFrom(source);
			_immersiveBorder.Visibility = Visibility.Collapsed;
			_immersiveBorder.Child = null;
			_containBorder.Visibility = Visibility.Visible;
		}
	}

	public async void EnterImmersiveMode(string taskId, int caretOffset = -1)
	{
		TaskDetailView taskDetailView = _taskView?.GetTaskDetail();
		TaskDetailView taskDetailView2 = TaskDetailView.TakePendingImmersiveSource(taskId) ?? taskDetailView;
		MarkDownEditor markDownEditor = taskDetailView2?.GetContentText() ?? taskDetailView2?.GetDescText();
		int sourceCaretOffset = ((caretOffset >= 0) ? caretOffset : (markDownEditor?.EditBox.CaretOffset ?? 0));
		ImmersiveContent editor = new ImmersiveContent
		{
			Margin = new Thickness(0.0, 24.0, 0.0, 12.0),
			VerticalAlignment = VerticalAlignment.Stretch
		};
		_immersiveBorder.Child = editor;
		_immersiveBorder.Visibility = Visibility.Visible;
		_containBorder.Visibility = Visibility.Collapsed;
		await editor.LoadData(taskId, taskDetailView2);
		MarkDownEditor markDownEditor2 = editor.TaskDetail.GetContentText() ?? editor.TaskDetail.GetDescText();
		if (markDownEditor2 != null && sourceCaretOffset >= 0)
		{
			markDownEditor2.EditBox.CaretOffset = Math.Min(sourceCaretOffset, markDownEditor2.EditBox.Text.Length);
			markDownEditor2.FocusEditBox();
			TextLocation location = markDownEditor2.EditBox.Document.GetLocation(markDownEditor2.EditBox.CaretOffset);
			markDownEditor2.EditBox.ScrollTo(location.Line, location.Column);
		}
	}

	public void SetMinSize(int width, int height)
	{
		base.MinWidth = width - 50;
		base.MinHeight = height;
	}

	public async Task NavigateTask(ProjectTask task)
	{
		if (await TaskUtils.TryLoadTask(task.TaskId, task.ProjectId) != null)
		{
			TaskDetailWindow taskDetailWindow = new TaskDetailWindow();
			taskDetailWindow.ShowInNavigate(task.TaskId, this);
			taskDetailWindow.DependentWindow = this;
		}
	}

	public void OnProjectWidthChanged(double width)
	{
	}

	public void OnDetailWidthChanged(double width)
	{
	}

	public void SaveSelectedProject(string saveProjectId)
	{
	}

	public double GetProjectWidth()
	{
		return 0.0;
	}

	public double GetDetailWidth()
	{
		return 0.0;
	}

	public string GetSelectedProject()
	{
		return "";
	}

	public static void SetWindowsBackBrush()
	{
		foreach (TaskListWindow value in Windows.Values)
		{
			value.SetBackBrush();
		}
	}
}
