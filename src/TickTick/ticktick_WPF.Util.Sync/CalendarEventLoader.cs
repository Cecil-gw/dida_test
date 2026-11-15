using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Service;

namespace ticktick_WPF.Util.Sync;

public static class CalendarEventLoader
{
	private static DateTime? _loadedStart;

	private static DateTime? _loadedEnd;

	private static bool _inited;

	public static void Reset()
	{
		_inited = false;
	}

	private static void Init(DateTime? start = null, DateTime? end = null)
	{
		_loadedStart = start;
		_loadedEnd = end;
		_inited = true;
	}

	public static async void PullRemoteEvent(DateTime start, DateTime end)
	{
		if (!_inited)
		{
			Init(start, end);
		}
		List<BindCalendarAccountModel> list = await BindCalendarAccountDao.GetBindCalendarAccounts();
		if (list == null || list.Count == 0)
		{
			return;
		}
		DelayActionHandlerCenter.TryDoAction("PullEventInCalendar", async delegate
		{
			DateTime value = start;
			DateTime? loadedStart = _loadedStart;
			if (value >= loadedStart)
			{
				value = end;
				loadedStart = _loadedEnd;
				if (value <= loadedStart)
				{
					return;
				}
			}
			if (start < _loadedStart.Value && (_loadedStart.Value - start).TotalDays <= 100.0)
			{
				if (end < _loadedEnd.Value)
				{
					end = _loadedStart.Value;
				}
				else
				{
					_loadedEnd = end;
				}
				_loadedStart = start;
			}
			else if (end > _loadedEnd.Value && (end - _loadedEnd.Value).TotalDays <= 100.0)
			{
				if (start > _loadedStart.Value)
				{
					start = _loadedEnd.Value;
				}
				else
				{
					_loadedStart = start;
				}
				_loadedEnd = end;
			}
			else
			{
				Init(start, end);
			}
			int times = 0;
			while (CalendarService.IsPulling && times <= 120)
			{
				await Task.Delay(500);
				times++;
			}
			CalendarService.PullAccountCalendarsAndEvents(start, end);
		}, 600);
	}
}
