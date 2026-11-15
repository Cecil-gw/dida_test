using System;
using WPFMediaKit.DirectShow.Controls;
using WPFMediaKit.DirectShow.MediaPlayers;

namespace ticktick_WPF.Views.Misc;

public class TTSoundPlayer : MediaUriElement
{
	private string _url;

	public TTSoundPlayer()
	{
		base.Loop = true;
		base.LoadedBehavior = MediaState.Play;
		base.Volume = 1.0;
	}

	public async void PlayVideo(string url)
	{
		if (!(_url == url))
		{
			_url = url;
			if (string.IsNullOrEmpty(url))
			{
				BeginInit();
				base.Source = null;
				EndInit();
				Stop();
			}
			else
			{
				BeginInit();
				base.Source = new Uri(url);
				EndInit();
				Play();
			}
		}
	}

	public static void PlayOnce(string fileName, double volume)
	{
		TTSoundPlayer soundPlay = new TTSoundPlayer
		{
			Loop = false,
			Volume = volume
		};
		soundPlay.MediaEnded += delegate
		{
			soundPlay.Close();
		};
		soundPlay.PlayVideo(fileName);
	}
}
