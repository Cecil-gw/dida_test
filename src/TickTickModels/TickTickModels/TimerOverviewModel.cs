using System;
using Newtonsoft.Json;

namespace TickTickModels;

public class TimerOverviewModel
{
	[JsonProperty("days")]
	public int Days { get; set; }

	[JsonProperty("today")]
	public long Today { get; set; }

	[JsonProperty("Total")]
	public long Total { get; set; }

	public int Date { get; set; }

	public void Add(TimerOverviewModel extra)
	{
		if (extra != null)
		{
			if (extra.Today > 0 && Today == 0L)
			{
				Days++;
			}
			Today += extra.Today;
			Total += extra.Total;
		}
	}

	public void Cut(TimerOverviewModel cut)
	{
		if (cut != null)
		{
			long today = Today;
			Today = Math.Max(0L, Today - cut.Today);
			Total -= cut.Total;
			if (today > 0 && Today == 0L)
			{
				Days--;
			}
		}
	}
}
