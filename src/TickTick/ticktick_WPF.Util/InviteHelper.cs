using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TickTickModels;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.Team;

namespace ticktick_WPF.Util;

public static class InviteHelper
{
	private static Dictionary<string, IEnumerable<InviteUserModel>> _teamContacts = new Dictionary<string, IEnumerable<InviteUserModel>>();

	private static Dictionary<string, IEnumerable<TeamMember>> _teamMembers = new Dictionary<string, IEnumerable<TeamMember>>();

	private static List<ProjectUsersMode> _projectUsersData = new List<ProjectUsersMode>();

	private static List<ShareContactsModel> _shareContacts = new List<ShareContactsModel>();

	private static List<InviteUserModel> _recentInviteUsers = new List<InviteUserModel>();

	private static async Task PullShareContacts()
	{
		List<ShareContactsModel> contactList = await Communicator.GetShareContactsList();
		if (contactList == null)
		{
			return;
		}
		if (contactList.Count == 0)
		{
			_recentInviteUsers = new List<InviteUserModel>();
			_shareContacts = contactList;
			return;
		}
		contactList.Sort((ShareContactsModel a, ShareContactsModel b) => b.lstTime.CompareTo(a.lstTime));
		List<UserPublicProfilesModel> list = await UserPublicProfilesDao.GetUsersInfoByUserCodes((from shareContactsModel in contactList
			where shareContactsModel != null
			select shareContactsModel.userCode into userCode
			where !string.IsNullOrWhiteSpace(userCode)
			select userCode).ToList());
		Func<ShareContactsModel, bool> canIncludeContact;
		Func<ShareContactsModel, string, string> func;
		if (Utils.IsDida())
		{
			canIncludeContact = (ShareContactsModel shareContactsModel) => true;
			func = PrepareDidaRecentContact;
		}
		else
		{
			canIncludeContact = (ShareContactsModel shareContactsModel) => !string.IsNullOrWhiteSpace(shareContactsModel.email);
			func = PrepareTickRecentContact;
		}
		List<InviteUserModel> list2 = new List<InviteUserModel>();
		foreach (ShareContactsModel contact in contactList.Where((ShareContactsModel shareContactsModel) => shareContactsModel != null && !string.IsNullOrWhiteSpace(shareContactsModel.userCode) && canIncludeContact(shareContactsModel)))
		{
			UserPublicProfilesModel userPublicProfilesModel = list?.FirstOrDefault((UserPublicProfilesModel info) => info.userCode == contact.userCode);
			contact.avatarUrl = userPublicProfilesModel?.avatarUrl ?? string.Empty;
			contact.siteId = userPublicProfilesModel?.siteId;
			string arg = ((!string.IsNullOrEmpty(userPublicProfilesModel?.nickName)) ? userPublicProfilesModel.nickName : userPublicProfilesModel?.displayName);
			string userName = func(contact, arg);
			InviteUserModel inviteUserModel = new InviteUserModel(contact)
			{
				UserName = userName
			};
			if (inviteUserModel.Id != LocalSettings.Settings.LoginUserId)
			{
				list2.Add(inviteUserModel);
			}
		}
		_recentInviteUsers = list2;
		_shareContacts = contactList;
	}

	private static string PrepareTickRecentContact(ShareContactsModel contact, string profileName)
	{
		contact.canSearchByPhone = false;
		string text = ((!string.IsNullOrWhiteSpace(contact.email) && !Utils.IsFakeEmail(contact.email)) ? contact.email : null);
		contact.displayName = FirstNotEmpty(profileName, text, contact.displayName);
		contact.displayEmail = FirstNotEmpty(text, contact.displayEmail);
		return contact.displayName;
	}

	private static string PrepareDidaRecentContact(ShareContactsModel contact, string profileName)
	{
		string maskedMainlandPhone = Utils.GetMaskedMainlandPhone(contact.phone);
		contact.canSearchByPhone = !string.IsNullOrEmpty(Utils.NormalizeMainlandPhone(contact.phone));
		string text = (ShareInviteTarget.IsSafeDisplayName(profileName) ? profileName : Utils.GetMaskedMainlandPhone(profileName));
		string realContactEmail = GetRealContactEmail(contact.email);
		string text2 = ((ShareInviteTarget.IsUsableUsername(contact.email) && string.IsNullOrEmpty(realContactEmail)) ? contact.email : null);
		contact.displayName = FirstNotEmpty(text, maskedMainlandPhone, text2, realContactEmail, Utils.GetString("InvitationPending"));
		contact.displayEmail = FirstNotEmpty(realContactEmail, maskedMainlandPhone);
		if (string.Equals(contact.displayName, contact.displayEmail, StringComparison.Ordinal))
		{
			contact.displayEmail = null;
		}
		return contact.displayName;
	}

	private static string FirstNotEmpty(params string[] values)
	{
		return values?.FirstOrDefault((string value) => !string.IsNullOrEmpty(value));
	}

	private static string GetRealContactEmail(string email)
	{
		if (!ShareInviteTarget.IsRealEmail(email))
		{
			return null;
		}
		return email;
	}

	private static async Task PullTeamMembers(string teamId, string projectId = "", ShareProjectDialog window = null)
	{
		string value = await Communicator.GetTeamMember(teamId);
		try
		{
			List<TeamMember> list = JsonConvert.DeserializeObject<List<TeamMember>>(value);
			list?.Sort(delegate(TeamMember left, TeamMember right)
			{
				if (left != null && right != null)
				{
					string text = (string.IsNullOrEmpty(left.nickname) ? left.displayName : left.nickname);
					string text2 = (string.IsNullOrEmpty(right.nickname) ? right.displayName : right.nickname);
					List<TitleChunk> list2 = text?.CompareNumber();
					List<TitleChunk> list3 = text2?.CompareNumber();
					if (list2 != null && list2.Count > 0 && list3 != null && list3.Count > 0)
					{
						int num = Utils.CompareChunks(list2, list3);
						if (num != 0)
						{
							return num;
						}
					}
					return string.Compare(text, text2, StringComparison.CurrentCulture);
				}
				return -1;
			});
			_teamMembers[teamId] = list;
		}
		catch (Exception)
		{
			if (window == null)
			{
				return;
			}
			ErrorModel errorModel = JsonConvert.DeserializeObject<ErrorModel>(value);
			if (errorModel == null)
			{
				return;
			}
			ProjectModel project = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == projectId);
			string errorCode = errorModel.errorCode;
			if (!(errorCode == "team_not_exist"))
			{
				if (errorCode == "no_team_permission" && new CustomerDialog(Utils.GetString("CanNotGetTeam"), string.Format(Utils.GetString("TeamNotExist"), CacheManager.GetTeams().FirstOrDefault((TeamModel team) => team.id == project?.teamId)?.name ?? " "), Utils.GetString("GotIt"), "")
				{
					Owner = window
				}.ShowDialog().HasValue)
				{
					window.Close();
				}
			}
			else if (new CustomerDialog(Utils.GetString("CanNotGetTeam"), string.Format(Utils.GetString("TeamNotExist"), CacheManager.GetTeams().FirstOrDefault((TeamModel team) => team.id == project?.teamId)?.name ?? " "), Utils.GetString("GotIt"), "")
			{
				Owner = window
			}.ShowDialog().HasValue)
			{
				window.Close();
			}
		}
	}

	private static async Task PullTeamContacts(string teamId)
	{
		List<InviteUserModel> recentInviteModels = new List<InviteUserModel>();
		List<TeamContactsModel> teamContacts = await Communicator.GetTeamContactsList(teamId);
		if (teamContacts != null && teamContacts.Count != 0)
		{
			teamContacts.Sort((TeamContactsModel a, TeamContactsModel b) => b.lstTime.CompareTo(a.lstTime));
			List<UserPublicProfilesModel> list = await UserPublicProfilesDao.GetUsersInfoByUserCodes(teamContacts.Select((TeamContactsModel c) => c.userCode).ToList());
			foreach (TeamContactsModel item in teamContacts)
			{
				if (item != null && !string.IsNullOrWhiteSpace(item.userCode))
				{
					UserPublicProfilesModel userPublicProfilesModel = list?.FirstOrDefault((UserPublicProfilesModel u) => u.userCode == item.userCode);
					if (userPublicProfilesModel != null && !(userPublicProfilesModel.userId.ToString() == LocalSettings.Settings.LoginUserId))
					{
						InviteUserModel inviteUserModel = new InviteUserModel(item)
						{
							Id = userPublicProfilesModel.userId.ToString(),
							AvatarUrl = (userPublicProfilesModel.avatarUrl ?? ""),
							UserName = (string.IsNullOrEmpty(userPublicProfilesModel.nickName) ? userPublicProfilesModel.displayName : userPublicProfilesModel.nickName)
						};
						inviteUserModel.SetAvatar();
						recentInviteModels.Add(inviteUserModel);
					}
				}
			}
		}
		if (_teamContacts.ContainsKey(teamId))
		{
			_teamContacts[teamId] = recentInviteModels;
		}
		else
		{
			_teamContacts.Add(teamId, recentInviteModels);
		}
	}

	private static async Task PullProjectUsers()
	{
		if (!Utils.IsNetworkAvailable())
		{
			return;
		}
		List<ProjectUsersMode> list = await Utils.GetRecentProjectUsersList();
		foreach (ProjectUsersMode item in list)
		{
			item.shareUsers.RemoveAll((ShareUserModel user) => user.deleted);
			item.shareUsers.Sort(delegate(ShareUserModel a, ShareUserModel b)
			{
				if (a.isOwner || (a.isAccept && !b.isAccept))
				{
					return -1;
				}
				return (b.isOwner || (!a.isAccept && b.isAccept)) ? 1 : (b.createdTime ?? DateTime.Now).CompareTo(a.createdTime ?? DateTime.Now);
			});
		}
		list?.Sort((ProjectUsersMode a, ProjectUsersMode b) => a.SortOrder.CompareTo(b.SortOrder));
		_projectUsersData = list;
	}

	public static async Task SetTeamMembers(string teamId, string projectId = "", ShareProjectDialog window = null)
	{
		if (_teamMembers.ContainsKey(teamId))
		{
			PullTeamMembers(teamId, projectId, window);
		}
		else
		{
			await PullTeamMembers(teamId, projectId, window);
		}
	}

	public static async Task SetTeamContacts(string teamId)
	{
		if (_teamContacts.ContainsKey(teamId))
		{
			PullTeamContacts(teamId);
		}
		else
		{
			await PullTeamContacts(teamId);
		}
	}

	public static async Task SetProjectUsers()
	{
		if (_projectUsersData == null || _projectUsersData.Count <= 0)
		{
			await PullProjectUsers();
		}
		else
		{
			PullProjectUsers();
		}
	}

	public static async Task SetShareContacts()
	{
		await PullShareContacts();
	}

	public static IEnumerable<TeamMember> GetTeamMembers(string teamId)
	{
		if (_teamMembers.ContainsKey(teamId))
		{
			return _teamMembers[teamId];
		}
		return null;
	}

	public static IEnumerable<InviteUserModel> GetTeamContacts(string teamId)
	{
		if (_teamContacts.ContainsKey(teamId))
		{
			foreach (InviteUserModel item in _teamContacts[teamId])
			{
				item.Selected = false;
			}
			return _teamContacts[teamId];
		}
		return null;
	}

	public static List<ProjectUsersMode> GetProjectUsers()
	{
		return _projectUsersData;
	}

	public static List<ShareContactsModel> GetShareContacts()
	{
		return _shareContacts;
	}

	public static List<InviteUserModel> GetRecentInvite()
	{
		_recentInviteUsers.ForEach(delegate(InviteUserModel u)
		{
			u.Selected = false;
		});
		return _recentInviteUsers;
	}

	public static void Clear()
	{
		_projectUsersData?.Clear();
		_shareContacts?.Clear();
		_recentInviteUsers?.Clear();
	}

	public static void DeleteRecentUser(string email, string phone = null)
	{
		string normalizedPhone = (Utils.IsDida() ? Utils.NormalizeMainlandPhone(phone) : string.Empty);
		_shareContacts?.RemoveAll((ShareContactsModel u) => (!string.IsNullOrEmpty(email) && string.Equals(u.email, email, StringComparison.OrdinalIgnoreCase)) || (!string.IsNullOrEmpty(normalizedPhone) && Utils.NormalizeMainlandPhone(u.phone) == normalizedPhone));
		_recentInviteUsers?.RemoveAll((InviteUserModel u) => (!string.IsNullOrEmpty(email) && string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)) || (!string.IsNullOrEmpty(normalizedPhone) && Utils.NormalizeMainlandPhone(u.Phone) == normalizedPhone));
	}

	public static void CleanupMissingPhone(string phone)
	{
		string normalizedPhone = (Utils.IsDida() ? Utils.NormalizeMainlandPhone(phone) : string.Empty);
		if (string.IsNullOrEmpty(normalizedPhone))
		{
			return;
		}
		foreach (ShareContactsModel contact in _shareContacts?.Where((ShareContactsModel u) => Utils.NormalizeMainlandPhone(u.phone) == normalizedPhone || Utils.NormalizeMainlandPhone(u.email) == normalizedPhone).ToList() ?? new List<ShareContactsModel>())
		{
			if (ShareInviteTarget.IsUsableUsername(contact.email))
			{
				contact.phone = null;
				contact.canSearchByPhone = false;
				if (!ShareInviteTarget.IsSafeDisplayName(contact.displayName))
				{
					contact.displayName = contact.email;
				}
				contact.displayEmail = GetRealContactEmail(contact.email);
				InviteUserModel inviteUserModel = _recentInviteUsers?.FirstOrDefault((InviteUserModel u) => (!string.IsNullOrEmpty(contact.userCode) && u.UserCode == contact.userCode) || Utils.NormalizeMainlandPhone(u.Phone) == normalizedPhone);
				if (inviteUserModel != null)
				{
					inviteUserModel.Phone = null;
					inviteUserModel.Target = ShareInviteTarget.FromContact(contact);
					if (!ShareInviteTarget.IsSafeDisplayName(inviteUserModel.UserName))
					{
						inviteUserModel.UserName = contact.email;
					}
				}
			}
			else
			{
				_shareContacts.Remove(contact);
				_recentInviteUsers?.RemoveAll((InviteUserModel u) => (!string.IsNullOrEmpty(contact.userCode) && u.UserCode == contact.userCode) || Utils.NormalizeMainlandPhone(u.Phone) == normalizedPhone || Utils.NormalizeMainlandPhone(u.Email) == normalizedPhone);
			}
		}
	}
}
