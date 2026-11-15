using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using TickTickDao;
using TickTickModels;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Framework.Collections;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Service;
using ticktick_WPF.SyncServices;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Pomo.FocusStatistics;

namespace ticktick_WPF.Views.Pomo.TimerDetail;

public class TimerDetailPanel : Grid
{
	private ListView _listView;

	private int _limits;

	private TimerModel _timer;

	private readonly TTAsyncTaskLocker<TimerModel> _loadAsync = new TTAsyncTaskLocker<TimerModel>(1, 1);

	private bool _loading;

	private TimerTimelineItemViewModel _timelineViewModel;

	private CancellationTokenSource _historyCancellation;

	private ObservableCollection<FocusStatisticsPanelItemViewModel> ItemsSource => (_listView.ItemsSource as ObservableCollection<FocusStatisticsPanelItemViewModel>) ?? new ExtObservableCollection<FocusStatisticsPanelItemViewModel>();

	public TimerDetailPanel()
	{
		InitTopOption();
		_listView = new ListView
		{
			Margin = new Thickness(0.0, 36.0, 0.0, 0.0)
		};
		base.Children.Add(_listView);
		_listView.ItemTemplateSelector = new TimerDetailItemTemplateSelector();
		_listView.SetResourceReference(FrameworkElement.StyleProperty, "ListViewStyle");
		_timelineViewModel = new TimerTimelineItemViewModel(this);
		base.Loaded += OnLoaded;
		base.Loaded += delegate
		{
			PomoNotifier.ServiceChanged += OnPomoChanged;
			PomoNotifier.Changed += OnPomoChanged;
			PomoNotifier.LinkChanged += OnPomoLinkChanged;
			PomoNotifier.PomoCommit += OnPomoCommit;
			TimeChangeNotifier.DayChanged += OnDayChanged;
		};
		base.Unloaded += delegate
		{
			_historyCancellation?.Cancel();
			PomoNotifier.ServiceChanged -= OnPomoChanged;
			PomoNotifier.LinkChanged -= OnPomoLinkChanged;
			PomoNotifier.Changed -= OnPomoChanged;
			PomoNotifier.PomoCommit -= OnPomoCommit;
			TimeChangeNotifier.DayChanged -= OnDayChanged;
		};
	}

	private void InitTopOption()
	{
		TextBlock textBlock = new TextBlock
		{
			Text = Utils.GetString("Close"),
			FontSize = 14.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Background = Brushes.Transparent,
			Cursor = Cursors.Hand,
			Margin = new Thickness(22.0, 4.0, 0.0, 0.0)
		};
		textBlock.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryColor");
		textBlock.MouseLeftButtonUp += OnCloseClick;
		base.Children.Add(textBlock);
		HoverIconButton hoverIconButton = new HoverIconButton
		{
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Top,
			Height = 22.0,
			Width = 22.0,
			Margin = new Thickness(0.0, 4.0, 22.0, 0.0)
		};
		hoverIconButton.SetResourceReference(HoverIconButton.IconDataProperty, "Ic.ProjectList.More");
		hoverIconButton.IsImage = false;
		hoverIconButton.MouseLeftButtonUp += OnMoreClick;
		base.Children.Add(hoverIconButton);
	}

	private void OnMoreClick(object sender, MouseButtonEventArgs e)
	{
		Mouse.Capture(null);
		EscPopup popup = new EscPopup
		{
			PlacementTarget = (sender as UIElement),
			StaysOpen = false,
			Placement = PlacementMode.Left,
			VerticalOffset = 20.0,
			HorizontalOffset = 28.0
		};
		bool flag = _timer.Status != 0;
		List<CustomMenuItemViewModel> list = new List<CustomMenuItemViewModel>
		{
			new CustomMenuItemViewModel("Archive", Utils.GetString(flag ? "Restore" : "Archive"), Utils.GetGeometry(flag ? "Ic.Habit.Unarchive" : "Ic.Habit.Archive")),
			new CustomMenuItemViewModel("Delete", Utils.GetString("Delete"), Utils.GetGeometry("Ic.ProjectList.Delete"))
			{
				IsDelete = true
			}
		};
		if (!flag)
		{
			list.Insert(0, new CustomMenuItemViewModel("Edit", Utils.GetString("Edit"), Utils.GetGeometry("Ic.TaskOM.Edit")));
			list.Insert(1, new CustomMenuItemViewModel("Add", Utils.GetString("AddRecord"), Utils.GetGeometry("Ic.ProjectList.Add"))
			{
				ImageMargin = new Thickness(11.0, 0.0, 2.0, 0.0)
			});
		}
		CustomMenuList customMenuList = new CustomMenuList(list, popup);
		customMenuList.Operated += OnActionSelected;
		customMenuList.Show();
	}

	private async void OnActionSelected(object sender, object e)
	{
		switch (e.ToString())
		{
		case "Edit":
		{
			TimerModel timerModel = await TimerDao.GetTimerById(_timer.Id);
			if (timerModel != null)
			{
				AddTimerWindow addTimerWindow = new AddTimerWindow(timerModel);
				addTimerWindow.Owner = Window.GetWindow(this);
				addTimerWindow.ShowDialog();
			}
			break;
		}
		case "Add":
		{
			TimerModel timer = await TimerDao.GetTimerById(_timer.Id);
			if (timer != null)
			{
				string text = await TimerService.GetObjTitle(timer.ObjId, timer.ObjType);
				if (text != null && timer.Name != text)
				{
					timer.Name = text;
					await TimerService.UpdateName(timer.Id, text);
				}
				new AddFocusRecordWindow(timer).ShowDialog();
				UserActCollectUtils.AddClickEvent("timer", "timer_detail", "add_record");
			}
			break;
		}
		case "Archive":
			if (_timer.Status != 0)
			{
				if (!(await ProChecker.CheckTimerLimit(Window.GetWindow(this))))
				{
					break;
				}
			}
			else if (new CustomerDialog("", Utils.GetString("ArchiveTimerMessage"), Utils.GetString("Archive"), Utils.GetString("Cancel"), Window.GetWindow(this)).ShowDialog() != true)
			{
				break;
			}
			TimerService.ChangeArchiveStatus(_timer.Id);
			break;
		case "Delete":
			if (new CustomerDialog("", Utils.GetString("DeleteTimerMessage"), Utils.GetString("Delete"), Utils.GetString("Cancel"), Window.GetWindow(this)).ShowDialog() == true)
			{
				TimerService.DeleteTimer(_timer.Id);
			}
			break;
		}
	}

	private void OnCloseClick(object sender, MouseButtonEventArgs e)
	{
		Utils.FindParent<FocusView>(this)?.ClearTimerSelect();
	}

	public async Task LoadTimer(TimerModel timer)
	{
		await TryLoadItems(timer);
		PullRemote(timer.Id);
	}

	private async Task TryLoadItems(TimerModel timer)
	{
		await base.Dispatcher.Invoke(() => LoadItems(timer));
	}

	private async Task LoadItems(TimerModel timer)
	{
		await _loadAsync.RunAsync(SetItems, timer);
	}

	private async Task SetItems(TimerModel timer)
	{
		if (_timer == null && timer == null)
		{
			return;
		}
		if (timer != null)
		{
			bool first = timer.Id != _timer?.Id;
			_timer = timer;
			_limits = 30;
			_timelineViewModel = new TimerTimelineItemViewModel(this);
			_timelineViewModel.SetTimer(timer);
			PullTimeline(_timelineViewModel, getFront: true, first);
		}
		else
		{
			_timer = await TimerDao.GetTimerById(_timer.Id);
			_timelineViewModel.ReloadData();
		}
		TimerOverviewModel data = await TimerService.GetTimerOverView(_timer);
		List<FocusStatisticsPanelItemViewModel> itemsSource = new List<FocusStatisticsPanelItemViewModel>
		{
			new TimerDetailTitleViewModel(_timer),
			new TimerStatisticsOverviewItem(data),
			_timelineViewModel
		};
		List<PomodoroModel> pomos = await PomoDao.GetPomoDescByStartAndTimerId(_limits, _timer.Id, _timer.ObjId, _timer.ObjType == "habit", _timelineViewModel.StartDate, _timelineViewModel.EndDate.AddDays(1.0));
		Dictionary<DateTime, List<FocusRecordItemViewModel>> datePomoDict = new Dictionary<DateTime, List<FocusRecordItemViewModel>>();
		foreach (PomodoroModel item2 in pomos)
		{
			PomodoroModel pomodoroModel = await PomoService.GetAdjustPomo(item2);
			PomoTask[] tasks = pomodoroModel.Tasks;
			DateTime date = pomodoroModel.EndTime.Date;
			FocusRecordItemViewModel item = new FocusRecordItemViewModel(pomodoroModel, date, tasks, showTitle: false);
			if (datePomoDict.ContainsKey(date))
			{
				datePomoDict[date].Add(item);
			}
			else
			{
				datePomoDict[date] = new List<FocusRecordItemViewModel> { item };
			}
			if (pomodoroModel.StartTime.Date != pomodoroModel.EndTime.Date)
			{
				date = pomodoroModel.StartTime.Date;
				item = new FocusRecordItemViewModel(pomodoroModel, date, tasks, showTitle: false);
				if (datePomoDict.ContainsKey(date))
				{
					datePomoDict[date].Add(item);
					continue;
				}
				datePomoDict[date] = new List<FocusRecordItemViewModel> { item };
			}
		}
		foreach (KeyValuePair<DateTime, List<FocusRecordItemViewModel>> item3 in datePomoDict)
		{
			itemsSource.Add(new FocusRecordItemViewModel(item3.Key));
			List<FocusRecordItemViewModel> value = item3.Value;
			if (value != null && value.Count > 0)
			{
				item3.Value.Last().IsLastItem = true;
				itemsSource.AddRange(item3.Value);
			}
		}
		if (pomos.Count == _limits)
		{
			itemsSource.Add(new FocusRecordLoadMoreViewModel
			{
				NeedClick = (_limits == 30)
			});
		}
		ItemsSourceHelper.SetItemsSource(_listView, itemsSource);
	}

	private async Task PullRemote(string id)
	{
		TimerOverviewModel overView = await TimerSyncService.PullTimerOverview(id);
		if (overView != null && _timer.Id == id && base.IsVisible)
		{
			_timer = await TimerDao.GetTimerById(id);
			overView = await TimerService.GetTimerOverView(_timer);
			base.Dispatcher.Invoke(delegate
			{
				ItemsSource[1] = new TimerStatisticsOverviewItem(overView);
				Utils.FindParent<FocusView>(this)?.UpdateTimerDuration(id, overView.Today * 60);
			});
		}
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		base.Loaded -= OnLoaded;
		if (_listView.Template.FindName("ScrollViewer", _listView) is ScrollViewer scrollViewer)
		{
			scrollViewer.ScrollChanged += OnScrollChanged;
		}
	}

	private void OnPomoLinkChanged(object sender, PomoLinkArgs e)
	{
		if (_timer != null && (e.NewTimerId == _timer.Id || e.PreviewTimerId == _timer.Id))
		{
			TryLoadItems(null);
		}
	}

	private void OnPomoChanged(object sender, PomoChangeArgs e)
	{
		TryLoadItems(null);
	}

	private void OnPomoCommit(object sender, EventArgs e)
	{
		if (_timer != null)
		{
			PullRemote(_timer.Id);
		}
		base.Dispatcher.Invoke(delegate
		{
			PullTimeline(_timelineViewModel, getFront: false);
		});
	}

	private async void OnDayChanged(object sender, EventArgs e)
	{
		if (_timer != null)
		{
			TryLoadItems(await TimerDao.GetTimerById(_timer.Id));
		}
	}

	private void OnPomoChanged(object sender, EventArgs e)
	{
		if (ItemsSource.Count > 3 && ItemsSource[2] is TimerTimelineItemViewModel timerTimelineItemViewModel)
		{
			timerTimelineItemViewModel.ReloadData();
		}
	}

	private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
	{
		if (sender is ScrollViewer scrollViewer && ItemsSource?.LastOrDefault() is FocusRecordLoadMoreViewModel { NeedClick: false } && e.VerticalOffset >= scrollViewer.ScrollableHeight - 10.0)
		{
			LoadMore();
		}
	}

	private async void LoadMore()
	{
		if (_loading)
		{
			return;
		}
		_loading = true;
		try
		{
			_limits += 30;
			await TryLoadItems(null);
		}
		finally
		{
			_loading = false;
		}
	}

	private async Task PullRemoteFocus(DateTime start, DateTime end, string id)
	{
		_historyCancellation?.Cancel();
		CancellationTokenSource cancellation = (_historyCancellation = new CancellationTokenSource());
		try
		{
			bool flag = await PomoSyncService.PullTimerHistory(start, end, cancellation.Token);
			if (!cancellation.IsCancellationRequested && flag && id == _timelineViewModel.TModel.Id && start == _timelineViewModel.StartDate && end == _timelineViewModel.EndDate.AddDays(1.0))
			{
				await TryLoadItems(null);
			}
		}
		catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
		{
		}
		catch (Exception arg)
		{
			UtilLog.Error($"TimerDetail.PullRemoteFocus failed: {arg}");
		}
		finally
		{
			if (_historyCancellation == cancellation)
			{
				_historyCancellation = null;
			}
			cancellation.Dispose();
		}
	}

	public void PullTimeline(TimerTimelineItemViewModel viewModel, bool getFront = true, bool first = false)
	{
		if (first)
		{
			TimerSyncService.ClearPulledKey();
		}
		string interval = viewModel.Interval;
		DateTime startDate = viewModel.StartDate;
		DateTime endDate = viewModel.EndDate;
		PullTimerStatistics(viewModel.TModel.Id, startDate, endDate, delay: false, interval);
		PullRemoteFocus(startDate, endDate.AddDays(1.0), viewModel.TModel.Id);
		if (getFront)
		{
			DateTime start = startDate;
			DateTime end = endDate;
			switch (viewModel.Interval)
			{
			case "month":
				start = startDate.AddMonths(-1);
				end = start.AddMonths(1).AddDays(-1.0);
				break;
			case "year":
				start = startDate.AddYears(-1);
				end = start.AddYears(1).AddDays(-1.0);
				break;
			case "week":
				start = startDate.AddDays(-7.0);
				end = start.AddDays(6.0);
				break;
			}
			PullTimerStatistics(viewModel.TModel.Id, start, end, delay: true, interval);
		}
	}

	private async Task PullTimerStatistics(string id, DateTime start, DateTime end, bool delay, string interval)
	{
		if (delay)
		{
			await Task.Delay(1000);
		}
		if (!(interval != _timelineViewModel.Interval))
		{
			bool flag = _timelineViewModel.TModel.Id == id;
			if (flag)
			{
				flag = await TimerSyncService.TryPullTimerStatistics(id, start, end, interval);
			}
			if (flag && start == _timelineViewModel.StartDate)
			{
				_timelineViewModel.ReloadData();
			}
		}
	}

	public void ReloadRecords()
	{
		_limits = 30;
		TryLoadItems(null);
	}
}
