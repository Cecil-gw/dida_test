using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using TickTickModels;

namespace TickTickDao;

public class BaseDao<T> where T : BaseModel, new()
{
	private class IdModel
	{
		public string Id { get; set; }
	}

	public static async Task<int> InsertAsync(T model)
	{
		return await Connection.DbConnection.InsertAsync(model);
	}

	public static async Task<int> InsertAllAsync(List<T> models)
	{
		return await Connection.DbConnection.InsertAllAsync(models);
	}

	public static async Task<int> UpdateAsync(T model)
	{
		return await Connection.DbConnection.UpdateAsync(model);
	}

	public static async Task<int> UpdateAllAsync(List<T> models)
	{
		return await Connection.DbConnection.UpdateAllAsync(models);
	}

	public static async Task<int> DeleteAsync(T model)
	{
		return await Connection.DbConnection.DeleteAsync(model);
	}

	public static async Task<List<T>> GetAllAsync()
	{
		return await Task.Run(async () => await Connection.DbConnection.Table<T>().ToListAsync());
	}

	public static async Task<List<T>> GetByExpressionAsync(Expression<Func<T, bool>> where)
	{
		return await Task.Run(async () => await Connection.DbConnection.Table<T>().Where(where).ToListAsync());
	}

	public static async Task<int> UpdateOrInsertModelListDbAsync(T t)
	{
		return await Task.Run(async delegate
		{
			_ = 2;
			try
			{
				if ((await (from v in Connection.DbConnection.Table<T>()
					where v._Id.Equals(t._Id)
					select v).ToListAsync()).Count != 0)
				{
					return await Connection.DbConnection.UpdateAsync(t);
				}
				await Connection.DbConnection.InsertAsync(t);
				return t._Id;
			}
			catch (Exception)
			{
				return -1;
			}
		});
	}

	public static async Task<bool> DeleteModelListDbAsync(int id)
	{
		return await Task.Run(async delegate
		{
			List<T> list = await (from v in Connection.DbConnection.Table<T>()
				where v._Id.Equals(id)
				select v).ToListAsync();
			try
			{
				await Connection.DbConnection.DeleteAsync(list[0]);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		});
	}

	protected static async Task<List<string>> GetEntityIds(string sql)
	{
		List<IdModel> list = await Connection.DbConnection.QueryAsync<IdModel>(sql, Array.Empty<object>());
		if (list != null && list.Count > 0)
		{
			return (from model in list
				select model.Id into id
				where !string.IsNullOrEmpty(id)
				select id).ToList();
		}
		return new List<string>();
	}

	protected static async Task<HashSet<string>> GetEntityIdSet(string sql)
	{
		HashSet<string> result = new HashSet<string>();
		List<IdModel> list = await Connection.DbConnection.QueryAsync<IdModel>(sql, Array.Empty<object>());
		if (list != null && list.Count > 0)
		{
			foreach (IdModel item in list)
			{
				result.Add(item.Id);
			}
		}
		return result;
	}
}
