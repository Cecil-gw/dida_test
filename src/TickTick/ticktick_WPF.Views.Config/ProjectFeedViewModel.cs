using System;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Config;

internal class ProjectFeedViewModel
{
	public const string UrlBase = "webcal://{0}/pub/calendar/feeds/{1}/basic.ics";

	public string Name { get; set; }

	public string Url { get; set; }

	public DateTime CreatedTime { get; set; }

	public string ProjectId { get; internal set; }

	public ProjectFeedViewModel(ProjectFeedCode code)
	{
		if (code.projectId == "all")
		{
			Name = Utils.GetString("AllList");
		}
		else if (code.projectId.StartsWith("inbox"))
		{
			Name = Utils.GetString("Inbox");
		}
		else
		{
			ProjectModel projectById = CacheManager.GetProjectById(code.projectId);
			Name = ((projectById == null) ? Utils.GetString("UnknownProject") : projectById.name);
		}
		ProjectId = code.projectId;
		Url = $"webcal://{BaseUrl.Domain}/pub/calendar/feeds/{code.code}/basic.ics";
		CreatedTime = code.createdTime ?? DateTime.Now;
	}
}
