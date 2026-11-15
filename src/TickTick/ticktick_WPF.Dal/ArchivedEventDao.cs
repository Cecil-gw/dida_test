using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Dal;

public class ArchivedEventDao : ArchivedDao
{
	public static async Task RemoveArchivedEvent(string key)
	{
		List<ArchivedEventModel> list = await (from cal in App.Connection.Table<ArchivedEventModel>()
			where cal.Key == key
			select cal).ToListAsync();
		if (!list.Any())
		{
			return;
		}
		foreach (ArchivedEventModel item in list)
		{
			item.SyncStatus = -1;
		}
		await App.Connection.UpdateAllAsync(list);
		DataChangedNotifier.NotifyEventArchivedChanged();
	}

	public static string GetRealId(string id)
	{
		if (!string.IsNullOrEmpty(id) && id.Contains("@"))
		{
			int length = id.IndexOf("@", StringComparison.Ordinal);
			id = id.Substring(0, length);
		}
		return id;
	}

	public static async Task AddArchivedModel(EventArchiveArgs args)
	{
		string key = GenerateEventKey(args);
		ArchivedEventModel item = new ArchivedEventModel
		{
			UserId = LocalSettings.Settings.LoginUserId,
			Key = key,
			StartTime = args.StartTime,
			Kind = 0,
			Title = args.Title
		};
		await App.Connection.InsertAsync(item);
		DataChangedNotifier.NotifyEventArchivedChanged();
	}

	public static string GenerateOriginEventKey(CalendarEventModel model, DateTime? startDate, DateTime? dueDate, bool isAllDay)
	{
		if (isAllDay)
		{
			startDate = startDate?.Date;
			dueDate = dueDate?.Date;
		}
		long num = 1L;
		if (!string.IsNullOrEmpty(model.Id))
		{
			num = ArchivedDao.GetOriginalId(model.Id).GetHashCode();
			num = 31 * num + (startDate.HasValue ? startDate.GetHashCode() : 0);
			num = 31 * num + (dueDate.HasValue ? dueDate.GetHashCode() : 0);
			num = 31 * num + (model.RepeatFlag?.GetHashCode() ?? 0);
		}
		else
		{
			if (!string.IsNullOrEmpty(model.Title))
			{
				num = 31 * num + model.Title.GetHashCode();
			}
			num = 31 * num + (startDate.HasValue ? startDate.GetHashCode() : 0);
			num = 31 * num + (dueDate.HasValue ? dueDate.GetHashCode() : 0);
			num = 31 * num + (model.RepeatFlag?.GetHashCode() ?? 0);
		}
		return num + string.Empty;
	}

	public static string GenerateEventKey(EventArchiveArgs args)
	{
		if ((args.EndTime == args.StartTime || !args.EndTime.HasValue) && args.StartTime.HasValue && args.IsAllDay)
		{
			args.EndTime = args.StartTime.Value.AddDays(1.0);
		}
		if (args.IsAllDay)
		{
			args.StartTime = args.StartTime?.Date;
			args.EndTime = args.EndTime?.Date;
		}
		args.Id = GetRealId(args.Id);
		string str = ((!string.IsNullOrEmpty(args.Id)) ? ArchivedDao.GetOriginalId(args.Id) : args.Title);
		long num = GetHashCode(31, 0, GetBytesFromString(str));
		num += GetHashCode(31, 0, (!args.StartTime.HasValue) ? null : GetBytesFromString(Utils.GetTimeStamp(args.StartTime) + string.Empty));
		num += GetHashCode(31, 0, (!args.EndTime.HasValue) ? null : GetBytesFromString(Utils.GetTimeStamp(args.EndTime) + string.Empty));
		string repeatFlag = args.RepeatFlag;
		if (repeatFlag != null && repeatFlag.Length > 0 && !args.RepeatFlag.StartsWith("RRULE:"))
		{
			args.RepeatFlag = "RRULE:" + args.RepeatFlag;
		}
		return num + GetHashCode(31, 0, GetBytesFromString(args.RepeatFlag)) + string.Empty;
	}

	public static int GetHashCode(int prime, int start, byte[] bytes)
	{
		if (bytes == null)
		{
			return start;
		}
		int num = start;
		foreach (byte b in bytes)
		{
			num = num * prime + b;
		}
		return num;
	}

	public static byte[] GetBytesFromString(string str)
	{
		if (string.IsNullOrEmpty(str))
		{
			return null;
		}
		return new ASCIIEncoding().GetBytes(str.ToCharArray());
	}
}
