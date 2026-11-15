using System.Collections.Generic;
using System.Linq;

namespace ticktick_WPF.Util.ListDiff;

public class Diff<T>
{
	public Operation Operation { get; }

	public IReadOnlyList<T> Items { get; set; }

	public Diff(Operation operation, IReadOnlyList<T> items)
	{
		Operation = operation;
		Items = items;
	}

	public override string ToString()
	{
		string text = string.Join("", Items.Select((T t) => t.ToString())).Replace('\n', '¶');
		return "Diff(" + Operation.ToString() + ",\"" + text + "\")";
	}

	public override bool Equals(object obj)
	{
		if (!(obj is Diff<T> obj2))
		{
			return false;
		}
		return Equals(obj2);
	}

	public bool Equals(Diff<T> obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (obj.Operation == Operation)
		{
			return obj.Items.SequenceEqual(Items);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Items.GetHashCode() ^ Operation.GetHashCode();
	}
}
