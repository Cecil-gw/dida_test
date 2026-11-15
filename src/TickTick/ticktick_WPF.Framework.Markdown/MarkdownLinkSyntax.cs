using System.Collections.Generic;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Framework.Markdown;

internal static class MarkdownLinkSyntax
{
	public static bool TryFindCurrentLinkContext(int selectionStart, int selectionLength, int caretOffset, IDictionary<int, LinkInfo> linkNameDict, IDictionary<int, LinkInfo> linkUrlDict, out int startIndex, out LinkInfo info, out LinkInfoSource source)
	{
		int selectionEnd = selectionStart + selectionLength;
		bool hasSelection = selectionLength > 0;
		foreach (KeyValuePair<int, LinkInfo> item in linkNameDict)
		{
			if (IsRangeActive(item.Key, item.Value.Link?.Length ?? 0, caretOffset, selectionStart, selectionEnd, hasSelection))
			{
				startIndex = item.Key;
				info = item.Value;
				source = LinkInfoSource.TextLine;
				return true;
			}
		}
		foreach (KeyValuePair<int, LinkInfo> item2 in linkUrlDict)
		{
			if (IsRangeActive(item2.Key, item2.Value.Url?.Length ?? 0, caretOffset, selectionStart, selectionEnd, hasSelection))
			{
				startIndex = item2.Key;
				info = item2.Value;
				source = LinkInfoSource.TextLineButton;
				return true;
			}
		}
		startIndex = -1;
		info = null;
		source = LinkInfoSource.Default;
		return false;
	}

	private static bool IsRangeActive(int start, int length, int caretOffset, int selectionStart, int selectionEnd, bool hasSelection)
	{
		if (length <= 0)
		{
			return false;
		}
		int num = start + length;
		if (hasSelection)
		{
			if (selectionStart < num)
			{
				return selectionEnd > start;
			}
			return false;
		}
		if (caretOffset > start)
		{
			return caretOffset < num;
		}
		return false;
	}
}
