using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Filter;

public class ProjectOrGroupCard : CardBase
{
	private List<string> SelectedProjectIds { get; set; } = new List<string>();

	private List<string> SelectedGroupIds { get; set; } = new List<string>();

	private List<string> SelectedCalendarAccountIds { get; set; } = new List<string>();

	private List<string> SelectedCalendarIds { get; set; } = new List<string>();

	public ProjectOrGroupCard(CardViewModel viewModel)
		: base(viewModel)
	{
		InitEditDialog();
	}

	private void InitEditDialog()
	{
		if (EditDialog is ProjectOrGroupEditDialog projectOrGroupEditDialog)
		{
			projectOrGroupEditDialog.OnSelectedProjectChanged += delegate(object sender, List<string> projectIds)
			{
				SelectedProjectIds = projectIds.ToList();
				((ProjectOrGroupCardViewModel)ViewModel).Values = projectIds.ToList();
				ViewModel.Content = ViewModel.ToCardText();
			};
			projectOrGroupEditDialog.OnSelectedGroupChanged += delegate(object sender, List<string> groupIds)
			{
				SelectedGroupIds = groupIds.ToList();
				((ProjectOrGroupCardViewModel)ViewModel).GroupIds = groupIds.ToList();
				ViewModel.Content = ViewModel.ToCardText();
			};
			projectOrGroupEditDialog.OnSelectedCalendarAccountChanged += delegate(object sender, List<string> accountIds)
			{
				SelectedCalendarAccountIds = accountIds.ToList();
				((ProjectOrGroupCardViewModel)ViewModel).CalendarAccounts = accountIds.ToList();
				ViewModel.Content = ViewModel.ToCardText();
			};
			projectOrGroupEditDialog.OnSelectedCalendarChanged += delegate(object sender, List<string> calendarIds)
			{
				SelectedCalendarIds = calendarIds.ToList();
				((ProjectOrGroupCardViewModel)ViewModel).Calendars = calendarIds.ToList();
				ViewModel.Content = ViewModel.ToCardText();
			};
			projectOrGroupEditDialog.OnLogicChanged += delegate(object sender, LogicType logicType)
			{
				((ProjectOrGroupCardViewModel)ViewModel).LogicType = logicType;
				ViewModel.Content = ViewModel.ToCardText();
			};
			projectOrGroupEditDialog.OnCancel += delegate
			{
				Popup.IsOpen = false;
			};
			projectOrGroupEditDialog.OnSave += delegate
			{
				IsSave = true;
				Popup.IsOpen = false;
			};
			Popup.Closed -= base.CardPopupClosed;
			Popup.Closed += base.CardPopupClosed;
		}
	}

	protected override void Init(CardViewModel model)
	{
		ProjectOrGroupCardViewModel projectOrGroupCardViewModel = (ProjectOrGroupCardViewModel)model;
		SelectedProjectIds = projectOrGroupCardViewModel.Values.ToList();
		SelectedGroupIds = projectOrGroupCardViewModel.GroupIds.ToList();
		SelectedCalendarAccountIds = projectOrGroupCardViewModel.CalendarAccounts.ToList();
		SelectedCalendarIds = projectOrGroupCardViewModel.Calendars.ToList();
	}

	protected override ConditionEditDialog GenerateEditDialog()
	{
		return new ProjectOrGroupEditDialog(showAll: false, SelectedProjectIds, SelectedGroupIds, ((ProjectOrGroupCardViewModel)ViewModel).LogicType, SelectedCalendarAccountIds, SelectedCalendarIds);
	}

	protected override bool CheckFilterValid()
	{
		if (SelectedProjectIds.Count <= 0 && SelectedGroupIds.Count <= 0 && SelectedCalendarAccountIds.Count <= 0)
		{
			return SelectedCalendarIds.Count > 0;
		}
		return true;
	}
}
