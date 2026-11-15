using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;

namespace ticktick_WPF.Cache;

public class TagCache : CacheBase<TagModel>
{
	public override async Task Load()
	{
		List<TagModel> list = await TagDao.GetAllTags();
		CheckTagSortOrders(list);
		AssembleData(list, (TagModel tag) => tag.name);
	}

	private async Task CheckTagSortOrders(List<TagModel> tags)
	{
		List<TagModel> list = tags;
		if (list == null || list.Count <= 0)
		{
			return;
		}
		tags = tags.OrderBy((TagModel p) => p.sortOrder).ToList();
		long? num = null;
		long? num2 = null;
		List<TagModel> needResort = new List<TagModel>();
		foreach (TagModel tag in tags)
		{
			TagModel t = needResort.LastOrDefault();
			if (t != null)
			{
				if (Math.Abs(tag.sortOrder - t.sortOrder) < 4)
				{
					needResort.Add(tag);
					continue;
				}
				if (needResort.Count > 1)
				{
					if (!num.HasValue || tag.sortOrder > num.Value)
					{
						num2 = tag.sortOrder;
					}
					num = num ?? (num2.Value - 268435456);
					num2 = num2 ?? (num.Value + 268435456);
					long num3 = (num2.Value - num.Value) / (needResort.Count + 1);
					for (int num4 = 0; num4 < needResort.Count; num4++)
					{
						TagModel tagModel = needResort[num4];
						tagModel.sortOrder = num.Value + num3 * (num4 + 1);
						tagModel.status = ((tagModel.status == 2) ? 1 : tagModel.status);
					}
					await App.Connection.UpdateAllAsync(needResort);
				}
				needResort.Clear();
				needResort.Add(tag);
				num = t.sortOrder;
				num2 = null;
			}
			else
			{
				needResort.Add(tag);
			}
		}
		if (needResort.Count > 1)
		{
			if (!num.HasValue)
			{
				num2 = needResort[0].sortOrder - 268435456;
				num = needResort[0].sortOrder - 268435456;
			}
			long num5 = (new long?(num2 ?? (num.Value + 268435456)).Value - num.Value) / (needResort.Count + 1);
			for (int num6 = 0; num6 < needResort.Count; num6++)
			{
				TagModel tagModel2 = needResort[num6];
				tagModel2.sortOrder = num.Value + num5 * (num6 + 1);
				tagModel2.status = ((tagModel2.status == 2) ? 1 : tagModel2.status);
			}
			await App.Connection.UpdateAllAsync(needResort);
		}
	}
}
