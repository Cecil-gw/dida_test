using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CountDown.Infrastructure;

namespace ticktick_WPF.Views.CountDown.ViewModels;

public class MainCountDownViewModel : ObservableObject
{
	private ObservableCollection<CountDownViewModel> _countdowns;

	private bool _isLoading;

	private bool _showArchived;

	private bool _showEmptyView;

	private bool _hasMoreItems;

	private bool _isShowGroup;

	private int _currentPage;

	private const int PageSize = 50;

	private List<CountdownModel> _allCountdownModels;

	private int _displayCount;

	public ObservableCollection<CountDownViewModel> Countdowns
	{
		get
		{
			return _countdowns;
		}
		set
		{
			SetProperty(ref _countdowns, value, "Countdowns");
		}
	}

	public bool IsLoading
	{
		get
		{
			return _isLoading;
		}
		set
		{
			SetProperty(ref _isLoading, value, "IsLoading");
		}
	}

	public bool ShowArchived
	{
		get
		{
			return _showArchived;
		}
		set
		{
			if (SetProperty(ref _showArchived, value, "ShowArchived"))
			{
				LoadCountdownsWithLock(clear: true, noTimeLimit: true);
			}
		}
	}

	public bool ShowEmptyView
	{
		get
		{
			return _showEmptyView;
		}
		set
		{
			SetProperty(ref _showEmptyView, value, "ShowEmptyView");
		}
	}

	public bool HasMoreItems
	{
		get
		{
			return _hasMoreItems;
		}
		set
		{
			SetProperty(ref _hasMoreItems, value, "HasMoreItems");
		}
	}

	public int DisplayCount
	{
		get
		{
			return _displayCount;
		}
		set
		{
			SetProperty(ref _displayCount, value, "DisplayCount");
		}
	}

	public bool IsShowGroup
	{
		get
		{
			return _isShowGroup;
		}
		set
		{
			SetProperty(ref _isShowGroup, value, "IsShowGroup");
		}
	}

	public Dictionary<CountDownType, string> Groups { get; set; } = new Dictionary<CountDownType, string>();

	public Action<string> OnGroupTitleUpdate { get; set; }

	public Action<CountDownType> OnShowGroupType { get; set; }

	public CountDownType lastGroupType { get; private set; } = CountDownType.All;

	private DateTime LastRefreshTime { get; set; }

	private bool IsWaitingLoadMore { get; set; }

	public ICommand RefreshCommand { get; }

	public ICommand LoadMoreCommand { get; }

	private SemaphoreSlim _loadSemaphore { get; } = new SemaphoreSlim(1, 1);

	public MainCountDownViewModel()
	{
		Countdowns = new ObservableCollection<CountDownViewModel>();
		if (!Utils.IsTickPackage())
		{
			foreach (CountDownType item in new List<CountDownType>
			{
				CountDownType.All,
				CountDownType.Anniversary,
				CountDownType.Countdown,
				CountDownType.Birthday,
				CountDownType.Holiday
			})
			{
				Groups.Add(item, CountdownModel.GetTypeResourceKey(item));
			}
		}
		else
		{
			foreach (CountDownType item2 in new List<CountDownType>
			{
				CountDownType.All,
				CountDownType.Countdown,
				CountDownType.Anniversary,
				CountDownType.Birthday,
				CountDownType.Holiday
			})
			{
				Groups.Add(item2, CountdownModel.GetTypeResourceKey(item2));
			}
		}
		RefreshCommand = new RelayCommand(async delegate
		{
			_currentPage = 0;
			await LoadCountdownsWithLock();
		});
		LoadMoreCommand = new RelayCommand(async delegate
		{
			await LoadMoreItemsAsync();
		});
		_showArchived = false;
		_currentPage = 0;
		InitConfig();
		LoadCountdownsWithLock();
	}

	public void InitConfig()
	{
		IsShowGroup = LocalSettings.Settings?.UserPreference?.CountdownConfig?.showSection == true;
		if (IsShowGroup && Enum.TryParse<CountDownType>(LocalSettings.Settings.ExtraSettings.LastSelectCountdownGroup, ignoreCase: true, out var result))
		{
			lastGroupType = result;
		}
		if (!IsShowGroup && lastGroupType != CountDownType.All)
		{
			OnShowGroupType?.Invoke(CountDownType.All);
			LoadCountdownsWithLock(clear: true, noTimeLimit: true);
		}
	}

	public async Task LoadCountdownsWithLock(bool clear = true, bool noTimeLimit = false)
	{
		DateTime now = DateTime.Now;
		if (clear)
		{
			if (IsLoading || (!noTimeLimit && LastRefreshTime.AddSeconds(1.0) >= now))
			{
				return;
			}
		}
		else
		{
			if (IsWaitingLoadMore)
			{
				return;
			}
			IsWaitingLoadMore = true;
		}
		await _loadSemaphore.WaitAsync();
		try
		{
			if (clear)
			{
				_currentPage = 0;
				_allCountdownModels = null;
			}
			else if (!HasMoreItems || IsLoading)
			{
				return;
			}
			IsLoading = true;
			await LoadCountdownsAsync();
		}
		catch (Exception ex)
		{
			UtilLog.Warn("LoadCountdownsAsync failed: " + ex.Message);
		}
		finally
		{
			LastRefreshTime = DateTime.Now;
			MainCountDownViewModel mainCountDownViewModel = this;
			bool isLoading = (IsWaitingLoadMore = false);
			mainCountDownViewModel.IsLoading = isLoading;
			_loadSemaphore.Release();
		}
	}

	private async Task LoadCountdownsAsync()
	{
		await CountdownDao.InitializeDefaultCountdowns();
		if (_allCountdownModels == null)
		{
			List<CountdownModel> allCountdownModels = ((!ShowArchived) ? (await CountdownDao.GetActiveCountdowns()) : (await CountdownDao.GetArchivedCountdowns()));
			_allCountdownModels = allCountdownModels;
		}
		ShowEmptyView = _allCountdownModels == null || _allCountdownModels.Count == 0;
		DisplayCount = _allCountdownModels?.Count ?? 0;
		if (ShowEmptyView)
		{
			await Application.Current.Dispatcher.InvokeAsync(delegate
			{
				foreach (CountDownViewModel countdown in Countdowns)
				{
					countdown.DisposeImage();
				}
				Countdowns.Clear();
			});
		}
		else
		{
			IEnumerable<CountdownModel> source = await Task.Run(() => SortAllModels(_allCountdownModels, ShowArchived));
			if (lastGroupType != CountDownType.All)
			{
				source = source.Where((CountdownModel x) => x.Type == lastGroupType);
			}
			IEnumerable<CountdownModel> pageModels = source.Skip(_currentPage * 50).Take(50);
			await Application.Current.Dispatcher.InvokeAsync(delegate
			{
				if (_currentPage == 0)
				{
					foreach (CountDownViewModel countdown2 in Countdowns)
					{
						countdown2.DisposeImage();
					}
					Countdowns.Clear();
					GC.Collect();
				}
				if (pageModels != null && pageModels.Any())
				{
					foreach (CountdownModel item2 in pageModels)
					{
						CountDownViewModel item = new CountDownViewModel(item2);
						Countdowns.Add(item);
					}
				}
			});
			HasMoreItems = (_currentPage + 1) * 50 < _allCountdownModels?.Count;
			_currentPage++;
		}
		UpdateGroupTitles();
	}

	private async Task UpdateGroupTitles()
	{
		HashSet<CountDownType> hashSet = new HashSet<CountDownType>();
		if (_allCountdownModels != null && _allCountdownModels.Any())
		{
			foreach (CountdownModel allCountdownModel in _allCountdownModels)
			{
				hashSet.Add(allCountdownModel.Type);
			}
		}
		Dictionary<CountDownType, string> dictionary = new Dictionary<CountDownType, string>();
		foreach (KeyValuePair<CountDownType, string> group in Groups)
		{
			if (group.Key == CountDownType.All)
			{
				dictionary.Add(group.Key, group.Value);
			}
			else if (hashSet.Contains(group.Key) && !dictionary.ContainsKey(group.Key))
			{
				dictionary.Add(group.Key, group.Value);
			}
		}
		string groupTitle = string.Join("|", dictionary.Values);
		if (dictionary.ContainsKey(lastGroupType) || lastGroupType == CountDownType.All)
		{
			await Application.Current.Dispatcher.InvokeAsync(delegate
			{
				OnGroupTitleUpdate?.Invoke(groupTitle);
				OnShowGroupType?.Invoke(lastGroupType);
			});
		}
		else
		{
			lastGroupType = CountDownType.All;
			_currentPage = 0;
			_allCountdownModels = null;
			await LoadCountdownsAsync();
		}
	}

	public static IEnumerable<CountdownModel> SortAllModels(IEnumerable<CountdownModel> models, bool isArchivedOrder, bool ignorePinOrder = false)
	{
		if (models == null || !models.Any())
		{
			return new List<CountdownModel>();
		}
		if (!isArchivedOrder)
		{
			if (!ignorePinOrder)
			{
				Func<CountdownModel, bool> pinCondition = (CountdownModel c) => c.pinnedTimeValue != null && c.pinnedTimeValue != "-1";
				List<CountdownModel> first = (from c in models.Where(pinCondition)
					orderby c.pinnedTimeValue descending
					select c).ToList();
				List<CountdownModel> second = models.Where((CountdownModel x) => !pinCondition(x)).OrderBy(delegate(CountdownModel c)
				{
					DisplayTimerMode? timerMode = c.TimerMode;
					int daysRemaining = c.GetDaysRemaining(null, null, timerMode);
					return (daysRemaining < 0) ? (1073741823 + Math.Abs(daysRemaining)) : daysRemaining;
				}).ToList();
				return first.Concat(second);
			}
			return models.OrderBy(delegate(CountdownModel c)
			{
				DisplayTimerMode? timerMode = c.TimerMode;
				int daysRemaining = c.GetDaysRemaining(null, null, timerMode);
				return (daysRemaining < 0) ? (1073741823 + Math.Abs(daysRemaining)) : daysRemaining;
			}).ToList();
		}
		return models.OrderByDescending((CountdownModel c) => c.ArchivedTime);
	}

	public async Task LoadMoreItemsAsync()
	{
		await LoadCountdownsWithLock(clear: false);
	}

	public bool UpdateFilter(string text)
	{
		CountDownType? countDownType = null;
		foreach (KeyValuePair<CountDownType, string> group in Groups)
		{
			if (group.Value == text)
			{
				countDownType = group.Key;
				break;
			}
		}
		if (!countDownType.HasValue)
		{
			return false;
		}
		return UpdateFilter(countDownType.Value);
	}

	public bool UpdateFilter(CountDownType type)
	{
		if (lastGroupType == type)
		{
			return false;
		}
		lastGroupType = type;
		LocalSettings.Settings.ExtraSettings.LastSelectCountdownGroup = type.ToString();
		LocalSettings.Settings.Save(force: true);
		return true;
	}

	public async Task Clear()
	{
		await _loadSemaphore.WaitAsync();
		try
		{
			_currentPage = 0;
			_allCountdownModels = null;
			IsLoading = true;
			ShowEmptyView = true;
			DisplayCount = 0;
			await Application.Current.Dispatcher.InvokeAsync(delegate
			{
				foreach (CountDownViewModel countdown in Countdowns)
				{
					countdown.DisposeImage();
				}
				Countdowns.Clear();
			});
			HasMoreItems = false;
			UpdateGroupTitles();
			GC.Collect();
		}
		catch (Exception ex)
		{
			UtilLog.Warn("LoadCountdownsAsync failed: " + ex.Message);
		}
		finally
		{
			LastRefreshTime = DateTime.Now;
			MainCountDownViewModel mainCountDownViewModel = this;
			bool isLoading = (IsWaitingLoadMore = false);
			mainCountDownViewModel.IsLoading = isLoading;
			_loadSemaphore.Release();
		}
	}

	public async Task UpdateDisplayItem(CountdownModel model)
	{
		if (string.IsNullOrEmpty(model?.Id))
		{
			return;
		}
		await _loadSemaphore.WaitAsync();
		try
		{
			Countdowns.FirstOrDefault((CountDownViewModel x) => x.Model?.Id == model?.Id)?.UpdateFromModel(model);
		}
		catch (Exception ex)
		{
			UtilLog.Warn("RemoveItem failed: " + ex.Message);
		}
		finally
		{
			_loadSemaphore.Release();
		}
	}

	public async Task UpdateItemWithSort(CountdownModel model, CountdownNotifyType? notifyType)
	{
		if (model == null)
		{
			return;
		}
		CountDownViewModel pinnedItem = null;
		await _loadSemaphore.WaitAsync();
		try
		{
			CountdownModel countdownModel = _allCountdownModels.FirstOrDefault((CountdownModel x) => x.Id == model.Id);
			switch (notifyType)
			{
			case CountdownNotifyType.Add:
				if (countdownModel == null)
				{
					_allCountdownModels.Add(model);
				}
				break;
			case CountdownNotifyType.Update:
				if (countdownModel != null)
				{
					_allCountdownModels[_allCountdownModels.IndexOf(countdownModel)] = model;
				}
				break;
			case CountdownNotifyType.Delete:
				if (countdownModel != null)
				{
					_allCountdownModels.Remove(countdownModel);
				}
				break;
			}
			DisplayCount = _allCountdownModels?.Count ?? 0;
			CountDownViewModel exist = Countdowns.FirstOrDefault((CountDownViewModel x) => x.Model?.Id == model?.Id);
			List<CountdownModel> list = ((!ShowArchived) ? (await CountdownDao.GetActiveCountdowns()) : (await CountdownDao.GetArchivedCountdowns()));
			List<CountdownModel> rawlist = list;
			ShowEmptyView = rawlist == null || rawlist.Count == 0;
			if (_currentPage <= 0 && !ShowEmptyView)
			{
				_currentPage = 1;
			}
			IEnumerable<CountdownModel> source = await Task.Run(() => SortAllModels(rawlist, ShowArchived));
			if (lastGroupType != CountDownType.All)
			{
				source = source.Where((CountdownModel x) => x.Type == lastGroupType);
			}
			IEnumerable<CountdownModel> source2 = source.Take(50 * _currentPage);
			CountdownModel countdownModel2 = source2.FirstOrDefault((CountdownModel x) => x.Id == model.Id);
			if (exist != null)
			{
				pinnedItem = Countdowns.FirstOrDefault((CountDownViewModel x) => x.IsPinned);
				int num = Countdowns.IndexOf(exist);
				if (countdownModel2 != null)
				{
					exist.UpdateFromModel(model);
					int newIndex = source2.ToList().IndexOf(countdownModel2);
					Countdowns.Move(num, newIndex);
				}
				else
				{
					Countdowns.RemoveAt(num);
					CountdownModel countdownModel3 = source.Skip(50 * _currentPage - 1).Take(1).FirstOrDefault();
					if (countdownModel3 != null)
					{
						CountDownViewModel item = new CountDownViewModel(countdownModel3);
						Countdowns.Add(item);
					}
				}
			}
			else if (countdownModel2 != null)
			{
				int index = source2.ToList().IndexOf(countdownModel2);
				CountDownViewModel item2 = new CountDownViewModel(model);
				Countdowns.Insert(index, item2);
				if (Countdowns.Count > 50 * _currentPage)
				{
					Countdowns.RemoveAt(Countdowns.Count - 1);
				}
			}
			UpdateGroupTitles();
		}
		catch (Exception ex)
		{
			UtilLog.Warn("UpdateItemWithSort failed: " + ex.Message);
		}
		finally
		{
			_loadSemaphore.Release();
		}
		if (!string.IsNullOrEmpty(pinnedItem?.Model?.Id) && model.pinnedTimeStamp > 0 && model.Id != pinnedItem.Model.Id)
		{
			await UpdateItemWithSort(await CountdownDao.GetCountdownById(pinnedItem.Model.Id), notifyType);
		}
	}

	public void SwitchShowRemark(bool show)
	{
		foreach (CountDownViewModel item in Countdowns.ToList())
		{
			item.SwitchShowDateOrRemark(show);
		}
	}
}
