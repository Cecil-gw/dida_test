using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using ICSharpCode.AvalonEdit.Document;
using Newtonsoft.Json;
using TickTickDao;
using TickTickModels;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.MainListView;
using ticktick_WPF.Views.MainListView.DetailView;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.QuickAdd;

namespace ticktick_WPF.Views.Calendar;

public class TaskWindow : IndependentWindow, IListViewParent
{
	private class TaskWindowSet
	{
		public double ProjectWidth { get; set; } = 256.0;

		public double DetailWidth { get; set; } = 1.0;

		public string SaveProject { get; set; }
	}

	public new static TaskWindow Window;

	public new static string Name = DisplayModule.Task.ToString();

	private ListViewContainer _listView;

	private WindowModel _model;

	private Border _immersiveBorder;

	private Border _containBorder;

	public static bool IsShowing => Window != null;

	private TaskWindow(WindowModel windowModel)
		: base(windowModel)
	{
		Id = Name;
		_model = windowModel;
		_immersiveBorder = new Border();
		Container.Children.Add(_immersiveBorder);
		_containBorder = new Border();
		Container.Children.Add(_containBorder);
		_listView = ListViewContainer.GetListView("TaskWindow", _containBorder);
		base.MinWidth = 400.0;
		base.MinHeight = 462.0;
		_containBorder.Child = _listView;
		base.MouseLeftButtonUp += OnWindowMouseUp;
		base.MouseLeftButtonDown += OnWindowMouseDown;
		base.Title = Utils.GetString("Task");
		base.Loaded += OnWindowLoaded;
	}

	private void OnWindowLoaded(object sender, RoutedEventArgs e)
	{
		((HwndSource)PresentationSource.FromVisual(this))?.AddHook(Hook);
		_listView.LoadSavedProject();
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
		_listView?.OnScroll(offset);
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
		if (_listView?.GetTaskView()?.DetailMouseOver() != true && Utils.GetMousePointVisibleItem<QuickAddView>(e, this) == null && Utils.GetMousePointVisibleItem<TextBox>(e, this) == null)
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

	public static async Task ShowWindow(bool force = true)
	{
		WindowModel windowModel = await WindowModelDao.GetModelByIdAsync(Name);
		if (!force && (windowModel == null || windowModel.Closed))
		{
			return;
		}
		if (Window != null)
		{
			Window.Show();
			Window.Activate();
			Window.Reload();
			return;
		}
		if (windowModel == null || windowModel.Closed)
		{
			IndependentWindow.AddShowEvent("tasks");
		}
		if (windowModel == null)
		{
			windowModel = await IndependentWindow.GetNewWindowModel(Name, isProject: false, JsonConvert.SerializeObject(new TaskWindowSet
			{
				ProjectWidth = LocalSettings.Settings.ProjectPanelWidth,
				DetailWidth = LocalSettings.Settings.DetailListDivide,
				SaveProject = LocalSettings.Settings.SelectProjectId
			}));
			BaseDao<WindowModel>.UpdateAsync(windowModel);
		}
		else
		{
			windowModel.Closed = false;
			await WindowModelDao.OpenWindow(windowModel);
		}
		Window = new TaskWindow(windowModel);
		Window.Show();
		Window.SetBackBrush();
	}

	private void Reload()
	{
		_listView?.ReloadView();
	}

	protected override void ClearCopiedTaskSelectionOnWindowMinimized()
	{
		_listView?.GetTaskView()?.ClearCopiedTaskSelectionOnWindowMinimized();
	}

	protected override void OnClosing()
	{
		Container.Children.Clear();
		ListViewContainer.RemoveListView("TaskWindow");
		Window = null;
	}

	public override void Print()
	{
		_listView?.TryPrint(isDetail: false);
	}

	public override void SwitchViewMode(string mode)
	{
		_listView?.ProjectIdentity?.SwitchViewMode(mode);
	}

	public static void OpenOrCloseWindow(Window owner)
	{
		if (Window != null)
		{
			Window.Close();
		}
		else if (IndependentWindow.CheckCount(owner))
		{
			ShowWindow();
		}
	}

	public async void OnProjectWidthChanged(double width)
	{
		WindowModel windowModel = await WindowModelDao.GetModelByIdAsync(Name);
		if (windowModel != null)
		{
			TaskWindowSet taskWindowSet = (string.IsNullOrEmpty(windowModel.Data) ? new TaskWindowSet() : (JsonConvert.DeserializeObject<TaskWindowSet>(windowModel.Data) ?? new TaskWindowSet()));
			taskWindowSet.ProjectWidth = width;
			windowModel.Data = JsonConvert.SerializeObject(taskWindowSet);
			await BaseDao<WindowModel>.UpdateAsync(windowModel);
			_model = windowModel;
		}
	}

	public async void OnDetailWidthChanged(double width)
	{
		WindowModel windowModel = await WindowModelDao.GetModelByIdAsync(Name);
		if (windowModel != null)
		{
			TaskWindowSet taskWindowSet = (string.IsNullOrEmpty(windowModel.Data) ? new TaskWindowSet() : (JsonConvert.DeserializeObject<TaskWindowSet>(windowModel.Data) ?? new TaskWindowSet()));
			taskWindowSet.DetailWidth = width;
			windowModel.Data = JsonConvert.SerializeObject(taskWindowSet);
			await BaseDao<WindowModel>.UpdateAsync(windowModel);
			_model = windowModel;
		}
	}

	public void SetMinSize(int width, int height)
	{
		base.MinWidth = width - 50;
		base.MinHeight = height;
	}

	public async void SaveSelectedProject(string saveProjectId)
	{
		WindowModel windowModel = await WindowModelDao.GetModelByIdAsync(Name);
		if (windowModel != null)
		{
			TaskWindowSet taskWindowSet = (string.IsNullOrEmpty(windowModel.Data) ? new TaskWindowSet() : (JsonConvert.DeserializeObject<TaskWindowSet>(windowModel.Data) ?? new TaskWindowSet()));
			taskWindowSet.SaveProject = saveProjectId;
			windowModel.Data = JsonConvert.SerializeObject(taskWindowSet);
			await BaseDao<WindowModel>.UpdateAsync(windowModel);
			_model = windowModel;
		}
	}

	public double GetProjectWidth()
	{
		return (string.IsNullOrEmpty(_model?.Data) ? new TaskWindowSet() : (JsonConvert.DeserializeObject<TaskWindowSet>(_model?.Data) ?? new TaskWindowSet())).ProjectWidth;
	}

	public double GetDetailWidth()
	{
		return (string.IsNullOrEmpty(_model?.Data) ? new TaskWindowSet() : (JsonConvert.DeserializeObject<TaskWindowSet>(_model?.Data) ?? new TaskWindowSet())).DetailWidth;
	}

	public string GetSelectedProject()
	{
		return (string.IsNullOrEmpty(_model?.Data) ? new TaskWindowSet() : (JsonConvert.DeserializeObject<TaskWindowSet>(_model?.Data) ?? new TaskWindowSet())).SaveProject;
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

	public void ExitImmersiveMode()
	{
		if (_immersiveBorder.Visibility == Visibility.Visible)
		{
			TaskDetailImmerseView source = (_immersiveBorder.Child as ImmersiveContent)?.TaskDetail;
			_listView?.GetTaskView()?.GetTaskDetail()?.CopyEditorStateFrom(source);
			_immersiveBorder.Visibility = Visibility.Collapsed;
			_immersiveBorder.Child = null;
			_containBorder.Visibility = Visibility.Visible;
		}
	}

	public async void EnterImmersiveMode(string taskId, int caretOffset = -1)
	{
		TaskDetailView taskDetailView = _listView?.GetTaskView()?.GetTaskDetail();
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

	public double GetListContainerWidth()
	{
		return base.ActualWidth;
	}

	public override void BatchSetPriorityCommand(int priority)
	{
		_listView?.TryBatchSetPriority(priority);
	}

	public override void BatchSetDateCommand(DateTime? date)
	{
		_listView?.TryBatchSetDate(date);
	}

	public override void BatchPinTaskCommand()
	{
		_listView?.BatchPinTask();
	}

	public override void BatchOpenStickyCommand()
	{
		_listView?.BatchOpenSticky();
	}

	public override void BatchDeleteCommand()
	{
		_listView?.BatchDeleteTask();
	}
}
