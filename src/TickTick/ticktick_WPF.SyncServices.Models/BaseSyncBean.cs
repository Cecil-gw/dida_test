using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace ticktick_WPF.SyncServices.Models;

public class BaseSyncBean<T>
{
	[JsonProperty("add")]
	public List<T> Add { get; set; } = new List<T>();

	[JsonProperty("update")]
	public List<T> Update { get; set; } = new List<T>();

	[JsonProperty("delete")]
	public List<string> Delete { get; set; } = new List<string>();

	[JsonIgnore]
	public bool HasAdd => Add.Any();

	[JsonIgnore]
	public bool HasUpdate => Update.Any();

	[JsonIgnore]
	public bool HasDelete => Delete.Any();

	public BaseSyncBean()
	{
	}

	public BaseSyncBean(ICollection<T> add = null, ICollection<T> update = null, ICollection<string> delete = null)
	{
		if (add != null)
		{
			Add = add.ToList();
		}
		if (update != null)
		{
			Update = update.ToList();
		}
		if (delete != null)
		{
			Delete = delete.ToList();
		}
	}

	public BaseSyncBean(IEnumerable<T> add = null, IEnumerable<T> update = null, IEnumerable<string> delete = null)
	{
		if (add != null)
		{
			Add = add.ToList();
		}
		if (update != null)
		{
			Update = update.ToList();
		}
		if (delete != null)
		{
			Delete = delete.ToList();
		}
	}

	public bool Any()
	{
		if (!HasAdd && !HasUpdate)
		{
			return HasDelete;
		}
		return true;
	}

	public int Count()
	{
		return Add.Count + Update.Count + Delete.Count;
	}

	public int AddOrUpdateCount()
	{
		return Add.Count + Update.Count;
	}

	public int DeleteCount()
	{
		return Delete.Count;
	}
}
