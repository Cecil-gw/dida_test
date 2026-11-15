using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Framework.Markdown;
using ticktick_WPF.Views.MarkDown.Colorizer;

namespace ticktick_WPF.Views.MarkDown;

internal sealed class CodeBlockAttachmentElementGenerator : VisualLineElementGenerator
{
	private const double AttachmentInlineFallbackWidthInset = 29.0;

	private readonly MarkDownEditor _editor;

	private readonly ConditionalWeakTable<MarkdownBlockNode, CodeBlockAttachmentControl> _controls = new ConditionalWeakTable<MarkdownBlockNode, CodeBlockAttachmentControl>();

	private readonly Dictionary<string, CodeBlockAttachmentControl> _controlsByAttachmentIdentity = new Dictionary<string, CodeBlockAttachmentControl>(StringComparer.Ordinal);

	private readonly Dictionary<string, IHighlightingDefinition> _highlightingDefinitions = new Dictionary<string, IHighlightingDefinition>(StringComparer.Ordinal);

	public CodeBlockAttachmentElementGenerator(MarkDownEditor editor)
	{
		_editor = editor;
	}

	public override int GetFirstInterestedOffset(int startOffset)
	{
		if (!TryMatch(startOffset, out var _, out var matchOffset))
		{
			return -1;
		}
		return matchOffset;
	}

	public override VisualLineElement ConstructElement(int offset)
	{
		if (!TryMatch(offset, out var block, out var matchOffset) || matchOffset != offset)
		{
			return null;
		}
		double attachmentWidth = GetAttachmentWidth();
		CodeBlockAttachmentControl orCreateControl = GetOrCreateControl(block);
		orCreateControl.UpdateBlock(block, attachmentWidth, GetHighlightingDefinition(block?.Language));
		double height = Math.Max(36.0 + Math.Max(1.0, _editor?.FontSize ?? 14.0), orCreateControl.AttachmentHeight);
		return new CodeBlockAttachmentElement(1, orCreateControl, attachmentWidth, height);
	}

	internal bool TryGetControlBounds(MarkdownBlockNode block, TextView textView, out Rect rect)
	{
		rect = Rect.Empty;
		if (block != null && TryGetControl(block, out var control))
		{
			return control.TryGetBounds(textView, out rect);
		}
		return false;
	}

	internal bool TryFocusCodeEditor(MarkdownBlockNode block, bool moveToEnd)
	{
		if (block != null && TryGetControl(block, out var control))
		{
			return control.TryFocusCodeEditor(moveToEnd);
		}
		return false;
	}

	internal bool IsCodeEditorKeyboardFocused(MarkdownBlockNode block)
	{
		if (block != null && TryGetControl(block, out var control))
		{
			return control.IsCodeEditorKeyboardFocused;
		}
		return false;
	}

	internal bool TryFocusCodeEditorPreservingCaret(MarkdownBlockNode block)
	{
		if (block != null && TryGetControl(block, out var control))
		{
			return control.TryFocusCodeEditorPreservingCaret();
		}
		return false;
	}

	internal bool TryRestoreCodeEditorFocus(MarkdownBlockNode block, int caretOffset, int selectionStart, int selectionLength, int? caretLine, int? caretColumn)
	{
		if (block != null && TryGetControl(block, out var control))
		{
			return control.TryRestoreCodeEditorFocus(caretOffset, selectionStart, selectionLength, caretLine, caretColumn);
		}
		return false;
	}

	internal bool TryGetFirstSearchMatchVerticalOffset(MarkdownBlockNode block, out double verticalOffset)
	{
		verticalOffset = 0.0;
		if (block != null && TryGetControl(block, out var control))
		{
			return control.TryGetFirstSearchMatchVerticalOffset(out verticalOffset);
		}
		return false;
	}

	private CodeBlockAttachmentControl GetOrCreateControl(MarkdownBlockNode block)
	{
		if (block == null)
		{
			return null;
		}
		if (_controls.TryGetValue(block, out var value))
		{
			return value;
		}
		string key = EnsureAttachmentIdentity(block);
		if (!_controlsByAttachmentIdentity.TryGetValue(key, out value))
		{
			PruneDetachedControlsIfNeeded();
			value = new CodeBlockAttachmentControl(_editor, block);
			_controlsByAttachmentIdentity[key] = value;
		}
		_controls.Add(block, value);
		return value;
	}

	private bool TryGetControl(MarkdownBlockNode block, out CodeBlockAttachmentControl control)
	{
		control = null;
		if (block == null)
		{
			return false;
		}
		if (_controls.TryGetValue(block, out control))
		{
			return true;
		}
		string codeBlockAttachmentIdentity = block.CodeBlockAttachmentIdentity;
		if (!string.IsNullOrEmpty(codeBlockAttachmentIdentity))
		{
			return _controlsByAttachmentIdentity.TryGetValue(codeBlockAttachmentIdentity, out control);
		}
		return false;
	}

	private static string EnsureAttachmentIdentity(MarkdownBlockNode block)
	{
		if (string.IsNullOrEmpty(block.CodeBlockAttachmentIdentity))
		{
			block.CodeBlockAttachmentIdentity = Guid.NewGuid().ToString("N");
		}
		return block.CodeBlockAttachmentIdentity;
	}

	private void PruneDetachedControlsIfNeeded()
	{
		List<MarkdownBlockNode> list = _editor?.GetSemanticBlocks()?.Where((MarkdownBlockNode block) => block != null && block.Kind == MarkdownBlockKind.CodeBlock).ToList();
		if (list == null || _controlsByAttachmentIdentity.Count <= list.Count + 8)
		{
			return;
		}
		HashSet<string> activeIdentities = new HashSet<string>(list.Select(EnsureAttachmentIdentity), StringComparer.Ordinal);
		foreach (string item in _controlsByAttachmentIdentity.Keys.Where((string identity) => !activeIdentities.Contains(identity)).ToList())
		{
			_controlsByAttachmentIdentity.Remove(item);
		}
	}

	private bool TryMatch(int startOffset, out MarkdownBlockNode block, out int matchOffset)
	{
		block = null;
		matchOffset = -1;
		TextDocument textDocument = _editor?.EditBox?.Document;
		DocumentLine documentLine = base.CurrentContext?.VisualLine?.FirstDocumentLine;
		if (textDocument == null || documentLine == null || documentLine.Offset != startOffset || documentLine.Length != 1 || textDocument.GetText(documentLine) != '\ue000'.ToString() || !_editor.TryGetSemanticBlockAtOffset(documentLine.Offset, out block) || !_editor.IsCodeBlockAttachment(block))
		{
			return false;
		}
		matchOffset = documentLine.Offset;
		return true;
	}

	private double GetAttachmentWidth()
	{
		double valueOrDefault = (_editor?.EditBox?.TextArea?.TextView?.ActualWidth).GetValueOrDefault();
		if (valueOrDefault > 1.0)
		{
			return Math.Max(80.0, valueOrDefault - 7.0);
		}
		double num = _editor?.ActualWidth ?? 0.0;
		if (num <= 0.0 && _editor != null && !double.IsNaN(_editor.Width))
		{
			num = _editor.Width;
		}
		return Math.Max(80.0, num - 29.0 - 7.0);
	}

	private IHighlightingDefinition GetHighlightingDefinition(string language)
	{
		if (!CodeBlockLanguageCatalog.TryGetHighlightingResourceKey(language, out var resourceKey))
		{
			return null;
		}
		MarkDownEditor editor = _editor;
		string text = ((editor != null && editor.IsDark) ? "dark" : "light");
		string key = text + "|" + resourceKey;
		if (!_highlightingDefinitions.TryGetValue(key, out var value))
		{
			value = new ThemedHighlightingDefinitionResolver(text).GetDefinition(resourceKey);
			_highlightingDefinitions[key] = value;
		}
		return value;
	}
}
