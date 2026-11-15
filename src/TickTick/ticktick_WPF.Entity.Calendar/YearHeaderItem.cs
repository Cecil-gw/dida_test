using System;
using ticktick_WPF.Util;

namespace ticktick_WPF.Entity.Calendar;

internal class YearHeaderItem : YearViewItem
{
	public int Year { get; set; }

	public string Title { get; set; }

	public YearHeaderItem(int year)
	{
		Year = year;
		Title = DateUtils.FormatYear(new DateTime(year, 1, 1));
	}
}
