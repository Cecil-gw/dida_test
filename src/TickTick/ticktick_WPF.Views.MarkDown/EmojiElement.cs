using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.MarkDown;

public sealed class EmojiElement : FormattedTextElement
{
	private readonly string _emoji;

	public EmojiElement(string emoji, TextLine text)
		: base(text, emoji.Length)
	{
		_emoji = emoji;
	}

	public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
	{
		return new EmojiTextRun(this, _emoji, base.TextRunProperties);
	}
}
