using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ticktick_WPF.Views.MarkDown.Colorizer;

public static class CodeBlockLanguageCatalog
{
	private static readonly CodeBlockLanguageOption[] SupportedLanguages = new CodeBlockLanguageOption[18]
	{
		new CodeBlockLanguageOption("aspx", "ASPX", "asp.net"),
		new CodeBlockLanguageOption("boo", "Boo"),
		new CodeBlockLanguageOption("cpp", "C++", "c", "cpp", "c++"),
		new CodeBlockLanguageOption("csharp", "C#", "cs", "csharp", "c#"),
		new CodeBlockLanguageOption("css", "CSS"),
		new CodeBlockLanguageOption("html", "HTML", "htm"),
		new CodeBlockLanguageOption("java", "Java"),
		new CodeBlockLanguageOption("javascript", "JavaScript", "js", "jsx"),
		new CodeBlockLanguageOption("json", "JSON"),
		new CodeBlockLanguageOption("markdown", "Markdown", "md"),
		new CodeBlockLanguageOption("patch", "Patch", "diff"),
		new CodeBlockLanguageOption("php", "PHP"),
		new CodeBlockLanguageOption("powershell", "PowerShell", "ps", "ps1"),
		new CodeBlockLanguageOption("python", "Python", "py"),
		new CodeBlockLanguageOption("sql", "SQL", "mysql", "plsql", "postgresql", "sqlite", "tsql"),
		new CodeBlockLanguageOption("tex", "TeX", "latex"),
		new CodeBlockLanguageOption("vb", "VB", "vbnet", "visualbasic"),
		new CodeBlockLanguageOption("xml", "XML", "xaml")
	};

	private static readonly IReadOnlyList<string> DisplayNames = new ReadOnlyCollection<string>(SupportedLanguages.Select((CodeBlockLanguageOption language) => language.DisplayName).ToList());

	private static readonly Dictionary<string, CodeBlockLanguageOption> LanguagesByResourceKey = SupportedLanguages.ToDictionary((CodeBlockLanguageOption language) => language.ResourceKey, StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, string> ResourceKeysByAlias = BuildResourceKeysByAlias();

	public static IReadOnlyList<string> HighlightedLanguageDisplayNames => DisplayNames;

	public static bool TryGetHighlightingResourceKey(string language, out string resourceKey)
	{
		resourceKey = null;
		string text = NormalizeLanguageInput(language);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (ResourceKeysByAlias.TryGetValue(text, out resourceKey))
		{
			return true;
		}
		if (text.IndexOf("sql", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			resourceKey = "sql";
			return true;
		}
		return false;
	}

	public static int GetHighlightedLanguageDisplayNameMatchIndex(string input)
	{
		string normalizedInput = NormalizeLanguageInput(input);
		if (string.IsNullOrWhiteSpace(normalizedInput))
		{
			return -1;
		}
		if (TryGetHighlightingResourceKey(normalizedInput, out var resourceKey) && LanguagesByResourceKey.TryGetValue(resourceKey, out var value))
		{
			return Array.IndexOf(SupportedLanguages, value);
		}
		int num = Array.FindIndex(SupportedLanguages, (CodeBlockLanguageOption language) => IsLanguagePrefixMatch(language, normalizedInput));
		if (num >= 0)
		{
			return num;
		}
		return Array.FindIndex(SupportedLanguages, (CodeBlockLanguageOption language) => IsLanguageContainsMatch(language, normalizedInput));
	}

	public static bool TryGetDisplayName(string language, out string displayName)
	{
		displayName = null;
		if (!TryGetHighlightingResourceKey(language, out var resourceKey) || !LanguagesByResourceKey.TryGetValue(resourceKey, out var value))
		{
			return false;
		}
		displayName = value.DisplayName;
		return true;
	}

	public static string GetDisplayNameOrInput(string language)
	{
		if (string.IsNullOrWhiteSpace(language))
		{
			return "text";
		}
		if (!TryGetDisplayName(language, out var displayName))
		{
			return language.Trim();
		}
		return displayName;
	}

	public static string NormalizeLanguageInput(string language)
	{
		if (string.IsNullOrEmpty(language))
		{
			return string.Empty;
		}
		return new string(language.Where((char ch) => ch != '`' && ch != '\r' && ch != '\n').ToArray()).Trim();
	}

	private static Dictionary<string, string> BuildResourceKeysByAlias()
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		CodeBlockLanguageOption[] supportedLanguages = SupportedLanguages;
		foreach (CodeBlockLanguageOption codeBlockLanguageOption in supportedLanguages)
		{
			dictionary[codeBlockLanguageOption.ResourceKey] = codeBlockLanguageOption.ResourceKey;
			dictionary[codeBlockLanguageOption.DisplayName] = codeBlockLanguageOption.ResourceKey;
			foreach (string alias in codeBlockLanguageOption.Aliases)
			{
				dictionary[alias] = codeBlockLanguageOption.ResourceKey;
			}
		}
		return dictionary;
	}

	private static bool IsLanguagePrefixMatch(CodeBlockLanguageOption language, string input)
	{
		if (!language.ResourceKey.StartsWith(input, StringComparison.OrdinalIgnoreCase) && !language.DisplayName.StartsWith(input, StringComparison.OrdinalIgnoreCase))
		{
			return language.Aliases.Any((string alias) => alias.StartsWith(input, StringComparison.OrdinalIgnoreCase));
		}
		return true;
	}

	private static bool IsLanguageContainsMatch(CodeBlockLanguageOption language, string input)
	{
		if (language.ResourceKey.IndexOf(input, StringComparison.OrdinalIgnoreCase) < 0 && language.DisplayName.IndexOf(input, StringComparison.OrdinalIgnoreCase) < 0)
		{
			return language.Aliases.Any((string alias) => alias.IndexOf(input, StringComparison.OrdinalIgnoreCase) >= 0);
		}
		return true;
	}
}
