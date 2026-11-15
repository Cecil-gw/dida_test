using System;
using System.Collections.Generic;

namespace ticktick_WPF.Util.Twitter;

public static class TaskTitleUtils
{
	public static string GetLastMatchName(ref string title, List<string> tokens, string identifier, string identifierExtra)
	{
		int index = -1;
		return GetLastMatchName(ref title, tokens, identifier, identifierExtra, ref index);
	}

	public static string GetLastMatchName(string title, List<string> tokens, string identifier, string identifierExtra, ref int index)
	{
		return GetLastMatchName(ref title, tokens, identifier, identifierExtra, ref index);
	}

	private static string GetLastMatchName(ref string title, List<string> tokens, string identifier, string identifierExtra, ref int index)
	{
		string text = null;
		title = title.Trim();
		foreach (string token in tokens)
		{
			int num = title.LastIndexOf(" " + identifier + token, StringComparison.Ordinal);
			int num2 = title.LastIndexOf(identifier + token + " ", StringComparison.Ordinal);
			if (num != -1 || num2 != -1)
			{
				if (num2 == num + 1 && num > index)
				{
					index = num;
					text = token;
				}
				if (num == -1 && num2 == 0 && index <= 0)
				{
					index = 0;
					text = token;
				}
				if (num == title.Length - token.Length - 2)
				{
					title = title.Remove(num, token.Length + 2);
					index = title.Length;
					return token;
				}
			}
			if (identifierExtra != identifier)
			{
				int num3 = title.LastIndexOf(" " + identifierExtra + token, StringComparison.Ordinal);
				int num4 = title.LastIndexOf(identifierExtra + token + " ", StringComparison.Ordinal);
				if (num3 != -1 || num4 != -1)
				{
					if (num4 == num3 + 1 && num3 > index)
					{
						index = num3;
						text = token;
					}
					if (num3 == -1 && num4 == 0 && index <= 0)
					{
						index = 0;
						text = token;
					}
					if (num3 == title.Length - token.Length - 2)
					{
						title = title.Remove(num3, token.Length + 2);
						index = title.Length;
						return token;
					}
				}
			}
			if (title == identifier + token || title == identifierExtra + token)
			{
				title = string.Empty;
				index = title.Length;
				return token;
			}
		}
		if (text != null)
		{
			title = title.Remove(index, text.Length + ((index > 0) ? 3 : 2)).Insert(index, " ");
		}
		return text;
	}
}
