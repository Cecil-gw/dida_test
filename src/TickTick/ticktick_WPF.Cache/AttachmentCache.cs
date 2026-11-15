using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util.Attachment;
using ticktick_WPF.Util.Files;

namespace ticktick_WPF.Cache;

public static class AttachmentCache
{
	private static ConcurrentDictionary<string, AttachmentModel> _attachmentModelDict = new ConcurrentDictionary<string, AttachmentModel>();

	private static ConcurrentDictionary<string, List<AttachmentModel>> _attachmentModelTaskDict = new ConcurrentDictionary<string, List<AttachmentModel>>();

	public static async Task ResetDictItems()
	{
		List<AttachmentModel> obj = await App.Connection.Table<AttachmentModel>().ToListAsync();
		ConcurrentDictionary<string, AttachmentModel> concurrentDictionary = new ConcurrentDictionary<string, AttachmentModel>();
		ConcurrentDictionary<string, List<AttachmentModel>> concurrentDictionary2 = new ConcurrentDictionary<string, List<AttachmentModel>>();
		foreach (AttachmentModel item in obj)
		{
			if (!string.IsNullOrEmpty(item.id) && !string.IsNullOrEmpty(item.taskId) && TaskCache.ExistTask(item.taskId))
			{
				if (!concurrentDictionary.TryAdd(item.id, item))
				{
					App.Connection.DeleteAsync(item);
					continue;
				}
				if (concurrentDictionary2.ContainsKey(item.taskId))
				{
					concurrentDictionary2[item.taskId].Add(item);
					continue;
				}
				concurrentDictionary2.TryAdd(item.taskId, new List<AttachmentModel> { item });
			}
		}
		_attachmentModelDict = concurrentDictionary;
		_attachmentModelTaskDict = concurrentDictionary2;
	}

	public static AttachmentModel GetAttachmentById(string id)
	{
		if (!string.IsNullOrEmpty(id) && _attachmentModelDict.ContainsKey(id))
		{
			AttachmentModel attachmentModel = _attachmentModelDict[id];
			if (string.IsNullOrEmpty(attachmentModel.localPath) && attachmentModel.id != attachmentModel.refId)
			{
				string localPath = AttachmentDownloadUtils.GetLocalPath(attachmentModel);
				if (!FileUtils.FileEmptyOrNotExists(localPath))
				{
					attachmentModel.localPath = localPath;
					TryUpdate(attachmentModel);
				}
			}
			return attachmentModel;
		}
		return null;
	}

	public static string GetFileName(string id)
	{
		if (!string.IsNullOrEmpty(id) && _attachmentModelDict.ContainsKey(id))
		{
			return _attachmentModelDict[id].fileName;
		}
		return null;
	}

	private static async void TryUpdate(AttachmentModel attachment)
	{
		AttachmentModel attachmentModel = await AttachmentDao.GetAttachmentById(attachment.id);
		if (attachmentModel != null)
		{
			attachment._Id = attachmentModel._Id;
			await App.Connection.UpdateAsync(attachment);
		}
		else
		{
			await App.Connection.InsertAsync(attachment);
		}
	}

	public static List<AttachmentModel> GetAllAttachments()
	{
		return _attachmentModelDict.Values.ToList();
	}

	public static List<AttachmentModel> GetAttachmentsByTaskId(string taskId)
	{
		if (taskId != null && _attachmentModelTaskDict.TryGetValue(taskId, out var value))
		{
			return value.Where((AttachmentModel a) => !a.deleted && a.status == 0).ToList();
		}
		return new List<AttachmentModel>();
	}

	public static bool GetTaskExistAttachment(string taskId)
	{
		if (taskId != null && _attachmentModelTaskDict.TryGetValue(taskId, out var value))
		{
			return value.Any((AttachmentModel a) => !a.deleted && a.status == 0);
		}
		return false;
	}

	public static void SetAttachment(AttachmentModel attachment)
	{
		if (string.IsNullOrEmpty(attachment?.id) || string.IsNullOrEmpty(attachment.taskId))
		{
			return;
		}
		_attachmentModelDict[attachment.id] = attachment;
		_attachmentModelTaskDict.AddOrUpdate(attachment.taskId, (string _) => new List<AttachmentModel> { attachment }, delegate(string _, List<AttachmentModel> current)
		{
			List<AttachmentModel> list = current.Where((AttachmentModel a) => a.id != attachment.id).ToList();
			list.Add(attachment);
			return list;
		});
	}

	public static int TodayAttachmentCount()
	{
		string todayAttachmentCount = LocalSettings.Settings.ExtraSettings.TodayAttachmentCount;
		if (string.IsNullOrEmpty(todayAttachmentCount))
		{
			return 0;
		}
		string text = DateTime.Now.ToUniversalTime().ToString("yyyyMMdd':'", App.Ci);
		if (todayAttachmentCount.StartsWith(text))
		{
			todayAttachmentCount = todayAttachmentCount.Replace(text, "");
			if (int.TryParse(todayAttachmentCount, out var result))
			{
				return result;
			}
		}
		return 0;
	}

	public static void SetTodayAttachmentCount(int num)
	{
		string text = DateTime.Now.ToUniversalTime().ToString("yyyyMMdd':'", App.Ci);
		LocalSettings.Settings.ExtraSettings.TodayAttachmentCount = text + num;
		LocalSettings.Settings.Save(force: true);
	}
}
