using System.Collections.Generic;

namespace ticktick_WPF.Models;

public class ReleaseNoteDetailModel
{
	public string VersionCode { get; set; }

	public List<FeatureModel> Features { get; set; }

	public EpicUpdateModel Epic { get; set; }
}
