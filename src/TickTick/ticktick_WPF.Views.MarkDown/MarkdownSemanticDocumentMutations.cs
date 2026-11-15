using System;
using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.Framework.Markdown;

namespace ticktick_WPF.Views.MarkDown;

internal static class MarkdownSemanticDocumentMutations
{
	public static void RemoveLineAndShiftFollowing(MarkdownSemanticDocument document, int lineIndex)
	{
		if (document?.Blocks == null)
		{
			return;
		}
		List<MarkdownBlockNode> list = new List<MarkdownBlockNode>();
		foreach (MarkdownBlockNode block in document.Blocks)
		{
			if (block == null)
			{
				continue;
			}
			if (block.StartLine > lineIndex)
			{
				block.StartLine = Math.Max(0, block.StartLine - 1);
				block.EndLine = Math.Max(block.StartLine, block.EndLine - 1);
			}
			else if (block.EndLine >= lineIndex)
			{
				if (block.StartLine == lineIndex && block.EndLine == lineIndex)
				{
					list.Add(block);
				}
				else if (block.StartLine <= lineIndex && block.EndLine >= lineIndex)
				{
					block.EndLine = Math.Max(block.StartLine, block.EndLine - 1);
				}
			}
		}
		foreach (MarkdownBlockNode item in list)
		{
			document.Blocks.Remove(item);
		}
	}

	public static bool ContainsCodeBlock(IReadOnlyList<MarkdownBlockNode> lineSnapshot)
	{
		return lineSnapshot?.Any((MarkdownBlockNode block) => block != null && block.Kind == MarkdownBlockKind.CodeBlock) ?? false;
	}

	public static bool DoesSelectionRemovalTouchCodeBlock(IReadOnlyList<MarkdownBlockNode> lineSnapshot, int startLineIndex, int endLineIndex)
	{
		if (lineSnapshot == null || lineSnapshot.Count == 0)
		{
			return false;
		}
		int num = Math.Max(0, startLineIndex);
		int num2 = Math.Min(lineSnapshot.Count - 1, Math.Max(num, endLineIndex));
		for (int i = num; i <= num2; i++)
		{
			MarkdownBlockNode markdownBlockNode = lineSnapshot[i];
			if (markdownBlockNode != null && markdownBlockNode.Kind == MarkdownBlockKind.CodeBlock)
			{
				return true;
			}
		}
		return false;
	}

	public static bool TryExpandCodeBlockLineRange(IReadOnlyList<MarkdownBlockNode> lineSnapshot, int startLineIndex, int endLineIndex, out int expandedStartLineIndex, out int expandedEndLineIndex)
	{
		expandedStartLineIndex = -1;
		expandedEndLineIndex = -1;
		if (lineSnapshot == null || lineSnapshot.Count == 0 || endLineIndex < startLineIndex)
		{
			return false;
		}
		int num = Math.Max(0, Math.Min(startLineIndex, lineSnapshot.Count - 1));
		int num2 = Math.Max(num, Math.Min(endLineIndex, lineSnapshot.Count - 1));
		bool flag = false;
		for (int i = num; i <= num2; i++)
		{
			MarkdownBlockNode markdownBlockNode = lineSnapshot[i];
			if (markdownBlockNode != null && markdownBlockNode.Kind == MarkdownBlockKind.CodeBlock)
			{
				if (!flag)
				{
					expandedStartLineIndex = Math.Max(0, markdownBlockNode.StartLine);
					expandedEndLineIndex = Math.Max(expandedStartLineIndex, markdownBlockNode.EndLine);
					flag = true;
				}
				else
				{
					expandedStartLineIndex = Math.Min(expandedStartLineIndex, Math.Max(0, markdownBlockNode.StartLine));
					expandedEndLineIndex = Math.Max(expandedEndLineIndex, Math.Max(0, markdownBlockNode.EndLine));
				}
			}
		}
		return flag;
	}

	public static List<MarkdownBlockNode> CaptureLineSnapshot(MarkdownSemanticDocument document, int lineCount)
	{
		List<MarkdownBlockNode> list = new List<MarkdownBlockNode>();
		int lineIndex;
		for (lineIndex = 0; lineIndex < Math.Max(0, lineCount); lineIndex++)
		{
			MarkdownBlockNode markdownBlockNode = document?.Blocks?.FirstOrDefault((MarkdownBlockNode currentBlock) => currentBlock != null && currentBlock.StartLine <= lineIndex && currentBlock.EndLine >= lineIndex);
			if (markdownBlockNode != null)
			{
				list.Add(CloneBlockAsSingleLine(markdownBlockNode, lineIndex));
				continue;
			}
			list.Add(new MarkdownBlockNode
			{
				Kind = MarkdownBlockKind.Paragraph,
				StartLine = lineIndex,
				EndLine = lineIndex,
				Indent = 0
			});
		}
		return list;
	}

	public static List<MarkdownBlockNode> BuildLineSnapshotAfterRangeRemoval(IReadOnlyList<MarkdownBlockNode> sourceLines, int startLineIndex, int endLineIndex, bool keepsStartLinePrefix, bool keepsEndLineSuffix, bool preservesStartLineBoundary)
	{
		List<MarkdownBlockNode> list = new List<MarkdownBlockNode>();
		if (sourceLines == null || sourceLines.Count == 0)
		{
			return list;
		}
		for (int i = 0; i < Math.Max(0, startLineIndex); i++)
		{
			list.Add(CloneBlockAsSingleLine(sourceLines[i], list.Count));
		}
		if (preservesStartLineBoundary && startLineIndex >= 0 && startLineIndex < sourceLines.Count)
		{
			MarkdownBlockNode markdownBlockNode = sourceLines[startLineIndex];
			MarkdownBlockNode source = (keepsStartLinePrefix ? markdownBlockNode : new MarkdownBlockNode
			{
				Kind = MarkdownBlockKind.Paragraph,
				Indent = 0
			});
			list.Add(CloneBlockAsSingleLine(source, list.Count));
			if (keepsEndLineSuffix && endLineIndex >= 0 && endLineIndex < sourceLines.Count)
			{
				list.Add(CloneBlockAsSingleLine(sourceLines[endLineIndex], list.Count));
			}
		}
		else if ((keepsStartLinePrefix || keepsEndLineSuffix) && startLineIndex >= 0 && startLineIndex < sourceLines.Count)
		{
			MarkdownBlockNode markdownBlockNode2 = null;
			if (keepsStartLinePrefix && (!keepsEndLineSuffix || !preservesStartLineBoundary))
			{
				markdownBlockNode2 = sourceLines[startLineIndex];
			}
			else if (keepsEndLineSuffix && endLineIndex >= 0 && endLineIndex < sourceLines.Count)
			{
				markdownBlockNode2 = sourceLines[endLineIndex];
			}
			list.Add(CloneBlockAsSingleLine(markdownBlockNode2 ?? new MarkdownBlockNode
			{
				Kind = MarkdownBlockKind.Paragraph,
				Indent = 0
			}, list.Count));
		}
		for (int j = Math.Max(0, endLineIndex + 1); j < sourceLines.Count; j++)
		{
			list.Add(CloneBlockAsSingleLine(sourceLines[j], list.Count));
		}
		return list;
	}

	public static MarkdownSemanticDocument BuildDocumentFromLineSnapshot(IReadOnlyList<MarkdownBlockNode> lineBlocks, int lineCount)
	{
		MarkdownSemanticDocument markdownSemanticDocument = new MarkdownSemanticDocument();
		if (lineBlocks == null || lineCount <= 0)
		{
			return markdownSemanticDocument;
		}
		int num = Math.Min(lineBlocks.Count, lineCount);
		for (int i = 0; i < num; i++)
		{
			MarkdownBlockNode source = lineBlocks[i] ?? new MarkdownBlockNode
			{
				Kind = MarkdownBlockKind.Paragraph
			};
			markdownSemanticDocument.Blocks.Add(CloneBlockAsSingleLine(source, i));
		}
		for (int j = num; j < lineCount; j++)
		{
			markdownSemanticDocument.Blocks.Add(new MarkdownBlockNode
			{
				Kind = MarkdownBlockKind.Paragraph,
				StartLine = j,
				EndLine = j,
				Indent = 0
			});
		}
		return markdownSemanticDocument;
	}

	public static MarkdownBlockNode CloneBlockAsSingleLine(MarkdownBlockNode source, int lineIndex)
	{
		if (source == null)
		{
			source = new MarkdownBlockNode();
		}
		return new MarkdownBlockNode
		{
			Kind = source.Kind,
			StartLine = lineIndex,
			EndLine = lineIndex,
			Indent = source.Indent,
			Level = source.Level,
			ListKind = source.ListKind,
			Checked = source.Checked,
			Folded = source.Folded,
			DisplayFolded = source.DisplayFolded,
			SoftTab = source.SoftTab,
			CustomOrder = source.CustomOrder,
			Language = source.Language,
			CodeText = source.CodeText,
			CodeBlockAttachmentIdentity = source.CodeBlockAttachmentIdentity,
			FileKind = source.FileKind,
			Source = source.Source,
			AltText = source.AltText,
			Caption = source.Caption
		};
	}

	public static MarkdownBlockNode CloneBlockAsLineRange(MarkdownBlockNode source, int startLineIndex, int endLineIndex)
	{
		if (source == null || endLineIndex < startLineIndex)
		{
			return null;
		}
		MarkdownBlockNode markdownBlockNode = CloneBlockAsSingleLine(source, startLineIndex);
		markdownBlockNode.EndLine = endLineIndex;
		return markdownBlockNode;
	}

	public static void RemoveRangeAndShiftFollowing(MarkdownSemanticDocument document, int startLineIndex, int endLineIndex, int removedLineCount)
	{
		if (document?.Blocks == null)
		{
			return;
		}
		removedLineCount = Math.Max(0, removedLineCount);
		List<MarkdownBlockNode> list = new List<MarkdownBlockNode>();
		foreach (MarkdownBlockNode block in document.Blocks)
		{
			if (block != null && block.EndLine >= startLineIndex)
			{
				if (block.StartLine > endLineIndex)
				{
					block.StartLine = Math.Max(0, block.StartLine - removedLineCount);
					block.EndLine = Math.Max(block.StartLine, block.EndLine - removedLineCount);
				}
				else
				{
					list.Add(block);
				}
			}
		}
		foreach (MarkdownBlockNode item in list)
		{
			document.Blocks.Remove(item);
		}
	}

	public static MarkdownSemanticDocument CloneDocument(MarkdownSemanticDocument source)
	{
		if (source == null)
		{
			return null;
		}
		MarkdownSemanticDocument markdownSemanticDocument = new MarkdownSemanticDocument();
		IEnumerable<MarkdownBlockNode> blocks = source.Blocks;
		foreach (MarkdownBlockNode item in blocks ?? Enumerable.Empty<MarkdownBlockNode>())
		{
			if (item != null)
			{
				markdownSemanticDocument.Blocks.Add(new MarkdownBlockNode
				{
					Kind = item.Kind,
					StartLine = item.StartLine,
					EndLine = item.EndLine,
					Indent = item.Indent,
					Level = item.Level,
					ListKind = item.ListKind,
					Checked = item.Checked,
					Folded = item.Folded,
					DisplayFolded = item.DisplayFolded,
					SoftTab = item.SoftTab,
					CustomOrder = item.CustomOrder,
					Language = item.Language,
					CodeText = item.CodeText,
					CodeBlockAttachmentIdentity = item.CodeBlockAttachmentIdentity,
					FileKind = item.FileKind,
					Source = item.Source,
					AltText = item.AltText,
					Caption = item.Caption
				});
			}
		}
		return markdownSemanticDocument;
	}

	public static List<MarkdownInlineDisplaySegment> CloneDisplaySegments(IEnumerable<MarkdownInlineDisplaySegment> segments)
	{
		List<MarkdownInlineDisplaySegment> list = new List<MarkdownInlineDisplaySegment>();
		if (segments == null)
		{
			return list;
		}
		foreach (MarkdownInlineDisplaySegment segment in segments)
		{
			if (segment != null)
			{
				list.Add(new MarkdownInlineDisplaySegment
				{
					Kind = segment.Kind,
					SourcePosition = segment.SourcePosition,
					SourceLength = segment.SourceLength,
					OpeningDelimiterLength = segment.OpeningDelimiterLength,
					ClosingDelimiterLength = segment.ClosingDelimiterLength,
					Color = segment.Color,
					LinkText = segment.LinkText,
					Url = segment.Url,
					UrlPosition = segment.UrlPosition,
					UrlLength = segment.UrlLength
				});
			}
		}
		return list;
	}

	public static bool TryGetCodeBlockDeletionLineRange(MarkdownBlockNode block, int lineIndex, bool backspace, bool atBoundary, bool hasClosingFenceLine, out int startLineIndex, out int endLineIndex)
	{
		startLineIndex = -1;
		endLineIndex = -1;
		if (!atBoundary || block == null || block.Kind != MarkdownBlockKind.CodeBlock || block.EndLine <= block.StartLine + 1)
		{
			return false;
		}
		bool flag = hasClosingFenceLine && lineIndex == block.EndLine;
		bool flag2 = IsEmptySingleContentLineCodeBlock(block, hasClosingFenceLine);
		if (backspace)
		{
			if (lineIndex != block.StartLine && (!flag2 || lineIndex != block.StartLine + 1) && !flag)
			{
				return false;
			}
		}
		else if ((!flag2 || lineIndex != block.EndLine - 1) && !flag)
		{
			return false;
		}
		startLineIndex = block.StartLine;
		endLineIndex = block.EndLine;
		return true;
	}

	private static bool IsEmptySingleContentLineCodeBlock(MarkdownBlockNode block, bool hasClosingFenceLine)
	{
		if (block == null || !hasClosingFenceLine)
		{
			return false;
		}
		if (block.EndLine == block.StartLine + 2)
		{
			return string.IsNullOrEmpty(block.CodeText);
		}
		return false;
	}

	public static bool TryGetAdjacentCodeBlockDeletionLineRange(IReadOnlyList<MarkdownBlockNode> lineSnapshot, int lineIndex, bool backspace, bool atBoundary, out int startLineIndex, out int endLineIndex)
	{
		startLineIndex = -1;
		endLineIndex = -1;
		if (!atBoundary || lineSnapshot == null || lineSnapshot.Count == 0)
		{
			return false;
		}
		if (backspace)
		{
			int num = lineIndex - 1;
			if (num < 0 || num >= lineSnapshot.Count)
			{
				return false;
			}
			MarkdownBlockNode markdownBlockNode = lineSnapshot[num];
			if (markdownBlockNode == null || markdownBlockNode.Kind != MarkdownBlockKind.CodeBlock || markdownBlockNode.EndLine != num)
			{
				return false;
			}
			startLineIndex = markdownBlockNode.StartLine;
			endLineIndex = markdownBlockNode.EndLine;
			return true;
		}
		int num2 = lineIndex + 1;
		if (num2 < 0 || num2 >= lineSnapshot.Count)
		{
			return false;
		}
		MarkdownBlockNode markdownBlockNode2 = lineSnapshot[num2];
		if (markdownBlockNode2 == null || markdownBlockNode2.Kind != MarkdownBlockKind.CodeBlock || markdownBlockNode2.StartLine != num2)
		{
			return false;
		}
		startLineIndex = markdownBlockNode2.StartLine;
		endLineIndex = markdownBlockNode2.EndLine;
		return true;
	}
}
