using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util.DragAndDrop;
using ticktick_WPF.Views.Calendar.Month;

namespace ticktick_WPF.Views.Calendar.Week;

public class MultiDayAllDayItems : Canvas
{
	private static int _initOffset = 4000;

	private MultiDayView _parent;

	private readonly SemaphoreSlim _checkLocker = new SemaphoreSlim(1, 1);

	private List<WeekEventModel> _itemsSource;

	private readonly ConcurrentDictionary<string, TaskBar> _dragItems = new ConcurrentDictionary<string, TaskBar>();

	public double ViewHeight;

	public double ViewWidth;

	public double HOffset;

	public double VOffset;

	private readonly BlockingSet<TaskBar> _hideItems = new BlockingSet<TaskBar>();

	private readonly ConcurrentDictionary<WeekEventModel, TaskBar> _showItemsDict = new ConcurrentDictionary<WeekEventModel, TaskBar>();

	private long _lastCheckTime;

	private double _dayWidth;

	public MultiDayAllDayItems(MultiDayView parent)
	{
		_parent = parent;
		base.Width = 8000.0;
		_itemsSource = new List<WeekEventModel>();
	}

	public void OnFontSizeChanged()
	{
		SetItemsSource(_itemsSource, checkHeight: true);
	}

	public async void SetDragItems(List<WeekEventModel> items)
	{
		if (items == null)
		{
			ConcurrentDictionary<string, TaskBar> dragItems = _dragItems;
			if (dragItems == null || dragItems.Count <= 0)
			{
				return;
			}
			foreach (string item in _dragItems.Keys.ToList())
			{
				if (_dragItems.TryGetValue(item, out var value))
				{
					base.Children.Remove(value);
				}
			}
			_dragItems.Clear();
			return;
		}
		List<string> list = _dragItems.Keys.ToList();
		foreach (WeekEventModel item2 in items)
		{
			list.Remove(item2.Data.TaskId);
			if (!_dragItems.TryGetValue(item2.Data.TaskId, out var value2))
			{
				value2 = new TaskBar(isDragMode: true);
				base.Children.Add(value2);
				_dragItems[item2.Data.TaskId] = value2;
			}
			value2.Visibility = Visibility.Visible;
			value2.DataContext = item2.Data;
			value2.SetMultiDayStyle(item2.Row);
			value2.SetValue(Panel.ZIndexProperty, item2.Data.Dragging ? 10 : 5);
			double num = HOffset + (double)item2.DisplayColumn * _parent.DayViewWidth + 1.0;
			int num2 = item2.Row * CalendarGeoHelper.TaskBarHeight;
			Canvas.SetTop(value2, num2);
			Canvas.SetLeft(value2, num);
			value2.Width = (double)item2.ColumnSpan * _parent.DayViewWidth;
			int num3 = ((LocalSettings.Settings.TimeFormat == "12Hour") ? 9 : 7);
			value2.SetTitlePosition(item2.DisplayColumn, item2.ColumnSpan, _parent.DayViewWidth, item2.Data.IsCalendarEvent);
			item2.Data.WidthEnoughShowIcon = _parent.DayViewWidth > CalendarGeoHelper.CalTitleFontSize * (double)num3 - 4.0;
			bool showIcon = !item2.Data.IsTaskIconType || item2.Data.WidthEnoughShowIcon;
			value2.SetShowIcon(showIcon, item2.Data.ForceShowIcon);
			item2.Position = new Rect(num + 2.0, num2 + 1, Math.Max(0.0, value2.Width - 4.0), Math.Max(0, CalendarGeoHelper.TaskBarHeight - 2));
		}
		foreach (string item3 in list)
		{
			if (_dragItems.TryRemove(item3, out var value3))
			{
				base.Children.Remove(value3);
			}
		}
	}

	public async Task SetItemsSource(List<WeekEventModel> items, bool checkHeight = false)
	{
		await _checkLocker.WaitAsync();
		try
		{
			if (!checkHeight)
			{
				HideAllItems();
			}
			_itemsSource = items;
			BlockingSet<string> blockingSet = _parent?.GetSelectedIds();
			Dictionary<string, int> dictionary = _parent?.GetSelectedIdsRow();
			foreach (WeekEventModel item in items)
			{
				if (blockingSet != null && item.Data.CanBatchSelected && blockingSet.Contains(item.Data.TaskId))
				{
					item.Data.BatchSelected = true;
					item.Data.Dragging = TickDragHelper.Dragging;
					if (dictionary != null)
					{
						dictionary[item.Data.TaskId] = item.Row;
					}
				}
				if (CheckModelVisible(item))
				{
					if (!_showItemsDict.TryGetValue(item, out var value))
					{
						ShowItem(item);
						continue;
					}
					if (checkHeight)
					{
						value.SetMultiDayStyle(item.Row);
						item.Data.WidthEnoughShowIcon = _parent.DayViewWidth > CalendarGeoHelper.CalTitleFontSize * 7.0 - 4.0;
						bool flag = !item.Data.IsTaskIconType || item.Data.WidthEnoughShowIcon;
						value.SetShowIcon(StaticPropertiesModel.Instance.AltKeyDown || flag, item.Data.ForceShowIcon);
						value.SetTitlePosition(item.DisplayColumn, item.ColumnSpan, _parent.DayViewWidth, item.Data.IsCalendarEvent);
					}
					value.SetValue(Panel.ZIndexProperty, item.Data.Dragging ? 10 : 5);
				}
				else
				{
					HideItem(item);
				}
			}
			base.Height = (_itemsSource.Any() ? ((_itemsSource.Max((WeekEventModel m) => m.Row) + 1) * CalendarGeoHelper.TaskBarHeight + 2) : 0);
		}
		finally
		{
			_checkLocker.Release();
		}
	}

	private void ShowItem(WeekEventModel model, int diffDays = 0)
	{
		TaskBar taskBar = _hideItems.FirstOrDefault();
		if (taskBar != null)
		{
			_hideItems.Remove(taskBar);
			_showItemsDict[model] = taskBar;
			taskBar.Visibility = Visibility.Visible;
			taskBar.DataContext = model.Data;
		}
		else
		{
			TaskBar taskBar2 = new TaskBar
			{
				DataContext = model.Data
			};
			base.Children.Add(taskBar2);
			_showItemsDict[model] = taskBar2;
			taskBar = taskBar2;
		}
		taskBar.SetMultiDayStyle(model.Row);
		taskBar.SetValue(Panel.ZIndexProperty, model.Data.Dragging ? 10 : 5);
		double num = (double)_initOffset + (double)model.DisplayColumn * _parent.DayViewWidth + 1.0;
		int num2 = model.Row * CalendarGeoHelper.TaskBarHeight;
		Canvas.SetTop(taskBar, num2);
		Canvas.SetLeft(taskBar, num);
		taskBar.Width = (double)model.ColumnSpan * _parent.DayViewWidth;
		int num3 = ((LocalSettings.Settings.TimeFormat == "12Hour") ? 9 : 7);
		taskBar.SetTitlePosition(model.DisplayColumn, model.ColumnSpan, _parent.DayViewWidth, model.Data.IsCalendarEvent);
		model.Data.WidthEnoughShowIcon = _parent.DayViewWidth > CalendarGeoHelper.CalTitleFontSize * (double)num3 - 4.0;
		bool flag = !model.Data.IsTaskIconType || model.Data.WidthEnoughShowIcon;
		taskBar.SetShowIcon(StaticPropertiesModel.Instance.AltKeyDown || flag, model.Data.ForceShowIcon);
		model.Position = new Rect(num + 2.0, num2 + 1, Math.Max(0.0, taskBar.Width - 4.0), Math.Max(0, CalendarGeoHelper.TaskBarHeight - 2));
	}

	private void HideAllItems()
	{
		foreach (WeekEventModel item in _showItemsDict.Keys.ToList())
		{
			if (_showItemsDict.TryRemove(item, out var value))
			{
				value.DataContext = null;
				value.Visibility = Visibility.Hidden;
				_hideItems.Add(value);
			}
		}
	}

	private void HideItem(WeekEventModel model)
	{
		if (model != null && _showItemsDict.TryRemove(model, out var value))
		{
			value.DataContext = null;
			value.Visibility = Visibility.Hidden;
			_hideItems.Add(value);
		}
	}

	private async Task CheckAndSetAllCellVisible()
	{
		if (_itemsSource == null)
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
			foreach (WeekEventModel item in _itemsSource)
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

	private bool CheckModelVisible(WeekEventModel model)
	{
		if ((double)_initOffset + (double)model.DisplayColumn * _dayWidth > ViewWidth + HOffset + 20.0 || (double)_initOffset + (double)(model.DisplayColumn + model.ColumnSpan) * _dayWidth < HOffset - 20.0)
		{
			return false;
		}
		int taskBarHeight = CalendarGeoHelper.TaskBarHeight;
		if (!((double)(model.Row * taskBarHeight) > ViewHeight + VOffset + (double)taskBarHeight * 1.5))
		{
			return !((double)(model.Row * taskBarHeight) < VOffset - (double)taskBarHeight * 1.5);
		}
		return false;
	}

	protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
	{
		base.OnRenderSizeChanged(sizeInfo);
		CheckAndSetAllCellVisible();
	}

	public void SetDayWidth(double dayWidth)
	{
		_dayWidth = dayWidth;
	}

	public void SetSize(Size size)
	{
		ViewHeight = size.Height;
		ViewWidth = size.Width;
	}

	public void SetScrollOffset(double vOffset, bool checkVisible = false)
	{
		HOffset = (double)_initOffset - ((base.RenderTransform as TranslateTransform)?.X ?? 0.0);
		VOffset = vOffset;
		if (checkVisible && !_parent.GetBarDragging())
		{
			CheckAndSetAllCellVisible();
		}
	}

	public void Clear()
	{
		base.Children.Clear();
		_hideItems.Clear();
		_showItemsDict.Clear();
	}

	public void RemoveDraggingItems()
	{
		foreach (WeekEventModel item in _showItemsDict.Keys.ToList())
		{
			if (item.Data.Dragging && _showItemsDict.TryRemove(item, out var value))
			{
				value.DataContext = null;
				base.Children.Remove(value);
			}
		}
	}

	public void SetDragSideBar(CalendarDisplayViewModel model, DateTime startDate, DateTime endDate, bool dragLeft)
	{
		if (model.DisplayStartDate.HasValue)
		{
			WeekEventModel weekEventModel = _itemsSource.FirstOrDefault((WeekEventModel i) => i.Data == model);
			if (weekEventModel != null && _showItemsDict.TryGetValue(weekEventModel, out var value))
			{
				weekEventModel.Column = model.Column + (int)(startDate - model.DisplayStartDate.Value.Date).TotalDays;
				weekEventModel.ColumnSpan = Math.Max(1, (int)(endDate - startDate).TotalDays);
				Canvas.SetLeft(value, (double)_initOffset + (double)weekEventModel.DisplayColumn * _parent.DayViewWidth + 1.0);
				value.Width = (double)weekEventModel.ColumnSpan * _parent.DayViewWidth;
				value.SetValue(Panel.ZIndexProperty, 10);
				value.SetDragBackRect(show: true);
			}
		}
	}

	public void SetShowIcon()
	{
		foreach (WeekEventModel item in _showItemsDict.Keys.ToList())
		{
			if (_showItemsDict.TryGetValue(item, out var value))
			{
				item.Data.SetIcon(notify: true);
				int num = ((LocalSettings.Settings.TimeFormat == "12Hour") ? 9 : 7);
				item.Data.WidthEnoughShowIcon = _parent.DayViewWidth > CalendarGeoHelper.CalTitleFontSize * (double)num - 4.0;
				bool flag = !item.Data.IsTaskIconType || item.Data.WidthEnoughShowIcon;
				value.SetShowIcon(StaticPropertiesModel.Instance.AltKeyDown || flag, item.Data.ForceShowIcon);
			}
		}
	}

	public void SetColor()
	{
		foreach (WeekEventModel item in _showItemsDict.Keys.ToList())
		{
			if (_showItemsDict.TryGetValue(item, out var _))
			{
				item.Data.SetColor();
			}
		}
	}

	public void TryBatchSelectInRect(Rect rect)
	{
		foreach (WeekEventModel item in _showItemsDict.Keys.ToList())
		{
			CalendarDisplayViewModel data = item.Data;
			if (!data.IsTaskOrNote || !data.SourceViewModel.Editable || data.RepeatDiff > 0.0)
			{
				continue;
			}
			if (item.Position.IntersectsWith(rect))
			{
				if (!data.TempBatchSelect && !data.BatchSelected)
				{
					data.TempBatchSelect = true;
				}
			}
			else if (data.TempBatchSelect)
			{
				data.TempBatchSelect = false;
			}
		}
	}

	public Dictionary<string, int> SetBatchSelectInTemp()
	{
		List<WeekEventModel> source = _showItemsDict.Keys.ToList();
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		foreach (WeekEventModel item in source.Where((WeekEventModel model) => model.Data.TempBatchSelect))
		{
			item.Data.BatchSelected = true;
			item.Data.TempBatchSelect = false;
			dictionary[item.Data.TaskId] = item.Row;
		}
		return dictionary;
	}

	public void ClearBatchSelected()
	{
		foreach (WeekEventModel item in _itemsSource)
		{
			item.Data.BatchSelected = false;
			item.Data.TempBatchSelect = false;
		}
	}

	public void SetBatchSelected(BlockingSet<string> batchSelected)
	{
		foreach (WeekEventModel item in _showItemsDict.Keys.ToList())
		{
			item.Data.BatchSelected = item.Data.CanBatchSelected && batchSelected.Contains(item.Data.TaskId);
			item.Data.TempBatchSelect = false;
		}
	}

	public void SetBatchDragging(bool dragging)
	{
		foreach (WeekEventModel item in _showItemsDict.Keys.ToList())
		{
			if (item.Data.BatchSelected)
			{
				item.Data.BatchDragging = dragging;
			}
		}
	}
}
