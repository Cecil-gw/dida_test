using System.Collections.Generic;
using Newtonsoft.Json;
using ticktick_WPF.Models;

namespace ticktick_WPF.Util.Sync;

public class SyncColumnBean
{
	private List<ColumnModel> update = new List<ColumnModel>();

	private List<ColumnModel> add = new List<ColumnModel>();

	private List<ColumnProjectModel> deleted = new List<ColumnProjectModel>();

	[JsonProperty(PropertyName = "update")]
	public List<ColumnModel> Update
	{
		get
		{
			return update;
		}
		set
		{
			update = value;
		}
	}

	[JsonProperty(PropertyName = "add")]
	public List<ColumnModel> Add
	{
		get
		{
			return add;
		}
		set
		{
			add = value;
		}
	}

	[JsonProperty(PropertyName = "delete")]
	public List<ColumnProjectModel> Deleted
	{
		get
		{
			return deleted;
		}
		set
		{
			deleted = value;
		}
	}
}
