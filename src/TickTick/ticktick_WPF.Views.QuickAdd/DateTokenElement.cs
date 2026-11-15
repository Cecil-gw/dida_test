using System.Windows.Input;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.QuickAdd;

public sealed class DateTokenElement : FormattedTextElement
{
	private readonly double _fontSize;

	private readonly DateTokenGenerator _generator;

	private readonly string _token;

	public DateTokenElement(TextLine text, string token, DateTokenGenerator generator, double fontSize, int length)
		: base(text, length)
	{
		_token = token;
		_generator = generator;
		_fontSize = fontSize;
	}

	public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
	{
		return new DateTokenTextRun(this, base.TextRunProperties, _token, _fontSize, _generator.GetColor());
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
			_generator?.AddIgnoreToken(_token);
		}
	}
}
