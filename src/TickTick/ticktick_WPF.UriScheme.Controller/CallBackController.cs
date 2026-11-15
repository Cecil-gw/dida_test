using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.UriScheme.Dto;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.UriScheme.Controller;

[UriRoute("x-callback-url")]
internal class CallBackController : UriControllerBase
{
	[UriRoute("v1/add_task")]
	public async Task AddTask(AddTaskUriWithCallBackDto dto)
	{
		try
		{
			if (string.IsNullOrEmpty(dto.Title))
			{
				throw new Exception("title cannot be null");
			}
			if ((dto.StartDate.HasValue || dto.EndDate.HasValue) && !dto.AllDay.HasValue)
			{
				throw new Exception("allDay cannot be null when date has value");
			}
			List<ProjectModel> projects = CacheManager.GetProjects();
			ProjectModel project = ((!string.IsNullOrEmpty(dto.List)) ? projects.FirstOrDefault((ProjectModel x) => x.name == dto.List) : projects.FirstOrDefault((ProjectModel x) => x.Isinbox));
			if (project == null)
			{
				throw new Exception("list '" + dto.List + "' don't exist");
			}
			if (await ProChecker.CheckTaskLimit(project.ProjectId))
			{
				throw new Exception("list '" + dto.List + "' task count is over limit");
			}
			TaskModel task = new TaskModel
			{
				id = Utils.GetGuid(),
				title = dto.Title,
				content = dto.Content,
				priority = dto.Priority,
				projectId = project.ProjectId,
				startDate = dto.StartDate,
				dueDate = dto.EndDate,
				isAllDay = dto.AllDay
			};
			if (!string.IsNullOrEmpty(dto.Tags))
			{
				task.tags = dto.Tags.Split(',');
				task.tag = TagSerializer.ToJsonContent(task.tags.ToList());
			}
			await TaskService.AddTask(task);
			if (!string.IsNullOrEmpty(dto.xsuccess))
			{
				Process.Start(new ProcessStartInfo(AddQueryParameters(new Uri(dto.xsuccess), "title=" + dto.Title + "&taskID=" + task.id).ToString())
				{
					UseShellExecute = true
				});
			}
		}
		catch (Exception ex)
		{
			if (!string.IsNullOrEmpty(dto.xerror))
			{
				Process.Start(new ProcessStartInfo(AddQueryParameters(new Uri(dto.xerror), "message=" + ex.Message).ToString())
				{
					UseShellExecute = true
				});
			}
			UtilLog.Error(ex);
		}
	}

	private Uri AddQueryParameters(Uri uri, string queryToAdd)
	{
		UriBuilder uriBuilder = new UriBuilder(uri);
		NameValueCollection nameValueCollection = HttpUtility.ParseQueryString(uriBuilder.Query);
		NameValueCollection nameValueCollection2 = HttpUtility.ParseQueryString(queryToAdd);
		foreach (string key in nameValueCollection2.Keys)
		{
			nameValueCollection.Set(key, nameValueCollection2[key]);
		}
		uriBuilder.Query = nameValueCollection.ToString();
		return uriBuilder.Uri;
	}
}
