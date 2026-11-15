using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Framework.Markdown;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.MarkDown;

internal sealed class CodeBlockSelectionLayer : UIElement
{
	private readonly MarkDownEditor _editor;

	private readonly TextView _textView;

	public CodeBlockSelectionLayer(MarkDownEditor editor, TextView textView)
	{
		_editor = editor;
		_textView = textView;
		base.Focusable = false;
		base.IsHitTestVisible = false;
	}

	protected override void OnRender(DrawingContext drawingContext)
	{
		base.OnRender(drawingContext);
		if (_editor?.EditBox?.Document == null || _editor.EditBox.SelectionLength <= 0 || _textView?.Document == null)
		{
			return;
		}
		Brush opaqueCodeBackgroundBrush = GetOpaqueCodeBackgroundBrush(ThemeUtil.GetColor("CodeBackground", _editor));
		Brush brush = _editor.EditBox.TextArea?.SelectionBrush ?? SystemColors.HighlightBrush;
		DrawContentSelectionSpacerCovers(drawingContext, opaqueCodeBackgroundBrush);
		foreach (MarkdownBlockNode semanticBlock in _editor.GetSemanticBlocks())
		{
			if (semanticBlock != null && semanticBlock.Kind == MarkdownBlockKind.CodeBlock && !_editor.IsSemanticBlockHiddenByHeadingFold(semanticBlock) && _editor.IsCodeBlockObjectSelected(semanticBlock) && TryGetCodeBlockRect(semanticBlock, out var rect))
			{
				drawingContext.DrawRoundedRectangle(opaqueCodeBackgroundBrush, null, rect, 2.0, 2.0);
				drawingContext.DrawRoundedRectangle(brush, null, rect, 2.0, 2.0);
			}
		}
	}

	private void DrawContentSelectionSpacerCovers(DrawingContext drawingContext, Brush codeBackground)
	{
		int num = Math.Max(0, Math.Min(_editor.EditBox.SelectionStart, _textView.Document.TextLength));
		int num2 = Math.Max(num, Math.Min(num + _editor.EditBox.SelectionLength, _textView.Document.TextLength));
		if (num2 <= num)
		{
			return;
		}
		foreach (MarkdownBlockNode semanticBlock in _editor.GetSemanticBlocks())
		{
			if (semanticBlock == null || semanticBlock.Kind != MarkdownBlockKind.CodeBlock || _editor.IsSemanticBlockHiddenByHeadingFold(semanticBlock) || _editor.IsCodeBlockAttachment(semanticBlock) || _editor.IsCodeBlockObjectSelected(semanticBlock))
			{
				continue;
			}
			int num3 = Math.Max(semanticBlock.StartLine + 2, 1);
			int num4 = Math.Min(semanticBlock.EndLine + 1, _textView.Document.LineCount);
			for (int i = num3; i <= num4; i++)
			{
				DocumentLine lineByNumber = _textView.Document.GetLineByNumber(i);
				if (CodeBlockLineClassifier.IsContentLine(_textView.Document, lineByNumber, semanticBlock))
				{
					int offset = lineByNumber.Offset;
					int num5 = Math.Min(offset + 1, _textView.Document.TextLength);
					if (num2 > offset && num < num5)
					{
						CoverSpacerSelection(drawingContext, codeBackground, lineByNumber, offset, num5);
					}
				}
			}
		}
	}

	private void CoverSpacerSelection(DrawingContext drawingContext, Brush codeBackground, DocumentLine line, int spacerStart, int spacerEnd)
	{
		if (spacerEnd <= spacerStart)
		{
			return;
		}
		VisualLine visualLine = _textView.GetVisualLine(line.LineNumber);
		if (visualLine == null)
		{
			return;
		}
		foreach (Rect item in BackgroundGeometryBuilder.GetRectsFromVisualSegment(_textView, visualLine, 0, 1))
		{
			if (!item.IsEmpty)
			{
				Rect rectangle = new Rect(item.Left, item.Top - 1.0, item.Width, item.Height + 2.0);
				drawingContext.DrawRectangle(codeBackground, null, rectangle);
			}
		}
	}

	private Brush GetOpaqueCodeBackgroundBrush(Brush brush)
	{
		if (brush is SolidColorBrush solidColorBrush)
		{
			return new SolidColorBrush(BlendOverEditorBackground(solidColorBrush.Color, solidColorBrush.Opacity));
		}
		Brush brush2 = brush?.CloneCurrentValue();
		if (brush2 == null)
		{
			return new SolidColorBrush(GetEditorBackgroundColor());
		}
		if (!(brush2.Opacity >= 1.0))
		{
			return new SolidColorBrush(GetEditorBackgroundColor());
		}
		return brush2;
	}

	private Color BlendOverEditorBackground(Color source, double brushOpacity)
	{
		Color editorBackgroundColor = GetEditorBackgroundColor();
		double alpha = Math.Max(0.0, Math.Min(1.0, (double)(int)source.A / 255.0 * brushOpacity));
		return Color.FromRgb(BlendColorChannel(source.R, editorBackgroundColor.R, alpha), BlendColorChannel(source.G, editorBackgroundColor.G, alpha), BlendColorChannel(source.B, editorBackgroundColor.B, alpha));
	}

	private Color GetEditorBackgroundColor()
	{
		if (TryGetOpaqueColor(_editor?.EditBox?.Background, out var color))
		{
			return color;
		}
		if (TryGetThemeResourceColor("WindowBackground", out var color2))
		{
			return color2;
		}
		if (TryGetThemeResourceColor("WindowBackgroundPrimary", out var color3))
		{
			return color3;
		}
		return SystemColors.WindowColor;
	}

	private bool TryGetThemeResourceColor(string key, out Color color)
	{
		color = Colors.Transparent;
		try
		{
			if (TryGetOpaqueColor(_editor?.TryFindResource(key) as Brush, out color))
			{
				return true;
			}
			if (TryGetOpaqueColor(Application.Current?.TryFindResource(key) as Brush, out color))
			{
				return true;
			}
		}
		catch
		{
			color = Colors.Transparent;
		}
		return false;
	}

	private static bool TryGetOpaqueColor(Brush brush, out Color color)
	{
		color = Colors.Transparent;
		if (!(brush is SolidColorBrush { Color: { A: not 0 }, Opacity: 0 } solidColorBrush))
		{
			return false;
		}
		color = ((solidColorBrush.Color.A == byte.MaxValue && solidColorBrush.Opacity >= 1.0) ? solidColorBrush.Color : BlendOverWhite(solidColorBrush.Color, solidColorBrush.Opacity));
		return true;
	}

	private static Color BlendOverWhite(Color source, double brushOpacity)
	{
		double alpha = Math.Max(0.0, Math.Min(1.0, (double)(int)source.A / 255.0 * brushOpacity));
		return Color.FromRgb(BlendColorChannel(source.R, byte.MaxValue, alpha), BlendColorChannel(source.G, byte.MaxValue, alpha), BlendColorChannel(source.B, byte.MaxValue, alpha));
	}

	private static byte BlendColorChannel(byte source, byte background, double alpha)
	{
		return (byte)Math.Round((double)(int)source * alpha + (double)(int)background * (1.0 - alpha));
	}

	private bool TryGetCodeBlockRect(MarkdownBlockNode block, out Rect rect)
	{
		rect = Rect.Empty;
		if (_editor.IsSemanticBlockHiddenByHeadingFold(block) || block.StartLine + 1 < 1 || block.EndLine + 1 > _textView.Document.LineCount)
		{
			return false;
		}
		DocumentLine lineByNumber = _textView.Document.GetLineByNumber(block.StartLine + 1);
		DocumentLine lineByNumber2 = _textView.Document.GetLineByNumber(block.EndLine + 1);
		if (_editor.IsCodeBlockAttachment(block))
		{
			return TryGetAttachmentCodeBlockRect(block, lineByNumber, out rect);
		}
		if (!TryGetLineTop(lineByNumber, out var top) || !TryGetLineBottom(lineByNumber2, out var bottom) || bottom <= top)
		{
			return false;
		}
		rect = new Rect(0.0, top, Math.Max(0.0, _textView.ActualWidth - 7.0), bottom - top);
		return true;
	}

	private bool TryGetAttachmentCodeBlockRect(MarkdownBlockNode block, DocumentLine line, out Rect rect)
	{
		rect = Rect.Empty;
		if (_editor.TryGetCodeBlockAttachmentBounds(block, out rect))
		{
			return true;
		}
		if (line == null || _textView.GetVisualLine(line.LineNumber) == null)
		{
			return false;
		}
		try
		{
			TextViewPosition position = new TextViewPosition(line.LineNumber, 1);
			double num = _textView.GetVisualPosition(position, VisualYPosition.LineTop).Y - _textView.ScrollOffset.Y;
			double num2 = _textView.GetVisualPosition(position, VisualYPosition.LineBottom).Y - _textView.ScrollOffset.Y;
			if (num2 <= num)
			{
				return false;
			}
			rect = new Rect(0.0, num, Math.Max(0.0, _textView.ActualWidth - 7.0), num2 - num);
			return true;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
		catch (ArgumentException)
		{
			return false;
		}
		catch (VisualLinesInvalidException)
		{
			return false;
		}
	}

	private bool TryGetLineTop(DocumentLine line, out double top)
	{
		top = 0.0;
		VisualLine visualLine = _textView.GetVisualLine(line.LineNumber);
		if (visualLine == null)
		{
			return false;
		}
		Rect rect = BackgroundGeometryBuilder.GetRectsFromVisualSegment(_textView, visualLine, line.Offset, line.Offset).FirstOrDefault();
		if (rect.IsEmpty)
		{
			return false;
		}
		top = rect.Top;
		return true;
	}

	private bool TryGetLineBottom(DocumentLine line, out double bottom)
	{
		bottom = 0.0;
		VisualLine visualLine = _textView.GetVisualLine(line.LineNumber);
		if (visualLine == null)
		{
			return false;
		}
		Rect rect = BackgroundGeometryBuilder.GetRectsFromVisualSegment(_textView, visualLine, line.Offset, line.Offset).FirstOrDefault();
		if (rect.IsEmpty)
		{
			return false;
		}
		bottom = rect.Bottom;
		return true;
	}
}
