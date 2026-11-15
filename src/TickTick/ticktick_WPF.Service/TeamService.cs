using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Service;

public static class TeamService
{
	public static async Task<bool> PullTeams()
	{
		if (Utils.IsNetworkAvailable())
		{
			List<TeamModel> list = await Communicator.GetAllTeams();
			if (list != null)
			{
				return await TeamDao.MergeTeams(list);
			}
		}
		return false;
	}

	public static ImageSource GetTeamLogo(TeamModel team)
	{
		string text = ThemeUtil.GetColor("PrimaryColor").Color.ToString();
		return ThemeUtil.SaveAsWriteableBitmap(new Border
		{
			Width = 36.0,
			Height = 36.0,
			Background = (string.IsNullOrEmpty(text) ? Brushes.Transparent : ThemeUtil.GetColorInString("#1A" + text.Substring(text.Length - 6))),
			CornerRadius = new CornerRadius(18.0),
			Child = new TextBlock
			{
				FontSize = 16.0,
				Text = team.name.Substring(0, 1),
				Foreground = ThemeUtil.GetColor("PrimaryColor"),
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Center
			}
		});
	}

	public static async Task<bool> CheckFreeTeamShareCount(string teamId, Window owner = null)
	{
		TeamModel teamById = CacheManager.GetTeamById(teamId);
		if (teamById != null && !teamById.IsPro())
		{
			bool flag = CacheManager.GetProjects().Count((ProjectModel p) => p.teamId == teamId && p.IsShareList()) >= 3;
			if (!flag)
			{
				flag = await Communicator.GetTeamShareCount(teamById.id) <= 0;
			}
			if (flag)
			{
				ProChecker.ShowUpgradeDialog(ProType.TeamShareLimit, owner, teamId);
				return false;
			}
		}
		return true;
	}

	public static string GetTeamId()
	{
		if (!UserManager.IsTeamUser())
		{
			return "";
		}
		return CacheManager.GetTeam()?.id;
	}
}
