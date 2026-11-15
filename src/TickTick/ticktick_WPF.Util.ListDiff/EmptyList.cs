using System;
using System.Collections;
using System.Collections.Generic;

namespace ticktick_WPF.Util.ListDiff;

internal class EmptyList<T> : IReadOnlyList<T>, IReadOnlyCollection<T>, IEnumerable<T>, IEnumerable
{
	public static readonly EmptyList<T> Instance = new EmptyList<T>();

	public int Count => 0;

	public T this[int index]
	{
		get
		{
			throw new IndexOutOfRangeException();
		}
	}

	public IEnumerator<T> GetEnumerator()
	{
		yield break;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
