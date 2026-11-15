using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Windows.Media;
using TickTickUtils;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Pomo;

public class PomoSoundViewModel : BaseViewModel
{
	private bool _selected;

	private bool _downloaded;

	private bool _downloading;

	public string Key { get; set; }

	public Geometry Data { get; set; }

	public string Name { get; set; }

	public double Percent { get; set; }

	public bool NeedPro { get; set; }

	public bool Selected
	{
		get
		{
			return _selected;
		}
		set
		{
			_selected = value;
			OnPropertyChanged("Selected");
		}
	}

	public bool Downloaded
	{
		get
		{
			return _downloaded;
		}
		set
		{
			_downloaded = value;
			OnPropertyChanged("Downloaded");
			OnPropertyChanged("NeedDownload");
		}
	}

	public bool Downloading
	{
		get
		{
			return _downloading;
		}
		set
		{
			_downloading = value;
			OnPropertyChanged("Downloading");
			OnPropertyChanged("NeedDownload");
		}
	}

	public bool NeedDownload
	{
		get
		{
			if (!Downloaded)
			{
				return !Downloading;
			}
			return false;
		}
	}

	public event EventHandler<string> NotifyDownLoadCompleted;

	public PomoSoundViewModel(string key)
	{
		Key = key;
		string soundIndex = FocusConstance.GetSoundIndex(key);
		Data = Utils.GetGeometry("Ic.Focus." + soundIndex);
		Name = Utils.GetString(soundIndex);
		Downloaded = key == "none" || File.Exists(AppPaths.PomoSoundDir + Key + ".ogg");
		if (key == "Clock" && !Downloaded && File.Exists(AppDomain.CurrentDomain.BaseDirectory + Key + ".ogg"))
		{
			try
			{
				if (!Directory.Exists(AppPaths.PomoSoundDir))
				{
					Directory.CreateDirectory(AppPaths.PomoSoundDir);
				}
				File.Copy(AppDomain.CurrentDomain.BaseDirectory + Key + ".ogg", AppPaths.PomoSoundDir + Key + ".ogg");
				Downloaded = true;
			}
			catch (Exception)
			{
			}
		}
		Selected = Downloaded && LocalSettings.Settings.PomoSound?.ToLower() == Key.ToLower();
		NeedPro = key != "none" && key != "Clock";
	}

	public async void TryDownload()
	{
		WebClient webClient = new WebClient();
		try
		{
			if (!Directory.Exists(AppPaths.PomoSoundDir))
			{
				Directory.CreateDirectory(AppPaths.PomoSoundDir);
			}
			webClient.DownloadFileCompleted += OnSoundDownLoaded;
			webClient.DownloadProgressChanged += OnSoundDownLoading;
			await webClient.DownloadFileTaskAsync(GetRemotePath() + Key + ".ogg", AppPaths.PomoSoundDir + Key + ".ogg");
		}
		catch (Exception)
		{
		}
	}

	private void OnSoundDownLoading(object sender, DownloadProgressChangedEventArgs e)
	{
		Downloading = true;
		Percent = e.ProgressPercentage * 100;
		OnPropertyChanged("Angle");
	}

	private static string GetRemotePath()
	{
		return "https://" + BaseUrl.GetPullDomain() + "/common/pomodoro/windows/";
	}

	private void OnSoundDownLoaded(object sender, AsyncCompletedEventArgs e)
	{
		if (!File.Exists(AppPaths.PomoSoundDir + Key + ".ogg"))
		{
			return;
		}
		FileInfo fileInfo = new FileInfo(AppPaths.PomoSoundDir + Key + ".ogg");
		if (fileInfo.Length > 0)
		{
			Downloaded = true;
			this.NotifyDownLoadCompleted?.Invoke(this, Key);
		}
		else
		{
			try
			{
				fileInfo.Attributes = FileAttributes.Normal;
				fileInfo.Delete();
			}
			catch (Exception)
			{
			}
		}
		Downloading = false;
	}
}
