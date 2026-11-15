using System;
using System.Collections.Generic;

namespace ticktick_WPF.ViewModels;

public class DefaultDateModel
{
	public DateTime? DefaultDate;

	public string Key;

	public int Priority;

	public static List<DefaultDateModel> GetDefaultDateMap()
	{
		return new List<DefaultDateModel>
		{
			new DefaultDateModel
			{
				Key = "nodue",
				Priority = 56,
				DefaultDate = null
			},
			new DefaultDateModel
			{
				Key = "today",
				Priority = 0,
				DefaultDate = DateTime.Today
			},
			new DefaultDateModel
			{
				Key = "thisweek",
				Priority = 0,
				DefaultDate = DateTime.Today
			},
			new DefaultDateModel
			{
				Key = "thismonth",
				Priority = 0,
				DefaultDate = DateTime.Today
			},
			new DefaultDateModel
			{
				Key = "tomorrow",
				Priority = 1,
				DefaultDate = DateTime.Today.AddDays(1.0)
			},
			new DefaultDateModel
			{
				Key = "overdue",
				Priority = -1,
				DefaultDate = DateTime.Today.AddDays(-1.0)
			}
		};
	}
}
