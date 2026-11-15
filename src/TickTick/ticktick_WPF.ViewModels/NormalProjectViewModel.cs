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

public class NormalProjectViewModel : ProjectItemViewModel, IDroppable
{
	public ProjectModel Project { get; private set; }

	public string ProjectId => Project.id;

	public DateTime? DefaultDate => null;

	public bool IsCompleted => false;

	public bool IsAbandoned => false;

	public bool IsDeleted => false;

	public List<string> Tags => new List<string>();

	public int Priority => 0;

	public bool Multiple => false;

	public NormalProjectViewModel()
	{
	}

	public NormalProjectViewModel(ProjectModel project)
	{
		base.Id = project.id;
		Project = project;
		base.Color = project.color;
		bool flag = project.IsShareList();
		if (project.IsNote)
		{
			base.Icon = Utils.GetGeometry(flag ? "Ic.ProjectList.NoteMember" : "Ic.ProjectList.Note");
		}
		else
		{
			base.Icon = Utils.GetGeometry(flag ? "Ic.ProjectList.SharedList" : "Ic.ProjectList.List");
		}
		base.Title = project.name;
		base.SortOrder = project.sortOrder;
		base.CanDrag = !TeamDao.IsTeamExpired(project.teamId);
		base.CanDrop = base.CanDrag && project.IsEnable();
		base.TeamId = CacheManager.GetTeamById(project.teamId)?.id;
		base.IsProjectPermit = project.IsProjectPermit();
		base.IsPtfItem = true;
		base.ViewMode = project.viewMode;
		base.ParallelViewMode = project.ParallelViewMode ?? "list";
		ListType = ProjectListType.Project;
		string emojiIcon = EmojiHelper.GetEmojiIcon(project.name);
		if (!string.IsNullOrEmpty(emojiIcon) && project.name.StartsWith(emojiIcon))
		{
			_emojiText = emojiIcon;
		}
		if (project.IsOverLimit())
		{
			base.InfoIcon = Utils.GetGeometry("Ic.Common.Warning");
			base.Count = 0;
			base.InfoIconBrush = ThemeUtil.GetColorInString("#FFB000");
			base.Info = Utils.GetString("UnsyncedLists");
		}
	}

	public override async Task<IEnumerable<ContextAction>> GetContextActions()
	{
		string saveIdentity = GetSaveIdentity();
		bool windowShowing = !string.IsNullOrEmpty(saveIdentity) && TaskListWindow.Windows.ContainsKey(saveIdentity);
		bool flag = Project.closed.HasValue && Project.closed.Value;
		ContextAction exitOrDelete = new ContextAction((!Project.IsShareList() || Project.isOwner) ? ContextActionKey.Delete : ContextActionKey.Exit);
		ContextAction openOrClose = new ContextAction(flag ? ContextActionKey.UnArchiveList : ContextActionKey.Archive);
		List<ContextAction> list;
		if (flag)
		{
			list = new List<ContextAction>
			{
				openOrClose,
				new ContextAction(ContextActionKey.Edit),
				exitOrDelete
			};
			if (base.IsProjectPermit)
			{
				list.Insert(1, new ContextAction(ContextActionKey.Duplicate));
			}
		}
		else
		{
			try
			{
				List<ContextAction> list2 = new List<ContextAction>
				{
					new ContextAction(ContextActionKey.Edit)
				};
				List<ContextAction> list3 = list2;
				list3.Add(new ContextAction((await ProjectPinSortOrderService.CheckIsProjectPinned(Project.id, 5)) ? ContextActionKey.Unpin : ContextActionKey.Pin));
				list2.Add(new ContextAction(ContextActionKey.Share));
				list2.Add(new ContextAction(windowShowing ? ContextActionKey.CloseWindow : ContextActionKey.OpenNewWindow));
				list2.Add(openOrClose);
				list2.Add(exitOrDelete);
				list = list2;
				if (base.IsProjectPermit)
				{
					list.Insert(2, new ContextAction(ContextActionKey.Duplicate));
				}
			}
			catch (Exception)
			{
				list = new List<ContextAction>();
			}
		}
		return list;
	}

	public override string GetSaveIdentity()
	{
		return "project:" + Project.id;
	}

	public override ProjectIdentity GetIdentity()
	{
		Project = CacheManager.GetProjectById(Project.id) ?? Project;
		return new NormalProjectIdentity(Project);
	}

	public override async void LoadCount()
	{
		base.Count = await Task.Run(async () => await TaskCountCache.TryGetCount(new NormalProjectIdentity(Project)));
	}

	public override PtfType GetPtfType()
	{
		return PtfType.Project;
	}
}
