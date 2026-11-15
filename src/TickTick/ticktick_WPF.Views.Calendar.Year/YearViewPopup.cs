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
using KotlinModels;
using TickTickUtils.Lang;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Undo;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Views.Calendar.Year;

public class YearViewPopup : MyWindow, IDragBarEvent, IToastShowWindow, IComponentConnector
{
	private readonly IToastShowWindow _handler;

	private bool _closing;

	public FrameworkElement Context;

	private Point _cellCenter;

	private double _cellWidth;

	private static YearViewPopup _window;

	private static DateTime _lastCloseTime;

	private static DateTime _lastClosedDate;

	internal Grid ContentBorder;

	internal new TextBlock Title;

	internal ScrollViewer TaskScrollViewer;

	internal StackPanel TaskPanel;

	internal TextBlock EmptyText;

	internal Image ArrowPath;

	internal ScaleTransform ArrowPathTransform;

	private bool _contentLoaded;

	private DateTime Date { get; }

	public IDragBarEvent DragBarEvent { get; set; }

	public static bool IsOpen
	{
		get
		{
			if (_window != null)
			{
				return !_window._closing;
			}
			return false;
		}
	}

	public event EventHandler Disappear;

	public YearViewPopup(DateTime date, FrameworkElement context, Point cellCenter, double cellWidth, string themeId = "", IToastShowWindow undo = null)
	{
		if (!string.IsNullOrEmpty(themeId))
		{
			ThemeUtil.SetTheme(themeId, this);
		}
		Date = date;
		Context = context;
		_cellCenter = cellCenter;
		_cellWidth = cellWidth;
		_handler = undo;
		InitializeComponent();
		_window = this;
	}

	public bool OnSelection()
	{
		return false;
	}

	public DateTime? DragSideOver(MouseEventArgs e, CalendarDisplayViewModel model, bool dragLeft)
	{
		return null;
	}

	public void SetBarDragging(bool b)
	{
	}

	public bool GetBarDragging()
	{
		return false;
	}

	public void TryHideToast()
	{
	}

	public void TaskDeleted(string taskId)
	{
		_handler?.TaskDeleted(taskId);
	}

	public void ToastDeleteRecUndo(List<TaskDeleteRecurrenceUndoEntity> undoModels)
	{
		_handler?.ToastDeleteRecUndo(undoModels);
	}

	public void Toast(FrameworkElement uiElement)
	{
		_handler?.Toast(uiElement);
	}

	public void ToastMoveProjectControl(string projectId, string taskName = null, MoveToastType moveType = MoveToastType.Move)
	{
		_handler?.ToastMoveProjectControl(projectId, taskName);
	}

	public void TryToastString(object obj, string toastString)
	{
		_handler?.TryToastString(obj, toastString);
	}

	public async Task<bool> BatchDeleteTask(List<TaskModel> tasks)
	{
		return await (_handler?.BatchDeleteTask(tasks));
	}

	public void TaskComplete(CloseUndoToast undo)
	{
		_handler?.TaskComplete(undo);
	}

	public static bool IsOpenForDate(DateTime date)
	{
		if (_window != null && !_window._closing)
		{
			return _window.Date == date;
		}
		return false;
	}

	public static bool WasRecentlyClosedForDate(DateTime date)
	{
		if ((DateTime.Now - _lastCloseTime).TotalMilliseconds < 200.0)
		{
			return _lastClosedDate == date;
		}
		return false;
	}

	public static void TryClose()
	{
		YearViewPopup window = _window;
		if (window != null && !window._closing)
		{
			window._closing = true;
			window.Close();
		}
	}

	public async Task ShowPopup(List<CalendarDisplayViewModel> children)
	{
		Title.Text = DateUtils.FormatYearMonthDay(Date, DateFormatLen.Middle);
		if (children == null || children.Count == 0)
		{
			TaskScrollViewer.Visibility = Visibility.Collapsed;
			EmptyText.Visibility = Visibility.Visible;
		}
		else
		{
			TaskScrollViewer.Visibility = Visibility.Visible;
			EmptyText.Visibility = Visibility.Collapsed;
			LoadTasks(children);
		}
		base.Left = -10000.0;
		base.Top = -10000.0;
		ArrowPath.Visibility = Visibility.Hidden;
		Show();
		await SetWindowLocation();
	}

	private void LoadTasks(List<CalendarDisplayViewModel> models)
	{
		TaskPanel.Children.Clear();
		if (models == null || models.Count == 0)
		{
			TaskScrollViewer.Visibility = Visibility.Collapsed;
			EmptyText.Visibility = Visibility.Visible;
			return;
		}
		TaskScrollViewer.Visibility = Visibility.Visible;
		EmptyText.Visibility = Visibility.Collapsed;
		CalendarControl calendarControl = Utils.FindParent<CalendarControl>(Context);
		models = models.OrderBy((CalendarDisplayViewModel m) => m.Row).ToList();
		foreach (CalendarDisplayViewModel model in models)
		{
			model.BatchSelected = model.CanBatchSelected && calendarControl?.BatchSelected?.Contains(model.TaskId) == true;
			AddTaskBar(model.MemberwiseClone1());
		}
	}

	private void AddTaskBar(CalendarDisplayViewModel model)
	{
		TaskBar taskBar = new TaskBar
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			Mode = TaskbarMode.LoadMore
		};
		TaskPanel.Children.Add(taskBar);
		int num = ((LocalSettings.Settings.TimeFormat == "12Hour") ? 9 : 7);
		model.WidthEnoughShowIcon = base.Width >= CalendarGeoHelper.CalTitleFontSize * (double)(num + 3) + 16.0;
		bool showIcon = !model.IsTaskIconType || model.WidthEnoughShowIcon;
		taskBar.SetShowIcon(showIcon, model.ForceShowIcon);
		taskBar.TimeText.Visibility = ((!(base.Width > CalendarGeoHelper.CalTitleFontSize * (double)num + 16.0)) ? Visibility.Collapsed : Visibility.Visible);
		taskBar.DataContext = model;
	}

	private void OnPopupClosed(object sender, EventArgs e)
	{
		Point position = Mouse.GetPosition(this);
		if (position.X > base.ActualWidth - 10.0 || position.X < 10.0 || position.Y < 10.0 || position.Y > base.ActualHeight - 10.0)
		{
			Close();
		}
		else
		{
			Activate();
		}
	}

	protected override async void OnDeactivated(EventArgs e)
	{
		base.OnDeactivated(e);
		await Task.Delay(10);
		Point position = Mouse.GetPosition(this);
		if ((position.X > base.ActualWidth - 10.0 || position.X < 10.0 || position.Y < 10.0 || position.Y > base.ActualHeight - 10.0) && !_closing)
		{
			_closing = true;
			try
			{
				Close();
			}
			catch (Exception)
			{
			}
		}
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		_lastCloseTime = DateTime.Now;
		_lastClosedDate = Date;
		_window = null;
		TaskPanel.Children.Clear();
		this.Disappear?.Invoke(this, null);
		base.OnClosing(e);
	}

	private void OnWindowLoaded(object sender, RoutedEventArgs e)
	{
	}

	public static void TryUpdateEventTitle(string eventId, string title)
	{
		YearViewPopup window = _window;
		if (window == null || window._closing)
		{
			return;
		}
		foreach (object child in window.TaskPanel.Children)
		{
			if (child is TaskBar { DataContext: CalendarDisplayViewModel dataContext } && dataContext.EventId == eventId)
			{
				dataContext.SourceViewModel.Title = title;
			}
		}
	}

	private async Task SetWindowLocation()
	{
		Window window = Utils.GetParentWindow(Context);
		if (window != null)
		{
			await Task.Delay(10);
			Point point = Context.TranslatePoint(_cellCenter, window);
			double num = point.X + _cellWidth / 2.0;
			double num2 = point.X - _cellWidth / 2.0;
			double num3 = window.GetActualLeft() + num;
			double num4 = 12.0 + ContentBorder.ActualHeight / 2.0;
			double num5 = window.GetActualTop() + point.Y - num4;
			bool flag = false;
			double num6 = window.GetActualLeft() + window.ActualWidth;
			if (num3 + base.ActualWidth > num6 - 10.0)
			{
				flag = true;
				num3 = window.GetActualLeft() + num2 - base.ActualWidth;
			}
			double actualTop = window.GetActualTop();
			double num7 = window.GetActualTop() + window.ActualHeight;
			if (num5 + base.ActualHeight > num7 - 10.0)
			{
				num5 = num7 - base.ActualHeight - 10.0;
			}
			if (num5 < actualTop + 10.0)
			{
				num5 = actualTop + 10.0;
			}
			double top = window.GetActualTop() + point.Y - num5 - 12.0 - 6.0 - 1.0;
			if (flag)
			{
				ArrowPathTransform.ScaleX = -1.0;
				ArrowPath.Margin = new Thickness(0.0, top, -4.0, 0.0);
				ArrowPath.HorizontalAlignment = HorizontalAlignment.Right;
			}
			else
			{
				ArrowPathTransform.ScaleX = 1.0;
				ArrowPath.Margin = new Thickness(-4.0, top, 0.0, 0.0);
				ArrowPath.HorizontalAlignment = HorizontalAlignment.Left;
			}
			base.Left = num3;
			base.Top = num5;
			WindowHelper.MoveTo(this, (int)num3, (int)num5);
			base.Left = num3;
			base.Top = num5;
			ArrowPath.UpdateLayout();
			ArrowPath.Visibility = Visibility.Visible;
		}
	}

	public void RemoveItem(TaskBar taskBar)
	{
		if (taskBar != null)
		{
			TaskPanel.Children.Remove(taskBar);
		}
	}

	public static void TrySetModels(DateTime date, List<CalendarDisplayViewModel> models)
	{
		if (_window != null && _window.Date == date)
		{
			models = models ?? new List<CalendarDisplayViewModel>();
			_window?.LoadTasks(models);
		}
	}

	public static async void TryRefresh()
	{
		YearViewPopup window = _window;
		if (window != null && !window._closing && window.Context is MonthCalendarDraw monthCalendarDraw)
		{
			List<CalendarDisplayViewModel> list = await monthCalendarDraw.GetTasksForDate(window.Date);
			if (_window == window && !window._closing)
			{
				window.LoadTasks(list ?? new List<CalendarDisplayViewModel>());
				await Task.Delay(100);
				window.SetWindowLocation();
			}
		}
	}

	public async void GetFocus()
	{
		await Task.Delay(250);
		if (PopupStateManager.IsViewPopOpened())
		{
			return;
		}
		try
		{
			FocusManager.SetFocusedElement(this, this);
			Keyboard.Focus(this);
		}
		catch (Exception)
		{
			UtilLog.Error("got focus error");
		}
	}

	public DateTime GetDate()
	{
		return Date;
	}

	public static void ExitBatchSelected()
	{
		if (_window == null)
		{
			return;
		}
		foreach (object child in _window.TaskPanel.Children)
		{
			if (child is TaskBar { DataContext: CalendarDisplayViewModel dataContext })
			{
				dataContext.BatchSelected = false;
			}
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/calendar/year/yearviewpopup.xaml", UriKind.Relative);
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
			ContentBorder = (Grid)target;
			break;
		case 2:
			Title = (TextBlock)target;
			break;
		case 3:
			TaskScrollViewer = (ScrollViewer)target;
			break;
		case 4:
			TaskPanel = (StackPanel)target;
			break;
		case 5:
			EmptyText = (TextBlock)target;
			break;
		case 6:
			ArrowPath = (Image)target;
			break;
		case 7:
			ArrowPathTransform = (ScaleTransform)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
