using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Search;

namespace ticktick_WPF.Views.Completed;

public class ClosedFilterViewModel : BaseViewModel
{
	private ProjectOrGroupFilter _projectOrGroupFilter;

	private DateFilter _dateFilter;

	private List<string> _selectedProjectGroupIds = new List<string>();

	private List<string> _selectedProjectIds = new List<string>();

	private DateTime? _startDate;

	private DateTime? _endDate;

	private string _selectedProjectDisplayText = Utils.GetString("AllList");

	private string _selectedStatusDisplayText = Utils.GetString("AllStatus");

	private string _selectedPriorityDisplayText = Utils.GetString("all_priorities");

	private bool _showAll = true;

	public bool IsCompleted = true;

	public string PersonalOrAllText => Utils.GetString((!ShowAll) ? (IsCompleted ? "CompletedbyMe" : "WontDobyMe") : (IsCompleted ? "AllCompleted" : "AllWontDo"));

	public bool ShowAll
	{
		get
		{
			return _showAll;
		}
		set
		{
			_showAll = value;
			OnPropertyChanged("PersonalOrAllText");
		}
	}

	public List<string> SelectedProjectIds
	{
		get
		{
			return _selectedProjectIds;
		}
		set
		{
			_selectedProjectIds = value;
			OnPropertyChanged("SelectedProjectIds");
		}
	}

	public List<string> SelectedProjectGroupIds
	{
		get
		{
			return _selectedProjectGroupIds;
		}
		set
		{
			_selectedProjectGroupIds = value;
			OnPropertyChanged("SelectedProjectGroupIds");
		}
	}

	public ProjectOrGroupFilter ProjectOrGroupFilter
	{
		get
		{
			return _projectOrGroupFilter;
		}
		set
		{
			_projectOrGroupFilter = value;
			OnPropertyChanged("ProjectOrGroupFilter");
		}
	}

	public DateFilter DateFilter
	{
		get
		{
			return _dateFilter;
		}
		set
		{
			_dateFilter = value;
			OnPropertyChanged("DateFilter");
		}
	}

	public DateTime? StartDate
	{
		get
		{
			return _startDate;
		}
		set
		{
			_startDate = value;
			OnPropertyChanged("StartDate");
		}
	}

	public DateTime? EndDate
	{
		get
		{
			return _endDate;
		}
		set
		{
			_endDate = value;
			OnPropertyChanged("EndDate");
		}
	}

	public string SelectedProjectDisplayText
	{
		get
		{
			return _selectedProjectDisplayText;
		}
		set
		{
			_selectedProjectDisplayText = value;
			OnPropertyChanged("SelectedProjectDisplayText");
		}
	}

	public string SelectedStatusDisplayText
	{
		get
		{
			return _selectedStatusDisplayText;
		}
		set
		{
			_selectedStatusDisplayText = value;
			OnPropertyChanged("SelectedStatusDisplayText");
		}
	}

	public string SelectedPriorityDisplayText
	{
		get
		{
			return _selectedPriorityDisplayText;
		}
		set
		{
			_selectedPriorityDisplayText = value;
			OnPropertyChanged("SelectedPriorityDisplayText");
		}
	}

	public bool Changed { get; set; } = true;

	public async Task<CompletedFilterData> GetCompletedFilterData()
	{
		CompletedFilterData data = new CompletedFilterData();
		_ = DateTime.Now.DayOfWeek;
		switch (DateFilter)
		{
		case DateFilter.All:
			data.FromTime = null;
			data.ToTime = DateTime.Now.Date.AddDays(1.0);
			break;
		case DateFilter.ThisWeek:
			data.FromTime = Utils.GetWeekStart(DateTime.Today);
			data.ToTime = Utils.GetWeekStart(DateTime.Today).AddDays(7.0);
			break;
		case DateFilter.LastWeek:
			data.FromTime = Utils.GetWeekStart(DateTime.Today).AddDays(-7.0);
			data.ToTime = Utils.GetWeekStart(DateTime.Today);
			break;
		case DateFilter.ThisMonth:
		{
			int day = DateTime.Now.Date.Day;
			data.FromTime = DateTime.Now.Date.AddDays(1 - day);
			data.ToTime = DateTime.Now.Date.AddDays(1 - day).AddMonths(1);
			break;
		}
		case DateFilter.Custom:
			data.FromTime = StartDate;
			data.ToTime = (EndDate ?? DateTime.Now).Date.AddDays(1.0);
			break;
		}
		switch (ProjectOrGroupFilter)
		{
		case ProjectOrGroupFilter.All:
			data.ProjectIds = new List<string>();
			_selectedProjectIds = new List<string>();
			break;
		case ProjectOrGroupFilter.Custom:
			data.ProjectIds = SelectedProjectIds;
			if (SelectedProjectGroupIds.Count <= 0)
			{
				break;
			}
			foreach (string selectedProjectGroupId in SelectedProjectGroupIds)
			{
				ObservableCollection<ProjectModel> observableCollection = await ProjectDao.GetProjectsInGroup(selectedProjectGroupId);
				if (observableCollection.Count > 0)
				{
					data.ProjectIds = data.ProjectIds.Union(observableCollection.Select((ProjectModel p) => p.id)).ToList();
				}
			}
			break;
		}
		return data;
	}
}
