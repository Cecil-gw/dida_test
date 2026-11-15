using System;
using ticktick_WPF.Service;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.Provider;

public class TomorrowProjectIdentity : SmartProjectIdentity
{
	public override string Id => "_special_id_tomorrow";

	public override string CatId => "tomorrow";

	public override TimeData GetTimeData()
	{
		return new TimeData
		{
			StartDate = DateTime.Today.AddDays(1.0),
			IsAllDay = true,
			IsDefault = true,
			Reminders = TimeData.GetDefaultAllDayReminders()
		};
	}

	public TomorrowProjectIdentity()
	{
		base.SortOption = SmartProjectService.GetSmartProjectSortOption("tomorrow", inKanban: false);
	}

	public override string GetDisplayTitle()
	{
		return Utils.GetString("Tomorrow");
	}

	public override ProjectIdentity Copy(ProjectIdentity project)
	{
		return ProjectIdentity.CreateSmartIdentity(project.Id);
	}
}
