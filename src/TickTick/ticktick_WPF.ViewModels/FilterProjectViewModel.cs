using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service.SortOrder;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Views.Calendar;

namespace ticktick_WPF.ViewModels;

public class FilterProjectViewModel : ProjectItemViewModel, IDroppable
{
	private readonly FilterTaskDefault _filterTaskDefault;

	public FilterModel Filter { get; private set; }

	public string ProjectId => _filterTaskDefault?.ProjectModel?.id;

	public DateTime? DefaultDate => _filterTaskDefault?.DefaultDate;

	public bool IsCompleted => false;

	public bool IsAbandoned => false;

	public bool IsDeleted => false;

	public List<string> Tags => _filterTaskDefault?.DefaultTags;

	public int Priority => _filterTaskDefault?.Priority ?? TaskDefaultDao.GetDefaultSafely().Priority;

	public bool Multiple => true;

	public FilterProjectViewModel(FilterModel filter)
	{
		base.Id = filter.id;
		Filter = filter;
		base.Icon = Utils.GetGeometry("Ic.ProjectList.Filter");
		base.Title = filter.name;
		base.IsPtfItem = true;
		base.CanDrag = true;
		base.CanDrop = false;
		base.ViewMode = filter.viewMode;
		base.ParallelViewMode = filter.ParallelViewMode ?? "list";
		ListType = ProjectListType.Filter;
		_filterTaskDefault = FilterViewModel.CalculateTaskDefault(filter.rule, withEmptyProject: true);
		string emojiIcon = EmojiHelper.GetEmojiIcon(filter.name);
		if (!string.IsNullOrEmpty(emojiIcon) && filter.name.StartsWith(emojiIcon))
		{
			_emojiText = emojiIcon;
		}
	}

	public override async Task<IEnumerable<ContextAction>> GetContextActions()
	{
		string saveIdentity = GetSaveIdentity();
		bool windowShowing = !string.IsNullOrEmpty(saveIdentity) && TaskListWindow.Windows.ContainsKey(saveIdentity);
		List<ContextAction> list = new List<ContextAction>
		{
			new ContextAction(ContextActionKey.Edit)
		};
		List<ContextAction> list2 = list;
		list2.Add(new ContextAction((await ProjectPinSortOrderService.CheckIsProjectPinned(Filter.id, 8)) ? ContextActionKey.Unpin : ContextActionKey.Pin));
		list.Add(new ContextAction(ContextActionKey.Duplicate));
		list.Add(new ContextAction(windowShowing ? ContextActionKey.CloseWindow : ContextActionKey.OpenNewWindow));
		list.Add(new ContextAction(ContextActionKey.Delete));
		return list;
	}

	public override string GetSaveIdentity()
	{
		return "filter:" + Filter.id;
	}

	public override async void LoadCount()
	{
		base.Count = TaskCountCache.TryGetCount(Filter.id);
		await Task.Delay(new Random().Next(1000, 3000));
		Task.Run(async () => await TaskCountCache.TryGetCount(new FilterProjectIdentity(Filter)));
		await Task.Delay(3000);
		base.Count = TaskCountCache.TryGetCount(Filter.id);
	}

	public override ProjectIdentity GetIdentity()
	{
		Filter = CacheManager.GetFilterById(Filter.id) ?? Filter;
		return new FilterProjectIdentity(Filter);
	}

	public override PtfType GetPtfType()
	{
		return PtfType.Filter;
	}
}
