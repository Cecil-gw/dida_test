using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TickTickUtils.Lang;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Dal;

public static class HabitCheckInDao
{
	private static readonly SemaphoreSlim NegativeValueRepairLock = new SemaphoreSlim(1, 1);

	private static string _negativeValuesRepairedUserId;

	private static async Task EnsureNegativeValuesRepaired()
	{
		string userId = Utils.GetCurrentUserStr();
		if (string.IsNullOrEmpty(userId) || _negativeValuesRepairedUserId == userId)
		{
			return;
		}
		await NegativeValueRepairLock.WaitAsync();
		try
		{
			if (_negativeValuesRepairedUserId == userId)
			{
				return;
			}
			List<HabitCheckInModel> negativeCheckIns = await (from v in App.Connection.Table<HabitCheckInModel>()
				where v.UserId == userId && v.Status != -1 && v.Value < 0.0
				select v).ToListAsync();
			foreach (HabitCheckInModel item in negativeCheckIns)
			{
				NormalizeNegativeValue(item, markForSync: true);
			}
			if (negativeCheckIns.Count > 0)
			{
				await App.Connection.UpdateAllAsync(negativeCheckIns);
				HabitCheckinCache.Reset();
				UtilLog.Info($"Repaired negative habit check-ins: {negativeCheckIns.Count}");
			}
			_negativeValuesRepairedUserId = userId;
		}
		catch (Exception ex)
		{
			UtilLog.Warn("Repair negative habit check-ins failed: " + ex.Message);
		}
		finally
		{
			NegativeValueRepairLock.Release();
		}
	}

	internal static bool NormalizeNegativeValue(HabitCheckInModel checkIn, bool markForSync)
	{
		if (checkIn == null || checkIn.Value >= 0.0)
		{
			return false;
		}
		checkIn.Value = 0.0;
		if (checkIn.CheckStatus == 2)
		{
			checkIn.CheckStatus = 0;
			checkIn.CheckinTime = null;
		}
		if (markForSync)
		{
			checkIn.Status = ((checkIn.Status != 0) ? 1 : 0);
			checkIn.opTime = DateTime.Now;
		}
		return true;
	}

	private static string ToCheckinStamp(DateTime date)
	{
		return date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
	}

	private static int ToCheckinStampInt(DateTime date)
	{
		return int.Parse(ToCheckinStamp(date));
	}

	public static async Task HandleCommitResult(BatchUpdateResult result, List<HabitCheckInModel> checkIns)
	{
		if (result == null)
		{
			return;
		}
		if (checkIns == null || checkIns.Count <= 0)
		{
			return;
		}
		List<HabitCheckInModel> updates = new List<HabitCheckInModel>();
		List<HabitCheckInModel> deletes = new List<HabitCheckInModel>();
		List<HabitCheckInModel> list = await GetCheckInsByIds(checkIns.Select((HabitCheckInModel c) => c.Id).ToList());
		if (list == null)
		{
			return;
		}
		foreach (HabitCheckInModel checkIn in checkIns)
		{
			HabitCheckInModel habitCheckInModel = list.FirstOrDefault((HabitCheckInModel l) => l.Id == checkIn.Id);
			if (habitCheckInModel != null)
			{
				if (habitCheckInModel.Status == checkIn.Status && Math.Abs(habitCheckInModel.Value - checkIn.Value) < 0.01 && (checkIn.Status == 0 || checkIn.Status == 1))
				{
					checkIn.Status = 2;
					updates.Add(checkIn);
				}
				if (checkIn.Status == -1)
				{
					deletes.Add(checkIn);
				}
			}
		}
		if (updates.Any())
		{
			await UpdateAllAsync(checkIns);
		}
		if (deletes.Any())
		{
			foreach (HabitCheckInModel item in deletes)
			{
				await DeleteAsync(item);
			}
		}
		if (updates.Any() || deletes.Any())
		{
			HabitCheckinCache.Reset();
		}
	}

	public static async Task<List<HabitCheckInModel>> GetNeedPostHabitCheckIns()
	{
		await EnsureNegativeValuesRepaired();
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from v in App.Connection.Table<HabitCheckInModel>()
			where v.UserId == userId && (v.Status == 0 || v.Status == 1 || v.Status == -1)
			select v).ToListAsync();
	}

	public static async Task<List<HabitCheckInModel>> GetCheckInsByIdsInSpan(List<string> ids, DateTime from, DateTime to)
	{
		try
		{
			await EnsureNegativeValuesRepaired();
			List<string> list = ids;
			if (list != null && list.Count > 0)
			{
				List<string> stamps = new List<string>();
				for (int i = 0; (double)i <= (to - from).TotalDays; i++)
				{
					stamps.Add(from.AddDays(i).ToString("yyyyMMdd", CultureInfo.InvariantCulture));
				}
				return await (from check in App.Connection.Table<HabitCheckInModel>()
					where stamps.Contains(check.CheckinStamp) && check.UserId == LocalSettings.Settings.LoginUserId && ids.Contains(check.HabitId) && check.Status != -1
					select check).ToListAsync();
			}
		}
		catch (Exception)
		{
			return null;
		}
		return null;
	}

	public static async Task<bool> HandleLocalDirtyCheckIns(string habitId)
	{
		string text = Utils.GetCurrentUserIdInt().ToString();
		List<HabitCheckInStamp> list = await App.Connection.QueryAsync<HabitCheckInStamp>("select checkinstamp as CheckInStamp from habitcheckinmodel where userid = '" + text + "' and habitid = '" + habitId + "' group by checkinstamp having count(1) > 1 ", Array.Empty<object>());
		List<HabitCheckInModel> dirtyCheckIns = new List<HabitCheckInModel>();
		if (list != null && list.Any())
		{
			foreach (HabitCheckInStamp item in list)
			{
				List<HabitCheckInModel> list2 = await App.Connection.QueryAsync<HabitCheckInModel>("select * from habitcheckinmodel where checkinstamp = '" + item.CheckInStamp + "' and habitid = '" + habitId + "'", Array.Empty<object>());
				if (list2.Any() && list2.Count > 1)
				{
					for (int i = 1; i < list2.Count; i++)
					{
						list2[i].Status = -1;
						dirtyCheckIns.Add(list2[i]);
					}
				}
			}
		}
		if (dirtyCheckIns.Any())
		{
			await App.Connection.UpdateAllAsync(dirtyCheckIns);
		}
		return dirtyCheckIns.Any();
	}

	private static async Task<List<HabitCheckInModel>> GetCheckInsByIds(List<string> ids)
	{
		try
		{
			List<string> list = ids;
			if (list != null && list.Count > 0)
			{
				if (ids.Count < 50)
				{
					return await (from check in App.Connection.Table<HabitCheckInModel>()
						where check.UserId == LocalSettings.Settings.LoginUserId && ids.Contains(check.Id)
						select check).ToListAsync();
				}
				return (await App.Connection.Table<HabitCheckInModel>().ToListAsync())?.Where((HabitCheckInModel c) => ids.Contains(c.Id)).ToList();
			}
		}
		catch (Exception)
		{
			return null;
		}
		return null;
	}

	public static async Task<List<HabitCheckInModel>> GetHabitCheckInsByHabitIdAndStamp(string habitId, int stamp)
	{
		await EnsureNegativeValuesRepaired();
		string text = Utils.GetCurrentUserIdInt().ToString();
		if (stamp > 0)
		{
			string sql = $"select * from HabitCheckinModel where userId = '{text}' and habitId = '{habitId}' and CheckinStamp > {stamp}";
			return await App.Connection.QueryAsync<HabitCheckInModel>(sql, Array.Empty<object>());
		}
		string sql2 = "select * from HabitCheckinModel where userId = '" + text + "' and habitId = '" + habitId + "'";
		return await App.Connection.QueryAsync<HabitCheckInModel>(sql2, Array.Empty<object>());
	}

	public static async Task<List<HabitCheckInModel>> GetHabitCheckIns()
	{
		await EnsureNegativeValuesRepaired();
		string userId = Utils.GetCurrentUserStr();
		return await (from v in App.Connection.Table<HabitCheckInModel>()
			where v.UserId == userId && v.Status != -1
			select v).ToListAsync();
	}

	public static async Task<List<HabitCheckInModel>> GetHabitCheckInsByHabitId(string habitId)
	{
		await EnsureNegativeValuesRepaired();
		string userId = Utils.GetCurrentUserStr();
		return await (from v in App.Connection.Table<HabitCheckInModel>()
			where v.UserId == userId && v.HabitId == habitId && v.Status != -1
			select v).ToListAsync();
	}

	public static async Task<int> CountValidHabitCheckInsByHabitId(string habitId)
	{
		string userId = Utils.GetCurrentUserStr();
		return await (from v in App.Connection.Table<HabitCheckInModel>()
			where v.UserId == userId && v.HabitId == habitId && v.Status != -1 && v.Value >= v.Goal
			select v).CountAsync();
	}

	public static async Task<HabitCheckInModel> GetHabitCheckInsByHabitIdAndStamp(string habitId, string stamp)
	{
		await EnsureNegativeValuesRepaired();
		string userId = Utils.GetCurrentUserStr();
		return await (from v in App.Connection.Table<HabitCheckInModel>()
			where v.UserId == userId && v.HabitId == habitId && v.CheckinStamp == stamp
			select v).FirstOrDefaultAsync();
	}

	public static async Task<HabitCheckInModel> GetHabitCheckInByHabitIdAndStamp(string habitId, string stamp)
	{
		await EnsureNegativeValuesRepaired();
		string userId = Utils.GetCurrentUserStr();
		List<HabitCheckInModel> results = await (from v in App.Connection.Table<HabitCheckInModel>()
			where v.UserId == userId && v.HabitId == habitId && v.CheckinStamp == stamp && v.Status != -1
			select v).ToListAsync();
		if (results != null && results.Count > 0)
		{
			for (int i = 1; i < results.Count; i++)
			{
				await DeleteAsync(results[i]);
			}
		}
		return (results != null && results.Count > 0) ? results[0] : null;
	}

	public static async Task<List<HabitCheckInModel>> GetHabitCheckInsByHabitIdInSpan(string habitId, DateTime startDate, DateTime endDate)
	{
		await EnsureNegativeValuesRepaired();
		int num = ToCheckinStampInt(startDate);
		int num2 = ToCheckinStampInt(endDate);
		string text = Utils.GetCurrentUserIdInt().ToString();
		string sql = $"select * from HabitCheckinModel where userId = '{text}' and habitId = '{habitId}' and CheckInStamp >= {num} and CheckInStamp <= {num2} and Value >= 0 and Status <> -1";
		return await App.Connection.QueryAsync<HabitCheckInModel>(sql, Array.Empty<object>());
	}

	public static async Task<List<HabitCheckInModel>> GetCheckInsInSpan(DateTime start, DateTime end)
	{
		await EnsureNegativeValuesRepaired();
		int num = ToCheckinStampInt(start);
		int num2 = ToCheckinStampInt(end);
		string arg = Utils.GetCurrentUserIdInt().ToString();
		string sql = $"select * from HabitCheckinModel where userId = '{arg}' and CheckinStamp >= {num} and CheckInStamp < {num2} and Status <> -1";
		return await App.Connection.QueryAsync<HabitCheckInModel>(sql, Array.Empty<object>());
	}

	public static async Task<List<HabitCheckInModel>> GetCheckInsInWeek()
	{
		await EnsureNegativeValuesRepaired();
		string text = ToCheckinStamp(Utils.GetWeekStart(DateTime.Today));
		string text2 = Utils.GetCurrentUserIdInt().ToString();
		string sql = "select * from HabitCheckinModel where userId = '" + text2 + "' and CheckinStamp >= " + text + " and Status <> -1";
		return await App.Connection.QueryAsync<HabitCheckInModel>(sql, Array.Empty<object>());
	}

	public static async Task UpdateAsync(HabitCheckInModel model)
	{
		await App.Connection.UpdateAsync(model);
	}

	public static async Task UpdateAllAsync(List<HabitCheckInModel> model)
	{
		await App.Connection.UpdateAllAsync(model);
	}

	public static async Task InsertAsync(HabitCheckInModel model)
	{
		await App.Connection.InsertAsync(model);
	}

	public static async Task DeleteAsync(HabitCheckInModel model)
	{
		await App.Connection.DeleteAsync(model);
	}

	public static async Task DeleteCheckInsByHabitId(string habitId)
	{
		List<HabitCheckInModel> list = await (from c in App.Connection.Table<HabitCheckInModel>()
			where c.HabitId == habitId
			select c).ToListAsync();
		if (list == null || list.Count <= 0)
		{
			return;
		}
		foreach (HabitCheckInModel item in list)
		{
			await DeleteAsync(item);
		}
	}

	public static async Task<bool> ExistCheckInBefore(string habitId, DateTime date)
	{
		string sql = "select id from HabitCheckInModel where CheckinStamp <= '" + ToCheckinStamp(date) + "' and HabitId = '" + habitId + "' and (CheckStatus != 0 or Value > 0) limit 1";
		List<IdModel> obj = await App.Connection.QueryAsync<IdModel>(sql, Array.Empty<object>());
		return obj != null && obj.Count > 0;
	}
}
