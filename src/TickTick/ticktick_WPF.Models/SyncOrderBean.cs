using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace ticktick_WPF.Models;

public class SyncOrderBean
{
	[JsonProperty("orderByType")]
	public Dictionary<string, Dictionary<string, SortOrderData>> OrderByType { get; set; }

	public SyncOrderBean GetTaskOrderBean()
	{
		SyncOrderBean syncOrderBean = new SyncOrderBean();
		syncOrderBean.OrderByType = new Dictionary<string, Dictionary<string, SortOrderData>>();
		foreach (string item in OrderByType.Keys.ToList())
		{
			if (item.StartsWith("taskBy#"))
			{
				syncOrderBean.OrderByType[item] = OrderByType[item];
			}
		}
		return syncOrderBean;
	}

	public SyncOrderBean GetOtherOrderBean()
	{
		SyncOrderBean syncOrderBean = new SyncOrderBean();
		syncOrderBean.OrderByType = new Dictionary<string, Dictionary<string, SortOrderData>>();
		foreach (string item in OrderByType.Keys.ToList())
		{
			if (!item.StartsWith("taskBy#"))
			{
				syncOrderBean.OrderByType[item] = OrderByType[item];
			}
		}
		return syncOrderBean;
	}
}
