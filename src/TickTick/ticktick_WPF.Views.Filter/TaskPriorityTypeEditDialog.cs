using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Filter;

public class TaskPriorityTypeEditDialog : TaskTypeEditDialog
{
	private static readonly Dictionary<string, string> Map = new Dictionary<string, string>
	{
		{
			"all",
			Utils.GetString("all_priorities")
		},
		{
			"5",
			Utils.GetString("high")
		},
		{
			"3",
			Utils.GetString("medium")
		},
		{
			"1",
			Utils.GetString("low")
		},
		{
			"0",
			Utils.GetString("none")
		}
	};

	public TaskPriorityTypeEditDialog(ICollection<string> selected)
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
					Title = Map["all"],
					Selected = (selected.Count == 0),
					IsAllItem = true
				},
				new FilterListItemViewModel
				{
					Value = "5",
					Title = Map["5"],
					Selected = selected.Contains("5")
				},
				new FilterListItemViewModel
				{
					Value = "3",
					Title = Map["3"],
					Selected = selected.Contains("3")
				},
				new FilterListItemViewModel
				{
					Value = "1",
					Title = Map["1"],
					Selected = selected.Contains("1")
				},
				new FilterListItemViewModel
				{
					Value = "0",
					Title = Map["0"],
					Selected = selected.Contains("0")
				}
			},
			IsAllSelected = false,
			SupportedLogic = new List<LogicType>()
		};
	}

	public static string FormatDisplayText(List<string> priorities)
	{
		List<string> list = (from it in Map.Keys
			where priorities.Contains(it.ToString())
			select Map[it]).ToList();
		if (list.Count <= 0)
		{
			return Utils.GetString("all_priorities");
		}
		return string.Join(", ", list);
	}
}
