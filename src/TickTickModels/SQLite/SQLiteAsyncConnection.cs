using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace SQLite;

public class SQLiteAsyncConnection
{
	private SQLiteConnectionString _connectionString;

	public SQLiteAsyncConnection(string databasePath, bool storeDateTimeAsTicks = false)
	{
		_connectionString = new SQLiteConnectionString(databasePath, storeDateTimeAsTicks);
	}

	private SQLiteConnectionWithLock GetConnection(SQLiteOpenFlags flags = SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create)
	{
		return SQLiteConnectionPool.Shared.GetConnection(_connectionString, flags);
	}

	public Task<CreateTablesResult> CreateTableAsync<T>() where T : new()
	{
		return CreateTablesAsync(typeof(T));
	}

	public Task<CreateTablesResult> CreateTablesAsync<T, T2>() where T : new() where T2 : new()
	{
		return CreateTablesAsync(typeof(T), typeof(T2));
	}

	public Task<CreateTablesResult> CreateTablesAsync<T, T2, T3>() where T : new() where T2 : new() where T3 : new()
	{
		return CreateTablesAsync(typeof(T), typeof(T2), typeof(T3));
	}

	public Task<CreateTablesResult> CreateTablesAsync<T, T2, T3, T4>() where T : new() where T2 : new() where T3 : new() where T4 : new()
	{
		return CreateTablesAsync(typeof(T), typeof(T2), typeof(T3), typeof(T4));
	}

	public Task<CreateTablesResult> CreateTablesAsync<T, T2, T3, T4, T5>() where T : new() where T2 : new() where T3 : new() where T4 : new() where T5 : new()
	{
		return CreateTablesAsync(typeof(T), typeof(T2), typeof(T3), typeof(T4), typeof(T5));
	}

	public async Task<CreateTablesResult> CreateTablesAsync(params Type[] types)
	{
		return await Task.Run(delegate
		{
			CreateTablesResult createTablesResult = new CreateTablesResult();
			SQLiteConnectionWithLock connection = GetConnection();
			using (connection.Lock())
			{
				Type[] array = types;
				foreach (Type type in array)
				{
					int value = connection.CreateTable(type);
					createTablesResult.Results[type] = value;
				}
				return createTablesResult;
			}
		});
	}

	public async Task<int> DropTableAsync<T>() where T : new()
	{
		return await Task.Run(delegate
		{
			SQLiteConnectionWithLock connection = GetConnection();
			using (connection.Lock())
			{
				return connection.DropTable<T>();
			}
		});
	}

	public async Task<int> InsertAsync(object item)
	{
		return await Task.Run(delegate
		{
			SQLiteConnectionWithLock connection = GetConnection();
			using (connection.Lock())
			{
				return connection.Insert(item);
			}
		});
	}

	public async Task<int> UpdateAsync(object item)
	{
		return await Task.Run(delegate
		{
			SQLiteConnectionWithLock connection = GetConnection();
			using (connection.Lock())
			{
				return connection.Update(item);
			}
		});
	}

	public async Task<int> DeleteAsync(object item)
	{
		return await Task.Run(delegate
		{
			SQLiteConnectionWithLock connection = GetConnection();
			using (connection.Lock())
			{
				return connection.Delete(item);
			}
		});
	}

	public async Task<T> GetAsync<T>(object pk) where T : new()
	{
		return await Task.Run(delegate
		{
			SQLiteConnectionWithLock connection = GetConnection(SQLiteOpenFlags.ReadOnly);
			using (connection.Lock())
			{
				return connection.Get<T>(pk);
			}
		});
	}

	public async Task<T> FindAsync<T>(object pk) where T : new()
	{
		return await Task.Run(delegate
		{
			SQLiteConnectionWithLock connection = GetConnection(SQLiteOpenFlags.ReadOnly);
			using (connection.Lock())
			{
				return connection.Find<T>(pk);
			}
		});
	}

	public async Task<T> GetAsync<T>(Expression<Func<T, bool>> predicate) where T : new()
	{
		return await Task.Run(delegate
		{
			SQLiteConnectionWithLock connection = GetConnection(SQLiteOpenFlags.ReadOnly);
			using (connection.Lock())
			{
				return connection.Get(predicate);
			}
		});
	}

	public async Task<T> FindAsync<T>(Expression<Func<T, bool>> predicate) where T : new()
	{
		return await Task.Run(delegate
		{
			SQLiteConnectionWithLock connection = GetConnection(SQLiteOpenFlags.ReadOnly);
			using (connection.Lock())
			{
				return connection.Find(predicate);
			}
		});
	}

	public async Task<int> ExecuteAsync(string query, params object[] args)
	{
		return await Task.Run(delegate
		{
			SQLiteConnectionWithLock connection = GetConnection();
			using (connection.Lock())
			{
				return connection.Execute(query, args);
			}
		});
	}

	public async Task<int> InsertAllAsync(IEnumerable items)
	{
		return await Task.Run(delegate
		{
			SQLiteConnectionWithLock connection = GetConnection();
			using (connection.Lock())
			{
				return connection.InsertAll(items);
			}
		});
	}

	public async Task<int> UpdateAllAsync(IEnumerable items)
	{
		return await Task.Run(delegate
		{
			SQLiteConnectionWithLock connection = GetConnection();
			using (connection.Lock())
			{
				return connection.UpdateAll(items);
			}
		});
	}

	[Obsolete("Will cause a deadlock if any call in action ends up in a different thread. Use RunInTransactionAsync(Action<SQLiteConnection>) instead.")]
	public async Task RunInTransactionAsync(Action<SQLiteAsyncConnection> action)
	{
		await Task.Run(delegate
		{
			SQLiteConnectionWithLock connection = GetConnection();
			using (connection.Lock())
			{
				connection.BeginTransaction();
				try
				{
					action(this);
					connection.Commit();
				}
				catch (Exception)
				{
					connection.Rollback();
					throw;
				}
			}
		});
	}

	public async Task RunInTransactionAsync(Action<SQLiteConnection> action)
	{
		await Task.Run(delegate
		{
			SQLiteConnectionWithLock connection = GetConnection();
			using (connection.Lock())
			{
				connection.BeginTransaction();
				try
				{
					action(connection);
					connection.Commit();
				}
				catch (Exception)
				{
					connection.Rollback();
					throw;
				}
			}
		});
	}

	public AsyncTableQuery<T> Table<T>() where T : new()
	{
		return new AsyncTableQuery<T>(GetConnection(SQLiteOpenFlags.ReadOnly).Table<T>());
	}

	public async Task<T> ExecuteScalarAsync<T>(string sql, params object[] args)
	{
		return await Task.Run(delegate
		{
			SQLiteConnectionWithLock connection = GetConnection();
			using (connection.Lock())
			{
				return connection.CreateCommand(sql, args).ExecuteScalar<T>();
			}
		});
	}

	public async Task<List<T>> QueryAsync<T>(string sql, params object[] args) where T : new()
	{
		return await Task.Run(delegate
		{
			SQLiteConnectionWithLock connection = GetConnection(SQLiteOpenFlags.ReadOnly);
			using (connection.Lock())
			{
				return connection.Query<T>(sql, args);
			}
		});
	}
}
