using System;
using System.Collections.Generic;
using ticktick_WPF.ViewModels;

public class ChecklistItemComparaer : IComparer<CheckItemViewModel>
{
	public int Compare(CheckItemViewModel x, CheckItemViewModel y)
	{
		if (x != null && y != null)
		{
			int num = CompareStatus(x.Status, y.Status);
			if (num != 0)
			{
				return num;
			}
			if (x.Status == 0)
			{
				int num2 = CompareSortOrder(x.SortOrder, y.SortOrder);
				if (num2 != 0)
				{
					return num2;
				}
			}
			int num3 = CompareCompletedTime(x.CompletedTime, y.CompletedTime);
			if (num3 != 0)
			{
				return num3;
			}
			int num4 = CompareSortOrder(x.SortOrder, y.SortOrder);
			if (num4 != 0)
			{
				return num4;
			}
		}
		return 0;
	}

	private static int CompareCompletedTime(DateTime? x, DateTime? y)
	{
		if (x.HasValue && y.HasValue)
		{
			if (x > y)
			{
				return 1;
			}
			if (x < y)
			{
				return -1;
			}
		}
		return 0;
	}

	private static int CompareStatus(int x, int y)
	{
		if (x != y)
		{
			if (x == 0)
			{
				return -1;
			}
			return 1;
		}
		return 0;
	}

	private static int CompareSortOrder(long x, long y)
	{
		if (x > y)
		{
			return 1;
		}
		if (x < y)
		{
			return -1;
		}
		return 0;
	}
}
