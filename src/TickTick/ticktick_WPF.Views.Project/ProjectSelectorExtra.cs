using System.Collections.Generic;

namespace ticktick_WPF.Views.Project;

public class ProjectSelectorExtra
{
	public bool showAll = true;

	public bool showAllProjectCategory;

	public bool showFilters;

	public bool showTags;

	public bool showSmartProjects;

	public bool showCalendars;

	public bool showProjects = true;

	public bool batchMode = true;

	public bool isCalendarMode;

	public string accountId = "";

	public bool canSelectGroup = true;

	public bool onlyShowPermission;

	public bool showSmartAll;

	public bool showFilterGroup = true;

	public bool showNoteProject = true;

	public bool showSharedProject = true;

	public bool showListGroup;

	public bool showCalendarCategory;

	public bool showHabitCategory;

	public bool useCircleCheckForAll;

	public bool useCircleCheckForSectionGroups;

	public double popupWidth = 215.0;

	public bool CanSearch;

	public bool canExpandAllList = true;

	public bool canExpandAllCalendars = true;

	public bool highlightAllListChildren = true;

	public bool ShowColumn;

	public string ColumnId;

	public bool? ShowInbox;

	public bool IgnoreMiniHeight;

	public List<string> LinkedProjectIds = new List<string>();
}
