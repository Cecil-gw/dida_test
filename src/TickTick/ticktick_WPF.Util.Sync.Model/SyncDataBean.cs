using System.Collections.Generic;

namespace ticktick_WPF.Util.Sync.Model;

public class SyncDataBean<T>
{
	private List<T> addeds = new List<T>();

	private List<T> updateds = new List<T>();

	private List<T> deleteds = new List<T>();

	public List<T> Addeds
	{
		get
		{
			return addeds;
		}
		set
		{
			addeds = value;
		}
	}

	public List<T> Updateds
	{
		get
		{
			return updateds;
		}
		set
		{
			updateds = value;
		}
	}

	public List<T> Deleteds
	{
		get
		{
			return deleteds;
		}
		set
		{
			deleteds = value;
		}
	}

	public bool IsEmpty()
	{
		List<T> list = Addeds;
		if (list != null && list.Count == 0)
		{
			List<T> list2 = Updateds;
			if (list2 != null && list2.Count == 0)
			{
				List<T> list3 = Deleteds;
				if (list3 == null)
				{
					return false;
				}
				return list3.Count == 0;
			}
		}
		return false;
	}
}
