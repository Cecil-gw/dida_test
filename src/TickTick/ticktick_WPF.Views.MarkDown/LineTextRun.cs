using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.MarkDown;

public sealed class LineTextRun : FormattedTextRun
{
	private const double LegacyEditorWidthInset = 23.0;

	private const double MinimumLineWidth = 1.0;

	private readonly TextEditor _editor;

	private double _formattedWidth;

	private double _formattedHeight = 1.0;

	public LineTextRun(FormattedTextElement element, TextEditor editor, TextRunProperties properties)
		: base(element, properties)
	{
		_editor = editor;
	}

	internal static double CalculateVisibleWidth(double formattedLineWidth, double textViewActualWidth, double editorActualWidth)
	{
		if (IsUsableWidth(formattedLineWidth))
		{
			return Math.Max(1.0, formattedLineWidth);
		}
		double unpaddedHostWidth = GetUnpaddedHostWidth(textViewActualWidth, editorActualWidth);
		return Math.Max(1.0, unpaddedHostWidth - LineElement.ReservedRightPadding);
	}

	public override void Draw(DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways)
	{
		SolidColorBrush color = ThemeUtil.GetColor("BaseColorOpacity10", _editor);
		drawingContext.DrawRectangle(color, null, new Rect(0.0, origin.Y - 6.0, GetLastFormattedWidth(), 0.75));
		base.Draw(drawingContext, origin, rightToLeft, sideways);
	}

	public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
	{
		FormattedText formattedText = new FormattedText("", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(LocalSettings.Settings.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), 14.0, ThemeUtil.GetPrimaryColor(1.0));
		TextEmbeddedObjectMetrics textEmbeddedObjectMetrics = new TextEmbeddedObjectMetrics(formattedText.WidthIncludingTrailingWhitespace, formattedText.Height, formattedText.Baseline);
		_formattedWidth = CalculateVisibleWidth(remainingParagraphWidth, GetTextViewActualWidth(), GetEditorActualWidth());
		_formattedHeight = Math.Max(1.0, textEmbeddedObjectMetrics.Height);
		return new TextEmbeddedObjectMetrics(_formattedWidth, textEmbeddedObjectMetrics.Height, textEmbeddedObjectMetrics.Baseline);
	}

	public override Rect ComputeBoundingBox(bool rightToLeft, bool sideways)
	{
		return new Rect(0.0, 0.0, GetLastFormattedWidth(), _formattedHeight);
	}

	private double GetLastFormattedWidth()
	{
		if (IsUsableWidth(_formattedWidth))
		{
			return _formattedWidth;
		}
		return CalculateVisibleWidth(double.NaN, GetTextViewActualWidth(), GetEditorActualWidth());
	}

	private double GetTextViewActualWidth()
	{
		return (_editor?.TextArea?.TextView?.ActualWidth).GetValueOrDefault();
	}

	private double GetEditorActualWidth()
	{
		if (_editor == null)
		{
			return 0.0;
		}
		if (IsUsableWidth(_editor.ActualWidth))
		{
			return _editor.ActualWidth;
		}
		if (!double.IsNaN(_editor.Width))
		{
			return _editor.Width;
		}
		return 0.0;
	}

	private static double GetUnpaddedHostWidth(double textViewActualWidth, double editorActualWidth)
	{
		if (IsUsableWidth(textViewActualWidth))
		{
			return textViewActualWidth;
		}
		if (IsUsableWidth(editorActualWidth))
		{
			return Math.Max(1.0, editorActualWidth - 23.0);
		}
		return 1.0;
	}

	private static bool IsUsableWidth(double width)
	{
		if (!double.IsNaN(width) && !double.IsInfinity(width))
		{
			return width > 1.0;
		}
		return false;
	}
}
