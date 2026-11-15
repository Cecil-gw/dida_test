using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Framework;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.DragAndDrop;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.Util.Sync.ClosedLoader;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.MainListView;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Tag;
using ticktick_WPF.Views.TaskList;

namespace ticktick_WPF.Views.Timeline;

public class TimelineContainer : UserControl, IBatchTaskSelector, IComponentConnector
{
	private readonly TimelineViewModel _model = new TimelineViewModel();

	private ProjectIdentity _projectIdentity;

	private bool _loaded;

	private bool _dayWidthChanging;

	private string _copiedTaskSelectId;

	private readonly DispatcherTimer _moveViewTimer = new DispatcherTimer(DispatcherPriority.Render);

	private static readonly Dictionary<string, DateTime> _projectDays = new Dictionary<string, DateTime>();

	private bool _projectScrollChanged;

	private bool _addTaskMouseDown;

	private bool _moved;

	private Point _prePoint;

	private bool _ignoreMouseUp;

	private CancellationTokenSource _ignoreMouseUpTaskToken;

	private int _line;

	private bool _batchMouseDown;

	private Point _batchStartPoint;

	private TimelineCellViewModel _currentModel;

	private double _offset = -1.0;

	private FrameworkElement _hoveredCell;

	private double _initWidth;

	private TimelineCellViewModel _dragCell;

	private DragAutoScrollHelper _autoScroller;

	private bool _dragOver;

	internal TimelineContainer Root;

	internal TimelineNavBar TimelineNavBar;

	internal ScrollViewer YearScroll;

	internal StackPanel OptionPanel;

	internal Border SwitchRangeBorder;

	internal EscPopup SelectRangePopup;

	internal StackPanel SelectRangeStackPanel;

	internal ScrollViewer DayLineScroll;

	internal TimelineHeader Header;

	internal TimelineHeaderHover HoverHeader;

	internal Grid PrePageGrid;

	internal Grid NextPageGrid;

	internal ScrollViewer MainScroll;

	internal Grid MainGrid;

	internal TimelineBackground TimelineBackground;

	internal TimelineVirtualizedCanvas CellCanvas;

	internal Border BatchSelectBorder;

	internal TimelineCellFloating TimelineFloating;

	internal TimelineArrange ArrangePanel;

	internal Grid GroupBorder;

	internal ScrollViewer ColumnScroll;

	internal TimelineToolTip ToolTipControl;

	internal Popup ColumnDragPopup;

	internal Grid ColumnDropLine;

	internal BatchTaskControl BatchControl;

	private bool _contentLoaded;

	public TimelineContainer()
	{
		InitializeComponent();
		_model.MoveSpan(DateTime.Today.AddDaysSafely(-1.0), DateTime.Today.AddDaysSafely(1.0));
		base.DataContext = _model;
		base.Loaded += OnContainerLoaded;
		base.Unloaded += OnContainerUnloaded;
		_moveViewTimer.Interval = TimeSpan.FromMilliseconds(20.0);
		_moveViewTimer.Tick += OnMoveViewTimerOnElapsed;
	}

	private void OnThemeChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == "ThemeId" && _model != null)
		{
			_model.IsDark = ThemeKey.IsDarkTheme(LocalSettings.Settings.ThemeId);
		}
	}

	private void OnContainerUnloaded(object sender, RoutedEventArgs e)
	{
		SaveProjectPos(force: true);
		DataChangedNotifier.ProjectChanged -= OnProjectChanged;
		DataChangedNotifier.ProjectColumnChanged -= OnColumnChanged;
		DataChangedNotifier.FilterChanged -= OnFilterChanged;
		DataChangedNotifier.TagChanged -= OnTagChanged;
		DataChangedNotifier.ProjectGroupChanged -= OnProjectGroupChanged;
		TasksChangeEventManager.RemoveHandler(TaskChangeNotifier.Notifier, TasksChanged);
		TickDragHelper.DragDrop -= OnDragDrop;
		TickDragHelper.DragMoving -= OnDragOver;
		PropertyChangedEventManager.RemoveHandler(LocalSettings.Settings, OnDisplayChanged, "HideComplete");
		PropertyChangedEventManager.RemoveHandler(LocalSettings.Settings, OnDisplayChanged, "ShowSubtasks");
		PropertyChangedEventManager.RemoveHandler(LocalSettings.Settings, OnThemeChanged, "ThemeId");
		_model.BatchSelectedChanged -= OnBatchSelectedChanged;
		_model.ClearCell();
		GlobalEventManager.TimelineSetChanged -= OnSettingsChanged;
	}

	private void OnDisplayChanged(object sender, PropertyChangedEventArgs e)
	{
		_model?.OnLocalSettingsChanged(e.PropertyName);
	}

	private async void OnContainerLoaded(object sender, RoutedEventArgs e)
	{
		TasksChangeEventManager.AddHandler(TaskChangeNotifier.Notifier, TasksChanged);
		DataChangedNotifier.ProjectChanged += OnProjectChanged;
		DataChangedNotifier.ProjectColumnChanged += OnColumnChanged;
		DataChangedNotifier.TagChanged += OnTagChanged;
		DataChangedNotifier.FilterChanged += OnFilterChanged;
		DataChangedNotifier.ProjectGroupChanged += OnProjectGroupChanged;
		TickDragHelper.DragDrop += OnDragDrop;
		TickDragHelper.DragMoving += OnDragOver;
		PropertyChangedEventManager.AddHandler(LocalSettings.Settings, OnDisplayChanged, "HideComplete");
		PropertyChangedEventManager.AddHandler(LocalSettings.Settings, OnDisplayChanged, "ShowSubtasks");
		PropertyChangedEventManager.AddHandler(LocalSettings.Settings, OnThemeChanged, "ThemeId");
		GlobalEventManager.TimelineSetChanged += OnSettingsChanged;
		_model.BatchSelectedChanged += OnBatchSelectedChanged;
		if (_model.ProjectIdentity != null && _projectDays.TryGetValue(_model.ProjectIdentity.CatId, out var value))
		{
			GotoDay(value, smooth: false, resetRange: true);
		}
		_model.IsDark = ThemeKey.IsDarkTheme(LocalSettings.Settings.ThemeId);
	}

	private void OnSettingsChanged(object sender, string e)
	{
		_model.OnSettingChanged(e);
	}

	private async void TasksChanged(object sender, TasksChangeEventArgs e)
	{
		if (base.IsVisible)
		{
			_model?.OnTasksChanged(e);
		}
	}

	private async void OnProjectGroupChanged(object sender, ProjectGroupModel e)
	{
		ProjectIdentity projectIdentity = _model.ProjectIdentity;
		if (!(projectIdentity is GroupProjectIdentity group) || !(group.CatId == e.id))
		{
			return;
		}
		ProjectGroupModel groupModel = CacheManager.GetGroupById(group.GroupId);
		if (groupModel != null && groupModel.viewMode == "list")
		{
			await Task.Delay(500);
			if (base.IsVisible)
			{
				group.SetGroup(groupModel);
				Utils.FindParent<ListViewContainer>(this)?.SelectProject(group);
			}
		}
		else if (groupModel?.Timeline != null)
		{
			if (!_model.TimelineSortOption.Equal(groupModel.Timeline.sortOption))
			{
				_model.TimelineSortOption = groupModel.Timeline.sortOption;
			}
			if (groupModel.Timeline.Range != _model.TimelineRange)
			{
				DateTime day = GetDay();
				_model.TimelineRange = groupModel.Timeline?.Range;
				await GotoDay(day);
			}
			if (groupModel.name != _model.ProjectTitle)
			{
				_model.SetProjectTitle(groupModel.name);
			}
		}
	}

	private async void OnTagChanged(object sender, TagModel e)
	{
		if (!(_model.ProjectIdentity is TagProjectIdentity tagProjectIdentity))
		{
			return;
		}
		TagModel tag = CacheManager.GetTagByName(tagProjectIdentity.Id);
		if (tag == null)
		{
			return;
		}
		if (tag.viewMode == "list")
		{
			await Task.Delay(500);
			if (base.IsVisible)
			{
				Utils.FindParent<ListViewContainer>(this)?.SelectProject(new TagProjectIdentity(tag));
			}
		}
		else if (tag.Timeline != null)
		{
			if (tag.Timeline.sortOption != null && !_model.TimelineSortOption.Equal(tag.Timeline.sortOption))
			{
				_model.TimelineSortOption = tag.Timeline.sortOption;
			}
			if (tag.Timeline.Range != _model.TimelineRange)
			{
				DateTime day = GetDay();
				_model.TimelineRange = tag.Timeline?.Range;
				await GotoDay(day);
			}
			if (tag.GetDisplayName() != _model.ProjectTitle)
			{
				_model.SetProjectTitle(tag.GetDisplayName());
			}
		}
	}

	private async void OnFilterChanged(object sender, FilterChangeArgs e)
	{
		if (e.RuleChanged && !string.IsNullOrEmpty(e.Filter.id))
		{
			_projectDays.Remove(e.Filter.id);
		}
		if (!string.IsNullOrEmpty(e.deleteId))
		{
			_projectDays.Remove(e.deleteId);
		}
		if (!(_model.ProjectIdentity?.Id == e.Filter?.id))
		{
			return;
		}
		FilterModel filter = CacheManager.GetFilterById(e.Filter?.id);
		if (filter != null && filter.viewMode == "list" && UserDao.IsPro())
		{
			await Task.Delay(500);
			if (base.IsVisible)
			{
				Utils.FindParent<ListViewContainer>(this)?.SelectProject(new FilterProjectIdentity(filter));
			}
		}
		else if (e.RuleChanged)
		{
			if (filter != null)
			{
				LoadProjectInternal(new FilterProjectIdentity(filter));
			}
		}
		else if (filter?.Timeline != null)
		{
			if (filter.Timeline.sortOption != null && !_model.TimelineSortOption.Equal(filter.Timeline.sortOption))
			{
				_model.TimelineSortOption = filter.Timeline.sortOption;
			}
			if (filter.Timeline.Range != _model.TimelineRange)
			{
				DateTime day = GetDay();
				_model.TimelineRange = filter.Timeline?.Range;
				await GotoDay(day);
			}
			if (filter.name != _model.ProjectTitle)
			{
				_model.SetProjectTitle(filter.name);
			}
		}
	}

	public async void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
	{
		e.Handled = true;
		if (Utils.IfCtrlPressed())
		{
			if (!_dayWidthChanging)
			{
				int addIndex = ((e.Delta < 0 && _model.TimelineRangeIndex < 9) ? 1 : ((e.Delta > 0 && _model.TimelineRangeIndex > 1) ? (-1) : 0));
				ScrollChangeDayWidth(addIndex);
			}
		}
		else if (Utils.IfShiftPressed())
		{
			MainScroll.ScrollToHorizontalOffset(MainScroll.HorizontalOffset - (double)e.Delta);
		}
		else
		{
			MainScroll.ScrollToVerticalOffset(MainScroll.VerticalOffset - (double)e.Delta);
		}
	}

	private async Task ScrollChangeDayWidth(int addIndex)
	{
		_dayWidthChanging = true;
		if (addIndex != 0)
		{
			try
			{
				if (MainScroll.ActualWidth <= 0.0)
				{
					return;
				}
				TimelineInlineMarginConverter.IgnoreLeftOutSide = true;
				Point hoverPoint = Mouse.GetPosition(MainScroll);
				double num = MainScroll.HorizontalOffset + hoverPoint.X;
				DateTime day = _model.StartDate.AddDaysSafely((int)(num / _model.OneDayWidth));
				await _model.SetRangIndex(_model.TimelineRangeIndex + addIndex);
				await GotoDay(day, smooth: false, resetRange: false, hoverPoint);
				await _model.UpdateCellLineAsync();
				TryCloseToolTips(force: true);
				await Task.Delay(100);
				TimelineInlineMarginConverter.IgnoreLeftOutSide = false;
			}
			catch (Exception ex)
			{
				UtilLog.Warn("TimelineContainer.ChangDayWidthError " + ex.Message);
			}
		}
		await Task.Delay(150);
		_dayWidthChanging = false;
	}

	private void OnBatchSelectedChanged(object sender, EventArgs e)
	{
		ShowBatchControl(_model.BatchSelect ? _model.GetSelectedTaskIds() : null);
	}

	private void ShowBatchControl(List<string> selectedIds)
	{
		if (selectedIds != null && selectedIds.Count > 0)
		{
			BatchControl.SetTaskIds(selectedIds, _model.ProjectIdentity.IsNote, useInList: true, _model.ProjectIdentity);
			BatchControl.Visibility = Visibility.Visible;
			TaskView taskView = Utils.FindParent<TaskView>(this);
			if (taskView != null && taskView.ShowInParallel)
			{
				DataChangedNotifier.NotifyCalOrListBatchSelected(calBatchSelected: false);
			}
		}
		else
		{
			BatchControl.Visibility = Visibility.Collapsed;
		}
	}

	private void OnMouseMove(object sender, MouseEventArgs e)
	{
		if (_batchMouseDown)
		{
			ShowBatchSelectBox(e);
		}
		else if (e.LeftButton != MouseButtonState.Pressed)
		{
			_addTaskMouseDown = false;
			StopMoveView();
		}
		else
		{
			TryMoveCanvas(e);
		}
	}

	private void TryMoveCanvas(MouseEventArgs e)
	{
		if (ToolTipControl.IsVisible)
		{
			Point position = e.GetPosition(this);
			MoveToolTips(position);
		}
		else
		{
			if (!_addTaskMouseDown)
			{
				return;
			}
			Point position2 = e.GetPosition(this);
			double num = _prePoint.X - position2.X;
			double num2 = _prePoint.Y - position2.Y;
			if (!(Math.Abs(num2) < 2.0) || !(Math.Abs(num) < 2.0))
			{
				MainScroll.ScrollToHorizontalOffset(MainScroll.HorizontalOffset + num);
				if (num == 0.0 || Math.Abs(num2 / num) > 0.6)
				{
					MainScroll.ScrollToVerticalOffset(MainScroll.VerticalOffset + num2);
				}
				_prePoint = position2;
				_moved = true;
			}
		}
	}

	public void DelayEndIgnoreMouseUp()
	{
		if (_ignoreMouseUpTaskToken != null)
		{
			CancellationTokenSource ignoreMouseUpTaskToken = _ignoreMouseUpTaskToken;
			_ignoreMouseUpTaskToken = null;
			ignoreMouseUpTaskToken.Cancel();
			ignoreMouseUpTaskToken.Dispose();
		}
		_ignoreMouseUpTaskToken = new CancellationTokenSource();
		Task.Delay(500, _ignoreMouseUpTaskToken.Token).ContinueWith(delegate(Task t)
		{
			if (t.Status == TaskStatus.RanToCompletion)
			{
				_ignoreMouseUp = false;
			}
		}, _ignoreMouseUpTaskToken.Token);
	}

	public void StartIgnoreMouseUp()
	{
		_ignoreMouseUpTaskToken?.Cancel();
		_ignoreMouseUp = true;
	}

	private void OnMouseLeftUp(object sender, MouseButtonEventArgs e)
	{
		Mouse.Capture(null);
		if (_batchMouseDown)
		{
			OnBatchMouseUp();
		}
		else
		{
			OnMoveMouseUp(sender, e);
		}
	}

	private async void OnMoveMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (!_ignoreMouseUp && !_moved && ClearCopiedTaskSelection())
		{
			Clear();
			return;
		}
		if (!_ignoreMouseUp && !_moved && _model.ClearBatchSelect())
		{
			Clear();
			return;
		}
		if (!PopupStateManager.CanShowAddPopup() || _model.GroupEditing)
		{
			Clear();
			return;
		}
		if (sender is Grid && !_moved && _addTaskMouseDown && !_ignoreMouseUp)
		{
			Point position = e.GetPosition(MainScroll);
			int dayOffset = GetDayOffsetByPoint(position);
			DateTime dateTime = _model.StartDate.AddDaysSafely(dayOffset);
			DateTime? dueDate = null;
			if (_model.NewTaskDefaultDays > 1)
			{
				dueDate = dateTime.AddDaysSafely(_model.NewTaskDefaultDays);
			}
			int line = GetLineByPoint(position);
			int num = line;
			if (_model.GroupDictModels.Any())
			{
				num = Math.Min(num, _model.GroupDictModels.Keys.Max());
			}
			if (_model.GroupDictModels.TryGetValue(num, out var value) && !value)
			{
				Clear();
				return;
			}
			string projectId = _model.ProjectIdentity?.GetProjectId();
			TaskDefaultModel defaultSafely = TaskDefaultDao.GetDefaultSafely();
			if (!(_model.ProjectIdentity?.CanAddTask() ?? false))
			{
				Clear();
				return;
			}
			if (string.IsNullOrEmpty(projectId))
			{
				projectId = defaultSafely.ProjectId;
			}
			_line = line;
			TaskBaseViewModel newTask = new TaskBaseViewModel
			{
				Id = Utils.GetGuid(),
				Kind = "TEXT",
				Title = string.Empty,
				Priority = defaultSafely.Priority,
				StartDate = dateTime,
				DueDate = dueDate,
				IsAllDay = true
			};
			if (_model.ProjectIdentity is FilterProjectIdentity filterProjectIdentity)
			{
				newTask.Priority = filterProjectIdentity.GetPriority();
				newTask.SetTags(filterProjectIdentity.GetTags());
			}
			if (_model.ProjectIdentity is TagProjectIdentity tagProjectIdentity)
			{
				newTask.SetTags(tagProjectIdentity.GetTags());
			}
			else
			{
				newTask.SetTags(defaultSafely.Tags);
			}
			switch (_model.TimelineSortOption.groupBy)
			{
			case "tag":
			{
				TimelineGroupViewModel timelineGroupViewModel2 = _model.GroupModels.LastOrDefault((TimelineGroupViewModel c) => c.Line <= _line);
				if (timelineGroupViewModel2 != null && !string.IsNullOrEmpty(timelineGroupViewModel2.Id) && timelineGroupViewModel2.Id != "!tag")
				{
					newTask.SetTags(new List<string> { timelineGroupViewModel2.Title });
				}
				else
				{
					newTask.SetTags(new List<string>());
				}
				break;
			}
			case "priority":
			{
				TimelineGroupViewModel timelineGroupViewModel4 = _model.GroupModels.LastOrDefault((TimelineGroupViewModel c) => c.Line <= _line);
				int result;
				if (timelineGroupViewModel4?.Id == "note")
				{
					newTask.Kind = "NOTE";
					newTask.Priority = 0;
				}
				else if (int.TryParse(timelineGroupViewModel4?.Id ?? "", out result))
				{
					newTask.Priority = result;
				}
				break;
			}
			case "assignee":
			{
				TimelineGroupViewModel timelineGroupViewModel5 = _model.GroupModels.LastOrDefault((TimelineGroupViewModel c) => c.Line <= _line);
				if (timelineGroupViewModel5 == null)
				{
					break;
				}
				newTask.Assignee = timelineGroupViewModel5.Id;
				if (!string.IsNullOrEmpty(newTask.Assignee) && newTask.Assignee != "-1" && _model.ProjectIdentity is GroupProjectIdentity groupProjectIdentity)
				{
					string text = ProjectDao.GetAssignProjectInGroup(groupProjectIdentity.GroupId, newTask.Assignee)?.id;
					if (text != null)
					{
						projectId = text;
					}
				}
				break;
			}
			case "sortOrder":
			{
				TimelineGroupViewModel timelineGroupViewModel3 = _model.GroupModels.LastOrDefault((TimelineGroupViewModel c) => c.Line <= line);
				if (timelineGroupViewModel3 != null)
				{
					newTask.ColumnId = timelineGroupViewModel3.Id;
				}
				break;
			}
			case "project":
			{
				TimelineGroupViewModel timelineGroupViewModel = _model.GroupModels.LastOrDefault((TimelineGroupViewModel c) => c.Line <= line);
				if (timelineGroupViewModel != null)
				{
					projectId = timelineGroupViewModel.Id;
				}
				break;
			}
			}
			if (string.IsNullOrEmpty(newTask.ColumnId) && _model.ProjectIdentity is NormalProjectIdentity)
			{
				newTask.ColumnId = (await ColumnDao.GetColumnsByProjectId(projectId))?.OrderBy((ColumnModel column) => column.sortOrder).FirstOrDefault()?.id;
			}
			ProjectModel projectById = CacheManager.GetProjectById(projectId);
			if (projectById == null || !projectById.IsEnable())
			{
				Clear();
				TryToastString(Utils.GetString("NoEditingPermission"));
				return;
			}
			newTask.ProjectId = projectById.id;
			newTask.Color = projectById.color;
			newTask.SortOrder = ProjectSortOrderDao.GetNewTaskSortOrderInProject(projectId, null, false);
			if (projectById.IsNote || (_model.ProjectIdentity is FilterProjectIdentity filterProjectIdentity2 && filterProjectIdentity2.FilterNote()))
			{
				newTask.Kind = "NOTE";
			}
			if (newTask.IsNote)
			{
				newTask.DueDate = null;
				newTask.Priority = 0;
			}
			TimelineCellViewModel model = new TimelineCellViewModel(_model, newTask)
			{
				Line = line,
				Left = (double)dayOffset * _model.OneDayWidth,
				Operation = TimelineCellOperation.None,
				IsNew = true
			};
			_model.AddCell(model, checkAva: true);
		}
		Clear();
		void Clear()
		{
			if (_ignoreMouseUp)
			{
				_ignoreMouseUp = false;
			}
			if (_moved)
			{
				UserActCollectUtils.AddClickEvent("timeline", "view_action", "drag_to_view");
			}
			_moved = false;
			_addTaskMouseDown = false;
			if (_model.GroupEditing)
			{
				_model.SetGroupEditing(value: false);
			}
		}
	}

	private void OnMouseLeftDown(object sender, MouseButtonEventArgs e)
	{
		if (sender is UIElement element)
		{
			Mouse.Capture(element);
		}
		if (!Utils.IfCtrlPressed())
		{
			if (PopupStateManager.CanShowAddPopup())
			{
				_addTaskMouseDown = true;
				_prePoint = e.GetPosition(this);
			}
		}
		else if (sender.Equals(MainGrid))
		{
			MainGrid.CaptureMouse();
			_batchMouseDown = true;
			StartMoveView();
			_batchStartPoint = e.GetPosition(MainGrid);
			_prePoint = _batchStartPoint;
		}
		TimelineFloating.Visibility = Visibility.Collapsed;
	}

	private async void OnMoveViewTimerOnElapsed(object s, EventArgs eventArgs)
	{
		ScrollViewer mainScroll = MainScroll;
		Point position = Mouse.GetPosition(mainScroll);
		if (position.Y > mainScroll.ActualHeight)
		{
			mainScroll.ScrollToVerticalOffset(mainScroll.VerticalOffset + (position.Y - mainScroll.ActualHeight));
		}
		else if (position.Y < 0.0)
		{
			mainScroll.ScrollToVerticalOffset(mainScroll.VerticalOffset + position.Y);
		}
		double moveWidth = 0.0;
		double num = 30.0;
		if (_model.ShowGroup && GroupBorder != null)
		{
			num = Math.Max(GroupBorder.ActualWidth, num);
		}
		if (position.X < num)
		{
			moveWidth = Math.Min(100.0, (position.X - num) / 10.0 * 2.0);
		}
		else if (position.X > mainScroll.ActualWidth - 30.0)
		{
			moveWidth = Math.Min(100.0, (position.X - mainScroll.ActualWidth + 30.0) / 10.0 * 2.0);
		}
		if (moveWidth != 0.0)
		{
			if (TimelineFloating.Visibility == Visibility.Visible)
			{
				await TimelineFloating.OnDragMoveOutSide();
			}
			mainScroll.ScrollToHorizontalOffset(mainScroll.HorizontalOffset + moveWidth);
		}
	}

	public void StartMoveView()
	{
		_moveViewTimer.Start();
	}

	public void StopMoveView()
	{
		_moveViewTimer.Stop();
	}

	private void ShowBatchSelectBox(MouseEventArgs e)
	{
		Point position = e.GetPosition(MainGrid);
		position.X = Math.Min(Math.Max(0.0, position.X), MainGrid.ActualWidth);
		position.Y = Math.Min(Math.Max(0.0, position.Y), MainGrid.ActualHeight);
		double value = _prePoint.X - position.X;
		if (!(Math.Abs(_prePoint.Y - position.Y) < 2.0) || !(Math.Abs(value) < 2.0))
		{
			_prePoint = position;
			if (BatchSelectBorder.Visibility == Visibility.Collapsed)
			{
				BatchSelectBorder.Visibility = Visibility.Visible;
				BatchSelectBorder.Margin = new Thickness(_batchStartPoint.X, _batchStartPoint.Y, 0.0, 0.0);
			}
			double num = position.X - _batchStartPoint.X;
			double num2 = position.Y - _batchStartPoint.Y;
			if (num < 0.0 || num2 < 0.0)
			{
				BatchSelectBorder.Margin = new Thickness((num < 0.0) ? Math.Max(0.0, _batchStartPoint.X + num) : _batchStartPoint.X, (num2 < 0.0) ? Math.Max(0.0, _batchStartPoint.Y + num2) : _batchStartPoint.Y, 0.0, 0.0);
			}
			BatchSelectBorder.Width = Math.Abs(num);
			BatchSelectBorder.Height = Math.Abs(num2);
			if (BatchSelectBorder.Width > 0.0)
			{
				_model.TryBatchSelectOnMove(new Rect(BatchSelectBorder.Margin.Left, BatchSelectBorder.Margin.Top, BatchSelectBorder.Width, BatchSelectBorder.Height));
			}
		}
	}

	private void OnBatchMouseUp()
	{
		_batchMouseDown = false;
		StopMoveView();
		BatchSelectBorder.Visibility = Visibility.Collapsed;
		_model.ClearTempBatchSelect();
		BatchSelectBorder.Width = 0.0;
		BatchSelectBorder.Height = 0.0;
		BatchSelectBorder.Margin = new Thickness(0.0);
	}

	public async void ShowMenuPopup(TimelineCellViewModel model)
	{
		ClearCopiedTaskSelection(model.Id);
		if (!model.BatchSelected)
		{
			_model.ClearBatchSelect();
		}
		else if (_model.BatchSelect)
		{
			ShowBatchOperationDialog();
			StartIgnoreMouseUp();
			return;
		}
		if (model.DisplayModel.IsEvent)
		{
			CalendarOperationDialog calendarOperationDialog = new CalendarOperationDialog(new EventArchiveArgs(model.DisplayModel));
			calendarOperationDialog.Operated += delegate
			{
				_model.RemoveCell(model, setPos: true);
			};
			calendarOperationDialog.SetPlaceTarget(this);
			calendarOperationDialog.Show();
			StartIgnoreMouseUp();
		}
		else
		{
			if (!model.Editable)
			{
				return;
			}
			_currentModel = model;
			OperationExtra operationExtra = await TaskOperationHelper.GetTaskAccessInfo(model.Id, canShowAddSubtask: false);
			if (operationExtra != null)
			{
				TaskOperationDialog taskOperationDialog = new TaskOperationDialog(operationExtra, TimelineNavBar.FoldGrid);
				taskOperationDialog.TimeClear += OnOperationTimeClear;
				taskOperationDialog.AbandonOrReopen += OnOperationAbandonOrReopen;
				taskOperationDialog.PrioritySelect += OnOperationPrioritySelect;
				taskOperationDialog.ProjectSelect += OnOperationProjectSelect;
				taskOperationDialog.QuickDateSelect += OnOperationQuickDateSelect;
				taskOperationDialog.TimeSelect += OnOperationTimeSelect;
				taskOperationDialog.Deleted += OnOperationDeleted;
				taskOperationDialog.AssigneeSelect += OnOperationAssigneeSelect;
				taskOperationDialog.TagsSelect += OnOperationTagsSelect;
				taskOperationDialog.Copied += OnOperationCopied;
				taskOperationDialog.LinkCopied += OnOperationLinkCopied;
				taskOperationDialog.SwitchTaskOrNote += OnOperationSwitchTaskOrNote;
				taskOperationDialog.Disappear += OnOperationDisappear;
				taskOperationDialog.SkipCurrentRecurrence += OnOperationSkipCurrentRecurrence;
				taskOperationDialog.Toast += OnOperationToast;
				taskOperationDialog.CompleteDateChanged += async delegate(object o, DateTime date)
				{
					await TaskService.ChangeCompleteDate(model.Id, date);
					SyncManager.TryDelaySync();
				};
				taskOperationDialog.Show();
				StartIgnoreMouseUp();
				TaskDetailPopup.TryCloseWindow(force: true);
			}
		}
	}

	private async void ShowBatchOperationDialog()
	{
		List<string> selectedIds = _model.GetSelectedTaskIds();
		if (selectedIds.Count == 0)
		{
			return;
		}
		List<TaskModel> tasks = null;
		List<TaskDetailItemModel> checkItems = null;
		tasks = await TaskDao.GetTaskAndChildrenInBatch(TaskDao.GetTreeTopIds(selectedIds, string.Empty));
		checkItems = await TaskDetailItemDao.GetCheckItemsInTaskIds(selectedIds);
		BatchTaskEditHelper batchHelper = new BatchTaskEditHelper(null)
		{
			ProjectIdentity = _projectIdentity,
			SelectedTaskIds = selectedIds,
			UseInList = false
		};
		batchHelper.ShowOrHideOperation += async delegate(object sender, bool b)
		{
			if (!b)
			{
				DelayEndIgnoreMouseUp();
				batchHelper.Dispose();
			}
		};
		batchHelper.CanUndo += async delegate(object sender, bool e)
		{
			await Task.Delay(200);
			bool flag = false;
			if (_projectIdentity is FilterProjectIdentity)
			{
				List<TaskBaseViewModel> matchedTasks = TaskViewModelHelper.GetMatchedTasks(_projectIdentity, selectedIds);
				List<TaskModel> list = tasks;
				if (list != null && list.Count > 0 && matchedTasks.Count < selectedIds.Count)
				{
					flag = true;
				}
			}
			if (flag || e)
			{
				UndoToast undoToast = new UndoToast(new TaskUndo(null, string.Empty, Utils.GetString("TaskHasBeenFiltered"), tasks, checkItems));
				undoToast.SetVisible(flag);
				Utils.FindParent<IToastShowWindow>(this).Toast(undoToast);
			}
		};
		batchHelper.ShowOperationDialog();
	}

	private void OnOperationToast(object sender, string e)
	{
		Utils.Toast(e);
	}

	private async void OnOperationSkipCurrentRecurrence(object sender, EventArgs e)
	{
		await UpdateTaskWithUndo(Skip(), _currentModel.Id, _currentModel.DisplayModel.Kind == "CHECKLIST");
		_model.UpdateCellTime(_currentModel);
		await _model.UpdateCellLineAsync();
		async Task Skip()
		{
			await Task.Yield();
			await TaskService.SkipCurrentRecurrence(_currentModel.Id, _currentModel.DisplayModel.Kind == "CHECKLIST");
		}
	}

	private async void OnOperationSwitchTaskOrNote(object sender, EventArgs e)
	{
		await UpdateTaskWithUndo(Skip(), _currentModel.Id, _currentModel.DisplayModel.Kind == "CHECKLIST");
		_model.UpdateCellTime(_currentModel);
		await _model.UpdateCellLineAsync();
		async Task Skip()
		{
			await Task.Delay((_currentModel.DisplayModel.Kind == "CHECKLIST") ? 100 : 30);
			await TaskService.SwitchTaskOrNote(_currentModel.Id);
		}
	}

	private async void OnOperationLinkCopied(object sender, EventArgs e)
	{
		TaskModel taskModel = await TaskDao.GetThinTaskById(_currentModel.Id);
		if (taskModel != null)
		{
			TaskUtils.CopyTaskLink(taskModel.id, taskModel.projectId, taskModel.title);
		}
	}

	private async void OnOperationCopied(object sender, EventArgs e)
	{
		TaskModel taskModel = await TaskService.CopyTask(_currentModel.Id, this);
		if (taskModel != null)
		{
			SelectCopiedTask(taskModel.id);
		}
	}

	private async void SelectCopiedTask(string taskId)
	{
		if (string.IsNullOrEmpty(taskId))
		{
			return;
		}
		ClearCopiedTaskSelection();
		_copiedTaskSelectId = taskId;
		_model.ClearBatchSelect();
		for (int i = 0; i < 8; i++)
		{
			await Task.Delay(150);
			if (_copiedTaskSelectId != taskId || TrySetCopiedTaskSelected(taskId))
			{
				return;
			}
		}
		if (_copiedTaskSelectId == taskId)
		{
			_copiedTaskSelectId = null;
		}
	}

	private bool TrySetCopiedTaskSelected(string taskId)
	{
		TimelineCellViewModel cellModel = _model.GetCellModel(taskId);
		if (cellModel == null)
		{
			return false;
		}
		if (cellModel.Top > MainScroll.ActualHeight + MainScroll.VerticalOffset)
		{
			MainScroll.ScrollToVerticalOffset(cellModel.Top - MainScroll.ActualHeight + 150.0);
		}
		else if (cellModel.Top < MainScroll.VerticalOffset)
		{
			MainScroll.ScrollToVerticalOffset(Math.Max(0.0, cellModel.Top - 24.0));
		}
		ClearCopiedTaskEditSelection(taskId);
		cellModel.Operation = cellModel.Operation.Add(TimelineCellOperation.Edit);
		return true;
	}

	public bool ClearCopiedTaskSelection(string clickedTaskId = null)
	{
		if (string.IsNullOrEmpty(_copiedTaskSelectId))
		{
			return false;
		}
		if (_copiedTaskSelectId == clickedTaskId)
		{
			_copiedTaskSelectId = null;
			return true;
		}
		TimelineCellViewModel cellModel = _model.GetCellModel(_copiedTaskSelectId);
		if (cellModel != null)
		{
			cellModel.Operation = cellModel.Operation.Remove(TimelineCellOperation.Edit);
		}
		_copiedTaskSelectId = null;
		return true;
	}

	private void ClearCopiedTaskEditSelection(string exceptTaskId)
	{
		foreach (TimelineCellViewModel item in _model.WhereCell((TimelineCellViewModel model) => model.Id != exceptTaskId && model.Operation.Contain(TimelineCellOperation.Edit)))
		{
			item.Operation = item.Operation.Remove(TimelineCellOperation.Edit);
		}
	}

	private async void OnOperationTagsSelect(object sender, TagSelectData tags)
	{
		await UpdateTaskWithUndo(SetTags(), _currentModel.Id);
		if (_model.GroupByEnum == Constants.SortType.tag)
		{
			await _model.UpdateGroupAsync();
			await _model.UpdateCellLineAsync();
		}
		_model.UpdateCellTime(_currentModel);
		await _model.UpdateCellLineAsync();
		async Task SetTags()
		{
			await Task.Yield();
			await TaskService.SetTags(_currentModel.Id, tags.OmniSelectTags);
		}
	}

	private async void OnOperationAssigneeSelect(object sender, AvatarInfo assignee)
	{
		await UpdateTaskWithUndo(SetAssignee(), _currentModel.Id);
		if (_model.GroupByEnum == Constants.SortType.assignee)
		{
			await _model.UpdateCellLineAsync();
		}
		async Task SetAssignee()
		{
			await Task.Yield();
			await TaskService.SetAssignee(_currentModel.Id, assignee.UserId);
		}
	}

	private async void OnOperationProjectSelect(object sender, SelectableItemViewModel e)
	{
		(string, string, string) projectAndColumnId = e.GetProjectAndColumnId();
		string item = projectAndColumnId.Item1;
		string columnId = projectAndColumnId.Item2;
		ProjectModel project = CacheManager.GetProjectById(item);
		if (project == null || (_currentModel.DisplayModel.ProjectId == item && (string.IsNullOrEmpty(columnId) || _currentModel.DisplayModel.ColumnId == columnId)))
		{
			return;
		}
		_model.RemoveCell(_currentModel);
		await _model.UpdateCellLineAsync();
		ProjectIdentity projectIdentity = _model.ProjectIdentity;
		if (projectIdentity is FilterProjectIdentity filter)
		{
			TaskModel task = await TaskDao.GetTaskById(_currentModel.Id);
			List<TaskModel> children = await TaskDao.GetAllSubTasksById(_currentModel.Id, _currentModel.DisplayModel.ProjectId);
			await Commit();
			if (TaskViewModelHelper.GetMatchedTasks(filter, new List<string> { task.id }).Count == 0)
			{
				TryToastTaskChangeUndo(task, children);
			}
		}
		else
		{
			await Commit();
			if (TaskViewModelHelper.GetMatchedTasks(_model.ProjectIdentity, new List<string> { _currentModel.Id }).Count == 0)
			{
				Utils.GetToastWindow()?.ToastMoveProjectControl(project.ProjectId);
			}
		}
		async Task Commit()
		{
			string id = _currentModel.Id;
			string projectId = project.ProjectId;
			string columnId2 = columnId;
			await TaskService.MoveProject(id, projectId, null, columnId2);
			if (!string.IsNullOrEmpty(columnId))
			{
				await TaskService.SaveTaskColumnId(_currentModel.Id, columnId);
			}
			SyncManager.TryDelaySync();
		}
	}

	private async void OnOperationAbandonOrReopen(object sender, EventArgs e)
	{
		int status = ((_currentModel.DisplayModel.Status != -1) ? (-1) : 0);
		await TaskService.SetTaskStatus(_currentModel.Id, status, status != 0, Utils.GetToastWindow());
		if (_model.HideCompleted && status != 0)
		{
			_model.RemoveCell(_currentModel);
		}
	}

	private async void OnOperationPrioritySelect(object sender, int e)
	{
		await UpdateTaskWithUndo(SetPriority(), _currentModel.Id);
		_model.UpdateCellTime(_currentModel);
		await _model.UpdateCellLineAsync();
		async Task SetPriority()
		{
			await Task.Yield();
			await TaskService.SetPriority(_currentModel.Id, e);
		}
	}

	private void OnOperationDisappear(object sender, bool e)
	{
		if (sender is TaskOperationDialog taskOperationDialog)
		{
			taskOperationDialog.TimeClear -= OnOperationTimeClear;
			taskOperationDialog.AbandonOrReopen -= OnOperationAbandonOrReopen;
			taskOperationDialog.PrioritySelect -= OnOperationPrioritySelect;
			taskOperationDialog.ProjectSelect -= OnOperationProjectSelect;
			taskOperationDialog.QuickDateSelect -= OnOperationQuickDateSelect;
			taskOperationDialog.TimeSelect -= OnOperationTimeSelect;
			taskOperationDialog.Deleted -= OnOperationDeleted;
			taskOperationDialog.AssigneeSelect -= OnOperationAssigneeSelect;
			taskOperationDialog.TagsSelect -= OnOperationTagsSelect;
			taskOperationDialog.Copied -= OnOperationCopied;
			taskOperationDialog.LinkCopied -= OnOperationLinkCopied;
			taskOperationDialog.SwitchTaskOrNote -= OnOperationSwitchTaskOrNote;
			taskOperationDialog.Disappear -= OnOperationDisappear;
			taskOperationDialog.SkipCurrentRecurrence -= OnOperationSkipCurrentRecurrence;
			taskOperationDialog.Toast -= OnOperationToast;
		}
		SyncManager.TryDelaySync();
		DelayEndIgnoreMouseUp();
	}

	private async void OnOperationDeleted(object sender, EventArgs e)
	{
		TaskModel taskModel = await TaskDao.GetThinTaskById(_currentModel.Id);
		if (taskModel != null)
		{
			taskModel.deleted = 1;
			taskModel.modifiedTime = DateTime.Now;
			await TaskService.UpdateTaskOnDeletedChanged(taskModel);
			UndoToast undoToast = new UndoToast();
			undoToast.InitTaskUndo(_currentModel.Id, _currentModel.Title);
			Utils.GetToastWindow().Toast(undoToast);
		}
	}

	private async void OnOperationQuickDateSelect(object sender, DateTime e)
	{
		if (sender is TaskOperationDialog taskOperationDialog)
		{
			taskOperationDialog.Dismiss();
		}
		if (!_currentModel.IsAllDay)
		{
			e = DateUtils.SetDateOnly(_currentModel.StartDate, e);
		}
		DateTime? endDate = _currentModel.EndDate;
		if (endDate.HasValue)
		{
			DateTime valueOrDefault = endDate.GetValueOrDefault();
			TimeSpan value = valueOrDefault - _currentModel.StartDate;
			_currentModel.TrySetStartDate(e);
			_currentModel.EndDate = e.Add(value);
		}
		else
		{
			_currentModel.TrySetStartDate(e);
		}
		await UpdateTaskWithUndo(QuickSetDate(), _currentModel.Id);
		_model.UpdateCellTime(_currentModel);
		await _model.UpdateCellLineAsync();
		async Task QuickSetDate()
		{
			await Task.Yield();
			await _currentModel.CommitDate(null);
		}
	}

	private async void OnOperationTimeSelect(object sender, TimeData e)
	{
		if (sender is TaskOperationDialog taskOperationDialog)
		{
			taskOperationDialog.Dismiss();
		}
		await UpdateTaskWithUndo(SetTime(), _currentModel.Id);
		async Task SetTime()
		{
			await Task.Yield();
			DateTime? startDate = e.StartDate;
			if (startDate.HasValue)
			{
				DateTime valueOrDefault = startDate.GetValueOrDefault();
				_currentModel.TrySetStartDate(valueOrDefault);
				_currentModel.EndDate = e.DueDate;
				_currentModel.IsAllDay = e.IsAllDay ?? true;
				await _currentModel.CommitDate(e);
				_model.UpdateCellTime(_currentModel);
				await _model.UpdateCellLineAsync();
			}
			else
			{
				await _currentModel.ClearDate();
			}
		}
	}

	private async void OnOperationTimeClear(object sender, EventArgs e)
	{
		if (sender is TaskOperationDialog taskOperationDialog)
		{
			taskOperationDialog.Dismiss();
		}
		await UpdateTaskWithUndo(_currentModel.ClearDate(), _currentModel.Id);
		_model.UpdateCellTime(_currentModel);
		await _model.UpdateCellLineAsync();
	}

	public int GetDayOffsetByPoint(Point point)
	{
		return (int)((MainScroll.HorizontalOffset + point.X) / _model.OneDayWidth);
	}

	public int GetLineByPoint(Point point)
	{
		if (point.Y < 0.0)
		{
			return -1;
		}
		return (int)((MainScroll.VerticalOffset + point.Y) / _model.OneLineHeight);
	}

	private DateTime GetDay()
	{
		if (MainScroll.ActualWidth <= 0.0)
		{
			return DateTime.MinValue;
		}
		double num = MainScroll.HorizontalOffset + MainScroll.ActualWidth / 2.0;
		return _model.StartDate.AddDaysSafely((int)Math.Round(num / _model.OneDayWidth, 0, MidpointRounding.AwayFromZero));
	}

	private async Task GotoDay(DateTime day, bool smooth = false, bool resetRange = false, Point hoverPoint = default(Point))
	{
		double dayWidth = _model.OneDayWidth;
		int num = (int)((base.IsLoaded ? MainScroll.ViewportWidth : _initWidth) / dayWidth / 2.0 + 1.0);
		DateTime dateTime = day.AddDaysSafely(-1 * num);
		var (dateTime4, dateTime5) = _model.StartEndTuple;
		if (resetRange)
		{
			await _model.MoveSpan(day.AddDaysSafely(num * -2), day.AddDaysSafely(num * 2));
		}
		else
		{
			if (dateTime4 > dateTime)
			{
				dateTime4 = day.AddDaysSafely(num * -1);
			}
			if (dateTime5 < day.AddDaysSafely(num))
			{
				dateTime5 = day.AddDaysSafely(num);
			}
			int num2 = num * 2;
			int days = (dateTime5 - dateTime4).Days;
			if (days < num2)
			{
				int num3 = (num2 - days + 5) / 2;
				if (num3 > 0)
				{
					dateTime4 = dateTime4.AddDaysSafely(-1 * num3);
					dateTime5 = dateTime5.AddDaysSafely(num3);
				}
			}
			if (dateTime4 < _model.StartDate || dateTime5 > _model.EndDate)
			{
				DateTime startDate = dateTime4.AddDaysSafely((double)(1 - dateTime4.DayOfWeek)).AddDaysSafely(-28.0);
				await _model.MoveSpan(startDate, dateTime5.AddDaysSafely(28.0));
			}
		}
		double offset = (day - _model.StartDate).TotalDays * dayWidth - ((hoverPoint != default(Point)) ? (hoverPoint.X - 0.5 * dayWidth) : (MainScroll.ActualWidth / 2.0));
		if (smooth)
		{
			if (!(Math.Abs(offset - MainScroll.HorizontalOffset) < 4.0))
			{
				SmoothScrollAsync(offset - MainScroll.HorizontalOffset);
			}
			return;
		}
		MainScroll.ScrollToHorizontalOffset(offset);
		if (Math.Abs(MainScroll.HorizontalOffset - offset) > 1.0)
		{
			int retry = 0;
			while (MainScroll.ScrollableWidth < offset && retry < 40)
			{
				await Task.Delay(10);
				retry++;
			}
			MainScroll.ScrollToHorizontalOffset(offset);
		}
	}

	private async void OnContainerSizeChanged()
	{
		if (_offset > 0.0)
		{
			int retry = 0;
			while (MainScroll.ScrollableWidth < _offset && retry < 400)
			{
				await Task.Delay(1);
				retry++;
			}
			if (MainScroll.ScrollableWidth < _offset)
			{
				_offset = -1.0;
				return;
			}
			MainScroll.ScrollToHorizontalOffset(_offset);
			_offset = -1.0;
		}
	}

	private async void OnScrollChanged(object sender, ScrollChangedEventArgs e)
	{
		if (e.HorizontalChange != 0.0)
		{
			if (!_projectScrollChanged && Mouse.Captured != null)
			{
				_projectScrollChanged = true;
			}
			DateTime item;
			DateTime item2;
			if (e.HorizontalChange > 0.0 && e.HorizontalOffset + e.ViewportWidth + 10.0 >= MainScroll.ExtentWidth)
			{
				TupleExtensions.Deconstruct(_model.StartEndTuple, out item, out item2);
				DateTime startDate = item;
				DateTime dt = item2;
				await _model.MoveSpan(startDate, dt.AddDaysSafely(60.0));
				if (Mouse.Captured is Thumb thumb && string.IsNullOrEmpty(thumb.Name))
				{
					Mouse.Capture(null);
				}
			}
			if (_offset < 0.0 && e.HorizontalChange < 0.0 && e.ExtentWidthChange >= 0.0 && e.HorizontalOffset < 10.0)
			{
				TupleExtensions.Deconstruct(_model.StartEndTuple, out item2, out item);
				DateTime dt2 = item2;
				DateTime endDate = item;
				DateTime startDate2 = dt2.AddDaysSafely((double)(1 - dt2.DayOfWeek)).AddDaysSafely(-70.0);
				_offset = 70.0 * _model.OneDayWidth;
				await _model.MoveSpan(startDate2, endDate);
				_batchStartPoint = new Point(_batchStartPoint.X + _offset, _batchStartPoint.Y);
				OnContainerSizeChanged();
				if (Mouse.Captured is Thumb thumb2 && string.IsNullOrEmpty(thumb2.Name))
				{
					Mouse.Capture(null);
				}
			}
			_model.XOffset = MainScroll.HorizontalOffset;
			DayLineScroll.ScrollToHorizontalOffset(MainScroll.HorizontalOffset);
			YearScroll.ScrollToHorizontalOffset(MainScroll.HorizontalOffset);
		}
		if (e.VerticalChange != 0.0)
		{
			ColumnScroll.ScrollToVerticalOffset(MainScroll.VerticalOffset);
		}
	}

	private async void OnGotoTodayMouseUp(object sender, MouseButtonEventArgs e)
	{
		UserActCollectUtils.AddClickEvent("timeline", "action_bar", "back_to_today");
		await GotoDay(DateTime.Today.AddDaysSafely((base.ActualWidth / 2.0 - (_model.ShowGroup ? _model.GroupWidth : 0.0) - 1.5 * TimelineConstants.GetOneDayWidth(-1)) / _model.OneDayWidth), smooth: true);
		_projectScrollChanged = true;
	}

	private void OnPrePageMouseUp(object sender, MouseButtonEventArgs e)
	{
		UserActCollectUtils.AddClickEvent("timeline", "view_action", "button_to_view");
		SmoothScrollAsync(MainScroll.ViewportWidth / 3.0 * -2.0);
	}

	private void OnNextPageMouseUp(object sender, MouseButtonEventArgs e)
	{
		UserActCollectUtils.AddClickEvent("timeline", "view_action", "button_to_view");
		SmoothScrollAsync(MainScroll.ViewportWidth / 3.0 * 2.0);
	}

	internal void RemoveHoverHeader()
	{
		_model.HoverStartEndTuple = null;
	}

	private void SmoothScrollAsync(double offset)
	{
		if (offset == 0.0)
		{
			return;
		}
		_model.GotoBtnEnabled = false;
		int sign = ((offset > 0.0) ? 1 : (-1));
		offset = Math.Abs(offset);
		double pos = 0.0;
		DispatcherTimer scrollTimer = new DispatcherTimer(DispatcherPriority.Render)
		{
			Interval = TimeSpan.FromMilliseconds(8.0)
		};
		scrollTimer.Tick += delegate
		{
			double val = EaseOutQuad(pos, 0.0, offset, 1000.0);
			pos += 8.0;
			val = Math.Max(1.0, val);
			MainScroll.ScrollToHorizontalOffset(MainScroll.HorizontalOffset + (double)sign * val);
			offset -= val;
			if (offset <= 0.0)
			{
				scrollTimer.Stop();
			}
		};
		scrollTimer.Start();
		_model.GotoBtnEnabled = true;
		static double EaseOutQuad(double t, double b, double c, double d)
		{
			return (0.0 - c) * (Math.Pow(t / d - 1.0, 4.0) - 1.0) + b;
		}
	}

	public void SetFoldMenuIcon(bool isHide, bool oneColumn)
	{
		TimelineNavBar.FoldImage.SetResourceReference(Path.DataProperty, oneColumn ? "Ic.ProjectList.List" : (isHide ? "Ic.ProjectList.ExpandSide" : "Ic.ProjectList.CollapseSide"));
		TimelineNavBar.SetModeColumn(oneColumn);
	}

	private async void OnTitleClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		await Task.Delay(1);
		Utils.FindParent<ListViewContainer>(this)?.ShowProjectMenu();
	}

	public void HideFoldMenuIcon()
	{
		TimelineNavBar.FoldGrid.Visibility = Visibility.Collapsed;
		TimelineNavBar.TitleGrid.Margin = new Thickness(2.0, 0.0, 0.0, 0.0);
	}

	private void SaveProjectPos(bool force = false)
	{
		if ((force || _projectScrollChanged) && _model.ProjectIdentity != null)
		{
			DateTime day = GetDay();
			_projectDays[_model.ProjectIdentity.CatId] = day;
		}
	}

	public async Task LoadProject(ProjectIdentity projectIdentity, string taskId = null)
	{
		SetTitle(projectIdentity);
		SaveProjectPos();
		_offset = -1.0;
		_projectScrollChanged = false;
		_projectIdentity = projectIdentity;
		await LoadProjectInternal(projectIdentity, taskId);
		if (projectIdentity.SortOption.groupByType == Constants.SortType.sortOrder)
		{
			ColumnBatchHandler.MergeWithServer(projectIdentity.GetProjectId());
		}
		Header.InvalidateVisual();
		HoverHeader.InvalidateVisual();
		TimelineBackground.InvalidateVisual();
	}

	private void SetTitle(ProjectIdentity projectIdentity)
	{
		TimelineNavBar.SetTitle(projectIdentity?.GetDisplayTitle());
		TimelineNavBar.SetTitleEnable((projectIdentity is NormalProjectIdentity { Project: not null } normalProjectIdentity && !normalProjectIdentity.Project.Isinbox && normalProjectIdentity.Project.IsEnable()) || projectIdentity is GroupProjectIdentity || projectIdentity is FilterProjectIdentity);
	}

	private async Task LoadProjectInternal(ProjectIdentity projectProject, string taskId = null)
	{
		if (projectProject == null)
		{
			return;
		}
		bool samePid = _model.ProjectIdentity?.Id == projectProject.Id;
		if (_projectDays.TryGetValue(projectProject.CatId, out var oldDay) && samePid && string.IsNullOrEmpty(taskId))
		{
			await _model.SetProjectIdentity(projectProject);
			await GotoDay(oldDay, smooth: false, resetRange: true);
			return;
		}
		await _model.SetProjectIdentity(projectProject);
		if (!string.IsNullOrEmpty(taskId))
		{
			_model.Editing = true;
		}
		await Task.Delay(10);
		while (_model.AvailableReset)
		{
			await Task.Delay(10);
		}
		DateTime gotoDay = DateTime.Today;
		bool hasOldDay = false;
		TaskBaseViewModel taskById = TaskCache.GetTaskById(taskId);
		if (taskById != null && taskById.StartDate.HasValue && taskById.StartDate.Value.Year > 1970)
		{
			gotoDay = taskById.StartDate.Value.Date;
			hasOldDay = true;
			_projectScrollChanged = true;
		}
		else if (oldDay != DateTime.MinValue)
		{
			hasOldDay = true;
			gotoDay = oldDay;
		}
		else
		{
			DateTime? rentCellDate = _model.GetRentCellDate();
			if (rentCellDate.HasValue)
			{
				DateTime valueOrDefault = rentCellDate.GetValueOrDefault();
				gotoDay = valueOrDefault;
			}
		}
		int retry = 0;
		while (MainScroll.ActualWidth == 0.0 && retry < 100)
		{
			await Task.Delay(4);
			retry++;
		}
		if (MainScroll.ActualWidth > 0.0)
		{
			if (!hasOldDay)
			{
				gotoDay = gotoDay.AddDaysSafely(base.ActualWidth / 4.0 / _model.OneDayWidth);
			}
			await GotoDay(gotoDay, smooth: false, resetRange: true);
			if (!samePid)
			{
				MainScroll.ScrollToTop();
			}
		}
		ClosedLoader closeLoader = new ClosedLoader();
		if (await closeLoader.NeedPullCompletedTasks(projectProject) && await closeLoader.TryLoadCompletedTasks(projectProject))
		{
			OnCompletedLoaded();
		}
		if (!string.IsNullOrEmpty(taskId))
		{
			TimelineCellViewModel cellModel = _model.GetCellModel(taskId);
			if (cellModel != null)
			{
				_model.Editing = true;
				if (cellModel.Top > MainScroll.ActualHeight + MainScroll.VerticalOffset)
				{
					MainScroll.ScrollToVerticalOffset(cellModel.Top - MainScroll.ActualHeight + 150.0);
				}
				await Task.Delay(100);
				CellCanvas.TryShowDetailWindow(cellModel);
				return;
			}
		}
		_model.Editing = false;
	}

	private void OnMainScrollSizeChanged(object sender, SizeChangedEventArgs e)
	{
		_model.ViewWidth = e.NewSize.Width;
	}

	private void OnResizeColumnDragDelta(object sender, DragDeltaEventArgs e)
	{
		double groupWidth = GroupBorder.Width + e.HorizontalChange;
		_model.GroupWidth = groupWidth;
	}

	public void MoveToolTips(Point point)
	{
		if (_hoveredCell != null)
		{
			point.Y = _hoveredCell.TranslatePoint(new Point(0.0, 0.0), this).Y + _hoveredCell.ActualHeight;
		}
		ToolTipControl.Margin = new Thickness(point.X - ToolTipControl.ActualWidth / 2.0, point.Y, 0.0, 0.0);
	}

	public void OpenToolTips(FrameworkElement cell, bool showDateTip = false)
	{
		if (cell == null)
		{
			return;
		}
		_hoveredCell = cell;
		if (cell.DataContext is TimelineCellViewModel timelineCellViewModel)
		{
			if (timelineCellViewModel.Operation.Contain(TimelineCellOperation.Edit))
			{
				TryCloseToolTips();
				return;
			}
			ToolTipControl.DataContext = timelineCellViewModel;
			ToolTipControl.ShowTip(showDateTip);
		}
		Point position = Mouse.GetPosition(this);
		Point point = cell.TranslatePoint(new Point(0.0, 0.0), this);
		ToolTipControl.Margin = new Thickness(position.X - ToolTipControl.ActualWidth / 2.0, point.Y + cell.ActualHeight + 3.0, 0.0, 0.0);
		ToolTipControl.Visibility = Visibility.Visible;
	}

	public void TryCloseToolTips(bool force = false)
	{
		if (ToolTipControl.Visibility == Visibility.Visible)
		{
			if (force)
			{
				ToolTipControl.Visibility = Visibility.Collapsed;
			}
			if (!ToolTipControl.IsMouseOver && (_hoveredCell == null || !_hoveredCell.IsMouseOver || !(_hoveredCell.DataContext is TimelineCellViewModel timelineCellViewModel) || timelineCellViewModel.Operation.Contain(TimelineCellOperation.Edit)))
			{
				ToolTipControl.Visibility = Visibility.Hidden;
			}
		}
	}

	private void OnToolTipPopupMouseLeave(object sender, MouseEventArgs e)
	{
		TryCloseToolTips();
	}

	public void OnTouchPadHorizontalScroll(int offset)
	{
		if (TimelineFloating.IsMouseOver)
		{
			TimelineFloating.DealWithMainScrollMove(hor: true, -1 * offset);
		}
		MainScroll.ScrollToHorizontalOffset(MainScroll.HorizontalOffset + (double)offset);
	}

	private void OnSwitchRangeMouseUp(object sender, MouseButtonEventArgs args)
	{
		SelectRangePopup.IsOpen = !SelectRangePopup.IsOpen;
		CustomMenuList customMenuList = new CustomMenuList(new List<CustomMenuItemViewModel>
		{
			new CustomMenuItemViewModel((object)"day", Utils.GetString("Day"), (Geometry)null, "")
			{
				Selected = (_model.TimelineRange == "day")
			},
			new CustomMenuItemViewModel((object)"week", Utils.GetString("Week"), (Geometry)null, "")
			{
				Selected = (_model.TimelineRange == "week")
			},
			new CustomMenuItemViewModel((object)"month", Utils.GetString("Month"), (Geometry)null, "")
			{
				Selected = (_model.TimelineRange == "month")
			}
		}, SelectRangePopup);
		customMenuList.Operated += async delegate(object o, object e)
		{
			if (e is string text)
			{
				DateTime day = GetDay();
				_model.TimelineRange = text;
				UserActCollectUtils.AddClickEvent("timeline", "view_zoom", text);
				await GotoDay(day);
				await _model.UpdateCellLineAsync();
			}
		};
		customMenuList.Show();
	}

	private async void OnSwitchRangeClick(object sender, EventArgs e)
	{
		if (sender is FrameworkElement { DataContext: TimelineViewModel dataContext, Tag: string tag })
		{
			DateTime day = GetDay();
			dataContext.TimelineRange = tag;
			UserActCollectUtils.AddClickEvent("timeline", "view_zoom", tag);
			await GotoDay(day);
			await _model.UpdateCellLineAsync();
		}
		SelectRangePopup.IsOpen = false;
	}

	public new void KeyDown(Key key)
	{
		switch (key)
		{
		case Key.Left:
			MainScroll.ScrollToHorizontalOffset(MainScroll.HorizontalOffset - base.ActualWidth / 10.0);
			break;
		case Key.Right:
			MainScroll.ScrollToHorizontalOffset(MainScroll.HorizontalOffset + base.ActualWidth / 10.0);
			break;
		case Key.Up:
			MainScroll.ScrollToVerticalOffset(Math.Max(0.0, MainScroll.VerticalOffset - _model.OneDayWidth));
			break;
		case Key.Down:
			MainScroll.ScrollToVerticalOffset(MainScroll.VerticalOffset + _model.OneDayWidth);
			break;
		case Key.D:
		case Key.M:
		case Key.W:
			if (!Utils.IfCtrlPressed() || !Utils.IfShiftPressed())
			{
				QuickSwitchRange(key);
			}
			break;
		}
		TimelineFloating.Close();
	}

	private async void QuickSwitchRange(Key key)
	{
		string text = "day";
		switch (key)
		{
		case Key.W:
			text = "week";
			break;
		case Key.M:
			text = "month";
			break;
		}
		int rangeDefaultWidthIndex = TimelineConstants.GetRangeDefaultWidthIndex(text);
		if (_model.TimelineRangeIndex != rangeDefaultWidthIndex)
		{
			DateTime day = GetDay();
			_model.SetRangIndex(rangeDefaultWidthIndex);
			UserActCollectUtils.AddClickEvent("timeline", "view_zoom", text);
			await GotoDay(day);
			await _model.UpdateCellLineAsync();
			TryCloseToolTips(force: true);
		}
	}

	public void ShowDragColumnPopup(TimelineGroupViewModel model, MouseEventArgs e)
	{
		_model?.OnDragColumn();
		ColumnDragPopup.DataContext = model;
		Point position = e.GetPosition(this);
		SetColumnDragPopupPosition(position);
		ColumnDragPopup.IsOpen = true;
		RegisterDragEvent();
	}

	private void SetColumnDragPopupPosition(Point point)
	{
		ColumnDragPopup.HorizontalOffset = point.X - 30.0;
		ColumnDragPopup.VerticalOffset = point.Y - 10.0;
	}

	private void RegisterDragEvent()
	{
		Window window = Window.GetWindow(this);
		if (window != null)
		{
			window.CaptureMouse();
			window.MouseMove -= OnDragColumnMouseMove;
			window.MouseMove += OnDragColumnMouseMove;
			window.MouseLeftButtonUp -= OnDragColumnDrop;
			window.MouseLeftButtonUp += OnDragColumnDrop;
		}
	}

	private async void OnDragColumnDrop(object sender, MouseButtonEventArgs e)
	{
		if (sender is Window window)
		{
			window.ReleaseMouseCapture();
			window.MouseMove -= OnDragColumnMouseMove;
			window.MouseLeftButtonUp -= OnDragColumnDrop;
		}
		ColumnDragPopup.IsOpen = false;
		try
		{
			if (!ColumnDropLine.IsVisible)
			{
				return;
			}
			ColumnDropLine.Visibility = Visibility.Collapsed;
			int num = (int)ColumnDropLine.Margin.Top + (int)MainScroll.VerticalOffset;
			int index = (num + 4) / 40;
			object dataContext = ColumnDragPopup.DataContext;
			if (!(dataContext is TimelineGroupViewModel model))
			{
				return;
			}
			ColumnModel columnModel = await ColumnDao.GetColumnById(model.Id);
			if (columnModel != null)
			{
				TimelineGroupViewModel timelineGroupViewModel = ((index <= 0) ? null : _model.GroupModels[index - 1]);
				TimelineGroupViewModel timelineGroupViewModel2 = ((index >= _model.GroupModels.Count) ? null : _model.GroupModels[index]);
				if ((timelineGroupViewModel != null || timelineGroupViewModel2 != null) && !(timelineGroupViewModel?.Id == model.Id) && !(timelineGroupViewModel2?.Id == model.Id))
				{
					long value = ((timelineGroupViewModel == null) ? (timelineGroupViewModel2.SortOrder - 268435456) : ((timelineGroupViewModel2 == null) ? (timelineGroupViewModel.SortOrder + 268435456) : MathUtil.LongAvg(timelineGroupViewModel.SortOrder, timelineGroupViewModel2.SortOrder)));
					columnModel.sortOrder = value;
					await ColumnDao.UpdateColumn(columnModel);
					DataChangedNotifier.NotifyColumnChanged(_projectIdentity?.Id);
					SyncManager.TryDelaySync();
				}
			}
		}
		finally
		{
			_model?.ReloadColumns();
		}
	}

	private void OnDragColumnMouseMove(object sender, MouseEventArgs e)
	{
		Point position = e.GetPosition(this);
		SetColumnDragPopupPosition(position);
		int count = _model.GroupModels.Count;
		int num = (int)position.Y - 127 + (int)MainScroll.VerticalOffset;
		if (num > 0 && (double)num < (double)count * 40.0)
		{
			ColumnDropLine.Visibility = Visibility.Visible;
			int num2 = num / 40;
			if ((double)(num % 40) > 20.0)
			{
				num2++;
			}
			ColumnDropLine.Margin = new Thickness(0.0, 40.0 * (double)num2 - 4.0 - (double)(int)MainScroll.VerticalOffset, 0.0, 0.0);
		}
		else
		{
			ColumnDropLine.Visibility = Visibility.Collapsed;
		}
	}

	private async void OnProjectChanged(object sender, EventArgs e)
	{
		if (base.Visibility != Visibility.Visible)
		{
			return;
		}
		string text = _model.ProjectIdentity?.CatId;
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		ProjectModel project = CacheManager.GetProjectById(text);
		if (project == null)
		{
			return;
		}
		if (project.viewMode == "kanban")
		{
			await Task.Delay(500);
			if (base.IsVisible)
			{
				DataChangedNotifier.NotifyProjectViewModeChanged(new NormalProjectIdentity(project));
			}
		}
		else if (project.viewMode == "list")
		{
			await Task.Delay(500);
			if (base.IsVisible)
			{
				DataChangedNotifier.NotifyProjectViewModeChanged(new NormalProjectIdentity(project));
			}
		}
		else if (project.Timeline != null)
		{
			if (project.Timeline.sortOption != null && project.Timeline.sortOption.Equal(_model.TimelineSortOption))
			{
				_model.TimelineSortOption = project.Timeline.sortOption;
			}
			if (project.Timeline.Range != _model.TimelineRange)
			{
				DateTime day = GetDay();
				_model.TimelineRange = project.Timeline?.Range;
				await GotoDay(day);
			}
			if (project.name != _model.ProjectTitle)
			{
				_model.SetProjectTitle(project.name);
			}
		}
	}

	private void OnColumnChanged(object sender, string e)
	{
		if (_model.ProjectIdentity?.GetTimelineModel().SortType == Constants.SortType.sortOrder.ToString() && _model.ProjectIdentity?.CatId == e)
		{
			_model.ReloadColumns();
		}
	}

	private void OnCompletedLoaded()
	{
		ProjectIdentity projectIdentity = _model.ProjectIdentity;
		NormalProjectIdentity prj = projectIdentity as NormalProjectIdentity;
		if (prj != null && _model.AvailableModels.Count == 0)
		{
			if (_projectDays.ContainsKey(prj.Project.id))
			{
				_projectDays.Remove(prj.Project.id);
			}
			_projectScrollChanged = false;
			base.Dispatcher.InvokeAsync((Func<Task>)async delegate
			{
				LoadProject(new NormalProjectIdentity(prj.Project));
			});
		}
	}

	public void TryToastTaskChangeUndo(TaskModel task, List<TaskModel> children = null, List<TaskDetailItemModel> checkItems = null)
	{
		if (task != null && TaskViewModelHelper.GetMatchedTasks(_projectIdentity, new List<string> { task.id }).Count == 0)
		{
			Utils.FindParent<IToastShowWindow>(this).Toast(new UndoToast(new TaskUndo(task, string.Empty, Utils.GetString("TaskHasBeenFiltered"), children, checkItems)));
		}
	}

	private async Task UpdateTaskWithUndo(Task t, string taskId, bool undoCheckItem = false)
	{
		if (_projectIdentity is FilterProjectIdentity)
		{
			TaskModel task = await TaskDao.GetTaskById(taskId);
			List<TaskDetailItemModel> checkItems = null;
			if (undoCheckItem)
			{
				checkItems = await TaskDetailItemDao.GetCheckItemsByTaskId(taskId);
			}
			await t;
			TryToastTaskChangeUndo(task, null, checkItems);
		}
		else
		{
			await t;
		}
	}

	public void TryToastString(string toast)
	{
		Utils.FindParent<IToastShowWindow>(this)?.TryToastString(null, toast);
	}

	public async void Reload()
	{
		if (!TickDragHelper.Dragging && base.IsVisible && !_batchMouseDown)
		{
			_model.Reload();
			Header.InvalidateVisual();
			HoverHeader.InvalidateVisual();
			TimelineBackground.InvalidateVisual();
		}
	}

	public async void ToggleGroupsOpen()
	{
		_model.ToggleGroupsOpen();
	}

	public async void DelaySetHitVisible()
	{
		base.IsHitTestVisible = false;
		await Task.Delay(100);
		base.IsHitTestVisible = true;
	}

	public void SetInitWidth(double initWidth)
	{
		_initWidth = initWidth;
	}

	public void Dispose()
	{
		_model.AvailableModels.ClearEvents();
	}

	public TimelineViewModel GetViewModel()
	{
		return _model;
	}

	public void ReloadSortOption()
	{
		_model.OnSortOptionChanged();
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
		ProjectIdentity projectIdentity = _model.ProjectIdentity;
		if (projectIdentity == null || !projectIdentity.CanEdit)
		{
			return;
		}
		Point position = e.GetPosition(MainScroll);
		double x = position.X;
		double y = position.Y;
		int num = (_model.IsArranging ? 215 : 0);
		bool flag = (num <= 0 || !(x > MainScroll.ActualWidth - (double)num)) && !(position.X < 0.0) && !(position.X > base.ActualWidth) && !(position.Y < -42.0) && !(position.Y > base.ActualHeight);
		if (_dragOver != flag)
		{
			if (flag)
			{
				OnDragEnter();
			}
			else
			{
				OnDragLeave();
			}
			_dragOver = flag;
		}
		if (!flag)
		{
			return;
		}
		if (_autoScroller == null)
		{
			_autoScroller = new DragAutoScrollHelper(MainScroll);
		}
		double leftMargin = (_model.ShowGroup ? _model.GroupWidth : 0.0);
		if (_autoScroller.TryAutoScroll(e, checkHorizon: true, leftMargin, 0.0, num, -42.0))
		{
			return;
		}
		TimelineCellViewModel cellModel = (TickDragHelper.DragData as TimelineCellViewModel) ?? _dragCell;
		if (cellModel == null)
		{
			return;
		}
		if (cellModel.CanDragSwitchLine && y != 0.0)
		{
			int line = GetLineByPoint(position);
			if (line < 0)
			{
				line = cellModel.StartDragLine.GetValueOrDefault(line);
			}
			if (cellModel.BatchSelected && cellModel.StartDragLine.HasValue)
			{
				TimelineGroupViewModel timelineGroupViewModel = cellModel.Parent.GroupModels.LastOrDefault((TimelineGroupViewModel c) => c.Line <= line);
				TimelineGroupViewModel timelineGroupViewModel2 = cellModel.Parent.GroupModels.LastOrDefault((TimelineGroupViewModel c) => c.Line <= cellModel.StartDragLine.Value);
				if (timelineGroupViewModel != null && timelineGroupViewModel.Id != timelineGroupViewModel2?.Id)
				{
					_model.SetBatchDragging(2);
					cellModel.DragStatus = 1;
					cellModel.Line = line;
				}
				else
				{
					_model.SetBatchDragging(1);
					cellModel.DragStatus = 1;
					cellModel.Line = cellModel.StartDragLine.Value;
				}
			}
			else
			{
				cellModel.DragStatus = 1;
				cellModel.Line = line;
			}
		}
		if (x != 0.0)
		{
			if (cellModel.Operation.Contain(TimelineCellOperation.Hide))
			{
				cellModel.Operation = TimelineCellOperation.Full;
				_model.AddToAvailable(cellModel);
			}
			if (y < 0.0 && y > -43.0)
			{
				_model.RemoveAvailable(cellModel);
				_model.HighLightHover = true;
			}
			else if (_model.HighLightHover)
			{
				_model.HighLightHover = false;
				_model.AddToAvailable(cellModel);
			}
			if (cellModel.Operation.Contain(TimelineCellOperation.Full))
			{
				double offset = x - ((_dragCell == null && !cellModel.DragFromArrange) ? TickDragHelper.GetStartDragLeft() : (_model.OneDayWidth / 2.0)) + MainScroll.HorizontalOffset;
				cellModel.UpdatePosition(offset, _model);
			}
		}
	}

	private void OnDragDrop(object sender, MouseEventArgs e)
	{
		if (_dragOver)
		{
			OnDragDrop();
			_dragOver = false;
		}
		else
		{
			TimelineCellViewModel timelineCellViewModel = (TickDragHelper.DragData as TimelineCellViewModel) ?? _dragCell;
			if (timelineCellViewModel == null)
			{
				return;
			}
			if (_model.BatchSelect)
			{
				_model.UpdateBatchSelectCellTime();
			}
			else
			{
				_model.UpdateCellTime(timelineCellViewModel, ignoreEdit: true, forceCheck: true);
				timelineCellViewModel.DragStatus = 0;
				timelineCellViewModel.Visibility = Visibility.Visible;
			}
		}
		_model.HighLightHover = false;
	}

	private async void OnDragDrop()
	{
		_autoScroller?.SetAutoScrolling(autoScroll: false);
		TimelineCellViewModel cellModel = (TickDragHelper.DragData as TimelineCellViewModel) ?? _dragCell;
		if (cellModel == null || !cellModel.DisplayModel.IsTaskOrNote)
		{
			return;
		}
		cellModel.DragStatus = 0;
		string copiedTaskId = null;
		if (!cellModel.Operation.Contain(TimelineCellOperation.Hide))
		{
			HashSet<string> draggingTaskIds = TickDragHelper.DraggingTaskIds;
			if (draggingTaskIds != null && draggingTaskIds.Count > 1)
			{
				if (_dragCell != null)
				{
					TimelineCellViewModel dragCell = _dragCell;
					_dragCell = null;
					if (dragCell != null)
					{
						_model.RemoveCell(dragCell);
					}
					List<string> selected = TickDragHelper.DraggingTaskIds.ToList();
					foreach (TaskBaseViewModel item in from taskBaseViewModel in selected.Select(TaskCache.GetTaskById)
						where taskBaseViewModel != null
						select taskBaseViewModel)
					{
						await item.SetIdentityProperties(_model.ProjectIdentity, Utils.FindParent<IToastShowWindow>(this), changeDate: false);
					}
					await cellModel.BatchCommitGroup(cellModel.Line, selected);
					await TaskService.BatchSetStartDate(selected, dragCell.StartDate);
					await Task.Delay(100);
				}
				else
				{
					List<string> selected = _model.GetSelectedTaskIds();
					List<TaskModel> tasks = await TaskDao.GetTaskAndChildrenInBatch(TaskDao.GetTreeTopIds(selected, string.Empty));
					List<TaskDetailItemModel> checkItems = await TaskDetailItemDao.GetCheckItemsInTaskIds(selected);
					await _model.OnBatchDragDrop((!cellModel.StartDragLine.HasValue || cellModel.StartDragLine == cellModel.Line) ? ((int?)null) : new int?(cellModel.Line));
					_model.SetBatchDragging(0, delay: true);
					bool visible = false;
					if (_model.ProjectIdentity is FilterProjectIdentity)
					{
						List<TaskBaseViewModel> matchedTasks = TaskViewModelHelper.GetMatchedTasks(_model.ProjectIdentity, selected);
						List<TaskModel> list = tasks;
						if (list != null && list.Count > 0 && matchedTasks.Count < selected.Count)
						{
							visible = true;
						}
					}
					UndoToast undoToast = new UndoToast(new TaskUndo(null, string.Empty, Utils.GetString("TaskHasBeenFiltered"), tasks, checkItems));
					undoToast.SetVisible(visible);
					Utils.FindParent<IToastShowWindow>(this).Toast(undoToast);
				}
			}
			else
			{
				ProjectIdentity projectIdentity = _model.ProjectIdentity;
				if (projectIdentity is FilterProjectIdentity filter)
				{
					TaskModel task = await TaskDao.GetTaskById(cellModel.Id);
					if (task != null)
					{
						List<TaskModel> tasks = await TaskDao.GetAllSubTasksById(cellModel.Id, cellModel.DisplayModel.ProjectId);
						copiedTaskId = await Commit();
						if (TaskViewModelHelper.GetMatchedTasks(filter, new List<string> { task.id }).Count == 0)
						{
							TryToastTaskChangeUndo(task, tasks);
						}
					}
				}
				else
				{
					copiedTaskId = await Commit();
				}
			}
		}
		if (_model.GroupByEnum == Constants.SortType.tag)
		{
			await _model.UpdateGroupAsync();
		}
		await _model.UpdateCellLineAsync();
		if (!string.IsNullOrEmpty(copiedTaskId))
		{
			SelectCopiedTask(cellModel.Id);
		}
		cellModel.StartDragLine = null;
		DelaySetHitVisible();
		async Task<string> Commit()
		{
			string copyTaskId = null;
			if (Utils.IfCtrlPressed())
			{
				copyTaskId = (await TaskService.CopyTask(cellModel.Id, this))?.id;
			}
			bool needDelay = false;
			if (_dragCell != null)
			{
				needDelay = true;
				TimelineCellViewModel dragCell2 = _dragCell;
				_dragCell = null;
				if (dragCell2 != null)
				{
					_model.RemoveCell(dragCell2);
				}
				await dragCell2.DisplayModel.SetIdentityProperties(_model.ProjectIdentity, Utils.FindParent<IToastShowWindow>(this), changeDate: false);
			}
			if (!_model.HighLightHover)
			{
				await cellModel.CommitGroup(cellModel.Line);
			}
			await cellModel.CommitDate(null);
			if (needDelay)
			{
				await Task.Delay(100);
				return copyTaskId;
			}
			_model.UpdateCellTime(cellModel, ignoreEdit: true);
			return copyTaskId;
		}
	}

	private async void OnDragEnter()
	{
		TaskBaseViewModel taskBaseViewModel = null;
		bool flag = false;
		object dragData = TickDragHelper.DragData;
		if (!(dragData is CalendarDisplayViewModel calendarDisplayViewModel))
		{
			if (!(dragData is DisplayItemModel displayItemModel))
			{
				if (dragData is TimelineCellViewModel timelineCellViewModel && !_model.AvailableModels.Contains(timelineCellViewModel))
				{
					_model.AvailableModels.Add(timelineCellViewModel);
				}
			}
			else
			{
				taskBaseViewModel = displayItemModel.SourceViewModel;
			}
		}
		else
		{
			taskBaseViewModel = calendarDisplayViewModel.SourceViewModel;
			flag = calendarDisplayViewModel.BatchSelected;
		}
		if (taskBaseViewModel != null && taskBaseViewModel.IsTaskOrNote)
		{
			if (_dragCell != null)
			{
				_model.RemoveCell(_dragCell);
			}
			TimelineCellViewModel timelineCellViewModel2 = (_dragCell = new TimelineCellViewModel(_model, taskBaseViewModel));
			_dragCell.BatchText = (flag ? string.Format(Utils.GetString("MultiTasks"), TickDragHelper.DraggingTaskIds.Count) : null);
			timelineCellViewModel2.Operation = TimelineCellOperation.Full;
			timelineCellViewModel2.StartDragLine = timelineCellViewModel2.Line;
			timelineCellViewModel2.DragStatus = 1;
			_model.AddCell(timelineCellViewModel2, checkAva: false, addToAva: true);
			timelineCellViewModel2.SetColorAndAvatar();
			TickDragHelper.ChangeDragContent(timelineCellViewModel2, Math.Max(timelineCellViewModel2.Width, 24.0));
		}
	}

	public void OnDragLeave()
	{
		TimelineCellViewModel dragCell = _dragCell;
		_dragCell = null;
		_model.HighLightHover = false;
		if (dragCell != null)
		{
			_model.RemoveCell(dragCell);
		}
		_autoScroller?.SetAutoScrolling(autoScroll: false);
		if (TickDragHelper.DragData is TimelineCellViewModel timelineCellViewModel)
		{
			if (timelineCellViewModel.BatchSelected)
			{
				_model.ResetBatchDates();
			}
			else
			{
				timelineCellViewModel.EndDate = timelineCellViewModel.DisplayModel.DueDate;
				timelineCellViewModel.TrySetStartDate(timelineCellViewModel.DisplayModel.StartDate);
			}
			_model.RemoveAvailable(timelineCellViewModel);
			_model.HighLightHover = false;
			_model.HoverStartEndTuple = null;
		}
	}

	public void SetBackground(bool transparent)
	{
		TimelineBackground.SetBackground(transparent);
		if (transparent)
		{
			base.Background = Brushes.Transparent;
		}
		else
		{
			SetResourceReference(Control.BackgroundProperty, "ShowAreaBottomColor");
		}
	}

	public void CancelBatchSelect()
	{
		_model.ClearBatchSelect();
		ShowBatchControl(null);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/timeline/timelinecontainer.xaml", UriKind.Relative);
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
			Root = (TimelineContainer)target;
			break;
		case 2:
			TimelineNavBar = (TimelineNavBar)target;
			break;
		case 3:
			YearScroll = (ScrollViewer)target;
			break;
		case 4:
			OptionPanel = (StackPanel)target;
			break;
		case 5:
			((Border)target).MouseLeftButtonUp += OnGotoTodayMouseUp;
			break;
		case 6:
			SwitchRangeBorder = (Border)target;
			SwitchRangeBorder.MouseLeftButtonUp += OnSwitchRangeMouseUp;
			break;
		case 7:
			SelectRangePopup = (EscPopup)target;
			break;
		case 8:
			SelectRangeStackPanel = (StackPanel)target;
			break;
		case 9:
			((Border)target).MouseDown += OnMouseLeftDown;
			((Border)target).MouseLeftButtonUp += OnMouseLeftUp;
			((Border)target).MouseMove += OnMouseMove;
			((Border)target).PreviewMouseWheel += OnPreviewMouseWheel;
			break;
		case 10:
			DayLineScroll = (ScrollViewer)target;
			break;
		case 11:
			Header = (TimelineHeader)target;
			break;
		case 12:
			HoverHeader = (TimelineHeaderHover)target;
			break;
		case 13:
			PrePageGrid = (Grid)target;
			PrePageGrid.MouseLeftButtonUp += OnPrePageMouseUp;
			break;
		case 14:
			NextPageGrid = (Grid)target;
			NextPageGrid.MouseLeftButtonUp += OnNextPageMouseUp;
			break;
		case 15:
			MainScroll = (ScrollViewer)target;
			MainScroll.SizeChanged += OnMainScrollSizeChanged;
			MainScroll.ScrollChanged += OnScrollChanged;
			MainScroll.PreviewMouseWheel += OnPreviewMouseWheel;
			break;
		case 16:
			MainGrid = (Grid)target;
			MainGrid.MouseLeftButtonDown += OnMouseLeftDown;
			MainGrid.MouseLeftButtonUp += OnMouseLeftUp;
			MainGrid.MouseMove += OnMouseMove;
			break;
		case 17:
			TimelineBackground = (TimelineBackground)target;
			break;
		case 18:
			CellCanvas = (TimelineVirtualizedCanvas)target;
			break;
		case 19:
			BatchSelectBorder = (Border)target;
			break;
		case 20:
			TimelineFloating = (TimelineCellFloating)target;
			break;
		case 21:
			ArrangePanel = (TimelineArrange)target;
			break;
		case 22:
			GroupBorder = (Grid)target;
			break;
		case 23:
			((Thumb)target).DragDelta += OnResizeColumnDragDelta;
			break;
		case 24:
			ColumnScroll = (ScrollViewer)target;
			ColumnScroll.PreviewMouseWheel += OnPreviewMouseWheel;
			break;
		case 25:
			ToolTipControl = (TimelineToolTip)target;
			break;
		case 26:
			ColumnDragPopup = (Popup)target;
			break;
		case 27:
			ColumnDropLine = (Grid)target;
			break;
		case 28:
			BatchControl = (BatchTaskControl)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
