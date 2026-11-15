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
using Emoji.Wpf;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.DragAndDrop;
using ticktick_WPF.Util.KotlinUtils;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar.Week;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Habit;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Pomo;
using ticktick_WPF.Views.Tag;
using ticktick_WPF.Views.TaskList;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Views.Calendar;

public class TaskCell : Canvas, IShowTaskDetailWindow
{
	private Point _cellDragStart = new Point(0.0, 0.0);

	private bool _draggingBottomHandle;

	private bool _draggingTopHandle;

	private IToastShowWindow _parentWindow;

	private bool _press;

	private string _taskId;

	private DateTime? _dragStartDate;

	private DateTime? _dragDueDate;

	private bool _isWaitingDoubleClick;

	private DateTime _lastClickTime;

	private Point _firstClickPoint;

	private bool _detailShowing;

	private Path _icon;

	private Border _iconBorder;

	private EmjTextBlock _text;

	private TextBlock _timeText;

	private Border _dragBorder;

	private bool _isMoveCell;

	private bool _showIcon;

	private CalendarViewSourceType? ViewSourceType { get; set; }

	public TaskCell(TaskCellViewModel model, bool isMove = false, bool showIcon = false)
	{
		_isMoveCell = isMove;
		_showIcon = showIcon;
		base.DataContext = model;
		SetEvent();
		base.VerticalAlignment = VerticalAlignment.Top;
		base.HorizontalAlignment = HorizontalAlignment.Left;
		base.Cursor = Cursors.Hand;
		base.UseLayoutRounding = true;
		base.Background = Brushes.Transparent;
		SetBinding(FrameworkElement.WidthProperty, "Width");
		SetBinding(FrameworkElement.HeightProperty, "Height");
		SetBinding(FrameworkElement.MarginProperty, "Margin");
		SetBinding(UIElement.OpacityProperty, "Opacity");
		base.ClipToBounds = true;
		model?.SetTaskCell(this);
		model?.SetSourceModel(model.SourceViewModel, setEvent: true);
		SetChildren(model);
		base.Loaded += delegate
		{
			PropertyChangedEventManager.AddHandler(StaticPropertiesModel.Instance, OnDraggingTaskChanged, "DragOverCalendar");
		};
		base.Unloaded += delegate
		{
			PropertyChangedEventManager.RemoveHandler(StaticPropertiesModel.Instance, OnDraggingTaskChanged, "DragOverCalendar");
		};
	}

	private void OnDraggingTaskChanged(object sender, PropertyChangedEventArgs e)
	{
		if (base.DataContext is CalendarDisplayViewModel calendarDisplayViewModel && !(TickDragHelper.DragData is CalendarDisplayViewModel) && !_isMoveCell)
		{
			if (calendarDisplayViewModel.RepeatDiff == 0.0 && TickDragHelper.DragOverCalendar)
			{
				calendarDisplayViewModel.Dragging = TickDragHelper.DraggingTaskIds?.Contains(calendarDisplayViewModel.TaskId) ?? false;
			}
			else if (calendarDisplayViewModel.Dragging && !TickDragHelper.DragOverCalendar)
			{
				calendarDisplayViewModel.Dragging = false;
			}
		}
	}

	private async void SetChildren(TaskCellViewModel model)
	{
		if (model?.SourceViewModel == null)
		{
			SetDragHandler(showDrag: false);
			base.Visibility = Visibility.Collapsed;
			return;
		}
		bool theme = (bool)FindResource("IsDarkTheme");
		model.SetTheme(theme);
		model.SetTimeText(inArrange: false, isSpan: true);
		double cellTextFontSize = CalendarGeoHelper.GetCellTextFontSize((int)model.Height);
		bool flag = model.Height >= (double)((int)(cellTextFontSize * 1.4) + (int)(CalendarGeoHelper.CellTimeFontSize * 1.3)) + CalendarGeoHelper.TaskCellTextTop || string.IsNullOrEmpty(model.Title);
		SetIcon(model, flag, cellTextFontSize);
		SetTitle(model, cellTextFontSize);
		SetTimeText(model, flag, cellTextFontSize);
		SetDragHandler(!model.ShowInFocus && model.ShowDragHandle);
	}

	private void SetDragHandler(bool showDrag)
	{
		if (showDrag && _dragBorder == null)
		{
			_dragBorder = new Border
			{
				Cursor = Cursors.SizeNS
			};
			_dragBorder.SetBinding(FrameworkElement.WidthProperty, "Width");
			_dragBorder.SetBinding(FrameworkElement.HeightProperty, "Height");
			_dragBorder.BorderThickness = new Thickness(0.0, 3.0, 0.0, 3.0);
			_dragBorder.BorderBrush = Brushes.Transparent;
			_dragBorder.PreviewMouseLeftButtonDown += OnHandleMouseDown;
			base.Children.Add(_dragBorder);
		}
		else if (!showDrag && _dragBorder != null)
		{
			base.Children.Remove(_dragBorder);
			_dragBorder = null;
		}
	}

	private void SetTitle(TaskCellViewModel model, double fontSize)
	{
		if (_text == null)
		{
			_text = new EmjTextBlock
			{
				VerticalAlignment = VerticalAlignment.Top,
				IsHitTestVisible = false
			};
			_text.SetBinding(TextBlock.ForegroundProperty, "TitleColor");
			_text.TextWrapping = TextWrapping.Wrap;
			_text.SizeChanged += OnTitleSizeChanged;
			base.Children.Add(_text);
		}
		_text.FontSize = fontSize;
		int num = (int)(_text.FontSize * 1.4);
		_text.LineHeight = num;
		int num2 = Math.Max(4, Math.Max(1, (int)(model.Height / (double)num)) * (int)model.Width / 3);
		string text = TaskUtils.EscapeLinkContent(model.Title) ?? string.Empty;
		if (text.Length > num2)
		{
			text = text.Substring(0, num2);
		}
		_text.Text = text;
		bool flag = _icon != null && (model.IsConfigShowIcon() || StaticPropertiesModel.Instance.AltKeyDown || !model.IsDerivative);
		_text.Width = Math.Max(0.0, model.Width - (flag ? (fontSize + 12.0) : 8.0));
		Canvas.SetLeft(_text, (flag ? (fontSize + 7.0) : 5.5) + (double)CalendarGeoHelper.TaskTitleLeft);
		double num3 = Math.Min(CalendarGeoHelper.TaskCellTextTop, (model.Height - (double)num - 1.0) / 2.0);
		Canvas.SetTop(_text, num3);
		double num4 = (model.Height - CalendarGeoHelper.CellTimeFontSize * 1.3 - 2.0 - num3) / (double)num;
		if (model.Height >= 48.0 && !string.IsNullOrEmpty(model.Course?.Room))
		{
			num4 -= 1.0;
		}
		_text.TextWrapping = ((!(num4 > 1.0)) ? TextWrapping.NoWrap : TextWrapping.Wrap);
		_text.MaxHeight = Math.Max((model.Height > (double)(num + 1)) ? (num + 1) : num, (int)num4 * num + 1);
	}

	private void OnTitleSizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (_timeText != null && _text != null && base.DataContext is TaskCellViewModel taskCellViewModel)
		{
			double fontSize = _text.FontSize;
			double num = Math.Min(CalendarGeoHelper.CellTimeFontSize, fontSize);
			_timeText.FontSize = num;
			int num2 = (int)(num * 1.4);
			_timeText.LineHeight = num;
			Canvas.SetLeft(_timeText, ((_icon != null) ? (fontSize + 7.0) : 4.0) + (double)CalendarGeoHelper.TaskTitleLeft);
			double num3 = Math.Min(CalendarGeoHelper.TaskCellTimeTextTop, (taskCellViewModel.Height - (double)num2) / 2.0);
			if (!string.IsNullOrEmpty(_text.Text))
			{
				num3 = ((!(_text.MaxHeight > _text.ActualHeight)) ? (_text.MaxHeight + CalendarGeoHelper.TaskCellTimeTextTop) : (_text.ActualHeight + CalendarGeoHelper.TaskCellTimeTextTop));
			}
			if (num3 > (double)num2 && num3 + (double)num2 + CalendarGeoHelper.TaskCellTextTop > taskCellViewModel.Height)
			{
				num3 = taskCellViewModel.Height - (double)num2 - CalendarGeoHelper.TaskCellTextTop;
			}
			Canvas.SetTop(_timeText, num3);
			if (!taskCellViewModel.IsConfigShowIcon() && !StaticPropertiesModel.Instance.AltKeyDown && taskCellViewModel.IsDerivative && _iconBorder != null)
			{
				Canvas.SetTop(_iconBorder, num3);
			}
		}
	}

	private void SetIcon(TaskCellViewModel model, bool showRepeat, double fontSize)
	{
		fontSize += 2.0;
		bool flag = model.ShowIcon && model.Width >= 11.0 && (model.ForceShowIcon || _showIcon);
		if (flag && !showRepeat && !model.IsConfigShowIcon() && !StaticPropertiesModel.Instance.AltKeyDown && model.IsDerivative)
		{
			flag = false;
		}
		if (flag && _iconBorder == null)
		{
			_iconBorder = new Border
			{
				Background = Brushes.Transparent
			};
			_icon = new Path
			{
				Width = fontSize,
				Height = fontSize,
				Stretch = Stretch.Uniform,
				Margin = new Thickness(4.0, 0.0, 0.0, 0.0),
				VerticalAlignment = VerticalAlignment.Center
			};
			_icon.SetBinding(Path.DataProperty, "Icon");
			_iconBorder.Child = _icon;
			base.Children.Add(_iconBorder);
		}
		else if (!flag && _iconBorder != null)
		{
			base.Children.Remove(_iconBorder);
			_iconBorder = null;
			_icon = null;
		}
		if (_iconBorder != null)
		{
			if (_icon != null)
			{
				_icon.Width = fontSize;
				_icon.Height = fontSize;
			}
			_icon?.SetBinding(Shape.FillProperty, "IconColor");
			_iconBorder.Opacity = 1.0;
			Canvas.SetLeft(_iconBorder, ((model.Width > fontSize) ? 0.0 : ((model.Width >= fontSize - 1.0) ? (model.Width - fontSize) : 0.0)) + (double)CalendarGeoHelper.TaskTitleLeft);
			Canvas.SetTop(_iconBorder, (model.Height > fontSize + CalendarGeoHelper.TaskCellIconTop * 2.0) ? CalendarGeoHelper.TaskCellIconTop : ((model.Height - fontSize) / 2.0 - 0.5));
		}
	}

	private bool OnIconClick(MouseButtonEventArgs e)
	{
		if (!(base.DataContext is CalendarDisplayViewModel calendarDisplayViewModel))
		{
			return false;
		}
		if (calendarDisplayViewModel.IsConfigShowIcon() || StaticPropertiesModel.Instance.AltKeyDown)
		{
			DisplayType type = calendarDisplayViewModel.Type;
			if ((uint)type <= 3u)
			{
				calendarDisplayViewModel.OnCheckBoxClick(GetToastWindow());
				return true;
			}
		}
		return false;
	}

	private void SetTimeText(TaskCellViewModel model, bool showTime, double fontSize)
	{
		double num = Math.Min(CalendarGeoHelper.CellTimeFontSize, fontSize);
		if (showTime)
		{
			model.SetTimeText(inArrange: false, isSpan: true);
		}
		if (showTime && _timeText == null)
		{
			_timeText = new TextBlock
			{
				FontSize = 11.0,
				LineHeight = 14.0,
				IsHitTestVisible = false
			};
			_timeText.SetBinding(TextBlock.ForegroundProperty, "TimeColor");
			_timeText.SetBinding(TextBlock.TextProperty, "TimeText");
			_timeText.Opacity = 0.8;
			_timeText.SetResourceReference(FrameworkElement.MarginProperty, "TaskCellTimeMargin");
			base.Children.Add(_timeText);
		}
		else if (!showTime && _timeText != null)
		{
			base.Children.Remove(_timeText);
			_timeText = null;
		}
		if (_timeText != null)
		{
			_timeText.FontSize = num;
			int num2 = (int)(num * 1.4);
			_timeText.LineHeight = num;
			Canvas.SetLeft(_timeText, ((_icon != null) ? (fontSize + 7.0) : 4.0) + (double)CalendarGeoHelper.TaskTitleLeft);
			double num3 = Math.Min(CalendarGeoHelper.TaskCellTimeTextTop, (model.Height - (double)num2) / 2.0);
			if (!string.IsNullOrEmpty(model.Title))
			{
				num3 = ((!(_text.MaxHeight > _text.ActualHeight)) ? (_text.MaxHeight + CalendarGeoHelper.TaskCellTimeTextTop) : (_text.ActualHeight + CalendarGeoHelper.TaskCellTimeTextTop));
			}
			if (model.Height >= (double)(num2 * 3) + CalendarGeoHelper.TaskCellTextTop * 2.0 && !string.IsNullOrEmpty(model.Course?.Room))
			{
				model.TimeText = model.TimeText + "\n" + model.Course.Room;
			}
			if (num3 > (double)num2 && num3 + (double)num2 + CalendarGeoHelper.TaskCellTextTop > model.Height)
			{
				num3 = model.Height - (double)num2 - CalendarGeoHelper.TaskCellTextTop;
			}
			Canvas.SetTop(_timeText, num3);
			if (!model.IsConfigShowIcon() && !StaticPropertiesModel.Instance.AltKeyDown && model.IsDerivative && _iconBorder != null)
			{
				Canvas.SetTop(_iconBorder, num3);
			}
			_timeText.Width = Math.Max(0.0, model.Width - ((_icon != null) ? (12.0 + _icon.Width) : 8.0));
		}
	}

	private void SetEvent()
	{
		base.PreviewMouseLeftButtonUp += OnCellClick;
		base.MouseLeftButtonDown += OnCellMouseDown;
		base.MouseRightButtonUp += OnShowOperation;
		base.DataContextChanged += OnDataBind;
	}

	private void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		(e.OldValue as TaskCellViewModel)?.SetTaskCell(null);
		if (e.NewValue is TaskCellViewModel taskCellViewModel)
		{
			taskCellViewModel.SetTaskCell(this);
			SetChildren(taskCellViewModel);
			taskCellViewModel.SetSourceModel(taskCellViewModel.SourceViewModel, setEvent: true);
		}
	}

	public void OnTaskPropertyChanged(string propertyName)
	{
		if (propertyName == null)
		{
			return;
		}
		switch (propertyName.Length)
		{
		case 5:
			switch (propertyName[0])
			{
			case 'W':
				if (!(propertyName == "Width"))
				{
					return;
				}
				break;
			case 'T':
				if (!(propertyName == "Title"))
				{
					return;
				}
				break;
			default:
				return;
			}
			goto IL_00de;
		case 6:
			if (!(propertyName == "Height"))
			{
				break;
			}
			goto IL_00de;
		case 4:
			if (!(propertyName == "Icon"))
			{
				break;
			}
			goto IL_00de;
		case 12:
			if (!(propertyName == "IsDerivative"))
			{
				break;
			}
			goto IL_00de;
		case 9:
			if (!(propertyName == "BackColor"))
			{
				break;
			}
			goto IL_00f0;
		case 15:
			if (!(propertyName == "BackBorderColor"))
			{
				break;
			}
			goto IL_00f0;
		case 19:
			if (!(propertyName == "BackBorderThickness"))
			{
				break;
			}
			goto IL_00f0;
		case 8:
			{
				if (propertyName == "Dragging" && base.DataContext is TaskCellViewModel taskCellViewModel && _iconBorder != null)
				{
					_iconBorder.Opacity = ((taskCellViewModel.Status == 0 || !taskCellViewModel._inDark) ? ((taskCellViewModel.Status == 0 || !taskCellViewModel.IsTaskOrNote) ? 0.6 : 0.3) : ((!taskCellViewModel.IsTaskOrNote) ? 1.0 : 0.7));
				}
				break;
			}
			IL_00de:
			SetChildren(base.DataContext as TaskCellViewModel);
			break;
			IL_00f0:
			InvalidateVisual();
			break;
		}
	}

	protected override void OnRender(DrawingContext dc)
	{
		base.OnRender(dc);
		if (!(base.DataContext is TaskCellViewModel { Width: >1.0, Height: >1.0 } taskCellViewModel))
		{
			return;
		}
		double width = Math.Max(0.0, taskCellViewModel.Width - 1.0);
		double height = Math.Max(0.0, taskCellViewModel.Height - 1.0);
		if (taskCellViewModel.BatchSelected)
		{
			dc.DrawRoundedRectangle(ThemeUtil.GetColor(taskCellViewModel._inDark ? "DarkBrush" : "LightBrush"), null, new Rect(0.0, 0.0, width, height), 4.0, 4.0);
		}
		LocalSettings settings = LocalSettings.Settings;
		if (settings != null && settings.UserPreference?.CalendarConfig?.ItemStyleType == CalendarConfig.CalendarItemStyle.Modern)
		{
			if (!ViewSourceType.HasValue)
			{
				ViewSourceType = Utils.FindParent<CalendarControl>(this)?.ViewSourceType;
			}
			if (ViewSourceType != CalendarViewSourceType.Widget)
			{
				SolidColorBrush brush = (taskCellViewModel._inDark ? new SolidColorBrush(ThemeUtil.GetColorValue("black"))
				{
					Opacity = 0.8
				} : new SolidColorBrush(ThemeUtil.GetColorValue("white"))
				{
					Opacity = 0.6
				});
				dc.DrawRoundedRectangle(brush, null, new Rect(0.0, 0.0, width, height), 4.0, 4.0);
			}
		}
		dc.DrawRoundedRectangle(taskCellViewModel.BackColor, null, new Rect(0.0, 0.0, width, height), 4.0, 4.0);
		LocalSettings settings2 = LocalSettings.Settings;
		if (settings2 != null && settings2.UserPreference?.CalendarConfig?.ItemStyleType == CalendarConfig.CalendarItemStyle.Modern && taskCellViewModel.BackBorderThickness.Top <= 0.0)
		{
			PathGeometry pathGeometry = new PathGeometry();
			Point startPoint = new Point(0.0, 4.0);
			PathFigure pathFigure = new PathFigure
			{
				StartPoint = startPoint
			};
			pathFigure.Segments.Add(new ArcSegment
			{
				Point = new Point(2.0, 1.0),
				Size = new Size(8.0, 8.0),
				SweepDirection = SweepDirection.Clockwise
			});
			pathFigure.Segments.Add(new LineSegment
			{
				Point = new Point(2.0, taskCellViewModel.Height - 1.0)
			});
			pathFigure.Segments.Add(new ArcSegment
			{
				Point = new Point(0.0, taskCellViewModel.Height - 4.0),
				Size = new Size(8.0, 8.0),
				SweepDirection = SweepDirection.Clockwise
			});
			pathGeometry.Figures.Add(pathFigure);
			dc.DrawGeometry(taskCellViewModel.LeftRectColor, new Pen(), pathGeometry);
		}
		if (taskCellViewModel.BackBorderThickness.Top > 0.0)
		{
			width = Math.Max(0.0, taskCellViewModel.Width - 2.0);
			height = Math.Max(0.0, taskCellViewModel.Height - 2.0);
			dc.DrawRoundedRectangle(null, taskCellViewModel.Dragging ? null : new Pen(taskCellViewModel.BackBorderColor, taskCellViewModel.BackBorderThickness.Top), new Rect(1.0, 1.0, width, height), 4.0, 4.0);
		}
	}

	private void InitEvents()
	{
		Window parentWindow = Utils.GetParentWindow(this);
		if (parentWindow != null)
		{
			parentWindow.CaptureMouse();
			parentWindow.PreviewMouseMove -= OnWeekDayCellMove;
			parentWindow.PreviewMouseMove += OnWeekDayCellMove;
			parentWindow.PreviewMouseLeftButtonUp -= StopDraggingHandle;
			parentWindow.PreviewMouseLeftButtonUp += StopDraggingHandle;
		}
	}

	private async void StopDraggingHandle(object sender, MouseButtonEventArgs e)
	{
		if (sender is Window window)
		{
			GetDragCell()?.SetCellHandling(value: false);
			window.ReleaseMouseCapture();
			window.PreviewMouseMove -= OnWeekDayCellMove;
			window.PreviewMouseLeftButtonUp -= StopDraggingHandle;
			await OnHandleDrop(_dragStartDate, _dragDueDate, _draggingBottomHandle);
			_press = false;
			_draggingBottomHandle = false;
			_draggingTopHandle = false;
			_dragDueDate = null;
			_dragStartDate = null;
			Mouse.OverrideCursor = null;
		}
	}

	private void OnWeekDayCellMove(object sender, MouseEventArgs e)
	{
		CalendarControl calendarParent = GetCalendarParent();
		if (calendarParent == null || !calendarParent.IsLocked)
		{
			TryMoveHandle(e);
		}
	}

	private void OnCellMouseDown(object sender, MouseButtonEventArgs e)
	{
		CalendarControl calendarParent = GetCalendarParent();
		if (calendarParent == null || !calendarParent.IsLocked)
		{
			base.MouseMove -= OnCellMove;
			base.MouseMove += OnCellMove;
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				e.Handled = true;
				_cellDragStart = e.GetPosition(this);
				UtilLog.Info("TCDS:" + _cellDragStart.X + "," + _cellDragStart.Y);
				_press = true;
			}
		}
	}

	private void OnHandleMouseDown(object sender, MouseButtonEventArgs e)
	{
		CalendarControl calendarParent = GetCalendarParent();
		if ((calendarParent == null || !calendarParent.IsLocked) && e.LeftButton == MouseButtonState.Pressed && base.DataContext is TaskCellViewModel taskCellViewModel)
		{
			e.Handled = true;
			if (sender is Border relativeTo)
			{
				Point position = e.GetPosition(relativeTo);
				_draggingBottomHandle = position.Y > 4.0;
				_draggingTopHandle = position.Y <= 4.0;
				_dragStartDate = taskCellViewModel.DisplayStartDate;
				_dragDueDate = taskCellViewModel.DisplayDueDate;
				taskCellViewModel.SourceViewModel = taskCellViewModel.SourceViewModel.Copy();
				InitEvents();
				GetDragCell()?.SetCellHandling(value: true);
			}
		}
	}

	private async void TryMoveHandle(MouseEventArgs e)
	{
		if (e.LeftButton != MouseButtonState.Pressed || (!_draggingBottomHandle && !_draggingTopHandle) || base.DataContext == null || !(base.DataContext is TaskCellViewModel taskCellViewModel) || (!string.IsNullOrEmpty(taskCellViewModel.AttendId) && !AgendaHelper.CanAccessAgenda(taskCellViewModel)))
		{
			return;
		}
		double roundOffset = CalendarGeoHelper.GetRoundOffset(e.GetPosition(this).Y + CalendarGeoHelper.GetTopFoldDiff());
		if (!taskCellViewModel.DisplayStartDate.HasValue)
		{
			return;
		}
		if (_draggingBottomHandle)
		{
			DateTime value = taskCellViewModel.BaseDate ?? taskCellViewModel.DisplayStartDate.Value;
			DateTime dateTime = CalendarGeoHelper.TranslateVerticalOffset(value.Date, roundOffset + taskCellViewModel.VerticalOffset);
			Mouse.OverrideCursor = Cursors.SizeNS;
			if (dateTime <= taskCellViewModel.DisplayStartDate.Value.AddMinutes(15.0))
			{
				dateTime = taskCellViewModel.DisplayStartDate.Value.AddMinutes(15.0);
			}
			taskCellViewModel.SourceViewModel.DueDate = dateTime.AddDays(-1.0 * taskCellViewModel.RepeatDiff);
			CalendarGeoHelper.ResetCellHeightAndVertical(taskCellViewModel, value);
		}
		else
		{
			if (!taskCellViewModel.DisplayDueDate.HasValue)
			{
				taskCellViewModel.SourceViewModel.DueDate = taskCellViewModel.StartDate;
			}
			if (taskCellViewModel.DisplayStartDate.HasValue && taskCellViewModel.DisplayDueDate.HasValue)
			{
				DateTime dateTime2 = CalendarGeoHelper.TranslateVerticalOffsetToStart((_dragStartDate ?? taskCellViewModel.DisplayStartDate).Value, taskCellViewModel.VerticalOffset + roundOffset);
				int num = ((LocalSettings.Settings.CalendarHourHeight / 3.0 >= CalendarGeoHelper.MinHeight) ? (-15) : (-30));
				if (dateTime2 >= taskCellViewModel.DisplayDueDate.Value.AddMinutes(num))
				{
					dateTime2 = taskCellViewModel.DisplayDueDate.Value.AddMinutes(num);
				}
				Mouse.OverrideCursor = Cursors.SizeNS;
				taskCellViewModel.SourceViewModel.StartDate = dateTime2.AddDays(-1.0 * taskCellViewModel.RepeatDiff);
				CalendarGeoHelper.ResetCellHeightAndVertical(taskCellViewModel, _dragStartDate);
			}
		}
		taskCellViewModel.SetTimeText(inArrange: false, isSpan: true);
	}

	private void OnCellMove(object sender, MouseEventArgs e)
	{
		e.Handled = true;
		if (e.LeftButton != MouseButtonState.Pressed || !_press)
		{
			return;
		}
		if (base.DataContext == null)
		{
			return;
		}
		object dataContext = base.DataContext;
		TaskCellViewModel model = dataContext as TaskCellViewModel;
		if (model == null || model.NewAdd || model.ShowInFocus)
		{
			return;
		}
		CalendarControl calendarParent = GetCalendarParent();
		if (calendarParent != null)
		{
			BlockingSet<string> batchSelected = calendarParent.BatchSelected;
			if (batchSelected != null && batchSelected.Count > 0 && !model.BatchSelected)
			{
				_press = false;
				return;
			}
		}
		if (model.CanDrag)
		{
			TryDragCell(e);
			return;
		}
		if (model.IsCourse)
		{
			GetToastWindow()?.TryToastString(null, Utils.GetString("OperationNotSupport"));
			return;
		}
		ProjectModel model2 = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == model.ProjectId);
		TryToastUnableString(model2);
		base.MouseMove -= OnCellMove;
	}

	private async void TryDragCell(MouseEventArgs e)
	{
		if (!Utils.CheckMoveDelta(this, e, _cellDragStart, 2))
		{
			return;
		}
		if (base.DataContext != null)
		{
			object dataContext = base.DataContext;
			if (dataContext is TaskCellViewModel model)
			{
				if (!model.BatchSelected)
				{
					GetCalendarParent()?.ExitBatchSelect();
				}
				CalendarEventDragHelper.CheckIfEventCanDrag(model, out var canDrag, out var needToast, out var toastStr);
				if (canDrag)
				{
					DateTime? start = model.StartDate;
					TaskCellViewModel taskCellViewModel = model.Clone();
					Point position = e.GetPosition(this);
					taskCellViewModel.DragVOffset = position.Y;
					if (!taskCellViewModel.BaseOnStart && taskCellViewModel.StartDate.HasValue)
					{
						taskCellViewModel.OriginalStart = taskCellViewModel.StartDate;
					}
					double num = Math.Min(Utils.FindParent<CalendarTimelineDayView>(this)?.ActualWidth ?? base.ActualWidth, 350.0);
					CalendarGeoHelper.ResetCellHeightAndVertical(taskCellViewModel, taskCellViewModel.BaseDate);
					taskCellViewModel.Width = num - 8.0;
					taskCellViewModel.HorizontalOffset = 0.0;
					taskCellViewModel.VerticalOffset = 0.0;
					DelaySetDragging(model);
					Utils.FindParent<MultiDayView>(this)?.SetBatchDragStarting();
					await TickDragHelper.StartDragItem(GetCalendarParent(), taskCellViewModel, (!model.BatchSelected) ? new List<string> { taskCellViewModel.TaskId } : (GetCalendarParent()?.BatchSelected?.ToList() ?? new List<string> { taskCellViewModel.TaskId }), num, new Point(Math.Min(position.X, num - 4.0), position.Y));
					await Task.Delay(200);
					while (ModifyRepeatHandler.DialogShowing)
					{
						await Task.Delay(100);
					}
					if (start == model.StartDate)
					{
						model.Dragging = false;
					}
				}
				else if (needToast)
				{
					GetToastWindow()?.TryToastString(null, toastStr);
				}
				_press = false;
			}
		}
		base.MouseMove -= OnCellMove;
	}

	private async Task DelaySetDragging(TaskCellViewModel model)
	{
		await Task.Delay(40);
		if (TickDragHelper.Dragging)
		{
			model.Dragging = !model.BatchSelected;
			if (model.BatchSelected)
			{
				GetCalendarParent()?.SetBatchDragging(dragging: true);
			}
		}
	}

	private IDragCellEvent GetDragCell()
	{
		return Utils.FindParent<IDragCellEvent>(this);
	}

	private async Task OnHandleDrop(DateTime? originStart, DateTime? originDue, bool dragBottom)
	{
		if (base.DataContext == null)
		{
			return;
		}
		object dataContext = base.DataContext;
		if (!(dataContext is TaskCellViewModel { Type: var type } model))
		{
			return;
		}
		switch (type)
		{
		case DisplayType.Task:
		case DisplayType.Derivative:
			if (model.DisplayStartDate == originStart && model.DisplayDueDate == originDue)
			{
				model.SetSourceModel(TaskCache.GetTaskById(model.SourceViewModel.Id));
				return;
			}
			if (!string.IsNullOrEmpty(model.RepeatFlag))
			{
				TaskModel task = await TaskDao.GetThinTaskById(model.TaskId);
				if (task != null && model.StartDate.HasValue && task.startDate.HasValue)
				{
					List<TaskReminderModel> reminders = await TaskReminderDao.GetRemindersByTaskId(task.id);
					TimeData reviseData = new TimeData
					{
						StartDate = model.DisplayStartDate,
						DueDate = model.DisplayDueDate,
						IsAllDay = (!model.DisplayStartDate.HasValue || (model.IsAllDay ?? true)),
						Reminders = reminders,
						RepeatFrom = model.RepeatFrom,
						RepeatFlag = model.RepeatFlag,
						ExDates = ((task.exDate != null) ? task.exDate.ToList() : new List<string>()),
						TimeZone = new TimeZoneViewModel(task.isFloating == true, task.timeZone)
					};
					ModifyRepeatHandler.TryUpdateDueDate(task.id, originStart, originDue, reviseData, 1, 0);
				}
			}
			else if (model.DisplayStartDate.HasValue && model.DisplayDueDate.HasValue)
			{
				await TaskService.SetTaskDateAfterDropHandle(model.TaskId, model.DisplayStartDate.Value, model.DisplayDueDate.Value, dragBottom);
			}
			model.SetSourceModel(TaskCache.GetTaskById(model.SourceViewModel.Id));
			break;
		case DisplayType.Event:
			if (model.StartDate.HasValue && model.DueDate.HasValue)
			{
				await CalendarService.SetEventDate(model.EventId, model.StartDate.Value, model.DueDate.Value, dragBottom);
			}
			break;
		}
		SyncManager.TryDelaySync();
	}

	private async void OnCellClick(object sender, MouseButtonEventArgs e)
	{
		if (GetCalendarParent()?.IsLocked ?? false)
		{
			return;
		}
		if (base.DataContext is TaskCellViewModel taskCellViewModel)
		{
			GetCalendarParent()?.ClearCopiedTaskSelection(taskCellViewModel.GetTaskId());
		}
		if (!Utils.IfCtrlPressed() && (GetCalendarParent()?.ExitBatchSelect() ?? false))
		{
			return;
		}
		base.MouseMove -= OnCellMove;
		if (!PopupStateManager.CanShowDetailPopup() || _draggingBottomHandle || _draggingTopHandle || PopupStateManager.IsInAdd() || (_iconBorder != null && _iconBorder.IsMouseOver && OnIconClick(e)))
		{
			return;
		}
		e.Handled = true;
		if (_detailShowing)
		{
			_detailShowing = false;
			return;
		}
		TaskCellViewModel model = base.DataContext as TaskCellViewModel;
		if (base.DataContext == null || model == null)
		{
			return;
		}
		CalendarControl calendarControl = Utils.FindParent<CalendarControl>(this);
		if (Utils.IfCtrlPressed())
		{
			if (model.CanBatchSelected)
			{
				model.BatchSelected = !model.BatchSelected;
				calendarControl?.SetBatchSelected(model.GetTaskId(), model.BatchSelected);
			}
			else
			{
				GetToastWindow()?.TryToastString(null, Utils.GetString("OptionNotSupport"));
			}
			return;
		}
		calendarControl?.AddItemClickEvent();
		if (model.IsCalendarEvent)
		{
			await ShowEventDetail(e, model);
		}
		else if (model.IsHabit)
		{
			ShowHabitDetail(e, model);
		}
		else if (model.IsPomo)
		{
			ShowPomoDetail(model);
		}
		else if (model.Course != null)
		{
			ShowCourseDetail(model);
		}
		else if (model.ShowInFocus)
		{
			_firstClickPoint = PopupLocationCalculator.GetMousePoint(isDefault: false);
			if (await ShowTaskDetailPopup(model, show: true) == null)
			{
				return;
			}
		}
		else
		{
			TryShowTaskDetail(model);
		}
		if (model.ShowInFocus)
		{
			string actCType = TickFocusManager.GetActCType();
			UserActCollectUtils.AddClickEvent("focus", actCType, "click_calendar_dialog");
		}
	}

	private async void ShowCourseDetail(TaskCellViewModel model)
	{
		if (model.Course != null)
		{
			CourseDetailViewModel viewModel = await CourseDetailViewModel.Build(model.Course);
			model.Selected = true;
			CourseDetailWindow courseDetailWindow = new CourseDetailWindow(viewModel);
			courseDetailWindow.Closed += async delegate
			{
				model.Selected = false;
				GetCalendarParent()?.SetEditting(editting: false);
				await Task.Delay(200);
				_detailShowing = false;
			};
			_detailShowing = true;
			courseDetailWindow.Show(this, base.ActualWidth, base.ActualWidth > 300.0);
			GetCalendarParent()?.SetEditting(editting: true);
		}
	}

	private async void ShowPomoDetail(TaskCellViewModel model)
	{
		PomodoroModel pomodoroModel = await PomoService.GetAdjustPomo(model.PomoId);
		if (pomodoroModel != null)
		{
			model.Selected = true;
			PomoDetailWindow pomoDetailWindow = new PomoDetailWindow(new PomoDisplayViewModel(pomodoroModel, pomodoroModel.Tasks), model);
			pomoDetailWindow.Closed += delegate
			{
				model.Selected = false;
				GetCalendarParent()?.SetEditting(editting: false);
			};
			pomoDetailWindow.Show(this, base.ActualWidth, base.ActualWidth > 300.0);
			GetCalendarParent()?.SetEditting(editting: true);
		}
	}

	private async void TryShowTaskDetail(TaskCellViewModel model)
	{
		_isWaitingDoubleClick = false;
		bool num = (DateTime.Now - _lastClickTime).TotalMilliseconds > 400.0;
		_lastClickTime = DateTime.Now;
		if (num)
		{
			_isWaitingDoubleClick = true;
			_firstClickPoint = PopupLocationCalculator.GetMousePoint(isDefault: false);
			await ShowTaskDetailPopup(model, show: false);
			await Task.Delay(400);
			if (!_isWaitingDoubleClick)
			{
				UserActCollectUtils.AddClickEvent("calendar", "action", "click_task");
			}
		}
		else
		{
			UserActCollectUtils.AddClickEvent("calendar", "action", "double_click_task");
			model.Selected = false;
			TaskDetailWindows.ShowTaskWindows(model.TaskId);
		}
	}

	private async Task ShowHabitDetail(MouseEventArgs e, CalendarDisplayViewModel model)
	{
		HabitModel habitModel = await HabitDao.GetHabitById(model.HabitId);
		if (habitModel != null)
		{
			HabitCheckInWindow window = new HabitCheckInWindow(habitModel, model.Status, model.StartDate, GetToastWindow());
			window.Show(this, base.ActualWidth, 0.0, base.ActualWidth >= 350.0);
			GetCalendarParent()?.SetEditting(editting: true);
			window.OnAction += OnCheckInAction;
			window.Closed += delegate
			{
				HandleOnWindowClosed();
				window.OnAction -= OnCheckInAction;
			};
			model.Selected = true;
		}
	}

	private void OnCheckInAction(object sender, EventArgs e)
	{
		HandleOnWindowClosed();
	}

	private async Task ShowEventDetail(MouseEventArgs e, TaskCellViewModel calendarDisplayViewModel)
	{
		string eventId = calendarDisplayViewModel.EventId;
		if (string.IsNullOrEmpty(eventId))
		{
			return;
		}
		CalendarEventModel calendarEventModel = await CalendarEventDao.GetEventById(ArchivedDao.GetOriginalId(eventId));
		if (calendarEventModel != null)
		{
			if (base.DataContext != null && base.DataContext is TaskCellViewModel taskCellViewModel)
			{
				calendarEventModel.DueStart = taskCellViewModel.DisplayStartDate;
				calendarEventModel.DueEnd = taskCellViewModel.DisplayDueDate;
				taskCellViewModel.Selected = true;
			}
			CalendarDetailWindow calendarDetailWindow = new CalendarDetailWindow(GetWidgetThemeId());
			calendarDetailWindow.Disappear += OnDetailClosed;
			calendarDetailWindow.EventArchiveChanged += OnEventArchiveChanged;
			calendarDetailWindow.Show(this, base.ActualWidth, 0.0, base.ActualWidth > 350.0, new CalendarDetailViewModel(calendarEventModel));
			GetCalendarParent()?.SetEditting(editting: true);
		}
	}

	private void OnEventArchiveChanged(object sender, bool archive)
	{
		TaskCellViewModel model = GetModel();
		if (model != null)
		{
			model.SourceViewModel.Status = (archive ? 2 : 0);
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

	private async Task<TaskDetailPopup> ShowTaskDetailPopup(CalendarDisplayViewModel model, bool show)
	{
		if (model == null)
		{
			return null;
		}
		if (PopupStateManager.LastTarget == this)
		{
			return null;
		}
		string taskId = model.GetTaskId();
		string repeatFlag = model.RepeatFlag;
		double repeatDiff = model.RepeatDiff;
		string itemId = model.ItemId;
		TaskDetailViewModel taskDetailViewModel = await TaskDetailViewModel.Build(taskId);
		if (taskDetailViewModel != null)
		{
			if (!base.IsLoaded)
			{
				return null;
			}
			taskDetailViewModel.RepeatDiff = (string.IsNullOrEmpty(repeatFlag) ? ((double?)null) : new double?(repeatDiff));
			TaskDetailPopup taskDetailPopup = new TaskDetailPopup
			{
				IsCalendarContext = true
			};
			IToastShowWindow toastWindow = GetToastWindow();
			if (toastWindow != null)
			{
				taskDetailPopup.DependentWindow = toastWindow;
			}
			model.Selected = true;
			taskDetailPopup.Disappear -= OnDetailClosed;
			taskDetailPopup.Disappear += OnDetailClosed;
			taskDetailPopup.Show(taskDetailViewModel, itemId, new TaskWindowDisplayArgs(this, base.ActualWidth + 10.0, (base.ActualWidth < 350.0 && base.ActualHeight < 350.0) ? default(Point) : _firstClickPoint, 0.0));
			GetCalendarParent()?.SetEditting(editting: true);
			return taskDetailPopup;
		}
		return null;
	}

	private void OnDetailClosed(object sender, string e)
	{
		HandleOnWindowClosed();
		if (sender is TaskDetailPopup taskDetailPopup)
		{
			taskDetailPopup.Disappear -= OnDetailClosed;
		}
	}

	private void HandleOnWindowClosed()
	{
		if (base.DataContext != null && base.DataContext is TaskCellViewModel taskCellViewModel)
		{
			taskCellViewModel.Selected = false;
		}
		GetCalendarParent()?.SetEditting(editting: false);
	}

	private CalendarControl GetCalendarParent()
	{
		return Utils.FindParent<CalendarControl>(this);
	}

	private bool TryToastUnableString(ProjectModel model)
	{
		if (model == null)
		{
			return false;
		}
		if (!string.IsNullOrEmpty(model.permission))
		{
			IToastShowWindow toastWindow = GetToastWindow();
			string permission = model.permission;
			if (permission == "read")
			{
				string arg = Utils.GetString("ReadOnly");
				toastWindow?.TryToastString(null, string.Format(Utils.GetString("NoPermissionToEdit"), arg));
				return true;
			}
			if (permission == "comment")
			{
				string arg = Utils.GetString("CanComment");
				toastWindow?.TryToastString(null, string.Format(Utils.GetString("NoPermissionToEdit"), arg));
				return true;
			}
		}
		return false;
	}

	private async void OnShowOperation(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext == null)
		{
			return;
		}
		object dataContext = base.DataContext;
		TaskCellViewModel model = dataContext as TaskCellViewModel;
		if (model == null)
		{
			return;
		}
		CalendarControl calendarParent = GetCalendarParent();
		calendarParent?.ClearCopiedTaskSelection(model.GetTaskId());
		if ((calendarParent != null && !model.BatchSelected && calendarParent.ExitBatchSelect()) || model.NewAdd || model.ShowInFocus)
		{
			return;
		}
		if (model.IsHabit)
		{
			ShowHabitOperation(model);
			return;
		}
		if (model.Type == DisplayType.Event)
		{
			ShowEventOperation(model);
			return;
		}
		if (model.Type == DisplayType.Course)
		{
			ShowCourseOperation(model);
			return;
		}
		ProjectModel model2 = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == model.ProjectId);
		if (TryToastUnableString(model2))
		{
			model.Selected = false;
		}
		else
		{
			ShowTaskOperation(model);
		}
	}

	private async Task ShowTaskOperation(TaskCellViewModel model)
	{
		if (model.Type == DisplayType.Task || model.Type == DisplayType.Derivative || model.Type == DisplayType.Note)
		{
			_taskId = model.TaskId;
		}
		else
		{
			_taskId = null;
		}
		if (string.IsNullOrEmpty(_taskId))
		{
			return;
		}
		GetToastWindow();
		if (model.BatchSelected)
		{
			ShowBatchTaskOperation(model);
			return;
		}
		OperationExtra operationExtra = await TaskOperationHelper.GetTaskAccessInfo(_taskId, canShowAddSubtask: false);
		if (operationExtra?.TimeModel == null)
		{
			return;
		}
		operationExtra.InCal = true;
		operationExtra.TimeModel.StartDate = model.DisplayStartDate ?? model.StartDate;
		operationExtra.TimeModel.DueDate = model.DisplayDueDate ?? model.DueDate;
		TaskOperationDialog dialog = new TaskOperationDialog(operationExtra, this);
		if (Window.GetWindow(this) is WidgetWindow widgetWindow)
		{
			dialog.Resources = widgetWindow.Resources;
		}
		dialog.Closed += OnDialogClosed;
		dialog.PrioritySelect += SetPriority;
		dialog.LinkCopied += CopyTaskLink;
		dialog.Copied += CopyTask;
		dialog.Deleted += DeleteTask;
		dialog.AbandonOrReopen += OnAbandonOrReopenTask;
		dialog.ProjectSelect += OnProjectSelect;
		dialog.AssigneeSelect += OnAssigneeSelect;
		dialog.TagsSelect += OnTagsSelect;
		dialog.SkipCurrentRecurrence += OnSkipRecurrence;
		dialog.SwitchTaskOrNote += OnSwitchTaskOrNoteClick;
		dialog.Toast += OnTaskOperationToast;
		dialog.CompleteDateChanged += async delegate(object o, DateTime date)
		{
			await TaskService.ChangeCompleteDate(model.TaskId, date);
			SyncManager.TryDelaySync();
		};
		dialog.TimeClear += async delegate
		{
			await ClearDate();
			dialog.Dismiss();
		};
		dialog.TimeSelect += async delegate(object arg, TimeData data)
		{
			dialog.Dismiss();
			if (string.IsNullOrEmpty(model.RepeatFlag) || !model.StartDate.HasValue || model.Status != 0)
			{
				await TaskService.SetDate(_taskId, data);
			}
			else
			{
				ModifyRepeatHandler.TryUpdateDueDate(model.TaskId, model.DisplayStartDate, model.DisplayDueDate, data, 1, 1);
			}
		};
		dialog.QuickDateSelect += async delegate(object arg, DateTime date)
		{
			dialog.Dismiss();
			if (!string.IsNullOrEmpty(model.RepeatFlag) && model.Status == 0)
			{
				TaskModel task = await TaskDao.GetThinTaskById(model.TaskId);
				if (task != null && model.StartDate.HasValue && task.startDate.HasValue)
				{
					List<TaskReminderModel> reminders = await TaskReminderDao.GetRemindersByTaskId(task.id);
					TimeData timeData = new TimeData
					{
						StartDate = model.StartDate,
						DueDate = model.DueDate,
						IsAllDay = (!model.StartDate.HasValue || (model.IsAllDay ?? true)),
						Reminders = reminders,
						RepeatFrom = model.RepeatFrom,
						RepeatFlag = model.RepeatFlag,
						ExDates = ((task.exDate != null) ? task.exDate.ToList() : new List<string>())
					};
					ModifyRepeatHandler.TryUpdateDueDateOnlyDate(model.TaskId, model.DisplayStartDate, model.DisplayDueDate, timeData, date, 1, 1);
				}
			}
			else
			{
				await TaskService.SetStartDate(_taskId, date);
				SyncManager.TryDelaySync();
			}
		};
		dialog.Show();
		PopupStateManager.OnViewPopupOpened();
		GetCalendarParent()?.SetEditting(editting: true);
	}

	private async void ShowBatchTaskOperation(TaskCellViewModel model)
	{
		List<string> taskIds = GetCalendarParent()?.BatchSelected?.ToList() ?? new List<string> { model.TaskId };
		OperationExtra extra = await BatchTaskEditHelper.GetBatchArgs(taskIds, isNote: false, useInList: false);
		if (extra == null)
		{
			return;
		}
		extra.InCal = true;
		TaskOperationDialog dialog = new TaskOperationDialog(extra, this);
		if (Window.GetWindow(this) is WidgetWindow widgetWindow)
		{
			dialog.Resources = widgetWindow.Resources;
		}
		dialog.Closed += delegate
		{
			GetCalendarParent()?.SetEditting(editting: false);
			PopupStateManager.OnViewPopupClosed(needWait: false);
		};
		dialog.OpenSticky += delegate
		{
			GetCalendarParent()?.BatchOpenSticky();
		};
		dialog.PrioritySelect += delegate(object o, int p)
		{
			GetCalendarParent()?.BatchSetPriority(p);
		};
		dialog.TextCopied += delegate
		{
			GetCalendarParent()?.CopyBatchTasksText(taskIds);
		};
		dialog.Copied += delegate
		{
			TaskService.CopyTasks(taskIds, GetCalendarParent());
		};
		dialog.Deleted += delegate
		{
			GetCalendarParent()?.OnBatchDelete();
		};
		dialog.AbandonOrReopen += delegate
		{
			GetCalendarParent()?.OnBatchComplete();
		};
		dialog.ProjectSelect += delegate(object o, SelectableItemViewModel p)
		{
			GetCalendarParent()?.OnBatchSetProject(p, extra.ProjectId, extra.ColumnId);
		};
		dialog.AssigneeSelect += delegate(object o, AvatarInfo a)
		{
			GetCalendarParent()?.OnBatchSetAssignee(a);
		};
		dialog.TagsSelect += delegate(object o, TagSelectData t)
		{
			GetCalendarParent()?.OnBatchSetTags(t);
		};
		dialog.SkipCurrentRecurrence += delegate
		{
			GetCalendarParent()?.OnBatchSkip();
		};
		dialog.SwitchTaskOrNote += delegate
		{
			GetCalendarParent()?.OnBatchSwitchTaskNote();
		};
		dialog.Toast += delegate(object o, string s)
		{
			GetToastWindow()?.TryToastString(o, s);
		};
		dialog.CompleteDateChanged += delegate(object o, DateTime date)
		{
			foreach (string item in taskIds)
			{
				TaskService.ChangeCompleteDate(item, date);
			}
			SyncManager.TryDelaySync();
		};
		dialog.TimeClear += async delegate
		{
			await ClearDate();
			dialog.Dismiss();
			GetCalendarParent()?.BatchClearDate();
		};
		dialog.TimeSelect += async delegate(object arg, TimeData data)
		{
			GetCalendarParent()?.BatchSetTimeData(data);
			dialog.Dismiss();
		};
		dialog.QuickDateSelect += async delegate(object arg, DateTime date)
		{
			GetCalendarParent()?.BatchSetQuickDate(date);
			dialog.Dismiss();
		};
		dialog.DelayOrAdvanced += delegate(object sender, long l)
		{
			GetCalendarParent()?.OnBatchDelay(l);
		};
		dialog.Show();
		PopupStateManager.OnViewPopupOpened();
		GetCalendarParent()?.SetEditting(editting: true);
	}

	private void ShowCourseOperation(TaskCellViewModel model)
	{
		if (!(model.StartDate < DateTime.Today))
		{
			model.Selected = true;
			ArchiveOperationDialog archiveOperationDialog = new ArchiveOperationDialog(model.Course.UniqueId, ArchiveKind.Course, model.Course.Archived);
			archiveOperationDialog.SetPlaceTarget(this);
			archiveOperationDialog.Closed += OnDialogClosed;
			GetCalendarParent()?.SetEditting(editting: true);
			archiveOperationDialog.Operated += delegate(object arg, KeyValuePair<string, ActionType> obj)
			{
				model.Course.Archived = obj.Value == ActionType.Archive;
				model.SourceViewModel.Status = ((model.Course.Archived || model.Course.CourseStart.Date < DateTime.Today) ? 2 : 0);
			};
			archiveOperationDialog.Show();
		}
	}

	private void ShowEventOperation(TaskCellViewModel model)
	{
		if ((!model.DueDate.HasValue || !(model.DueDate.Value <= DateTime.Today)) && (model.DueDate.HasValue || !model.StartDate.HasValue || !(model.StartDate.Value < DateTime.Today)))
		{
			model.Selected = true;
			CalendarOperationDialog calendarOperationDialog = new CalendarOperationDialog(new EventArchiveArgs(model.SourceViewModel), model.Status != 0);
			calendarOperationDialog.SetPlaceTarget(this);
			GetCalendarParent()?.SetEditting(editting: true);
			calendarOperationDialog.Closed += OnDialogClosed;
			calendarOperationDialog.Operated += delegate(object arg, KeyValuePair<string, ActionType> obj)
			{
				model.SourceViewModel.Status = ((obj.Value == ActionType.Archive) ? 2 : 0);
			};
			calendarOperationDialog.Show();
		}
	}

	private void ShowHabitOperation(CalendarDisplayViewModel model)
	{
		if (model.Status != 0)
		{
			return;
		}
		List<OperationItemViewModel> list = new List<OperationItemViewModel>
		{
			new OperationItemViewModel(ActionType.EditHabitLog),
			new OperationItemViewModel(ActionType.Skip),
			new OperationItemViewModel(ActionType.UnComplete)
		};
		HabitBaseViewModel habitBaseViewModel = (model.IsHabit ? (model.SourceViewModel as HabitBaseViewModel) : null);
		HabitModel habit = habitBaseViewModel?.Habit;
		HabitCheckInModel habitCheckInModel = habitBaseViewModel?.HabitCheckIn;
		if (habitCheckInModel != null && habitCheckInModel.Value > 0.0)
		{
			list.Add(new OperationItemViewModel(ActionType.Reset));
		}
		if (LocalSettings.Settings.EnableFocus)
		{
			list.Insert(0, new OperationItemViewModel(ActionType.StartFocus)
			{
				Selectable = false,
				SubActions = new List<OperationItemViewModel>
				{
					new OperationItemViewModel(ActionType.StartPomo)
					{
						Enable = (!TickFocusManager.Working || LocalSettings.Settings.PomoType == FocusConstance.Focus)
					},
					new OperationItemViewModel(ActionType.StartTiming)
					{
						Enable = (!TickFocusManager.Working || LocalSettings.Settings.PomoType == FocusConstance.Timing)
					}
				}
			});
		}
		OperationDialog operationDialog = new OperationDialog(model.HabitId, list);
		operationDialog.SetPlaceTarget(this);
		operationDialog.Operated += async delegate(object obj, KeyValuePair<string, ActionType> kv)
		{
			switch (kv.Value)
			{
			case ActionType.StartPomo:
				TickFocusManager.TryStartFocusHabit(kv.Key, isPomo: true);
				break;
			case ActionType.StartTiming:
				TickFocusManager.TryStartFocusHabit(kv.Key, isPomo: false);
				break;
			case ActionType.Skip:
				await HabitService.SkipHabit(model.HabitId);
				GetCalendarParent()?.RemoveItem(model);
				GetToastWindow()?.TryToastString(null, Utils.GetString("Skipped"));
				break;
			case ActionType.EditHabitLog:
				HabitUtils.ShowHabitRecordWindow(model.HabitId, habit?.Name, DateTime.Today, isEdit: true, !model.IsCompleted);
				break;
			case ActionType.UnComplete:
				await HabitService.UncompleteHabit(model.HabitId, DateTime.Today);
				break;
			case ActionType.Reset:
				await HabitService.ResetCheckInHabit(model.HabitId, DateTime.Today, habit?.Type, this);
				break;
			}
		};
		operationDialog.Show();
	}

	private void OnTaskOperationToast(object sender, string e)
	{
		GetToastWindow()?.TryToastString(null, e);
	}

	private async void OnAbandonOrReopenTask(object sender, EventArgs e)
	{
		if (base.DataContext == null)
		{
			return;
		}
		object dataContext = base.DataContext;
		if (!(dataContext is CalendarDisplayViewModel { DisplayStartDate: var displayStartDate } model))
		{
			return;
		}
		if (displayStartDate.HasValue && model.StartDate != model.DisplayStartDate && model.Status == 0)
		{
			IToastShowWindow toastWindow = GetToastWindow();
			if (!(await ModifyRepeatHandler.CompleteOrSkipRecurrence(model.TaskId, model.DisplayStartDate, toastWindow)))
			{
				return;
			}
		}
		int num = ((!model.IsAbandoned) ? (-1) : 0);
		await TaskService.SetTaskStatus(model.TaskId, num, num != 0);
	}

	private async void OnSwitchTaskOrNoteClick(object sender, EventArgs e)
	{
		if (base.DataContext != null && base.DataContext is CalendarDisplayViewModel calendarDisplayViewModel && await TaskService.SwitchTaskOrNote(calendarDisplayViewModel.TaskId, toast: false))
		{
			GetToastWindow()?.TryToastString(null, Utils.GetString("Converted"));
			SyncManager.TryDelaySync();
		}
	}

	private async Task ClearDate()
	{
		TaskModel taskModel = await TaskDao.GetThinTaskById(_taskId);
		if (taskModel != null)
		{
			if (string.IsNullOrEmpty(taskModel.attendId))
			{
				await TaskService.ClearDate(_taskId);
				SyncManager.TryDelaySync();
			}
			else if (await TaskOperationHelper.CheckIfTaskAllowClearDate(_taskId, this))
			{
				await TaskService.ClearAgendaDate(_taskId);
				SyncManager.TryDelaySync();
			}
		}
	}

	private async void OnSkipRecurrence(object sender, EventArgs e)
	{
		TaskCellViewModel model = GetModel();
		if (model == null || !model.DisplayStartDate.HasValue || !(model.DisplayStartDate != model.StartDate))
		{
			await TaskService.SkipCurrentRecurrence(_taskId, notify: true, GetToastWindow());
		}
		else
		{
			await TaskService.SkipSelectRecurrence(_taskId, model.DisplayStartDate.Value.Date, GetToastWindow());
		}
		SyncManager.TryDelaySync();
	}

	private async void OnTagsSelect(object sender, TagSelectData tags)
	{
		await TaskService.SetTags(_taskId, tags.OmniSelectTags);
		SyncManager.TryDelaySync();
	}

	private async void OnAssigneeSelect(object sender, AvatarInfo assignee)
	{
		await TaskService.SetAssignee(_taskId, assignee.UserId);
		SyncManager.TryDelaySync();
	}

	private async void OnProjectSelect(object sender, SelectableItemViewModel e)
	{
		(string, string, string) projectAndColumnId = e.GetProjectAndColumnId();
		string item = projectAndColumnId.Item1;
		string item2 = projectAndColumnId.Item2;
		string taskId = _taskId;
		string columnId = item2;
		await TaskService.MoveProject(taskId, item, null, columnId);
		SyncManager.TryDelaySync();
	}

	private async void DeleteTask(object sender, EventArgs e)
	{
		PopupStateManager.OnViewPopupClosed(needWait: false);
		TaskModel task = await TaskDao.GetThinTaskById(_taskId);
		if (task == null)
		{
			return;
		}
		if (string.IsNullOrEmpty(task.attendId))
		{
			if (!string.IsNullOrEmpty(task.repeatFlag) && task.startDate.HasValue && task.status == 0)
			{
				if (base.DataContext != null && base.DataContext is CalendarDisplayViewModel calendarDisplayViewModel && calendarDisplayViewModel.TaskId == task.id)
				{
					ModifyRepeatHandler.TryDeleteRecurrence(task.id, calendarDisplayViewModel.DisplayStartDate, calendarDisplayViewModel.DisplayDueDate, GetToastWindow());
				}
				return;
			}
			List<TaskModel> list = await TaskService.GetAllSubTasksByIdAsync(_taskId, task.projectId);
			if (list != null && list.Count > 0)
			{
				list.Add(task);
				GetToastWindow()?.BatchDeleteTask(list);
			}
			else
			{
				GetToastWindow()?.TaskDeleted(_taskId);
			}
		}
		else if (await TaskOperationHelper.CheckIfAllowDeleteAgenda(task, this))
		{
			if (!string.IsNullOrEmpty(task.repeatFlag) && task.startDate.HasValue && task.status == 0)
			{
				ModifyRepeatHandler.TryDeleteRecurrence(task.id, task.startDate, task.dueDate, GetToastWindow());
				return;
			}
			await TaskService.DeleteAgenda(_taskId, task.projectId, task.attendId);
			SyncManager.TryDelaySync();
		}
		if (base.DataContext != null && base.DataContext is CalendarDisplayViewModel model)
		{
			GetCalendarParent()?.RemoveItem(model);
		}
	}

	private async void CopyTask(object sender, EventArgs e)
	{
		TaskModel taskModel = await TaskService.CopyTask(_taskId, this);
		if (taskModel != null)
		{
			GetCalendarParent()?.SelectCopiedTask(taskModel.id);
		}
		SyncManager.TryDelaySync();
	}

	private async void CopyTaskLink(object sender, EventArgs e)
	{
		TaskModel taskModel = await TaskDao.GetThinTaskById(_taskId);
		if (taskModel != null)
		{
			TaskUtils.CopyTaskLink(taskModel.id, taskModel.projectId, taskModel.title);
		}
	}

	private async void SetPriority(object sender, int priority)
	{
		await TaskService.SetPriority(_taskId, priority);
		SyncManager.TryDelaySync();
	}

	private void OnDialogClosed(object sender, EventArgs e)
	{
		TaskCellViewModel model = GetModel();
		if (model != null)
		{
			model.Selected = false;
		}
		GetCalendarParent()?.SetEditting(editting: false);
		PopupStateManager.OnViewPopupClosed(needWait: false);
	}

	private TaskCellViewModel GetModel()
	{
		if (base.DataContext != null && base.DataContext is TaskCellViewModel result)
		{
			return result;
		}
		return null;
	}

	private IToastShowWindow GetToastWindow()
	{
		_parentWindow = _parentWindow ?? Utils.FindParent<IToastShowWindow>(this);
		return _parentWindow;
	}

	public void SetShowIcon(bool showIcon)
	{
		_showIcon = showIcon;
	}

	public void OnDetailKeyUp(Key key)
	{
	}
}
