namespace ticktick_WPF.Views.MarkDown;

public sealed class MarkdownTextStatisticsResult
{
	public string PlainText { get; set; } = string.Empty;

	public int WordCount { get; set; }

	public int CharacterCount { get; set; }
}
