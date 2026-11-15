using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Framework.Markdown;

namespace ticktick_WPF.Views.MarkDown;

internal sealed class TitleLinkTextElement : VisualLineElement
{
	private string _source;

	private string _display;

	private int[] _displayToSource;

	private readonly LinkUrlTextElement _interaction;

	public override bool CanSplit => true;

	internal TitleLinkTextElement(string source, LinkUrlTextElement interaction)
		: base(MarkdownSemanticConverter.DecodeEscapedSyntaxForDisplay(source).Length, source.Length)
	{
		_interaction = interaction;
		SetSource(source);
	}

	private void SetSource(string source)
	{
		_source = source;
		StringBuilder stringBuilder = new StringBuilder();
		List<int> list = new List<int> { 0 };
		for (int i = 0; i < source.Length; i++)
		{
			if (source[i] == '\\' && i + 1 < source.Length && MarkdownSemanticConverter.IsEscapableLiteralChar(source[i + 1]))
			{
				i++;
			}
			stringBuilder.Append(source[i]);
			list.Add(i + 1);
		}
		_display = stringBuilder.ToString();
		_displayToSource = list.ToArray();
	}

	public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
	{
		_interaction?.ApplyLinkStyle(base.TextRunProperties);
		int num = startVisualColumn - base.VisualColumn;
		return new TextCharacters(_display, num, _display.Length - num, base.TextRunProperties);
	}

	public override int GetRelativeOffset(int visualColumn)
	{
		int num = Math.Max(0, Math.Min(base.VisualLength, visualColumn - base.VisualColumn));
		return base.RelativeTextOffset + _displayToSource[num];
	}

	public override int GetVisualColumn(int relativeTextOffset)
	{
		int value = Math.Max(0, Math.Min(base.DocumentLength, relativeTextOffset - base.RelativeTextOffset));
		int num = Array.BinarySearch(_displayToSource, value);
		return base.VisualColumn + ((num >= 0) ? num : (~num - 1));
	}

	public override int GetNextCaretPosition(int visualColumn, LogicalDirection direction, CaretPositioningMode mode)
	{
		int nextCaretPosition = TextUtilities.GetNextCaretPosition(new StringTextSource(_display), visualColumn - base.VisualColumn, direction, mode);
		if (nextCaretPosition >= 0)
		{
			return base.VisualColumn + nextCaretPosition;
		}
		return -1;
	}

	public override bool IsWhitespace(int visualColumn)
	{
		return char.IsWhiteSpace(_display[visualColumn - base.VisualColumn]);
	}

	public override TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(int visualColumnLimit, ITextRunConstructionContext context)
	{
		int num = Math.Min(_display.Length, visualColumnLimit - base.VisualColumn);
		return new TextSpan<CultureSpecificCharacterBufferRange>(num, new CultureSpecificCharacterBufferRange(base.TextRunProperties.CultureInfo, new CharacterBufferRange(_display, 0, num)));
	}

	public override void Split(int splitVisualColumn, IList<VisualLineElement> elements, int elementIndex)
	{
		int num = GetRelativeOffset(splitVisualColumn) - base.RelativeTextOffset;
		TitleLinkTextElement titleLinkTextElement = new TitleLinkTextElement(_source.Substring(num), _interaction);
		SplitHelper(this, titleLinkTextElement, splitVisualColumn, base.RelativeTextOffset + num);
		SetSource(_source.Substring(0, num));
		elements.Insert(elementIndex + 1, titleLinkTextElement);
	}

	protected override void OnMouseLeave(MouseEventArgs e)
	{
		_interaction?.HandleTitleMouseLeave(e);
	}

	protected override void OnQueryCursor(QueryCursorEventArgs e)
	{
		_interaction?.HandleTitleQueryCursor(e);
	}

	protected override void OnMouseUp(MouseButtonEventArgs e)
	{
		_interaction?.HandleTitleMouseUp(e);
	}
}
