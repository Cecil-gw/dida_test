using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using TickTickUtils;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Guide;

public class CommonGuideWindow : Window, IComponentConnector
{
	internal CommonGuideWindow Root;

	internal Grid LeftContainer;

	internal Grid MediaGrid;

	internal Grid ChooseFeatureGrid;

	private bool _contentLoaded;

	private TTMediaPlayer mediaPlayer { get; set; }

	private CommonGuideViewModel viewModel { get; set; }

	private bool IsStop { get; set; }

	private bool IsLoop { get; set; }

	public CommonGuideWindow()
	{
		InitializeComponent();
		base.DataContext = (viewModel = new CommonGuideViewModel());
		CommonGuideViewModel commonGuideViewModel2 = viewModel;
		commonGuideViewModel2.OnEndGuide = (Action)Delegate.Combine(commonGuideViewModel2.OnEndGuide, new Action(OnEndGuide));
		CommonGuideViewModel commonGuideViewModel3 = viewModel;
		commonGuideViewModel3.OnMediaUrlSwitch = (Action<string, bool>)Delegate.Combine(commonGuideViewModel3.OnMediaUrlSwitch, new Action<string, bool>(OnMediaUrlSwitch));
		CommonGuideViewModel commonGuideViewModel4 = viewModel;
		commonGuideViewModel4.OnMediaPathSwitch = (Action<string, bool>)Delegate.Combine(commonGuideViewModel4.OnMediaPathSwitch, new Action<string, bool>(OnMediaPathSwitch));
		CommonGuideViewModel commonGuideViewModel5 = viewModel;
		commonGuideViewModel5.OnMediaBackStep = (Action<string, bool>)Delegate.Combine(commonGuideViewModel5.OnMediaBackStep, new Action<string, bool>(OnMediaBackStep));
	}

	private void Window_Closing(object sender, CancelEventArgs e)
	{
		CommonGuideViewModel commonGuideViewModel = viewModel;
		commonGuideViewModel.OnEndGuide = (Action)Delegate.Remove(commonGuideViewModel.OnEndGuide, new Action(OnEndGuide));
		CommonGuideViewModel commonGuideViewModel2 = viewModel;
		commonGuideViewModel2.OnMediaUrlSwitch = (Action<string, bool>)Delegate.Remove(commonGuideViewModel2.OnMediaUrlSwitch, new Action<string, bool>(OnMediaUrlSwitch));
		CommonGuideViewModel commonGuideViewModel3 = viewModel;
		commonGuideViewModel3.OnMediaPathSwitch = (Action<string, bool>)Delegate.Remove(commonGuideViewModel3.OnMediaPathSwitch, new Action<string, bool>(OnMediaPathSwitch));
		CommonGuideViewModel commonGuideViewModel4 = viewModel;
		commonGuideViewModel4.OnMediaBackStep = (Action<string, bool>)Delegate.Remove(commonGuideViewModel4.OnMediaBackStep, new Action<string, bool>(OnMediaBackStep));
		StopPlay(remove: true);
	}

	public void Set8FuncGuide()
	{
		viewModel.Init8FuncGuide();
	}

	private void OnEndGuide()
	{
		Close();
	}

	private async void OnMediaUrlSwitch(string url, bool isLoop)
	{
		IsLoop = isLoop;
		PlayMedia(await GetMediaPath(url, isPath: false));
	}

	private async void OnMediaPathSwitch(string relativePath, bool isLoop)
	{
		IsLoop = isLoop;
		PlayMedia(await GetMediaPath(relativePath, isPath: true));
	}

	private async void OnMediaBackStep(string path, bool isLocal)
	{
		PlayMediaLastFrame(await GetMediaPath(path, isLocal));
	}

	public async Task<string> GetMediaPath(string pathOrUrl, bool isPath)
	{
		_ = string.Empty;
		return (!isPath) ? (await DownloadFromUrl(pathOrUrl)) : AssetsToFile(pathOrUrl);
	}

	private string AssetsToFile(string relativePath)
	{
		if (string.IsNullOrEmpty(relativePath))
		{
			return null;
		}
		string fileName = Path.GetFileName(relativePath);
		string text = Path.Combine(AppPaths.ImageDir, fileName);
		if (!File.Exists(text))
		{
			Assembly executingAssembly = Assembly.GetExecutingAssembly();
			string name = typeof(App).Namespace + "." + relativePath.Replace('/', '.').Replace('\\', '.');
			using Stream stream = executingAssembly.GetManifestResourceStream(name);
			if (stream == null)
			{
				return null;
			}
			using FileStream destination = File.Create(text);
			stream.CopyTo(destination);
		}
		return text;
	}

	private async Task<string> DownloadFromUrl(string url)
	{
		string mediaPath = null;
		if (!string.IsNullOrEmpty(url))
		{
			string fileName = Path.GetFileName(url);
			if (fileName != null && (fileName.EndsWith(".mp4") || fileName.EndsWith(".avi")))
			{
				List<string> list = url.Split('/').ToList();
				fileName = fileName.Insert(fileName.Length - 4, "_" + ((list.Count > 1) ? list[list.Count - 2] : ""));
				if (!File.Exists(AppPaths.ImageDir + fileName))
				{
					await IOUtils.DownloadFile(AppPaths.ImageDir, fileName, url);
				}
				if (File.Exists(AppPaths.ImageDir + fileName))
				{
					mediaPath = AppPaths.ImageDir + fileName;
				}
			}
		}
		return mediaPath;
	}

	private void PlayMedia(string mediaPath)
	{
		if (string.IsNullOrEmpty(mediaPath))
		{
			MediaGrid.Visibility = Visibility.Collapsed;
		}
		else
		{
			MediaGrid.Visibility = Visibility.Visible;
		}
		if (string.IsNullOrEmpty(mediaPath))
		{
			StopPlay();
			return;
		}
		try
		{
			mediaPath = Path.GetFullPath(mediaPath);
			if (File.Exists(mediaPath))
			{
				Play(mediaPath);
			}
		}
		catch (Exception)
		{
		}
	}

	private void PlayMediaLastFrame(string mediaPath)
	{
		if (string.IsNullOrEmpty(mediaPath))
		{
			MediaGrid.Visibility = Visibility.Collapsed;
		}
		else
		{
			MediaGrid.Visibility = Visibility.Visible;
		}
		if (string.IsNullOrEmpty(mediaPath))
		{
			StopPlay();
			return;
		}
		try
		{
			mediaPath = Path.GetFullPath(mediaPath);
			if (File.Exists(mediaPath))
			{
				PlayLastFrame(mediaPath);
			}
		}
		catch (Exception)
		{
		}
	}

	public void Play(string filePath)
	{
		if (mediaPlayer == null)
		{
			mediaPlayer = new TTMediaPlayer();
			mediaPlayer.Container.Background = Brushes.Transparent;
			MediaGrid.Children.Add(mediaPlayer);
		}
		mediaPlayer.MediaEle.Loop = IsLoop;
		mediaPlayer.PlayVideo(filePath);
	}

	public void PlayLastFrame(string filePath)
	{
		if (mediaPlayer == null)
		{
			mediaPlayer = new TTMediaPlayer();
			mediaPlayer.Container.Background = Brushes.Transparent;
			MediaGrid.Children.Add(mediaPlayer);
		}
		mediaPlayer.MediaEle.Loop = false;
		mediaPlayer.PlayVideo(filePath);
		mediaPlayer.MediaEle.Pause();
		mediaPlayer.MediaEle.MediaPosition = mediaPlayer.MediaEle.MediaDuration;
	}

	private void MainMediaElement_MediaEnded(object sender, RoutedEventArgs e)
	{
		if (IsLoop)
		{
			_ = IsStop;
		}
	}

	public void StopPlay(bool remove = false)
	{
		if (remove)
		{
			if (mediaPlayer != null)
			{
				MediaGrid.Children.Clear();
				mediaPlayer.PlayVideo(null);
				mediaPlayer = null;
			}
		}
		else
		{
			mediaPlayer?.PlayVideo(null);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/guide/commonguidewindow.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			Root = (CommonGuideWindow)target;
			Root.Closing += Window_Closing;
			break;
		case 2:
			LeftContainer = (Grid)target;
			break;
		case 3:
			MediaGrid = (Grid)target;
			break;
		case 4:
			ChooseFeatureGrid = (Grid)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
