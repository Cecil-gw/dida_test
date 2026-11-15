using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ticktick_WPF.Models;

namespace ticktick_WPF.Views.Timeline;

public class TimelineVirtualizedCanvas : Canvas
{
	private readonly SemaphoreSlim _checkLocker = new SemaphoreSlim(1, 1);

	public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(TimeLineItemList), typeof(TimelineVirtualizedCanvas), new PropertyMetadata(OnItemsSourcePropertyChanged));

	public static readonly DependencyProperty ViewHeightProperty = DependencyProperty.Register("ViewHeight", typeof(double), typeof(TimelineVirtualizedCanvas), new PropertyMetadata(0.0, OnViewSizeChanged));

	public static readonly DependencyProperty ViewWidthProperty = DependencyProperty.Register("ViewWidth", typeof(double), typeof(TimelineVirtualizedCanvas), new PropertyMetadata(0.0, OnViewSizeChanged));

	public static readonly DependencyProperty HOffsetProperty = DependencyProperty.Register("HOffset", typeof(double), typeof(TimelineVirtualizedCanvas), new PropertyMetadata(0.0, OnViewSizeChanged));

	public static readonly DependencyProperty VOffsetProperty = DependencyProperty.Register("VOffset", typeof(double), typeof(TimelineVirtualizedCanvas), new PropertyMetadata(0.0, OnViewSizeChanged));

	public static readonly DependencyProperty IsResetProperty = DependencyProperty.Register("IsReset", typeof(bool), typeof(TimelineVirtualizedCanvas), new PropertyMetadata(false));

	private readonly BlockingSet<TimelineCellInline> _hideItemsDict = new BlockingSet<TimelineCellInline>();

	private readonly ConcurrentDictionary<TimelineCellViewModel, TimelineCellInline> _showItemsDict = new ConcurrentDictionary<TimelineCellViewModel, TimelineCellInline>();

	private bool _check;

	private double _checkedHOffset;

	private double _checkedVOffset;

	private double _checkedHeight;

	private double _checkedWidth;

	private long _lastCheckTime;

	public bool IsReset
	{
		get
		{
			return (bool)GetValue(IsResetProperty);
		}
		set
		{
			SetValue(IsResetProperty, value);
		}
	}

	public TimeLineItemList ItemsSource
	{
		get
		{
			return (TimeLineItemList)GetValue(ItemsSourceProperty);
		}
		set
		{
			SetValue(ItemsSourceProperty, value);
		}
	}

	public double ViewHeight
	{
		get
		{
			return (double)GetValue(ViewHeightProperty);
		}
		set
		{
			SetValue(ViewHeightProperty, value);
		}
	}

	public double ViewWidth
	{
		get
		{
			return (double)GetValue(ViewWidthProperty);
		}
		set
		{
			SetValue(ViewWidthProperty, value);
		}
	}

	public double HOffset
	{
		get
		{
			return (double)GetValue(HOffsetProperty);
		}
		set
		{
			SetValue(HOffsetProperty, value);
		}
	}

	public double VOffset
	{
		get
		{
			return (double)GetValue(VOffsetProperty);
		}
		set
		{
			SetValue(VOffsetProperty, value);
		}
	}

	private static void OnItemsSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is TimelineVirtualizedCanvas timelineVirtualizedCanvas)
		{
			timelineVirtualizedCanvas.OnItemsSourceChanged(e.OldValue as TimeLineItemList, e.NewValue as TimeLineItemList);
		}
	}

	private void OnItemsSourceChanged(TimeLineItemList oldValue, TimeLineItemList newValue)
	{
		if (oldValue != null)
		{
			oldValue.ItemsChange -= OnItemsSourceChanged;
		}
		if (newValue != null)
		{
			newValue.ClearEvents();
			newValue.ItemsChange += OnItemsSourceChanged;
		}
	}

	private void OnItemsSourceChanged(object sender, ListItemChangeArgs<TimelineCellViewModel> e)
	{
		switch (e.Action)
		{
		case ListChangeAction.Remove:
			if (e.Items == null)
			{
				break;
			}
			{
				foreach (TimelineCellViewModel item in e.Items)
				{
					HideItem(item);
				}
				break;
			}
		case ListChangeAction.Clear:
		{
			foreach (TimelineCellViewModel item2 in e.Items)
			{
				HideItem(item2);
			}
			break;
		}
		case ListChangeAction.Add:
		{
			foreach (TimelineCellViewModel item3 in e.Items)
			{
				CheckAndSetCellModelVisible(item3);
			}
			break;
		}
		case ListChangeAction.Change:
		{
			HashSet<TimelineCellViewModel> models = new HashSet<TimelineCellViewModel>(ItemsSource.ToList());
			{
				foreach (TimelineCellViewModel item4 in e.Items.Where((TimelineCellViewModel item) => models.Contains(item)))
				{
					CheckAndSetCellModelVisible(item4);
				}
				break;
			}
		}
		case ListChangeAction.Replace:
			break;
		}
	}

	private void ShowItem(TimelineCellViewModel model)
	{
		model.SetColorAndAvatar();
		TimelineCellInline timelineCellInline = _hideItemsDict.FirstOrDefault();
		if (timelineCellInline != null)
		{
			_hideItemsDict.Remove(timelineCellInline);
			_showItemsDict[model] = timelineCellInline;
			timelineCellInline.Opacity = 1.0;
			timelineCellInline.DataContext = model;
		}
		else
		{
			TimelineCellInline timelineCellInline2 = new TimelineCellInline
			{
				DataContext = model
			};
			base.Children.Add(timelineCellInline2);
			_showItemsDict[model] = timelineCellInline2;
		}
	}

	private void HideItem(TimelineCellViewModel model)
	{
		if (model != null && _showItemsDict.TryRemove(model, out var value))
		{
			base.Children.Remove(value);
		}
	}

	private async Task CheckAndSetAllCellVisible()
	{
		if (ItemsSource == null)
		{
			return;
		}
		long lastCheckTime = (_lastCheckTime = DateTime.Now.Ticks);
		await _checkLocker.WaitAsync();
		try
		{
			if (lastCheckTime != _lastCheckTime)
			{
				return;
			}
			_checkedHOffset = HOffset;
			_checkedVOffset = VOffset;
			_checkedHeight = ViewHeight;
			_checkedWidth = ViewWidth;
			foreach (TimelineCellViewModel item in ItemsSource.ToList())
			{
				if (CheckModelVisible(item))
				{
					if (!_showItemsDict.ContainsKey(item))
					{
						ShowItem(item);
					}
				}
				else
				{
					HideItem(item);
				}
			}
		}
		finally
		{
			_checkLocker.Release();
		}
	}

	private bool CheckModelVisible(TimelineCellViewModel model)
	{
		if (model.Top < 0.0)
		{
			return false;
		}
		if (model.Left > ViewWidth + HOffset + 550.0 || model.Left + model.Width < HOffset - 550.0)
		{
			return false;
		}
		if (!(model.Top > ViewHeight + VOffset + 100.0))
		{
			return !(model.Top < VOffset - 100.0);
		}
		return false;
	}

	private async Task CheckAndSetCellModelVisible(TimelineCellViewModel model)
	{
		await _checkLocker.WaitAsync();
		try
		{
			if (CheckModelVisible(model))
			{
				if (!_showItemsDict.ContainsKey(model))
				{
					ShowItem(model);
				}
			}
			else
			{
				HideItem(model);
			}
		}
		finally
		{
			_checkLocker.Release();
		}
	}

	private static void OnViewSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (!(d is TimelineVirtualizedCanvas timelineVirtualizedCanvas))
		{
			return;
		}
		switch (e.Property.Name)
		{
		case "HOffset":
			if (Math.Abs(timelineVirtualizedCanvas._checkedHOffset - timelineVirtualizedCanvas.HOffset) < 500.0)
			{
				return;
			}
			break;
		case "VOffset":
			if (Math.Abs(timelineVirtualizedCanvas._checkedVOffset - timelineVirtualizedCanvas.VOffset) < 50.0)
			{
				return;
			}
			break;
		case "ViewHeight":
			if (Math.Abs(timelineVirtualizedCanvas._checkedHeight - timelineVirtualizedCanvas.ViewHeight) < 50.0)
			{
				return;
			}
			break;
		case "ViewWidth":
			if (Math.Abs(timelineVirtualizedCanvas._checkedWidth - timelineVirtualizedCanvas.ViewWidth) < 500.0)
			{
				return;
			}
			break;
		}
		timelineVirtualizedCanvas.CheckAndSetAllCellVisible();
	}

	protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
	{
		base.OnRenderSizeChanged(sizeInfo);
		CheckAndSetAllCellVisible();
	}

	public void TryShowDetailWindow(TimelineCellViewModel cellModel)
	{
		if (_showItemsDict.TryGetValue(cellModel, out var value))
		{
			value.ShowDetail();
		}
	}
}
