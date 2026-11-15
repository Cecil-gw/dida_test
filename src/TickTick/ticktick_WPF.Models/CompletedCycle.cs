using System.Collections.Generic;

namespace ticktick_WPF.Models;

public class CompletedCycle
{
	public int StartDate;

	public int EndDate;

	public int TargetDays;

	public List<int> CheckinDays = new List<int>();

	public int Streak => CheckinDays.Count;

	public bool isComplete => Streak == TargetDays;
}
