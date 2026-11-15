using System;

namespace ticktick_WPF.Framework.Markdown;

internal static class MarkdownListStyle
{
	public static MarkdownBulletStyle GetBulletStyle(int indentLevel)
	{
		return (Math.Max(0, indentLevel) % 3) switch
		{
			1 => MarkdownBulletStyle.HollowCircle, 
			2 => MarkdownBulletStyle.SolidSquare, 
			_ => MarkdownBulletStyle.SolidCircle, 
		};
	}
}
