using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.QuickAdd;

public class TokenGenerator : VisualLineElementGenerator
{
	private static Regex _defaultLinkRegex;

	private readonly List<QuickAddToken> _tokens = new List<QuickAddToken>();

	private readonly List<string> _removeTokens = new List<string>();

	private readonly Dictionary<string, Brush> _tokenBrushes = new Dictionary<string, Brush>();

	private readonly QuickAddText _quickAddText;

	public static List<string> PriorityText = new List<string>
	{
		"!" + Utils.GetPriorityName(0),
		"!" + Utils.GetPriorityName(1),
		"!" + Utils.GetPriorityName(3),
		"!" + Utils.GetPriorityName(5),
		"！" + Utils.GetPriorityName(0),
		"！" + Utils.GetPriorityName(1),
		"！" + Utils.GetPriorityName(3),
		"！" + Utils.GetPriorityName(5),
		"!4",
		"!3",
		"!2",
		"!1",
		"！4",
		"！3",
		"！2",
		"！1"
	};

	public Regex PriorityRegex { get; }

	public event EventHandler<QuickAddToken> TokenRemoved;

	public TokenGenerator(QuickAddText quickAddText)
	{
		_quickAddText = quickAddText;
		string seed = "";
		seed = PriorityText.Aggregate(seed, (string current, string p) => current + "(" + p + " )|");
		seed = seed.Substring(0, seed.Length - 1);
		PriorityRegex = new Regex(seed);
	}

	internal Brush GetTokenBrush(string color, int percent)
	{
		string key = color + "|" + percent;
		if (!_tokenBrushes.TryGetValue(key, out var value))
		{
			value = ThemeUtil.GetAlphaColor(color, percent);
			if (value.CanFreeze)
			{
				value.Freeze();
			}
			_tokenBrushes[key] = value;
		}
		return value;
	}

	public QuickAddToken TryAddToken(QuickAddToken token)
	{
		QuickAddToken result = null;
		if (token.Exclusive)
		{
			result = _tokens.FirstOrDefault((QuickAddToken t) => t.TokenType == token.TokenType);
			_tokens.RemoveAll((QuickAddToken item) => item.TokenType == token.TokenType);
		}
		if (!_tokens.Exists((QuickAddToken item) => item.TokenType == token.TokenType && item.Value == token.Value))
		{
			_tokens.Add(token);
			_tokens.Sort((QuickAddToken a, QuickAddToken b) => b.Value.Length.CompareTo(a.Value.Length));
		}
		return result;
	}

	public bool TryAddTagToken(QuickAddToken token)
	{
		if (!_tokens.Exists((QuickAddToken item) => item.TokenType == token.TokenType && string.Equals(item.Value, token.Value, StringComparison.CurrentCultureIgnoreCase)) && !_removeTokens.Exists((string item) => string.Equals(item, token.Value, StringComparison.CurrentCultureIgnoreCase)))
		{
			_tokens.Add(token);
			_tokens.Sort((QuickAddToken a, QuickAddToken b) => b.Value.Length.CompareTo(a.Value.Length));
			return true;
		}
		return false;
	}

	public bool AddTokenCheckExist(QuickAddToken token)
	{
		if (!_tokens.Exists((QuickAddToken item) => item.TokenType == token.TokenType && string.Equals(item.Value, token.Value, StringComparison.CurrentCultureIgnoreCase)) && !_removeTokens.Exists((string item) => string.Equals(item, token.Value, StringComparison.CurrentCultureIgnoreCase)))
		{
			_tokens.Add(token);
			return true;
		}
		return false;
	}

	public IEnumerable<QuickAddToken> GetTokens()
	{
		return _tokens;
	}

	public List<QuickAddToken> GetTokensByType(TokenType type)
	{
		return _tokens.Where((QuickAddToken t) => t.TokenType == type).ToList();
	}

	public void RemoveToken(QuickAddToken token, bool notify = true, bool removeTemp = true)
	{
		if (removeTemp)
		{
			_removeTokens.Add(token.Value);
		}
		_tokens.Remove(token);
		if (notify)
		{
			this.TokenRemoved?.Invoke(this, token);
		}
	}

	public void CheckInvalidTokens(string content)
	{
		if (!_tokens.Any())
		{
			return;
		}
		foreach (QuickAddToken item in _tokens.Where((QuickAddToken token) => !content.Contains(token.Value)))
		{
			this.TokenRemoved?.Invoke(this, item);
		}
		_tokens.RemoveAll((QuickAddToken token) => !content.Contains(token.Value));
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
		int endOffset = base.CurrentContext.VisualLine.LastDocumentLine.EndOffset;
		StringSegment text = base.CurrentContext.GetText(startOffset, endOffset - startOffset);
		matchOffset = -1;
		string text2 = string.Empty;
		if (_tokens.Any())
		{
			text2 = _tokens.Aggregate(text2, (string current, QuickAddToken token) => current + "(" + token.Value.Replace("^", "\\^").Replace("+", "\\+").Replace("*", "\\*")
				.Replace(")", "\\)")
				.Replace("(", "\\(") + ")|");
			if (text2.EndsWith("|"))
			{
				text2 = text2.Substring(0, text2.Length - 1);
			}
		}
		if (string.IsNullOrEmpty(text.Text) || string.IsNullOrEmpty(text2))
		{
			return null;
		}
		Regex regex = new Regex(text2);
		int num = text.Offset;
		int length = text.Count;
		while (num < text.Text.Length)
		{
			Match match = regex.Match(text.Text, num, length);
			if (match.Success)
			{
				string input = text.Text.Substring(0, startOffset);
				MatchCollection matchCollection = regex.Matches(input);
				bool flag = true;
				if (matchCollection.Count > 0)
				{
					for (int num2 = 0; num2 < matchCollection.Count; num2++)
					{
						if (matchCollection[num2].Value == match.Value)
						{
							num = match.Index + match.Value.Length;
							length = text.Text.Length - (match.Index + match.Value.Length);
							flag = false;
							break;
						}
					}
				}
				if (flag)
				{
					matchOffset = match.Index - text.Offset + startOffset;
					return match;
				}
				continue;
			}
			return null;
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
			if (match.Success)
			{
				QuickAddToken quickAddToken = _tokens.FirstOrDefault((QuickAddToken val) => val.Value == match.Value);
				if (quickAddToken != null)
				{
					string color = _quickAddText.GetColor();
					if (quickAddToken.TokenType == TokenType.Priority)
					{
						int priorityByText = GetPriorityByText(match.Value);
						color = ((_quickAddText.UseInWidgetDark() != true || priorityByText != 0) ? Utils.GetPriorityColor(priorityByText).Color.ToString() : "#66FFFFFF");
					}
					return new TokenElement(FormattedTextElement.PrepareText(TextFormatter.Create(), match.Value, new VisualLineElementTextRunProperties(base.CurrentContext.GlobalTextRunProperties)), quickAddToken, this, _quickAddText.GetFontSize() - 1.0, match.Value.Length, color);
				}
			}
			return null;
		}
		catch (Exception)
		{
			return null;
		}
	}

	public void Reset()
	{
		_tokens.Clear();
	}

	public List<string> GetSelectedTags()
	{
		return (from token in _tokens
			where token.TokenType == TokenType.Tag
			select token.Value.ToLower().Substring(1)).ToList();
	}

	public string GetSelectedProject()
	{
		return (from token in _tokens
			where token.TokenType == TokenType.Project
			select token.Value.ToLower().Substring(1)).ToList().FirstOrDefault();
	}

	public string GetProjectTokenValue()
	{
		return _tokens.FirstOrDefault((QuickAddToken token) => token.TokenType == TokenType.Project)?.Value;
	}

	public List<QuickAddToken> RemoveTokenByType(TokenType tokenType)
	{
		List<QuickAddToken> result = _tokens.Where((QuickAddToken item) => item.TokenType == tokenType).ToList();
		_tokens.RemoveAll((QuickAddToken item) => item.TokenType == tokenType);
		return result;
	}

	public bool ExistToken(QuickAddToken token)
	{
		return _tokens.Any((QuickAddToken t) => t.TokenType == token.TokenType && t.Value == token.Value);
	}

	public bool ExistToken(TokenType type)
	{
		return _tokens.Any((QuickAddToken t) => t.TokenType == type);
	}

	public bool ExistRemoveToken(string token)
	{
		return _removeTokens.Any((string t) => string.Equals(t, token, StringComparison.CurrentCultureIgnoreCase));
	}

	public List<QuickAddToken> GetTagTokens()
	{
		return _tokens.Where((QuickAddToken token) => token.TokenType == TokenType.Tag).ToList();
	}

	public void HandleRemainText(List<string> remains)
	{
		if (remains == null)
		{
			return;
		}
		foreach (QuickAddToken item in _tokens.Where((QuickAddToken item) => !remains.Any((string text) => string.Equals(text, item.Value, StringComparison.CurrentCultureIgnoreCase))).ToList())
		{
			if (item.TokenType == TokenType.Tag)
			{
				_tokens.Remove(item);
				this.TokenRemoved?.Invoke(this, item);
			}
		}
		_removeTokens.RemoveAll((string item) => remains.Count == 0 || !remains.Any((string text) => string.Equals(text, item, StringComparison.CurrentCultureIgnoreCase)));
	}

	public static int GetPriorityByText(string pri)
	{
		int num = PriorityText.IndexOf(pri);
		if (num < 0)
		{
			return 0;
		}
		num %= 4;
		if (num != 0)
		{
			return 2 * num - 1;
		}
		return 0;
	}
}
