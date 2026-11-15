using System;
using System.Collections.Generic;
using System.Linq;

namespace ticktick_WPF.Framework.Markdown;

public static class MarkdownHeadingFoldHelper
{
	public static bool TryGetFoldRange(IReadOnlyList<MarkdownBlockNode> blocks, MarkdownBlockNode heading, int totalLineCount, out MarkdownHeadingFoldRange range)
	{
		range = null;
		if (heading == null || heading.Kind != MarkdownBlockKind.Heading || !heading.Level.HasValue)
		{
			return false;
		}
		List<MarkdownBlockNode> list = (from block in blocks ?? new List<MarkdownBlockNode>()
			where block != null
			orderby block.StartLine, block.EndLine
			select block).ToList();
		if (list.Count == 0)
		{
			return false;
		}
		int num = list.IndexOf(heading);
		if (num < 0)
		{
			num = list.FindIndex((MarkdownBlockNode block) => block.Kind == MarkdownBlockKind.Heading && block.StartLine == heading.StartLine && block.EndLine == heading.EndLine && block.Level == heading.Level);
		}
		if (num < 0)
		{
			return false;
		}
		int headingLevel = Math.Max(1, Math.Min(6, heading.Level.Value));
		int num2 = Math.Max(heading.EndLine + 1, totalLineCount);
		int val = heading.EndLine;
		for (int num3 = num + 1; num3 < list.Count; num3++)
		{
			MarkdownBlockNode markdownBlockNode = list[num3];
			if (markdownBlockNode.Kind == MarkdownBlockKind.Heading && markdownBlockNode.Level.HasValue && Math.Max(1, Math.Min(6, markdownBlockNode.Level.Value)) <= headingLevel)
			{
				break;
			}
			val = Math.Max(val, markdownBlockNode.EndLine);
		}
		int num4 = heading.EndLine + 1;
		if (list.Count == num + 1)
		{
			val = Math.Max(val, num2 - 1);
		}
		else
		{
			MarkdownBlockNode markdownBlockNode2 = list.Skip(num + 1).FirstOrDefault((MarkdownBlockNode block) => block.Kind == MarkdownBlockKind.Heading && block.Level.HasValue && Math.Max(1, Math.Min(6, block.Level.Value)) <= headingLevel);
			val = ((markdownBlockNode2 == null) ? Math.Max(val, num2 - 1) : Math.Max(val, markdownBlockNode2.StartLine - 1));
		}
		if (val < num4)
		{
			return false;
		}
		if (!HasFoldableContent(list, num4, val))
		{
			return false;
		}
		range = new MarkdownHeadingFoldRange
		{
			Heading = heading,
			HeadingLineIndex = heading.StartLine,
			StartLineIndex = num4,
			EndLineIndex = val
		};
		return true;
	}

	private static bool HasFoldableContent(IEnumerable<MarkdownBlockNode> blocks, int startLineIndex, int endLineIndex)
	{
		if (endLineIndex >= startLineIndex)
		{
			return true;
		}
		return (blocks ?? Enumerable.Empty<MarkdownBlockNode>()).Any((MarkdownBlockNode block) => IsFoldableContentBlock(block, startLineIndex, endLineIndex));
	}

	private static bool IsFoldableContentBlock(MarkdownBlockNode block, int startLineIndex, int endLineIndex)
	{
		if (block == null || block.EndLine < startLineIndex || block.StartLine > endLineIndex)
		{
			return false;
		}
		return true;
	}

	public static IReadOnlyList<MarkdownHeadingFoldRange> GetFoldedRanges(MarkdownSemanticDocument document, int totalLineCount)
	{
		return GetFoldedRanges(document, totalLineCount, useDisplayFolded: false);
	}

	public static IReadOnlyList<MarkdownHeadingFoldRange> GetDisplayFoldedRanges(MarkdownSemanticDocument document, int totalLineCount)
	{
		return GetFoldedRanges(document, totalLineCount, useDisplayFolded: true);
	}

	private static IReadOnlyList<MarkdownHeadingFoldRange> GetFoldedRanges(MarkdownSemanticDocument document, int totalLineCount, bool useDisplayFolded)
	{
		List<MarkdownHeadingFoldRange> list = new List<MarkdownHeadingFoldRange>();
		List<MarkdownBlockNode> list2 = document?.Blocks;
		if (list2 == null || list2.Count == 0)
		{
			return list;
		}
		foreach (MarkdownBlockNode block in list2)
		{
			MarkdownBlockNode markdownBlockNode = block;
			if (markdownBlockNode != null && markdownBlockNode.Kind == MarkdownBlockKind.Heading && IsHeadingFolded(block, useDisplayFolded))
			{
				int totalLineCount2 = Math.Max(totalLineCount, Math.Max(block.EndLine + 1, list2.Where((MarkdownBlockNode candidate) => candidate != null).DefaultIfEmpty(block).Max((MarkdownBlockNode candidate) => candidate?.EndLine ?? block.EndLine) + 1));
				if (TryGetFoldRange(list2, block, totalLineCount2, out var range))
				{
					list.Add(range);
				}
			}
		}
		return list;
	}

	private static bool IsHeadingFolded(MarkdownBlockNode block, bool useDisplayFolded)
	{
		if (!useDisplayFolded || !block.DisplayFolded.HasValue)
		{
			return block.Folded == true;
		}
		return block.DisplayFolded.Value;
	}

	public static IReadOnlyList<MarkdownHeadingFoldRange> GetFoldedRanges(MarkdownSemanticDocument document)
	{
		List<MarkdownBlockNode> list = document?.Blocks;
		int totalLineCount = ((list != null && list.Count != 0) ? (list.Where((MarkdownBlockNode block) => block != null).Max((MarkdownBlockNode block) => block.EndLine) + 1) : 0);
		return GetFoldedRanges(document, totalLineCount);
	}

	public static IReadOnlyList<int> GetFoldedHeadingLinesContainingMatches(MarkdownSemanticDocument document, int totalLineCount, IEnumerable<int> matchedLineIndices, ISet<int> excludedHeadingLineIndices = null)
	{
		HashSet<int> matchedLines = new HashSet<int>(matchedLineIndices ?? Enumerable.Empty<int>());
		if (matchedLines.Count == 0)
		{
			return new List<int>();
		}
		return (from lineIndex in (from range in GetFoldedRanges(document, totalLineCount)
				where range != null && (excludedHeadingLineIndices == null || !excludedHeadingLineIndices.Contains(range.HeadingLineIndex)) && matchedLines.Any((int lineIndex) => lineIndex >= range.StartLineIndex && lineIndex <= range.EndLineIndex)
				select range.HeadingLineIndex).Distinct()
			orderby lineIndex
			select lineIndex).ToList();
	}

	public static IReadOnlyList<MarkdownHeadingFoldRange> GetDisplayFoldedRangesContainingMatches(MarkdownSemanticDocument document, int totalLineCount, IEnumerable<int> matchedLineIndices, ISet<MarkdownBlockNode> excludedHeadings = null)
	{
		HashSet<int> matchedLines = new HashSet<int>(matchedLineIndices ?? Enumerable.Empty<int>());
		if (matchedLines.Count == 0)
		{
			return new List<MarkdownHeadingFoldRange>();
		}
		return (from range in GetSearchExpandableRanges(document, totalLineCount)
			where range != null && (excludedHeadings == null || !excludedHeadings.Contains(range.Heading)) && matchedLines.Any((int lineIndex) => lineIndex >= range.StartLineIndex && lineIndex <= range.EndLineIndex)
			group range by range.Heading into @group
			select @group.First() into range
			orderby range.HeadingLineIndex
			select range).ToList();
	}

	private static IReadOnlyList<MarkdownHeadingFoldRange> GetSearchExpandableRanges(MarkdownSemanticDocument document, int totalLineCount)
	{
		List<MarkdownHeadingFoldRange> list = new List<MarkdownHeadingFoldRange>();
		List<MarkdownBlockNode> list2 = document?.Blocks;
		if (list2 == null || list2.Count == 0)
		{
			return list;
		}
		foreach (MarkdownBlockNode block in list2)
		{
			MarkdownBlockNode markdownBlockNode = block;
			if (markdownBlockNode != null && markdownBlockNode.Kind == MarkdownBlockKind.Heading && (block.Folded == true || block.DisplayFolded == true))
			{
				int totalLineCount2 = Math.Max(totalLineCount, Math.Max(block.EndLine + 1, list2.Where((MarkdownBlockNode candidate) => candidate != null).DefaultIfEmpty(block).Max((MarkdownBlockNode candidate) => candidate?.EndLine ?? block.EndLine) + 1));
				if (TryGetFoldRange(list2, block, totalLineCount2, out var range))
				{
					list.Add(range);
				}
			}
		}
		return list;
	}
}
