using System;
using System.IO;
using System.Media;
using System.Timers;
using System.Windows;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Pomo;

public class PomoSoundPlayer
{
	private static string _currentSound = "none";

	private static TTSoundPlayer _ttSoundPlayer;

	private static SoundPlayer _soundPlayer;

	private static readonly Timer _soundTimer = new Timer(5000.0);

	private static bool _trial;

	public static void TryPlaySound(bool trial)
	{
		_trial = trial;
		string soundPath = AppPaths.PomoSoundDir + LocalSettings.Settings.PomoSound + ".ogg";
		if (!File.Exists(soundPath))
		{
			LocalSettings.Settings.PomoSound = "none";
		}
		else
		{
			Application.Current?.Dispatcher?.Invoke(PlaySound);
		}
		void PlaySound()
		{
			try
			{
				string text = AppPaths.PomoSoundDir + LocalSettings.Settings.PomoSound + ".wav";
				if (!File.Exists(text) || _currentSound != LocalSettings.Settings.PomoSound)
				{
					OggWavConverter.Convert(new FileStream(soundPath, FileMode.Open, FileAccess.Read), LocalSettings.Settings.PomoSound);
					_currentSound = LocalSettings.Settings.PomoSound;
				}
				StopPlaySound();
				if (File.Exists(text))
				{
					if (Utils.IsWindows7())
					{
						_soundPlayer = new SoundPlayer
						{
							SoundLocation = text
						};
						_soundPlayer.PlayLooping();
					}
					else
					{
						_ttSoundPlayer = new TTSoundPlayer
						{
							Volume = ((LocalSettings.Settings.ExtraSettings.FocusVolume > 0.0) ? (0.7 + LocalSettings.Settings.ExtraSettings.FocusVolume / 100.0 * 0.3) : 0.0)
						};
						_ttSoundPlayer.PlayVideo(text);
					}
				}
				PomoSoundHelper.SetPomoSoundPlaying(playing: true);
				if (_trial)
				{
					StopInSeconds();
				}
			}
			catch (Exception ex)
			{
				UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
			}
		}
	}

	private static void StopInSeconds()
	{
		_soundTimer.Stop();
		_soundTimer.Elapsed -= TryStopSound;
		_soundTimer.Elapsed += TryStopSound;
		_soundTimer.Start();
	}

	public static void StartPlaySound(bool trial)
	{
		if (LocalSettings.Settings.PomoSound != "none")
		{
			try
			{
				TryPlaySound(trial);
				return;
			}
			catch (Exception)
			{
				return;
			}
		}
		StopPlaySound();
	}

	public static void StopPlaySound()
	{
		if (Utils.IsWindows7())
		{
			SoundPlayer soundPlayer = _soundPlayer;
			_soundPlayer = null;
			if (soundPlayer != null)
			{
				soundPlayer.Stop();
				soundPlayer.Dispose();
			}
		}
		else
		{
			_ttSoundPlayer?.Close();
			_ttSoundPlayer = null;
		}
		PomoSoundHelper.SetPomoSoundPlaying(playing: false);
	}

	private static void TryStopSound(object sender, ElapsedEventArgs e)
	{
		Utils.RunOnUiThread(Application.Current?.Dispatcher, TryStopSound);
	}

	public static void TryStopSound()
	{
		_soundTimer.Stop();
		if (_trial)
		{
			StopPlaySound();
		}
		_trial = false;
	}

	public static void OnVolumeChanged()
	{
		if (_ttSoundPlayer != null)
		{
			_ttSoundPlayer.Volume = ((LocalSettings.Settings.ExtraSettings.FocusVolume > 0.0) ? (0.7 + LocalSettings.Settings.ExtraSettings.FocusVolume / 100.0 * 0.3) : 0.0);
		}
	}
}
