using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using Emoji.Wpf;
using ticktick_WPF.Util;
using ticktick_WPF.Util.DragAndDrop;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Detail;

namespace ticktick_WPF.Views.Timeline;

public class TimelineCellFloating : TimelineCellBase, IComponentConnector
{
	public static readonly DependencyProperty ArrangingProperty = DependencyProperty.Register("Arranging", typeof(bool), typeof(TimelineCellFloating), new PropertyMetadata(false));

	private bool _reopenHoverBorder;

	private TimelineCellBase _currentUi;

	private readonly TTAsyncLocker _locker = new TTAsyncLocker(1, 1);

	private DateTime _originStart;

	private DateTime? _originEnd;

	private double _originLeft;

	private int _originLine;

	private Point _originPoint;

	private Point _startPoint;

	private bool _startDrag;

	private int? _startDragLine;

	private bool _showTextTips;

	internal TimelineCellFloating Root;

	internal Grid Cell;

	internal Rectangle BackgroundBorder;

	internal Grid InfoTile;

	internal EmjTextBlock TitleText;

	internal TaskCircleProgress Progress;

	internal Border AvatarBorder;

	internal Thumb StartDateThumb;

	internal Thumb EndDateThumb;

	internal Popup TipsPopup;

	internal Border TipsPanel;

	private bool _contentLoaded;

	public bool Arranging
	{
		get
		{
			return (bool)GetValue(ArrangingProperty);
		}
		set
		{
			SetValue(ArrangingProperty, value);
		}
	}

	public TimelineCellFloating()
	{
		InitializeComponent();
	}

	private async void OnEndDateThumbDragDelta(object sender, DragDeltaEventArgs e)
	{
		if (!CheckEditable())
		{
			return;
		}
		base.timelineModel.ClearBatchSelect();
		await UpdateModel();
		if (cellModel != null)
		{
			double left = Canvas.GetLeft(Cell);
			Point point = _currentUi.TranslatePoint(new Point(0.0, 0.0), base.Container?.MainScroll);
			if (Math.Abs(left - point.X) > 0.1)
			{
				Canvas.SetLeft(Cell, point.X);
			}
			TimelineCellViewModel timelineCellViewModel = cellModel;
			timelineCellViewModel.SetWidth();
			if (Math.Abs(timelineCellViewModel.Width - InfoTile.Width) > 0.1)
			{
				InfoTile.Width = timelineCellViewModel.Width;
				base.Container?.MoveToolTips(new Point(point.X + timelineCellViewModel.Width, 0.0));
			}
		}
	}

	private async void OnStartDateThumbDragDelta(object sender, DragDeltaEventArgs e)
	{
		if (!CheckEditable())
		{
			return;
		}
		base.timelineModel.ClearBatchSelect();
		await UpdateModel();
		if (cellModel != null)
		{
			double left = Canvas.GetLeft(Cell);
			double valueOrDefault = (cellModel.Left - base.Container?.MainScroll.HorizontalOffset).GetValueOrDefault();
			if (Math.Abs(left - valueOrDefault) > 0.1)
			{
				Canvas.SetLeft(Cell, valueOrDefault);
				base.Container?.MoveToolTips(new Point(valueOrDefault, 0.0));
			}
			TimelineCellViewModel timelineCellViewModel = cellModel;
			timelineCellViewModel.SetWidth();
			if (Math.Abs(timelineCellViewModel.Width - InfoTile.Width) > 0.1)
			{
				InfoTile.Width = timelineCellViewModel.Width;
			}
		}
	}

	private async Task OnItemMouseUp(bool openWindow)
	{
		base.Container?.StopMoveView();
		TitleText.SetBinding(EmjTextBlock.TextProperty, "Title");
		TimelineCellViewModel currentCell = cellModel;
		TimelineViewModel parentModel = base.timelineModel;
		if (ModelInvalid())
		{
			return;
		}
		if (IsCursorDisabled())
		{
			RestoreOriginProperty(currentCell);
			ResetCursor();
			currentCell.DragStatus = 0;
			base.timelineModel.SetBatchDragging(0, delay: true);
			base.timelineModel.ResetBatchDates();
			Close();
			return;
		}
		ResetCursor();
		ResetHoverBorder();
		TipsPopup.IsOpen = false;
		if (openWindow)
		{
			parentModel.Hovering = false;
			base.Container?.ClearCopiedTaskSelection(currentCell.Id);
			if (Utils.IfCtrlPressed())
			{
				if (currentCell.DisplayModel.IsTaskOrNote)
				{
					if (!currentCell.Editable)
					{
						base.Container?.TryToastString(Utils.GetString("NoEditingPermission"));
						return;
					}
					TaskDetailPopup.TryCloseWindow();
					parentModel.ChangeBatchSelectStatus(currentCell);
					bool thumbVisibility = cellModel.BatchSelected || (cellModel.DisplayModel.Type != DisplayType.Task && cellModel.DisplayModel.Type != DisplayType.Agenda) || !cellModel.Editable;
					SetThumbVisibility(thumbVisibility);
				}
				else
				{
					base.Container?.TryToastString(Utils.GetString("OperationNotSupport"));
				}
			}
			else if (_currentUi != null && base.IsMouseOver)
			{
				await _currentUi.OpenDetailWindow();
			}
		}
		else
		{
			base.timelineModel.Hovering = false;
			await currentCell.CommitGroup(currentCell.Line);
			await currentCell.CommitDate(null);
			parentModel.UpdateCellTime(currentCell, ignoreEdit: true);
		}
	}

	private async void OnItemDragCompleted(object sender, DragCompletedEventArgs e)
	{
		UserActCollectUtils.AddClickEvent("timeline", "task_action", "drag_task_length");
		await OnItemMouseUp(e.HorizontalChange == 0.0 || !(cellModel?.Editable ?? false));
	}

	private void UpdateCursor()
	{
		double x = Mouse.GetPosition(this).X;
		double num = (base.timelineModel.IsArranging ? 215.0 : 0.0);
		double num2 = (base.timelineModel.ShowGroup ? base.timelineModel.GroupWidth : 0.0);
		if (x > base.ActualWidth - num || x < num2)
		{
			DisableCursor();
		}
		else
		{
			ResetCursor();
		}
	}

	private bool IsCursorDisabled()
	{
		if (InfoTile.Cursor != Cursors.No && StartDateThumb.Cursor != Cursors.No)
		{
			return EndDateThumb.Cursor == Cursors.No;
		}
		return true;
	}

	private void ResetCursor()
	{
		InfoTile.Cursor = Cursors.Hand;
		StartDateThumb.Cursor = Cursors.SizeWE;
		EndDateThumb.Cursor = Cursors.SizeWE;
	}

	private void DisableCursor()
	{
		InfoTile.Cursor = Cursors.No;
		StartDateThumb.Cursor = Cursors.No;
		EndDateThumb.Cursor = Cursors.No;
	}

	private async Task UpdateModel()
	{
		if (!ModelInvalid() && cellModel.Editable)
		{
			UpdateCursor();
			await _locker.RunAsync((Action)UpdateModelInternal);
		}
	}

	private void UpdateModelInternal()
	{
		if (cellModel.Operation.Contain(TimelineCellOperation.Start) || cellModel.Operation.Contain(TimelineCellOperation.End))
		{
			double offset = 0.0;
			if (cellModel.Operation.Contain(TimelineCellOperation.Start))
			{
				offset = (Mouse.GetPosition(base.Container?.MainScroll).X + base.Container?.MainScroll.HorizontalOffset).GetValueOrDefault();
			}
			if (cellModel.Operation.Contain(TimelineCellOperation.End))
			{
				offset = Mouse.GetPosition(InfoTile).X;
			}
			cellModel.UpdatePosition(offset, base.timelineModel);
		}
	}

	private void OnItemDragStarted(object sender, DragStartedEventArgs e)
	{
		if (!(sender is FrameworkElement { Tag: var tag }) || !(tag is TimelineCellOperation timelineCellOperation))
		{
			return;
		}
		TimelineCellViewModel timelineCellViewModel = cellModel;
		if (timelineCellViewModel != null && !ModelInvalid() && timelineCellViewModel.Editable)
		{
			if (timelineCellOperation != TimelineCellOperation.Full)
			{
				TaskDetailPopup.TryCloseWindow(force: true);
			}
			else
			{
				base.Container?.TryCloseToolTips(force: true);
			}
			timelineCellViewModel.Operation = timelineCellViewModel.Operation.SetPos(timelineCellOperation);
			SetHoverBorder();
			if (base.timelineModel != null)
			{
				base.timelineModel.Hovering = true;
			}
		}
	}

	private void OnMouseRightUp(object sender, MouseButtonEventArgs e)
	{
		TimelineCellViewModel timelineCellViewModel = cellModel;
		if (!_startDrag && timelineCellViewModel != null)
		{
			if (timelineCellViewModel.DisplayModel.IsTaskOrNote && !timelineCellViewModel.Editable)
			{
				base.Container?.TryToastString(Utils.GetString("NoEditingPermission"));
			}
			else if (timelineCellViewModel.DisplayModel.Type == DisplayType.CheckItem)
			{
				base.Container?.TryToastString(Utils.GetString("OperationNotSupport"));
			}
			else
			{
				base.Container?.ShowMenuPopup(timelineCellViewModel);
			}
		}
	}

	private void SaveOriginProperty(TimelineCellViewModel model)
	{
		_originStart = model.StartDate;
		_originEnd = model.EndDate;
		_originLeft = model.Left;
		_originLine = model.Line;
	}

	private void RestoreOriginProperty(TimelineCellViewModel model)
	{
		model.TrySetStartDate(_originStart);
		model.EndDate = _originEnd;
		model.Left = _originLeft;
		model.Line = _originLine;
	}

	private void SetThumbVisibility(bool hide)
	{
		StartDateThumb.Visibility = (hide ? Visibility.Hidden : Visibility.Visible);
		StartDateThumb.IsHitTestVisible = !hide;
		EndDateThumb.Visibility = (hide ? Visibility.Hidden : Visibility.Visible);
		EndDateThumb.IsHitTestVisible = !hide;
	}

	public void Open(TimelineCellViewModel timelineCellViewModel, TimelineCellBase ui)
	{
		double width;
		Point point;
		if (ui != null && timelineCellViewModel != null && timelineCellViewModel != base.DataContext)
		{
			_currentUi = ui;
			Arranging = false;
			RemoveAndResetDataContext(timelineCellViewModel);
			if (ModelInvalid())
			{
				Utils.Toast("Invalid Model");
			}
			else if (!cellModel.PlayingAnimation)
			{
				SaveOriginProperty(timelineCellViewModel);
				string groupBy = base.timelineModel.TimelineSortOption.groupBy;
				timelineCellViewModel.CanDragSwitchLine = groupBy != "tag" && (!timelineCellViewModel.BatchSelected || !base.timelineModel.IsBatchSelectOverGroup());
				width = Math.Max(base.timelineModel.OneDayWidth - 6.0, cellModel.Width);
				InfoTile.Width = width;
				SetHoverBorder();
				SetTextProgressAndAvatarWidth(timelineCellViewModel);
				point = ui.TranslatePoint(new Point(0.0, 0.0), base.Container?.MainScroll);
				Canvas.SetLeft(Cell, point.X);
				Canvas.SetTop(Cell, point.Y);
				base.Visibility = Visibility.Visible;
				DelayActionHandlerCenter.TryDoAction("ShowTimelineFloatingThumb", ShowThumb, 200);
			}
		}
		void ShowThumb(object sender, EventArgs eventArgs)
		{
			base.Dispatcher.Invoke(delegate
			{
				if (cellModel != null)
				{
					bool thumbVisibility = cellModel.BatchSelected || (cellModel.DisplayModel.Type != DisplayType.Task && cellModel.DisplayModel.Type != DisplayType.Agenda) || !cellModel.Editable;
					SetThumbVisibility(thumbVisibility);
					if (width < 24.0 && !cellModel.BatchSelected)
					{
						double num = ((width <= 24.0) ? 5.0 : 0.0);
						width = Math.Max(width, 24.0);
						InfoTile.Width = width;
						Canvas.SetLeft(Cell, point.X - num);
						SetTextProgressAndAvatarWidth(timelineCellViewModel);
					}
				}
			});
		}
	}

	public void OpenArrange(TimelineCellViewModel timelineCellViewModel)
	{
		Arranging = true;
		RemoveAndResetDataContext(timelineCellViewModel);
		SaveOriginProperty(timelineCellViewModel);
		if (ModelInvalid())
		{
			Utils.Toast("Invalid Model");
			return;
		}
		base.timelineModel.Hovering = true;
		bool flag = cellModel.DisplayModel.Kind == "NOTE";
		SetThumbVisibility(flag);
		if (!cellModel.DisplayModel.StartDate.HasValue && !flag)
		{
			InfoTile.Width = base.timelineModel.OneDayWidth * (double)base.timelineModel.NewTaskDefaultDays;
		}
		else
		{
			InfoTile.Width = Math.Max(base.timelineModel.OneDayWidth - 6.0, Math.Max(cellModel.Width, 24.0));
		}
		Point position = Mouse.GetPosition(base.Container.MainScroll);
		position.X -= InfoTile.Width / 2.0;
		Canvas.SetLeft(Cell, position.X);
		Canvas.SetTop(Cell, position.Y - 20.0);
		base.Visibility = Visibility.Visible;
		SetTextProgressAndAvatarWidth(timelineCellViewModel);
		if (!cellModel.DisplayModel.StartDate.HasValue)
		{
			int dayOffsetByPoint = base.Container.GetDayOffsetByPoint(position);
			if (flag)
			{
				cellModel.TrySetStartDate(base.timelineModel.StartDate.AddDays(dayOffsetByPoint));
				cellModel.EndDate = null;
			}
			else if (base.timelineModel.NewTaskDefaultDays > 1)
			{
				int num = base.timelineModel.NewTaskDefaultDays / 2;
				timelineCellViewModel.TrySetStartDate(base.timelineModel.StartDate.AddDays(dayOffsetByPoint - num));
				timelineCellViewModel.EndDate = timelineCellViewModel.StartDate.AddDays(base.timelineModel.NewTaskDefaultDays);
				timelineCellViewModel.IsAllDay = true;
			}
			else
			{
				timelineCellViewModel.TrySetStartDate(base.timelineModel.StartDate.AddDays(dayOffsetByPoint));
			}
		}
		MouseButtonEventArgs e = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
		{
			RoutedEvent = Mouse.MouseDownEvent
		};
		OnFullMouseDown(InfoTile, e);
	}

	private void SetTextProgressAndAvatarWidth(TimelineCellViewModel model)
	{
		if (model == null)
		{
			return;
		}
		int num = 14;
		double num2 = InfoTile.Width;
		bool flag = model.AvatarUrl != "-1" && !string.IsNullOrEmpty(model.AvatarUrl);
		bool flag2 = model.Progress > 0 && model.Status == 0;
		double num3 = (string.IsNullOrEmpty(model.Title) ? 0.0 : Utils.MeasureString(model.Title, new Typeface(Utils.GetDefaultFontFamily(), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), 12.0).Width);
		if (num3 < 70.0)
		{
			if (flag2 && num2 <= 40.0)
			{
				flag2 = num2 - (double)num - num3 >= 16.0;
			}
			if (flag && num2 <= 70.0)
			{
				flag = num2 - (double)num - num3 - (double)(flag2 ? 16 : 0) >= 22.0;
			}
		}
		else
		{
			flag2 = flag2 && num2 > 40.0;
			flag = flag && num2 > 70.0;
		}
		num += (flag2 ? 16 : 0);
		num += (flag ? 22 : 0);
		if (!double.IsPositiveInfinity(num2))
		{
			num2 -= (double)num;
			if (num2 < 6.0)
			{
				num2 = 0.0;
			}
		}
		TitleText.MaxWidth = ((!model.Inline) ? Math.Min(num2, 120.0) : num2);
		_showTextTips = TitleText.MaxWidth < num3;
		Progress.Visibility = ((!flag2) ? Visibility.Collapsed : Visibility.Visible);
		AvatarBorder.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void RemoveAndResetDataContext(TimelineCellViewModel newContext)
	{
		if (base.DataContext is TimelineCellViewModel timelineCellViewModel && timelineCellViewModel != newContext)
		{
			timelineCellViewModel.Operation = timelineCellViewModel.Operation.Remove(TimelineCellOperation.Hover);
		}
		base.DataContext = newContext;
		newContext.Operation = newContext.Operation.Add(TimelineCellOperation.Hover);
	}

	public void TryClose(TimelineCellViewModel vm)
	{
		if ((base.timelineModel == null || !base.timelineModel.Hovering) && base.DataContext == vm && !base.IsMouseOver)
		{
			Close();
		}
	}

	public void Close()
	{
		TimelineViewModel timelineViewModel = base.timelineModel;
		if (base.DataContext is TimelineCellViewModel timelineCellViewModel)
		{
			timelineCellViewModel.Operation = timelineCellViewModel.Operation.RemovePos() & timelineCellViewModel.Operation.Remove(TimelineCellOperation.Hover);
			base.Container?.TryCloseToolTips();
			base.DataContext = null;
		}
		ResetHoverBorder();
		base.Visibility = Visibility.Collapsed;
		TipsPopup.IsOpen = false;
		_startDrag = false;
		StartDateThumb.Visibility = Visibility.Hidden;
		EndDateThumb.Visibility = Visibility.Hidden;
		if (timelineViewModel != null)
		{
			timelineViewModel.Hovering = false;
		}
	}

	private async void OnMouseLeave(object sender, MouseEventArgs e)
	{
		DelayActionHandlerCenter.RemoveAction("ShowTimelineFloatingThumb");
		if (ModelInvalid())
		{
			Close();
			return;
		}
		if (base.timelineModel.Editing)
		{
			Close();
			return;
		}
		while (base.timelineModel?.Hovering ?? false)
		{
			await Task.Delay(100);
		}
		Close();
	}

	private void SetHoverBorder()
	{
		_reopenHoverBorder = true;
		cellModel?.UpdateHoverBorderPosition(base.timelineModel);
		base.Container.HoverHeader.Visibility = Visibility.Visible;
	}

	private void ResetHoverBorder()
	{
		_reopenHoverBorder = false;
		Task.Delay(150).ContinueWith(delegate
		{
			if (!_reopenHoverBorder)
			{
				base.Dispatcher.Invoke(delegate
				{
					base.Container?.RemoveHoverHeader();
				});
			}
		});
	}

	private void OnTextMouseEnter(object sender, MouseEventArgs e)
	{
		if (!_showTextTips)
		{
			return;
		}
		Task.Delay(200).ContinueWith(delegate
		{
			ThreadUtil.DetachedRunOnUiThread(delegate
			{
				if (InfoTile.IsMouseOver && !StartDateThumb.IsMouseOver && !EndDateThumb.IsMouseOver && e.LeftButton != MouseButtonState.Pressed)
				{
					base.Container?.OpenToolTips(Cell);
				}
			});
		});
	}

	private void OnTextMouseLeave(object sender, MouseEventArgs e)
	{
		base.Container?.TryCloseToolTips();
	}

	private void OnTextMouseMove(object sender, MouseEventArgs e)
	{
	}

	private void OnItemDragStartDateMouseEnter(object sender, MouseEventArgs e)
	{
		cellModel.Operation = cellModel.Operation.SetPos(TimelineCellOperation.Start);
		Task.Delay(20).ContinueWith(delegate
		{
			ThreadUtil.DetachedRunOnUiThread(delegate
			{
				if (StartDateThumb.IsMouseOver)
				{
					base.Container?.OpenToolTips(Cell, showDateTip: true);
				}
			});
		});
	}

	private void OnItemDragEndDateMouseEnter(object sender, MouseEventArgs e)
	{
		cellModel.Operation = cellModel.Operation.SetPos(TimelineCellOperation.End);
		Task.Delay(20).ContinueWith(delegate
		{
			ThreadUtil.DetachedRunOnUiThread(delegate
			{
				if (EndDateThumb.IsMouseOver)
				{
					base.Container?.OpenToolTips(Cell, showDateTip: true);
				}
			});
		});
	}

	private void OnItemDragStartDateEndDateMouseLeave(object sender, MouseEventArgs e)
	{
		base.Container?.TryCloseToolTips(force: true);
	}

	private void OnFloatingMouseWheel(object sender, MouseWheelEventArgs e)
	{
		DealWithMainScrollMove(Keyboard.IsKeyDown(Key.LeftShift), e.Delta);
		base.Container?.OnPreviewMouseWheel(sender, e);
	}

	public void DealWithMainScrollMove(bool hor, int offset)
	{
		if (hor)
		{
			Canvas.SetLeft(Cell, Canvas.GetLeft(Cell) + (double)offset);
		}
		else
		{
			Close();
		}
	}

	private void OnFullMouseDown(object sender, MouseButtonEventArgs e)
	{
		base.timelineModel.SetItemEditing(value: true);
		OnItemDragStarted(sender, null);
		_startPoint = e.GetPosition(this);
		_originPoint = _startPoint;
		_startDrag = true;
		if (cellModel != null)
		{
			cellModel.StartDragLine = (cellModel.BatchSelected ? new int?(cellModel.Line) : ((int?)null));
		}
		InfoTile.MouseMove -= OnStartDrag;
		InfoTile.MouseMove += OnStartDrag;
	}

	private async void OnStartDrag(object sender, MouseEventArgs e)
	{
		if (TickDragHelper.Dragging || !_startDrag)
		{
			return;
		}
		Point position = e.GetPosition(this);
		if (Math.Abs(position.X - _startPoint.X) < 3.0 && Math.Abs(position.Y - _startPoint.Y) < 3.0)
		{
			return;
		}
		TaskDetailPopup.TryCloseWindow(force: true);
		if (!CheckEditable())
		{
			base.Container?.StopMoveView();
			Mouse.Capture(null);
		}
		else
		{
			if (ModelInvalid() || !cellModel.Editable)
			{
				return;
			}
			InfoTile.MouseMove -= OnStartDrag;
			if (!cellModel.BatchSelected)
			{
				base.timelineModel.ClearBatchSelect();
				cellModel.DragStatus = 1;
			}
			else if (cellModel.DragStatus == 0)
			{
				base.timelineModel.SetBatchDragging(1);
			}
			Point point = e.GetPosition(Cell);
			double num = Cell.ActualWidth;
			if (num > 400.0)
			{
				num = 400.0;
				if (point.X > 400.0)
				{
					point = new Point(380.0, point.Y);
				}
			}
			TimelineCellViewModel model = cellModel;
			model.StartDragLine = model.Line;
			base.Visibility = Visibility.Collapsed;
			model.SetBatchText(model.BatchSelected ? base.timelineModel.GetSelectedTaskIds().Count : 0);
			await TickDragHelper.StartDragItem(base.Container, model, base.timelineModel.BatchSelect ? base.timelineModel.GetSelectedTaskIds() : new List<string> { model.Id }, num, point);
			model.SetBatchText(0);
			model.DragStatus = 0;
			model.StartDragLine = null;
			UserActCollectUtils.AddClickEvent("timeline", "task_action", Arranging ? "drag_arrangement" : "drag_task_position");
			TitleText.SetBinding(EmjTextBlock.TextProperty, "Title");
			Close();
			base.timelineModel.UpdateTimelineSortType();
		}
	}

	private bool CheckEditable()
	{
		TimelineCellViewModel timelineCellViewModel = cellModel;
		bool flag = true;
		if (ModelInvalid())
		{
			flag = false;
		}
		else
		{
			switch (timelineCellViewModel.DisplayModel.Type)
			{
			case DisplayType.CheckItem:
			case DisplayType.Event:
				base.Container?.TryToastString(Utils.GetString("OptionNotSupport"));
				flag = false;
				break;
			case DisplayType.Task:
			case DisplayType.Note:
				if (!timelineCellViewModel.Editable)
				{
					base.Container?.TryToastString(Utils.GetString("NoEditingPermission"));
					flag = false;
				}
				break;
			}
		}
		if (!flag)
		{
			Mouse.Capture(null);
			_startDrag = false;
			base.timelineModel.Hovering = false;
		}
		return flag;
	}

	private async void OnFullMouseUp(object sender, MouseButtonEventArgs e)
	{
		Mouse.Capture(null);
		if (cellModel != null)
		{
			_startDrag = false;
			await OnItemMouseUp(_startPoint == _originPoint || (base.timelineModel != null && !cellModel.Editable));
		}
	}

	public async Task OnDragMoveOutSide()
	{
		if (ModelInvalid())
		{
			return;
		}
		TimelineCellViewModel model = cellModel;
		if (model == null || !model.Editable)
		{
			return;
		}
		switch (model.Operation.GetPos())
		{
		case TimelineCellOperation.Start:
			await UpdateModel();
			Canvas.SetLeft(Cell, (model.Left - base.Container?.MainScroll.HorizontalOffset).GetValueOrDefault());
			model.SetWidth();
			InfoTile.Width = model.Width;
			break;
		case TimelineCellOperation.Full:
			await UpdateModel();
			break;
		case TimelineCellOperation.End:
		{
			Point? point = _currentUi?.TranslatePoint(new Point(0.0, 0.0), base.Container?.MainScroll);
			if (point.HasValue)
			{
				Canvas.SetLeft(Cell, point.Value.X);
				await UpdateModel();
				model.SetWidth();
				InfoTile.Width = model.Width;
			}
			break;
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
			Uri resourceLocator = new Uri("/TickTick;component/views/timeline/timelinecellfloating.xaml", UriKind.Relative);
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
			Root = (TimelineCellFloating)target;
			break;
		case 2:
			Cell = (Grid)target;
			Cell.MouseRightButtonUp += OnMouseRightUp;
			Cell.MouseLeave += OnMouseLeave;
			break;
		case 3:
			BackgroundBorder = (Rectangle)target;
			break;
		case 4:
			InfoTile = (Grid)target;
			InfoTile.MouseLeftButtonDown += OnFullMouseDown;
			InfoTile.MouseLeftButtonUp += OnFullMouseUp;
			InfoTile.MouseEnter += OnTextMouseEnter;
			InfoTile.MouseLeave += OnTextMouseLeave;
			break;
		case 5:
			((Grid)target).MouseMove += OnTextMouseMove;
			((Grid)target).MouseEnter += OnTextMouseEnter;
			((Grid)target).MouseLeave += OnTextMouseLeave;
			break;
		case 6:
			TitleText = (EmjTextBlock)target;
			break;
		case 7:
			Progress = (TaskCircleProgress)target;
			break;
		case 8:
			AvatarBorder = (Border)target;
			break;
		case 9:
			StartDateThumb = (Thumb)target;
			StartDateThumb.DragDelta += OnStartDateThumbDragDelta;
			StartDateThumb.DragCompleted += OnItemDragCompleted;
			StartDateThumb.DragStarted += OnItemDragStarted;
			StartDateThumb.MouseEnter += OnItemDragStartDateMouseEnter;
			StartDateThumb.MouseLeave += OnItemDragStartDateEndDateMouseLeave;
			break;
		case 10:
			EndDateThumb = (Thumb)target;
			EndDateThumb.DragDelta += OnEndDateThumbDragDelta;
			EndDateThumb.DragCompleted += OnItemDragCompleted;
			EndDateThumb.DragStarted += OnItemDragStarted;
			EndDateThumb.MouseEnter += OnItemDragEndDateMouseEnter;
			EndDateThumb.MouseLeave += OnItemDragStartDateEndDateMouseLeave;
			break;
		case 11:
			TipsPopup = (Popup)target;
			break;
		case 12:
			TipsPanel = (Border)target;
			TipsPanel.MouseLeave += OnMouseLeave;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
