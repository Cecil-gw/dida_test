using System;

namespace ticktick_WPF.Util.Provider;

public class DateSection
{
	public Section Later = new LaterSection(Utils.GetString("later"), 1, DateTime.Now.AddDays(8.0).Date, "later");

	public Section Nodate = new NodateSection(Utils.GetString("NoDate"), 0, null, "nodate");

	public Section Outdated = new OutdatedSection
	{
		Ordinal = 8L
	};

	public Section Today = new TodaySection
	{
		Ordinal = 4L
	};

	public Section Tomorrow = new TomorrowSection
	{
		Ordinal = 3L
	};

	public Section Week = new WeekSection(Utils.GetString("Next7Day"), 2, DateTime.Now.AddDays(2.0).Date, "week");
}
