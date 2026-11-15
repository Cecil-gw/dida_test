using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SQLite;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service.SortOrder;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Sync;

namespace ticktick_WPF.Dal;

public static class MatrixProfileDao
{
	private static readonly SemaphoreSlim MigrationLock = new SemaphoreSlim(1, 1);

	private static string UserId => LocalSettings.Settings.LoginUserId;

	public static int GetProfileLimit()
	{
		return (int)LimitCache.GetLimitByKey(Constants.LimitKind.MatrixProfileNumber);
	}

	public static void EnsureMigratedSync()
	{
		RunSync(EnsureMigratedAsync);
	}

	public static async Task EnsureMigratedAsync()
	{
		if (App.Connection == null || string.IsNullOrEmpty(UserId))
		{
			return;
		}
		await MigrationLock.WaitAsync();
		try
		{
			string text = LocalSettings.Settings.MatrixModel?.LegacyActiveViewId;
			if (!string.IsNullOrEmpty(text))
			{
				Dictionary<string, string> dictionary = LocalSettings.Settings.Common?.GetOtherSettings().MatrixActiveViewIds;
				if (dictionary == null || !dictionary.ContainsKey(UserId))
				{
					LocalSettings.Settings.MatrixActiveViewId = text;
				}
				LocalSettings.Settings.MatrixModel.LegacyActiveViewId = null;
				await LocalSettings.Settings.Save();
			}
			List<MatrixProfileModel> list = await GetProfilesAsync();
			if (list.Any())
			{
				await EnsureLocalSelectionAsync(list);
				return;
			}
			MatrixModel matrixModel = LocalSettings.Settings.MatrixModel ?? MatrixModel.GetDefault();
			MatrixProfileModel profile = MatrixProfileModel.Create(Utils.GetGuid(), null, 0L, Constants.SyncStatus.SYNC_INIT.ToString());
			List<MatrixProfileQuadrantModel> quadrants = new List<MatrixProfileQuadrantModel>();
			int i;
			for (i = 1; i <= 4; i++)
			{
				QuadrantModel quadrant = matrixModel.quadrants?.FirstOrDefault((QuadrantModel q) => q.id == "quadrant" + i) ?? QuadrantModel.GetDefault(i);
				quadrants.Add(MatrixProfileQuadrantModel.Create(profile.sid, i, CloneQuadrant(quadrant), Constants.SyncStatus.SYNC_INIT.ToString()));
			}
			await App.Connection.RunInTransactionAsync(delegate(SQLiteConnection connection)
			{
				if (connection.Insert(profile) != 1)
				{
					throw new InvalidOperationException("Failed to create the initial matrix profile.");
				}
				foreach (MatrixProfileQuadrantModel item in quadrants)
				{
					if (connection.Insert(item) != 1)
					{
						throw new InvalidOperationException("Failed to create an initial matrix quadrant.");
					}
				}
				SyncSortOrderDao.CopyLegacyMatrixOrders(connection, profile.userId, profile.sid);
			});
			LocalSettings.Settings.MatrixActiveViewId = profile.sid;
			await LocalSettings.Settings.Save();
		}
		catch (Exception ex)
		{
			UtilLog.Warn("MatrixProfileDao.EnsureMigratedAsync " + ex.Message);
		}
		finally
		{
			MigrationLock.Release();
		}
	}

	public static List<MatrixProfileModel> GetProfilesSync(bool includeDeleted = false)
	{
		EnsureMigratedSync();
		return RunSync(() => GetProfilesAsync(includeDeleted));
	}

	public static async Task<List<MatrixProfileModel>> GetProfilesAsync(bool includeDeleted = false)
	{
		List<MatrixProfileModel> list = await (from m in App.Connection.Table<MatrixProfileModel>()
			where m.userId == UserId
			select m).ToListAsync();
		if (!includeDeleted)
		{
			list = list.Where((MatrixProfileModel m) => !m.deleted).ToList();
		}
		list.Sort((MatrixProfileModel a, MatrixProfileModel b) => a.sortOrder.GetValueOrDefault().CompareTo(b.sortOrder.GetValueOrDefault()));
		return list;
	}

	public static async Task<MatrixProfileModel> GetActiveProfileAsync()
	{
		await EnsureMigratedAsync();
		return GetActiveProfile(await GetProfilesAsync());
	}

	public static MatrixProfileModel GetActiveProfileSync()
	{
		EnsureMigratedSync();
		return RunSync(GetActiveProfileAsync);
	}

	public static async Task<List<QuadrantModel>> GetActiveQuadrantsAsync()
	{
		MatrixProfileModel matrixProfileModel = await GetActiveProfileAsync();
		if (matrixProfileModel == null)
		{
			return MatrixModel.GetDefault().quadrants;
		}
		return BuildQuadrants(await GetQuadrantsAsync(matrixProfileModel.sid));
	}

	public static async Task<List<QuadrantModel>> GetProfileQuadrantsAsync(string sid)
	{
		await EnsureMigratedAsync();
		return BuildQuadrants(await GetQuadrantsAsync(sid));
	}

	public static List<QuadrantModel> GetProfileQuadrantsSync(string sid)
	{
		EnsureMigratedSync();
		return RunSync(() => GetProfileQuadrantsAsync(sid));
	}

	public static List<QuadrantModel> GetActiveQuadrantsSync()
	{
		EnsureMigratedSync();
		return RunSync(GetActiveQuadrantsAsync);
	}

	public static async Task<MatrixProfileModel> AddProfileFromCurrentAsync(string name)
	{
		await EnsureMigratedAsync();
		List<MatrixProfileModel> list = await GetProfilesAsync();
		if (list.Count >= GetProfileLimit())
		{
			return null;
		}
		string name2 = NormalizeName(name);
		if (NameExists(list, name2, null))
		{
			throw new InvalidOperationException("MatrixNameExists");
		}
		string sid = Utils.GetGuid();
		MatrixProfileModel profile = MatrixProfileModel.Create(sid, name2, list.Any() ? (list.Max((MatrixProfileModel p) => p.sortOrder.GetValueOrDefault()) + 268435456) : 0, Constants.SyncStatus.SYNC_NEW.ToString());
		List<QuadrantModel> source = await GetActiveQuadrantsAsync();
		List<MatrixProfileQuadrantModel> quadrants = new List<MatrixProfileQuadrantModel>();
		int i;
		for (i = 1; i <= 4; i++)
		{
			QuadrantModel quadrant = source.FirstOrDefault((QuadrantModel q) => q.id == "quadrant" + i) ?? QuadrantModel.GetDefault(i);
			quadrants.Add(MatrixProfileQuadrantModel.Create(sid, i, CloneQuadrant(quadrant), Constants.SyncStatus.SYNC_NEW.ToString()));
		}
		await App.Connection.InsertAsync(profile);
		await App.Connection.InsertAllAsync(quadrants);
		SyncManager.TryDelaySync();
		return profile;
	}

	public static async Task SaveProfileAsync(string sid, string name, List<QuadrantModel> quadrants, bool makeActive)
	{
		await EnsureMigratedAsync();
		List<MatrixProfileModel> list = await GetProfilesAsync();
		MatrixProfileModel matrixProfileModel = list.FirstOrDefault((MatrixProfileModel p) => p.sid == sid);
		if (matrixProfileModel == null)
		{
			return;
		}
		string persistedName = GetPersistedName(matrixProfileModel, name);
		if (NameExists(list, GetNameForDuplicateCheck(matrixProfileModel, persistedName), sid))
		{
			throw new InvalidOperationException("MatrixNameExists");
		}
		bool changed = false;
		if (matrixProfileModel.name != persistedName)
		{
			matrixProfileModel.name = persistedName;
			Touch(matrixProfileModel);
			await App.Connection.UpdateAsync(matrixProfileModel);
			changed = true;
		}
		List<MatrixProfileQuadrantModel> dbQuadrants = await GetQuadrantsAsync(sid);
		int i;
		for (i = 1; i <= 4; i++)
		{
			QuadrantModel quadrantModel = quadrants?.FirstOrDefault((QuadrantModel q) => q.id == "quadrant" + i) ?? QuadrantModel.GetDefault(i);
			MatrixProfileQuadrantModel dbQuadrant = dbQuadrants.FirstOrDefault((MatrixProfileQuadrantModel q) => q.quadrant == i);
			if (dbQuadrant == null)
			{
				dbQuadrant = MatrixProfileQuadrantModel.Create(sid, i, quadrantModel, Constants.SyncStatus.SYNC_NEW.ToString());
				await App.Connection.InsertAsync(dbQuadrant);
				dbQuadrants.Add(dbQuadrant);
				changed = true;
			}
			else if (!SameQuadrant(dbQuadrant.ToQuadrantModel(), quadrantModel))
			{
				dbQuadrant.Apply(quadrantModel);
				Touch(dbQuadrant);
				await App.Connection.UpdateAsync(dbQuadrant);
				changed = true;
			}
		}
		if (makeActive || (changed && IsMainActiveProfile(sid)))
		{
			await SetActiveProfileAsync(sid, notify: false);
		}
		if (changed)
		{
			SyncManager.TryDelaySync();
		}
	}

	public static async Task SaveProfileNameAsync(string sid, string name, bool makeActive)
	{
		await EnsureMigratedAsync();
		List<MatrixProfileModel> list = await GetProfilesAsync();
		MatrixProfileModel matrixProfileModel = list.FirstOrDefault((MatrixProfileModel p) => p.sid == sid);
		if (matrixProfileModel != null)
		{
			string persistedName = GetPersistedName(matrixProfileModel, name);
			if (NameExists(list, GetNameForDuplicateCheck(matrixProfileModel, persistedName), sid))
			{
				throw new InvalidOperationException("MatrixNameExists");
			}
			bool changed = false;
			if (matrixProfileModel.name != persistedName)
			{
				matrixProfileModel.name = persistedName;
				Touch(matrixProfileModel);
				await App.Connection.UpdateAsync(matrixProfileModel);
				changed = true;
			}
			if (makeActive && LocalSettings.Settings.MatrixActiveViewId != sid)
			{
				await SetActiveProfileAsync(sid, notify: false);
			}
			if (changed)
			{
				SyncManager.TryDelaySync();
			}
		}
	}

	public static async Task DeleteProfileAsync(string sid)
	{
		await EnsureMigratedAsync();
		List<MatrixProfileModel> profiles = await GetProfilesAsync();
		if (profiles.Count <= 1)
		{
			return;
		}
		MatrixProfileModel profile = profiles.FirstOrDefault((MatrixProfileModel p) => p.sid == sid);
		if (profile == null)
		{
			return;
		}
		if (IsLocalOnly(profile))
		{
			await ClearProfileTaskOrdersAsync(profile.sid);
			MatrixProfileModel next = ((LocalSettings.Settings.MatrixActiveViewId == sid) ? profiles.FirstOrDefault((MatrixProfileModel p) => p.sid != sid) : null);
			foreach (MatrixProfileQuadrantModel item in await GetQuadrantsAsync(sid))
			{
				await App.Connection.DeleteAsync(item);
			}
			await App.Connection.DeleteAsync(profile);
			if (next != null)
			{
				await SetActiveProfileAsync(next.sid, notify: false);
			}
			return;
		}
		profile.deleted = true;
		Touch(profile);
		await App.Connection.UpdateAsync(profile);
		await ClearProfileTaskOrdersAsync(profile.sid);
		List<MatrixProfileQuadrantModel> list = await GetQuadrantsAsync(sid);
		foreach (MatrixProfileQuadrantModel item2 in list)
		{
			item2.deleted = true;
			Touch(item2);
		}
		if (list.Any())
		{
			await App.Connection.UpdateAllAsync(list);
		}
		if (LocalSettings.Settings.MatrixActiveViewId == sid)
		{
			MatrixProfileModel matrixProfileModel = profiles.FirstOrDefault((MatrixProfileModel p) => p.sid != sid);
			if (matrixProfileModel != null)
			{
				await SetActiveProfileAsync(matrixProfileModel.sid, notify: false);
			}
		}
		SyncManager.TryDelaySync();
	}

	public static async Task ReorderProfilesAsync(List<string> orderedSids)
	{
		await EnsureMigratedAsync();
		if (orderedSids == null || orderedSids.Count == 0)
		{
			return;
		}
		List<MatrixProfileModel> list = await GetProfilesAsync();
		int i;
		for (i = 0; i < orderedSids.Count; i++)
		{
			MatrixProfileModel matrixProfileModel = list.FirstOrDefault((MatrixProfileModel p) => p.sid == orderedSids[i]);
			if (matrixProfileModel != null)
			{
				long num = (long)i * 268435456L;
				if (matrixProfileModel.sortOrder != num)
				{
					matrixProfileModel.sortOrder = num;
					Touch(matrixProfileModel);
				}
			}
		}
		await App.Connection.UpdateAllAsync(list);
		SyncManager.TryDelaySync();
	}

	public static async Task SetActiveProfileAsync(string sid, bool notify = true)
	{
		await EnsureMigratedAsync();
		List<MatrixProfileModel> list = await GetProfilesAsync();
		MatrixProfileModel matrixProfileModel = list.FirstOrDefault((MatrixProfileModel p) => p.sid == sid) ?? GetActiveProfile(list);
		if (matrixProfileModel != null)
		{
			LocalSettings.Settings.MatrixActiveViewId = matrixProfileModel.sid;
			await LocalSettings.Settings.Save();
			if (notify)
			{
				DataChangedNotifier.NotifyMatrixQuadrantChanged(0);
			}
		}
	}

	public static void UpdateCurrentQuadrantSync(int level, string rule, SortOption sort, string name)
	{
		RunSync(() => UpdateCurrentQuadrantAsync(level, rule, sort, name));
	}

	public static async Task UpdateCurrentQuadrantAsync(int level, string rule, SortOption sort, string name)
	{
		await EnsureMigratedAsync();
		MatrixProfileModel matrixProfileModel = await GetActiveProfileAsync();
		if (matrixProfileModel != null)
		{
			await UpdateProfileQuadrantAsync(matrixProfileModel, level, rule, sort, name);
		}
	}

	public static async Task UpdateProfileQuadrantAsync(string sid, int level, string rule, SortOption sort, string name)
	{
		await EnsureMigratedAsync();
		MatrixProfileModel matrixProfileModel = (await GetProfilesAsync()).FirstOrDefault((MatrixProfileModel p) => p.sid == sid);
		if (matrixProfileModel != null)
		{
			await UpdateProfileQuadrantAsync(matrixProfileModel, level, rule, sort, name);
		}
	}

	private static async Task UpdateProfileQuadrantAsync(MatrixProfileModel profile, int level, string rule, SortOption sort, string name)
	{
		List<MatrixProfileQuadrantModel> list = await GetQuadrantsAsync(profile.sid);
		MatrixProfileQuadrantModel dbQuadrant = list.FirstOrDefault((MatrixProfileQuadrantModel q) => q.quadrant == level);
		if (dbQuadrant == null)
		{
			dbQuadrant = MatrixProfileQuadrantModel.Create(profile.sid, level, QuadrantModel.GetDefault(level), Constants.SyncStatus.SYNC_NEW.ToString());
			list.Add(dbQuadrant);
			await App.Connection.InsertAsync(dbQuadrant);
		}
		if (rule != null)
		{
			dbQuadrant.rule = rule;
		}
		if (sort != null)
		{
			dbQuadrant.SortOption = sort;
			dbQuadrant.sortType = ((sort.groupByType == Constants.SortType.none) ? sort.orderBy : sort.groupBy);
		}
		if (name != null)
		{
			dbQuadrant.name = ((name == string.Empty && string.IsNullOrWhiteSpace(dbQuadrant.name)) ? null : ((name == string.Empty) ? Utils.GetString("MatrixTitleQuadrant" + level) : name));
		}
		Touch(dbQuadrant);
		await App.Connection.UpdateAsync(dbQuadrant);
		SyncManager.TryDelaySync();
	}

	public static void UpdateCurrentQuadrantOrdersSync(Dictionary<string, long> orders)
	{
		RunSync(() => UpdateCurrentQuadrantOrdersAsync(orders));
	}

	public static async Task UpdateCurrentQuadrantOrdersAsync(Dictionary<string, long> orders)
	{
		await EnsureMigratedAsync();
		MatrixProfileModel matrixProfileModel = await GetActiveProfileAsync();
		if (matrixProfileModel != null && orders != null)
		{
			await UpdateProfileQuadrantOrdersAsync(matrixProfileModel.sid, orders);
		}
	}

	public static void UpdateProfileQuadrantOrdersSync(string sid, Dictionary<string, long> orders)
	{
		RunSync(() => UpdateProfileQuadrantOrdersAsync(sid, orders));
	}

	public static async Task UpdateProfileQuadrantOrdersAsync(string sid, Dictionary<string, long> orders)
	{
		await EnsureMigratedAsync();
		if (string.IsNullOrEmpty(sid) || orders == null)
		{
			return;
		}
		MatrixProfileModel matrixProfileModel = (await GetProfilesAsync()).FirstOrDefault((MatrixProfileModel p) => p.sid == sid);
		if (matrixProfileModel == null)
		{
			return;
		}
		List<MatrixProfileQuadrantModel> list = await GetQuadrantsAsync(matrixProfileModel.sid);
		foreach (MatrixProfileQuadrantModel item in list)
		{
			string key = "quadrant" + item.quadrant;
			if (orders.ContainsKey(key))
			{
				item.sortOrder = orders[key];
				Touch(item);
			}
		}
		if (list.Any())
		{
			await App.Connection.UpdateAllAsync(list);
		}
		SyncManager.TryDelaySync();
	}

	public static async Task<bool> UpdateAffectedQuadrantsOnTagChangedAsync(string original, string revised)
	{
		await EnsureMigratedAsync();
		if (string.IsNullOrEmpty(original))
		{
			return false;
		}
		string originalTagContent = "\"" + original + "\"";
		string revisedTagContent = "\"" + revised?.ToLower() + "\"";
		List<MatrixProfileQuadrantModel> obj = await (from q in App.Connection.Table<MatrixProfileQuadrantModel>()
			where q.userId == UserId && q.deleted == false
			select q).ToListAsync();
		List<MatrixProfileQuadrantModel> list = new List<MatrixProfileQuadrantModel>();
		foreach (MatrixProfileQuadrantModel item in obj)
		{
			if (item.rule != null && item.rule.Contains(originalTagContent) && !item.rule.Contains(revisedTagContent))
			{
				item.rule = item.rule.Replace(originalTagContent, revisedTagContent);
				Touch(item);
				list.Add(item);
			}
		}
		if (!list.Any())
		{
			return false;
		}
		await App.Connection.UpdateAllAsync(list);
		SyncManager.TryDelaySync();
		return true;
	}

	public static async Task MergeFromServerAsync(List<MatrixProfileModel> serverProfiles, List<MatrixProfileQuadrantModel> serverQuadrants)
	{
		if (serverProfiles == null && serverQuadrants == null)
		{
			return;
		}
		List<MatrixProfileModel> list = serverProfiles;
		if (list == null || !list.Any())
		{
			await EnsureMigratedAsync();
		}
		List<MatrixProfileModel> localProfiles = await GetProfilesAsync(includeDeleted: true);
		List<MatrixProfileQuadrantModel> localQuadrants = await (from q in App.Connection.Table<MatrixProfileQuadrantModel>()
			where q.userId == UserId
			select q).ToListAsync();
		if (serverProfiles?.Any() ?? false)
		{
			List<string> initProfileIds = (from p in localProfiles
				where p.status == Constants.SyncStatus.SYNC_INIT.ToString() && !serverProfiles.Any((MatrixProfileModel remote) => remote.sid == p.sid)
				select p.sid).ToList();
			foreach (MatrixProfileModel profile in localProfiles.Where((MatrixProfileModel p) => initProfileIds.Contains(p.sid)).ToList())
			{
				await ClearProfileTaskOrdersAsync(profile.sid);
				await App.Connection.DeleteAsync(profile);
			}
			foreach (MatrixProfileQuadrantModel item in localQuadrants.Where((MatrixProfileQuadrantModel q) => initProfileIds.Contains(q.matrixSid)).ToList())
			{
				await App.Connection.DeleteAsync(item);
			}
			localProfiles.RemoveAll((MatrixProfileModel p) => initProfileIds.Contains(p.sid));
			localQuadrants.RemoveAll((MatrixProfileQuadrantModel q) => initProfileIds.Contains(q.matrixSid));
		}
		List<MatrixProfileModel> profileUpdates = new List<MatrixProfileModel>();
		List<MatrixProfileModel> profileAdds = new List<MatrixProfileModel>();
		foreach (MatrixProfileModel profile2 in serverProfiles ?? new List<MatrixProfileModel>())
		{
			profile2.userId = UserId;
			MatrixProfileModel matrixProfileModel = localProfiles.FirstOrDefault((MatrixProfileModel p) => p.sid == profile2.sid);
			if (matrixProfileModel == null)
			{
				profile2.status = Constants.SyncStatus.SYNC_DONE.ToString();
				profileAdds.Add(profile2);
			}
			else if (ShouldAcceptRemote(matrixProfileModel, profile2))
			{
				profile2._Id = matrixProfileModel._Id;
				profile2.status = Constants.SyncStatus.SYNC_DONE.ToString();
				profileUpdates.Add(profile2);
			}
		}
		List<MatrixProfileQuadrantModel> quadrantUpdates = new List<MatrixProfileQuadrantModel>();
		List<MatrixProfileQuadrantModel> quadrantAdds = new List<MatrixProfileQuadrantModel>();
		foreach (MatrixProfileQuadrantModel quadrant in serverQuadrants ?? new List<MatrixProfileQuadrantModel>())
		{
			quadrant.userId = UserId;
			MatrixProfileQuadrantModel matrixProfileQuadrantModel = localQuadrants.FirstOrDefault((MatrixProfileQuadrantModel q) => q.id == quadrant.id);
			if (matrixProfileQuadrantModel == null)
			{
				quadrant.status = Constants.SyncStatus.SYNC_DONE.ToString();
				quadrantAdds.Add(quadrant);
			}
			else if (ShouldAcceptRemote(matrixProfileQuadrantModel, quadrant))
			{
				quadrant._Id = matrixProfileQuadrantModel._Id;
				quadrant.status = Constants.SyncStatus.SYNC_DONE.ToString();
				quadrantUpdates.Add(quadrant);
			}
		}
		if (profileAdds.Any())
		{
			await App.Connection.InsertAllAsync(profileAdds);
		}
		if (quadrantAdds.Any())
		{
			await App.Connection.InsertAllAsync(quadrantAdds);
		}
		await ApplyRemoteUpdatesAsync(profileUpdates, quadrantUpdates);
		await BindConfirmedInitialProfileAsync(localProfiles, new HashSet<string>(from p in serverProfiles ?? new List<MatrixProfileModel>()
			where !p.deleted
			select p.sid));
		await ConfirmQuotaProfilesAsync(localProfiles, localQuadrants, serverProfiles ?? new List<MatrixProfileModel>(), serverQuadrants ?? new List<MatrixProfileQuadrantModel>());
		foreach (MatrixProfileModel item2 in from p in profileAdds.Concat(profileUpdates)
			where p.deleted
			select p)
		{
			await ClearProfileTaskOrdersAsync(item2.sid);
		}
		await EnsureLocalSelectionAsync(await GetProfilesAsync(), allowFallback: true);
	}

	public static async Task<bool> MergeFromServerAsync(List<MatrixProfileSyncModel> serverProfiles, ISet<string> committedProfileIds = null, bool authoritative = true)
	{
		if (serverProfiles == null)
		{
			return false;
		}
		if (authoritative && !serverProfiles.Any())
		{
			await EnsureMigratedAsync();
		}
		bool changed = false;
		List<MatrixProfileModel> localProfiles = await GetProfilesAsync(includeDeleted: true);
		if (!authoritative && localProfiles.Any((MatrixProfileModel p) => IsInitialStatus(p.status)))
		{
			return false;
		}
		List<MatrixProfileQuadrantModel> localQuadrants = await (from q in App.Connection.Table<MatrixProfileQuadrantModel>()
			where q.userId == UserId
			select q).ToListAsync();
		if (serverProfiles.Any())
		{
			List<string> initProfileIds = (from p in localProfiles
				where p.status == Constants.SyncStatus.SYNC_INIT.ToString() && !serverProfiles.Any((MatrixProfileSyncModel remote) => remote?.id == p.sid)
				select p.sid).ToList();
			foreach (MatrixProfileModel profile in localProfiles.Where((MatrixProfileModel p) => initProfileIds.Contains(p.sid)).ToList())
			{
				await ClearProfileTaskOrdersAsync(profile.sid);
				await App.Connection.DeleteAsync(profile);
			}
			foreach (MatrixProfileQuadrantModel item in localQuadrants.Where((MatrixProfileQuadrantModel q) => initProfileIds.Contains(q.matrixSid)).ToList())
			{
				await App.Connection.DeleteAsync(item);
			}
			if (initProfileIds.Any())
			{
				localProfiles.RemoveAll((MatrixProfileModel p) => initProfileIds.Contains(p.sid));
				localQuadrants.RemoveAll((MatrixProfileQuadrantModel q) => initProfileIds.Contains(q.matrixSid));
				changed = true;
			}
		}
		List<MatrixProfileModel> profileUpdates = new List<MatrixProfileModel>();
		List<MatrixProfileModel> profileAdds = new List<MatrixProfileModel>();
		List<MatrixProfileQuadrantModel> quadrantUpdates = new List<MatrixProfileQuadrantModel>();
		List<MatrixProfileQuadrantModel> quadrantAdds = new List<MatrixProfileQuadrantModel>();
		foreach (MatrixProfileSyncModel serverProfile in serverProfiles.Where((MatrixProfileSyncModel p) => !string.IsNullOrEmpty(p?.id)))
		{
			MatrixProfileModel matrixProfileModel = localProfiles.FirstOrDefault((MatrixProfileModel p) => p.sid == serverProfile.id);
			MatrixProfileModel matrixProfileModel2 = ToLocalProfile(serverProfile, matrixProfileModel);
			if (matrixProfileModel == null)
			{
				profileAdds.Add(matrixProfileModel2);
				changed = true;
			}
			else if ((ShouldAcceptRemote(matrixProfileModel, matrixProfileModel2) && !SameProfile(matrixProfileModel, matrixProfileModel2)) || ((committedProfileIds?.Contains(serverProfile.id) ?? false) && !string.IsNullOrEmpty(matrixProfileModel2.etag) && matrixProfileModel.etag != matrixProfileModel2.etag))
			{
				profileUpdates.Add(matrixProfileModel2);
				changed = true;
			}
			foreach (MatrixProfileQuadrantSyncModel item2 in serverProfile.matrixs ?? new List<MatrixProfileQuadrantSyncModel>())
			{
				int level = GetSyncQuadrantLevel(item2);
				if (level >= 1 && level <= 4)
				{
					MatrixProfileQuadrantModel matrixProfileQuadrantModel = localQuadrants.FirstOrDefault((MatrixProfileQuadrantModel q) => q.matrixSid == serverProfile.id && q.quadrant == level);
					MatrixProfileQuadrantModel matrixProfileQuadrantModel2 = ToLocalQuadrant(serverProfile.id, level, item2, matrixProfileQuadrantModel);
					if (matrixProfileQuadrantModel == null)
					{
						quadrantAdds.Add(matrixProfileQuadrantModel2);
						changed = true;
					}
					else if ((ShouldAcceptRemote(matrixProfileQuadrantModel, matrixProfileQuadrantModel2) && !SameQuadrant(matrixProfileQuadrantModel, matrixProfileQuadrantModel2)) || ((committedProfileIds?.Contains(serverProfile.id) ?? false) && !string.IsNullOrEmpty(matrixProfileQuadrantModel2.etag) && matrixProfileQuadrantModel.etag != matrixProfileQuadrantModel2.etag))
					{
						quadrantUpdates.Add(matrixProfileQuadrantModel2);
						changed = true;
					}
				}
			}
		}
		if (profileAdds.Any())
		{
			await App.Connection.InsertAllAsync(profileAdds);
		}
		if (quadrantAdds.Any())
		{
			await App.Connection.InsertAllAsync(quadrantAdds);
		}
		await ApplyRemoteUpdatesAsync(profileUpdates, quadrantUpdates, committedProfileIds);
		await BindConfirmedInitialProfileAsync(localProfiles, new HashSet<string>(from p in serverProfiles
			where p != null && p.deleted != 1
			select p.id));
		changed = await ConfirmQuotaProfilesAsync(localProfiles.Where(delegate(MatrixProfileModel p)
		{
			ISet<string> set = committedProfileIds;
			return set == null || !set.Contains(p.sid);
		}), localQuadrants, from p in serverProfiles
			where !string.IsNullOrEmpty(p?.id)
			select ToLocalProfile(p, null), serverProfiles.Where((MatrixProfileSyncModel p) => !string.IsNullOrEmpty(p?.id)).SelectMany((MatrixProfileSyncModel p) => from q in p.matrixs ?? new List<MatrixProfileQuadrantSyncModel>()
			where GetSyncQuadrantLevel(q) >= 1 && GetSyncQuadrantLevel(q) <= 4
			select ToLocalQuadrant(p.id, GetSyncQuadrantLevel(q), q, null))) || changed;
		foreach (MatrixProfileModel item3 in from p in profileAdds.Concat(profileUpdates)
			where p.deleted
			select p)
		{
			await ClearProfileTaskOrdersAsync(item3.sid);
		}
		if (authoritative)
		{
			changed = await MarkMissingRemoteProfilesDeletedAsync(localProfiles, localQuadrants, serverProfiles) || changed;
		}
		return await EnsureLocalSelectionAsync(await GetProfilesAsync(), authoritative) || changed;
	}

	private static Task ApplyRemoteUpdatesAsync(List<MatrixProfileModel> profiles, List<MatrixProfileQuadrantModel> quadrants, ISet<string> committedProfileIds = null)
	{
		string userId = UserId;
		return App.Connection.RunInTransactionAsync(delegate(SQLiteConnection connection)
		{
			foreach (MatrixProfileModel item in profiles.ToList())
			{
				MatrixProfileModel matrixProfileModel = connection.Find<MatrixProfileModel>(item._Id);
				if (matrixProfileModel == null || matrixProfileModel.userId != userId)
				{
					profiles.Remove(item);
				}
				else if (!ShouldAcceptRemote(matrixProfileModel, item))
				{
					ISet<string> set = committedProfileIds;
					if (set != null && set.Contains(matrixProfileModel.sid) && !string.IsNullOrEmpty(item.etag))
					{
						matrixProfileModel.etag = item.etag;
						connection.Update(matrixProfileModel);
					}
					profiles.Remove(item);
				}
				else
				{
					connection.Update(item);
				}
			}
			foreach (MatrixProfileQuadrantModel item2 in quadrants.ToList())
			{
				MatrixProfileQuadrantModel matrixProfileQuadrantModel = connection.Find<MatrixProfileQuadrantModel>(item2._Id);
				if (matrixProfileQuadrantModel == null || matrixProfileQuadrantModel.userId != userId)
				{
					quadrants.Remove(item2);
				}
				else if (!ShouldAcceptRemote(matrixProfileQuadrantModel, item2))
				{
					ISet<string> set2 = committedProfileIds;
					if (set2 != null && set2.Contains(matrixProfileQuadrantModel.matrixSid) && !string.IsNullOrEmpty(item2.etag))
					{
						matrixProfileQuadrantModel.etag = item2.etag;
						connection.Update(matrixProfileQuadrantModel);
					}
					quadrants.Remove(item2);
				}
				else
				{
					connection.Update(item2);
				}
			}
		});
	}

	public static async Task<ModelSyncBean<MatrixProfileModel>> BuildProfileSyncBeanAsync()
	{
		await EnsureMigratedAsync();
		ModelSyncBean<MatrixProfileModel> bean = new ModelSyncBean<MatrixProfileModel>();
		foreach (MatrixProfileModel item in (await GetProfilesAsync(includeDeleted: true)).Where(NeedPost))
		{
			if (item.deleted)
			{
				bean.Deleted.Add(item.sid);
			}
			else if (item.status == Constants.SyncStatus.SYNC_NEW.ToString() || IsInitialStatus(item.status))
			{
				bean.Add.Add(item);
			}
			else
			{
				bean.Update.Add(item);
			}
		}
		return bean;
	}

	public static async Task<ModelSyncBean<MatrixProfileSyncModel>> BuildSyncBeanAsync()
	{
		await EnsureMigratedAsync();
		ModelSyncBean<MatrixProfileSyncModel> bean = new ModelSyncBean<MatrixProfileSyncModel>();
		List<MatrixProfileModel> profiles = await GetProfilesAsync(includeDeleted: true);
		List<MatrixProfileQuadrantModel> list = await (from q in App.Connection.Table<MatrixProfileQuadrantModel>()
			where q.userId == UserId
			select q).ToListAsync();
		HashSet<string> changedProfileIds = new HashSet<string>(from id in (from p in profiles.Where(NeedPost)
				select p.sid).Union(from q in list.Where(NeedPost)
				select q.matrixSid)
			where !string.IsNullOrEmpty(id)
			select id);
		int num = profiles.Count((MatrixProfileModel p) => !IsLocalOnly(p) && (!p.deleted || NeedPost(p)));
		int num2 = Math.Max(0, GetProfileLimit() - num);
		foreach (MatrixProfileModel item in (from p in profiles
			where changedProfileIds.Contains(p.sid)
			orderby p.sortOrder.GetValueOrDefault()
			select p).ThenBy((MatrixProfileModel p) => p.sid, StringComparer.Ordinal))
		{
			if (item.deleted)
			{
				bean.Deleted.Add(item.sid);
			}
			else if (item.status == Constants.SyncStatus.SYNC_NEW.ToString() || IsInitialStatus(item.status))
			{
				if (num2 > 0)
				{
					bean.Add.Add(ToSyncProfile(item, list));
					num2--;
				}
			}
			else
			{
				bean.Update.Add(ToSyncProfile(item, list));
			}
		}
		return bean;
	}

	public static async Task<bool> HasPendingInitialProfilesAsync()
	{
		await EnsureMigratedAsync();
		if ((await GetProfilesAsync(includeDeleted: true)).Any((MatrixProfileModel p) => IsInitialStatus(p.status)))
		{
			return true;
		}
		string initialStatus = Constants.SyncStatus.SYNC_INIT.ToString();
		string quotaStatus = Constants.SyncStatus.SYNC_INIT_UP_LIMIT.ToString();
		return await (from q in App.Connection.Table<MatrixProfileQuadrantModel>()
			where q.userId == UserId && (q.status == initialStatus || q.status == quotaStatus)
			select q).CountAsync() > 0;
	}

	public static async Task<ModelSyncBean<MatrixProfileQuadrantModel>> BuildQuadrantSyncBeanAsync()
	{
		await EnsureMigratedAsync();
		ModelSyncBean<MatrixProfileQuadrantModel> bean = new ModelSyncBean<MatrixProfileQuadrantModel>();
		foreach (MatrixProfileQuadrantModel item in (await (from q in App.Connection.Table<MatrixProfileQuadrantModel>()
			where q.userId == UserId
			select q).ToListAsync()).Where(NeedPost))
		{
			if (item.deleted)
			{
				bean.Deleted.Add(item.id);
			}
			else if (item.status == Constants.SyncStatus.SYNC_NEW.ToString() || IsInitialStatus(item.status))
			{
				bean.Add.Add(item);
			}
			else
			{
				bean.Update.Add(item);
			}
		}
		return bean;
	}

	public static async Task MarkCommittedAsync(Dictionary<string, string> profileEtags, Dictionary<string, string> quadrantEtags)
	{
		if (profileEtags?.Any() ?? false)
		{
			List<MatrixProfileModel> list = await GetProfilesAsync(includeDeleted: true);
			foreach (MatrixProfileModel item in list.Where((MatrixProfileModel p) => profileEtags.ContainsKey(p.sid)))
			{
				item.etag = profileEtags[item.sid];
				item.status = Constants.SyncStatus.SYNC_DONE.ToString();
			}
			await App.Connection.UpdateAllAsync(list);
		}
		if (!(quadrantEtags?.Any() ?? false))
		{
			return;
		}
		List<MatrixProfileQuadrantModel> list2 = await (from q in App.Connection.Table<MatrixProfileQuadrantModel>()
			where q.userId == UserId
			select q).ToListAsync();
		foreach (MatrixProfileQuadrantModel item2 in list2.Where((MatrixProfileQuadrantModel q) => quadrantEtags.ContainsKey(q.id)))
		{
			item2.etag = quadrantEtags[item2.id];
			item2.status = Constants.SyncStatus.SYNC_DONE.ToString();
		}
		await App.Connection.UpdateAllAsync(list2);
	}

	public static async Task<bool> MarkCommittedAsync(BatchUpdateResult result, ModelSyncBean<MatrixProfileSyncModel> bean)
	{
		if (result == null || bean == null)
		{
			return false;
		}
		Dictionary<string, string> id2etag = result.Id2etag ?? new Dictionary<string, string>();
		Dictionary<string, string> id2error = result.Id2error ?? new Dictionary<string, string>();
		bool changed = await PreserveRejectedAddedProfilesAsync(bean, id2error);
		List<string> committedProfileIds = (from id in (from p in bean.Add.Concat(bean.Update)
				select p.id).Concat(bean.Deleted)
			where !string.IsNullOrEmpty(id) && !id2error.ContainsKey(id)
			select id).Distinct().ToList();
		if (!committedProfileIds.Any())
		{
			return changed;
		}
		await BindConfirmedInitialProfileAsync(await GetProfilesAsync(includeDeleted: true), new HashSet<string>(committedProfileIds));
		Dictionary<string, MatrixProfileSyncModel> submittedProfiles = bean.Add.Concat(bean.Update).ToDictionary((MatrixProfileSyncModel p) => p.id);
		string userId = UserId;
		await App.Connection.RunInTransactionAsync(delegate(SQLiteConnection connection)
		{
			foreach (MatrixProfileModel item in from p in (from p in connection.Table<MatrixProfileModel>()
					where p.userId == userId
					select p).ToList()
				where committedProfileIds.Contains(p.sid)
				select p)
			{
				submittedProfiles.TryGetValue(item.sid, out var value);
				bool num;
				if (value != null)
				{
					num = MatchesSubmittedProfile(item, value);
				}
				else
				{
					if (!item.deleted)
					{
						goto IL_00e8;
					}
					num = bean.Deleted.Contains(item.sid);
				}
				if (!num)
				{
					goto IL_00e8;
				}
				string text = Constants.SyncStatus.SYNC_DONE.ToString();
				goto IL_010a;
				IL_010a:
				string text2 = text;
				if (id2etag.TryGetValue(item.sid, out var value2) && item.etag != value2)
				{
					item.etag = value2;
					changed = true;
				}
				if (item.status != text2)
				{
					item.status = text2;
					changed = true;
				}
				connection.Update(item);
				continue;
				IL_00e8:
				text = Constants.SyncStatus.SYNC_UPDATE.ToString();
				goto IL_010a;
			}
			foreach (MatrixProfileQuadrantModel quadrant in from q in (from q in connection.Table<MatrixProfileQuadrantModel>()
					where q.userId == userId
					select q).ToList()
				where committedProfileIds.Contains(q.matrixSid)
				select q)
			{
				submittedProfiles.TryGetValue(quadrant.matrixSid, out var value3);
				MatrixProfileQuadrantSyncModel matrixProfileQuadrantSyncModel = value3?.matrixs?.FirstOrDefault((MatrixProfileQuadrantSyncModel q) => GetSyncQuadrantLevel(q) == quadrant.quadrant);
				bool num2;
				if (matrixProfileQuadrantSyncModel != null)
				{
					num2 = MatchesSubmittedQuadrant(quadrant, matrixProfileQuadrantSyncModel);
				}
				else
				{
					if (!quadrant.deleted)
					{
						goto IL_02b6;
					}
					num2 = bean.Deleted.Contains(quadrant.matrixSid);
				}
				if (!num2)
				{
					goto IL_02b6;
				}
				string text3 = Constants.SyncStatus.SYNC_DONE.ToString();
				goto IL_02d8;
				IL_02b6:
				text3 = Constants.SyncStatus.SYNC_UPDATE.ToString();
				goto IL_02d8;
				IL_02d8:
				string text4 = text3;
				if (TryGetCommittedQuadrantEtag(id2etag, bean, quadrant, out var etag) && quadrant.etag != etag)
				{
					quadrant.etag = etag;
					changed = true;
				}
				if (quadrant.status != text4)
				{
					quadrant.status = text4;
					changed = true;
				}
				connection.Update(quadrant);
			}
		});
		if (await EnsureLocalSelectionAsync(await GetProfilesAsync(), allowFallback: true))
		{
			changed = true;
		}
		return changed;
	}

	private static bool MatchesSubmittedProfile(MatrixProfileModel local, MatrixProfileSyncModel submitted)
	{
		if ((local.name ?? string.Empty) == (submitted.name ?? string.Empty) && local.sortOrder == submitted.sortOrder && local.deleted == (submitted.deleted == 1))
		{
			DateTime value = ToSyncTime(local.modifiedTime);
			DateTime? modifiedTime = submitted.modifiedTime;
			return value == modifiedTime;
		}
		return false;
	}

	private static bool MatchesSubmittedQuadrant(MatrixProfileQuadrantModel local, MatrixProfileQuadrantSyncModel submitted)
	{
		SortOption sortOption = submitted.GetSortOption();
		if ((local.name ?? string.Empty) == (submitted.name ?? string.Empty) && local.rule == submitted.rule && local.sortType == submitted.sortType && local.groupBy == sortOption?.groupBy && local.orderBy == sortOption?.orderBy && local.order == sortOption?.order && local.sortOrder == submitted.sortOrder && local.deleted == (submitted.deleted == 1))
		{
			DateTime value = ToSyncTime(local.modifiedTime);
			DateTime? modifiedTime = submitted.modifiedTime;
			return value == modifiedTime;
		}
		return false;
	}

	private static async Task<bool> PreserveRejectedAddedProfilesAsync(ModelSyncBean<MatrixProfileSyncModel> bean, Dictionary<string, string> id2error)
	{
		string value;
		HashSet<string> rejectedAddIds = new HashSet<string>(from p in bean.Add
			where !string.IsNullOrEmpty(p.id) && id2error.TryGetValue(p.id, out value) && value == "EXCEED_QUOTA"
			select p.id);
		if (!rejectedAddIds.Any())
		{
			return false;
		}
		List<MatrixProfileModel> list = (await GetProfilesAsync(includeDeleted: true)).Where((MatrixProfileModel p) => rejectedAddIds.Contains(p.sid)).ToList();
		if (!list.Any())
		{
			return false;
		}
		bool changed = false;
		foreach (MatrixProfileModel item in list)
		{
			if (item.status == Constants.SyncStatus.SYNC_INIT.ToString())
			{
				item.status = Constants.SyncStatus.SYNC_INIT_UP_LIMIT.ToString();
				changed = true;
			}
		}
		await App.Connection.UpdateAllAsync(list);
		List<MatrixProfileQuadrantModel> list2 = await (from q in App.Connection.Table<MatrixProfileQuadrantModel>()
			where q.userId == UserId && rejectedAddIds.Contains(q.matrixSid)
			select q).ToListAsync();
		foreach (MatrixProfileQuadrantModel item2 in list2)
		{
			if (item2.status == Constants.SyncStatus.SYNC_INIT.ToString())
			{
				item2.status = Constants.SyncStatus.SYNC_INIT_UP_LIMIT.ToString();
				changed = true;
			}
		}
		await App.Connection.UpdateAllAsync(list2);
		return changed;
	}

	private static bool TryGetCommittedQuadrantEtag(Dictionary<string, string> id2etag, ModelSyncBean<MatrixProfileSyncModel> bean, MatrixProfileQuadrantModel quadrant, out string etag)
	{
		etag = null;
		if (id2etag == null || quadrant == null)
		{
			return false;
		}
		string text = "quadrant" + quadrant.quadrant;
		string[] array = new string[4]
		{
			quadrant.id,
			quadrant.matrixSid + "_" + text,
			quadrant.matrixSid + "." + text,
			quadrant.matrixSid + ":" + text
		};
		foreach (string text2 in array)
		{
			if (!string.IsNullOrEmpty(text2) && id2etag.TryGetValue(text2, out etag))
			{
				return true;
			}
		}
		return false;
	}

	public static string NormalizeName(string name)
	{
		return name?.Trim();
	}

	public static bool IsDefaultMatrixName(string name)
	{
		return string.Equals(NormalizeName(name), Utils.GetString("Matrix"), StringComparison.CurrentCulture);
	}

	public static bool SameQuadrants(IList<QuadrantModel> left, IList<QuadrantModel> right)
	{
		int i;
		for (i = 1; i <= 4; i++)
		{
			QuadrantModel left2 = left?.FirstOrDefault((QuadrantModel q) => q.id == "quadrant" + i) ?? QuadrantModel.GetDefault(i);
			QuadrantModel right2 = right?.FirstOrDefault((QuadrantModel q) => q.id == "quadrant" + i) ?? QuadrantModel.GetDefault(i);
			if (!SameQuadrant(left2, right2))
			{
				return false;
			}
		}
		return true;
	}

	public static QuadrantModel CloneQuadrant(QuadrantModel quadrant)
	{
		if (quadrant == null)
		{
			return null;
		}
		return new QuadrantModel
		{
			id = quadrant.id,
			name = quadrant.name,
			rule = quadrant.rule,
			sortType = quadrant.sortType,
			sortOrder = quadrant.sortOrder,
			SortOption = ((quadrant.SortOption == null) ? null : new SortOption
			{
				groupBy = quadrant.SortOption.groupBy,
				orderBy = quadrant.SortOption.orderBy,
				order = quadrant.SortOption.order
			})
		};
	}

	private static async Task<List<MatrixProfileQuadrantModel>> GetQuadrantsAsync(string sid)
	{
		return (from q in await (from q in App.Connection.Table<MatrixProfileQuadrantModel>()
				where q.userId == UserId && q.matrixSid == sid
				select q).ToListAsync()
			where !q.deleted
			orderby q.quadrant
			select q).ToList();
	}

	private static async Task<bool> EnsureLocalSelectionAsync(List<MatrixProfileModel> profiles, bool allowFallback = false)
	{
		string activeSid = LocalSettings.Settings.MatrixActiveViewId;
		if (!allowFallback && !string.IsNullOrEmpty(activeSid) && !profiles.Any((MatrixProfileModel p) => p.sid == activeSid) && !(await GetProfilesAsync(includeDeleted: true)).Any((MatrixProfileModel p) => p.sid == activeSid && p.deleted))
		{
			return false;
		}
		MatrixProfileModel profile = GetActiveProfile(profiles);
		if (profile == null)
		{
			return false;
		}
		bool changed = false;
		List<MatrixProfileQuadrantModel> quadrants = await GetQuadrantsAsync(profile.sid);
		int level;
		for (level = 1; level <= 4; level++)
		{
			if (!quadrants.Any((MatrixProfileQuadrantModel q) => q.quadrant == level))
			{
				await App.Connection.InsertAsync(MatrixProfileQuadrantModel.Create(profile.sid, level, QuadrantModel.GetDefault(level), Constants.SyncStatus.SYNC_INIT.ToString()));
				changed = true;
			}
		}
		if (activeSid == profile.sid)
		{
			return changed;
		}
		LocalSettings.Settings.MatrixActiveViewId = profile.sid;
		await LocalSettings.Settings.Save();
		return true;
	}

	private static MatrixProfileModel GetActiveProfile(List<MatrixProfileModel> profiles)
	{
		if (profiles == null || profiles.Count == 0)
		{
			return null;
		}
		string activeSid = LocalSettings.Settings.MatrixActiveViewId;
		return profiles.FirstOrDefault((MatrixProfileModel p) => p.sid == activeSid) ?? profiles.FirstOrDefault((MatrixProfileModel p) => p.sid == LocalSettings.Settings.MatrixModel?.defaultViewId) ?? profiles.FirstOrDefault();
	}

	private static bool IsMainActiveProfile(string sid)
	{
		string text = LocalSettings.Settings.MatrixActiveViewId;
		if (string.IsNullOrEmpty(text))
		{
			text = GetActiveProfile(RunSync(() => GetProfilesAsync()))?.sid;
		}
		return string.Equals(text, sid, StringComparison.Ordinal);
	}

	private static List<QuadrantModel> BuildQuadrants(List<MatrixProfileQuadrantModel> quadrants)
	{
		List<QuadrantModel> list = new List<QuadrantModel>();
		int i;
		for (i = 1; i <= 4; i++)
		{
			list.Add(quadrants.FirstOrDefault((MatrixProfileQuadrantModel q) => q.quadrant == i)?.ToQuadrantModel() ?? QuadrantModel.GetDefault(i));
		}
		return list;
	}

	private static async Task BindConfirmedInitialProfileAsync(IEnumerable<MatrixProfileModel> profiles, ISet<string> confirmedIds)
	{
		MatrixModel matrix = LocalSettings.Settings.MatrixModel;
		if (matrix != null && string.IsNullOrEmpty(matrix.defaultViewId))
		{
			MatrixProfileModel initial = profiles.FirstOrDefault((MatrixProfileModel p) => !p.deleted && IsInitialStatus(p.status) && confirmedIds.Contains(p.sid));
			if (initial != null)
			{
				MatrixModel matrixModel = matrix;
				matrixModel.quadrants = BuildQuadrants(await GetQuadrantsAsync(initial.sid));
				matrix.SetDefaultView(initial.sid);
				LocalSettings.Settings.UserPreference.mtime = matrix.mtime;
				LocalSettings.Settings.SetChanged();
				await LocalSettings.Settings.Save();
			}
		}
	}

	private static async Task<bool> ConfirmQuotaProfilesAsync(IEnumerable<MatrixProfileModel> localProfiles, IEnumerable<MatrixProfileQuadrantModel> localQuadrants, IEnumerable<MatrixProfileModel> remoteProfiles, IEnumerable<MatrixProfileQuadrantModel> remoteQuadrants)
	{
		List<MatrixProfileModel> profiles = remoteProfiles.Where((MatrixProfileModel p) => !p.deleted).ToList();
		List<MatrixProfileQuadrantModel> quadrants = remoteQuadrants.ToList();
		bool result = false;
		foreach (MatrixProfileModel local in localProfiles.Where((MatrixProfileModel p) => p.status == Constants.SyncStatus.SYNC_INIT_UP_LIMIT.ToString()))
		{
			MatrixProfileModel matrixProfileModel = profiles.FirstOrDefault((MatrixProfileModel p) => p.sid == local.sid);
			if (matrixProfileModel == null)
			{
				continue;
			}
			if (!string.IsNullOrEmpty(matrixProfileModel.etag))
			{
				local.etag = matrixProfileModel.etag;
			}
			local.status = Constants.SyncStatus.SYNC_UPDATE.ToString();
			await App.Connection.UpdateAsync(local);
			foreach (MatrixProfileQuadrantModel quadrant in localQuadrants.Where((MatrixProfileQuadrantModel q) => q.matrixSid == local.sid && q.status == Constants.SyncStatus.SYNC_INIT_UP_LIMIT.ToString()))
			{
				MatrixProfileQuadrantModel matrixProfileQuadrantModel = quadrants.FirstOrDefault((MatrixProfileQuadrantModel q) => q.matrixSid == local.sid && q.quadrant == quadrant.quadrant);
				if (!string.IsNullOrEmpty(matrixProfileQuadrantModel?.etag))
				{
					quadrant.etag = matrixProfileQuadrantModel.etag;
				}
				quadrant.status = Constants.SyncStatus.SYNC_UPDATE.ToString();
				await App.Connection.UpdateAsync(quadrant);
			}
			result = true;
		}
		return result;
	}

	private static bool NameExists(List<MatrixProfileModel> profiles, string name, string exceptSid)
	{
		string normalized = NormalizeName(name);
		if (string.IsNullOrEmpty(normalized))
		{
			return false;
		}
		return profiles.Any((MatrixProfileModel p) => p.sid != exceptSid && string.Equals(NormalizeName(p.DisplayName), normalized, StringComparison.CurrentCultureIgnoreCase));
	}

	private static string GetPersistedName(MatrixProfileModel profile, string name)
	{
		string text = NormalizeName(name);
		if (!string.IsNullOrWhiteSpace(profile.name) || !IsDefaultMatrixName(text))
		{
			return text;
		}
		return null;
	}

	private static string GetNameForDuplicateCheck(MatrixProfileModel profile, string persistedName)
	{
		return persistedName ?? Utils.GetString("Matrix");
	}

	private static bool SameQuadrant(QuadrantModel left, QuadrantModel right)
	{
		if (left?.id == right?.id && left?.name == right?.name && left?.rule == right?.rule && left?.sortType == right?.sortType && left?.sortOrder == right?.sortOrder && left?.SortOption?.groupBy == right?.SortOption?.groupBy && left?.SortOption?.orderBy == right?.SortOption?.orderBy)
		{
			return left?.SortOption?.order == right?.SortOption?.order;
		}
		return false;
	}

	private static void Touch(MatrixProfileModel profile)
	{
		profile.modifiedTime = DateTime.Now;
		profile.status = NextStatus(profile.status);
	}

	private static void Touch(MatrixProfileQuadrantModel quadrant)
	{
		quadrant.modifiedTime = DateTime.Now;
		quadrant.status = NextStatus(quadrant.status);
	}

	private static string NextStatus(string status)
	{
		if (!(status == Constants.SyncStatus.SYNC_NEW.ToString()) && !IsInitialStatus(status))
		{
			return Constants.SyncStatus.SYNC_UPDATE.ToString();
		}
		return status;
	}

	private static bool IsInitialStatus(string status)
	{
		if (!(status == Constants.SyncStatus.SYNC_INIT.ToString()))
		{
			return status == Constants.SyncStatus.SYNC_INIT_UP_LIMIT.ToString();
		}
		return true;
	}

	private static bool NeedPost(MatrixProfileModel profile)
	{
		if (profile.status != null)
		{
			return profile.status != Constants.SyncStatus.SYNC_DONE.ToString();
		}
		return false;
	}

	private static bool NeedPost(MatrixProfileQuadrantModel quadrant)
	{
		if (quadrant.status != null)
		{
			return quadrant.status != Constants.SyncStatus.SYNC_DONE.ToString();
		}
		return false;
	}

	private static bool IsLocalOnly(MatrixProfileModel profile)
	{
		if (profile != null)
		{
			if (!(profile.status == Constants.SyncStatus.SYNC_NEW.ToString()))
			{
				return IsInitialStatus(profile.status);
			}
			return true;
		}
		return false;
	}

	private static bool ShouldAcceptRemote(MatrixProfileModel local, MatrixProfileModel remote)
	{
		if (local.status == Constants.SyncStatus.SYNC_DONE.ToString() || string.IsNullOrEmpty(local.status))
		{
			return true;
		}
		if (local.status == Constants.SyncStatus.SYNC_INIT.ToString())
		{
			return !string.IsNullOrEmpty(remote.etag);
		}
		return false;
	}

	private static bool ShouldAcceptRemote(MatrixProfileQuadrantModel local, MatrixProfileQuadrantModel remote)
	{
		if (local.status == Constants.SyncStatus.SYNC_DONE.ToString() || string.IsNullOrEmpty(local.status))
		{
			return true;
		}
		if (local.status == Constants.SyncStatus.SYNC_INIT.ToString())
		{
			return !string.IsNullOrEmpty(remote.etag);
		}
		return false;
	}

	private static bool SameProfile(MatrixProfileModel local, MatrixProfileModel remote)
	{
		if (local.sid == remote.sid && local.name == remote.name && local.sortOrder == remote.sortOrder && local.etag == remote.etag)
		{
			return local.deleted == remote.deleted;
		}
		return false;
	}

	private static bool SameQuadrant(MatrixProfileQuadrantModel local, MatrixProfileQuadrantModel remote)
	{
		if (local.matrixSid == remote.matrixSid && local.quadrant == remote.quadrant && local.name == remote.name && local.rule == remote.rule && local.sortType == remote.sortType && local.groupBy == remote.groupBy && local.orderBy == remote.orderBy && local.order == remote.order && local.sortOrder == remote.sortOrder && local.etag == remote.etag)
		{
			return local.deleted == remote.deleted;
		}
		return false;
	}

	private static MatrixProfileSyncModel ToSyncProfile(MatrixProfileModel profile, List<MatrixProfileQuadrantModel> quadrants)
	{
		return new MatrixProfileSyncModel
		{
			id = profile.sid,
			name = GetStringForSync(profile.name, profile.status),
			sortOrder = profile.sortOrder,
			createdTime = ToSyncTime(profile.createdTime),
			modifiedTime = ToSyncTime(profile.modifiedTime),
			etag = profile.etag,
			deleted = (profile.deleted ? new int?(1) : ((int?)null)),
			matrixs = (from q in quadrants
				where q.matrixSid == profile.sid && !q.deleted
				orderby q.quadrant
				select q).Select(ToSyncQuadrant).ToList()
		};
	}

	private static MatrixProfileQuadrantSyncModel ToSyncQuadrant(MatrixProfileQuadrantModel quadrant)
	{
		return new MatrixProfileQuadrantSyncModel
		{
			id = "quadrant" + quadrant.quadrant,
			name = GetStringForSync(quadrant.name, quadrant.status),
			rule = quadrant.rule,
			sortType = quadrant.sortType,
			SortOption = quadrant.SortOption,
			sortOrder = quadrant.sortOrder,
			createdTime = ToSyncTime(quadrant.createdTime),
			modifiedTime = ToSyncTime(quadrant.modifiedTime),
			etag = quadrant.etag,
			deleted = (quadrant.deleted ? new int?(1) : ((int?)null))
		};
	}

	private static DateTime ToSyncTime(DateTime time)
	{
		if (time.Kind != DateTimeKind.Utc)
		{
			return time;
		}
		return time.ToLocalTime();
	}

	private static string GetStringForSync(string value, string status)
	{
		if (value != null)
		{
			return value;
		}
		if (!(status == Constants.SyncStatus.SYNC_UPDATE.ToString()))
		{
			return null;
		}
		return string.Empty;
	}

	private static async Task<bool> MarkMissingRemoteProfilesDeletedAsync(List<MatrixProfileModel> localProfiles, List<MatrixProfileQuadrantModel> localQuadrants, List<MatrixProfileSyncModel> serverProfiles)
	{
		List<string> list = (from p in serverProfiles
			where !string.IsNullOrEmpty(p?.id)
			select p.id).ToList();
		if (!list.Any())
		{
			return false;
		}
		HashSet<string> remoteIdSet = new HashSet<string>(list);
		List<MatrixProfileModel> missingProfiles = localProfiles.Where((MatrixProfileModel p) => !p.deleted && p.status == Constants.SyncStatus.SYNC_DONE.ToString() && !remoteIdSet.Contains(p.sid)).ToList();
		if (!missingProfiles.Any())
		{
			return false;
		}
		foreach (MatrixProfileModel item in missingProfiles)
		{
			item.deleted = true;
			item.status = Constants.SyncStatus.SYNC_DONE.ToString();
		}
		await App.Connection.UpdateAllAsync(missingProfiles);
		foreach (MatrixProfileModel item2 in missingProfiles)
		{
			await ClearProfileTaskOrdersAsync(item2.sid);
		}
		HashSet<string> missingIds = new HashSet<string>(missingProfiles.Select((MatrixProfileModel p) => p.sid));
		List<MatrixProfileQuadrantModel> list2 = localQuadrants.Where((MatrixProfileQuadrantModel q) => !q.deleted && missingIds.Contains(q.matrixSid)).ToList();
		foreach (MatrixProfileQuadrantModel item3 in list2)
		{
			item3.deleted = true;
			item3.status = Constants.SyncStatus.SYNC_DONE.ToString();
		}
		if (list2.Any())
		{
			await App.Connection.UpdateAllAsync(list2);
		}
		return true;
	}

	private static Task ClearProfileTaskOrdersAsync(string sid)
	{
		return TaskSortOrderService.DeleteAllTaskSortOrdersInLists(from level in Enumerable.Range(1, 4)
			select "quadrant" + level + "_" + sid);
	}

	private static MatrixProfileModel ToLocalProfile(MatrixProfileSyncModel remote, MatrixProfileModel local)
	{
		MatrixProfileModel matrixProfileModel = new MatrixProfileModel
		{
			id = (local?.id ?? Utils.GetGuid()),
			sid = remote.id,
			userId = UserId,
			name = remote.name,
			sortOrder = remote.sortOrder,
			createdTime = ((!(remote.createdTime.GetValueOrDefault() == default(DateTime))) ? remote.createdTime.Value : (local?.createdTime ?? DateTime.Now)),
			modifiedTime = ((!(remote.modifiedTime.GetValueOrDefault() == default(DateTime))) ? remote.modifiedTime.Value : (local?.modifiedTime ?? DateTime.Now)),
			etag = remote.etag,
			deleted = (remote.deleted == 1),
			status = Constants.SyncStatus.SYNC_DONE.ToString()
		};
		if (local != null)
		{
			matrixProfileModel._Id = local._Id;
		}
		return matrixProfileModel;
	}

	private static MatrixProfileQuadrantModel ToLocalQuadrant(string matrixSid, int level, MatrixProfileQuadrantSyncModel remote, MatrixProfileQuadrantModel local)
	{
		SortOption sortOption = remote.GetSortOption();
		MatrixProfileQuadrantModel matrixProfileQuadrantModel = new MatrixProfileQuadrantModel
		{
			id = matrixSid + "_quadrant" + level,
			userId = UserId,
			matrixSid = matrixSid,
			quadrant = level,
			name = remote.name,
			rule = remote.rule,
			sortType = remote.sortType,
			groupBy = (sortOption?.groupBy ?? remote.groupBy),
			orderBy = (sortOption?.orderBy ?? remote.orderBy),
			order = (sortOption?.order ?? remote.order),
			sortOrder = remote.sortOrder,
			createdTime = ((!(remote.createdTime.GetValueOrDefault() == default(DateTime))) ? remote.createdTime.Value : (local?.createdTime ?? DateTime.Now)),
			modifiedTime = ((!(remote.modifiedTime.GetValueOrDefault() == default(DateTime))) ? remote.modifiedTime.Value : (local?.modifiedTime ?? DateTime.Now)),
			etag = remote.etag,
			deleted = (remote.deleted == 1),
			status = Constants.SyncStatus.SYNC_DONE.ToString()
		};
		if (local != null)
		{
			matrixProfileQuadrantModel._Id = local._Id;
		}
		return matrixProfileQuadrantModel;
	}

	private static int GetSyncQuadrantLevel(MatrixProfileQuadrantSyncModel quadrant)
	{
		if (quadrant == null)
		{
			return 0;
		}
		if (quadrant.quadrant.GetValueOrDefault() > 0)
		{
			return quadrant.quadrant.Value;
		}
		string id = quadrant.id;
		if (id == null || !id.StartsWith("quadrant") || !int.TryParse(quadrant.id.Substring("quadrant".Length), out var result))
		{
			return 0;
		}
		return result;
	}

	private static void RunSync(Func<Task> action)
	{
		Task.Run(action).GetAwaiter().GetResult();
	}

	private static T RunSync<T>(Func<Task<T>> action)
	{
		return Task.Run(action).GetAwaiter().GetResult();
	}
}
