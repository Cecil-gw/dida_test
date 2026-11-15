using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TickTickUtils;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util.Files;

namespace ticktick_WPF.Util.Attachment;

public static class AttachmentDownloadUtils
{
	private static readonly object TryDownloadNoPathLock = new object();

	public static HashSet<string> TryDownloadNoPathAttachmentIds = new HashSet<string>();

	public static string GetLocalPath(AttachmentModel attachment)
	{
		if (attachment == null)
		{
			return string.Empty;
		}
		if (attachment.fileType == Constants.AttachmentKind.IMAGE.ToString())
		{
			string text = attachment.fileName.Split('.').Last();
			return AppPaths.ImageDir + attachment.refId + "." + text;
		}
		return AppPaths.ImageDir + attachment.refId + FileUtils.ToValidFileName(attachment.fileName);
	}

	public static async Task AutoDownloadImgAttachment(AttachmentModel item)
	{
		TaskModel taskModel = await TaskDao.GetTaskById(item.taskId);
		if (taskModel != null)
		{
			await CheckThenDownload(item, taskModel, GetLocalPath(item));
		}
	}

	public static async Task DownloadFileAttachment(string fileId, bool commentAttachment = false)
	{
		if (string.IsNullOrEmpty(fileId))
		{
			return;
		}
		AttachmentModel fileModel = AttachmentCache.GetAttachmentById(fileId);
		if (fileModel == null)
		{
			return;
		}
		if (!string.IsNullOrEmpty(fileModel.refId) && fileModel.refId != fileModel.id)
		{
			AttachmentModel attachmentModel = await AttachmentDao.GetAttachmentById(fileModel.refId);
			if (attachmentModel != null)
			{
				fileModel.path = attachmentModel.path;
			}
		}
		if (string.IsNullOrEmpty(fileModel.path) && !MarkNoPathDownloadAttempt(fileModel.id))
		{
			Utils.Toast(Utils.GetString("DownloadFailedCauseFileNotUpload"));
			return;
		}
		TaskModel task = null;
		if (!commentAttachment)
		{
			task = await TaskDao.GetTaskById(fileModel.taskId);
		}
		else
		{
			List<CommentModel> list = await CommentDao.GetCommentsById(fileModel.taskId);
			if (list != null && list.Count > 0)
			{
				task = await TaskDao.GetTaskById(list[0].taskSid);
			}
		}
		if (task != null)
		{
			await CheckThenDownload(fileModel, task, GetLocalPath(fileModel), commentAttachment);
		}
	}

	private static bool MarkNoPathDownloadAttempt(string id)
	{
		lock (TryDownloadNoPathLock)
		{
			return TryDownloadNoPathAttachmentIds.Add(id);
		}
	}

	public static void ClearNoPathDownloadAttempt(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return;
		}
		lock (TryDownloadNoPathLock)
		{
			TryDownloadNoPathAttachmentIds.Remove(id);
		}
	}

	private static async Task CheckThenDownload(AttachmentModel item, TaskModel task, string filePath, bool commentAttachment = false)
	{
		if (item == null || task == null)
		{
			return;
		}
		if (!Utils.IsNetworkAvailable())
		{
			AttachmentLoadHelper.NotifyDownloadFailed(item.id);
			Utils.Toast(Utils.GetString("DownloadAttachmentNetworkError"));
			return;
		}
		if (!string.IsNullOrEmpty(item.refId) && item.refId != item.id)
		{
			AttachmentModel attachmentModel = await AttachmentDao.GetAttachmentById(item.refId);
			if (attachmentModel != null)
			{
				item.path = attachmentModel.path;
			}
		}
		if (string.IsNullOrEmpty(item.path) && !MarkNoPathDownloadAttempt(item.id))
		{
			AttachmentLoadHelper.NotifyDownloadFailed(item.id);
			Utils.Toast(Utils.GetString("DownloadFailedCauseFileNotUpload"));
		}
		else if (!FileUtils.FileEmptyOrNotExists(filePath))
		{
			AttachmentLoadHelper.OnDownloadCompleted(null, new AttachmentDownloadSuccessModel
			{
				AttachmentId = item.id,
				Success = true
			});
		}
		else
		{
			await DownloadFile(task.projectId, task.id, item.id, filePath, commentAttachment);
		}
	}

	public static async Task DownloadCountdownFile(string countdownId, string attachmentId, string locationPath, Action<bool, string> callback = null)
	{
		if (!Utils.IsNetworkAvailable())
		{
			AttachmentLoadHelper.NotifyDownloadFailed(attachmentId);
			Utils.Toast(Utils.GetString("DownloadAttachmentNetworkError"));
			callback?.Invoke(arg1: false, null);
			return;
		}
		try
		{
			Directory.CreateDirectory(locationPath);
			string downloadUri = BaseUrl.GetApiDomain() + $"/api/v1/attachment/countdown?id={countdownId}&attachmentId={attachmentId}";
			string filePath = Path.Combine(locationPath, "countdown_" + countdownId + ".png");
			FileDownloader fileDownloader = new FileDownloader(downloadUri, filePath, attachmentId);
			fileDownloader.DownloadFileCompleted += AttachmentLoadHelper.OnDownloadCompleted;
			fileDownloader.DownloadFileCompleted += delegate(object s, AttachmentDownloadSuccessModel e)
			{
				if (e.Success)
				{
					callback?.Invoke(arg1: true, filePath);
				}
				else
				{
					Utils.Toast(Utils.GetString("DownloadAttachmentNetworkError"));
					callback?.Invoke(arg1: false, null);
				}
			};
			fileDownloader.DownloadProgressChanged += AttachmentLoadHelper.OnDownloadProgressChanged;
			await fileDownloader.StartDownloadAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			AttachmentLoadHelper.NotifyDownloadFailed(attachmentId);
			callback?.Invoke(arg1: false, null);
			MessageBox.Show(ex.Message);
		}
	}

	public static async Task DownloadFile(string projectId, string taskId, string attachmentId, string locationPath, bool commentAttachment = false)
	{
		if (!Utils.IsNetworkAvailable())
		{
			AttachmentLoadHelper.NotifyDownloadFailed(attachmentId);
			Utils.Toast(Utils.GetString("DownloadAttachmentNetworkError"));
			return;
		}
		try
		{
			if (!AttachmentLoadHelper.StartDownload(attachmentId))
			{
				return;
			}
			FileDownloader fileDownloader = new FileDownloader(BaseUrl.GetApiDomain() + string.Format(commentAttachment ? "/api/v1/attachment/comment/{0}/{1}/{2}" : "/api/v1/attachment/{0}/{1}/{2}", projectId, taskId, attachmentId), locationPath, attachmentId);
			fileDownloader.DownloadFileCompleted += AttachmentLoadHelper.OnDownloadCompleted;
			fileDownloader.DownloadFileCompleted += delegate(object s, AttachmentDownloadSuccessModel e)
			{
				if (!e.Success)
				{
					Utils.Toast(Utils.GetString("DownloadAttachmentNetworkError"));
				}
			};
			fileDownloader.DownloadProgressChanged += AttachmentLoadHelper.OnDownloadProgressChanged;
			fileDownloader.StartDownloadAsync();
		}
		catch (Exception ex)
		{
			AttachmentLoadHelper.NotifyDownloadFailed(attachmentId);
			MessageBox.Show(ex.Message);
		}
	}
}
