using System.Collections.Generic;
using System.Threading.Tasks;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;

namespace ticktick_WPF.Util;

public static class AgendaHelper
{
	public interface IAgenda
	{
		string GetTaskId();

		string GetAttendId();
	}

	private static readonly Dictionary<string, TaskAttendModel> AttendDict = new Dictionary<string, TaskAttendModel>();

	public static bool CanAccessAgenda(IAgenda agenda)
	{
		return agenda.GetAttendId() == agenda.GetTaskId();
	}

	public static async Task<TaskAttendModel> GetAgendaAttendModel(string attendId, string taskId)
	{
		if (string.IsNullOrEmpty(attendId))
		{
			return null;
		}
		if (AttendDict.ContainsKey(attendId))
		{
			return AttendDict[attendId];
		}
		TaskAttendModel taskAttendModel = await AttendModelDao.GetTaskAttendModelById(attendId);
		if (taskAttendModel != null)
		{
			if (AttendDict.ContainsKey(attendId))
			{
				AttendDict[attendId] = taskAttendModel;
			}
			else
			{
				AttendDict.Add(attendId, taskAttendModel);
			}
			return taskAttendModel;
		}
		return await GetRemoteModel(attendId, taskId);
	}

	public static async Task<TaskAttendModel> GetRemoteModel(string attendId, string taskId)
	{
		TaskAttendModel model = await Communicator.LoadTaskAttendInfo(attendId, taskId);
		if (model != null)
		{
			if (AttendDict.ContainsKey(attendId))
			{
				AttendDict[attendId] = model;
			}
			else
			{
				AttendDict.Add(attendId, model);
			}
			await AttendModelDao.SaveTaskAttendModel(attendId, model);
			return model;
		}
		return null;
	}
}
