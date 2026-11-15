using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Sort;
using ticktick_WPF.Views.Config;

namespace ticktick_WPF.Dal;

public static class CalendarSubscribeProfileDao
{
	public static async Task SaveProfiles(List<CalendarSubscribeProfileModel> models)
	{
		if (models == null)
		{
			return;
		}
		if (!models.Any())
		{
			DeleteAll();
			return;
		}
		List<CalendarSubscribeProfileModel> locals = CacheManager.GetSubscribeCalendars();
		List<CalendarSubscribeProfileModel> notExisted = new List<CalendarSubscribeProfileModel>();
		bool hasSendLocalUrlVis = LocalSettings.Settings.ExtraSettings.CheckHasFlag(HasSetFlag.SendLocalUrlVis);
		foreach (CalendarSubscribeProfileModel model in models)
		{
			CalendarSubscribeProfileModel local = locals.FirstOrDefault((CalendarSubscribeProfileModel m) => m.Id == model.Id);
			if (local != null)
			{
				model.Expired = local.Expired;
				locals.Remove(local);
				if (!(local.Name != model.Name) && !(local.Show != model.Show) && local.ReminderEnable == model.ReminderEnable && !(local.Color != model.Color) && local.Order == model.Order && local.Kind == model.Kind)
				{
					continue;
				}
				if (local.Show != model.Show)
				{
					if (!hasSendLocalUrlVis)
					{
						Communicator.CalendarUrlSetVisible(model.Id, local.Show);
					}
					else
					{
						local.Show = model.Show;
					}
				}
				local.Kind = model.Kind;
				local.Name = model.Name;
				local.ReminderEnable = model.ReminderEnable;
				local.Color = model.Color;
				local.Order = model.Order;
				await App.Connection.UpdateAsync(local);
				CacheManager.UpdateCalendarProfile(local);
			}
			else if (notExisted.All((CalendarSubscribeProfileModel m) => m.Id != model.Id))
			{
				notExisted.Add(model);
				CacheManager.UpdateCalendarProfile(model);
			}
		}
		if (!hasSendLocalUrlVis)
		{
			LocalSettings.Settings.ExtraSettings.SetHasFlag(HasSetFlag.SendLocalUrlVis);
			LocalSettings.Settings.Save();
		}
		foreach (CalendarSubscribeProfileModel local in locals)
		{
			await App.Connection.DeleteAsync(local);
			await CalendarEventDao.DeleteByCalendarId(local.Id);
			CacheManager.DeleteCalendarProfile(local);
		}
		await App.Connection.InsertAllAsync(notExisted);
		SubscribeCalendarHelper.ParseUrlCalendars(models);
	}

	public static async Task<CalendarSubscribeProfileModel> GetProfileById(string calendarId)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from model in App.Connection.Table<CalendarSubscribeProfileModel>()
			where model.UserId == userId && model.Id == calendarId
			select model).FirstOrDefaultAsync();
	}

	public static async Task<int> GetProfilesCount()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from model in App.Connection.Table<CalendarSubscribeProfileModel>()
			where model.UserId == userId
			select model).CountAsync();
	}

	public static async Task<List<CalendarSubscribeProfileModel>> GetProfiles(bool onlyVisible = false)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		List<CalendarSubscribeProfileModel> list = await (from calendarSubscribeProfileModel in App.Connection.Table<CalendarSubscribeProfileModel>()
			where calendarSubscribeProfileModel.UserId == userId && calendarSubscribeProfileModel.Id != null
			select calendarSubscribeProfileModel).ToListAsync();
		List<CalendarSubscribeProfileModel> result = new List<CalendarSubscribeProfileModel>();
		if (list != null)
		{
			foreach (CalendarSubscribeProfileModel model in list)
			{
				if (result.All((CalendarSubscribeProfileModel m) => m.Id != model.Id))
				{
					result.Add(model);
					continue;
				}
				App.Connection.DeleteAsync(model);
				await CalendarEventDao.DeleteByCalendarId(model.Id);
			}
		}
		if (onlyVisible && result.Any())
		{
			return result.Where((CalendarSubscribeProfileModel calendarSubscribeProfileModel) => calendarSubscribeProfileModel.Show != "hidden").ToList();
		}
		return result;
	}

	public static async Task DeleteCalendar(string calendarId)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		List<CalendarSubscribeProfileModel> list = await (from model in App.Connection.Table<CalendarSubscribeProfileModel>()
			where model.UserId == userId && model.Id == calendarId
			select model).ToListAsync();
		if (list == null || !list.Any())
		{
			return;
		}
		foreach (CalendarSubscribeProfileModel profile in list)
		{
			await App.Connection.DeleteAsync(profile);
			await CalendarEventDao.DeleteByCalendarId(profile.Id);
			CacheManager.DeleteCalendarProfile(profile);
		}
	}

	public static async Task DeleteAll()
	{
		List<CalendarSubscribeProfileModel> list = await GetProfiles();
		if (list != null && list.Any())
		{
			foreach (CalendarSubscribeProfileModel profile in list)
			{
				await App.Connection.DeleteAsync(profile);
				await CalendarEventDao.DeleteByCalendarId(profile.Id);
			}
		}
		CacheManager.ClearCalendarProfile();
	}

	public static async Task AddOrUpdateCalendar(CalendarSubscribeProfileModel profile)
	{
		CalendarSubscribeProfileModel calendarSubscribeProfileModel = await GetProfileById(profile.Id);
		if (calendarSubscribeProfileModel != null)
		{
			profile._Id = calendarSubscribeProfileModel._Id;
			await App.Connection.UpdateAsync(profile);
		}
		else
		{
			if (!profile.Order.HasValue)
			{
				profile.Order = BindCalenderSortHelper.GetNewTopOrder();
			}
			await App.Connection.InsertAsync(profile);
			if (profile.Order.HasValue)
			{
				await Communicator.UploadBindCalenderSort(new UploadBindCalendarSortDto
				{
					Id = profile.Id,
					Order = profile.Order.Value,
					IsUrl = true
				});
			}
		}
		CacheManager.UpdateCalendarProfile(profile);
	}

	public static async Task<bool> UpdateUrlSubscribe(SubscribeCalendarViewModel data)
	{
		CalendarSubscribeProfileModel profile = await GetProfileById(data.CalendarId);
		if (profile == null)
		{
			return false;
		}
		if (profile.Show != data.ShowStatus.ToString() || profile.Color != data.Color || profile.Name != data.Title)
		{
			string lastStatus = profile.Show;
			string lastName = profile.Name;
			profile.ShowType = data.ShowStatus;
			profile.Color = data.Color;
			profile.Name = data.Title;
			await SubscribeCalendarHelper.UpdateUrlOrHolidaySubscribe(profile);
			if (lastName != data.Title)
			{
				DataChangedNotifier.NotifyCalendarTitleChanged(profile.Id);
			}
			return lastStatus != data.ShowStatus.ToString();
		}
		return false;
	}

	public static async Task<bool> UpdateHolidaySubscribe(SubscribeCalendarViewModel data)
	{
		CalendarSubscribeProfileModel profile = await GetProfileById(data.CalendarId);
		if (profile == null)
		{
			return false;
		}
		if (profile.Show != data.ShowStatus.ToString() || profile.Color != data.Color || profile.Name != data.Title || profile.ReminderEnable != data.ReminderEnable)
		{
			string lastStatus = profile.Show;
			bool? lastRemindEnable = profile.ReminderEnable;
			string lastName = profile.Name;
			profile.ShowType = data.ShowStatus;
			profile.Color = data.Color;
			profile.Name = data.Title;
			profile.ReminderEnable = data.ReminderEnable;
			await SubscribeCalendarHelper.UpdateUrlOrHolidaySubscribe(profile);
			if (lastRemindEnable != profile.ReminderEnable)
			{
				Task.Run(async delegate
				{
					await SubscribeCalendarHelper.ParseUrlCalendar(profile.Id, profile.Url);
					CalendarEventChangeNotifier.NotifyRemoteChanged();
				});
			}
			if (lastName != data.Title)
			{
				DataChangedNotifier.NotifyCalendarTitleChanged(profile.Id);
			}
			return lastStatus != data.ShowStatus.ToString();
		}
		return false;
	}

	private static string GetShowStatus(int index)
	{
		return index switch
		{
			0 => "show", 
			1 => "calendar", 
			2 => "hidden", 
			_ => "show", 
		};
	}

	public static async Task SaveOrder(string calendarId, long order)
	{
		CalendarSubscribeProfileModel profile = await GetProfileById(calendarId);
		if (profile != null && profile.Order != order)
		{
			profile.Order = order;
			await App.Connection.UpdateAsync(profile);
			CacheManager.UpdateCalendarProfile(profile);
			await Communicator.UploadBindCalenderSort(new UploadBindCalendarSortDto
			{
				Id = calendarId,
				Order = order,
				IsUrl = true
			});
			DataChangedNotifier.NotifyCalendarProjectFilterChanged(null);
		}
	}

	public static async Task SaveOrders(Dictionary<string, long> orders)
	{
		if (orders == null)
		{
			return;
		}
		List<CalendarSubscribeProfileModel> list = await GetProfiles();
		if (list == null || !list.Any())
		{
			return;
		}
		List<UploadBindCalendarSortDto> uploads = new List<UploadBindCalendarSortDto>();
		foreach (CalendarSubscribeProfileModel item in list)
		{
			if (orders.TryGetValue(item.Id, out var value))
			{
				item.Order = value;
				CacheManager.UpdateCalendarProfile(item);
				uploads.Add(new UploadBindCalendarSortDto
				{
					Id = item.Id,
					Order = value,
					IsUrl = true
				});
			}
		}
		await App.Connection.UpdateAllAsync(list);
		await Communicator.UploadBindCalenderSort(uploads.ToArray());
		DataChangedNotifier.NotifyCalendarProjectFilterChanged(null);
	}
}
