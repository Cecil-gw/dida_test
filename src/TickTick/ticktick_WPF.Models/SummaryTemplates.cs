using System.Collections.Generic;

namespace ticktick_WPF.Models;

public class SummaryTemplates : PreferenceBaseModel
{
	public List<SummaryTemplate> templates { get; set; }

	public override bool SetRemoteValue(PreferenceBaseModel model)
	{
		if (model is SummaryTemplates summaryTemplates)
		{
			if (summaryTemplates.mtime > base.mtime)
			{
				templates = summaryTemplates.templates;
				base.mtime = summaryTemplates.mtime;
			}
			else if (summaryTemplates.mtime < base.mtime)
			{
				return true;
			}
		}
		return false;
	}
}
