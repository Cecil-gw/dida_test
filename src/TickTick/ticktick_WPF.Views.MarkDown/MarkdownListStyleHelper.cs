using System;
using ticktick_WPF.Framework.Markdown;

namespace ticktick_WPF.Views.MarkDown;

public static class MarkdownListStyleHelper
{
	private const int MaxVisibleListLevels = 12;

	public const int MaxDisplayIndentLevel = 11;

	private const int IndentUnitWidth = 15;

	private const int ContentStartWidth = 22;

	private const int BulletBaseOffset = 8;

	private const int ListBaseOffset = -4;

	public static int GetIndentLevel(int leadingWhitespaceCount)
	{
		return GetDisplayIndentLevel(MarkdownListSyntax.GetIndentLevel(leadingWhitespaceCount));
	}

	public static int GetDisplayIndentLevel(int indentLevel)
	{
		return Math.Min(11, Math.Max(0, indentLevel));
	}

	public static MarkdownBulletStyle GetBulletStyle(int indentLevel)
	{
		return MarkdownListStyle.GetBulletStyle(GetDisplayIndentLevel(indentLevel));
	}

	public static double GetBulletX(int indentLevel)
	{
		return 4.0 + GetIndentOffset(indentLevel);
	}

	public static double GetReservedWidth(int indentLevel)
	{
		return 18.0 + GetIndentOffset(indentLevel);
	}

	public static double GetReservedWidth(int indentLevel, double requiredContentWidth)
	{
		return Math.Max(GetReservedWidth(indentLevel), -4.0 + GetIndentOffset(indentLevel) + Math.Max(0.0, requiredContentWidth));
	}

	public static double GetCenteredMarkerX(double reservedWidth, double markerWidth)
	{
		return Math.Max(0.0, (reservedWidth - markerWidth) / 2.0);
	}

	public static double GetCenteredMarkerX(int indentLevel, double reservedWidth, double markerWidth)
	{
		double indentOffset = GetIndentOffset(indentLevel);
		return -4.0 + indentOffset + GetCenteredMarkerX(Math.Max(0.0, reservedWidth - -4.0 - indentOffset), markerWidth);
	}

	public static double GetIndentOffset(int indentLevel)
	{
		return 15 * GetDisplayIndentLevel(indentLevel);
	}
}
