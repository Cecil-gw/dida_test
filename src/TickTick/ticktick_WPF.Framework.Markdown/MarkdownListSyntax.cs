namespace ticktick_WPF.Framework.Markdown;

internal static class MarkdownListSyntax
{
	public const int IndentSpacesPerLevel = 4;

	public static int GetIndentLevel(int leadingWhitespaceCount)
	{
		if (leadingWhitespaceCount <= 0)
		{
			return 0;
		}
		return leadingWhitespaceCount / 4;
	}
}
