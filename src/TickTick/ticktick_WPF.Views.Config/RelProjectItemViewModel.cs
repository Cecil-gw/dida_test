using TickTickModels;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Config;

public class RelProjectItemViewModel : BaseViewModel
{
	public CalendarContentProject Project;

	private string _calendarName;

	private string _projectName;

	private string _message;

	public bool ListValid { get; set; } = true;

	public bool CalendarValid { get; set; } = true;

	public bool CanEdit { get; set; }

	public string CalendarName
	{
		get
		{
			return _calendarName;
		}
		set
		{
			_calendarName = value;
			OnPropertyChanged("CalendarName");
		}
	}

	public string ProjectName
	{
		get
		{
			return _projectName;
		}
		set
		{
			_projectName = value;
			OnPropertyChanged("ProjectName");
		}
	}

	public string Message
	{
		get
		{
			return _message;
		}
		set
		{
			_message = value;
			OnPropertyChanged("Message");
		}
	}

	public RelProjectItemViewModel(CalendarContentCalendar calendar, CalendarContentProject project)
	{
		SetProperties(calendar, project);
	}

	public void SetProperties(CalendarContentCalendar calendar, CalendarContentProject project)
	{
		Project = project;
		if (calendar != null)
		{
			CalendarName = calendar.name;
		}
		else
		{
			CalendarName = Utils.GetString("InvalidCalendar");
			CalendarValid = false;
		}
		bool flag = project.syncProjectScope == "all";
		if (flag)
		{
			ProjectName = Utils.GetString("All");
		}
		else
		{
			ProjectModel projectById = CacheManager.GetProjectById(project.syncProjectScope);
			if (projectById != null && !projectById.delete_status)
			{
				ProjectName = projectById.name;
			}
			else
			{
				ProjectName = Utils.GetString("InvalidList");
				ListValid = false;
			}
		}
		CanEdit = ListValid && CalendarValid;
		string arg = CacheManager.GetProjectById(flag ? project.syncProjectId : project.syncProjectScope)?.name;
		Message = string.Format(Utils.GetString(flag ? "RelAllProjectMessage" : "RelProjectMessage"), arg, CalendarName, Utils.GetAppName());
	}
}
