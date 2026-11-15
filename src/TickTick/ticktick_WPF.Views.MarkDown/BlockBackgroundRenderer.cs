using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using CommonMark.Syntax;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using TickTickUtils.Lang;
using ticktick_WPF.Framework.Markdown;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.MarkDown;

public class BlockBackgroundRenderer : IBackgroundRenderer
{
	internal const double CodeBlockHeaderHeight = 24.0;

	internal const double CodeBlockBottomPadding = 12.0;

	private MarkDownEditor _editor;

	private Block _abstractSyntaxTree;

	public KnownLayer Layer => KnownLayer.Background;

	public BlockBackgroundRenderer(MarkDownEditor editor)
	{
		_editor = editor;
	}

	public void Draw(TextView textView, DrawingContext drawingContext)
	{
		Block abstractSyntaxTree = _abstractSyntaxTree;
		SolidColorBrush codeBrush = null;
		SolidColorBrush codeBrush2 = null;
		SolidColorBrush codeDividerBrush = null;
		SolidColorBrush highLightBrush = null;
		int caret = _editor.EditBox.CaretOffset;
		if (_editor.EditBox.SelectionLength > 0)
		{
			caret = ((_editor.EditBox.SelectionStart == _editor.EditBox.CaretOffset) ? (_editor.EditBox.SelectionStart + _editor.EditBox.SelectionLength) : _editor.EditBox.SelectionStart);
		}
		DrawSemanticBlockBackgrounds(textView, drawingContext, ref codeBrush, ref codeDividerBrush);
		DrawSemanticInlineBackgrounds(textView, drawingContext, caret, ref codeBrush2, ref highLightBrush);
		if (abstractSyntaxTree == null)
		{
			return;
		}
		foreach (Block item in AbstractSyntaxTree.EnumerateBlocks(abstractSyntaxTree.FirstChild).ToList())
		{
			if (item.SourcePosition + item.SourceLength > _editor.Text.Length || item.Tag == BlockTag.BlockQuote || item.Tag == BlockTag.FencedCode || (item.Tag != BlockTag.Paragraph && item.Tag != BlockTag.SetextHeading && item.Tag != BlockTag.AtxHeading && item.Tag != BlockTag.IndentedCode))
			{
				continue;
			}
			foreach (Inline item2 in AbstractSyntaxTree.EnumerateInlines(item.InlineContent))
			{
				if (!IsLegacyInlineBackgroundHandledBySemantic(item2) && item2.Tag != InlineTag.Code && item2.Tag != InlineTag.HighLight)
				{
					_ = item2.Tag;
					_ = 13;
				}
			}
		}
	}

	private bool IsLegacyInlineBackgroundHandledBySemantic(Inline inline)
	{
		if (inline == null)
		{
			return false;
		}
		MarkdownMarkKind? kind = null;
		switch (inline.Tag)
		{
		case InlineTag.Code:
			kind = MarkdownMarkKind.Code;
			break;
		case InlineTag.HighLight:
		case InlineTag.HighLightEx:
			kind = MarkdownMarkKind.Highlight;
			break;
		}
		if (!kind.HasValue)
		{
			return false;
		}
		List<MarkdownInlineDisplaySegment> semanticMarkList = _editor.GetSemanticMarkList();
		if (semanticMarkList == null || semanticMarkList.Count == 0)
		{
			return false;
		}
		return semanticMarkList.Any((MarkdownInlineDisplaySegment mark) => mark.Kind == kind.Value && mark.SourcePosition == inline.SourcePosition && mark.SourceLength == inline.SourceLength);
	}

	private void DrawSemanticInlineBackgrounds(TextView textView, DrawingContext drawingContext, int caret, ref SolidColorBrush codeBrush, ref SolidColorBrush highLightBrush)
	{
		List<MarkdownInlineDisplaySegment> semanticMarkList = _editor.GetSemanticMarkList();
		if (semanticMarkList == null || semanticMarkList.Count == 0 || textView.Document == null)
		{
			return;
		}
		foreach (MarkdownInlineDisplaySegment item in semanticMarkList)
		{
			if (item.SourcePosition < 0 || item.SourcePosition + item.SourceLength > textView.Document.TextLength || item.ContentLength <= 0)
			{
				continue;
			}
			if (item.Kind == MarkdownMarkKind.Code)
			{
				if (_editor.IsDocumentOffsetHiddenByHeadingFold(item.SourcePosition))
				{
					continue;
				}
				TextSegment segment = new TextSegment
				{
					StartOffset = item.SourcePosition,
					Length = item.SourceLength
				};
				foreach (Rect item2 in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
				{
					if (!(item2.Width <= 3.0))
					{
						codeBrush = codeBrush ?? ThemeUtil.GetColor("InlineCodeBackground", _editor);
						drawingContext.DrawRoundedRectangle(codeBrush, null, item2, 2.0, 2.0);
					}
				}
				continue;
			}
			if (item.Kind != MarkdownMarkKind.Highlight)
			{
				continue;
			}
			int num;
			int num2;
			if (caret >= item.SourcePosition)
			{
				num = ((caret <= item.SourcePosition + item.SourceLength) ? 1 : 0);
				if (num != 0)
				{
					num2 = item.SourcePosition;
					goto IL_0160;
				}
			}
			else
			{
				num = 0;
			}
			num2 = item.ContentPosition;
			goto IL_0160;
			IL_0160:
			int startOffset = num2;
			int length = ((num != 0) ? item.SourceLength : item.ContentLength);
			DrawSemanticHighlightBackgroundByLine(textView, drawingContext, item, startOffset, length, ref highLightBrush);
		}
	}

	private void DrawSemanticHighlightBackgroundByLine(TextView textView, DrawingContext drawingContext, MarkdownInlineDisplaySegment mark, int startOffset, int length, ref SolidColorBrush highLightBrush)
	{
		TextDocument document = textView.Document;
		if (document == null || length <= 0)
		{
			return;
		}
		int num = Math.Max(0, Math.Min(startOffset, document.TextLength));
		int num2 = Math.Max(num, Math.Min(startOffset + length, document.TextLength));
		if (num2 <= num)
		{
			return;
		}
		SolidColorBrush semanticHighlightBrush = GetSemanticHighlightBrush(mark, ref highLightBrush);
		if (semanticHighlightBrush == null)
		{
			return;
		}
		DocumentLine lineByOffset = document.GetLineByOffset(num);
		DocumentLine lineByOffset2 = document.GetLineByOffset(Math.Max(num, num2 - 1));
		if (lineByOffset == null || lineByOffset2 == null)
		{
			return;
		}
		DocumentLine documentLine = lineByOffset;
		while (documentLine != null && documentLine.LineNumber <= lineByOffset2.LineNumber)
		{
			if (!_editor.IsSemanticLineHiddenByHeadingFold(documentLine.LineNumber - 1))
			{
				int num3 = Math.Max(num, documentLine.Offset);
				int num4 = Math.Min(num2, documentLine.EndOffset);
				if (num4 > num3)
				{
					TextSegment segment = new TextSegment
					{
						StartOffset = num3,
						Length = num4 - num3
					};
					foreach (Rect item in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
					{
						if (!(item.Width <= 3.0))
						{
							drawingContext.DrawRoundedRectangle(semanticHighlightBrush, null, item, 2.0, 2.0);
						}
					}
				}
			}
			documentLine = documentLine.NextLine;
		}
	}

	private SolidColorBrush GetSemanticHighlightBrush(MarkdownInlineDisplaySegment mark, ref SolidColorBrush defaultHighlightBrush)
	{
		if (!string.IsNullOrWhiteSpace(mark?.Color))
		{
			try
			{
				SolidColorBrush editorHighlightBrush = HighlightColorMenuState.GetEditorHighlightBrush(mark.Color);
				if (editorHighlightBrush != null)
				{
					return editorHighlightBrush;
				}
			}
			catch (Exception ex)
			{
				UtilLog.Error(ex);
			}
		}
		defaultHighlightBrush = defaultHighlightBrush ?? HighlightColorMenuState.GetEditorHighlightBrush("#FFEC6C");
		return defaultHighlightBrush;
	}

	private void DrawSemanticBlockBackgrounds(TextView textView, DrawingContext drawingContext, ref SolidColorBrush codeBrush, ref SolidColorBrush codeDividerBrush)
	{
		IList<MarkdownBlockNode> semanticBlocks = _editor.GetSemanticBlocks();
		if (semanticBlocks == null || semanticBlocks.Count == 0 || textView.Document == null)
		{
			return;
		}
		for (int i = 0; i < semanticBlocks.Count; i++)
		{
			MarkdownBlockNode markdownBlockNode = semanticBlocks[i];
			if (_editor.IsSemanticBlockHiddenByHeadingFold(markdownBlockNode))
			{
				continue;
			}
			if (markdownBlockNode.Kind == MarkdownBlockKind.BlockQuote)
			{
				int endLine = markdownBlockNode.EndLine;
				while (i + 1 < semanticBlocks.Count && semanticBlocks[i + 1].Kind == MarkdownBlockKind.BlockQuote && semanticBlocks[i + 1].StartLine == endLine + 1)
				{
					i++;
					endLine = semanticBlocks[i].EndLine;
				}
				DrawQuoteBackground(textView, drawingContext, markdownBlockNode.StartLine, endLine);
			}
			else if (markdownBlockNode.Kind == MarkdownBlockKind.CodeBlock)
			{
				DrawCodeBlockBackground(textView, drawingContext, markdownBlockNode, ref codeBrush, ref codeDividerBrush);
			}
		}
	}

	private void DrawQuoteBackground(TextView textView, DrawingContext drawingContext, int startLine, int endLine)
	{
		double? num = null;
		double num2 = 0.0;
		for (int i = startLine + 1; i <= endLine + 1; i++)
		{
			if (i < 1 || i > textView.Document.LineCount)
			{
				continue;
			}
			VisualLine visualLine = textView.GetVisualLine(i);
			if (visualLine != null && visualLine.Elements.Count >= 1 && visualLine.Elements[0] is QuoteItemElement)
			{
				Rect rect = BackgroundGeometryBuilder.GetRectsFromVisualSegment(textView, visualLine, 0, 1000).FirstOrDefault();
				if (!(rect == default(Rect)))
				{
					if (!num.HasValue)
					{
						num = rect.Top;
					}
					num2 += visualLine.Height;
				}
			}
			else if (num2 > 0.0)
			{
				drawingContext.DrawRectangle(ThemeUtil.GetColor("MarkDownQuoteColor", _editor), null, new Rect(1.0, num.GetValueOrDefault(), 2.0, num2 - 6.0));
				num2 = 0.0;
				num = null;
			}
		}
		if (num2 > 0.0)
		{
			drawingContext.DrawRectangle(ThemeUtil.GetColor("MarkDownQuoteColor", _editor), null, new Rect(1.0, num.GetValueOrDefault(), 2.0, num2 - 6.0));
		}
	}

	private void DrawCodeBlockBackground(TextView textView, DrawingContext drawingContext, MarkdownBlockNode block, ref SolidColorBrush codeBrush, ref SolidColorBrush codeDividerBrush)
	{
		if (block.StartLine + 1 < 1 || block.EndLine + 1 > textView.Document.LineCount)
		{
			return;
		}
		DocumentLine lineByNumber = textView.Document.GetLineByNumber(block.StartLine + 1);
		VisualLine visualLine = textView.GetVisualLine(lineByNumber.LineNumber);
		if (visualLine == null)
		{
			return;
		}
		Rect rect = BackgroundGeometryBuilder.GetRectsFromVisualSegment(textView, visualLine, lineByNumber.Offset, lineByNumber.Offset).FirstOrDefault();
		DocumentLine lineByNumber2 = textView.Document.GetLineByNumber(block.EndLine + 1);
		VisualLine visualLine2 = textView.GetVisualLine(lineByNumber2.LineNumber);
		if (visualLine2 == null)
		{
			return;
		}
		Rect rect2 = BackgroundGeometryBuilder.GetRectsFromVisualSegment(textView, visualLine2, lineByNumber2.Offset, lineByNumber2.Offset).FirstOrDefault();
		RectInfo rectInfo = new RectInfo
		{
			Top = rect.Top,
			Bottom = rect2.Top + rect2.Height
		};
		if (rectInfo.Top >= 0.0 && rectInfo.Bottom >= 0.0 && rectInfo.Bottom > rectInfo.Top)
		{
			codeBrush = codeBrush ?? ThemeUtil.GetColor("CodeBackground", _editor);
			codeDividerBrush = codeDividerBrush ?? ThemeUtil.GetColor("MarkDownCodeDivider", _editor);
			double width = Math.Max(0.0, textView.ActualWidth - 7.0);
			double num = Math.Max(0.0, rectInfo.Top);
			double num2 = rectInfo.Bottom;
			if (!_editor.IsCodeBlockAttachment(block) && !IsClosingCodeFenceLine(textView.Document, block, lineByNumber2))
			{
				num2 += 12.0;
			}
			drawingContext.DrawRoundedRectangle(codeBrush, new Pen(codeDividerBrush, 0.0), new Rect(0.0, num, width, num2 - num), 6.0, 6.0);
		}
	}

	private static bool IsClosingCodeFenceLine(TextDocument document, MarkdownBlockNode block, DocumentLine line)
	{
		if (document != null && block != null && block.Kind == MarkdownBlockKind.CodeBlock && line != null && block.EndLine > block.StartLine && line.LineNumber - 1 == block.EndLine)
		{
			return CodeBlockLineClassifier.IsFenceLine(document, line);
		}
		return false;
	}

	public void UpdateAbstractSyntaxTree(Block ast)
	{
		_abstractSyntaxTree = ast;
	}
}
