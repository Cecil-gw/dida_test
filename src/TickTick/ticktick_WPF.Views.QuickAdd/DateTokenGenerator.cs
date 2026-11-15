using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;
using ticktick_WPF.Util.DateParser;

namespace ticktick_WPF.Views.QuickAdd;

public class DateTokenGenerator : VisualLineElementGenerator
{
	private readonly List<string> _ignoreTokens = new List<string>();

	private readonly IDateParseBox _parseTextBox;

	private readonly List<string> _tokens = new List<string>();

	private List<string> _parsedToken = new List<string>();

	public DateTokenGenerator(IDateParseBox parseTextBox)
	{
		_parseTextBox = parseTextBox;
	}

	public List<string> GetValidTokens()
	{
		return _tokens.Except(_ignoreTokens).ToList();
	}

	public void AddTokens(IEnumerable<string> tokens, string content)
	{
		List<string> parsedTokens = tokens.ToList();
		if (parsedTokens.Count == _parsedToken.Count && _parsedToken.All((string t) => parsedTokens.Contains(t)))
		{
			return;
		}
		_tokens.Clear();
		_parsedToken = parsedTokens;
		if (content == null)
		{
			return;
		}
		if (_parsedToken.Count > 0)
		{
			Match match = ParseDueDate.RemindMeRegex.Match(content.ToLower());
			_parsedToken.Add(content.Substring(match.Index, match.Length));
		}
		string[] array = new string[content.Length];
		foreach (string item in _parsedToken)
		{
			int num = content.IndexOf(item, StringComparison.Ordinal);
			if (num >= 0 && (array[num] == null || array[num].Length < item.Length))
			{
				array[num] = item;
			}
		}
		int num2 = 0;
		while (num2 < array.Length)
		{
			string text = array[num2];
			if (string.IsNullOrEmpty(text))
			{
				num2++;
				continue;
			}
			int num3 = num2 + 1;
			while (num3 < array.Length && num3 <= num2 + text.Length)
			{
				string text2 = array[num3];
				if (string.IsNullOrEmpty(text2))
				{
					num3++;
					continue;
				}
				if (num3 + text2.Length <= content.Length)
				{
					text = content.Substring(num2, num3 + text2.Length - num2);
				}
				num3++;
			}
			num2 = num3;
			_tokens.Add(text.Trim());
		}
	}

	public void ClearTokens()
	{
		_tokens.Clear();
		_parsedToken.Clear();
	}

	public void Reset()
	{
		_ignoreTokens.Clear();
		_tokens.Clear();
		_parsedToken.Clear();
	}

	public void AddIgnoreToken(string token)
	{
		if (_ignoreTokens != null && !_ignoreTokens.Contains(token))
		{
			_ignoreTokens.Add(token);
		}
		_parseTextBox?.ForceRender();
	}

	private Match GetMatch(int startOffset, out int matchOffset)
	{
		try
		{
			return Match(startOffset, out matchOffset);
		}
		catch (Exception)
		{
			matchOffset = -1;
			return null;
		}
	}

	private Match Match(int startOffset, out int matchOffset)
	{
		if (!_parseTextBox.CanParseDate)
		{
			matchOffset = -1;
			return null;
		}
		int endOffset = base.CurrentContext.VisualLine.LastDocumentLine.EndOffset;
		StringSegment text = base.CurrentContext.GetText(startOffset, endOffset - startOffset);
		matchOffset = -1;
		string text2 = string.Empty;
		List<string> source = _tokens.Except(_ignoreTokens).ToList();
		if (source.Any())
		{
			text2 = source.Aggregate(text2, (string current, string token) => current + "(" + token.Replace(")", "\\)").Replace("(", "\\)") + ")|");
			if (text2.EndsWith("|"))
			{
				text2 = text2.Substring(0, text2.Length - 1);
			}
		}
		if (string.IsNullOrEmpty(text.Text) || string.IsNullOrEmpty(text2))
		{
			return null;
		}
		Match match = new Regex(text2).Match(text.Text, text.Offset, text.Count);
		if (match.Success)
		{
			string text3 = text.Text.Substring(0, startOffset);
			if (text3.Contains(match.Value))
			{
				_parseTextBox?.RemoveTokenText(ref text3);
				if (text3.Contains(match.Value))
				{
					return null;
				}
			}
			matchOffset = match.Index - text.Offset + startOffset;
			return match;
		}
		return null;
	}

	public override int GetFirstInterestedOffset(int startOffset)
	{
		GetMatch(startOffset, out var matchOffset);
		return matchOffset;
	}

	public override VisualLineElement ConstructElement(int offset)
	{
		try
		{
			int matchOffset;
			Match match = GetMatch(offset, out matchOffset);
			return new DateTokenElement(FormattedTextElement.PrepareText(TextFormatter.Create(), match.Value, new VisualLineElementTextRunProperties(base.CurrentContext.GlobalTextRunProperties)), match.Value, this, _parseTextBox.GetFontSize() - 1.0, match.Value.Length);
		}
		catch (Exception)
		{
			return null;
		}
	}

	public List<string> GetIgnoreTokens()
	{
		return _ignoreTokens;
	}

	public void CheckIgnoreToken(string editBoxText)
	{
		foreach (string item in _ignoreTokens.ToList())
		{
			if (!editBoxText.Contains(item))
			{
				_ignoreTokens.Remove(item);
			}
		}
	}

	public void SetIgnoreToken(List<string> tokens)
	{
		_ignoreTokens.Clear();
		_ignoreTokens.AddRange(tokens);
	}

	public string GetColor()
	{
		return _parseTextBox.GetColor();
	}

	public bool Any()
	{
		return _parsedToken.Any();
	}
}
