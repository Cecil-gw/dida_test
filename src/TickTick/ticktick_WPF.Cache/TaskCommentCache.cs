using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;

namespace ticktick_WPF.Cache;

public static class TaskCommentCache
{
	private static DateTime _lastCacheTime;

	private static ConcurrentDictionary<string, List<CommentModel>> _cache = new ConcurrentDictionary<string, List<CommentModel>>();

	public static async Task<ConcurrentDictionary<string, List<CommentModel>>> GetTaskCommentDict()
	{
		if ((DateTime.Now - _lastCacheTime).TotalSeconds < 300.0)
		{
			return _cache;
		}
		_lastCacheTime = DateTime.Now;
		List<CommentModel> list = await CommentDao.GetAllComments();
		if (list != null)
		{
			ConcurrentDictionary<string, List<CommentModel>> concurrentDictionary = new ConcurrentDictionary<string, List<CommentModel>>();
			foreach (CommentModel item in list)
			{
				string text = item?.taskSid;
				if (!string.IsNullOrEmpty(text))
				{
					if (concurrentDictionary.ContainsKey(text))
					{
						concurrentDictionary[text].Add(item);
						continue;
					}
					concurrentDictionary[text] = new List<CommentModel> { item };
				}
			}
			_cache = concurrentDictionary;
		}
		return _cache;
	}

	public static void OnCommentChanged(List<CommentModel> comments, bool isAdd = false)
	{
		_lastCacheTime = DateTime.Now.AddMinutes(-10.0);
	}

	public static void OnCommentDeleted(CommentModel comment)
	{
		_lastCacheTime = DateTime.Now.AddMinutes(-10.0);
	}

	public static List<CommentModel> GetTaskComments(string taskId)
	{
		if (_cache.ContainsKey(taskId))
		{
			return _cache[taskId];
		}
		return new List<CommentModel>();
	}
}
