using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TickTickModels;

namespace ticktick_WPF.Models;

public class GuideProjectTask
{
	public string id { get; set; }

	public string title { get; set; }

	public string columnId { get; set; }

	public string content { get; set; }

	public string desc { get; set; }

	public string parentId { get; set; }

	public string repeatFlag { get; set; }

	public string repeatFrom { get; set; }

	public string timeZone { get; set; }

	public string label { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? startDate { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? dueDate { get; set; }

	public bool? floating { get; set; }

	public bool? allDay { get; set; }

	public int priority { get; set; }

	public List<string> tags { get; set; }

	public List<string> reminders { get; set; }

	public List<GuideProjectTaskItem> items { get; set; }

	public List<GuideProjectTaskAction> actions { get; set; }

	public List<GuideProjectTaskResource> resources { get; set; }
}
