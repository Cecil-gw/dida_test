using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TickTickDao;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Attachment;

namespace ticktick_WPF.Service;

public class CommentService
{
	private static readonly HashSet<string> PullingTaskIds = new HashSet<string>();

	private static readonly Dictionary<string, List<AttachmentModel>> UploadingAttachmentMap = new Dictionary<string, List<AttachmentModel>>();

	public static readonly Dictionary<string, RemoteCommentModel> NeedPostCommentList = new Dictionary<string, RemoteCommentModel>();

	public static async Task AddCommentUploadAttachment(string commentId, AttachmentModel attachment)
	{
		if (!UploadingAttachmentMap.ContainsKey(commentId))
		{
			UploadingAttachmentMap.Add(commentId, new List<AttachmentModel>());
		}
		UploadingAttachmentMap[commentId].Add(attachment);
		TryAddCommentOnAttachmentUploaded(commentId);
	}

	private static async void TryAddCommentOnAttachmentUploaded(string commentId)
	{
		if (!NeedPostCommentList.TryGetValue(commentId, out var value))
		{
			return;
		}
		List<AttachmentModel> list = UploadingAttachmentMap[commentId];
		if (list.Count == value.attachmentCount)
		{
			value.attachments = list?.Select((AttachmentModel it) => new AttachmentIdModel
			{
				id = it.id
			}).ToList();
			await Communicator.AddComment(value);
			NeedPostCommentList.Remove(commentId);
			UploadingAttachmentMap.Remove(commentId);
		}
	}

	public static async Task TryPullRemoteComments(string projectSid, string taskSid)
	{
		lock (PullingTaskIds)
		{
			if (string.IsNullOrEmpty(taskSid) || PullingTaskIds.Contains(taskSid))
			{
				return;
			}
			PullingTaskIds.Add(taskSid);
		}
		await MergeRemoteComments(projectSid, taskSid);
		lock (PullingTaskIds)
		{
			PullingTaskIds.Remove(taskSid);
		}
	}

	private static async Task MergeRemoteComments(string projectSid, string taskSid, bool needCheck = true)
	{
		if (await SyncStatusDao.ExistTaskMoveStatus(taskSid))
		{
			return;
		}
		bool remoteAdded = false;
		bool remoteChanged = false;
		bool remoteDeleted = false;
		List<CommentModel> localComments = await CommentDao.GetCommentsByTaskSidWithDeleted(taskSid);
		List<CommentModel> remoteComments = await Communicator.GetComments(projectSid, taskSid);
		UtilLog.Info($"MergeRemoteComments: P{projectSid} T{taskSid}, local :{localComments?.Count},remote :{remoteComments?.Count}");
		if (remoteComments == null)
		{
			return;
		}
		Dictionary<string, CommentModel> localDict = new Dictionary<string, CommentModel>();
		Dictionary<string, CommentModel> remoteDict = new Dictionary<string, CommentModel>();
		UserInfoModel userInfo = await UserManager.GetUserInfo(force: true);
		List<CommentModel> remoteDeletedModelList = new List<CommentModel>();
		List<RemoteCommentModel> needPostModelList = new List<RemoteCommentModel>();
		remoteComments?.ForEach(delegate(CommentModel remote)
		{
			if (!remoteDict.ContainsKey(remote.id))
			{
				remoteDict.Add(remote.id, remote);
			}
		});
		if (localComments != null)
		{
			foreach (CommentModel item in localComments)
			{
				if (item.id != null)
				{
					if (!localDict.ContainsKey(item.id))
					{
						localDict.Add(item.id, item);
					}
					if (item.syncStatus != 0 && !remoteDict.ContainsKey(item.id))
					{
						remoteDeletedModelList.Add(item);
					}
					if (!remoteDict.ContainsKey(item.id) && item.syncStatus == 0 && item.deleted != 1 && !NeedPostCommentList.ContainsKey(item.id))
					{
						needPostModelList.Add(new RemoteCommentModel(item));
					}
				}
			}
		}
		if (remoteComments.Count > 0)
		{
			foreach (CommentModel remoteComment in remoteComments)
			{
				if (!localDict.ContainsKey(remoteComment.id))
				{
					remoteAdded = true;
					remoteComment.taskSid = taskSid;
					remoteComment.projectSid = projectSid;
					remoteComment.deleted = 0;
					remoteComment.syncStatus = 2;
					if (remoteComment.userProfile != null)
					{
						UserProfile userProfile = remoteComment.userProfile;
						remoteComment.isMySelf = userProfile.isMyself == true;
						if (remoteComment.isMySelf)
						{
							remoteComment.userName = ((!string.IsNullOrEmpty(userInfo?.name)) ? userInfo.name : userInfo?.username);
							remoteComment.avatarUrl = userInfo?.picture;
						}
						else
						{
							remoteComment.avatarUrl = userProfile.avatarUrl;
							remoteComment.userName = (string.IsNullOrEmpty(userProfile.name) ? userProfile.username : userProfile.name);
						}
					}
					await CommentDao.SaveComment(remoteComment);
					AttachmentModel[] attachments = remoteComment.Attachments;
					if (attachments != null && attachments.Length != 0)
					{
						AttachmentModel[] attachments2 = remoteComment.Attachments;
						foreach (AttachmentModel attachment in attachments2)
						{
							attachment.taskId = remoteComment.id;
							await AttachmentDao.InsertOrUpdateAttachment(attachment);
							await AttachmentDownloadUtils.DownloadFileAttachment(attachment.id, commentAttachment: true);
						}
					}
				}
				else
				{
					CommentModel commentModel = localDict[remoteComment.id];
					bool flag = commentModel.isMySelf != (remoteComment.userProfile?.isMyself == true) || commentModel.modifiedTime != remoteComment.modifiedTime;
					remoteChanged = remoteChanged || flag;
					if (commentModel.syncStatus == 0 || flag)
					{
						commentModel.syncStatus = 2;
						commentModel.title = remoteComment.title;
						commentModel.isMySelf = remoteComment.userProfile?.isMyself == true;
						await CommentDao.UpdateComment(commentModel);
					}
				}
			}
		}
		if (remoteDeletedModelList.Count > 0)
		{
			remoteDeleted = true;
			foreach (CommentModel remoteComment in remoteDeletedModelList)
			{
				await CommentDao.DeleteComment(remoteComment);
				Communicator.DeleteComment(remoteComment.projectSid, remoteComment.taskSid, remoteComment.id);
			}
		}
		if (remoteAdded || remoteDeleted || remoteChanged)
		{
			await TaskService.SaveCommentCount(taskSid, sync: false);
		}
		if (!(needPostModelList.Any() && needCheck))
		{
			return;
		}
		foreach (RemoteCommentModel item2 in needPostModelList)
		{
			await Communicator.AddComment(item2);
		}
		await MergeRemoteComments(projectSid, taskSid, needCheck: false);
	}

	public static async Task<bool> MergeComments(IEnumerable<CommentModel> comments)
	{
		if (comments == null)
		{
			return false;
		}
		List<CommentModel> newCommentList = new List<CommentModel>();
		List<CommentModel> updateCommentList = new List<CommentModel>();
		List<CommentModel> localComments = await CommentDao.GetAllComments();
		Dictionary<string, CommentModel> localDict = new Dictionary<string, CommentModel>();
		UserInfoModel userInfo = await UserManager.GetUserInfo(force: true);
		foreach (CommentModel item in localComments)
		{
			if (item.id != null && !localDict.ContainsKey(item.id))
			{
				localDict.Add(item.id, item);
			}
		}
		foreach (CommentModel remoteComment in comments)
		{
			if (!localDict.ContainsKey(remoteComment.id))
			{
				TaskModel taskModel = await TaskDao.GetTaskById(remoteComment.taskSid);
				if (taskModel == null)
				{
					continue;
				}
				remoteComment.projectSid = taskModel.projectId;
				remoteComment.deleted = 0;
				remoteComment.syncStatus = 2;
				if (remoteComment.userProfile != null)
				{
					UserProfile userProfile = remoteComment.userProfile;
					remoteComment.isMySelf = userProfile.isMyself == true;
					if (remoteComment.isMySelf)
					{
						remoteComment.userName = ((!string.IsNullOrEmpty(userInfo?.name)) ? userInfo.name : userInfo?.username);
						remoteComment.avatarUrl = userInfo?.picture;
					}
					else
					{
						remoteComment.avatarUrl = userProfile.avatarUrl;
						remoteComment.userName = (string.IsNullOrEmpty(userProfile.name) ? userProfile.username : userProfile.name);
					}
				}
				newCommentList.Add(remoteComment);
			}
			else
			{
				CommentModel commentModel = localDict[remoteComment.id];
				if (commentModel.isMySelf != (remoteComment.userProfile?.isMyself == true) || commentModel.modifiedTime != remoteComment.modifiedTime)
				{
					commentModel.syncStatus = 2;
					commentModel.title = remoteComment.title;
					commentModel.isMySelf = remoteComment.userProfile?.isMyself == true;
					updateCommentList.Add(commentModel);
				}
			}
		}
		if (newCommentList.Count > 0)
		{
			await CommentDao.SaveComments(newCommentList);
		}
		if (updateCommentList.Count > 0)
		{
			await CommentDao.UpdateComments(updateCommentList);
		}
		return newCommentList.Count > 0 || updateCommentList.Count > 0;
	}

	public static async Task AddComment(CommentModel comment)
	{
		await CommentDao.SaveComment(comment);
		if (!comment.createdTime.HasValue)
		{
			comment.createdTime = DateTime.Now;
		}
		RemoteCommentModel remoteCommentModel = new RemoteCommentModel(comment);
		if (comment.mentions != null && comment.mentions.Count > 0)
		{
			remoteCommentModel.mentions = new List<Mention>();
			foreach (Mention mention in comment.mentions)
			{
				if (comment.title.Contains(mention.atLabel))
				{
					remoteCommentModel.mentions.Add(mention);
				}
			}
		}
		List<AttachmentModel> value;
		if (comment.attachmentCount == 0)
		{
			await Communicator.AddComment(remoteCommentModel);
		}
		else if (UploadingAttachmentMap.TryGetValue(comment.id, out value))
		{
			if (value.Count == comment.attachmentCount)
			{
				remoteCommentModel.attachments = value?.Select((AttachmentModel it) => new AttachmentIdModel
				{
					id = it.id
				}).ToList();
				await Communicator.AddComment(remoteCommentModel);
			}
			else
			{
				NeedPostCommentList.Add(comment.id, remoteCommentModel);
			}
		}
		else
		{
			NeedPostCommentList.Add(comment.id, remoteCommentModel);
		}
	}

	public static async Task DeleteComment(CommentModel comment)
	{
		comment.deleted = 1;
		await CommentDao.UpdateComment(comment);
		Communicator.DeleteComment(comment.projectSid, comment.taskSid, comment.id);
	}

	public static async Task UpdateComment(CommentModel model)
	{
		if (model == null)
		{
			return;
		}
		List<CommentModel> locals = await CommentDao.GetCommentsById(model.id);
		for (int i = 0; i < locals.Count; i++)
		{
			CommentModel commentModel = locals[i];
			if (i > 0)
			{
				await BaseDao<CommentModel>.DeleteAsync(commentModel);
				continue;
			}
			model._Id = commentModel._Id;
			model.syncStatus = 1;
			await CommentDao.UpdateComment(model);
			await Communicator.AddComment(new RemoteCommentModel(model), isUpdate: true);
		}
	}
}
