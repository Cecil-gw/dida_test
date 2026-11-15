using System.Collections.Generic;

namespace ticktick_WPF.Util;

public class PriorityUtils
{
	public static Dictionary<string, int> Priorities = new Dictionary<string, int>
	{
		{
			Utils.GetString("PriorityHigh"),
			5
		},
		{
			Utils.GetString("PriorityMedium"),
			3
		},
		{
			Utils.GetString("PriorityLow"),
			1
		},
		{
			Utils.GetString("PriorityNull"),
			0
		}
	};
}
