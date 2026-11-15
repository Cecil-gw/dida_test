using System;
using ticktick_WPF.Service;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.Provider;

public class WeekProjectIdentity : SmartProjectIdentity
{
	public override string Id => "_special_id_week";

	public override string CatId => "n7ds";

	public WeekProjectIdentity()
	{
		base.SortOption = SmartProjectService.GetSmartProjectSortOption("week", inKanban: false);
	}

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

	public override string GetDisplayTitle()
	{
		return Utils.GetString("Next7Day");
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
