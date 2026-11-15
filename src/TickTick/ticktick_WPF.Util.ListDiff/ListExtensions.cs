using System.Collections.Generic;
using System.Linq;

namespace ticktick_WPF.Util.ListDiff;

internal static class ListExtensions
{
	public static List<T> Splice<T>(this List<T> input, int start, int count, params T[] objects)
	{
		List<T> range = input.GetRange(start, count);
		input.RemoveRange(start, count);
		input.InsertRange(start, objects);
		return range;
	}

	public static bool StartsWith<T>(this IReadOnlyList<T> target, IReadOnlyList<T> other)
	{
		if (target.Count >= other.Count)
		{
			return target.Take(other.Count).SequenceEqual(other);
		}
		return false;
	}

	public static bool EndsWith<T>(this IReadOnlyList<T> target, IReadOnlyList<T> other)
	{
		if (target.Count >= other.Count)
		{
			return target.Skip(target.Count - other.Count).Take(other.Count).SequenceEqual(other);
		}
		return false;
	}

	public static IReadOnlyList<T> Substring<T>(this IReadOnlyList<T> target, int start, int length = -1)
	{
		if (length == -1)
		{
			length = target.Count - start;
		}
		return (target as List<T>)?.GetRange(start, length) ?? target.Skip(start).Take(length).ToList();
	}

	private static bool CompareRange<T>(IReadOnlyList<T> listA, int offsetA, IReadOnlyList<T> listB, int offsetB, int count)
	{
		for (int i = 0; i < count; i++)
		{
			if (!EqualityComparer<T>.Default.Equals(listA[offsetA + i], listB[offsetB + i]))
			{
				return false;
			}
		}
		return true;
	}

	public static int IndexOf<T>(this IReadOnlyList<T> target, IReadOnlyList<T> other, int start = 0)
	{
		int num = target.Count - other.Count;
		for (int i = start; i < num; i++)
		{
			if (CompareRange(target, i, other, 0, other.Count))
			{
				return i;
			}
		}
		return -1;
	}

	public static bool EqualsAt<T>(this IReadOnlyList<T> target, int targetPos, IReadOnlyList<T> other, int otherPos)
	{
		return EqualityComparer<T>.Default.Equals(target[targetPos], other[otherPos]);
	}
}
