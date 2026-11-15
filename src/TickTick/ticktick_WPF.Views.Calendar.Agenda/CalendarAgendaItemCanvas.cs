using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Calendar.Agenda;

public class CalendarAgendaItemCanvas : Canvas
{
	private Dictionary<CalendarAgendaItemViewModel, CalendarAgendaItem> DisplayItemDict = new Dictionary<CalendarAgendaItemViewModel, CalendarAgendaItem>();

	public int StartIndex = -1;

	private CalendarAgendaItemDateTitle _dateTitle = new CalendarAgendaItemDateTitle();

	private bool _scrolling;

	private bool _loading;

	private int _itemsCount = 10;

	private double _displayHeight;

	private readonly CalendarAgendaView _agendaView;

	private string _selectedId;

	private double _selectedDiff;

	private readonly SemaphoreSlim _loadSemaphoreSlim = new SemaphoreSlim(1, 1);

	private List<CalendarAgendaItemViewModel> _items { get; set; } = new List<CalendarAgendaItemViewModel>();

	private double CurrentOffset { get; set; }

	private int CurrentIndex { get; set; }

	private DateTime CurrentDate { get; set; }

	public CalendarAgendaItemCanvas(CalendarAgendaView agendaView)
	{
		_agendaView = agendaView;
		base.Background = Brushes.Transparent;
		base.VerticalAlignment = VerticalAlignment.Stretch;
		base.HorizontalAlignment = HorizontalAlignment.Center;
		base.MaxWidth = 1040.0;
		base.Margin = new Thickness(30.0, -384.0, 20.0, 20.0);
		CalendarEventChangeNotifier.TitleChanged += NotifyEventTitleChanged;
		base.Children.Add(_dateTitle);
		Canvas.SetTop(_dateTitle, 410.0);
	}

	private void NotifyEventTitleChanged(object sender, TextExtra e)
	{
		foreach (CalendarAgendaItemViewModel item in _items.Where((CalendarAgendaItemViewModel task) => task.Id == e.Id))
		{
			item.SourceViewModel.Title = e.Text;
		}
	}

	public void OnMouseWheel(object sender, MouseWheelEventArgs e)
	{
		double delta = (double)e.Delta / (double)((Math.Abs(e.Delta) <= 100) ? 1 : 2);
		Scroll(delta);
	}

	private async Task Scroll(double delta, bool loadMore = true)
	{
		if (_loading)
		{
			return;
		}
		DateTime time = DateTime.Now;
		await _loadSemaphoreSlim.WaitAsync();
		try
		{
			if ((DateTime.Now - time).TotalMilliseconds > 600.0)
			{
				return;
			}
			if (Math.Abs(delta) > 40.0)
			{
				delta /= 3.0;
				for (int i = 0; i < 3; i++)
				{
					Do();
					await Task.Delay(1);
				}
			}
			else
			{
				Do();
			}
		}
		finally
		{
			_loadSemaphoreSlim.Release();
		}
		void Do()
		{
			bool flag = delta < 0.0;
			if (_items?.Count > StartIndex && StartIndex >= 0)
			{
				if (delta < 0.0)
				{
					CalendarAgendaItemViewModel calendarAgendaItemViewModel = _items[_items.Count - 1];
					if (calendarAgendaItemViewModel.Showing && calendarAgendaItemViewModel.Top != 0.0 && calendarAgendaItemViewModel.Top + calendarAgendaItemViewModel.Height + delta < base.ActualHeight)
					{
						delta = base.ActualHeight - calendarAgendaItemViewModel.Top - calendarAgendaItemViewModel.Height;
						if (delta > 0.0)
						{
							delta = 0.0;
						}
					}
				}
				else
				{
					CalendarAgendaItemViewModel calendarAgendaItemViewModel2 = _items[0];
					if (calendarAgendaItemViewModel2.Showing && calendarAgendaItemViewModel2.Top + delta > 400.0)
					{
						delta = 400.0 - calendarAgendaItemViewModel2.Top;
						if (delta < 0.0)
						{
							delta = 0.0;
						}
					}
				}
				CalendarAgendaItemViewModel calendarAgendaItemViewModel3 = _items[StartIndex];
				calendarAgendaItemViewModel3.Top = calendarAgendaItemViewModel3.GetTop() + delta;
				if (delta < 0.0)
				{
					while (calendarAgendaItemViewModel3.NextItem != null && calendarAgendaItemViewModel3.Top + calendarAgendaItemViewModel3.Height < 390.0)
					{
						CalendarAgendaItemViewModel calendarAgendaItemViewModel4 = calendarAgendaItemViewModel3.GetLastShowingNextItem()?.NextItem;
						if (calendarAgendaItemViewModel4 != null)
						{
							if (DisplayItemDict.Count < _itemsCount)
							{
								AddNewItem(calendarAgendaItemViewModel4, isEnd: true);
							}
							else
							{
								CalendarAgendaItemViewModel firstShowingFrontItem = calendarAgendaItemViewModel3.GetFirstShowingFrontItem();
								if (firstShowingFrontItem != null && DisplayItemDict.TryGetValue(firstShowingFrontItem, out var value))
								{
									value.DataContext = calendarAgendaItemViewModel4;
									DisplayItemDict.Remove(firstShowingFrontItem);
									firstShowingFrontItem.Showing = false;
									firstShowingFrontItem.SetTop(0.0);
									firstShowingFrontItem.Height = 60.0;
									DisplayItemDict[calendarAgendaItemViewModel4] = value;
									calendarAgendaItemViewModel4.Showing = true;
									calendarAgendaItemViewModel4.Top = calendarAgendaItemViewModel4.FrontItem.Top + calendarAgendaItemViewModel4.Height;
								}
							}
						}
						calendarAgendaItemViewModel3 = calendarAgendaItemViewModel3.NextItem;
					}
					StartIndex = calendarAgendaItemViewModel3.Index;
					if (!calendarAgendaItemViewModel3.IsLoadBefore)
					{
						_agendaView.SetCurrentDate(calendarAgendaItemViewModel3.Date);
						_dateTitle.SetDateText(calendarAgendaItemViewModel3.Date, IsRestDay(calendarAgendaItemViewModel3.Date), IsWorkDay(calendarAgendaItemViewModel3.Date));
						HideDateTitle(CheckHideDateTitle(calendarAgendaItemViewModel3));
					}
					else
					{
						HideDateTitle(hide: true);
					}
				}
				else
				{
					while (calendarAgendaItemViewModel3.FrontItem != null && calendarAgendaItemViewModel3.Top >= 390.0)
					{
						CalendarAgendaItemViewModel calendarAgendaItemViewModel5 = calendarAgendaItemViewModel3.GetFirstShowingFrontItem()?.FrontItem;
						if (calendarAgendaItemViewModel5 != null)
						{
							if (DisplayItemDict.Count < _itemsCount)
							{
								AddNewItem(calendarAgendaItemViewModel5, isEnd: false);
							}
							else if (calendarAgendaItemViewModel3.Index - calendarAgendaItemViewModel5.Index <= 5)
							{
								CalendarAgendaItemViewModel lastShowingNextItem = calendarAgendaItemViewModel3.GetLastShowingNextItem();
								if (lastShowingNextItem != null && DisplayItemDict.TryGetValue(lastShowingNextItem, out var value2))
								{
									value2.DataContext = calendarAgendaItemViewModel5;
									DisplayItemDict.Remove(lastShowingNextItem);
									lastShowingNextItem.Showing = false;
									lastShowingNextItem.SetTop(0.0);
									lastShowingNextItem.Height = 60.0;
									DisplayItemDict[calendarAgendaItemViewModel5] = value2;
									calendarAgendaItemViewModel5.Showing = true;
									calendarAgendaItemViewModel5.Top = calendarAgendaItemViewModel5.NextItem.Top - calendarAgendaItemViewModel5.Height;
								}
							}
						}
						calendarAgendaItemViewModel3 = calendarAgendaItemViewModel3.FrontItem;
					}
					StartIndex = calendarAgendaItemViewModel3.Index;
					if (!calendarAgendaItemViewModel3.IsLoadBefore)
					{
						_agendaView.SetCurrentDate(calendarAgendaItemViewModel3.Date);
						_dateTitle.SetDateText(calendarAgendaItemViewModel3.Date, IsRestDay(calendarAgendaItemViewModel3.Date), IsWorkDay(calendarAgendaItemViewModel3.Date));
						HideDateTitle(CheckHideDateTitle(calendarAgendaItemViewModel3));
						ShowNextDateTitle(calendarAgendaItemViewModel3);
					}
					else
					{
						HideDateTitle(hide: true);
					}
				}
			}
			SetCurrentIndexAndOffset();
			if (loadMore)
			{
				List<CalendarAgendaItemViewModel> items = _items;
				if (items != null && items.Count > 0)
				{
					loadMore = false;
					if (flag && _items[_items.Count - 1].Showing && !_items[_items.Count - 1].IsLoadAfter)
					{
						Utils.FindParent<CalendarAgendaView>(this)?.LoadMore(isEnd: true);
					}
					else if (!flag && _items[0].Showing && !_items[0].IsLoadBefore)
					{
						Utils.FindParent<CalendarAgendaView>(this)?.LoadMore(isEnd: false);
					}
				}
			}
		}
	}

	private void ShowNextDateTitle(CalendarAgendaItemViewModel startItem)
	{
		CalendarAgendaItemViewModel calendarAgendaItemViewModel = startItem.GetLastItemInSameDay();
		if (calendarAgendaItemViewModel != null)
		{
			while (!calendarAgendaItemViewModel.ShowTitleDate)
			{
				calendarAgendaItemViewModel = calendarAgendaItemViewModel.NextItem;
				if (calendarAgendaItemViewModel == null)
				{
					break;
				}
			}
		}
		if (calendarAgendaItemViewModel != null && DisplayItemDict.TryGetValue(calendarAgendaItemViewModel, out var value))
		{
			value.SetDateTitleOpacity(1);
		}
	}

	private bool CheckHideDateTitle(CalendarAgendaItemViewModel startItem)
	{
		if (base.ActualWidth < 640.0)
		{
			return true;
		}
		if (startItem.NextItem == null)
		{
			return true;
		}
		if (startItem.NextItem == null || startItem.NextItem.Date != startItem.Date)
		{
			if (!startItem.ShowTitleDate && DisplayItemDict.TryGetValue(startItem, out var value))
			{
				value.SetDateTitle(startItem.Date, IsRestDay(startItem.Date), IsWorkDay(startItem.Date));
			}
			return true;
		}
		CalendarAgendaItemViewModel firstItemInSameDay = startItem.GetFirstItemInSameDay();
		DisplayItemDict.TryGetValue(firstItemInSameDay, out var value2);
		if (startItem.IsEmpty && (startItem.Top >= 343.0 || (startItem.ShowYearText && startItem.Top >= 280.0)))
		{
			value2?.SetDateTitleOpacity(1);
			return true;
		}
		CalendarAgendaItemViewModel lastItemInSameDay = startItem.GetLastItemInSameDay();
		DisplayItemDict.TryGetValue(lastItemInSameDay, out var value3);
		if (lastItemInSameDay.Showing && lastItemInSameDay.Top < 400.0)
		{
			value3?.SetDateTitle(startItem.Date, IsRestDay(startItem.Date), IsWorkDay(startItem.Date));
			return true;
		}
		value2?.SetDateTitleOpacity(0);
		value3?.SetDateTitleOpacity(0);
		return false;
	}

	private void SetCurrentIndexAndOffset(double? top = null)
	{
		CalendarAgendaItemViewModel calendarAgendaItemViewModel = _items[StartIndex];
		if (calendarAgendaItemViewModel.IsEmpty && calendarAgendaItemViewModel.NextItem != null)
		{
			calendarAgendaItemViewModel = calendarAgendaItemViewModel.NextItem;
		}
		CurrentOffset = top ?? calendarAgendaItemViewModel.Top;
		CurrentDate = calendarAgendaItemViewModel.Date;
		int num = 0;
		while (calendarAgendaItemViewModel.FrontItem != null && calendarAgendaItemViewModel.FrontItem.Date == calendarAgendaItemViewModel.Date && !calendarAgendaItemViewModel.FrontItem.IsEmpty)
		{
			num++;
			calendarAgendaItemViewModel = calendarAgendaItemViewModel.FrontItem;
		}
		CurrentIndex = num;
	}

	public async void SetItems(List<CalendarAgendaItemViewModel> items, DateTime dateTime, double height, bool isReload = false)
	{
		if (items == null)
		{
			return;
		}
		await _loadSemaphoreSlim.WaitAsync();
		try
		{
			_loading = true;
			if (!isReload)
			{
				HideDateTitle(hide: true);
			}
			_items = items;
			_displayHeight = height;
			_itemsCount = (int)(height / 60.0) + 8;
			_dateTitle.SetDateText(dateTime, IsWorkDay(dateTime), IsRestDay(dateTime));
			CalendarAgendaItemViewModel calendarAgendaItemViewModel = items.FirstOrDefault((CalendarAgendaItemViewModel i) => i.Date == dateTime && !i.IsEmpty);
			double top = 400.0;
			if (isReload && CurrentDate == dateTime && calendarAgendaItemViewModel != null)
			{
				top = CurrentOffset;
				int num = CurrentIndex;
				int num2 = items.IndexOf(calendarAgendaItemViewModel);
				while (num > 0 && calendarAgendaItemViewModel.Date == dateTime)
				{
					num--;
					num2++;
					if (num2 < items.Count)
					{
						calendarAgendaItemViewModel = items[num2];
					}
				}
			}
			if (calendarAgendaItemViewModel == null)
			{
				calendarAgendaItemViewModel = items.FirstOrDefault((CalendarAgendaItemViewModel i) => i.Date > dateTime);
			}
			if (calendarAgendaItemViewModel != null)
			{
				StartIndex = items.IndexOf(calendarAgendaItemViewModel);
				for (int num3 = 0; num3 < items.Count; num3++)
				{
					CalendarAgendaItemViewModel calendarAgendaItemViewModel2 = items[num3];
					CalendarAgendaItemViewModel frontItem = ((num3 > 0) ? items[num3 - 1] : null);
					CalendarAgendaItemViewModel nextItem = ((num3 < items.Count - 1) ? items[num3 + 1] : null);
					calendarAgendaItemViewModel2.Index = num3;
					calendarAgendaItemViewModel2.Showing = false;
					calendarAgendaItemViewModel2.Parent = this;
					calendarAgendaItemViewModel2.FrontItem = frontItem;
					calendarAgendaItemViewModel2.NextItem = nextItem;
					calendarAgendaItemViewModel2.Selected = calendarAgendaItemViewModel2.Id == _selectedId && Math.Abs(calendarAgendaItemViewModel2.RepeatDiff - _selectedDiff) < 0.1;
				}
				List<CalendarAgendaItem> list = DisplayItemDict.Values.ToList();
				List<CalendarAgendaItem> list2 = new List<CalendarAgendaItem>();
				foreach (object child in base.Children)
				{
					list2.Add(child as CalendarAgendaItem);
				}
				DisplayItemDict.Clear();
				if (list.Count == 0)
				{
					CalendarAgendaItem calendarAgendaItem = new CalendarAgendaItem();
					calendarAgendaItem.SetBinding(FrameworkElement.WidthProperty, new Binding("ActualWidth")
					{
						Source = this
					});
					calendarAgendaItem.SetBinding(Canvas.TopProperty, "Top");
					list.Add(calendarAgendaItem);
					base.Children.Add(calendarAgendaItem);
				}
				int num4 = StartIndex + 1;
				int num5 = _itemsCount - 5;
				int num6 = Math.Min(num5, items.Count - num4);
				num5 -= num6;
				for (int num7 = 0; num7 < num6; num7++)
				{
					if (list.Count < num7 + 2)
					{
						CalendarAgendaItem calendarAgendaItem2 = new CalendarAgendaItem();
						calendarAgendaItem2.SetBinding(FrameworkElement.WidthProperty, new Binding("ActualWidth")
						{
							Source = this
						});
						calendarAgendaItem2.SetBinding(Canvas.TopProperty, "Top");
						list.Add(calendarAgendaItem2);
						base.Children.Add(calendarAgendaItem2);
					}
					CalendarAgendaItemViewModel calendarAgendaItemViewModel3 = items[num4 + num7];
					calendarAgendaItemViewModel3.Showing = true;
					list[num7 + 1].DataContext = calendarAgendaItemViewModel3;
					DisplayItemDict[calendarAgendaItemViewModel3] = list[num7 + 1];
				}
				int num8 = StartIndex - 1;
				int num9 = Math.Min(4 + num5, StartIndex);
				for (int num10 = 0; num10 < num9; num10++)
				{
					if (list.Count < num10 + num6 + 2)
					{
						CalendarAgendaItem calendarAgendaItem3 = new CalendarAgendaItem();
						calendarAgendaItem3.SetBinding(FrameworkElement.WidthProperty, new Binding("ActualWidth")
						{
							Source = this
						});
						calendarAgendaItem3.SetBinding(Canvas.TopProperty, "Top");
						list.Add(calendarAgendaItem3);
						base.Children.Add(calendarAgendaItem3);
					}
					CalendarAgendaItemViewModel calendarAgendaItemViewModel4 = items[num8 - num10];
					calendarAgendaItemViewModel4.Showing = true;
					list[num10 + num6 + 1].DataContext = calendarAgendaItemViewModel4;
					DisplayItemDict[calendarAgendaItemViewModel4] = list[num10 + num6 + 1];
				}
				UpdateLayout();
				calendarAgendaItemViewModel.SetTop(top);
				calendarAgendaItemViewModel.Showing = true;
				list[0].DataContext = calendarAgendaItemViewModel;
				DisplayItemDict[calendarAgendaItemViewModel] = list[0];
				foreach (CalendarAgendaItem item in DisplayItemDict.Values.ToList())
				{
					list2.Remove(item);
				}
				foreach (CalendarAgendaItem item2 in list2)
				{
					base.Children.Remove(item2);
				}
				UpdateLayout();
				calendarAgendaItemViewModel.RefreshHeight();
				SetCurrentIndexAndOffset();
			}
		}
		finally
		{
			_loading = false;
			_loadSemaphoreSlim.Release();
		}
		if (isReload)
		{
			Scroll(0.01);
		}
	}

	public async Task AddItems(List<CalendarAgendaItemViewModel> models, bool isEnd)
	{
		if (models == null || models.Count == 0)
		{
			return;
		}
		await _loadSemaphoreSlim.WaitAsync();
		while (_scrolling)
		{
			await Task.Delay(10);
		}
		try
		{
			_loading = true;
			CalendarAgendaItemViewModel calendarAgendaItemViewModel = _items[StartIndex];
			if (isEnd)
			{
				CalendarAgendaItemViewModel calendarAgendaItemViewModel2 = _items.LastOrDefault();
				if (calendarAgendaItemViewModel2 != null && calendarAgendaItemViewModel2.IsLoadAfter)
				{
					if (models[0].IsLoadAfter)
					{
						calendarAgendaItemViewModel2.Date = models[0].Date;
						calendarAgendaItemViewModel2.NotifyPropertyChanged("Date");
						return;
					}
					calendarAgendaItemViewModel2.IsLoadAfter = false;
					calendarAgendaItemViewModel2.NotifyPropertyChanged("IsLoadAfter");
				}
				List<CalendarAgendaItemViewModel> list = models.ToList();
				list[0].ShowYearText = true;
				list[0].NotifyPropertyChanged("ShowYearText");
				_items.AddRange(list);
			}
			else
			{
				CalendarAgendaItemViewModel calendarAgendaItemViewModel3 = _items.FirstOrDefault();
				if (calendarAgendaItemViewModel3 != null && calendarAgendaItemViewModel3.IsLoadBefore)
				{
					if (models[models.Count - 1].IsLoadBefore)
					{
						calendarAgendaItemViewModel3.Date = models[models.Count - 1].Date;
						calendarAgendaItemViewModel3.NotifyPropertyChanged("Date");
						return;
					}
					calendarAgendaItemViewModel3.IsLoadBefore = false;
					calendarAgendaItemViewModel3.ShowYearText = false;
					calendarAgendaItemViewModel3.NotifyPropertyChanged("IsLoadBefore");
					calendarAgendaItemViewModel3.NotifyPropertyChanged("ShowYearText");
				}
				models[0].ShowYearText = true;
				models[0].NotifyPropertyChanged("ShowYearText");
				List<CalendarAgendaItemViewModel> list2 = models.ToList();
				list2.AddRange(_items);
				_items = list2;
			}
			for (int i = 0; i < _items.Count; i++)
			{
				CalendarAgendaItemViewModel calendarAgendaItemViewModel4 = _items[i];
				CalendarAgendaItemViewModel frontItem = ((i > 0) ? _items[i - 1] : null);
				CalendarAgendaItemViewModel nextItem = ((i < _items.Count - 1) ? _items[i + 1] : null);
				calendarAgendaItemViewModel4.Index = i;
				calendarAgendaItemViewModel4.Parent = this;
				calendarAgendaItemViewModel4.FrontItem = frontItem;
				calendarAgendaItemViewModel4.NextItem = nextItem;
			}
			StartIndex = _items.IndexOf(calendarAgendaItemViewModel);
			int num = Math.Min(_itemsCount - DisplayItemDict.Count, models.Count);
			if (num > 0)
			{
				for (int j = 0; j < num; j++)
				{
					AddNewItem(models[isEnd ? j : (models.Count - 1 - j)], isEnd);
				}
			}
			for (int k = 0; k < 4; k++)
			{
				CalendarAgendaItemViewModel firstShowingFrontItem = calendarAgendaItemViewModel.GetFirstShowingFrontItem();
				CalendarAgendaItemViewModel lastShowingNextItem = calendarAgendaItemViewModel.GetLastShowingNextItem();
				CalendarAgendaItemViewModel calendarAgendaItemViewModel5 = (isEnd ? firstShowingFrontItem : lastShowingNextItem);
				CalendarAgendaItemViewModel calendarAgendaItemViewModel6 = ((!isEnd) ? (firstShowingFrontItem?.FrontItem ?? calendarAgendaItemViewModel.FrontItem) : lastShowingNextItem?.NextItem);
				if (calendarAgendaItemViewModel5 != null && calendarAgendaItemViewModel6 != null && (calendarAgendaItemViewModel5.Top < 340.0 || calendarAgendaItemViewModel5.Top - 400.0 > _displayHeight) && DisplayItemDict.TryGetValue(calendarAgendaItemViewModel5, out var value))
				{
					value.DataContext = calendarAgendaItemViewModel6;
					DisplayItemDict.Remove(calendarAgendaItemViewModel5);
					calendarAgendaItemViewModel5.Showing = false;
					calendarAgendaItemViewModel5.SetTop(0.0);
					calendarAgendaItemViewModel5.Height = 60.0;
					DisplayItemDict[calendarAgendaItemViewModel6] = value;
					calendarAgendaItemViewModel6.Showing = true;
					calendarAgendaItemViewModel6.Top = (isEnd ? ((calendarAgendaItemViewModel6.FrontItem?.Top ?? 0.0) + calendarAgendaItemViewModel6.Height) : ((calendarAgendaItemViewModel6.NextItem?.Top ?? 0.0) - calendarAgendaItemViewModel6.Height));
				}
			}
			if (StartIndex == 0)
			{
				calendarAgendaItemViewModel.Top = 400.0;
			}
			calendarAgendaItemViewModel.RefreshHeight();
		}
		finally
		{
			_loading = false;
			_loadSemaphoreSlim.Release();
		}
		Scroll(isEnd ? (-20) : 20, loadMore: false);
	}

	private void AddNewItem(CalendarAgendaItemViewModel model, bool isEnd)
	{
		CalendarAgendaItem calendarAgendaItem = new CalendarAgendaItem();
		calendarAgendaItem.SetBinding(FrameworkElement.WidthProperty, new Binding("ActualWidth")
		{
			Source = this
		});
		calendarAgendaItem.SetBinding(Canvas.TopProperty, "Top");
		DisplayItemDict[model] = calendarAgendaItem;
		calendarAgendaItem.DataContext = model;
		base.Children.Add(calendarAgendaItem);
		model.Showing = true;
		model.Top = (isEnd ? ((model.FrontItem?.Top ?? 0.0) + model.Height) : ((model.NextItem?.Top ?? 0.0) - model.Height));
	}

	public void RemoveItem(CalendarAgendaItemViewModel model)
	{
		if (DisplayItemDict.TryGetValue(model, out var _))
		{
			_agendaView.Reload();
		}
	}

	public List<CalendarAgendaItemViewModel> GetTasksByIds(HashSet<string> taskIds)
	{
		return (from t in _items?.ToList()
			where taskIds.Contains(t.Id)
			select t).ToList();
	}

	public void DelayReloadItems(double newHeight)
	{
		DelayActionHandlerCenter.TryDoAction("AgendaReloadItems", delegate
		{
			base.Dispatcher.Invoke(delegate
			{
				if (_items.Count > 0)
				{
					CalendarAgendaView agendaView = _agendaView;
					if (agendaView != null && agendaView.CurrentDate.HasValue)
					{
						SetItems(_items, _agendaView.CurrentDate.Value, newHeight, isReload: true);
					}
				}
			});
		}, 200);
	}

	public void SetEditingTaskId(string taskId, double diff)
	{
		_selectedId = taskId;
		_selectedDiff = diff;
		if (string.IsNullOrEmpty(taskId))
		{
			_items.ForEach(delegate(CalendarAgendaItemViewModel item)
			{
				item.Selected = false;
			});
		}
	}

	public void ClearEditingTaskId(string taskId)
	{
		if (!(_selectedId == taskId))
		{
			return;
		}
		_selectedId = "";
		_selectedDiff = 0.0;
		_items.ForEach(delegate(CalendarAgendaItemViewModel item)
		{
			if (taskId == item.Id)
			{
				item.Selected = false;
			}
		});
	}

	public CalendarAgendaItemViewModel GetShowingTask(string taskId)
	{
		return _items?.FirstOrDefault((CalendarAgendaItemViewModel t) => t.Top >= 384.0 && t.Top < 384.0 + _displayHeight - 40.0 && t.Id == taskId);
	}

	public void MoveToTask(string taskId)
	{
		List<CalendarAgendaItemViewModel> list = _items.ToList();
		CalendarAgendaItemViewModel calendarAgendaItemViewModel = list.FirstOrDefault((CalendarAgendaItemViewModel t) => t.Id == taskId);
		if (calendarAgendaItemViewModel != null)
		{
			int startIndex = list.IndexOf(calendarAgendaItemViewModel);
			StartIndex = startIndex;
			SetCurrentIndexAndOffset(400.0);
			SetItems(list, calendarAgendaItemViewModel.Date, _displayHeight, isReload: true);
			_agendaView.SetCurrentDate(calendarAgendaItemViewModel.Date);
			_dateTitle.SetDateText(calendarAgendaItemViewModel.Date, IsWorkDay(calendarAgendaItemViewModel.Date), IsRestDay(calendarAgendaItemViewModel.Date));
		}
	}

	private bool IsWorkDay(DateTime date)
	{
		HolidayModel holidayModel = ((LocalSettings.Settings.EnableHoliday && Utils.IsDida()) ? HolidayManager.GetHoliday(date) : null);
		if (holidayModel != null)
		{
			return holidayModel.type == 1;
		}
		return false;
	}

	public bool IsRestDay(DateTime date)
	{
		HolidayModel holidayModel = ((LocalSettings.Settings.EnableHoliday && Utils.IsDida()) ? HolidayManager.GetHoliday(date) : null);
		if (holidayModel != null)
		{
			return holidayModel.type == 0;
		}
		return false;
	}

	public void SetDateTitleVisible(bool hide)
	{
		_dateTitle.Visibility = (hide ? Visibility.Collapsed : Visibility.Visible);
	}

	private void HideDateTitle(bool hide)
	{
		_dateTitle.Opacity = ((!hide) ? 1 : 0);
	}
}
