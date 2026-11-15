using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.DragAndDrop;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar.Month;
using ticktick_WPF.Views.Timeline;

namespace ticktick_WPF.Views.Calendar.Week;

public class MultiDayAllDayGrid : Grid, IMultiDayGrid
{
	private static readonly SolidColorBrush _jpHolidayColor = ThemeUtil.GetColorInString("#FF0000");

	private static readonly SolidColorBrush _holidayColor = ThemeUtil.GetColorInString("#5DCA94");

	private Path _path;

	private Image _image;

	private TextBlock _dateText;

	private TextBlock _lunarText;

	private Border _backBorder;

	private TextBlock _weekDayText;

	public double Offset;

	private bool _dragOver;

	public bool IsDateMouseOver
	{
		get
		{
			if (_path != null)
			{
				return _path.IsMouseOver;
			}
			return false;
		}
	}

	public MultiDayAllDayGrid()
	{
		base.Background = Brushes.Transparent;
		_backBorder = new Border
		{
			Margin = new Thickness(0.0, 32.0, 0.0, 0.0),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch
		};
		base.Children.Add(_backBorder);
		_path = new Path
		{
			Width = 24.0,
			Height = 24.0,
			Margin = new Thickness(4.0, 36.0, 0.0, 0.0),
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Cursor = Cursors.Hand
		};
		_path.MouseEnter += OnPathMouseOverChanged;
		_path.MouseLeave += OnPathMouseOverChanged;
		_path.MouseLeftButtonUp += OnDayTextClick;
		_path.SetBinding(Path.DataProperty, "EllipseGeometry");
		base.Children.Add(_path);
		_image = new Image
		{
			Width = 12.0,
			Height = 12.0,
			Margin = new Thickness(23.0, 35.0, 0.0, 0.0),
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top
		};
		base.Children.Add(_image);
		_dateText = new TextBlock
		{
			IsHitTestVisible = false,
			Width = 24.0,
			TextAlignment = TextAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top
		};
		_dateText.SetResourceReference(TextBlock.FontSizeProperty, "MonthDayTextFontSize");
		_dateText.SetResourceReference(FrameworkElement.MarginProperty, "WeekDayTextMargin");
		base.Children.Add(_dateText);
		_weekDayText = new TextBlock
		{
			FontSize = 12.0,
			IsHitTestVisible = false,
			Margin = new Thickness(0.0, 9.0, 0.0, 0.0),
			TextAlignment = TextAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Top
		};
		_weekDayText.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity60");
		base.Children.Add(_weekDayText);
		_lunarText = new TextBlock
		{
			FontSize = 12.0,
			IsHitTestVisible = false,
			Margin = new Thickness(30.0, 40.0, 12.0, 0.0),
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Top,
			TextAlignment = TextAlignment.Left
		};
		base.Children.Add(_lunarText);
		base.DataContextChanged += OnDataChanged;
	}

	private void OnDataChanged(object sender, DependencyPropertyChangedEventArgs e)
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
		_dateText.Text = model.Date.Day.ToString();
		_weekDayText.Text = model.Date.ToString("ddd", App.Ci);
		_lunarText.Text = DateUtils.GetLunarText(model.Date, showWeek: false);
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
			if (base.DataContext is MonthDayViewModel { IsToday: false } monthDayViewModel)
			{
				_dateText.SetResourceReference(TextBlock.ForegroundProperty, monthDayViewModel.IsCurrentMonth ? "BaseColorOpacity100_80" : "BaseColorOpacity40");
			}
			break;
		}
	}

	private void SetBackGround(MonthDayViewModel model)
	{
		if (model == null || model.State == MonthCellState.Normal)
		{
			_backBorder.Background = Brushes.Transparent;
		}
		else
		{
			_backBorder.SetResourceReference(Panel.BackgroundProperty, "BaseColorOpacity5");
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

	public DateTime GetDate()
	{
		if (base.DataContext is MonthDayViewModel monthDayViewModel)
		{
			return monthDayViewModel.Date;
		}
		return DateTime.Today;
	}

	private void OnDayTextClick(object sender, MouseButtonEventArgs e)
	{
		CalendarControl calendarControl = Utils.FindParent<CalendarControl>(this);
		if (calendarControl == null || !calendarControl.IsLocked)
		{
			e.Handled = true;
			if (base.DataContext != null && base.DataContext is MonthDayViewModel monthDayViewModel)
			{
				Utils.FindParent<CalendarControl>(this)?.NavigateDate(monthDayViewModel.Date, CalendarViewType.Day);
			}
		}
	}

	public void SetMonth(DateTime month)
	{
		GetModel()?.SetCurrentMonth(month);
	}

	private MonthDayViewModel GetModel()
	{
		if (base.DataContext != null && base.DataContext is MonthDayViewModel result)
		{
			return result;
		}
		return null;
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

	public async void OnDragDrop()
	{
		object data = TickDragHelper.GetDragData();
		if (GetModel() == null || data == null)
		{
			return;
		}
		DateTime dropDate = GetModel().Date;
		bool needAddEvent = false;
		object obj = data;
		if (!(obj is DisplayItemModel displayItemModel))
		{
			if (!(obj is CheckItemViewModel checkItemViewModel))
			{
				if (!(obj is TimelineCellViewModel timelineCellViewModel))
				{
					if (!(obj is TaskCellViewModel taskCellViewModel))
					{
						if (obj is CalendarDisplayViewModel calendarDisplayViewModel)
						{
							DateTime value = dropDate;
							DateTime? obj2 = (calendarDisplayViewModel.DragStartDate ?? calendarDisplayViewModel.StartDate)?.Date;
							int? num = (int?)(value - obj2)?.TotalDays;
							await calendarDisplayViewModel.SaveDate((!num.HasValue) ? dropDate : (calendarDisplayViewModel.DisplayStartDate?.AddDays(num.Value) ?? dropDate), Window.GetWindow(this));
						}
					}
					else
					{
						taskCellViewModel.SaveDate(dropDate, Window.GetWindow(this), toAllDay: true);
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
							await item.SetDropDate(dropDate, Window.GetWindow(this), toAllDay: true);
						}
					}
					else
					{
						timelineCellViewModel.DisplayModel.SetDropDate(dropDate, Window.GetWindow(this), toAllDay: true);
					}
				}
			}
			else if (UserDao.IsPro())
			{
				needAddEvent = true;
				checkItemViewModel.SourceViewModel.SetDropDate(dropDate, Window.GetWindow(this), toAllDay: true);
			}
		}
		else
		{
			needAddEvent = true;
			if (displayItemModel.BatchMode)
			{
				List<TaskBaseViewModel> tasksByIds2 = TaskCache.GetTasksByIds(displayItemModel.BatchSelectedIds);
				foreach (TaskBaseViewModel item2 in tasksByIds2)
				{
					await item2.SetDropDate(dropDate, Window.GetWindow(this), toAllDay: true);
				}
			}
			else
			{
				displayItemModel.SourceViewModel.SetDropDate(dropDate, Window.GetWindow(this), toAllDay: true);
			}
		}
		if (needAddEvent)
		{
			CalendarControl calendarControl = Utils.FindParent<CalendarControl>(this);
			if (calendarControl != null && calendarControl.ParallelShow)
			{
				string ctype = ((calendarControl.ViewSourceType == CalendarViewSourceType.WithList) ? "task+calendar" : "calendar+task");
				UserActCollectUtils.AddClickEvent("split_view", ctype, "drag_to_calendar");
			}
		}
		Utils.FindParent<CalendarControl>(this)?.CheckItemFiltered(data);
	}

	public void OnDragLeave()
	{
		ClearDropTarget();
	}

	public void OnDragEnter()
	{
		HandleDragOver();
	}

	public void OnDragOver(MouseEventArgs e)
	{
	}

	private void HandleDragOver()
	{
		object dragData = TickDragHelper.GetDragData();
		if (!(dragData is DisplayItemModel) && !(dragData is TimelineCellViewModel))
		{
			if (!(dragData is TaskCellViewModel taskCellViewModel))
			{
				if (dragData is CalendarDisplayViewModel { DragStartDate: var dragStartDate } calendarDisplayViewModel)
				{
					if (!dragStartDate.HasValue)
					{
						calendarDisplayViewModel.DragStartDate = GetModel().Date;
					}
					TickDragHelper.ChangeDragContent(dragData, base.ActualWidth);
					DateTime? dateTime = calendarDisplayViewModel.DisplayStartDate ?? calendarDisplayViewModel.SourceViewModel.CompletedTime;
					if (calendarDisplayViewModel.BatchSelected && dateTime.HasValue)
					{
						int num = (int)(GetModel().Date - dateTime.Value.Date).TotalDays - (int)(calendarDisplayViewModel.DragStartDate.Value.Date - dateTime.Value.Date).TotalDays;
						Utils.FindParent<MultiDayView>(this)?.OnDragDiffChanged(TimeSpan.FromDays(num), num);
						return;
					}
					if (!dateTime.HasValue)
					{
						SetDropTarget();
						return;
					}
					int num2 = (int)(GetModel().Date - dateTime.Value.Date).TotalDays - (int)(calendarDisplayViewModel.DragStartDate.Value.Date - dateTime.Value.Date).TotalDays;
					DateTime start = dateTime?.AddDays(num2).Date ?? GetModel().Date;
					DateTime end = calendarDisplayViewModel.DisplayDueDate?.AddMinutes(-1.0).AddDays(num2).Date ?? GetModel().Date;
					Utils.FindParent<MultiDayAllDayView>(this)?.SetDropTargets(start, end);
				}
			}
			else
			{
				TickDragHelper.ChangeDragContent(2, base.ActualWidth);
				if (taskCellViewModel.BatchSelected && taskCellViewModel.DisplayStartDate.HasValue)
				{
					int num3 = (int)(GetModel().Date - taskCellViewModel.DisplayStartDate.Value.Date).TotalDays;
					Utils.FindParent<MultiDayView>(this)?.OnDragDiffChanged(TimeSpan.FromDays(num3), num3, toAllDay: true);
				}
				else
				{
					SetDropTarget();
				}
			}
		}
		else
		{
			SetDropTarget();
			TickDragHelper.SetDragOverCalendar();
			TickDragHelper.ChangeDragContent(2, base.ActualWidth);
		}
	}

	public void CheckDragging()
	{
		Point position = Mouse.GetPosition(this);
		if (position.X > 0.0 && position.X < base.ActualWidth && position.Y > 0.0 && position.Y < base.ActualHeight)
		{
			HandleDragOver();
		}
	}
}
