using System;
using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.Models;

namespace ticktick_WPF.Util;

public class ChecklistUtils
{
	private const string LineSeperator = "\r\n";

	private const string DescriptionSeperator = "\r\n  \r\n";

	public static ChecklistExtra Text2Items(string content)
	{
		content = content?.Replace("\r\n", "\n").Replace("\n", "\r\n") ?? string.Empty;
		ChecklistExtra checklistExtra = new ChecklistExtra
		{
			Description = string.Empty,
			ChecklistItems = new List<string>()
		};
		string[] array = content.Split(new string[1] { "\r\n  \r\n" }, StringSplitOptions.None);
		int num = 0;
		if (array.Length >= 2)
		{
			checklistExtra.Description = array[0];
			num = 1;
		}
		for (int i = num; i < array.Length; i++)
		{
			string[] array2 = array[i].Split(new string[1] { "\r\n" }, StringSplitOptions.None);
			if (array2.Length == 0)
			{
				continue;
			}
			string[] array3 = array2;
			foreach (string text in array3)
			{
				if (!string.IsNullOrEmpty(text))
				{
					checklistExtra.ChecklistItems.Add(text);
				}
			}
		}
		if (checklistExtra.ChecklistItems.Count == 0)
		{
			checklistExtra.ChecklistItems.Add(string.Empty);
		}
		return checklistExtra;
	}

	public static List<TaskDetailItemModel> BuildChecklist(int taskId, string taskSid, List<string> titles)
	{
		List<TaskDetailItemModel> list = new List<TaskDetailItemModel>();
		int num = 0;
		foreach (string title in titles)
		{
			TaskDetailItemModel taskDetailItemModel = new TaskDetailItemModel
			{
				id = Utils.GetGuid(),
				title = title,
				status = 0,
				TaskId = taskId,
				TaskServerId = taskSid,
				sortOrder = num
			};
			if (title.Trim().StartsWith("- [ ] "))
			{
				taskDetailItemModel.title = taskDetailItemModel.title.Trim().Substring(6);
			}
			if (title.Trim().StartsWith("- [x] "))
			{
				taskDetailItemModel.title = taskDetailItemModel.title.Trim().Substring(6);
				taskDetailItemModel.status = 2;
			}
			num += 2048;
			list.Add(taskDetailItemModel);
		}
		return list;
	}

	public static string Items2Text(string description, List<string> checklist)
	{
		string empty = string.Empty;
		if (!string.IsNullOrEmpty(description))
		{
			if (checklist == null || checklist.Count == 0)
			{
				return description + "\r\n  \r\n";
			}
			return AppendContent(description, checklist, empty);
		}
		return AppendContent(description, checklist, empty);
	}

	private static string AppendContent(string description, IEnumerable<string> checklist, string content)
	{
		if (!string.IsNullOrEmpty(description))
		{
			content += description;
			if (!content.EndsWith("\r\n  \r\n"))
			{
				content += "\r\n  \r\n";
			}
		}
		content = checklist.Aggregate(content, (string current, string item) => current + item + "\r\n");
		if (content.EndsWith("\r\n"))
		{
			content = content.Substring(0, content.Length - "\r\n".Length);
		}
		return content;
	}
}
