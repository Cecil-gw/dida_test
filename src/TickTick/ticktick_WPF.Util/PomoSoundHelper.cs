using System.IO;
using TickTickUtils;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Util;

public class PomoSoundHelper
{
	private static bool _isPomoSoundPlaying;

	public static void SetPomoSoundPlaying(bool playing)
	{
		_isPomoSoundPlaying = playing;
	}

	public static bool IsPomoSoundPlaying()
	{
		return _isPomoSoundPlaying;
	}

	public static void InitSetFocusSound()
	{
		if (string.IsNullOrEmpty(LocalSettings.Settings.PomoSound) || !FocusConstance.KeyIndexDict.ContainsKey(LocalSettings.Settings.PomoSound))
		{
			LocalSettings.Settings.PomoSound = (string.IsNullOrEmpty(LocalSettings.Settings.PomoSound) ? "none" : "Clock");
			LocalSettings.Settings.Save();
		}
		if (LocalSettings.Settings.PomoSound != "none" && !File.Exists(AppPaths.PomoSoundDir + LocalSettings.Settings.PomoSound + ".ogg"))
		{
			LocalSettings.Settings.PomoSound = "none";
			LocalSettings.Settings.Save();
		}
	}
}
