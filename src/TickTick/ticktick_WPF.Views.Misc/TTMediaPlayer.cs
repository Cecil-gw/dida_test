using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using TickTickUtils;
using TickTickUtils.Lang;
using WPFMediaKit.DirectShow.Controls;

namespace ticktick_WPF.Views.Misc;

public class TTMediaPlayer : UserControl, IComponentConnector
{
	private string _url;

	internal Border Container;

	internal MediaUriElement MediaEle;

	private bool _contentLoaded;

	public TTMediaPlayer()
	{
		InitializeComponent();
		MediaEle.Loop = true;
		base.Cursor = Cursors.Hand;
		base.MouseLeftButtonUp += OnMouseClick;
	}

	private void OnMouseClick(object sender, MouseButtonEventArgs e)
	{
		try
		{
			if (!string.IsNullOrEmpty(_url))
			{
				Process.Start(_url);
			}
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
		}
	}

	public async void PlayVideo(string url)
	{
		if (!(_url == url))
		{
			_url = url;
			if (string.IsNullOrEmpty(url))
			{
				MediaEle.Visibility = Visibility.Hidden;
				MediaEle.Source = null;
				MediaEle.Stop();
			}
			else
			{
				MediaEle.Source = new Uri(url);
				await Task.Delay(300);
				MediaEle.Visibility = Visibility.Visible;
			}
		}
	}

	public bool IsPlaying(string url)
	{
		return _url == url;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/misc/ttmediaplayer.xaml", UriKind.Relative);
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
			Container = (Border)target;
			break;
		case 2:
			MediaEle = (MediaUriElement)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
