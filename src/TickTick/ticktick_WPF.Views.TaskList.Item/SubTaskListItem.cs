using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.MainListView.TaskList;

namespace ticktick_WPF.Views.TaskList.Item;

public class SubTaskListItem : UserControl, ITaskOperation, IComponentConnector
{
	private DisplayItemController _controller;

	private bool _startDrag;

	private Point _startPoint;

	private bool _mouseDown;

	private DisplayItemModel _model;

	internal Grid ItemGrid;

	internal Grid DragBar;

	internal Grid ItemControl;

	internal ListItemContent TitleContent;

	internal TextBlock TimeText;

	internal Border NavigateGrid;

	internal Grid LineGrid;

	private bool _contentLoaded;

	public SubTaskListItem()
	{
		InitializeComponent();
	}

	public void SetPriority(int priority)
	{
		_controller?.SetPriority(priority);
	}

	public void SetDate(string key)
	{
		TitleContent.TryClearParseDate();
		_controller.SetDate(key);
	}

	public async void ClearDate()
	{
		TitleContent.TitleTextBox.SetCanParseDate(canParseDate: false);
		_controller.ClearDate();
	}

	public void SelectDate(bool relative)
	{
		_controller.SelectDate();
	}

	public void ToggleTaskCompleted()
	{
		if (base.DataContext is DisplayItemModel model)
		{
			_controller.ToggleItemCompleted(model);
		}
	}

	public void Delete()
	{
		_controller?.DeleteSelectedTasks();
	}

	public TaskListView GetParentList()
	{
		return _controller?.GetTaskListView();
	}

	public bool ParsingDate()
	{
		return TitleContent.TitleTextBox.CanParseDate;
	}

	public bool IsNewAdd()
	{
		if (base.DataContext is DisplayItemModel displayItemModel)
		{
			return displayItemModel.IsNewAdd;
		}
		return false;
	}

	public void PinOrUnpinTask()
	{
	}

	public void OpenSticky()
	{
		if (base.DataContext is DisplayItemModel displayItemModel)
		{
			TaskStickyWindow.ShowTaskSticky(new List<string> { displayItemModel.TaskId });
		}
	}

	private void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (base.DataContext is DisplayItemModel displayItemModel)
		{
			SetModel(displayItemModel);
			TimeText.Cursor = ((!displayItemModel.Enable) ? Cursors.Arrow : Cursors.Hand);
			if (_controller == null)
			{
				_controller = new DisplayItemController(this, displayItemModel);
			}
			else
			{
				_controller.Reset(this, displayItemModel);
			}
			if (displayItemModel.InSticky)
			{
				SetStickyMode();
			}
		}
	}

	private void SetStickyMode()
	{
		LineGrid.Visibility = Visibility.Collapsed;
		NavigateGrid.Visibility = Visibility.Collapsed;
		ItemControl.SetResourceReference(FrameworkElement.HeightProperty, "StickyHeight30");
		DragBar.SetResourceReference(FrameworkElement.HeightProperty, "StickyHeight30");
		TimeText.SetResourceReference(Control.FontSizeProperty, "StickyFont12");
		TimeText.Margin = new Thickness(12.0, 0.0, 0.0, 0.0);
		TitleContent.Margin = new Thickness(0.0, -4.0, 0.0, 0.0);
		TitleContent.SetStickyMode();
	}

	private void SetModel(DisplayItemModel model)
	{
		model.SetPropertyChangedEvent();
		if (_model != null)
		{
			PropertyChangedEventManager.RemoveHandler(_model, OnTaskTitleChanged, "Title");
		}
		_model = model;
		PropertyChangedEventManager.AddHandler(_model, OnTaskTitleChanged, "Title");
	}

	private void OnTaskTitleChanged(object sender, PropertyChangedEventArgs e)
	{
		if (!TitleContent.TitleTextBox.KeyboardFocused && base.DataContext is DisplayItemModel displayItemModel && displayItemModel.Title != TitleContent.TitleTextBox.Text)
		{
			TitleContent.TitleTextBox.SetTextOffset(displayItemModel.Title, restoreOffset: true, clearUndo: true);
			TitleContent.SetHintText(string.IsNullOrEmpty(displayItemModel.Title));
		}
	}

	protected virtual void OnRightClick(object sender, MouseButtonEventArgs e)
	{
		if (!(base.DataContext is DisplayItemModel displayItemModel) || (displayItemModel.HitVisible && !displayItemModel.InSticky))
		{
			_controller?.ShowOperationDialogSafely();
		}
	}

	private void TimeTextClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is DisplayItemModel { Enable: not false })
		{
			Window window = Window.GetWindow(this);
			Point point = TranslatePoint(new Point(base.ActualWidth - 180.0, 30.0), window);
			_controller?.SelectDate(window, point.X, point.Y);
		}
	}

	private void NavigateTaskClick(object sender, RoutedEventArgs e)
	{
		_controller?.NavigateTaskClick();
	}

	private void OnDragMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is DisplayItemModel { HitVisible: not false, InSticky: false })
		{
			_startDrag = true;
		}
	}

	private void OnMouseMove(object sender, MouseEventArgs e)
	{
		if (_startDrag && e.LeftButton == MouseButtonState.Pressed)
		{
			_controller?.OnDragMouseDown(sender, e);
		}
		_startDrag = false;
	}

	private void OnMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (!(base.DataContext is DisplayItemModel { HitVisible: false }) && !DragBar.IsMouseOver && !TitleContent.IsIconMouseOver && !NavigateGrid.IsMouseOver)
		{
			if (!Utils.IfCtrlPressed() && !Utils.IfShiftPressed())
			{
				_startPoint = e.GetPosition(this);
				_startDrag = true;
			}
			_mouseDown = true;
		}
	}

	private async void OnItemClick(object sender, MouseButtonEventArgs e)
	{
		if (!(base.DataContext is DisplayItemModel { HitVisible: not false } displayItemModel))
		{
			return;
		}
		if (TitleContent.IsIconMouseOver || TimeText.IsMouseOver)
		{
			_mouseDown = false;
		}
		if (_mouseDown)
		{
			_mouseDown = false;
			_controller?.SelectItem(focusTitle: false, displayItemModel.InSticky);
			if (!TitleContent.TitleTextBox.CurrentFocused && !Utils.IfCtrlPressed() && !Utils.IfShiftPressed())
			{
				await Task.Delay(20);
				TitleContent.TitleTextBox.FocusEnd();
			}
		}
	}

	private void OnItemMouseMove(object sender, MouseEventArgs e)
	{
		if (!TitleContent.TitleTextBox.CurrentFocused && _startDrag && e.LeftButton == MouseButtonState.Pressed)
		{
			Point position = e.GetPosition(this);
			if (!(Math.Abs(position.X - _startPoint.X) > 5.0) && !(Math.Abs(position.Y - _startPoint.Y) > 2.0))
			{
				return;
			}
			_controller?.OnDragMouseDown(sender, e);
		}
		_startDrag = false;
	}

	private void OnDragMouseUp(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
	}

	private void OnDoubleClick(object sender, MouseButtonEventArgs e)
	{
		if (TitleContent.TitleTextBox.TextArea.Selection.Length <= 0 && !DragBar.IsMouseOver && !TitleContent.IsIconMouseOver && base.DataContext is DisplayItemModel { HitVisible: not false, InSticky: false } displayItemModel)
		{
			TaskDetailPopup.TryCloseWindow();
			TaskDetailWindows.ShowTaskWindows(displayItemModel.Id);
			e.Handled = true;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/tasklist/item/subtasklistitem.xaml", UriKind.Relative);
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
			((SubTaskListItem)target).DataContextChanged += OnDataBind;
			((SubTaskListItem)target).MouseMove += OnItemMouseMove;
			((SubTaskListItem)target).MouseRightButtonUp += OnRightClick;
			((SubTaskListItem)target).MouseDoubleClick += OnDoubleClick;
			((SubTaskListItem)target).PreviewMouseLeftButtonDown += OnMouseDown;
			((SubTaskListItem)target).PreviewMouseLeftButtonUp += OnItemClick;
			break;
		case 2:
			ItemGrid = (Grid)target;
			break;
		case 3:
			DragBar = (Grid)target;
			DragBar.PreviewMouseLeftButtonDown += OnDragMouseDown;
			DragBar.PreviewMouseLeftButtonUp += OnDragMouseUp;
			DragBar.MouseMove += OnMouseMove;
			break;
		case 4:
			ItemControl = (Grid)target;
			break;
		case 5:
			TitleContent = (ListItemContent)target;
			break;
		case 6:
			TimeText = (TextBlock)target;
			TimeText.MouseLeftButtonUp += TimeTextClick;
			break;
		case 7:
			NavigateGrid = (Border)target;
			break;
		case 8:
			((Border)target).MouseLeftButtonUp += NavigateTaskClick;
			break;
		case 9:
			LineGrid = (Grid)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
