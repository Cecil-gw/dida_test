using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Framework.Markdown;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.MarkDown.Colorizer;

namespace ticktick_WPF.Views.MarkDown;

internal sealed class CodeBlockAttachmentControl : Border
{
	private const string TickTickPlainTextClipboardFormat = "TickTickPlainText";

	private const double ContentLeftPadding = 16.0;

	private const double ContentRightPadding = 16.0;

	private const string InternalCodeEditorDragDataFormat = "TickTick.CodeBlockAttachment.InternalDrag";

	private static readonly FontFamily CodeFontFamily = new FontFamily("Consolas");

	private readonly MarkDownEditor _owner;

	private readonly TextEditor _codeEditor;

	private readonly SearchHighlightBackgroundRender _searchRenderer;

	private readonly string _internalDragOwnerId = Guid.NewGuid().ToString("N");

	private MarkdownBlockNode _block;

	private bool _synchronizing;

	private int _visualLineCount;

	private DispatcherOperation _pendingLayoutRefresh;

	private bool _selectAllShortcutPrimed;

	private bool _preserveFocusDuringLayoutRefresh;

	private bool _restoreFocusAfterPendingLayoutRefresh;

	private int? _pendingCaretLineAfterDocumentChange;

	private int? _pendingCaretColumnAfterDocumentChange;

	private bool _applyingMeasuredEditorHeight;

	private string _measuredHeightCodeText;

	private double _measuredHeightControlWidth = double.NaN;

	private double _measuredHeightFontSize = double.NaN;

	private double _measuredHeightLineSpacing = double.NaN;

	private double _measuredEditorHeight = double.NaN;

	public MarkdownBlockNode Block => _block;

	internal double AttachmentHeight
	{
		get
		{
			double num = _codeEditor.Height;
			if (double.IsNaN(num) || double.IsInfinity(num) || num <= 0.0)
			{
				num = Math.Max(1.0, _codeEditor.MinHeight);
			}
			return 24.0 + num;
		}
	}

	internal bool IsCodeEditorKeyboardFocused => _codeEditor.IsKeyboardFocusWithin;

	public CodeBlockAttachmentControl(MarkDownEditor owner, MarkdownBlockNode block)
	{
		_owner = owner;
		_block = block;
		base.Background = Brushes.Transparent;
		base.SnapsToDevicePixels = true;
		Grid grid = new Grid
		{
			RowDefinitions = 
			{
				new RowDefinition
				{
					Height = new GridLength(24.0)
				},
				new RowDefinition
				{
					Height = GridLength.Auto
				}
			}
		};
		_codeEditor = new TextEditor
		{
			Background = Brushes.Transparent,
			BorderThickness = new Thickness(0.0),
			ShowLineNumbers = false,
			WordWrap = true,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
			Margin = new Thickness(0.0),
			Padding = new Thickness(16.0, 0.0, 16.0, 12.0),
			MinHeight = Math.Max(1.0, owner?.FontSize ?? 14.0)
		};
		_codeEditor.Options.EnableHyperlinks = false;
		_codeEditor.Options.EnableEmailHyperlinks = false;
		_codeEditor.Options.IndentationSize = 4;
		_codeEditor.Options.ConvertTabsToSpaces = true;
		_codeEditor.Options.EnableTextDragDrop = true;
		_codeEditor.AllowDrop = true;
		_codeEditor.TextArea.AllowDrop = true;
		_codeEditor.TextArea.TextView.AllowDrop = true;
		_codeEditor.PreviewKeyDown += OnCodeEditorPreviewKeyDown;
		_codeEditor.PreviewDrop += OnCodeEditorPreviewDrop;
		DataObject.AddCopyingHandler(_codeEditor.TextArea, OnCodeEditorDataObjectCopying);
		_codeEditor.PreviewMouseLeftButtonDown += OnCodeEditorPreviewMouseLeftButtonDown;
		_codeEditor.GotKeyboardFocus += OnCodeEditorGotKeyboardFocus;
		_codeEditor.LostKeyboardFocus += OnCodeEditorLostKeyboardFocus;
		_codeEditor.Document.Changed += OnCodeEditorDocumentChanged;
		_codeEditor.TextChanged += OnCodeEditorTextChanged;
		_codeEditor.TextArea.Caret.PositionChanged += OnCodeEditorCaretPositionChanged;
		_codeEditor.TextArea.TextView.VisualLinesChanged += OnCodeEditorVisualLinesChanged;
		_searchRenderer = new SearchHighlightBackgroundRender(_codeEditor, new Dictionary<int, LinkInfo>(), isTitle: false, inDetail: false);
		_codeEditor.TextArea.TextView.BackgroundRenderers.Add(_searchRenderer);
		Grid.SetRow(_codeEditor, 1);
		grid.Children.Add(_codeEditor);
		Child = grid;
		UpdateBlock(block, 80.0, null);
	}

	public bool TryGetBounds(TextView textView, out Rect rect)
	{
		rect = Rect.Empty;
		if (textView == null || !base.IsVisible || base.ActualWidth <= 0.0 || base.ActualHeight <= 0.0)
		{
			return false;
		}
		try
		{
			GeneralTransform generalTransform = TransformToAncestor(textView);
			rect = generalTransform.TransformBounds(new Rect(new Point(0.0, 0.0), base.RenderSize));
			return !rect.IsEmpty && rect.Width > 0.0 && rect.Height > 0.0;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
		catch (ArgumentException)
		{
			return false;
		}
	}

	public bool TryFocusCodeEditor(bool moveToEnd)
	{
		if (!base.IsVisible || !_codeEditor.IsVisible)
		{
			return false;
		}
		_selectAllShortcutPrimed = false;
		_codeEditor.Focus();
		Keyboard.Focus(_codeEditor.TextArea);
		int num = _codeEditor.Document?.TextLength ?? 0;
		_codeEditor.SelectionLength = 0;
		_codeEditor.CaretOffset = (moveToEnd ? num : 0);
		_codeEditor.TextArea.Caret.DesiredXPos = double.NaN;
		_codeEditor.TextArea.Caret.Show();
		return _codeEditor.IsKeyboardFocusWithin;
	}

	internal bool TryFocusCodeEditorPreservingCaret()
	{
		if (!base.IsVisible || !_codeEditor.IsVisible)
		{
			return false;
		}
		_selectAllShortcutPrimed = false;
		_codeEditor.Focus();
		Keyboard.Focus(_codeEditor.TextArea);
		if (!_codeEditor.IsKeyboardFocusWithin)
		{
			return false;
		}
		_codeEditor.TextArea.Caret.DesiredXPos = double.NaN;
		_codeEditor.TextArea.Caret.CaretBrush = _codeEditor.Foreground ?? Brushes.Black;
		_codeEditor.TextArea.Caret.Show();
		_owner?.HideParentCaretForCodeBlockAttachment();
		return true;
	}

	public void UpdateBlock(MarkdownBlockNode block, double width, IHighlightingDefinition highlightingDefinition)
	{
		_block = block;
		base.Width = Math.Max(80.0, width);
		_codeEditor.FontFamily = CodeFontFamily;
		_codeEditor.FontSize = _owner?.FontSize ?? 14.0;
		_codeEditor.Foreground = _owner?.EditBox?.Foreground ?? Brushes.Black;
		_codeEditor.TextArea.Caret.CaretBrush = (_codeEditor.IsKeyboardFocusWithin ? _codeEditor.Foreground : Brushes.Transparent);
		_codeEditor.TextArea.TextView.LineSpacing = Math.Max(0.0, _owner?.LineSpacing ?? 0.0);
		_codeEditor.SyntaxHighlighting = highlightingDefinition;
		_codeEditor.IsReadOnly = _owner?.ReadOnly ?? false;
		_searchRenderer.InDetail = _owner?.IsSearchRenderInDetail ?? false;
		string text = NormalizeCodeText(block?.CodeText);
		if (RequiresCodeTextSynchronization(_codeEditor.Text, text))
		{
			_synchronizing = true;
			try
			{
				_codeEditor.Text = text;
			}
			finally
			{
				_synchronizing = false;
			}
		}
		UpdateEditorHeight();
		RefreshSearchHighlight();
	}

	internal bool TryHandleTextInputForMaxLength(string text)
	{
		return TryInsertLimitedText(text);
	}

	internal bool TryHandlePaste(IDataObject dataObject, IDataObject fallbackDataObject)
	{
		if (dataObject == null && fallbackDataObject == null)
		{
			return false;
		}
		string text = TryGetClipboardText(dataObject, "TickTickPlainText") ?? TryGetClipboardText(fallbackDataObject, "TickTickPlainText");
		if (text != null)
		{
			return TryInsertClipboardText(text);
		}
		MarkDownEditor owner = _owner;
		if (owner != null && owner.MaxLength < 0)
		{
			return false;
		}
		string text2 = null;
		if (dataObject != null && dataObject.GetDataPresent(DataFormats.UnicodeText, autoConvert: true))
		{
			text2 = dataObject.GetData(DataFormats.UnicodeText, autoConvert: true) as string;
		}
		else if (dataObject != null && dataObject.GetDataPresent(DataFormats.Text, autoConvert: true))
		{
			text2 = dataObject.GetData(DataFormats.Text, autoConvert: true) as string;
		}
		if (text2 != null)
		{
			return TryInsertLimitedText(NormalizeCodeText(text2));
		}
		return false;
	}

	private static string TryGetClipboardText(IDataObject dataObject, string format)
	{
		if (dataObject == null)
		{
			return null;
		}
		try
		{
			return dataObject.GetDataPresent(format, autoConvert: false) ? (dataObject.GetData(format, autoConvert: false) as string) : null;
		}
		catch (Exception)
		{
			return null;
		}
	}

	private bool TryInsertClipboardText(string text)
	{
		MarkDownEditor owner = _owner;
		if ((owner != null && owner.ReadOnly) || _codeEditor.IsReadOnly)
		{
			return false;
		}
		text = NormalizeCodeText(text);
		int num = Math.Max(0, _codeEditor.SelectionLength);
		int val = text.Length;
		MarkDownEditor owner2 = _owner;
		if (owner2 != null && owner2.MaxLength >= 0)
		{
			int contentLengthForMaxLength = _owner.GetContentLengthForMaxLength();
			val = Math.Max(0, _owner.MaxLength - contentLengthForMaxLength + num);
		}
		string text2 = text.Substring(0, Math.Min(text.Length, val));
		int num2 = Math.Max(0, Math.Min(_codeEditor.SelectionStart, _codeEditor.Document?.TextLength ?? 0));
		_codeEditor.Document.Replace(num2, num, text2);
		_codeEditor.SelectionLength = 0;
		_codeEditor.CaretOffset = num2 + text2.Length;
		if (text2.Length < text.Length)
		{
			Utils.Toast(Utils.GetString("CharactersNumberLimit"));
		}
		return true;
	}

	private bool TryInsertLimitedText(string text)
	{
		MarkDownEditor owner = _owner;
		if ((owner == null || !owner.ReadOnly) && !_codeEditor.IsReadOnly)
		{
			MarkDownEditor owner2 = _owner;
			if ((owner2 == null || owner2.MaxLength >= 0) && !string.IsNullOrEmpty(text))
			{
				int contentLengthForMaxLength = _owner.GetContentLengthForMaxLength();
				int num = Math.Max(0, _codeEditor.SelectionLength);
				int num2 = Math.Max(0, _owner.MaxLength - contentLengthForMaxLength + num);
				if (text.Length <= num2)
				{
					return false;
				}
				string text2 = text.Substring(0, Math.Min(text.Length, num2));
				if (text2.Length > 0)
				{
					int num3 = Math.Max(0, Math.Min(_codeEditor.SelectionStart, _codeEditor.Document?.TextLength ?? 0));
					_codeEditor.Document.Replace(num3, num, text2);
					_codeEditor.SelectionLength = 0;
					_codeEditor.CaretOffset = num3 + text2.Length;
				}
				Utils.Toast(Utils.GetString("CharactersNumberLimit"));
				return true;
			}
		}
		return false;
	}

	public static bool TryFindFromOriginalSource(object originalSource, out CodeBlockAttachmentControl control)
	{
		control = null;
		DependencyObject dependencyObject = originalSource as DependencyObject;
		while (dependencyObject != null)
		{
			if (dependencyObject is CodeBlockAttachmentControl codeBlockAttachmentControl)
			{
				control = codeBlockAttachmentControl;
				return true;
			}
			try
			{
				dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
			}
			catch (InvalidOperationException)
			{
				dependencyObject = LogicalTreeHelper.GetParent(dependencyObject);
			}
		}
		return false;
	}

	public bool IsSourceInsideCodeEditor(object originalSource)
	{
		DependencyObject dependencyObject = originalSource as DependencyObject;
		while (dependencyObject != null)
		{
			if (dependencyObject == _codeEditor)
			{
				return true;
			}
			if (dependencyObject == this)
			{
				return false;
			}
			try
			{
				dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
			}
			catch (InvalidOperationException)
			{
				dependencyObject = LogicalTreeHelper.GetParent(dependencyObject);
			}
		}
		return false;
	}

	private void OnCodeEditorTextChanged(object sender, EventArgs e)
	{
		_selectAllShortcutPrimed = false;
		if (!_synchronizing && _block != null)
		{
			int visualLineCount = _visualLineCount;
			_owner?.UpdateCodeBlockAttachmentContent(_block, _codeEditor.Text);
			UpdateEditorHeight();
			if (_visualLineCount != visualLineCount)
			{
				ScheduleParentLayoutRefresh();
			}
		}
	}

	private void OnCodeEditorVisualLinesChanged(object sender, EventArgs e)
	{
		if (TryApplyMeasuredEditorHeight())
		{
			ScheduleParentLayoutRefresh();
		}
	}

	private void OnCodeEditorCaretPositionChanged(object sender, EventArgs e)
	{
		if (!_codeEditor.IsKeyboardFocusWithin)
		{
			return;
		}
		TextView textView = _codeEditor.TextArea?.TextView;
		TextView textView2 = _owner?.EditBox?.TextArea?.TextView;
		if (textView == null || textView2 == null)
		{
			return;
		}
		try
		{
			textView.EnsureVisualLines();
			Point point = textView.GetVisualPosition(_codeEditor.TextArea.Caret.Position, VisualYPosition.LineBottom) - textView.ScrollOffset;
			double offset = textView.TransformToAncestor(textView2).Transform(point).Y + textView2.ScrollOffset.Y;
			_owner.NotifyCodeBlockAttachmentCaretVerticalOffset(offset);
		}
		catch (InvalidOperationException)
		{
		}
		catch (ArgumentException)
		{
		}
		catch (VisualLinesInvalidException)
		{
		}
	}

	private void OnCodeEditorDocumentChanged(object sender, DocumentChangeEventArgs e)
	{
		if (!_synchronizing && _codeEditor.IsKeyboardFocusWithin && e != null)
		{
			TextLocation caretLocationAfterDocumentChange = GetCaretLocationAfterDocumentChange(_codeEditor.Document, e.Offset, e.InsertionLength);
			_pendingCaretLineAfterDocumentChange = caretLocationAfterDocumentChange.Line;
			_pendingCaretColumnAfterDocumentChange = caretLocationAfterDocumentChange.Column;
		}
	}

	private void OnCodeEditorPreviewKeyDown(object sender, KeyEventArgs e)
	{
		Key key = ((e.Key == Key.ImeProcessed) ? e.ImeProcessedKey : e.Key);
		ModifierKeys modifiers = Keyboard.Modifiers;
		if (MarkDownEditor.IsSemanticUndoShortcut(key, modifiers) || MarkDownEditor.IsSemanticRedoShortcut(key, modifiers))
		{
			MarkDownEditor owner = _owner;
			if (owner == null || !owner.ReadOnly)
			{
				_owner?.TryPerformSemanticHistoryFromCodeBlockAttachment(_block, MarkDownEditor.IsSemanticRedoShortcut(key, modifiers), _codeEditor.CaretOffset, _codeEditor.SelectionStart, _codeEditor.SelectionLength);
			}
			e.Handled = true;
			return;
		}
		if (IsExitCodeBlockShortcut(key, modifiers))
		{
			MarkDownEditor owner2 = _owner;
			if ((owner2 == null || !owner2.ReadOnly) && !_codeEditor.IsReadOnly)
			{
				MarkDownEditor owner3 = _owner;
				if (owner3 != null && owner3.WouldExceedMaxLengthForContentInsertion(1))
				{
					Utils.Toast(Utils.GetString("CharactersNumberLimit"));
					e.Handled = true;
					return;
				}
			}
			MarkDownEditor owner4 = _owner;
			if (owner4 != null && owner4.TryInsertParagraphAfterCodeBlockAttachment(_block))
			{
				_restoreFocusAfterPendingLayoutRefresh = false;
				_pendingCaretLineAfterDocumentChange = null;
				_pendingCaretColumnAfterDocumentChange = null;
				_selectAllShortcutPrimed = false;
				e.Handled = true;
				return;
			}
		}
		if (modifiers == ModifierKeys.None && (key == Key.Return || key == Key.Return) && TryInsertLimitedText("\n"))
		{
			e.Handled = true;
			return;
		}
		if (modifiers == ModifierKeys.None && key == Key.Tab && WouldCodeEditorTabExceedMaxLength())
		{
			Utils.Toast(Utils.GetString("CharactersNumberLimit"));
			e.Handled = true;
			return;
		}
		if (key == Key.Return)
		{
			TextDocument document = _codeEditor.Document;
			int offset = Math.Max(0, Math.Min(val2: document?.TextLength ?? 0, val1: _codeEditor.SelectionStart));
			_pendingCaretLineAfterDocumentChange = (document?.GetLocation(offset) ?? new TextLocation(1, 1)).Line + 1;
			_pendingCaretColumnAfterDocumentChange = 1;
		}
		if (modifiers == ModifierKeys.None && _codeEditor.SelectionLength > 0 && (e.Key == Key.Left || e.Key == Key.Right))
		{
			int start = ((e.Key == Key.Left) ? _codeEditor.SelectionStart : (_codeEditor.SelectionStart + _codeEditor.SelectionLength));
			_codeEditor.Select(start, 0);
			_codeEditor.TextArea.Caret.BringCaretToView();
			_selectAllShortcutPrimed = false;
			e.Handled = true;
			return;
		}
		if (modifiers == ModifierKeys.None && _codeEditor.SelectionLength == 0)
		{
			if (key == Key.Back || key == Key.Delete)
			{
				TextDocument document2 = _codeEditor.Document;
				if (document2 == null || document2.TextLength == 0)
				{
					MarkDownEditor owner5 = _owner;
					if (owner5 != null && owner5.TrySelectCodeBlockAttachment(_block))
					{
						_restoreFocusAfterPendingLayoutRefresh = false;
						_selectAllShortcutPrimed = false;
						e.Handled = true;
						return;
					}
				}
			}
			if (key == Key.Up && IsCaretOnFirstVisualRow())
			{
				MarkDownEditor owner6 = _owner;
				if (owner6 != null && owner6.TryMoveCaretAboveCodeBlockAttachment(_block))
				{
					_selectAllShortcutPrimed = false;
					e.Handled = true;
					return;
				}
			}
			if (key == Key.Down && IsCaretOnLastVisualRow())
			{
				MarkDownEditor owner7 = _owner;
				if (owner7 != null && owner7.TryMoveCaretBelowCodeBlockAttachment(_block))
				{
					_selectAllShortcutPrimed = false;
					e.Handled = true;
					return;
				}
			}
		}
		if (!MarkDownEditor.IsSelectAllShortcut(key, modifiers))
		{
			_selectAllShortcutPrimed = false;
			return;
		}
		int num = _codeEditor.Document?.TextLength ?? 0;
		int num2 = Math.Max(0, Math.Min(_codeEditor.SelectionStart, num));
		int num3 = Math.Max(0, Math.Min(_codeEditor.SelectionLength, num - num2));
		if (num2 == 0 && num3 == num && (num > 0 || _selectAllShortcutPrimed))
		{
			MarkDownEditor owner8 = _owner;
			if (owner8 != null && owner8.TrySelectAllFromCodeBlockAttachment(_block))
			{
				_selectAllShortcutPrimed = false;
				e.Handled = true;
				return;
			}
		}
		_selectAllShortcutPrimed = true;
	}

	private void OnCodeEditorPreviewDrop(object sender, DragEventArgs e)
	{
		if (ShouldBlockCodeEditorDropForMaxLength(e))
		{
			Utils.Toast(Utils.GetString("CharactersNumberLimit"));
			e.Handled = true;
		}
	}

	private void OnCodeEditorDataObjectCopying(object sender, DataObjectCopyingEventArgs e)
	{
		if (e != null && e.IsDragDrop)
		{
			e.DataObject.SetData("TickTick.CodeBlockAttachment.InternalDrag", _internalDragOwnerId);
		}
	}

	private bool ShouldBlockCodeEditorDropForMaxLength(DragEventArgs e)
	{
		IDataObject dataObject = e?.Data;
		MarkDownEditor owner = _owner;
		if ((owner == null || !owner.ReadOnly) && !_codeEditor.IsReadOnly)
		{
			MarkDownEditor owner2 = _owner;
			if ((owner2 == null || owner2.MaxLength >= 0) && dataObject != null)
			{
				string text = (dataObject.GetDataPresent(DataFormats.UnicodeText, autoConvert: true) ? (dataObject.GetData(DataFormats.UnicodeText, autoConvert: true) as string) : (dataObject.GetDataPresent(DataFormats.Text, autoConvert: true) ? (dataObject.GetData(DataFormats.Text, autoConvert: true) as string) : null));
				if (string.IsNullOrEmpty(text))
				{
					return false;
				}
				int num = (((e.AllowedEffects & DragDropEffects.Move) == DragDropEffects.Move && (e.KeyStates & DragDropKeyStates.ControlKey) == 0 && dataObject.GetDataPresent("TickTick.CodeBlockAttachment.InternalDrag", autoConvert: true) && string.Equals(dataObject.GetData("TickTick.CodeBlockAttachment.InternalDrag", autoConvert: true) as string, _internalDragOwnerId, StringComparison.Ordinal)) ? Math.Max(0, _codeEditor.SelectionLength) : 0);
				int num2 = Math.Max(0, _owner.MaxLength - _owner.GetContentLengthForMaxLength() + num);
				return NormalizeCodeText(text).Length > num2;
			}
		}
		return false;
	}

	private bool WouldCodeEditorTabExceedMaxLength()
	{
		MarkDownEditor owner = _owner;
		if ((owner == null || !owner.ReadOnly) && !_codeEditor.IsReadOnly)
		{
			MarkDownEditor owner2 = _owner;
			if (owner2 == null || owner2.MaxLength >= 0)
			{
				TextEditorOptions options = _codeEditor.Options;
				string text = options?.IndentationString ?? "    ";
				TextArea textArea = _codeEditor.TextArea;
				if (textArea != null && textArea.Selection?.IsMultiline == true && _codeEditor.Document != null)
				{
					int num = 1;
					int num2 = Math.Max(0, Math.Min(_codeEditor.SelectionStart, _codeEditor.Document.TextLength));
					int num3 = Math.Max(num2, Math.Min(num2 + _codeEditor.SelectionLength, _codeEditor.Document.TextLength));
					int offset = ((num3 > num2) ? (num3 - 1) : num3);
					num = Math.Max(1, _codeEditor.Document.GetLineByOffset(offset).LineNumber - _codeEditor.Document.GetLineByOffset(num2).LineNumber + 1);
					return _owner.WouldExceedMaxLengthForContentInsertion(text.Length * num);
				}
				int column = Math.Max(1, _codeEditor.TextArea?.Caret?.Column ?? 1);
				string text2 = options?.GetIndentationString(column) ?? text;
				return _owner.WouldExceedMaxLengthForContentInsertion(text2.Length, Math.Max(0, _codeEditor.SelectionLength));
			}
		}
		return false;
	}

	internal static bool IsExitCodeBlockShortcut(Key key, ModifierKeys modifiers)
	{
		if (key == Key.Return || key == Key.Return)
		{
			return modifiers == ModifierKeys.Shift;
		}
		return false;
	}

	internal static bool IsCaretOnFirstCodeEditorLine(TextDocument document, int caretOffset)
	{
		if (document == null || document.LineCount <= 0)
		{
			return false;
		}
		int offset = Math.Max(0, Math.Min(caretOffset, document.TextLength));
		return document.GetLineByOffset(offset).LineNumber == 1;
	}

	internal static bool IsCaretOnLastCodeEditorLine(TextDocument document, int caretOffset)
	{
		if (document == null || document.LineCount <= 0)
		{
			return false;
		}
		int offset = Math.Max(0, Math.Min(caretOffset, document.TextLength));
		return document.GetLineByOffset(offset).LineNumber == document.LineCount;
	}

	private bool IsCaretOnFirstVisualRow()
	{
		return IsCaretOnBoundaryVisualRow(firstRow: true);
	}

	private bool IsCaretOnLastVisualRow()
	{
		return IsCaretOnBoundaryVisualRow(firstRow: false);
	}

	private bool IsCaretOnBoundaryVisualRow(bool firstRow)
	{
		TextDocument document = _codeEditor.Document;
		TextView textView = _codeEditor.TextArea?.TextView;
		if (document == null || textView == null || document.LineCount <= 0)
		{
			return false;
		}
		try
		{
			textView.EnsureVisualLines();
			int num = Math.Max(0, Math.Min(_codeEditor.CaretOffset, document.TextLength));
			DocumentLine lineByOffset = document.GetLineByOffset(num);
			if (firstRow ? (lineByOffset.LineNumber != 1) : (lineByOffset.LineNumber != document.LineCount))
			{
				return false;
			}
			VisualLine orConstructVisualLine = textView.GetOrConstructVisualLine(lineByOffset);
			if (orConstructVisualLine?.TextLines == null || orConstructVisualLine.TextLines.Count == 0)
			{
				return firstRow ? IsCaretOnFirstCodeEditorLine(document, num) : IsCaretOnLastCodeEditorLine(document, num);
			}
			int relativeTextOffset = num - orConstructVisualLine.FirstDocumentLine.Offset;
			int visualColumn = orConstructVisualLine.GetVisualColumn(relativeTextOffset);
			TextLine textLine = orConstructVisualLine.GetTextLine(visualColumn, _codeEditor.TextArea.Caret.Position.IsAtEndOfLine);
			return firstRow ? (textLine == orConstructVisualLine.TextLines[0]) : (textLine == orConstructVisualLine.TextLines[orConstructVisualLine.TextLines.Count - 1]);
		}
		catch (InvalidOperationException)
		{
			return false;
		}
		catch (VisualLinesInvalidException)
		{
			return false;
		}
	}

	private void OnCodeEditorPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		_selectAllShortcutPrimed = false;
	}

	private void OnCodeEditorLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
	{
		if (!_preserveFocusDuringLayoutRefresh)
		{
			_selectAllShortcutPrimed = false;
			_pendingCaretLineAfterDocumentChange = null;
			_pendingCaretColumnAfterDocumentChange = null;
			_codeEditor.TextArea?.ClearSelection();
			if (_codeEditor.TextArea?.Caret != null)
			{
				_codeEditor.TextArea.Caret.CaretBrush = Brushes.Transparent;
				_codeEditor.TextArea.Caret.Hide();
			}
			_owner?.RefreshParentCaretVisibilityAfterCodeBlockFocusChange();
		}
	}

	private void OnCodeEditorGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
	{
		_preserveFocusDuringLayoutRefresh = false;
		_owner?.HideParentCaretForCodeBlockAttachment();
		if (_codeEditor.TextArea?.Caret != null)
		{
			_codeEditor.TextArea.Caret.CaretBrush = _codeEditor.Foreground ?? Brushes.Black;
			_codeEditor.TextArea.Caret.Show();
		}
	}

	private void UpdateEditorHeight()
	{
		double num = Math.Max(1.0, _codeEditor.FontSize);
		double num2 = Math.Max(0.0, (_codeEditor.TextArea?.TextView?.LineSpacing).GetValueOrDefault());
		string text = NormalizeCodeText(_codeEditor.Text);
		if (CanReuseMeasuredEditorHeight(text, base.Width, num, num2))
		{
			_codeEditor.Height = _measuredEditorHeight;
			return;
		}
		double num3 = Math.Max(40.0, base.Width - 16.0 - 16.0 - 4.0);
		double num4 = Math.Max(1.0, num * 0.62);
		int num5 = Math.Max(1, (int)Math.Floor(num3 / num4));
		int num6 = 0;
		string[] array = text.Split('\n');
		foreach (string obj in array)
		{
			int num7 = 0;
			string text2 = obj;
			foreach (char c in text2)
			{
				num7 += ((c == '\t') ? 4 : ((c <= 'ÿ') ? 1 : 2));
			}
			num6 += Math.Max(1, (num7 + num5 - 1) / num5);
		}
		_visualLineCount = Math.Max(1, num6);
		double num8 = _codeEditor.FontFamily?.LineSpacing ?? 1.0;
		double num9 = Math.Max(1.0, num8 * num + num2);
		_codeEditor.Height = (double)_visualLineCount * num9 + _codeEditor.Padding.Top + _codeEditor.Padding.Bottom;
	}

	private bool TryApplyMeasuredEditorHeight()
	{
		TextView textView = _codeEditor.TextArea?.TextView;
		if (_applyingMeasuredEditorHeight || textView == null || !textView.VisualLinesValid || textView.ActualWidth <= 0.0 || base.ActualWidth <= 0.0 || Math.Abs(base.ActualWidth - base.Width) > 0.5 || textView.DocumentHeight <= 0.0)
		{
			return false;
		}
		double num = ResolveMeasuredCodeEditorHeight(textView.DocumentHeight, _codeEditor.Padding, _codeEditor.MinHeight);
		string measuredHeightCodeText = NormalizeCodeText(_codeEditor.Text);
		double measuredHeightFontSize = Math.Max(1.0, _codeEditor.FontSize);
		double measuredHeightLineSpacing = Math.Max(0.0, textView.LineSpacing);
		_measuredHeightCodeText = measuredHeightCodeText;
		_measuredHeightControlWidth = base.Width;
		_measuredHeightFontSize = measuredHeightFontSize;
		_measuredHeightLineSpacing = measuredHeightLineSpacing;
		_measuredEditorHeight = num;
		if (Math.Abs(_codeEditor.Height - num) <= 0.5)
		{
			return false;
		}
		_applyingMeasuredEditorHeight = true;
		try
		{
			_codeEditor.Height = num;
		}
		finally
		{
			_applyingMeasuredEditorHeight = false;
		}
		return true;
	}

	private bool CanReuseMeasuredEditorHeight(string normalizedCodeText, double controlWidth, double fontSize, double lineSpacing)
	{
		if (!double.IsNaN(_measuredEditorHeight) && _measuredEditorHeight > 0.0 && string.Equals(_measuredHeightCodeText, normalizedCodeText, StringComparison.Ordinal) && Math.Abs(_measuredHeightControlWidth - controlWidth) <= 0.5 && Math.Abs(_measuredHeightFontSize - fontSize) <= 0.01)
		{
			return Math.Abs(_measuredHeightLineSpacing - lineSpacing) <= 0.01;
		}
		return false;
	}

	internal static double ResolveMeasuredCodeEditorHeight(double documentHeight, Thickness padding, double minimumHeight)
	{
		double num = ((documentHeight > 0.0 && !double.IsNaN(documentHeight) && !double.IsInfinity(documentHeight)) ? documentHeight : 0.0);
		return Math.Max(Math.Max(1.0, minimumHeight), num + Math.Max(0.0, padding.Top) + Math.Max(0.0, padding.Bottom));
	}

	private void RefreshSearchHighlight()
	{
		if (_searchRenderer != null && _codeEditor?.TextArea?.TextView != null)
		{
			_searchRenderer.SetTextChanged(b: true);
			_codeEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Background);
		}
	}

	internal bool TryGetFirstSearchMatchVerticalOffset(out double verticalOffset)
	{
		verticalOffset = 0.0;
		TextDocument textDocument = _codeEditor?.Document;
		TextView textView = _codeEditor?.TextArea?.TextView;
		int? num = _searchRenderer?.GetFirstMatchIndex(_codeEditor?.Text);
		if (textDocument == null || textView == null || !num.HasValue)
		{
			return false;
		}
		int offset = Math.Max(0, Math.Min(num.Value, textDocument.TextLength));
		try
		{
			textView.EnsureVisualLines();
			TextViewPosition position = new TextViewPosition(textDocument.GetLocation(offset));
			double y = textView.GetVisualPosition(position, VisualYPosition.LineBottom).Y;
			if (double.IsNaN(y) || double.IsInfinity(y) || y < 0.0)
			{
				return false;
			}
			verticalOffset = 24.0 + y;
			return true;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
		catch (VisualLinesInvalidException)
		{
			return false;
		}
	}

	private void ScheduleParentLayoutRefresh()
	{
		_restoreFocusAfterPendingLayoutRefresh |= _codeEditor.IsKeyboardFocusWithin;
		DispatcherOperation pendingLayoutRefresh = _pendingLayoutRefresh;
		if (pendingLayoutRefresh != null && pendingLayoutRefresh.Status == DispatcherOperationStatus.Pending)
		{
			return;
		}
		_pendingLayoutRefresh = base.Dispatcher.BeginInvoke((Action)delegate
		{
			_pendingLayoutRefresh = null;
			bool restoreFocusAfterPendingLayoutRefresh = _restoreFocusAfterPendingLayoutRefresh;
			_restoreFocusAfterPendingLayoutRefresh = false;
			int? pendingCaretLineAfterDocumentChange = _pendingCaretLineAfterDocumentChange;
			int? pendingCaretColumnAfterDocumentChange = _pendingCaretColumnAfterDocumentChange;
			_pendingCaretLineAfterDocumentChange = null;
			_pendingCaretColumnAfterDocumentChange = null;
			int caretOffset = _codeEditor.CaretOffset;
			int selectionStart = _codeEditor.SelectionStart;
			int selectionLength = ((!pendingCaretLineAfterDocumentChange.HasValue || !pendingCaretColumnAfterDocumentChange.HasValue) ? _codeEditor.SelectionLength : 0);
			if (restoreFocusAfterPendingLayoutRefresh)
			{
				_preserveFocusDuringLayoutRefresh = true;
			}
			_owner?.RefreshCodeBlockAttachmentLayout();
			if (restoreFocusAfterPendingLayoutRefresh)
			{
				ScheduleCodeEditorFocusRestore(caretOffset, selectionStart, selectionLength, pendingCaretLineAfterDocumentChange, pendingCaretColumnAfterDocumentChange);
			}
		}, DispatcherPriority.Background);
	}

	private void ScheduleCodeEditorFocusRestore(int caretOffset, int selectionStart, int selectionLength, int? caretLine, int? caretColumn)
	{
		base.Dispatcher.BeginInvoke((Action)delegate
		{
			_owner?.UpdateLayout();
			MarkDownEditor.TryEnsureVisualLines(_owner?.EditBox?.TextArea?.TextView);
			MarkDownEditor owner = _owner;
			if (owner != null && owner.TryRestoreCodeBlockAttachmentFocus(_block, caretOffset, selectionStart, selectionLength, caretLine, caretColumn))
			{
				_preserveFocusDuringLayoutRefresh = false;
			}
			else
			{
				base.Dispatcher.BeginInvoke((Action)delegate
				{
					_owner?.UpdateLayout();
					MarkDownEditor.TryEnsureVisualLines(_owner?.EditBox?.TextArea?.TextView);
					MarkDownEditor owner2 = _owner;
					if (owner2 == null || !owner2.TryRestoreCodeBlockAttachmentFocus(_block, caretOffset, selectionStart, selectionLength, caretLine, caretColumn))
					{
						_preserveFocusDuringLayoutRefresh = false;
						_codeEditor.TextArea?.ClearSelection();
						if (_codeEditor.TextArea?.Caret != null)
						{
							_codeEditor.TextArea.Caret.CaretBrush = Brushes.Transparent;
							_codeEditor.TextArea.Caret.Hide();
						}
						_owner?.RefreshParentCaretVisibilityAfterCodeBlockFocusChange();
					}
				}, DispatcherPriority.ContextIdle);
			}
		}, DispatcherPriority.Loaded);
	}

	internal bool TryRestoreCodeEditorFocus(int caretOffset, int selectionStart, int selectionLength, int? caretLine, int? caretColumn)
	{
		if (!base.IsVisible || !_codeEditor.IsVisible)
		{
			return false;
		}
		_codeEditor.Focus();
		Keyboard.Focus(_codeEditor.TextArea);
		if (!_codeEditor.IsKeyboardFocusWithin)
		{
			return false;
		}
		int num = _codeEditor.Document?.TextLength ?? 0;
		int num2;
		int num3;
		if (caretLine.HasValue)
		{
			num2 = (caretColumn.HasValue ? 1 : 0);
			if (num2 != 0)
			{
				num3 = GetOffsetForCodeEditorLocation(_codeEditor.Document, caretLine.Value, caretColumn.Value);
				goto IL_009e;
			}
		}
		else
		{
			num2 = 0;
		}
		num3 = Math.Max(0, Math.Min(caretOffset, num));
		goto IL_009e;
		IL_009e:
		int num4 = num3;
		int num5 = ((num2 != 0) ? num4 : Math.Max(0, Math.Min(selectionStart, num)));
		int length = ((num2 == 0) ? Math.Max(0, Math.Min(selectionLength, num - num5)) : 0);
		_codeEditor.Select(num5, length);
		_codeEditor.CaretOffset = num4;
		_codeEditor.TextArea.Caret.DesiredXPos = double.NaN;
		_codeEditor.TextArea.Caret.CaretBrush = _codeEditor.Foreground ?? Brushes.Black;
		_codeEditor.TextArea.Caret.Show();
		_preserveFocusDuringLayoutRefresh = false;
		_owner?.HideParentCaretForCodeBlockAttachment();
		return true;
	}

	private static string NormalizeCodeText(string text)
	{
		return (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
	}

	internal static TextLocation GetCaretLocationAfterDocumentChange(TextDocument document, int changeOffset, int insertionLength)
	{
		if (document == null)
		{
			return new TextLocation(1, 1);
		}
		int offset = Math.Max(0, Math.Min(changeOffset + Math.Max(0, insertionLength), document.TextLength));
		return document.GetLocation(offset);
	}

	private static int GetOffsetForCodeEditorLocation(TextDocument document, int line, int column)
	{
		if (document == null || document.LineCount <= 0)
		{
			return 0;
		}
		int num = Math.Max(1, Math.Min(line, document.LineCount));
		DocumentLine lineByNumber = document.GetLineByNumber(num);
		int column2 = Math.Max(1, Math.Min(column, lineByNumber.Length + 1));
		return document.GetOffset(num, column2);
	}

	internal static bool RequiresCodeTextSynchronization(string editorText, string blockCodeText)
	{
		return !string.Equals(NormalizeCodeText(editorText), NormalizeCodeText(blockCodeText), StringComparison.Ordinal);
	}
}
