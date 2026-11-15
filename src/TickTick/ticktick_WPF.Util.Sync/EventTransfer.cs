using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Util.Sync.Model;

namespace ticktick_WPF.Util.Sync;

public static class EventTransfer
{
	private enum ModifyType
	{
		Add,
		Modify,
		Move,
		Delete
	}

	public static async Task<SyncEventBean> DescribeSyncTaskBean(List<SyncStatusModel> addedEvents, List<SyncStatusModel> updatedEvents, List<SyncStatusModel> movedEvents, List<SyncStatusModel> deletedEvents, bool isCalDav = false)
	{
		SyncEventBean bean = new SyncEventBean();
		List<BindCalendarModel> bindCalendars = CacheManager.GetBindCalendars();
		List<BindCalendarAccountModel> list = ((!isCalDav) ? CacheManager.GetBindCalendarAccounts().FindAll((BindCalendarAccountModel bindCalendarAccountModel) => !bindCalendarAccountModel.IsCalDav()) : CacheManager.GetBindCalendarAccounts().FindAll((BindCalendarAccountModel bindCalendarAccountModel) => bindCalendarAccountModel.IsCalDav()));
		if (list.Any())
		{
			foreach (BindCalendarAccountModel account in list)
			{
				CalendarAccountBean calendarAccountBean = new CalendarAccountBean
				{
					AccountId = account.Id
				};
				List<BindCalendarModel> list2 = bindCalendars.Where((BindCalendarModel cal) => cal.AccountId == account.Id && cal.Accessible).ToList();
				List<CalendarEventBean> list3 = new List<CalendarEventBean>();
				if (list2.Any())
				{
					foreach (BindCalendarModel item in list2)
					{
						list3.Add(new CalendarEventBean
						{
							CalendarId = item.Id
						});
					}
				}
				calendarAccountBean.CalendarBeans = list3;
				bean.CalendarAccountBeans.Add(calendarAccountBean);
			}
		}
		if (movedEvents.Any())
		{
			foreach (SyncStatusModel movedEvent in movedEvents)
			{
				await AddUpdateEvent(bean, movedEvent, ModifyType.Move);
			}
		}
		if (addedEvents.Any())
		{
			foreach (SyncStatusModel addedEvent in addedEvents)
			{
				await AddUpdateEvent(bean, addedEvent, ModifyType.Add);
			}
		}
		if (updatedEvents.Any())
		{
			foreach (SyncStatusModel updatedEvent in updatedEvents)
			{
				await AddUpdateEvent(bean, updatedEvent, ModifyType.Modify);
			}
		}
		if (deletedEvents.Any())
		{
			foreach (SyncStatusModel deletedEvent in deletedEvents)
			{
				await AddUpdateEvent(bean, deletedEvent, ModifyType.Delete);
			}
		}
		SyncEventBean syncEventBean = new SyncEventBean();
		foreach (CalendarAccountBean calendarAccountBean2 in bean.CalendarAccountBeans)
		{
			List<CalendarEventBean> list4 = new List<CalendarEventBean>();
			if (calendarAccountBean2.CalendarBeans.Any())
			{
				foreach (CalendarEventBean calendarBean in calendarAccountBean2.CalendarBeans)
				{
					if (calendarBean.Add.Any() || calendarBean.Move.Any() || calendarBean.Delete.Any() || calendarBean.Update.Any())
					{
						list4.Add(calendarBean);
					}
				}
			}
			if (list4.Any())
			{
				calendarAccountBean2.CalendarBeans = list4;
				syncEventBean.CalendarAccountBeans.Add(calendarAccountBean2);
			}
		}
		return syncEventBean;
	}

	private static async Task AddUpdateEvent(SyncEventBean eventBean, SyncStatusModel model, ModifyType type)
	{
		CalendarEventModel calendarEventModel = await CalendarEventDao.GetEventByEventId(model.EntityId);
		if (calendarEventModel == null)
		{
			return;
		}
		calendarEventModel.Id = calendarEventModel.EventId;
		if (!calendarEventModel.DueStart.HasValue)
		{
			calendarEventModel.DueStart = DateTime.Today;
		}
		if (!calendarEventModel.DueEnd.HasValue)
		{
			calendarEventModel.DueEnd = DateTime.Today;
		}
		if (calendarEventModel.IsAllDay)
		{
			double totalMinutes = DateTimeOffset.Now.Offset.TotalMinutes;
			calendarEventModel.DueStart = calendarEventModel.DueStart.Value.Date.AddMinutes(totalMinutes);
			calendarEventModel.DueEnd = calendarEventModel.DueEnd.Value.Date.AddMinutes(totalMinutes);
		}
		if (calendarEventModel.ReminderList != null && calendarEventModel.ReminderList.Any())
		{
			calendarEventModel.ReminderList = calendarEventModel.ReminderList.Where((int reminder) => reminder >= 0).ToList();
		}
		foreach (CalendarAccountBean calendarAccountBean in eventBean.CalendarAccountBeans)
		{
			foreach (CalendarEventBean calendarBean in calendarAccountBean.CalendarBeans)
			{
				if (type == ModifyType.Move)
				{
					if (calendarBean.CalendarId == model.MoveFromId)
					{
						calendarBean.Move.Add(new EventMoveModel
						{
							EventId = calendarEventModel.EventId,
							Destination = calendarEventModel.CalendarId
						});
						break;
					}
				}
				else if (calendarBean.CalendarId == calendarEventModel.CalendarId)
				{
					switch (type)
					{
					case ModifyType.Add:
						calendarBean.Add.Add(calendarEventModel);
						break;
					case ModifyType.Modify:
						calendarBean.Update.Add(calendarEventModel);
						break;
					case ModifyType.Delete:
						calendarBean.Delete.Add(calendarEventModel.Id);
						break;
					}
				}
			}
		}
	}
}
