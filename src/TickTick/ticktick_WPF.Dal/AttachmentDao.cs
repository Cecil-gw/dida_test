using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TickTickDao;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Attachment;
using ticktick_WPF.Util.Files;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Util.Sync.Model;

namespace ticktick_WPF.Dal;

public class AttachmentDao : BaseDao<AttachmentModel>
{
	private static TTAsyncLocker _asyncLocker = new TTAsyncLocker(1, 1);

	public static string GetInternalAttachmentId(string url)
	{
		if (string.IsNullOrWhiteSpace(url) || Uri.IsWellFormedUriString(url, UriKind.Absolute))
		{
			return null;
		}
		int num = url.IndexOf('/');
		if (num <= 0)
		{
			return null;
		}
		string text = url.Substring(0, num);
		if (text.Length != 24 || !text.All(Uri.IsHexDigit))
		{
			return null;
		}
		return text;
	}

	public static async Task InsertOrUpdateAttachment(AttachmentModel attachment)
	{
		AttachmentModel attachmentModel = await GetAttachmentById(attachment.id);
		if (attachmentModel == null)
		{
			await App.Connection.InsertAsync(attachment);
			AttachmentCache.SetAttachment(await GetAttachmentById(attachment.id));
			return;
		}
		attachment._Id = attachmentModel._Id;
		attachment.localPath = attachmentModel.localPath;
		AttachmentCache.SetAttachment(attachment);
		await App.Connection.UpdateAsync(attachment);
	}

	public static async Task UpdateAttachmentById(AttachmentModel attachment, int localId)
	{
		AttachmentModel attachmentModel = await GetAttachmentByLocalId(localId);
		if (attachmentModel != null)
		{
			attachment._Id = attachmentModel._Id;
			AttachmentCache.SetAttachment(attachment);
			await App.Connection.UpdateAsync(attachment);
		}
	}

	public static async Task UpdateAttachment(AttachmentModel attachment)
	{
		AttachmentModel attachmentModel = await GetAttachmentById(attachment.id);
		if (attachmentModel != null)
		{
			attachment._Id = attachmentModel._Id;
			AttachmentCache.SetAttachment(attachment);
			await App.Connection.UpdateAsync(attachment);
		}
	}

	public static async Task UpdateUploadAttachment(AttachmentModel attachment)
	{
		AttachmentModel attachmentModel = await GetAttachmentById(attachment.id);
		if (attachmentModel != null)
		{
			attachmentModel.path = attachment.path;
			AttachmentCache.SetAttachment(attachmentModel);
			await App.Connection.UpdateAsync(attachmentModel);
		}
	}

	public static async Task<AttachmentModel> GetAttachmentByLocalId(int id)
	{
		return await (from t in App.Connection.Table<AttachmentModel>()
			where t._Id == id
			select t).FirstOrDefaultAsync();
	}

	public static async Task<AttachmentModel> GetAttachmentById(string id)
	{
		return await (from t in App.Connection.Table<AttachmentModel>()
			where t.id == id
			select t).FirstOrDefaultAsync();
	}

	private static async Task<List<AttachmentModel>> GetAttachmentsById(string id)
	{
		return await (from t in App.Connection.Table<AttachmentModel>()
			where t.id == id
			select t).ToListAsync();
	}

	public static async Task<List<AttachmentModel>> GetTaskAttachments(string taskId, bool active = false)
	{
		if (active)
		{
			return await (from v in App.Connection.Table<AttachmentModel>()
				where v.taskId == taskId && v.deleted != true && v.status == 0
				select v).ToListAsync();
		}
		return await (from v in App.Connection.Table<AttachmentModel>()
			where v.taskId == taskId && v.deleted != true
			select v).ToListAsync();
	}

	public static async Task<List<AttachmentModel>> GetTaskAttachmentsInTasks(List<string> taskIds)
	{
		return await (from v in App.Connection.Table<AttachmentModel>()
			where taskIds.Contains(v.taskId) && v.deleted != true
			select v).ToListAsync();
	}

	public static async Task<int> GetTaskAttachmentCount(string taskId)
	{
		int taskAttachmentCount = await (from v in App.Connection.Table<AttachmentModel>()
			where v.taskId == taskId && v.deleted != true && v.status == 0
			select v).CountAsync();
		List<CommentModel> list = await CommentDao.GetCommentsByTaskId(taskId);
		if (list != null && list.Any())
		{
			foreach (CommentModel item in list)
			{
				taskAttachmentCount += await GetTaskAttachmentCount(item.id);
			}
		}
		return taskAttachmentCount;
	}

	public static async Task<List<AttachmentModel>> GetAllAttachments()
	{
		return await (from v in App.Connection.Table<AttachmentModel>()
			where v.deleted != true
			select v).ToListAsync();
	}

	public static async Task<List<AttachmentModel>> GetNeedUpdateAttachments()
	{
		return await (from v in App.Connection.Table<AttachmentModel>()
			where v.deleted != true && (v.sync_status == "0" || v.sync_status == "1")
			select v).ToListAsync();
	}

	public static async Task FakeDeleteAttachment(string id)
	{
		List<AttachmentModel> list = await GetAttachmentsById(id);
		if (list == null || list.Count <= 0)
		{
			return;
		}
		foreach (AttachmentModel item in list)
		{
			item.deleted = true;
			AttachmentCache.SetAttachment(item);
			await App.Connection.UpdateAsync(item);
		}
	}

	public static async Task SaveServerMergeToDb(AttachmentSyncBean attachmentSyncBean)
	{
		List<AttachmentModel> attachmentsToRecover = new List<AttachmentModel>();
		await _asyncLocker.RunAsync(async delegate
		{
			foreach (AttachmentModel deleted in attachmentSyncBean.Deleted)
			{
				if (!HasPendingLocalAttachmentChange(await GetAttachmentById(deleted.id)))
				{
					await FakeDeleteAttachment(deleted.id);
				}
			}
			foreach (AttachmentModel deleted in attachmentSyncBean.Added)
			{
				if (!HasPendingLocalAttachmentChange(await GetAttachmentById(deleted.id)))
				{
					await InsertOrUpdateAttachment(deleted);
				}
			}
			foreach (AttachmentModel deleted in attachmentSyncBean.Updated)
			{
				if (!HasPendingLocalAttachmentChange(await GetAttachmentById(deleted.id)))
				{
					await UpdateAttachment(deleted);
					attachmentsToRecover.Add(deleted);
				}
			}
			await AttachmentCache.ResetDictItems();
		});
		foreach (AttachmentModel item in attachmentsToRecover)
		{
			await TryRecoverRemoteAttachment(item);
		}
	}

	private static bool HasPendingLocalAttachmentChange(AttachmentModel attachment)
	{
		if (attachment != null)
		{
			if (!(attachment.sync_status == 0.ToString()))
			{
				return attachment.sync_status == 1.ToString();
			}
			return true;
		}
		return false;
	}

	public static async Task TryRecoverRemoteAttachment(AttachmentModel attachment)
	{
		if (attachment != null && !string.IsNullOrEmpty(attachment.id) && !string.IsNullOrEmpty(attachment.path))
		{
			AttachmentDownloadUtils.ClearNoPathDownloadAttempt(attachment.id);
			AttachmentLoadHelper.ClearFailed(attachment.id);
			if (attachment.status == 0 && !(attachment.fileType != Constants.AttachmentKind.IMAGE.ToString()) && FileUtils.FileEmptyOrNotExists(attachment.localPath))
			{
				await AttachmentDownloadUtils.AutoDownloadImgAttachment(attachment);
			}
		}
	}

	public static async Task CheckTaskAttachment(string taskId, IReadOnlyCollection<string> referencedAttachmentIds, string persistedText, bool deactivateMissingAttachments)
	{
		if (string.IsNullOrEmpty(taskId))
		{
			return;
		}
		await _asyncLocker.RunAsync(async delegate
		{
			string log = string.Empty;
			TaskModel task = await TaskDao.GetThinTaskById(taskId);
			if (task != null && task.kind != "CHECKLIST" && HasSamePersistedContent(task.content, persistedText))
			{
				List<AttachmentModel> attachments = await GetTaskAttachments(taskId);
				bool flag = false;
				foreach (AttachmentModel item in attachments)
				{
					if (!item.deleted)
					{
						string id = item.id;
						bool parsedReferenceExists = HasParsedAttachmentReference(referencedAttachmentIds, id);
						bool rawTargetExists = HasRawAttachmentTarget(persistedText, id);
						int? nextAttachmentStatus = GetNextAttachmentStatus(item.status, parsedReferenceExists, rawTargetExists);
						if (nextAttachmentStatus == 0 || (deactivateMissingAttachments && nextAttachmentStatus == 1))
						{
							item.status = nextAttachmentStatus.Value;
							if (item.sync_status != 0.ToString())
							{
								item.sync_status = 1.ToString();
							}
							string text = ((nextAttachmentStatus == 0) ? "active" : "inactive");
							log = log + text + " attachmentId: " + item.id + " taskId:" + item.taskId + " \r\n";
							await InsertOrUpdateAttachment(item);
							flag = true;
						}
					}
				}
				if (flag)
				{
					await AttachmentCache.ResetDictItems();
					TaskChangeNotifier.NotifyAttachmentChanged(taskId, null);
					int count = attachments.Count((AttachmentModel a) => a.IsActive());
					TaskDao.UpdateTaskAttachmentCount(task.id, count);
					SyncManager.TryDelaySync();
				}
			}
			if (!string.IsNullOrEmpty(log))
			{
				UtilLog.Info(log);
			}
		});
	}

	private static bool HasParsedAttachmentReference(IReadOnlyCollection<string> referencedAttachmentIds, string attachmentId)
	{
		if (string.IsNullOrEmpty(attachmentId))
		{
			return false;
		}
		return referencedAttachmentIds?.Any((string id) => string.Equals(id, attachmentId, StringComparison.OrdinalIgnoreCase)) ?? false;
	}

	private static bool HasRawAttachmentTarget(string persistedText, string attachmentId)
	{
		if (!string.IsNullOrEmpty(attachmentId) && !string.IsNullOrEmpty(persistedText))
		{
			return persistedText.IndexOf("(" + attachmentId, StringComparison.Ordinal) >= 0;
		}
		return false;
	}

	private static bool HasSamePersistedContent(string currentContent, string expectedContent)
	{
		return string.Equals(NormalizeNewLines(currentContent), NormalizeNewLines(expectedContent), StringComparison.Ordinal);
	}

	private static string NormalizeNewLines(string text)
	{
		return (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
	}

	private static int? GetNextAttachmentStatus(int currentStatus, bool parsedReferenceExists, bool rawTargetExists)
	{
		if (currentStatus == 0 && !parsedReferenceExists && !rawTargetExists)
		{
			return 1;
		}
		if (currentStatus == 1 && parsedReferenceExists)
		{
			return 0;
		}
		return null;
	}

	public static async Task CopyAttachment(AttachmentModel otherAttachment, string newId, string editingTaskId)
	{
		AttachmentModel attachmentModel = AttachmentModel.Copy(otherAttachment);
		attachmentModel.taskId = editingTaskId;
		attachmentModel.id = newId;
		attachmentModel._Id = 0;
		attachmentModel.deleted = false;
		attachmentModel.status = 0;
		attachmentModel.sync_status = 0.ToString();
		attachmentModel.createdTime = DateTime.Now;
		await InsertOrUpdateAttachment(attachmentModel);
		await SyncStatusDao.AddSyncStatus(editingTaskId, 0);
	}

	public static async Task<bool> DownloadAttachmentDone(string id, AsyncCompletedEventArgs e)
	{
		AttachmentModel attachmentModel = await GetAttachmentById(id);
		if (e.Error != null)
		{
			try
			{
				if (File.Exists(attachmentModel.localPath))
				{
					FileInfo fileInfo = new FileInfo(attachmentModel.localPath);
					fileInfo.Attributes = FileAttributes.Normal;
					fileInfo.Delete();
				}
			}
			catch (Exception)
			{
			}
			attachmentModel.sync_status = 2.ToString();
			attachmentModel.localPath = string.Empty;
			await UpdateAttachment(attachmentModel);
			return false;
		}
		attachmentModel.sync_status = 2.ToString();
		await UpdateAttachment(attachmentModel);
		return true;
	}

	public static async Task<string> AddAttachmentStrings(string taskTaskId, string content)
	{
		if (TaskCache.GetTaskById(taskTaskId) == null)
		{
			return content;
		}
		List<AttachmentModel> list = await GetTaskAttachments(taskTaskId, active: true);
		if (list != null && list.Count > 0)
		{
			content = AddAttachmentStrings(content, list);
		}
		return content;
	}

	public static string AddAttachmentStrings(string content, List<AttachmentModel> attachments, bool preserveOrder = false)
	{
		content = content ?? string.Empty;
		if (!preserveOrder)
		{
			attachments.Sort((AttachmentModel a, AttachmentModel b) => (a.createdTime.HasValue && b.createdTime.HasValue) ? b.createdTime.Value.CompareTo(a.createdTime.Value) : 0);
		}
		content = content.Replace("\r\n", "\n").Replace("\r", "\n");
		foreach (AttachmentModel attachment in attachments)
		{
			if (!attachment.deleted && attachment.status == 0)
			{
				string text = (string.IsNullOrEmpty(attachment.fileType) ? AttachmentProvider.GetFileType(attachment.fileName).ToString() : attachment.fileType);
				string text2 = "![" + ((text == Constants.AttachmentKind.IMAGE.ToString()) ? "image" : "file") + "](" + attachment.id + "/" + Utils.UrlEncode(attachment.fileName) + ")";
				if (!content.StartsWith(text2 + "\n") && !content.EndsWith("\n" + text2) && !content.Contains("\n" + text2 + "\n"))
				{
					UtilLog.Info(content.Replace("\n", "==="));
					UtilLog.Info(text2);
					content = ((!string.IsNullOrEmpty(content) && !content.EndsWith("\n")) ? (content + "\n" + text2) : (content + text2));
				}
			}
		}
		return content;
	}

	public static async Task HandleCommitResult(List<TaskModel> tasks, BatchUpdateResult updatedResult)
	{
		if (updatedResult == null)
		{
			return;
		}
		await _asyncLocker.RunAsync(async delegate
		{
			foreach (TaskModel task in tasks)
			{
				if ((updatedResult.Id2error == null || !updatedResult.Id2error.ContainsKey(task.id)) && updatedResult.Id2etag != null && updatedResult.Id2etag.ContainsKey(task.id))
				{
					AttachmentModel[] attachments = task.Attachments;
					foreach (AttachmentModel committedAttachment in attachments)
					{
						AttachmentModel attachmentModel = await GetAttachmentById(committedAttachment.id);
						if (CanMarkAttachmentCommitDone(attachmentModel, committedAttachment))
						{
							attachmentModel.sync_status = 2.ToString();
							AttachmentCache.SetAttachment(attachmentModel);
							await App.Connection.UpdateAsync(attachmentModel);
						}
					}
				}
			}
		});
	}

	private static bool CanMarkAttachmentCommitDone(AttachmentModel currentAttachment, AttachmentModel committedAttachment)
	{
		if (currentAttachment != null && committedAttachment != null && string.Equals(currentAttachment.id, committedAttachment.id, StringComparison.Ordinal) && string.Equals(currentAttachment.taskId, committedAttachment.taskId, StringComparison.Ordinal) && string.Equals(currentAttachment.sync_status, committedAttachment.sync_status, StringComparison.Ordinal) && currentAttachment.status == committedAttachment.status)
		{
			return currentAttachment.deleted == committedAttachment.deleted;
		}
		return false;
	}

	public static AttachmentModel GetAttachmentInUrl(string url)
	{
		string[] array = url.Split('/');
		if (array.Length != 0)
		{
			return AttachmentCache.GetAttachmentById(array[0]);
		}
		return null;
	}

	public static async Task SetAttachemntActive(string projectId, string taskId, string attachmentId, int active)
	{
		AttachmentModel obj = await GetAttachmentById(attachmentId);
		obj.status = active;
		await Communicator.UpdateAttachment(obj, taskId, projectId);
	}

	public static async void DeleteAttachmentByTaskIdAsync(string taskId)
	{
	}
}
