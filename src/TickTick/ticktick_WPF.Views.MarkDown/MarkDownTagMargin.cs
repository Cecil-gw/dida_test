using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Framework.Markdown;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.MarkDown;

public class MarkDownTagMargin : AbstractMargin
{
	private const double ParagraphIconSize = 14.0;

	private const double TitleArrowIconSize = 12.0;

	private const double HoverAddIconSize = 12.0;

	private const double NonAlignedTitleArrowRightOffset = 2.0;

	private const string TitleArrowIconKey = "Ic.MarkdownEditor.TitleArrow";

	private const string HoverAddIconKey = "Ic.ProjectList.Add";

	private readonly TextArea _area;

	private readonly double _width;

	private MarkDownEditor _editor;

	private string _currentAction = string.Empty;

	private int _forceShowLine = -1;

	private readonly bool _inSticky;

	private static SolidColorBrush darkIconBrush { get; set; } = new SolidColorBrush(Colors.White)
	{
		Opacity = 0.6
	};

	private static SolidColorBrush lightIconBrush { get; set; } = new SolidColorBrush(Color.FromRgb(25, 25, 25))
	{
		Opacity = 0.6
	};

	public MarkDownTagMargin(TextArea area, MarkDownEditor editor, double width = 12.0, bool inSticky = false)
	{
		_area = area;
		_editor = editor;
		_width = width;
		_inSticky = inSticky;
		if (_inSticky)
		{
			base.Width = width;
			base.HorizontalAlignment = HorizontalAlignment.Left;
		}
		_editor.MouseMove += OnMouseMove;
		_editor.MouseLeave += OnMouseLeave;
		base.Unloaded += OnUnloaded;
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		if (_editor != null)
		{
			_editor.MouseMove -= OnMouseMove;
			_editor.MouseLeave -= OnMouseLeave;
		}
	}

	private void OnMouseLeave(object sender, MouseEventArgs e)
	{
		if (_forceShowLine != -1)
		{
			_forceShowLine = -1;
			InvalidateVisual();
		}
	}

	private void OnMouseMove(object sender, MouseEventArgs e)
	{
		UpdateHoverLineFromPoint(e.GetPosition(_editor));
	}

	private void UpdateHoverLineFromPoint(Point editorPoint, bool forceInvalidate = false)
	{
		if (!_editor.StyleEnabled)
		{
			if (forceInvalidate || _forceShowLine != -1)
			{
				_forceShowLine = -1;
				InvalidateVisual();
			}
			return;
		}
		int num = -1;
		if (TryGetLineIndexFromPoint(editorPoint, out var lineIndex) && ((_inSticky && CanShowAddIconForVisualLine(lineIndex)) || _editor.TryGetInteractiveHeadingFoldStateAtLine(lineIndex, out var _, out var _)))
		{
			num = lineIndex;
		}
		if (forceInvalidate || _forceShowLine != num)
		{
			_forceShowLine = num;
			InvalidateVisual();
		}
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		return new Size(_width, 0.0);
	}

	protected override void OnQueryCursor(QueryCursorEventArgs e)
	{
		e.Cursor = (_editor.StyleEnabled ? Cursors.Hand : Cursors.Arrow);
	}

	protected override void OnMouseDown(MouseButtonEventArgs e)
	{
		if (e.ChangedButton != MouseButton.Left || !_editor.StyleEnabled)
		{
			return;
		}
		if (TryGetHeadingFoldLineForClick(e.GetPosition(_editor), out var lineIndex) && IsFoldToggleHit(e.GetPosition(this), lineIndex))
		{
			_editor.FocusEditBox();
			Keyboard.Focus(_editor.EditBox.TextArea);
			if (_editor.ToggleHeadingFoldAtLine(lineIndex))
			{
				_editor.PrepareHeadingFoldTogglePointerCompletion();
				UpdateHoverLineFromPoint(e.GetPosition(_editor), forceInvalidate: true);
				e.Handled = true;
				return;
			}
		}
		UserActCollectUtils.AddClickEvent("slash_om", "method", string.IsNullOrEmpty(_currentAction) ? "+" : "heading");
		_editor.ShowStyleMenu(string.Empty, _currentAction, fromSplash: false);
		e.Handled = true;
	}

	protected override void OnRender(DrawingContext drawingContext)
	{
		_currentAction = string.Empty;
		TextView textView = base.TextView;
		bool firstLine = _area.Caret.Line == 1;
		int currentLineIndex = GetCurrentLineIndex();
		bool? flag = null;
		int forceShowLine = _forceShowLine;
		int level;
		if (ShouldDrawCurrentLineMarker() && textView != null && textView.VisualLinesValid)
		{
			VisualLine visualLineByLineIndex = GetVisualLineByLineIndex(currentLineIndex);
			if (visualLineByLineIndex != null)
			{
				bool folded;
				bool flag2 = _editor.TryGetHeadingFoldStateAtLine(currentLineIndex, out folded, out level);
				bool folded2;
				bool flag3 = _editor.TryGetInteractiveHeadingFoldStateAtLine(currentLineIndex, out folded2, out level);
				if (!IsSemanticCodeBlockAtOffset(_editor, visualLineByLineIndex.StartOffset))
				{
					if (!flag.HasValue)
					{
						flag = ThemeUtil.GetColor("BaseColorOpacity100", _editor).Color.ToString() == "#FFFFFFFF";
					}
					if (flag2)
					{
						CheckDrawHeading(drawingContext, visualLineByLineIndex, textView, _width, firstLine, flag.Value);
						if (flag3 && forceShowLine != currentLineIndex)
						{
							DrawHeadingFoldArrow(drawingContext, visualLineByLineIndex, textView, folded2, flag.Value, !_inSticky);
						}
					}
					else if (forceShowLine != currentLineIndex && CanShowAddIconForVisualLine(visualLineByLineIndex))
					{
						CheckDrawHeading(drawingContext, visualLineByLineIndex, textView, _width, firstLine, flag.Value);
					}
				}
			}
		}
		VisualLine visualLine = (_editor.StyleEnabled ? GetVisualLineByLineIndex(forceShowLine) : null);
		if (visualLine != null)
		{
			if (!flag.HasValue)
			{
				flag = ThemeUtil.GetColor("BaseColorOpacity100", _editor).Color.ToString() == "#FFFFFFFF";
			}
			if (_editor.TryGetInteractiveHeadingFoldStateAtLine(forceShowLine, out var folded3, out level))
			{
				DrawHeadingFoldArrow(drawingContext, visualLine, textView, folded3, flag.Value, ShouldAlignFoldArrowWithHeadingIcon(forceShowLine));
			}
			else if (CanShowAddIconForVisualLine(forceShowLine))
			{
				DrawHoverAddIcon(drawingContext, visualLine, textView, _width, firstLine: false, flag.Value);
			}
		}
	}

	private int GetCurrentLineIndex()
	{
		TextView textView = base.TextView;
		if (textView?.Document == null)
		{
			return -1;
		}
		return textView.Document.GetLineByOffset(_area.Caret.Offset).LineNumber - 1;
	}

	private static bool IsSemanticCodeBlockAtOffset(MarkDownEditor editor, int offset)
	{
		if (editor != null && editor.TryGetSemanticBlockAtOffset(offset, out var block))
		{
			if (block == null)
			{
				return false;
			}
			return block.Kind == MarkdownBlockKind.CodeBlock;
		}
		return false;
	}

	private bool TryGetLineIndexFromPoint(Point point, out int lineIndex)
	{
		lineIndex = -1;
		if (_editor.EditBox.Text.Length <= 0)
		{
			return false;
		}
		TextViewPosition? positionFromPoint = _editor.EditBox.GetPositionFromPoint(point);
		if (!positionFromPoint.HasValue)
		{
			return false;
		}
		lineIndex = positionFromPoint.Value.Line - 1;
		return lineIndex >= 0;
	}

	private VisualLine GetVisualLineByLineIndex(int lineIndex)
	{
		TextView textView = base.TextView;
		if (lineIndex < 0 || textView == null || !textView.VisualLinesValid)
		{
			return null;
		}
		foreach (VisualLine visualLine in textView.VisualLines)
		{
			if (visualLine?.FirstDocumentLine?.LineNumber == lineIndex + 1)
			{
				return visualLine;
			}
		}
		return null;
	}

	private bool IsEmptyVisualLine(int lineIndex)
	{
		VisualLine visualLineByLineIndex = GetVisualLineByLineIndex(lineIndex);
		return IsEmptyVisualLine(visualLineByLineIndex);
	}

	private bool IsEmptyVisualLine(VisualLine line)
	{
		DocumentLine documentLine = line?.FirstDocumentLine;
		if (documentLine == null || _editor?.EditBox?.Document == null)
		{
			return false;
		}
		return IsVisuallyEmptyLineText(_editor.EditBox.Document.GetText(documentLine.Offset, documentLine.Length));
	}

	private bool CanShowAddIconForVisualLine(int lineIndex)
	{
		VisualLine visualLineByLineIndex = GetVisualLineByLineIndex(lineIndex);
		return CanShowAddIconForVisualLine(visualLineByLineIndex);
	}

	private bool CanShowAddIconForVisualLine(VisualLine line)
	{
		if (IsEmptyVisualLine(line))
		{
			return !HasSemanticStructureAtLine(line);
		}
		return false;
	}

	private bool HasSemanticStructureAtLine(VisualLine line)
	{
		if (line == null || _editor == null || !_editor.TryGetSemanticBlockAtOffset(line.StartOffset, out var block) || block == null)
		{
			return false;
		}
		if (block.Kind != MarkdownBlockKind.Paragraph)
		{
			return true;
		}
		return Math.Max(0, block.Indent) > 0;
	}

	private static bool IsVisuallyEmptyLineText(string lineText)
	{
		if (string.IsNullOrEmpty(lineText))
		{
			return true;
		}
		foreach (char c in lineText)
		{
			if (!IsIgnorableVisualEmptyLineChar(c) && !char.IsWhiteSpace(c))
			{
				return false;
			}
		}
		return true;
	}

	private bool ShouldDrawCurrentLineMarker()
	{
		if (_editor.StyleEnabled)
		{
			if (!_editor.KeyboardFocused && !_editor.CurrentFocused)
			{
				return _editor.EditBox?.IsKeyboardFocusWithin ?? false;
			}
			return true;
		}
		return false;
	}

	private static bool IsIgnorableVisualEmptyLineChar(char ch)
	{
		if (ch != '\u00a0' && ch != '\u2065' && ch != '\u00a0' && ch != '\u200b' && ch != '\u200c' && ch != '\u200d' && ch != '\u200e' && ch != '\u200f' && ch != '\u2060' && ch != '\u2061' && ch != '\u2062' && ch != '\u2063' && ch != '\u2064' && ch != '\u2065' && ch != '\u2066')
		{
			return ch == '\u206a';
		}
		return true;
	}

	private bool TryGetHeadingFoldLineForClick(Point editorPoint, out int lineIndex)
	{
		lineIndex = _forceShowLine;
		if (lineIndex >= 0 && _editor.TryGetInteractiveHeadingFoldStateAtLine(lineIndex, out var folded, out var level))
		{
			return true;
		}
		if (TryGetLineIndexFromPoint(editorPoint, out lineIndex) && _editor.TryGetInteractiveHeadingFoldStateAtLine(lineIndex, out folded, out level))
		{
			return true;
		}
		lineIndex = -1;
		return false;
	}

	private void CheckDrawHeading(DrawingContext drawingContext, VisualLine line, IScrollInfo textView, double width, bool firstLine, bool isDark)
	{
		Tuple<string, string> paragraphType = GetParagraphType(line, firstLine, isDark);
		if (paragraphType.Item2 != null)
		{
			bool flag = paragraphType.Item1.StartsWith("Heading", StringComparison.Ordinal);
			if (!(_inSticky && flag))
			{
				int num = (flag ? 2 : 0);
				DrawSlideIcon(drawingContext, line, textView, width, paragraphType.Item2, isDark, num);
			}
		}
		_currentAction = paragraphType.Item1;
	}

	private void DrawHoverAddIcon(DrawingContext drawingContext, VisualLine line, IScrollInfo textView, double width, bool firstLine, bool isDark)
	{
		Tuple<string, string> paragraphType = GetParagraphType(line, firstLine, isDark);
		if (paragraphType.Item2 != null)
		{
			DrawSlideIcon(drawingContext, line, textView, width, paragraphType.Item2, isDark);
		}
	}

	private void DrawHeadingFoldArrow(DrawingContext drawingContext, VisualLine line, IScrollInfo textView, bool folded, bool isDark, bool alignWithHeadingIcon)
	{
		double num = line.GetTextLineVisualYPosition(line.TextLines[0], VisualYPosition.TextMiddle) - textView.VerticalOffset - 1.0;
		SolidColorBrush brush = (isDark ? new SolidColorBrush(Colors.White)
		{
			Opacity = 1.0
		} : new SolidColorBrush(Color.FromRgb(25, 25, 25))
		{
			Opacity = 1.0
		});
		double foldArrowIconLeft = GetFoldArrowIconLeft(alignWithHeadingIcon);
		drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(foldArrowIconLeft, num - 6.0, 12.0, 12.0));
		drawingContext.DrawGeometry(brush, null, CreateFoldArrowGeometry(foldArrowIconLeft, num, folded));
	}

	private double GetFoldArrowIconLeft(bool alignWithHeadingIcon)
	{
		if (!alignWithHeadingIcon)
		{
			return Math.Max(0.0, (_width - 12.0) / 2.0) + (_inSticky ? 0.0 : 2.0);
		}
		return 0.0;
	}

	private bool ShouldAlignFoldArrowWithHeadingIcon(int lineIndex)
	{
		if (!_inSticky && ShouldDrawCurrentLineMarker() && lineIndex >= 0)
		{
			return lineIndex == GetCurrentLineIndex();
		}
		return false;
	}

	private static Geometry CreateFoldArrowGeometry(double iconLeft, double centerY, bool folded)
	{
		Geometry geometry = Utils.GetGeometry("Ic.MarkdownEditor.TitleArrow")?.Clone();
		if (geometry == null)
		{
			return CreateFallbackFoldTriangle(iconLeft, centerY, folded);
		}
		TransformGroup transformGroup = new TransformGroup();
		if (folded)
		{
			transformGroup.Children.Add(new RotateTransform(-90.0, 6.0, 6.0));
		}
		transformGroup.Children.Add(new TranslateTransform(iconLeft, centerY - 6.0));
		geometry.Transform = transformGroup;
		return geometry;
	}

	private static Geometry CreateFallbackFoldTriangle(double iconLeft, double centerY, bool folded)
	{
		double num = iconLeft + 2.5;
		PathFigure pathFigure = new PathFigure
		{
			IsClosed = true,
			IsFilled = true
		};
		if (folded)
		{
			pathFigure.StartPoint = new Point(num + 1.0, centerY - 4.0);
			pathFigure.Segments.Add(new LineSegment(new Point(num + 1.0, centerY + 4.0), isStroked: true));
			pathFigure.Segments.Add(new LineSegment(new Point(num + 6.0, centerY), isStroked: true));
		}
		else
		{
			pathFigure.StartPoint = new Point(num, centerY - 2.5);
			pathFigure.Segments.Add(new LineSegment(new Point(num + 7.0, centerY - 2.5), isStroked: true));
			pathFigure.Segments.Add(new LineSegment(new Point(num + 3.5, centerY + 3.5), isStroked: true));
		}
		return new PathGeometry(new PathFigure[1] { pathFigure });
	}

	private bool IsFoldToggleHit(Point point, int lineIndex)
	{
		double foldArrowIconLeft = GetFoldArrowIconLeft(ShouldAlignFoldArrowWithHeadingIcon(lineIndex));
		if (point.X >= Math.Max(0.0, foldArrowIconLeft - 1.0))
		{
			return point.X <= Math.Min(_width, foldArrowIconLeft + 12.0 + 1.0);
		}
		return false;
	}

	private Tuple<string, string> GetParagraphType(VisualLine line, bool firstLine, bool isDark)
	{
		if (line == null)
		{
			return new Tuple<string, string>(string.Empty, null);
		}
		if (_editor.TryGetSemanticBlockAtOffset(line.StartOffset, out var block) && block.Kind == MarkdownBlockKind.Heading && block.Level.HasValue)
		{
			int num = Math.Max(1, Math.Min(6, block.Level.Value));
			return new Tuple<string, string>("Heading" + num, "Ic.MarkdownEditor.Header" + num);
		}
		if (CanShowAddIconForVisualLine(line))
		{
			return new Tuple<string, string>(string.Empty, "Ic.ProjectList.Add");
		}
		return new Tuple<string, string>(string.Empty, null);
	}

	private void DrawSlideIcon(DrawingContext drawingContext, VisualLine line, IScrollInfo textView, double width, string icon, bool isDark, double xOffset = 0.0)
	{
		bool flag = string.Equals(icon, "Ic.ProjectList.Add", StringComparison.Ordinal);
		double num = (flag ? 12.0 : Math.Min(14.0, Math.Max(8.0, width - 2.0)));
		double num2 = line.GetTextLineVisualYPosition(line.TextLines[0], VisualYPosition.TextTop) - textView.VerticalOffset;
		double num3 = xOffset + (width - num) / 2.0;
		double num4 = (flag ? ((14.0 - num) / 2.0) : 0.0);
		Rect rectangle = new Rect(new Point(num3 + ((_inSticky && flag) ? 0.0 : 2.5), num2 + 1.5 + num4), new Size(num, num));
		drawingContext.DrawRectangle(Brushes.Transparent, null, rectangle);
		Geometry geometry = Utils.GetGeometry(icon).Clone();
		double scaleX = num / 24.0;
		double scaleY = num / 24.0;
		ScaleTransform value = new ScaleTransform(scaleX, scaleY);
		TranslateTransform value2 = new TranslateTransform(rectangle.X, rectangle.Y);
		TransformGroup transformGroup = new TransformGroup();
		transformGroup.Children.Add(value);
		transformGroup.Children.Add(value2);
		geometry.Transform = transformGroup;
		SolidColorBrush brush = (isDark ? darkIconBrush : lightIconBrush);
		drawingContext.DrawGeometry(brush, null, geometry);
	}
}
