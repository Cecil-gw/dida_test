using System.Collections.Generic;

namespace ticktick_WPF.Models;

public class TemplatesModel
{
	public List<TaskTemplateModel> noteTemplates { get; set; } = new List<TaskTemplateModel>();

	public List<TaskTemplateModel> taskTemplates { get; set; } = new List<TaskTemplateModel>();
}
