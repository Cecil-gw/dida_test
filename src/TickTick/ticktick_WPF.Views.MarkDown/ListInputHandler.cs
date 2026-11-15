using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;

namespace ticktick_WPF.Views.MarkDown;

public static class ListInputHandler
{
	public static readonly Regex UnorderedListCheckboxPattern = new Regex("^[ \\t]*(?:[ ]{0,3}[-\\*\\+][ ]{1,3}\\[[ xX]\\]) ", RegexOptions.Compiled);

	public static readonly Regex OrderedListPattern = new Regex("^[ \\t]*[ ]{0,3}(\\d+)\\. ", RegexOptions.Compiled);

	public static readonly Regex UnorderedListPattern = new Regex("^[ \\t]*(?:[ ]{0,3}[-\\*\\+]) ", RegexOptions.Compiled);

	public static readonly Regex BlockQuotePattern = new Regex("^[ \\t]*(?:[ ]{0,3}>) ", RegexOptions.Compiled);

	public static readonly Regex ImageRegex = new Regex("!\\[([^\\)]+)\\]\\(([^\\)]+)\\)");

	public static void TryRemoveEmptyLineOnDelete(TextEditor editor)
	{
		if (editor.CaretOffset == 0)
		{
			TryRemoveEmptyChar(editor);
			editor.SelectionStart = 0;
		}
	}

	private static void TryRemoveEmptyChar(TextEditor editor)
	{
		TextDocument document = editor.Document;
		DocumentLine lineByOffset = document.GetLineByOffset(editor.SelectionStart);
		if (lineByOffset != null && lineByOffset.NextLine != null && string.IsNullOrEmpty(document.GetText(0, lineByOffset.Length).Trim()))
		{
			editor.Document.Remove(editor.CaretOffset, lineByOffset.Length + 1);
		}
	}

	public static void AdjustList(TextEditor editor, bool lineCountIncreased)
	{
		TextDocument document = editor.Document;
		DocumentLine line = document.GetLineByOffset(editor.SelectionStart)?.PreviousLine ?? document.GetLineByOffset(editor.SelectionStart);
		if (line == null)
		{
			return;
		}
		string text = document.GetText(line.Offset, line.Length);
		string normalizedText = MarkDownEditor.NormalizeDisplayTaskMarkerLine(text);
		DocumentLine nextLine = line.NextLine;
		string nextText = string.Empty;
		if (nextLine != null)
		{
			nextText = document.GetText(nextLine.Offset, nextLine.Length);
		}
		string normalizedNextText = MarkDownEditor.NormalizeDisplayTaskMarkerLine(nextText);
		if (!string.IsNullOrEmpty(nextText) && ImageRegex.Match(nextText).Value == nextText)
		{
			return;
		}
		Action<string, IEnumerable<Tuple<Regex, Action<Match>>>> obj = delegate(string txt, IEnumerable<Tuple<Regex, Action<Match>>> patterns)
		{
			var anon = patterns.Select((Tuple<Regex, Action<Match>> pattern) => new
			{
				Match = pattern.Item1.Match(txt),
				Action = pattern.Item2
			}).FirstOrDefault(ma => ma.Match.Success);
			anon?.Action(anon.Match);
		};
		Action<Action> ifIncreased = delegate(Action func)
		{
			if (lineCountIncreased)
			{
				func();
			}
		};
		document.BeginUpdate();
		obj(text, new Tuple<Regex, Action<Match>>[4]
		{
			new Tuple<Regex, Action<Match>>(UnorderedListCheckboxPattern, delegate(Match m)
			{
				ifIncreased(delegate
				{
					string text2 = m.Groups[0].Value.TrimStart();
					if (!(normalizedText.TrimStart() == text2) || !string.IsNullOrEmpty(normalizedNextText.Trim()))
					{
						if (document.GetLineByOffset(editor.SelectionStart).Offset + nextText.Length - nextText.TrimStart().Length == editor.SelectionStart)
						{
							document.Insert(editor.SelectionStart, MarkDownEditor.NormalizePersistenceTextForEditorDisplay(m.Groups[0].Value.Replace("x", " ").TrimStart()));
						}
					}
					else
					{
						RetreatOrRemoveEmptyListLine(editor, document, line, text2, adjustOrderedList: false);
					}
				});
			}),
			new Tuple<Regex, Action<Match>>(UnorderedListPattern, delegate(Match m)
			{
				ifIncreased(delegate
				{
					string text2 = m.Groups[0].Value.TrimStart();
					if (!(normalizedText.TrimStart() == text2) || !string.IsNullOrEmpty(normalizedNextText.Trim()))
					{
						document.Insert(editor.SelectionStart, MarkDownEditor.NormalizePersistenceTextForEditorDisplay(m.Groups[0].Value.TrimStart()));
					}
					else
					{
						RetreatOrRemoveEmptyListLine(editor, document, line, text2, adjustOrderedList: false);
					}
				});
			}),
			new Tuple<Regex, Action<Match>>(OrderedListPattern, delegate(Match m)
			{
				if (int.TryParse(m.Groups[1].Value, out var result))
				{
					if (!(normalizedText.TrimStart() == result + ". ") || !string.IsNullOrEmpty(normalizedNextText.Trim()))
					{
						if (lineCountIncreased)
						{
							result++;
							document.Insert(editor.SelectionStart, MarkDownEditor.BuildDisplayOrderedListMarker(result.ToString()));
							line = line.NextLine;
						}
					}
					else
					{
						RetreatOrRemoveEmptyListLine(editor, document, line, result + ". ", adjustOrderedList: true);
					}
				}
			}),
			new Tuple<Regex, Action<Match>>(BlockQuotePattern, delegate(Match m)
			{
				ifIncreased(delegate
				{
					string text2 = m.Groups[0].Value.TrimStart();
					if (!(normalizedText.TrimStart() == text2) || !string.IsNullOrEmpty(normalizedNextText.Trim()))
					{
						document.Insert(editor.SelectionStart, MarkDownEditor.NormalizePersistenceTextForEditorDisplay(m.Groups[0].Value.TrimStart()));
					}
					else
					{
						RemoveEmptyLine(editor, document, line);
					}
				});
			})
		});
		editor.AdjustNumberedList();
		document.EndUpdate();
	}

	public static bool TryHandleEnter(TextEditor editor, out bool lineCountChanged)
	{
		lineCountChanged = false;
		if (editor == null || editor.SelectionLength > 0)
		{
			return false;
		}
		TextDocument document = editor.Document;
		int caretOffset = editor.CaretOffset;
		DocumentLine lineByOffset = document.GetLineByOffset(caretOffset);
		if (lineByOffset == null || caretOffset != lineByOffset.EndOffset)
		{
			return false;
		}
		string lineText = MarkDownEditor.NormalizeDisplayTaskMarkerLine(document.GetText(lineByOffset.Offset, lineByOffset.Length));
		if (TryMatchOrderedListLine(lineText, out var match))
		{
			HandleOrderedListEnter(editor, lineByOffset, lineText, match, out lineCountChanged);
			return true;
		}
		if (TryMatchListLine(lineText, UnorderedListCheckboxPattern, out var match2))
		{
			HandlePlainListEnter(editor, lineByOffset, lineText, match2.Groups[0].Value.TrimStart(), out lineCountChanged);
			return true;
		}
		if (TryMatchListLine(lineText, UnorderedListPattern, out var match3))
		{
			HandlePlainListEnter(editor, lineByOffset, lineText, match3.Groups[0].Value.TrimStart(), out lineCountChanged);
			return true;
		}
		if (TryMatchListLine(lineText, BlockQuotePattern, out var match4))
		{
			HandleBlockQuoteEnter(editor, lineByOffset, lineText, match4.Groups[0].Value.TrimStart(), out lineCountChanged);
			return true;
		}
		return false;
	}

	private static void RemoveEmptyLine(TextEditor editor, TextDocument document, DocumentLine line)
	{
		document.Remove(line);
		int num = editor.CaretOffset - 1;
		if (num >= 0)
		{
			editor.CaretOffset = num;
			editor.Document.Remove(editor.CaretOffset, 1);
		}
	}

	public static bool TryBuildRetreatedListLine(string lineText, string marker, out string replacementText)
	{
		replacementText = null;
		if (string.IsNullOrEmpty(lineText) || string.IsNullOrEmpty(marker))
		{
			return false;
		}
		string text = lineText.Substring(0, lineText.Length - lineText.TrimStart().Length);
		if (!string.Equals(lineText.TrimStart(), marker, StringComparison.Ordinal))
		{
			return false;
		}
		if (string.IsNullOrEmpty(text))
		{
			replacementText = string.Empty;
			return true;
		}
		replacementText = RemoveOneIndentLevel(text) + marker;
		return true;
	}

	private static void RetreatOrRemoveEmptyListLine(TextEditor editor, TextDocument document, DocumentLine line, string marker, bool adjustOrderedList)
	{
		if (!TryBuildRetreatedListLine(document.GetText(line.Offset, line.Length), marker, out var replacementText))
		{
			RemoveEmptyLine(editor, document, line);
			return;
		}
		document.Replace(line.Offset, line.Length, MarkDownEditor.NormalizePersistenceTextForEditorDisplay(replacementText));
		if (line.DelimiterLength > 0 && !string.IsNullOrEmpty(replacementText))
		{
			document.Remove(line.EndOffset, line.DelimiterLength);
		}
		editor.CaretOffset = line.Offset + replacementText.Length;
		if (adjustOrderedList)
		{
			editor.AdjustNumberedList();
		}
	}

	private static string RemoveOneIndentLevel(string indent)
	{
		if (string.IsNullOrEmpty(indent))
		{
			return string.Empty;
		}
		if (indent.StartsWith("\t", StringComparison.Ordinal))
		{
			return indent.Substring(1);
		}
		if (indent.Length >= 4)
		{
			return indent.Substring(4);
		}
		return string.Empty;
	}

	private static void HandlePlainListEnter(TextEditor editor, DocumentLine line, string lineText, string marker, out bool lineCountChanged)
	{
		lineCountChanged = false;
		TextDocument document = editor.Document;
		if (TryBuildRetreatedListLine(lineText, marker, out var replacementText))
		{
			string text = MarkDownEditor.NormalizePersistenceTextForEditorDisplay(replacementText);
			document.Replace(line.Offset, line.Length, text);
			editor.CaretOffset = line.Offset + text.Length;
		}
		else
		{
			string leadingWhitespace = GetLeadingWhitespace(lineText);
			string text2 = MarkDownEditor.NormalizePersistenceTextForEditorDisplay(marker);
			document.Insert(line.EndOffset, Environment.NewLine + leadingWhitespace + text2);
			editor.CaretOffset = line.EndOffset + Environment.NewLine.Length + leadingWhitespace.Length + text2.Length;
			lineCountChanged = true;
		}
	}

	private static void HandleOrderedListEnter(TextEditor editor, DocumentLine line, string lineText, Match match, out bool lineCountChanged)
	{
		lineCountChanged = false;
		TextDocument document = editor.Document;
		string marker = match.Groups[0].Value.TrimStart();
		if (TryBuildRetreatedListLine(lineText, marker, out var replacementText))
		{
			string text = MarkDownEditor.NormalizePersistenceTextForEditorDisplay(replacementText);
			document.Replace(line.Offset, line.Length, text);
			editor.CaretOffset = line.Offset + text.Length;
			editor.AdjustNumberedList(line.LineNumber);
		}
		else
		{
			string leadingWhitespace = GetLeadingWhitespace(lineText);
			string text2 = MarkDownEditor.BuildDisplayOrderedListMarker((int.Parse(match.Groups[1].Value) + 1).ToString());
			document.Insert(line.EndOffset, Environment.NewLine + leadingWhitespace + text2);
			editor.CaretOffset = line.EndOffset + Environment.NewLine.Length + leadingWhitespace.Length + text2.Length;
			editor.AdjustNumberedList(line.LineNumber);
			lineCountChanged = true;
		}
	}

	private static void HandleBlockQuoteEnter(TextEditor editor, DocumentLine line, string lineText, string marker, out bool lineCountChanged)
	{
		lineCountChanged = false;
		TextDocument document = editor.Document;
		if (TryBuildRetreatedBlockQuoteLine(lineText, marker, out var replacementText))
		{
			string text = MarkDownEditor.NormalizePersistenceTextForEditorDisplay(replacementText);
			document.Replace(line.Offset, line.Length, text);
			editor.CaretOffset = line.Offset + text.Length;
		}
		else
		{
			string leadingWhitespace = GetLeadingWhitespace(lineText);
			string text2 = MarkDownEditor.NormalizePersistenceTextForEditorDisplay(marker);
			document.Insert(line.EndOffset, Environment.NewLine + leadingWhitespace + text2);
			editor.CaretOffset = line.EndOffset + Environment.NewLine.Length + leadingWhitespace.Length + text2.Length;
			lineCountChanged = true;
		}
	}

	public static bool TryBuildRetreatedBlockQuoteLine(string lineText, string marker, out string replacementText)
	{
		replacementText = null;
		if (string.IsNullOrEmpty(lineText) || string.IsNullOrEmpty(marker))
		{
			return false;
		}
		string leadingWhitespace = GetLeadingWhitespace(lineText);
		if (!string.Equals(lineText.TrimStart(), marker, StringComparison.Ordinal))
		{
			return false;
		}
		replacementText = (string.IsNullOrEmpty(leadingWhitespace) ? string.Empty : leadingWhitespace);
		return true;
	}

	private static bool TryMatchListLine(string lineText, Regex pattern, out Match match)
	{
		match = pattern.Match(lineText);
		if (match.Success)
		{
			return match.Index == 0;
		}
		return false;
	}

	private static bool TryMatchOrderedListLine(string lineText, out Match match)
	{
		match = OrderedListPattern.Match(lineText);
		if (match.Success)
		{
			return match.Index == 0;
		}
		return false;
	}

	private static string GetLeadingWhitespace(string text)
	{
		int num = text.Length - text.TrimStart().Length;
		if (num <= 0)
		{
			return string.Empty;
		}
		return text.Substring(0, num);
	}
}
