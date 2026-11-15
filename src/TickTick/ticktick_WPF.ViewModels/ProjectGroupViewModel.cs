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

public class ProjectGroupViewModel : ProjectItemViewModel
{
	public bool IsNew;

	private bool _editing;

	public ProjectGroupModel ProjectGroup { get; private set; }

	public bool Editing
	{
		get
		{
			return _editing;
		}
		set
		{
			_editing = true;
			OnPropertyChanged("Editing");
		}
	}

	public ProjectGroupViewModel(ProjectGroupModel itemGroup)
	{
		base.Id = itemGroup.id;
		ProjectGroup = itemGroup;
		base.Icon = Utils.GetGeometry(itemGroup.open ? "Ic.ProjectList.ExpandFolder" : "Ic.ProjectList.Folder");
		base.Title = itemGroup.name;
		base.SortOrder = itemGroup.sortOrder.GetValueOrDefault();
		base.Open = itemGroup.open;
		base.CanDrag = !TeamDao.IsTeamExpired(itemGroup.teamId);
		base.ViewMode = itemGroup.viewMode;
		base.ParallelViewMode = itemGroup.ParallelViewMode ?? "list";
		base.TeamId = itemGroup.teamId;
		string emojiIcon = EmojiHelper.GetEmojiIcon(itemGroup.name);
		base.IsPtfItem = true;
		if (!string.IsNullOrEmpty(emojiIcon) && itemGroup.name.StartsWith(emojiIcon))
		{
			_emojiText = emojiIcon;
		}
		base.IsGroupItem = true;
	}

	public override string GetSaveIdentity()
	{
		return "group:" + ProjectGroup.id;
	}

	public override async Task<IEnumerable<ContextAction>> GetContextActions()
	{
		string saveIdentity = GetSaveIdentity();
		bool windowShowing = !string.IsNullOrEmpty(saveIdentity) && TaskListWindow.Windows.ContainsKey(saveIdentity);
		List<ContextAction> list = new List<ContextAction>
		{
			new ContextAction(ContextActionKey.AddList),
			new ContextAction(ContextActionKey.Edit)
		};
		List<ContextAction> list2 = list;
		list2.Add(new ContextAction((await ProjectPinSortOrderService.CheckIsProjectPinned(ProjectGroup.id, 6)) ? ContextActionKey.Unpin : ContextActionKey.Pin));
		list.Add(new ContextAction(windowShowing ? ContextActionKey.CloseWindow : ContextActionKey.OpenNewWindow));
		list.Add(new ContextAction(ContextActionKey.Ungroup));
		return list;
	}

	public override async void LoadCount()
	{
		if (IsNew)
		{
			await Task.Delay(500);
		}
		base.Count = await Task.Run(async () => await TaskCountCache.TryGetCount(new GroupProjectIdentity(ProjectGroup)));
	}

	public override ProjectIdentity GetIdentity()
	{
		List<ProjectModel> projectsInGroup = CacheManager.GetProjectsInGroup(ProjectGroup.id);
		ProjectGroup = CacheManager.GetGroupById(ProjectGroup.id) ?? ProjectGroup;
		return new GroupProjectIdentity(ProjectGroup, projectsInGroup);
	}

	public override PtfType GetPtfType()
	{
		return PtfType.Project;
	}
}
