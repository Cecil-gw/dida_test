using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TickTickModels;
using TickTickUtils.Lang;
using ticktick_WPF.Notifier;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Pomo.MainFocus;

namespace ticktick_WPF.Views.Pomo;

public class FocusTimerListView : SortableListView<FocusTimerViewModel>
{
	private string _selectedId;

	private Border _emptyPanel;

	private bool _archive;

	public FocusTimerListView()
	{
		base.Loaded += Init;
		base.Loaded += OnLoaded;
		base.Unloaded += OnUnloaded;
	}

	private void Init(object sender, RoutedEventArgs e)
	{
		base.Loaded -= Init;
		ContentControl contentControl = new ContentControl();
		contentControl.SetResourceReference(FrameworkElement.StyleProperty, "PopupContentStyle");
		contentControl.Margin = new Thickness(0.0);
		contentControl.Content = new TimerListItem
		{
			InPopup = true
		};
		PopupContent = contentControl;
		PopupContentHeight = 54.0;
		ItemList.SetResourceReference(ItemsControl.ItemTemplateProperty, "TimerItemTemplate");
		base.SizeChanged += delegate
		{
			if (_emptyPanel != null)
			{
				_emptyPanel.Margin = new Thickness(0.0, base.ActualHeight * 0.28, 0.0, 0.0);
			}
		};
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		TasksChangeEventManager.AddHandler(TaskChangeNotifier.Notifier, OnTasksChanged);
		TimeChangeNotifier.DayChanged += OnDayChanged;
	}

	private void OnDayChanged(object sender, EventArgs e)
	{
		if (!_archive && ItemList.ItemsSource is ObservableCollection<FocusTimerViewModel> source)
		{
			ItemsSourceHelper.SetItemsSource(ItemList, source.ToList());
		}
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		TasksChangeEventManager.RemoveHandler(TaskChangeNotifier.Notifier, OnTasksChanged);
		TimeChangeNotifier.DayChanged -= OnDayChanged;
	}

	private void OnTasksChanged(object sender, TasksChangeEventArgs e)
	{
		if (!e.TaskTextChangedIds.Any() || !(ItemList.ItemsSource is ObservableCollection<FocusTimerViewModel> observableCollection))
		{
			return;
		}
		foreach (FocusTimerViewModel item in observableCollection)
		{
			if (e.TaskTextChangedIds.Contains(item.ObjId))
			{
				item.CheckTimerName();
			}
		}
	}

	public void SetItems(List<TimerModel> items, bool archive, string selectedId)
	{
		_archive = archive;
		if (items == null || items.Count == 0)
		{
			ItemsSourceHelper.SetItemsSource(ItemList, new List<FocusTimerViewModel>());
			ShowEmptyImage();
			return;
		}
		if (_emptyPanel != null)
		{
			_emptyPanel.Visibility = Visibility.Collapsed;
		}
		List<FocusTimerViewModel> list = items.Select((TimerModel m) => new FocusTimerViewModel(m)
		{
			Selected = (m.Id == selectedId)
		}).ToList();
		if (!archive)
		{
			CopyCurrentDurationText(list);
			SetDefaultDurationText(list);
		}
		ItemsSourceHelper.SetItemsSource(ItemList, list);
		if (!archive)
		{
			SetTimerTodayDuration(list);
		}
	}

	private static async Task SetTimerTodayDuration(List<FocusTimerViewModel> models)
	{
		try
		{
			await TimerService.SetTimerTodayDuration(models);
		}
		catch (Exception ex)
		{
			UtilLog.Error(ex);
		}
	}

	private void CopyCurrentDurationText(List<FocusTimerViewModel> models)
	{
		if (!(ItemList.ItemsSource is ObservableCollection<FocusTimerViewModel> observableCollection))
		{
			return;
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		foreach (FocusTimerViewModel item in observableCollection)
		{
			if (!string.IsNullOrEmpty(item.Id) && !string.IsNullOrEmpty(item.DurationText) && !dictionary.ContainsKey(item.Id))
			{
				dictionary[item.Id] = item.DurationText;
			}
		}
		foreach (FocusTimerViewModel model in models)
		{
			if (!string.IsNullOrEmpty(model.Id) && dictionary.TryGetValue(model.Id, out var value))
			{
				model.DurationText = value;
			}
		}
	}

	private static void SetDefaultDurationText(List<FocusTimerViewModel> models)
	{
		foreach (FocusTimerViewModel model in models)
		{
			if (string.IsNullOrEmpty(model.DurationText))
			{
				model.DurationText = Utils.GetShortDurationString(0L, showZeroMinute: false);
			}
		}
	}

	private async void ShowEmptyImage()
	{
		bool num = base.ActualHeight < 1.0;
		if (_emptyPanel == null)
		{
			_emptyPanel = new Border
			{
				Margin = new Thickness(0.0, base.ActualHeight * 0.28, 0.0, 0.0),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Top,
				Child = new EmptyWithTextView()
			};
			base.Children.Add(_emptyPanel);
		}
		if (_emptyPanel.Child is EmptyWithTextView emptyWithTextView)
		{
			emptyWithTextView.SetData(Utils.GetString(_archive ? "NoArchiveTimer" : "NoTimer"), Utils.GetString(_archive ? "NoArchiveTimerMessage" : "NoTimerMessage"), new EmptyIconsSourceModel(_archive ? "HabitListArchiveEmpty" : "HabitListEmpty", _archive ? new List<int> { 4, 5 } : new List<int> { 1, 2, 3, 4 }), !_archive);
		}
		_emptyPanel.Visibility = Visibility.Visible;
		int times = 20;
		if (!num)
		{
			return;
		}
		while (times > 0)
		{
			await Task.Delay(10);
			times--;
			if (!(base.ActualHeight < 1.0))
			{
				_emptyPanel.Margin = new Thickness(0.0, base.ActualHeight * 0.28, 0.0, 0.0);
				break;
			}
		}
	}

	public void SetItemFocusing()
	{
		if (!(ItemList.ItemsSource is ObservableCollection<FocusTimerViewModel> observableCollection))
		{
			return;
		}
		foreach (FocusTimerViewModel item in observableCollection)
		{
			item.SetFocusing();
		}
	}

	public void OnItemClick(FocusTimerViewModel model)
	{
		ClearSelect();
		model.Selected = true;
		Utils.FindParent<MainFocusControl>(this)?.OnTimerSelect(model.Id);
	}

	public void ClearSelect()
	{
		if (!(ItemList.ItemsSource is ObservableCollection<FocusTimerViewModel> observableCollection))
		{
			return;
		}
		foreach (FocusTimerViewModel item in observableCollection)
		{
			item.Selected = false;
		}
	}

	public void UpdateTimerDuration(string timerId, long duration)
	{
		if (!string.IsNullOrEmpty(timerId) && ItemList.ItemsSource is ObservableCollection<FocusTimerViewModel> source)
		{
			FocusTimerViewModel focusTimerViewModel = source.FirstOrDefault((FocusTimerViewModel m) => m.Id == timerId);
			if (focusTimerViewModel != null)
			{
				focusTimerViewModel.DurationText = Utils.GetShortDurationString(duration, showZeroMinute: false);
			}
		}
	}
}
