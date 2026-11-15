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
using KotlinModels;
using TickTickUtils.Lang;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Undo;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Views.Calendar;

public class LoadMoreWindow : MyWindow, IDragBarEvent, IToastShowWindow, IComponentConnector
{
	private readonly IToastShowWindow _handler;

	private bool _closing;

	public FrameworkElement Context;

	private static LoadMoreWindow _window;

	internal ContentControl Container;

	internal new TextBlock Title;

	internal StackPanel TaskPanel;

	private bool _contentLoaded;

	private DateTime Date { get; }

	public IDragBarEvent DragBarEvent { get; set; }

	public event EventHandler Disappear;

	public LoadMoreWindow(DateTime date, FrameworkElement element, bool isLastRow = true, string themeId = "", IToastShowWindow undo = null, bool locked = false)
	{
		if (!string.IsNullOrEmpty(themeId))
		{
			ThemeUtil.SetTheme(themeId, this);
		}
		Date = date;
		Context = element;
		_handler = undo;
		InitializeComponent();
		if (locked)
		{
			TaskPanel.IsEnabled = false;
		}
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

	public static void TryClose()
	{
		LoadMoreWindow window = _window;
		if (window != null && !window._closing)
		{
			window._closing = true;
			window.Close();
		}
	}

	public async Task ShowPopup(List<CalendarDisplayViewModel> children)
	{
		Title.Text = DateUtils.FormatYearMonthDay(Date, DateFormatLen.Middle);
		LoadTasks(children);
		Show();
	}

	private void LoadTasks(List<CalendarDisplayViewModel> models)
	{
		TaskPanel.Children.Clear();
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
		if (AppLockWindowManager.IsLocked)
		{
			return;
		}
		Point position = Mouse.GetPosition(this);
		if ((!(position.X > base.ActualWidth - 10.0) && !(position.X < 10.0) && !(position.Y < 10.0) && !(position.Y > base.ActualHeight - 10.0)) || _closing)
		{
			return;
		}
		_closing = true;
		try
		{
			Close();
		}
		catch (Exception)
		{
		}
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		_window = null;
		TaskPanel.Children.Clear();
		this.Disappear?.Invoke(this, null);
		base.OnClosing(e);
	}

	private void OnWindowLoaded(object sender, RoutedEventArgs e)
	{
		SetWindowLocation();
	}

	private async Task SetWindowLocation()
	{
		Window parentWindow = Utils.GetParentWindow(Context);
		if (parentWindow != null)
		{
			Point point = Context.TranslatePoint(new Point(0.0, Context.ActualHeight), parentWindow);
			double num = parentWindow.GetActualLeft() + point.X - 4.0;
			double num2 = parentWindow.GetActualTop() + point.Y - base.ActualHeight - 6.0;
			double num3 = num;
			double num4 = num2;
			base.Left = num3;
			base.Top = num4;
			WindowHelper.MoveTo(this, (int)num3, (int)num4);
			base.Left = num3;
			base.Top = num4;
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
			Uri resourceLocator = new Uri("/TickTick;component/views/calendar/loadmorewindow.xaml", UriKind.Relative);
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
			Container = (ContentControl)target;
			break;
		case 2:
			Title = (TextBlock)target;
			break;
		case 3:
			TaskPanel = (StackPanel)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
