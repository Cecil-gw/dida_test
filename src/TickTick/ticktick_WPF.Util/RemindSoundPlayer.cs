using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Windows;
using NAudio.Wave;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Resource;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Util;

public class RemindSoundPlayer
{
	private static readonly object AnnoyingReminderLocker = new object();

	private static readonly HashSet<string> AnnoyingReminderKeys = new HashSet<string>();

	private static WaveOut WaveOut;

	private static WaveStream WaveReader;

	private static SoundPlayer Player;

	private static TTSoundPlayer TTPlayer;

	private static bool IsPlayingAnnoyingReminderLoop;

	private static string AnnoyingReminderLoopSound;

	private static Dictionary<string, string> _remindSoundFileName = new Dictionary<string, string>
	{
		{ "MusicBox", "music_box.mp3" },
		{ "Beep", "beep.mp3" },
		{ "Leap", "leap.mp3" },
		{ "Harp", "harp.mp3" },
		{ "Ladder", "ladder.mp3" },
		{ "Crystal", "crystal.mp3" },
		{ "Matrix", "matrix.mp3" },
		{ "Chimes", "chimes.mp3" },
		{ "Drum", "africa.mp3" },
		{ "Blocks", "blocks.mp3" },
		{ "Lattice", "lattice.mp3" },
		{ "Pulse", "pulse.mp3" }
	};

	public static string GetSoundFileName(string sound)
	{
		if (!_remindSoundFileName.ContainsKey(sound))
		{
			return null;
		}
		return _remindSoundFileName[sound];
	}

	public static void PlayTaskRemindSound(bool isTry)
	{
		string sound = LocalSettings.Settings.ExtraSettings.RemindSound;
		if (string.IsNullOrEmpty(sound))
		{
			sound = (LocalSettings.Settings.EnableRingtone ? "Harp" : "None");
		}
		if (!Utils.IsWindows7() && !LocalSettings.Settings.ShowReminderInClient && sound == "Harp")
		{
			if (!isTry)
			{
				return;
			}
			string fileName = Environment.GetEnvironmentVariable("WINDIR") + "\\Media\\Windows Notify System Generic.wav";
			if (!File.Exists(fileName))
			{
				return;
			}
			Application.Current?.Dispatcher?.Invoke(delegate
			{
				if (!Utils.IsWindows7())
				{
					TTSoundPlayer.PlayOnce(fileName, (LocalSettings.Settings.ExtraSettings.ReminderVolume > 0.0) ? (0.7 + LocalSettings.Settings.ExtraSettings.ReminderVolume / 100.0 * 0.3) : 0.0);
				}
				else
				{
					new SoundPlayer(fileName).Play();
				}
			});
		}
		else
		{
			Application.Current?.Dispatcher?.Invoke(delegate
			{
				PlayRemindSound(sound, isTry, loop: false, HasActiveAnnoyingReminderLoop());
			});
		}
	}

	public static void PlayFocusRemindSound(bool isBreak, bool trial = false)
	{
		string text = (isBreak ? LocalSettings.Settings.PomoLocalSetting.BreakEndSound : LocalSettings.Settings.PomoLocalSetting.FocusEndSound);
		if (string.IsNullOrEmpty(text))
		{
			text = (LocalSettings.Settings.EnableRingtone ? "MusicBox" : "None");
		}
		PlayRemindSound(text, trial, loop: false, HasActiveAnnoyingReminderLoop());
	}

	public static void StartTaskRemindSoundLoop(string reminderKey)
	{
		if (!string.IsNullOrEmpty(reminderKey))
		{
			lock (AnnoyingReminderLocker)
			{
				AnnoyingReminderKeys.Add(reminderKey);
			}
			Application.Current?.Dispatcher?.Invoke(EnsureAnnoyingReminderLoop);
		}
	}

	public static void StopTaskRemindSoundLoop(string reminderKey)
	{
		if (string.IsNullOrEmpty(reminderKey))
		{
			return;
		}
		bool flag = false;
		lock (AnnoyingReminderLocker)
		{
			AnnoyingReminderKeys.Remove(reminderKey);
			flag = AnnoyingReminderKeys.Count == 0;
		}
		if (!flag)
		{
			return;
		}
		Application.Current?.Dispatcher?.Invoke(delegate
		{
			if (IsPlayingAnnoyingReminderLoop)
			{
				StopCurrentPlayback();
			}
			IsPlayingAnnoyingReminderLoop = false;
			AnnoyingReminderLoopSound = null;
		});
	}

	private static bool HasActiveAnnoyingReminderLoop()
	{
		lock (AnnoyingReminderLocker)
		{
			return AnnoyingReminderKeys.Count > 0;
		}
	}

	private static void EnsureAnnoyingReminderLoop()
	{
		if (!HasActiveAnnoyingReminderLoop())
		{
			return;
		}
		string text = LocalSettings.Settings.ExtraSettings.RemindSound;
		if (string.IsNullOrEmpty(text))
		{
			text = (LocalSettings.Settings.EnableRingtone ? "Harp" : "None");
		}
		if (text == "None")
		{
			if (IsPlayingAnnoyingReminderLoop)
			{
				StopCurrentPlayback();
			}
			IsPlayingAnnoyingReminderLoop = false;
			AnnoyingReminderLoopSound = null;
		}
		else if (!IsPlayingAnnoyingReminderLoop || !(AnnoyingReminderLoopSound == text))
		{
			PlayRemindSound(text, trial: false, loop: true);
			IsPlayingAnnoyingReminderLoop = true;
			AnnoyingReminderLoopSound = text;
		}
	}

	private static void ResumeAnnoyingReminderLoopIfNeeded()
	{
		if (HasActiveAnnoyingReminderLoop())
		{
			EnsureAnnoyingReminderLoop();
		}
	}

	private static void PlayRemindSound(string sound, bool trial = false, bool loop = false, bool resumeAnnoyingReminderLoop = false)
	{
		try
		{
			UtilLog.Info($"PlayRemindSound {sound} {trial}");
			StopCurrentPlayback();
			IsPlayingAnnoyingReminderLoop = false;
			AnnoyingReminderLoopSound = null;
			if (sound == "None")
			{
				if (resumeAnnoyingReminderLoop)
				{
					ResumeAnnoyingReminderLoopIfNeeded();
				}
				return;
			}
			string text = AppDomain.CurrentDomain.BaseDirectory + ((sound == RemindSound.Spiral.ToString()) ? "completion_sound_spiral.wav" : ("RemindSound\\" + GetSoundFileName(sound)));
			if (!File.Exists(text))
			{
				return;
			}
			if (text.EndsWith("wav"))
			{
				if (!Utils.IsWindows7())
				{
					TTPlayer = new TTSoundPlayer
					{
						Loop = loop,
						Volume = ((LocalSettings.Settings.ExtraSettings.ReminderVolume > 0.0) ? (0.7 + LocalSettings.Settings.ExtraSettings.ReminderVolume / 100.0 * 0.3) : 0.0)
					};
					if (!loop)
					{
						TTPlayer.MediaEnded += delegate
						{
							TTPlayer?.Close();
							TTPlayer = null;
							if (resumeAnnoyingReminderLoop)
							{
								ResumeAnnoyingReminderLoopIfNeeded();
							}
						};
					}
					TTPlayer.PlayVideo(text);
				}
				else
				{
					Player = new SoundPlayer
					{
						SoundLocation = text
					};
					if (loop)
					{
						Player.PlayLooping();
					}
					else
					{
						Player.Play();
					}
				}
				return;
			}
			if (!Utils.IsWindows7())
			{
				TTPlayer = new TTSoundPlayer
				{
					Loop = loop,
					Volume = ((LocalSettings.Settings.ExtraSettings.ReminderVolume > 0.0) ? (0.7 + LocalSettings.Settings.ExtraSettings.ReminderVolume / 100.0 * 0.3) : 0.0)
				};
				TTPlayer.PlayVideo(text);
				if (loop)
				{
					return;
				}
				TTPlayer.MediaEnded += delegate
				{
					TTPlayer?.Close();
					TTPlayer = null;
					if (resumeAnnoyingReminderLoop)
					{
						ResumeAnnoyingReminderLoopIfNeeded();
					}
				};
				return;
			}
			WaveReader = new Mp3FileReader(text);
			WaveOut = new WaveOut();
			WaveOut.Init(WaveReader);
			if (loop)
			{
				WaveOut.PlaybackStopped += delegate
				{
					Application.Current?.Dispatcher?.InvokeAsync(delegate
					{
						if (HasActiveAnnoyingReminderLoop())
						{
							StopCurrentPlayback();
							IsPlayingAnnoyingReminderLoop = false;
							AnnoyingReminderLoopSound = null;
							ResumeAnnoyingReminderLoopIfNeeded();
						}
					});
				};
			}
			else if (resumeAnnoyingReminderLoop)
			{
				WaveOut.PlaybackStopped += delegate
				{
					ResumeAnnoyingReminderLoopIfNeeded();
				};
			}
			WaveOut.Play();
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
			if (resumeAnnoyingReminderLoop)
			{
				ResumeAnnoyingReminderLoopIfNeeded();
			}
		}
	}

	public static void Stop()
	{
		try
		{
			Application.Current?.Dispatcher?.Invoke(delegate
			{
				StopCurrentPlayback();
				IsPlayingAnnoyingReminderLoop = false;
				AnnoyingReminderLoopSound = null;
				ResumeAnnoyingReminderLoopIfNeeded();
			});
		}
		catch
		{
		}
	}

	private static void StopCurrentPlayback()
	{
		if (Utils.IsWindows7())
		{
			if (Player != null)
			{
				Player.Stop();
				Player.Dispose();
				Player = null;
			}
			if (WaveOut != null)
			{
				WaveOut.Stop();
				WaveOut.Dispose();
				WaveOut = null;
			}
			if (WaveReader != null)
			{
				WaveReader.Dispose();
				WaveReader = null;
			}
		}
		else
		{
			TTPlayer?.Close();
			TTPlayer = null;
		}
	}

	public static void OnVolumeChanged()
	{
		if (TTPlayer != null)
		{
			TTPlayer.Volume = ((LocalSettings.Settings.ExtraSettings.ReminderVolume > 0.0) ? (0.7 + LocalSettings.Settings.ExtraSettings.ReminderVolume / 100.0 * 0.3) : 0.0);
		}
	}
}
