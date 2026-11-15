using System.Collections.Generic;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.QuickAdd;

public interface IProjectTaskDefault
{
	string GetProjectId();

	string GetProjectName();

	string GetColumnId();

	TimeData GetTimeData();

	int GetPriority();

	List<string> GetTags();

	bool UseDefaultTags();

	bool IsCalendar();

	string GetAccountId();

	bool GetIsNote();

	string GetAssignee();

	bool GetForceShowProjectName();
}
