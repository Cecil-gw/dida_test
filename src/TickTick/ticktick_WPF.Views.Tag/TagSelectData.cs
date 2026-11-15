using System.Collections.Generic;
using System.Linq;

namespace ticktick_WPF.Views.Tag;

public class TagSelectData
{
	public List<string> OmniSelectTags = new List<string>();

	public List<string> PartSelectTags = new List<string>();

	public TagSelectData Clone()
	{
		TagSelectData tagSelectData = new TagSelectData();
		tagSelectData.OmniSelectTags.AddRange(OmniSelectTags);
		tagSelectData.PartSelectTags.AddRange(PartSelectTags);
		return tagSelectData;
	}

	public bool IsEqual(TagSelectData tags)
	{
		if (tags.OmniSelectTags.Count != OmniSelectTags.Count || tags.PartSelectTags.Count != PartSelectTags.Count)
		{
			return false;
		}
		if (tags.OmniSelectTags.Any((string t) => !OmniSelectTags.Contains(t)) || tags.PartSelectTags.Any((string t) => !PartSelectTags.Contains(t)))
		{
			return false;
		}
		return true;
	}
}
