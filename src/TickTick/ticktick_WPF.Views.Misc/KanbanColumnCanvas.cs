using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Kanban;

namespace ticktick_WPF.Views.Misc;

public class KanbanColumnCanvas : Canvas
{
	private readonly SemaphoreSlim _locker = new SemaphoreSlim(1, 1);

	private Point _uiStartPoint;

	private bool _isDragging;

	public static readonly DependencyProperty ColumnWidthProperty = DependencyProperty.Register("ColumnWidth", typeof(double), typeof(KanbanColumnCanvas), new PropertyMetadata(282.0, OnColumnWidthChanged));

	private readonly Storyboard _board = new Storyboard();

	private BlockingList<ColumnViewModel> _columnViewModels = new BlockingList<ColumnViewModel>();

	public readonly BlockingList<KanbanColumnView> ChildrenList = new BlockingList<KanbanColumnView>();

	private double _horizonOffset;

	private double _scrollWidth;

	private List<ColumnViewModel> _displayModels;

	private bool _moveChanged;

	public bool IsDragging => _isDragging;

	public double ColumnWidth
	{
		get
		{
			return (double)GetValue(ColumnWidthProperty);
		}
		set
		{
			SetValue(ColumnWidthProperty, value);
		}
	}

	private double ColumnSpacing => LocalSettings.Settings.ExtraSettings.KbShowColumnColor ? 12 : 0;

	private double ColumnPitch => ColumnWidth + ColumnSpacing;

	public event EventHandler<Tuple<ColumnViewModel, ColumnViewModel>> Switched;

	public KanbanColumnCanvas()
	{
		InitStoryboard();
		SetResourceReference(ColumnWidthProperty, "KanbanColumnWidth");
	}

	public double GetColumnLeft(int index)
	{
		return (double)index * ColumnPitch;
	}

	public double GetColumnRight(int index)
	{
		return GetColumnLeft(index) + ColumnWidth;
	}

	private double GetCanvasWidth()
	{
		return (double)_columnViewModels.Count * ColumnPitch;
	}

	public void SetChild(int index, object dataContext)
	{
		if (ChildrenList.Count > index)
		{
			ChildrenList[index].DataContext = dataContext;
		}
	}

	public void RemoveItem(ColumnViewModel model)
	{
		_columnViewModels.Remove(model);
		base.Width = GetCanvasWidth();
		SetColumns();
	}

	public void Insert(int index, ColumnViewModel model)
	{
		_columnViewModels.Insert(index, model);
		base.Width = GetCanvasWidth();
		SetColumns();
	}

	private void OnChildMouseMove(object sender, MouseEventArgs e)
	{
		if (!(sender is KanbanColumnView kanbanColumnView) || e.LeftButton != MouseButtonState.Pressed || !(kanbanColumnView.DataContext is ColumnViewModel columnViewModel) || _uiStartPoint == default(Point))
		{
			return;
		}
		Point position = e.GetPosition(this);
		double length = position.X - _uiStartPoint.X;
		if (!_isDragging)
		{
			Panel.SetZIndex(kanbanColumnView, base.Children.Count);
			RotateTransform renderTransform = new RotateTransform(-3.0);
			kanbanColumnView.RenderTransform = renderTransform;
			kanbanColumnView.CaptureMouse();
			kanbanColumnView.SetColumnDragging(dragging: true);
		}
		base.Cursor = Cursors.Hand;
		_isDragging = true;
		kanbanColumnView.DragMouseDown = false;
		double x = position.X;
		Canvas.SetLeft(kanbanColumnView, length);
		int num = _columnViewModels.IndexOf(columnViewModel);
		int num2 = LeftToIndex(x, num);
		if (num2 < num)
		{
			if (num >= 1)
			{
				ColumnViewModel preVm = _columnViewModels[num - 1];
				KanbanColumnView kanbanColumnView2 = ChildrenList.FirstOrDefault((KanbanColumnView m) => m.DataContext == preVm);
				if (kanbanColumnView2 != null)
				{
					double columnLeft = GetColumnLeft(num);
					MoveChild(Canvas.GetLeft(kanbanColumnView2), columnLeft, kanbanColumnView2);
					_columnViewModels[num - 1] = columnViewModel;
					_columnViewModels[num] = preVm;
				}
			}
		}
		else if (num2 > num && num <= _columnViewModels.Count - 2)
		{
			ColumnViewModel nextVm = _columnViewModels[num + 1];
			KanbanColumnView kanbanColumnView3 = ChildrenList.FirstOrDefault((KanbanColumnView m) => m.DataContext == nextVm);
			if (kanbanColumnView3 != null)
			{
				double columnLeft2 = GetColumnLeft(num);
				MoveChild(Canvas.GetLeft(kanbanColumnView3), columnLeft2, kanbanColumnView3);
				_columnViewModels[num + 1] = columnViewModel;
				_columnViewModels[num] = nextVm;
			}
		}
	}

	private void InitStoryboard()
	{
		DoubleAnimation doubleAnimation = new DoubleAnimation
		{
			Duration = new Duration(TimeSpan.FromMilliseconds(200.0))
		};
		Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath("(Canvas.Left)"));
		_board.FillBehavior = FillBehavior.Stop;
		_board.Children.Add(doubleAnimation);
	}

	private void MoveChild(double from, double to, KanbanColumnView child)
	{
		_moveChanged = true;
		_board.SeekAlignedToLastTick(TimeSpan.FromMilliseconds(200.0));
		_board.Stop();
		if (_board.Children.Any() && _board.Children.First() is DoubleAnimation doubleAnimation)
		{
			doubleAnimation.To = to;
			doubleAnimation.From = from;
			Storyboard.SetTarget(doubleAnimation, child);
			_board.Completed += OnCompleted;
			_board.Begin();
		}
		void OnCompleted(object sender, EventArgs e)
		{
			_board.Completed -= OnCompleted;
			Canvas.SetLeft(child, to);
		}
	}

	private int LeftToIndex(double left, int currentIndex)
	{
		return (int)(left / ColumnPitch);
	}

	private async void OnChildLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (sender is KanbanColumnView { DataContext: var dataContext } ui && dataContext is ColumnViewModel model && _isDragging)
		{
			_isDragging = false;
			base.Cursor = Cursors.Arrow;
			_uiStartPoint = default(Point);
			int num = _columnViewModels.IndexOf(model);
			if (num >= 0)
			{
				Canvas.SetLeft(ui, GetColumnLeft(num));
				if (_moveChanged)
				{
					ColumnViewModel columnViewModel = ((num > 0) ? _columnViewModels[num - 1] : null);
					ColumnViewModel columnViewModel2 = ((num < _columnViewModels.Count - 1) ? _columnViewModels[num + 1] : null);
					string text = columnViewModel?.ColumnId ?? columnViewModel2?.ColumnId;
					if (!string.IsNullOrEmpty(text))
					{
						long? num2 = await ColumnDao.SaveSortOrder(model.ColumnId, text, columnViewModel == null);
						SyncManager.TryDelaySync();
						DataChangedNotifier.NotifyColumnChanged(model.Identity?.GetProjectId());
						model.SortOrder = num2 ?? model.SortOrder;
					}
				}
			}
			_moveChanged = false;
			Panel.SetZIndex(ui, 0);
			ui.ReleaseMouseCapture();
			ui.MouseMove -= OnChildMouseMove;
			ui.RenderTransform = null;
			ui.SetColumnDragging(dragging: false);
		}
		_isDragging = false;
		base.Cursor = Cursors.Arrow;
		_uiStartPoint = default(Point);
	}

	private async void OnChildLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (sender is KanbanColumnView { IsMouseDragOver: not false, DataContext: ColumnViewModel { Enable: not false } } kanbanColumnView)
		{
			_uiStartPoint = e.GetPosition(kanbanColumnView);
			kanbanColumnView.MouseMove -= OnChildMouseMove;
			kanbanColumnView.MouseMove += OnChildMouseMove;
			kanbanColumnView.DragMouseDown = true;
		}
	}

	public void Clear()
	{
		ChildrenList.Clear();
		base.Children.Clear();
	}

	public void ReloadQuickAddView()
	{
		foreach (object child in base.Children)
		{
			if (child is KanbanColumnView kanbanColumnView)
			{
				kanbanColumnView.ResetQuickAddView();
			}
		}
	}

	public void SetItemModels(BlockingList<ColumnViewModel> columnViewModels)
	{
		_columnViewModels = columnViewModels;
		base.Width = GetCanvasWidth();
		SetColumns();
	}

	private async Task SetColumns()
	{
		await _locker.WaitAsync();
		try
		{
			if (!(_scrollWidth > 0.0))
			{
				return;
			}
			int num = (int)(_horizonOffset / ColumnPitch) - 1;
			if (num >= _columnViewModels.Count)
			{
				return;
			}
			if (num < 0)
			{
				num = 0;
			}
			int num2 = num + (int)(_scrollWidth / ColumnPitch) + 2;
			if (num2 >= _columnViewModels.Count)
			{
				num2 = _columnViewModels.Count - 1;
			}
			List<KanbanColumnView> list = ChildrenList.ToList();
			List<ColumnViewModel> models = new List<ColumnViewModel>();
			for (int i = num; i <= num2; i++)
			{
				models.Add(_columnViewModels[i]);
			}
			if (_displayModels != null && _displayModels.Count == models.Count)
			{
				bool flag = true;
				for (int j = 0; j < models.Count; j++)
				{
					flag = _displayModels[j] == models[j];
					if (!flag)
					{
						break;
					}
				}
				if (flag)
				{
					return;
				}
			}
			_displayModels = models;
			for (int k = 0; k < models.Count; k++)
			{
				ColumnViewModel model = models[k];
				KanbanColumnView kanbanColumnView = list.FirstOrDefault((KanbanColumnView c) => (c.ColumnId == model.ColumnId) ? true : false) ?? list.FirstOrDefault((KanbanColumnView c) => models.All((ColumnViewModel m) => m.ColumnId != c.ColumnId));
				if (kanbanColumnView != null)
				{
					list.Remove(kanbanColumnView);
					if (kanbanColumnView.DataContext != model)
					{
						kanbanColumnView.DataContext = model;
					}
				}
				else
				{
					kanbanColumnView = new KanbanColumnView(model);
					if (base.ActualHeight > 18.0)
					{
						kanbanColumnView.SetMaxHeight(base.ActualHeight - 18.0);
					}
					base.Children.Add(kanbanColumnView);
					ChildrenList.Add(kanbanColumnView);
					kanbanColumnView.MouseLeftButtonDown += OnChildLeftButtonDown;
					kanbanColumnView.MouseLeftButtonUp += OnChildLeftButtonUp;
				}
				Canvas.SetLeft(kanbanColumnView, GetColumnLeft(num + k));
				Canvas.SetTop(kanbanColumnView, 16.0);
			}
			foreach (KanbanColumnView item in list)
			{
				base.Children.Remove(item);
				ChildrenList.Remove(item);
				item.MouseLeftButtonDown -= OnChildLeftButtonDown;
				item.MouseLeftButtonUp -= OnChildLeftButtonUp;
				item.UnbindEvents();
			}
		}
		finally
		{
			_locker.Release();
		}
	}

	public void SetScrollOffset(double horizonOffset, double scrollWidth)
	{
		_horizonOffset = horizonOffset;
		_scrollWidth = scrollWidth;
		SetColumns();
	}

	public KanbanColumnView GetColumnById(string columnId)
	{
		return ChildrenList.FirstOrDefault((KanbanColumnView c) => c.GetColumnId() == columnId);
	}

	public void SetItemMaxHeight()
	{
		foreach (KanbanColumnView item in ChildrenList.Value)
		{
			if (base.ActualHeight > 18.0)
			{
				item.SetMaxHeight(base.ActualHeight - 18.0);
			}
		}
	}

	public void Dispose()
	{
		base.Children.Clear();
		ChildrenList.Clear();
	}

	public void SetShowAddInColumn()
	{
		foreach (KanbanColumnView item in ChildrenList.Value)
		{
			item.SetShowAdd();
		}
	}

	private static void OnColumnWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		((KanbanColumnCanvas)d).ColumnWidthChanged();
	}

	public void ColumnWidthChanged()
	{
		_displayModels = null;
		base.Width = GetCanvasWidth();
		SetColumns();
	}
}
