using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommonMark;
using CommonMark.Syntax;

namespace ticktick_WPF.Views.MarkDown;

public static class AbstractSyntaxTree
{
	private static readonly CommonMarkSettings CommonMarkSettings;

	public static readonly Dictionary<BlockTag, Func<Theme, Highlight>> BlockHighlighter;

	public static readonly Dictionary<InlineTag, Func<Theme, Highlight>> InlineHighlighter;

	static AbstractSyntaxTree()
	{
		BlockHighlighter = new Dictionary<BlockTag, Func<Theme, Highlight>>
		{
			{
				BlockTag.AtxHeading,
				(Theme t) => t.HighlightHeading
			},
			{
				BlockTag.SetextHeading,
				(Theme t) => t.HighlightHeading
			},
			{
				BlockTag.BlockQuote,
				(Theme t) => t.HighlightBlockQuote
			},
			{
				BlockTag.FencedCode,
				(Theme t) => t.HighlightBlockCode
			}
		};
		InlineHighlighter = new Dictionary<InlineTag, Func<Theme, Highlight>>
		{
			{
				InlineTag.HighLight,
				(Theme t) => t.HighlightHighlight
			},
			{
				InlineTag.HighLightEx,
				(Theme t) => t.HighlightNormal
			}
		};
		CommonMarkSettings = CommonMarkSettings.Default.Clone();
		CommonMarkSettings.TrackSourcePosition = true;
		CommonMarkSettings.AdditionalFeatures = CommonMarkAdditionalFeatures.StrikethroughTilde;
	}

	public static Block GenerateAbstractSyntaxTree(string text)
	{
		using StringReader source = new StringReader(Normalize(text));
		Block block = CommonMarkConverter.ProcessStage1(source, CommonMarkSettings);
		CommonMarkConverter.ProcessStage2(block, CommonMarkSettings);
		return block;
	}

	private static string Normalize(string value)
	{
		return value ?? string.Empty;
	}

	public static IEnumerable<Block> EnumerateSpanningBlocks(Block ast, int startOffset, int endOffset)
	{
		return (from b in EnumerateBlocks(ast.FirstChild)
			where b.SourcePosition + b.SourceLength > startOffset
			select b).TakeWhile((Block b) => b.SourcePosition < endOffset);
	}

	public static IEnumerable<Block> EnumerateBlocks(Block block)
	{
		if (block == null)
		{
			yield break;
		}
		Stack<Block> stack = new Stack<Block>();
		stack.Push(block);
		while (stack.Any())
		{
			Block next = stack.Pop();
			yield return next;
			if (next.NextSibling != null)
			{
				stack.Push(next.NextSibling);
			}
			if (next.FirstChild != null)
			{
				stack.Push(next.FirstChild);
			}
		}
	}

	public static IEnumerable<Inline> EnumerateInlines(Inline inline)
	{
		if (inline == null)
		{
			yield break;
		}
		Stack<Inline> stack = new Stack<Inline>();
		stack.Push(inline);
		while (stack.Any())
		{
			Inline next = stack.Pop();
			yield return next;
			if (next.NextSibling != null)
			{
				stack.Push(next.NextSibling);
			}
			if (next.FirstChild != null)
			{
				stack.Push(next.FirstChild);
			}
		}
	}

	public static bool PositionSafeForSmartLink(Block ast, int start, int length)
	{
		if (ast == null)
		{
			return true;
		}
		int end = start + length;
		BlockTag[] source = new BlockTag[4]
		{
			BlockTag.FencedCode,
			BlockTag.HtmlBlock,
			BlockTag.IndentedCode,
			BlockTag.ReferenceDefinition
		};
		InlineTag[] inlineTags = new InlineTag[4]
		{
			InlineTag.Code,
			InlineTag.Link,
			InlineTag.RawHtml,
			InlineTag.Image
		};
		BlockTag lastBlockTag = BlockTag.Document;
		foreach (Block block in EnumerateBlocks(ast.FirstChild))
		{
			if (block.SourcePosition + block.SourceLength < start)
			{
				lastBlockTag = block.Tag;
				continue;
			}
			if (block.SourcePosition >= end)
			{
				return source.All((BlockTag tag) => tag != lastBlockTag);
			}
			if (source.Any((BlockTag tag) => tag == block.Tag))
			{
				return false;
			}
			return !(from il in EnumerateInlines(block.InlineContent).TakeWhile((Inline il) => il.SourcePosition < end)
				where il.SourcePosition + il.SourceLength > start
				select il).Any((Inline il) => inlineTags.Any((InlineTag tag) => tag == il.Tag));
		}
		return true;
	}
}
