using System;
using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util;

public static class TaskPinUtils
{
	[Obsolete]
	public static List<DisplayItemModel> GetPinnedInModels(List<DisplayItemModel> models, HashSet<string> pinnedIds)
	{
		List<DisplayItemModel> list = new List<DisplayItemModel>();
		Dictionary<string, List<DisplayItemModel>> parentChildrenDict = new Dictionary<string, List<DisplayItemModel>>();
		foreach (DisplayItemModel item in models.Where((DisplayItemModel model) => model.Status == 0))
		{
			if (!string.IsNullOrEmpty(item.ParentId))
			{
				if (parentChildrenDict.ContainsKey(item.ParentId))
				{
					parentChildrenDict[item.ParentId].Add(item);
				}
				else
				{
					parentChildrenDict[item.ParentId] = new List<DisplayItemModel> { item };
				}
			}
			if (item.IsPinned)
			{
				list.Add(item);
			}
		}
		int count = list.Count;
		for (int num = 0; num < count; num++)
		{
			DisplayItemModel displayItemModel = list[num];
			if (pinnedIds.Contains(displayItemModel.Id))
			{
				continue;
			}
			pinnedIds.Add(displayItemModel.Id);
			List<DisplayItemModel> list2 = new List<DisplayItemModel>();
			HashSet<string> childIds = new HashSet<string>();
			GetAllChildren(displayItemModel, list2, childIds);
			foreach (DisplayItemModel item2 in list2)
			{
				if (!pinnedIds.Contains(item2.Id))
				{
					list.Add(item2);
					pinnedIds.Add(item2.Id);
				}
			}
		}
		return list;
		void GetAllChildren(DisplayItemModel model, List<DisplayItemModel> children, HashSet<string> hashSet)
		{
			if (parentChildrenDict.ContainsKey(model.Id))
			{
				foreach (DisplayItemModel item3 in parentChildrenDict[model.Id])
				{
					if (!hashSet.Contains(item3.Id) && item3.IsPinned)
					{
						hashSet.Add(item3.Id);
						children.Add(item3);
						GetAllChildren(item3, children, hashSet);
					}
				}
			}
		}
	}
}
