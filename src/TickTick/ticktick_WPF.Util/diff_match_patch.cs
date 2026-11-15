using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ticktick_WPF.Util;

public class diff_match_patch
{
	public float Diff_Timeout = 1f;

	public short Diff_EditCost = 4;

	public short Diff_DualThreshold = 32;

	public float Match_Threshold = 0.5f;

	public int Match_Distance = 1000;

	public float Patch_DeleteThreshold = 0.5f;

	public short Patch_Margin = 4;

	private int Match_MaxBits = 32;

	private Regex BLANKLINEEND = new Regex("\\n\\r?\\n\\Z");

	private Regex BLANKLINESTART = new Regex("\\A\\r?\\n\\r?\\n");

	public List<Diff> diff_main(string text1, string text2)
	{
		return diff_main(text1, text2, checklines: true);
	}

	public List<Diff> diff_main(string text1, string text2, bool checklines)
	{
		List<Diff> list;
		if (text1 == text2)
		{
			list = new List<Diff>();
			list.Add(new Diff(Operation.EQUAL, text1));
			return list;
		}
		int num = diff_commonPrefix(text1, text2);
		string text3 = text1.Substring(0, num);
		text1 = text1.Substring(num);
		text2 = text2.Substring(num);
		num = diff_commonSuffix(text1, text2);
		string text4 = text1.Substring(text1.Length - num);
		text1 = text1.Substring(0, text1.Length - num);
		text2 = text2.Substring(0, text2.Length - num);
		list = diff_compute(text1, text2, checklines);
		if (text3.Length != 0)
		{
			list.Insert(0, new Diff(Operation.EQUAL, text3));
		}
		if (text4.Length != 0)
		{
			list.Add(new Diff(Operation.EQUAL, text4));
		}
		diff_cleanupMerge(list);
		return list;
	}

	protected List<Diff> diff_compute(string text1, string text2, bool checklines)
	{
		List<Diff> list = new List<Diff>();
		if (text1.Length == 0)
		{
			list.Add(new Diff(Operation.INSERT, text2));
			return list;
		}
		if (text2.Length == 0)
		{
			list.Add(new Diff(Operation.DELETE, text1));
			return list;
		}
		string text3 = ((text1.Length > text2.Length) ? text1 : text2);
		string text4 = ((text1.Length > text2.Length) ? text2 : text1);
		int num = text3.IndexOf(text4);
		if (num != -1)
		{
			Operation operation = ((text1.Length <= text2.Length) ? Operation.INSERT : Operation.DELETE);
			list.Add(new Diff(operation, text3.Substring(0, num)));
			list.Add(new Diff(Operation.EQUAL, text4));
			list.Add(new Diff(operation, text3.Substring(num + text4.Length)));
			return list;
		}
		text4 = null;
		string[] array = diff_halfMatch(text1, text2);
		if (array != null)
		{
			string text5 = array[0];
			string text6 = array[1];
			string text7 = array[2];
			string text8 = array[3];
			string text9 = array[4];
			List<Diff> list2 = diff_main(text5, text7, checklines);
			List<Diff> collection = diff_main(text6, text8, checklines);
			list = list2;
			list.Add(new Diff(Operation.EQUAL, text9));
			list.AddRange(collection);
			return list;
		}
		if (checklines && (text1.Length < 100 || text2.Length < 100))
		{
			checklines = false;
		}
		List<string> lineArray = null;
		if (checklines)
		{
			object[] array2 = diff_linesToChars(text1, text2);
			text1 = (string)array2[0];
			text2 = (string)array2[1];
			lineArray = (List<string>)array2[2];
		}
		list = diff_map(text1, text2);
		if (list == null)
		{
			list = new List<Diff>();
			list.Add(new Diff(Operation.DELETE, text1));
			list.Add(new Diff(Operation.INSERT, text2));
		}
		if (checklines)
		{
			diff_charsToLines(list, lineArray);
			diff_cleanupSemantic(list);
			list.Add(new Diff(Operation.EQUAL, string.Empty));
			int i = 0;
			int num2 = 0;
			int num3 = 0;
			string text10 = string.Empty;
			string text11 = string.Empty;
			for (; i < list.Count; i++)
			{
				switch (list[i].operation)
				{
				case Operation.INSERT:
					num3++;
					text11 += list[i].text;
					break;
				case Operation.DELETE:
					num2++;
					text10 += list[i].text;
					break;
				case Operation.EQUAL:
					if (num2 >= 1 && num3 >= 1)
					{
						List<Diff> list3 = diff_main(text10, text11, checklines: false);
						list.RemoveRange(i - num2 - num3, num2 + num3);
						i = i - num2 - num3;
						list.InsertRange(i, list3);
						i += list3.Count;
					}
					num3 = 0;
					num2 = 0;
					text10 = string.Empty;
					text11 = string.Empty;
					break;
				}
			}
			list.RemoveAt(list.Count - 1);
		}
		return list;
	}

	protected object[] diff_linesToChars(string text1, string text2)
	{
		List<string> list = new List<string>();
		Dictionary<string, int> lineHash = new Dictionary<string, int>();
		list.Add(string.Empty);
		string text3 = diff_linesToCharsMunge(text1, list, lineHash);
		string text4 = diff_linesToCharsMunge(text2, list, lineHash);
		return new object[3] { text3, text4, list };
	}

	private string diff_linesToCharsMunge(string text, List<string> lineArray, Dictionary<string, int> lineHash)
	{
		int num = 0;
		int num2 = -1;
		StringBuilder stringBuilder = new StringBuilder();
		while (num2 < text.Length - 1)
		{
			num2 = text.IndexOf('\n', num);
			if (num2 == -1)
			{
				num2 = text.Length - 1;
			}
			string text2 = text.JavaSubstring(num, num2 + 1);
			num = num2 + 1;
			if (lineHash.ContainsKey(text2))
			{
				stringBuilder.Append((char)lineHash[text2]);
				continue;
			}
			lineArray.Add(text2);
			lineHash.Add(text2, lineArray.Count - 1);
			stringBuilder.Append((char)(lineArray.Count - 1));
		}
		return stringBuilder.ToString();
	}

	protected void diff_charsToLines(ICollection<Diff> diffs, List<string> lineArray)
	{
		foreach (Diff diff in diffs)
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < diff.text.Length; i++)
			{
				stringBuilder.Append(lineArray[diff.text[i]]);
			}
			diff.text = stringBuilder.ToString();
		}
	}

	protected List<Diff> diff_map(string text1, string text2)
	{
		DateTime dateTime = DateTime.Now + new TimeSpan(0, 0, (int)Diff_Timeout);
		int length = text1.Length;
		int length2 = text2.Length;
		int num = length + length2 - 1;
		bool flag = Diff_DualThreshold * 2 < num;
		List<HashSet<long>> list = new List<HashSet<long>>();
		List<HashSet<long>> list2 = new List<HashSet<long>>();
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		Dictionary<int, int> dictionary2 = new Dictionary<int, int>();
		dictionary.Add(1, 0);
		dictionary2.Add(1, 0);
		long key = 0L;
		Dictionary<long, int> dictionary3 = new Dictionary<long, int>();
		bool flag2 = false;
		bool flag3 = (length + length2) % 2 == 1;
		for (int i = 0; i < num; i++)
		{
			if (Diff_Timeout > 0f && DateTime.Now > dateTime)
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
					key = diff_footprint(num2, num3);
					if (flag3 && dictionary3.ContainsKey(key))
					{
						flag2 = true;
					}
					if (!flag3)
					{
						dictionary3.Add(key, i);
					}
				}
				while (!flag2 && num2 < length && num3 < length2 && text1[num2] == text2[num3])
				{
					num2++;
					num3++;
					if (flag)
					{
						key = diff_footprint(num2, num3);
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
				list[i].Add(diff_footprint(num2, num3));
				if (num2 == length && num3 == length2)
				{
					return diff_path1(list, text1, text2);
				}
				if (flag2)
				{
					list2 = list2.GetRange(0, dictionary3[key] + 1);
					List<Diff> list3 = diff_path1(list, text1.Substring(0, num2), text2.Substring(0, num3));
					list3.AddRange(diff_path2(list2, text1.Substring(num2), text2.Substring(num3)));
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
				key = diff_footprint(length - num2, length2 - num3);
				if (!flag3 && dictionary3.ContainsKey(key))
				{
					flag2 = true;
				}
				if (flag3)
				{
					dictionary3.Add(key, i);
				}
				while (!flag2 && num2 < length && num3 < length2 && text1[length - num2 - 1] == text2[length2 - num3 - 1])
				{
					num2++;
					num3++;
					key = diff_footprint(length - num2, length2 - num3);
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
				list2[i].Add(diff_footprint(num2, num3));
				if (flag2)
				{
					list = list.GetRange(0, dictionary3[key] + 1);
					List<Diff> list4 = diff_path1(list, text1.Substring(0, length - num2), text2.Substring(0, length2 - num3));
					list4.AddRange(diff_path2(list2, text1.Substring(length - num2), text2.Substring(length2 - num3)));
					return list4;
				}
			}
		}
		return null;
	}

	protected List<Diff> diff_path1(List<HashSet<long>> v_map, string text1, string text2)
	{
		LinkedList<Diff> linkedList = new LinkedList<Diff>();
		int num = text1.Length;
		int num2 = text2.Length;
		Operation? operation = null;
		for (int num3 = v_map.Count - 2; num3 >= 0; num3--)
		{
			while (true)
			{
				if (v_map[num3].Contains(diff_footprint(num - 1, num2)))
				{
					num--;
					if (operation == Operation.DELETE)
					{
						linkedList.First().text = text1[num] + linkedList.First().text;
					}
					else
					{
						linkedList.AddFirst(new Diff(Operation.DELETE, text1.Substring(num, 1)));
					}
					operation = Operation.DELETE;
					break;
				}
				if (v_map[num3].Contains(diff_footprint(num, num2 - 1)))
				{
					num2--;
					if (operation == Operation.INSERT)
					{
						linkedList.First().text = text2[num2] + linkedList.First().text;
					}
					else
					{
						linkedList.AddFirst(new Diff(Operation.INSERT, text2.Substring(num2, 1)));
					}
					operation = Operation.INSERT;
					break;
				}
				num--;
				num2--;
				if (operation == Operation.EQUAL)
				{
					linkedList.First().text = text1[num] + linkedList.First().text;
				}
				else
				{
					linkedList.AddFirst(new Diff(Operation.EQUAL, text1.Substring(num, 1)));
				}
				operation = Operation.EQUAL;
			}
		}
		return linkedList.ToList();
	}

	protected List<Diff> diff_path2(List<HashSet<long>> v_map, string text1, string text2)
	{
		LinkedList<Diff> linkedList = new LinkedList<Diff>();
		int num = text1.Length;
		int num2 = text2.Length;
		Operation? operation = null;
		for (int num3 = v_map.Count - 2; num3 >= 0; num3--)
		{
			while (true)
			{
				if (v_map[num3].Contains(diff_footprint(num - 1, num2)))
				{
					num--;
					if (operation == Operation.DELETE)
					{
						linkedList.Last().text += text1[text1.Length - num - 1];
					}
					else
					{
						linkedList.AddLast(new Diff(Operation.DELETE, text1.Substring(text1.Length - num - 1, 1)));
					}
					operation = Operation.DELETE;
					break;
				}
				if (v_map[num3].Contains(diff_footprint(num, num2 - 1)))
				{
					num2--;
					if (operation == Operation.INSERT)
					{
						linkedList.Last().text += text2[text2.Length - num2 - 1];
					}
					else
					{
						linkedList.AddLast(new Diff(Operation.INSERT, text2.Substring(text2.Length - num2 - 1, 1)));
					}
					operation = Operation.INSERT;
					break;
				}
				num--;
				num2--;
				if (operation == Operation.EQUAL)
				{
					linkedList.Last().text += text1[text1.Length - num - 1];
				}
				else
				{
					linkedList.AddLast(new Diff(Operation.EQUAL, text1.Substring(text1.Length - num - 1, 1)));
				}
				operation = Operation.EQUAL;
			}
		}
		return linkedList.ToList();
	}

	protected long diff_footprint(int x, int y)
	{
		return ((long)x << 32) + y;
	}

	public int diff_commonPrefix(string text1, string text2)
	{
		int num = Math.Min(text1.Length, text2.Length);
		for (int i = 0; i < num; i++)
		{
			if (text1[i] != text2[i])
			{
				return i;
			}
		}
		return num;
	}

	public int diff_commonSuffix(string text1, string text2)
	{
		int length = text1.Length;
		int length2 = text2.Length;
		int num = Math.Min(text1.Length, text2.Length);
		for (int i = 1; i <= num; i++)
		{
			if (text1[length - i] != text2[length2 - i])
			{
				return i - 1;
			}
		}
		return num;
	}

	protected string[] diff_halfMatch(string text1, string text2)
	{
		string text3 = ((text1.Length > text2.Length) ? text1 : text2);
		string text4 = ((text1.Length > text2.Length) ? text2 : text1);
		if (text3.Length < 10 || text4.Length < 1)
		{
			return null;
		}
		string[] array = diff_halfMatchI(text3, text4, (text3.Length + 3) / 4);
		string[] array2 = diff_halfMatchI(text3, text4, (text3.Length + 1) / 2);
		if (array == null && array2 == null)
		{
			return null;
		}
		string[] array3 = ((array2 == null) ? array : ((array != null) ? ((array[4].Length > array2[4].Length) ? array : array2) : array2));
		if (text1.Length > text2.Length)
		{
			return array3;
		}
		return new string[5]
		{
			array3[2],
			array3[3],
			array3[0],
			array3[1],
			array3[4]
		};
	}

	private string[] diff_halfMatchI(string longtext, string shorttext, int i)
	{
		string value = longtext.Substring(i, longtext.Length / 4);
		int num = -1;
		string text = string.Empty;
		string text2 = string.Empty;
		string text3 = string.Empty;
		string text4 = string.Empty;
		string text5 = string.Empty;
		while (num < shorttext.Length && (num = shorttext.IndexOf(value, num + 1)) != -1)
		{
			int num2 = diff_commonPrefix(longtext.Substring(i), shorttext.Substring(num));
			int num3 = diff_commonSuffix(longtext.Substring(0, i), shorttext.Substring(0, num));
			if (text.Length < num3 + num2)
			{
				text = shorttext.Substring(num - num3, num3) + shorttext.Substring(num, num2);
				text2 = longtext.Substring(0, i - num3);
				text3 = longtext.Substring(i + num2);
				text4 = shorttext.Substring(0, num - num3);
				text5 = shorttext.Substring(num + num2);
			}
		}
		if (text.Length >= longtext.Length / 2)
		{
			return new string[5] { text2, text3, text4, text5, text };
		}
		return null;
	}

	public void diff_cleanupSemantic(List<Diff> diffs)
	{
		bool flag = false;
		Stack<int> stack = new Stack<int>();
		string text = null;
		int i = 0;
		int num = 0;
		int num2 = 0;
		for (; i < diffs.Count; i++)
		{
			if (diffs[i].operation == Operation.EQUAL)
			{
				stack.Push(i);
				num = num2;
				num2 = 0;
				text = diffs[i].text;
				continue;
			}
			num2 += diffs[i].text.Length;
			if (text != null && text.Length <= num && text.Length <= num2)
			{
				diffs.Insert(stack.Peek(), new Diff(Operation.DELETE, text));
				diffs[stack.Peek() + 1].operation = Operation.INSERT;
				stack.Pop();
				if (stack.Count > 0)
				{
					stack.Pop();
				}
				i = ((stack.Count > 0) ? stack.Peek() : (-1));
				num = 0;
				num2 = 0;
				text = null;
				flag = true;
			}
		}
		if (flag)
		{
			diff_cleanupMerge(diffs);
		}
		diff_cleanupSemanticLossless(diffs);
	}

	public void diff_cleanupSemanticLossless(List<Diff> diffs)
	{
		for (int i = 1; i < diffs.Count - 1; i++)
		{
			if (diffs[i - 1].operation != Operation.EQUAL || diffs[i + 1].operation != Operation.EQUAL)
			{
				continue;
			}
			string text = diffs[i - 1].text;
			string text2 = diffs[i].text;
			string text3 = diffs[i + 1].text;
			int num = diff_commonSuffix(text, text2);
			if (num > 0)
			{
				string text4 = text2.Substring(text2.Length - num);
				text = text.Substring(0, text.Length - num);
				text2 = text4 + text2.Substring(0, text2.Length - num);
				text3 = text4 + text3;
			}
			string text5 = text;
			string text6 = text2;
			string text7 = text3;
			int num2 = diff_cleanupSemanticScore(text, text2) + diff_cleanupSemanticScore(text2, text3);
			while (text2.Length != 0 && text3.Length != 0 && text2[0] == text3[0])
			{
				text += text2[0];
				text2 = text2.Substring(1) + text3[0];
				text3 = text3.Substring(1);
				int num3 = diff_cleanupSemanticScore(text, text2) + diff_cleanupSemanticScore(text2, text3);
				if (num3 >= num2)
				{
					num2 = num3;
					text5 = text;
					text6 = text2;
					text7 = text3;
				}
			}
			if (diffs[i - 1].text != text5)
			{
				if (text5.Length != 0)
				{
					diffs[i - 1].text = text5;
				}
				else
				{
					diffs.RemoveAt(i - 1);
					i--;
				}
				diffs[i].text = text6;
				if (text7.Length != 0)
				{
					diffs[i + 1].text = text7;
					continue;
				}
				diffs.RemoveAt(i + 1);
				i--;
			}
		}
	}

	private int diff_cleanupSemanticScore(string one, string two)
	{
		if (one.Length == 0 || two.Length == 0)
		{
			return 5;
		}
		int num = 0;
		if (!char.IsLetterOrDigit(one[one.Length - 1]) || !char.IsLetterOrDigit(two[0]))
		{
			num++;
			if (char.IsWhiteSpace(one[one.Length - 1]) || char.IsWhiteSpace(two[0]))
			{
				num++;
				if (char.IsControl(one[one.Length - 1]) || char.IsControl(two[0]))
				{
					num++;
					if (BLANKLINEEND.IsMatch(one) || BLANKLINESTART.IsMatch(two))
					{
						num++;
					}
				}
			}
		}
		return num;
	}

	public void diff_cleanupEfficiency(List<Diff> diffs)
	{
		bool flag = false;
		Stack<int> stack = new Stack<int>();
		string text = string.Empty;
		int i = 0;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		bool flag5 = false;
		for (; i < diffs.Count; i++)
		{
			if (diffs[i].operation == Operation.EQUAL)
			{
				if (diffs[i].text.Length < Diff_EditCost && (flag4 || flag5))
				{
					stack.Push(i);
					flag2 = flag4;
					flag3 = flag5;
					text = diffs[i].text;
				}
				else
				{
					stack.Clear();
					text = string.Empty;
				}
				flag4 = (flag5 = false);
				continue;
			}
			if (diffs[i].operation == Operation.DELETE)
			{
				flag5 = true;
			}
			else
			{
				flag4 = true;
			}
			if (text.Length == 0 || (!(flag2 && flag3 && flag4 && flag5) && (text.Length >= Diff_EditCost / 2 || (flag2 ? 1 : 0) + (flag3 ? 1 : 0) + (flag4 ? 1 : 0) + (flag5 ? 1 : 0) != 3)))
			{
				continue;
			}
			diffs.Insert(stack.Peek(), new Diff(Operation.DELETE, text));
			diffs[stack.Peek() + 1].operation = Operation.INSERT;
			stack.Pop();
			text = string.Empty;
			if (flag2 && flag3)
			{
				flag4 = (flag5 = true);
				stack.Clear();
			}
			else
			{
				if (stack.Count > 0)
				{
					stack.Pop();
				}
				i = ((stack.Count > 0) ? stack.Peek() : (-1));
				flag4 = (flag5 = false);
			}
			flag = true;
		}
		if (flag)
		{
			diff_cleanupMerge(diffs);
		}
	}

	public void diff_cleanupMerge(List<Diff> diffs)
	{
		diffs.Add(new Diff(Operation.EQUAL, string.Empty));
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		string text = string.Empty;
		string text2 = string.Empty;
		while (num < diffs.Count)
		{
			switch (diffs[num].operation)
			{
			case Operation.INSERT:
				num3++;
				text2 += diffs[num].text;
				num++;
				break;
			case Operation.DELETE:
				num2++;
				text += diffs[num].text;
				num++;
				break;
			case Operation.EQUAL:
				if (num2 != 0 || num3 != 0)
				{
					if (num2 != 0 && num3 != 0)
					{
						int num4 = diff_commonPrefix(text2, text);
						if (num4 != 0)
						{
							if (num - num2 - num3 > 0 && diffs[num - num2 - num3 - 1].operation == Operation.EQUAL)
							{
								diffs[num - num2 - num3 - 1].text += text2.Substring(0, num4);
							}
							else
							{
								diffs.Insert(0, new Diff(Operation.EQUAL, text2.Substring(0, num4)));
								num++;
							}
							text2 = text2.Substring(num4);
							text = text.Substring(num4);
						}
						num4 = diff_commonSuffix(text2, text);
						if (num4 != 0)
						{
							diffs[num].text = text2.Substring(text2.Length - num4) + diffs[num].text;
							text2 = text2.Substring(0, text2.Length - num4);
							text = text.Substring(0, text.Length - num4);
						}
					}
					if (num2 == 0)
					{
						diffs.Splice(num - num2 - num3, num2 + num3, new Diff(Operation.INSERT, text2));
					}
					else if (num3 == 0)
					{
						diffs.Splice(num - num2 - num3, num2 + num3, new Diff(Operation.DELETE, text));
					}
					else
					{
						diffs.Splice(num - num2 - num3, num2 + num3, new Diff(Operation.DELETE, text), new Diff(Operation.INSERT, text2));
					}
					num = num - num2 - num3 + ((num2 != 0) ? 1 : 0) + ((num3 != 0) ? 1 : 0) + 1;
				}
				else if (num != 0 && diffs[num - 1].operation == Operation.EQUAL)
				{
					diffs[num - 1].text += diffs[num].text;
					diffs.RemoveAt(num);
				}
				else
				{
					num++;
				}
				num3 = 0;
				num2 = 0;
				text = string.Empty;
				text2 = string.Empty;
				break;
			}
		}
		if (diffs[diffs.Count - 1].text.Length == 0)
		{
			diffs.RemoveAt(diffs.Count - 1);
		}
		bool flag = false;
		for (num = 1; num < diffs.Count - 1; num++)
		{
			if (diffs[num - 1].operation == Operation.EQUAL && diffs[num + 1].operation == Operation.EQUAL)
			{
				if (diffs[num].text.EndsWith(diffs[num - 1].text))
				{
					diffs[num].text = diffs[num - 1].text + diffs[num].text.Substring(0, diffs[num].text.Length - diffs[num - 1].text.Length);
					diffs[num + 1].text = diffs[num - 1].text + diffs[num + 1].text;
					diffs.Splice(num - 1, 1);
					flag = true;
				}
				else if (diffs[num].text.StartsWith(diffs[num + 1].text))
				{
					diffs[num - 1].text += diffs[num + 1].text;
					diffs[num].text = diffs[num].text.Substring(diffs[num + 1].text.Length) + diffs[num + 1].text;
					diffs.Splice(num + 1, 1);
					flag = true;
				}
			}
		}
		if (flag)
		{
			diff_cleanupMerge(diffs);
		}
	}

	public int diff_xIndex(List<Diff> diffs, int loc)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		Diff diff = null;
		foreach (Diff diff2 in diffs)
		{
			if (diff2.operation != Operation.INSERT)
			{
				num += diff2.text.Length;
			}
			if (diff2.operation != Operation.DELETE)
			{
				num2 += diff2.text.Length;
			}
			if (num > loc)
			{
				diff = diff2;
				break;
			}
			num3 = num;
			num4 = num2;
		}
		if (diff != null && diff.operation == Operation.DELETE)
		{
			return num4;
		}
		return num4 + (loc - num3);
	}

	public string diff_prettyHtml(List<Diff> diffs)
	{
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		foreach (Diff diff in diffs)
		{
			string value = diff.text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
				.Replace("\n", "&para;<BR>");
			switch (diff.operation)
			{
			case Operation.INSERT:
				stringBuilder.Append("<INS STYLE=\"background:#E6FFE6;\" TITLE=\"i=").Append(num).Append("\">")
					.Append(value)
					.Append("</INS>");
				break;
			case Operation.DELETE:
				stringBuilder.Append("<DEL STYLE=\"background:#FFE6E6;\" TITLE=\"i=").Append(num).Append("\">")
					.Append(value)
					.Append("</DEL>");
				break;
			case Operation.EQUAL:
				stringBuilder.Append("<SPAN TITLE=\"i=").Append(num).Append("\">")
					.Append(value)
					.Append("</SPAN>");
				break;
			}
			if (diff.operation != Operation.DELETE)
			{
				num += diff.text.Length;
			}
		}
		return stringBuilder.ToString();
	}

	public string diff_text1(List<Diff> diffs)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (Diff diff in diffs)
		{
			if (diff.operation != Operation.INSERT)
			{
				stringBuilder.Append(diff.text);
			}
		}
		return stringBuilder.ToString();
	}

	public string diff_text2(List<Diff> diffs)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (Diff diff in diffs)
		{
			if (diff.operation != Operation.DELETE)
			{
				stringBuilder.Append(diff.text);
			}
		}
		return stringBuilder.ToString();
	}

	public int diff_levenshtein(List<Diff> diffs)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		foreach (Diff diff in diffs)
		{
			switch (diff.operation)
			{
			case Operation.INSERT:
				num2 += diff.text.Length;
				break;
			case Operation.DELETE:
				num3 += diff.text.Length;
				break;
			case Operation.EQUAL:
				num += Math.Max(num2, num3);
				num2 = 0;
				num3 = 0;
				break;
			}
		}
		return num + Math.Max(num2, num3);
	}

	public string diff_toDelta(List<Diff> diffs)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (Diff diff in diffs)
		{
			switch (diff.operation)
			{
			case Operation.INSERT:
				stringBuilder.Append("+").Append(diff.text).Replace('+', ' ')
					.Append("\t");
				break;
			case Operation.DELETE:
				stringBuilder.Append("-").Append(diff.text.Length).Append("\t");
				break;
			case Operation.EQUAL:
				stringBuilder.Append("=").Append(diff.text.Length).Append("\t");
				break;
			}
		}
		string text = stringBuilder.ToString();
		if (text.Length != 0)
		{
			text = text.Substring(0, text.Length - 1);
			text = unescapeForEncodeUriCompatability(text);
		}
		return text;
	}

	public List<Diff> diff_fromDelta(string text1, string delta)
	{
		List<Diff> list = new List<Diff>();
		int num = 0;
		string[] array = delta.Split(new string[1] { "\t" }, StringSplitOptions.None);
		foreach (string text2 in array)
		{
			if (text2.Length == 0)
			{
				continue;
			}
			string text3 = text2.Substring(1);
			switch (text2[0])
			{
			case '+':
				text3 = text3.Replace("+", "%2b");
				list.Add(new Diff(Operation.INSERT, text3));
				break;
			case '-':
			case '=':
			{
				int num2;
				try
				{
					num2 = Convert.ToInt32(text3);
				}
				catch (FormatException innerException)
				{
					throw new ArgumentException("Invalid number in diff_fromDelta: " + text3, innerException);
				}
				if (num2 < 0)
				{
					throw new ArgumentException("Negative number in diff_fromDelta: " + text3);
				}
				string text4;
				try
				{
					text4 = text1.Substring(num, num2);
					num += num2;
				}
				catch (ArgumentOutOfRangeException innerException2)
				{
					throw new ArgumentException("Delta length (" + num + ") larger than source text length (" + text1.Length + ").", innerException2);
				}
				if (text2[0] == '=')
				{
					list.Add(new Diff(Operation.EQUAL, text4));
				}
				else
				{
					list.Add(new Diff(Operation.DELETE, text4));
				}
				break;
			}
			default:
				throw new ArgumentException("Invalid diff operation in diff_fromDelta: " + text2[0]);
			}
		}
		if (num != text1.Length)
		{
			throw new ArgumentException("Delta length (" + num + ") smaller than source text length (" + text1.Length + ").");
		}
		return list;
	}

	public int match_main(string text, string pattern, int loc)
	{
		loc = Math.Max(0, Math.Min(loc, text.Length));
		if (text == pattern)
		{
			return 0;
		}
		if (text.Length == 0)
		{
			return -1;
		}
		if (loc + pattern.Length <= text.Length && text.Substring(loc, pattern.Length) == pattern)
		{
			return loc;
		}
		return match_bitap(text, pattern, loc);
	}

	protected int match_bitap(string text, string pattern, int loc)
	{
		Dictionary<char, int> dictionary = match_alphabet(pattern);
		double num = Match_Threshold;
		int num2 = text.IndexOf(pattern, loc);
		if (num2 != -1)
		{
			num = Math.Min(match_bitapScore(0, num2, loc, pattern), num);
			num2 = text.LastIndexOf(pattern, Math.Min(loc + pattern.Length, text.Length));
			if (num2 != -1)
			{
				num = Math.Min(match_bitapScore(0, num2, loc, pattern), num);
			}
		}
		int num3 = 1 << pattern.Length - 1;
		num2 = -1;
		int num4 = pattern.Length + text.Length;
		int[] array = new int[0];
		for (int i = 0; i < pattern.Length; i++)
		{
			int num5 = 0;
			int num6 = num4;
			while (num5 < num6)
			{
				if (match_bitapScore(i, loc + num6, loc, pattern) <= num)
				{
					num5 = num6;
				}
				else
				{
					num4 = num6;
				}
				num6 = (num4 - num5) / 2 + num5;
			}
			num4 = num6;
			int num7 = Math.Max(1, loc - num6 + 1);
			int num8 = Math.Min(loc + num6, text.Length) + pattern.Length;
			int[] array2 = new int[num8 + 2];
			array2[num8 + 1] = (1 << i) - 1;
			for (int num9 = num8; num9 >= num7; num9--)
			{
				int num10 = ((text.Length > num9 - 1 && dictionary.ContainsKey(text[num9 - 1])) ? dictionary[text[num9 - 1]] : 0);
				if (i == 0)
				{
					array2[num9] = ((array2[num9 + 1] << 1) | 1) & num10;
				}
				else
				{
					array2[num9] = (((array2[num9 + 1] << 1) | 1) & num10) | (((array[num9 + 1] | array[num9]) << 1) | 1) | array[num9 + 1];
				}
				if ((array2[num9] & num3) != 0)
				{
					double num11 = match_bitapScore(i, num9 - 1, loc, pattern);
					if (num11 <= num)
					{
						num = num11;
						num2 = num9 - 1;
						if (num2 <= loc)
						{
							break;
						}
						num7 = Math.Max(1, 2 * loc - num2);
					}
				}
			}
			if (match_bitapScore(i + 1, loc, loc, pattern) > num)
			{
				break;
			}
			array = array2;
		}
		return num2;
	}

	private double match_bitapScore(int e, int x, int loc, string pattern)
	{
		float num = (float)e / (float)pattern.Length;
		int num2 = Math.Abs(loc - x);
		if (Match_Distance == 0)
		{
			if (num2 != 0)
			{
				return 1.0;
			}
			return num;
		}
		return num + (float)num2 / (float)Match_Distance;
	}

	protected Dictionary<char, int> match_alphabet(string pattern)
	{
		Dictionary<char, int> dictionary = new Dictionary<char, int>();
		char[] array = pattern.ToCharArray();
		char[] array2 = array;
		foreach (char key in array2)
		{
			if (!dictionary.ContainsKey(key))
			{
				dictionary.Add(key, 0);
			}
		}
		int num = 0;
		array2 = array;
		foreach (char key2 in array2)
		{
			int value = dictionary[key2] | (1 << pattern.Length - num - 1);
			dictionary[key2] = value;
			num++;
		}
		return dictionary;
	}

	protected void patch_addContext(Patch patch, string text)
	{
		if (text.Length != 0)
		{
			string text2 = text.Substring(patch.start2, patch.length1);
			int num = 0;
			while (text.IndexOf(text2) != text.LastIndexOf(text2) && text2.Length < Match_MaxBits - Patch_Margin - Patch_Margin)
			{
				num += Patch_Margin;
				text2 = text.JavaSubstring(Math.Max(0, patch.start2 - num), Math.Min(text.Length, patch.start2 + patch.length1 + num));
			}
			num += Patch_Margin;
			string text3 = text.JavaSubstring(Math.Max(0, patch.start2 - num), patch.start2);
			if (text3.Length != 0)
			{
				patch.diffs.Insert(0, new Diff(Operation.EQUAL, text3));
			}
			string text4 = text.JavaSubstring(patch.start2 + patch.length1, Math.Min(text.Length, patch.start2 + patch.length1 + num));
			if (text4.Length != 0)
			{
				patch.diffs.Add(new Diff(Operation.EQUAL, text4));
			}
			patch.start1 -= text3.Length;
			patch.start2 -= text3.Length;
			patch.length1 += text3.Length + text4.Length;
			patch.length2 += text3.Length + text4.Length;
		}
	}

	public List<Patch> patch_make(string text1, string text2)
	{
		List<Diff> list = diff_main(text1, text2, checklines: true);
		if (list.Count > 2)
		{
			diff_cleanupSemantic(list);
			diff_cleanupEfficiency(list);
		}
		return patch_make(text1, list);
	}

	public List<Patch> patch_make(List<Diff> diffs)
	{
		string text = diff_text1(diffs);
		return patch_make(text, diffs);
	}

	public List<Patch> patch_make(string text1, string text2, List<Diff> diffs)
	{
		return patch_make(text1, diffs);
	}

	public List<Patch> patch_make(string text1, List<Diff> diffs)
	{
		List<Patch> list = new List<Patch>();
		if (diffs.Count == 0)
		{
			return list;
		}
		Patch patch = new Patch();
		int num = 0;
		int num2 = 0;
		string text2 = text1;
		string text3 = text1;
		foreach (Diff diff in diffs)
		{
			if (patch.diffs.Count == 0 && diff.operation != Operation.EQUAL)
			{
				patch.start1 = num;
				patch.start2 = num2;
			}
			switch (diff.operation)
			{
			case Operation.INSERT:
				patch.diffs.Add(diff);
				patch.length2 += diff.text.Length;
				text3 = text3.Substring(0, num2) + diff.text + text3.Substring(num2);
				break;
			case Operation.DELETE:
				patch.length1 += diff.text.Length;
				patch.diffs.Add(diff);
				text3 = text3.Substring(0, num2) + text3.Substring(num2 + diff.text.Length);
				break;
			case Operation.EQUAL:
				if (diff.text.Length <= 2 * Patch_Margin && patch.diffs.Count() != 0 && diff != diffs.Last())
				{
					patch.diffs.Add(diff);
					patch.length1 += diff.text.Length;
					patch.length2 += diff.text.Length;
				}
				if (diff.text.Length >= 2 * Patch_Margin && patch.diffs.Count != 0)
				{
					patch_addContext(patch, text2);
					list.Add(patch);
					patch = new Patch();
					text2 = text3;
					num = num2;
				}
				break;
			}
			if (diff.operation != Operation.INSERT)
			{
				num += diff.text.Length;
			}
			if (diff.operation != Operation.DELETE)
			{
				num2 += diff.text.Length;
			}
		}
		if (patch.diffs.Count != 0)
		{
			patch_addContext(patch, text2);
			list.Add(patch);
		}
		return list;
	}

	public List<Patch> patch_deepCopy(List<Patch> patches)
	{
		List<Patch> list = new List<Patch>();
		foreach (Patch patch2 in patches)
		{
			Patch patch = new Patch();
			foreach (Diff diff in patch2.diffs)
			{
				Diff item = new Diff(diff.operation, diff.text);
				patch.diffs.Add(item);
			}
			patch.start1 = patch2.start1;
			patch.start2 = patch2.start2;
			patch.length1 = patch2.length1;
			patch.length2 = patch2.length2;
			list.Add(patch);
		}
		return list;
	}

	public object[] patch_apply(List<Patch> patches, string text)
	{
		if (patches.Count != 0)
		{
			patches = patch_deepCopy(patches);
			string text2 = patch_addPadding(patches);
			text = text2 + text + text2;
			patch_splitMax(patches);
			int num = 0;
			int num2 = 0;
			bool[] array = new bool[patches.Count];
			foreach (Patch patch in patches)
			{
				int num3 = patch.start2 + num2;
				string text3 = diff_text1(patch.diffs);
				int num4 = -1;
				int num5;
				if (text3.Length > Match_MaxBits)
				{
					num5 = match_main(text, text3.Substring(0, Match_MaxBits), num3);
					if (num5 != -1)
					{
						num4 = match_main(text, text3.Substring(text3.Length - Match_MaxBits), num3 + text3.Length - Match_MaxBits);
						if (num4 == -1 || num5 >= num4)
						{
							num5 = -1;
						}
					}
				}
				else
				{
					num5 = match_main(text, text3, num3);
				}
				if (num5 == -1)
				{
					array[num] = false;
					num2 -= patch.length2 - patch.length1;
				}
				else
				{
					array[num] = true;
					num2 = num5 - num3;
					string text4 = ((num4 != -1) ? text.JavaSubstring(num5, Math.Min(num4 + Match_MaxBits, text.Length)) : text.JavaSubstring(num5, Math.Min(num5 + text3.Length, text.Length)));
					if (text3 == text4)
					{
						text = text.Substring(0, num5) + diff_text2(patch.diffs) + text.Substring(num5 + text3.Length);
					}
					else
					{
						List<Diff> diffs = diff_main(text3, text4, checklines: false);
						if (text3.Length > Match_MaxBits && (float)diff_levenshtein(diffs) / (float)text3.Length > Patch_DeleteThreshold)
						{
							array[num] = false;
						}
						else
						{
							diff_cleanupSemanticLossless(diffs);
							int num6 = 0;
							foreach (Diff diff in patch.diffs)
							{
								if (diff.operation != Operation.EQUAL)
								{
									int num7 = diff_xIndex(diffs, num6);
									if (diff.operation == Operation.INSERT)
									{
										text = text.Insert(num5 + num7, diff.text);
									}
									else if (diff.operation == Operation.DELETE)
									{
										text = text.Substring(0, num5 + num7) + text.Substring(num5 + diff_xIndex(diffs, num6 + diff.text.Length));
									}
								}
								if (diff.operation != Operation.DELETE)
								{
									num6 += diff.text.Length;
								}
							}
						}
					}
				}
				num++;
			}
			text = text.JavaSubstring(text2.Length, text.Length - text2.Length);
			return new object[2] { text, array };
		}
		return new object[2]
		{
			text,
			new bool[0]
		};
	}

	public string patch_addPadding(List<Patch> patches)
	{
		int patch_Margin = Patch_Margin;
		string text = string.Empty;
		for (int i = 1; i <= patch_Margin; i++)
		{
			text += (char)i;
		}
		foreach (Patch patch2 in patches)
		{
			patch2.start1 += patch_Margin;
			patch2.start2 += patch_Margin;
		}
		Patch patch = patches.First();
		List<Diff> diffs = patch.diffs;
		if (diffs.Count == 0 || diffs.First().operation != Operation.EQUAL)
		{
			diffs.Insert(0, new Diff(Operation.EQUAL, text));
			patch.start1 -= patch_Margin;
			patch.start2 -= patch_Margin;
			patch.length1 += patch_Margin;
			patch.length2 += patch_Margin;
		}
		else if (patch_Margin > diffs.First().text.Length)
		{
			Diff diff = diffs.First();
			int num = patch_Margin - diff.text.Length;
			diff.text = text.Substring(diff.text.Length) + diff.text;
			patch.start1 -= num;
			patch.start2 -= num;
			patch.length1 += num;
			patch.length2 += num;
		}
		patch = patches.Last();
		diffs = patch.diffs;
		if (diffs.Count == 0 || diffs.Last().operation != Operation.EQUAL)
		{
			diffs.Add(new Diff(Operation.EQUAL, text));
			patch.length1 += patch_Margin;
			patch.length2 += patch_Margin;
		}
		else if (patch_Margin > diffs.Last().text.Length)
		{
			Diff diff2 = diffs.Last();
			int num2 = patch_Margin - diff2.text.Length;
			diff2.text += text.Substring(0, num2);
			patch.length1 += num2;
			patch.length2 += num2;
		}
		return text;
	}

	public void patch_splitMax(List<Patch> patches)
	{
		for (int i = 0; i < patches.Count; i++)
		{
			if (patches[i].length1 <= Match_MaxBits)
			{
				continue;
			}
			Patch patch = patches[i];
			patches.Splice(i--, 1);
			int match_MaxBits = Match_MaxBits;
			int num = patch.start1;
			int num2 = patch.start2;
			string text = string.Empty;
			while (patch.diffs.Count != 0)
			{
				Patch patch2 = new Patch();
				bool flag = true;
				patch2.start1 = num - text.Length;
				patch2.start2 = num2 - text.Length;
				if (text.Length != 0)
				{
					patch2.length1 = (patch2.length2 = text.Length);
					patch2.diffs.Add(new Diff(Operation.EQUAL, text));
				}
				while (patch.diffs.Count != 0 && patch2.length1 < match_MaxBits - Patch_Margin)
				{
					Operation operation = patch.diffs[0].operation;
					string text2 = patch.diffs[0].text;
					switch (operation)
					{
					case Operation.INSERT:
						patch2.length2 += text2.Length;
						num2 += text2.Length;
						patch2.diffs.Add(patch.diffs.First());
						patch.diffs.RemoveAt(0);
						flag = false;
						continue;
					case Operation.DELETE:
						if (patch2.diffs.Count == 1 && patch2.diffs.First().operation == Operation.EQUAL && text2.Length > 2 * match_MaxBits)
						{
							patch2.length1 += text2.Length;
							num += text2.Length;
							flag = false;
							patch2.diffs.Add(new Diff(operation, text2));
							patch.diffs.RemoveAt(0);
							continue;
						}
						break;
					}
					text2 = text2.Substring(0, Math.Min(text2.Length, match_MaxBits - patch2.length1 - Patch_Margin));
					patch2.length1 += text2.Length;
					num += text2.Length;
					if (operation == Operation.EQUAL)
					{
						patch2.length2 += text2.Length;
						num2 += text2.Length;
					}
					else
					{
						flag = false;
					}
					patch2.diffs.Add(new Diff(operation, text2));
					if (text2 == patch.diffs[0].text)
					{
						patch.diffs.RemoveAt(0);
					}
					else
					{
						patch.diffs[0].text = patch.diffs[0].text.Substring(text2.Length);
					}
				}
				text = diff_text2(patch2.diffs);
				text = text.Substring(Math.Max(0, text.Length - Patch_Margin));
				string text3 = null;
				text3 = ((diff_text1(patch.diffs).Length <= Patch_Margin) ? diff_text1(patch.diffs) : diff_text1(patch.diffs).Substring(0, Patch_Margin));
				if (text3.Length != 0)
				{
					patch2.length1 += text3.Length;
					patch2.length2 += text3.Length;
					if (patch2.diffs.Count != 0 && patch2.diffs[patch2.diffs.Count - 1].operation == Operation.EQUAL)
					{
						patch2.diffs[patch2.diffs.Count - 1].text += text3;
					}
					else
					{
						patch2.diffs.Add(new Diff(Operation.EQUAL, text3));
					}
				}
				if (!flag)
				{
					patches.Splice(++i, 0, patch2);
				}
			}
		}
	}

	public string patch_toText(List<Patch> patches)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (Patch patch in patches)
		{
			stringBuilder.Append(patch);
		}
		return stringBuilder.ToString();
	}

	public List<Patch> patch_fromText(string textline)
	{
		List<Patch> list = new List<Patch>();
		if (textline.Length == 0)
		{
			return list;
		}
		LinkedList<string> linkedList = new LinkedList<string>(new List<string>(textline.Split(new string[1] { "\n" }, StringSplitOptions.None)));
		Regex regex = new Regex("^@@ -(\\d+),?(\\d*) \\+(\\d+),?(\\d*) @@$");
		while (linkedList.Count != 0)
		{
			Match match = regex.Match(linkedList.First());
			if (!match.Success)
			{
				throw new ArgumentException("Invalid patch string: " + linkedList.First());
			}
			Patch patch = new Patch();
			list.Add(patch);
			patch.start1 = Convert.ToInt32(match.Groups[1].Value);
			if (match.Groups[2].Length == 0)
			{
				patch.start1--;
				patch.length1 = 1;
			}
			else if (match.Groups[2].Value == "0")
			{
				patch.length1 = 0;
			}
			else
			{
				patch.start1--;
				patch.length1 = Convert.ToInt32(match.Groups[2].Value);
			}
			patch.start2 = Convert.ToInt32(match.Groups[3].Value);
			if (match.Groups[4].Length == 0)
			{
				patch.start2--;
				patch.length2 = 1;
			}
			else if (match.Groups[4].Value == "0")
			{
				patch.length2 = 0;
			}
			else
			{
				patch.start2--;
				patch.length2 = Convert.ToInt32(match.Groups[4].Value);
			}
			linkedList.RemoveFirst();
			while (linkedList.Count != 0)
			{
				char c;
				try
				{
					c = linkedList.First()[0];
				}
				catch (IndexOutOfRangeException)
				{
					linkedList.RemoveFirst();
					continue;
				}
				string text = linkedList.First().Substring(1);
				text = text.Replace("+", "%2b");
				if (c == '-')
				{
					patch.diffs.Add(new Diff(Operation.DELETE, text));
				}
				else if (c == '+')
				{
					patch.diffs.Add(new Diff(Operation.INSERT, text));
				}
				else
				{
					if (c != ' ')
					{
						if (c == '@')
						{
							break;
						}
						throw new ArgumentException("Invalid patch mode '" + c + "' in: " + text);
					}
					patch.diffs.Add(new Diff(Operation.EQUAL, text));
				}
				linkedList.RemoveFirst();
			}
		}
		return list;
	}

	public static string unescapeForEncodeUriCompatability(string str)
	{
		return str.Replace("%21", "!").Replace("%7e", "~").Replace("%27", "'")
			.Replace("%28", "(")
			.Replace("%29", ")")
			.Replace("%3b", ";")
			.Replace("%2f", "/")
			.Replace("%3f", "?")
			.Replace("%3a", ":")
			.Replace("%40", "@")
			.Replace("%26", "&")
			.Replace("%3d", "=")
			.Replace("%2b", "+")
			.Replace("%24", "$")
			.Replace("%2c", ",")
			.Replace("%23", "#");
	}
}
