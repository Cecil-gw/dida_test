#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using Microsoft.IdentityModel.Tokens;
using ticktick_WPF.Framework.Markdown;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Views.MarkDown;

public static class EditorUtilities
{
	private static readonly List<string> MdStyleRegexList = new List<string> { "^#{1,6} .*", "^> .*", "^[-\\*\\+] .*", "^(\\d+)\\. .*", "^```", "^> .*", "^- [ ] .*", "^- [x] .*", "^- [X] .*" };

	private static MarkDownEditor TryGetSemanticHost(TextEditor editor)
	{
		return editor?.Tag as MarkDownEditor;
	}

	public static void SelectWordAt(this TextEditor editor, int offset)
	{
		if (offset >= 0 && offset < editor.Document.TextLength && IsWordPart(editor.Document.GetCharAt(offset)))
		{
			int num = offset;
			int i = offset;
			TextDocument document = editor.Document;
			while (num > 0 && IsWordPart(document.GetCharAt(num - 1)))
			{
				num--;
			}
			for (; i < document.TextLength - 1 && IsWordPart(document.GetCharAt(i + 1)); i++)
			{
			}
			editor.Select(num, i - num + 1);
		}
	}

	private static bool IsWordPart(char ch)
	{
		if (!char.IsLetterOrDigit(ch) && ch != '_')
		{
			return ch == '*';
		}
		return true;
	}

	public static void SelectHeader(this TextEditor editor, bool next)
	{
		int startat = editor.SelectionStart + (next ? editor.SelectionLength : 0);
		RegexOptions options = (RegexOptions)(2 | ((!next) ? 64 : 0));
		Regex regex = new Regex("^#{1,6}[^#].*", options);
		string input = MarkDownEditor.NormalizeDisplayTaskMarkersForPersistence(editor.Text);
		Match match = regex.Match(input, startat);
		if (!match.Success)
		{
			Notify.Beep();
			return;
		}
		editor.Select(match.Index, match.Length);
		TextLocation location = editor.Document.GetLocation(match.Index);
		editor.ScrollTo(location.Line, location.Column);
	}

	public static void ReplaceChar(this TextEditor editor, char origin, char revised)
	{
		editor.Document.Insert(editor.TextArea.Caret.Offset, "x");
	}

	public static void RemoveStyle(TextEditor editor)
	{
		int lineNumber = editor.Document.GetLineByOffset(editor.SelectionStart).LineNumber;
		MarkDownEditor markDownEditor = TryGetSemanticHost(editor);
		if (markDownEditor != null)
		{
			markDownEditor.RemoveStartText();
			editor.AdjustNumberedList(lineNumber);
			return;
		}
		if (editor.SelectedText.IsNullOrEmpty())
		{
			RemoveStyleByCaret(editor);
		}
		else
		{
			int lineNumber2 = editor.Document.GetLineByOffset(editor.SelectionStart).LineNumber;
			int lineNumber3 = editor.Document.GetLineByOffset(editor.SelectionStart + editor.SelectedText.Length).LineNumber;
			for (int i = lineNumber2; i <= lineNumber3; i++)
			{
				RemoveStyleByLineNumber(editor, i);
			}
		}
		editor.AdjustNumberedList(lineNumber);
	}

	public static void RemoveStyleByLineNumber(TextEditor editor, int lineNumber)
	{
		if (TryGetSemanticHost(editor) != null)
		{
			return;
		}
		DocumentLine lineByNumber = editor.Document.GetLineByNumber(lineNumber);
		if (lineByNumber == null)
		{
			return;
		}
		string text = editor.Document.GetText(lineByNumber.Offset, lineByNumber.Length);
		string text2 = text.Trim();
		string text3 = MarkDownEditor.NormalizeDisplayTaskMarkerLine(text2);
		if (!CheckIfIsStyleText(text2))
		{
			return;
		}
		if (MarkdownBlockSyntax.IsSplitLine(text3) || text3 == "```")
		{
			editor.Document.Remove(lineByNumber);
			editor.Document.Insert(lineByNumber.Offset, string.Empty);
			return;
		}
		int num = text3.IndexOf(" ", StringComparison.Ordinal);
		if (num > 0)
		{
			string text4 = text.Substring(0, text.Length - text.TrimStart().Length) + text3.Substring(num + 1);
			int offset = lineByNumber.Offset;
			editor.Document.Remove(lineByNumber);
			editor.Document.Insert(offset, text4);
		}
	}

	private static bool CheckIfIsStyleText(string line)
	{
		string text = MarkDownEditor.NormalizeDisplayTaskMarkerLine(line ?? string.Empty);
		if (MarkdownBlockSyntax.IsSplitLine(text))
		{
			return true;
		}
		string normalized = text.TrimStart();
		return (from style in MdStyleRegexList
			select new Regex(style) into pattern
			select pattern.Match(normalized)).Any((Match match) => match.Success);
	}

	public static void RemoveStyleByCaret(TextEditor editor)
	{
		if (TryGetSemanticHost(editor) != null)
		{
			return;
		}
		DocumentLine lineByOffset = editor.Document.GetLineByOffset(editor.CaretOffset);
		if (lineByOffset == null)
		{
			return;
		}
		string text = editor.Document.GetText(lineByOffset.Offset, lineByOffset.Length);
		string text2 = text.Trim();
		string text3 = MarkDownEditor.NormalizeDisplayTaskMarkerLine(text2);
		if (!CheckIfIsStyleText(text2))
		{
			return;
		}
		if (MarkdownBlockSyntax.IsSplitLine(text3) || text3 == "```")
		{
			editor.Document.Remove(lineByOffset);
			editor.Document.Insert(lineByOffset.Offset, string.Empty);
			return;
		}
		int num = text3.IndexOf(" ", StringComparison.Ordinal);
		if (num > 0)
		{
			string text4 = text.Substring(0, text.Length - text.TrimStart().Length) + text3.Substring(num + 1);
			editor.Document.Remove(lineByOffset);
			editor.Document.Insert(lineByOffset.Offset, text4);
		}
	}

	public static void AddLineText(TextEditor editor, string text)
	{
		if (TryGetSemanticHost(editor) != null)
		{
			return;
		}
		DocumentLine lineByOffset = editor.Document.GetLineByOffset(editor.CaretOffset);
		if (lineByOffset == null)
		{
			return;
		}
		string text2 = editor.Document.GetText(lineByOffset.Offset, lineByOffset.Length).Trim();
		if (string.IsNullOrEmpty(text2))
		{
			editor.Document.Remove(lineByOffset);
			editor.Document.Insert(lineByOffset.Offset, text + "\n");
		}
		else
		{
			editor.Document.Remove(lineByOffset.Offset, text2.Length);
			if (lineByOffset.NextLine == null)
			{
				editor.Document.Insert(lineByOffset.Offset, text2 + "\n" + text + "\n");
			}
			else
			{
				editor.Document.Insert(lineByOffset.Offset, text2 + "\n" + text);
			}
		}
		editor.TryFocus();
	}

	public static void AddCodeBlock(TextEditor editor)
	{
		MarkDownEditor markDownEditor = TryGetSemanticHost(editor);
		if (markDownEditor != null)
		{
			markDownEditor.InsertCodeBlock();
		}
		else
		{
			AddLineText(editor, MarkDownEditor.ConvertNormalizedBlockMarkerToDisplay("```"));
		}
	}

	public static void AddHorizontalLine(TextEditor editor)
	{
		MarkDownEditor markDownEditor = TryGetSemanticHost(editor);
		if (markDownEditor != null)
		{
			markDownEditor.InsertLine();
		}
		else
		{
			AddHorizontalLineText(editor, MarkDownEditor.ConvertNormalizedBlockMarkerToDisplay("---"));
		}
	}

	private static void AddHorizontalLineText(TextEditor editor, string text)
	{
		if (editor?.Document == null || string.IsNullOrEmpty(text))
		{
			return;
		}
		DocumentLine lineByOffset = editor.Document.GetLineByOffset(editor.CaretOffset);
		if (lineByOffset == null)
		{
			return;
		}
		bool num = string.IsNullOrWhiteSpace(editor.Document.GetText(lineByOffset.Offset, lineByOffset.Length));
		int num2 = (num ? lineByOffset.Offset : (lineByOffset.EndOffset + lineByOffset.DelimiterLength));
		string text2 = text + "\n";
		int val = num2 + text2.Length;
		if (!num)
		{
			if (lineByOffset.NextLine == null)
			{
				num2 = lineByOffset.EndOffset;
				text2 = "\n" + text + "\n";
				val = num2 + text2.Length;
			}
			else
			{
				text2 = text + "\n\n";
				val = num2 + text.Length + 1;
			}
		}
		editor.Document.Insert(num2, text2);
		editor.CaretOffset = Math.Max(0, Math.Min(val, editor.Document.TextLength));
		editor.TryFocus();
	}

	public static bool IsListTypeText(string content, out int offset, out int length)
	{
		content = MarkDownEditor.NormalizeDisplayTaskMarkerLine(content ?? string.Empty);
		if (!string.IsNullOrEmpty(content))
		{
			if (new Regex("^[ \\t]*> .*").IsMatch(content))
			{
				offset = content.IndexOf(">", StringComparison.Ordinal);
				length = 2;
				return true;
			}
			if (new Regex("^[ \\t]*- \\[[ xX]\\] .*").IsMatch(content))
			{
				offset = content.IndexOf("-", StringComparison.Ordinal);
				length = 6;
				return true;
			}
			if (new Regex("^[ \\t]*[-\\*\\+] .*").IsMatch(content))
			{
				offset = content.IndexOf("-", StringComparison.Ordinal);
				if (offset <= 0)
				{
					offset = content.IndexOf("*", StringComparison.Ordinal);
				}
				if (offset <= 0)
				{
					offset = content.IndexOf("+", StringComparison.Ordinal);
				}
				length = 2;
				return true;
			}
			Match match = new Regex("^[ \\t]*(\\d+)\\. .*").Match(content);
			if (match.Success)
			{
				string text = match.Groups[1].ToString();
				offset = content.IndexOf(".", StringComparison.Ordinal) - text.Length;
				length = 3 + text.Length - 1;
				return true;
			}
		}
		length = 0;
		offset = 0;
		return false;
	}

	public static async void AddRemoveTextInStart(TextEditor editor, string quote, int offset = -1)
	{
		string selectedText = editor.SelectedText;
		if (string.IsNullOrEmpty(selectedText) || (!selectedText.Contains("\n") && !selectedText.Contains("\r")))
		{
			if (offset == -1)
			{
				offset = editor.CaretOffset;
			}
			DocumentLine lineByOffset = editor.Document.GetLineByOffset(offset);
			if (lineByOffset == null)
			{
				return;
			}
			HandleLineList(editor, quote, lineByOffset);
		}
		else
		{
			AddOrRemoveSelectionQuote(editor, quote);
		}
		Task.Delay(100);
		editor.TryFocus();
	}

	private static void HandleLineList(TextEditor editor, string quote, DocumentLine line)
	{
		string text = editor.Document.GetText(line.Offset, line.Length);
		string text2 = MarkDownEditor.ConvertNormalizedBlockMarkerToDisplay(quote);
		string text3 = MarkDownEditor.NormalizeDisplayTaskMarkerLine(text).TrimStart();
		string text4 = text.Substring(0, text.Length - text.TrimStart().Length);
		if (IsListTypeText(text, out var offset, out var length))
		{
			if (offset < 0)
			{
				offset = 0;
			}
			editor.Document.Remove(line.Offset + offset, Math.Min(editor.Document.TextLength - line.Offset - offset, length));
			if (!text3.StartsWith(quote))
			{
				editor.Document.Insert(line.Offset, text4 + text2);
				editor.Document.Remove(line.Offset + (text4 + text2).Length, Math.Min(editor.Document.TextLength - line.Offset - (text4 + text2).Length, text4.Length));
			}
		}
		else
		{
			editor.Document.Insert(line.Offset, text2);
		}
	}

	public static void AddText(this TextEditor editor, string insert, bool focused, bool keepCurrentLine = false, int? insertOffset = null, int selectionLength = 0)
	{
		MarkDownEditor markDownEditor = TryGetSemanticHost(editor);
		if (markDownEditor != null)
		{
			if (!focused && !string.IsNullOrEmpty(editor.Text))
			{
				markDownEditor.InsertEditorText("\r\n" + insert);
			}
			else
			{
				markDownEditor.InsertEditorText(insert);
			}
		}
		else if (!focused)
		{
			if (insertOffset.HasValue)
			{
				int num = Math.Max(0, Math.Min(insertOffset.Value, editor.Document.TextLength));
				int num2 = Math.Max(0, Math.Min(selectionLength, editor.Document.TextLength - num));
				string text = insert;
				if (num2 == 0 && !keepCurrentLine)
				{
					int offset = ((editor.Document.TextLength != 0) ? Math.Max(0, Math.Min(num, editor.Document.TextLength - 1)) : 0);
					DocumentLine lineByOffset = editor.Document.GetLineByOffset(offset);
					if (!string.IsNullOrEmpty(editor.Document.GetText(lineByOffset.Offset, lineByOffset.Length)))
					{
						text = "\r\n" + text;
					}
				}
				if (num2 > 0)
				{
					editor.Document.Replace(num, num2, text);
				}
				else
				{
					editor.Document.Insert(num, text);
				}
				editor.CaretOffset = Math.Max(0, num + text.Length);
				editor.TryFocus();
			}
			else
			{
				int length = editor.Text.Length;
				DocumentLine lineByOffset2 = editor.Document.GetLineByOffset(length);
				string text2 = editor.Document.GetText(lineByOffset2.Offset, lineByOffset2.Length);
				string text3 = insert;
				if (!keepCurrentLine && !string.IsNullOrEmpty(text2))
				{
					text3 = "\r\n" + text3;
				}
				editor.Document.Insert(editor.Text.Length, text3);
				editor.CaretOffset = Math.Max(0, length + text3.Length);
				editor.TryFocus();
			}
		}
		else if (string.IsNullOrEmpty(editor.SelectedText))
		{
			editor.Document.Insert(editor.TextArea.Caret.Offset, insert);
			editor.TryFocus();
		}
		else
		{
			editor.Document.Replace(editor.SelectionStart, editor.SelectionLength, insert);
		}
	}

	public static void SurroundText(this TextEditor editor, string prefix, string postfix)
	{
		string selectedText = editor.SelectedText;
		if (!string.IsNullOrEmpty(selectedText))
		{
			editor.SelectedText = selectedText.SurroundWith(prefix, postfix);
		}
	}

	public static void AddRemoveText(this TextEditor editor, string quote, bool focused = false)
	{
		MarkDownEditor markDownEditor = TryGetSemanticHost(editor);
		if (markDownEditor != null)
		{
			switch (quote)
			{
			case "**":
				markDownEditor.Bold();
				return;
			case "*":
				markDownEditor.Italic();
				return;
			case "`":
				markDownEditor.Code();
				return;
			case "~":
				markDownEditor.UnderLine();
				return;
			case "~~":
				markDownEditor.StrokeLine();
				return;
			case "==":
			case "::":
				markDownEditor.Highlight();
				return;
			}
		}
		if (!focused)
		{
			int length = editor.Text.Length;
			DocumentLine lineByOffset = editor.Document.GetLineByOffset(length);
			string text = editor.Document.GetText(lineByOffset.Offset, lineByOffset.Length);
			string text2 = quote + quote;
			if (!string.IsNullOrEmpty(text))
			{
				text2 = "\r\n" + text2;
			}
			editor.Document.Insert(editor.Text.Length, text2);
			editor.CaretOffset = Math.Max(0, length + text2.Length - quote.Length);
			editor.TryFocus();
			return;
		}
		string selectedText = editor.SelectedText;
		if (string.IsNullOrEmpty(selectedText))
		{
			editor.SelectWordAt(editor.CaretOffset);
			selectedText = editor.SelectedText;
		}
		if (string.IsNullOrEmpty(selectedText))
		{
			editor.Document.Insert(editor.TextArea.Caret.Offset, quote + quote);
			editor.CaretOffset -= quote.Length;
			editor.TryFocus();
			return;
		}
		bool flag = selectedText.StartsWith(quote) && selectedText.EndsWith(quote);
		if (flag)
		{
			if (quote == "*" && selectedText.StartsWith("**") && selectedText.EndsWith("**") && (!selectedText.StartsWith("***") || !selectedText.EndsWith("***")))
			{
				flag = false;
			}
			if (quote == "~" && selectedText.StartsWith("~~") && selectedText.EndsWith("~~") && (!selectedText.StartsWith("~~~") || !selectedText.EndsWith("~~~")))
			{
				flag = false;
			}
		}
		editor.SelectedText = (flag ? selectedText.UnSurroundWith(quote) : selectedText.SurroundWith(quote));
	}

	public static bool Find(this TextEditor editor, Regex find)
	{
		try
		{
			bool flag = find.Options.HasFlag(RegexOptions.RightToLeft);
			int startat = (flag ? editor.SelectionStart : (editor.SelectionStart + editor.SelectionLength));
			Match match = find.Match(editor.Text, startat);
			if (!match.Success)
			{
				match = find.Match(editor.Text, flag ? editor.Text.Length : 0);
			}
			if (match.Success)
			{
				editor.Select(match.Index, match.Length);
				TextLocation location = editor.Document.GetLocation(match.Index);
				editor.ScrollTo(location.Line, location.Column);
			}
			return match.Success;
		}
		catch (Exception ex)
		{
			Trace.WriteLine(ex.ToString());
			return false;
		}
	}

	public static void AdjustNumberedList(this TextEditor editor, int? affectedStartLine = null)
	{
		MarkDownEditor markDownEditor = TryGetSemanticHost(editor);
		if (markDownEditor != null)
		{
			markDownEditor.AdjustOrderedListMarkers(affectedStartLine);
		}
		else
		{
			AdjustNumberedListCore(editor, affectedStartLine);
		}
	}

	internal static void AdjustNumberedListCore(TextEditor editor, int? affectedStartLine = null)
	{
		List<NumberedListOrder> list = new List<NumberedListOrder>();
		int lineCount = editor.LineCount;
		int line = editor.TextArea.Caret.Line;
		int recalculateFromLine = affectedStartLine ?? line;
		int num = Math.Max(1, Math.Min(lineCount, recalculateFromLine));
		Regex regex = new Regex("^\\s*(\\d+)\\. ", RegexOptions.Multiline);
		for (int num2 = num; num2 >= 1; num2--)
		{
			DocumentLine lineByNumber = editor.Document.GetLineByNumber(num2);
			string text = MarkDownEditor.NormalizeDisplayTaskMarkerLine(editor.Document.GetText(lineByNumber.Offset, lineByNumber.Length));
			Match match = regex.Match(text);
			if (!match.Success)
			{
				break;
			}
			string text2 = match.Groups[0].Value.Trim().Replace(".", string.Empty);
			int level = text.IndexOf(text2, StringComparison.Ordinal) / 4;
			if (int.TryParse(text2, out var result))
			{
				list.Add(new NumberedListOrder(level, result, num2));
			}
		}
		for (int i = num + 1; i <= lineCount; i++)
		{
			DocumentLine lineByNumber2 = editor.Document.GetLineByNumber(i);
			string text3 = MarkDownEditor.NormalizeDisplayTaskMarkerLine(editor.Document.GetText(lineByNumber2.Offset, lineByNumber2.Length));
			Match match2 = regex.Match(text3);
			if (!match2.Success)
			{
				break;
			}
			string text4 = match2.Groups[0].Value.Trim().Replace(".", string.Empty);
			int level2 = text3.IndexOf(text4, StringComparison.Ordinal) / 4;
			if (int.TryParse(text4, out var result2))
			{
				list.Add(new NumberedListOrder(level2, result2, i));
			}
		}
		if (list.Count > 0)
		{
			list = list.OrderBy((NumberedListOrder numberedListOrder) => numberedListOrder.LineNumber).ToList();
		}
		if (list.Count > 0)
		{
			int recalculateFromIndex = Math.Max(0, list.FindIndex((NumberedListOrder item) => item.LineNumber >= recalculateFromLine));
			List<int> list2 = CalculateOrderedListNumbers(list.Select((NumberedListOrder item) => item.Level).ToList(), list.Select((NumberedListOrder item) => item.Number).ToList(), recalculateFromIndex);
			for (int num3 = 0; num3 < list.Count && num3 < list2.Count; num3++)
			{
				list[num3].Number = list2[num3];
			}
		}
		if (!list.Any())
		{
			return;
		}
		editor.BeginChange();
		try
		{
			int caretOffset = editor.CaretOffset;
			foreach (NumberedListOrder item in list)
			{
				DocumentLine lineByNumber3 = editor.Document.GetLineByNumber(item.LineNumber);
				string text5 = editor.Document.GetText(lineByNumber3.Offset, lineByNumber3.Length);
				string text6 = MarkDownEditor.NormalizeDisplayTaskMarkerLine(text5);
				Match match3 = regex.Match(text6);
				if (match3.Success)
				{
					int num4 = text5.Length - text5.TrimStart().Length;
					string text7 = string.Concat((num4 > 0) ? text5.Substring(0, num4) : string.Empty, str2: text6.Substring(match3.Length), str1: MarkDownEditor.BuildDisplayOrderedListMarker(item.Number.ToString()));
					editor.Document.Replace(lineByNumber3.Offset, lineByNumber3.Length, text7);
				}
			}
			editor.CaretOffset = caretOffset;
		}
		catch (Exception)
		{
		}
		finally
		{
			editor.EndChange();
		}
	}

	public static bool Replace(this TextEditor editor, Regex find, string replace)
	{
		try
		{
			string text = editor.Text.Substring(editor.SelectionStart, editor.SelectionLength);
			Match match = find.Match(text);
			bool flag = false;
			if (match.Success && match.Index == 0 && match.Length == text.Length)
			{
				string text2 = match.Result(replace);
				editor.Document.Replace(editor.SelectionStart, editor.SelectionLength, text2);
				flag = true;
			}
			if (!editor.Find(find) && !flag)
			{
				Notify.Beep();
			}
			return flag;
		}
		catch (Exception ex)
		{
			Trace.WriteLine(ex.ToString());
			return false;
		}
	}

	public static void ReplaceAll(this TextEditor editor, Regex find, string replace)
	{
		try
		{
			int num = 0;
			editor.BeginChange();
			foreach (Match item in find.Matches(editor.Text))
			{
				string text = item.Result(replace);
				editor.Document.Replace(num + item.Index, item.Length, text);
				num += text.Length - item.Length;
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.ToString());
		}
		finally
		{
			editor.EndChange();
		}
	}

	public static bool ErrorBeep()
	{
		Notify.Beep();
		return false;
	}

	public static List<int> CalculateOrderedListNumbers(IReadOnlyList<int> levels, IReadOnlyList<int> originalNumbers, int recalculateFromIndex)
	{
		List<int> list = new List<int>();
		if (levels == null || originalNumbers == null || levels.Count == 0 || levels.Count != originalNumbers.Count)
		{
			return list;
		}
		for (int i = 0; i < levels.Count; i++)
		{
			if (i < recalculateFromIndex)
			{
				list.Add(originalNumbers[i]);
				continue;
			}
			int item = 1;
			for (int num = i - 1; num >= 0; num--)
			{
				if (levels[num] == levels[i])
				{
					item = list[num] + 1;
					break;
				}
				if (levels[num] < levels[i])
				{
					break;
				}
			}
			if (i == 0 && recalculateFromIndex == 0)
			{
				item = Math.Max(1, originalNumbers[i]);
			}
			list.Add(item);
		}
		return list;
	}

	public static void MoveSegmentUp(TextEditor textEditor)
	{
		TextArea textArea = textEditor.TextArea;
		TextDocument document = textEditor.Document;
		Selection selection = textArea.Selection;
		int num;
		DocumentLine previousLine;
		if (selection != null && selection.StartPosition.Line > 1)
		{
			num = Math.Max(selection.StartPosition.Line, selection.EndPosition.Line);
			previousLine = document.GetLineByNumber(Math.Min(selection.StartPosition.Line, selection.EndPosition.Line)).PreviousLine;
		}
		else
		{
			num = textArea.Caret.Line;
			if (num == 1)
			{
				return;
			}
			previousLine = document.GetLineByNumber(num).PreviousLine;
		}
		textEditor.BeginChange();
		try
		{
			string text = document.GetText(previousLine.Offset, previousLine.TotalLength);
			document.Remove(previousLine.Offset, previousLine.TotalLength);
			int offset = ((num >= document.LineCount) ? document.TextLength : document.GetOffset(num, 0));
			document.Insert(offset, text);
			textArea.Caret.Line = num - 1;
			textArea.Caret.BringCaretToView();
		}
		finally
		{
			textEditor.EndChange();
			textArea.TextView.Redraw(DispatcherPriority.ApplicationIdle);
		}
	}

	public static void MoveSegmentDown(TextEditor textEditor)
	{
		TextArea textArea = textEditor.TextArea;
		TextDocument document = textEditor.Document;
		Selection selection = textArea.Selection;
		int num;
		DocumentLine nextLine;
		if (selection != null && selection.StartPosition.Line > 0 && selection.EndPosition.Line < textEditor.LineCount)
		{
			num = Math.Min(selection.StartPosition.Line, selection.EndPosition.Line);
			nextLine = document.GetLineByNumber(Math.Max(selection.StartPosition.Line, selection.EndPosition.Line)).NextLine;
		}
		else
		{
			num = textArea.Caret.Line;
			if (num >= textEditor.LineCount)
			{
				return;
			}
			nextLine = document.GetLineByNumber(num).NextLine;
		}
		textEditor.BeginChange();
		try
		{
			string text = document.GetText(nextLine.Offset, nextLine.TotalLength);
			document.Remove(nextLine.Offset, nextLine.TotalLength);
			document.Insert(document.GetOffset(num, 0), text);
			textArea.Caret.Line = num + 1;
			textArea.Caret.BringCaretToView();
		}
		finally
		{
			textEditor.EndChange();
			textArea.TextView.Redraw(DispatcherPriority.ApplicationIdle);
		}
	}

	public static void AddOrRemoveHeading(TextEditor textEditor, string quote, int offset = -1)
	{
		MarkDownEditor markDownEditor = TryGetSemanticHost(textEditor);
		if (markDownEditor != null)
		{
			Match match = Regex.Match(MarkDownEditor.NormalizeDisplayTaskMarkerLine(quote ?? string.Empty), "^(#{1,6}) ");
			if (match.Success)
			{
				markDownEditor.InsertHeader(match.Groups[1].Value.Length);
				return;
			}
		}
		Regex regex = new Regex("^#{1,6} .*");
		string text = MarkDownEditor.ConvertNormalizedBlockMarkerToDisplay(quote);
		string selectedText = textEditor.SelectedText;
		TextDocument document = textEditor.Document;
		if (string.IsNullOrEmpty(selectedText) || !selectedText.Contains("\r\n"))
		{
			if (offset == -1)
			{
				offset = textEditor.CaretOffset;
			}
			DocumentLine lineByOffset = textEditor.Document.GetLineByOffset(offset);
			if (lineByOffset == null)
			{
				return;
			}
			string text2 = MarkDownEditor.NormalizeDisplayTaskMarkerLine(textEditor.Document.GetText(lineByOffset.Offset, lineByOffset.Length));
			Match match2 = regex.Match(text2);
			if (match2.Success)
			{
				document.Remove(lineByOffset);
				if (match2.Value.Substring(0, match2.Value.IndexOf(" ", StringComparison.Ordinal)) != quote.TrimEnd())
				{
					document.Insert(lineByOffset.Offset, text + text2.Substring(text2.IndexOf(" ", StringComparison.Ordinal)).TrimStart());
				}
				else
				{
					document.Insert(lineByOffset.Offset, text2.Substring(text2.IndexOf(" ", StringComparison.Ordinal)).TrimStart());
				}
			}
			else
			{
				textEditor.Document.Insert(lineByOffset.Offset, text);
			}
			textEditor.TryFocus();
		}
		else
		{
			BatchAddHeading(textEditor, quote);
		}
	}

	private static void BatchAddHeading(TextEditor textEditor, string quote)
	{
		TextDocument document = textEditor.Document;
		Regex regex = new Regex("^#{1,6} .*");
		string text = MarkDownEditor.ConvertNormalizedBlockMarkerToDisplay(quote);
		Selection selection = textEditor.TextArea.Selection;
		int num = Math.Min(selection.StartPosition.Line, selection.EndPosition.Line);
		int num2 = Math.Max(selection.StartPosition.Line, selection.EndPosition.Line);
		if (num == 0)
		{
			return;
		}
		textEditor.BeginChange();
		try
		{
			foreach (int item in Enumerable.Range(num, num2 - num + 1))
			{
				DocumentLine lineByNumber = document.GetLineByNumber(item);
				int offset = lineByNumber.Offset;
				string text2 = document.GetText(lineByNumber);
				if (string.IsNullOrWhiteSpace(text2))
				{
					continue;
				}
				string text3 = MarkDownEditor.NormalizeDisplayTaskMarkerLine(text2);
				if (regex.Match(text3).Success)
				{
					document.Remove(lineByNumber);
					if (!text3.StartsWith(quote))
					{
						document.Insert(offset, text + text3.Substring(text3.IndexOf(" ", StringComparison.Ordinal)).TrimStart());
					}
					else
					{
						document.Insert(offset, text3.Substring(text3.IndexOf(" ", StringComparison.Ordinal)).TrimStart());
					}
				}
				else
				{
					document.Insert(document.GetOffset(item, 0), text);
				}
			}
		}
		finally
		{
			textEditor.EndChange();
		}
	}

	private static void AddOrRemoveSelectionQuote(TextEditor editor, string quote)
	{
		Selection selection = editor.TextArea.Selection;
		int num = Math.Min(selection.StartPosition.Line, selection.EndPosition.Line);
		if (num == 0)
		{
			return;
		}
		int num2 = Math.Max(selection.StartPosition.Line, selection.EndPosition.Line);
		TextDocument document = editor.Document;
		editor.BeginChange();
		try
		{
			foreach (int item in Enumerable.Range(num, num2 - num + 1))
			{
				if (item <= document.LineCount)
				{
					DocumentLine lineByNumber = document.GetLineByNumber(item);
					HandleLineList(editor, quote, lineByNumber);
				}
			}
		}
		finally
		{
			editor.EndChange();
		}
	}

	public static void ConvertSelectionToList(TextEditor textEditor)
	{
		MarkDownEditor markDownEditor = TryGetSemanticHost(textEditor);
		if (markDownEditor != null)
		{
			markDownEditor.InsertNumberedList();
			return;
		}
		Selection selection = textEditor.TextArea.Selection;
		int num = Math.Min(selection.StartPosition.Line, selection.EndPosition.Line);
		if (num == 0)
		{
			return;
		}
		int num2 = Math.Max(selection.StartPosition.Line, selection.EndPosition.Line);
		TextDocument document = textEditor.Document;
		textEditor.BeginChange();
		int num3 = 1;
		try
		{
			foreach (int item in Enumerable.Range(num, num2 - num + 1))
			{
				DocumentLine lineByNumber = document.GetLineByNumber(item);
				string text = document.GetText(lineByNumber);
				if (string.IsNullOrWhiteSpace(text))
				{
					continue;
				}
				if (IsListTypeText(text, out var offset, out var length))
				{
					if (offset < 0)
					{
						offset = 0;
					}
					textEditor.Document.Remove(lineByNumber.Offset + offset, length);
					if (!new Regex("^[ \\t]*(\\d+)\\. .*").Match(MarkDownEditor.NormalizeDisplayTaskMarkerLine(text)).Success)
					{
						document.Insert(lineByNumber.Offset, MarkDownEditor.BuildDisplayOrderedListMarker(num3++.ToString()));
					}
				}
				else
				{
					document.Insert(lineByNumber.Offset, MarkDownEditor.BuildDisplayOrderedListMarker(num3++.ToString()));
				}
			}
		}
		finally
		{
			textEditor.EndChange();
			textEditor.AdjustNumberedList(num);
		}
	}

	public static void ScrollToOffset(TextEditor editor, int offset)
	{
		DocumentLine lineByOffset = editor.Document.GetLineByOffset(offset);
		if (lineByOffset != null)
		{
			editor.ScrollToLine(lineByOffset.LineNumber);
			editor.CaretOffset = offset;
		}
	}

	public static void ScrollToLine(TextEditor editor, int line)
	{
		line = Math.Min(Math.Max(1, editor.Document.LineCount), Math.Max(line, 1));
		editor.ScrollToLine(line);
		int offset = editor.Document.GetOffset(line, 0);
		editor.CaretOffset = offset;
	}

	public static void InsertBlockQuote(TextEditor textEditor)
	{
		MarkDownEditor markDownEditor = TryGetSemanticHost(textEditor);
		if (markDownEditor != null)
		{
			markDownEditor.Quote();
			return;
		}
		TextArea textArea = textEditor.TextArea;
		TextDocument document = textEditor.Document;
		Selection selection = textEditor.TextArea.Selection;
		int num = Math.Min(selection.StartPosition.Line, selection.EndPosition.Line);
		if (num == 0)
		{
			num = textArea.Caret.Line;
		}
		int num2 = Math.Max(selection.StartPosition.Line, selection.EndPosition.Line);
		if (num2 == 0)
		{
			num2 = textArea.Caret.Line;
		}
		textEditor.BeginChange();
		try
		{
			foreach (int item in Enumerable.Range(num, num2 - num + 1))
			{
				int offset = document.GetOffset(item, 0);
				document.Insert(offset, MarkDownEditor.ConvertNormalizedBlockMarkerToDisplay("> "));
			}
		}
		finally
		{
			textEditor.EndChange();
		}
	}

	public static void InsertHyperlink(TextEditor editor, string link)
	{
		MarkDownEditor markDownEditor = TryGetSemanticHost(editor);
		if (markDownEditor != null)
		{
			markDownEditor.InsertHyperlinkText(link);
			return;
		}
		string linkTextForInsert = GetLinkTextForInsert(link);
		if (!string.IsNullOrEmpty(linkTextForInsert))
		{
			editor.Document.Replace(editor.SelectionStart, editor.SelectionLength, linkTextForInsert);
		}
	}

	public static string GetLinkTextForInsert(string link)
	{
		if (string.IsNullOrWhiteSpace(link))
		{
			return string.Empty;
		}
		string[] array = link.Split(new char[1] { '"' }, 2);
		if (array.Length != 1)
		{
			return "[" + array[1].Trim('"', ' ') + "](" + array[0].Trim() + ")";
		}
		return array[0].Trim() ?? "";
	}

	public static void ReplaceLink(TextEditor editorEditBox, string original, string revised, LinkLineInfoModel model)
	{
		MarkDownEditor markDownEditor = TryGetSemanticHost(editorEditBox);
		if (markDownEditor != null)
		{
			markDownEditor.ReplaceLinkText(original, revised, model);
			return;
		}
		int caretOffset = editorEditBox.CaretOffset;
		string text = editorEditBox.Text;
		int num = -1;
		if (model != null && model.TryResolveStartIndex(out var startIndex))
		{
			bool num2 = model.StartIndexProvider != null;
			num = ((startIndex < 1 || startIndex > text.Length) ? text.IndexOf(original, StringComparison.Ordinal) : ((model.Source != LinkInfoSource.TextLineButton) ? text.IndexOf(original, startIndex - 1, StringComparison.Ordinal) : text.Substring(0, startIndex).LastIndexOf("[" + model.Text + "](", StringComparison.Ordinal)));
			if ((!num2 || (num >= 0 && startIndex >= num && startIndex <= num + original.Length)) && num >= 0)
			{
				editorEditBox.Document.Replace(num, original.Length, revised ?? string.Empty);
				int caretOffset2 = ((caretOffset < num) ? caretOffset : Math.Min(editorEditBox.Text.Length, num + Math.Max(0, revised?.Length ?? 0)));
				editorEditBox.CaretOffset = caretOffset2;
			}
		}
	}

	public static void OutIndentList(TextEditor textEditor)
	{
		MarkDownEditor markDownEditor = TryGetSemanticHost(textEditor);
		if (markDownEditor != null)
		{
			markDownEditor.DeIndent();
			return;
		}
		TextArea textArea = textEditor.TextArea;
		TextDocument document = textEditor.Document;
		Selection selection = textEditor.TextArea.Selection;
		int num = Math.Min(selection.StartPosition.Line, selection.EndPosition.Line);
		if (num == 0)
		{
			num = textArea.Caret.Line;
		}
		int num2 = Math.Max(selection.StartPosition.Line, selection.EndPosition.Line);
		if (num2 == 0)
		{
			num2 = textArea.Caret.Line;
		}
		textEditor.BeginChange();
		try
		{
			foreach (int item in Enumerable.Range(num, num2 - num + 1))
			{
				DocumentLine lineByNumber = document.GetLineByNumber(item);
				int offset = lineByNumber.Offset;
				string text = document.GetText(lineByNumber);
				if (text.StartsWith("    "))
				{
					text = text.Substring(4);
				}
				else if (text.StartsWith("\t"))
				{
					text = text.Substring(1);
				}
				document.Remove(lineByNumber);
				document.Insert(offset, text);
			}
		}
		finally
		{
			textEditor.EndChange();
			textEditor.AdjustNumberedList(num);
		}
	}

	public static void IndentList(TextEditor textEditor)
	{
		MarkDownEditor markDownEditor = TryGetSemanticHost(textEditor);
		if (markDownEditor != null)
		{
			markDownEditor.Indent();
			return;
		}
		TextArea textArea = textEditor.TextArea;
		TextDocument document = textEditor.Document;
		Selection selection = textEditor.TextArea.Selection;
		int num = Math.Min(selection.StartPosition.Line, selection.EndPosition.Line);
		if (num == 0)
		{
			num = textArea.Caret.Line;
		}
		int num2 = Math.Max(selection.StartPosition.Line, selection.EndPosition.Line);
		if (num2 == 0)
		{
			num2 = textArea.Caret.Line;
		}
		textEditor.BeginChange();
		try
		{
			foreach (int item in Enumerable.Range(num, num2 - num + 1))
			{
				int offset = document.GetOffset(item, 0);
				document.Insert(offset, "    ");
			}
		}
		finally
		{
			textEditor.EndChange();
			textEditor.AdjustNumberedList(num);
		}
	}
}
