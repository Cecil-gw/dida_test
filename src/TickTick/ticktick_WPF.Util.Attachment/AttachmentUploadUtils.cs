using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util.Files;
using ticktick_WPF.Util.Network;

namespace ticktick_WPF.Util.Attachment;

public static class AttachmentUploadUtils
{
	private static readonly SemaphoreSlim _semaphore;

	private static readonly HashSet<string> UploadedIds;

	static AttachmentUploadUtils()
	{
		_semaphore = new SemaphoreSlim(5);
		UploadedIds = new HashSet<string>();
		NetworkChange.NetworkAvailabilityChanged -= OnNetWorkChanged;
		NetworkChange.NetworkAvailabilityChanged += OnNetWorkChanged;
	}

	public static async Task UploadCommentFile(string projectId, string taskId, AttachmentModel attachment)
	{
		if (!Utils.IsNetworkAvailable() || FileUtils.FileOverSize(attachment.localPath))
		{
			return;
		}
		string uploadUri = BaseUrl.GetApiDomain() + $"/api/v1/attachment/upload/comment/{projectId}/{taskId}";
		string localPath = attachment.localPath;
		string item = (string.IsNullOrEmpty(attachment.fileName) ? Path.GetFileName(localPath) : attachment.fileName);
		try
		{
			AttachmentModel result = await ApiClient.SendFileAsync<AttachmentModel>(uploadUri, new(string, string)[1] { (localPath, item) });
			attachment.id = result.id;
			attachment.refId = result.refId;
			attachment.path = attachment.path;
			AttachmentModel attachmentModel = await AttachmentDao.GetAttachmentByLocalId(attachment._Id);
			if (attachmentModel != null)
			{
				attachmentModel.id = result.id;
				attachmentModel.refId = result.refId;
				attachmentModel.path = attachment.path;
				await AttachmentDao.UpdateAttachmentById(attachmentModel, attachment._Id);
			}
		}
		catch (ApiException ex)
		{
			if (ex.ErrorCode.ToLower() == "no_project_permission")
			{
				UtilLog.Info("upload no_project_permission uri=" + uploadUri);
				attachment.sync_status = 2.ToString();
				await AttachmentDao.UpdateAttachment(attachment);
				AttachmentCache.SetTodayAttachmentCount(AttachmentCache.TodayAttachmentCount() - 1);
			}
			if (ex.ErrorCode.ToLower() == "exceed_quota")
			{
				AttachmentCache.SetTodayAttachmentCount((int)LimitCache.GetLimitByKey(Constants.LimitKind.DailyUploadNumber));
			}
		}
		catch (Exception ex2)
		{
			UtilLog.Error("upload UploadFailed ms) uri=" + uploadUri + ", ex: " + ex2);
		}
	}

	public static async Task UploadCountdownImage(string attachmentId, string localPath)
	{
		if (Utils.IsNetworkAvailable() && !FileUtils.FileOverSize(localPath))
		{
			await ApiClient.SendFileAsync<AttachmentModel>(BaseUrl.GetApiDomain() + $"/api/v1/attachment/upload/countdown?attachmentId={attachmentId}", new(string, string)[1] { (localPath, Path.GetFileName(localPath)) });
		}
	}

	public static async Task UpFile(string projectId, string taskId, AttachmentModel attachment, IToastShowWindow toastWindow, bool ignoreUploadCollect = false)
	{
		if (attachment == null)
		{
			return;
		}
		if (!Utils.IsNetworkAvailable())
		{
			AttachmentLoadHelper.NotifyUploadFailed(attachment.id);
			toastWindow?.TryToastString(null, Utils.GetString("UploadAttachmentNetworkError"));
			return;
		}
		if (!ignoreUploadCollect)
		{
			FileUtils.CollectFileSize(attachment.localPath);
		}
		if (FileUtils.FileOverSize(attachment.localPath))
		{
			toastWindow?.TryToastString(null, LocalSettings.Settings.IsPro ? Utils.GetString("AttachmentSizeLimitPro") : Utils.GetString("AttachmentSizeLimit"));
			AttachmentLoadHelper.NotifyUploadFailed(attachment.id);
			return;
		}
		string attachmentId = attachment.id;
		if (!AttachmentLoadHelper.StartUpload(attachmentId))
		{
			return;
		}
		string uploadUri = BaseUrl.GetApiDomain() + $"/api/v1/attachment/upload/{projectId}/{taskId}/{attachmentId}";
		string file = attachment.localPath;
		string fileName = (string.IsNullOrEmpty(attachment.fileName) ? Path.GetFileName(file) : attachment.fileName);
		Task.Run(async delegate
		{
			Stopwatch watch = Stopwatch.StartNew();
			try
			{
				await _semaphore.WaitAsync();
				Task<AttachmentModel> task = ApiClient.SendFileAsync<AttachmentModel>(uploadUri, new(string, string)[1] { (file, fileName) });
				UtilRun.OnProgressAsync(task, delegate(ProgressEventArgs args)
				{
					AttachmentLoadHelper.NotifyUploading(new AttachmentProgressModel
					{
						AttachmentId = attachment.id,
						ProgressPercentage = args.Progress
					});
				});
				await AttachmentDao.UpdateUploadAttachment(await task);
				watch.Stop();
				UtilLog.Info("upload success (" + watch.ElapsedMilliseconds + "ms) uri=" + uploadUri);
				AttachmentLoadHelper.NotifyUploadCompleted(attachmentId);
			}
			catch (ApiException ex)
			{
				UtilLog.Error("upload UploadFailed (" + watch.ElapsedMilliseconds + "ms) uri=" + uploadUri + ", ex: " + ex);
				if (ex.ErrorCode.ToLower() == "no_project_permission")
				{
					UtilLog.Info("upload no_project_permission uri=" + uploadUri);
					attachment.sync_status = 2.ToString();
					await AttachmentDao.UpdateAttachment(attachment);
					AttachmentCache.SetTodayAttachmentCount(AttachmentCache.TodayAttachmentCount() - 1);
					AttachmentLoadHelper.NotifyUploadCompleted(attachmentId);
				}
				else
				{
					AttachmentLoadHelper.NotifyUploadFailed(attachmentId);
				}
				if (ex.ErrorCode.ToLower() == "exceed_quota")
				{
					AttachmentCache.SetTodayAttachmentCount((int)LimitCache.GetLimitByKey(Constants.LimitKind.DailyUploadNumber));
				}
			}
			catch (Exception ex2)
			{
				UtilLog.Error("upload UploadFailed (" + watch.ElapsedMilliseconds + "ms) uri=" + uploadUri + ", ex: " + ex2);
				AttachmentLoadHelper.NotifyUploadFailed(attachmentId);
			}
			finally
			{
				watch.Stop();
				_semaphore.Release();
			}
		});
	}

	public static bool CompressImage(AttachmentModel attachment)
	{
		if (attachment.fileType == Constants.AttachmentKind.IMAGE.ToString())
		{
			string text = "." + attachment.localPath.Split('.').Last().ToLower();
			int flag = (text.EndsWith("png") ? 90 : 80);
			string text2 = attachment.localPath.Replace(text, "_compress" + text);
			FileInfo fileInfo = new FileInfo(attachment.localPath);
			long userLimit = Utils.GetUserLimit(Constants.LimitKind.AttachmentSize);
			if (fileInfo.Length <= userLimit / 10)
			{
				return true;
			}
			if (text2 != attachment.localPath && CompressImage(attachment.localPath, text2, flag, 1024) && File.Exists(text2))
			{
				FileInfo fileInfo2 = new FileInfo(text2);
				if (fileInfo2.Length <= 0 || fileInfo2.Length > fileInfo.Length)
				{
					try
					{
						fileInfo2.Attributes = FileAttributes.Normal;
						fileInfo2.Delete();
					}
					catch (Exception)
					{
					}
				}
				else
				{
					attachment.size = (int)fileInfo2.Length;
					try
					{
						fileInfo.Attributes = FileAttributes.Normal;
						fileInfo.Delete();
						fileInfo2.CopyTo(attachment.localPath);
						fileInfo2.Attributes = FileAttributes.Normal;
						fileInfo2.Delete();
					}
					catch (Exception)
					{
						attachment.localPath = text2;
					}
				}
				return new FileInfo(attachment.localPath).Length <= userLimit;
			}
		}
		return true;
	}

	private static bool CompressImage(string sFile, string dFile, int flag = 95, int size = 512)
	{
		Image image = Image.FromFile(sFile);
		ImageFormat rawFormat = image.RawFormat;
		int width = image.Width;
		int height = image.Height;
		Bitmap bitmap = new Bitmap(width, height);
		Graphics graphics = Graphics.FromImage(bitmap);
		graphics.Clear(Color.WhiteSmoke);
		graphics.CompositingQuality = CompositingQuality.HighQuality;
		graphics.SmoothingMode = SmoothingMode.HighQuality;
		graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
		graphics.DrawImage(image, new Rectangle(0, 0, width, height), 0, 0, width, height, GraphicsUnit.Pixel);
		graphics.Dispose();
		EncoderParameters encoderParameters = new EncoderParameters();
		EncoderParameter encoderParameter = new EncoderParameter(value: new long[1] { flag }, encoder: Encoder.Quality);
		encoderParameters.Param[0] = encoderParameter;
		try
		{
			ImageCodecInfo[] imageEncoders = ImageCodecInfo.GetImageEncoders();
			string suf = sFile.Split('.').Last().ToLower();
			string text = suf;
			if (text != null)
			{
				int length = text.Length;
				if (length != 3)
				{
					if (length == 4)
					{
						char c = text[0];
						if (c != 'j')
						{
							switch (c)
							{
							case 't':
								if (text == "tiff")
								{
									suf = "TIFF";
								}
								break;
							case 'w':
								if (text == "webp")
								{
									suf = "WEBP";
								}
								break;
							}
						}
						else if (text == "jpeg")
						{
							goto IL_01ce;
						}
					}
				}
				else
				{
					char c = text[0];
					if ((uint)c <= 103u)
					{
						switch (c)
						{
						case 'b':
							if (text == "bmp")
							{
								suf = "BMP";
							}
							break;
						case 'g':
							if (!(text == "gif"))
							{
								break;
							}
							return true;
						}
					}
					else if (c != 'j')
					{
						if (c == 'p' && text == "png")
						{
							suf = "PNG";
						}
					}
					else if (text == "jpg")
					{
						goto IL_01ce;
					}
				}
			}
			goto IL_0204;
			IL_0204:
			ImageCodecInfo imageCodecInfo = imageEncoders.FirstOrDefault((ImageCodecInfo t) => t.FormatDescription.Equals(suf));
			if (imageCodecInfo != null)
			{
				bitmap.Save(dFile, imageCodecInfo, encoderParameters);
			}
			else
			{
				bitmap.Save(dFile, rawFormat);
			}
			return true;
			IL_01ce:
			suf = "JPEG";
			goto IL_0204;
		}
		catch
		{
			return false;
		}
		finally
		{
			image.Dispose();
			bitmap.Dispose();
			GC.Collect();
		}
	}

	public static async Task UploadAllFiles()
	{
		List<AttachmentModel> list = (await AttachmentDao.GetAllAttachments())?.Where((AttachmentModel att) => att.status == 0 && !UploadedIds.Contains(att.id) && !string.IsNullOrEmpty(att.localPath) && File.Exists(att.localPath) && string.IsNullOrEmpty(att.path) && !AttachmentLoadHelper.IsLoading(att.id)).ToList();
		if (list == null || list.Count == 0)
		{
			return;
		}
		foreach (AttachmentModel attachment in list)
		{
			TaskModel taskModel = await TaskDao.GetThinTaskById(attachment.taskId);
			if (taskModel != null && taskModel.deleted == 0)
			{
				UploadedIds.Add(attachment.id);
				UpFile(taskModel.projectId, attachment.taskId, attachment, null);
			}
		}
	}

	private static async void OnNetWorkChanged(object sender, NetworkAvailabilityEventArgs e)
	{
		if (e.IsAvailable)
		{
			await Task.Delay(6000);
			UploadedIds.Clear();
		}
	}
}
