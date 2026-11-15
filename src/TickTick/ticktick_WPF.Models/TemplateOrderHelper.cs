using System;
using System.Collections.Generic;
using System.Linq;

namespace ticktick_WPF.Models;

public static class TemplateOrderHelper
{
	public static int Compare(TaskTemplateModel first, TaskTemplateModel second)
	{
		return CompareInternal(first?.Order.HasValue ?? false, (first?.Order).GetValueOrDefault(), first?.CreatedTime ?? DateTime.MinValue, second?.Order.HasValue ?? false, (second?.Order).GetValueOrDefault(), second?.CreatedTime ?? DateTime.MinValue);
	}

	public static int Compare(TemplateViewModel first, TemplateViewModel second)
	{
		return CompareInternal(first?.HasOrder ?? false, first?.SortOrder ?? 0, first?.CreatedTime ?? DateTime.MinValue, second?.HasOrder ?? false, second?.SortOrder ?? 0, second?.CreatedTime ?? DateTime.MinValue);
	}

	public static int CountWithoutOrder(IEnumerable<TemplateViewModel> templates)
	{
		return templates?.Count((TemplateViewModel template) => !template.HasOrder) ?? 0;
	}

	public static void ResetOrders(IList<TemplateViewModel> templates)
	{
		if (templates != null)
		{
			for (int i = 0; i < templates.Count; i++)
			{
				templates[i].SortOrder = (long)i * 268435456L;
				templates[i].HasOrder = true;
			}
		}
	}

	public static long? CalculateInsertedOrder(IList<TemplateViewModel> templates, int newIndex)
	{
		if (templates == null || templates.Count <= 1)
		{
			return null;
		}
		TemplateViewModel templateViewModel = ((newIndex > 0) ? templates[newIndex - 1] : null);
		TemplateViewModel templateViewModel2 = ((newIndex < templates.Count - 1) ? templates[newIndex + 1] : null);
		long? num = ((templateViewModel != null && templateViewModel.HasOrder) ? new long?(templateViewModel.SortOrder) : ((long?)null));
		long? num2 = ((templateViewModel2 != null && templateViewModel2.HasOrder) ? new long?(templateViewModel2.SortOrder) : ((long?)null));
		if (!num.HasValue && !num2.HasValue)
		{
			return null;
		}
		if (!num.HasValue)
		{
			return (num2 ?? 268435456) - 268435456;
		}
		if (!num2.HasValue)
		{
			return num.Value + 268435456;
		}
		return num.Value + (num2.Value - num.Value) / 2;
	}

	private static int CompareInternal(bool firstHasOrder, long firstOrder, DateTime firstCreatedTime, bool secondHasOrder, long secondOrder, DateTime secondCreatedTime)
	{
		if (!firstHasOrder && !secondHasOrder)
		{
			return secondCreatedTime.CompareTo(firstCreatedTime);
		}
		if (!firstHasOrder)
		{
			return 1;
		}
		if (!secondHasOrder)
		{
			return -1;
		}
		return firstOrder.CompareTo(secondOrder);
	}
}
