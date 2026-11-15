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
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service.Display;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Project;

namespace ticktick_WPF.Views.Pomo;

public class FocusTimeline : Grid
{
	private List<FocusTimelineViewModel> _timelines;

	private List<Border> _pauseBorder = new List<Border>();

	private DateTime _startTime;

	private Line _currentLine;

	private Ellipse _currentEllipse;

	private double _leftOffset;

	private readonly List<TaskCell> _taskCells = new List<TaskCell>();

	private bool _taskLoading;

	private readonly Border _topBorder = new Border
	{
		CornerRadius = new CornerRadius(2.0, 2.0, 0.0, 0.0),
		HorizontalAlignment = HorizontalAlignment.Left,
		VerticalAlignment = VerticalAlignment.Top,
		MinWidth = 10.0
	};

	private readonly PauseText _pauseText = new PauseText
	{
		VerticalAlignment = VerticalAlignment.Top,
		HorizontalAlignment = HorizontalAlignment.Right
	};

	private readonly Border _bottomBorder = new Border
	{
		CornerRadius = new CornerRadius(0.0, 0.0, 2.0, 2.0),
		HorizontalAlignment = HorizontalAlignment.Left,
		VerticalAlignment = VerticalAlignment.Top,
		MinWidth = 10.0,
		BorderThickness = new Thickness(1.0)
	};

	private readonly TextBlock _timeText = new TextBlock
	{
		FontSize = 12.0,
		HorizontalAlignment = HorizontalAlignment.Left,
		Opacity = 0.8
	};

	private List<CalendarDisplayModel> _validTasks;

	private double HourHeight => Math.Max(60.0, (base.Height - 20.0) / 4.0);

	public FocusTimeline()
	{
		InitTimePoint();
		InitCurrentTimeLine();
		InitCurrentFocusRect();
		base.Height = 260.0;
		base.Loaded += OnLoaded;
		base.Loaded += BindEvent;
		base.Unloaded += UnbindEvent;
	}

	private void BindEvent(object sender, RoutedEventArgs e)
	{
		DataChangedNotifier.PeriodicCheck += NotifyPointer;
		DataChangedNotifier.IsDarkChanged += OnIsDarkChanged;
		GlobalEventManager.TimeFormatChanged += OnFormatChanged;
		DataChangedNotifier.HabitCheckInChanged += OnCheckInChanged;
		PropertyChangedEventManager.AddHandler(TickFocusManager.Config.FocusVModel, OnFocusIdChanged, "NoTask");
		TasksChangeEventManager.AddHandler(TaskChangeNotifier.Notifier, OnTasksChanged);
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		base.Loaded -= OnLoaded;
		_timelines.ForEach(delegate(FocusTimelineViewModel t)
		{
			t.LineWidth = base.ActualWidth - t.TimeWidth - 12.0;
		});
		_leftOffset = ((LocalSettings.Settings.TimeFormat == "24Hour") ? 48 : 70);
		_currentLine.X1 = _leftOffset;
		_currentLine.X2 = Math.Max(60.0, base.ActualWidth);
		_topBorder.Margin = new Thickness(_leftOffset, 10.0, 0.0, 0.0);
		SetRectColor();
		DrawCurrentFocus();
	}

	private void UnbindEvent(object sender, RoutedEventArgs e)
	{
		DataChangedNotifier.PeriodicCheck -= NotifyPointer;
		DataChangedNotifier.IsDarkChanged -= OnIsDarkChanged;
		GlobalEventManager.TimeFormatChanged -= OnFormatChanged;
		DataChangedNotifier.HabitCheckInChanged -= OnCheckInChanged;
		PropertyChangedEventManager.RemoveHandler(TickFocusManager.Config.FocusVModel, OnFocusIdChanged, "NoTask");
		TasksChangeEventManager.RemoveHandler(TaskChangeNotifier.Notifier, OnTasksChanged);
	}

	private void OnCheckInChanged(object sender, HabitCheckInModel e)
	{
		DrawCurrentFocus();
	}

	private void OnIsDarkChanged(object sender, EventArgs e)
	{
		DrawCurrentFocus();
	}

	private void OnTasksChanged(object sender, TasksChangeEventArgs e)
	{
		if (base.IsVisible && (e.BatchChangedIds.Any() || e.DeletedChangedIds.Any() || e.UndoDeletedIds.Any() || e.AddIds.Any() || e.StatusChangedIds.Any() || e.KindChangedIds.Any() || e.DateChangedIds.Any() || e.CheckItemChangedIds.Any() || e.ProjectChangedIds.Any() || e.TagChangedIds.Any()))
		{
			DrawCurrentFocus();
		}
	}

	private void OnFocusIdChanged(object sender, PropertyChangedEventArgs e)
	{
		SetRectColor();
	}

	private void OnFormatChanged(object sender, EventArgs e)
	{
		_leftOffset = ((LocalSettings.Settings.TimeFormat == "24Hour") ? 48 : 70);
		SetStartTime(_startTime);
		_timelines.ForEach(delegate(FocusTimelineViewModel t)
		{
			t.LineWidth = base.ActualWidth - t.TimeWidth - 12.0;
		});
		_currentLine.X1 = _leftOffset;
		DrawCurrentFocus();
	}

	public void SetSize(double width, double height)
	{
		base.Height = height;
		_timelines.ForEach(delegate(FocusTimelineViewModel t)
		{
			t.LineWidth = width - t.TimeWidth - 12.0;
		});
		_currentLine.X2 = Math.Max(40.0, width);
		_timelines.ForEach(delegate(FocusTimelineViewModel t)
		{
			t.SetMargin((base.Height - 100.0) / 4.0);
		});
		DrawCurrentFocus(reloadTasks: false);
	}

	private void InitTimePoint()
	{
		ItemsControl itemsControl = new ItemsControl();
		itemsControl.SetResourceReference(ItemsControl.ItemTemplateProperty, "FocusTimelineDataTemplate");
		DateTime now = DateTime.Now;
		_startTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);
		_timelines = FocusTimelineViewModel.InitModels(_startTime);
		itemsControl.ItemsSource = _timelines;
		base.Children.Add(itemsControl);
	}

	private void InitCurrentTimeLine()
	{
		_currentLine = new Line
		{
			X2 = 50.0,
			Stroke = Brushes.Red,
			StrokeThickness = 1.0,
			VerticalAlignment = VerticalAlignment.Top
		};
		_currentLine.SetValue(Panel.ZIndexProperty, 100);
		base.Children.Add(_currentLine);
		_currentEllipse = new Ellipse
		{
			Width = 8.0,
			Height = 8.0,
			Fill = Brushes.Red,
			VerticalAlignment = VerticalAlignment.Top,
			HorizontalAlignment = HorizontalAlignment.Left
		};
		_currentEllipse.SetValue(Panel.ZIndexProperty, 100);
		base.Children.Add(_currentEllipse);
	}

	private void SetCurrentLineOffset()
	{
		double num = (DateTime.Now - _startTime).TotalHours * HourHeight + 10.0;
		_currentLine.Margin = new Thickness(0.0, num, 0.0, 0.0);
		_currentEllipse.Margin = new Thickness(_leftOffset - 6.0, num - 4.0, 0.0, 0.0);
	}

	private void NotifyPointer(object sender, EventArgs e)
	{
		DrawCurrentFocus();
	}

	private void InitCurrentFocusRect()
	{
		_topBorder.SetResourceReference(Panel.BackgroundProperty, "PrimaryColor80");
		_topBorder.SetValue(Panel.ZIndexProperty, 50);
		_topBorder.Cursor = Cursors.Hand;
		base.Children.Add(_topBorder);
		_bottomBorder.SetResourceReference(Panel.BackgroundProperty, "PrimaryColor10");
		_bottomBorder.SetResourceReference(Border.BorderBrushProperty, "PrimaryColor30");
		_bottomBorder.SetValue(Panel.ZIndexProperty, 10);
		base.Children.Add(_bottomBorder);
		_timeText.SetValue(Panel.ZIndexProperty, 60);
		_timeText.IsHitTestVisible = false;
		base.Children.Add(_timeText);
		_pauseText.SetValue(Panel.ZIndexProperty, 60);
		_pauseText.Visibility = Visibility.Collapsed;
		base.Children.Add(_pauseText);
		_topBorder.MouseEnter += async delegate
		{
			await Task.Delay(200);
			if (_topBorder.IsMouseOver)
			{
				_pauseText.Visibility = (string.IsNullOrEmpty(_pauseText.Text) ? Visibility.Collapsed : Visibility.Visible);
			}
		};
		_topBorder.MouseLeave += delegate
		{
			_pauseText.Visibility = Visibility.Collapsed;
		};
	}

	public async Task DrawCurrentFocus(bool reloadTasks = true)
	{
		await base.Dispatcher.Invoke((Func<Task>)async delegate
		{
			if (base.IsVisible)
			{
				if (!_taskLoading)
				{
					_taskLoading = true;
					try
					{
						DateTime pomoStart = ((Utils.IsEmptyDate(TickFocusManager.Config.StartTime) || TickFocusManager.Status == PomoStatus.WaitingWork) ? DateTime.Now : TickFocusManager.Config.StartTime);
						DateTime dateTime = pomoStart;
						dateTime = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day).AddHours(dateTime.Hour - 1);
						bool pomoing = TickFocusManager.IsPomo && !TickFocusManager.Config.AutoStopTime.HasValue;
						DateTime dateTime2 = DateTime.Now.AddSeconds(pomoing ? TickFocusManager.Config.CurrentSeconds : 0.0);
						if ((dateTime2 - dateTime).TotalHours > 3.0)
						{
							dateTime = new DateTime(dateTime2.Year, dateTime2.Month, dateTime2.Day).AddHours(dateTime2.Hour).AddHours(-3.0);
						}
						if (!dateTime.Equals(_startTime))
						{
							SetStartTime(dateTime);
						}
						SetCurrentLineOffset();
						DateTime focusDisplayStart = ((pomoStart > _startTime) ? pomoStart : _startTime);
						TaskCellViewModel taskCellViewModel = await LoadTasks(focusDisplayStart, DateTime.Now.AddMinutes(pomoing ? (TickFocusManager.Config.CurrentSeconds / 60.0) : 0.0), reloadTasks);
						if (TickFocusManager.Status == PomoStatus.WaitingWork)
						{
							_topBorder.Height = 0.0;
							_bottomBorder.Height = 0.0;
							_timeText.Text = "";
							_pauseText.SetText(null);
						}
						else
						{
							double top = ((pomoStart > _startTime) ? ((pomoStart - _startTime).TotalHours * HourHeight + 10.0) : 10.0);
							double width = Math.Max(2.0, (taskCellViewModel.Width <= 0.0) ? (base.ActualWidth - _leftOffset) : (taskCellViewModel.Width - 2.0));
							_topBorder.Margin = new Thickness(taskCellViewModel.HorizontalOffset, top, 0.0, 0.0);
							_topBorder.Width = width;
							bool flag = TickFocusManager.IsPomo && TickFocusManager.Config.AutoStopTime.HasValue;
							DateTime dateTime3 = (flag ? TickFocusManager.Config.AutoStopTime.Value : DateTime.Now);
							_topBorder.Height = Math.Max(0.0, (dateTime3 - focusDisplayStart).TotalHours * HourHeight);
							_topBorder.CornerRadius = (flag ? new CornerRadius(2.0) : new CornerRadius(2.0, 2.0, 0.0, 0.0));
							_bottomBorder.Height = Math.Max(0.0, pomoing ? (TickFocusManager.Config.CurrentSeconds / 3600.0 * HourHeight) : 0.0);
							_bottomBorder.Margin = new Thickness(taskCellViewModel.HorizontalOffset, _currentLine.Margin.Top, 0.0, 0.0);
							_bottomBorder.Width = width;
							_timeText.Text = DateUtils.GetTimeText(pomoStart) + " - " + DateUtils.GetTimeText(DateTime.Now);
							if (_topBorder.Height >= 15.0)
							{
								_timeText.Margin = new Thickness(_topBorder.Margin.Left + 2.0, _topBorder.Margin.Top + 2.0, 0.0, 0.0);
								_timeText.Foreground = Brushes.White;
								_timeText.Opacity = 0.8;
								_timeText.MaxWidth = Math.Max(10.0, _topBorder.Width - 6.0);
							}
							else
							{
								_timeText.Opacity = 0.0;
							}
							List<(DateTime, DateTime)> pauseSpans = TickFocusManager.Config.GetPauseSpans();
							double num = 0.0;
							foreach (var item in pauseSpans)
							{
								double totalMinutes = (item.Item2 - item.Item1).TotalMinutes;
								num += totalMinutes;
							}
							if (num >= 1.0)
							{
								_pauseText.SetText(Utils.GetString("Pause") + " " + Utils.GetShortDurationString((long)(num * 60.0)));
								_pauseText.Margin = new Thickness(0.0, _topBorder.Margin.Top - 22.0, base.ActualWidth - _topBorder.Margin.Left - _topBorder.Width, 0.0);
							}
							else
							{
								_pauseText.SetText(null);
							}
						}
						return;
					}
					finally
					{
						_taskLoading = false;
					}
				}
				SetCurrentLineOffset();
			}
		});
	}

	private void SetStartTime(DateTime time)
	{
		_startTime = time;
		foreach (FocusTimelineViewModel timeline in _timelines)
		{
			timeline.SetHour(time);
			time = time.AddHours(1.0);
		}
	}

	private async void SetRectColor()
	{
		string text = string.Empty;
		FocusViewModel focusVModel = TickFocusManager.Config.FocusVModel;
		if (!string.IsNullOrEmpty(focusVModel.FocusId))
		{
			text = (focusVModel.IsHabit ? (await HabitDao.GetHabitById(focusVModel.FocusId))?.Color : CacheManager.GetProjectById(TaskCache.GetTaskById(focusVModel.FocusId)?.ProjectId)?.color);
		}
		if (string.IsNullOrEmpty(text) || text.ToLower() == "transparent")
		{
			_topBorder.SetResourceReference(Panel.BackgroundProperty, "PrimaryColor80");
			_bottomBorder.SetResourceReference(Panel.BackgroundProperty, "PrimaryColor10");
			_bottomBorder.SetResourceReference(Border.BorderBrushProperty, "PrimaryColor30");
		}
		else
		{
			_topBorder.Background = ThemeUtil.GetColorInDict(text, 80);
			_bottomBorder.Background = ThemeUtil.GetColorInDict(text, 10);
			_bottomBorder.BorderBrush = ThemeUtil.GetColorInDict(text, 30);
		}
	}

	private async Task<TaskCellViewModel> LoadTasks(DateTime start, DateTime end, bool reloadTasks)
	{
		TaskCellViewModel result = new TaskCellViewModel
		{
			SourceViewModel = new TaskBaseViewModel
			{
				StartDate = start,
				DueDate = end
			},
			BaseOnStart = true
		};
		if (reloadTasks)
		{
			ProjectExtra projectFilter = ProjectExtra.Deserialize(LocalSettings.Settings.CalendarFilterData);
			_validTasks = (await CalendarDisplayService.GetDisplayModels(_startTime, _startTime.AddHours(4.0), withCourses: true, withFocus: false, projectFilter))?.Where((CalendarDisplayModel task) => DateUtils.IsTaskInDay(_startTime, task.DisplayStartDate, task.DisplayDueDate, task.IsAllDay)).ToList();
		}
		if (_validTasks != null && _validTasks.Any())
		{
			List<TaskCellViewModel> list = new List<TaskCellViewModel>();
			if (_validTasks.Count > 0)
			{
				foreach (CalendarDisplayModel validTask in _validTasks)
				{
					if (!(validTask.Id == TickFocusManager.Config.Id))
					{
						list.Add(TaskCellViewModel.Build(validTask, _startTime));
					}
				}
			}
			if (TickFocusManager.Status != PomoStatus.WaitingWork)
			{
				list.Add(result);
			}
			CalendarGeoHelper.AssemblyCells(list, base.ActualWidth - _leftOffset + 9.0 + 5.0, _startTime, _startTime, HourHeight);
			foreach (TaskCellViewModel item in list)
			{
				if (item != result)
				{
					item.HorizontalOffset += _leftOffset;
					item.VerticalOffset += 10.0;
					item.Height -= 0.5;
					item.Width -= 0.5;
					item.ShowInFocus = true;
				}
			}
			list.Remove(result);
			int num = Math.Max(_taskCells.Count, list.Count);
			for (int num2 = 0; num2 < num; num2++)
			{
				if (num2 < list.Count)
				{
					if (num2 < _taskCells.Count)
					{
						_taskCells[num2].DataContext = list[num2];
						continue;
					}
					TaskCell taskCell = new TaskCell(list[num2]);
					taskCell.SetValue(Panel.ZIndexProperty, 40);
					_taskCells.Add(taskCell);
					base.Children.Add(taskCell);
				}
				else
				{
					TaskCell taskCell2 = _taskCells[list.Count];
					_taskCells.Remove(taskCell2);
					base.Children.Remove(taskCell2);
				}
			}
		}
		else
		{
			foreach (TaskCell taskCell3 in _taskCells)
			{
				base.Children.Remove(taskCell3);
			}
			_taskCells.Clear();
		}
		result.HorizontalOffset += _leftOffset;
		return result;
	}
}
