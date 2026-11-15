using System;
using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Filter;
using ticktick_WPF.Views.Filter;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.ViewModels;

public class TagCardViewModel : FilterListDisplayModel<string>
{
	public List<string> Values
	{
		get
		{
			return ValueList;
		}
		set
		{
			ValueList = TagSerializer.NormalizeNames(value) ?? new List<string>();
			base.Content = ToCardText();
			OnPropertyChanged("Values");
		}
	}

	public TagCardViewModel()
	{
		base.ConditionName = "tag";
	}

	public override string ToCardText()
	{
		switch (ValueList.Count)
		{
		case 0:
			return Utils.GetString("Tags");
		case 1:
			if (base.LogicType == LogicType.Not)
			{
				return Utils.GetString("Exclude") + " " + GetTagValue(ValueList[0]);
			}
			return GetTagValue(ValueList[0]);
		default:
			if (base.LogicType == LogicType.Not)
			{
				return FilterListDisplayModel<string>.GetLogicString(base.LogicType) + " ( " + DisplayTagText(ValueList, ", ") + " )";
			}
			return DisplayTagText(ValueList, " " + FilterListDisplayModel<string>.GetLogicString(base.LogicType) + " ");
		}
	}

	public static string ToNormalDisplayText(List<string> tags, int displayNum = 3)
	{
		return ToTagText(tags, ", ", displayNum);
	}

	private static string ToTagText(List<string> tags, string divider, int displayNum = 3)
	{
		if (tags.Count == 0)
		{
			return Utils.GetString("AllTags");
		}
		if (tags.Count <= displayNum)
		{
			return DisplayTagText(tags, divider);
		}
		return string.Format(Utils.GetString("TagDisplayMessage"), DisplayTagText(tags.Take(displayNum).ToList(), divider), tags.Count - displayNum);
	}

	private static string DisplayTagText(IReadOnlyList<string> tags, string divider)
	{
		string text = string.Empty;
		if (tags != null && tags.Count > 0)
		{
			for (int i = 0; i < tags.Count; i++)
			{
				text = ((i >= tags.Count - 1) ? (text + GetTagValue(tags[i])) : (text + GetTagValue(tags[i]) + divider));
			}
		}
		return text;
	}

	private static string GetTagValue(string key)
	{
		if (key == "!tag")
		{
			return Utils.GetString("NoTagsTitle");
		}
		if (key == "*withtags")
		{
			return Utils.GetString("WithTags");
		}
		return (CacheManager.GetTags()?.FirstOrDefault((TagModel t) => string.Equals(t.name, key, StringComparison.CurrentCultureIgnoreCase)))?.GetDisplayName() ?? key;
	}

	public override ConditionEditDialog GenerateEditDialog()
	{
		return new TagEditDialog();
	}

	public override Rule<string> GetParseRule()
	{
		return new TagRule();
	}
}
