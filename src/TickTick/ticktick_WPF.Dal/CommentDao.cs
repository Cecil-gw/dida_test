using System.Collections.Generic;
using System.Threading.Tasks;
using TickTickDao;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;

namespace ticktick_WPF.Dal;

public class CommentDao : BaseDao<CommentModel>
{
	public static async Task<CommentModel> SaveComment(CommentModel comment)
	{
		await App.Connection.InsertAsync(comment);
		TaskCommentCache.OnCommentChanged(new List<CommentModel> { comment }, isAdd: true);
		return comment;
	}

	public static async Task SaveComments(List<CommentModel> comments)
	{
		if (comments != null)
		{
			await App.Connection.InsertAllAsync(comments);
			TaskCommentCache.OnCommentChanged(comments, isAdd: true);
		}
	}

	public static async Task UpdateComments(List<CommentModel> comments)
	{
		if (comments != null)
		{
			await App.Connection.UpdateAllAsync(comments);
			TaskCommentCache.OnCommentChanged(comments);
		}
	}

	public static async Task UpdateComment(CommentModel comment)
	{
		await App.Connection.UpdateAsync(comment);
		TaskCommentCache.OnCommentChanged(new List<CommentModel> { comment });
	}

	public static async Task<List<CommentModel>> GetCommentsByTaskId(string taskSid)
	{
		return await (from v in App.Connection.Table<CommentModel>()
			where v.taskSid == taskSid && v.deleted == 0
			select v).ToListAsync();
	}

	public static async Task<List<CommentModel>> GetCommentsByTaskSidWithDeleted(string taskSid)
	{
		return await (from v in App.Connection.Table<CommentModel>()
			where v.taskSid == taskSid
			select v).ToListAsync();
	}

	public static async Task DeleteComment(CommentModel model)
	{
		await App.Connection.DeleteAsync(model);
		TaskCommentCache.OnCommentDeleted(model);
	}

	public static async void ChangeCommentProjectId(TaskModel task, string projectId)
	{
		List<CommentModel> list = await GetCommentsByTaskSidWithDeleted(task.id);
		if (list != null)
		{
			list.ForEach(delegate(CommentModel c)
			{
				c.projectSid = projectId;
			});
			await App.Connection.UpdateAllAsync(list);
		}
	}

	public static async Task<List<CommentModel>> GetAllComments()
	{
		return await (from v in App.Connection.Table<CommentModel>()
			where v.deleted == 0
			select v).ToListAsync();
	}

	public static async Task<List<CommentModel>> GetCommentsById(string modelId)
	{
		return await (from v in App.Connection.Table<CommentModel>()
			where v.id == modelId && v.deleted == 0
			select v).ToListAsync();
	}
}
