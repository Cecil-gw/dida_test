using System.Collections.Generic;

namespace ticktick_WPF.Resource;

public static class MarkDownAction
{
	public const string Heading1 = "Heading1";

	public const string Heading2 = "Heading2";

	public const string Heading3 = "Heading3";

	public const string Heading4 = "Heading4";

	public const string Heading5 = "Heading5";

	public const string Heading6 = "Heading6";

	public const string Bold = "Bold";

	public const string Italic = "Italic";

	public const string UnderLine = "UnderLine";

	public const string StrikeThrough = "StrikeThrough";

	public const string Quote = "Quote";

	public const string DateTime = "DateTime";

	public const string Code = "Code";

	public const string NumberedList = "NumberedList";

	public const string BulletList = "BulletList";

	public const string CheckItem = "CheckItem";

	public const string Link = "Link";

	public const string HighLight = "HighLight";

	public const string SplitLine = "SplitLine";

	public const string Attachment = "Attachment";

	public const string HighLightEx = "HighLightEx";

	public const string Tag = "Tag";

	public const string Template = "Template";

	public const string LinkTask = "LinkTask";

	public const string Subtask = "Subtask";

	public const string CodeBlock = "CodeBlock";

	public const string Text = "Text";

	private static readonly Dictionary<string, string> Action2IconEditor = new Dictionary<string, string>
	{
		{ "Text", "Ic.MarkdownEditor.Body" },
		{ "Heading1", "Ic.MarkdownEditor.Header1" },
		{ "Heading2", "Ic.MarkdownEditor.Header2" },
		{ "Heading3", "Ic.MarkdownEditor.Header3" },
		{ "Heading4", "Ic.MarkdownEditor.Header4" },
		{ "Heading5", "Ic.MarkdownEditor.Header5" },
		{ "Heading6", "Ic.MarkdownEditor.Header6" },
		{ "BulletList", "Ic.MarkdownEditor.UnorderedList" },
		{ "NumberedList", "Ic.MarkdownEditor.OrderedList" },
		{ "Quote", "Ic.MarkdownEditor.Refer" },
		{ "Code", "Ic.MarkdownEditor.Code" },
		{ "CheckItem", "Ic.MarkdownEditor.Checked" },
		{ "SplitLine", "Ic.MarkdownEditor.Divider" },
		{ "Attachment", "Ic.MarkdownEditor.Attachment" },
		{ "Subtask", "Ic.MarkdownOM.SubTask" },
		{ "Tag", "Ic.MarkdownOM.Tag" },
		{ "LinkTask", "Ic.MarkdownOM.LinkToTask" }
	};

	private static readonly Dictionary<string, string> Action2Label = new Dictionary<string, string>
	{
		{ "Text", "text" },
		{ "Heading1", "h1" },
		{ "Heading2", "h2" },
		{ "Heading3", "h3" },
		{ "Heading4", "h4" },
		{ "Heading5", "h5" },
		{ "Heading6", "h6" },
		{ "BulletList", "bulleted_list" },
		{ "NumberedList", "numbered_list" },
		{ "Quote", "quote" },
		{ "Bold", "bold" },
		{ "UnderLine", "underline" },
		{ "StrikeThrough", "strike" },
		{ "Italic", "italic" },
		{ "Code", "code" },
		{ "CheckItem", "check_item" },
		{ "SplitLine", "horizontal_line" },
		{ "Attachment", "attachment" },
		{ "Subtask", "subtask" },
		{ "Tag", "tag" },
		{ "Link", "link" },
		{ "HighLight", "highlight" },
		{ "LinkTask", "linked_task" }
	};

	private static readonly Dictionary<string, string> Action2Icon = new Dictionary<string, string>
	{
		{ "Text", "Ic.MarkdownOM.Body" },
		{ "Heading1", "Ic.MarkdownOM.Header1" },
		{ "Heading2", "Ic.MarkdownOM.Header2" },
		{ "Heading3", "Ic.MarkdownOM.Header3" },
		{ "Heading4", "Ic.MarkdownOM.Header4" },
		{ "Heading5", "Ic.MarkdownOM.Header5" },
		{ "Heading6", "Ic.MarkdownOM.Header6" },
		{ "BulletList", "Ic.MarkdownOM.UnorderedList" },
		{ "NumberedList", "Ic.MarkdownOM.OrderedList" },
		{ "Quote", "Ic.MarkdownOM.Refer" },
		{ "Code", "Ic.MarkdownEditor.Code" },
		{ "CheckItem", "Ic.MarkdownOM.Checked" },
		{ "SplitLine", "Ic.MarkdownOM.Divider" },
		{ "Attachment", "Ic.MarkdownOM.Attachment" },
		{ "Subtask", "Ic.MarkdownOM.SubTask" },
		{ "Tag", "Ic.MarkdownOM.Tag" },
		{ "LinkTask", "Ic.MarkdownOM.LinkToTask" }
	};

	public static string TryGetIcon(string action)
	{
		if (!Action2Icon.TryGetValue(action, out var value))
		{
			return "Ic.MarkdownOM.Body";
		}
		return value;
	}

	public static string TryGetIconInEditor(string action)
	{
		if (!Action2IconEditor.TryGetValue(action, out var value))
		{
			return "Ic.MarkdownEditor.Body";
		}
		return value;
	}

	public static string TryGetLabel(string action)
	{
		if (!Action2Label.TryGetValue(action, out var value))
		{
			return "unknown";
		}
		return value;
	}
}
