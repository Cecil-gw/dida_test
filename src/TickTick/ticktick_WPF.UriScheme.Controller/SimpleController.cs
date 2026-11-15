using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.UriScheme.Dto;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Lock;
using ticktick_WPF.Views.Search;
using ticktick_WPF.Views.Tag;
using ticktick_WPF.Views.Upgrade;

namespace ticktick_WPF.UriScheme.Controller;

[UriRoute("v1")]
internal class SimpleController : UriControllerBase
{
	public async Task Show(string smartlist)
	{
		if (string.IsNullOrEmpty(smartlist))
		{
			return;
		}
		SmartListType? type = null;
		switch (smartlist)
		{
		case "all":
			type = SmartListType.All;
			break;
		case "today":
			type = SmartListType.Today;
			break;
		case "tomorrow":
			type = SmartListType.Tomorrow;
			break;
		case "next_7_days":
			type = SmartListType.Week;
			break;
		case "assign_to_me":
			type = SmartListType.Assign;
			break;
		}
		if (!type.HasValue)
		{
			return;
		}
		if (await AppLockCache.GetAppLocked())
		{
			AppUnlockWindow.TryUnlockApp(delegate
			{
				App.Window?.NavigateSmartListProject(type.Value);
			});
		}
		else
		{
			App.Window?.NavigateSmartListProject(type.Value);
		}
	}

	public async Task Search(string keyword)
	{
		if (!string.IsNullOrEmpty(keyword))
		{
			SearchExtra searchExtra = new SearchExtra
			{
				from = "list",
				SearchKey = keyword
			};
			await (App.Window?.OnSearch(searchExtra, restore: false));
			App.Window?.TryShowMainWindow();
		}
	}

	[UriRoute("add_task")]
	public async Task AddTask(AddTaskUriDto dto)
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
			TaskModel taskModel = new TaskModel
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
				taskModel.tags = dto.Tags.Split(',');
				taskModel.tag = TagSerializer.ToJsonContent(taskModel.tags.ToList());
			}
			await TaskService.AddTask(taskModel);
		}
		catch (Exception ex)
		{
			UtilLog.Error(ex);
		}
	}

	[UriRoute("/app/payment")]
	public async Task ShowAfterPayment(string result)
	{
		if (!(result != "success"))
		{
			Application.Current.Dispatcher.Invoke((Func<Task>)async delegate
			{
				await Task.Delay(300);
				App.Window?.ShowWindow();
				PayProWindow.PayTime = DateTime.Now;
				App.Window?.PollingCheckPayment(PayProWindow.PayTime).ContinueWith(UtilRun.LogTask);
			});
		}
	}
}
