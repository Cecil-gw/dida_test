using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using CommonMark.Syntax;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Framework.Markdown;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.MarkDown.Colorizer;

public class HighlightColorizer : DocumentColorizingTransformer
{
	private readonly MarkDownEditor _editor;

	private Block _abstractSyntaxTree;

	private Theme _theme;

	private static readonly Regex CheckItemRegex = new Regex("^[ \\t]*- \\[ \\] .*");

	private static readonly Regex CheckedItemRegex = new Regex("^[ \\t]*- \\[[x|X]\\] .*");

	public HighlightColorizer(MarkDownEditor editor)
	{
		_editor = editor;
	}

	protected override void ColorizeLine(DocumentLine line)
	{
		Block abstractSyntaxTree = _abstractSyntaxTree;
		if (_theme == null)
		{
			_theme = new Theme(_editor);
		}
		Theme theme = _theme;
		int offset = line.Offset;
		int end = line.EndOffset;
		int leadingSpaces = base.CurrentContext.GetText(offset, end - offset).Text.TakeWhile(char.IsWhiteSpace).Count();
		MarkdownBlockNode block = null;
		bool flag = _editor != null && _editor.TryGetSemanticBlockAtOffset(offset, out block) && block != null;
		if (abstractSyntaxTree != null)
		{
			foreach (Block item in AbstractSyntaxTree.EnumerateSpanningBlocks(abstractSyntaxTree, offset, end))
			{
				if (ShouldSkipLegacyBlockHighlight(item, flag ? block : null))
				{
					continue;
				}
				double magnify = double.NaN;
				if (item.Tag == BlockTag.List || item.Tag == BlockTag.IndentedCode)
				{
					if (item.SourcePosition < 0 || item.SourcePosition + item.SourceLength > _editor.Text.Length)
					{
						return;
					}
					string text = _editor.Text.Substring(item.SourcePosition, item.SourceLength);
					string[] array = text.Split(new string[3]
					{
						Environment.NewLine,
						"\n",
						"\r"
					}, StringSplitOptions.None);
					int startIndex = 0;
					string[] array2 = array;
					foreach (string text2 in array2)
					{
						if (CheckedItemRegex.Match(text2).Success)
						{
							string text3 = text2.Substring(text2.IndexOf("]", StringComparison.Ordinal) + 2);
							int num = text.IndexOf(text2, startIndex, StringComparison.Ordinal);
							int num2 = text2.IndexOf("]", StringComparison.Ordinal) + 2;
							int num3 = num + num2;
							ApplyLinePart(_theme.HighlightCheckedItem, item.SourcePosition + num3, text3.Length, offset, end, leadingSpaces, magnify);
							if (num >= 0)
							{
								startIndex = num + text2.Length;
							}
						}
						if (CheckItemRegex.Match(text2).Success)
						{
							int num4 = text.IndexOf(text2, startIndex, StringComparison.Ordinal);
							if (num4 >= 0)
							{
								startIndex = num4 + text2.Length;
							}
						}
					}
				}
				if (AbstractSyntaxTree.BlockHighlighter.TryGetValue(item.Tag, out var value))
				{
					int sourceLength = ((item.Tag == BlockTag.ListItem) ? Math.Min(item.SourceLength, item.ListData.Padding) : item.SourceLength);
					if (item.Tag == BlockTag.BlockQuote && item.FirstChild?.InlineContent != null)
					{
						bool flag2 = true;
						foreach (Inline item2 in AbstractSyntaxTree.EnumerateInlines(item.FirstChild.InlineContent))
						{
							if (item2.Tag == InlineTag.SoftBreak && item2.NextSibling != null)
							{
								flag2 = item2.SourceLength + item2.SourcePosition != item2.NextSibling.SourcePosition;
							}
							if (item2.SourcePosition >= offset && item2.SourcePosition <= end)
							{
								if (item2.Tag == InlineTag.String && flag2)
								{
									ApplyLinePart(value(theme), item2.SourcePosition - 2, item2.SourceLength + 2, offset, end, 0, magnify);
								}
								else
								{
									HandleInline(item2, theme, ref magnify, offset, end, 0, out var _);
								}
							}
						}
						continue;
					}
					if (item.Heading.Level >= 1)
					{
						string text4 = base.CurrentContext.GetText(offset, end - offset).Text;
						if (text4.Replace("#", string.Empty).TrimEnd().Length > 0)
						{
							if (item.Heading.Level == 1)
							{
								if (!text4.StartsWith("#"))
								{
									AbstractSyntaxTree.BlockHighlighter.TryGetValue(BlockTag.Document, out value);
									magnify = 1.0;
								}
								else
								{
									magnify = theme.Header1Height;
								}
							}
							if (item.Heading.Level == 2)
							{
								if (!text4.StartsWith("##") && !text4.StartsWith("=="))
								{
									AbstractSyntaxTree.BlockHighlighter.TryGetValue(BlockTag.Document, out value);
									magnify = 1.0;
								}
								else
								{
									magnify = theme.Header2Height;
								}
							}
							if (item.Heading.Level == 1)
							{
								magnify = theme.Header1Height;
							}
							if (item.Heading.Level == 2)
							{
								magnify = theme.Header2Height;
							}
							if (item.Heading.Level == 3)
							{
								magnify = theme.Header3Height;
							}
							if (item.Heading.Level == 4)
							{
								magnify = theme.Header4Height;
							}
							if (item.Heading.Level == 5)
							{
								magnify = theme.Header5Height;
							}
							if (item.Heading.Level == 6)
							{
								magnify = theme.Header6Height;
							}
						}
						else
						{
							value = (Theme t) => t.HighlightNormal;
							magnify = 1.0;
						}
					}
					if (item.Tag != BlockTag.ListItem && item.Tag != BlockTag.List && value != null)
					{
						ApplyLinePart(value(theme), item.SourcePosition, sourceLength, offset, end, leadingSpaces, magnify);
					}
				}
				foreach (Inline item3 in AbstractSyntaxTree.EnumerateInlines(item.InlineContent).TakeWhile((Inline il) => il.SourcePosition < end))
				{
					HandleInline(item3, theme, ref magnify, offset, end, leadingSpaces, out value);
				}
			}
		}
		ApplySemanticBlockHighlights(theme, offset, end);
		ApplySemanticInlineHighlights(theme, offset, end, leadingSpaces);
	}

	private static bool ShouldSkipLegacyBlockHighlight(Block block, MarkdownBlockNode semanticBlock)
	{
		if (block == null)
		{
			return false;
		}
		if (block.Tag == BlockTag.FencedCode)
		{
			return true;
		}
		if (semanticBlock != null && IsLegacyBlockSyntaxConflictingWithSemanticBlock(block.Tag, semanticBlock))
		{
			return true;
		}
		return false;
	}

	private static bool IsLegacyBlockSyntaxConflictingWithSemanticBlock(BlockTag legacyTag, MarkdownBlockNode semanticBlock)
	{
		if (semanticBlock == null)
		{
			return false;
		}
		switch (legacyTag)
		{
		case BlockTag.AtxHeading:
		case BlockTag.SetextHeading:
			return semanticBlock.Kind != MarkdownBlockKind.Heading;
		case BlockTag.BlockQuote:
			return semanticBlock.Kind != MarkdownBlockKind.BlockQuote;
		case BlockTag.List:
		case BlockTag.ListItem:
			return semanticBlock.Kind != MarkdownBlockKind.List;
		default:
			return false;
		}
	}

	private void ApplySemanticBlockHighlights(Theme theme, int start, int end)
	{
		if (theme != null && end > start && _editor.TryGetSemanticBlockAtOffset(start, out var block) && block != null)
		{
			switch (block.Kind)
			{
			case MarkdownBlockKind.CodeBlock:
				ApplyLinePart(theme.HighlightBlockCode, start, end - start, start, end, 0, double.NaN);
				break;
			case MarkdownBlockKind.BlockQuote:
				ApplySemanticQuoteHighlight(theme, start, end);
				break;
			case MarkdownBlockKind.Heading:
				ApplySemanticHeadingHighlight(theme, block, start, end);
				break;
			case MarkdownBlockKind.List:
				ApplySemanticListHighlight(theme, block, start, end);
				break;
			case MarkdownBlockKind.HorizontalRule:
				break;
			}
		}
	}

	private void ApplySemanticHeadingHighlight(Theme theme, MarkdownBlockNode semanticBlock, int start, int end)
	{
		int level = Math.Max(1, Math.Min(6, semanticBlock.Level ?? 1));
		int num = (base.CurrentContext.GetText(start, end - start).Text ?? string.Empty).TakeWhile(char.IsWhiteSpace).Count();
		int num2 = Math.Min(end, start + num);
		if (num2 < end)
		{
			double headingMagnify = GetHeadingMagnify(theme, level);
			ApplyLinePart(theme.HighlightHeading, num2, end - num2, start, end, 0, headingMagnify);
		}
	}

	private static double GetHeadingMagnify(Theme theme, int level)
	{
		return level switch
		{
			1 => theme.Header1Height, 
			2 => theme.Header2Height, 
			3 => theme.Header3Height, 
			4 => theme.Header4Height, 
			5 => theme.Header5Height, 
			_ => theme.Header6Height, 
		};
	}

	private void ApplySemanticQuoteHighlight(Theme theme, int start, int end)
	{
		int num = (base.CurrentContext.GetText(start, end - start).Text ?? string.Empty).TakeWhile(char.IsWhiteSpace).Count();
		int num2 = Math.Min(end, start + num);
		if (num2 < end)
		{
			ApplyLinePart(theme.HighlightBlockQuote, num2, end - num2, start, end, 0, double.NaN);
		}
	}

	private void ApplySemanticListHighlight(Theme theme, MarkdownBlockNode semanticBlock, int start, int end)
	{
		if (semanticBlock != null && semanticBlock.ListKind == MarkdownListKind.Task && semanticBlock.Checked == true)
		{
			int num = (base.CurrentContext.GetText(start, end - start).Text ?? string.Empty).TakeWhile(char.IsWhiteSpace).Count();
			int num2 = Math.Min(end, start + num);
			if (num2 < end)
			{
				ApplyLinePart(theme.HighlightCheckedItem, num2, end - num2, start, end, 0, double.NaN);
			}
		}
	}

	private void ApplySemanticInlineHighlights(Theme theme, int start, int end, int leadingSpaces)
	{
		List<MarkdownInlineDisplaySegment> semanticMarkList = _editor.GetSemanticMarkList();
		if (semanticMarkList == null || semanticMarkList.Count == 0)
		{
			return;
		}
		foreach (MarkdownInlineDisplaySegment item in semanticMarkList)
		{
			if (item.SourcePosition >= end || item.SourcePosition + item.SourceLength <= start || item.ContentLength <= 0)
			{
				continue;
			}
			Highlight delimiterHighlight = theme.HighlightFlag;
			double magnify = double.NaN;
			if (TryGetSemanticInlineHighlight(theme, item.Kind, out var contentHighlight, ref delimiterHighlight, ref magnify))
			{
				if (IsInlineCodeInsideSemanticHeading(item))
				{
					magnify = double.NaN;
				}
				ApplySemanticInlinePart(item, delimiterHighlight, contentHighlight, start, end, leadingSpaces, magnify);
			}
		}
	}

	private bool TryGetSemanticInlineHighlight(Theme theme, MarkdownMarkKind kind, out Highlight contentHighlight, ref Highlight delimiterHighlight, ref double magnify)
	{
		contentHighlight = null;
		switch (kind)
		{
		case MarkdownMarkKind.Code:
			contentHighlight = theme.HighlightInlineCode;
			magnify = theme.CodeHeight;
			return true;
		case MarkdownMarkKind.Highlight:
			contentHighlight = theme.HighlightHighlight;
			delimiterHighlight = theme.HighlightFlagExtra;
			return true;
		case MarkdownMarkKind.Strong:
			contentHighlight = theme.HighlightStrongEmphasis;
			return true;
		case MarkdownMarkKind.Emphasis:
			contentHighlight = theme.HighlightEmphasis;
			return true;
		case MarkdownMarkKind.Underline:
			contentHighlight = theme.HighlightUnderLine;
			return true;
		case MarkdownMarkKind.Strikethrough:
			contentHighlight = theme.HighlightStrikethrough;
			return true;
		default:
			return false;
		}
	}

	private void ApplySemanticInlinePart(MarkdownInlineDisplaySegment mark, Highlight delimiterHighlight, Highlight contentHighlight, int start, int end, int leadingSpaces, double magnify)
	{
		ApplyLinePart(delimiterHighlight, mark.SourcePosition, mark.OpeningDelimiterLength, start, end, leadingSpaces, double.NaN);
		ApplyLinePart(contentHighlight, mark.ContentPosition, mark.ContentLength, start, end, leadingSpaces, magnify);
		ApplyLinePart(delimiterHighlight, mark.SourcePosition + mark.SourceLength - mark.ClosingDelimiterLength, mark.ClosingDelimiterLength, start, end, leadingSpaces, double.NaN);
	}

	private bool IsInlineCodeInsideSemanticHeading(MarkdownInlineDisplaySegment mark)
	{
		if (mark != null && mark.Kind == MarkdownMarkKind.Code && _editor != null && _editor.TryGetSemanticBlockAtOffset(mark.ContentPosition, out var block))
		{
			if (block == null)
			{
				return false;
			}
			return block.Kind == MarkdownBlockKind.Heading;
		}
		return false;
	}

	private void HandleInline(Inline inline, Theme theme, ref double magnify, int start, int end, int leadingSpaces, out Func<Theme, Highlight> highlighter)
	{
		AbstractSyntaxTree.InlineHighlighter.TryGetValue(inline.Tag, out highlighter);
		if (ShouldSkipLegacyInlineHighlight(inline))
		{
			return;
		}
		int sourcePosition = inline.SourcePosition;
		int sourceLength = inline.SourceLength;
		if (inline.Tag == InlineTag.Code)
		{
			magnify = theme.CodeHeight;
		}
		if (inline.Tag == InlineTag.Strong || inline.Tag == InlineTag.Emphasis || inline.Tag == InlineTag.Strikethrough || inline.Tag == InlineTag.UnderLine || inline.Tag == InlineTag.Code || inline.Tag == InlineTag.HighLight)
		{
			int flagLength = 2;
			if (inline.Tag == InlineTag.Emphasis || inline.Tag == InlineTag.UnderLine || inline.Tag == InlineTag.Link || inline.Tag == InlineTag.Placeholder || inline.Tag == InlineTag.Code)
			{
				flagLength = 1;
			}
			if (inline.Tag == InlineTag.HighLight)
			{
				HighlightFlagInlineExtra(flagLength, theme, sourcePosition, start, end, leadingSpaces, highlighter, sourceLength);
			}
			else
			{
				HighlightFlagInline(flagLength, theme, sourcePosition, start, end, leadingSpaces, highlighter, sourceLength);
			}
		}
		else if (inline.Tag == InlineTag.HighLightEx)
		{
			if (highlighter != null)
			{
				Highlight highlight = highlighter(theme);
				highlight.Foreground = ((inline.Color != null) ? (new BrushConverter().ConvertFromString(inline.Color) as SolidColorBrush) : ThemeUtil.GetColor("BaseColorOpacity100"));
				ApplyLinePart(highlight, sourcePosition, sourceLength, start, end, leadingSpaces, 1.0);
			}
		}
		else if (highlighter != null)
		{
			ApplyLinePart(highlighter(theme), sourcePosition, sourceLength, start, end, leadingSpaces, magnify);
		}
	}

	private static bool ShouldSkipLegacyInlineHighlight(Inline inline)
	{
		if (inline == null)
		{
			return false;
		}
		switch (inline.Tag)
		{
		case InlineTag.Code:
		case InlineTag.Emphasis:
		case InlineTag.Strong:
		case InlineTag.Strikethrough:
		case InlineTag.UnderLine:
			return true;
		default:
			return false;
		}
	}

	private void HighlightFlagInlineExtra2(int flagLength, Theme theme, int position, int start, int end, int leadingSpaces, Func<Theme, Highlight> highlighter, int length)
	{
		ApplyLinePart(theme.HighlightFlagExtra, position, flagLength, start, end, leadingSpaces, double.NaN);
		if (highlighter != null)
		{
			ApplyLinePart(highlighter(theme), position + flagLength, length - flagLength * 2, start, end, leadingSpaces, double.NaN);
		}
		ApplyLinePart(theme.HighlightFlagExtra, position + length - flagLength, flagLength, start, end, leadingSpaces, double.NaN);
	}

	private void HighlightFlagInlineExtra(int flagLength, Theme theme, int position, int start, int end, int leadingSpaces, Func<Theme, Highlight> highlighter, int length)
	{
		ApplyLinePart(theme.HighlightFlagExtra, position, flagLength, start, end, leadingSpaces, double.NaN);
		if (highlighter != null)
		{
			ApplyLinePart(highlighter(theme), position + flagLength, length - flagLength * 2, start, end, leadingSpaces, double.NaN);
		}
		ApplyLinePart(theme.HighlightFlagExtra, position + length - flagLength, flagLength, start, end, leadingSpaces, double.NaN);
	}

	private void HighlightFlagInline(int flagLength, Theme theme, int position, int start, int end, int leadingSpaces, Func<Theme, Highlight> highlighter, int length)
	{
		ApplyLinePart(theme.HighlightFlag, position, flagLength, start, end, leadingSpaces, double.NaN);
		if (highlighter != null)
		{
			ApplyLinePart(highlighter(theme), position + flagLength, length - flagLength * 2, start, end, leadingSpaces, double.NaN);
		}
		ApplyLinePart(theme.HighlightFlag, position + length - flagLength, flagLength, start, end, leadingSpaces, double.NaN);
	}

	private void ApplyLinePart(Highlight highlight, int sourceStart, int sourceLength, int lineStart, int lineEnd, int leadingSpaces, double magnify)
	{
		int num = Math.Max(sourceStart, lineStart + leadingSpaces);
		int num2 = Math.Min(lineEnd, sourceStart + sourceLength);
		if (num < num2)
		{
			ChangeLinePart(num, num2, delegate(VisualLineElement element)
			{
				ApplyHighlight(element, highlight, magnify);
			});
		}
	}

	private static void ApplyHighlight(VisualLineElement element, Highlight highlight, double magnify)
	{
		VisualLineElementTextRunProperties textRunProperties = element.TextRunProperties;
		SolidColorBrush foreground = highlight.Foreground;
		if (foreground != null)
		{
			textRunProperties.SetForegroundBrush(foreground);
		}
		if (!highlight.Name.Contains("Block") && !string.Equals(highlight.Name, "InlineCode", StringComparison.Ordinal))
		{
			SolidColorBrush background = highlight.Background;
			if (background != null)
			{
				textRunProperties.SetBackgroundBrush(background);
			}
		}
		if (!string.IsNullOrWhiteSpace(highlight.FontWeight) || !string.IsNullOrWhiteSpace(highlight.FontStyle))
		{
			Typeface typeface = textRunProperties.Typeface;
			FontWeight weight = ConvertFontWeight(highlight.FontWeight) ?? typeface.Weight;
			FontStyle style = ConvertFontStyle(highlight.FontStyle) ?? typeface.Style;
			Typeface typeface2 = new Typeface(typeface.FontFamily, style, weight, typeface.Stretch);
			textRunProperties.SetTypeface(typeface2);
		}
		if (!string.IsNullOrEmpty(highlight.FontType))
		{
			Typeface typeface3 = textRunProperties.Typeface;
			Typeface typeface4 = new Typeface(new FontFamily(highlight.FontType), typeface3.Style, typeface3.Weight, typeface3.Stretch);
			textRunProperties.SetTypeface(typeface4);
		}
		if (highlight.Underline)
		{
			textRunProperties.SetTextDecorations(TextDecorations.Underline);
		}
		if (highlight.Strikethrough)
		{
			textRunProperties.SetTextDecorations(highlight.TextDecorations ?? TextDecorations.Strikethrough);
		}
		if (!double.IsNaN(magnify))
		{
			textRunProperties.SetFontRenderingEmSize(textRunProperties.FontRenderingEmSize * magnify);
		}
	}

	private static FontWeight? ConvertFontWeight(string fontWeight)
	{
		if (string.IsNullOrWhiteSpace(fontWeight))
		{
			return null;
		}
		try
		{
			return (FontWeight)new FontWeightConverter().ConvertFromString(fontWeight);
		}
		catch (FormatException)
		{
			return null;
		}
		catch (NotSupportedException)
		{
			return null;
		}
	}

	private static FontStyle? ConvertFontStyle(string fontStyle)
	{
		if (string.IsNullOrWhiteSpace(fontStyle))
		{
			return null;
		}
		try
		{
			return (FontStyle)new FontStyleConverter().ConvertFromString(fontStyle);
		}
		catch (FormatException)
		{
			return null;
		}
		catch (NotSupportedException)
		{
			return null;
		}
	}

	public void UpdateAbstractSyntaxTree(Block ast)
	{
		_abstractSyntaxTree = ast;
	}

	public void OnThemeChanged(Theme theme)
	{
		_theme = theme;
	}
}
