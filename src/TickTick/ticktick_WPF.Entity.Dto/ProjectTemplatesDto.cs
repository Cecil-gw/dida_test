using System.Collections.Generic;
using ticktick_WPF.Models;

namespace ticktick_WPF.Entity.Dto;

public class ProjectTemplatesDto
{
	public long Timestamp { get; set; }

	public List<ProjectTemplate> ProjectTemplates { get; set; }
}
