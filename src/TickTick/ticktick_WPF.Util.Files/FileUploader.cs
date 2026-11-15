using System;
using System.Net;
using TickTickUtils.Lang;

namespace ticktick_WPF.Util.Files;

public class FileUploader
{
	private WebClient _client;

	private Uri _uri;

	private string _fileName;

	private bool _started;

	private object _lock = new object();

	public event UploadFileCompletedEventHandler UploadFileCompleted;

	public event UploadProgressChangedEventHandler UploadProgressChanged;

	public FileUploader(string uploadUri, string filePath, WebHeaderCollection headers)
		: this(new Uri(uploadUri), filePath, headers)
	{
	}

	public FileUploader(Uri uploadUri, string filePath, WebHeaderCollection headers)
	{
		_client = new WebClient();
		_client.Headers = headers;
		_client.UploadFileCompleted += OnWebClientUploadFileCompleted;
		_client.UploadProgressChanged += OnWebClientUploadProgressChanged;
		_uri = uploadUri;
		_fileName = filePath;
	}

	public bool StartAsync()
	{
		lock (_lock)
		{
			if (_started)
			{
				return false;
			}
			_started = true;
		}
		if ((object)_uri == null || string.IsNullOrEmpty(_fileName) || string.IsNullOrWhiteSpace(_fileName))
		{
			ClearUp();
			return false;
		}
		try
		{
			_client.UploadFileAsync(_uri, _fileName);
		}
		catch (Exception ex)
		{
			UtilLog.Error("FileDownloader, UploadFileAsync Exception: " + ex.Message);
			ClearUp();
			return false;
		}
		return true;
	}

	private void ClearUp()
	{
		this.UploadFileCompleted = null;
		this.UploadProgressChanged = null;
		if (_client != null)
		{
			_client.UploadFileCompleted -= OnWebClientUploadFileCompleted;
			_client.UploadProgressChanged -= OnWebClientUploadProgressChanged;
			_client.CancelAsync();
			_client.Dispose();
		}
	}

	private void OnWebClientUploadProgressChanged(object sender, UploadProgressChangedEventArgs e)
	{
		this.UploadProgressChanged?.Invoke(sender, e);
	}

	private void OnWebClientUploadFileCompleted(object sender, UploadFileCompletedEventArgs e)
	{
		this.UploadFileCompleted?.Invoke(sender, e);
		ClearUp();
	}
}
