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
using System.Windows.Shapes;
using Emoji.Wpf;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Service.SortOrder;
using ticktick_WPF.Util;
using ticktick_WPF.Util.DragAndDrop;
using ticktick_WPF.Util.Filter;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Util.Sort;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.MainListView.TaskList;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Tag;
using ticktick_WPF.Views.TaskList;

namespace ticktick_WPF.Views.Eisenhower;

public class QuadrantControl : Border, IBatchEditable, IComponentConnector
{
	public TaskListView TaskList = new TaskListView();

	public static readonly DependencyProperty InOptionProperty = DependencyProperty.Register("InOption", typeof(bool), typeof(QuadrantControl), new PropertyMetadata(false));

	private int _level = 1;

	private MatrixQuadrantIdentity _identity;

	private BatchTaskEditHelper _batchHelper;

	private bool _previewDrag;

	private bool _isLocked;

	private MatrixContainer _matrix;

	private bool _omMouseDown;

	private bool _dragOver;

	internal QuadrantControl Root;

	internal Border BackBorder;

	internal Border HoverBorder;

	internal ColumnDefinition OptionColumn;

	internal Grid IconGrid;

	internal Path Icon;

	internal EmjTextBlock Emoji;

	internal EmjTextBlock Title;

	internal HoverIconButton AddButton;

	internal HoverIconButton MoreButton;

	internal EscPopup MorePopup;

	internal ContentControl TaskListContent;

	internal TextBlock EmptyText;

	private bool _contentLoaded;

	public bool InOption
	{
		get
		{
			return (bool)GetValue(InOptionProperty);
		}
		set
		{
			SetValue(InOptionProperty, value);
		}
	}

	public QuadrantModel Quadrant => _identity?.Quadrant;

	public int Level
	{
		get
		{
			return _level;
		}
		set
		{
			_level = value;
			OnLevelSet();
		}
	}

	public QuadrantControl()
	{
		InitializeComponent();
		base.Loaded += delegate
		{
			if (TaskListContent.Content == null)
			{
				TaskListContent.Content = TaskList;
			}
			TickDragHelper.DragDrop += OnDrop;
			TickDragHelper.DragMoving += OnDragOver;
			DataChangedNotifier.InterfaceStyleChanged += DataChangedNotifier_InterfaceStyleChanged;
			DataChangedNotifier_InterfaceStyleChanged(null, null);
		};
		base.Unloaded += delegate
		{
			TaskListContent.Content = null;
			TickDragHelper.DragDrop -= OnDrop;
			TickDragHelper.DragMoving -= OnDragOver;
			DataChangedNotifier.InterfaceStyleChanged -= DataChangedNotifier_InterfaceStyleChanged;
		};
		_batchHelper = new BatchTaskEditHelper(this, _level);
		_batchHelper.ShowOrHideOperation += OnBatchOperationChanged;
		TaskList.MatrixLoadRequested = LoadAsync;
		TaskList.ViewModel.BatchEditor = this;
		TaskList.ItemsCountChanged += OnTaskCountChanged;
	}

	private void DataChangedNotifier_InterfaceStyleChanged(object sender, EventArgs e)
	{
		switch (LocalSettings.Settings.ExtraSettings.InterfaceStyle)
		{
		case InterfaceStyle.Flat:
			BackBorder.BorderThickness = new Thickness(0.0);
			BackBorder.Effect = null;
			break;
		case InterfaceStyle.Card:
			BackBorder.BorderThickness = new Thickness(1.0);
			BackBorder.SetResourceReference(UIElement.EffectProperty, "Shadow");
			break;
		}
	}

	private void OnBatchOperationChanged(object sender, bool e)
	{
		SetMatrixInOperation(e);
	}

	private void ReloadData(object sender, List<string> e)
	{
		LoadAsync();
	}

	private void OnLevelSet()
	{
		if (_level >= 1 && _level <= 4)
		{
			string key = "MatrixColorQuadrant" + _level;
			string index = "MatrixIconQuadrant" + _level;
			string key2 = "MatrixTitleQuadrant" + _level;
			SolidColorBrush color = ThemeUtil.GetColor(key);
			Icon.Data = Utils.GetIcon(index);
			Icon.Fill = color;
			Title.Foreground = color;
			Title.Text = Utils.GetString(key2);
			TaskList.QuadrantLevel = _level;
		}
	}

	private void OnAddTaskClick(object sender, MouseButtonEventArgs e)
	{
		if (!PopupStateManager.CanShowAddPopup())
		{
			return;
		}
		UserActCollectUtils.AddClickEvent("matrix", "matrix_action", "add");
		TaskDetailViewModel taskDetailViewModel = TaskDetailViewModel.BuildInitModel(null);
		if (_identity != null)
		{
			taskDetailViewModel.SourceViewModel.Tag = TagSerializer.ToJsonContent(_identity.GetTags());
			taskDetailViewModel.SourceViewModel.Priority = ((!_identity.IsNote) ? _identity.GetPriority() : 0);
			taskDetailViewModel.SourceViewModel.ProjectId = _identity.GetProjectId();
			taskDetailViewModel.SourceViewModel.ProjectName = _identity.GetProjectName();
			taskDetailViewModel.SourceViewModel.Assignee = _identity.GetAssignee();
			TimeData timeData = _identity.GetTimeData();
			taskDetailViewModel.SetTimeData(timeData);
			taskDetailViewModel.SourceViewModel.Kind = (_identity.GetIsNote() ? "NOTE" : "TEXT");
		}
		taskDetailViewModel.IsNewAdd = true;
		TaskDetailPopup taskDetailPopup = new TaskDetailPopup();
		IToastShowWindow toastShowWindow = Utils.FindParent<IToastShowWindow>(this);
		if (toastShowWindow != null)
		{
			if (PopupStateManager.LastTarget == this)
			{
				return;
			}
			taskDetailPopup.DependentWindow = toastShowWindow;
		}
		Grid target = sender as Grid;
		taskDetailPopup.TaskSaved += OnTaskAdded;
		taskDetailPopup.Disappear += OnDetailWindowClosed;
		taskDetailPopup.Show(taskDetailViewModel, string.Empty, new TaskWindowDisplayArgs(target, 25.0, focus: false, _level), default(Point), show: true, canParse: true);
		InOption = true;
		SetMatrixInOperation(inOperation: true);
	}

	private void OnDetailWindowClosed(object sender, string e)
	{
		InOption = false;
		SetMatrixInOperation(inOperation: false);
		if (sender is TaskDetailPopup taskDetailPopup)
		{
			taskDetailPopup.Disappear -= OnDetailWindowClosed;
		}
	}

	private async void OnTaskAdded(object sender, string taskId)
	{
		if (sender is TaskDetailWindow taskDetailWindow)
		{
			taskDetailWindow.TaskSaved -= OnTaskAdded;
		}
		TaskModel taskModel = await TaskDao.GetThinTaskById(taskId);
		if (taskModel == null)
		{
			return;
		}
		TaskBaseViewModel item = TaskCache.SafeGetTaskViewModel(taskModel);
		List<TaskBaseViewModel> taskMatchedNormalFilter = TaskService.GetTaskMatchedNormalFilter(Parser.ToNormalModel(_identity.Quadrant.rule), new List<TaskBaseViewModel> { item }, inAll: true);
		if (taskMatchedNormalFilter != null && taskMatchedNormalFilter.Count > 0)
		{
			LoadAsync();
			return;
		}
		MatrixContainer matrixParent = GetMatrixParent();
		foreach (QuadrantModel item2 in matrixParent?.GetActiveQuadrantsSync() ?? MatrixProfileDao.GetActiveQuadrantsSync())
		{
			if (item2.id != _identity.Quadrant.id)
			{
				List<TaskBaseViewModel> taskMatchedNormalFilter2 = TaskService.GetTaskMatchedNormalFilter(Parser.ToNormalModel(item2.rule), new List<TaskBaseViewModel> { item }, inAll: true);
				if (taskMatchedNormalFilter2 != null && taskMatchedNormalFilter2.Count > 0)
				{
					int.TryParse(item2.id.Substring(8), out var result);
					string toast = string.Format(Utils.GetString("AddedToProject"), matrixParent?.GetQuadrantNameByLevel(result) ?? MatrixManager.GetQuadrantNameByLevel(result));
					matrixParent?.Reload(item2.id);
					matrixParent?.Toast(toast);
					return;
				}
			}
		}
		GetMatrixParent()?.ToastMoveProjectControl(taskModel.projectId, taskModel.title, MoveToastType.Add);
	}

	private MatrixContainer GetMatrixParent()
	{
		return _matrix ?? (_matrix = Utils.FindParent<MatrixContainer>(this));
	}

	internal string GetRule()
	{
		return _identity.Quadrant.rule;
	}

	internal void SetDragOver(bool over)
	{
		HoverBorder.BorderThickness = new Thickness(over ? 1 : 0);
	}

	internal int GetLevel()
	{
		return _level;
	}

	private async void OnOMClick(object sender, MouseButtonEventArgs e)
	{
		if (_omMouseDown && _identity != null)
		{
			_omMouseDown = false;
			List<SortTypeViewModel> smartProjectSortTypeModels = SortOptionHelper.GetSmartProjectSortTypeModels(_identity.IsNote);
			new List<CustomMenuItemViewModel>().AddRange(smartProjectSortTypeModels.Select((SortTypeViewModel sortType) => new CustomMenuItemViewModel((object)sortType.Id, sortType.Title, (DrawingImage)null, "")));
			List<CustomMenuItemViewModel> extraItem = new List<CustomMenuItemViewModel>
			{
				new CustomMenuItemViewModel("edit", Utils.GetString("Edit"), Utils.GetGeometry("Ic.TaskOM.Edit"))
			};
			SortTypeSelector sortTypeSelector = new SortTypeSelector(_identity, smartProjectSortTypeModels, _identity.SortOption, inKanban: false, MorePopup, extraItem);
			sortTypeSelector.ResetSortOrder += OnResetSortOrder;
			sortTypeSelector.SortOptionSelect += OnSortOptionSelect;
			sortTypeSelector.OptionSelect += OnOptionSelect;
			sortTypeSelector.Show();
			SetMatrixInOperation(inOperation: false);
			InOption = true;
		}
	}

	private void OnOptionSelect(object sender, string e)
	{
		if (e == "edit")
		{
			MatrixContainer matrixParent = GetMatrixParent();
			MatrixProfileModel matrixProfileModel = matrixParent?.GetActiveProfileSync();
			QuadrantModel quadrant = matrixParent?.GetQuadrantByLevel(_level);
			EditQuadrantWindow obj = ((matrixProfileModel == null) ? new EditQuadrantWindow(_level) : new EditQuadrantWindow(_level, matrixProfileModel.sid, quadrant));
			obj.Owner = Window.GetWindow(this);
			obj.ShowDialog();
			if (matrixParent != null && matrixParent.IsWidgetMode && !string.Equals(LocalSettings.Settings.MatrixActiveViewId, matrixProfileModel?.sid, StringComparison.Ordinal))
			{
				matrixParent.Reload("quadrant" + _level);
			}
		}
	}

	internal void ClearSelected()
	{
		TaskList.SetSelected(null);
	}

	private void OnSortOptionSelect(object o, SortOption sort)
	{
		MatrixContainer matrixParent = GetMatrixParent();
		if (matrixParent != null)
		{
			matrixParent.SaveQuadrant(_level, null, sort, null);
		}
		else
		{
			LocalSettings.Settings.SaveMatrixQuadrant(_level, null, sort, null);
			DataChangedNotifier.NotifyMatrixQuadrantChanged(_level);
		}
		_identity.SortOption = sort;
	}

	private async void OnResetSortOrder(object o, int e)
	{
		await TaskSortOrderService.DeleteAllSortOrderBySortOptionInListId(_identity.SortOption, _identity.CatId);
		DataChangedNotifier.NotifyMatrixQuadrantChanged(_level);
	}

	public Task LoadAsync(bool restore = true)
	{
		if (!base.Dispatcher.CheckAccess())
		{
			return base.Dispatcher.InvokeAsync(() => LoadOnUiThread(restore)).Task.Unwrap();
		}
		return LoadOnUiThread(restore);
	}

	private Task LoadOnUiThread(bool restore)
	{
		return GetMatrixParent()?.LoadQuadrant(this, restore) ?? LoadFromMatrixAsync(restore);
	}

	public Task LoadTaskAsync(bool restore = true)
	{
		return LoadAsync(restore);
	}

	internal async Task LoadFromMatrixAsync(bool restore = true)
	{
		SetIdentity();
		await TaskList.LoadAsync(forceRefresh: true, loadMore: true, restore, ignoreFocus: false, setSelect: true, scroll: false, refreshData: true, coordinatedMatrixLoad: true);
	}

	private async void OnDrop(object sender, MouseEventArgs e)
	{
		if (!_dragOver)
		{
			return;
		}
		_dragOver = false;
		if (!(TickDragHelper.GetDragData() is DisplayItemModel displayItemModel))
		{
			return;
		}
		QuadrantControl previous = Utils.FindParent<QuadrantControl>(TickDragHelper.DragFrom);
		if (object.Equals(previous, this))
		{
			return;
		}
		List<string> taskIds;
		if (displayItemModel.BatchModels != null && displayItemModel.BatchModels.Any())
		{
			taskIds = displayItemModel.BatchModels.Select((DisplayItemModel c) => c.Id).ToList();
			foreach (DisplayItemModel batchModel in displayItemModel.BatchModels)
			{
				List<DisplayItemModel> childrenModels = batchModel.GetChildrenModels(getAll: false);
				if (childrenModels != null)
				{
					taskIds.AddRange(childrenModels.Select((DisplayItemModel t) => t.Id));
				}
			}
		}
		else
		{
			taskIds = new List<string> { displayItemModel.Id };
			List<DisplayItemModel> childrenModels2 = displayItemModel.GetChildrenModels(getAll: false);
			if (childrenModels2 != null)
			{
				taskIds.AddRange(childrenModels2.Select((DisplayItemModel t) => t.Id));
			}
		}
		QuadrantControl dropQuadrant = this;
		MatrixContainer matrix = Utils.FindParent<MatrixContainer>(this);
		UserActCollectUtils.AddClickEvent("matrix", "matrix_action", "drag_task");
		matrix?.OnQuadrantTaskDragOver(null);
		string previewRule = previous?.GetRule() ?? "";
		string rule = dropQuadrant.GetRule();
		if (!(await TaskService.BatchUpdateTaskQuadrantProperties(taskIds, previewRule, rule, Utils.FindParent<IToastShowWindow>(this))))
		{
			return;
		}
		previous?.RemoveItems(taskIds);
		await TickDragHelper.WaitForDragCompletedAsync();
		if (previous == null)
		{
			if (matrix != null)
			{
				await matrix.LoadTask(restore: false);
			}
			else
			{
				await dropQuadrant.LoadTaskAsync(restore: false);
			}
		}
		else
		{
			await Task.WhenAll(dropQuadrant.LoadTaskAsync(restore: false), previous.LoadTaskAsync(restore: false));
		}
	}

	private void OnDragOver(object sender, MouseEventArgs e)
	{
		if (!base.IsVisible)
		{
			return;
		}
		Window window = Window.GetWindow(this);
		if (window == null || !window.IsMouseCaptured || TaskDetailPopup.IsDragOver(e))
		{
			return;
		}
		Point position = e.GetPosition(this);
		bool flag = !(position.X < 0.0) && !(position.X > base.ActualWidth) && !(position.Y < 0.0) && !(position.Y > base.ActualHeight);
		if (_dragOver != flag)
		{
			if (!flag)
			{
				Utils.FindParent<MatrixContainer>(this)?.OnQuadrantTaskDragOver(null);
			}
			else if (TickDragHelper.GetDragData() is DisplayItemModel && !object.Equals(Utils.FindParent<QuadrantControl>(TickDragHelper.DragFrom), this))
			{
				Utils.FindParent<MatrixContainer>(this)?.OnQuadrantTaskDragOver(this);
			}
			_dragOver = flag;
		}
		if (_dragOver && HoverBorder.BorderThickness != new Thickness(1.0) && !object.Equals(Utils.FindParent<QuadrantControl>(TickDragHelper.DragFrom), this))
		{
			Utils.FindParent<MatrixContainer>(this)?.OnQuadrantTaskDragOver(this);
		}
	}

	private void OnTaskCountChanged(object sender, EventArgs e)
	{
		bool flag = !TaskList.DisplayModels.Any();
		EmptyText.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
		EmptyText.Text = Utils.GetString((Parser.GetFilterRuleVersion(_identity.Quadrant.rule) > 7) ? "FilterVersionUpdate" : "NoTask");
	}

	public void RemoveItems(List<string> taskIds)
	{
		TaskList.RemoveItemByIds(taskIds);
	}

	private void OnMorePopupClosed(object sender, EventArgs e)
	{
		InOption = false;
		SetMatrixInOperation(inOperation: false);
	}

	public void ShowBatchOperationDialog()
	{
		_batchHelper.ShowOperationDialog();
	}

	public void SetSelectedTaskIds(List<string> taskIds)
	{
		_batchHelper.SelectedTaskIds = taskIds;
	}

	public void RemoveSelectedId(string id)
	{
		_batchHelper.SelectedTaskIds?.Remove(id);
		TaskList.RemoveSelectedId(id);
	}

	public List<string> GetSelectedTaskIds()
	{
		return _batchHelper.SelectedTaskIds;
	}

	public void ReloadList()
	{
		LoadTaskAsync();
	}

	public UIElement BatchOperaPlacementTarget()
	{
		return Title;
	}

	public void HideBatchSelectControl()
	{
	}

	public void SetIdentity()
	{
		MatrixContainer matrixParent = GetMatrixParent();
		MatrixProfileModel matrixProfileModel = matrixParent?.GetActiveProfileSync() ?? MatrixProfileDao.GetActiveProfileSync();
		QuadrantModel quadrantModel = matrixParent?.GetQuadrantByLevel(_level) ?? MatrixManager.GetQuadrantByLevel(_level);
		SetTitle(quadrantModel.name);
		_identity = new MatrixQuadrantIdentity(quadrantModel, matrixProfileModel?.sid);
		_batchHelper.ProjectIdentity = _identity;
		TaskList.SetIdentity(_identity, TaskListDisplayType.Matrix);
	}

	private void SetTitle(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			Title.Text = Utils.GetString("MatrixTitleQuadrant" + _level);
			Title.Foreground = ThemeUtil.GetColor("MatrixColorQuadrant" + _level);
			Emoji.Text = string.Empty;
			Icon.Visibility = Visibility.Visible;
			return;
		}
		string emojiIcon = EmojiHelper.GetEmojiIcon(name);
		if (!string.IsNullOrEmpty(emojiIcon) && name.StartsWith(emojiIcon))
		{
			Title.Text = name.Remove(0, emojiIcon.Length);
			Emoji.Text = emojiIcon;
			Icon.Visibility = Visibility.Collapsed;
			Title.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity60");
		}
		else
		{
			Title.Text = name;
			Emoji.Text = string.Empty;
			Icon.Visibility = Visibility.Visible;
			Title.Foreground = ThemeUtil.GetColor("MatrixColorQuadrant" + _level);
		}
	}

	public async Task BatchPinTask()
	{
		List<string> selectedTaskIds = GetSelectedTaskIds();
		List<TaskBaseViewModel> tasksByIds = TaskCache.GetTasksByIds(selectedTaskIds);
		await TaskService.BatchStarTaskOrNote(pin: !(tasksByIds.Count > 0 && tasksByIds.All((TaskBaseViewModel t) => t.IsPinned)), ids: selectedTaskIds, project: _identity.CatId);
	}

	public void BatchOpenSticky()
	{
		TaskStickyWindow.ShowTaskSticky(GetSelectedTaskIds());
	}

	public async Task BatchSetDate(DateTime? date)
	{
		if (await TaskService.BatchSetDate(GetSelectedTaskIds(), date))
		{
			LoadAsync();
		}
	}

	public async Task BatchSetPriority(int priority)
	{
		if (await TaskService.BatchSetPriority(GetSelectedTaskIds(), priority))
		{
			LoadAsync();
		}
	}

	public void BatchDeleteTask()
	{
		List<string> selectedTaskIds = _batchHelper.SelectedTaskIds;
		if (selectedTaskIds != null && selectedTaskIds.Count > 1)
		{
			TaskService.BatchDeleteTaskByIds(_batchHelper.SelectedTaskIds);
		}
	}

	private void OnHeadMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (!AddButton.IsMouseOver && !MoreButton.IsMouseOver)
		{
			_previewDrag = true;
			BackBorder.BorderThickness = new Thickness(0.0);
			BackBorder.Effect = null;
		}
	}

	public void OnHeadMouseUp()
	{
		DataChangedNotifier_InterfaceStyleChanged(null, null);
	}

	private void OnHeadMouseMove(object sender, MouseEventArgs e)
	{
		if (!_isLocked && _previewDrag && e.LeftButton == MouseButtonState.Pressed)
		{
			Utils.FindParent<MatrixContainer>(this)?.StartDragQuadrant(this, e);
		}
		_previewDrag = false;
	}

	public void SetLocked(bool isLocked)
	{
		_isLocked = isLocked;
		AddButton.Visibility = (isLocked ? Visibility.Collapsed : Visibility.Visible);
		TaskList.IsLocked = isLocked;
		TaskList.LoadAsync(forceRefresh: true);
	}

	public void SetBackBorderOpacity(double opacity)
	{
		BackBorder.Opacity = Math.Max(Math.Min(opacity, 1.0), 0.2);
	}

	public void SetWidgetBackColor(double opacity)
	{
		BackBorder.SetResourceReference(Border.BackgroundProperty, "WidgetCardColor");
		BackBorder.Opacity = Math.Max(Math.Min(opacity, 1.0), 0.2);
	}

	public void SetMatrixInOperation(bool inOperation)
	{
		Utils.FindParent<MatrixContainer>(this)?.SetInOperation(inOperation);
	}

	private void OnOmMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (!MorePopup.IsOpen)
		{
			_omMouseDown = true;
		}
	}

	public void ToggleAllSection(bool? open)
	{
		TaskList.ExpandOrFoldAllSection(open);
	}

	public void ExpandOrFoldAllTask(bool? open)
	{
		TaskList.ExpandOrFoldAllTask(open);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/eisenhower/quadrantcontrol.xaml", UriKind.Relative);
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
			Root = (QuadrantControl)target;
			break;
		case 2:
			BackBorder = (Border)target;
			break;
		case 3:
			HoverBorder = (Border)target;
			break;
		case 4:
			((Grid)target).MouseLeftButtonDown += OnHeadMouseDown;
			((Grid)target).MouseMove += OnHeadMouseMove;
			break;
		case 5:
			OptionColumn = (ColumnDefinition)target;
			break;
		case 6:
			IconGrid = (Grid)target;
			break;
		case 7:
			Icon = (Path)target;
			break;
		case 8:
			Emoji = (EmjTextBlock)target;
			break;
		case 9:
			Title = (EmjTextBlock)target;
			break;
		case 10:
			AddButton = (HoverIconButton)target;
			break;
		case 11:
			MoreButton = (HoverIconButton)target;
			break;
		case 12:
			MorePopup = (EscPopup)target;
			break;
		case 13:
			TaskListContent = (ContentControl)target;
			break;
		case 14:
			EmptyText = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
