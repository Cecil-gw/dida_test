using System;

namespace ticktick_WPF.UriScheme.Dto;

internal class AddTaskUriDto
{
	public string Title { get; set; }

	public DateTime? StartDate { get; set; }

	public DateTime? EndDate { get; set; }

	public bool? AllDay { get; set; }

	public int Priority { get; set; }

	public string Content { get; set; }

	public string Subtasks { get; set; }

	public string Tags { get; set; }

	public string List { get; set; }
}
