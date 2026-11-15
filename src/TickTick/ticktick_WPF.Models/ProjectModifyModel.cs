using System.Collections.Generic;

namespace ticktick_WPF.Models;

public class ProjectModifyModel
{
	public string id;

	public string action;

	public string when;

	public string title;

	public string description;

	public List<string> taskIds;

	public ProfileModel whoProfile;
}
