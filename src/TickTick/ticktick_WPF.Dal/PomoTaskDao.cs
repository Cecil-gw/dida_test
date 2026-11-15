using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Dal;

public class PomoTaskDao
{
	public static async Task<int> DeleteAllByPomoId(string pomoId)
	{
		return await App.Connection.ExecuteAsync("DELETE FROM PomoTask WHERE PomoId=?", pomoId);
	}

	public static async Task UpdateAsync(PomoTask pomo)
	{
		await App.Connection.UpdateAsync(pomo);
	}

	public static async Task DeleteAsync(PomoTask pomo)
	{
		await App.Connection.DeleteAsync(pomo);
	}

	public static async Task InsertAsync(PomoTask pomo)
	{
		await App.Connection.InsertAsync(pomo);
	}

	public static async Task InsertAllAsync(IEnumerable<PomoTask> pomos)
	{
		await App.Connection.InsertAllAsync(pomos);
	}

	public static async Task<List<PomoTask>> GetTodayPomoTasks()
	{
		string text = DateTime.Today.ToString("yyyy'-'MM'-'dd' 00:00:00'", App.Ci);
		return await App.Connection.QueryAsync<PomoTask>("Select * FROM PomoTask WHERE EndTime > '" + text + "'", Array.Empty<object>());
	}

	public static async Task<List<PomoTask>> GetRecentPomoTasks(int days)
	{
		string text = DateTime.Today.AddDays(-1 * days).ToString("yyyy'-'MM'-'dd' 00:00:00'", App.Ci);
		return await App.Connection.QueryAsync<PomoTask>("Select * FROM PomoTask WHERE EndTime > '" + text + "'", Array.Empty<object>());
	}

	public static async Task<List<PomoTask>> GetPomoTaskOfTimer(string timerId, string objId, bool isHabit, DateTime? start = null, DateTime? end = null, bool isNew = false, bool delete = false)
	{
		string text = "";
		if (start.HasValue && end.HasValue)
		{
			string text2 = start.Value.ToString("yyyy'-'MM'-'dd' 00:00:00'", App.Ci);
			string text3 = end.Value.ToString("yyyy'-'MM'-'dd' 23:59:59'", App.Ci);
			text = " and b.StartTime >= '" + text2 + "' and b.StartTime < '" + text3 + "'";
		}
		string text4 = (delete ? " and a.SyncStatus = -1 and a.Etag is not null " : (isNew ? " and a.SyncStatus = 0 " : " and ( a.SyncStatus = 1 or a.SyncStatus = 2 )"));
		string text5 = (string.IsNullOrEmpty(objId) ? (" b.TimerSid = '" + timerId + "' ") : ((!isHabit) ? (" ( b.TimerSid = '" + timerId + "' or b.TaskId = '" + objId + "' ) ") : ("( b.TimerSid = '" + timerId + "' or b.HabitId = '" + objId + "' ) ")));
		string sql = "Select b.StartTime,b.EndTime FROM PomodoroModel a,PomoTask b WHERE a.UserId = '" + LocalSettings.Settings.LoginUserId + "' and  a.Id = b.PomoId and " + text5 + " " + text4 + " " + text + " ";
		return await App.Connection.QueryAsync<PomoTask>(sql, Array.Empty<object>());
	}
}
