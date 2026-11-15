using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Team;

namespace ticktick_WPF.Dal;

public static class TeamDao
{
	public static bool IsTeamValid()
	{
		return CacheManager.GetTeams()?.Any((TeamModel t) => !t.expired) ?? false;
	}

	public static async Task<bool> IsTeamPro()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return (await (from v in App.Connection.Table<TeamModel>()
			where v.userId == userId && v.teamPro
			select v).ToListAsync())?.Any((TeamModel t) => t.IsPro()) ?? false;
	}

	public static async Task<List<TeamModel>> GetAllTeams()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from v in App.Connection.Table<TeamModel>()
			where v.userId == userId
			orderby v.joinedTime descending
			select v).ToListAsync();
	}

	public static async Task<bool> MergeTeams(List<TeamModel> teams)
	{
		bool result = false;
		string userId = Utils.GetCurrentUserIdInt().ToString();
		List<TeamModel> list = await (from v in App.Connection.Table<TeamModel>()
			where v.userId == userId
			select v).ToListAsync();
		if (teams.Count > 0)
		{
			if (list != null && list.Count > 0)
			{
				List<string> localTeamIds = list.Select((TeamModel teamModel2) => teamModel2.id).ToList();
				List<string> teamIds = teams.Select((TeamModel teamModel2) => teamModel2.id).ToList();
				List<TeamModel> list2 = teams.Where((TeamModel teamModel2) => !localTeamIds.Contains(teamModel2.id)).ToList();
				if (list2.Count > 0)
				{
					result = true;
					AddTeam(list2);
				}
				List<TeamModel> list3 = list.Where((TeamModel teamModel2) => !teamIds.Contains(teamModel2.id)).ToList();
				if (list3.Count > 0)
				{
					result = true;
					DeleteTeam(list3);
				}
				foreach (TeamModel team in teams.Where((TeamModel teamModel2) => localTeamIds.Contains(teamModel2.id)))
				{
					TeamModel teamModel = list.FirstOrDefault((TeamModel model) => model.id == team.id);
					if (teamModel != null)
					{
						team.open = teamModel.open;
						team._Id = teamModel._Id;
						UpdateTeam(team);
						if (teamModel.name != team.name || teamModel.IsPro() != team.IsPro())
						{
							result = true;
						}
					}
				}
			}
			else
			{
				result = true;
				AddTeam(teams);
			}
		}
		else if (list != null && list.Count > 0)
		{
			result = true;
			DeleteTeam(list);
		}
		return result;
	}

	private static async void DeleteTeam(List<TeamModel> teams)
	{
		if (teams == null || teams.Count <= 0)
		{
			return;
		}
		foreach (TeamModel team in teams)
		{
			await App.Connection.DeleteAsync(team);
			CacheManager.DeleteTeam(team);
		}
	}

	public static async Task UpdateTeam(TeamModel team)
	{
		if (team != null)
		{
			team.name = team.name.Trim();
			team.userId = Utils.GetCurrentUserIdInt().ToString();
			await App.Connection.UpdateAsync(team);
			CacheManager.UpdateTeam(team);
		}
	}

	private static async void AddTeam(List<TeamModel> teams)
	{
		if (teams == null)
		{
			return;
		}
		List<TeamModel> list = teams.Where((TeamModel t) => t.expired).ToList();
		if (list.Count > 0)
		{
			new TeamExpiredRemindDialog(list).ShowDialog();
		}
		foreach (TeamModel team in teams)
		{
			team.name = team.name.Trim();
			team.expiredChecked = team.expired;
			team.userId = Utils.GetCurrentUserIdInt().ToString();
			await App.Connection.InsertAsync(team);
			CacheManager.UpdateTeam(team);
		}
	}

	public static async void SaveOpenStatus(string id, bool open)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		TeamModel team = await (from t in App.Connection.Table<TeamModel>()
			where t.id == id && t.userId == userId
			select t).FirstOrDefaultAsync();
		if (team != null)
		{
			team.open = open;
			team.modifiedTime = DateTime.Now;
			await App.Connection.UpdateAsync(team);
			CacheManager.UpdateTeam(team);
		}
	}

	public static bool IsTeamExpired(string teamId)
	{
		return (from team in CacheManager.GetTeams()
			where team.expired
			select team.id).ToList().Contains(teamId);
	}

	public static long GetNewProjectOrderInTeam(string teamId)
	{
		long num = 0L;
		ProjectModel projectModel = (from t in CacheManager.GetProjects()
			where t.teamId == teamId
			orderby t.sortOrder
			select t).FirstOrDefault();
		if (projectModel != null)
		{
			num = projectModel.sortOrder - 268435456;
		}
		ProjectGroupModel projectGroupModel = (from t in CacheManager.GetProjectGroups()
			where t.teamId == teamId
			orderby t.sortOrder
			select t).FirstOrDefault();
		if (projectGroupModel != null && projectGroupModel.sortOrder < num)
		{
			num = projectGroupModel.sortOrder ?? (-268435456);
		}
		return num;
	}

	public static long GetNewProjectInPersonal()
	{
		long num = 0L;
		ProjectModel projectModel = (from t in CacheManager.GetProjects()
			where string.IsNullOrEmpty(t.teamId)
			orderby t.sortOrder
			select t).FirstOrDefault();
		if (projectModel != null)
		{
			num = projectModel.sortOrder - 268435456;
		}
		ProjectGroupModel projectGroupModel = (from t in CacheManager.GetProjectGroups()
			where string.IsNullOrEmpty(t.teamId)
			orderby t.sortOrder
			select t).FirstOrDefault();
		if (projectGroupModel != null && projectGroupModel.sortOrder < num)
		{
			num = projectGroupModel.sortOrder ?? (-268435456);
		}
		return num;
	}
}
