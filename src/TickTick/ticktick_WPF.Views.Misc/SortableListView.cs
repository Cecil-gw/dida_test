using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using TickTickUtils.Lang;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc;

public class SortableListView<T> : Grid where T : SortableListItemViewModel
{
	public bool DragEnable = true;

	public int StartDragDiff = 8;

	private double _startY = -1.0;

	private T _dragItem;

	private int _currentDragIndex;

	public readonly ListView ItemList = new ListView();

	private readonly Popup _dragPopup = new Popup
	{
		AllowsTransparency = true,
		Placement = PlacementMode.Relative,
		StaysOpen = true
	};

	public double PopupContentHeight = 40.0;

	public FrameworkElement PopupContent;

	private bool _currentDragIsOpen;

	public ObservableCollection<T> Items => (ItemList.ItemsSource as ObservableCollection<T>) ?? new ObservableCollection<T>();

	public SortableListView()
	{
		base.MouseMove += OnDragMove;
		ItemList.SetResourceReference(FrameworkElement.StyleProperty, "ListViewStyle");
		ItemList.VerticalAlignment = VerticalAlignment.Top;
		base.Children.Add(ItemList);
		_dragPopup.PlacementTarget = this;
		_dragPopup.PreviewMouseLeftButtonUp += OnDrop;
	}

	public void TryDragItem(T item, MouseEventArgs e)
	{
		if (DragEnable)
		{
			_startY = e.GetPosition(this).Y;
			_dragItem = item;
			_currentDragIndex = Items.IndexOf(item);
		}
	}

	private void OnDragMove(object sender, MouseEventArgs e)
	{
		if (_dragItem == null)
		{
			return;
		}
		Point position = e.GetPosition(this);
		if (_dragPopup.IsOpen)
		{
			if (_dragItem.IsSection)
			{
				_dragPopup.HorizontalOffset = position.X - PopupContentHeight / 2.0;
				_dragPopup.VerticalOffset = position.Y - PopupContentHeight / 2.0;
			}
			else
			{
				_dragPopup.HorizontalOffset = 10.0;
				_dragPopup.VerticalOffset = position.Y - PopupContentHeight / 2.0;
			}
			Point pos = new Point(10.0, e.GetPosition(this).Y);
			ListViewItem mousePositionItem = GetMousePositionItem(pos);
			if (!(mousePositionItem?.DataContext is T val))
			{
				return;
			}
			SortableListItemViewModel sortableListItemViewModel = val;
			if (sortableListItemViewModel != null && !sortableListItemViewModel.CanSort)
			{
				return;
			}
			int num = Items.IndexOf(val);
			if (_currentDragIndex < 0)
			{
				return;
			}
			Point position2 = e.GetPosition(mousePositionItem);
			if (num == _currentDragIndex)
			{
				return;
			}
			int num2 = -1;
			if (_currentDragIndex < num && position2.Y > mousePositionItem.ActualHeight / 2.0)
			{
				num2 = num;
				T val2 = ((num2 + 1 < Items.Count) ? Items[num2 + 1] : null);
				if (_dragItem.IsSection && val2 != null && !val2.IsSection)
				{
					return;
				}
			}
			else if (_currentDragIndex > num && position2.Y < mousePositionItem.ActualHeight / 2.0)
			{
				num2 = num;
				if ((_dragItem.IsSection && !val.IsSection) || (!_dragItem.IsSection && num2 == 0 && val.IsSection))
				{
					return;
				}
			}
			if (num2 != _currentDragIndex && num2 >= 0 && num2 < Items.Count)
			{
				List<T> list = Items.ToList();
				list.Remove(_dragItem);
				list.Insert(Math.Min(list.Count, num2), _dragItem);
				try
				{
					ItemsSourceHelper.SetItemsSource(ItemList, list);
				}
				catch (Exception)
				{
				}
				_currentDragIndex = num2;
			}
		}
		else if (_startY > 0.0 && _dragItem != null && Math.Abs(position.Y - _startY) > (double)StartDragDiff)
		{
			_dragItem.Dragging = true;
			_dragItem.VisualState = DragVisualState.Placeholder;
			base.PreviewMouseLeftButtonUp -= OnDrop;
			base.PreviewMouseLeftButtonUp += OnDrop;
			Mouse.Capture(ItemList);
			if (_dragItem.IsSection)
			{
				StartDragSection(_dragItem);
			}
			ShowDragPop(_dragItem, position);
		}
	}

	private void ShowDragPop(T model, Point position)
	{
		if (PopupContent != null)
		{
			if (_dragPopup.Child == null)
			{
				_dragPopup.Child = PopupContent;
			}
			PopupContent.DataContext = model;
		}
		_dragPopup.HorizontalOffset = position.X - 1.0 * ((base.ActualWidth > 400.0) ? 200.0 : (base.ActualWidth / 2.0));
		_dragPopup.VerticalOffset = position.Y - PopupContentHeight / 2.0;
		_dragPopup.Width = base.ActualWidth - 20.0;
		try
		{
			_dragPopup.IsOpen = true;
			foreach (T item in Items)
			{
				item.ParentDragging = true;
				item.OnParentDraggingChanged();
			}
		}
		catch (Exception)
		{
			UtilLog.Info("SortableListView.ShowDragPop error");
		}
	}

	private void StartDragSection(T model)
	{
		if (model.IsOpen)
		{
			List<T> items = Items.Where((T h) => !h.IsSection || !(h.SectionId == model.Id)).ToList();
			ItemsSourceHelper.SetItemsSource(ItemList, items);
			_currentDragIsOpen = model.IsOpen;
			model.IsOpen = false;
		}
	}

	private ListViewItem GetMousePositionItem(Point pos)
	{
		HitTestResult hitTestResult = VisualTreeHelper.HitTest(this, pos);
		if (hitTestResult != null)
		{
			return Utils.FindParent<ListViewItem>(hitTestResult.VisualHit);
		}
		return null;
	}

	private void OnDrop(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		T dragItem = _dragItem;
		if (!_dragPopup.IsOpen && _dragItem != null)
		{
			_dragItem.Dragging = false;
			_dragItem.VisualState = DragVisualState.Normal;
			_dragItem = null;
		}
		ReleaseMouseCapture();
		base.PreviewMouseLeftButtonUp -= OnDrop;
		if (dragItem != null && !dragItem.IsSection)
		{
			long num = dragItem.SortOrder;
			T val = ((_currentDragIndex > 0) ? Items[_currentDragIndex - 1] : null);
			T val2 = ((_currentDragIndex < Items.Count - 1) ? Items[_currentDragIndex + 1] : null);
			long? num2 = ((val != null && !val.IsSection) ? new long?(val.SortOrder) : ((long?)null));
			long? num3 = ((val2 != null && !val2.IsSection) ? new long?(val2.SortOrder) : ((long?)null));
			if (num2.HasValue || num3.HasValue)
			{
				num = ((!num2.HasValue) ? (num3.Value - 268435456) : (num3.HasValue ? (num2.Value + (num3.Value - num2.Value) / 2) : (num2.Value + 268435456)));
			}
			string text = dragItem.SectionId;
			if (val != null && !val.IsSection)
			{
				text = val.SectionId;
			}
			else if (val != null && val.IsSection)
			{
				text = val.Id;
			}
			if (num != dragItem.SortOrder || text != dragItem.SectionId)
			{
				dragItem.SectionId = text;
				dragItem.SortOrder = num;
				dragItem.SaveSortOrder();
			}
		}
		else if (dragItem != null && dragItem.IsSection)
		{
			List<T> list = Items.Where((T v) => v.IsSection).ToList();
			int num4 = list.IndexOf(dragItem);
			long? front = ((num4 > 0) ? new long?(list[num4 - 1].SortOrder) : ((long?)null));
			long? next = ((num4 < list.Count - 1) ? new long?(list[num4 + 1].SortOrder) : ((long?)null));
			long num5 = GetSortOrderBetween(front, next) ?? dragItem.SortOrder;
			dragItem.IsOpen = _currentDragIsOpen;
			if (num5 != dragItem.SortOrder)
			{
				dragItem.SortOrder = num5;
				dragItem.SortOrder = num5;
				dragItem.SaveSortOrder();
			}
		}
		if (_dragItem != null)
		{
			_dragItem.Dragging = false;
			_dragItem.VisualState = DragVisualState.Normal;
			_dragItem = null;
		}
		_startY = -1.0;
		_currentDragIndex = -1;
		_dragPopup.IsOpen = false;
		foreach (T item in Items)
		{
			item.ParentDragging = false;
			item.OnParentDraggingChanged();
		}
	}

	private static long? GetSortOrderBetween(long? front, long? next)
	{
		if (front.HasValue || next.HasValue)
		{
			if (!front.HasValue)
			{
				return next.Value - 268435456;
			}
			if (!next.HasValue)
			{
				return front.Value + 268435456;
			}
			return front.Value + (next.Value - front.Value) / 2;
		}
		return null;
	}
}
