using System.Collections.Generic;
using System.Linq;

namespace ticktick_WPF.Models;

public class TagExtra
{
	private Dictionary<string, List<string>> _data = new Dictionary<string, List<string>>();

	public TagExtra()
	{
	}

	public TagExtra(string taskId, List<string> tagList)
	{
		AddTaskTagsPair(taskId, tagList);
	}

	public void AddTaskTagsPair(string id, List<string> tags)
	{
		if (tags != null)
		{
			List<string> value = new List<string>(tags);
			_data[id] = value;
		}
		else
		{
			_data[id] = new List<string>();
		}
	}

	public bool ContainsId(string id)
	{
		if (!string.IsNullOrEmpty(id))
		{
			return _data.ContainsKey(id);
		}
		return false;
	}

	public List<string> GetIds()
	{
		if (_data.Any())
		{
			return _data.Keys.ToList();
		}
		return new List<string>();
	}

	public List<string> GetTags(string id)
	{
		if (_data.Any() && _data.ContainsKey(id))
		{
			return _data[id];
		}
		return new List<string>();
	}

	public bool Any()
	{
		return _data.Any();
	}
}
