using System.Collections.Generic;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.DateParser;

public interface IPaserDueDate
{
	List<string> GetRecognizeStrings();

	TimeData ToTimeData(bool addDefaultReminder);

	bool IsTimeSeted();
}
