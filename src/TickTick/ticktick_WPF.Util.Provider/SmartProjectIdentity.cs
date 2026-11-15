using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Service;
using ticktick_WPF.Util.Sort;
using ticktick_WPF.Views.TaskList;

namespace ticktick_WPF.Util.Provider;

public class SmartProjectIdentity : ProjectIdentity
{
	public override string ViewMode => SmartProjectService.GetSmartProjectViewMode(Id) ?? "list";

	public override string ParallelViewMode => SmartProjectService.GetSmartProjectViewMode(Id, inParallel: true) ?? "list";

	public static SmartProjectIdentity BuildSmartProject(string smartId)
	{
		return smartId switch
		{
			"_special_id_all" => new AllProjectIdentity(), 
			"_special_id_today" => new TodayProjectIdentity(), 
			"_special_id_tomorrow" => new TomorrowProjectIdentity(), 
			"_special_id_week" => new WeekProjectIdentity(), 
			"_special_id_assigned" => new AssignToMeProjectIdentity(), 
			"_special_id_completed" => new CompletedProjectIdentity(), 
			"_special_id_abandoned" => new AbandonedProjectIdentity(), 
			"_special_id_trash" => new TrashProjectIdentity(), 
			"_special_id_summary" => new SummaryProjectIdentity(), 
			_ => null, 
		};
	}

	public override async Task SwitchViewMode(string viewMode, bool isParallel = false)
	{
		if ((!(Id == "_special_id_today") && !(Id == "_special_id_tomorrow") && !(Id == "_special_id_week")) || !(viewMode == "timeline"))
		{
			SmartProjectService.SaveSmartProjectViewMode(Id, viewMode, isParallel);
			DataChangedNotifier.NotifyProjectViewModeChanged(BuildSmartProject(Id));
		}
	}

	public override List<string> GetSwitchViewModes()
	{
		switch (Id)
		{
		case "_special_id_today":
		case "_special_id_tomorrow":
		case "_special_id_week":
			return new List<string> { "list", "kanban" };
		default:
			return new List<string> { "list", "kanban", "timeline" };
		}
	}

	public override TimelineModel GetTimelineModel()
	{
		return SmartProjectService.GetSmartProjectTimeline(Id).Copy();
	}

	public override void CommitTimeline(TimelineModel model)
	{
		if (model != null)
		{
			if (model.SortType == "priority")
			{
				model.SortType = "project";
			}
			SmartProjectService.SaveSmartProjectTimeline(Id, model);
		}
	}

	public override List<SortTypeViewModel> GetTimelineSortTypes()
	{
		return SortOptionHelper.GetSmartProjectSortTypeModels(isNote: false, inTimeline: true);
	}

	public override Geometry GetProjectIcon()
	{
		return SpecialListUtils.GetIconBySmartType(SpecialListUtils.GetSmartTypeById(Id));
	}
}
