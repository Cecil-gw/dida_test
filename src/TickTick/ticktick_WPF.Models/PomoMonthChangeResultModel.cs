using System.Collections.Generic;

namespace ticktick_WPF.Models;

public class PomoMonthChangeResultModel
{
	public long checkPoint { get; set; }

	public List<PomoMonthResultModel> pomodoroRecords { get; set; }
}
