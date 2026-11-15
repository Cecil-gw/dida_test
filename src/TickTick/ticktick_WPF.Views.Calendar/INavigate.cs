using System;
using System.Collections.Generic;

namespace ticktick_WPF.Views.Calendar;

public interface INavigate
{
	DateTime Today();

	DateTime Next();

	DateTime Last();

	DateTime GoTo(DateTime dateTime, bool checkStart = true, bool scrollToNow = false, bool delay = false);

	(DateTime, DateTime) GetTimeSpan();

	(DateTime, DateTime) GetTaskTimeSpan();

	void Reload(bool byThemeChange = false);

	void ShowAddWindow(DateTime dateTime);

	int ExistTasks(HashSet<string> taskIds);

	void RemoveItem(CalendarDisplayViewModel model);

	void LoadVirtualModels();
}
