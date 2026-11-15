using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Service.SortOrder;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Views.Calendar;

namespace ticktick_WPF.ViewModels;

public class SmartProjectViewModel : ProjectItemViewModel, IDroppable
{
	public readonly SmartProject Project;

	public string ProjectId => string.Empty;

	public DateTime? DefaultDate => Project.DefaultDate;

	public bool IsCompleted => Project is CompletedProject;

	public bool IsAbandoned => Project is AbandonedProject;

	public List<string> Tags => new List<string>();

	public int Priority => 0;

	public bool Multiple => false;

	public bool IsDeleted => Project is TrashProject;

	public SmartProjectViewModel(SmartProject smartProject)
	{
		base.Id = smartProject.Id;
		base.Icon = smartProject.Icon;
		base.Title = smartProject.Name;
		base.Count = smartProject.Count;
		base.IconText = smartProject.IconText;
		Project = smartProject;
		base.CanDrag = !(smartProject is CompletedProject) && !(smartProject is TrashProject) && !(smartProject is AbandonedProject);
		base.CanDrop = smartProject is TodayProject || smartProject is TomorrowProject || smartProject is WeekProject || smartProject is CompletedProject || smartProject is AbandonedProject || smartProject is TrashProject;
		base.SortOrder = smartProject.SortOrder;
		base.ShowCount = !(smartProject is CompletedProject) && !(smartProject is AbandonedProject) && !(smartProject is TrashProject);
		ListType = ProjectListType.Smart;
		base.ViewMode = SmartProjectService.GetSmartProjectViewMode(smartProject.Id);
		base.ParallelViewMode = SmartProjectService.GetSmartProjectViewMode(smartProject.Id, inParallel: true);
	}

	public override async Task<IEnumerable<ContextAction>> GetContextActions()
	{
		if (Project is SummaryProject)
		{
			return new List<ContextAction>
			{
				new ContextAction(ContextActionKey.Hide)
			};
		}
		if (Project is InboxProject)
		{
			return null;
		}
		bool withAuto = false;
		bool auto = false;
		bool flag = false;
		bool flag2 = false;
		if (Project != null)
		{
			SmartProject project = Project;
			if (!(project is TodayProject))
			{
				if (!(project is TomorrowProject))
				{
					if (!(project is WeekProject))
					{
						if (!(project is AssignProject))
						{
							if (project is AbandonedProject)
							{
								auto = IsProjectAuto("SmartListAbandoned");
								withAuto = true;
							}
						}
						else
						{
							auto = IsProjectAuto("SmartListForMe");
							withAuto = true;
						}
					}
					else
					{
						auto = IsProjectAuto("SmartList7Day");
						withAuto = true;
					}
				}
				else
				{
					auto = IsProjectAuto("SmartListTomorrow");
					withAuto = true;
				}
			}
			else
			{
				auto = IsProjectAuto("SmartListToday");
				withAuto = true;
			}
			project = Project;
			if (project is AbandonedProject || project is CompletedProject || project is TrashProject)
			{
				flag2 = await ProjectPinSortOrderService.CheckIsProjectPinned(Project.UserEventId, 11);
				flag = true;
			}
			List<ContextAction> list = new List<ContextAction>();
			List<ContextAction> list2 = new List<ContextAction>();
			if (flag)
			{
				list.Add(flag2 ? new ContextAction(ContextActionKey.Unpin) : new ContextAction(ContextActionKey.Pin));
			}
			list.Add(new ContextAction(ContextActionKey.DisplayOption)
			{
				SubActions = list2
			});
			list2.Add(new ContextAction(ContextActionKey.Show)
			{
				Selected = (!withAuto || !auto)
			});
			if (withAuto)
			{
				list2.Add(new ContextAction(ContextActionKey.ShowIfNotEmpty)
				{
					Selected = auto
				});
			}
			list2.Add(new ContextAction(ContextActionKey.Hide));
			string saveIdentity = GetSaveIdentity();
			bool flag3 = !string.IsNullOrEmpty(saveIdentity) && TaskListWindow.Windows.ContainsKey(saveIdentity);
			list.Add(new ContextAction(flag3 ? ContextActionKey.CloseWindow : ContextActionKey.OpenNewWindow));
			return list;
		}
		return null;
	}

	protected static bool IsProjectAuto(string key)
	{
		return (int)LocalSettings.Settings[key] == 2;
	}

	public override string GetSaveIdentity()
	{
		return "smart:" + Project.Id;
	}

	public override ProjectIdentity GetIdentity()
	{
		return SmartProjectIdentity.BuildSmartProject(Project.Id);
	}

	public static SmartProjectViewModel BuildModel(SmartListType smartType)
	{
		return smartType switch
		{
			SmartListType.All => new SmartProjectViewModel(new AllProject()), 
			SmartListType.Today => new SmartProjectViewModel(new TodayProject()), 
			SmartListType.Tomorrow => new SmartProjectViewModel(new TomorrowProject()), 
			SmartListType.Week => new SmartProjectViewModel(new WeekProject()), 
			SmartListType.Assign => new SmartProjectViewModel(new AssignProject()), 
			SmartListType.Completed => new SmartProjectViewModel(new CompletedProject()), 
			SmartListType.Abandoned => new SmartProjectViewModel(new AbandonedProject()), 
			SmartListType.Trash => new SmartProjectViewModel(new TrashProject()), 
			SmartListType.Summary => new SmartProjectViewModel(new SummaryProject()), 
			SmartListType.Inbox => new InboxProjectViewModel(new InboxProject()), 
			_ => null, 
		};
	}

	public override async void LoadCount()
	{
		base.Count = await Task.Run(async delegate
		{
			SmartProjectIdentity smartList = SmartProjectIdentity.BuildSmartProject(base.Id);
			if (smartList is TodayProjectIdentity || smartList is TomorrowProjectIdentity || smartList is WeekProjectIdentity)
			{
				await ProjectAndTaskIdsCache.ResetProjectTaskIds(smartList);
			}
			return await TaskCountCache.TryGetCount(smartList);
		});
	}
}
