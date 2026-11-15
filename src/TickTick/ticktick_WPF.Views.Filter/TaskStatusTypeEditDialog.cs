using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Filter;

public class TaskStatusTypeEditDialog : TaskTypeEditDialog
{
	private static readonly Dictionary<string, string> Map = new Dictionary<string, string>
	{
		{
			"all",
			Utils.GetString("AllStatus")
		},
		{
			"completed",
			Utils.GetString("Completed")
		},
		{
			"inProgress",
			Utils.GetString("SummaryInProgress")
		},
		{
			"uncompleted",
			Utils.GetString("Uncompleted")
		},
		{
			"wontDo",
			Utils.GetString("Abandoned")
		}
	};

	public TaskStatusTypeEditDialog(ICollection<string> selected)
		: base(selected)
	{
	}

	protected override void InitData(ICollection<string> selected)
	{
		base.ViewModel = new FilterConditionViewModel
		{
			Type = CondType.TaskType,
			ItemsSource = new ObservableCollection<FilterListItemViewModel>
			{
				new FilterListItemViewModel
				{
					Value = "all",
					Title = Utils.GetString("AllStatus"),
					Selected = (selected.Count == 0),
					IsAllItem = true
				},
				new FilterListItemViewModel
				{
					Value = "completed",
					Title = Utils.GetString("Completed"),
					Selected = selected.Contains("completed")
				},
				new FilterListItemViewModel
				{
					Value = "inProgress",
					Title = Utils.GetString("SummaryInProgress"),
					Selected = selected.Contains("inProgress")
				},
				new FilterListItemViewModel
				{
					Value = "uncompleted",
					Title = Utils.GetString("Uncompleted"),
					Selected = selected.Contains("uncompleted")
				},
				new FilterListItemViewModel
				{
					Value = "wontDo",
					Title = Utils.GetString("Abandoned"),
					Selected = selected.Contains("wontDo")
				}
			},
			IsAllSelected = false,
			SupportedLogic = new List<LogicType>()
		};
	}

	public static string FormatDisplayText(List<string> status)
	{
		List<string> list = (from it in Map.Keys
			where status.Contains(it.ToString())
			select Map[it]).ToList();
		if (list.Count <= 0)
		{
			return Utils.GetString("AllStatus");
		}
		return string.Join(", ", list);
	}
}
