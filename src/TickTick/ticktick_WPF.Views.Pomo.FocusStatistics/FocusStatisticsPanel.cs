using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.SyncServices;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Pomo.FocusStatistics;

public sealed class FocusStatisticsPanel : Grid
{
	private ListView _listView;

	private FocusStatisticsTitleView _floatTitle;

	private FocusRecordEmptyItem _emptyItem;

	private int _limits;

	private List<FocusStatisticsPanelItemViewModel> _itemsSource;

	private bool _loading;

	private int limitDelta = 30;

	public FocusStatisticsPanel()
	{
		_listView = new ListView();
		base.Children.Add(_listView);
		_listView.ItemTemplateSelector = new FocusStatisticsPanelItemTemplateSelector();
		_listView.SetResourceReference(FrameworkElement.StyleProperty, "ListViewStyle");
		_limits = limitDelta;
		PullRemote(firstPull: true);
		PullStatistics();
		_emptyItem = new FocusRecordEmptyItem
		{
			Margin = new Thickness(0.0, 200.0, 0.0, 0.0),
			VerticalAlignment = VerticalAlignment.Center,
			Visibility = Visibility.Collapsed
		};
		base.Children.Add(_emptyItem);
		LoadItems();
		base.Loaded += OnLoaded;
		base.Loaded += BindEvent;
		base.Unloaded += RemoveEvent;
		_floatTitle = new FocusStatisticsTitleView(showBottomLine: true)
		{
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(0.0, 6.0, 0.0, 0.0),
			DataContext = new FocusStatisticsTitleItemViewModel(Utils.GetString("FocusRecord"), showButton: true)
		};
	}

	private void RemoveEvent(object sender, RoutedEventArgs e)
	{
		PomoNotifier.Changed -= OnPomoChanged;
		PomoNotifier.ServiceChanged -= OnPomoChanged;
	}

	private void BindEvent(object sender, RoutedEventArgs e)
	{
		PomoNotifier.Changed += OnPomoChanged;
		PomoNotifier.ServiceChanged += OnPomoChanged;
	}

	private async Task PullStatistics()
	{
		await PomoSyncService.PullStatistics();
		await base.Dispatcher.InvokeAsync(delegate
		{
			if (_listView.ItemsSource is ObservableCollection<FocusStatisticsPanelItemViewModel> { Count: >1 } observableCollection)
			{
				observableCollection[1] = new FocusStatisticsOverviewItem(LocalSettings.Settings.StatisticsModel);
			}
		});
	}

	private void LoadItems()
	{
		base.Dispatcher.Invoke((Func<Task>)TryLoadItems);
	}

	private async Task TryLoadItems()
	{
		List<FocusStatisticsPanelItemViewModel> itemsSource = new List<FocusStatisticsPanelItemViewModel>
		{
			new FocusStatisticsTitleItemViewModel(Utils.GetString("Overview"), showButton: false),
			new FocusStatisticsOverviewItem(LocalSettings.Settings.StatisticsModel),
			new FocusStatisticsTitleItemViewModel(Utils.GetString("FocusRecord"), showButton: true)
		};
		List<PomodoroModel> pomos = await PomoDao.GetPomoDescByStart(_limits);
		bool flag = pomos?.Any() ?? false;
		if (itemsSource.FirstOrDefault((FocusStatisticsPanelItemViewModel x) => x is FocusStatisticsTitleItemViewModel focusStatisticsTitleItemViewModel3 && focusStatisticsTitleItemViewModel3.ShowButton) is FocusStatisticsTitleItemViewModel focusStatisticsTitleItemViewModel)
		{
			focusStatisticsTitleItemViewModel.ShowMoreButton = flag;
		}
		if (_floatTitle?.DataContext is FocusStatisticsTitleItemViewModel focusStatisticsTitleItemViewModel2)
		{
			focusStatisticsTitleItemViewModel2.ShowButton = flag;
		}
		Dictionary<DateTime, List<FocusRecordItemViewModel>> datePomoDict = new Dictionary<DateTime, List<FocusRecordItemViewModel>>();
		foreach (PomodoroModel item2 in pomos)
		{
			PomodoroModel pomodoroModel = await PomoService.GetAdjustPomo(item2);
			PomoTask[] tasks = pomodoroModel.Tasks;
			foreach (DateTime focusRecordDate in GetFocusRecordDates(pomodoroModel))
			{
				FocusRecordItemViewModel item = new FocusRecordItemViewModel(pomodoroModel, focusRecordDate, tasks);
				if (!datePomoDict.ContainsKey(focusRecordDate))
				{
					datePomoDict[focusRecordDate] = new List<FocusRecordItemViewModel>();
				}
				datePomoDict[focusRecordDate].Add(item);
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
				NeedClick = (_limits == limitDelta)
			});
		}
		_emptyItem.Visibility = ((pomos.Count != 0) ? Visibility.Collapsed : Visibility.Visible);
		ItemsSourceHelper.SetItemsSource(_listView, itemsSource);
		_itemsSource = itemsSource;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		base.Loaded -= OnLoaded;
		if (_listView.Template.FindName("ScrollViewer", _listView) is ScrollViewer scrollViewer)
		{
			scrollViewer.ScrollChanged += OnScrollChanged;
		}
	}

	private void OnPomoChanged(object sender, PomoChangeArgs e)
	{
		DelayActionHandlerCenter.TryDoAction("FocusStatisticsPanelLoadItems", delegate
		{
			LoadItems();
		}, 500);
	}

	private void OnPomoChanged(object sender, EventArgs e)
	{
		DelayActionHandlerCenter.TryDoAction("FocusStatisticsPanelPullStatisticsAndLoadItems", async delegate
		{
			try
			{
				await PullStatistics();
			}
			catch (Exception)
			{
			}
			LoadItems();
		}, 500);
	}

	private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
	{
		FocusRecordItem mousePointItem = Utils.GetMousePointItem<FocusRecordItem>(new Point(50.0, (_listView.Clip != null) ? 50 : 10), _listView);
		_listView.Clip = ((mousePointItem != null) ? new RectangleGeometry(new Rect(0.0, 40.0, base.ActualWidth, base.ActualHeight - 40.0)) : null);
		base.Children.Remove(_floatTitle);
		if (mousePointItem != null)
		{
			base.Children.Add(_floatTitle);
		}
		if (sender is ScrollViewer scrollViewer && _itemsSource?.LastOrDefault() is FocusRecordLoadMoreViewModel { NeedClick: false } && e.VerticalOffset >= scrollViewer.ScrollableHeight - 10.0)
		{
			LoadMore();
		}
	}

	public async Task LoadMore()
	{
		if (!_loading)
		{
			_loading = true;
			_limits += limitDelta;
			LoadItems();
			await PullRemote();
			_loading = false;
		}
	}

	private async Task PullRemote(bool firstPull = false)
	{
		if (await PomoSyncService.PullFocusTimelines(firstPull))
		{
			await PullStatistics();
			LoadItems();
		}
	}

	public void Reload()
	{
		LoadItems();
	}

	private static IEnumerable<DateTime> GetFocusRecordDates(PomodoroModel pomo)
	{
		if (pomo == null)
		{
			yield break;
		}
		bool hasVisibleDate = false;
		DateTime date = pomo.EndTime.Date;
		while (date >= pomo.StartTime.Date)
		{
			if (PomoService.GetFocusDurationInRange(pomo, date, date.AddDays(1.0)) > 0)
			{
				hasVisibleDate = true;
				yield return date;
			}
			if (date == DateTime.MinValue.Date)
			{
				yield break;
			}
			date = date.AddDays(-1.0);
		}
		if (!hasVisibleDate)
		{
			yield return pomo.EndTime.Date;
		}
	}
}
