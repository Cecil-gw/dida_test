using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Entity.Binding;
using ticktick_WPF.Entity.Dto;
using ticktick_WPF.Entity.Enums;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Views;

namespace ticktick_WPF.ViewModels.AI;

internal class AIRecommendViewModel : INotifyPropertyChanged
{
	private string _title;

	private AIRecommendStep _step;

	private string _aiDescription;

	private bool _isShowAIWarning;

	private bool _isShowDetail;

	private bool _isLoaded;

	private bool _isShowEmpty;

	private bool _isShowAIPart;

	private bool _isShowHint;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand<AIRecommendGroup>? viewMoreCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? backViewCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? recommendClickCommand;

	public Action<string> OnToast { get; set; }

	public Action OnEnterDetail { get; set; }

	public Action OnGenerating { get; set; }

	public Action OnGenerated { get; set; }

	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			_title = value;
			NotifyPropertyChanged("Title");
		}
	}

	public AIRecommendStep Step
	{
		get
		{
			return _step;
		}
		set
		{
			_step = value;
			NotifyPropertyChanged("Step");
		}
	}

	public string AiDescription
	{
		get
		{
			return _aiDescription;
		}
		set
		{
			_aiDescription = value;
			NotifyPropertyChanged("AiDescription");
		}
	}

	public bool IsShowAIWarning
	{
		get
		{
			return _isShowAIWarning;
		}
		set
		{
			if (_isShowAIWarning != value)
			{
				_isShowAIWarning = value;
				NotifyPropertyChanged("IsShowAIWarning");
			}
		}
	}

	public bool IsShowDetail
	{
		get
		{
			return _isShowDetail;
		}
		set
		{
			_isShowDetail = value;
			NotifyPropertyChanged("IsShowDetail");
		}
	}

	public bool IsLoaded
	{
		get
		{
			return _isLoaded;
		}
		set
		{
			_isLoaded = value;
			NotifyPropertyChanged("IsLoaded");
		}
	}

	public bool IsShowEmpty
	{
		get
		{
			return _isShowEmpty;
		}
		set
		{
			if (_isShowEmpty != value)
			{
				_isShowEmpty = value;
				NotifyPropertyChanged("IsShowEmpty");
			}
		}
	}

	public bool IsShowAIPart
	{
		get
		{
			return _isShowAIPart;
		}
		set
		{
			if (_isShowAIPart != value)
			{
				_isShowAIPart = value;
				NotifyPropertyChanged("IsShowAIPart");
			}
		}
	}

	public bool IsShowHint
	{
		get
		{
			return _isShowHint;
		}
		set
		{
			_isShowHint = value;
			NotifyPropertyChanged("IsShowHint");
		}
	}

	public ObservableCollection<AIRecommendGroup> DefaultGroups { get; set; } = new ObservableCollection<AIRecommendGroup>();

	public ObservableCollection<AIRecommendGroup> AIRecommendGroups { get; set; } = new ObservableCollection<AIRecommendGroup>();

	public ObservableCollection<AIRecommendGroup> DetailGroups { get; set; } = new ObservableCollection<AIRecommendGroup>();

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<AIRecommendGroup> ViewMoreCommand => viewMoreCommand ?? (viewMoreCommand = new AsyncRelayCommand<AIRecommendGroup>(ViewMore));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand BackViewCommand => backViewCommand ?? (backViewCommand = new AsyncRelayCommand(BackView));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand RecommendClickCommand => recommendClickCommand ?? (recommendClickCommand = new AsyncRelayCommand(RecommendClick));

	public event PropertyChangedEventHandler PropertyChanged;

	public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public AIRecommendViewModel()
	{
		Title = Utils.GetString("AISuggestedTasks");
		AiDescription = Utils.GetString("AISuggestTaskFitSituation");
		Load();
	}

	private async Task Load()
	{
		_ = 1;
		try
		{
			DateTime today = DateTime.Today;
			DateTime dateTime = today.AddDays(1.0);
			DateTime dateTime2 = today.AddDays(2.0);
			DateTime dateTime3 = today.AddDays(3.0);
			IEnumerable<TaskBaseViewModel> tasks = AIRecommendService.GetTasks();
			List<AIRecommendTaskInfo> recentAdd = new List<AIRecommendTaskInfo>();
			List<AIRecommendTaskInfo> longTimeAgos = new List<AIRecommendTaskInfo>();
			List<AIRecommendTaskInfo> comings = new List<AIRecommendTaskInfo>();
			List<AIRecommendTaskInfo> editFrequent = new List<AIRecommendTaskInfo>();
			List<AIRecommendTaskInfo> list = new List<AIRecommendTaskInfo>();
			List<AIRecommendTaskInfo> aiRecs = new List<AIRecommendTaskInfo>();
			foreach (TaskBaseViewModel item in tasks)
			{
				if (!string.IsNullOrEmpty(item.RepeatFlag))
				{
					continue;
				}
				DateTime? startDate = item.StartDate;
				DateTime? dueDate = item.DueDate;
				if (startDate.HasValue)
				{
					if (dueDate.HasValue)
					{
						DateTime value = dateTime;
						DateTime? startDate2 = item.StartDate;
						if (value > startDate2)
						{
							value = today;
							startDate2 = item.DueDate;
							if (value < startDate2)
							{
								continue;
							}
						}
					}
					else if (startDate.Value.Date == today)
					{
						continue;
					}
				}
				aiRecs.Add(new AIRecommendTaskInfo(item));
				DateTime? createdTime = item.CreatedTime;
				DateTime? dateTime4 = dueDate ?? startDate;
				if (createdTime.HasValue)
				{
					double totalDays = (today - createdTime.Value.Date).TotalDays;
					if (totalDays <= 1.0)
					{
						if (!startDate.HasValue && !dueDate.HasValue)
						{
							recentAdd.Add(new AIRecommendTaskInfo(item));
						}
					}
					else if (totalDays >= 7.0 && (!dateTime4.HasValue || dateTime4 < DateTime.Today))
					{
						longTimeAgos.Add(new AIRecommendTaskInfo(item));
					}
				}
				if (startDate.HasValue)
				{
					if (dateTime4.Value.Date == dateTime || dateTime4.Value.Date == dateTime2 || (dueDate.HasValue && dateTime4.Value == dateTime3 && item.IsAllDay == true))
					{
						comings.Add(new AIRecommendTaskInfo(item));
					}
					if (dateTime4 < DateTime.Today)
					{
						list.Add(new AIRecommendTaskInfo(item));
					}
				}
			}
			Dictionary<string, AIRecommendTaskInfo> delayDicts = list.ToDictionary((AIRecommendTaskInfo x) => x.Task.Id, (AIRecommendTaskInfo x) => x);
			Dictionary<string, int> dictionary = await Communicator.GetTaskActivityCount(delayDicts.Keys);
			if (dictionary != null)
			{
				foreach (KeyValuePair<string, int> item2 in dictionary.Where((KeyValuePair<string, int> x) => x.Value >= 2))
				{
					if (!string.IsNullOrEmpty(item2.Key) && delayDicts.TryGetValue(item2.Key, out var value2))
					{
						value2.ActivityCount = item2.Value;
						editFrequent.Add(value2);
					}
				}
			}
			if (recentAdd.Any())
			{
				AddToDefaultGroups(recentAdd, Utils.GetString("AIRecentlyAdded"), AIRecommendGroupType.RecentlyAdd);
			}
			if (editFrequent.Any())
			{
				AddToDefaultGroups(editFrequent, Utils.GetString("AIRepeatedDelays"), AIRecommendGroupType.EditFrequent);
			}
			if (longTimeAgos.Any())
			{
				if (editFrequent.Any())
				{
					IEnumerable<string> editFrequentIds = editFrequent.Select((AIRecommendTaskInfo x) => x.Task.Id);
					longTimeAgos.RemoveAll((AIRecommendTaskInfo x) => editFrequentIds.Contains(x.Task.Id));
				}
				AddToDefaultGroups(longTimeAgos, Utils.GetString("AILongPending"), AIRecommendGroupType.LongTimeAgo);
			}
			if (comings.Any())
			{
				AddToDefaultGroups(comings, Utils.GetString("AIUpcoming"), AIRecommendGroupType.ComingSoon);
			}
			if (IsShowAIPart)
			{
				Dictionary<string, AIRecommendTaskInfo> aiRecDict = aiRecs.OrderByDescending((AIRecommendTaskInfo x) => x.Task.ModifiedTime ?? x.Task.CreatedTime).Take(200).ToDictionary((AIRecommendTaskInfo x) => x.Task.Id, (AIRecommendTaskInfo x) => x);
				List<AIRecommendHistoryDto> list2 = await Communicator.AIRecommendHistory(aiRecDict.Keys);
				if (list2 != null && list2.Any())
				{
					List<AIRecommendTaskInfo> list3 = new List<AIRecommendTaskInfo>();
					List<AIRecommendTaskInfo> list4 = new List<AIRecommendTaskInfo>();
					long num = 0L;
					long num2 = 0L;
					foreach (AIRecommendHistoryDto item3 in list2)
					{
						num = Math.Max(item3.ctime, num);
						num2 = Math.Max(item3.mtime, num2);
						if (!string.IsNullOrEmpty(item3.id))
						{
							if (item3.estimated_duration <= 60 && item3.estimated_duration >= 0 && aiRecDict.TryGetValue(item3.id, out var value3))
							{
								value3.Duration = item3.estimated_duration;
								value3.Importance = item3.importance;
								list3.Add(value3);
							}
							if (item3.importance >= 5 && aiRecDict.TryGetValue(item3.id, out var value4))
							{
								value4.Duration = item3.estimated_duration;
								value4.Importance = item3.importance;
								list4.Add(value4);
							}
						}
					}
					if (list3.Count + list4.Count > 0)
					{
						Step = AIRecommendStep.Generated;
						if (list3.Any())
						{
							AddToAIGroups(list3, Utils.GetString("AIQuickWins"), AIRecommendGroupType.EasyHandle);
						}
						if (list4.Any())
						{
							AddToAIGroups(list4, Utils.GetString("AIKeyTasks"), AIRecommendGroupType.Important);
						}
					}
					DateTime localDateTime = DateTimeOffset.FromUnixTimeMilliseconds(num2).LocalDateTime;
					if (localDateTime > AIRecommendService.lastMTime)
					{
						AIRecommendService.lastMTime = localDateTime;
					}
					DateTime localDateTime2 = DateTimeOffset.FromUnixTimeMilliseconds(num).LocalDateTime;
					if (localDateTime2 > AIRecommendService.lastCTime)
					{
						AIRecommendService.lastCTime = localDateTime2;
					}
					UpdateAIDescription();
				}
			}
			UpdateLastDefault();
			CheckIfEmpty();
			if (!IsShowEmpty && !LocalSettings.Settings.ExtraSettings.CheckHasFlag(HasSetFlag.AIRecommendItemHint))
			{
				IsShowHint = true;
				LocalSettings.Settings.ExtraSettings.SetHasFlag(HasSetFlag.AIRecommendItemHint);
				LocalSettings.Settings.Save();
				Task.Run(async delegate
				{
					await Task.Delay(3000);
					Application.Current.Dispatcher.Invoke(delegate
					{
						IsShowHint = false;
					});
				});
			}
		}
		catch (Exception ex)
		{
			UtilLog.Error("Load AIRecommendTask:" + ex);
		}
		IsLoaded = true;
	}

	public void UpdateAIDescription()
	{
		DateTime lastMTime = AIRecommendService.lastMTime;
		string text = DateUtils.FormatMonthDay(lastMTime, DateFormatLen.Middle);
		string hourMinuteText = DateUtils.GetHourMinuteText(lastMTime);
		AiDescription = string.Format(Utils.GetString("AIUpdateOn"), text + ", " + hourMinuteText);
	}

	private void AddToDefaultGroups(List<AIRecommendTaskInfo> tasks, string title, AIRecommendGroupType type)
	{
		AddTotalGroups(tasks, title, type, RecommendTotalGroupType.Default);
	}

	private void AddToDetailGroups(List<AIRecommendTaskInfo> tasks, string title, AIRecommendGroupType type)
	{
		AddTotalGroups(tasks, title, type, RecommendTotalGroupType.Detail);
	}

	private void AddToAIGroups(List<AIRecommendTaskInfo> tasks, string title, AIRecommendGroupType type)
	{
		AddTotalGroups(tasks, title, type, RecommendTotalGroupType.AI);
	}

	private void AddTotalGroups(List<AIRecommendTaskInfo> tasks, string title, AIRecommendGroupType type, RecommendTotalGroupType totalType)
	{
		if (!tasks.Any())
		{
			return;
		}
		tasks = SortTasks(tasks, type);
		AIRecommendGroup aIRecommendGroup = new AIRecommendGroup
		{
			Title = title,
			TotalType = totalType,
			GroupType = type
		};
		if (totalType != RecommendTotalGroupType.Detail)
		{
			aIRecommendGroup.Tasks = tasks;
			aIRecommendGroup.IsShowMore = tasks.Count > 5;
		}
		IEnumerable<AIRecommendTaskInfo> enumerable;
		if (totalType != RecommendTotalGroupType.Detail)
		{
			enumerable = tasks.Take(5);
		}
		else
		{
			IEnumerable<AIRecommendTaskInfo> enumerable2 = tasks;
			enumerable = enumerable2;
		}
		foreach (AIRecommendTaskInfo item2 in enumerable)
		{
			AIRecommendItem item = new AIRecommendItem(item2)
			{
				GroupType = type,
				TotalType = totalType
			};
			ApplyTaskToItem(item2, item);
			aIRecommendGroup.Items.Add(item);
		}
		GetTotalGroups(totalType)?.Add(aIRecommendGroup);
	}

	private void AddOrUpdateTotalGroups(List<AIRecommendTaskInfo> tasks, string title, AIRecommendGroupType type, RecommendTotalGroupType totalType)
	{
		if (!tasks.Any())
		{
			return;
		}
		ObservableCollection<AIRecommendGroup> totalGroups = GetTotalGroups(totalType);
		if (totalGroups == null)
		{
			return;
		}
		AIRecommendGroup aIRecommendGroup = totalGroups.FirstOrDefault((AIRecommendGroup x) => x.GroupType == type && x.TotalType == totalType);
		bool flag = aIRecommendGroup == null;
		if (flag)
		{
			aIRecommendGroup = new AIRecommendGroup
			{
				Title = title,
				TotalType = totalType,
				GroupType = type
			};
			tasks = SortTasks(tasks, type);
			aIRecommendGroup.Tasks = tasks;
		}
		else
		{
			HashSet<string> hashSet = new HashSet<string>(aIRecommendGroup.Tasks.Select((AIRecommendTaskInfo x) => x.Task.Id));
			foreach (AIRecommendTaskInfo task in tasks)
			{
				if (hashSet.Add(task.Task.Id))
				{
					aIRecommendGroup.Tasks.Add(task);
				}
			}
			aIRecommendGroup.Tasks = SortTasks(aIRecommendGroup.Tasks, type);
			aIRecommendGroup.Items.Clear();
		}
		aIRecommendGroup.IsShowMore = aIRecommendGroup.Tasks.Count > 5;
		foreach (AIRecommendTaskInfo item2 in aIRecommendGroup.Tasks.Take(5))
		{
			AIRecommendItem item = new AIRecommendItem(item2)
			{
				GroupType = type,
				TotalType = totalType
			};
			ApplyTaskToItem(item2, item);
			aIRecommendGroup.Items.Add(item);
		}
		if (flag)
		{
			totalGroups.Add(aIRecommendGroup);
		}
	}

	private void UpdateLastDefault()
	{
		int count = DefaultGroups.Count;
		for (int i = 0; i < count; i++)
		{
			AIRecommendGroup aIRecommendGroup = DefaultGroups[i];
			int count2 = aIRecommendGroup.Items.Count;
			for (int j = 0; j < count2; j++)
			{
				aIRecommendGroup.Items[j].IsLastDefault = i == count - 1 && j == count2 - 1;
			}
		}
	}

	private void CheckIfEmpty()
	{
		if (!IsShowAIPart)
		{
			IsShowEmpty = (!DefaultGroups.Any() || !DefaultGroups.SelectMany((AIRecommendGroup x) => x.Tasks).Any()) && (!AIRecommendGroups.Any() || !AIRecommendGroups.SelectMany((AIRecommendGroup x) => x.Tasks).Any());
		}
	}

	private List<AIRecommendTaskInfo> SortTasks(List<AIRecommendTaskInfo> tasks, AIRecommendGroupType type)
	{
		switch (type)
		{
		case AIRecommendGroupType.RecentlyAdd:
		case AIRecommendGroupType.LongTimeAgo:
			return tasks.OrderByDescending((AIRecommendTaskInfo x) => x.Task.CreatedTime).ToList();
		case AIRecommendGroupType.EditFrequent:
			return tasks.OrderByDescending((AIRecommendTaskInfo x) => x.ActivityCount).ToList();
		case AIRecommendGroupType.ComingSoon:
			return (from x in tasks
				orderby x.Task.DueDate ?? x.Task.StartDate, (!x.Task.DueDate.HasValue) ? 1 : 0
				select x).ToList();
		case AIRecommendGroupType.EasyHandle:
			return tasks.OrderBy((AIRecommendTaskInfo x) => x.Duration).ToList();
		case AIRecommendGroupType.Important:
			return tasks.OrderByDescending((AIRecommendTaskInfo x) => x.Importance).ToList();
		default:
			return tasks;
		}
	}

	[RelayCommand]
	public async Task ViewMore(AIRecommendGroup group)
	{
		if (group.TotalType == RecommendTotalGroupType.Detail)
		{
			return;
		}
		Title = group.Title;
		DetailGroups.Clear();
		IsShowAIWarning = false;
		List<AIRecommendTaskInfo> tasks = group.Tasks;
		DateTime today = DateTime.Today;
		AIRecommendGroupType groupType = group.GroupType;
		switch (groupType)
		{
		case AIRecommendGroupType.RecentlyAdd:
		{
			DateTime now = DateTime.Now;
			DateTime dateTime = today.AddDays(-1.0);
			List<AIRecommendTaskInfo> list10 = new List<AIRecommendTaskInfo>();
			List<AIRecommendTaskInfo> list11 = new List<AIRecommendTaskInfo>();
			List<AIRecommendTaskInfo> list12 = new List<AIRecommendTaskInfo>();
			foreach (AIRecommendTaskInfo item in tasks)
			{
				if (item.Task.CreatedTime.HasValue)
				{
					DateTime value = item.Task.CreatedTime.Value;
					if ((now - value).TotalHours < 3.0)
					{
						list10.Add(item);
					}
					else if (value.Date == today)
					{
						list11.Add(item);
					}
					else if (value.Date == dateTime)
					{
						list12.Add(item);
					}
				}
			}
			AddToDetailGroups(list10, Utils.GetString("AIJustNow"), groupType);
			AddToDetailGroups(list11, Utils.GetString("Today"), groupType);
			AddToDetailGroups(list12, Utils.GetString("PublicYesterday"), groupType);
			break;
		}
		case AIRecommendGroupType.EditFrequent:
		{
			List<AIRecommendTaskInfo> list15 = new List<AIRecommendTaskInfo>();
			List<AIRecommendTaskInfo> list16 = new List<AIRecommendTaskInfo>();
			List<AIRecommendTaskInfo> list17 = new List<AIRecommendTaskInfo>();
			foreach (AIRecommendTaskInfo item2 in tasks)
			{
				if (item2.ActivityCount == 2)
				{
					list17.Add(item2);
				}
				else if (item2.ActivityCount > 2 && item2.ActivityCount < 5)
				{
					list16.Add(item2);
				}
				else if (item2.ActivityCount >= 5)
				{
					list15.Add(item2);
				}
			}
			AddToDetailGroups(list15, string.Format(Utils.GetString("AIOverXTimes"), 5), groupType);
			AddToDetailGroups(list16, string.Format(Utils.GetString("AIOverXTimes"), 3), groupType);
			AddToDetailGroups(list17, string.Format(Utils.GetString("AIOverXTimes"), 2), groupType);
			break;
		}
		case AIRecommendGroupType.LongTimeAgo:
		{
			List<AIRecommendTaskInfo> list4 = new List<AIRecommendTaskInfo>();
			List<AIRecommendTaskInfo> list5 = new List<AIRecommendTaskInfo>();
			List<AIRecommendTaskInfo> list6 = new List<AIRecommendTaskInfo>();
			foreach (AIRecommendTaskInfo item3 in tasks)
			{
				if (item3.Task.CreatedTime.HasValue)
				{
					int days = (today - item3.Task.CreatedTime.Value.Date).Days;
					if (days >= 7 && days < 14)
					{
						list4.Add(item3);
					}
					else if (days >= 14 && days < 30)
					{
						list5.Add(item3);
					}
					else if (days >= 30)
					{
						list6.Add(item3);
					}
				}
			}
			AddToDetailGroups(list4, string.Format(Utils.GetString("AIOverXDays"), 7), groupType);
			AddToDetailGroups(list5, string.Format(Utils.GetString("AIOverXDays"), 14), groupType);
			AddToDetailGroups(list6, string.Format(Utils.GetString("AIOverXDays"), 30), groupType);
			break;
		}
		case AIRecommendGroupType.ComingSoon:
		{
			List<AIRecommendTaskInfo> list13 = new List<AIRecommendTaskInfo>();
			List<AIRecommendTaskInfo> list14 = new List<AIRecommendTaskInfo>();
			foreach (AIRecommendTaskInfo item4 in tasks)
			{
				DateTime? dateTime2 = item4.Task.DueDate ?? item4.Task.StartDate;
				if (dateTime2.HasValue)
				{
					if (dateTime2.Value.Date == today.AddDays(1.0) || (item4.Task.DueDate.HasValue && dateTime2.Value == today.AddDays(2.0) && item4.Task.IsAllDay))
					{
						list13.Add(item4);
					}
					else if (dateTime2.Value.Date == today.AddDays(2.0) || (item4.Task.DueDate.HasValue && dateTime2.Value == today.AddDays(3.0) && item4.Task.IsAllDay))
					{
						list14.Add(item4);
					}
				}
			}
			AddToDetailGroups(list13, Utils.GetString("Tomorrow"), groupType);
			AddToDetailGroups(list14, Utils.GetString("In2Days"), groupType);
			break;
		}
		case AIRecommendGroupType.EasyHandle:
		{
			List<AIRecommendTaskInfo> list7 = new List<AIRecommendTaskInfo>();
			List<AIRecommendTaskInfo> list8 = new List<AIRecommendTaskInfo>();
			List<AIRecommendTaskInfo> list9 = new List<AIRecommendTaskInfo>();
			foreach (AIRecommendTaskInfo item5 in tasks)
			{
				if (item5.Duration <= 60 && item5.Duration >= 0)
				{
					if (item5.Duration < 5)
					{
						list7.Add(item5);
					}
					else if (item5.Duration < 30)
					{
						list8.Add(item5);
					}
					else
					{
						list9.Add(item5);
					}
				}
			}
			AddToDetailGroups(list7, Utils.GetString("AIDoRightNow"), groupType);
			AddToDetailGroups(list8, Utils.GetString("AIHandleQuick"), groupType);
			AddToDetailGroups(list9, Utils.GetString("AISpareTime"), groupType);
			IsShowAIWarning = true;
			break;
		}
		case AIRecommendGroupType.Important:
		{
			List<AIRecommendTaskInfo> list = new List<AIRecommendTaskInfo>();
			List<AIRecommendTaskInfo> list2 = new List<AIRecommendTaskInfo>();
			List<AIRecommendTaskInfo> list3 = new List<AIRecommendTaskInfo>();
			foreach (AIRecommendTaskInfo item6 in tasks)
			{
				if (item6.Importance >= 5)
				{
					if (item6.Importance >= 9)
					{
						list.Add(item6);
					}
					else if (item6.Importance >= 7)
					{
						list2.Add(item6);
					}
					else
					{
						list3.Add(item6);
					}
				}
			}
			AddToDetailGroups(list, Utils.GetString("AIVeryImportant"), groupType);
			AddToDetailGroups(list2, Utils.GetString("AIImportant"), groupType);
			AddToDetailGroups(list3, Utils.GetString("AIWorthAttention"), groupType);
			IsShowAIWarning = true;
			break;
		}
		}
		OnEnterDetail?.Invoke();
		IsShowDetail = true;
	}

	public bool ApplyTaskToItem(AIRecommendTaskInfo taskInfo, AIRecommendItem item, bool needCheck = false)
	{
		AIRecommendTaskInfo.TaskInfo task = taskInfo.Task;
		item.IsCompleted = task.Status == 2;
		item.Name = task.Title;
		item.UpdateIcon(task.Status);
		item.Description = GetTaskDescribe(taskInfo, item.GroupType);
		if (!needCheck)
		{
			return true;
		}
		if (task.Deleted != 0)
		{
			return false;
		}
		if (task.Kind == "NOTE")
		{
			return false;
		}
		if (task.Status != 0)
		{
			return false;
		}
		if (!string.IsNullOrEmpty(task.RepeatFlag))
		{
			return false;
		}
		DateTime? startDate = task.StartDate;
		DateTime? dueDate = task.DueDate;
		DateTime today = DateTime.Today;
		DateTime dateTime = today.AddDays(1.0);
		if (startDate.HasValue)
		{
			if (dueDate.HasValue)
			{
				DateTime value = dateTime;
				DateTime? startDate2 = task.StartDate;
				if (value > startDate2)
				{
					value = today;
					startDate2 = task.DueDate;
					if (value < startDate2)
					{
						return false;
					}
				}
			}
			else if (startDate.Value.Date == today)
			{
				return false;
			}
		}
		switch (item.GroupType)
		{
		case AIRecommendGroupType.RecentlyAdd:
			if (startDate.HasValue || dueDate.HasValue)
			{
				return false;
			}
			if (!task.CreatedTime.HasValue)
			{
				return false;
			}
			break;
		case AIRecommendGroupType.LongTimeAgo:
		{
			DateTime? dateTime3 = dueDate ?? startDate;
			if (dateTime3.HasValue && dateTime3.Value >= today)
			{
				return false;
			}
			if (!task.CreatedTime.HasValue)
			{
				return false;
			}
			break;
		}
		case AIRecommendGroupType.ComingSoon:
		{
			DateTime? dateTime2 = dueDate ?? startDate;
			if (!dateTime2.HasValue)
			{
				return false;
			}
			if (dateTime2.Value.Date != today.AddDays(1.0) && dateTime2.Value.Date != today.AddDays(2.0) && (!dueDate.HasValue || !(dateTime2.Value == today.AddDays(3.0)) || !task.IsAllDay))
			{
				return false;
			}
			break;
		}
		}
		AIRecommendGroup itemTaskGroup = GetItemTaskGroup(item);
		if (itemTaskGroup != null)
		{
			int num = itemTaskGroup.Tasks.FindIndex((AIRecommendTaskInfo x) => x.Task.Id == item.TaskId);
			if (num >= 0)
			{
				itemTaskGroup.Tasks[num].Task = task;
				itemTaskGroup.Tasks = SortTasks(itemTaskGroup.Tasks, item.GroupType);
			}
		}
		return true;
	}

	private string GetTaskDescribe(AIRecommendTaskInfo task, AIRecommendGroupType type)
	{
		DateTime today = DateTime.Today;
		switch (type)
		{
		case AIRecommendGroupType.RecentlyAdd:
		{
			AIRecommendTaskInfo.TaskInfo task2 = task.Task;
			if (task2 == null || !task2.CreatedTime.HasValue)
			{
				return null;
			}
			string empty = string.Empty;
			empty = ((!(task.Task.CreatedTime.Value.Date == today)) ? Utils.GetString("PublicYesterday") : Utils.GetString("Today"));
			return string.Format(Utils.GetString("AIAddedInX"), string.Format("{0}{1} {2:HH:mm}", Utils.IsCn() ? "" : " ", empty, task.Task.CreatedTime));
		}
		case AIRecommendGroupType.EditFrequent:
			return string.Format(Utils.GetString("AIEditedXTimes"), task.ActivityCount);
		case AIRecommendGroupType.LongTimeAgo:
		{
			AIRecommendTaskInfo.TaskInfo task3 = task.Task;
			if (task3 == null || !task3.CreatedTime.HasValue)
			{
				return string.Empty;
			}
			int days = (today.Date - task.Task.CreatedTime.Value.Date).Days;
			return string.Format(Utils.GetString("AICreatedXDaysAgo"), days);
		}
		case AIRecommendGroupType.ComingSoon:
		{
			DateTime? dateTime = task.Task.DueDate ?? task.Task.StartDate;
			if (!dateTime.HasValue)
			{
				return null;
			}
			DateTime date = dateTime.Value.Date;
			if (date == today.AddDays(1.0) || (task.Task.DueDate.HasValue && dateTime.Value == today.AddDays(2.0) && task.Task.IsAllDay))
			{
				return Utils.GetString("AIDueTomorrow");
			}
			if (date == today.AddDays(2.0) || (task.Task.DueDate.HasValue && dateTime.Value == today.AddDays(3.0) && task.Task.IsAllDay))
			{
				return Utils.GetString("AIDue2Days");
			}
			break;
		}
		case AIRecommendGroupType.EasyHandle:
			if (task.Duration < 5)
			{
				return string.Format(Utils.GetString("AILessThanXMins"), 5);
			}
			if (task.Duration < 30)
			{
				return string.Format(Utils.GetString("AILessThanXMins"), 30);
			}
			if (task.Duration <= 60)
			{
				return string.Format(Utils.GetString("AILessThanXMins"), 60);
			}
			break;
		case AIRecommendGroupType.Important:
			if (task.Importance >= 9)
			{
				return Utils.GetString("AIVeryImportant");
			}
			if (task.Importance >= 7)
			{
				return Utils.GetString("AIImportant");
			}
			if (task.Importance >= 5)
			{
				return Utils.GetString("AIWorthAttention");
			}
			break;
		}
		return null;
	}

	[RelayCommand]
	public async Task BackView()
	{
		IsShowAIWarning = false;
		IsShowDetail = false;
		Title = Utils.GetString("AISuggestedTasks");
		AIRecommendItem aIRecommendItem = DetailGroups.FirstOrDefault()?.Items?.FirstOrDefault();
		if (aIRecommendItem == null)
		{
			return;
		}
		AIRecommendGroup itemTaskGroup = GetItemTaskGroup(aIRecommendItem);
		if (itemTaskGroup.Tasks.Any())
		{
			itemTaskGroup.Items.Clear();
			RecommendTotalGroupType sourceTotalType = GetSourceTotalType(aIRecommendItem.GroupType);
			itemTaskGroup.IsShowMore = itemTaskGroup.Tasks.Count > 5;
			foreach (AIRecommendTaskInfo item2 in itemTaskGroup.Tasks.Take(5))
			{
				AIRecommendItem item = new AIRecommendItem(item2)
				{
					GroupType = aIRecommendItem.GroupType,
					TotalType = sourceTotalType
				};
				ApplyTaskToItem(item2, item);
				itemTaskGroup.Items.Add(item);
			}
			if (sourceTotalType == RecommendTotalGroupType.Default)
			{
				UpdateLastDefault();
			}
		}
		CheckIfEmpty();
	}

	public void RemoveItem(AIRecommendItem item, AIRecommendRemoveBy removeBy)
	{
		ObservableCollection<AIRecommendGroup> totalGroups = GetTotalGroups(item.TotalType);
		if (totalGroups == null)
		{
			return;
		}
		if (item.TotalType != RecommendTotalGroupType.Detail)
		{
			AIRecommendGroup aIRecommendGroup = totalGroups.FirstOrDefault((AIRecommendGroup x) => x.GroupType == item.GroupType);
			if (aIRecommendGroup == null)
			{
				return;
			}
			aIRecommendGroup.Items.Remove(item);
		}
		else
		{
			for (int num = totalGroups.Count - 1; num >= 0; num--)
			{
				totalGroups[num].Items.Remove(item);
				if (!totalGroups[num].Items.Any())
				{
					totalGroups.RemoveAt(num);
				}
			}
		}
		if (item.TotalType != RecommendTotalGroupType.Detail && removeBy == AIRecommendRemoveBy.AddToToday)
		{
			SetEventLabel(item.GroupType);
		}
		AIRecommendGroup itemTaskGroup = GetItemTaskGroup(item);
		if (itemTaskGroup == null)
		{
			return;
		}
		if (item.TotalType == RecommendTotalGroupType.Detail && (!totalGroups.Any() || !totalGroups.SelectMany((AIRecommendGroup x) => x.Items).Any()))
		{
			BackView();
			switch (GetSourceTotalType(item.GroupType))
			{
			case RecommendTotalGroupType.AI:
				AIRecommendGroups.Remove(itemTaskGroup);
				break;
			case RecommendTotalGroupType.Default:
				DefaultGroups.Remove(itemTaskGroup);
				break;
			}
		}
		foreach (AIRecommendGroup item2 in DefaultGroups.ToList())
		{
			RemoveInSourceGroup(item2, item, removeBy);
		}
		foreach (AIRecommendGroup item3 in AIRecommendGroups.ToList())
		{
			RemoveInSourceGroup(item3, item, removeBy);
		}
		if (item.TotalType == RecommendTotalGroupType.Default)
		{
			UpdateLastDefault();
		}
		CheckIfEmpty();
	}

	private void RemoveInSourceGroup(AIRecommendGroup group, AIRecommendItem item, AIRecommendRemoveBy removeBy)
	{
		if (group.Tasks != null)
		{
			int num = group.Tasks.FindIndex((AIRecommendTaskInfo x) => x.Task.Id == item.TaskId);
			if (num >= 0)
			{
				group.Tasks.RemoveAt(num);
			}
		}
		AIRecommendItem aIRecommendItem = group.Items.FirstOrDefault((AIRecommendItem x) => x.TaskId == item.TaskId);
		if (aIRecommendItem != null)
		{
			group.Items.Remove(aIRecommendItem);
			if (removeBy == AIRecommendRemoveBy.AddToToday)
			{
				SetEventLabel(group.GroupType);
			}
		}
		if (group.Items.Count <= 0)
		{
			if (group.Tasks != null && group.Tasks.Any())
			{
				group.IsShowMore = group.Tasks.Count > 5;
				{
					foreach (AIRecommendTaskInfo item3 in group.Tasks.Take(5))
					{
						AIRecommendItem item2 = new AIRecommendItem(item3)
						{
							GroupType = item.GroupType,
							TotalType = item.TotalType
						};
						ApplyTaskToItem(item3, item2);
						group.Items.Add(item2);
					}
					return;
				}
			}
			GetTotalGroups(group.TotalType)?.Remove(group);
			if (!AIRecommendGroups.Any())
			{
				Step = AIRecommendStep.Init;
			}
		}
		else
		{
			group.IsShowMore = group.Tasks.Count > 5 || group.Items.Count != group.Tasks.Count;
		}
	}

	private void SetEventLabel(AIRecommendGroupType groupType, string eventName = "add_to_today")
	{
		string data = null;
		switch (groupType)
		{
		case AIRecommendGroupType.RecentlyAdd:
			data = "recently_added";
			break;
		case AIRecommendGroupType.EditFrequent:
			data = "postponed_tasks";
			break;
		case AIRecommendGroupType.LongTimeAgo:
			data = "long_overdue";
			break;
		case AIRecommendGroupType.ComingSoon:
			data = "upcoming";
			break;
		case AIRecommendGroupType.EasyHandle:
			data = "quick_wins";
			break;
		case AIRecommendGroupType.Important:
			data = "important_tasks";
			break;
		}
		UserActCollectUtils.AddClickEvent("ai_suggested_tasks", eventName, data);
	}

	[RelayCommand]
	public async Task RecommendClick()
	{
		if (IsShowAIPart)
		{
			if (!UserDao.IsPro())
			{
				ProChecker.ShowUpgradeDialog(ProType.AIRecommendTask);
			}
			else if ((Step != AIRecommendStep.Init || !(AIRecommendService.lastCTime == default(DateTime)) || new CustomerDialog(Utils.GetString("AIAnalyzeTaskDialogTitle"), Utils.GetString("AIAnalyzeTaskDialogDesc"), Utils.GetString("AISuggest"), Utils.GetString("Cancel")).ShowDialog() == true) && await AIRecommendService.StartRecommend())
			{
				Step = AIRecommendStep.Generating;
				OnGenerating?.Invoke();
			}
		}
	}

	public async void OnAIRecommendResult(List<AIRecommendPropertyDto> list)
	{
		if (!list.Any())
		{
			Application.Current.Dispatcher.Invoke((Func<Task>)async delegate
			{
				OnGenerated?.Invoke();
				if (AIRecommendGroups.Any())
				{
					Step = AIRecommendStep.Generated;
				}
				else
				{
					Step = AIRecommendStep.Init;
				}
			});
			return;
		}
		AIRecommendService.lastMTime = DateTime.Now;
		Application.Current.Dispatcher.Invoke((Func<Task>)async delegate
		{
			OnGenerated?.Invoke();
		});
		IEnumerable<TaskBaseViewModel> tasks = AIRecommendService.GetTasks();
		Dictionary<string, AIRecommendTaskInfo> dictionary = new Dictionary<string, AIRecommendTaskInfo>();
		DateTime today = DateTime.Today;
		DateTime dateTime = today.AddDays(1.0);
		foreach (TaskBaseViewModel item in tasks)
		{
			if (string.IsNullOrEmpty(item.Id))
			{
				continue;
			}
			DateTime? startDate = item.StartDate;
			DateTime? dueDate = item.DueDate;
			if (startDate.HasValue)
			{
				if (dueDate.HasValue)
				{
					DateTime value = dateTime;
					DateTime? startDate2 = item.StartDate;
					if (value > startDate2)
					{
						value = today;
						startDate2 = item.DueDate;
						if (value < startDate2)
						{
							continue;
						}
					}
				}
				else if (startDate.Value.Date == today)
				{
					continue;
				}
			}
			dictionary[item.Id] = new AIRecommendTaskInfo(item);
		}
		List<AIRecommendTaskInfo> easys = new List<AIRecommendTaskInfo>();
		List<AIRecommendTaskInfo> importants = new List<AIRecommendTaskInfo>();
		foreach (AIRecommendPropertyDto item2 in list)
		{
			if (!string.IsNullOrEmpty(item2.id))
			{
				if (item2.estimated_duration <= 60 && item2.estimated_duration >= 0 && dictionary.TryGetValue(item2.id, out var value2))
				{
					value2.Duration = item2.estimated_duration;
					value2.Importance = item2.importance;
					easys.Add(value2);
				}
				if (item2.importance >= 5 && dictionary.TryGetValue(item2.id, out var value3))
				{
					value3.Duration = item2.estimated_duration;
					value3.Importance = item2.importance;
					importants.Add(value3);
				}
			}
		}
		Application.Current.Dispatcher.Invoke((Func<Task>)async delegate
		{
			if (easys.Count + importants.Count > 0)
			{
				Step = AIRecommendStep.Generated;
				if (easys.Any())
				{
					AddOrUpdateTotalGroups(easys, Utils.GetString("AIQuickWins"), AIRecommendGroupType.EasyHandle, RecommendTotalGroupType.AI);
				}
				if (importants.Any())
				{
					AddOrUpdateTotalGroups(importants, Utils.GetString("AIKeyTasks"), AIRecommendGroupType.Important, RecommendTotalGroupType.AI);
				}
			}
			else if (AIRecommendGroups.Any())
			{
				Step = AIRecommendStep.Generated;
			}
			else
			{
				Step = AIRecommendStep.Init;
			}
			UpdateAIDescription();
		});
	}

	public AIRecommendAllGroup TakeSnapshot()
	{
		return JsonConvert.DeserializeObject<AIRecommendAllGroup>(JsonConvert.SerializeObject(new AIRecommendAllGroup
		{
			DefaultGroups = DefaultGroups,
			AIRecommendGroups = AIRecommendGroups,
			DetailGroups = DetailGroups
		}));
	}

	public void RecoverFromSnapShot(AIRecommendAllGroup allGroup)
	{
		RestoreGroupCollection(DefaultGroups, allGroup.DefaultGroups);
		RestoreGroupCollection(AIRecommendGroups, allGroup.AIRecommendGroups);
		RestoreGroupCollection(DetailGroups, allGroup.DetailGroups);
	}

	private void RestoreGroupCollection(ObservableCollection<AIRecommendGroup> target, ObservableCollection<AIRecommendGroup> source)
	{
		target.Clear();
		foreach (AIRecommendGroup item in source)
		{
			target.Add(item);
		}
	}

	public HashSet<string> GetAllTaskIds()
	{
		return new HashSet<string>((from x in DefaultGroups.SelectMany((AIRecommendGroup x) => x.Tasks)
			select x.Task.Id).Union(from x in AIRecommendGroups.SelectMany((AIRecommendGroup x) => x.Tasks)
			select x.Task.Id));
	}

	private ObservableCollection<AIRecommendGroup> GetTotalGroups(RecommendTotalGroupType totalType)
	{
		return totalType switch
		{
			RecommendTotalGroupType.Default => DefaultGroups, 
			RecommendTotalGroupType.Detail => DetailGroups, 
			RecommendTotalGroupType.AI => AIRecommendGroups, 
			_ => null, 
		};
	}

	private RecommendTotalGroupType GetSourceTotalType(AIRecommendGroupType groupType)
	{
		if ((uint)(groupType - 4) <= 1u)
		{
			return RecommendTotalGroupType.AI;
		}
		return RecommendTotalGroupType.Default;
	}

	private AIRecommendGroup GetItemTaskGroup(AIRecommendItem item)
	{
		if (item.TotalType == RecommendTotalGroupType.Detail)
		{
			RecommendTotalGroupType sourceTotalType = GetSourceTotalType(item.GroupType);
			return GetTotalGroups(sourceTotalType)?.FirstOrDefault((AIRecommendGroup x) => x.GroupType == item.GroupType);
		}
		return GetTotalGroups(item.TotalType).FirstOrDefault((AIRecommendGroup x) => x.GroupType == item.GroupType);
	}
}
