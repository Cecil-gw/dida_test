using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace ticktick_WPF.Views.Misc;

public class VirtualizingListView : VirtualizingPanel, IScrollInfo
{
	internal class ExtentInfo
	{
		public double ExtentHeight;

		public double MaxVerticalOffset;
	}

	public class ItemLayoutInfo
	{
		public int FirstRealizedItemIndex;

		public double FirstRealizedLineTop;

		public int LastRealizedItemIndex;
	}

	private const double ScrollLineAmount = 16.0;

	private Size _extentSize;

	private Size _viewportSize;

	private Point _offset;

	private ItemsControl _itemsControl;

	private readonly Dictionary<UIElement, Rect> _childLayouts = new Dictionary<UIElement, Rect>();

	public static readonly DependencyProperty ItemWidthProperty = DependencyProperty.Register("ItemWidth", typeof(double), typeof(VirtualizingListView), new PropertyMetadata(-1.0, HandleItemDimensionChanged));

	public static readonly DependencyProperty ItemHeightProperty = DependencyProperty.Register("ItemHeight", typeof(double), typeof(VirtualizingListView), new PropertyMetadata(1.0, HandleItemDimensionChanged));

	private static readonly DependencyProperty VirtualItemIndexProperty = DependencyProperty.RegisterAttached("VirtualItemIndex", typeof(int), typeof(VirtualizingListView), new PropertyMetadata(-1));

	private IRecyclingItemContainerGenerator _itemsGenerator;

	private bool _isInMeasure;

	private bool _needRemeasure;

	public double ItemHeight
	{
		get
		{
			return (double)GetValue(ItemHeightProperty);
		}
		set
		{
			SetValue(ItemHeightProperty, value);
		}
	}

	public double ItemWidth
	{
		get
		{
			return (double)GetValue(ItemWidthProperty);
		}
		set
		{
			SetValue(ItemWidthProperty, value);
		}
	}

	public bool CanVerticallyScroll { get; set; }

	public bool CanHorizontallyScroll { get; set; }

	public double ExtentWidth => _extentSize.Width;

	public double ExtentHeight => _extentSize.Height;

	public double ViewportWidth => _viewportSize.Width;

	public double ViewportHeight => _viewportSize.Height;

	public double HorizontalOffset => _offset.X;

	public double VerticalOffset => _offset.Y;

	public ScrollViewer ScrollOwner { get; set; }

	private static int GetVirtualItemIndex(DependencyObject obj)
	{
		return (int)obj.GetValue(VirtualItemIndexProperty);
	}

	private static void SetVirtualItemIndex(DependencyObject obj, int value)
	{
		obj.SetValue(VirtualItemIndexProperty, value);
	}

	public VirtualizingListView()
	{
		if (!DesignerProperties.GetIsInDesignMode(this))
		{
			base.Dispatcher.BeginInvoke(new Action(Initialize));
		}
	}

	private void Initialize()
	{
		_itemsControl = ItemsControl.GetItemsOwner(this);
		_itemsGenerator = (IRecyclingItemContainerGenerator)base.ItemContainerGenerator;
		InvalidateMeasure();
	}

	protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args)
	{
		base.OnItemsChanged(sender, args);
		InvalidateMeasure();
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		if (_itemsControl == null)
		{
			return new Size(double.IsInfinity(availableSize.Width) ? 0.0 : availableSize.Width, double.IsInfinity(availableSize.Height) ? 0.0 : availableSize.Height);
		}
		if (_itemsGenerator == null)
		{
			_itemsGenerator = (IRecyclingItemContainerGenerator)base.ItemContainerGenerator;
			if (_itemsGenerator == null)
			{
				return new Size(double.IsInfinity(availableSize.Width) ? 0.0 : availableSize.Width, double.IsInfinity(availableSize.Height) ? 0.0 : availableSize.Height);
			}
		}
		_isInMeasure = true;
		_childLayouts.Clear();
		var (extentInfo, itemLayoutInfo) = GetInfos(availableSize);
		RecycleItems(itemLayoutInfo);
		GeneratorPosition position = _itemsGenerator.GeneratorPositionFromIndex(itemLayoutInfo.FirstRealizedItemIndex);
		int num = 0;
		double num2 = itemLayoutInfo.FirstRealizedLineTop;
		using (_itemsGenerator.StartAt(position, GeneratorDirection.Forward, allowStartAtRealizedItem: true))
		{
			int num3 = itemLayoutInfo.FirstRealizedItemIndex;
			while (num3 <= itemLayoutInfo.LastRealizedItemIndex && num3 >= 0)
			{
				bool isNewlyRealized;
				FrameworkElement frameworkElement = (FrameworkElement)_itemsGenerator.GenerateNext(out isNewlyRealized);
				SetVirtualItemIndex(frameworkElement, num3);
				if (isNewlyRealized)
				{
					InsertInternalChild(num, frameworkElement);
				}
				else if (!base.Children.Contains(frameworkElement))
				{
					AddInternalChild(frameworkElement);
				}
				_itemsGenerator.PrepareItemContainer(frameworkElement);
				double width = ((ItemWidth < 0.0) ? availableSize.Width : ItemWidth);
				double itemHeight = ItemHeight;
				if (frameworkElement.DataContext is IVirtualizingItem virtualizingItem)
				{
					itemHeight = virtualizingItem.GetItemHeight();
				}
				_childLayouts.Add(frameworkElement, new Rect(0.0, num2, width, itemHeight));
				num2 += itemHeight;
				num3++;
				num++;
			}
		}
		EnsureScrollOffsetIsWithinConstrains(extentInfo);
		RemoveRedundantChildren();
		UpdateScrollInfo(availableSize, extentInfo);
		Size result = new Size(double.IsInfinity(availableSize.Width) ? 0.0 : availableSize.Width, double.IsInfinity(availableSize.Height) ? 0.0 : availableSize.Height);
		_isInMeasure = false;
		if (_needRemeasure)
		{
			TryInvalidateMeasure();
		}
		return result;
	}

	private void EnsureScrollOffsetIsWithinConstrains(ExtentInfo extentInfo)
	{
		_offset.Y = Clamp(_offset.Y, 0.0, extentInfo.MaxVerticalOffset);
	}

	private void RecycleItems(ItemLayoutInfo layoutInfo)
	{
		foreach (UIElement child in base.Children)
		{
			SetVirtualItemIndex(child, -1);
		}
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		foreach (UIElement child in base.Children)
		{
			child.Arrange(_childLayouts[child]);
		}
		return finalSize;
	}

	private void UpdateScrollInfo(Size availableSize, ExtentInfo extentInfo)
	{
		_viewportSize = availableSize;
		_extentSize = new Size(availableSize.Width, extentInfo.ExtentHeight);
		InvalidateScrollInfo();
	}

	private void RemoveRedundantChildren()
	{
		for (int num = base.Children.Count - 1; num >= 0; num--)
		{
			if (GetVirtualItemIndex(base.Children[num]) == -1)
			{
				RemoveInternalChildRange(num, 1);
			}
		}
	}

	private (ExtentInfo, ItemLayoutInfo) GetInfos(Size viewPortSize)
	{
		if (_itemsControl == null)
		{
			return (new ExtentInfo(), new ItemLayoutInfo());
		}
		ExtentInfo extentInfo = new ExtentInfo();
		ItemLayoutInfo itemLayoutInfo = new ItemLayoutInfo();
		List<IVirtualizingItem> list = null;
		try
		{
			list = _itemsControl.ItemsSource.Cast<IVirtualizingItem>().ToList();
		}
		catch (Exception)
		{
		}
		if (list != null)
		{
			double num = 0.0;
			int num2 = -1;
			double num3 = -1.0;
			for (int i = 0; i < list.Count; i++)
			{
				IVirtualizingItem virtualizingItem = list[i];
				num += virtualizingItem.GetItemHeight();
				if (num2 < 0 && num >= VerticalOffset)
				{
					num2 = Math.Max(0, i - 2);
					num3 = num - VerticalOffset - virtualizingItem.GetItemHeight() - ((i >= 2) ? list[i - 2].GetItemHeight() : 0.0) - ((i >= 1) ? list[i - 1].GetItemHeight() : 0.0);
				}
			}
			int num4 = (int)Math.Ceiling((viewPortSize.Height - num3) / ItemHeight);
			itemLayoutInfo.FirstRealizedItemIndex = num2;
			itemLayoutInfo.FirstRealizedLineTop = num3;
			itemLayoutInfo.LastRealizedItemIndex = Math.Min(num2 + num4 + 2, _itemsControl.Items.Count - 1);
			extentInfo.ExtentHeight = num;
		}
		else
		{
			double num5 = Math.Max((double)_itemsControl.Items.Count * ItemHeight, viewPortSize.Height);
			extentInfo.ExtentHeight = num5 + 10.0;
			int num6 = (int)Math.Floor(VerticalOffset / ItemHeight);
			int num7 = Math.Max(num6 - 2, 0);
			double num8 = (double)num7 * ItemHeight - VerticalOffset;
			double num9 = ((num6 == 0) ? num8 : (num8 + ItemHeight));
			int num10 = (int)Math.Ceiling((viewPortSize.Height - num9) / ItemHeight);
			int lastRealizedItemIndex = Math.Min(num7 + num10 + 2, _itemsControl.Items.Count - 1);
			itemLayoutInfo.FirstRealizedItemIndex = num7;
			itemLayoutInfo.FirstRealizedLineTop = num8;
			itemLayoutInfo.LastRealizedItemIndex = lastRealizedItemIndex;
		}
		extentInfo.MaxVerticalOffset = extentInfo.ExtentHeight - viewPortSize.Height;
		return (extentInfo, itemLayoutInfo);
	}

	public void LineUp()
	{
		SetVerticalOffset(VerticalOffset - 16.0);
	}

	public void LineDown()
	{
		SetVerticalOffset(VerticalOffset + 16.0);
	}

	public void LineLeft()
	{
		SetHorizontalOffset(HorizontalOffset + 16.0);
	}

	public void LineRight()
	{
		SetHorizontalOffset(HorizontalOffset - 16.0);
	}

	public void PageUp()
	{
		SetVerticalOffset(VerticalOffset - ViewportHeight);
	}

	public void PageDown()
	{
		SetVerticalOffset(VerticalOffset + ViewportHeight);
	}

	public void PageLeft()
	{
		SetHorizontalOffset(HorizontalOffset + base.ActualWidth);
	}

	public void PageRight()
	{
		SetHorizontalOffset(HorizontalOffset - base.ActualWidth);
	}

	public void MouseWheelUp()
	{
		SetVerticalOffset(VerticalOffset - 16.0 * (double)SystemParameters.WheelScrollLines);
	}

	public void MouseWheelDown()
	{
		SetVerticalOffset(VerticalOffset + 16.0 * (double)SystemParameters.WheelScrollLines);
	}

	public void MouseWheelLeft()
	{
		SetHorizontalOffset(HorizontalOffset - 16.0 * (double)SystemParameters.WheelScrollLines);
	}

	public void MouseWheelRight()
	{
		SetHorizontalOffset(HorizontalOffset + 16.0 * (double)SystemParameters.WheelScrollLines);
	}

	public void SetHorizontalOffset(double offset)
	{
		if (!_isInMeasure)
		{
			offset = Clamp(offset, 0.0, ExtentWidth - ViewportWidth);
			_offset = new Point(offset, _offset.Y);
			InvalidateScrollInfo();
			InvalidateMeasure();
		}
	}

	public void SetVerticalOffset(double offset)
	{
		if (!_isInMeasure)
		{
			offset = Clamp(offset, 0.0, ExtentHeight - ViewportHeight);
			_offset = new Point(_offset.X, offset);
			InvalidateScrollInfo();
			InvalidateMeasure();
		}
	}

	public Rect MakeVisible(Visual visual, Rect rectangle)
	{
		if (rectangle.IsEmpty || visual == null || visual == this || !IsAncestorOf(visual))
		{
			return Rect.Empty;
		}
		rectangle = visual.TransformToAncestor(this).TransformBounds(rectangle);
		Rect rect = new Rect(HorizontalOffset, VerticalOffset, ViewportWidth, ViewportHeight);
		rectangle.X += rect.X;
		rectangle.Y += rect.Y;
		rect.X = CalculateNewScrollOffset(rect.Left, rect.Right, rectangle.Left, rectangle.Right);
		rect.Y = CalculateNewScrollOffset(rect.Top, rect.Bottom, rectangle.Top, rectangle.Bottom);
		SetHorizontalOffset(rect.X);
		SetVerticalOffset(rect.Y);
		rectangle.Intersect(rect);
		rectangle.X -= rect.X;
		rectangle.Y -= rect.Y;
		return rectangle;
	}

	private static double CalculateNewScrollOffset(double topView, double bottomView, double topChild, double bottomChild)
	{
		bool flag = topChild < topView && bottomChild < bottomView;
		bool flag2 = bottomChild > bottomView && topChild > topView;
		bool flag3 = bottomChild - topChild > bottomView - topView;
		if (!flag && !flag2)
		{
			return topView;
		}
		if ((flag && !flag3) || (flag2 && flag3))
		{
			return topChild;
		}
		return bottomChild - (bottomView - topView);
	}

	private void InvalidateScrollInfo()
	{
		if (ScrollOwner != null)
		{
			ScrollOwner.InvalidateScrollInfo();
		}
	}

	private static void HandleItemDimensionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is VirtualizingListView virtualizingListView)
		{
			virtualizingListView.InvalidateMeasure();
		}
	}

	private double Clamp(double value, double min, double max)
	{
		return Math.Min(Math.Max(value, min), max);
	}

	public void TryInvalidateMeasure()
	{
		if (_isInMeasure)
		{
			_needRemeasure = true;
		}
		else
		{
			InvalidateMeasure();
		}
	}
}
