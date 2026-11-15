using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using ICSharpCode.AvalonEdit.Highlighting;
using TickTickUtils.Lang;
using ticktick_WPF.Framework.Markdown;

namespace ticktick_WPF.Views.MarkDown;

public static class MarkdownClipboardHtmlRenderer
{
	public static string CreatePlainText(string markdown, bool preserveLinks = false)
	{
		if (string.IsNullOrEmpty(markdown))
		{
			return string.Empty;
		}
		try
		{
			MarkdownSemanticDocument markdownSemanticDocument = MarkdownSemanticConverter.Parse(markdown);
			if (markdownSemanticDocument?.Blocks == null || markdownSemanticDocument.Blocks.Count == 0)
			{
				return markdown;
			}
			List<string> values = markdownSemanticDocument.Blocks.Select((MarkdownBlockNode block) => GetPlainTextLine(block, preserveLinks)).ToList();
			return string.Join(markdown.Contains("\r\n") ? "\r\n" : (markdown.Contains("\r") ? "\r" : "\n"), values);
		}
		catch (Exception)
		{
			return markdown;
		}
	}

	public static string CreateHtml(string markdown)
	{
		if (string.IsNullOrEmpty(markdown))
		{
			return string.Empty;
		}
		try
		{
			string text = BuildHtmlFragment(MarkdownSemanticConverter.Parse(markdown));
			if (string.IsNullOrWhiteSpace(text))
			{
				return string.Empty;
			}
			DataObject dataObject = new DataObject();
			HtmlClipboard.SetHtml(dataObject, text);
			return (dataObject.GetData(DataFormats.Html) as string) ?? string.Empty;
		}
		catch (Exception ex)
		{
			UtilLog.Error("Clipboard.Html: " + ex.Message);
			return string.Empty;
		}
	}

	private static string BuildHtmlFragment(MarkdownSemanticDocument document)
	{
		if (document?.Blocks == null || document.Blocks.Count == 0)
		{
			return string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<div style=\"font-family:'Microsoft YaHei UI','Microsoft YaHei',sans-serif;font-size:14px;line-height:1.5;\">");
		int num = 0;
		while (num < document.Blocks.Count)
		{
			MarkdownBlockNode markdownBlockNode = document.Blocks[num];
			if (markdownBlockNode == null)
			{
				num++;
				continue;
			}
			if (markdownBlockNode.Kind == MarkdownBlockKind.List)
			{
				num = AppendListHtml(stringBuilder, document.Blocks, num);
				continue;
			}
			AppendBlockHtml(stringBuilder, markdownBlockNode);
			num++;
		}
		stringBuilder.Append("</div>");
		return stringBuilder.ToString();
	}

	private static string GetPlainTextLine(MarkdownBlockNode block, bool preserveLinks)
	{
		if (block == null)
		{
			return string.Empty;
		}
		string text = new string(' ', Math.Max(0, block.Indent));
		if (block.Kind == MarkdownBlockKind.CodeBlock)
		{
			return block.CodeText ?? string.Empty;
		}
		if (block.Kind == MarkdownBlockKind.FileBlock)
		{
			return block.Caption ?? block.AltText ?? block.Source ?? string.Empty;
		}
		string inlinePlainText = MarkdownSemanticConverter.GetInlinePlainText(block.Inlines, preserveLinks);
		return block.Kind switch
		{
			MarkdownBlockKind.List => block.ListKind switch
			{
				MarkdownListKind.Task => text + ((block.Checked == true) ? "☑ " : "☐ ") + inlinePlainText, 
				MarkdownListKind.Numbered => text + (string.IsNullOrWhiteSpace(block.CustomOrder) ? "1" : block.CustomOrder) + ". " + inlinePlainText, 
				_ => text + "• " + inlinePlainText, 
			}, 
			MarkdownBlockKind.BlockQuote => text + "> " + inlinePlainText, 
			MarkdownBlockKind.HorizontalRule => text + "---", 
			_ => text + inlinePlainText, 
		};
	}

	private static int AppendListHtml(StringBuilder builder, IList<MarkdownBlockNode> blocks, int startIndex)
	{
		int num;
		for (num = startIndex; num < blocks.Count; num = AppendListLevelHtml(builder, blocks, num, Math.Max(0, blocks[num].Indent), isNested: false))
		{
			MarkdownBlockNode markdownBlockNode = blocks[num];
			if (markdownBlockNode == null || markdownBlockNode.Kind != MarkdownBlockKind.List)
			{
				break;
			}
		}
		return num;
	}

	private static int AppendListLevelHtml(StringBuilder builder, IList<MarkdownBlockNode> blocks, int startIndex, int indent, bool isNested)
	{
		MarkdownBlockNode markdownBlockNode = blocks[startIndex];
		MarkdownListKind valueOrDefault = markdownBlockNode.ListKind.GetValueOrDefault();
		string text = ((valueOrDefault == MarkdownListKind.Numbered) ? "ol" : "ul");
		AppendListOpeningTag(builder, text, valueOrDefault, markdownBlockNode, isNested);
		int num = startIndex;
		while (num < blocks.Count)
		{
			MarkdownBlockNode markdownBlockNode2 = blocks[num];
			if (markdownBlockNode2 == null || markdownBlockNode2.Kind != MarkdownBlockKind.List || Math.Max(0, markdownBlockNode2.Indent) != indent || markdownBlockNode2.ListKind.GetValueOrDefault() != valueOrDefault)
			{
				break;
			}
			builder.Append("<li style=\"margin:0 0 4px 0;\">");
			builder.Append(GetTaskListPrefixHtml(markdownBlockNode2));
			builder.Append(RenderInlinesHtml(markdownBlockNode2.Inlines));
			for (num++; num < blocks.Count; num = AppendListLevelHtml(builder, blocks, num, Math.Max(0, blocks[num].Indent), isNested: true))
			{
				MarkdownBlockNode markdownBlockNode3 = blocks[num];
				if (markdownBlockNode3 == null || markdownBlockNode3.Kind != MarkdownBlockKind.List || Math.Max(0, blocks[num].Indent) <= indent)
				{
					break;
				}
			}
			builder.Append("</li>");
		}
		builder.Append("</").Append(text).Append('>');
		return num;
	}

	private static void AppendListOpeningTag(StringBuilder builder, string tag, MarkdownListKind listKind, MarkdownBlockNode firstBlock, bool isNested)
	{
		builder.Append('<').Append(tag);
		int orderedListStart = GetOrderedListStart(firstBlock);
		if (orderedListStart > 1 && tag == "ol")
		{
			builder.Append(" start=\"").Append(orderedListStart).Append('"');
		}
		if (listKind == MarkdownListKind.Task)
		{
			builder.Append(" style=\"margin:0 0 8px ").Append(isNested ? "24" : "0").Append("px;padding:0;list-style:none;\">");
		}
		else
		{
			builder.Append(" style=\"margin:0 0 8px 24px;padding:0;\">");
		}
	}

	private static string GetTaskListPrefixHtml(MarkdownBlockNode block)
	{
		if (block == null || block.ListKind != MarkdownListKind.Task)
		{
			return string.Empty;
		}
		if (block.Checked != true)
		{
			return "&#9744; ";
		}
		return "&#9745; ";
	}

	private static int GetOrderedListStart(MarkdownBlockNode block)
	{
		if (block != null && block.ListKind == MarkdownListKind.Numbered && int.TryParse(block.CustomOrder, out var result) && result > 1)
		{
			return result;
		}
		return 1;
	}

	private static void AppendBlockHtml(StringBuilder builder, MarkdownBlockNode block)
	{
		switch (block.Kind)
		{
		case MarkdownBlockKind.Heading:
		{
			int value = Math.Max(1, Math.Min(6, block.Level ?? 1));
			builder.Append("<h").Append(value).Append(" style=\"margin:12px 0 8px 0;font-weight:700;\">")
				.Append(RenderInlinesHtml(block.Inlines))
				.Append("</h")
				.Append(value)
				.Append('>');
			break;
		}
		case MarkdownBlockKind.BlockQuote:
			builder.Append("<blockquote style=\"margin:0 0 8px 0;padding:0 0 0 12px;border-left:3px solid #D0D5DD;color:#667085;\">").Append(RenderParagraphContentHtml(block.Inlines)).Append("</blockquote>");
			break;
		case MarkdownBlockKind.HorizontalRule:
			builder.Append("<hr style=\"border:0;border-top:1px solid #D9D9D9;margin:10px 0;\" />");
			break;
		case MarkdownBlockKind.CodeBlock:
			builder.Append("<pre style=\"margin:0 0 8px 0;padding:8px 10px;background:#F2F2F2;border-radius:4px;font-family:Consolas,'Courier New',monospace;font-size:13px;white-space:pre-wrap;\"><code");
			if (!string.IsNullOrWhiteSpace(block.Language))
			{
				builder.Append(" class=\"language-").Append(HtmlEncodeAttribute(block.Language.Trim())).Append("\"");
			}
			builder.Append(">").Append(HtmlEncodeText(block.CodeText ?? string.Empty)).Append("</code></pre>");
			break;
		case MarkdownBlockKind.FileBlock:
			AppendFileBlockHtml(builder, block);
			break;
		default:
			builder.Append("<p style=\"margin:0 0 8px 0;\">").Append(RenderParagraphContentHtml(block.Inlines)).Append("</p>");
			break;
		}
	}

	private static void AppendFileBlockHtml(StringBuilder builder, MarkdownBlockNode block)
	{
		string text = block?.Source ?? string.Empty;
		string text2 = (string.IsNullOrEmpty(block?.AltText) ? text : block.AltText);
		if (block != null && block.FileKind == MarkdownFileBlockKind.Image && !string.IsNullOrEmpty(text))
		{
			builder.Append("<p style=\"margin:0 0 8px 0;\"><img src=\"").Append(HtmlEncodeAttribute(text)).Append("\" alt=\"")
				.Append(HtmlEncodeAttribute(text2))
				.Append("\" /></p>");
			return;
		}
		builder.Append("<p style=\"margin:0 0 8px 0;\">");
		if (!string.IsNullOrEmpty(text))
		{
			builder.Append("<a href=\"").Append(HtmlEncodeAttribute(text)).Append("\">")
				.Append(HtmlEncodeText(text2))
				.Append("</a>");
		}
		else
		{
			builder.Append(HtmlEncodeText(text2));
		}
		builder.Append("</p>");
	}

	private static string RenderParagraphContentHtml(IList<MarkdownInlineNode> inlines)
	{
		string text = RenderInlinesHtml(inlines);
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return "&nbsp;";
	}

	private static string RenderInlinesHtml(IList<MarkdownInlineNode> inlines)
	{
		if (inlines == null || inlines.Count == 0)
		{
			return string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (MarkdownInlineNode inline in inlines)
		{
			if (inline != null)
			{
				stringBuilder.Append(RenderInlineHtml(inline));
			}
		}
		return stringBuilder.ToString();
	}

	private static string RenderInlineHtml(MarkdownInlineNode inline)
	{
		string text = HtmlEncodeText(inline?.Text ?? string.Empty);
		if (inline?.Marks == null || inline.Marks.Count == 0)
		{
			return text;
		}
		string text2 = text;
		foreach (MarkdownMark item in inline.Marks.OrderByDescending(GetMarkPriority))
		{
			if (item != null)
			{
				text2 = WrapInlineHtml(text2, item);
			}
		}
		return text2;
	}

	private static int GetMarkPriority(MarkdownMark mark)
	{
		return mark?.Kind switch
		{
			MarkdownMarkKind.Link => 1, 
			MarkdownMarkKind.Highlight => 2, 
			MarkdownMarkKind.Code => 3, 
			MarkdownMarkKind.Strong => 4, 
			MarkdownMarkKind.Emphasis => 5, 
			MarkdownMarkKind.Underline => 6, 
			MarkdownMarkKind.Strikethrough => 7, 
			_ => 100, 
		};
	}

	private static string WrapInlineHtml(string html, MarkdownMark mark)
	{
		switch (mark.Kind)
		{
		case MarkdownMarkKind.Link:
			if (string.IsNullOrEmpty(mark.Url))
			{
				return html;
			}
			return "<a href=\"" + HtmlEncodeAttribute(mark.Url) + "\">" + html + "</a>";
		case MarkdownMarkKind.Code:
			return "<code style=\"font-family:Consolas,'Courier New',monospace;background:#EFEFEF;border-radius:3px;padding:1px 3px;font-size:90%;\">" + html + "</code>";
		case MarkdownMarkKind.Strong:
			return "<strong>" + html + "</strong>";
		case MarkdownMarkKind.Emphasis:
			return "<em>" + html + "</em>";
		case MarkdownMarkKind.Underline:
			return "<u>" + html + "</u>";
		case MarkdownMarkKind.Strikethrough:
			return "<s>" + html + "</s>";
		case MarkdownMarkKind.Highlight:
			return "<mark style=\"background-color:" + HtmlEncodeAttribute(NormalizeColor(mark.Color)) + ";\">" + html + "</mark>";
		default:
			return html;
		}
	}

	private static string NormalizeColor(string color)
	{
		if (string.IsNullOrWhiteSpace(color))
		{
			return "#FFF2A8";
		}
		color = color.Trim();
		if (color.Length == 9 && color[0] == '#')
		{
			return "#" + color.Substring(3);
		}
		return color;
	}

	private static string HtmlEncodeText(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}
		return WebUtility.HtmlEncode(text.Replace("\r\n", "\n").Replace('\r', '\n')).Replace("\n", "<br />");
	}

	private static string HtmlEncodeAttribute(string text)
	{
		return WebUtility.HtmlEncode(text ?? string.Empty);
	}
}
