using System;
using System.Collections;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ticktick_WPF.Views;

public class DragSortBehavior
{
	private class DragState : IDisposable
	{
		private Popup _popup;

		private ContentControl _previewContent;

		private DispatcherTimer _scrollTimer;

		private double _scrollSpeed;

		private UIElement _captureTarget;

		private Popup _capturePopup;

		private MouseEventHandler _captureMove;

		private MouseButtonEventHandler _captureUp;

		private MouseEventHandler _captureLost;

		private KeyEventHandler _captureKeyDown;

		public bool IsAcquiringCapture { get; private set; }

		public bool HasMouseCapture => Mouse.Captured == _captureTarget;

		public object DragItem { get; set; }

		public int DragIndex { get; set; }

		public int OriginalIndex { get; set; }

		public DependencyObject DragSource { get; set; }

		public Point StartPoint { get; set; }

		public Point DragOffset { get; set; }

		public double DragWidth { get; set; }

		public bool IsDragging { get; set; }

		public ScrollViewer ParentScrollViewer { get; set; }

		public bool CaptureMouse(ItemsControl control)
		{
			_captureTarget = ((GetHandler(control) == null) ? control : ((PresentationSource.FromVisual(control)?.RootVisual as UIElement) ?? control));
			_capturePopup = (_captureTarget as FrameworkElement)?.Parent as Popup;
			if (_captureTarget != control)
			{
				_captureMove = delegate(object sender, MouseEventArgs e)
				{
					OnPreviewMouseMove(control, e);
				};
				_captureUp = delegate(object sender, MouseButtonEventArgs e)
				{
					OnPreviewMouseLeftButtonUp(control, e);
				};
				_captureLost = delegate(object sender, MouseEventArgs e)
				{
					OnLostMouseCapture(control, e);
				};
				_captureKeyDown = delegate(object sender, KeyEventArgs e)
				{
					OnPreviewKeyDown(control, e);
				};
				_captureTarget.PreviewMouseMove += _captureMove;
				_captureTarget.PreviewMouseLeftButtonUp += _captureUp;
				_captureTarget.LostMouseCapture += _captureLost;
				_captureTarget.PreviewKeyDown += _captureKeyDown;
			}
			IsAcquiringCapture = true;
			try
			{
				Popup capturePopup = _capturePopup;
				if (capturePopup != null && capturePopup.IsOpen && !_capturePopup.StaysOpen)
				{
					if (!HasMouseCapture && Mouse.Captured is DependencyObject descendant && _captureTarget.IsAncestorOf(descendant))
					{
						Mouse.Capture(null);
					}
					return HasMouseCapture;
				}
				return Mouse.Capture(_captureTarget, (_capturePopup == null) ? CaptureMode.Element : CaptureMode.SubTree);
			}
			finally
			{
				IsAcquiringCapture = false;
			}
		}

		public void ReleaseMouseCapture()
		{
			UIElement captureTarget = _captureTarget;
			if (captureTarget == null)
			{
				return;
			}
			if (_captureMove != null)
			{
				captureTarget.PreviewMouseMove -= _captureMove;
			}
			if (_captureUp != null)
			{
				captureTarget.PreviewMouseLeftButtonUp -= _captureUp;
			}
			if (_captureLost != null)
			{
				captureTarget.LostMouseCapture -= _captureLost;
			}
			if (_captureKeyDown != null)
			{
				captureTarget.PreviewKeyDown -= _captureKeyDown;
			}
			if (HasMouseCapture)
			{
				Popup capturePopup = _capturePopup;
				if (capturePopup == null || !capturePopup.IsOpen || _capturePopup.StaysOpen)
				{
					Mouse.Capture(null);
				}
			}
			_captureTarget = null;
			_capturePopup = null;
			_captureMove = null;
			_captureUp = null;
			_captureLost = null;
			_captureKeyDown = null;
		}

		public DragState()
		{
			_scrollTimer = new DispatcherTimer
			{
				Interval = TimeSpan.FromMilliseconds(16.0)
			};
			_scrollTimer.Tick += OnScrollTick;
		}

		private void OnScrollTick(object sender, EventArgs e)
		{
			if (ParentScrollViewer == null || _scrollSpeed == 0.0)
			{
				_scrollTimer.Stop();
				return;
			}
			double offset = ParentScrollViewer.VerticalOffset + _scrollSpeed;
			ParentScrollViewer.ScrollToVerticalOffset(offset);
		}

		public void UpdateScrollSpeed(double speed)
		{
			_scrollSpeed = speed;
			if (speed != 0.0 && !_scrollTimer.IsEnabled)
			{
				_scrollTimer.Start();
			}
			else if (speed == 0.0)
			{
				_scrollTimer.Stop();
			}
		}

		public void ShowPreview(ItemsControl itemsControl, Point position)
		{
			if (_popup == null)
			{
				_popup = new Popup
				{
					AllowsTransparency = true,
					Placement = PlacementMode.Relative,
					PlacementTarget = itemsControl,
					StaysOpen = true
				};
				_previewContent = new ContentControl();
				_popup.Child = _previewContent;
			}
			object obj = ((DragItem is ICloneable cloneable) ? cloneable.Clone() : DragItem);
			if (obj != DragItem && obj is IDraggable draggable)
			{
				draggable.VisualState = DragVisualState.Preview;
			}
			_previewContent.Content = obj;
			_previewContent.ContentTemplate = itemsControl.ItemTemplate;
			if (GetHandler(itemsControl) != null)
			{
				_previewContent.Width = ((DragWidth > 0.0) ? DragWidth : itemsControl.ActualWidth);
				_previewContent.Margin = new Thickness(6.0);
				_previewContent.HorizontalContentAlignment = HorizontalAlignment.Stretch;
				_previewContent.CacheMode = new BitmapCache();
				_previewContent.IsHitTestVisible = false;
			}
			_popup.HorizontalOffset = position.X - DragOffset.X - _previewContent.Margin.Left;
			_popup.VerticalOffset = position.Y - DragOffset.Y - _previewContent.Margin.Top;
			_popup.IsOpen = true;
		}

		public void UpdatePreviewPosition(Point position)
		{
			if (_popup != null && _popup.IsOpen)
			{
				_popup.HorizontalOffset = position.X - DragOffset.X - _previewContent.Margin.Left;
				_popup.VerticalOffset = position.Y - DragOffset.Y - _previewContent.Margin.Top;
			}
		}

		public void HidePreview()
		{
			if (_popup != null)
			{
				_popup.IsOpen = false;
				_previewContent.Content = null;
			}
		}

		public void Clear()
		{
			_scrollTimer?.Stop();
			_scrollSpeed = 0.0;
			DragItem = null;
			DragSource = null;
			DragIndex = -1;
			OriginalIndex = -1;
			IsDragging = false;
		}

		public void Dispose()
		{
			_scrollTimer?.Stop();
			ReleaseMouseCapture();
			HidePreview();
			_popup = null;
			_previewContent = null;
		}
	}

	public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(DragSortBehavior), new PropertyMetadata(false, OnIsEnabledChanged));

	public static readonly DependencyProperty DragThresholdProperty = DependencyProperty.RegisterAttached("DragThreshold", typeof(int), typeof(DragSortBehavior), new PropertyMetadata(8));

	public static readonly DependencyProperty DragCursorProperty = DependencyProperty.RegisterAttached("DragCursor", typeof(Cursor), typeof(DragSortBehavior), new PropertyMetadata(Cursors.SizeAll));

	public static readonly DependencyProperty DragHandleTagProperty = DependencyProperty.RegisterAttached("DragHandleTag", typeof(object), typeof(DragSortBehavior), new PropertyMetadata(null));

	public static readonly DependencyProperty SortCompletedProperty = DependencyProperty.RegisterAttached("SortCompleted", typeof(Action<object, int, int>), typeof(DragSortBehavior), new PropertyMetadata(null));

	public static readonly DependencyProperty HandlerProperty = DependencyProperty.RegisterAttached("Handler", typeof(IDragSortHandler), typeof(DragSortBehavior));

	private static readonly DependencyPropertyKey IsDraggingPropertyKey = DependencyProperty.RegisterAttachedReadOnly("IsDragging", typeof(bool), typeof(DragSortBehavior), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

	public static readonly DependencyProperty IsDraggingProperty = IsDraggingPropertyKey.DependencyProperty;

	private static readonly DependencyProperty DragStateProperty = DependencyProperty.RegisterAttached("DragState", typeof(DragState), typeof(DragSortBehavior), new PropertyMetadata(null));

	public static bool GetIsEnabled(DependencyObject obj)
	{
		return (bool)obj.GetValue(IsEnabledProperty);
	}

	public static void SetIsEnabled(DependencyObject obj, bool value)
	{
		obj.SetValue(IsEnabledProperty, value);
	}

	public static int GetDragThreshold(DependencyObject obj)
	{
		return (int)obj.GetValue(DragThresholdProperty);
	}

	public static void SetDragThreshold(DependencyObject obj, int value)
	{
		obj.SetValue(DragThresholdProperty, value);
	}

	public static Cursor GetDragCursor(DependencyObject obj)
	{
		return (Cursor)obj.GetValue(DragCursorProperty);
	}

	public static void SetDragCursor(DependencyObject obj, Cursor value)
	{
		obj.SetValue(DragCursorProperty, value);
	}

	public static object GetDragHandleTag(DependencyObject obj)
	{
		return obj.GetValue(DragHandleTagProperty);
	}

	public static void SetDragHandleTag(DependencyObject obj, object value)
	{
		obj.SetValue(DragHandleTagProperty, value);
	}

	public static Action<object, int, int> GetSortCompleted(DependencyObject obj)
	{
		return (Action<object, int, int>)obj.GetValue(SortCompletedProperty);
	}

	public static void SetSortCompleted(DependencyObject obj, Action<object, int, int> value)
	{
		obj.SetValue(SortCompletedProperty, value);
	}

	public static IDragSortHandler GetHandler(DependencyObject obj)
	{
		return (IDragSortHandler)obj.GetValue(HandlerProperty);
	}

	public static void SetHandler(DependencyObject obj, IDragSortHandler value)
	{
		obj.SetValue(HandlerProperty, value);
	}

	public static bool GetIsDragging(DependencyObject obj)
	{
		return (bool)obj.GetValue(IsDraggingProperty);
	}

	public static bool IsDragging(DependencyObject obj)
	{
		return GetIsDragging(obj);
	}

	internal static bool IsDragPending(DependencyObject obj)
	{
		DragState dragState = GetDragState(obj);
		if (dragState != null && dragState.DragItem != null)
		{
			return !dragState.IsDragging;
		}
		return false;
	}

	private static DragState GetDragState(DependencyObject obj)
	{
		return (DragState)obj.GetValue(DragStateProperty);
	}

	private static void SetDragState(DependencyObject obj, DragState value)
	{
		obj.SetValue(DragStateProperty, value);
	}

	private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (!(d is ItemsControl itemsControl))
		{
			return;
		}
		if ((bool)e.NewValue)
		{
			DragState dragState = new DragState();
			SetDragState(itemsControl, dragState);
			dragState.ParentScrollViewer = FindParentScrollViewer(itemsControl);
			itemsControl.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
			itemsControl.PreviewMouseMove += OnPreviewMouseMove;
			itemsControl.PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
			itemsControl.PreviewKeyDown += OnPreviewKeyDown;
			itemsControl.LostMouseCapture += OnLostMouseCapture;
			itemsControl.Unloaded += OnItemsControlUnloaded;
			if (dragState.ParentScrollViewer == null)
			{
				itemsControl.Loaded += OnItemsControlLoaded;
			}
			return;
		}
		itemsControl.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
		itemsControl.PreviewMouseMove -= OnPreviewMouseMove;
		itemsControl.PreviewMouseLeftButtonUp -= OnPreviewMouseLeftButtonUp;
		itemsControl.PreviewKeyDown -= OnPreviewKeyDown;
		itemsControl.LostMouseCapture -= OnLostMouseCapture;
		itemsControl.Unloaded -= OnItemsControlUnloaded;
		itemsControl.Loaded -= OnItemsControlLoaded;
		DragState dragState2 = GetDragState(itemsControl);
		if (dragState2 != null && dragState2.IsDragging)
		{
			FinishDrag(itemsControl, dragState2, cancelled: true);
		}
		dragState2?.Dispose();
		SetDragState(itemsControl, null);
	}

	private static void OnItemsControlLoaded(object sender, RoutedEventArgs e)
	{
		if (sender is ItemsControl itemsControl)
		{
			DragState dragState = GetDragState(itemsControl);
			if (dragState != null && dragState.ParentScrollViewer == null)
			{
				dragState.ParentScrollViewer = FindParentScrollViewer(itemsControl);
			}
			itemsControl.Loaded -= OnItemsControlLoaded;
		}
	}

	private static void OnItemsControlUnloaded(object sender, RoutedEventArgs e)
	{
		if (sender is ItemsControl itemsControl)
		{
			DragState dragState = GetDragState(itemsControl);
			if (dragState != null && dragState.IsDragging)
			{
				FinishDrag(itemsControl, dragState, cancelled: true);
			}
			dragState?.Clear();
		}
	}

	private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (!(sender is ItemsControl itemsControl))
		{
			return;
		}
		DragState dragState = GetDragState(itemsControl);
		if (dragState == null || dragState.IsDragging)
		{
			return;
		}
		dragState.Clear();
		object dragHandleTag = GetDragHandleTag(itemsControl);
		if (dragHandleTag != null && !IsFromDragHandle(e.OriginalSource as DependencyObject, itemsControl, dragHandleTag))
		{
			dragState.Clear();
			return;
		}
		FrameworkElement frameworkElement = FindContainerFromPoint(itemsControl, e.GetPosition(itemsControl));
		if (frameworkElement == null)
		{
			return;
		}
		object obj = itemsControl.ItemContainerGenerator.ItemFromContainer(frameworkElement);
		if (obj != null)
		{
			IDragSortHandler handler = GetHandler(itemsControl);
			if (handler == null || handler.CanStart(obj, e.OriginalSource as DependencyObject))
			{
				dragState.DragSource = e.OriginalSource as DependencyObject;
				dragState.StartPoint = e.GetPosition(itemsControl);
				dragState.DragOffset = e.GetPosition(frameworkElement);
				dragState.DragWidth = frameworkElement.ActualWidth;
				dragState.DragItem = obj;
				dragState.DragIndex = itemsControl.Items.IndexOf(obj);
			}
		}
	}

	private static void OnPreviewMouseMove(object sender, MouseEventArgs e)
	{
		if (!(sender is ItemsControl itemsControl))
		{
			return;
		}
		DragState dragState = GetDragState(itemsControl);
		if (dragState == null || dragState.DragItem == null || dragState.IsAcquiringCapture)
		{
			return;
		}
		Point position = e.GetPosition(itemsControl);
		int dragThreshold = GetDragThreshold(itemsControl);
		if (e.LeftButton != MouseButtonState.Pressed)
		{
			if (dragState.IsDragging)
			{
				FinishDrag(itemsControl, dragState, cancelled: true);
			}
			dragState.Clear();
		}
		else if (dragState.IsDragging)
		{
			dragState.UpdatePreviewPosition(position);
			e.Handled = true;
			if (dragState.ParentScrollViewer != null)
			{
				Point position2 = e.GetPosition(dragState.ParentScrollViewer);
				ScrollViewer parentScrollViewer = dragState.ParentScrollViewer;
				double num = 50.0;
				double num2 = 10.0;
				double speed = 0.0;
				if (position2.Y < num && parentScrollViewer.VerticalOffset > 0.0)
				{
					double num3 = 1.0 - position2.Y / num;
					speed = (0.0 - num2) * num3;
				}
				else if (position2.Y > parentScrollViewer.ActualHeight - num && parentScrollViewer.VerticalOffset < parentScrollViewer.ScrollableHeight)
				{
					double num4 = parentScrollViewer.ActualHeight - position2.Y;
					double num5 = 1.0 - num4 / num;
					speed = num2 * num5;
				}
				dragState.UpdateScrollSpeed(speed);
			}
			IDragSortHandler handler = GetHandler(itemsControl);
			FrameworkElement frameworkElement = ((handler == null) ? FindContainerFromPoint(itemsControl, position) : FindDragTargetContainer(itemsControl, position));
			if (handler != null)
			{
				handler.Move(itemsControl, dragState.DragItem, (frameworkElement == null) ? null : itemsControl.ItemContainerGenerator.ItemFromContainer(frameworkElement), frameworkElement != null && e.GetPosition(frameworkElement).Y > frameworkElement.ActualHeight / 2.0);
			}
			else if (frameworkElement != null)
			{
				object item = itemsControl.ItemContainerGenerator.ItemFromContainer(frameworkElement);
				int num6 = itemsControl.Items.IndexOf(item);
				if (num6 >= 0 && num6 != dragState.DragIndex)
				{
					SwapItems(itemsControl, dragState, num6);
				}
			}
		}
		else if (Math.Abs(position.X - dragState.StartPoint.X) > (double)dragThreshold || Math.Abs(position.Y - dragState.StartPoint.Y) > (double)dragThreshold)
		{
			StartDrag(itemsControl, dragState, position);
			e.Handled = true;
		}
	}

	private static void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (!(sender is ItemsControl itemsControl))
		{
			return;
		}
		DragState dragState = GetDragState(itemsControl);
		if (dragState == null)
		{
			return;
		}
		if (dragState.IsDragging)
		{
			e.Handled = true;
			if (GetHandler(itemsControl) != null)
			{
				FrameworkElement frameworkElement = FindDragTargetContainer(itemsControl, e.GetPosition(itemsControl));
				GetHandler(itemsControl).Move(itemsControl, dragState.DragItem, (frameworkElement == null) ? null : itemsControl.ItemContainerGenerator.ItemFromContainer(frameworkElement), frameworkElement != null && e.GetPosition(frameworkElement).Y > frameworkElement.ActualHeight / 2.0);
				FinishDrag(itemsControl, dragState, !IsPositionInBounds(itemsControl, e.GetPosition(itemsControl)));
				return;
			}
			object dragItem = dragState.DragItem;
			int originalIndex = dragState.OriginalIndex;
			int dragIndex = dragState.DragIndex;
			EndDrag(itemsControl, dragState);
			bool flag = IsPositionInBounds(itemsControl, e.GetPosition(itemsControl));
			if (!flag && originalIndex != dragIndex)
			{
				if (itemsControl.ItemsSource is IList source)
				{
					MoveItem(source, dragIndex, originalIndex);
				}
			}
			else if (flag && originalIndex != dragIndex)
			{
				GetSortCompleted(itemsControl)?.Invoke(dragItem, originalIndex, dragIndex);
			}
		}
		dragState.Clear();
	}

	private static void StartDrag(ItemsControl itemsControl, DragState state, Point position)
	{
		int num = itemsControl.Items.IndexOf(state.DragItem);
		IDragSortHandler handler = GetHandler(itemsControl);
		if (num < 0 || (handler != null && !handler.CanStart(state.DragItem, state.DragSource ?? itemsControl)))
		{
			state.Clear();
			return;
		}
		state.DragIndex = num;
		state.OriginalIndex = num;
		state.IsDragging = true;
		itemsControl.SetValue(IsDraggingPropertyKey, true);
		if (!state.CaptureMouse(itemsControl) || !state.IsDragging)
		{
			if (state.IsDragging)
			{
				FinishDrag(itemsControl, state, cancelled: true);
			}
			return;
		}
		if (handler != null && !handler.Start(itemsControl, state.DragItem))
		{
			FinishDrag(itemsControl, state, cancelled: true);
			return;
		}
		if (handler != null)
		{
			state.ParentScrollViewer = FindChildScrollViewer(itemsControl);
		}
		if (state.DragItem is IDraggable draggable)
		{
			draggable.VisualState = DragVisualState.Placeholder;
		}
		state.ShowPreview(itemsControl, position);
		Mouse.OverrideCursor = GetDragCursor(itemsControl);
	}

	private static void EndDrag(ItemsControl itemsControl, DragState state)
	{
		state.IsDragging = false;
		itemsControl.SetValue(IsDraggingPropertyKey, false);
		Mouse.OverrideCursor = null;
		state.ReleaseMouseCapture();
		state.HidePreview();
		if (state.DragItem is IDraggable draggable)
		{
			draggable.VisualState = DragVisualState.Normal;
		}
	}

	private static void FinishDrag(ItemsControl itemsControl, DragState state, bool cancelled)
	{
		object dragItem = state.DragItem;
		EndDrag(itemsControl, state);
		try
		{
			IDragSortHandler handler = GetHandler(itemsControl);
			if (handler != null)
			{
				handler.Complete(itemsControl, dragItem, cancelled);
			}
			else if (cancelled && itemsControl.ItemsSource is IList source)
			{
				MoveItem(source, state.DragIndex, state.OriginalIndex);
			}
		}
		finally
		{
			state.Clear();
		}
	}

	private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Escape && sender is ItemsControl itemsControl)
		{
			DragState dragState = GetDragState(itemsControl);
			if (dragState != null && dragState.IsDragging)
			{
				e.Handled = true;
				FinishDrag(itemsControl, GetDragState(itemsControl), cancelled: true);
			}
		}
	}

	private static void OnLostMouseCapture(object sender, MouseEventArgs e)
	{
		if (sender is ItemsControl itemsControl)
		{
			DragState dragState = GetDragState(itemsControl);
			if (dragState != null && dragState.IsDragging && !dragState.IsAcquiringCapture && !dragState.HasMouseCapture)
			{
				FinishDrag(itemsControl, GetDragState(itemsControl), cancelled: true);
			}
		}
	}

	private static void SwapItems(ItemsControl itemsControl, DragState state, int hoverIndex)
	{
		if (itemsControl.ItemsSource is IList source)
		{
			MoveItem(source, state.DragIndex, hoverIndex);
			state.DragIndex = hoverIndex;
		}
	}

	private static void MoveItem(IList source, int oldIndex, int newIndex)
	{
		if (source != null && oldIndex != newIndex && oldIndex >= 0 && newIndex >= 0 && oldIndex < source.Count && newIndex < source.Count)
		{
			MethodInfo method = source.GetType().GetMethod("Move", new Type[2]
			{
				typeof(int),
				typeof(int)
			});
			if (method != null)
			{
				method.Invoke(source, new object[2] { oldIndex, newIndex });
			}
			else
			{
				object value = source[oldIndex];
				source.RemoveAt(oldIndex);
				source.Insert(newIndex, value);
			}
		}
	}

	private static FrameworkElement FindContainerFromPoint(ItemsControl itemsControl, Point point)
	{
		HitTestResult hitTestResult = VisualTreeHelper.HitTest(itemsControl, point);
		if (hitTestResult == null)
		{
			return null;
		}
		for (DependencyObject dependencyObject = hitTestResult.VisualHit; dependencyObject != null; dependencyObject = VisualTreeHelper.GetParent(dependencyObject))
		{
			if (dependencyObject is FrameworkElement container && itemsControl.ItemContainerGenerator.ContainerFromItem(itemsControl.ItemContainerGenerator.ItemFromContainer(container)) is FrameworkElement result)
			{
				return result;
			}
		}
		return null;
	}

	private static FrameworkElement FindDragTargetContainer(ItemsControl itemsControl, Point point)
	{
		if (!IsPositionInBounds(itemsControl, point))
		{
			return null;
		}
		return FindContainerFromPoint(itemsControl, point) ?? FindContainerFromPoint(itemsControl, new Point(point.X, point.Y - 2.0)) ?? FindContainerFromPoint(itemsControl, new Point(point.X, point.Y + 2.0));
	}

	private static bool IsFromDragHandle(DependencyObject source, DependencyObject stopAt, object dragHandleTag)
	{
		DependencyObject dependencyObject = source;
		while (dependencyObject != null && !object.Equals(dependencyObject, stopAt))
		{
			if (dependencyObject is FrameworkElement frameworkElement && object.Equals(frameworkElement.Tag, dragHandleTag))
			{
				return true;
			}
			dependencyObject = GetParent(dependencyObject);
		}
		return false;
	}

	private static DependencyObject GetParent(DependencyObject child)
	{
		if (child is Visual)
		{
			return VisualTreeHelper.GetParent(child);
		}
		return LogicalTreeHelper.GetParent(child);
	}

	private static ScrollViewer FindParentScrollViewer(DependencyObject child)
	{
		if (child is FrameworkElement { Parent: ScrollViewer parent })
		{
			return parent;
		}
		for (DependencyObject parent2 = VisualTreeHelper.GetParent(child); parent2 != null; parent2 = VisualTreeHelper.GetParent(parent2))
		{
			if (parent2 is ScrollViewer result)
			{
				return result;
			}
		}
		for (DependencyObject parent3 = LogicalTreeHelper.GetParent(child); parent3 != null; parent3 = LogicalTreeHelper.GetParent(parent3))
		{
			if (parent3 is ScrollViewer result2)
			{
				return result2;
			}
		}
		return null;
	}

	private static ScrollViewer FindChildScrollViewer(DependencyObject parent)
	{
		for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
		{
			DependencyObject child = VisualTreeHelper.GetChild(parent, i);
			if (child is ScrollViewer result)
			{
				return result;
			}
			ScrollViewer scrollViewer = FindChildScrollViewer(child);
			if (scrollViewer != null)
			{
				return scrollViewer;
			}
		}
		return null;
	}

	private static bool IsPositionInBounds(FrameworkElement element, Point position)
	{
		if (position.X < 0.0 || position.Y < 0.0)
		{
			return false;
		}
		if (position.X > element.ActualWidth || position.Y > element.ActualHeight)
		{
			return false;
		}
		return true;
	}
}
