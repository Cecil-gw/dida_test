using System;
using System.Linq;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Util.Filter;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Util.Provider;

public class FilterListData : SortProjectData
{
	public FilterModel Filter { get; set; }

	public FilterTaskDefault TaskDefault { get; set; }

	public FilterListData(FilterModel filter)
	{
		Filter = filter;
		TaskDefault = FilterViewModel.CalculateTaskDefault(filter.rule);
		base.DefaultProjectModel = TaskDefault.ProjectModel;
		base.DefaultTaskDate = TaskDefault.DefaultDate;
		base.AddTaskHint = GetTaskHint(TaskDefault);
		base.ShowProjectSort = true;
		base.ShowCustomSort = false;
		base.ShowLoadMore = false;
		base.EmptyTitle = Utils.GetString("NoTasksInFilter");
		base.EmptyContent = Utils.GetString((Parser.GetFilterRuleVersion(Filter.rule) > 7) ? "FilterVersionUpdate" : ((!Parser.CheckFilterRuleExpired(Filter.rule)) ? "NoTaskInFilterDesc" : "FilterB2"));
		FilterTaskDefault taskDefault = TaskDefault;
		if (taskDefault != null && taskDefault.DefaultDate.HasValue && TaskDefault.DefaultDate.Value.Date == DateTime.Today.AddDays(1.0).Date)
		{
			base.IgnoreTodaySection = true;
		}
	}

	public override bool ShowFilterExpired()
	{
		return Parser.CheckFilterRuleExpired(Filter.rule);
	}

	private static string GetTaskHint(FilterTaskDefault taskDefault)
	{
		if (taskDefault.DefaultTags != null && taskDefault.DefaultTags.Count > 0)
		{
			return taskDefault.DefaultTags.Aggregate(string.Empty, (string current, string tag) => current + "#" + tag + " ");
		}
		string text = string.Empty;
		string text2 = string.Empty;
		ProjectModel projectModel = taskDefault.ProjectModel;
		if (taskDefault.ProjectModel != null)
		{
			text2 = (projectModel.Isinbox ? Utils.GetString("Inbox") : projectModel.name);
		}
		if (taskDefault.DefaultDate.HasValue)
		{
			if (taskDefault.DefaultDate.Value.Date == DateTime.Now.Date)
			{
				text = Utils.GetString("Today");
			}
			else if (taskDefault.DefaultDate.Value.Date == DateTime.Now.Date.AddDays(1.0))
			{
				text = Utils.GetString("Tomorrow");
			}
			else if (taskDefault.DefaultDate.Value.Date == DateTime.Now.Date.AddDays(-1.0))
			{
				text = Utils.GetString("PublicYesterday");
			}
			else if ((taskDefault.DefaultDate.Value.Date - DateTime.Now.Date).Days <= 7)
			{
				text = taskDefault.DefaultDate.Value.ToString("ddd", App.Ci);
			}
		}
		if (!string.IsNullOrEmpty(text2))
		{
			if (string.IsNullOrEmpty(text))
			{
				return string.Format(Utils.GetString("CenterAddTaskTextBoxPreviewText"), text2, Utils.GetString(projectModel.IsNote ? "Notes" : "Task").ToLower());
			}
			return string.Format(Utils.GetString("CenterAddTaskDateTextBoxPreviewText"), text, text2, Utils.GetString(projectModel.IsNote ? "Notes" : "Task").ToLower());
		}
		return string.Empty;
	}

	public override string GetTitle()
	{
		return Filter.name;
	}

	public override async void SaveSortOption(SortOption sortOption)
	{
		Filter.SortOption = sortOption;
		Filter.sortType = ((sortOption.groupBy == "none") ? sortOption.orderBy : sortOption.groupBy);
		await FilterDao.UpdateFilter(Filter);
	}

	public override EmptyIconsSourceModel GetEmptyIconsSource(bool allCompleted)
	{
		return new EmptyIconsSourceModel("FilterEmpty");
	}
}
