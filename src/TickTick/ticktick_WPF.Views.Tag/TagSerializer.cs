using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ticktick_WPF.Views.Tag;

public static class TagSerializer
{
	public static string NormalizeName(string name)
	{
		return NormalizeLabel(name)?.ToLower();
	}

	public static string NormalizeLabel(string label)
	{
		return label?.Replace("#", string.Empty).Replace("＃", string.Empty).Trim();
	}

	public static List<string> NormalizeNames(IEnumerable<string> names)
	{
		if (names == null)
		{
			return new List<string>();
		}
		return (from name in names.Select(NormalizeName)
			where !string.IsNullOrEmpty(name)
			select name).Distinct(StringComparer.Ordinal).ToList();
	}

	public static List<string> NormalizeLabels(IEnumerable<string> labels)
	{
		if (labels == null)
		{
			return null;
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		List<string> list = new List<string>();
		foreach (string label in labels)
		{
			string text = NormalizeLabel(label);
			string text2 = NormalizeName(text);
			if (!string.IsNullOrEmpty(text2) && hashSet.Add(text2))
			{
				list.Add(text);
			}
		}
		return list;
	}

	public static List<string> ToTags(string content)
	{
		if (!string.IsNullOrEmpty(content))
		{
			try
			{
				List<string> list = ((JArray)JsonConvert.DeserializeObject(content)).ToObject<List<string>>();
				if (list != null && list.Count > 0)
				{
					return NormalizeNames(list);
				}
			}
			catch (Exception)
			{
				return new List<string>();
			}
		}
		return new List<string>();
	}

	public static string ToJsonContent(List<string> tags)
	{
		if (tags != null)
		{
			return JsonConvert.SerializeObject(NormalizeNames(tags));
		}
		return string.Empty;
	}
}
