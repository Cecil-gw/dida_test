using System.Collections.Generic;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.Provider;

internal class AttachmentComparer : IComparer<AttachmentViewModel>
{
	public int Compare(AttachmentViewModel left, AttachmentViewModel right)
	{
		if (left != null && right != null)
		{
			if (left.Ordinal > right.Ordinal)
			{
				return 1;
			}
			if (left.Ordinal < right.Ordinal)
			{
				return -1;
			}
			return CompareCreatedTime(left, right);
		}
		return 0;
	}

	private static int CompareCreatedTime(AttachmentViewModel left, AttachmentViewModel right)
	{
		if (Utils.IsEmptyDate(left.CreatedTime) && !Utils.IsEmptyDate(right.CreatedTime))
		{
			return 1;
		}
		if (!Utils.IsEmptyDate(left.CreatedTime) && Utils.IsEmptyDate(right.CreatedTime))
		{
			return -1;
		}
		if (!Utils.IsEmptyDate(left.CreatedTime) && !Utils.IsEmptyDate(right.CreatedTime) && left.CreatedTime.HasValue && right.CreatedTime.HasValue)
		{
			int num = left.CreatedTime.Value.CompareTo(right.CreatedTime.Value);
			if (num != 0)
			{
				return -num;
			}
		}
		return 0;
	}
}
