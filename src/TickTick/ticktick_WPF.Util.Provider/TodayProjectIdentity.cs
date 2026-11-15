using System;
using ticktick_WPF.Service;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.Provider;

public class TodayProjectIdentity : SmartProjectIdentity
{
	public override string Id => "_special_id_today";

	public override string CatId => "today";

	public override TimeData GetTimeData()
	{
		return new TimeData
		{
			StartDate = DateTime.Today,
			IsAllDay = true,
			IsDefault = true,
			Reminders = TimeData.GetDefaultAllDayReminders()
		};
	}

	public TodayProjectIdentity()
	{
		base.SortOption = SmartProjectService.GetSmartProjectSortOption("today", inKanban: false);
	}

	public override string GetDisplayTitle()
	{
		return Utils.GetString("Today");
	}

	public override ProjectIdentity Copy(ProjectIdentity project)
	{
		return ProjectIdentity.CreateSmartIdentity(project.Id);
	}

	public override DateTime? GetCompletedFromTime()
	{
		return DateTime.Today;
	}
}
