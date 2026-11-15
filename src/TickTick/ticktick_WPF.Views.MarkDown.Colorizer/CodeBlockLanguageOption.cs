using System.Collections.Generic;

namespace ticktick_WPF.Views.MarkDown.Colorizer;

public sealed class CodeBlockLanguageOption
{
	public string ResourceKey { get; private set; }

	public string DisplayName { get; private set; }

	internal IReadOnlyList<string> Aliases { get; private set; }

	internal CodeBlockLanguageOption(string resourceKey, string displayName, params string[] aliases)
	{
		ResourceKey = resourceKey;
		DisplayName = displayName;
		Aliases = aliases ?? new string[0];
	}
}
