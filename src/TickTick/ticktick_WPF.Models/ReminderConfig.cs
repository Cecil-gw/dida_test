using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using ticktick_WPF.Views;

namespace ticktick_WPF.Models;

public class ReminderConfig : PreferenceBaseModel
{
	public bool annoyingAlertEnabled { get; set; }

	public List<string> annoyingRemindItems
	{
		get
		{
			return RemindTypes?.Select((DisplayModule x) => x.ToString().ToLower()).ToList();
		}
		set
		{
			if (value == null)
			{
				RemindTypes = null;
				return;
			}
			List<DisplayModule> list = new List<DisplayModule>();
			foreach (string item in value)
			{
				if (Enum.TryParse<DisplayModule>(item, ignoreCase: true, out var result))
				{
					list.Add(result);
				}
			}
			RemindTypes = list.Distinct().ToList();
		}
	}

	[JsonIgnore]
	public List<DisplayModule> RemindTypes { get; set; }

	public override bool SetRemoteValue(PreferenceBaseModel model)
	{
		if (model is ReminderConfig reminderConfig)
		{
			if (reminderConfig.mtime > base.mtime)
			{
				annoyingAlertEnabled = reminderConfig.annoyingAlertEnabled;
				annoyingRemindItems = reminderConfig.annoyingRemindItems;
				base.mtime = reminderConfig.mtime;
			}
			else if (reminderConfig.mtime < base.mtime)
			{
				return true;
			}
		}
		return false;
	}
}
