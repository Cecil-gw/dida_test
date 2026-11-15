using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace SQLite;

public class AsyncTableQuery<T> where T : new()
{
	private TableQuery<T> _innerQuery;

	public AsyncTableQuery(TableQuery<T> innerQuery)
	{
		_innerQuery = innerQuery;
	}

	public AsyncTableQuery<T> Where(Expression<Func<T, bool>> predExpr)
	{
		return new AsyncTableQuery<T>(_innerQuery.Where(predExpr));
	}

	public AsyncTableQuery<T> Skip(int n)
	{
		return new AsyncTableQuery<T>(_innerQuery.Skip(n));
	}

	public AsyncTableQuery<T> Take(int n)
	{
		return new AsyncTableQuery<T>(_innerQuery.Take(n));
	}

	public AsyncTableQuery<T> OrderBy<U>(Expression<Func<T, U>> orderExpr)
	{
		return new AsyncTableQuery<T>(_innerQuery.OrderBy(orderExpr));
	}

	public AsyncTableQuery<T> OrderByDescending<U>(Expression<Func<T, U>> orderExpr)
	{
		return new AsyncTableQuery<T>(_innerQuery.OrderByDescending(orderExpr));
	}

	public async Task<List<T>> ToListAsync()
	{
		return await Task.Run(delegate
		{
			using (((SQLiteConnectionWithLock)_innerQuery.Connection).Lock())
			{
				return _innerQuery.ToList();
			}
		});
	}

	public async Task<int> CountAsync()
	{
		return await Task.Run(delegate
		{
			using (((SQLiteConnectionWithLock)_innerQuery.Connection).Lock())
			{
				return _innerQuery.Count();
			}
		});
	}

	public async Task<T> ElementAtAsync(int index)
	{
		return await Task.Run(delegate
		{
			using (((SQLiteConnectionWithLock)_innerQuery.Connection).Lock())
			{
				return _innerQuery.ElementAt(index);
			}
		});
	}

	public async Task<T> FirstAsync()
	{
		return await Task.Run(delegate
		{
			using (((SQLiteConnectionWithLock)_innerQuery.Connection).Lock())
			{
				return _innerQuery.First();
			}
		});
	}

	public async Task<T> FirstOrDefaultAsync()
	{
		return await Task.Run(delegate
		{
			using (((SQLiteConnectionWithLock)_innerQuery.Connection).Lock())
			{
				return _innerQuery.FirstOrDefault();
			}
		});
	}
}
