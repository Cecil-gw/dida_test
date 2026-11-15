using ticktick_WPF.Models;
using ticktick_WPF.Resource;

namespace ticktick_WPF.ViewModels;

public class TeamGroupViewModel : PtfAllViewModel
{
	public readonly TeamModel Team;

	public TeamGroupViewModel(TeamModel team)
		: base(PtfType.Project)
	{
		base.Id = team.id;
		Team = team;
		base.Title = team.name;
		base.Open = team.open;
		base.TeamId = team.id;
		base.Expired = team.expired;
	}
}
