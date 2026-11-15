using System;

namespace ticktick_WPF.Util.Provider;

public class CreateTimeSection
{
	public Section Today = new TodaySection(createTimeGroup: true)
	{
		Ordinal = 0L
	};

	public Section Yesterday = new YesterdaySection
	{
		Ordinal = 1L
	};

	public Section Week = new CreateTimePeriodSection(Utils.GetString("Previous7Days"), 2, DateTime.Today.AddDays(-2.0), DateUtils.GetCreateTimeSectionEntityId(DateUtils.CreateTimeCategory.LastWeek));

	public Section Month = new CreateTimePeriodSection(Utils.GetString("Previous30Days"), 3, DateTime.Today.AddDays(-8.0), DateUtils.GetCreateTimeSectionEntityId(DateUtils.CreateTimeCategory.LastMonth));

	public Section Early = new CreateTimePeriodSection(Utils.GetString("Earlier"), 4, DateTime.Now.AddDays(-31.0).Date, DateUtils.GetCreateTimeSectionEntityId(DateUtils.CreateTimeCategory.Earlier));
}
