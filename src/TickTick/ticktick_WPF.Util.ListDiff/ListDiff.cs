using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ticktick_WPF.Util.ListDiff;

public class ListDiff
{
	private readonly TimeSpan _diffTimeout;

	private readonly int _diffDualThreshold;

	private ListDiff(TimeSpan timeout, int dualThreshold)
	{
		_diffTimeout = timeout;
		_diffDualThreshold = dualThreshold;
	}

	public static void ApplyDiff<T>(ObservableCollection<T> original, ObservableCollection<T> revised)
	{
		List<Diff<T>> list = Compare(original, revised).ToList();
		if (!list.Any())
		{
			return;
		}
		int num = 0;
		foreach (Diff<T> item in list)
		{
			switch (item.Operation)
			{
			case Operation.Equal:
				num += item.Items.Count;
				break;
			case Operation.Delete:
				foreach (T item2 in item.Items)
				{
					original.Remove(item2);
				}
				break;
			case Operation.Insert:
				foreach (T item3 in item.Items)
				{
					original.Insert(num, item3);
					num++;
				}
				break;
			}
		}
	}

	public static IEnumerable<Diff<T>> Compare<T>(IReadOnlyList<T> text1, IReadOnlyList<T> text2, TimeSpan? timeSpan = null, int diffDualThreshold = 32)
	{
		if (!timeSpan.HasValue)
		{
			timeSpan = TimeSpan.FromSeconds(1.0);
		}
		return new ListDiff(timeSpan.Value, diffDualThreshold).CompareUsingOptions(text1, text2);
	}

	private List<Diff<T>> CompareUsingOptions<T>(IReadOnlyList<T> text1, IReadOnlyList<T> text2)
	{
		if (text1.SequenceEqual(text2))
		{
			return new List<Diff<T>>
			{
				new Diff<T>(Operation.Equal, text1)
			};
		}
		int commonPrefix = GetCommonPrefix(text1, text2);
		IReadOnlyList<T> readOnlyList = text1.Substring(0, commonPrefix);
		text1 = text1.Substring(commonPrefix);
		text2 = text2.Substring(commonPrefix);
		commonPrefix = GetCommonSuffix(text1, text2);
		IReadOnlyList<T> readOnlyList2 = text1.Substring(text1.Count - commonPrefix);
		text1 = text1.Substring(0, text1.Count - commonPrefix);
		text2 = text2.Substring(0, text2.Count - commonPrefix);
		List<Diff<T>> list = Compute(text1, text2);
		if (readOnlyList.Count != 0)
		{
			list.Insert(0, new Diff<T>(Operation.Equal, readOnlyList));
		}
		if (readOnlyList2.Count != 0)
		{
			list.Add(new Diff<T>(Operation.Equal, readOnlyList2));
		}
		CleanupMerge(list);
		return list;
	}

	private List<Diff<T>> Compute<T>(IReadOnlyList<T> text1, IReadOnlyList<T> text2)
	{
		List<Diff<T>> list = new List<Diff<T>>();
		if (text1.Count == 0)
		{
			list.Add(new Diff<T>(Operation.Insert, text2));
			return list;
		}
		if (text2.Count == 0)
		{
			list.Add(new Diff<T>(Operation.Delete, text1));
			return list;
		}
		IReadOnlyList<T> target = ((text1.Count > text2.Count) ? text1 : text2);
		IReadOnlyList<T> readOnlyList = ((text1.Count > text2.Count) ? text2 : text1);
		int num = target.IndexOf(readOnlyList);
		if (num != -1)
		{
			Operation operation = ((text1.Count <= text2.Count) ? Operation.Insert : Operation.Delete);
			list.Add(new Diff<T>(operation, target.Substring(0, num)));
			list.Add(new Diff<T>(Operation.Equal, readOnlyList));
			list.Add(new Diff<T>(operation, target.Substring(num + readOnlyList.Count)));
			return list;
		}
		IReadOnlyList<T>[] halfMatch = GetHalfMatch(text1, text2);
		if (halfMatch != null)
		{
			IReadOnlyList<T> text3 = halfMatch[0];
			IReadOnlyList<T> text4 = halfMatch[1];
			IReadOnlyList<T> text5 = halfMatch[2];
			IReadOnlyList<T> text6 = halfMatch[3];
			IReadOnlyList<T> items = halfMatch[4];
			List<Diff<T>> list2 = CompareUsingOptions(text3, text5);
			List<Diff<T>> collection = CompareUsingOptions(text4, text6);
			list = list2;
			list.Add(new Diff<T>(Operation.Equal, items));
			list.AddRange(collection);
			return list;
		}
		list = GetMap(text1, text2);
		return list ?? new List<Diff<T>>
		{
			new Diff<T>(Operation.Delete, text1),
			new Diff<T>(Operation.Insert, text2)
		};
	}

	private List<Diff<T>> GetMap<T>(IReadOnlyList<T> text1, IReadOnlyList<T> text2)
	{
		DateTime dateTime = DateTime.Now + _diffTimeout;
		int count = text1.Count;
		int count2 = text2.Count;
		int num = count + count2 - 1;
		bool flag = _diffDualThreshold * 2 < num;
		List<HashSet<long>> list = new List<HashSet<long>>();
		List<HashSet<long>> list2 = new List<HashSet<long>>();
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		Dictionary<int, int> dictionary2 = new Dictionary<int, int>();
		dictionary.Add(1, 0);
		dictionary2.Add(1, 0);
		long key = 0L;
		Dictionary<long, int> dictionary3 = new Dictionary<long, int>();
		bool flag2 = false;
		bool flag3 = (count + count2) % 2 == 1;
		for (int i = 0; i < num; i++)
		{
			if (DateTime.Now > dateTime)
			{
				return null;
			}
			list.Add(new HashSet<long>());
			for (int j = -i; j <= i; j += 2)
			{
				int num2 = ((j != -i && (j == i || dictionary[j - 1] >= dictionary[j + 1])) ? (dictionary[j - 1] + 1) : dictionary[j + 1]);
				int num3 = num2 - j;
				if (flag)
				{
					key = GetFootprint(num2, num3);
					if (flag3 && dictionary3.ContainsKey(key))
					{
						flag2 = true;
					}
					if (!flag3)
					{
						dictionary3.Add(key, i);
					}
				}
				while (!flag2 && num2 < count && num3 < count2 && text1.EqualsAt(num2, text2, num3))
				{
					num2++;
					num3++;
					if (flag)
					{
						key = GetFootprint(num2, num3);
						if (flag3 && dictionary3.ContainsKey(key))
						{
							flag2 = true;
						}
						if (!flag3)
						{
							dictionary3.Add(key, i);
						}
					}
				}
				if (dictionary.ContainsKey(j))
				{
					dictionary[j] = num2;
				}
				else
				{
					dictionary.Add(j, num2);
				}
				list[i].Add(GetFootprint(num2, num3));
				if (num2 == count && num3 == count2)
				{
					return DiffPath1(list, text1, text2);
				}
				if (flag2)
				{
					list2 = list2.GetRange(0, dictionary3[key] + 1);
					List<Diff<T>> list3 = DiffPath1(list, text1.Substring(0, num2), text2.Substring(0, num3));
					list3.AddRange(DiffPath2(list2, text1.Substring(num2), text2.Substring(num3)));
					return list3;
				}
			}
			if (!flag)
			{
				continue;
			}
			list2.Add(new HashSet<long>());
			for (int k = -i; k <= i; k += 2)
			{
				int num2 = ((k != -i && (k == i || dictionary2[k - 1] >= dictionary2[k + 1])) ? (dictionary2[k - 1] + 1) : dictionary2[k + 1]);
				int num3 = num2 - k;
				key = GetFootprint(count - num2, count2 - num3);
				if (!flag3 && dictionary3.ContainsKey(key))
				{
					flag2 = true;
				}
				if (flag3)
				{
					dictionary3.Add(key, i);
				}
				while (!flag2 && num2 < count && num3 < count2 && text1.EqualsAt(count - num2 - 1, text2, count2 - num3 - 1))
				{
					num2++;
					num3++;
					key = GetFootprint(count - num2, count2 - num3);
					if (!flag3 && dictionary3.ContainsKey(key))
					{
						flag2 = true;
					}
					if (flag3)
					{
						dictionary3.Add(key, i);
					}
				}
				if (dictionary2.ContainsKey(k))
				{
					dictionary2[k] = num2;
				}
				else
				{
					dictionary2.Add(k, num2);
				}
				list2[i].Add(GetFootprint(num2, num3));
				if (flag2)
				{
					list = list.GetRange(0, dictionary3[key] + 1);
					List<Diff<T>> list4 = DiffPath1(list, text1.Substring(0, count - num2), text2.Substring(0, count2 - num3));
					list4.AddRange(DiffPath2(list2, text1.Substring(count - num2), text2.Substring(count2 - num3)));
					return list4;
				}
			}
		}
		return null;
	}

	private static List<Diff<T>> DiffPath1<T>(IReadOnlyList<HashSet<long>> vMap, IReadOnlyList<T> text1, IReadOnlyList<T> text2)
	{
		LinkedList<Diff<T>> linkedList = new LinkedList<Diff<T>>();
		int num = text1.Count;
		int num2 = text2.Count;
		Operation? operation = null;
		for (int num3 = vMap.Count - 2; num3 >= 0; num3--)
		{
			while (true)
			{
				if (vMap[num3].Contains(GetFootprint(num - 1, num2)))
				{
					num--;
					if (operation == Operation.Delete)
					{
						linkedList.First().Items = text1.Substring(num, 1).Concat(linkedList.First().Items).ToList();
					}
					else
					{
						linkedList.AddFirst(new Diff<T>(Operation.Delete, text1.Substring(num, 1)));
					}
					operation = Operation.Delete;
					break;
				}
				if (vMap[num3].Contains(GetFootprint(num, num2 - 1)))
				{
					num2--;
					if (operation == Operation.Insert)
					{
						linkedList.First().Items = text2.Substring(num2, 1).Concat(linkedList.First().Items).ToList();
					}
					else
					{
						linkedList.AddFirst(new Diff<T>(Operation.Insert, text2.Substring(num2, 1)));
					}
					operation = Operation.Insert;
					break;
				}
				num--;
				num2--;
				if (operation == Operation.Equal)
				{
					linkedList.First().Items = text1.Substring(num, 1).Concat(linkedList.First().Items).ToList();
				}
				else
				{
					linkedList.AddFirst(new Diff<T>(Operation.Equal, text1.Substring(num, 1)));
				}
				operation = Operation.Equal;
			}
		}
		return linkedList.ToList();
	}

	private static List<Diff<T>> DiffPath2<T>(IReadOnlyList<HashSet<long>> vMap, IReadOnlyList<T> text1, IReadOnlyList<T> text2)
	{
		LinkedList<Diff<T>> linkedList = new LinkedList<Diff<T>>();
		int num = text1.Count;
		int num2 = text2.Count;
		Operation? operation = null;
		for (int num3 = vMap.Count - 2; num3 >= 0; num3--)
		{
			while (true)
			{
				if (vMap[num3].Contains(GetFootprint(num - 1, num2)))
				{
					num--;
					if (operation == Operation.Delete)
					{
						linkedList.Last().Items = linkedList.Last().Items.Concat(text1.Substring(text1.Count - num - 1, 1)).ToList();
					}
					else
					{
						linkedList.AddLast(new Diff<T>(Operation.Delete, text1.Substring(text1.Count - num - 1, 1)));
					}
					operation = Operation.Delete;
					break;
				}
				if (vMap[num3].Contains(GetFootprint(num, num2 - 1)))
				{
					num2--;
					if (operation == Operation.Insert)
					{
						linkedList.Last().Items = linkedList.Last().Items.Concat(text2.Substring(text2.Count - num2 - 1, 1)).ToList();
					}
					else
					{
						linkedList.AddLast(new Diff<T>(Operation.Insert, text2.Substring(text2.Count - num2 - 1, 1)));
					}
					operation = Operation.Insert;
					break;
				}
				num--;
				num2--;
				if (operation == Operation.Equal)
				{
					linkedList.Last().Items = linkedList.Last().Items.Concat(text1.Substring(text1.Count - num - 1, 1)).ToList();
				}
				else
				{
					linkedList.AddLast(new Diff<T>(Operation.Equal, text1.Substring(text1.Count - num - 1, 1)));
				}
				operation = Operation.Equal;
			}
		}
		return linkedList.ToList();
	}

	private static long GetFootprint(int x, int y)
	{
		return ((long)x << 32) + y;
	}

	private static int GetCommonPrefix<T>(IReadOnlyList<T> text1, IReadOnlyList<T> text2)
	{
		int num = Math.Min(text1.Count, text2.Count);
		for (int i = 0; i < num; i++)
		{
			if (!text1.EqualsAt(i, text2, i))
			{
				return i;
			}
		}
		return num;
	}

	private static int GetCommonSuffix<T>(IReadOnlyList<T> text1, IReadOnlyList<T> text2)
	{
		int count = text1.Count;
		int count2 = text2.Count;
		int num = Math.Min(text1.Count, text2.Count);
		for (int i = 1; i <= num; i++)
		{
			if (!text1.EqualsAt(count - i, text2, count2 - i))
			{
				return i - 1;
			}
		}
		return num;
	}

	private static IReadOnlyList<T>[] GetHalfMatch<T>(IReadOnlyList<T> text1, IReadOnlyList<T> text2)
	{
		IReadOnlyList<T> readOnlyList = ((text1.Count > text2.Count) ? text1 : text2);
		IReadOnlyList<T> readOnlyList2 = ((text1.Count > text2.Count) ? text2 : text1);
		if (readOnlyList.Count < 10 || readOnlyList2.Count < 1)
		{
			return null;
		}
		IReadOnlyList<T>[] halfMatchI = GetHalfMatchI(readOnlyList, readOnlyList2, (readOnlyList.Count + 3) / 4);
		IReadOnlyList<T>[] halfMatchI2 = GetHalfMatchI(readOnlyList, readOnlyList2, (readOnlyList.Count + 1) / 2);
		if (halfMatchI == null && halfMatchI2 == null)
		{
			return null;
		}
		IReadOnlyList<T>[] array = ((halfMatchI2 == null) ? halfMatchI : ((halfMatchI != null) ? ((halfMatchI[4].Count > halfMatchI2[4].Count) ? halfMatchI : halfMatchI2) : halfMatchI2));
		if (text1.Count <= text2.Count)
		{
			return new IReadOnlyList<T>[5]
			{
				array[2],
				array[3],
				array[0],
				array[1],
				array[4]
			};
		}
		return array;
	}

	private static IReadOnlyList<T>[] GetHalfMatchI<T>(IReadOnlyList<T> longtext, IReadOnlyList<T> shorttext, int i)
	{
		IReadOnlyList<T> other = longtext.Substring(i, longtext.Count / 4);
		int num = -1;
		IReadOnlyList<T> readOnlyList = EmptyList<T>.Instance;
		IReadOnlyList<T> readOnlyList2 = EmptyList<T>.Instance;
		IReadOnlyList<T> readOnlyList3 = EmptyList<T>.Instance;
		IReadOnlyList<T> readOnlyList4 = EmptyList<T>.Instance;
		IReadOnlyList<T> readOnlyList5 = EmptyList<T>.Instance;
		while (num < shorttext.Count && (num = shorttext.IndexOf(other, num + 1)) != -1)
		{
			int commonPrefix = GetCommonPrefix(longtext.Substring(i), shorttext.Substring(num));
			int commonSuffix = GetCommonSuffix(longtext.Substring(0, i), shorttext.Substring(0, num));
			if (readOnlyList.Count < commonSuffix + commonPrefix)
			{
				readOnlyList = shorttext.Substring(num - commonSuffix, commonSuffix).Concat(shorttext.Substring(num, commonPrefix)).ToList();
				readOnlyList2 = longtext.Substring(0, i - commonSuffix);
				readOnlyList3 = longtext.Substring(i + commonPrefix);
				readOnlyList4 = shorttext.Substring(0, num - commonSuffix);
				readOnlyList5 = shorttext.Substring(num + commonPrefix);
			}
		}
		if (readOnlyList.Count >= longtext.Count / 2)
		{
			return new IReadOnlyList<T>[5] { readOnlyList2, readOnlyList3, readOnlyList4, readOnlyList5, readOnlyList };
		}
		return null;
	}

	private static void CleanupMerge<T>(List<Diff<T>> diffs)
	{
		bool flag;
		do
		{
			diffs.Add(new Diff<T>(Operation.Equal, EmptyList<T>.Instance));
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			IReadOnlyList<T> readOnlyList = EmptyList<T>.Instance;
			IReadOnlyList<T> readOnlyList2 = EmptyList<T>.Instance;
			while (num < diffs.Count)
			{
				switch (diffs[num].Operation)
				{
				case Operation.Insert:
					num3++;
					readOnlyList2 = readOnlyList2.Concat(diffs[num].Items).ToList();
					num++;
					break;
				case Operation.Delete:
					num2++;
					readOnlyList = readOnlyList.Concat(diffs[num].Items).ToList();
					num++;
					break;
				case Operation.Equal:
					if (num2 != 0 || num3 != 0)
					{
						if (num2 != 0 && num3 != 0)
						{
							int commonPrefix = GetCommonPrefix(readOnlyList2, readOnlyList);
							if (commonPrefix != 0)
							{
								if (num - num2 - num3 > 0 && diffs[num - num2 - num3 - 1].Operation == Operation.Equal)
								{
									Diff<T> diff = diffs[num - num2 - num3 - 1];
									diff.Items = diff.Items.Concat(readOnlyList2.Substring(0, commonPrefix)).ToList();
								}
								else
								{
									diffs.Insert(0, new Diff<T>(Operation.Equal, readOnlyList2.Substring(0, commonPrefix)));
									num++;
								}
								readOnlyList2 = readOnlyList2.Substring(commonPrefix);
								readOnlyList = readOnlyList.Substring(commonPrefix);
							}
							commonPrefix = GetCommonSuffix(readOnlyList2, readOnlyList);
							if (commonPrefix != 0)
							{
								diffs[num].Items = readOnlyList2.Substring(readOnlyList2.Count - commonPrefix).Concat(diffs[num].Items).ToList();
								readOnlyList2 = readOnlyList2.Substring(0, readOnlyList2.Count - commonPrefix);
								readOnlyList = readOnlyList.Substring(0, readOnlyList.Count - commonPrefix);
							}
						}
						if (num2 == 0)
						{
							diffs.Splice(num - num2 - num3, num2 + num3, new Diff<T>(Operation.Insert, readOnlyList2));
						}
						else if (num3 == 0)
						{
							diffs.Splice(num - num2 - num3, num2 + num3, new Diff<T>(Operation.Delete, readOnlyList));
						}
						else
						{
							diffs.Splice(num - num2 - num3, num2 + num3, new Diff<T>(Operation.Delete, readOnlyList), new Diff<T>(Operation.Insert, readOnlyList2));
						}
						num = num - num2 - num3 + ((num2 != 0) ? 1 : 0) + ((num3 != 0) ? 1 : 0) + 1;
					}
					else if (num != 0 && diffs[num - 1].Operation == Operation.Equal)
					{
						diffs[num - 1].Items = diffs[num - 1].Items.Concat(diffs[num].Items).ToList();
						diffs.RemoveAt(num);
					}
					else
					{
						num++;
					}
					num3 = 0;
					num2 = 0;
					readOnlyList = EmptyList<T>.Instance;
					readOnlyList2 = EmptyList<T>.Instance;
					break;
				}
			}
			if (diffs[diffs.Count - 1].Items.Count == 0)
			{
				diffs.RemoveAt(diffs.Count - 1);
			}
			flag = false;
			for (num = 1; num < diffs.Count - 1; num++)
			{
				if (diffs[num - 1].Operation == Operation.Equal && diffs[num + 1].Operation == Operation.Equal)
				{
					if (diffs[num].Items.EndsWith(diffs[num - 1].Items))
					{
						diffs[num].Items = diffs[num - 1].Items.Concat(diffs[num].Items.Substring(0, diffs[num].Items.Count - diffs[num - 1].Items.Count)).ToList();
						diffs[num + 1].Items = diffs[num - 1].Items.Concat(diffs[num + 1].Items).ToList();
						diffs.Splice(num - 1, 1);
						flag = true;
					}
					else if (diffs[num].Items.StartsWith(diffs[num + 1].Items))
					{
						diffs[num - 1].Items = diffs[num - 1].Items.Concat(diffs[num + 1].Items).ToList();
						diffs[num].Items = diffs[num].Items.Substring(diffs[num + 1].Items.Count).Concat(diffs[num + 1].Items).ToList();
						diffs.Splice(num + 1, 1);
						flag = true;
					}
				}
			}
		}
		while (flag);
	}
}
