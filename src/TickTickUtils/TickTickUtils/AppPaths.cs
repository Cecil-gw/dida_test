namespace TickTickUtils;

public static class AppPaths
{
	public static string ExeDir { get; private set; }

	public static string AppIconDir { get; private set; }

	public static string DataDir { get; private set; }

	public static string CountdownDir { get; private set; }

	public static string ImageDir { get; private set; }

	public static string AvatarDir { get; private set; }

	public static string DictionariesDir { get; private set; }

	public static string CustomDictionaryPath { get; private set; }

	public static string ThemeDir { get; private set; }

	public static string ResourceDir { get; private set; }

	public static string TickTickDbPath { get; private set; }

	public static string UpgradeProDir { get; private set; }

	public static string CalendarDir { get; private set; }

	public static string TimeZoneDictPath { get; private set; }

	public static string PomoSoundDir { get; private set; }

	public static string PackageDir { get; private set; }

	public static string TTFDir { get; private set; }

	public static string ENTTFDir { get; private set; }

	public static string LocaleDir { get; private set; }

	public static void Init(string dataDir, string exeDir)
	{
		DataDir = dataDir;
		ExeDir = exeDir;
		ImageDir = DataDir + "Image\\";
		CountdownDir = DataDir + "Countdown\\";
		DictionariesDir = DataDir + "Dictionaries\\";
		CustomDictionaryPath = DataDir + "custom_dictionary.txt";
		ThemeDir = DataDir + "Theme\\";
		ResourceDir = DataDir + "Resource\\";
		TickTickDbPath = DataDir + "TickTick.db";
		TTFDir = DataDir + "TTF\\";
		UpgradeProDir = DataDir + "UpgradePro\\";
		CalendarDir = DataDir + "Calendar\\";
		TimeZoneDictPath = DataDir + "TimeZoneDict.txt";
		PomoSoundDir = DataDir + "PomoSound\\";
		PackageDir = DataDir + "Package\\";
		AvatarDir = DataDir + "Avatar\\";
		AppIconDir = ExeDir + "AppIcons\\";
		LocaleDir = ExeDir + "locales\\";
		ENTTFDir = ExeDir + "TTF\\";
	}
}
