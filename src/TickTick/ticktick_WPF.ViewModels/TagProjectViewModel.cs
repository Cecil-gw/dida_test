using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Service.SortOrder;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Views.Calendar;

namespace ticktick_WPF.ViewModels;

public class TagProjectViewModel : ProjectItemViewModel, IDroppable
{
	public TagModel TagModel;

	public bool IsParent;

	public bool IsNew;

	public string ProjectId => string.Empty;

	public DateTime? DefaultDate => null;

	public bool IsCompleted => false;

	public bool IsAbandoned => false;

	public bool IsDeleted => false;

	public List<string> Tags => new List<string> { TagModel.name };

	public int Priority => 0;

	public bool Multiple => false;

	public TagProjectViewModel(TagModel tag)
	{
		UpdateTagModel(tag);
		base.CanDrag = true;
		base.CanDrop = true;
		base.Open = !tag.collapsed;
		base.IsSubItem = tag.IsChild();
		base.IsPtfItem = true;
		IsParent = tag.IsParent();
		base.IsGroupItem = IsParent;
		ListType = ProjectListType.Tag;
		base.ViewMode = tag.viewMode;
		base.ParallelViewMode = tag.ParallelViewMode ?? "list";
	}

	public void UpdateTagModel(TagModel tag)
	{
		base.Id = tag.name;
		base.Icon = Utils.GetGeometry("Ic.ProjectList.Tag");
		TagModel = tag;
		_emojiText = tag.GetDisplayEmoji();
		base.Title = tag.GetDisplayName();
		base.Color = tag.color;
	}

	public override async Task<IEnumerable<ContextAction>> GetContextActions()
	{
		int count = (IsParent ? (from t in CacheManager.GetTags()
			where !t.IsChild()
			select t).ToList().Count : CacheManager.GetTags().Count);
		string saveIdentity = GetSaveIdentity();
		bool windowShowing = !string.IsNullOrEmpty(saveIdentity) && TaskListWindow.Windows.ContainsKey(saveIdentity);
		List<ContextAction> list = new List<ContextAction>
		{
			new ContextAction(ContextActionKey.Edit)
		};
		List<ContextAction> list2 = list;
		list2.Add(new ContextAction((await ProjectPinSortOrderService.CheckIsProjectPinned(TagModel.name, 7)) ? ContextActionKey.Unpin : ContextActionKey.Pin));
		list.Add(new ContextAction(windowShowing ? ContextActionKey.CloseWindow : ContextActionKey.OpenNewWindow));
		list.Add(new ContextAction(ContextActionKey.Delete));
		List<ContextAction> list3 = list;
		if (TagService.ExistShareTag())
		{
			TagModel tagModel = TagModel;
			list3.Insert(2, (tagModel != null && tagModel.type == 2) ? new ContextAction(ContextActionKey.ToPersonalTag) : new ContextAction(ContextActionKey.ToShareTag));
		}
		if (count > 1)
		{
			list3.Insert(2, new ContextAction(ContextActionKey.MergeTags));
		}
		if (!base.IsSubItem)
		{
			list3.Insert(2, new ContextAction(ContextActionKey.AddSubTag));
		}
		return list3;
	}

	public override string GetSaveIdentity()
	{
		return "tag:" + TagModel.name;
	}

	public override async void LoadCount()
	{
		if (!IsNew)
		{
			base.Count = await Task.Run(async () => await TaskCountCache.TryGetCount(new TagProjectIdentity(TagModel)));
		}
	}

	public override ProjectIdentity GetIdentity()
	{
		TagModel = CacheManager.GetTagByName(base.Id) ?? TagModel;
		return new TagProjectIdentity(TagModel);
	}

	public override PtfType GetPtfType()
	{
		return PtfType.Tag;
	}
}
