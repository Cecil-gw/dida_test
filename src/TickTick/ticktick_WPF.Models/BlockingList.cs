using System;
using System.Collections.Generic;
using System.Linq;

namespace ticktick_WPF.Models;

public class BlockingList<T>
{
	private readonly object _locker = new object();

	private readonly List<T> _models = new List<T>();

	public int Count
	{
		get
		{
			lock (_locker)
			{
				return _models.Count;
			}
		}
	}

	public List<T> Value => _models;

	public T this[int index]
	{
		get
		{
			lock (_locker)
			{
				return _models[index];
			}
		}
		set
		{
			lock (_locker)
			{
				T item = _models[index];
				_models[index] = value;
				this.ItemsChange?.Invoke(this, new ListItemChangeArgs<T>
				{
					Action = ListChangeAction.Replace,
					Items = new List<T> { value },
					OldItems = new List<T> { item }
				});
			}
		}
	}

	public event EventHandler<ListItemChangeArgs<T>> ItemsChange;

	public BlockingList()
	{
	}

	public BlockingList(IEnumerable<T> models)
	{
		_models = models.ToList();
	}

	public List<T> ToList()
	{
		lock (_locker)
		{
			return _models.ToList();
		}
	}

	public void Add(T model)
	{
		lock (_locker)
		{
			_models.Add(model);
			this.ItemsChange?.Invoke(this, new ListItemChangeArgs<T>
			{
				Action = ListChangeAction.Add,
				Items = new List<T> { model }
			});
		}
	}

	public void AddRange(IEnumerable<T> models)
	{
		lock (_locker)
		{
			_models.AddRange(models);
			this.ItemsChange?.Invoke(this, new ListItemChangeArgs<T>
			{
				Action = ListChangeAction.Add,
				Items = models.ToList()
			});
		}
	}

	public T RemoveAt(int index)
	{
		lock (_locker)
		{
			if (_models.Count <= index)
			{
				return default(T);
			}
			T val = _models[index];
			_models.RemoveAt(index);
			this.ItemsChange?.Invoke(this, new ListItemChangeArgs<T>
			{
				Action = ListChangeAction.Remove,
				Items = new List<T> { val }
			});
			return val;
		}
	}

	public List<TResult> SelectList<TResult>(Func<T, TResult> match)
	{
		lock (_locker)
		{
			return _models.Select(match).ToList();
		}
	}

	public List<T> Where(Predicate<T> match)
	{
		lock (_locker)
		{
			return _models.Where((T item) => match(item)).ToList();
		}
	}

	public List<T> RemoveAll(Predicate<T> match)
	{
		lock (_locker)
		{
			List<T> list = _models.FindAll(match);
			_models.RemoveAll(match);
			this.ItemsChange?.Invoke(this, new ListItemChangeArgs<T>
			{
				Action = ListChangeAction.Remove,
				Items = list
			});
			return list;
		}
	}

	public void Clear()
	{
		lock (_locker)
		{
			this.ItemsChange?.Invoke(this, new ListItemChangeArgs<T>
			{
				Action = ListChangeAction.Clear,
				Items = _models.ToList()
			});
			_models.Clear();
		}
	}

	public void Foreach(Action<T> func)
	{
		lock (_locker)
		{
			_models.ForEach(func);
		}
	}

	public T FirstOrDefault(Func<T, bool> func)
	{
		lock (_locker)
		{
			return _models.FirstOrDefault(func);
		}
	}

	public bool Contains(T m)
	{
		lock (_locker)
		{
			return _models.Contains(m);
		}
	}

	public void Remove(T m)
	{
		lock (_locker)
		{
			_models.Remove(m);
			this.ItemsChange?.Invoke(this, new ListItemChangeArgs<T>
			{
				Action = ListChangeAction.Remove,
				Items = new List<T> { m }
			});
		}
	}

	public bool All(Func<T, bool> func)
	{
		lock (_locker)
		{
			return _models.All(func);
		}
	}

	public int IndexOf(T model)
	{
		lock (_locker)
		{
			return _models.IndexOf(model);
		}
	}

	public void NotifyItemChanged(T model)
	{
		if (Contains(model))
		{
			this.ItemsChange?.Invoke(this, new ListItemChangeArgs<T>
			{
				Action = ListChangeAction.Change,
				Items = new List<T> { model }
			});
		}
	}

	public void NotifyItemsChanged(List<T> models)
	{
		this.ItemsChange?.Invoke(this, new ListItemChangeArgs<T>
		{
			Action = ListChangeAction.Change,
			Items = models
		});
	}

	public void ClearEvents()
	{
		this.ItemsChange = null;
	}

	public bool Exists(Predicate<T> match)
	{
		lock (_locker)
		{
			return _models.Any((T item) => match(item));
		}
	}

	public int GetCount(Predicate<T> match)
	{
		lock (_locker)
		{
			return _models.Count((T item) => match(item));
		}
	}

	public void Do(Action action)
	{
		lock (_locker)
		{
			action();
		}
	}

	public void Insert(int index, T model)
	{
		lock (_locker)
		{
			_models.Insert(index, model);
		}
	}

	public void Sort(Func<T, T, int> func)
	{
		lock (_locker)
		{
			_models.Sort((T a, T b) => func(a, b));
		}
	}
}
