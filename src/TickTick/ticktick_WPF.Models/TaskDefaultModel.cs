using System;
using System.Collections.Generic;
using System.Linq;
using SQLite;
using TickTickModels;
using ticktick_WPF.Cache;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Models;

[Serializable]
public class TaskDefaultModel : BaseModel
{
	public string UserId { get; set; }

	public int DateMode { get; set; }

	public string Date { get; set; }

	public int Duration { get; set; }

	public string TimeReminders { get; set; }

	public string AllDayReminders { get; set; }

	public int Priority { get; set; }

	public int AddTo { get; set; }

	public string TagString { get; set; }

	public string projectId { get; set; }

	[Ignore]
	public List<string> Tags
	{
		get
		{
			return TagSerializer.ToTags(TagString);
		}
		set
		{
			TagString = TagSerializer.ToJsonContent(value);
		}
	}

	[Ignore]
	public string ProjectId
	{
		get
		{
			ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == projectId);
			if (projectModel == null || !projectModel.IsEnable() || projectModel.kind == Constants.ProjectKind.NOTE.ToString())
			{
				return LocalSettings.Settings.InServerBoxId;
			}
			return projectId;
		}
		set
		{
			projectId = value;
		}
	}

	public DateTime? GetDefaultDateTime()
	{
		return GetDefaultDate(Date);
	}

	public static TaskDefaultModel BuildDefault()
	{
		return new TaskDefaultModel
		{
			UserId = Utils.GetCurrentUserIdInt().ToString(),
			DateMode = 0,
			Priority = 0,
			AddTo = 0,
			Duration = 60,
			ProjectId = LocalSettings.Settings.InServerBoxId,
			TimeReminders = "TRIGGER:PT0S"
		};
	}

	public static DateTime? GetDefaultDate(string date)
	{
		return date switch
		{
			"today" => DateTime.Today, 
			"tomorrow" => DateTime.Today.AddDays(1.0), 
			"day_after_tomorrow" => DateTime.Today.AddDays(2.0), 
			"NextWeek" => DateTime.Today.AddDays(7.0), 
			_ => null, 
		};
	}

	public bool Equal(TaskDefaultModel model)
	{
		if (DateMode == model.DateMode && Date == model.Date && Duration == model.Duration && TimeReminders == model.TimeReminders && AllDayReminders == model.AllDayReminders && Priority == model.Priority && AddTo == model.AddTo && TagString == model.TagString)
		{
			return ProjectId == model.ProjectId;
		}
		return false;
	}
}
