using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TickTickUtils.Lang;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Habit;

namespace ticktick_WPF.Dal;

internal class HabitRecordDao
{
	private static int ToHabitRecordStamp(DateTime date)
	{
		return int.Parse(date.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
	}

	public static async Task<List<HabitRecordModel>> GetNeedPostHabitRecords()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from v in App.Connection.Table<HabitRecordModel>()
			where v.UserId == userId && (v.Status == 0 || v.Status == 1)
			select v).ToListAsync();
	}

	public static async Task HandleRecordCommitResult(BatchUpdateResult result, List<HabitRecordModel> records)
	{
		if (result == null || records == null || !records.Any())
		{
			return;
		}
		foreach (HabitRecordModel record in records)
		{
			if (result.Id2error != null && result.Id2error.ContainsKey(record.Id))
			{
				if (result.Id2error[record.Id] == "NOHABIT")
				{
					UtilLog.Info("HabitRecord NOHABIT " + record.HabitId);
				}
				continue;
			}
			record.Status = 2;
			if (result.Id2etag == null || !result.Id2etag.ContainsKey(record.Id))
			{
				UtilLog.Info("HabitRecord NoErrorAndEtag " + record.Id + " , habitId : " + record.HabitId);
			}
		}
		await UpdateAllAsync(records);
	}

	public static async Task<List<HabitRecordModel>> GetHabitRecords(int checkStamp)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from v in App.Connection.Table<HabitRecordModel>()
			where v.UserId == userId && v.Deleted != 1 && v.Stamp >= checkStamp
			select v).ToListAsync();
	}

	public static async Task<List<HabitRecordModel>> GetHabitRecordsByHabitId(string habitId, int stamp = 0)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from v in App.Connection.Table<HabitRecordModel>()
			where v.UserId == userId && v.HabitId == habitId && v.Deleted != 1 && v.Stamp > stamp
			select v).ToListAsync();
	}

	public static async Task<HabitRecordModel> GetHabitRecordsByHabitIdAndDate(string habitId, int stamp)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from v in App.Connection.Table<HabitRecordModel>()
			where v.UserId == userId && v.HabitId == habitId && v.Stamp == stamp
			select v).FirstOrDefaultAsync();
	}

	private static async Task<HabitRecordModel> GetHabitRecordByRecordId(string recordId)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from v in App.Connection.Table<HabitRecordModel>()
			where v.UserId == userId && v.Id == recordId
			select v).FirstOrDefaultAsync();
	}

	public static async Task SaveHabitCheckinRecord(string recordId, string content, int emoji)
	{
		HabitRecordModel habitRecordModel = await GetHabitRecordByRecordId(recordId);
		if (habitRecordModel != null)
		{
			habitRecordModel.Content = content;
			habitRecordModel.Emoji = emoji;
			if (habitRecordModel.Status != 0)
			{
				habitRecordModel.Status = 1;
			}
			habitRecordModel.opTime = DateTime.Now;
			await UpdateAsync(habitRecordModel);
		}
	}

	private static async Task<HabitRecordModel> GetHabitRecordByHabitIdAndStamp(string habitId, int stamp)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from v in App.Connection.Table<HabitRecordModel>()
			where v.UserId == userId && v.HabitId == habitId && v.Stamp == stamp
			select v).FirstOrDefaultAsync();
	}

	public static async Task DeleteHabitRecord(string logId)
	{
		HabitRecordModel habitRecordModel = await GetHabitRecordByRecordId(logId);
		if (habitRecordModel != null)
		{
			habitRecordModel.Deleted = 1;
			habitRecordModel.Status = 1;
			habitRecordModel.opTime = DateTime.Now;
			await UpdateAsync(habitRecordModel);
		}
	}

	public static async Task AddCheckInRecord(CheckInLogViewModel vm)
	{
		int stamp = ToHabitRecordStamp(vm.Date);
		HabitRecordModel habitRecordModel = await GetHabitRecordByHabitIdAndStamp(vm.HabitId, stamp);
		if (habitRecordModel == null)
		{
			await InsertAsync(new HabitRecordModel
			{
				Id = Utils.GetGuid(),
				Content = vm.Content,
				HabitId = vm.HabitId,
				Stamp = stamp,
				Status = 0,
				UserId = Utils.GetCurrentUserIdInt().ToString(),
				Emoji = vm.Score,
				opTime = DateTime.Now
			});
		}
		else
		{
			await SaveHabitCheckinRecord(habitRecordModel.Id, vm.Content, vm.Score);
		}
	}

	public static async Task InsertAsync(HabitRecordModel model)
	{
		await App.Connection.InsertAsync(model);
	}

	public static async Task UpdateAsync(HabitRecordModel model)
	{
		await App.Connection.UpdateAsync(model);
	}

	public static async Task UpdateAllAsync(List<HabitRecordModel> model)
	{
		await App.Connection.UpdateAllAsync(model);
	}

	public static async Task DeleteAsync(HabitRecordModel model)
	{
		await App.Connection.DeleteAsync(model);
	}

	public static async Task DeleteRecordsByHabitId(string habitId)
	{
		List<HabitRecordModel> list = await (from c in App.Connection.Table<HabitRecordModel>()
			where c.HabitId == habitId
			select c).ToListAsync();
		if (list == null || list.Count <= 0)
		{
			return;
		}
		foreach (HabitRecordModel item in list)
		{
			await DeleteAsync(item);
		}
	}
}
