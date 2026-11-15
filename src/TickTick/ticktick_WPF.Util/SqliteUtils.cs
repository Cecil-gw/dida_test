using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ticktick_WPF.Models;

namespace ticktick_WPF.Util;

public class SqliteUtils
{
	public static async Task<List<T>> BatchGetModelsAsync<T>(string sql, List<string> ids, int step = 50) where T : new()
	{
		if (ids == null || ids.Count == 0)
		{
			return new List<T>();
		}
		int num = 0;
		BlockingList<T> result = new BlockingList<T>();
		List<Task> list = new List<Task>();
		do
		{
			list.Add(GetModels(num, Math.Min(ids.Count, num + step)));
			num += step;
		}
		while (ids.Count > num);
		await Task.WhenAll(list);
		return result.Value;
		async Task GetModels(int start, int end)
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = start; i < end; i++)
			{
				stringBuilder.Append("'").Append(ids[i]).Append("'")
					.Append(",");
			}
			stringBuilder.Remove(stringBuilder.Length - 1, 1);
			List<T> list2 = await App.Connection.QueryAsync<T>(string.Format(sql, stringBuilder), Array.Empty<object>());
			if (list2 != null)
			{
				result.AddRange(list2);
			}
		}
	}
}
