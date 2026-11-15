using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service.SortOrder;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Util.Sync.ClosedLoader;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Tag;
using ticktick_WPF.Views.Timeline;

namespace ticktick_WPF.Util.Sort;

public static class SortHelper
{
	public class TagCompare : IComparer<DisplayItemModel>
	{
		private readonly List<SyncSortOrderModel> _sortOrders;

		public TagCompare(List<SyncSortOrderModel> sortOrders)
		{
			_sortOrders = sortOrders.OrderBy((SyncSortOrderModel x) => x.SortOrder).ToList();
		}

		public int Compare(DisplayItemModel x, DisplayItemModel y)
		{
			if (x == null || y == null)
			{
				return 0;
			}
			return _sortOrders.FindIndex((SyncSortOrderModel o) => o.EntityId == x.Id).CompareTo(_sortOrders.FindIndex((SyncSortOrderModel o) => o.EntityId == y.Id));
		}
	}

	private class TitleComparer : PriorityDateComparer
	{
		public TitleComparer(bool inNote)
			: base(inNote)
		{
			InNoteSection = inNote;
			CheckSpecial = false;
		}

		public override int Compare(DisplayItemModel left, DisplayItemModel right)
		{
			if (left == right)
			{
				return 0;
			}
			if (left != null && right != null)
			{
				List<TitleChunk> titleNum = left.GetTitleNum();
				List<TitleChunk> titleNum2 = right.GetTitleNum();
				if (titleNum != null && titleNum.Count > 0 && titleNum2 != null && titleNum2.Count > 0)
				{
					int num = Utils.CompareChunks(titleNum, titleNum2);
					if (num != 0)
					{
						return num;
					}
				}
				else
				{
					if (string.IsNullOrEmpty(left.Title) && string.IsNullOrEmpty(right.Title))
					{
						return 0;
					}
					if (string.IsNullOrEmpty(left.Title))
					{
						return 1;
					}
					if (string.IsNullOrEmpty(right.Title))
					{
						return -1;
					}
					int num2 = string.Compare(left.Title, right.Title, StringComparison.CurrentCulture);
					if (num2 != 0)
					{
						return num2;
					}
				}
			}
			if (InNoteSection)
			{
				return CreatedTimeComparer.CompareCreatedTime(left, right);
			}
			return base.Compare(left, right);
		}
	}

	private class TitleComparerDesc : PriorityDateComparer
	{
		public TitleComparerDesc(bool inNote)
			: base(inNote)
		{
			InNoteSection = inNote;
			CheckSpecial = false;
		}

		public override int Compare(DisplayItemModel left, DisplayItemModel right)
		{
			if (left == right)
			{
				return 0;
			}
			if (left != null && right != null)
			{
				List<TitleChunk> titleNum = left.GetTitleNum();
				List<TitleChunk> titleNum2 = right.GetTitleNum();
				if (titleNum != null && titleNum.Count > 0 && titleNum2 != null && titleNum2.Count > 0)
				{
					int num = Utils.CompareChunksDesc(titleNum, titleNum2);
					if (num != 0)
					{
						return num;
					}
				}
				else
				{
					if (string.IsNullOrEmpty(left.Title) && string.IsNullOrEmpty(right.Title))
					{
						return 0;
					}
					if (string.IsNullOrEmpty(left.Title))
					{
						return 1;
					}
					if (string.IsNullOrEmpty(right.Title))
					{
						return -1;
					}
					int num2 = string.Compare(left.Title, right.Title, StringComparison.CurrentCulture);
					if (num2 != 0)
					{
						return num2;
					}
				}
			}
			if (InNoteSection)
			{
				return CreatedTimeComparer.CompareCreatedTime(left, right);
			}
			return base.Compare(left, right);
		}
	}

	private class SortByTagComparaer : IComparer<DisplayItemModel>
	{
		private List<string> _sortTags;

		private Dictionary<string, TagModel> _modelTagDict;

		public SortByTagComparaer(List<string> sortTags, Dictionary<string, TagModel> modelTagDict)
		{
			_sortTags = sortTags;
			_modelTagDict = modelTagDict;
		}

		public int Compare(DisplayItemModel left, DisplayItemModel right)
		{
			if (left == null || right == null)
			{
				return 0;
			}
			if (_modelTagDict.ContainsKey(left.Id) && _modelTagDict.ContainsKey(right.Id))
			{
				long num = _modelTagDict[left.Id]?.sortOrder ?? long.MaxValue;
				long num2 = _modelTagDict[right.Id]?.sortOrder ?? long.MaxValue;
				if (num != num2)
				{
					return num.CompareTo(num2);
				}
			}
			if (left.Tags != null && right.Tags != null)
			{
				foreach (string sortTag in _sortTags)
				{
					if (left.Tags.Contains(sortTag) && !right.Tags.Contains(sortTag))
					{
						return -1;
					}
					if (!left.Tags.Contains(sortTag) && right.Tags.Contains(sortTag))
					{
						return 1;
					}
				}
			}
			return 0;
		}
	}

	private class PinnedTaskComparer : IComparer<DisplayItemModel>
	{
		private List<SyncSortOrderModel> _sortOrder;

		public PinnedTaskComparer(List<SyncSortOrderModel> sortOrder)
		{
			_sortOrder = sortOrder;
		}

		public int Compare(DisplayItemModel x, DisplayItemModel y)
		{
			if (x == null || y == null)
			{
				return 0;
			}
			if (_sortOrder == null || !_sortOrder.Any())
			{
				return 0;
			}
			long num = _sortOrder.FirstOrDefault((SyncSortOrderModel s) => s.EntityId == x.Id)?.SortOrder ?? 0;
			long value = _sortOrder.FirstOrDefault((SyncSortOrderModel s) => s.EntityId == y.Id)?.SortOrder ?? 0;
			return num.CompareTo(value);
		}
	}

	private class TagComparer : DateComparer
	{
		private List<string> _sortTags;

		public TagComparer(List<string> sortTags, bool inNote)
			: base(inNote)
		{
			_sortTags = sortTags;
		}

		public override int Compare(DisplayItemModel left, DisplayItemModel right)
		{
			if (left == right)
			{
				return 0;
			}
			if (CheckSpecial && left != null && right != null)
			{
				int num = left.SpecialOrder.CompareTo(right.SpecialOrder);
				if (num != 0)
				{
					return num;
				}
			}
			int num3;
			if (left != null)
			{
				int? num2 = left.Tags?.Length;
				num3 = 0;
				if (num2 > num3 && (right?.Tags == null || right.Tags.Length == 0))
				{
					return -1;
				}
			}
			if (right != null)
			{
				int? num2 = right.Tags?.Length;
				num3 = 0;
				if (num2 > num3 && (left?.Tags == null || left.Tags.Length == 0))
				{
					return 1;
				}
			}
			if (left?.Tags != null && right?.Tags != null)
			{
				foreach (string sortTag in _sortTags)
				{
					if (left.Tags.Contains(sortTag) && !right.Tags.Contains(sortTag))
					{
						num3 = -1;
					}
					else
					{
						if (left.Tags.Contains(sortTag) || !right.Tags.Contains(sortTag))
						{
							continue;
						}
						num3 = 1;
					}
					goto IL_0188;
				}
			}
			if (InNoteSection)
			{
				return CreatedTimeComparer.CompareCreatedTime(left, right);
			}
			int num4 = base.Compare(left, right);
			if (num4 != 0)
			{
				return num4;
			}
			return 0;
			IL_0188:
			return num3;
		}
	}

	private class TagComparerDesc : DateComparer
	{
		private List<string> _sortTags;

		public TagComparerDesc(List<string> sortTags, bool inNote)
			: base(inNote)
		{
			_sortTags = sortTags;
		}

		public override int Compare(DisplayItemModel left, DisplayItemModel right)
		{
			if (left == right)
			{
				return 0;
			}
			if (CheckSpecial && left != null && right != null)
			{
				int num = left.SpecialOrder.CompareTo(right.SpecialOrder);
				if (num != 0)
				{
					return num;
				}
			}
			int num3;
			if (left != null)
			{
				int? num2 = left.Tags?.Length;
				num3 = 0;
				if (num2 > num3 && (right?.Tags == null || right.Tags.Length == 0))
				{
					return -1;
				}
			}
			if (right != null)
			{
				int? num2 = right.Tags?.Length;
				num3 = 0;
				if (num2 > num3 && (left?.Tags == null || left.Tags.Length == 0))
				{
					return 1;
				}
			}
			if (left?.Tags != null && right?.Tags != null)
			{
				foreach (string sortTag in _sortTags)
				{
					if (left.Tags.Contains(sortTag) && !right.Tags.Contains(sortTag))
					{
						num3 = 1;
					}
					else
					{
						if (left.Tags.Contains(sortTag) || !right.Tags.Contains(sortTag))
						{
							continue;
						}
						num3 = -1;
					}
					goto IL_0188;
				}
			}
			if (InNoteSection)
			{
				return CreatedTimeComparer.CompareCreatedTime(left, right);
			}
			int num4 = base.Compare(left, right);
			if (num4 != 0)
			{
				return num4;
			}
			return 0;
			IL_0188:
			return num3;
		}
	}

	private class OutDateComparaer : IComparer<DisplayItemModel>
	{
		public int Compare(DisplayItemModel left, DisplayItemModel right)
		{
			if (left != null && right != null)
			{
				if (!left.StartDate.HasValue && right.StartDate.HasValue)
				{
					return 1;
				}
				if (left.StartDate.HasValue && !right.StartDate.HasValue)
				{
					return -1;
				}
				if (left.StartDate.HasValue && right.StartDate.HasValue)
				{
					int num = CompareModelOutDate(left, right);
					if (num != 0)
					{
						return num;
					}
				}
				return string.Compare(left.Title, right.Title, StringComparison.Ordinal);
			}
			return 0;
		}
	}

	public class ProjectDateComparer : DateComparer
	{
		public ProjectDateComparer(bool inNote)
			: base(inNote)
		{
		}

		public override int Compare(DisplayItemModel left, DisplayItemModel right)
		{
			if (left == right)
			{
				return 0;
			}
			if (left != null && right != null)
			{
				if (CheckSpecial && left.SpecialOrder != right.SpecialOrder)
				{
					if (left.SpecialOrder > right.SpecialOrder)
					{
						return 1;
					}
					return -1;
				}
				int num = DateComparer.CompareDate(left, right);
				if (num != 0)
				{
					return num;
				}
			}
			return base.Compare(left, right);
		}
	}

	private class PriorityDateComparer : DateComparer
	{
		public PriorityDateComparer(bool inNote)
			: base(inNote)
		{
		}

		public override int Compare(DisplayItemModel left, DisplayItemModel right)
		{
			if (left == right)
			{
				return 0;
			}
			if (left != null && right != null)
			{
				if (CheckSpecial && left.SpecialOrder != right.SpecialOrder)
				{
					if (left.SpecialOrder > right.SpecialOrder)
					{
						return 1;
					}
					return -1;
				}
				int num = right.Priority.CompareTo(left.Priority);
				if (num != 0)
				{
					return num;
				}
				if (InNoteSection)
				{
					return CreatedTimeComparer.CompareCreatedTime(left, right);
				}
				int num2 = DateComparer.CompareDate(left, right);
				if (num2 != 0)
				{
					return num2;
				}
			}
			return base.Compare(left, right);
		}
	}

	private class PriorityDateComparerAsc : DateComparer
	{
		public PriorityDateComparerAsc(bool inNote)
			: base(inNote)
		{
		}

		public override int Compare(DisplayItemModel left, DisplayItemModel right)
		{
			if (left == right)
			{
				return 0;
			}
			if (left != null && right != null)
			{
				if (CheckSpecial && left.SpecialOrder != right.SpecialOrder)
				{
					if (left.SpecialOrder > right.SpecialOrder)
					{
						return 1;
					}
					return -1;
				}
				int num = left.Priority.CompareTo(right.Priority);
				if (num != 0)
				{
					return num;
				}
				if (InNoteSection)
				{
					return CreatedTimeComparer.CompareCreatedTime(left, right);
				}
				int num2 = DateComparer.CompareDate(left, right);
				if (num2 != 0)
				{
					return num2;
				}
			}
			return base.Compare(left, right);
		}
	}

	public class SortOrderComparer : IComparer<DisplayItemModel>
	{
		public virtual int Compare(DisplayItemModel left, DisplayItemModel right)
		{
			if (left == right)
			{
				return 0;
			}
			return CompareCustomSortOrder(left, right);
		}
	}

	private class AssigneeComparer : DateComparer
	{
		private Dictionary<string, int> _dict = new Dictionary<string, int>();

		public AssigneeComparer(string projectId, bool inNote)
			: base(inNote)
		{
			List<ShareUserModel> projectUsers = AvatarHelper.GetProjectUsers(projectId);
			if (projectUsers != null)
			{
				for (int i = 0; i < projectUsers.Count; i++)
				{
					_dict[projectUsers[i].userId.ToString()] = i;
				}
			}
		}

		public override int Compare(DisplayItemModel left, DisplayItemModel right)
		{
			if (left == right)
			{
				return 0;
			}
			if (left != null && right != null)
			{
				int num = ((!string.IsNullOrEmpty(left.Assignee) && _dict.ContainsKey(left.Assignee)) ? _dict[left.Assignee] : 100000);
				int num2 = ((!string.IsNullOrEmpty(right.Assignee) && _dict.ContainsKey(right.Assignee)) ? _dict[right.Assignee] : 100000);
				if (num != num2)
				{
					return num.CompareTo(num2);
				}
			}
			if (InNoteSection)
			{
				return CreatedTimeComparer.CompareCreatedTime(left, right);
			}
			return base.Compare(left, right);
		}
	}

	private class AssigneeComparerDesc : DateComparer
	{
		private Dictionary<string, int> _dict = new Dictionary<string, int>();

		public AssigneeComparerDesc(string projectId, bool inNote)
			: base(inNote)
		{
			List<ShareUserModel> projectUsers = AvatarHelper.GetProjectUsers(projectId);
			if (projectUsers != null)
			{
				for (int i = 0; i < projectUsers.Count; i++)
				{
					_dict[projectUsers[i].userId.ToString()] = i;
				}
			}
		}

		public override int Compare(DisplayItemModel left, DisplayItemModel right)
		{
			if (left == right)
			{
				return 0;
			}
			if (left != null && right != null)
			{
				bool flag = !string.IsNullOrEmpty(left.Assignee) && _dict.ContainsKey(left.Assignee);
				bool flag2 = !string.IsNullOrEmpty(right.Assignee) && _dict.ContainsKey(right.Assignee);
				if (flag && flag2)
				{
					return _dict[right.Assignee].CompareTo(_dict[left.Assignee]);
				}
				if (flag && !flag2)
				{
					return -1;
				}
				if (!flag && flag2)
				{
					return 1;
				}
			}
			if (InNoteSection)
			{
				return CreatedTimeComparer.CompareCreatedTime(left, right);
			}
			return base.Compare(left, right);
		}
	}

	public class DateComparer : IComparer<DisplayItemModel>
	{
		protected bool InNoteSection;

		protected bool CheckSpecial;

		public DateComparer(bool inNote, bool checkSpecial = true)
		{
			InNoteSection = inNote;
			CheckSpecial = checkSpecial;
		}

		public virtual int Compare(DisplayItemModel left, DisplayItemModel right)
		{
			return CompareDate(left, right, InNoteSection, CheckSpecial);
		}

		public static int CompareDate(DisplayItemModel left, DisplayItemModel right, bool InNoteSection = false, bool CheckSpecial = true)
		{
			if (left == right)
			{
				return 0;
			}
			if (left == null)
			{
				return 1;
			}
			if (right == null)
			{
				return -1;
			}
			if (left != null && right != null)
			{
				if (CheckSpecial)
				{
					int num = left.SpecialOrder.CompareTo(right.SpecialOrder);
					if (num != 0)
					{
						return num;
					}
				}
				if (!left.StartDate.HasValue && right.StartDate.HasValue)
				{
					return 1;
				}
				if (left.StartDate.HasValue && !right.StartDate.HasValue)
				{
					return -1;
				}
				if (left.StartDate.HasValue && right.StartDate.HasValue)
				{
					int num2 = CompareModelDate(left, right);
					if (num2 != 0)
					{
						return num2;
					}
				}
				if (InNoteSection)
				{
					return CreatedTimeComparer.CompareCreatedTime(left, right);
				}
				if (left.Priority != right.Priority)
				{
					return right.Priority.CompareTo(left.Priority);
				}
				int num3 = CompareProjectOrder(left, right);
				if (num3 != 0)
				{
					return num3;
				}
				return CompareCustomSortOrder(left, right);
			}
			return 0;
		}

		public void SetCheckSpecial(bool checkSpecial)
		{
			CheckSpecial = checkSpecial;
		}
	}

	public class DateComparerDesc : IComparer<DisplayItemModel>
	{
		protected bool InNoteSection;

		protected bool CheckSpecial;

		public DateComparerDesc(bool inNote, bool checkSpecial = true)
		{
			InNoteSection = inNote;
			CheckSpecial = checkSpecial;
		}

		public virtual int Compare(DisplayItemModel left, DisplayItemModel right)
		{
			if (left == right)
			{
				return 0;
			}
			if (left != null && right != null)
			{
				if (CheckSpecial)
				{
					int num = left.SpecialOrder.CompareTo(right.SpecialOrder);
					if (num != 0)
					{
						return num;
					}
				}
				if (!left.StartDate.HasValue && right.StartDate.HasValue)
				{
					return 1;
				}
				if (left.StartDate.HasValue && !right.StartDate.HasValue)
				{
					return -1;
				}
				if (left.StartDate.HasValue && right.StartDate.HasValue)
				{
					int num2 = CompareModelDate(left, right, descending: true);
					if (num2 != 0)
					{
						return num2;
					}
				}
				if (InNoteSection)
				{
					return CreatedTimeComparer.CompareCreatedTime(left, right);
				}
				if (left.Priority != right.Priority)
				{
					return right.Priority.CompareTo(left.Priority);
				}
				int num3 = CompareProjectOrder(left, right);
				if (num3 != 0)
				{
					return num3;
				}
				return CompareCustomSortOrder(left, right);
			}
			return 0;
		}

		public void SetCheckSpecial(bool checkSpecial)
		{
			CheckSpecial = checkSpecial;
		}
	}

	private class SpecialOrderComparer : IComparer<DisplayItemModel>
	{
		public int Compare(DisplayItemModel left, DisplayItemModel right)
		{
			if (left != null && right != null)
			{
				return left.SpecialOrder.CompareTo(right.SpecialOrder);
			}
			return 0;
		}
	}

	public class CreatedTimeComparer : IComparer<DisplayItemModel>
	{
		public int Compare(DisplayItemModel left, DisplayItemModel right)
		{
			return CompareCreatedTime(left, right);
		}

		public static int CompareCreatedTime(DisplayItemModel left, DisplayItemModel right)
		{
			if (left != null && right != null)
			{
				int typePriority = GetTypePriority(left);
				int typePriority2 = GetTypePriority(right);
				if (typePriority != typePriority2)
				{
					return typePriority.CompareTo(typePriority2);
				}
				if (typePriority > 0)
				{
					return DateComparer.CompareDate(left, right);
				}
				if (right.CreatedTime.CompareTo(left.CreatedTime) == 0)
				{
					return left.SortOrder.CompareTo(right.SortOrder);
				}
				return right.CreatedTime.CompareTo(left.CreatedTime);
			}
			return 0;
		}
	}

	private class ModifiedTimeComparer : IComparer<DisplayItemModel>
	{
		public int Compare(DisplayItemModel left, DisplayItemModel right)
		{
			if (left != null && right != null)
			{
				int typePriority = GetTypePriority(left);
				int typePriority2 = GetTypePriority(right);
				if (typePriority != typePriority2)
				{
					return typePriority.CompareTo(typePriority2);
				}
				if (typePriority > 0)
				{
					return DateComparer.CompareDate(left, right);
				}
				if (right.ModifiedTime.CompareTo(left.ModifiedTime) == 0)
				{
					return left.SortOrder.CompareTo(right.SortOrder);
				}
				return right.ModifiedTime.CompareTo(left.ModifiedTime);
			}
			return 0;
		}
	}

	private class CreatedTimeComparerAsc : IComparer<DisplayItemModel>
	{
		public int Compare(DisplayItemModel left, DisplayItemModel right)
		{
			if (left != null && right != null)
			{
				int typePriority = GetTypePriority(left);
				int typePriority2 = GetTypePriority(right);
				if (typePriority != typePriority2)
				{
					return typePriority.CompareTo(typePriority2);
				}
				if (typePriority > 0)
				{
					return DateComparer.CompareDate(left, right);
				}
				if (left.CreatedTime.CompareTo(right.CreatedTime) == 0)
				{
					return left.SortOrder.CompareTo(right.SortOrder);
				}
				return left.CreatedTime.CompareTo(right.CreatedTime);
			}
			return 0;
		}
	}

	private class ModifiedTimeComparerAsc : IComparer<DisplayItemModel>
	{
		public int Compare(DisplayItemModel left, DisplayItemModel right)
		{
			if (left != null && right != null)
			{
				int typePriority = GetTypePriority(left);
				int typePriority2 = GetTypePriority(right);
				if (typePriority != typePriority2)
				{
					return typePriority.CompareTo(typePriority2);
				}
				if (typePriority > 0)
				{
					return DateComparer.CompareDate(left, right);
				}
				if (left.ModifiedTime.CompareTo(right.ModifiedTime) == 0)
				{
					return left.SortOrder.CompareTo(right.SortOrder);
				}
				return left.ModifiedTime.CompareTo(right.ModifiedTime);
			}
			return 0;
		}
	}

	public class ReverseComparer<T> : IComparer<T>
	{
		private readonly IComparer<T> _comparer;

		public ReverseComparer(IComparer<T> comparer)
		{
			_comparer = comparer ?? throw new ArgumentNullException("comparer");
		}

		public int Compare(T x, T y)
		{
			return -_comparer.Compare(x, y);
		}
	}

	public class TimelineTagComparer : IComparer<TimelineCellViewModel>
	{
		private List<string> _sortTags;

		public TimelineTagComparer()
		{
			_sortTags = TagDataHelper.GetTags()?.Select((TagModel t) => t.name).ToList();
		}

		public int Compare(TimelineCellViewModel a, TimelineCellViewModel b)
		{
			if (a == null || b == null)
			{
				if (b != null)
				{
					return 1;
				}
				if (a != null)
				{
					return -1;
				}
				return 0;
			}
			if (a.StartDate.Date != b.StartDate.Date)
			{
				return a.StartDate.CompareTo(b.StartDate);
			}
			if (Math.Abs(b.Width - a.Width) > 0.01)
			{
				return b.Width.CompareTo(a.Width);
			}
			if (a.IsAllDay != b.IsAllDay)
			{
				return a.IsAllDay.CompareTo(b.IsAllDay);
			}
			int num = a.StartDate.TimeOfDay.CompareTo(b.StartDate.TimeOfDay);
			if (num == 0)
			{
				string[] tags = a.DisplayModel.Tags;
				if (tags != null && tags.Length != 0 && (b.DisplayModel.Tags == null || b.DisplayModel.Tags.Length == 0))
				{
					return -1;
				}
				string[] tags2 = b.DisplayModel.Tags;
				if (tags2 != null && tags2.Length != 0 && (a.DisplayModel.Tags == null || a.DisplayModel.Tags.Length == 0))
				{
					return 1;
				}
				if (a.DisplayModel.Tags != null && b.DisplayModel.Tags != null)
				{
					foreach (string sortTag in _sortTags)
					{
						if (a.DisplayModel.Tags.Contains(sortTag) && !b.DisplayModel.Tags.Contains(sortTag))
						{
							return -1;
						}
						if (!a.DisplayModel.Tags.Contains(sortTag) && b.DisplayModel.Tags.Contains(sortTag))
						{
							return 1;
						}
					}
				}
				if (a.DisplayModel.Priority != b.DisplayModel.Priority)
				{
					return b.DisplayModel.Priority.CompareTo(a.DisplayModel.Priority);
				}
				if (a.DisplayModel.Type != b.DisplayModel.Type && (a.DisplayModel.IsCheckItem || b.DisplayModel.IsCheckItem) && a.DisplayModel.CreatedTime.HasValue && b.DisplayModel.CreatedTime.HasValue)
				{
					return a.DisplayModel.CreatedTime.Value.CompareTo(b.DisplayModel.CreatedTime.Value);
				}
				if (a.DisplayModel.ProjectOrder != b.DisplayModel.ProjectOrder)
				{
					return a.DisplayModel.ProjectOrder.CompareTo(b.DisplayModel.ProjectOrder);
				}
				if (a.SortOrder != b.SortOrder)
				{
					return a.SortOrder.CompareTo(b.SortOrder);
				}
				return string.Compare(a.Id, b.Id, StringComparison.Ordinal);
			}
			return num;
		}
	}

	public class CourseStartDateComparer : IComparer<DisplayItemModel>
	{
		public virtual int Compare(DisplayItemModel a, DisplayItemModel b)
		{
			if (a == b)
			{
				return 0;
			}
			if (!a.StartDate.HasValue || !b.StartDate.HasValue)
			{
				if (a.StartDate.HasValue)
				{
					return -1;
				}
				return 1;
			}
			return a.StartDate.Value.CompareTo(b.StartDate.Value);
		}
	}

	public class CourseStartDateComparerDesc : IComparer<DisplayItemModel>
	{
		public virtual int Compare(DisplayItemModel a, DisplayItemModel b)
		{
			if (a == b)
			{
				return 0;
			}
			if (!a.StartDate.HasValue || !b.StartDate.HasValue)
			{
				if (a.StartDate.HasValue)
				{
					return -1;
				}
				return 1;
			}
			return b.StartDate.Value.CompareTo(a.StartDate.Value);
		}
	}

	public static async Task<ObservableCollection<DisplayItemModel>> Sort(ObservableCollection<DisplayItemModel> models, ProjectIdentity identity, bool showFold = true, bool showLoadMore = false, List<ColumnModel> columns = null, ClosedLoader closedLoader = null, bool? showComplete = null, bool buildByChildIds = false)
	{
		List<SectionStatusModel> sectionStatusModels = (showFold ? (from sectionStatusModel in CacheManager.GetSectionStatus()
			where sectionStatusModel.Identity == identity.Id
			select sectionStatusModel).ToList() : new List<SectionStatusModel>());
		ProjectSortExtra extra = ProjectSortExtra.Build(identity);
		extra.BuildByChildIds = buildByChildIds;
		if (showComplete.HasValue)
		{
			extra.ShowCompleted = showComplete.Value;
		}
		SortOption sortOption = extra.SortOption;
		extra.ShowFold = showFold;
		List<SyncSortOrderModel> sortOrderPinned = await TaskSortOrderService.GetAsyncList("taskPinned", identity.CatId);
		string tagParent = null;
		if (identity is TagProjectIdentity tagProjectIdentity)
		{
			tagParent = tagProjectIdentity.Tag;
		}
		HashSet<string> openedTaskIds = null;
		ProjectIdentity projectIdentity = ((identity is ColumnProjectIdentity columnProjectIdentity) ? columnProjectIdentity.Project : identity);
		if (!(projectIdentity is NormalProjectIdentity) && !(projectIdentity is GroupProjectIdentity) && !(projectIdentity is MatrixQuadrantIdentity))
		{
			openedTaskIds = SmartListTaskFoldHelper.GetOpenedTaskIds(projectIdentity.CatId);
		}
		CountdownSection countdowns = new CountdownSection();
		foreach (DisplayItemModel model2 in models)
		{
			if (model2.IsCountdown)
			{
				countdowns.Children.Add(model2);
			}
		}
		ObservableCollection<DisplayItemModel> results = new ObservableCollection<DisplayItemModel>();
		if (countdowns.Children.Count > 0)
		{
			AssembleSection(countdowns, null, sectionStatusModels, results, null, compareChild: true, null, !extra.InKanban);
		}
		List<DisplayItemModel> uncompleted = models.Where((DisplayItemModel p) => !p.IsCountdown && p.Status == 0).ToList();
		if (uncompleted.Any() || sortOption.groupByType == Constants.SortType.sortOrder)
		{
			if (!sortOption.IsNone())
			{
				List<DisplayItemModel> pinned = uncompleted.Where((DisplayItemModel displayItemModel) => displayItemModel.IsPinned).ToList();
				if (pinned.Count > 0)
				{
					await AssembleStarredSection(pinned, sectionStatusModels, results, openedTaskIds, sortOrderPinned, extra.BuildByChildIds);
				}
				bool existStarred = pinned.Count > 0 || countdowns.Children.Count > 0;
				uncompleted.RemoveAll((DisplayItemModel displayItemModel) => displayItemModel.IsPinned);
				if (extra.InKanban && !string.IsNullOrEmpty(extra.SortKey))
				{
					List<SyncSortOrderModel> list = await TaskSortOrderService.GetAsyncList(extra.SortKey, identity.GetSortProjectId());
					long num = ((TaskDefaultDao.GetDefaultSafely().AddTo == 0) ? long.MinValue : long.MaxValue);
					foreach (DisplayItemModel model in uncompleted)
					{
						long? num2 = list?.FirstOrDefault((SyncSortOrderModel o) => o.EntityId == model.EntityId)?.SortOrder;
						model.SpecialOrder = num2 ?? num;
						if (num2.HasValue)
						{
							extra.ExistSpecialOrderItem = true;
						}
					}
				}
				if (existStarred && extra.InKanban && extra.SortOption.groupByType == Constants.SortType.none)
				{
					extra.SortOption.groupBy = extra.SortOption.orderBy;
				}
				IComparer<DisplayItemModel> compareByOrderBy = GetCompareByOrderBy(sortOption.orderBy, sortOption.orderDirection, extra.ProjectId, extra.InNoteColumn);
				switch (sortOption.groupBy)
				{
				case "none":
				case "title":
				case "modifiedTime":
					await GroupAsNone(uncompleted, sectionStatusModels, results, openedTaskIds, existStarred, compareByOrderBy, extra);
					break;
				case "createdTime":
					await GroupAsCreateTime(uncompleted, sectionStatusModels, results, openedTaskIds, compareByOrderBy, extra);
					break;
				case "sortOrder":
					await GroupAsSortOrder(uncompleted, results, columns, sectionStatusModels, openedTaskIds, existStarred, compareByOrderBy, extra);
					break;
				case "project":
					await GroupAsProject(uncompleted, sectionStatusModels, results, openedTaskIds, compareByOrderBy, extra);
					break;
				case "dueDate":
				{
					IComparer<DisplayItemModel> compareByOrderBy3 = GetCompareByOrderBy(sortOption.orderBy, sortOption.orderDirection, extra.ProjectId, isNote: true);
					await GroupAsDate(uncompleted, sectionStatusModels, results, openedTaskIds, compareByOrderBy, compareByOrderBy3, extra);
					break;
				}
				case "priority":
				{
					IComparer<DisplayItemModel> compareByOrderBy2 = GetCompareByOrderBy(sortOption.orderBy, sortOption.orderDirection, extra.ProjectId, isNote: true);
					await GroupAsPriority(uncompleted, sectionStatusModels, results, openedTaskIds, compareByOrderBy, compareByOrderBy2, extra);
					break;
				}
				case "assignee":
					await GroupAsAssignee(uncompleted, sectionStatusModels, results, openedTaskIds, compareByOrderBy, extra);
					break;
				case "tag":
					await GroupAsTag(uncompleted, sectionStatusModels, results, tagParent, openedTaskIds, compareByOrderBy, extra);
					break;
				default:
					SortAsNone(uncompleted, results, "none", extra);
					break;
				}
			}
			else
			{
				SortAsNone(uncompleted, results, "none", extra);
			}
		}
		if (extra.IsTrash)
		{
			List<DisplayItemModel> list2 = models.ToList();
			list2.Sort((DisplayItemModel a, DisplayItemModel b) => (b.ModifiedTime.CompareTo(a.ModifiedTime) == 0) ? string.Compare(a.Title, b.Title, CultureInfo.CurrentCulture, CompareOptions.None) : b.ModifiedTime.CompareTo(a.ModifiedTime));
			Dictionary<string, Node<DisplayItemModel>> nodeDict = GetNodeDict(list2);
			BuildDisplayNodeTree(nodeDict, extra);
			ObservableCollection<DisplayItemModel> observableCollection = new ObservableCollection<DisplayItemModel>();
			foreach (Node<DisplayItemModel> value in nodeDict.Values)
			{
				if (value.HasParent)
				{
					continue;
				}
				if (openedTaskIds != null)
				{
					value.Value.IsOpen = openedTaskIds.Contains(value.Value.Id);
				}
				observableCollection.Add(value.Value);
				List<DisplayItemModel> allChildrenValue = value.GetAllChildrenValue();
				if (!value.Value.IsOpen)
				{
					continue;
				}
				foreach (DisplayItemModel item3 in allChildrenValue)
				{
					if (openedTaskIds != null)
					{
						item3.IsOpen = openedTaskIds.Contains(item3.Id);
					}
					observableCollection.Add(item3);
				}
			}
			DisplayItemModel item = DisplayItemModel.BuildLoadMore();
			if (showLoadMore && observableCollection.Count >= 50)
			{
				observableCollection.Add(item);
			}
			return observableCollection;
		}
		if (extra.IsSearch || extra.ShowCompleted || extra.IsClosed)
		{
			IEnumerable<DisplayItemModel> enumerable = models.Where((DisplayItemModel p) => p.Status != 0);
			ObservableCollection<DisplayItemModel> observableCollection2 = (sortOption.IsNone() ? new ObservableCollection<DisplayItemModel>(enumerable) : new ObservableCollection<DisplayItemModel>(enumerable.OrderByDescending((DisplayItemModel p) => p.CompletedTime)));
			if (observableCollection2.Any())
			{
				if (extra.IsClosed)
				{
					if (extra.IsCompleted)
					{
						showLoadMore = ClosedTaskWithFilterLoader.CompletionLoader.NeedShowLoadMore(observableCollection2.Count);
					}
					if (extra.IsAbandoned)
					{
						showLoadMore = ClosedTaskWithFilterLoader.AbandonedLoader.NeedShowLoadMore(observableCollection2.Count);
					}
					foreach (Section item4 in GetCompletedSection(observableCollection2))
					{
						AssembleSection(item4, new SpecialOrderComparer(), sectionStatusModels, results);
					}
					DisplayItemModel item2 = DisplayItemModel.BuildLoadMore();
					if (showLoadMore)
					{
						results.Add(item2);
					}
				}
				else
				{
					AppendCompletedSection(observableCollection2, sectionStatusModels, results, showLoadMore, showCompletedSection: true, extra.LoadAll, closedLoader, forceShowLoadMore: false, openedTaskIds, extra.BuildByChildIds);
				}
			}
		}
		if (!extra.Editable)
		{
			foreach (DisplayItemModel item5 in results)
			{
				item5.SourceViewModel.Editable = false;
			}
		}
		return results;
	}

	private static IEnumerable<Section> GetCompletedSection(IEnumerable<DisplayItemModel> models)
	{
		List<Section> list = new List<Section>();
		List<DateTime> list2 = new List<DateTime>();
		List<DisplayItemModel> list3 = models.ToList();
		list3.Sort((DisplayItemModel a, DisplayItemModel b) => (b.CompletedTime.HasValue && a.CompletedTime.HasValue) ? b.CompletedTime.Value.CompareTo(a.CompletedTime.Value) : 0);
		int num = 0;
		foreach (DisplayItemModel item in list3)
		{
			item.SpecialOrder = num;
			if (item.CompletedTime.HasValue)
			{
				DateTime date = item.CompletedTime.Value.Date;
				if (!list2.Contains(date))
				{
					list2.Add(date);
					Section section = new Section(DateUtils.FormatTimeDesc(date, isAllDay: true) + date.ToString(" ddd"), sectionId: DateUtils.FormatTimeDesc(date, isAllDay: true), oridinal: num++, sectionDate: date);
					section.Children.Add(item);
					list.Add(section);
				}
				else
				{
					int index = list2.IndexOf(date);
					list[index].Children.Add(item);
				}
			}
		}
		return list;
	}

	private static async Task GroupAsTag(IReadOnlyCollection<DisplayItemModel> models, List<SectionStatusModel> sectionStatusModels, ObservableCollection<DisplayItemModel> results, string tagParent, HashSet<string> openedTaskIds, IComparer<DisplayItemModel> comparer, ProjectSortExtra extra)
	{
		if (models == null || models.Count <= 0)
		{
			return;
		}
		List<TagModel> tags = TagDataHelper.GetTags();
		Dictionary<string, TagSection> dictionary = new Dictionary<string, TagSection>();
		HabitSection habits = new HabitSection(extra.IsWeek);
		Dictionary<string, TagModel> dictionary2 = new Dictionary<string, TagModel>();
		Dictionary<string, long> tagSortDict = TagDataHelper.GetTagSortDict();
		foreach (DisplayItemModel model in models)
		{
			if (model.Habit != null)
			{
				continue;
			}
			if (model.Tags != null && model.Tags.Length != 0)
			{
				List<string> limits = null;
				if (!string.IsNullOrEmpty(tagParent))
				{
					limits = (from t in tags
						where t.parent == tagParent || t.name == tagParent
						select t.name).ToList();
				}
				string primaryTag = TagDataHelper.GetPrimaryTag(tagSortDict, model.Tags.ToList(), limits);
				TagModel value = tags.FirstOrDefault((TagModel t) => t.name == primaryTag);
				dictionary2[model.Id] = value;
			}
			else
			{
				dictionary2[model.Id] = null;
			}
		}
		Dictionary<string, Node<DisplayItemModel>> nodeDict = GetNodeDict(models.ToList());
		BuildDisplayNodeTree(nodeDict, extra);
		foreach (Node<DisplayItemModel> item in nodeDict.Values.Where((Node<DisplayItemModel> node) => !node.HasParent))
		{
			item.GetAllChildrenValue();
			DisplayItemModel value2 = item.Value;
			if (value2.Habit != null)
			{
				habits.Children.Add(value2);
				continue;
			}
			if (openedTaskIds != null)
			{
				value2.IsOpen = openedTaskIds.Contains(value2.Id);
			}
			TagModel tagModel = (dictionary2.ContainsKey(value2.Id) ? dictionary2[value2.Id] : null);
			if (tagModel != null && value2.Tags != null && value2.Tags.Length != 0)
			{
				if (!dictionary.ContainsKey(tagModel.name))
				{
					TagSection value3 = new TagSection(tagModel.GetDisplayName())
					{
						Ordinal = tagModel.parentSortOrder,
						Ordinal2 = tagModel.sortOrder,
						SectionId = tagModel.name,
						SectionEntityId = tagModel.name
					};
					dictionary.Add(tagModel.name, value3);
					dictionary[tagModel.name].Children.Add(value2);
				}
				else
				{
					dictionary[tagModel.name].Children.Add(value2);
				}
			}
			else
			{
				TryAddNoTagSection(dictionary, value2, item);
			}
		}
		IEnumerable<TagSection> enumerable = ((extra.InKanban && extra.SortOption.orderDirection != Constants.OrderDirection.Asc) ? (from x in dictionary.Values.ToList()
			orderby x.SectionEntityId == "noTag"
			select x).ThenByDescending((TagSection p) => p.Ordinal).ThenByDescending((TagSection p) => p.Ordinal2) : (from p in dictionary.Values.ToList()
			orderby p.Ordinal, p.Ordinal2
			select p));
		foreach (TagSection section in enumerable)
		{
			if (!extra.InKanban)
			{
				await SetItemSpecialOrder(section, extra.SortKey, extra.SortCatId, extra.DefaultOrder);
			}
			AssembleSection(section, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra);
		}
		AssembleSection(habits, null, sectionStatusModels, results);
	}

	private static async Task SortByOrders(IList<DisplayItemModel> models, List<SyncSortOrderModel> orders, IEqualityComparer<SyncSortOrderModel> comparer = null)
	{
		comparer = comparer ?? new DefaultSortOrderModelComparer();
		List<DisplayItemModel> sortedModels = new List<DisplayItemModel>();
		if (orders != null && orders.Any())
		{
			orders.Sort((SyncSortOrderModel a, SyncSortOrderModel b) => a.SortOrder.CompareTo(b.SortOrder));
			foreach (SyncSortOrderModel sortModel in orders.Distinct(comparer))
			{
				DisplayItemModel displayItemModel = models.FirstOrDefault((DisplayItemModel item) => item.Id == sortModel.EntityId);
				if (displayItemModel != null)
				{
					sortedModels.Add(displayItemModel);
					models.Remove(displayItemModel);
					displayItemModel.SpecialOrder = sortModel.SortOrder;
				}
			}
			if (models.Count > 0 && sortedModels.Count > 0)
			{
				SyncSortOrderModel minOrder = orders[0];
				long num = minOrder.SortOrder;
				List<SyncSortOrderModel> newOrders = new List<SyncSortOrderModel>();
				for (int num2 = models.Count - 1; num2 >= 0; num2--)
				{
					num -= 268435456;
					DisplayItemModel displayItemModel2 = models[num2];
					displayItemModel2.SpecialOrder = num;
					sortedModels.Insert(0, displayItemModel2);
					SyncSortOrderModel syncSortOrderModel = new SyncSortOrderModel();
					SyncSortOrderModel.Copy(syncSortOrderModel, minOrder);
					syncSortOrderModel.SyncStatus = 1;
					syncSortOrderModel.EntityId = displayItemModel2.Id;
					syncSortOrderModel.SortOrder = displayItemModel2.SpecialOrder;
					newOrders.Add(syncSortOrderModel);
				}
				models.Clear();
				await SyncSortOrderDao.InsertAllAsync(newOrders);
				TaskSortOrderService.AddSortModels(minOrder.SortOrderType, minOrder.GroupId, newOrders);
				SyncManager.TryDelaySync();
			}
		}
		sortedModels.ForEach(delegate(DisplayItemModel item)
		{
			models.Add(item);
		});
	}

	private static async Task AssembleStarredSection(List<DisplayItemModel> starred, List<SectionStatusModel> sectionStatusModels, ObservableCollection<DisplayItemModel> results, HashSet<string> openedTaskIds = null, List<SyncSortOrderModel> sortOrderPinned = null, bool buildByChildIds = false)
	{
		PinnedSection starredSection = new PinnedSection();
		starred.Sort((DisplayItemModel a, DisplayItemModel b) => a.SortOrder.CompareTo(b.SortOrder));
		starred.Sort((DisplayItemModel a, DisplayItemModel b) => b.PinnedTime.CompareTo(a.PinnedTime));
		await SortByOrders(starred, sortOrderPinned, new TaskPinnedSortOrderModelComparer());
		List<Node<DisplayItemModel>> sortedTaskNodeTree = TaskNodeUtils.GetSortedTaskNodeTree(starred, buildByChildIds);
		if (sortedTaskNodeTree != null && sortedTaskNodeTree.Any())
		{
			foreach (Node<DisplayItemModel> item in sortedTaskNodeTree)
			{
				item.GetAllChildrenValue();
				starredSection.Children.Add(item.Value);
			}
		}
		if (starredSection.Children.Count > 0)
		{
			AssembleSection(starredSection, null, sectionStatusModels, results, openedTaskIds, compareChild: false);
		}
	}

	private static void TryAddNoTagSection(Dictionary<string, TagSection> tagSections, DisplayItemModel model, Node<DisplayItemModel> node)
	{
		string text = Utils.GetString("NoTags");
		if (!tagSections.ContainsKey(text))
		{
			tagSections.Add(text, new TagSection(text)
			{
				Ordinal = 9223372036854775806L,
				SectionId = "notag",
				SectionEntityId = "noTag"
			});
		}
		tagSections[text].Children.Add(model);
	}

	private static async Task GroupAsAssignee(IReadOnlyCollection<DisplayItemModel> models, List<SectionStatusModel> sectionStatusModels, ObservableCollection<DisplayItemModel> results, HashSet<string> openedTaskIds, IComparer<DisplayItemModel> comparer, ProjectSortExtra extra)
	{
		if (models == null || models.Count <= 0)
		{
			return;
		}
		Dictionary<string, Node<DisplayItemModel>> nodeDict = GetNodeDict(models.ToList());
		BuildDisplayNodeTree(nodeDict, extra);
		Dictionary<string, AssigneeSection> dictionary = new Dictionary<string, AssigneeSection>();
		List<Node<DisplayItemModel>> list = nodeDict.Values.Where((Node<DisplayItemModel> n) => !n.HasParent).ToList();
		List<AvatarViewModel> list2 = (extra.InGroup ? AvatarHelper.GetProjectAvatarsFromCacheInGroup(extra.SortCatId) : AvatarHelper.GetProjectAvatarsFromCache(extra.SortCatId, sort: true));
		for (int num = 0; num < list.Count; num++)
		{
			list[num].GetAllChildrenValue();
			DisplayItemModel value = list[num].Value;
			if (openedTaskIds != null)
			{
				value.IsOpen = openedTaskIds.Contains(value.Id);
			}
			string userId = value.Assignee;
			if (string.IsNullOrEmpty(value.Assignee) || value.Assignee == "-1")
			{
				userId = "-1";
			}
			if (!dictionary.ContainsKey(userId))
			{
				string projectId = string.Empty;
				if (num + 1 < list.Count)
				{
					projectId = list[num + 1].Value.ProjectId;
				}
				AvatarViewModel avatarViewModel = ((userId == "-1") ? null : list2.FirstOrDefault((AvatarViewModel u) => (u.UserId ?? "") == userId));
				dictionary.Add(userId, (userId == "-1") ? new NoAssingeeSection() : new AssigneeSection
				{
					SectionId = userId,
					Name = avatarViewModel?.Name,
					Assingee = userId,
					Ordinal = ((avatarViewModel?.UserId == LocalSettings.Settings.LoginUserId) ? long.MinValue : list2.IndexOf(avatarViewModel)),
					ProjectId = projectId,
					SectionEntityId = userId
				});
			}
			dictionary[userId].Children.Add(value);
		}
		IEnumerable<AssigneeSection> enumerable = ((extra.InKanban && extra.SortOption.orderDirection != Constants.OrderDirection.Asc) ? (from p in dictionary.Values.ToList()
			orderby p.Ordinal descending
			select p) : (from p in dictionary.Values.ToList()
			orderby p.Ordinal
			select p));
		foreach (AssigneeSection section in enumerable)
		{
			if (!extra.InKanban)
			{
				await SetItemSpecialOrder(section, extra.SortKey, extra.SortCatId, extra.DefaultOrder);
			}
			AssembleSection(section, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra);
		}
	}

	private static async Task GroupAsPriority(IReadOnlyCollection<DisplayItemModel> models, List<SectionStatusModel> sectionStatusModels, ObservableCollection<DisplayItemModel> results, HashSet<string> openedTaskIds, IComparer<DisplayItemModel> comparer, IComparer<DisplayItemModel> noteComparer, ProjectSortExtra extra)
	{
		if (models == null || models.Count <= 0)
		{
			return;
		}
		List<Node<DisplayItemModel>> sortedTaskNodeTree = TaskNodeUtils.GetSortedTaskNodeTree(models, extra.BuildByChildIds);
		HighPrioritySection high = new HighPrioritySection();
		MediumPrioritySection medium = new MediumPrioritySection();
		LowPrioritySection low = new LowPrioritySection();
		NoPrioritySection none = new NoPrioritySection();
		HabitSection habits = new HabitSection(extra.IsWeek);
		NoteSection notes = new NoteSection();
		foreach (Node<DisplayItemModel> item in sortedTaskNodeTree)
		{
			DisplayItemModel value = item.Value;
			item.GetAllChildrenValue();
			if (value.IsNote && value.Priority != 0)
			{
				value.SourceViewModel.Priority = 0;
			}
			if (value.Habit != null)
			{
				habits.Children.Add(value);
				continue;
			}
			if (value.IsNote)
			{
				notes.Children.Add(value);
				continue;
			}
			if (openedTaskIds != null)
			{
				value.IsOpen = openedTaskIds.Contains(value.Id);
			}
			switch (value.Priority)
			{
			case 5:
				high.Children.Add(value);
				break;
			case 3:
				medium.Children.Add(value);
				break;
			case 1:
				low.Children.Add(value);
				break;
			default:
				none.Children.Add(value);
				break;
			}
		}
		if (!extra.InKanban)
		{
			await Task.WhenAll(SetItemSpecialOrder(high, extra.SortKey, extra.SortCatId, extra.DefaultOrder), SetItemSpecialOrder(medium, extra.SortKey, extra.SortCatId, extra.DefaultOrder), SetItemSpecialOrder(low, extra.SortKey, extra.SortCatId, extra.DefaultOrder), SetItemSpecialOrder(none, extra.SortKey, extra.SortCatId, extra.DefaultOrder));
		}
		if (!extra.InKanban || (extra.InKanban && extra.SortOption.orderDirection != Constants.OrderDirection.Asc))
		{
			AssembleSection(high, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra);
			AssembleSection(medium, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra);
			AssembleSection(low, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra);
			AssembleSection(none, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra);
		}
		else
		{
			AssembleSection(none, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra);
			AssembleSection(low, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra);
			AssembleSection(medium, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra);
			AssembleSection(high, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra);
		}
		AssembleSection(notes, noteComparer, sectionStatusModels, results);
		AssembleSection(habits, null, sectionStatusModels, results);
	}

	private static async Task SetItemSpecialOrder(Section section, string key, string catId, long def)
	{
		List<DisplayItemModel> children = section.Children;
		if (children == null || children.Count <= 0 || string.IsNullOrEmpty(key) || string.IsNullOrEmpty(section.SectionEntityId))
		{
			return;
		}
		List<SyncSortOrderModel> list = await TaskSortOrderService.GetAsyncList(string.Format(key, section.SectionEntityId), catId);
		foreach (DisplayItemModel model in section.Children)
		{
			long? obj = list?.FirstOrDefault((SyncSortOrderModel o) => o.EntityId == model.EntityId)?.SortOrder;
			model.SpecialOrder = obj ?? def;
			List<DisplayItemModel> childrenModels = model.GetChildrenModels(getAll: true);
			if (childrenModels == null || childrenModels.Count <= 0)
			{
				continue;
			}
			foreach (DisplayItemModel child in childrenModels)
			{
				long? obj2 = list?.FirstOrDefault((SyncSortOrderModel o) => o.EntityId == child.EntityId)?.SortOrder;
				child.SpecialOrder = obj2 ?? def;
			}
		}
	}

	private static async Task GroupAsNone(IReadOnlyCollection<DisplayItemModel> models, List<SectionStatusModel> sectionStatusModels, ObservableCollection<DisplayItemModel> results, HashSet<string> openedTaskIds, bool existStarred, IComparer<DisplayItemModel> comparer, ProjectSortExtra extra)
	{
		if (!extra.InKanban && extra.SortKey != null)
		{
			List<SyncSortOrderModel> list = await TaskSortOrderService.GetAsyncList(string.Format(extra.SortKey, "none"), extra.SortCatId);
			long num = ((TaskDefaultDao.GetDefaultSafely().AddTo == 0) ? long.MinValue : long.MaxValue);
			foreach (DisplayItemModel model in models)
			{
				long? obj = list?.FirstOrDefault((SyncSortOrderModel o) => o.EntityId == model.EntityId)?.SortOrder;
				model.SpecialOrder = obj ?? num;
			}
		}
		HabitSection habitSection = new HabitSection(extra.IsWeek);
		List<DisplayItemModel> list2 = (from m in TaskNodeUtils.GetModelsFromNodes(TaskNodeUtils.GetSortedTaskNodeTree(models, extra.BuildByChildIds))
			where m.Level == 0
			select m).ToList();
		UnpinnedSection unpinnedSection = new UnpinnedSection
		{
			SectionEntityId = "none"
		};
		foreach (DisplayItemModel item in list2)
		{
			if (item.Habit != null)
			{
				habitSection.Children.Add(item);
				continue;
			}
			if (openedTaskIds != null)
			{
				item.IsOpen = openedTaskIds.Contains(item.Id);
			}
			unpinnedSection.Children.Add(item);
		}
		if (unpinnedSection.Children.Count > 0)
		{
			AssembleSection(unpinnedSection, comparer, sectionStatusModels, results, null, compareChild: true, extra, existStarred);
		}
		AssembleSection(habitSection, null, sectionStatusModels, results, null, compareChild: true, null, !extra.InKanban);
	}

	private static async Task GroupAsDate(IReadOnlyCollection<DisplayItemModel> models, List<SectionStatusModel> sectionStatusModels, ObservableCollection<DisplayItemModel> results, HashSet<string> openedTaskIds, IComparer<DisplayItemModel> comparer, IComparer<DisplayItemModel> noteComparer, ProjectSortExtra extra)
	{
		if (extra.IsToday)
		{
			await SortAsDateInToday(models, sectionStatusModels, results, openedTaskIds, comparer, noteComparer, extra);
		}
		else if (extra.IsTomorrow)
		{
			await SortAsDateInTomorrow(models, sectionStatusModels, results, openedTaskIds, comparer, noteComparer, extra);
		}
		else if (extra.IsWeek)
		{
			await SortAsDateInWeek(models, sectionStatusModels, results, openedTaskIds, comparer, noteComparer, extra);
		}
		else
		{
			await SortAsDateNormal(models, sectionStatusModels, results, openedTaskIds, comparer, noteComparer, extra);
		}
	}

	private static async Task SortAsDateNormal(IEnumerable<DisplayItemModel> models, List<SectionStatusModel> sectionStatusModels, ObservableCollection<DisplayItemModel> results, HashSet<string> openedTaskIds, IComparer<DisplayItemModel> comparer, IComparer<DisplayItemModel> noteComparer, ProjectSortExtra extra)
	{
		DateSection section = new DateSection();
		Dictionary<string, Node<DisplayItemModel>> nodeDict = GetNodeDict(models.ToList());
		NoteSection notes = new NoteSection();
		BuildDisplayNodeTree(nodeDict, extra);
		foreach (Node<DisplayItemModel> item in nodeDict.Values.Where((Node<DisplayItemModel> n) => !n.HasParent))
		{
			DisplayItemModel value = item.Value;
			item.GetAllChildrenValue();
			if (openedTaskIds != null)
			{
				value.IsOpen = openedTaskIds.Contains(value.Id);
			}
			if (value.IsNote)
			{
				notes.Children.Add(value);
				continue;
			}
			switch (DateUtils.GetSectionCategory(value.StartDate, value.DueDate, value.IsAllDay))
			{
			case DateUtils.DateSectionCategory.NoDate:
				section.Nodate.Children.Add(value);
				break;
			case DateUtils.DateSectionCategory.OutDated:
				section.Outdated.Children.Add(value);
				break;
			case DateUtils.DateSectionCategory.Today:
				section.Today.Children.Add(value);
				break;
			case DateUtils.DateSectionCategory.Tomorrow:
				section.Tomorrow.Children.Add(value);
				break;
			case DateUtils.DateSectionCategory.ThisWeek:
				section.Week.Children.Add(value);
				break;
			case DateUtils.DateSectionCategory.Future:
				section.Later.Children.Add(value);
				break;
			default:
				section.Nodate.Children.Add(value);
				break;
			}
		}
		if (section.Outdated is OutdatedSection outdatedSection)
		{
			outdatedSection.ShowPostpone = extra.IsAll;
		}
		if (LocalSettings.Settings.PosOfOverdue == 0)
		{
			DateComparer obj = comparer as DateComparer;
			obj?.SetCheckSpecial(checkSpecial: false);
			AssembleSection(section.Outdated, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra.ExistSpecialOrderItem ? extra : null);
			obj?.SetCheckSpecial(checkSpecial: true);
		}
		if (!extra.InKanban)
		{
			await Task.WhenAll(SetItemSpecialOrder(section.Today, extra.SortKey, extra.SortCatId, extra.DefaultOrder), SetItemSpecialOrder(section.Tomorrow, extra.SortKey, extra.SortCatId, extra.DefaultOrder), SetItemSpecialOrder(section.Nodate, extra.SortKey, extra.SortCatId, extra.DefaultOrder));
		}
		if (!extra.InKanban || extra.SortOption.orderDirection == Constants.OrderDirection.Asc)
		{
			AssembleSection(section.Today, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra);
			AssembleSection(section.Tomorrow, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra);
			AssembleSection(section.Week, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra.ExistSpecialOrderItem ? extra : null);
			AssembleSection(section.Later, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra.ExistSpecialOrderItem ? extra : null);
		}
		else
		{
			AssembleSection(section.Later, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra.ExistSpecialOrderItem ? extra : null);
			AssembleSection(section.Week, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra.ExistSpecialOrderItem ? extra : null);
			AssembleSection(section.Tomorrow, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra);
			AssembleSection(section.Today, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra);
		}
		AssembleSection(section.Nodate, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra);
		if (LocalSettings.Settings.PosOfOverdue == 1)
		{
			DateComparer obj2 = comparer as DateComparer;
			obj2?.SetCheckSpecial(checkSpecial: false);
			AssembleSection(section.Outdated, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra.ExistSpecialOrderItem ? extra : null);
			obj2?.SetCheckSpecial(checkSpecial: true);
		}
		AssembleSection(notes, noteComparer, sectionStatusModels, results, openedTaskIds);
	}

	private static async Task SortAsDateInWeek(IEnumerable<DisplayItemModel> models, List<SectionStatusModel> sectionStatusModels, ObservableCollection<DisplayItemModel> results, HashSet<string> openedTaskIds, IComparer<DisplayItemModel> comparer, IComparer<DisplayItemModel> noteComparer, ProjectSortExtra extra)
	{
		List<Section> list = GenerateWeekSections();
		HabitSection habits = new HabitSection(useInWeek: true, sortAsDate: true);
		Dictionary<string, Node<DisplayItemModel>> nodeDict = GetNodeDict(models.ToList());
		NoteSection notes = new NoteSection();
		BuildDisplayNodeTree(nodeDict, extra);
		foreach (Node<DisplayItemModel> item in nodeDict.Values.Where((Node<DisplayItemModel> n) => !n.HasParent))
		{
			DisplayItemModel value = item.Value;
			if (value.Habit != null)
			{
				habits.Children.Add(value);
				continue;
			}
			if (value.IsNote)
			{
				notes.Children.Add(value);
				continue;
			}
			item.GetAllChildrenValue();
			if (value.StartDate.HasValue)
			{
				GetWeekSection(value.StartDate, value.DueDate, value.IsAllDay, list)?.Children.Add(value);
			}
		}
		Section outDated = list.FirstOrDefault((Section section2) => section2.SectionId == "outdated");
		if (extra.SortOption.orderDirection == Constants.OrderDirection.Desc)
		{
			new CourseStartDateComparerDesc();
			new DateComparerDesc(inNote: false);
		}
		else
		{
			new CourseStartDateComparer();
			new DateComparer(inNote: false);
		}
		if (LocalSettings.Settings.PosOfOverdue == 0)
		{
			DateComparer obj = comparer as DateComparer;
			obj?.SetCheckSpecial(checkSpecial: false);
			AssembleSection(outDated, comparer, sectionStatusModels, results, openedTaskIds);
			obj?.SetCheckSpecial(checkSpecial: true);
		}
		foreach (Section section in list.Where((Section section2) => section2.SectionId != "outdated"))
		{
			if (!extra.InKanban)
			{
				await SetItemSpecialOrder(section, extra.SortKey, extra.SortCatId, extra.DefaultOrder);
			}
			AssembleSection(section, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra);
			if (extra.ShowFold && section.SectionDate == DateTime.Today)
			{
				AssembleSection(habits, null, sectionStatusModels, results);
			}
		}
		if (LocalSettings.Settings.PosOfOverdue == 1)
		{
			DateComparer obj2 = comparer as DateComparer;
			obj2?.SetCheckSpecial(checkSpecial: false);
			AssembleSection(outDated, comparer, sectionStatusModels, results, openedTaskIds);
			obj2?.SetCheckSpecial(checkSpecial: true);
		}
		AssembleSection(notes, noteComparer, sectionStatusModels, results, openedTaskIds);
		if (!extra.ShowFold)
		{
			AssembleSection(habits, null, sectionStatusModels, results);
		}
	}

	private static Dictionary<string, Node<DisplayItemModel>> GetNodeDict(List<DisplayItemModel> displayItemModels)
	{
		Dictionary<string, Node<DisplayItemModel>> dictionary = new Dictionary<string, Node<DisplayItemModel>>();
		foreach (DisplayItemModel displayItemModel in displayItemModels)
		{
			if (!string.IsNullOrEmpty(displayItemModel.Id) && !dictionary.ContainsKey(displayItemModel.Id))
			{
				dictionary.Add(displayItemModel.Id, new DisplayItemNode(displayItemModel));
			}
		}
		return dictionary;
	}

	private static void BuildDisplayNodeTree(Dictionary<string, Node<DisplayItemModel>> nodeDict, ProjectSortExtra extra)
	{
		TaskNodeUtils.BuildNodeTree(nodeDict, checkParentExist: false, extra?.BuildByChildIds ?? false);
	}

	private static Section GetWeekSection(DateTime? startDate, DateTime? dueDate, bool? isAllDay, IReadOnlyList<Section> weekSections)
	{
		DateTime checkDate = startDate ?? DateTime.Today;
		if (startDate.HasValue && startDate.Value.Date == DateTime.Today.AddDays(1.0))
		{
			checkDate = DateTime.Today.AddDays(1.0);
		}
		else if (DateUtils.GetSectionCategory(startDate, dueDate, isAllDay) == DateUtils.DateSectionCategory.Today)
		{
			checkDate = DateTime.Today;
		}
		Section section = weekSections.FirstOrDefault((Section section2) => section2.SectionDate.HasValue && section2.SectionDate.Value.Date == checkDate.Date);
		if (section != null)
		{
			return section;
		}
		return weekSections[0];
	}

	private static List<Section> GenerateWeekSections()
	{
		List<Section> list = new List<Section>
		{
			new OutdatedSection
			{
				Ordinal = 8L,
				ShowPostpone = true
			},
			new Section(string.Format(Utils.GetString("7DayThisWeek"), Utils.GetString("Today"), DateTime.Now.ToString("ddd", App.Ci)), 7, DateTime.Now.Date, "today", "", canSort: true, canSwitch: true)
			{
				SectionEntityId = DateTime.Today.ToString("yyyyMMdd", DateTimeFormatInfo.InvariantInfo)
			},
			new Section(string.Format(Utils.GetString("7DayThisWeek"), Utils.GetString("Tomorrow"), DateTime.Now.AddDays(1.0).ToString("ddd", App.Ci)), 6, DateTime.Now.AddDays(1.0).Date, "tomorrow", "", canSort: true, canSwitch: true)
			{
				SectionEntityId = DateTime.Today.AddDays(1.0).ToString("yyyyMMdd", DateTimeFormatInfo.InvariantInfo)
			}
		};
		DayOfWeek weekStartFromType = LocalSettings.Settings.WeekStartFromType;
		DateTime dateTime = DateTime.Today;
		for (int i = 0; i < 7; i++)
		{
			dateTime = dateTime.AddDays(1.0);
			if (dateTime.DayOfWeek == weekStartFromType)
			{
				break;
			}
		}
		for (int j = 2; j < 7; j++)
		{
			DateTime dateTime2 = DateTime.Today.AddDays(j);
			Section item = new Section(canSort: true, canSwitch: true)
			{
				SectionId = dateTime2.Date.ToString("yyyyMMdd", App.Ci),
				Ordinal = 7 - j,
				SectionDate = dateTime2.Date,
				Name = string.Format((dateTime2 >= dateTime) ? Utils.GetString("7DayTodayNextWeek") : Utils.GetString("7DayThisWeek"), DateUtils.FormatShortDate(dateTime2), DateUtils.FormatWeekDayName(dateTime2)),
				SectionEntityId = DateTime.Today.AddDays(j).ToString("yyyyMMdd", DateTimeFormatInfo.InvariantInfo)
			};
			list.Add(item);
		}
		return list;
	}

	private static async Task SortAsDateInTomorrow(IEnumerable<DisplayItemModel> models, List<SectionStatusModel> sectionStatusModels, ObservableCollection<DisplayItemModel> results, HashSet<string> openedTaskIds, IComparer<DisplayItemModel> comparer, IComparer<DisplayItemModel> noteComparer, ProjectSortExtra extra)
	{
		TomorrowSection section = new TomorrowSection();
		Dictionary<string, Node<DisplayItemModel>> nodeDict = GetNodeDict(models.ToList());
		NoteSection notes = new NoteSection();
		BuildDisplayNodeTree(nodeDict, extra);
		foreach (Node<DisplayItemModel> item in nodeDict.Values.Where((Node<DisplayItemModel> n) => !n.HasParent))
		{
			DisplayItemModel value = item.Value;
			if (value.IsNote)
			{
				notes.Children.Add(value);
				continue;
			}
			item.GetAllChildrenValue();
			section.Children.Add(value);
		}
		if (!extra.InKanban)
		{
			await SetItemSpecialOrder(section, extra.SortKey, extra.SortCatId, extra.DefaultOrder);
		}
		bool showSection = extra.InKanban || results.Count > 0;
		AssembleSection(section, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra, showSection);
		AssembleSection(notes, noteComparer, sectionStatusModels, results, openedTaskIds);
	}

	public static async Task GroupAsCreateTime(IReadOnlyCollection<DisplayItemModel> models, List<SectionStatusModel> sectionStatusModels, ObservableCollection<DisplayItemModel> results, HashSet<string> openedTaskIds, IComparer<DisplayItemModel> comparer, ProjectSortExtra extra)
	{
		if (extra.IsToday || extra.IsTomorrow || extra.IsWeek)
		{
			await SortAsCreateTimeInSmartDate(models, sectionStatusModels, results, openedTaskIds, comparer, extra);
		}
		else
		{
			await SortAsCreateTimeNormal(models, sectionStatusModels, results, openedTaskIds, comparer, extra);
		}
	}

	private static async Task SortAsCreateTimeNormal(IEnumerable<DisplayItemModel> models, List<SectionStatusModel> sectionStatusModels, ObservableCollection<DisplayItemModel> results, HashSet<string> openedTaskIds, IComparer<DisplayItemModel> comparer, ProjectSortExtra extra)
	{
		CreateTimeSection section = new CreateTimeSection();
		ProjectSection calendars = new ProjectSection(canSort: false)
		{
			SectionId = "8ac3038d93c54b80a67321b6a03df066",
			Name = Utils.GetString("SubscribeCalendar")
		};
		Dictionary<string, Node<DisplayItemModel>> nodeDict = GetNodeDict(models.ToList());
		BuildDisplayNodeTree(nodeDict, extra);
		foreach (Node<DisplayItemModel> item in nodeDict.Values.Where((Node<DisplayItemModel> n) => !n.HasParent))
		{
			DisplayItemModel value = item.Value;
			if (value.ProjectId == "8ac3038d93c54b80a67321b6a03df066")
			{
				calendars.Children.Add(value);
				continue;
			}
			item.GetAllChildrenValue();
			if (openedTaskIds != null)
			{
				value.IsOpen = openedTaskIds.Contains(value.Id);
			}
			switch (DateUtils.GetCreateTimeSectionCategory(value.CreatedTime))
			{
			case DateUtils.CreateTimeCategory.Today:
				section.Today.Children.Add(value);
				break;
			case DateUtils.CreateTimeCategory.Yesterday:
				section.Yesterday.Children.Add(value);
				break;
			case DateUtils.CreateTimeCategory.LastWeek:
				section.Week.Children.Add(value);
				break;
			case DateUtils.CreateTimeCategory.LastMonth:
				section.Month.Children.Add(value);
				break;
			case DateUtils.CreateTimeCategory.Earlier:
				section.Early.Children.Add(value);
				break;
			}
		}
		if (!extra.InKanban)
		{
			await Task.WhenAll(SetItemSpecialOrder(section.Today, extra.SortKey, extra.SortCatId, extra.DefaultOrder), SetItemSpecialOrder(section.Yesterday, extra.SortKey, extra.SortCatId, extra.DefaultOrder), SetItemSpecialOrder(section.Week, extra.SortKey, extra.SortCatId, extra.DefaultOrder), SetItemSpecialOrder(section.Month, extra.SortKey, extra.SortCatId, extra.DefaultOrder), SetItemSpecialOrder(section.Early, extra.SortKey, extra.SortCatId, extra.DefaultOrder));
		}
		if (extra.InKanban && extra.SortOption.orderDirection == Constants.OrderDirection.Asc)
		{
			AssembleSection(section.Early, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra.ExistSpecialOrderItem ? extra : null);
			AssembleSection(section.Month, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra.ExistSpecialOrderItem ? extra : null);
			AssembleSection(section.Week, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra.ExistSpecialOrderItem ? extra : null);
			AssembleSection(section.Yesterday, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra);
			AssembleSection(section.Today, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra);
		}
		else
		{
			AssembleSection(section.Today, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra);
			AssembleSection(section.Yesterday, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra);
			AssembleSection(section.Week, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra.ExistSpecialOrderItem ? extra : null);
			AssembleSection(section.Month, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra.ExistSpecialOrderItem ? extra : null);
			AssembleSection(section.Early, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra.ExistSpecialOrderItem ? extra : null);
		}
		IComparer<DisplayItemModel> comparer2 = ((extra.SortOption.orderDirection != Constants.OrderDirection.Desc) ? ((IComparer<DisplayItemModel>)new DateComparer(inNote: false)) : ((IComparer<DisplayItemModel>)new DateComparerDesc(inNote: false)));
		AssembleSection(calendars, comparer2, sectionStatusModels, results, openedTaskIds);
	}

	private static async Task SortAsCreateTimeInSmartDate(IEnumerable<DisplayItemModel> models, List<SectionStatusModel> sectionStatusModels, ObservableCollection<DisplayItemModel> results, HashSet<string> openedTaskIds, IComparer<DisplayItemModel> comparer, ProjectSortExtra extra)
	{
		CreateTimeSection section = new CreateTimeSection();
		HabitSection habits = new HabitSection();
		TimetableSection courses = new TimetableSection();
		ProjectSection calendars = new ProjectSection(canSort: false)
		{
			SectionId = "8ac3038d93c54b80a67321b6a03df066",
			Name = Utils.GetString("SubscribeCalendar")
		};
		Dictionary<string, Node<DisplayItemModel>> nodeDict = GetNodeDict(models.ToList());
		BuildDisplayNodeTree(nodeDict, extra);
		foreach (Node<DisplayItemModel> item in nodeDict.Values.Where((Node<DisplayItemModel> n) => !n.HasParent))
		{
			DisplayItemModel value = item.Value;
			if (value.Habit != null)
			{
				habits.Children.Add(value);
				continue;
			}
			if (value.IsCourse)
			{
				courses.Children.Add(value);
				continue;
			}
			if (value.ProjectId == "8ac3038d93c54b80a67321b6a03df066")
			{
				calendars.Children.Add(value);
				continue;
			}
			if (openedTaskIds != null)
			{
				value.IsOpen = openedTaskIds.Contains(value.Id);
			}
			DateUtils.CreateTimeCategory createTimeSectionCategory = DateUtils.GetCreateTimeSectionCategory(value.CreatedTime);
			item.GetAllChildrenValue();
			switch (createTimeSectionCategory)
			{
			case DateUtils.CreateTimeCategory.Today:
				section.Today.Children.Add(value);
				break;
			case DateUtils.CreateTimeCategory.Yesterday:
				section.Yesterday.Children.Add(value);
				break;
			case DateUtils.CreateTimeCategory.LastWeek:
				section.Week.Children.Add(value);
				break;
			case DateUtils.CreateTimeCategory.LastMonth:
				section.Month.Children.Add(value);
				break;
			case DateUtils.CreateTimeCategory.Earlier:
				section.Early.Children.Add(value);
				break;
			}
		}
		if (!extra.InKanban)
		{
			await Task.WhenAll(SetItemSpecialOrder(section.Today, extra.SortKey, extra.SortCatId, extra.DefaultOrder), SetItemSpecialOrder(section.Yesterday, extra.SortKey, extra.SortCatId, extra.DefaultOrder), SetItemSpecialOrder(section.Week, extra.SortKey, extra.SortCatId, extra.DefaultOrder), SetItemSpecialOrder(section.Month, extra.SortKey, extra.SortCatId, extra.DefaultOrder), SetItemSpecialOrder(section.Early, extra.SortKey, extra.SortCatId, extra.DefaultOrder));
		}
		AssembleSection(section.Today, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra);
		AssembleSection(section.Yesterday, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra);
		AssembleSection(section.Week, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra.ExistSpecialOrderItem ? extra : null);
		AssembleSection(section.Month, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra.ExistSpecialOrderItem ? extra : null);
		AssembleSection(section.Early, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra.ExistSpecialOrderItem ? extra : null);
		IComparer<DisplayItemModel> comparer2 = ((extra.SortOption.orderByType == Constants.SortType.createdTime || extra.SortOption.orderByType == Constants.SortType.modifiedTime) ? new DateComparer(inNote: false) : ((extra.SortOption.orderDirection != Constants.OrderDirection.Desc) ? ((IComparer<DisplayItemModel>)new DateComparer(inNote: false)) : ((IComparer<DisplayItemModel>)new DateComparerDesc(inNote: false))));
		AssembleSection(calendars, comparer2, sectionStatusModels, results, openedTaskIds);
		bool isDescForCourse = extra.SortOption.orderByType != Constants.SortType.createdTime && extra.SortOption.orderByType != Constants.SortType.modifiedTime && extra.SortOption.orderDirection == Constants.OrderDirection.Desc;
		courses.Children.Sort(delegate(DisplayItemModel a, DisplayItemModel b)
		{
			if (!a.StartDate.HasValue || !b.StartDate.HasValue)
			{
				if (a.StartDate.HasValue)
				{
					return -1;
				}
				return 1;
			}
			return (!isDescForCourse) ? a.StartDate.Value.CompareTo(b.StartDate.Value) : b.StartDate.Value.CompareTo(a.StartDate.Value);
		});
		AssembleSection(courses, null, sectionStatusModels, results);
		AssembleSection(habits, null, sectionStatusModels, results);
	}

	private static void AssembleSection(Section section, IComparer<DisplayItemModel> comparer, IEnumerable<SectionStatusModel> sectionStatusModels, ObservableCollection<DisplayItemModel> results, HashSet<string> openedTaskIds = null, bool compareChild = true, ProjectSortExtra extra = null, bool showSection = true)
	{
		if (!section.Customized && section.Children.Count <= 0)
		{
			return;
		}
		DisplayItemModel displayItemModel = DisplayItemModel.BuildSection(section);
		displayItemModel.IsOpen = !IsSectionClosed(section, sectionStatusModels);
		if (showSection)
		{
			results.Add(displayItemModel);
		}
		if (comparer != null)
		{
			List<DisplayItemModel> children = section.Children.OrderBy((DisplayItemModel x) => x, comparer).ToList();
			section.Children = children;
		}
		int num;
		if (extra != null && !string.IsNullOrEmpty(extra.SortKey))
		{
			SortOption sortOption = extra.SortOption;
			num = ((((sortOption == null || sortOption.orderByType != Constants.SortType.sortOrder) && !string.IsNullOrEmpty(section.SectionEntityId)) || extra.ExistSpecialOrderItem) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		bool flag = (byte)num != 0;
		List<DisplayItemModel> list = new List<DisplayItemModel>();
		foreach (DisplayItemModel child in section.Children)
		{
			list.Add(child);
			child.Section = section;
			child.Parent = displayItemModel;
			if (openedTaskIds != null)
			{
				child.IsOpen = openedTaskIds.Contains(child.Id);
			}
			if (compareChild)
			{
				child.SortChildren(comparer);
			}
			if (displayItemModel.IsOpen || !showSection)
			{
				results.Add(child);
				if (child.IsOpen)
				{
					child.GetChildrenModels(getAll: false, openedTaskIds).ForEach(delegate(DisplayItemModel child)
					{
						child.Section = section;
						if (openedTaskIds != null)
						{
							child.IsOpen = openedTaskIds.Contains(child.Id);
						}
						results.Add(child);
					});
				}
			}
			if (flag)
			{
				list.AddRange(child.GetChildrenModels(getAll: true));
			}
		}
		if (flag)
		{
			CheckSpecialOrder(list, section, extra);
		}
	}

	private static void CheckSpecialOrder(List<DisplayItemModel> allItems, Section section, ProjectSortExtra extra)
	{
		string text = (extra.InKanban ? extra.SortKey : string.Format(extra.SortKey, section.SectionEntityId));
		long num = long.MinValue;
		long num2 = long.MinValue;
		List<SyncSortOrderModel> list = new List<SyncSortOrderModel>();
		List<DisplayItemModel> list2 = new List<DisplayItemModel>();
		for (int i = 0; i < allItems.Count; i++)
		{
			DisplayItemModel displayItemModel = allItems[i];
			if (displayItemModel.SpecialOrder == extra.DefaultOrder)
			{
				list2.Add(displayItemModel);
			}
			else if (list2.Count > 0)
			{
				num2 = displayItemModel.SpecialOrder;
				long num3 = ((num == long.MinValue) ? 268435456 : ((num2 - num) / (list2.Count + 1)));
				for (int num4 = list2.Count - 1; num4 >= 0; num4--)
				{
					DisplayItemModel displayItemModel2 = list2[num4];
					displayItemModel2.SpecialOrder = num2 - num3 * (num4 + 1);
					SyncSortOrderModel item = new SyncSortOrderModel(text)
					{
						Type = EntityType.GetEntityTypeNum(displayItemModel2.Type),
						SortOrder = displayItemModel2.SpecialOrder,
						GroupId = extra.SortCatId,
						EntityId = displayItemModel2.EntityId,
						SyncStatus = 1
					};
					list.Add(item);
				}
				list2.Clear();
				num = long.MinValue;
				num2 = long.MinValue;
			}
			else
			{
				num = displayItemModel.SpecialOrder;
			}
		}
		if ((num != long.MinValue || extra.ExistSpecialOrderItem) && num2 == long.MinValue && list2.Count > 0)
		{
			if (num == long.MinValue)
			{
				num = new Random().Next(1, 512);
			}
			long num5 = 268435456L;
			for (int num6 = list2.Count - 1; num6 >= 0; num6--)
			{
				DisplayItemModel displayItemModel3 = list2[num6];
				displayItemModel3.SpecialOrder = num + num5 * (num6 + 1);
				SyncSortOrderModel item2 = new SyncSortOrderModel(text)
				{
					Type = EntityType.GetEntityTypeNum(displayItemModel3.Type),
					SortOrder = displayItemModel3.SpecialOrder,
					GroupId = extra.SortCatId,
					EntityId = displayItemModel3.EntityId,
					SyncStatus = 1
				};
				list.Add(item2);
			}
			list2.Clear();
		}
		TaskSortOrderService.AddSortModels(text, extra.SortCatId, list);
		SyncSortOrderDao.InsertAllAsync(list);
		if (list.Count > 0)
		{
			SyncManager.TryDelaySync(2000);
		}
	}

	private static bool IsSectionClosed(Section section, IEnumerable<SectionStatusModel> sectionStatusModels)
	{
		AssigneeSection assigneeSection = section as AssigneeSection;
		if (assigneeSection != null)
		{
			return sectionStatusModels.Any((SectionStatusModel model) => model.Name == assigneeSection.Assingee);
		}
		return sectionStatusModels.Any((SectionStatusModel model) => model.Name == section.Name);
	}

	private static async Task SortAsDateInToday(IEnumerable<DisplayItemModel> models, List<SectionStatusModel> sectionStatusModels, ObservableCollection<DisplayItemModel> results, HashSet<string> openedTaskIds, IComparer<DisplayItemModel> comparer, IComparer<DisplayItemModel> noteComparer, ProjectSortExtra extra)
	{
		TodaySection today = new TodaySection();
		OutdatedSection outDated = new OutdatedSection
		{
			ShowPostpone = true
		};
		HabitSection habits = new HabitSection();
		NoteSection notes = new NoteSection();
		Dictionary<string, Node<DisplayItemModel>> nodeDict = GetNodeDict(models.ToList());
		BuildDisplayNodeTree(nodeDict, extra);
		foreach (Node<DisplayItemModel> item in nodeDict.Values.Where((Node<DisplayItemModel> n) => !n.HasParent))
		{
			DisplayItemModel value = item.Value;
			if (value.Habit != null)
			{
				habits.Children.Add(value);
				continue;
			}
			if (value.IsNote)
			{
				notes.Children.Add(value);
				continue;
			}
			if (openedTaskIds != null)
			{
				value.IsOpen = openedTaskIds.Contains(value.Id);
			}
			DateUtils.DateSectionCategory sectionCategory = DateUtils.GetSectionCategory(value.StartDate, value.DueDate, value.IsAllDay);
			item.GetAllChildrenValue();
			switch (sectionCategory)
			{
			case DateUtils.DateSectionCategory.Today:
				today.Children.Add(value);
				break;
			case DateUtils.DateSectionCategory.OutDated:
				outDated.Children.Add(value);
				break;
			}
		}
		if (!extra.InKanban)
		{
			await SetItemSpecialOrder(today, extra.SortKey, extra.SortCatId, extra.DefaultOrder);
		}
		if (extra.SortOption.orderDirection == Constants.OrderDirection.Desc)
		{
			new CourseStartDateComparerDesc();
			new DateComparerDesc(inNote: false);
		}
		else
		{
			new CourseStartDateComparer();
			new DateComparer(inNote: false);
		}
		if (LocalSettings.Settings.PosOfOverdue == 0)
		{
			AssembleSection(outDated, comparer, sectionStatusModels, results, openedTaskIds);
			bool showSection = extra.InKanban || results.Count > 0;
			AssembleSection(today, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra, showSection);
			AssembleSection(notes, noteComparer, sectionStatusModels, results);
			AssembleSection(habits, null, sectionStatusModels, results);
		}
		else
		{
			bool showSection = extra.InKanban || results.Count > 0;
			AssembleSection(today, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra, showSection);
			AssembleSection(outDated, comparer, sectionStatusModels, results, openedTaskIds);
			AssembleSection(notes, noteComparer, sectionStatusModels, results);
			AssembleSection(habits, null, sectionStatusModels, results);
		}
	}

	private static async Task GroupAsProject(IReadOnlyCollection<DisplayItemModel> models, List<SectionStatusModel> sectionStatusModels, ObservableCollection<DisplayItemModel> results, HashSet<string> openedTaskIds, IComparer<DisplayItemModel> comparer, ProjectSortExtra extra)
	{
		if (models == null || models.Count <= 0)
		{
			return;
		}
		Dictionary<string, ProjectSection> dictionary = new Dictionary<string, ProjectSection>();
		Dictionary<string, long> projectSortOrders = CacheManager.GetProjectSortOrders();
		List<ProjectModel> projects = CacheManager.GetProjects();
		HabitSection habits = new HabitSection(extra.IsWeek);
		TimetableSection courses = new TimetableSection();
		foreach (Node<DisplayItemModel> item in from n in TaskNodeUtils.GetSortedTaskNodeTree(models, extra.BuildByChildIds)
			where !n.HasParent
			select n)
		{
			DisplayItemModel model = item.Value;
			item.GetAllChildrenValue();
			if (model.Habit != null)
			{
				habits.Children.Add(model);
			}
			else if (model.IsCourse)
			{
				courses.Children.Add(model);
			}
			else
			{
				if (model.ProjectId == null)
				{
					continue;
				}
				if (openedTaskIds != null)
				{
					model.IsOpen = openedTaskIds.Contains(model.Id);
				}
				if (!dictionary.ContainsKey(model.ProjectId))
				{
					dictionary.Add(value: new ProjectSection((projects?.FirstOrDefault((ProjectModel p) => p.id == model.ProjectId))?.IsProjectPermit() ?? false)
					{
						SectionId = model.ProjectId,
						ProjectId = model.ProjectId,
						Name = model.ProjectName,
						Ordinal = GetProjectOrdinal(model, projectSortOrders),
						SectionEntityId = model.ProjectId
					}, key: model.ProjectId);
				}
				dictionary[model.ProjectId].Children.Add(model);
			}
		}
		List<ProjectSection> list = (from p in dictionary.Values.ToList()
			orderby p.Ordinal
			select p).ToList();
		foreach (ProjectSection section in list)
		{
			if (!extra.InKanban)
			{
				await SetItemSpecialOrder(section, extra.SortKey, extra.SortCatId, extra.DefaultOrder);
			}
			if (section.SectionId == "8ac3038d93c54b80a67321b6a03df066")
			{
				section.Name = Utils.GetString("SubscribeCalendar");
				IComparer<DisplayItemModel> comparer2 = ((extra.SortOption.orderByType == Constants.SortType.createdTime || extra.SortOption.orderByType == Constants.SortType.modifiedTime) ? new DateComparer(inNote: false) : ((extra.SortOption.orderDirection != Constants.OrderDirection.Desc) ? ((IComparer<DisplayItemModel>)new DateComparer(inNote: false)) : ((IComparer<DisplayItemModel>)new DateComparerDesc(inNote: false))));
				AssembleSection(section, comparer2, sectionStatusModels, results, openedTaskIds);
			}
			else
			{
				AssembleSection(section, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra);
			}
		}
		bool isDescForCourse = extra.SortOption.orderByType != Constants.SortType.createdTime && extra.SortOption.orderByType != Constants.SortType.modifiedTime && extra.SortOption.orderDirection == Constants.OrderDirection.Desc;
		courses.Children.Sort(delegate(DisplayItemModel a, DisplayItemModel b)
		{
			if (!a.StartDate.HasValue || !b.StartDate.HasValue)
			{
				if (a.StartDate.HasValue)
				{
					return -1;
				}
				return 1;
			}
			return (!isDescForCourse) ? a.StartDate.Value.CompareTo(b.StartDate.Value) : b.StartDate.Value.CompareTo(a.StartDate.Value);
		});
		AssembleSection(courses, null, sectionStatusModels, results);
		AssembleSection(habits, null, sectionStatusModels, results);
	}

	private static long GetProjectOrdinal(DisplayItemModel model, IReadOnlyDictionary<string, long> sorts)
	{
		if (model.IsEvent)
		{
			return long.MaxValue;
		}
		if (!(model.ProjectId == Utils.GetInboxId()))
		{
			if (!sorts.ContainsKey(model.ProjectId))
			{
				return 0L;
			}
			return sorts[model.ProjectId];
		}
		return long.MinValue;
	}

	private static async Task GroupAsSortOrder(List<DisplayItemModel> models, ObservableCollection<DisplayItemModel> results, List<ColumnModel> columns, List<SectionStatusModel> sectionStatusModels, HashSet<string> openedTaskIds, bool existStarred, IComparer<DisplayItemModel> comparer, ProjectSortExtra extra)
	{
		List<Node<DisplayItemModel>> sortedTaskNodeTree = TaskNodeUtils.GetSortedTaskNodeTree(models, extra.BuildByChildIds);
		List<DisplayItemModel> sortedTreeModels = TaskNodeUtils.GetModelsFromNodes(sortedTaskNodeTree);
		List<CustomizedSection> sections = new List<CustomizedSection>();
		List<ColumnModel> list = columns;
		if (list != null && list.Count > 0)
		{
			columns.Sort(delegate(ColumnModel a, ColumnModel b)
			{
				if (a.sortOrder.HasValue && b.sortOrder.HasValue)
				{
					return a.sortOrder.Value.CompareTo(b.sortOrder.Value);
				}
				return b.sortOrder.HasValue ? (-1) : 0;
			});
			sections.AddRange(columns.Select((ColumnModel column) => new CustomizedSection(column)
			{
				CanDelete = (columns.Count > 1)
			}));
		}
		Dictionary<string, List<SyncSortOrderModel>> orderDict = new Dictionary<string, List<SyncSortOrderModel>>();
		if (!string.IsNullOrEmpty(extra.SortKey))
		{
			if (sections.Count <= 1)
			{
				List<SyncSortOrderModel> list2 = await TaskSortOrderService.GetAsyncList(string.Format(extra.SortKey, (sections.Count == 1) ? sections[0].SectionId : "none"), extra.SortCatId);
				foreach (DisplayItemModel model in models)
				{
					long? obj = list2?.FirstOrDefault((SyncSortOrderModel o) => o.EntityId == model.EntityId)?.SortOrder;
					model.SpecialOrder = obj ?? extra.DefaultOrder;
				}
			}
			else
			{
				foreach (CustomizedSection section in sections)
				{
					List<SyncSortOrderModel> value = await TaskSortOrderService.GetAsyncList(string.Format(extra.SortKey, section.SectionId), extra.SortCatId);
					orderDict[section.SectionId] = value;
				}
			}
		}
		if (sections.Count > 1 || existStarred)
		{
			foreach (DisplayItemModel model2 in sortedTreeModels.Where((DisplayItemModel displayItemModel) => displayItemModel.Level == 0))
			{
				if (existStarred)
				{
					if (sections.Count > 0)
					{
						if (sections.Count == 1)
						{
							sections[0].CanDelete = false;
						}
						Section section2 = GetDisplaySectionByColumnId(sections, model2) ?? sections[0];
						if (model2.Status == 0 && model2.ColumnId != section2.SectionId && section2.SectionId != "NotPinned" && model2.ProjectId == section2.ProjectId && section2 == sections[0] && string.IsNullOrEmpty(TaskCache.GetLastDefaultColumnId(model2.ProjectId)))
						{
							TaskCache.SetLastDefaultColumnId(model2.ProjectId, section2.SectionId);
						}
						section2.Children.Add(model2);
						if (sections.Count <= 1 || string.IsNullOrEmpty(extra.SortKey))
						{
							continue;
						}
						List<SyncSortOrderModel> list3 = ((section2.SectionId != null && orderDict.ContainsKey(section2.SectionId)) ? orderDict[section2.SectionId] : null);
						long? obj2 = list3?.FirstOrDefault((SyncSortOrderModel o) => o.EntityId == model2.EntityId)?.SortOrder;
						model2.SpecialOrder = obj2 ?? extra.DefaultOrder;
						List<DisplayItemModel> childrenModels = model2.GetChildrenModels(getAll: true);
						if (childrenModels == null || childrenModels.Count <= 0)
						{
							continue;
						}
						foreach (DisplayItemModel child in childrenModels)
						{
							long? obj3 = list3?.FirstOrDefault((SyncSortOrderModel o) => o.EntityId == child.EntityId)?.SortOrder;
							child.SpecialOrder = obj3 ?? extra.DefaultOrder;
						}
					}
					else
					{
						string projectId = model2.ProjectId;
						CustomizedSection customizedSection = new CustomizedSection
						{
							Name = Utils.GetString((projectId == Utils.GetInboxId() || extra.InKanban) ? "NotPinned" : "NotSectioned"),
							SectionId = "NotPinned",
							SectionEntityId = "none",
							Ordinal = 0L,
							ProjectId = projectId,
							Customized = false
						};
						sections.Add(customizedSection);
						customizedSection.Children.Add(model2);
					}
					continue;
				}
				Section section3 = GetDisplaySectionByColumnId(sections, model2) ?? sections[0];
				if (model2.Status == 0 && model2.ColumnId != section3.SectionId && model2.ProjectId == section3.ProjectId && section3 == sections[0] && string.IsNullOrEmpty(TaskCache.GetLastDefaultColumnId(model2.ProjectId)))
				{
					TaskCache.SetLastDefaultColumnId(model2.ProjectId, section3.SectionId);
				}
				section3.Children.Add(model2);
				if (string.IsNullOrEmpty(extra.SortKey))
				{
					continue;
				}
				List<SyncSortOrderModel> list4 = ((section3.SectionId != null && orderDict.ContainsKey(section3.SectionId)) ? orderDict[section3.SectionId] : null);
				long? obj4 = list4?.FirstOrDefault((SyncSortOrderModel o) => o.EntityId == model2.EntityId)?.SortOrder;
				model2.SpecialOrder = obj4 ?? extra.DefaultOrder;
				List<DisplayItemModel> childrenModels2 = model2.GetChildrenModels(getAll: true);
				if (childrenModels2 == null || childrenModels2.Count <= 0)
				{
					continue;
				}
				foreach (DisplayItemModel child2 in childrenModels2)
				{
					long? obj5 = list4?.FirstOrDefault((SyncSortOrderModel o) => o.EntityId == child2.EntityId)?.SortOrder;
					child2.SpecialOrder = obj5 ?? extra.DefaultOrder;
				}
			}
			{
				foreach (CustomizedSection item in sections)
				{
					AssembleSection(item, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra);
				}
				return;
			}
		}
		CustomizedSection customizedSection2 = ((sections.Count == 1) ? sections[0] : new CustomizedSection
		{
			SectionEntityId = "none",
			SectionId = "none",
			Customized = false
		});
		sortedTreeModels = sortedTreeModels.Where((DisplayItemModel displayItemModel) => displayItemModel.Level == 0).ToList();
		foreach (DisplayItemModel item2 in sortedTreeModels)
		{
			item2.Section = customizedSection2;
			customizedSection2.Children.Add(item2);
		}
		AssembleSection(customizedSection2, comparer, sectionStatusModels, results, openedTaskIds, compareChild: true, extra, showSection: false);
	}

	private static IComparer<DisplayItemModel> GetCompareByOrderBy(string orderBy, Constants.OrderDirection? sortDirection, string projectId, bool isNote = false)
	{
		switch (orderBy)
		{
		case "title":
			if (sortDirection != Constants.OrderDirection.Desc)
			{
				return new TitleComparer(isNote);
			}
			return new TitleComparerDesc(isNote);
		case "createdTime":
			if (sortDirection != Constants.OrderDirection.Asc)
			{
				return new CreatedTimeComparer();
			}
			return new CreatedTimeComparerAsc();
		case "modifiedTime":
			if (sortDirection != Constants.OrderDirection.Asc)
			{
				return new ModifiedTimeComparer();
			}
			return new ModifiedTimeComparerAsc();
		case "dueDate":
			if (sortDirection != Constants.OrderDirection.Desc)
			{
				return new DateComparer(isNote);
			}
			return new DateComparerDesc(isNote);
		case "sortOrder":
			return new SortOrderComparer();
		case "project":
			return new ProjectDateComparer(isNote);
		case "priority":
			if (sortDirection != Constants.OrderDirection.Asc)
			{
				return new PriorityDateComparer(isNote);
			}
			return new PriorityDateComparerAsc(isNote);
		case "assignee":
			if (sortDirection != Constants.OrderDirection.Desc)
			{
				return new AssigneeComparer(projectId, isNote);
			}
			return new AssigneeComparerDesc(projectId, isNote);
		case "tag":
		{
			List<string> sortTags = TagDataHelper.GetTags()?.Select((TagModel t) => t.name).ToList();
			if (sortDirection != Constants.OrderDirection.Desc)
			{
				return new TagComparer(sortTags, isNote);
			}
			return new TagComparerDesc(sortTags, isNote);
		}
		default:
			return new DateComparer(isNote);
		}
	}

	private static void SortAsNone(IReadOnlyCollection<DisplayItemModel> models, ObservableCollection<DisplayItemModel> results, string orderBy, ProjectSortExtra extra = null)
	{
		if (!(orderBy == "none"))
		{
			return;
		}
		if (results == null)
		{
			throw new ArgumentNullException("results");
		}
		if (models == null || models.Count <= 0)
		{
			return;
		}
		if (extra != null && extra.BuildByChildIds)
		{
			foreach (DisplayItemModel modelsFromNode in TaskNodeUtils.GetModelsFromNodes(TaskNodeUtils.GetSortedTaskNodeTree(models, buildByChildIds: true)))
			{
				results.Add(modelsFromNode);
			}
			return;
		}
		foreach (DisplayItemModel model in models)
		{
			results.Add(model);
		}
	}

	private static Section GetDisplaySectionByColumnId(IEnumerable<Section> sections, DisplayItemModel model)
	{
		List<Section> list = sections?.ToList();
		if (list == null || list.Count == 0 || model == null)
		{
			return null;
		}
		Section section = list.FirstOrDefault((Section s) => s.SectionId == model.ColumnId);
		if (section != null)
		{
			return section;
		}
		string lastDefaultColumnId = TaskCache.GetLastDefaultColumnId(model.ProjectId);
		if (!string.IsNullOrEmpty(lastDefaultColumnId))
		{
			Section section2 = list.FirstOrDefault((Section s) => s.SectionId == lastDefaultColumnId && s.ProjectId == model.ProjectId);
			if (section2 != null)
			{
				return section2;
			}
		}
		return list[0];
	}

	private static void AppendCompletedSection(IReadOnlyList<DisplayItemModel> completed, IEnumerable<SectionStatusModel> sectionStatusModels, ICollection<DisplayItemModel> results, bool showLoadMore, bool showCompletedSection = true, bool loadAll = false, ClosedLoader closedLoader = null, bool forceShowLoadMore = false, HashSet<string> openedTaskIds = null, bool buildByChildIds = false)
	{
		int num = completed.Count((DisplayItemModel item) => item.IsAbandoned);
		CompletedSection completedSection = new CompletedSection();
		if (num == completed.Count)
		{
			completedSection.SetAbandonedOnly();
		}
		else if (num <= 0)
		{
			completedSection.SetCompletedOnly();
		}
		completedSection.Count = completed.Count;
		DisplayItemModel displayItemModel = DisplayItemModel.BuildSection(completedSection);
		if (completed.Count <= 0)
		{
			return;
		}
		displayItemModel.IsOpen = !IsSectionClosed(completedSection, sectionStatusModels);
		displayItemModel.Num = completed.Count;
		displayItemModel.Section = completedSection;
		if (showCompletedSection)
		{
			results.Add(displayItemModel);
		}
		completedSection.Children.Clear();
		int num2 = completed.Count;
		if (!loadAll && closedLoader != null && closedLoader.GetCompletedLimit() < num2)
		{
			num2 = closedLoader.GetCompletedLimit();
		}
		List<DisplayItemModel> modelsFromNodes = TaskNodeUtils.GetModelsFromNodes(TaskNodeUtils.GetSortedTaskNodeTree(completed, buildByChildIds));
		List<DisplayItemModel> list = new List<DisplayItemModel>();
		for (int num3 = 0; num3 < Math.Min(num2, modelsFromNodes.Count); num3++)
		{
			list.Add(modelsFromNodes[num3]);
		}
		modelsFromNodes = TaskNodeUtils.GetModelsFromNodes(TaskNodeUtils.GetSortedTaskNodeTree(list, buildByChildIds));
		completedSection.Children = modelsFromNodes.Where((DisplayItemModel child) => child.Level == 0).ToList();
		foreach (DisplayItemModel child in completedSection.Children)
		{
			child.Section = completedSection;
			child.Parent = displayItemModel;
			if (openedTaskIds != null)
			{
				child.IsOpen = openedTaskIds.Contains(child.Id);
			}
			if (!displayItemModel.IsOpen)
			{
				continue;
			}
			results.Add(child);
			if (!child.IsOpen)
			{
				continue;
			}
			child.GetChildrenModels(getAll: false, openedTaskIds).ForEach(delegate(DisplayItemModel child)
			{
				child.Section = completedSection;
				if (openedTaskIds != null)
				{
					child.IsOpen = openedTaskIds.Contains(child.Id);
				}
				results.Add(child);
			});
		}
		if (completed.Count > 0 && forceShowLoadMore && displayItemModel.IsOpen)
		{
			AddLoadMore(results, completedSection);
		}
		else if ((closedLoader == null || !closedLoader.CheckDrainOff(num2)) && !(!showLoadMore || completed.Count < 5 || loadAll) && displayItemModel.IsOpen)
		{
			AddLoadMore(results, completedSection);
		}
	}

	private static void AddLoadMore(ICollection<DisplayItemModel> results, CompletedSection completedSection)
	{
		DisplayItemModel displayItemModel = DisplayItemModel.BuildLoadMore();
		displayItemModel.Section = completedSection;
		results.Add(displayItemModel);
	}

	private static int CompareTaskDisplayType(DisplayType left, DisplayType right)
	{
		if ((left == DisplayType.Task || left == DisplayType.Note) && (right == DisplayType.Task || right == DisplayType.Note))
		{
			return 0;
		}
		if (left != right)
		{
			int num = (int)left;
			return num.CompareTo((int)right);
		}
		return 0;
	}

	private static int CompareModelOutDate(DisplayItemModel left, DisplayItemModel right)
	{
		if (left != null && right != null)
		{
			int num = CompareOutDate(left, right);
			if (num != 0)
			{
				return num;
			}
			int num2 = ComparePriority(left.Priority, right.Priority);
			if (num2 != 0)
			{
				return num2;
			}
			int num3 = CompareProjectOrder(left, right);
			if (num3 != 0)
			{
				return num3;
			}
			return CompareCustomSortOrder(left, right);
		}
		return 0;
	}

	private static int CompareModelDate(DisplayItemModel left, DisplayItemModel right, bool descending = false)
	{
		if (left != null && right != null)
		{
			int num = 0;
			bool flag = left.OutDate();
			bool flag2 = right.OutDate();
			num = ((flag && flag2) ? CompareOutDate(left, right, descending) : ((flag || flag2) ? (descending ? (flag ? 1 : (-1)) : ((!flag) ? 1 : (-1))) : CompareDate(left, right, descending)));
			if (num != 0)
			{
				return num;
			}
			int num2 = ComparePriority(left.Priority, right.Priority);
			if (num2 != 0)
			{
				return num2;
			}
			int num3 = CompareProjectOrder(left, right);
			if (num3 != 0)
			{
				return num3;
			}
			return CompareCustomSortOrder(left, right);
		}
		return 0;
	}

	private static int CompareCustomSortOrder(DisplayItemModel left, DisplayItemModel right)
	{
		if (left.IsItem && right.IsItem && left.ParentOrder != right.ParentOrder)
		{
			return left.ParentOrder.CompareTo(right.ParentOrder);
		}
		if (left.ProjectId != right.ProjectId)
		{
			ProjectModel projectById = CacheManager.GetProjectById(left.ProjectId);
			ProjectModel projectById2 = CacheManager.GetProjectById(right.ProjectId);
			if (projectById != null && projectById2 != null)
			{
				return projectById.CompareTo(projectById2);
			}
		}
		long sortOrder = left.SortOrder;
		long sortOrder2 = right.SortOrder;
		if (sortOrder < sortOrder2)
		{
			return -1;
		}
		if (sortOrder > sortOrder2)
		{
			return 1;
		}
		return 0;
	}

	private static int CompareProjectOrder(DisplayItemModel left, DisplayItemModel right)
	{
		if (left.ProjectId != right.ProjectId)
		{
			ProjectModel projectById = CacheManager.GetProjectById(left.ProjectId);
			ProjectModel projectById2 = CacheManager.GetProjectById(right.ProjectId);
			if (projectById != null && projectById2 != null)
			{
				return projectById.CompareTo(projectById2);
			}
		}
		return 0;
	}

	private static int ComparePriority(int left, int right)
	{
		if (left > right)
		{
			return -1;
		}
		if (left < right)
		{
			return 1;
		}
		return 0;
	}

	private static int CompareOutDate(DisplayItemModel left, DisplayItemModel right, bool descending = false)
	{
		int num = CompareOutDateCore(left, right);
		if (!descending)
		{
			return num;
		}
		return -num;
	}

	private static int CompareOutDateCore(DisplayItemModel left, DisplayItemModel right)
	{
		if (left.StartDate.HasValue && right.StartDate.HasValue)
		{
			DateTime dateTime = left.StartDate.Value;
			DateTime dateTime2 = right.StartDate.Value;
			bool flag = CheckAllDay(left);
			bool flag2 = CheckAllDay(right);
			if (left.DueDate.HasValue)
			{
				dateTime = left.DueDate.Value;
				if (flag)
				{
					dateTime = dateTime.Date.AddDays(-1.0);
				}
			}
			if (right.DueDate.HasValue)
			{
				dateTime2 = right.DueDate.Value;
				if (flag2)
				{
					dateTime2 = dateTime2.Date.AddDays(-1.0);
				}
			}
			if (dateTime.Date < dateTime2.Date)
			{
				return -1;
			}
			if (dateTime.Date > dateTime2.Date)
			{
				return 1;
			}
			if (flag && !flag2)
			{
				return 1;
			}
			if (!flag && flag2)
			{
				return -1;
			}
			if (dateTime < dateTime2)
			{
				return -1;
			}
			if (dateTime > dateTime2)
			{
				return 1;
			}
			DateTime value = left.StartDate.Value;
			DateTime value2 = right.StartDate.Value;
			if (value < value2)
			{
				return -1;
			}
			if (value > value2)
			{
				return 1;
			}
			return CompareTimeOrDuration(left, right);
		}
		return 0;
	}

	private static int CompareDate(DisplayItemModel left, DisplayItemModel right, bool descending = false)
	{
		if (left.StartDate.HasValue && right.StartDate.HasValue)
		{
			DateTime date = left.StartDate.Value.Date;
			DateTime date2 = right.StartDate.Value.Date;
			if (date < date2)
			{
				if (!descending)
				{
					return -1;
				}
				return 1;
			}
			if (date > date2)
			{
				if (!descending)
				{
					return 1;
				}
				return -1;
			}
			bool flag = CheckAllDay(left);
			bool flag2 = CheckAllDay(right);
			if (flag && !flag2)
			{
				if (!descending)
				{
					return 1;
				}
				return -1;
			}
			if (!flag && flag2)
			{
				if (!descending)
				{
					return -1;
				}
				return 1;
			}
			int num = left.StartDate.Value.CompareTo(right.StartDate.Value);
			if (num != 0)
			{
				if (!descending)
				{
					return num;
				}
				return -num;
			}
			DateTime dateTime = left.StartDate.Value.Date;
			DateTime dateTime2 = right.StartDate.Value.Date;
			if (left.DueDate.HasValue)
			{
				dateTime = left.DueDate.Value.Date;
				if (flag)
				{
					dateTime = dateTime.AddDays(-1.0);
				}
			}
			if (right.DueDate.HasValue)
			{
				dateTime2 = right.DueDate.Value.Date;
				if (flag2)
				{
					dateTime2 = dateTime2.AddDays(-1.0);
				}
			}
			if (dateTime < dateTime2)
			{
				if (!descending)
				{
					return -1;
				}
				return 1;
			}
			if (dateTime > dateTime2)
			{
				if (!descending)
				{
					return 1;
				}
				return -1;
			}
			int num2 = CompareTimeOrDuration(left, right);
			if (num2 != 0)
			{
				if (!descending)
				{
					return num2;
				}
				return -num2;
			}
			int num3 = ComparePriority(left.Priority, right.Priority);
			if (num3 != 0)
			{
				return num3;
			}
			int num4 = CompareCustomSortOrder(left, right);
			if (num4 != 0)
			{
				return num4;
			}
			int num5 = CompareTaskDisplayType(left.Type, right.Type);
			if (num5 != 0)
			{
				return num5;
			}
		}
		return 0;
	}

	private static bool CheckAllDay(DisplayItemModel model)
	{
		if (model.IsAllDay.HasValue)
		{
			return model.IsAllDay.Value;
		}
		return false;
	}

	private static int CompareTimeOrDuration(DisplayItemModel left, DisplayItemModel right)
	{
		if (left.StartDate.HasValue && right.StartDate.HasValue)
		{
			DateTime dateTime = left.StartDate.Value;
			DateTime dateTime2 = right.StartDate.Value;
			if (left.IsAllDay.HasValue && left.IsAllDay.Value)
			{
				dateTime = left.StartDate.Value.Date;
			}
			if (right.IsAllDay.HasValue && right.IsAllDay.Value)
			{
				dateTime2 = right.StartDate.Value.Date;
			}
			if (dateTime < dateTime2)
			{
				return -1;
			}
			if (dateTime > dateTime2)
			{
				return 1;
			}
			int num = 0;
			int num2 = 0;
			if (left.DueDate.HasValue)
			{
				num = (int)(left.DueDate.Value - left.StartDate.Value).TotalMinutes;
			}
			if (right.DueDate.HasValue)
			{
				num2 = (int)(right.DueDate.Value - right.StartDate.Value).TotalMinutes;
			}
			if (num < num2)
			{
				return -1;
			}
			if (num > num2)
			{
				return 1;
			}
		}
		return 0;
	}

	public static async Task<bool> ShowResetDate(string projectId)
	{
		ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == projectId);
		if (projectModel != null && projectModel.sortType == Constants.SortType.dueDate.ToString())
		{
			List<TaskSortOrderInDateModel> list = await TaskSortOrderInDateDao.GetSortOrderInDateByProjectId(projectId);
			if (list != null && list.Any())
			{
				return true;
			}
		}
		return false;
	}

	public static async Task<bool> ShowResetPriority(string projectId)
	{
		ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == projectId);
		if (projectModel != null && projectModel.sortType == Constants.SortType.priority.ToString())
		{
			List<TaskSortOrderInPriorityModel> list = await TaskSortOrderInPriorityDao.GetSortOrders(projectId);
			if (list != null && list.Any())
			{
				return true;
			}
		}
		return false;
	}

	public static async Task<bool> ShowResetTag(string projectId)
	{
		ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == projectId);
		if (projectModel != null && projectModel.sortType == Constants.SortType.tag.ToString())
		{
			return await SyncSortOrderDao.CountTagSortOrderByGroupId(projectId) > 0;
		}
		return false;
	}

	private static int GetTypePriority(DisplayItemModel model)
	{
		if (model.ProjectId == "8ac3038d93c54b80a67321b6a03df066")
		{
			return 1;
		}
		if (model.IsCourse)
		{
			return 2;
		}
		return 0;
	}
}
