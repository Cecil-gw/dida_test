using System.Windows.Input;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.QuickAdd;

public sealed class TokenElement : FormattedTextElement
{
	private readonly double _fontSize;

	private readonly TokenGenerator _generator;

	private readonly QuickAddToken _token;

	private string _color;

	public TokenElement(TextLine text, QuickAddToken token, TokenGenerator generator, double fontSize, int length, string color)
		: base(text, length)
	{
		_token = token;
		_color = color;
		_generator = generator;
		_fontSize = fontSize;
	}

	public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
	{
		return new TagTokenRun(this, base.TextRunProperties, _token, _fontSize, _generator.GetTokenBrush(_color, 100), _generator.GetTokenBrush(_color, 20));
	}

	protected override void OnQueryCursor(QueryCursorEventArgs e)
	{
		e.Cursor = Cursors.Hand;
		e.Handled = true;
	}

	protected override void OnMouseDown(MouseButtonEventArgs e)
	{
		if (e.ChangedButton == MouseButton.Left && !e.Handled)
		{
			e.Handled = true;
			_generator?.RemoveToken(_token);
		}
	}
}
