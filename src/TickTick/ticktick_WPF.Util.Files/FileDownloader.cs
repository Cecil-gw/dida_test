using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TickTickUtils.Lang;
using ticktick_WPF.Util.Attachment;
using ticktick_WPF.Util.Network;

namespace ticktick_WPF.Util.Files;

public class FileDownloader
{
	private Uri _uri;

	private string _fileName;

	private bool _started;

	private object _lock = new object();

	private HttpClient _httpClient;

	private string _attachmentId;

	public event EventHandler<AttachmentDownloadSuccessModel> DownloadFileCompleted;

	public event EventHandler<AttachmentProgressModel> DownloadProgressChanged;

	public FileDownloader(string downloadUri, string filePath, string attachmentId)
		: this(new Uri(downloadUri), filePath, attachmentId)
	{
	}

	public FileDownloader(Uri downloadUri, string filePath, string attachmentId)
	{
		_httpClient = ApiClient.GetDownloadClient();
		_httpClient.DefaultRequestHeaders.Add("attachmentId", attachmentId);
		_uri = downloadUri;
		_fileName = FileUtils.ToValidFileName(filePath);
		_attachmentId = attachmentId;
	}

	public async Task StartDownloadAsync()
	{
		lock (_lock)
		{
			if (_started)
			{
				return;
			}
			_started = true;
		}
		if ((object)_uri == null || string.IsNullOrEmpty(_fileName) || string.IsNullOrWhiteSpace(_fileName))
		{
			ClearUp();
			return;
		}
		AttachmentDownloadSuccessModel completeModel = new AttachmentDownloadSuccessModel
		{
			AttachmentId = _attachmentId,
			Success = true
		};
		try
		{
			HttpResponseMessage httpResponseMessage = await _httpClient.GetAsync(_uri);
			httpResponseMessage.Content.Headers.ContentLength.GetValueOrDefault();
			if (httpResponseMessage.StatusCode != HttpStatusCode.OK)
			{
				completeModel.Success = false;
				string text = await httpResponseMessage.Content.ReadAsStringAsync();
				UtilLog.Error(string.Format("{0}, Failed Download attachmentId:{1}, url:{2}, result:{3}", "FileDownloader", _attachmentId, _uri, text));
				return;
			}
			FileInfo fileInfo = new FileInfo(_fileName);
			FileInfo bakFile = new FileInfo(Path.Combine(Path.GetDirectoryName(_fileName), "bak_" + fileInfo.Name));
			if (fileInfo.Exists)
			{
				fileInfo.Attributes = FileAttributes.Normal;
				fileInfo.Delete();
			}
			using (FileStream fileStream = bakFile.Create())
			{
				using Stream stream = await httpResponseMessage.Content.ReadAsStreamAsync();
				await stream.CopyToAsync(fileStream);
			}
			bakFile.CopyTo(_fileName, overwrite: true);
			bakFile.Attributes = FileAttributes.Normal;
			bakFile.Delete();
			this.DownloadProgressChanged?.Invoke(null, new AttachmentProgressModel
			{
				AttachmentId = _attachmentId,
				ProgressPercentage = 100
			});
		}
		catch (Exception ex)
		{
			UtilLog.Error("FileDownloader, DownloadFileAsync Exception: " + ex.Message);
			completeModel.Success = false;
		}
		finally
		{
			this.DownloadFileCompleted?.Invoke(null, completeModel);
			ClearUp();
		}
	}

	private void ClearUp()
	{
		this.DownloadFileCompleted = null;
		this.DownloadProgressChanged = null;
	}
}
