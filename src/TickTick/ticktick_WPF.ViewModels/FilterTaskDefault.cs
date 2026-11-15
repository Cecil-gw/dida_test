using System;
using System.Collections.Generic;
using ticktick_WPF.Models;

namespace ticktick_WPF.ViewModels;

public class FilterTaskDefault
{
	public ProjectModel ProjectModel { get; set; }

	public int? Priority { get; set; }

	public List<string> DefaultTags { get; set; } = new List<string>();

	public string DefaultAssignee { get; } = "-1";

	public DateTime? DefaultDate { get; set; }

	public bool IsNote { get; set; }

	public bool OnlyNote { get; set; }

	public bool HasProjectSelected { get; set; }
}
