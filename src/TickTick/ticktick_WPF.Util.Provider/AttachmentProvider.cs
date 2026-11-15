using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util.Attachment;
using ticktick_WPF.Util.Files;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.QuickAdd;

namespace ticktick_WPF.Util.Provider;

public class AttachmentProvider : IDisposable
{
	private readonly List<AttachmentViewModel> _displayAttachmentList = new List<AttachmentViewModel>();

	private readonly AttachmentComparer _attachmentComparer = new AttachmentComparer();

	public string TaskId { get; set; }

	public void UpdateModels(List<AttachmentModel> models, int imageMode, out List<string> addIds, out List<string> outRemoveIds)
	{
		List<string> removeIds = (from vm in _displayAttachmentList
			where !models.Exists((AttachmentModel attachmentModel) => vm.Id == attachmentModel.id)
			select vm.Id).ToList();
		_displayAttachmentList.RemoveAll((AttachmentViewModel vm) => removeIds.Contains(vm.Id));
		List<AttachmentModel> list = new List<AttachmentModel>();
		foreach (AttachmentModel model in models)
		{
			AttachmentViewModel attachmentViewModel = _displayAttachmentList.FirstOrDefault((AttachmentViewModel item) => item.Id == model.id);
			if (attachmentViewModel != null)
			{
				attachmentViewModel.UpdateProperty(model, imageMode);
			}
			else
			{
				list.Add(model);
			}
		}
		list.ForEach(DealWithAttachment);
		IEnumerable<AttachmentViewModel> collection = list.Select((AttachmentModel attachment) => new AttachmentViewModel(attachment, imageMode));
		_displayAttachmentList.AddRange(collection);
		_displayAttachmentList.Sort(_attachmentComparer);
		addIds = list.Select((AttachmentModel item) => item.id).ToList();
		outRemoveIds = removeIds;
	}

	public AttachmentProvider(IEnumerable<AttachmentModel> attachmentModelList, int imageMode)
	{
		foreach (AttachmentModel attachmentModel in attachmentModelList)
		{
			DealWithAttachment(attachmentModel);
			_displayAttachmentList.Add(new AttachmentViewModel(attachmentModel, imageMode));
		}
		_displayAttachmentList.Sort(_attachmentComparer);
		AttachmentLoadHelper.Downloaded += OnAttachmentDownloaded;
		AttachmentLoadHelper.DownloadFailed += OnAttachmentDownloadFailed;
		AttachmentLoadHelper.DownloadProgressChanged += OnDownloadProgressChanged;
		AttachmentLoadHelper.Uploaded += OnAttachmentUploaded;
		AttachmentLoadHelper.UploadFailed += OnAttachmentUploadFailed;
		AttachmentLoadHelper.UploadProgressChanged += OnUploadProgressChanged;
	}

	private void DealWithAttachment(AttachmentModel item)
	{
		try
		{
			if (item.fileType == "IMAGE" && FileUtils.FileEmptyOrNotExists(item.localPath))
			{
				AttachmentDownloadUtils.AutoDownloadImgAttachment(item);
			}
		}
		catch (Exception)
		{
		}
		if (item.fileType == "IMAGE" && FileUtils.FileExists(item.localPath) && !FileUtils.FileInFolder(item.localPath, AppPaths.ImageDir))
		{
			string text = item.fileName.Split('.').Last();
			string text2 = AppPaths.ImageDir + item.id + "." + text;
			try
			{
				File.Copy(item.localPath, text2, overwrite: true);
				item.localPath = text2;
				AttachmentDao.UpdateAttachment(item);
			}
			catch (Exception)
			{
			}
		}
	}

	public void Dispose()
	{
		AttachmentLoadHelper.Downloaded -= OnAttachmentDownloaded;
		AttachmentLoadHelper.DownloadFailed -= OnAttachmentDownloadFailed;
		AttachmentLoadHelper.DownloadProgressChanged -= OnDownloadProgressChanged;
		AttachmentLoadHelper.Uploaded -= OnAttachmentUploaded;
		AttachmentLoadHelper.UploadFailed -= OnAttachmentUploadFailed;
		AttachmentLoadHelper.UploadProgressChanged -= OnUploadProgressChanged;
	}

	public List<AttachmentViewModel> GetModels()
	{
		return _displayAttachmentList.ToList();
	}

	private AttachmentViewModel GetModelById(string id)
	{
		return _displayAttachmentList.FirstOrDefault((AttachmentViewModel m) => m.Id == id);
	}

	private void OnUploadProgressChanged(object sender, AttachmentProgressModel e)
	{
		GetModelById(e.AttachmentId)?.SetFileStatus((e.ProgressPercentage >= 100) ? OptTypeEnum.None : OptTypeEnum.Loading);
	}

	private void OnAttachmentUploadFailed(object sender, string e)
	{
		GetModelById(e)?.SetFileStatus(OptTypeEnum.UploadFailed);
	}

	private void OnAttachmentUploaded(object sender, string e)
	{
		GetModelById(e)?.SetFileStatus(OptTypeEnum.None);
	}

	private void OnDownloadProgressChanged(object sender, AttachmentProgressModel e)
	{
		GetModelById(e.AttachmentId)?.SetFileStatus((e.ProgressPercentage >= 100) ? OptTypeEnum.None : OptTypeEnum.Loading);
	}

	private void OnAttachmentDownloadFailed(object sender, string e)
	{
		GetModelById(e)?.SetFileStatus(OptTypeEnum.Loading);
	}

	private void OnAttachmentDownloaded(object sender, string e)
	{
		AttachmentViewModel attachmentViewModel = _displayAttachmentList.FirstOrDefault((AttachmentViewModel a) => a.Id == e);
		if (attachmentViewModel != null && e != null)
		{
			AttachmentModel attachmentById = AttachmentCache.GetAttachmentById(e);
			attachmentViewModel.LocalPath = attachmentById.localPath;
			attachmentViewModel.SetFileStatus(OptTypeEnum.Menu);
		}
	}

	public static Constants.AttachmentKind GetFileType(string fileName)
	{
		switch (fileName.Split('.').Last().ToLower())
		{
		case "bmp":
		case "gif":
		case "jpg":
		case "png":
		case "jpeg":
		case "tiff":
		case "webp":
			return Constants.AttachmentKind.IMAGE;
		case "amr":
		case "mp3":
		case "m4a":
		case "m4r":
		case "3gp":
		case "3gpp":
			return Constants.AttachmentKind.AUDIO;
		case "avi":
		case "mov":
		case "mp4":
			return Constants.AttachmentKind.VIDEO;
		case "pdf":
			return Constants.AttachmentKind.PDF;
		case "doc":
		case "odt":
		case "wps":
		case "docx":
		case "pages":
			return Constants.AttachmentKind.DOC;
		case "txt":
		case "rtf":
			return Constants.AttachmentKind.TEXT;
		case "ods":
		case "xls":
		case "xlsx":
		case "numbers":
			return Constants.AttachmentKind.XLS;
		case "csv":
			return Constants.AttachmentKind.CSV;
		case "ppt":
		case "pps":
		case "odp":
		case "pptx":
		case "ppsx":
		case "keynote":
			return Constants.AttachmentKind.PPT;
		case "rar":
		case "zip":
		case "7z":
			return Constants.AttachmentKind.ZIP;
		default:
			return Constants.AttachmentKind.OTHER;
		}
	}

	public static string GetFileTypeName(string fileName)
	{
		if (GetFileType(fileName) == Constants.AttachmentKind.IMAGE)
		{
			return Utils.GetString("Image").ToLower();
		}
		return Utils.GetString("File").ToLower();
	}

	public void Clear()
	{
		_displayAttachmentList.Clear();
	}

	public static async Task<List<AttachmentModel>> AddAttachmentModels(string taskId, List<AddTaskAttachmentInfo> files)
	{
		List<AttachmentModel> result = new List<AttachmentModel>();
		if (files == null || files.Count == 0)
		{
			return result;
		}
		foreach (AddTaskAttachmentInfo file in files)
		{
			string path = file.Path;
			if (FileUtils.FileEmptyOrNotExists(path))
			{
				UtilLog.Warn("UploadAttachment empty file " + path);
				continue;
			}
			string fileName = Path.GetFileName(file.Path);
			Constants.AttachmentKind fileType = GetFileType(fileName);
			FileInfo fileInfo = new FileInfo(path);
			AttachmentModel uploadAttachmentModel = new AttachmentModel
			{
				id = Utils.GetGuid()
			};
			uploadAttachmentModel.refId = uploadAttachmentModel.id;
			uploadAttachmentModel.taskId = taskId;
			uploadAttachmentModel.fileName = fileName;
			uploadAttachmentModel.localPath = path;
			uploadAttachmentModel.fileType = fileType.ToString();
			uploadAttachmentModel.createdTime = DateTime.Now;
			uploadAttachmentModel.size = (int)fileInfo.Length;
			uploadAttachmentModel.sync_status = 0.ToString();
			if (uploadAttachmentModel.fileType == Constants.AttachmentKind.IMAGE.ToString())
			{
				AttachmentUploadUtils.CompressImage(uploadAttachmentModel);
			}
			FileUtils.CollectFileSize(uploadAttachmentModel.localPath);
			if (FileUtils.FileOverSize(uploadAttachmentModel.localPath))
			{
				Utils.Toast(LocalSettings.Settings.IsPro ? Utils.GetString("AttachmentSizeLimitPro") : Utils.GetString("AttachmentSizeLimit"));
				continue;
			}
			await AttachmentDao.InsertOrUpdateAttachment(uploadAttachmentModel);
			result.Add(uploadAttachmentModel);
		}
		return result;
	}

	public static void AppendTaskContent(TaskModel task, List<AttachmentModel> attachments)
	{
		if (task.kind != "CHECKLIST")
		{
			task.content = AttachmentDao.AddAttachmentStrings(task.content, attachments);
		}
	}
}
