using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TickTickModels;
using ticktick_WPF.Models;

namespace ticktick_WPF.Entity.Dto;

public class AIUserInputDto
{
	public class ChildId
	{
		public string title { get; set; }

		public string content { get; set; }

		[JsonConverter(typeof(UtcDateTimeConverter))]
		public DateTime? startDate { get; set; }

		[JsonConverter(typeof(UtcDateTimeConverter))]
		public DateTime? dueDate { get; set; }

		public List<string> tags { get; set; }
	}

	public string title { get; set; }

	public string content { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? startDate { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? dueDate { get; set; }

	public List<string> tags { get; set; }

	public string category { get; set; }

	public List<TaskDetailItemModel> items { get; set; }

	public List<ChildId> childIds { get; set; }
}
