using System.Collections.Generic;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Filter;

public class InitConditionCard : ConditionCard
{
	private readonly Dictionary<CondType, string> _conditionDict = new Dictionary<CondType, string>
	{
		{
			CondType.Lists,
			Utils.GetString("lists")
		},
		{
			CondType.Tag,
			Utils.GetString("tag")
		},
		{
			CondType.Date,
			Utils.GetString("date")
		},
		{
			CondType.Priority,
			Utils.GetString("priority")
		},
		{
			CondType.Assignee,
			Utils.GetString("assignee")
		},
		{
			CondType.KeyWords,
			Utils.GetString("ContentInclude")
		},
		{
			CondType.TaskType,
			Utils.GetString("TaskType")
		}
	};

	public InitConditionCard(CardViewModel viewModel)
		: base(viewModel)
	{
	}

	protected override void OnCardClick()
	{
		ShowSelectConditionDialog();
	}

	private void ShowSelectConditionDialog()
	{
		List<ListItemData> list = new List<ListItemData>();
		foreach (CondType leftCondType in GetLeftCondTypes())
		{
			list.Add(new ListItemData(leftCondType, _conditionDict[leftCondType]));
		}
		ShowDropdownDialog(list, async delegate(object x, ListItemData item)
		{
			CondType condType = (CondType)item.Key;
			CardViewModel cardViewModel = null;
			switch (condType)
			{
			case CondType.Priority:
				cardViewModel = new PriorityCardViewModel();
				break;
			case CondType.Assignee:
				cardViewModel = new AssigneeCardViewModel();
				break;
			case CondType.Date:
				cardViewModel = new DateCardViewModel();
				break;
			case CondType.Tag:
				cardViewModel = new TagCardViewModel();
				break;
			case CondType.Lists:
				cardViewModel = new ProjectOrGroupCardViewModel();
				break;
			case CondType.KeyWords:
				cardViewModel = new KeywordsViewModel
				{
					IsNewAdd = true
				};
				break;
			case CondType.TaskType:
				cardViewModel = new TaskTypeViewModel();
				break;
			}
			if (cardViewModel != null)
			{
				cardViewModel.ShowDropdown = true;
				FireEvent(new ChangeData
				{
					Type = ChangeType.Filter,
					From = ViewModel,
					To = cardViewModel
				});
			}
		});
	}
}
