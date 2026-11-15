using System;

namespace ticktick_WPF.Models;

public class UpdateModel
{
	public double versionNum { get; set; }

	public string downLoadUri { get; set; }

	public string fileName { get; set; }

	public DateTime publishDate { get; set; }

	public bool forceUpdate { get; set; }

	public bool greyForced { get; set; }

	public string[] forceVersions { get; set; }

	public ReleaseNoteModel releaseNote { get; set; }
}
