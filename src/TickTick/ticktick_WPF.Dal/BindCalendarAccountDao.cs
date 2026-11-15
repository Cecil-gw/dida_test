using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Sort;
using ticktick_WPF.Views.Config;

namespace ticktick_WPF.Dal;

public static class BindCalendarAccountDao
{
	public static async Task SaveBindCalendarAccounts(List<BindCalendarAccountModel> models)
	{
		List<BindCalendarAccountModel> list = CacheManager.GetAccountCalDict().Values.ToList();
		int count = list.Count;
		foreach (BindCalendarAccountModel account in models)
		{
			account?.Calendars?.ForEach(delegate(BindCalendarModel cal)
			{
				if (cal != null && cal.Color != null)
				{
					cal.Color = Utils.RgbaToArgb(cal.Color);
				}
			});
			SaveBindCalendarAccount(account, handleCalendar: true, updateExpired: false);
			list.RemoveAll((BindCalendarAccountModel a) => a.Id == account?.Id);
		}
		string logString = $"MergeBindAccount result remote:local {models.Count}:{count}";
		if (list.Any())
		{
			logString += " DeleteIds";
			foreach (BindCalendarAccountModel account2 in list)
			{
				await DeleteBindAccount(account2);
				logString = logString + " " + account2.Id;
			}
		}
		UtilLog.Info(logString);
	}

	public static async Task SaveBindCalendarAccount(BindCalendarAccountModel model, bool handleCalendar = true, bool updateExpired = true)
	{
		if (model == null)
		{
			return;
		}
		string userId = Utils.GetCurrentUserIdInt().ToString();
		if (string.IsNullOrEmpty(model.UserId))
		{
			model.UserId = userId;
		}
		long? beforeOrder = null;
		BindCalendarAccountModel account = await GetBindCalendarAccount(model.Id);
		if (account != null)
		{
			beforeOrder = account.Order;
			await DeleteBindAccount(account, deleteCalendars: false);
			if (!updateExpired)
			{
				model.Expired = account.Expired;
			}
		}
		if (model.Expired && (account == null || !account.Expired))
		{
			ShowAccountExpiredWindow(model);
		}
		if (!model.Order.HasValue)
		{
			model.Order = BindCalenderSortHelper.GetNewTopOrder();
		}
		await App.Connection.InsertAsync(model);
		if (model.Order.HasValue && model.Order != beforeOrder)
		{
			await Communicator.UploadBindCalenderSort(new UploadBindCalendarSortDto
			{
				Id = model.Id,
				Order = model.Order.Value,
				IsUrl = false
			});
		}
		CacheManager.UpdateBindAccount(model);
		if (!handleCalendar)
		{
			return;
		}
		List<BindCalendarModel> notExisted = new List<BindCalendarModel>();
		List<BindCalendarModel> updated = new List<BindCalendarModel>();
		List<BindCalendarModel> localCalendars = account?.Calendars ?? new List<BindCalendarModel>();
		if (model.Calendars == null)
		{
			return;
		}
		bool hasSendLocalCalendarVis = LocalSettings.Settings.ExtraSettings.CheckHasFlag(HasSetFlag.SendLocalAccountVis);
		int order = 0;
		foreach (BindCalendarModel cal in model.Calendars)
		{
			cal.sortOrder = order++;
			List<BindCalendarModel> localCals = localCalendars.Where((BindCalendarModel local) => local.Id == cal.Id).ToList();
			if (localCals.Count > 0)
			{
				for (int i = localCals.Count - 1; i >= 0; i--)
				{
					BindCalendarModel bindCalendarModel = localCals[i];
					localCalendars.Remove(bindCalendarModel);
					if (i == localCals.Count - 1)
					{
						if (bindCalendarModel.OutId == null || !bindCalendarModel.IsEqual(cal))
						{
							if (bindCalendarModel.Show != cal.Show)
							{
								if (!hasSendLocalCalendarVis)
								{
									Communicator.CalendarSetVisible(model.Id, cal.Id, bindCalendarModel.Show);
								}
								else
								{
									bindCalendarModel.Show = cal.Show;
								}
							}
							bindCalendarModel.OutId = cal.OutId;
							bindCalendarModel.Color = cal.Color;
							bindCalendarModel.Name = cal.Name;
							bindCalendarModel.CurrentUserPrivilegeSet = cal.CurrentUserPrivilegeSet;
							bindCalendarModel.AccessRole = cal.AccessRole;
							bindCalendarModel.AccountId = model.Id;
							bindCalendarModel.sortOrder = cal.sortOrder;
							updated.Add(bindCalendarModel);
						}
						CacheManager.UpdateBindCalendar(bindCalendarModel);
					}
					else
					{
						await App.Connection.DeleteAsync(bindCalendarModel);
					}
				}
			}
			else
			{
				cal.AccountId = model.Id;
				cal.UserId = userId;
				notExisted.Add(cal);
				CacheManager.UpdateBindCalendar(cal);
			}
		}
		if (!hasSendLocalCalendarVis)
		{
			LocalSettings.Settings.ExtraSettings.SetHasFlag(HasSetFlag.SendLocalAccountVis);
			LocalSettings.Settings.Save();
		}
		if (localCalendars.Any())
		{
			foreach (BindCalendarModel item in localCalendars)
			{
				CacheManager.DeleteBindCalendar(item);
				await App.Connection.DeleteAsync(item);
			}
		}
		if (updated.Any())
		{
			await App.Connection.UpdateAllAsync(updated);
		}
		if (notExisted.Any())
		{
			await App.Connection.InsertAllAsync(notExisted);
		}
	}

	private static void ShowAccountExpiredWindow(BindCalendarAccountModel model)
	{
		Application.Current.Dispatcher.Invoke(delegate
		{
			SubscribeExpiredWindow.TryShowWindow(new List<BindCalendarAccountModel> { model });
		});
	}

	public static async Task DeleteBindAccountById(string accountId)
	{
		List<BindCalendarAccountModel> list = await GetBindCalendarAccountById(accountId);
		if (list == null || !list.Any())
		{
			return;
		}
		foreach (BindCalendarAccountModel item in list)
		{
			await DeleteBindAccount(item);
		}
	}

	private static async Task<List<BindCalendarAccountModel>> GetBindCalendarAccountById(string accountId)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from model in App.Connection.Table<BindCalendarAccountModel>()
			where model.UserId == userId && model.Id == accountId
			select model).ToListAsync();
	}

	public static async Task DeleteBindAccounts()
	{
		List<BindCalendarAccountModel> list = await GetBindCalendarAccounts();
		if (list != null && list.Any())
		{
			foreach (BindCalendarAccountModel item in list)
			{
				await DeleteBindAccount(item);
			}
		}
		await CalendarEventDao.DeleteBindEvents();
	}

	public static async Task HandleAccountExpired(BindCalendarAccountModel account)
	{
		if (account == null)
		{
			return;
		}
		account.Expired = true;
		await SaveBindCalendarAccount(account);
		List<BindCalendarModel> list = await GetBindCalendarsByAccountId(account.Id);
		if (list == null || !list.Any())
		{
			return;
		}
		foreach (BindCalendarModel item in list)
		{
			await CalendarEventDao.DeleteByCalendarId(item.Id);
		}
	}

	private static async Task DeleteBindAccount(BindCalendarAccountModel account, bool deleteCalendars = true)
	{
		if (account != null)
		{
			await App.Connection.DeleteAsync(account);
			CacheManager.DeleteBindAccount(account);
			if (deleteCalendars)
			{
				await DeleteCalendarsByAccountId(account.Id);
				DataChangedNotifier.NotifyCalendarChanged(50);
			}
		}
	}

	public static async Task DeleteCalendarsByAccountId(string accountId)
	{
		List<BindCalendarModel> list = await GetBindCalendarsByAccountId(accountId);
		if (list == null || !list.Any())
		{
			return;
		}
		foreach (BindCalendarModel calendar in list)
		{
			CacheManager.DeleteBindCalendar(calendar);
			await App.Connection.DeleteAsync(calendar);
			await CalendarEventDao.DeleteByCalendarId(calendar.Id);
		}
	}

	public static async Task<List<BindCalendarAccountModel>> GetBindCalendarAccounts()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from model in App.Connection.Table<BindCalendarAccountModel>()
			where model.UserId == userId
			select model).ToListAsync();
	}

	public static async Task<int> GetBindCalendarAccountsCount()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from model in App.Connection.Table<BindCalendarAccountModel>()
			where model.UserId == userId
			select model).CountAsync();
	}

	public static async Task<BindCalendarAccountModel> GetBindCalendarAccount(string id)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		BindCalendarAccountModel model = await (from account in App.Connection.Table<BindCalendarAccountModel>()
			where account.Id == id && account.UserId == userId
			select account).FirstOrDefaultAsync();
		if (model != null)
		{
			List<BindCalendarModel> list = await GetBindCalendarsByAccountId(model.Id);
			if (list != null && list.Any())
			{
				model.Calendars = list;
			}
		}
		return model;
	}

	public static BindCalendarAccountModel GetBindCalendarAccountInstant(string id)
	{
		return Task.Run(async () => await GetBindCalendarAccount(id)).GetAwaiter().GetResult();
	}

	private static async Task<List<BindCalendarModel>> GetBindCalendarsByAccountId(string id)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from model in App.Connection.Table<BindCalendarModel>()
			where model.UserId == userId && model.AccountId == id
			select model).ToListAsync();
	}

	public static async Task<List<BindCalendarModel>> GetBindCalendars()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from model in App.Connection.Table<BindCalendarModel>()
			where model.UserId == userId
			select model).ToListAsync();
	}

	public static async Task<bool> SaveCalendarShowStatus(string calendarId, string status)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		BindCalendarModel bindCalendarModel = await (from model in App.Connection.Table<BindCalendarModel>()
			where model.UserId == userId && model.Id == calendarId
			select model).FirstOrDefaultAsync();
		if (bindCalendarModel != null)
		{
			string beforeStatus = bindCalendarModel.Show;
			UtilLog.Info("SaveCalendarShowStatus id: " + bindCalendarModel.Id + ",name: " + bindCalendarModel.Name + ",status: " + status);
			bindCalendarModel.Show = status;
			CacheManager.UpdateBindCalendar(bindCalendarModel);
			await App.Connection.UpdateAsync(bindCalendarModel);
			return beforeStatus != status;
		}
		return false;
	}

	public static async Task SaveCalendarInfo(BindCalendarModel calendar, string accountId)
	{
		string text = Utils.RgbaToArgb(calendar.Color);
		BindCalendarModel bindCalendarModel = CacheManager.GetBindCalendars().FirstOrDefault((BindCalendarModel model) => model.Id == calendar.Id);
		if (bindCalendarModel == null)
		{
			UtilLog.Info("SaveNewCalendarInfo id: " + calendar.Id + ",name: " + calendar.Name + ",accountId: " + accountId);
			if (!string.IsNullOrEmpty(accountId))
			{
				calendar.UserId = LocalSettings.Settings.LoginUserId;
				calendar.Show = "show";
				calendar.AccountId = accountId;
				CacheManager.UpdateBindCalendar(calendar);
				await App.Connection.InsertAsync(calendar);
			}
		}
		else if (!string.IsNullOrEmpty(calendar.Name) && !string.IsNullOrEmpty(text))
		{
			bindCalendarModel.Name = calendar.Name;
			bindCalendarModel.Color = text;
			bindCalendarModel.sortOrder = calendar.sortOrder;
			if (!string.IsNullOrEmpty(accountId))
			{
				bindCalendarModel.AccountId = accountId;
			}
			CacheManager.UpdateBindCalendar(bindCalendarModel);
			await App.Connection.UpdateAsync(bindCalendarModel);
		}
	}

	public static async Task UpdateDisplayName(string accountId, string name)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		BindCalendarAccountModel obj = await (from x in App.Connection.Table<BindCalendarAccountModel>()
			where x.Id == accountId && x.UserId == userId
			select x).FirstOrDefaultAsync();
		obj.DisplayName = name;
		await SaveBindCalendarAccount(obj);
		Communicator.UpdateBindAccountCalendarDesc(accountId, name);
		DataChangedNotifier.NotifyCalendarTitleChanged(accountId);
	}

	public static async Task SaveOrder(string calendarId, long order)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		BindCalendarAccountModel profile = await (from model in App.Connection.Table<BindCalendarAccountModel>()
			where model.UserId == userId && model.Id == calendarId
			select model).FirstOrDefaultAsync();
		if (profile != null && profile.Order != order)
		{
			profile.Order = order;
			await App.Connection.UpdateAsync(profile);
			CacheManager.UpdateBindAccount(profile);
			await Communicator.UploadBindCalenderSort(new UploadBindCalendarSortDto
			{
				Id = calendarId,
				Order = order,
				IsUrl = false
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
		string userId = Utils.GetCurrentUserIdInt().ToString();
		List<BindCalendarAccountModel> list = await (from model in App.Connection.Table<BindCalendarAccountModel>()
			where model.UserId == userId
			select model).ToListAsync();
		if (list == null || !list.Any())
		{
			return;
		}
		List<UploadBindCalendarSortDto> uploads = new List<UploadBindCalendarSortDto>();
		foreach (BindCalendarAccountModel item in list)
		{
			if (orders.TryGetValue(item.Id, out var value))
			{
				item.Order = value;
				CacheManager.UpdateBindAccount(item);
				uploads.Add(new UploadBindCalendarSortDto
				{
					Id = item.Id,
					Order = value,
					IsUrl = false
				});
			}
		}
		await App.Connection.UpdateAllAsync(list);
		await Communicator.UploadBindCalenderSort(uploads.ToArray());
		DataChangedNotifier.NotifyCalendarProjectFilterChanged(null);
	}
}
