namespace ticktick_WPF.Views.Misc;

public class TeamSectionViewModel : SelectableItemViewModel
{
	public TeamSectionViewModel(string id, string title)
	{
		base.Id = id;
		base.Title = title;
		base.Type = "team";
		base.ShowIcon = false;
		base.Selectable = false;
		base.IsSectionGroup = true;
		base.ShowSelected = false;
		base.ShowIndent = true;
		base.Open = true;
		base.TeamId = id;
		base.IsTeam = true;
		base.IsParent = true;
	}
}
