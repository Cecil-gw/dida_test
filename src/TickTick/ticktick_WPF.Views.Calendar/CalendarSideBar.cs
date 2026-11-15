using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util.DragAndDrop;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.Time;
using ticktick_WPF.Views.Timeline;

namespace ticktick_WPF.Views.Calendar;

public class CalendarSideBar : UserControl, IComponentConnector
{
	private ItemSelection _filter;

	private bool _isAll;

	private CalendarControl _calendarControl;

	private bool _dragOver;

	internal Grid DayPickerGrid;

	internal TickDatePicker DayPicker;

	internal Grid FilterGrid;

	private bool _contentLoaded;

	public SolidColorBrush DayColor { get; set; }

	public bool IsAll => _isAll;

	public event EventHandler<DateTime> DateSelected;

	private void ResetFilterStatus(ProjectExtra data)
	{
		_isAll = data.IsAll;
	}

	public CalendarSideBar()
	{
		InitializeComponent();
		InitFilter();
		base.Loaded += OnLoaded;
		base.Unloaded += delegate
		{
			UnbindEvents();
		};
		DayPicker.SetInCalMode();
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		ResetFilterStatus((_calendarControl != null && _calendarControl.ViewSourceType == CalendarViewSourceType.WithList) ? LocalSettings.Settings.ExtraSettings.CalendarParallelSelectProject : ProjectExtra.Deserialize(LocalSettings.Settings.CalendarFilterData));
		BindEvents();
	}

	private void UnbindEvents()
	{
		DataChangedNotifier.ProjectChanged -= OnProjectChanged;
		DataChangedNotifier.ProjectGroupChanged -= OnProjectGroupChanged;
		DataChangedNotifier.FilterChanged -= OnFilterChanged;
		DataChangedNotifier.CalendarChanged -= OnCalendarChanged;
		TimeChangeNotifier.DayChanged -= OnDayChanged;
		TickDragHelper.DragDrop -= OnDrop;
		TickDragHelper.DragMoving -= OnDragOver;
	}

	private void BindEvents()
	{
		DataChangedNotifier.ProjectChanged += OnProjectChanged;
		DataChangedNotifier.ProjectGroupChanged += OnProjectGroupChanged;
		DataChangedNotifier.FilterChanged += OnFilterChanged;
		DataChangedNotifier.CalendarChanged += OnCalendarChanged;
		TimeChangeNotifier.DayChanged += OnDayChanged;
		TickDragHelper.DragDrop += OnDrop;
		TickDragHelper.DragMoving += OnDragOver;
	}

	private void OnCalendarChanged(object sender, EventArgs e)
	{
		ReloadFilter();
	}

	private void OnDayChanged(object sender, EventArgs e)
	{
		DayPicker.SetDayCells();
	}

	private void OnFilterChanged(object sender, FilterChangeArgs filterChangeArgs)
	{
		ReloadFilter();
	}

	private void OnProjectChanged(object sender, EventArgs e)
	{
		ReloadFilter();
	}

	private void OnProjectGroupChanged(object sender, ProjectGroupModel e)
	{
		ReloadFilter();
	}

	private void OnDaySelected(object sender, DateTime date)
	{
		this.DateSelected?.Invoke(this, date);
	}

	public void SetSelectedRange(DateTime? start, DateTime? end)
	{
		DayPicker.SelectRange(start, end);
	}

	private void InitFilter()
	{
		if (_filter == null)
		{
			_filter = new ItemSelection(null)
			{
				OriginalData = ((_calendarControl != null && _calendarControl.ViewSourceType == CalendarViewSourceType.WithList) ? LocalSettings.Settings.ExtraSettings.CalendarParallelSelectProject : ProjectExtra.Deserialize(LocalSettings.Settings.CalendarFilterData)),
				ShowAll = true,
				ShowFilters = true,
				ShowTags = true,
				ShowSmartProjects = false,
				ShowCalendars = true,
				BatchMode = true,
				CanSelectGroup = true,
				OnlyShowPermission = false,
				ShowSmartAll = false,
				ShowFilterGroup = true,
				IsCalFilter = true,
				ShowListGroup = true,
				UseCalendarSidebarFilterStyle = true
			};
		}
		_filter.ItemSelect -= OnFilterItemSelect;
		_filter.ItemSelect += OnFilterItemSelect;
		FilterGrid.Children.Add(_filter);
	}

	public void SetItemData(bool inList)
	{
		if (_filter != null)
		{
			ProjectExtra originalData = (inList ? (LocalSettings.Settings.ExtraSettings.CalendarParallelSelectProject ?? new ProjectExtra()) : ProjectExtra.Deserialize(LocalSettings.Settings.CalendarFilterData));
			_filter.OriginalData = originalData;
			_filter.LoadData();
		}
	}

	private void OnFilterItemSelect(object sender, SelectableItemViewModel e)
	{
		ProjectExtra selectedData = _filter.GetSelectedData();
		if (_calendarControl != null && _calendarControl.ViewSourceType == CalendarViewSourceType.WithList)
		{
			LocalSettings.Settings.ExtraSettings.CalendarParallelSelectProject = selectedData;
		}
		else
		{
			LocalSettings.Settings.CalendarFilterData = ProjectExtra.Serialize(selectedData);
			UtilLog.Info(LocalSettings.Settings.CalendarFilterData);
		}
		DataChangedNotifier.NotifyCalendarProjectFilterChanged(this);
		ResetFilterStatus(selectedData);
	}

	public void ReloadFilter()
	{
		_filter.ItemSelect -= OnFilterItemSelect;
		_filter.OriginalData = ((_calendarControl != null && _calendarControl.ViewSourceType == CalendarViewSourceType.WithList) ? LocalSettings.Settings.ExtraSettings.CalendarParallelSelectProject : ProjectExtra.Deserialize(LocalSettings.Settings.CalendarFilterData));
		_filter.LoadData();
		_filter.ItemSelect += OnFilterItemSelect;
	}

	public void CheckHoverDayCell(MouseEventArgs e)
	{
		if (base.Opacity != 0.0)
		{
			Point position = e.GetPosition(DayPicker.DayCells);
			double x = position.X;
			double y = position.Y;
			if (x > 0.0 && x < DayPicker.ActualWidth && y > 0.0 && y < DayPicker.ActualHeight)
			{
				double num = DayPicker.DayCells.ActualWidth / 7.0;
				int column = (int)(x / num);
				int row = (int)(y / 34.0);
				DayPicker.SetHoverCell(row, column);
			}
			else
			{
				DayPicker.ClearHoverCell();
			}
		}
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		double num = base.ActualHeight - DayPickerGrid.ActualHeight;
		if (_filter != null)
		{
			if (num != double.NaN && num > 250.0)
			{
				_filter.SelectableItems.MaxHeight = num;
			}
			else
			{
				_filter.SelectableItems.MaxHeight = 250.0;
			}
		}
	}

	public void SetMonth(DateTime monthDate)
	{
		DayPicker.SetPivotDate(monthDate);
	}

	public void SetCalendar(CalendarControl calendarControl)
	{
		_calendarControl = calendarControl;
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
				OnDragLeave();
			}
			_dragOver = flag;
		}
		if (flag)
		{
			CheckHoverDayCell(e);
		}
	}

	private async void OnDrop(object sender, MouseEventArgs e)
	{
		if (!_dragOver)
		{
			return;
		}
		_dragOver = false;
		DateTime? dropDate = DayPicker.GetHoverDate();
		if (dropDate.HasValue)
		{
			object data = TickDragHelper.GetDragData();
			object obj = data;
			if (!(obj is DisplayItemModel displayItemModel))
			{
				if (!(obj is TimelineCellViewModel timelineCellViewModel))
				{
					if (!(obj is CheckItemViewModel checkItemViewModel))
					{
						if (obj is CalendarDisplayViewModel calendarDisplayViewModel)
						{
							HashSet<string> draggingTaskIds = TickDragHelper.DraggingTaskIds;
							if (draggingTaskIds == null || draggingTaskIds.Count <= 1)
							{
								await calendarDisplayViewModel.SaveDate(dropDate, Window.GetWindow(this));
							}
							else
							{
								List<TaskBaseViewModel> tasksByIds = TaskCache.GetTasksByIds(TickDragHelper.DraggingTaskIds.ToList());
								foreach (TaskBaseViewModel item in tasksByIds)
								{
									await item.SetDropDate(dropDate.Value, Window.GetWindow(this));
								}
							}
						}
					}
					else if (UserDao.IsPro())
					{
						await checkItemViewModel.SourceViewModel.SetDropDate(dropDate.Value, Window.GetWindow(this));
					}
				}
				else if (timelineCellViewModel.BatchSelected)
				{
					List<TaskBaseViewModel> tasksByIds2 = TaskCache.GetTasksByIds(TickDragHelper.DraggingTaskIds?.ToList());
					foreach (TaskBaseViewModel item2 in tasksByIds2)
					{
						await item2.SetDropDate(dropDate.Value, Window.GetWindow(this));
					}
				}
				else
				{
					timelineCellViewModel.DisplayModel.SetDropDate(dropDate.Value, Window.GetWindow(this));
				}
			}
			else if (!displayItemModel.BatchMode)
			{
				await displayItemModel.SourceViewModel.SetDropDate(dropDate.Value, Window.GetWindow(this));
			}
			else
			{
				List<TaskBaseViewModel> tasksByIds3 = TaskCache.GetTasksByIds(displayItemModel.BatchSelectedIds);
				foreach (TaskBaseViewModel item3 in tasksByIds3)
				{
					await item3.SetDropDate(dropDate.Value, Window.GetWindow(this));
				}
			}
			_calendarControl?.CheckItemFiltered(data);
		}
		DayPicker.ClearHoverCell();
	}

	private void OnDragLeave()
	{
		DayPicker.ClearHoverCell();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/calendar/calendarsidebar.xaml", UriKind.Relative);
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
			((CalendarSideBar)target).SizeChanged += OnSizeChanged;
			break;
		case 2:
			DayPickerGrid = (Grid)target;
			break;
		case 3:
			DayPicker = (TickDatePicker)target;
			break;
		case 4:
			FilterGrid = (Grid)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
