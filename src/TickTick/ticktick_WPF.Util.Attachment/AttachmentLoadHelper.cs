using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Util.Files;
using ticktick_WPF.Views.Detail;

namespace ticktick_WPF.Util.Attachment;

public static class AttachmentLoadHelper
{
	private static readonly HashSet<string> UploadingIds = new HashSet<string>();

	private static readonly HashSet<string> DownloadingIds = new HashSet<string>();

	private static readonly HashSet<string> DownloadFailedIds = new HashSet<string>();

	private static readonly HashSet<string> UploadFailedIds = new HashSet<string>();

	private static readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

	public static event EventHandler<string> Downloaded;

	public static event EventHandler<string> DownloadFailed;

	public static event EventHandler<string> Uploaded;

	public static event EventHandler<string> UploadFailed;

	public static event EventHandler<AttachmentProgressModel> DownloadProgressChanged;

	public static event EventHandler<AttachmentProgressModel> UploadProgressChanged;

	public static OptTypeEnum AttachmentStatus(AttachmentModel attachment, bool enable = true)
	{
		return ThreadUtil.ReadLock(_lock, delegate
		{
			if (attachment == null || !enable)
			{
				return OptTypeEnum.None;
			}
			if (DownloadFailedIds.Contains(attachment.id))
			{
				return OptTypeEnum.DownloadFailed;
			}
			if (UploadFailedIds.Contains(attachment.id))
			{
				return OptTypeEnum.UploadFailed;
			}
			if (DownloadingIds.Contains(attachment.id) || UploadingIds.Contains(attachment.id))
			{
				return OptTypeEnum.Loading;
			}
			if (string.IsNullOrWhiteSpace(attachment.path) && !FileUtils.FileEmptyOrNotExists(attachment.localPath) && !UploadingIds.Contains(attachment.id))
			{
				return OptTypeEnum.Upload;
			}
			return (!DownloadingIds.Contains(attachment.id) && FileUtils.FileEmptyOrNotExists(attachment.localPath)) ? OptTypeEnum.Download : OptTypeEnum.Menu;
		});
	}

	public static bool IsFailed(string id)
	{
		return ThreadUtil.ReadLock(_lock, () => IsFailedInternal(id));
	}

	public static bool IsLoading(string id)
	{
		return ThreadUtil.ReadLock(_lock, () => IsLoadingInternal(id));
	}

	private static bool IsFailedInternal(string id)
	{
		if (!DownloadFailedIds.Contains(id))
		{
			return UploadFailedIds.Contains(id);
		}
		return true;
	}

	private static bool IsLoadingInternal(string id)
	{
		if (!DownloadingIds.Contains(id))
		{
			return UploadingIds.Contains(id);
		}
		return true;
	}

	public static async void OnDownloadCompleted(object sender, AttachmentDownloadSuccessModel model)
	{
		AttachmentModel attachment = AttachmentCache.GetAttachmentById(model.AttachmentId);
		string localPath = AttachmentDownloadUtils.GetLocalPath(attachment);
		if (attachment == null)
		{
			return;
		}
		if (!model.Success)
		{
			try
			{
				if (File.Exists(localPath))
				{
					FileInfo fileInfo = new FileInfo(localPath);
					fileInfo.Attributes = FileAttributes.Normal;
					fileInfo.Delete();
				}
			}
			catch (Exception)
			{
			}
			finally
			{
				attachment.localPath = null;
				await AttachmentDao.UpdateAttachment(attachment);
				NotifyDownloadFailed(attachment.id);
			}
		}
		else
		{
			attachment.localPath = localPath;
			if (string.IsNullOrEmpty(attachment.path))
			{
				attachment.path = "-1";
			}
			attachment.sync_status = 2.ToString();
			await AttachmentDao.UpdateAttachment(attachment);
			NotifyDownloadCompleted(attachment.id);
		}
	}

	public static void OnDownloadProgressChanged(object sender, AttachmentProgressModel e)
	{
		if (AttachmentCache.GetAttachmentById(e.AttachmentId) != null)
		{
			NotifyDownloading(e);
		}
	}

	public static void OnUploadCompleted(object sender, UploadFileCompletedEventArgs e)
	{
		App.Instance.Dispatcher.Invoke((Func<Task>)async delegate
		{
			if (sender is WebClient webClient)
			{
				string[] values = webClient.Headers.GetValues("attachmentId");
				if (values != null && values.Length != 0)
				{
					UploadingIds.Remove(values[0]);
					if (e.Error != null)
					{
						Utils.Toast(Utils.GetString("UploadFailed"));
						SyncManager.Sync();
						NotifyUploadFailed(values[0]);
						UtilLog.Info("AttachmentUploadFailed " + values[0]);
					}
					else
					{
						UtilLog.Info("AttachmentUploadSuccess " + values[0]);
						NotifyUploadCompleted(values[0]);
					}
				}
			}
		});
	}

	public static void OnUploadProgressChanged(object sender, UploadProgressChangedEventArgs e)
	{
		App.Instance.Dispatcher.Invoke(delegate
		{
			if (sender is WebClient webClient)
			{
				string[] values = webClient.Headers.GetValues("attachmentId");
				if (values != null && values.Length != 0)
				{
					AttachmentModel attachmentById = AttachmentCache.GetAttachmentById(values[0]);
					if (attachmentById != null)
					{
						NotifyUploading(new AttachmentProgressModel
						{
							AttachmentId = attachmentById.id,
							ProgressPercentage = e.ProgressPercentage
						});
					}
				}
			}
		});
	}

	private static void RemoveAll(string id, AttachmentLoadingType type)
	{
		if (type != AttachmentLoadingType.UploadFailed && UploadFailedIds.Contains(id))
		{
			UploadFailedIds.Remove(id);
		}
		if (type != AttachmentLoadingType.DownloadingFailed && DownloadFailedIds.Contains(id))
		{
			DownloadFailedIds.Remove(id);
		}
		if (type != AttachmentLoadingType.Uploading && UploadingIds.Contains(id))
		{
			UploadingIds.Remove(id);
		}
		if (type != AttachmentLoadingType.Downloading && DownloadingIds.Contains(id))
		{
			DownloadingIds.Remove(id);
		}
	}

	public static void ClearFailed()
	{
		ThreadUtil.WriteLock(_lock, delegate
		{
			UploadFailedIds.Clear();
			DownloadFailedIds.Clear();
		});
	}

	public static void ClearFailed(string id)
	{
		if (!string.IsNullOrEmpty(id))
		{
			ThreadUtil.WriteLock(_lock, delegate
			{
				UploadFailedIds.Remove(id);
				DownloadFailedIds.Remove(id);
			});
		}
	}

	public static bool StartDownload(string id)
	{
		bool num = ThreadUtil.WriteLock(_lock, delegate
		{
			if (IsLoadingInternal(id))
			{
				return false;
			}
			RemoveAll(id, AttachmentLoadingType.Downloading);
			DownloadingIds.Add(id);
			return true;
		});
		if (num)
		{
			ThreadUtil.DetachedRunOnUiThread(delegate
			{
				AttachmentLoadHelper.DownloadProgressChanged?.Invoke(null, new AttachmentProgressModel
				{
					AttachmentId = id,
					ProgressPercentage = 0
				});
			});
		}
		return num;
	}

	public static bool StartUpload(string id)
	{
		bool num = ThreadUtil.WriteLock(_lock, delegate
		{
			if (IsLoadingInternal(id))
			{
				return false;
			}
			RemoveAll(id, AttachmentLoadingType.Uploading);
			UploadingIds.Add(id);
			return true;
		});
		if (num)
		{
			App.Instance.Dispatcher.Invoke(delegate
			{
				AttachmentLoadHelper.UploadProgressChanged?.Invoke(null, new AttachmentProgressModel
				{
					AttachmentId = id,
					ProgressPercentage = 0
				});
			});
		}
		return num;
	}

	public static void NotifyUploadFailed(string id)
	{
		ThreadUtil.WriteLock(_lock, delegate
		{
			RemoveAll(id, AttachmentLoadingType.UploadFailed);
			UploadFailedIds.Add(id);
		});
		ThreadUtil.DetachedRunOnUiThread(delegate
		{
			AttachmentLoadHelper.UploadFailed?.Invoke(null, id);
		});
	}

	public static void NotifyDownloadFailed(string id)
	{
		ThreadUtil.WriteLock(_lock, delegate
		{
			RemoveAll(id, AttachmentLoadingType.DownloadingFailed);
			DownloadFailedIds.Add(id);
		});
		ThreadUtil.DetachedRunOnUiThread(delegate
		{
			AttachmentLoadHelper.DownloadFailed?.Invoke(null, id);
		});
	}

	public static void NotifyDownloadCompleted(string id)
	{
		ThreadUtil.WriteLock(_lock, delegate
		{
			RemoveAll(id, AttachmentLoadingType.None);
		});
		ThreadUtil.DetachedRunOnUiThread(delegate
		{
			AttachmentLoadHelper.Downloaded?.Invoke(null, id);
		});
	}

	public static void NotifyUploadCompleted(string id)
	{
		ThreadUtil.WriteLock(_lock, delegate
		{
			RemoveAll(id, AttachmentLoadingType.None);
		});
		ThreadUtil.DetachedRunOnUiThread(delegate
		{
			AttachmentLoadHelper.Uploaded?.Invoke(null, id);
		});
	}

	public static void NotifyUploading(AttachmentProgressModel model)
	{
		ThreadUtil.WriteLock(_lock, delegate
		{
			RemoveAll(model.AttachmentId, AttachmentLoadingType.Uploading);
		});
		ThreadUtil.DetachedRunOnUiThread(delegate
		{
			AttachmentLoadHelper.UploadProgressChanged?.Invoke(null, model);
		});
	}

	public static void NotifyDownloading(AttachmentProgressModel model)
	{
		ThreadUtil.WriteLock(_lock, delegate
		{
			RemoveAll(model.AttachmentId, AttachmentLoadingType.Downloading);
		});
		ThreadUtil.DetachedRunOnUiThread(delegate
		{
			AttachmentLoadHelper.DownloadProgressChanged?.Invoke(null, model);
		});
	}
}
