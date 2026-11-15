using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.DragAndDrop;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Timeline;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Views.Calendar.Month;

public class MultiWeekDayControl : Grid
{
	private static readonly SolidColorBrush _jpHolidayColor = ThemeUtil.GetColorInString("#FF0000");

	private static readonly SolidColorBrush _holidayColor = ThemeUtil.GetColorInString("#5DCA94");

	private bool _pressed;

	private MultiWeekControl _parent;

	private IToastShowWindow _parentWindow;

	private Path _path;

	private Image _image;

	private TextBlock _dateText;

	private TextBlock _lunarText;

	private Border _flashBorder;

	public MultiWeekDayControl()
	{
		_path = new Path
		{
			Height = 24.0,
			Margin = new Thickness(2.0, 2.0, 0.0, 0.0),
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Cursor = Cursors.Hand
		};
		_path.SetBinding(FrameworkElement.WidthProperty, "IconWidth");
		_path.MouseEnter += OnPathMouseOverChanged;
		_path.MouseLeave += OnPathMouseOverChanged;
		_path.PreviewMouseLeftButtonUp += OnDateClick;
		_path.SetBinding(Path.DataProperty, "EllipseGeometry");
		base.Children.Add(_path);
		_image = new Image
		{
			Width = 12.0,
			Height = 12.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top
		};
		base.Children.Add(_image);
		_dateText = new TextBlock
		{
			IsHitTestVisible = false,
			TextAlignment = TextAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Center
		};
		_dateText.SetResourceReference(TextBlock.FontSizeProperty, "MonthDayTextFontSize");
		Border border = new Border
		{
			IsHitTestVisible = false,
			Height = 24.0,
			Margin = new Thickness(2.0, 2.0, 0.0, 0.0),
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Child = _dateText
		};
		border.SetBinding(FrameworkElement.WidthProperty, "IconWidth");
		base.Children.Add(border);
		_lunarText = new TextBlock
		{
			FontSize = 11.0,
			IsHitTestVisible = false,
			Margin = new Thickness(30.0, 7.0, 8.0, 0.0),
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Top,
			TextAlignment = TextAlignment.Left
		};
		base.Children.Add(_lunarText);
		base.DataContextChanged += OnDataBind;
		base.MouseEnter += OnCellMouseEnter;
		base.MouseLeftButtonDown += OnCellLeftDown;
		base.MouseLeftButtonUp += OnCellLeftUp;
	}

	private void OnDateClick(object sender, MouseButtonEventArgs e)
	{
		CalendarControl calendarParent = GetCalendarParent();
		if (calendarParent == null || !calendarParent.IsLocked)
		{
			if (!PopupStateManager.CanStartMonthSelection())
			{
				e.Handled = true;
			}
			else if (base.DataContext is MonthDayViewModel monthDayViewModel)
			{
				e.Handled = true;
				Utils.FindParent<CalendarControl>(this)?.NavigateDate(monthDayViewModel.Date, CalendarViewType.Week);
			}
		}
	}

	private void OnPathMouseOverChanged(object sender, MouseEventArgs e)
	{
		if (base.DataContext is MonthDayViewModel { IsToday: false })
		{
			if (_path.IsMouseOver)
			{
				_path.SetResourceReference(Shape.FillProperty, "PrimaryColor10");
			}
			else
			{
				_path.Fill = Brushes.Transparent;
			}
		}
	}

	private void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (e.OldValue is MonthDayViewModel source)
		{
			PropertyChangedEventManager.RemoveHandler(source, OnPropertyChanged, "");
		}
		if (e.NewValue is MonthDayViewModel monthDayViewModel)
		{
			PropertyChangedEventManager.AddHandler(monthDayViewModel, OnPropertyChanged, "");
			SetData(monthDayViewModel);
		}
	}

	private void SetData(MonthDayViewModel model)
	{
		SetBackGround(model);
		if (model.IsToday)
		{
			_dateText.Foreground = Brushes.White;
			_path.SetResourceReference(Shape.FillProperty, "PrimaryColor");
		}
		else
		{
			_dateText.SetResourceReference(TextBlock.ForegroundProperty, model.IsCurrentMonth ? "BaseColorOpacity100_80" : "BaseColorOpacity40");
			if (_path.IsMouseOver)
			{
				_path.SetResourceReference(Shape.FillProperty, "PrimaryColor10");
			}
			else
			{
				_path.Fill = Brushes.Transparent;
			}
		}
		_dateText.Text = ((model.Date.Day == 1) ? DateUtils.FormatMonthDay(model.Date, DateFormatLen.Middle) : model.Date.Day.ToString());
		_lunarText.Text = DateUtils.GetLunarText(model.Date, LocalSettings.Settings.ShowWeek);
		_lunarText.Margin = new Thickness(model.IconWidth + 6.0, 7.0, 8.0, 0.0);
		if (HolidayManager.IsHolidayText(_lunarText.Text))
		{
			_lunarText.Foreground = (Utils.IsJp() ? _jpHolidayColor : _holidayColor);
		}
		else
		{
			_lunarText.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity40");
		}
		if (model.IsRestDay)
		{
			_image.SetResourceReference(Image.SourceProperty, "RestDrawingImage");
		}
		else if (model.IsWorkDay)
		{
			_image.SetResourceReference(Image.SourceProperty, "WorkDrawingImage");
		}
		else
		{
			_image.Source = null;
		}
		_image.Opacity = model.WorkRestOpacity;
		if (_image.Source != null)
		{
			_image.Margin = new Thickness(model.IconWidth - 3.0, 1.0, 0.0, 0.0);
		}
	}

	private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
		case "State":
			SetBackGround(base.DataContext as MonthDayViewModel);
			break;
		case "Date":
			SetData(base.DataContext as MonthDayViewModel);
			break;
		case "IsCurrentMonth":
			if (base.DataContext is MonthDayViewModel monthDayViewModel)
			{
				if (monthDayViewModel.IsToday)
				{
					_dateText.Foreground = Brushes.White;
				}
				else
				{
					_dateText.SetResourceReference(TextBlock.ForegroundProperty, monthDayViewModel.IsCurrentMonth ? "BaseColorOpacity100_80" : "BaseColorOpacity40");
				}
				_image.Opacity = monthDayViewModel.WorkRestOpacity;
			}
			break;
		}
	}

	private void SetBackGround(MonthDayViewModel model)
	{
		if (model == null || model.State == MonthCellState.Normal)
		{
			if (model != null && model.IsWeekend)
			{
				SetResourceReference(Panel.BackgroundProperty, "BaseColorOpacity2");
			}
			else
			{
				base.Background = Brushes.Transparent;
			}
		}
		else
		{
			SetResourceReference(Panel.BackgroundProperty, "BaseColorOpacity5");
		}
	}

	private void OnCellLeftDown(object sender, MouseButtonEventArgs e)
	{
		CalendarControl calendarParent = GetCalendarParent();
		if ((calendarParent == null || !calendarParent.IsLocked) && !_path.IsMouseOver)
		{
			if (!PopupStateManager.CanStartMonthSelection())
			{
				e.Handled = true;
				GetParent()?.ClearSelection();
			}
			else if (base.DataContext is MonthDayViewModel monthDayViewModel)
			{
				GetParent()?.SetFirstDate(monthDayViewModel.Date);
			}
		}
	}

	private void OnCellMouseEnter(object sender, MouseEventArgs e)
	{
		MultiWeekControl parent = GetParent();
		if (!PopupStateManager.CanStartMonthSelection())
		{
			e.Handled = true;
			return;
		}
		e.Handled = true;
		if (e.LeftButton == MouseButtonState.Pressed)
		{
			if (base.DataContext is MonthDayViewModel monthDayViewModel)
			{
				parent?.SetSelection(monthDayViewModel.Date, this);
			}
		}
		else
		{
			parent?.ClearSelection();
		}
	}

	private async void OnCellLeftUp(object sender, MouseButtonEventArgs e)
	{
		if (GetCalendarParent()?.IsLocked ?? false)
		{
			return;
		}
		if (!PopupStateManager.CanStartMonthSelection())
		{
			e.Handled = true;
			GetParent()?.ClearSelection();
			return;
		}
		MultiWeekControl parent = GetParent();
		if (parent != null)
		{
			KeyValuePair<DateTime, DateTime> selectDateSpan = parent.GetSelectDateSpan();
			DateTime key = selectDateSpan.Key;
			DateTime value = selectDateSpan.Value;
			ShowAddTaskPopup(e, key, value);
		}
		else if (_pressed)
		{
			_pressed = false;
			DateTime date = GetModel().Date;
			ShowAddTaskPopup(e, date, date);
		}
	}

	private MonthDayViewModel GetModel()
	{
		if (base.DataContext != null && base.DataContext is MonthDayViewModel result)
		{
			return result;
		}
		return null;
	}

	public void ShowAddTaskPopup(MouseEventArgs e, DateTime start, DateTime stop)
	{
		if (!PopupStateManager.CanShowAddPopup())
		{
			return;
		}
		if (e != null)
		{
			e.Handled = true;
		}
		CalendarControl calendarParent = GetCalendarParent();
		if ((calendarParent != null && calendarParent.ClearCopiedTaskSelection()) || (calendarParent != null && calendarParent.ExitBatchSelect()) || !(base.DataContext is MonthDayViewModel monthDayViewModel))
		{
			return;
		}
		if (Utils.IsEmptyDate(start))
		{
			start = monthDayViewModel.Date;
			stop = monthDayViewModel.Date;
		}
		monthDayViewModel.State = MonthCellState.Selected;
		TaskDetailViewModel taskDetailViewModel = TaskDetailViewModel.BuildInitModel(null);
		if (start == stop)
		{
			taskDetailViewModel.SourceViewModel.StartDate = start;
		}
		else
		{
			taskDetailViewModel.SourceViewModel.StartDate = start;
			taskDetailViewModel.SourceViewModel.DueDate = stop.Date.AddDays(1.0);
		}
		CalendarControl calendarControl = Utils.FindParent<CalendarControl>(this);
		string calendarDefaultAddProjectId = CalendarUtils.GetCalendarDefaultAddProjectId(calendarControl != null && calendarControl.ViewSourceType == CalendarViewSourceType.WithList);
		taskDetailViewModel.SourceViewModel.ProjectId = calendarDefaultAddProjectId;
		TaskDetailPopup taskDetailPopup = new TaskDetailPopup
		{
			IsCalendarContext = true
		};
		IToastShowWindow parentWindow = GetParentWindow();
		if (parentWindow != null)
		{
			if (PopupStateManager.LastTarget == this)
			{
				return;
			}
			taskDetailPopup.DependentWindow = parentWindow;
		}
		taskDetailPopup.Disappear -= OnDetailClosed;
		taskDetailPopup.Disappear += OnDetailClosed;
		taskDetailPopup.CalendarAddFlowClosed -= OnCalendarAddFlowClosed;
		taskDetailPopup.CalendarAddFlowClosed += OnCalendarAddFlowClosed;
		taskDetailPopup.TaskSaved += OnTaskAdded;
		taskDetailPopup.Show(taskDetailViewModel, string.Empty, new TaskWindowDisplayArgs(this, base.ActualWidth + 8.0, focus: false, 0)
		{
			TitleParseMode = TitleDateParseMode.TimeOnlyOnCurrentDate
		});
		GetParent()?.GetParent()?.SetEditting(editting: true);
	}

	private async void OnTaskAdded(object sender, string taskId)
	{
		if (sender is TaskDetailWindow taskDetailWindow)
		{
			taskDetailWindow.TaskSaved -= OnTaskAdded;
		}
		CalendarControl calendarControl = Utils.FindParent<CalendarControl>(this);
		string text = CalendarUtils.CheckTaskCanShown(taskId, calendarControl != null && calendarControl.ViewSourceType == CalendarViewSourceType.WithList);
		if (!string.IsNullOrEmpty(text))
		{
			GetParentWindow()?.TryToastString(null, text);
		}
	}

	private IToastShowWindow GetParentWindow()
	{
		_parentWindow = _parentWindow ?? Utils.FindParent<IToastShowWindow>(this);
		return _parentWindow;
	}

	private void OnDetailClosed(object sender, string e)
	{
		if (sender is TaskDetailPopup taskDetailPopup)
		{
			if (taskDetailPopup.DelayCalendarAddCleanup)
			{
				return;
			}
			taskDetailPopup.Disappear -= OnDetailClosed;
			taskDetailPopup.CalendarAddFlowClosed -= OnCalendarAddFlowClosed;
			taskDetailPopup.TaskSaved -= OnTaskAdded;
		}
		ResetAddPopupState(e);
	}

	private void OnCalendarAddFlowClosed(object sender, CalendarEventModel e)
	{
		string taskId = (sender as TaskDetailPopup)?.CalendarAddFlowTaskId;
		if (sender is TaskDetailPopup taskDetailPopup)
		{
			taskDetailPopup.CalendarAddFlowClosed -= OnCalendarAddFlowClosed;
			taskDetailPopup.Disappear -= OnDetailClosed;
			taskDetailPopup.TaskSaved -= OnTaskAdded;
		}
		ResetAddPopupState(taskId, forceRemoveTask: true);
		ShowCalendarEventHiddenToast(e);
	}

	private void ResetAddPopupState(string taskId = null, bool forceRemoveTask = false)
	{
		RemoveUnsavedTask(taskId, forceRemoveTask);
		if (base.DataContext != null && base.DataContext is MonthDayViewModel monthDayViewModel)
		{
			monthDayViewModel.State = MonthCellState.Normal;
		}
		GetParent()?.ClearSelection();
		GetParent()?.GetParent()?.SetEditting(editting: false);
	}

	private void RemoveUnsavedTask(string taskId, bool forceRemoveTask = false)
	{
		if (!string.IsNullOrEmpty(taskId) && (forceRemoveTask || !TaskCache.ExistTask(taskId)))
		{
			GetParent()?.RemoveTaskById(taskId);
		}
	}

	private void ShowCalendarEventHiddenToast(CalendarEventModel calEvent)
	{
		CalendarControl calendarControl = Utils.FindParent<CalendarControl>(this);
		string text = CalendarUtils.CheckCalendarEventCanShown(calEvent, calendarControl != null && calendarControl.ViewSourceType == CalendarViewSourceType.WithList);
		if (!string.IsNullOrEmpty(text))
		{
			GetParentWindow()?.TryToastString(null, text);
		}
	}

	private CalendarControl GetCalendarParent()
	{
		return Utils.FindParent<CalendarControl>(this);
	}

	private MultiWeekControl GetParent()
	{
		_parent = _parent ?? Utils.FindParent<MultiWeekControl>(this);
		return _parent;
	}

	public void ClearDropTarget()
	{
		MonthDayViewModel model = GetModel();
		if (model != null && model.State != MonthCellState.Normal)
		{
			model.State = MonthCellState.Normal;
		}
	}

	public void SetDropTarget()
	{
		MonthDayViewModel model = GetModel();
		if (model != null && model.State != MonthCellState.Drop)
		{
			model.State = MonthCellState.Drop;
		}
	}

	public bool IsDragHover(MouseEventArgs e)
	{
		Point position = e.GetPosition(this);
		double x = position.X;
		double y = position.Y;
		if (x > 0.0 && x < base.ActualWidth && y > 0.0)
		{
			return y < base.ActualHeight;
		}
		return false;
	}

	public async Task ShowLoadMore(List<CalendarDisplayViewModel> children, bool locked)
	{
		if (PopupStateManager.CanShowLoadMorePopup())
		{
			MonthDayViewModel model = GetModel();
			if (model != null)
			{
				LoadMoreWindow loadMoreWindow = new LoadMoreWindow(model.Date, this, isLastRow: true, GetWidgetThemeId(), Utils.FindParent<IToastShowWindow>(this), locked);
				loadMoreWindow.Width = base.ActualWidth + 20.0;
				loadMoreWindow.DragBarEvent = GetParent();
				loadMoreWindow.Owner = Window.GetWindow(this);
				loadMoreWindow.Disappear -= OnLoadMoreClosed;
				loadMoreWindow.Disappear += OnLoadMoreClosed;
				PopupStateManager.OnLoadMorePopupOpened();
				GetParent()?.GetParent()?.SetEditting(editting: true);
				loadMoreWindow.ShowPopup(children);
			}
		}
	}

	private string GetWidgetThemeId()
	{
		CalendarWidget calendarWidget = Utils.FindParent<CalendarWidget>(this);
		if (calendarWidget != null)
		{
			return calendarWidget.ThemeId;
		}
		return string.Empty;
	}

	private void OnLoadMoreClosed(object sender, EventArgs e)
	{
		GetParent()?.GetParent()?.SetEditting(editting: false);
		PopupStateManager.OnLoadMorePopupClosed();
	}

	public void ClearData()
	{
		base.DataContext = null;
		_parent = null;
		_parentWindow = null;
	}

	public async Task Flash(bool delay)
	{
		Border border = new Border
		{
			Opacity = 0.0,
			IsHitTestVisible = false
		};
		RemoveFlashBorder();
		border.SetResourceReference(Border.BackgroundProperty, "BaseColorOpacity10");
		border.SetValue(Panel.ZIndexProperty, -5);
		_flashBorder = border;
		base.Children.Add(_flashBorder);
		for (int i = 0; i < 10; i++)
		{
			await Task.Delay(delay ? 20 : 5);
			border.Opacity = (double)i / 10.0;
		}
		for (int i = 0; i < 25; i++)
		{
			await Task.Delay(20);
			border.Opacity = 1.0 - (double)i / 25.0;
		}
		base.Children.Remove(border);
		if (_flashBorder == border)
		{
			RemoveFlashBorder();
		}
	}

	private void RemoveFlashBorder()
	{
		base.Children.Remove(_flashBorder);
		_flashBorder = null;
	}

	public async Task OnDragDrop()
	{
		ClearDropTarget();
		object data = TickDragHelper.GetDragData();
		if (GetModel() == null || data == null)
		{
			return;
		}
		DateTime dropDate = GetModel().Date;
		bool needAddEvent = false;
		CalendarControl calendar = Utils.FindParent<CalendarControl>(this);
		object obj = data;
		if (!(obj is DisplayItemModel displayItemModel))
		{
			if (!(obj is TimelineCellViewModel timelineCellViewModel))
			{
				if (!(obj is CheckItemViewModel checkItemViewModel))
				{
					if (obj is CalendarDisplayViewModel calendarDisplayViewModel)
					{
						DateTime value = dropDate;
						DateTime? obj2 = (calendarDisplayViewModel.DragStartDate ?? calendarDisplayViewModel.StartDate)?.Date;
						int? num = (int?)(value - obj2)?.TotalDays;
						if (calendarDisplayViewModel.BatchSelected)
						{
							if (num == 0)
							{
								_parent?.ClearDragAfterDrop();
							}
							else
							{
								await calendar.OnMultiWeekBatchDragDrop(num.GetValueOrDefault());
							}
							return;
						}
						await calendarDisplayViewModel.SaveDate((!num.HasValue) ? dropDate : (calendarDisplayViewModel.DisplayStartDate?.AddDays(num.Value) ?? dropDate), Window.GetWindow(this));
					}
				}
				else if (UserDao.IsPro())
				{
					needAddEvent = true;
					await checkItemViewModel.SourceViewModel.SetDropDate(dropDate, Window.GetWindow(this));
				}
			}
			else
			{
				needAddEvent = true;
				if (timelineCellViewModel.BatchSelected)
				{
					List<TaskBaseViewModel> tasksByIds = TaskCache.GetTasksByIds(TickDragHelper.DraggingTaskIds?.ToList());
					foreach (TaskBaseViewModel item in tasksByIds)
					{
						await item.SetDropDate(dropDate, Window.GetWindow(this));
					}
				}
				else
				{
					timelineCellViewModel.DisplayModel.SetDropDate(dropDate, Window.GetWindow(this));
				}
			}
		}
		else
		{
			needAddEvent = true;
			if (!displayItemModel.BatchMode)
			{
				await displayItemModel.SourceViewModel.SetDropDate(dropDate, Window.GetWindow(this));
			}
			else
			{
				List<TaskBaseViewModel> tasksByIds2 = TaskCache.GetTasksByIds(displayItemModel.BatchSelectedIds);
				foreach (TaskBaseViewModel item2 in tasksByIds2)
				{
					await item2.SetDropDate(dropDate, Window.GetWindow(this));
				}
			}
		}
		calendar?.CheckItemFiltered(data);
		if (needAddEvent && (calendar?.ParallelShow ?? false))
		{
			string ctype = ((calendar.ViewSourceType == CalendarViewSourceType.WithList) ? "task+calendar" : "calendar+task");
			UserActCollectUtils.AddClickEvent("split_view", ctype, "drag_to_calendar");
		}
	}

	public void OnDragLeave()
	{
		ClearDropTarget();
	}

	public void OnDragEnter()
	{
		object dragData = TickDragHelper.GetDragData();
		if (!(dragData is DisplayItemModel) && !(dragData is TimelineCellViewModel))
		{
			if (dragData is CalendarDisplayViewModel model)
			{
				TickDragHelper.ChangeDragContent(dragData, base.ActualWidth);
				OnTaskDragOver(model);
			}
		}
		else
		{
			TickDragHelper.ChangeDragContent(2, base.ActualWidth);
			SetDropTarget();
			TickDragHelper.SetDragOverCalendar();
		}
	}

	private void OnTaskDragOver(CalendarDisplayViewModel model)
	{
		if (!model.DragStartDate.HasValue)
		{
			model.DragStartDate = GetModel().Date;
		}
		if (model.BatchSelected)
		{
			GetParent()?.SetDragBar(GetModel().Date, model);
			return;
		}
		DateTime date = GetModel().Date;
		DateTime? dragStartDate = model.DragStartDate;
		int num = (date - dragStartDate)?.Days ?? 0;
		DateTime start = model.DisplayStartDate?.AddDays(num).Date ?? GetModel().Date;
		DateTime end = model.DisplayDueDate?.AddMinutes(-1.0).AddDays(num).Date ?? GetModel().Date;
		GetParent().SetDropTargets(start, end);
	}

	public DateTime? GetDate()
	{
		return GetModel()?.Date;
	}

	public void HideDateText()
	{
		_image.Visibility = Visibility.Collapsed;
		_path.Opacity = 0.0;
		_dateText.Opacity = 0.0;
		_lunarText.Opacity = 0.0;
	}

	public void ShowDateText()
	{
		_image.Visibility = Visibility.Visible;
		_path.Opacity = 1.0;
		_dateText.Opacity = 1.0;
		_lunarText.Opacity = 1.0;
	}
}
