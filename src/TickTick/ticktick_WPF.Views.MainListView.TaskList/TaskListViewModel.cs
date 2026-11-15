using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service.Display;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.Util.Sync.ClosedLoader;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Kanban;
using ticktick_WPF.Views.Search;
using ticktick_WPF.Views.TaskList;

namespace ticktick_WPF.Views.MainListView.TaskList;

public class TaskListViewModel : BaseViewModel
{
	private const int DetailItemsBulkReplaceThreshold = 200;

	private string _title;

	public readonly TrashSyncService TrashTaskLoader = new TrashSyncService();

	public List<TaskBaseViewModel> SourceModels = new List<TaskBaseViewModel>();

	private bool _loading;

	private KanbanViewModel _kanban;

	private string _previewLoadId;

	private List<DisplayItemModel> _cacheItems;

	private bool _forceReadOnly;

	public SortOption SortOption => ProjectIdentity.SortOption;

	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			if (_title != value)
			{
				_title = value;
				OnPropertyChanged("Title");
			}
		}
	}

	public ProjectIdentity ProjectIdentity { get; set; } = ProjectIdentity.CreateInboxProject();

	public ClosedLoader CompletedTaskLoader { get; private set; } = new ClosedLoader();

	public ObservableCollection<DisplayItemModel> Items { get; set; } = new ObservableCollection<DisplayItemModel>();

	public TaskListDisplayType ListDisplayType { get; set; }

	public SortProjectData ProjectData { get; set; }

	public bool InKanban => ListDisplayType == TaskListDisplayType.Kanban;

	public bool InMatrix => ListDisplayType == TaskListDisplayType.Matrix;

	public bool InDetail => ListDisplayType == TaskListDisplayType.SubTask;

	public bool IsList => ListDisplayType == TaskListDisplayType.Normal;

	public List<string> SelectedTaskIds { get; set; }

	public IBatchEditable BatchEditor { get; set; }

	public bool AddingTask { get; set; }

	public bool InBatchSelect { get; set; }

	public string FirstBatchSelectId { get; set; }

	public TaskListViewModel()
	{
	}

	public TaskListViewModel(ProjectIdentity projectIdentity)
	{
		ProjectIdentity = projectIdentity;
		Title = projectIdentity.GetDisplayTitle();
		CompletedTaskLoader.SetIdentity(projectIdentity);
	}

	public void SetIdentity(ProjectIdentity projectIdentity, TaskListDisplayType displayType = TaskListDisplayType.Normal)
	{
		if (projectIdentity.Id != ProjectIdentity?.Id)
		{
			_loading = false;
		}
		ProjectIdentity = projectIdentity;
		Title = projectIdentity.GetDisplayTitle();
		ListDisplayType = displayType;
		if (displayType != TaskListDisplayType.Kanban && displayType != TaskListDisplayType.Matrix)
		{
			CompletedTaskLoader.SetIdentity(projectIdentity);
			return;
		}
		ProjectData = new SortProjectData
		{
			ProjectIdentity = projectIdentity,
			ShowLoadMore = true
		};
	}

	public async Task LoadModels(bool refreshData, bool loadMore, bool forceReadOnly = false)
	{
		_forceReadOnly = forceReadOnly;
		if (ProjectIdentity is ColumnProjectIdentity)
		{
			await _kanban.LoadTasks();
			return;
		}
		bool forceLoad = _previewLoadId != ProjectIdentity.CatId;
		if (refreshData)
		{
			_previewLoadId = ProjectIdentity.CatId;
			SourceModels = await TaskListRefreshHelper.InitData(ProjectIdentity);
		}
		await LoadSortedItems(loadMore, null, forceLoad);
	}

	public async Task<List<DisplayItemModel>> BuildItems(bool loadMore, List<TaskBaseViewModel> models)
	{
		_loading = true;
		List<ColumnModel> columns = null;
		if (ProjectIdentity is NormalProjectIdentity normalProjectIdentity && normalProjectIdentity.SortOption?.groupBy == Constants.SortType.sortOrder.ToString() && !InKanban)
		{
			columns = await ColumnDao.GetColumnsByProjectId(normalProjectIdentity.Project.id);
		}
		return (await TaskListRefreshHelper.GetSortedDisplayItems(ProjectIdentity, models, loadMore, columns, CompletedTaskLoader, _forceReadOnly)) ?? new List<DisplayItemModel>();
	}

	public async Task LoadSortedItems(bool loadMore, SectionAddTaskViewModel addingModel = null, bool force = false)
	{
		if (SourceModels == null)
		{
			SourceModels = new List<TaskBaseViewModel>();
		}
		if (SourceModels.Count == 0 && SortOption?.groupBy != "sortOrder")
		{
			Items = new ObservableCollection<DisplayItemModel>();
			OnPropertyChanged("Items");
		}
		else
		{
			if (_loading && !force)
			{
				return;
			}
			List<DisplayItemModel> revisedItems = await BuildItems(loadMore, SourceModels);
			List<string> list = BatchEditor?.GetSelectedTaskIds();
			if (list != null && list.Count > 0)
			{
				bool flag = list.Count > 0 && InBatchSelect;
				foreach (DisplayItemModel item in revisedItems)
				{
					if (!item.IsOpen)
					{
						foreach (DisplayItemModel childrenModel in item.GetChildrenModels(getAll: true))
						{
							childrenModel.Selected = list.Contains(childrenModel.Id);
							childrenModel.InBatchSelected = childrenModel.Selected && flag;
						}
					}
					if (item.IsSection)
					{
						item.InBatchSelected = InBatchSelect;
						continue;
					}
					item.Selected = list.Contains(item.Id);
					item.InBatchSelected = item.Selected && flag;
				}
			}
			if (LocalSettings.Settings.ShowDetails && !InMatrix && !InKanban)
			{
				revisedItems.ForEach(delegate(DisplayItemModel item)
				{
					if (item.IsTask)
					{
						item.CompletionRate = TaskCompletionRateDao.GetRateStrById(item.Id);
					}
				});
				List<DisplayItemModel> taskModels = revisedItems.Where((DisplayItemModel item) => item.IsTask).ToList();
				TaskCompletionRateDao.FetchRateByModels(taskModels).ContinueWith(delegate(Task<List<string>> task)
				{
					if (task.IsCompleted && task.Exception == null && task.Result != null && task.Result.Any())
					{
						taskModels.Where((DisplayItemModel item) => task.Result.Contains(item.Id)).ToList().ForEach(delegate(DisplayItemModel item)
						{
							item.CompletionRate = TaskCompletionRateDao.GetRateStrById(item.Id);
						});
					}
				});
			}
			SetKanbanProperty(revisedItems);
			await TaskItemLoadHelper.LoadIndicator(revisedItems, LocalSettings.Settings.ShowDetails || InKanban);
			if (InKanban)
			{
				try
				{
					await TaskItemLoadHelper.LoadShowReminders(revisedItems);
				}
				catch (Exception ex)
				{
					UtilLog.Error(ex);
				}
			}
			if (addingModel != null)
			{
				for (int num = 0; num < revisedItems.Count; num++)
				{
					if (revisedItems[num].Section?.SectionId == addingModel.SectionId)
					{
						revisedItems.Insert(num + 1, new DisplayItemModel
						{
							SourceViewModel = new TaskBaseViewModel
							{
								Id = string.Empty
							},
							AddViewModel = addingModel,
							Section = revisedItems[num].Section
						});
						AddingTask = true;
						break;
					}
				}
			}
			if (revisedItems.Count > 0 && revisedItems[0].IsSection)
			{
				revisedItems[0].UnderTaskItem = false;
			}
			if (Math.Abs(Items.Count - revisedItems.Count) > 500)
			{
				Items = new ObservableCollection<DisplayItemModel>(revisedItems);
				OnPropertyChanged("Items");
			}
			else
			{
				ItemsSourceHelper.CopyTo(revisedItems, Items);
			}
			_cacheItems = revisedItems;
			_loading = false;
		}
	}

	public void SetKanbanProperty(List<DisplayItemModel> models)
	{
		if (!InKanban)
		{
			return;
		}
		for (int i = 0; i < models.Count; i++)
		{
			DisplayItemModel displayItemModel = models[i];
			if (i < models.Count - 1 && models[i + 1].Level > 0)
			{
				displayItemModel.ShowBottomMargin = false;
			}
			else
			{
				displayItemModel.ShowBottomMargin = true;
			}
			displayItemModel.ShowTopMargin = displayItemModel.Level <= 0;
			displayItemModel.IsLastInKanbanColumn = i == models.Count - 1;
			displayItemModel.InKanban = true;
		}
	}

	public void RemoveSelectedId(string id)
	{
		SelectedTaskIds?.Remove(id);
		BatchEditor?.RemoveSelectedId(id);
	}

	public DisplayItemModel SetSelectedTaskId(string id)
	{
		bool flag = string.IsNullOrEmpty(id);
		DisplayItemModel result = null;
		IEnumerable<DisplayItemModel> enumerable;
		if (!flag)
		{
			IEnumerable<DisplayItemModel> items = Items;
			enumerable = items;
		}
		else
		{
			IEnumerable<DisplayItemModel> items = GetItemsIncludingHiddenChildren();
			enumerable = items;
		}
		foreach (DisplayItemModel item in enumerable)
		{
			if (!flag && item.Id == id)
			{
				item.Selected = true;
				result = item;
				if (!item.CanBatchSelect)
				{
					InBatchSelect = false;
				}
				item.InBatchSelected = InBatchSelect;
				FirstBatchSelectId = (InBatchSelect ? id : "");
			}
			else
			{
				item.Selected = false;
				item.InBatchSelected = false;
			}
		}
		if (flag)
		{
			InBatchSelect = false;
			FirstBatchSelectId = "";
		}
		List<string> selectedTaskIds = (flag ? new List<string>() : new List<string> { id });
		SetSelectedTaskIds(selectedTaskIds);
		return result;
	}

	private List<DisplayItemModel> GetItemsIncludingHiddenChildren()
	{
		return GetItemsIncludingHiddenChildren(Items);
	}

	private static List<DisplayItemModel> GetItemsIncludingHiddenChildren(IEnumerable<DisplayItemModel> source)
	{
		List<DisplayItemModel> list = new List<DisplayItemModel>();
		HashSet<DisplayItemModel> visited = new HashSet<DisplayItemModel>();
		foreach (DisplayItemModel item in source)
		{
			AddItemAndChildren(item, list, visited);
		}
		return list;
	}

	private static void AddItemAndChildren(DisplayItemModel item, List<DisplayItemModel> items, HashSet<DisplayItemModel> visited)
	{
		if (item == null || !visited.Add(item))
		{
			return;
		}
		items.Add(item);
		if (item.Children == null)
		{
			return;
		}
		foreach (DisplayItemModel child in item.Children)
		{
			AddItemAndChildren(child, items, visited);
		}
	}

	public void SetSelectedTaskIds(List<string> ids)
	{
		SelectedTaskIds = ids;
		BatchEditor?.SetSelectedTaskIds(ids);
	}

	public async Task CheckSearchMatched(List<string> ids)
	{
		List<TaskBaseViewModel> tasks = TaskCache.GetTasksByIds(ids);
		ConcurrentDictionary<string, List<CommentModel>> concurrentDictionary = await TaskCommentCache.GetTaskCommentDict();
		List<TaskBaseViewModel> list = SourceModels;
		string keyword = SearchHelper.SearchKey;
		if (string.IsNullOrEmpty(keyword))
		{
			return;
		}
		List<string> list2 = keyword.Split(' ').ToList();
		list2.Remove("");
		bool flag = false;
		bool flag2 = false;
		foreach (TaskBaseViewModel task in tasks)
		{
			string title = task.Title;
			if (task.Kind == "TEXT")
			{
				title = title + "\n" + task.Content;
			}
			else
			{
				title = title + "\n" + task.Desc;
				BlockingList<TaskBaseViewModel> checkItems = task.CheckItems;
				if (checkItems != null && checkItems.Count > 0)
				{
					title = task.CheckItems.ToList().Aggregate(title, (string text, TaskBaseViewModel checkItem) => text + "\n" + checkItem.Title);
				}
			}
			if (!string.IsNullOrEmpty(task.Id) && concurrentDictionary.ContainsKey(task.Id))
			{
				title = concurrentDictionary[task.Id].Aggregate(title, (string text, CommentModel comment) => text + "\r\n" + comment.title?.ToLower());
			}
			if (SearchHelper.KeyWordMatched(title, keyword, list2))
			{
				if (list.All((TaskBaseViewModel m) => m.Id != task.Id))
				{
					list.Add(task);
					flag = true;
					flag2 = true;
				}
			}
			else
			{
				list.Remove(task);
				flag = true;
			}
		}
		if (flag2)
		{
			List<TaskBaseViewModel> list3 = new List<TaskBaseViewModel>();
			List<TaskBaseViewModel> list4 = new List<TaskBaseViewModel>();
			foreach (TaskBaseViewModel item in list)
			{
				if (item.Title != null && item.Title.Contains(keyword))
				{
					list3.Add(item);
					continue;
				}
				if (item.Kind == "TEXT")
				{
					if (item.Content != null && item.Content.Contains(keyword))
					{
						list3.Add(item);
						continue;
					}
				}
				else if ((item.Desc != null && item.Desc.Contains(keyword)) || item.CheckItems?.ToList()?.Any((TaskBaseViewModel c) => c.Title.Contains(keyword)) == true)
				{
					list3.Add(item);
					continue;
				}
				if (!string.IsNullOrEmpty(item.Id) && concurrentDictionary.ContainsKey(item.Id) && (concurrentDictionary[item.Id]?.Any((CommentModel c) => c.title.Contains(keyword)) ?? false))
				{
					list3.Add(item);
				}
				else
				{
					list4.Add(item);
				}
			}
			list = TaskDisplayService.GetSearchModels(list3, list4);
		}
		if (flag)
		{
			SourceModels = list;
			LoadSortedItems(loadMore: false);
		}
	}

	public void CheckOpenChangedIds(HashSet<string> ids, bool useTaskOpen)
	{
		List<DisplayItemModel> models = Items.ToList();
		List<DisplayItemModel> list = models.Where((DisplayItemModel m) => ids.Contains(m.TaskId)).ToList();
		if (list.Count == 0)
		{
			return;
		}
		HashSet<string> hashSet = null;
		if (!useTaskOpen)
		{
			hashSet = SmartListTaskFoldHelper.GetOpenedTaskIds(ProjectIdentity.CatId);
		}
		bool flag = false;
		foreach (DisplayItemModel item in list)
		{
			bool flag2 = (useTaskOpen ? item.SourceViewModel.IsOpen : hashSet.Contains(item.Id));
			if (item.IsOpen == flag2)
			{
				continue;
			}
			if (item.IsOpen)
			{
				item.GetChildrenModels(getAll: true).ForEach(delegate(DisplayItemModel m)
				{
					models.Remove(m);
				});
			}
			else
			{
				List<DisplayItemModel> childrenModels = item.GetChildrenModels(getAll: false, hashSet);
				int num = models.IndexOf(item);
				if (num >= 0)
				{
					for (int num2 = childrenModels.Count - 1; num2 >= 0; num2--)
					{
						models.Insert(num + 1, childrenModels[num2]);
					}
				}
			}
			flag = true;
			item.IsOpen = flag2;
		}
		if (flag)
		{
			ItemsSourceHelper.CopyTo(models, Items);
		}
	}

	public async Task OpenOrCloseAllSections(List<DisplayItemModel> sections, bool isOpen)
	{
		List<DisplayItemModel> models = Items.ToList();
		foreach (DisplayItemModel section in sections)
		{
			if (section.IsOpen == isOpen)
			{
				continue;
			}
			string name = section.Title;
			if (section.Section is AssigneeSection assigneeSection)
			{
				name = assigneeSection.Assingee;
			}
			if (InKanban)
			{
				if (isOpen)
				{
					await SectionStatusDao.OpenProjectSection(ProjectIdentity?.Id, name);
				}
				else
				{
					await SectionStatusDao.CloseProjectSection(ProjectIdentity?.Id, name);
				}
				continue;
			}
			section.IsOpen = isOpen;
			List<DisplayItemModel> list = new List<DisplayItemModel>();
			foreach (DisplayItemModel child in section.Section.Children)
			{
				list.Add(child);
				if (child.IsOpen && child.IsTask)
				{
					List<DisplayItemModel> childrenModels = child.GetChildrenModels(getAll: false);
					list.AddRange(childrenModels);
				}
			}
			if (isOpen)
			{
				SectionStatusDao.OpenProjectSection(ProjectIdentity?.Id, name);
				int num = models.IndexOf(section);
				if (num >= 0)
				{
					for (int i = 0; i < list.Count; i++)
					{
						models.Insert(num + 1 + i, list[i]);
					}
					if (section.Section?.SectionId == new CompletedSection().SectionId)
					{
						DisplayItemModel displayItemModel = DisplayItemModel.BuildLoadMore();
						displayItemModel.Section = section.Section;
						models.Add(displayItemModel);
					}
				}
			}
			else
			{
				SectionStatusDao.CloseProjectSection(ProjectIdentity?.Id, name);
				list.ForEach(delegate(DisplayItemModel child)
				{
					models.Remove(child);
				});
				if (section.Section?.SectionId == new CompletedSection().SectionId && models[models.Count - 1].Type == DisplayType.LoadMore)
				{
					models.RemoveAt(models.Count - 1);
				}
			}
		}
		if (!InKanban)
		{
			SetItems(models);
		}
	}

	public void OpenOrCloseSection(SectionStatus status, bool storeStatus)
	{
		List<DisplayItemModel> models = Items.ToList();
		DisplayItemModel displayItemModel = models.FirstOrDefault((DisplayItemModel item) => item.Type == DisplayType.Section && item.Section.SectionId == status.SectionId);
		if (displayItemModel == null)
		{
			return;
		}
		List<DisplayItemModel> list = new List<DisplayItemModel>();
		foreach (DisplayItemModel child in displayItemModel.Section.Children)
		{
			list.Add(child);
			if (child.IsOpen && child.IsTask)
			{
				List<DisplayItemModel> childrenModels = child.GetChildrenModels(getAll: false);
				list.AddRange(childrenModels);
			}
		}
		string name = displayItemModel.Title;
		if (displayItemModel.Section is AssigneeSection assigneeSection)
		{
			name = assigneeSection.Assingee;
		}
		if (status.IsOpen)
		{
			int num = models.IndexOf(displayItemModel);
			if (storeStatus)
			{
				SectionStatusDao.OpenProjectSection(ProjectIdentity.Id, name);
			}
			if (num >= 0)
			{
				DisplayItemModel displayItemModel2 = models[num];
				if (displayItemModel2 != null)
				{
					for (int num2 = 0; num2 < list.Count; num2++)
					{
						models.Insert(num + 1 + num2, list[num2]);
					}
					if (status.SectionId == new CompletedSection().SectionId)
					{
						DisplayItemModel displayItemModel3 = DisplayItemModel.BuildLoadMore();
						displayItemModel3.Section = displayItemModel2.Section;
						models.Add(displayItemModel3);
					}
				}
			}
		}
		else
		{
			if (storeStatus && !string.IsNullOrEmpty(ProjectIdentity.Id))
			{
				SectionStatusDao.CloseProjectSection(ProjectIdentity.Id, name);
			}
			list.ForEach(delegate(DisplayItemModel child)
			{
				models.Remove(child);
			});
			if (status.SectionId == new CompletedSection().SectionId && models[models.Count - 1].Type == DisplayType.LoadMore)
			{
				models.RemoveAt(models.Count - 1);
			}
		}
		if (status.IsOpen)
		{
			SetItems(models);
		}
		else
		{
			RemoveMissingItems(models);
		}
	}

	public void TempFoldModel(DisplayItemModel model, bool fold)
	{
		if (model == null)
		{
			return;
		}
		Items.Where((DisplayItemModel m) => m != null).ToList();
		if (fold && model.IsOpen)
		{
			model.GetChildrenModels(getAll: true).ForEach(delegate(DisplayItemModel m)
			{
				Items.Remove(m);
			});
		}
		else
		{
			if (fold || !model.IsOpen)
			{
				return;
			}
			List<DisplayItemModel> childrenModels = model.GetChildrenModels(getAll: false);
			int num = Items.IndexOf(model);
			if (num >= 0)
			{
				for (int num2 = childrenModels.Count - 1; num2 >= 0; num2--)
				{
					Items.Insert(num + 1, childrenModels[num2]);
				}
			}
		}
	}

	public void ChangeModelOpenStatus(DisplayItemModel model, bool useTaskOpen)
	{
		if (model == null)
		{
			return;
		}
		List<DisplayItemModel> models = Items.Where((DisplayItemModel m) => m != null).ToList();
		if (model.IsOpen)
		{
			model.GetChildrenModels(getAll: true).ForEach(delegate(DisplayItemModel m)
			{
				models.Remove(m);
			});
		}
		else
		{
			HashSet<string> openedTaskIds = null;
			if (!useTaskOpen)
			{
				openedTaskIds = SmartListTaskFoldHelper.GetOpenedTaskIds(ProjectIdentity.CatId);
			}
			List<DisplayItemModel> childrenModels = model.GetChildrenModels(getAll: false, openedTaskIds);
			int num = models.IndexOf(model);
			if (num >= 0)
			{
				for (int num2 = childrenModels.Count - 1; num2 >= 0; num2--)
				{
					models.Insert(num + 1, childrenModels[num2]);
				}
			}
		}
		model.IsOpen = !model.IsOpen;
		SetItems(models);
	}

	public List<DisplayItemModel> RemoveItemByIds(List<string> taskIds, string except = null)
	{
		List<DisplayItemModel> result = new List<DisplayItemModel>();
		if (Items.Any())
		{
			List<DisplayItemModel> models = Items.ToList();
			models.Where((DisplayItemModel i) => taskIds.Contains(i.Id) || (except == null && taskIds.Contains(i.TaskId))).ToList().ForEach(delegate(DisplayItemModel item)
			{
				DisplayItemModel displayItemModel = null;
				int num = models.IndexOf(item);
				if (num > 0)
				{
					displayItemModel = models.ElementAt(num - 1);
				}
				if (displayItemModel != null && item.Selected)
				{
					displayItemModel.LineVisible = true;
				}
				List<DisplayItemModel> childrenModels = item.GetChildrenModels(getAll: false);
				if (item.Id != except)
				{
					models.Remove(item);
					result.Add(item);
					RemoveItemInParent(item, models);
				}
				else if (item.Parent == null || item.Parent.IsSection)
				{
					result.Add(item);
				}
				if (childrenModels != null)
				{
					foreach (DisplayItemModel item2 in childrenModels)
					{
						models.Remove(item2);
						result.Add(item2);
					}
				}
			});
			SetItems(models);
		}
		return result;
	}

	private static void RemoveItemInParent(DisplayItemModel item, List<DisplayItemModel> models)
	{
		if (item.Parent != null)
		{
			item.Parent.RemoveItem(item);
			if (item.Parent.IsSection && !item.Parent.IsCustomizedSection && item.Parent.Section.Children.Count == 0)
			{
				models.Remove(item.Parent);
			}
		}
	}

	public void RemoveItemById(string id)
	{
		if (!Items.Any())
		{
			return;
		}
		List<DisplayItemModel> list = Items.ToList();
		DisplayItemModel displayItemModel = list.FirstOrDefault((DisplayItemModel i) => i.Id == id);
		if (displayItemModel == null)
		{
			return;
		}
		DisplayItemModel displayItemModel2 = null;
		int num = list.IndexOf(displayItemModel);
		if (num > 0)
		{
			displayItemModel2 = list.ElementAt(num - 1);
		}
		if (displayItemModel2 != null && displayItemModel.Selected)
		{
			displayItemModel2.LineVisible = true;
		}
		list.Remove(displayItemModel);
		RemoveItemInParent(displayItemModel, list);
		SourceModels.Remove(displayItemModel.SourceViewModel);
		if (LocalSettings.Settings.ShowSubtasks)
		{
			foreach (DisplayItemModel item in list.Where((DisplayItemModel model) => model.Type == DisplayType.CheckItem && model.TaskId == id).ToList())
			{
				list.Remove(item);
				RemoveItemInParent(item, list);
			}
		}
		RemoveMissingItems(list);
	}

	private void RemoveMissingItems(List<DisplayItemModel> models)
	{
		_loading = true;
		try
		{
			SetKanbanProperty(models);
			ItemsSourceHelper.RemoveMissing(models, Items);
		}
		finally
		{
			_loading = false;
		}
	}

	public void SetItems(List<DisplayItemModel> models)
	{
		ApplyItems(models, replaceCollection: false);
	}

	public void SetDetailItems(List<DisplayItemModel> models, bool restoreSelection)
	{
		models = models ?? new List<DisplayItemModel>();
		if (restoreSelection)
		{
			HashSet<string> selectedIds = new HashSet<string>(from item in GetItemsIncludingHiddenChildren()
				where item.Selected && !string.IsNullOrEmpty(item.Id)
				select item.Id, StringComparer.Ordinal);
			RestoreSelection(models, selectedIds, InBatchSelect);
		}
		bool replaceCollection = Math.Max(Items.Count, models.Count) > 200;
		ApplyItems(models, replaceCollection);
	}

	private void ApplyItems(List<DisplayItemModel> models, bool replaceCollection)
	{
		_loading = true;
		try
		{
			models = models ?? new List<DisplayItemModel>();
			if (SourceModels == null)
			{
				SourceModels = new List<TaskBaseViewModel>();
			}
			SetKanbanProperty(models);
			if (replaceCollection)
			{
				Items = new ObservableCollection<DisplayItemModel>(models);
				OnPropertyChanged("Items");
			}
			else
			{
				ItemsSourceHelper.CopyTo(models, Items);
			}
		}
		finally
		{
			_loading = false;
		}
	}

	private static void RestoreSelection(IEnumerable<DisplayItemModel> models, HashSet<string> selectedIds, bool inBatchSelect)
	{
		foreach (DisplayItemModel itemsIncludingHiddenChild in GetItemsIncludingHiddenChildren(models))
		{
			bool flag = (itemsIncludingHiddenChild.Selected = !string.IsNullOrEmpty(itemsIncludingHiddenChild.Id) && selectedIds.Contains(itemsIncludingHiddenChild.Id));
			itemsIncludingHiddenChild.InBatchSelected = flag && inBatchSelect;
		}
	}

	public DisplayItemModel GetSelectedItem()
	{
		return Items.FirstOrDefault((DisplayItemModel i) => i.Selected);
	}

	public void SetKanbanParent(KanbanViewModel kanban)
	{
		_kanban = kanban;
		CompletedTaskLoader = _kanban.CompletedTaskLoader;
	}

	public void CheckTaskChanged(TasksChangeEventArgs e)
	{
		if (e.PomoChangedIds.Any() && (SettingsHelper.GetShowDetail() || InKanban))
		{
			List<DisplayItemModel> list = Items.Where((DisplayItemModel m) => e.PomoChangedIds.Contains(m.Id)).ToList();
			if (list.Count > 0)
			{
				foreach (DisplayItemModel item in list)
				{
					item.LoadPomo();
				}
			}
		}
		if (e.AttachmentChangedIds.Any())
		{
			List<DisplayItemModel> list2 = Items.Where((DisplayItemModel m) => e.AttachmentChangedIds.Contains(m.Id)).ToList();
			if (list2.Count > 0)
			{
				foreach (DisplayItemModel item2 in list2)
				{
					item2.ShowAttachment = AttachmentCache.GetTaskExistAttachment(item2.Id);
					item2.NotifyShowIconsChanged();
				}
			}
		}
		if (e.DateChangedIds.Any())
		{
			List<DisplayItemModel> list3 = Items.Where((DisplayItemModel m) => e.DateChangedIds.Value.Contains(m.TaskId)).ToList();
			if (list3.Count > 0)
			{
				foreach (DisplayItemModel item3 in list3)
				{
					TaskItemLoadHelper.LoadShowReminder(item3);
				}
			}
		}
		if (e.TaskTextChangedIds.Any() && ProjectIdentity is SearchProjectIdentity)
		{
			CheckSearchMatched(e.TaskTextChangedIds.ToList());
		}
		if (e.TasksOpenChangedIds.Any())
		{
			ProjectIdentity projectIdentity = ProjectIdentity;
			if (projectIdentity is ColumnProjectIdentity columnProjectIdentity)
			{
				projectIdentity = columnProjectIdentity.Project;
			}
			bool useTaskOpen = projectIdentity is NormalProjectIdentity || projectIdentity is GroupProjectIdentity || projectIdentity is ParentTaskIdentity || projectIdentity is MatrixQuadrantIdentity || projectIdentity is CompletedProjectIdentity || projectIdentity is ClosedProjectIdentity;
			CheckOpenChangedIds(e.TasksOpenChangedIds.Value, useTaskOpen);
		}
	}

	public void SetForceReadOnly(bool forceReadOnly)
	{
		if (forceReadOnly != _forceReadOnly)
		{
			_forceReadOnly = forceReadOnly;
			Items?.ToList().ForEach(delegate(DisplayItemModel m)
			{
				m.SetForceReadOnly(_forceReadOnly);
			});
		}
	}

	public void Dispose()
	{
		SourceModels?.Clear();
		Items = new ObservableCollection<DisplayItemModel>();
		OnPropertyChanged("Items");
		BatchEditor = null;
		CompletedTaskLoader = null;
		_kanban = null;
	}

	public void Clear()
	{
		SourceModels?.Clear();
		Items = new ObservableCollection<DisplayItemModel>();
		OnPropertyChanged("Items");
	}

	public void SetBatchEditor(IBatchEditable editor)
	{
		if (BatchEditor == null)
		{
			editor.SetSelectedTaskIds(SelectedTaskIds);
		}
		BatchEditor = editor;
	}

	public bool IsAllSectionOpen()
	{
		return Items.ToList().All((DisplayItemModel m) => !m.IsSection || (m.IsSection && m.IsOpen));
	}

	public bool IsAllTaskOpen()
	{
		return Items.ToList().All((DisplayItemModel m) => !m.IsTask || (m.IsTask && (!m.HasChildren || m.IsOpen)));
	}

	public async Task OpenOrCloseAllTasksInKanban(bool isOpen)
	{
		List<DisplayItemModel> list = new List<DisplayItemModel>();
		foreach (DisplayItemModel item in Items.ToList())
		{
			if (item.IsTask && item.Level == 0 && item.HasChildren)
			{
				list.Add(item);
				list.AddRange(from c in item.GetChildrenModels(getAll: true)
					where c.HasChildren
					select c);
			}
		}
		if (list.Count > 0)
		{
			List<string> list2 = list.Select((DisplayItemModel m) => m.Id).ToList();
			if (ProjectIdentity is ColumnProjectIdentity columnProjectIdentity)
			{
				ProjectIdentity = columnProjectIdentity.Project;
			}
			if (ProjectIdentity is NormalProjectIdentity || ProjectIdentity is GroupProjectIdentity || ProjectIdentity is ParentTaskIdentity)
			{
				await TaskDao.FoldOrOpenTasks(list2, isOpen);
			}
			else
			{
				SmartListTaskFoldHelper.ResetStatus(ProjectIdentity.CatId, isOpen ? list2 : null);
			}
		}
	}

	public void ExitBatchSelect(string selectId = null)
	{
		SetSelectedTaskIds(string.IsNullOrEmpty(selectId) ? new List<string>() : new List<string> { selectId });
		InBatchSelect = false;
		FirstBatchSelectId = "";
		BatchEditor?.HideBatchSelectControl();
		foreach (DisplayItemModel item in Items)
		{
			SetItemProperties(item);
			if (item.IsOpen)
			{
				continue;
			}
			foreach (DisplayItemModel childrenModel in item.GetChildrenModels(getAll: true))
			{
				SetItemProperties(childrenModel);
			}
		}
		void SetItemProperties(DisplayItemModel item)
		{
			item.InBatchSelected = false;
			if (item.Selected)
			{
				if (!item.InKanban)
				{
					item.ShowTopMargin = true;
					item.ShowBottomMargin = true;
				}
				if (item.Id == selectId)
				{
					item.Selected = true;
					item.InBatchSelected = false;
				}
				else
				{
					item.Selected = false;
					item.InBatchSelected = false;
				}
			}
		}
	}

	public void SetFirstBatchSelectId(string id)
	{
		if (string.IsNullOrEmpty(FirstBatchSelectId))
		{
			FirstBatchSelectId = id;
		}
	}

	public int GetFirstBatchSelectIndex()
	{
		List<DisplayItemModel> list = Items.ToList();
		DisplayItemModel displayItemModel = list.FirstOrDefault((DisplayItemModel i) => i.Id == FirstBatchSelectId && i.Selected) ?? list.FirstOrDefault((DisplayItemModel i) => i.Selected);
		FirstBatchSelectId = displayItemModel?.Id;
		if (displayItemModel == null)
		{
			return -1;
		}
		return list.IndexOf(displayItemModel);
	}

	public void ClearBatchSelect()
	{
		ExitBatchSelect();
		OnBatchSelectChanged();
	}

	public List<DisplayItemModel> GetSelectedItems(bool forceBatch = false)
	{
		List<DisplayItemModel> selectedTasks = new List<DisplayItemModel>();
		List<DisplayItemModel> sections = new List<DisplayItemModel>();
		DisplayItemModel frontOne = null;
		foreach (DisplayItemModel item in Items)
		{
			SetItemProperties(item);
			if (item.IsOpen)
			{
				continue;
			}
			foreach (DisplayItemModel childrenModel in item.GetChildrenModels(getAll: true))
			{
				SetItemProperties(childrenModel);
			}
			if (!InKanban)
			{
				frontOne = (item.Selected ? item : null);
			}
		}
		if (frontOne != null)
		{
			frontOne.ShowBottomMargin = true;
		}
		bool inBatch = InBatchSelect || selectedTasks.Count > 1 || (forceBatch && selectedTasks.Count >= 1);
		foreach (DisplayItemModel item2 in selectedTasks)
		{
			item2.InBatchSelected = inBatch;
		}
		sections.ForEach(delegate(DisplayItemModel s)
		{
			s.InBatchSelected = inBatch;
		});
		return selectedTasks;
		void SetItemProperties(DisplayItemModel item)
		{
			if (item.IsSection)
			{
				sections.Add(item);
			}
			else if (item.Selected)
			{
				if (!InKanban)
				{
					if (frontOne == null)
					{
						item.ShowTopMargin = true;
					}
					else
					{
						item.ShowTopMargin = false;
						frontOne.ShowBottomMargin = false;
					}
					frontOne = item;
				}
				selectedTasks.Add(item);
			}
			else
			{
				if (!InKanban && frontOne != null)
				{
					frontOne.ShowBottomMargin = true;
					frontOne = null;
				}
				item.InBatchSelected = false;
			}
		}
	}

	public bool OnBatchSelectChanged()
	{
		List<DisplayItemModel> selectedItems = GetSelectedItems(forceBatch: true);
		bool flag = selectedItems.Count >= 1;
		bool flag2 = flag != InBatchSelect;
		selectedItems.ForEach(delegate(DisplayItemModel item)
		{
			item.InBatchSelected = item.Selected;
		});
		InBatchSelect = flag;
		List<string> list = selectedItems.Select((DisplayItemModel t) => t.Id).ToList();
		bool num = selectedItems.Count > 0 && (SelectedTaskIds == null || list.Count != SelectedTaskIds.Count || list.Union(SelectedTaskIds).Count() != SelectedTaskIds.Count);
		SetSelectedTaskIds(list);
		return num || flag2;
	}

	public int GetCurrentIndex(string taskId)
	{
		for (int i = 0; i < Items.Count; i++)
		{
			if (Items[i].Id == taskId)
			{
				return i;
			}
		}
		return -1;
	}

	public void CacheItems()
	{
		_cacheItems = Items.ToList();
	}

	public void LoadCacheItems()
	{
		if (_cacheItems == null || Items.Count != 0)
		{
			return;
		}
		foreach (DisplayItemModel cacheItem in _cacheItems)
		{
			Items.Add(cacheItem);
		}
	}

	public void CheckSelectedIds()
	{
		bool inBatchSelect = InBatchSelect;
		List<DisplayItemModel> list = GetSelectedItems();
		bool flag = InBatchSelect || list.Count > 1;
		if (flag)
		{
			foreach (DisplayItemModel item in list.Where((DisplayItemModel item) => !item.CanBatchSelect).ToList())
			{
				item.Selected = false;
				item.InBatchSelected = false;
			}
			list = list.Where((DisplayItemModel item) => item.CanBatchSelect).ToList();
		}
		InBatchSelect = flag && list.Count > 0;
		list = GetSelectedItems();
		if (InBatchSelect)
		{
			if (string.IsNullOrEmpty(FirstBatchSelectId) || list.All((DisplayItemModel item) => item.Id != FirstBatchSelectId))
			{
				FirstBatchSelectId = list[0].Id;
			}
		}
		else
		{
			FirstBatchSelectId = "";
		}
		List<string> selectedTaskIds = list.Select((DisplayItemModel t) => t.Id).ToList();
		SetSelectedTaskIds(selectedTaskIds);
		if (inBatchSelect && !InBatchSelect)
		{
			BatchEditor?.HideBatchSelectControl();
		}
	}
}
