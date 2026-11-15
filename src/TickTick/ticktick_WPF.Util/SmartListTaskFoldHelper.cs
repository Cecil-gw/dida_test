using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TickTickUtils.Lang;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Util;

public class SmartListTaskFoldHelper
{
	private static Dictionary<string, HashSet<string>> _statusDict = new Dictionary<string, HashSet<string>>();

	public static HashSet<string> GetOpenedTaskIds(string catId)
	{
		if (string.IsNullOrEmpty(catId) || !_statusDict.ContainsKey(catId))
		{
			return new HashSet<string>();
		}
		return _statusDict[catId];
	}

	public static void UpdateStatus(string taskId, string catId, bool isFolded)
	{
		if (isFolded)
		{
			RemoveStatus(taskId, catId);
		}
		else
		{
			AddStatus(taskId, catId);
		}
	}

	private static void RemoveStatus(string taskId, string catId)
	{
		if (_statusDict.ContainsKey(catId))
		{
			_statusDict[catId]?.Remove(taskId);
		}
		Save();
	}

	private static void AddStatus(string taskId, string catId)
	{
		if (_statusDict.ContainsKey(catId))
		{
			HashSet<string> hashSet = _statusDict[catId];
			if (hashSet == null)
			{
				_statusDict[catId] = new HashSet<string> { taskId };
			}
			else
			{
				hashSet.Add(taskId);
			}
		}
		else
		{
			_statusDict[catId] = new HashSet<string> { taskId };
		}
		Save();
	}

	public static void ResetStatus(string catId, List<string> ids)
	{
		_statusDict[catId] = ((ids != null && ids.Count > 0) ? new HashSet<string>(ids) : new HashSet<string>());
		Save();
	}

	public static void Save()
	{
		try
		{
			string taskFoldStatus = JsonConvert.SerializeObject(_statusDict);
			LocalSettings.Settings.ExtraSettings.TaskFoldStatus = taskFoldStatus;
			LocalSettings.Settings.SetChanged();
			LocalSettings.Settings.Save();
		}
		catch (Exception ex)
		{
			UtilLog.Error(ex);
		}
	}

	public static void Load()
	{
		string taskFoldStatus = LocalSettings.Settings.ExtraSettings.TaskFoldStatus;
		try
		{
			if (taskFoldStatus != null && !string.IsNullOrEmpty(taskFoldStatus) && !string.Equals(taskFoldStatus.ToLower(), "null"))
			{
				_statusDict = JsonConvert.DeserializeObject<Dictionary<string, HashSet<string>>>(taskFoldStatus);
			}
			else
			{
				_statusDict = new Dictionary<string, HashSet<string>>();
			}
		}
		catch (Exception ex)
		{
			UtilLog.Error(ex);
			_statusDict = new Dictionary<string, HashSet<string>>();
		}
	}
}
