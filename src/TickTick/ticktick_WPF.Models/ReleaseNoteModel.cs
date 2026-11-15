using System;
using System.Collections.Generic;

namespace ticktick_WPF.Models;

public class ReleaseNoteModel
{
	public string version { get; set; }

	public List<NoteItemModel> data { get; set; }

	public int release_date { get; set; }

	public string release_type { get; set; } = "grey";

	public PackageSizeModel size { get; set; }

	public string[] forceVersions { get; set; } = Array.Empty<string>();

	public bool IsForced()
	{
		return release_type == "rollout";
	}

	public bool IsGreyForced()
	{
		if (!IsForced())
		{
			return release_type == "grey_force";
		}
		return true;
	}
}
