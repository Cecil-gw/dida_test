using System;

namespace ticktick_WPF.ViewModels;

public class FilterDatePair
{
	public DateTime? Start;

	public DateTime? End;

	public bool isRepeat;

	public bool IsNoDate;

	public FilterDatePair(DateTime? start, DateTime? end)
	{
		Start = start;
		End = end;
	}

	public FilterDatePair(bool isNoDate)
	{
		IsNoDate = isNoDate;
		isRepeat = !isNoDate;
	}
}
