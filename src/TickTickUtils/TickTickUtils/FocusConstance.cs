using System.Collections.Generic;

namespace TickTickUtils;

public static class FocusConstance
{
	public static string Focus = "Focus";

	public static string Timing = "Timing";

	public const string StartPause = "StartPause";

	public const string KeyNone = "none";

	public const string KeyMorning = "Morning";

	public const string KeyChirp = "Chirp";

	public const string KeyForest = "Forest";

	public const string KeyStream = "Stream";

	public const string KeyDeepSea = "DeepSea";

	public const string KeyWave = "Wave";

	public const string KeyDesert = "Desert";

	public const string KeyStorm = "Storm";

	public const string KeyRain = "Rain";

	public const string KeyStove = "Stove";

	public const string KeyBoiling = "Boiling";

	public const string KeyCafe = "Cafe";

	public const string KeyClock = "Clock";

	public const string KeyMusicBox = "MusicBox";

	public const string KeySummer = "Summer";

	public const string KeyStreetTraffic = "StreetTraffic";

	public const string KeyTempleBlock = "TempleBlock";

	public const int MaxPomoSecond = 10800;

	public static Dictionary<string, string> KeyIndexDict = new Dictionary<string, string>
	{
		{ "none", "Mute" },
		{ "Clock", "Clock" },
		{ "Morning", "Morning" },
		{ "Chirp", "Chirp" },
		{ "Forest", "Forest" },
		{ "Stream", "Stream" },
		{ "DeepSea", "DeepSea" },
		{ "Wave", "Wave" },
		{ "Desert", "DesertSound" },
		{ "Storm", "Storm" },
		{ "Rain", "Rain" },
		{ "Stove", "Stove" },
		{ "Boiling", "Boiling" },
		{ "Cafe", "Cafe" },
		{ "MusicBox", "MusicBox" },
		{ "Summer", "SummerDay" },
		{ "StreetTraffic", "StreetTraffic" },
		{ "TempleBlock", "TempleBlock" }
	};

	public static List<string> HourUnit = new List<string> { "小时", "hour" };

	public static List<string> MinuteUnit = new List<string> { "分钟", "minute" };

	public static string GetSoundIndex(string key)
	{
		if (key != null && KeyIndexDict.ContainsKey(key))
		{
			return KeyIndexDict[key];
		}
		return "Mute";
	}
}
