using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.MarkDown;

public static class StringExtensions
{
	private static Regex _numRegex = new Regex("(^0x[\\da-fA-F]+$|^([+-]?(?:\\d+(?:\\.\\d*)?|\\.\\d+)(?:[eE][+-]?\\d+)?(?!\\.\\d+)(?=\\D|\\s|$))|\\d+)");

	private static Regex _zeroRegex = new Regex("^0+");

	public static List<TitleChunk> CompareNumber(this string text)
	{
		text = text.Trim();
		MatchCollection matchCollection = _numRegex.Matches(text);
		List<TitleChunk> list = new List<TitleChunk>();
		int num = 0;
		if (matchCollection.Count > 0)
		{
			for (int i = 0; i < matchCollection.Count; i++)
			{
				Match match = matchCollection[i];
				if (match.Index > num)
				{
					string text2 = text.Substring(num, match.Index - num);
					list.Add(new TitleChunk(text2, isNum: false, 0));
				}
				string text3 = _zeroRegex.Replace(match.Value, "");
				list.Add(new TitleChunk(text3, isNum: true, match.Length));
				num = match.Index + match.Length;
			}
			if (text.Length > num)
			{
				string text4 = text.Substring(num, text.Length - num);
				list.Add(new TitleChunk(text4, isNum: false, 0));
			}
		}
		else
		{
			list.Add(new TitleChunk(text, isNum: false, 0));
		}
		return list;
	}

	public static List<string> SplitByStr(this string text, string sep)
	{
		if (string.IsNullOrEmpty(text))
		{
			return new List<string> { text };
		}
		List<string> list = new List<string>();
		do
		{
			int num = text.IndexOf(sep, StringComparison.Ordinal);
			if (num >= 0)
			{
				list.Add(text.Substring(0, num));
				text = ((num + sep.Length < text.Length) ? text.Substring(num + sep.Length) : null);
			}
			else
			{
				list.Add(text);
				text = null;
			}
		}
		while (!string.IsNullOrEmpty(text));
		return list;
	}

	public static string UpCaseFirst(this string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}
		return text.Substring(0, 1).ToUpper() + text.Substring(1).ToLower();
	}

	public static string PaddingWith(this string text, string quote)
	{
		if (text.Contains("\r\n"))
		{
			string[] array = text.Replace("\r\n", "\n").Split('\n');
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < array.Length; i++)
			{
				string text2 = array[i];
				if (!string.IsNullOrEmpty(text2))
				{
					stringBuilder.Append(quote + text2);
					if (i < array.Length - 1)
					{
						stringBuilder.Append("\r\n");
					}
				}
			}
			return stringBuilder.ToString();
		}
		return quote + text;
	}

	public static string UnPaddingWith(this string text, string quote)
	{
		if (text.Contains("\r\n"))
		{
			string[] array = text.Replace("\r\n", "\n").Split('\n');
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < array.Length; i++)
			{
				string text2 = array[i];
				if (!string.IsNullOrEmpty(text2))
				{
					stringBuilder.Append(text2.TrimStart(quote.ToCharArray()));
					if (i < array.Length - 1)
					{
						stringBuilder.Append("\r\n");
					}
				}
			}
			return stringBuilder.ToString();
		}
		if (quote != null)
		{
			return text.TrimStart(quote.ToCharArray());
		}
		return text;
	}

	public static string SurroundWith(this string text, string prefix, string postfix = null)
	{
		if (text.Contains("\r\n"))
		{
			text = text.Replace("\r\n", "\n");
		}
		if (postfix == null)
		{
			postfix = prefix;
		}
		if (text.Contains("\n"))
		{
			string[] array = text.Replace("\r\n", "\n").Split('\n');
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < array.Length; i++)
			{
				string text2 = array[i];
				if (!string.IsNullOrEmpty(text2))
				{
					stringBuilder.Append(prefix + text2.TrimEnd() + postfix);
					if (i < array.Length - 1)
					{
						stringBuilder.Append("\n");
					}
				}
				else
				{
					stringBuilder.Append("\n");
				}
			}
			return stringBuilder.ToString();
		}
		return prefix + text.TrimEnd() + postfix;
	}

	public static string UnSurroundWith(this string text, string quote)
	{
		if (string.IsNullOrEmpty(quote))
		{
			return text;
		}
		if (text.Contains("\r\n"))
		{
			text = text.Replace("\r\n", "\n");
		}
		if (text.Contains("\n"))
		{
			string[] array = text.Split('\n');
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < array.Length; i++)
			{
				string text2 = array[i];
				if (!string.IsNullOrEmpty(text2))
				{
					stringBuilder.Append(text2.Trim(quote.ToCharArray()));
					if (i < array.Length - 1)
					{
						stringBuilder.Append("\n");
					}
				}
				else
				{
					stringBuilder.Append("\n");
				}
			}
			return stringBuilder.ToString();
		}
		if (text.StartsWith(quote) && text.EndsWith(quote))
		{
			text = text.Substring(quote.Length);
			if (text.Length >= quote.Length)
			{
				text = text.Substring(0, text.Length - quote.Length);
			}
		}
		return text;
	}

	public static string ReplaceSmartChars(this string smart)
	{
		return smart.Replace('–', '-').Replace('—', '-').Replace('―', '-')
			.Replace('‗', '_')
			.Replace('‘', '\'')
			.Replace('’', '\'')
			.Replace('‚', ',')
			.Replace('‛', '\'')
			.Replace('“', '"')
			.Replace('”', '"')
			.Replace('„', '"')
			.Replace("…", "...")
			.Replace('′', '\'')
			.Replace('″', '"');
	}

	public static int WordCount(this string text)
	{
		if (text != null)
		{
			return Regex.Matches(text, "[\\S]+").Count;
		}
		return 0;
	}

	public static string AddOffsetToFileName(this string file, int offset)
	{
		return $"{file.StripOffsetFromFileName()}|{offset}";
	}

	public static string ReplaceDate(this string text)
	{
		return new Regex("\\$DATE(?:\\(\"(.+)\"\\))?\\$").Replace(text, (Match match) => DateTime.Now.ToString(match?.Groups[1].Value, CultureInfo.InvariantCulture));
	}

	public static string StripOffsetFromFileName(this string file)
	{
		if (string.IsNullOrWhiteSpace(file))
		{
			return file;
		}
		int num = file.IndexOf('|');
		if (num < 0)
		{
			return file;
		}
		return file.Substring(0, num);
	}

	public static string ToLowerInvariant(this string text)
	{
		return text.ToLower(CultureInfo.InvariantCulture);
	}

	public static string ToUpperInvariant(this string text)
	{
		return text.ToUpper(CultureInfo.InvariantCulture);
	}

	public static string ToSlug(this string value, bool toLower = false)
	{
		if (value == null)
		{
			return "";
		}
		string text = value.Normalize(NormalizationForm.FormKD);
		int length = text.Length;
		bool flag = false;
		StringBuilder stringBuilder = new StringBuilder(length);
		for (int i = 0; i < length; i++)
		{
			char c = text[i];
			if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
			{
				if (flag)
				{
					stringBuilder.Append('-');
					flag = false;
				}
				stringBuilder.Append(c);
			}
			else if (c >= 'A' && c <= 'Z')
			{
				if (flag)
				{
					stringBuilder.Append('-');
					flag = false;
				}
				if (toLower)
				{
					stringBuilder.Append((char)(c | 0x20));
				}
				else
				{
					stringBuilder.Append(c);
				}
			}
			else if (c == ' ' || c == ',' || c == '.' || c == '/' || c == '\\' || c == '-' || c == '_' || c == '=')
			{
				if (!flag && stringBuilder.Length > 0)
				{
					flag = true;
				}
			}
			else
			{
				string text2 = ConvertEdgeCases(c, toLower);
				if (text2 != null)
				{
					if (flag)
					{
						stringBuilder.Append('-');
						flag = false;
					}
					stringBuilder.Append(text2);
				}
			}
			if (stringBuilder.Length == 80)
			{
				break;
			}
		}
		return stringBuilder.ToString();
	}

	private static string ConvertEdgeCases(char c, bool toLower)
	{
		string result = null;
		switch (c)
		{
		case 'ı':
			result = "i";
			break;
		case 'ł':
			result = "l";
			break;
		case 'Ł':
			result = (toLower ? "l" : "L");
			break;
		case 'đ':
			result = "d";
			break;
		case 'ß':
			result = "ss";
			break;
		case 'ø':
			result = "o";
			break;
		case 'Þ':
			result = "th";
			break;
		}
		return result;
	}
}
