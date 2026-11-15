using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Framework.Markdown;

namespace ticktick_WPF.Views.MarkDown.Colorizer;

public class CodeColorizer : DocumentColorizingTransformer
{
	private readonly IHighlightingDefinition _definition;

	private readonly MarkDownEditor _editor;

	private readonly string _languageKey;

	private readonly bool _isFixedHighlighter;

	private IHighlighter _highlighter;

	private bool _isInHighlightingGroup;

	private DocumentLine _lastColorizedLine;

	private int _lineNumberBeingColorized;

	private TextView _textView;

	public CodeColorizer(MarkDownEditor editor, IHighlightingDefinition definition)
		: this(editor, definition, definition?.Name)
	{
	}

	public CodeColorizer(MarkDownEditor editor, IHighlightingDefinition definition, string languageKey)
	{
		_editor = editor;
		_definition = definition ?? throw new ArgumentNullException("definition");
		_languageKey = languageKey ?? definition.Name;
	}

	private void textView_DocumentChanged(object sender, EventArgs e)
	{
		TextView textView = (TextView)sender;
		DeregisterServices(textView);
		RegisterServices(textView);
	}

	protected virtual void DeregisterServices(TextView textView)
	{
		if (_highlighter != null)
		{
			if (_isInHighlightingGroup)
			{
				_highlighter.EndHighlighting();
				_isInHighlightingGroup = false;
			}
			_highlighter.HighlightingStateChanged -= OnHighlightStateChanged;
			if (textView.Services.GetService(typeof(IHighlighter)) == _highlighter)
			{
				textView.Services.RemoveService(typeof(IHighlighter));
			}
			if (!_isFixedHighlighter)
			{
				_highlighter?.Dispose();
				_highlighter = null;
			}
		}
	}

	protected virtual void RegisterServices(TextView textView)
	{
		if (textView.Document == null)
		{
			return;
		}
		if (!_isFixedHighlighter)
		{
			_highlighter = ((textView.Document != null) ? CreateHighlighter(textView, textView.Document) : null);
		}
		if (_highlighter != null && _highlighter.Document == textView.Document)
		{
			if (textView.Services.GetService(typeof(IHighlighter)) == null)
			{
				textView.Services.AddService(typeof(IHighlighter), _highlighter);
			}
			_highlighter.HighlightingStateChanged += OnHighlightStateChanged;
		}
	}

	protected virtual IHighlighter CreateHighlighter(TextView textView, TextDocument document)
	{
		if (_definition != null)
		{
			return new DocumentHighlighter(document, _definition);
		}
		throw new NotSupportedException("Cannot create a highlighter because no IHighlightingDefinition was specified, and the CreateHighlighter() method was not overridden.");
	}

	protected override void OnAddToTextView(TextView textView)
	{
		if (_textView != null)
		{
			throw new InvalidOperationException("Cannot use a HighlightingColorizer instance in multiple text views. Please create a separate instance for each text view.");
		}
		base.OnAddToTextView(textView);
		_textView = textView;
		textView.DocumentChanged += textView_DocumentChanged;
		textView.VisualLineConstructionStarting += textView_VisualLineConstructionStarting;
		textView.VisualLinesChanged += textView_VisualLinesChanged;
		RegisterServices(textView);
	}

	protected override void OnRemoveFromTextView(TextView textView)
	{
		DeregisterServices(textView);
		textView.DocumentChanged -= textView_DocumentChanged;
		textView.VisualLineConstructionStarting -= textView_VisualLineConstructionStarting;
		textView.VisualLinesChanged -= textView_VisualLinesChanged;
		base.OnRemoveFromTextView(textView);
		_textView = null;
	}

	private void textView_VisualLineConstructionStarting(object sender, VisualLineConstructionStartEventArgs e)
	{
		if (_highlighter != null)
		{
			_lineNumberBeingColorized = e.FirstLineInView.LineNumber - 1;
			if (!_isInHighlightingGroup)
			{
				_highlighter.BeginHighlighting();
				_isInHighlightingGroup = true;
			}
			_highlighter.UpdateHighlightingState(_lineNumberBeingColorized);
			_lineNumberBeingColorized = 0;
		}
	}

	private void textView_VisualLinesChanged(object sender, EventArgs e)
	{
		if (_highlighter != null && _isInHighlightingGroup)
		{
			_highlighter.EndHighlighting();
			_isInHighlightingGroup = false;
		}
	}

	protected override void Colorize(ITextRunConstructionContext context)
	{
		_lastColorizedLine = null;
		base.Colorize(context);
		if (_lastColorizedLine != context.VisualLine.LastDocumentLine && _highlighter != null)
		{
			_lineNumberBeingColorized = context.VisualLine.LastDocumentLine.LineNumber;
			_highlighter.UpdateHighlightingState(_lineNumberBeingColorized);
			_lineNumberBeingColorized = 0;
		}
		_lastColorizedLine = null;
	}

	protected override void ColorizeLine(DocumentLine line)
	{
		if (_highlighter != null)
		{
			_lineNumberBeingColorized = line.LineNumber;
			HighlightedLine highlightedLine = _highlighter.HighlightLine(_lineNumberBeingColorized);
			_lineNumberBeingColorized = 0;
			if (IsCurrentLineValid(line))
			{
				foreach (HighlightedSection section in highlightedLine.Sections)
				{
					if (!IsEmptyColor(section.Color))
					{
						ChangeLinePart(section.Offset, section.Offset + section.Length, delegate(VisualLineElement visualLineElement)
						{
							ApplyColorToElement(visualLineElement, section.Color);
						});
					}
				}
			}
		}
		_lastColorizedLine = line;
	}

	private bool IsCurrentLineValid(DocumentLine line)
	{
		if (_editor != null && _editor.TryGetSemanticBlockAtOffset(line.Offset, out var block) && block != null && block.Kind == MarkdownBlockKind.CodeBlock)
		{
			return IsSemanticCodeBlockContentLine(_editor.EditBox?.Document, line, block, _languageKey);
		}
		if (_editor.CodeDict != null && _editor.CodeDict.Any())
		{
			foreach (string key in _editor.CodeDict.Keys)
			{
				if (!string.Equals(key, _languageKey, StringComparison.CurrentCultureIgnoreCase))
				{
					continue;
				}
				foreach (Point item in _editor.CodeDict[key])
				{
					int offset = line.Offset;
					int endOffset = line.EndOffset;
					if (item.X <= (double)offset && item.Y >= (double)endOffset)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	internal static bool IsSemanticCodeBlockContentLine(TextDocument document, DocumentLine line, MarkdownBlockNode block, string languageKey)
	{
		if (block == null || block.Kind != MarkdownBlockKind.CodeBlock || !CodeBlockLineClassifier.IsContentLine(document, line, block))
		{
			return false;
		}
		return string.Equals(block.Language ?? string.Empty, languageKey ?? string.Empty, StringComparison.CurrentCultureIgnoreCase);
	}

	private static bool IsEmptyColor(HighlightingColor color)
	{
		if (color == null)
		{
			return true;
		}
		if (color.Background == null && color.Foreground == null && !color.FontStyle.HasValue && !color.FontWeight.HasValue && !color.Underline.HasValue)
		{
			return !color.Strikethrough.HasValue;
		}
		return false;
	}

	protected virtual void ApplyColorToElement(VisualLineElement element, HighlightingColor color)
	{
		ApplyColorToElement(element, color, base.CurrentContext);
	}

	private static void ApplyColorToElement(VisualLineElement element, HighlightingColor color, ITextRunConstructionContext context)
	{
		if (color.Foreground != null)
		{
			Brush brush = color.Foreground.GetBrush(context);
			if (brush != null)
			{
				element.TextRunProperties.SetForegroundBrush(brush);
			}
		}
		if (color.Background != null)
		{
			Brush brush2 = color.Background.GetBrush(context);
			if (brush2 != null)
			{
				element.BackgroundBrush = brush2;
			}
		}
		if (color.FontStyle.HasValue || color.FontWeight.HasValue || color.FontFamily != null)
		{
			Typeface typeface = element.TextRunProperties.Typeface;
			element.TextRunProperties.SetTypeface(new Typeface(color.FontFamily ?? typeface.FontFamily, color.FontStyle ?? typeface.Style, color.FontWeight ?? typeface.Weight, typeface.Stretch));
		}
		if (color.Underline == true)
		{
			element.TextRunProperties.SetTextDecorations(TextDecorations.Underline);
		}
		if (color.Strikethrough == true)
		{
			element.TextRunProperties.SetTextDecorations(TextDecorations.Strikethrough);
		}
		if (color.FontSize.HasValue)
		{
			element.TextRunProperties.SetFontRenderingEmSize(color.FontSize.Value);
		}
	}

	private void OnHighlightStateChanged(int fromLineNumber, int toLineNumber)
	{
		if (_lineNumberBeingColorized == 0 || toLineNumber > _lineNumberBeingColorized)
		{
			if (fromLineNumber == toLineNumber)
			{
				_textView.Redraw(_textView.Document.GetLineByNumber(fromLineNumber));
				return;
			}
			DocumentLine lineByNumber = _textView.Document.GetLineByNumber(fromLineNumber);
			DocumentLine lineByNumber2 = _textView.Document.GetLineByNumber(toLineNumber);
			int offset = lineByNumber.Offset;
			_textView.Redraw(offset, lineByNumber2.EndOffset - offset);
		}
	}
}
