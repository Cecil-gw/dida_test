using System;

namespace ticktick_WPF.Models;

public class StatisticsModel
{
	public int todayPomoCount { get; set; }

	public int totalPomoCount { get; set; }

	public long todayPomoDuration { get; set; }

	public long totalPomoDuration { get; set; }

	public int Date { get; set; }

	public void Add(StatisticsModel extra)
	{
		if (extra != null)
		{
			todayPomoCount += extra.todayPomoCount;
			totalPomoCount += extra.totalPomoCount;
			todayPomoDuration += extra.todayPomoDuration;
			totalPomoDuration += extra.totalPomoDuration;
			NormalizeNonNegative();
		}
	}

	private void NormalizeNonNegative()
	{
		todayPomoCount = Math.Max(0, todayPomoCount);
		totalPomoCount = Math.Max(0, totalPomoCount);
		todayPomoDuration = Math.Max(0L, todayPomoDuration);
		totalPomoDuration = Math.Max(0L, totalPomoDuration);
	}

	public void ClearToday()
	{
		todayPomoCount = 0;
		todayPomoDuration = 0L;
	}
}
