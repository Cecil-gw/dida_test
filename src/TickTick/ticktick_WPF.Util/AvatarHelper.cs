using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.MainListView;

namespace ticktick_WPF.Util;

public static class AvatarHelper
{
	private static readonly ConcurrentDictionary<string, List<ShareUserModel>> ShareListCache = new ConcurrentDictionary<string, List<ShareUserModel>>();

	private static readonly ConcurrentDictionary<string, List<ShareUserModel>> UserListCache = new ConcurrentDictionary<string, List<ShareUserModel>>();

	private static Dictionary<string, ShareUserModel> UserCache = new Dictionary<string, ShareUserModel>();

	private static ConcurrentDictionary<string, BitmapImage> Avatars = new ConcurrentDictionary<string, BitmapImage>();

	private const string AllShares = "allshares";

	private static readonly Dictionary<string, DateTime> LastPullTimeDict = new Dictionary<string, DateTime>();

	public static ConcurrentDictionary<string, List<ShareUserModel>> GetProjectUserCache(string projectId, bool checkAllUsers = true)
	{
		ProjectModel projectById = CacheManager.GetProjectById(projectId);
		if (!checkAllUsers || projectById == null || !projectById.OpenToTeam)
		{
			return ShareListCache;
		}
		return UserListCache;
	}

	public static async Task GetAllProjectAvatars()
	{
		if (!CacheManager.GetProjects().Any((ProjectModel p) => p.IsShareList()))
		{
			return;
		}
		string avatarsData = await Communicator.GetAllShareContacts();
		Dictionary<string, List<ShareUserModel>> userDicts;
		try
		{
			userDicts = JsonConvert.DeserializeObject<Dictionary<string, List<ShareUserModel>>>(avatarsData);
		}
		catch (Exception)
		{
			userDicts = null;
		}
		bool saveRemote = userDicts != null;
		if (userDicts == null)
		{
			TempDataModel tempDataModel = await TempDataDao.QueryTempDataModelListDbByTypeAndEntityIdAsync("SHAREUSERLIST", "allshares");
			try
			{
				if (tempDataModel != null)
				{
					userDicts = JsonConvert.DeserializeObject<Dictionary<string, List<ShareUserModel>>>(tempDataModel.Data);
				}
			}
			catch (Exception)
			{
				userDicts = null;
			}
		}
		if (userDicts != null && userDicts.Any())
		{
			foreach (KeyValuePair<string, List<ShareUserModel>> item in userDicts)
			{
				string key = item.Key;
				List<ShareUserModel> value = item.Value;
				value = Sort(value);
				ConcurrentDictionary<string, List<ShareUserModel>> projectUserCache = GetProjectUserCache(key);
				if (!projectUserCache.ContainsKey(key))
				{
					projectUserCache.TryAdd(key, value);
				}
				else
				{
					projectUserCache[key] = value;
				}
			}
			if (saveRemote)
			{
				TempDataDao.UpdateOrInsertTempDataModelListDbByTypeAndEntityIdAsync(new TempDataModel
				{
					User_Id = Utils.GetCurrentUserIdInt().ToString(),
					DataType = "SHAREUSERLIST",
					ModifyTime = Utils.GetNowTimeStamp(),
					Data = avatarsData,
					EntityId = "allshares"
				});
			}
		}
		SetUserCache();
	}

	public static void SetUserCache()
	{
		List<List<ShareUserModel>> source = ShareListCache.Values.ToList();
		Dictionary<string, ShareUserModel> dictionary = new Dictionary<string, ShareUserModel>();
		foreach (List<ShareUserModel> item in source.Where((List<ShareUserModel> users) => users != null && users.Count > 0))
		{
			foreach (ShareUserModel item2 in item.Where((ShareUserModel user) => user.userId.HasValue))
			{
				if (!dictionary.ContainsKey(item2.userId.ToString()))
				{
					dictionary.Add(item2.userId.ToString(), item2);
				}
			}
		}
		foreach (List<ShareUserModel> item3 in from users in UserListCache.Values.ToList()
			where users != null && users.Count > 0
			select users)
		{
			foreach (ShareUserModel item4 in item3.Where((ShareUserModel user) => user.userId.HasValue))
			{
				if (!dictionary.ContainsKey(item4.userId.ToString()))
				{
					dictionary.Add(item4.userId.ToString(), item4);
				}
			}
		}
		UserCache = dictionary;
	}

	public static ShareUserModel GetUserModelById(string userId)
	{
		if (!string.IsNullOrEmpty(userId) && UserCache.ContainsKey(userId))
		{
			return UserCache[userId];
		}
		return null;
	}

	public static Dictionary<string, ShareUserModel> GetAllShareUsers()
	{
		return UserCache;
	}

	public static async Task ResetProjectAvatars(string projectId, bool force = false, bool checkAllUsers = true)
	{
		List<ShareUserModel> list = await GetRemoteShareUsers(projectId, force, checkAllUsers);
		if (list == null || !list.Any())
		{
			return;
		}
		int num = list.Count((ShareUserModel c) => c.isAccept);
		if (num >= 1)
		{
			ProjectModel projectById = CacheManager.GetProjectById(projectId);
			if (projectById != null && projectById.userCount != num)
			{
				projectById.userCount = num;
				CacheManager.UpdateProject(projectById, notify: false);
				ListViewContainer.ReloadProjectData();
			}
		}
		ConcurrentDictionary<string, List<ShareUserModel>> projectUserCache = GetProjectUserCache(projectId, checkAllUsers);
		list = Sort(list);
		if (!projectUserCache.ContainsKey(projectId))
		{
			projectUserCache.TryAdd(projectId, list);
		}
		else
		{
			projectUserCache[projectId] = list;
		}
	}

	public static List<AvatarViewModel> GetCacheProjectAvatars(string projectId, bool showMyName = true)
	{
		ConcurrentDictionary<string, List<ShareUserModel>> projectUserCache = GetProjectUserCache(projectId);
		if (projectId != null && projectUserCache.ContainsKey(projectId))
		{
			List<ShareUserModel> list = projectUserCache[projectId];
			if (list != null && list.Any())
			{
				return (from user in list
					where user.isAccept
					select new AvatarViewModel
					{
						AvatarUrl = user.avatarUrl,
						Name = GetDisplayName(user, showMyName),
						UserCode = user.userCode,
						UserId = user.userId.ToString(),
						Date = (user.createdTime ?? DateTime.Now)
					} into user
					group user by user.UserId into user
					select user.FirstOrDefault()).ToList();
			}
		}
		return new List<AvatarViewModel>();
	}

	public static ShareUserModel GetUserByIdAndProjectId(string userId, string projectId)
	{
		ConcurrentDictionary<string, List<ShareUserModel>> projectUserCache = GetProjectUserCache(projectId);
		if (projectId != null && projectUserCache.ContainsKey(projectId))
		{
			List<ShareUserModel> list = projectUserCache[projectId];
			if (list != null && list.Any())
			{
				return list.FirstOrDefault((ShareUserModel u) => u.userId.ToString() == userId);
			}
		}
		return null;
	}

	public static AvatarViewModel GetAvatarByIdAndProjectId(string userId, string projectId)
	{
		ShareUserModel userByIdAndProjectId = GetUserByIdAndProjectId(userId, projectId);
		if (userByIdAndProjectId != null)
		{
			return new AvatarViewModel
			{
				AvatarUrl = userByIdAndProjectId.avatarUrl,
				Name = GetDisplayName(userByIdAndProjectId, showMyName: true),
				UserCode = userByIdAndProjectId.userCode,
				UserId = userByIdAndProjectId.userId.ToString(),
				Date = (userByIdAndProjectId.createdTime ?? DateTime.Now)
			};
		}
		return null;
	}

	public static async Task<List<AvatarViewModel>> GetProjectAvatars(string projectId, bool showMyName = true, bool fetchRemote = false)
	{
		List<ShareUserModel> list = await GetProjectUsersAsync(projectId, ownerTop: false, fetchRemote);
		if (list != null && list.Any())
		{
			return (from user in list.Distinct()
				where user.isAccept
				select new AvatarViewModel
				{
					AvatarUrl = user.avatarUrl,
					Name = GetDisplayName(user, showMyName),
					UserCode = user.userCode,
					UserId = user.userId.ToString()
				} into user
				group user by user.UserId into user
				select user.FirstOrDefault()).ToList();
		}
		return new List<AvatarViewModel>();
	}

	public static List<AvatarViewModel> GetProjectAvatarsFromCache(string projectId, bool sort = false)
	{
		ConcurrentDictionary<string, List<ShareUserModel>> projectUserCache = GetProjectUserCache(projectId);
		List<ShareUserModel> list = ((!string.IsNullOrEmpty(projectId) && projectUserCache.ContainsKey(projectId)) ? projectUserCache[projectId] : null);
		if (list != null && list.Any())
		{
			if (sort)
			{
				list.Sort((ShareUserModel a, ShareUserModel b) => (a.createdTime != b.createdTime && a.createdTime.HasValue && b.createdTime.HasValue) ? a.createdTime.Value.CompareTo(b.createdTime.Value) : a.userId.GetValueOrDefault().CompareTo(b.userId.GetValueOrDefault()));
			}
			return (from user in list.Distinct()
				where user.isAccept
				select new AvatarViewModel
				{
					AvatarUrl = user.avatarUrl,
					Name = GetDisplayName(user, showMyName: false),
					UserCode = user.userCode,
					UserId = user.userId.ToString()
				} into user
				group user by user.UserId into user
				select user.FirstOrDefault()).ToList();
		}
		return new List<AvatarViewModel>();
	}

	public static List<ShareUserModel> GetProjectUsers(string projectId, bool ownerTop = false)
	{
		if (string.IsNullOrEmpty(projectId))
		{
			return new List<ShareUserModel>();
		}
		ConcurrentDictionary<string, List<ShareUserModel>> concurrentDictionary = (CacheManager.GetProjectById(projectId).OpenToTeam ? UserListCache : ShareListCache);
		List<ShareUserModel> users = null;
		if (concurrentDictionary.ContainsKey(projectId))
		{
			users = concurrentDictionary[projectId];
		}
		return Sort(users, ownerTop);
	}

	public static async Task<List<ShareUserModel>> GetProjectUsersAsync(string projectId, bool ownerTop = false, bool fetchRemote = false, bool checkUserList = true)
	{
		if (string.IsNullOrEmpty(projectId))
		{
			return new List<ShareUserModel>();
		}
		ProjectModel project = CacheManager.GetProjectById(projectId);
		if (project == null)
		{
			return new List<ShareUserModel>();
		}
		ConcurrentDictionary<string, List<ShareUserModel>> cache = ((checkUserList && project.OpenToTeam) ? UserListCache : ShareListCache);
		List<ShareUserModel> list;
		if (!fetchRemote && cache.ContainsKey(projectId))
		{
			list = cache[projectId];
		}
		else
		{
			if (fetchRemote)
			{
				List<ShareUserModel> list2 = ((!project.IsShareList()) ? new List<ShareUserModel>() : (await GetRemoteShareUsers(projectId, force: false, checkUserList)));
				list = list2;
			}
			else
			{
				list = await GetLocalShareUsers(projectId);
				if (project.IsShareList() && (list == null || (list.Count == 0 && !project.OpenToTeam) || (list.Count == 1 && checkUserList && project.OpenToTeam)))
				{
					list = await GetRemoteShareUsers(projectId, force: false, checkUserList);
				}
			}
			if (list != null && list.Any())
			{
				if (!cache.ContainsKey(projectId))
				{
					cache.TryAdd(projectId, list);
				}
				else
				{
					cache[projectId] = list;
				}
			}
		}
		return Sort(list, ownerTop);
	}

	private static async Task<List<ShareUserModel>> GetLocalShareUsers(string projectId)
	{
		TempDataModel tempDataModel = await TempDataDao.QueryTempDataModelListDbByTypeAndEntityIdAsync("SHAREUSERLIST", projectId);
		if (tempDataModel == null)
		{
			return null;
		}
		try
		{
			List<ShareUserModel> list = JsonConvert.DeserializeObject<List<ShareUserModel>>(tempDataModel?.Data);
			if (list != null && list.Any())
			{
				foreach (ShareUserModel item in list.Where((ShareUserModel user) => Utils.IsFakeEmail(user.username)))
				{
					item.inviteIdentifier = item.username;
					item.username = string.Empty;
				}
			}
			return list;
		}
		catch (Exception)
		{
			return new List<ShareUserModel>();
		}
	}

	private static List<ShareUserModel> Sort(List<ShareUserModel> users, bool ownerTop = false)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		users?.Sort(delegate(ShareUserModel a, ShareUserModel b)
		{
			if (ownerTop)
			{
				if (a.isOwner && !b.isOwner)
				{
					return -1;
				}
				if (!a.isOwner && b.isOwner)
				{
					return 1;
				}
			}
			if (a.userId.ToString() == userId && b.userId.ToString() != userId)
			{
				return -1;
			}
			if (a.userId.ToString() != userId && b.userId.ToString() == userId)
			{
				return 1;
			}
			if (a.isAccept && !b.isAccept)
			{
				return -1;
			}
			return (!a.isAccept && b.isAccept) ? 1 : (b.createdTime ?? DateTime.Now).CompareTo(a.createdTime ?? DateTime.Now);
		});
		return users;
	}

	private static string GetDisplayName(ShareUserModel item, bool showMyName)
	{
		if (showMyName && item.userId == Utils.GetCurrentUserIdInt())
		{
			return Utils.GetString("Me");
		}
		if (!string.IsNullOrEmpty(item.displayName))
		{
			return item.displayName;
		}
		return item.username;
	}

	public static void ResetPullTime(string projectId)
	{
		if (string.IsNullOrEmpty(projectId))
		{
			return;
		}
		lock (LastPullTimeDict)
		{
			if (LastPullTimeDict.ContainsKey(projectId))
			{
				LastPullTimeDict[projectId] = DateTime.Now.AddHours(-1.0);
			}
		}
	}

	private static async Task<List<ShareUserModel>> GetRemoteShareUsers(string projectId, bool force = false, bool checkAllUsers = true)
	{
		if (string.IsNullOrEmpty(projectId))
		{
			return null;
		}
		lock (LastPullTimeDict)
		{
			if (!force && LastPullTimeDict.ContainsKey(projectId))
			{
				DateTime dateTime = LastPullTimeDict[projectId];
				List<ShareUserModel> value;
				if ((DateTime.Now - dateTime).TotalSeconds < 60.0)
				{
					return GetProjectUserCache(projectId).TryGetValue(projectId, out value) ? value : new List<ShareUserModel>();
				}
			}
			LastPullTimeDict[projectId] = DateTime.Now;
		}
		ProjectModel project = CacheManager.GetProjectById(projectId);
		if (project == null)
		{
			return new List<ShareUserModel>();
		}
		if (project.IsNew())
		{
			await Task.Delay(1500);
		}
		bool userList = checkAllUsers && project.OpenToTeam;
		string dateType = (userList ? "USERLIST" : "SHAREUSERLIST");
		if (Utils.IsNetworkAvailable())
		{
			string data = await Communicator.GetShareUserList(projectId, userList);
			if (Utils.IsDida() && !userList)
			{
				data = await MergeShareUserPhones(projectId, data);
			}
			await TempDataDao.UpdateOrInsertTempDataModelListDbByTypeAndEntityIdAsync(new TempDataModel
			{
				User_Id = Utils.GetCurrentUserIdInt().ToString(),
				DataType = dateType,
				ModifyTime = Utils.GetNowTimeStamp(),
				Data = data,
				EntityId = projectId
			});
			try
			{
				return HandleUsersFakeEmail(JsonConvert.DeserializeObject<List<ShareUserModel>>(data));
			}
			catch (Exception)
			{
				return new List<ShareUserModel>();
			}
		}
		TempDataModel tempDataModel = await TempDataDao.QueryTempDataModelListDbByTypeAndEntityIdAsync(dateType, projectId);
		try
		{
			return HandleUsersFakeEmail(JsonConvert.DeserializeObject<List<ShareUserModel>>(tempDataModel.Data));
		}
		catch (Exception)
		{
			return new List<ShareUserModel>();
		}
	}

	private static List<ShareUserModel> HandleUsersFakeEmail(List<ShareUserModel> users)
	{
		if (users != null && users.Any())
		{
			foreach (ShareUserModel item in users.Where((ShareUserModel user) => Utils.IsFakeEmail(user.username)))
			{
				item.inviteIdentifier = item.username;
				item.username = string.Empty;
			}
			return users;
		}
		return new List<ShareUserModel>();
	}

	private static async Task<string> MergeShareUserPhones(string projectId, string shareData)
	{
		try
		{
			List<ShareUserModel> shareUsers = JsonConvert.DeserializeObject<List<ShareUserModel>>(shareData);
			if (shareUsers == null || shareUsers.Count == 0)
			{
				return shareData;
			}
			List<ShareUserModel> list = JsonConvert.DeserializeObject<List<ShareUserModel>>(await Communicator.GetShareUserList(projectId, openToTeam: true));
			if (list == null || list.Count == 0)
			{
				return shareData;
			}
			foreach (ShareUserModel shareUser in shareUsers.Where((ShareUserModel user) => string.IsNullOrWhiteSpace(user.phone)))
			{
				ShareUserModel shareUserModel = list.FirstOrDefault((ShareUserModel user) => !string.IsNullOrWhiteSpace(user.phone) && IsSameShareUser(shareUser, user));
				if (shareUserModel != null)
				{
					shareUser.phone = shareUserModel.phone;
				}
			}
			return JsonConvert.SerializeObject(shareUsers);
		}
		catch (Exception ex)
		{
			UtilLog.Warn("Merge share user phones failed: " + ex.Message);
			return shareData;
		}
	}

	private static bool IsSameShareUser(ShareUserModel left, ShareUserModel right)
	{
		if (left != null && right != null)
		{
			if ((string.IsNullOrWhiteSpace(left.recordId) || !string.Equals(left.recordId, right.recordId, StringComparison.Ordinal)) && (string.IsNullOrWhiteSpace(left.userCode) || !string.Equals(left.userCode, right.userCode, StringComparison.Ordinal)) && (!left.userId.HasValue || left.userId != right.userId))
			{
				if (!string.IsNullOrWhiteSpace(left.username))
				{
					return string.Equals(left.username, right.username, StringComparison.OrdinalIgnoreCase);
				}
				return false;
			}
			return true;
		}
		return false;
	}

	private static async Task<ShareUserModel> GetUserModel(string assignId, string projectId)
	{
		List<ShareUserModel> list = await GetProjectUsersAsync(projectId);
		if (list != null && list.Any())
		{
			ShareUserModel shareUserModel = list.FirstOrDefault((ShareUserModel avatar) => avatar.userId.ToString() == assignId);
			if (shareUserModel != null)
			{
				return shareUserModel;
			}
		}
		return null;
	}

	public static string GetCacheUrl(string assignId, string projectId)
	{
		List<AvatarViewModel> cacheProjectAvatars = GetCacheProjectAvatars(projectId);
		if (cacheProjectAvatars.Any())
		{
			AvatarViewModel avatarViewModel = cacheProjectAvatars.FirstOrDefault((AvatarViewModel p) => p.UserId == assignId);
			if (avatarViewModel != null)
			{
				return avatarViewModel.AvatarUrl;
			}
		}
		return string.Empty;
	}

	public static async Task<string> GetAvatarUrl(string assignId, string projectId)
	{
		ShareUserModel shareUserModel = await GetUserModel(assignId, projectId);
		return (shareUserModel != null) ? shareUserModel.avatarUrl : string.Empty;
	}

	public static async Task<string> GetUserName(string assignId, string projectId)
	{
		ShareUserModel shareUserModel = await GetUserModel(assignId, projectId);
		return (shareUserModel != null) ? GetUserName(shareUserModel) : string.Empty;
	}

	private static string GetUserName(ShareUserModel model)
	{
		if (!string.IsNullOrEmpty(model.displayName))
		{
			return model.displayName;
		}
		if (!string.IsNullOrEmpty(model.username))
		{
			return model.username;
		}
		return string.Empty;
	}

	public static string GetCacheUserName(string assignId, string projectId)
	{
		if (assignId == Utils.GetCurrentUserIdInt().ToString())
		{
			return Utils.GetString("Me");
		}
		ConcurrentDictionary<string, List<ShareUserModel>> projectUserCache = GetProjectUserCache(projectId);
		if (projectUserCache.ContainsKey(projectId))
		{
			ShareUserModel shareUserModel = projectUserCache[projectId].FirstOrDefault((ShareUserModel avatar) => avatar.userId.ToString() == assignId);
			if (shareUserModel != null)
			{
				return GetUserName(shareUserModel);
			}
		}
		return string.Empty;
	}

	public static BitmapImage GetAvatarByUrl(string avatarUrl, int width = 64, bool getDefault = true)
	{
		if (string.IsNullOrEmpty(avatarUrl) || avatarUrl == "-1")
		{
			return null;
		}
		if (avatarUrl.Contains("/Assets/logo.png"))
		{
			return new BitmapImage(new Uri("pack://application:,,,/Assets/logo.png"));
		}
		if (!avatarUrl.Contains("avatar-new"))
		{
			string key = avatarUrl + width;
			if (Avatars.ContainsKey(key))
			{
				return Avatars[key];
			}
			if (!getDefault)
			{
				return null;
			}
		}
		return GetDefaultAvatar();
	}

	public static async Task<BitmapImage> GetAvatarByUrlAsync(string avatarUrl, int width = 64)
	{
		if (string.IsNullOrEmpty(avatarUrl) || avatarUrl == "-1")
		{
			return null;
		}
		string key = avatarUrl + width;
		if (Avatars.ContainsKey(key) && Avatars[key] != null)
		{
			return Avatars[key];
		}
		if (avatarUrl.Contains("/Assets/logo.png"))
		{
			return new BitmapImage(new Uri("pack://application:,,,/Assets/logo.png"));
		}
		if (!(avatarUrl.Split('?').FirstOrDefault()?.Contains("avatar-new") ?? true))
		{
			int retry = 0;
			while (Avatars.ContainsKey(key))
			{
				BitmapImage bitmapImage = Avatars[key];
				if (retry > 20)
				{
					return GetDefaultAvatar();
				}
				if (bitmapImage != null)
				{
					return bitmapImage;
				}
				await Task.Delay(100);
				retry++;
			}
			Avatars[key] = null;
			try
			{
				if (!Directory.Exists(AppPaths.AvatarDir))
				{
					Directory.CreateDirectory(AppPaths.AvatarDir);
				}
				int hashCode = avatarUrl.GetHashCode();
				string path = AppPaths.AvatarDir + hashCode + ".png";
				if ((!File.Exists(path) || new FileInfo(path).Length == 0L) && !(await Utils.TryDownloadFile(avatarUrl, path)))
				{
					Avatars.TryRemove(key, out var _);
					return GetDefaultAvatar();
				}
				if (File.Exists(path) && new FileInfo(path).Length > 0)
				{
					BitmapImage imageByUrl = ImageUtils.GetImageByUrl(path, width);
					Avatars[key] = imageByUrl;
					return imageByUrl;
				}
				if (Avatars.ContainsKey(key))
				{
					Avatars.TryRemove(key, out var _);
				}
			}
			catch (Exception ex)
			{
				UtilLog.Info("GetAvatarByUrlAsync Error : " + avatarUrl + " \r\n " + ex.Message);
			}
		}
		return GetDefaultAvatar();
	}

	public static BitmapImage GetDefaultAvatar()
	{
		if (ThemeUtil.IsDark())
		{
			return new BitmapImage(new Uri("pack://application:,,,/Assets/avatar-new-dark.png"));
		}
		return new BitmapImage(new Uri("pack://application:,,,/Assets/avatar-new.png"));
	}

	public static async Task<BitmapImage> GetNotionAvatarByUrlAsync(string avatarUrl, int width = 40)
	{
		if (string.IsNullOrEmpty(avatarUrl) || avatarUrl == "-1")
		{
			return null;
		}
		string key = avatarUrl + width;
		if (Avatars.ContainsKey(key) && Avatars[key] != null)
		{
			return Avatars[key];
		}
		int retry = 0;
		while (Avatars.ContainsKey(key))
		{
			BitmapImage bitmapImage = Avatars[key];
			if (retry > 20)
			{
				return null;
			}
			if (bitmapImage != null)
			{
				return bitmapImage;
			}
			await Task.Delay(100);
			retry++;
		}
		Avatars[key] = null;
		try
		{
			if (!Directory.Exists(AppPaths.AvatarDir))
			{
				Directory.CreateDirectory(AppPaths.AvatarDir);
			}
			int hashCode = avatarUrl.GetHashCode();
			string path = AppPaths.AvatarDir + hashCode + ".png";
			if ((!File.Exists(path) || new FileInfo(path).Length == 0L) && !(await Utils.TryDownloadFile(avatarUrl, path)))
			{
				Avatars.TryRemove(key, out var _);
				return null;
			}
			if (File.Exists(path) && new FileInfo(path).Length > 0)
			{
				BitmapImage imageByUrl = ImageUtils.GetImageByUrl(path, width);
				Avatars[key] = imageByUrl;
				return imageByUrl;
			}
			if (Avatars.ContainsKey(key))
			{
				Avatars.TryRemove(key, out var _);
			}
		}
		catch (Exception ex)
		{
			UtilLog.Info("GetAvatarByUrlAsync Error : " + avatarUrl + " \r\n " + ex.Message);
		}
		return null;
	}

	public static void Clear()
	{
		ShareListCache.Clear();
		UserListCache.Clear();
		UserCache.Clear();
	}

	public static void RemoveProjectUser(string projectId, long userId)
	{
		if (string.IsNullOrEmpty(projectId))
		{
			return;
		}
		if (ShareListCache.TryGetValue(projectId, out var value))
		{
			value?.RemoveAll((ShareUserModel u) => u.userId == userId);
		}
		if (UserListCache.TryGetValue(projectId, out var value2))
		{
			value2?.RemoveAll((ShareUserModel u) => u.userId == userId);
		}
	}

	public static List<AvatarViewModel> GetProjectAvatarsFromCacheInGroup(string groupId)
	{
		List<AvatarViewModel> list = new List<AvatarViewModel>();
		List<ProjectModel> projectsInGroup = CacheManager.GetProjectsInGroup(groupId);
		projectsInGroup.Sort((ProjectModel a, ProjectModel b) => a.sortOrder.CompareTo(b.sortOrder));
		foreach (ProjectModel item in projectsInGroup)
		{
			foreach (AvatarViewModel avatar in GetProjectAvatarsFromCache(item.id, sort: true))
			{
				if (!list.Any((AvatarViewModel u) => u.UserId == avatar.UserId))
				{
					list.Add(avatar);
				}
			}
		}
		return list;
	}
}
