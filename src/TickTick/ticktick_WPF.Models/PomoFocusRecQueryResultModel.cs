using System.Collections.Generic;

namespace ticktick_WPF.Models;

public class PomoFocusRecQueryResultModel
{
	public string pageToken { get; set; }

	public List<PomodoroModel> results { get; set; }
}
