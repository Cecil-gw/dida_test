using System;
using System.Collections.Generic;
using System.Linq;

namespace ticktick_WPF.Models;

public class BlockingSet<T>
{
	private readonly HashSet<T> _models = new HashSet<T>();

	public HashSet<T> Value
	{
		get
		{
			lock (_models)
			{
				return _models;
			}
		}
	}

	public int Count
	{
		get
		{
			lock (_models)
			{
				return _models.Count;
			}
		}
	}

	public BlockingSet()
	{
	}

	public BlockingSet(IEnumerable<T> models)
	{
		_models = new HashSet<T>(models);
	}

	public List<T> ToList()
	{
		lock (_models)
		{
			return _models.ToList();
		}
	}

	public bool Add(T model)
	{
		lock (_models)
		{
			return _models.Add(model);
		}
	}

	public void AddRange(IEnumerable<T> models, bool checkNull = false)
	{
		if (models == null)
		{
			return;
		}
		lock (_models)
		{
			foreach (T model in models)
			{
				if (!checkNull || model != null)
				{
					_models.Add(model);
				}
			}
		}
	}

	public void AddRange(BlockingSet<T> set)
	{
		AddRange(set?._models);
	}

	public List<TResult> Select<TResult>(Func<T, TResult> match)
	{
		lock (_models)
		{
			return _models.Select(match).ToList();
		}
	}

	public List<T> Where(Predicate<T> match)
	{
		lock (_models)
		{
			return _models.Where((T item) => match(item)).ToList();
		}
	}

	public List<T> RemoveAll(Predicate<T> match)
	{
		lock (_models)
		{
			List<T> list = _models.Where((T item) => match(item)).ToList();
			RemoveAll(list);
			return list;
		}
	}

	private void RemoveAll(IEnumerable<T> removeModels)
	{
		lock (_models)
		{
			foreach (T removeModel in removeModels)
			{
				_models.Remove(removeModel);
			}
		}
	}

	public void Clear()
	{
		lock (_models)
		{
			_models.Clear();
		}
	}

	public T FirstOrDefault(Func<T, bool> func)
	{
		lock (_models)
		{
			return _models.FirstOrDefault(func);
		}
	}

	public T FirstOrDefault()
	{
		lock (_models)
		{
			return _models.FirstOrDefault();
		}
	}

	public bool Contains(T m)
	{
		lock (_models)
		{
			return _models.Contains(m);
		}
	}

	public bool Remove(T m)
	{
		lock (_models)
		{
			return _models.Remove(m);
		}
	}

	public bool Any()
	{
		lock (_models)
		{
			return _models.Any();
		}
	}

	public BlockingSet<T> Copy()
	{
		lock (_models)
		{
			return new BlockingSet<T>(ToList());
		}
	}
}
