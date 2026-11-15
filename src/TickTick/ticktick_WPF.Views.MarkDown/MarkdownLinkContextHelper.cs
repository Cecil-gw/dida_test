using System;
using System.Collections.Generic;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Framework.Markdown;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Views.MarkDown;

internal static class MarkdownLinkContextHelper
{
	public static LinkLineInfoModel TryCreateCurrentLinkLineInfo(int selectionStart, int selectionLength, int caretOffset, IDictionary<int, LinkInfo> linkNameDict, IDictionary<int, LinkInfo> linkUrlDict, Func<int, int?> getLineNumberByOffset, Func<int, VisualLine> getVisualLine)
	{
		if (!MarkdownLinkSyntax.TryFindCurrentLinkContext(selectionStart, selectionLength, caretOffset, linkNameDict, linkUrlDict, out var startIndex, out var info, out var source))
		{
			return null;
		}
		return CreateLinkLineInfoModel(startIndex, info, source, getLineNumberByOffset, getVisualLine);
	}

	private static LinkLineInfoModel CreateLinkLineInfoModel(int startIndex, LinkInfo info, LinkInfoSource source, Func<int, int?> getLineNumberByOffset, Func<int, VisualLine> getVisualLine)
	{
		int? num = getLineNumberByOffset?.Invoke(startIndex);
		return new LinkLineInfoModel
		{
			StartIndex = startIndex,
			Text = info?.Link,
			Url = info?.Url,
			VisualLine = ((!num.HasValue) ? null : getVisualLine?.Invoke(num.Value)),
			IsNew = false,
			Source = source
		};
	}
}
