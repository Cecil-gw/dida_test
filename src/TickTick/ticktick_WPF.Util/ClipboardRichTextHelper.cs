using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;
using ticktick_WPF.Views.MarkDown;

namespace ticktick_WPF.Util;

public static class ClipboardRichTextHelper
{
	private sealed class InlineWrapper
	{
		private readonly Func<string, string> _apply;

		public InlineWrapper(Func<string, string> apply)
		{
			_apply = apply;
		}

		public string Apply(string text)
		{
			return _apply?.Invoke(text) ?? text;
		}
	}

	private sealed class InlineStyleState
	{
		public bool Strong { get; set; }

		public bool Emphasis { get; set; }

		public bool Underline { get; set; }

		public bool Strikethrough { get; set; }

		public bool Code { get; set; }

		public string HighlightColor { get; set; }

		public string LinkUrl { get; set; }

		public InlineStyleState Clone()
		{
			return new InlineStyleState
			{
				Strong = Strong,
				Emphasis = Emphasis,
				Underline = Underline,
				Strikethrough = Strikethrough,
				Code = Code,
				HighlightColor = HighlightColor,
				LinkUrl = LinkUrl
			};
		}
	}

	private sealed class InlineTextRun
	{
		public string Text { get; set; }

		public bool PreserveWhitespace { get; set; }

		public bool Strong { get; set; }

		public bool Emphasis { get; set; }

		public bool Underline { get; set; }

		public bool Strikethrough { get; set; }

		public bool Code { get; set; }

		public string HighlightColor { get; set; }

		public string LinkUrl { get; set; }

		public bool HasSameStyle(InlineStyleState style)
		{
			if (style != null && Strong == style.Strong && Emphasis == style.Emphasis && Underline == style.Underline && Strikethrough == style.Strikethrough && Code == style.Code && string.Equals(HighlightColor ?? string.Empty, style.HighlightColor ?? string.Empty, StringComparison.OrdinalIgnoreCase))
			{
				return string.Equals(LinkUrl ?? string.Empty, style.LinkUrl ?? string.Empty, StringComparison.Ordinal);
			}
			return false;
		}
	}

	private sealed class InlineSerializationResult
	{
		public string RawText { get; set; } = string.Empty;

		public int VisibleLength { get; set; }
	}

	private sealed class HtmlConversionContext
	{
		public int QuoteDepth { get; set; }

		public bool IsListItemContent { get; set; }

		public int DisplayLength { get; set; }

		public HtmlConversionContext Clone()
		{
			return new HtmlConversionContext
			{
				QuoteDepth = QuoteDepth,
				IsListItemContent = IsListItemContent,
				DisplayLength = DisplayLength
			};
		}
	}

	private const string ExportTextClipboardFormat = "TickTickExportText";

	private static readonly Regex StartFragmentRegex = new Regex("StartFragment:(?<value>\\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex EndFragmentRegex = new Regex("EndFragment:(?<value>\\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex SelfClosingBreakRegex = new Regex("<br(?<attrs>\\s[^>]*)?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex SelfClosingHrRegex = new Regex("<hr(?<attrs>(?:\\s[^>]*?)?)(?<!/)>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex StandaloneAmpersandRegex = new Regex("&(?!#?\\w+;)", RegexOptions.Compiled);

	private static readonly Regex NbspEntityRegex = new Regex("&nbsp;", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex HtmlVoidTagRegex = new Regex("<(?<tag>area|base|col|embed|img|input|link|meta|param|source|track|wbr)(?<attrs>\\s[^<>]*?)?(?<self>/)?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex LarkRecordClipboardRegex = new Regex("<span\\b[^>]*\\bdata-lark-record-format\\s*=\\s*[\"']docx/record[\"'][^>]*>.*?</span>", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);

	private static readonly Regex RgbaColorRegex = new Regex("^rgba?\\(\\s*(?<r>\\d{1,3})\\s*,\\s*(?<g>\\d{1,3})\\s*,\\s*(?<b>\\d{1,3})(?:\\s*,\\s*(?<a>(?:0|1|0?\\.\\d+)))?\\s*\\)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex CodeLanguageClassRegex = new Regex("(?:^|\\s)(?:language|lang)-(?<value>[A-Za-z0-9_+#.-]+)(?:\\s|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex CssBackgroundColorRegex = new Regex("background-color\\s*:", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex PreformattedWhitespaceStyleRegex = new Regex("white-space\\s*:\\s*(?:pre-wrap|break-spaces|pre)(?![-\\w])", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex MonospaceFontStyleRegex = new Regex("font-family\\s*:[^;>]*(?:monospace|consolas|courier|menlo|monaco|cascadia)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex MonospaceFontFamilyValueRegex = new Regex("(?:^|[,\\s\\\"'])(?:ui-monospace|monospace|consolas|courier(?:\\s+new)?|menlo|monaco|cascadia(?:\\s+(?:code|mono))?)(?=$|[,\\s\\\"'])", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex SyntaxHighlightedCodeTokenRegex = new Regex("\\b(?:abstract|as|async|await|break|case|catch|class|const|continue|def|do|elif|else|except|export|finally|for|from|function|if|import|in|interface|let|new|private|protected|public|return|static|switch|throw|try|using|var|while|with|yield)\\b|=>|===|!==|==|!=|<=|>=|&&|\\|\\||[{};]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex InlineCodeMarkdownRegex = new Regex("(?<!\\\\)`[^`\\r\\n]+`", RegexOptions.Compiled);

	private static readonly Regex InlineStrongMarkdownRegex = new Regex("(?<!\\\\)(?:\\*\\*(?=\\S).+?(?<=\\S)(?<!\\\\)\\*\\*|__(?=\\S).+?(?<=\\S)(?<!\\\\)__)", RegexOptions.Compiled);

	private static readonly Regex InlineEmphasisMarkdownRegex = new Regex("(?<!\\\\)(?:(?<!\\*)\\*(?!\\*)(?=\\S).+?(?<=\\S)(?<!\\\\)(?<!\\*)\\*(?!\\*)|(?<!_)_(?!_)(?=\\S).+?(?<=\\S)(?<!\\\\)(?<!_)_(?!_))", RegexOptions.Compiled);

	private const string TickTickMarkdownClipboardFormat = "TickTickMarkdown";

	private const string TickTickPlainTextClipboardFormat = "TickTickPlainText";

	private const string InlineSemanticMetadataPrefix = "<!--tt_inline_sem:";

	private const string InlineSemanticMetadataSuffix = "-->";

	public static DataObject CreateExportTextDataObject(string text)
	{
		DataObject dataObject = new DataObject();
		dataObject.SetData(DataFormats.UnicodeText, text ?? string.Empty);
		dataObject.SetData(DataFormats.Text, text ?? string.Empty);
		dataObject.SetData("TickTickExportText", text ?? string.Empty, autoConvert: false);
		return dataObject;
	}

	private static ClipboardRichTextContent ExtractExportText(IDataObject data)
	{
		if (data == null || !data.GetDataPresent("TickTickExportText", autoConvert: false) || !(data.GetData("TickTickExportText", autoConvert: false) is string text))
		{
			return null;
		}
		return new ClipboardRichTextContent
		{
			PlainText = text,
			PreferredText = text,
			IsLiteralExport = true
		};
	}

	public static ClipboardRichTextContent Extract(IDataObject dataObject)
	{
		return Extract(dataObject, null);
	}

	public static ClipboardRichTextContent Extract(IDataObject dataObject, IDataObject fallbackDataObject)
	{
		ClipboardRichTextContent clipboardRichTextContent = ExtractExportText(dataObject);
		if (clipboardRichTextContent != null)
		{
			return clipboardRichTextContent;
		}
		ClipboardRichTextContent clipboardRichTextContent2 = ExtractExportText(fallbackDataObject);
		if (clipboardRichTextContent2 != null && string.Equals(GetClipboardText(dataObject), clipboardRichTextContent2.PlainText, StringComparison.Ordinal))
		{
			return clipboardRichTextContent2;
		}
		ClipboardRichTextContent clipboardRichTextContent3 = ExtractCore(dataObject);
		if (HasExplicitMarkdownData(fallbackDataObject))
		{
			return ExtractCore(fallbackDataObject);
		}
		if (!ShouldUseFallbackContent(clipboardRichTextContent3, fallbackDataObject))
		{
			return clipboardRichTextContent3;
		}
		ClipboardRichTextContent clipboardRichTextContent4 = ExtractCore(fallbackDataObject);
		if (!ShouldPreferFallbackContent(clipboardRichTextContent3, clipboardRichTextContent4))
		{
			return clipboardRichTextContent3;
		}
		return clipboardRichTextContent4;
	}

	public static string ExtractPlainText(IDataObject dataObject)
	{
		return GetClipboardText(dataObject);
	}

	private static ClipboardRichTextContent ExtractCore(IDataObject dataObject)
	{
		string clipboardText = GetClipboardText(dataObject);
		string clipboardMarkdown = GetClipboardMarkdown(dataObject);
		string clipboardHtmlFragment = GetClipboardHtmlFragment(dataObject);
		string text = ConvertHtmlFragmentToMarkdown(clipboardHtmlFragment);
		if (ShouldDiscardSyntaxHighlightMarkup(clipboardHtmlFragment, clipboardText, text))
		{
			text = clipboardText;
		}
		bool flag = !string.IsNullOrWhiteSpace(clipboardMarkdown);
		bool shouldParseEmphasis = false;
		string preferredText = ((!flag) ? SelectPreferredText(clipboardText, text, out shouldParseEmphasis) : (HasInlineSemanticMetadata(clipboardMarkdown) ? clipboardMarkdown : NormalizeMarkdown(clipboardMarkdown)));
		if (flag)
		{
			shouldParseEmphasis = true;
		}
		return new ClipboardRichTextContent
		{
			PlainText = clipboardText,
			HtmlFragment = clipboardHtmlFragment,
			PreferredText = preferredText,
			HasExplicitMarkdown = flag,
			ShouldParseEmphasis = shouldParseEmphasis
		};
	}

	private static bool ShouldDiscardSyntaxHighlightMarkup(string htmlFragment, string plainText, string convertedMarkdown)
	{
		if (string.IsNullOrWhiteSpace(htmlFragment) || string.IsNullOrWhiteSpace(plainText))
		{
			return false;
		}
		if (HasPreformattedMonospacePresentationWithoutSemanticCodeBlock(htmlFragment) && HasLostLeadingWhitespace(plainText, convertedMarkdown))
		{
			return true;
		}
		if (CssBackgroundColorRegex.Matches(htmlFragment).Count < 2 || string.IsNullOrEmpty(convertedMarkdown) || convertedMarkdown.IndexOf("=={#", StringComparison.Ordinal) < 0)
		{
			return false;
		}
		string text = plainText.Replace("\r\n", "\n").Replace('\r', '\n').Replace('\u00a0', ' ');
		if (text.Split('\n').Count((string line) => !string.IsNullOrWhiteSpace(line)) < 2 || SyntaxHighlightedCodeTokenRegex.Matches(text).Count < 3)
		{
			return false;
		}
		return true;
	}

	private static bool HasPreformattedMonospacePresentationWithoutSemanticCodeBlock(string htmlFragment)
	{
		if (!PreformattedWhitespaceStyleRegex.IsMatch(htmlFragment) || !MonospaceFontStyleRegex.IsMatch(htmlFragment))
		{
			return false;
		}
		try
		{
			List<XElement> source = ParseHtmlFragment(htmlFragment)?.Descendants().ToList() ?? new List<XElement>();
			if (source.Any((XElement element) => TryGetStandaloneCodeBlockElement(element, out var _)))
			{
				return false;
			}
			return source.Any(delegate(XElement element)
			{
				Dictionary<string, string> dictionary = ParseStyleAttributes(element.Attribute("style")?.Value);
				string value;
				string value2;
				return dictionary.TryGetValue("white-space", out value) && IsPreformattedWhiteSpaceValue(value) && dictionary.TryGetValue("font-family", out value2) && MonospaceFontFamilyValueRegex.IsMatch(value2);
			});
		}
		catch (Exception)
		{
			return false;
		}
	}

	private static bool IsPreformattedWhiteSpaceValue(string value)
	{
		string a = Regex.Replace(value ?? string.Empty, "\\s*!important\\s*$", string.Empty, RegexOptions.IgnoreCase).Trim();
		if (!string.Equals(a, "pre", StringComparison.OrdinalIgnoreCase) && !string.Equals(a, "pre-wrap", StringComparison.OrdinalIgnoreCase))
		{
			return string.Equals(a, "break-spaces", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private static bool HasLostLeadingWhitespace(string plainText, string convertedMarkdown)
	{
		string[] array = NormalizeNewLinesForComparison(plainText).Trim('\n').Split('\n');
		string[] array2 = NormalizeNewLinesForComparison(convertedMarkdown).Trim('\n').Split('\n');
		if (array.Length != array2.Length)
		{
			return false;
		}
		for (int i = 0; i < array.Length; i++)
		{
			if (!string.IsNullOrWhiteSpace(array[i]) && CountLeadingWhitespace(array[i]) > CountLeadingWhitespace(array2[i]))
			{
				return true;
			}
		}
		return false;
	}

	private static string NormalizeNewLinesForComparison(string text)
	{
		return (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
	}

	private static int CountLeadingWhitespace(string text)
	{
		int i;
		for (i = 0; i < (text?.Length ?? 0) && (text[i] == ' ' || text[i] == '\t' || text[i] == '\u00a0'); i++)
		{
		}
		return i;
	}

	private static string SelectPreferredText(string plainText, string markdown, out bool shouldParseEmphasis)
	{
		if (ShouldPreferPlainTextMarkdownSource(plainText, markdown))
		{
			shouldParseEmphasis = false;
			return NormalizeMarkdown(plainText);
		}
		if (string.IsNullOrWhiteSpace(markdown))
		{
			shouldParseEmphasis = false;
			return plainText;
		}
		shouldParseEmphasis = !string.Equals(NormalizeMarkdown(markdown), NormalizeMarkdown(plainText), StringComparison.Ordinal);
		return markdown;
	}

	private static bool ShouldPreferPlainTextMarkdownSource(string plainText, string markdown)
	{
		if (string.IsNullOrWhiteSpace(plainText) || string.IsNullOrWhiteSpace(markdown))
		{
			return false;
		}
		int listCount;
		int headingCount;
		int num = ScoreMarkdownStructure(plainText, out listCount, out headingCount);
		int inlineCodeCount;
		int inlineStyleCount;
		int num2 = ScoreInlineMarkdownStructure(plainText, out inlineCodeCount, out inlineStyleCount);
		if (num <= 0 && num2 <= 0)
		{
			return false;
		}
		int listCount2;
		int headingCount2;
		int num3 = ScoreMarkdownStructure(markdown, out listCount2, out headingCount2);
		int inlineCodeCount2;
		int inlineStyleCount2;
		int num4 = ScoreInlineMarkdownStructure(markdown, out inlineCodeCount2, out inlineStyleCount2);
		if (listCount > listCount2)
		{
			return true;
		}
		if (headingCount > headingCount2)
		{
			return true;
		}
		if (inlineCodeCount > inlineCodeCount2)
		{
			return true;
		}
		if (inlineStyleCount > inlineStyleCount2)
		{
			return true;
		}
		if (num + num2 >= 2)
		{
			return num3 + num4 == 0;
		}
		return false;
	}

	private static int ScoreMarkdownStructure(string text, out int listCount, out int headingCount)
	{
		listCount = 0;
		headingCount = 0;
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0;
		}
		int num = 0;
		string[] array = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = array[i]?.TrimStart() ?? string.Empty;
			if (text2.Length != 0)
			{
				if (Regex.IsMatch(text2, "^(#{1,6})\\s+\\S"))
				{
					headingCount++;
					num++;
				}
				if (Regex.IsMatch(text2, "^(-|\\*|\\+)\\s+\\S") || Regex.IsMatch(text2, "^\\d+\\.\\s+\\S") || Regex.IsMatch(text2, "^- \\[( |x|X)\\]\\s+\\S"))
				{
					listCount++;
					num++;
				}
				if (Regex.IsMatch(text2, "^>\\s+\\S") || text2.StartsWith("```", StringComparison.Ordinal))
				{
					num++;
				}
			}
		}
		return num;
	}

	private static int ScoreInlineMarkdownStructure(string text, out int inlineCodeCount, out int inlineStyleCount)
	{
		inlineCodeCount = 0;
		inlineStyleCount = 0;
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0;
		}
		string input = text.Replace("\r\n", "\n").Replace('\r', '\n');
		inlineCodeCount = InlineCodeMarkdownRegex.Matches(input).Count;
		inlineStyleCount = InlineStrongMarkdownRegex.Matches(input).Count + InlineEmphasisMarkdownRegex.Matches(input).Count;
		return inlineCodeCount * 2 + inlineStyleCount;
	}

	private static bool ShouldUseFallbackContent(ClipboardRichTextContent primaryContent, IDataObject fallbackDataObject)
	{
		if (fallbackDataObject == null)
		{
			return false;
		}
		if (HasInlineSemanticMarkdownData(fallbackDataObject))
		{
			return true;
		}
		if (primaryContent != null && primaryContent.HasRichText)
		{
			return string.Equals(primaryContent.PreferredText, primaryContent.PlainText, StringComparison.Ordinal);
		}
		return true;
	}

	private static bool ShouldPreferFallbackContent(ClipboardRichTextContent primaryContent, ClipboardRichTextContent fallbackContent)
	{
		if (fallbackContent == null)
		{
			return false;
		}
		if (primaryContent == null)
		{
			return true;
		}
		if (HasInlineSemanticMetadata(fallbackContent.PreferredText) && !HasInlineSemanticMetadata(primaryContent.PreferredText))
		{
			return true;
		}
		if (fallbackContent.HasRichText && !primaryContent.HasRichText)
		{
			return true;
		}
		if (fallbackContent.HasRichText && string.Equals(primaryContent.PreferredText, primaryContent.PlainText, StringComparison.Ordinal))
		{
			return !string.Equals(fallbackContent.PreferredText, fallbackContent.PlainText, StringComparison.Ordinal);
		}
		return false;
	}

	public static string ConvertHtmlFragmentToMarkdown(string htmlFragment)
	{
		if (string.IsNullOrWhiteSpace(htmlFragment))
		{
			return string.Empty;
		}
		try
		{
			XElement xElement = ParseHtmlFragment(htmlFragment);
			if (xElement == null)
			{
				return string.Empty;
			}
			HtmlConversionContext context = new HtmlConversionContext();
			StringBuilder stringBuilder = new StringBuilder();
			AppendChildBlocks(stringBuilder, xElement, context);
			return NormalizeMarkdown(stringBuilder.ToString());
		}
		catch (Exception)
		{
			return string.Empty;
		}
	}

	private static string GetClipboardText(IDataObject dataObject)
	{
		string text = TryGetStringData(dataObject, "TickTickPlainText");
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return TryGetStringData(dataObject, DataFormats.UnicodeText) ?? TryGetStringData(dataObject, DataFormats.Text) ?? string.Empty;
	}

	private static string GetClipboardMarkdown(IDataObject dataObject)
	{
		return TryGetStringData(dataObject, "TickTickMarkdown") ?? TryGetStringData(dataObject, "Markdown") ?? string.Empty;
	}

	private static bool HasInlineSemanticMarkdownData(IDataObject dataObject)
	{
		return HasInlineSemanticMetadata(GetClipboardMarkdown(dataObject));
	}

	private static bool HasExplicitMarkdownData(IDataObject dataObject)
	{
		return !string.IsNullOrWhiteSpace(GetClipboardMarkdown(dataObject));
	}

	private static bool HasInlineSemanticMetadata(string text)
	{
		if (!string.IsNullOrEmpty(text))
		{
			return text.IndexOf("<!--tt_inline_sem:", StringComparison.Ordinal) >= 0;
		}
		return false;
	}

	private static string GetClipboardHtmlFragment(IDataObject dataObject)
	{
		string text = TryGetStringData(dataObject, DataFormats.Html);
		if (string.IsNullOrWhiteSpace(text))
		{
			return string.Empty;
		}
		int num = text.IndexOf("<!--StartFragment-->", StringComparison.OrdinalIgnoreCase);
		int num2 = text.IndexOf("<!--EndFragment-->", StringComparison.OrdinalIgnoreCase);
		if (num >= 0 && num2 > num)
		{
			num += "<!--StartFragment-->".Length;
			return text.Substring(num, num2 - num);
		}
		Match match = StartFragmentRegex.Match(text);
		Match match2 = EndFragmentRegex.Match(text);
		if (match.Success && match2.Success && int.TryParse(match.Groups["value"].Value, out var result) && int.TryParse(match2.Groups["value"].Value, out var result2) && result >= 0 && result2 > result && result2 <= text.Length)
		{
			return text.Substring(result, result2 - result);
		}
		return text;
	}

	private static string TryGetStringData(IDataObject dataObject, string format)
	{
		try
		{
			return (dataObject != null && dataObject.GetDataPresent(format, autoConvert: true)) ? (dataObject.GetData(format, autoConvert: true) as string) : null;
		}
		catch (Exception)
		{
			return null;
		}
	}

	private static XElement ParseHtmlFragment(string htmlFragment)
	{
		string input = htmlFragment.Replace("\r", string.Empty);
		input = LarkRecordClipboardRegex.Replace(input, string.Empty);
		input = NbspEntityRegex.Replace(input, "&#160;");
		input = SelfClosingBreakRegex.Replace(input, "<br${attrs} />");
		input = SelfClosingHrRegex.Replace(input, "<hr${attrs} />");
		input = HtmlVoidTagRegex.Replace(input, delegate(Match match)
		{
			string value = match.Groups["tag"].Value;
			string text = (match.Groups["attrs"].Success ? match.Groups["attrs"].Value : string.Empty);
			return "<" + value + text + " />";
		});
		input = StandaloneAmpersandRegex.Replace(input, "&amp;");
		return XElement.Parse("<root>" + input + "</root>", LoadOptions.PreserveWhitespace);
	}

	private static void AppendBlockNode(StringBuilder builder, XNode node, HtmlConversionContext context)
	{
		if (node is XText xText)
		{
			string text = NormalizeInlineText(xText.Value);
			if (!string.IsNullOrEmpty(text))
			{
				string text2 = ApplyQuotePrefix(text, context.QuoteDepth);
				AppendBlockText(builder, text2, text2.Length, context);
			}
		}
		else
		{
			if (!(node is XElement xElement))
			{
				return;
			}
			string text3 = xElement.Name.LocalName.ToLowerInvariant();
			if (text3 != "html" && text3 != "body" && TryGetStandaloneCodeBlockElement(xElement, out var codeBlockElement))
			{
				AppendCodeBlock(builder, codeBlockElement, context, xElement);
				return;
			}
			switch (text3)
			{
			case "body":
			case "html":
				AppendChildBlocks(builder, xElement, context);
				break;
			case "head":
				break;
			case "h1":
			case "h2":
			case "h3":
			case "h4":
			case "h5":
			case "h6":
			{
				int count = int.Parse(text3.Substring(1), CultureInfo.InvariantCulture);
				InlineSerializationResult inlineSerializationResult2 = ConvertInlineNodes(xElement.Nodes(), context);
				string text5 = ApplyQuotePrefix(new string('#', count) + " " + inlineSerializationResult2.RawText, context.QuoteDepth);
				if (!string.IsNullOrEmpty(inlineSerializationResult2.RawText))
				{
					AppendBlockText(builder, text5, inlineSerializationResult2.VisibleLength, context);
				}
				break;
			}
			case "ul":
			case "ol":
				AppendList(builder, xElement, context, text3 == "ol");
				break;
			case "blockquote":
			{
				HtmlConversionContext htmlConversionContext = context.Clone();
				htmlConversionContext.QuoteDepth++;
				AppendChildBlocks(builder, xElement, htmlConversionContext);
				context.DisplayLength = htmlConversionContext.DisplayLength;
				break;
			}
			case "hr":
				AppendBlockText(builder, ApplyQuotePrefix("---", context.QuoteDepth), "---".Length, context);
				break;
			case "pre":
				AppendCodeBlock(builder, xElement, context, xElement);
				break;
			case "div":
			case "p":
			{
				if (ContainsBlockChildren(xElement))
				{
					AppendChildBlocks(builder, xElement, context);
					break;
				}
				if (IsExplicitBlankLineElement(xElement))
				{
					AppendBlankLine(builder, context);
					break;
				}
				InlineSerializationResult inlineSerializationResult3 = ConvertInlineNodes(xElement.Nodes(), context);
				string text6 = ApplyQuotePrefix(inlineSerializationResult3.RawText, context.QuoteDepth);
				if (!string.IsNullOrEmpty(inlineSerializationResult3.RawText))
				{
					AppendBlockText(builder, text6, inlineSerializationResult3.VisibleLength, context);
				}
				break;
			}
			case "br":
				if (builder.Length > 0 && builder[builder.Length - 1] != '\n')
				{
					builder.AppendLine();
				}
				break;
			default:
			{
				if (ContainsBlockChildren(xElement))
				{
					AppendChildBlocks(builder, xElement, context);
					break;
				}
				InlineSerializationResult inlineSerializationResult = ConvertInlineNodes(new XElement[1] { xElement }, context);
				string text4 = ApplyQuotePrefix(inlineSerializationResult.RawText, context.QuoteDepth);
				if (!string.IsNullOrEmpty(inlineSerializationResult.RawText))
				{
					AppendBlockText(builder, text4, inlineSerializationResult.VisibleLength, context);
				}
				break;
			}
			}
		}
	}

	private static void AppendChildBlocks(StringBuilder builder, XElement element, HtmlConversionContext context)
	{
		AppendBlockNodes(builder, element.Nodes(), context);
	}

	private static void AppendBlockNodes(StringBuilder builder, IEnumerable<XNode> nodes, HtmlConversionContext context)
	{
		List<XNode> list = new List<XNode>();
		foreach (XNode node in nodes)
		{
			if (node is XElement element && (IsBlockElement(element) || ContainsBlockChildren(element) || TryGetStandaloneCodeBlockElement(element, out var _)))
			{
				AppendInlineParagraph(builder, list, context);
				list.Clear();
				AppendBlockNode(builder, node, context);
			}
			else
			{
				list.Add(node);
			}
		}
		AppendInlineParagraph(builder, list, context);
	}

	private static void AppendInlineParagraph(StringBuilder builder, IEnumerable<XNode> nodes, HtmlConversionContext context)
	{
		InlineSerializationResult inlineSerializationResult = ConvertInlineNodes(nodes, context);
		if (!string.IsNullOrWhiteSpace(inlineSerializationResult.RawText))
		{
			AppendBlockText(builder, ApplyQuotePrefix(inlineSerializationResult.RawText, context.QuoteDepth), inlineSerializationResult.VisibleLength, context);
		}
	}

	private static void AppendList(StringBuilder builder, XElement listElement, HtmlConversionContext context, bool ordered)
	{
		HtmlConversionContext htmlConversionContext = context.Clone();
		int num = 1;
		foreach (XElement item in listElement.Elements())
		{
			if (string.Equals(item.Name.LocalName, "li", StringComparison.OrdinalIgnoreCase))
			{
				AppendListItem(builder, item, htmlConversionContext, ordered, num);
				num++;
			}
		}
		context.DisplayLength = htmlConversionContext.DisplayLength;
		if (context.IsListItemContent)
		{
			AppendBlankLine(builder, context);
		}
	}

	private static void AppendListItem(StringBuilder builder, XElement item, HtmlConversionContext context, bool ordered, int index)
	{
		string text = (ordered ? (index + ". ") : "- ");
		string continuationPrefix = new string(' ', text.Length);
		StringBuilder stringBuilder = new StringBuilder();
		HtmlConversionContext htmlConversionContext = context.Clone();
		htmlConversionContext.QuoteDepth = 0;
		htmlConversionContext.IsListItemContent = true;
		AppendBlockNodes(stringBuilder, item.Nodes(), htmlConversionContext);
		AppendBlockText(builder, ApplyQuotePrefix(BuildListItemText(text, continuationPrefix, stringBuilder.ToString()), context.QuoteDepth), Math.Max(0, htmlConversionContext.DisplayLength - context.DisplayLength), context);
	}

	private static void AppendCodeBlock(StringBuilder builder, XElement element, HtmlConversionContext context, XElement languageScope)
	{
		string text = ExtractCodeBlockText(element);
		if (!string.IsNullOrEmpty(text))
		{
			string codeBlockLanguage = GetCodeBlockLanguage(element, languageScope);
			string text2 = "```" + codeBlockLanguage + Environment.NewLine + text.Replace("\n", Environment.NewLine) + Environment.NewLine + "```";
			AppendBlockText(builder, ApplyQuotePrefix(text2, context.QuoteDepth), text.Replace("\n", Environment.NewLine).Length, context);
		}
	}

	private static bool TryGetStandaloneCodeBlockElement(XElement element, out XElement codeBlockElement)
	{
		codeBlockElement = null;
		if (element == null)
		{
			return false;
		}
		string text = element.Name.LocalName.ToLowerInvariant();
		if (text == "pre")
		{
			codeBlockElement = element.Elements().FirstOrDefault((XElement child) => string.Equals(child.Name.LocalName, "code", StringComparison.OrdinalIgnoreCase)) ?? element;
			return true;
		}
		if (text == "code" && IsBlockCodeElement(element))
		{
			codeBlockElement = element;
			return true;
		}
		XElement xElement = element.Elements().FirstOrDefault((XElement child) => string.Equals(child.Name.LocalName, "pre", StringComparison.OrdinalIgnoreCase));
		if (xElement != null && (HasCodeBlockContainerHint(element) || HasOnlyCodeBlockChrome(element, xElement)))
		{
			codeBlockElement = xElement;
			return true;
		}
		XElement xElement2 = element.Elements().FirstOrDefault(IsBlockCodeElement);
		if (xElement2 != null && (HasCodeBlockContainerHint(element) || HasOnlyCodeBlockChrome(element, xElement2)))
		{
			codeBlockElement = xElement2;
			return true;
		}
		if (!HasCodeBlockContainerHint(element))
		{
			return false;
		}
		XElement xElement3 = FindCodeBlockContentElement(element);
		if (xElement3 == null)
		{
			return false;
		}
		codeBlockElement = xElement3;
		return true;
	}

	private static XElement FindCodeBlockContentElement(XElement container)
	{
		if (container == null)
		{
			return null;
		}
		List<XElement> codeElements = container.Descendants().Where(IsCodeBlockLikeElement).ToList();
		if (codeElements.Count == 0)
		{
			return null;
		}
		XElement xElement = container.Elements().FirstOrDefault((XElement child) => codeElements.All((XElement codeElement) => codeElement == child || codeElement.Ancestors().Contains(child)));
		if (xElement != null)
		{
			return xElement;
		}
		if (codeElements.Count != 1)
		{
			return container;
		}
		return codeElements[0];
	}

	private static bool HasOnlyCodeBlockChrome(XElement container, XElement codeBlockElement)
	{
		if (container == null || codeBlockElement == null)
		{
			return false;
		}
		foreach (XNode item in container.Nodes())
		{
			if (item is XText xText)
			{
				if (!string.IsNullOrWhiteSpace(xText.Value))
				{
					return false;
				}
			}
			else if (item is XElement xElement && xElement != codeBlockElement && !xElement.Descendants().Contains(codeBlockElement) && !IsCodeBlockChromeElement(xElement))
			{
				return false;
			}
		}
		return true;
	}

	private static bool IsCodeBlockChromeElement(XElement element)
	{
		if (element == null)
		{
			return false;
		}
		switch (element.Name.LocalName.ToLowerInvariant())
		{
		case "button":
		case "svg":
		case "script":
		case "style":
			return true;
		default:
		{
			string text = element.Attribute("class")?.Value ?? string.Empty;
			if (text.IndexOf("header", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("toolbar", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("copy", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("action", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
			string language;
			return TryNormalizeCodeLanguage(element.Value, out language);
		}
		}
	}

	private static bool IsCodeBlockLikeElement(XElement element)
	{
		if (element != null)
		{
			if (!string.Equals(element.Name.LocalName, "pre", StringComparison.OrdinalIgnoreCase))
			{
				return string.Equals(element.Name.LocalName, "code", StringComparison.OrdinalIgnoreCase);
			}
			return true;
		}
		return false;
	}

	private static bool IsBlockCodeElement(XElement element)
	{
		if (element == null)
		{
			return false;
		}
		string text = element.Name.LocalName.ToLowerInvariant();
		if (text == "pre")
		{
			return true;
		}
		if (text != "code")
		{
			return false;
		}
		string text2 = element.Attribute("class")?.Value ?? string.Empty;
		string text3 = element.Attribute("style")?.Value ?? string.Empty;
		if (!CodeLanguageClassRegex.IsMatch(text2) && text2.IndexOf("hljs", StringComparison.OrdinalIgnoreCase) < 0 && text2.IndexOf("prism", StringComparison.OrdinalIgnoreCase) < 0 && (text3.IndexOf("white-space", StringComparison.OrdinalIgnoreCase) < 0 || text3.IndexOf("pre", StringComparison.OrdinalIgnoreCase) < 0) && (element.Value ?? string.Empty).IndexOf('\n') < 0)
		{
			return element.Elements().Any((XElement child) => IsCodeLineElement(child.Name.LocalName));
		}
		return true;
	}

	private static bool HasCodeBlockContainerHint(XElement element)
	{
		if (element == null)
		{
			return false;
		}
		string text = element.Attribute("class")?.Value ?? string.Empty;
		string text2 = element.Attribute("style")?.Value ?? string.Empty;
		if (text.IndexOf("code", StringComparison.OrdinalIgnoreCase) < 0 && text.IndexOf("highlight", StringComparison.OrdinalIgnoreCase) < 0 && text.IndexOf("syntax", StringComparison.OrdinalIgnoreCase) < 0 && text.IndexOf("shiki", StringComparison.OrdinalIgnoreCase) < 0 && text.IndexOf("hljs", StringComparison.OrdinalIgnoreCase) < 0 && text.IndexOf("prism", StringComparison.OrdinalIgnoreCase) < 0)
		{
			if (text2.IndexOf("font-family", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return text2.IndexOf("mono", StringComparison.OrdinalIgnoreCase) >= 0;
			}
			return false;
		}
		return true;
	}

	private static string ExtractCodeBlockText(XElement element)
	{
		StringBuilder stringBuilder = new StringBuilder();
		AppendCodeText(stringBuilder, element);
		return stringBuilder.ToString().Replace("\r\n", "\n").Replace('\r', '\n')
			.Replace('\u00a0', ' ')
			.Trim('\n');
	}

	private static void AppendCodeText(StringBuilder builder, XNode node)
	{
		if (node is XText xText)
		{
			builder.Append(WebUtility.HtmlDecode(xText.Value ?? string.Empty).Replace('\u00a0', ' '));
		}
		else
		{
			if (!(node is XElement xElement))
			{
				return;
			}
			string text = xElement.Name.LocalName.ToLowerInvariant();
			switch (text)
			{
			case "br":
				AppendCodeLineBreak(builder);
				return;
			case "button":
				return;
			case "script":
				return;
			case "style":
				return;
			case "svg":
				return;
			}
			bool flag = IsCodeLineElement(text);
			if (flag && builder.Length > 0 && builder[builder.Length - 1] != '\n')
			{
				builder.Append('\n');
			}
			foreach (XNode item in xElement.Nodes())
			{
				AppendCodeText(builder, item);
			}
			if (flag && builder.Length > 0 && builder[builder.Length - 1] != '\n')
			{
				builder.Append('\n');
			}
		}
	}

	private static bool IsCodeLineElement(string tagName)
	{
		if (!string.Equals(tagName, "div", StringComparison.OrdinalIgnoreCase) && !string.Equals(tagName, "p", StringComparison.OrdinalIgnoreCase) && !string.Equals(tagName, "li", StringComparison.OrdinalIgnoreCase))
		{
			return string.Equals(tagName, "tr", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private static void AppendCodeLineBreak(StringBuilder builder)
	{
		if (builder.Length == 0 || builder[builder.Length - 1] != '\n')
		{
			builder.Append('\n');
		}
	}

	private static string GetCodeBlockLanguage(XElement codeElement, XElement languageScope)
	{
		for (XElement xElement = codeElement; xElement != null; xElement = xElement.Parent)
		{
			string languageFromElementAttributes = GetLanguageFromElementAttributes(xElement);
			if (!string.IsNullOrWhiteSpace(languageFromElementAttributes))
			{
				return languageFromElementAttributes;
			}
			if (xElement == languageScope)
			{
				break;
			}
		}
		foreach (XElement item in codeElement.Descendants())
		{
			string languageFromElementAttributes2 = GetLanguageFromElementAttributes(item);
			if (!string.IsNullOrWhiteSpace(languageFromElementAttributes2))
			{
				return languageFromElementAttributes2;
			}
		}
		return GetLanguageFromCodeHeader(languageScope, codeElement) ?? string.Empty;
	}

	private static string GetLanguageFromElementAttributes(XElement element)
	{
		if (element == null)
		{
			return null;
		}
		if (TryNormalizeCodeLanguage(element.Attribute("data-language")?.Value ?? element.Attribute("data-lang")?.Value ?? element.Attribute("lang")?.Value, out var language))
		{
			return language;
		}
		string text = element.Attribute("class")?.Value;
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		Match match = CodeLanguageClassRegex.Match(text);
		if (!match.Success || !TryNormalizeCodeLanguage(match.Groups["value"].Value, out language))
		{
			return null;
		}
		return language;
	}

	private static string GetLanguageFromCodeHeader(XElement scope, XElement codeElement)
	{
		if (scope == null || scope == codeElement)
		{
			return null;
		}
		foreach (XElement item in scope.Elements())
		{
			if (item == codeElement || item.Descendants().Contains(codeElement))
			{
				break;
			}
			if (TryNormalizeCodeLanguage(item.Value, out var language))
			{
				return language;
			}
		}
		return null;
	}

	private static bool TryNormalizeCodeLanguage(string value, out string language)
	{
		language = null;
		if (string.IsNullOrWhiteSpace(value))
		{
			return false;
		}
		string input = WebUtility.HtmlDecode(value).Replace('\u00a0', ' ').Replace("复制代码", string.Empty)
			.Replace("复制", string.Empty)
			.Replace("下载", string.Empty)
			.Replace("Copy", string.Empty)
			.Replace("copy", string.Empty)
			.Replace("Download", string.Empty)
			.Replace("download", string.Empty);
		input = Regex.Replace(input, "\\s+", " ").Trim();
		if (string.IsNullOrWhiteSpace(input))
		{
			return false;
		}
		string text = input.Split(new char[6] { ' ', '\t', '\n', '\r', '|', '/' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
		if (string.IsNullOrWhiteSpace(text) || text.Length > 32 || !Regex.IsMatch(text, "^[A-Za-z0-9_+#.-]+$"))
		{
			return false;
		}
		language = text;
		return true;
	}

	private static InlineSerializationResult ConvertInlineNodes(IEnumerable<XNode> nodes, HtmlConversionContext context)
	{
		List<InlineTextRun> runs = new List<InlineTextRun>();
		foreach (XNode node in nodes)
		{
			InlineStyleState inlineStyleState = new InlineStyleState();
			foreach (XElement item in node.Ancestors().Reverse())
			{
				inlineStyleState = CreateChildInlineStyle(item.Name.LocalName.ToLowerInvariant(), item, inlineStyleState);
			}
			CollectInlineRuns(runs, new XNode[1] { node }, context, inlineStyleState);
		}
		return SerializeInlineRuns(runs, context.DisplayLength);
	}

	private static void CollectInlineRuns(List<InlineTextRun> runs, IEnumerable<XNode> nodes, HtmlConversionContext context, InlineStyleState style)
	{
		if (nodes == null)
		{
			return;
		}
		foreach (XNode node in nodes)
		{
			if (node is XText xText)
			{
				string text = NormalizeInlineWhitespace(WebUtility.HtmlDecode(xText.Value ?? string.Empty));
				string inlineWhiteSpace = GetInlineWhiteSpace(xText);
				bool flag = inlineWhiteSpace == "pre" || inlineWhiteSpace == "pre-wrap" || inlineWhiteSpace == "break-spaces";
				if (!flag)
				{
					text = Regex.Replace(text, (inlineWhiteSpace == "pre-line") ? "[ \\t\\f]+" : "[ \\t\\r\\n\\f]+", " ");
				}
				AppendInlineRun(runs, text, style, flag);
			}
			else
			{
				if (!(node is XElement xElement))
				{
					continue;
				}
				string text2 = xElement.Name.LocalName.ToLowerInvariant();
				switch (text2)
				{
				case "br":
					AppendInlineRun(runs, Environment.NewLine, style);
					continue;
				case "meta":
				case "style":
				case "script":
				case "head":
					continue;
				}
				if (!IsListTag(text2) && !(text2 == "blockquote") && !(text2 == "pre"))
				{
					InlineStyleState style2 = CreateChildInlineStyle(text2, xElement, style);
					CollectInlineRuns(runs, xElement.Nodes(), context, style2);
				}
			}
		}
	}

	private static InlineStyleState CreateChildInlineStyle(string tagName, XElement element, InlineStyleState parentStyle)
	{
		InlineStyleState inlineStyleState = parentStyle.Clone();
		Dictionary<string, string> dictionary = ParseStyleAttributes(element.Attribute("style")?.Value);
		if (tagName == "a")
		{
			string text = element.Attribute("href")?.Value;
			if (!string.IsNullOrWhiteSpace(text))
			{
				inlineStyleState.LinkUrl = text.Trim();
			}
		}
		if (tagName == "code")
		{
			inlineStyleState.Code = true;
		}
		if (tagName == "strong" || tagName == "b" || HasBoldStyle(dictionary))
		{
			inlineStyleState.Strong = true;
		}
		if (tagName == "em" || tagName == "i" || HasItalicStyle(dictionary))
		{
			inlineStyleState.Emphasis = true;
		}
		if (dictionary.TryGetValue("font-weight", out var value))
		{
			value = NormalizeCssKeyword(value);
			if (value == "normal" || value == "initial" || (int.TryParse(value, out var result) && result < 600))
			{
				inlineStyleState.Strong = false;
			}
		}
		if (dictionary.TryGetValue("font-style", out var value2) && (NormalizeCssKeyword(value2) == "normal" || NormalizeCssKeyword(value2) == "initial"))
		{
			inlineStyleState.Emphasis = false;
		}
		if (tagName == "u" || HasUnderlineStyle(dictionary))
		{
			inlineStyleState.Underline = true;
		}
		switch (tagName)
		{
		default:
			if (!HasStrikeStyle(dictionary))
			{
				break;
			}
			goto case "s";
		case "s":
		case "strike":
		case "del":
			inlineStyleState.Strikethrough = true;
			break;
		}
		string highlightColor = GetHighlightColor(tagName, dictionary);
		if (!string.IsNullOrWhiteSpace(highlightColor))
		{
			inlineStyleState.HighlightColor = highlightColor;
		}
		return inlineStyleState;
	}

	private static List<InlineWrapper> BuildInlineWrappers(string tagName, XElement element)
	{
		List<InlineWrapper> list = new List<InlineWrapper>();
		Dictionary<string, string> styles = ParseStyleAttributes(element.Attribute("style")?.Value);
		if (tagName == "strong" || tagName == "b" || HasBoldStyle(styles))
		{
			list.Add(new InlineWrapper((string text) => WrapWithDelimiter(text, "**")));
		}
		if (tagName == "em" || tagName == "i" || HasItalicStyle(styles))
		{
			list.Add(new InlineWrapper((string text) => WrapWithDelimiter(text, "*")));
		}
		if (tagName == "u" || HasUnderlineStyle(styles))
		{
			list.Add(new InlineWrapper((string text) => WrapWithDelimiter(text, "~")));
		}
		switch (tagName)
		{
		default:
			if (!HasStrikeStyle(styles))
			{
				break;
			}
			goto case "s";
		case "s":
		case "strike":
		case "del":
			list.Add(new InlineWrapper((string text) => WrapWithDelimiter(text, "~~")));
			break;
		}
		string highlightColor = GetHighlightColor(tagName, styles);
		if (!string.IsNullOrWhiteSpace(highlightColor))
		{
			list.Add(new InlineWrapper((string text) => "=={" + highlightColor + "}" + text + "=="));
		}
		return list;
	}

	private static string GetInlineWhiteSpace(XNode node)
	{
		for (XElement parent = node.Parent; parent != null; parent = parent.Parent)
		{
			if (ParseStyleAttributes(parent.Attribute("style")?.Value).TryGetValue("white-space", out var value))
			{
				value = NormalizeCssKeyword(value);
				switch (value)
				{
				case "normal":
				case "nowrap":
				case "pre-line":
				case "pre-wrap":
				case "pre":
				case "break-spaces":
					return value;
				case "revert":
				case "initial":
					return "normal";
				}
			}
		}
		return "normal";
	}

	private static string NormalizeCssKeyword(string value)
	{
		return Regex.Replace(value ?? string.Empty, "\\s*!important\\s*$", string.Empty, RegexOptions.IgnoreCase).Trim().ToLowerInvariant();
	}

	private static void AppendInlineRun(List<InlineTextRun> runs, string text, InlineStyleState style, bool preserveWhitespace = false)
	{
		if (!string.IsNullOrEmpty(text))
		{
			if (runs.Count > 0 && runs[runs.Count - 1].PreserveWhitespace == preserveWhitespace && runs[runs.Count - 1].HasSameStyle(style) && !ContainsLineBreak(runs[runs.Count - 1].Text) && !ContainsLineBreak(text))
			{
				runs[runs.Count - 1].Text += text;
				return;
			}
			runs.Add(new InlineTextRun
			{
				Text = text,
				PreserveWhitespace = preserveWhitespace,
				Strong = style.Strong,
				Emphasis = style.Emphasis,
				Underline = style.Underline,
				Strikethrough = style.Strikethrough,
				Code = style.Code,
				HighlightColor = style.HighlightColor,
				LinkUrl = style.LinkUrl
			});
		}
	}

	private static bool ContainsLineBreak(string text)
	{
		if (!string.IsNullOrEmpty(text))
		{
			if (text.IndexOf('\n') < 0)
			{
				return text.IndexOf('\r') >= 0;
			}
			return true;
		}
		return false;
	}

	private static InlineSerializationResult SerializeInlineRuns(IEnumerable<InlineTextRun> runs, int baseDisplayOffset)
	{
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		bool flag = false;
		foreach (InlineTextRun item in runs ?? Enumerable.Empty<InlineTextRun>())
		{
			string[] array = item.Text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
			for (int i = 0; i < array.Length; i++)
			{
				if (i > 0)
				{
					stringBuilder.Append(Environment.NewLine);
					num += Environment.NewLine.Length;
					flag = false;
				}
				string text = (item.PreserveWhitespace ? array[i] : Regex.Replace(array[i], "[ \\t]+", " "));
				if (item.PreserveWhitespace)
				{
					if (text.Length != 0)
					{
						if (flag && stringBuilder.Length > 0 && stringBuilder[stringBuilder.Length - 1] != '\n')
						{
							stringBuilder.Append(' ');
							num++;
						}
						int num2 = text.Length - text.TrimStart(' ', '\t').Length;
						int num3 = Math.Max(num2, text.TrimEnd(' ', '\t').Length);
						stringBuilder.Append(text.Substring(0, num2));
						if (num3 > num2)
						{
							stringBuilder.Append(ApplyInlineStyle(text.Substring(num2, num3 - num2), item, baseDisplayOffset + num + num2));
						}
						stringBuilder.Append(text.Substring(num3));
						num += text.Length;
						flag = false;
					}
					continue;
				}
				string text2 = text.Trim(' ', '\t');
				if (text.Length > 0 && (text[0] == ' ' || text[0] == '\t'))
				{
					flag = true;
				}
				if (text2.Length != 0)
				{
					if (flag && stringBuilder.Length > 0 && stringBuilder[stringBuilder.Length - 1] != '\n')
					{
						stringBuilder.Append(' ');
						num++;
					}
					stringBuilder.Append(ApplyInlineStyle(text2, item, baseDisplayOffset + num));
					num += text2.Length;
					flag = text[text.Length - 1] == ' ' || text[text.Length - 1] == '\t';
				}
			}
		}
		return new InlineSerializationResult
		{
			RawText = stringBuilder.ToString(),
			VisibleLength = num
		};
	}

	private static string ApplyInlineStyle(string text, InlineTextRun run, int displayOffset)
	{
		string text2 = EscapeLiteralEmphasisText(text);
		bool flag = run.Strong && run.Emphasis;
		bool flag2 = run.Strikethrough && run.Underline;
		if (run.Code)
		{
			text2 = WrapWithDelimiter(text2, "`");
		}
		if (run.Emphasis && !flag)
		{
			text2 = WrapWithDelimiter(text2, "*");
		}
		if (run.Underline && !flag2)
		{
			text2 = WrapWithDelimiter(text2, "~");
		}
		if (run.Strong && !flag)
		{
			text2 = WrapWithDelimiter(text2, "**");
		}
		if (run.Strikethrough && !flag2)
		{
			text2 = WrapWithDelimiter(text2, "~~");
		}
		if (!string.IsNullOrWhiteSpace(run.HighlightColor))
		{
			text2 = "=={" + run.HighlightColor + "}" + text2 + "==";
		}
		if (flag)
		{
			text2 = "***" + text2 + "***";
		}
		if (flag2)
		{
			text2 = "~~~" + text2 + "~~~";
		}
		if (!string.IsNullOrWhiteSpace(run.LinkUrl))
		{
			text2 = "[" + text2 + "](" + run.LinkUrl + ")";
		}
		return text2;
	}

	private static string EscapeLiteralEmphasisText(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text ?? string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		foreach (char c in text)
		{
			if (c == '\\' || c == '*')
			{
				stringBuilder.Append('\\');
			}
			stringBuilder.Append(c);
		}
		return stringBuilder.ToString();
	}

	private static Dictionary<string, string> ParseStyleAttributes(string styleText)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (string.IsNullOrWhiteSpace(styleText))
		{
			return dictionary;
		}
		string[] array = styleText.Split(new char[1] { ';' }, StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < array.Length; i++)
		{
			string[] array2 = array[i].Split(new char[1] { ':' }, 2);
			if (array2.Length == 2)
			{
				string text = array2[0].Trim();
				string value = array2[1].Trim();
				if (!string.IsNullOrEmpty(text))
				{
					dictionary[text] = value;
				}
			}
		}
		return dictionary;
	}

	private static bool HasBoldStyle(IReadOnlyDictionary<string, string> styles)
	{
		if (!styles.TryGetValue("font-weight", out var value))
		{
			return false;
		}
		if (value.IndexOf("bold", StringComparison.OrdinalIgnoreCase) < 0)
		{
			if (int.TryParse(value, out var result))
			{
				return result >= 600;
			}
			return false;
		}
		return true;
	}

	private static bool HasItalicStyle(IReadOnlyDictionary<string, string> styles)
	{
		if (styles.TryGetValue("font-style", out var value))
		{
			return value.IndexOf("italic", StringComparison.OrdinalIgnoreCase) >= 0;
		}
		return false;
	}

	private static bool HasUnderlineStyle(IReadOnlyDictionary<string, string> styles)
	{
		if (styles.TryGetValue("text-decoration", out var value))
		{
			return value.IndexOf("underline", StringComparison.OrdinalIgnoreCase) >= 0;
		}
		return false;
	}

	private static bool HasStrikeStyle(IReadOnlyDictionary<string, string> styles)
	{
		if (styles.TryGetValue("text-decoration", out var value))
		{
			return value.IndexOf("line-through", StringComparison.OrdinalIgnoreCase) >= 0;
		}
		return false;
	}

	private static string GetHighlightColor(string tagName, IReadOnlyDictionary<string, string> styles)
	{
		if (string.Equals(tagName, "mark", StringComparison.OrdinalIgnoreCase))
		{
			if (!styles.TryGetValue("background-color", out var value))
			{
				return "#FFFFD64E";
			}
			if (!IsIncidentalHtmlBackground(value))
			{
				return NormalizeColor(value);
			}
			return null;
		}
		if (!styles.TryGetValue("background-color", out var value2) || IsIncidentalHtmlBackground(value2, ignoreNeutralGray: true))
		{
			return null;
		}
		return NormalizeColor(value2);
	}

	private static bool IsIncidentalHtmlBackground(string color, bool ignoreNeutralGray = false)
	{
		if (!TryConvertCssColor(color, out var parsedColor))
		{
			return false;
		}
		if (parsedColor.A != 0 && (!ignoreNeutralGray || parsedColor.R != parsedColor.G || parsedColor.G != parsedColor.B) && (parsedColor.R > 40 || parsedColor.G > 40 || parsedColor.B > 40))
		{
			if (parsedColor.R >= 240 && parsedColor.G >= 240)
			{
				return parsedColor.B >= 240;
			}
			return false;
		}
		return true;
	}

	private static string NormalizeColor(string color)
	{
		if (string.IsNullOrWhiteSpace(color))
		{
			return null;
		}
		if (TryConvertCssColor(color, out var parsedColor))
		{
			return MapToNearestHighlightEditorColor(parsedColor);
		}
		return HighlightColorMenuState.GetEditorColor(color.Trim()) ?? color.Trim();
	}

	private static bool TryConvertCssColor(string color, out Color parsedColor)
	{
		parsedColor = default(Color);
		if (string.IsNullOrWhiteSpace(color))
		{
			return false;
		}
		string text = color.Trim();
		if (TryParseCssColor(text, out parsedColor))
		{
			return true;
		}
		try
		{
			parsedColor = (Color)ColorConverter.ConvertFromString(text);
			return true;
		}
		catch (Exception)
		{
			parsedColor = default(Color);
			return false;
		}
	}

	private static bool ContainsBlockChildren(XElement element)
	{
		return element.Descendants().Any(IsBlockElement);
	}

	private static bool IsBlockElement(XElement element)
	{
		string text = element.Name.LocalName.ToLowerInvariant();
		switch (text)
		{
		case "ul":
		case "ol":
		case "blockquote":
		case "pre":
		case "hr":
		case "h1":
		case "h2":
		case "h3":
		case "h4":
		case "h5":
		case "h6":
			return true;
		default:
		{
			if (ParseStyleAttributes(element.Attribute("style")?.Value).TryGetValue("display", out var value))
			{
				switch (NormalizeCssKeyword(value))
				{
				case "block":
				case "table":
				case "flow-root":
				case "list-item":
				case "table-row":
				case "flex":
				case "grid":
				case "table-footer-group":
				case "table-header-group":
				case "table-row-group":
				case "table-cell":
				case "table-caption":
					return true;
				case "inline-block":
				case "inline-table":
				case "inline-flex":
				case "inline-grid":
				case "inline":
				case "contents":
					return false;
				}
			}
			switch (text)
			{
			default:
				return text == "h6";
			case "html":
			case "body":
			case "hr":
			case "p":
			case "div":
			case "ul":
			case "ol":
			case "li":
			case "blockquote":
			case "pre":
			case "section":
			case "article":
			case "main":
			case "header":
			case "footer":
			case "nav":
			case "aside":
			case "address":
			case "figure":
			case "figcaption":
			case "dl":
			case "dt":
			case "dd":
			case "table":
			case "thead":
			case "tbody":
			case "tfoot":
			case "tr":
			case "td":
			case "th":
			case "caption":
			case "form":
			case "fieldset":
			case "legend":
			case "details":
			case "summary":
			case "menu":
			case "hgroup":
			case "search":
			case "dialog":
			case "center":
			case "h1":
			case "h2":
			case "h3":
			case "h4":
			case "h5":
				return true;
			}
		}
		}
	}

	private static bool IsListTag(string tagName)
	{
		if (!string.Equals(tagName, "ul", StringComparison.OrdinalIgnoreCase))
		{
			return string.Equals(tagName, "ol", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private static bool IsFormattingWhitespaceNode(XNode node)
	{
		if (node is XText xText)
		{
			return string.IsNullOrWhiteSpace(xText.Value);
		}
		return false;
	}

	private static string BuildListItemText(string firstLinePrefix, string continuationPrefix, string rawText)
	{
		if (string.IsNullOrWhiteSpace(rawText))
		{
			return string.Empty;
		}
		List<string> list = (from line in rawText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')
			select line?.TrimEnd() ?? string.Empty).ToList();
		while (list.Count > 0 && string.IsNullOrWhiteSpace(list[0]))
		{
			list.RemoveAt(0);
		}
		while (list.Count > 0 && string.IsNullOrWhiteSpace(list[list.Count - 1]))
		{
			list.RemoveAt(list.Count - 1);
		}
		if (list.Count == 0)
		{
			return string.Empty;
		}
		for (int num = 0; num < list.Count; num++)
		{
			list[num] = ((num == 0) ? firstLinePrefix : continuationPrefix) + list[num];
		}
		return string.Join(Environment.NewLine, list);
	}

	private static string ApplyQuotePrefix(string text, int quoteDepth)
	{
		if (quoteDepth <= 0 || string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		string prefix = string.Concat(Enumerable.Repeat("> ", quoteDepth));
		string[] source = text.Replace("\r\n", "\n").Split('\n');
		return string.Join(Environment.NewLine, source.Select((string line) => (!string.IsNullOrWhiteSpace(line)) ? (prefix + line) : prefix.TrimEnd()));
	}

	private static bool IsExplicitBlankLineElement(XElement element)
	{
		if (element == null)
		{
			return false;
		}
		if (element.Nodes().All((XNode node) => node is XText xText && string.IsNullOrWhiteSpace(xText.Value)))
		{
			return true;
		}
		return element.Nodes().All(delegate(XNode node)
		{
			if (node is XText xText)
			{
				return string.IsNullOrWhiteSpace(xText.Value);
			}
			return node is XElement xElement && string.Equals(xElement.Name.LocalName, "br", StringComparison.OrdinalIgnoreCase);
		});
	}

	private static void AppendBlockText(StringBuilder builder, string text, int visibleLength, HtmlConversionContext context)
	{
		if (!string.IsNullOrWhiteSpace(text))
		{
			if (builder.Length > 0 && builder[builder.Length - 1] != '\n')
			{
				builder.AppendLine();
				context.DisplayLength += Environment.NewLine.Length;
			}
			builder.Append(text.TrimEnd());
			if (builder.Length == 0 || builder[builder.Length - 1] != '\n')
			{
				builder.AppendLine();
			}
			context.DisplayLength += Math.Max(0, visibleLength) + Environment.NewLine.Length;
		}
	}

	private static void AppendBlankLine(StringBuilder builder, HtmlConversionContext context)
	{
		if (builder.Length != 0)
		{
			if (builder[builder.Length - 1] != '\n')
			{
				builder.AppendLine();
				context.DisplayLength += Environment.NewLine.Length;
			}
			builder.AppendLine();
			context.DisplayLength += Environment.NewLine.Length;
		}
	}

	private static string NormalizeMarkdown(string markdown)
	{
		if (string.IsNullOrWhiteSpace(markdown))
		{
			return string.Empty;
		}
		return markdown.Replace("\r\n", "\n").Replace('\r', '\n').Trim('\n')
			.Replace("\n", Environment.NewLine);
	}

	private static string NormalizeInlineWhitespace(string text)
	{
		if (!string.IsNullOrEmpty(text))
		{
			return text.Replace('\u00a0', ' ');
		}
		return string.Empty;
	}

	private static string NormalizeInlineText(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}
		string input = text.Replace("\r\n", "\n").Replace('\u00a0', ' ');
		input = Regex.Replace(input, "[ \\t]+", " ");
		input = Regex.Replace(input, " ?\\n ?", "\n");
		input = input.Trim(' ', '\t');
		if (input.Length == 0)
		{
			return string.Empty;
		}
		if (input.All((char ch) => ch == '\n'))
		{
			return input;
		}
		return input.Trim('\n');
	}

	private static string WrapWithDelimiter(string text, string delimiter)
	{
		if (!string.IsNullOrWhiteSpace(text))
		{
			return delimiter + text + delimiter;
		}
		return string.Empty;
	}

	private static bool TryParseCssColor(string color, out Color parsedColor)
	{
		parsedColor = default(Color);
		Match match = RgbaColorRegex.Match(color ?? string.Empty);
		if (!match.Success)
		{
			return false;
		}
		if (!byte.TryParse(match.Groups["r"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) || !byte.TryParse(match.Groups["g"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result2) || !byte.TryParse(match.Groups["b"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result3))
		{
			return false;
		}
		byte a = byte.MaxValue;
		if (match.Groups["a"].Success && double.TryParse(match.Groups["a"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result4))
		{
			result4 = Math.Max(0.0, Math.Min(1.0, result4));
			a = (byte)Math.Round(result4 * 255.0);
		}
		parsedColor = Color.FromArgb(a, result, result2, result3);
		return true;
	}

	private static string MapToNearestHighlightEditorColor(Color color)
	{
		byte b = Math.Max(color.R, Math.Max(color.G, color.B));
		byte b2 = Math.Min(color.R, Math.Min(color.G, color.B));
		if (b == 0 || (double)(b - b2) / (double)(int)b < 0.1)
		{
			return null;
		}
		double value = GetColorHue(color).Value;
		HighlightColorData highlightColorData = null;
		double num = double.MaxValue;
		foreach (HighlightColorData highlightColor in HighlightColorMenuState.HighlightColors)
		{
			double hueDifference = GetHueDifference(value, highlightColor.OptionColor);
			double hueDifference2 = GetHueDifference(value, highlightColor.EditorColor);
			double num2 = Math.Min(hueDifference, hueDifference2);
			if (num2 < num)
			{
				num = num2;
				highlightColorData = highlightColor;
			}
		}
		if (highlightColorData == null || !(num <= 15.0))
		{
			return null;
		}
		return highlightColorData.EditorColor;
	}

	private static double GetHueDifference(double sourceHue, string targetColor)
	{
		try
		{
			double? colorHue = GetColorHue((Color)ColorConverter.ConvertFromString(targetColor));
			if (!colorHue.HasValue)
			{
				return double.MaxValue;
			}
			double num = Math.Abs(sourceHue - colorHue.Value);
			return Math.Min(num, 360.0 - num);
		}
		catch (Exception)
		{
			return double.MaxValue;
		}
	}

	private static double? GetColorHue(Color color)
	{
		byte b = Math.Max(color.R, Math.Max(color.G, color.B));
		byte b2 = Math.Min(color.R, Math.Min(color.G, color.B));
		int num = b - b2;
		if (num == 0)
		{
			return null;
		}
		return (((b == color.R) ? ((double)(color.G - color.B) / (double)num) : ((b == color.G) ? (2.0 + (double)(color.B - color.R) / (double)num) : (4.0 + (double)(color.R - color.G) / (double)num))) * 60.0 + 360.0) % 360.0;
	}
}
