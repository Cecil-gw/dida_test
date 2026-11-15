using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Input;
using TickTickUtils;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.MarkDown.SpellCheck;

public static class SpellCheckUtils
{
	public static readonly ICommand CorrectSpellingError = new RelayCommand(delegate(object o)
	{
		((SpellCheckHandler)o).CorrectSpellingError();
	});

	public static readonly ICommand IgnoreSpellingError = new RelayCommand(delegate(object o)
	{
		((SpellCheckHandler)o).IgnoreSpellingError();
	});

	public static readonly ICommand OpenOrClose = new RelayCommand(delegate(object o)
	{
		OpenOrCloseSpellCheck((bool)o);
	});

	public static readonly ICommand LanguageSelected = new RelayCommand(delegate(object o)
	{
		SetSpellCheckLanguage((string)o);
	});

	public static bool Downloading { get; set; }

	private static void OpenOrCloseSpellCheck(bool enable)
	{
		LocalSettings.Settings.SpellCheckEnable = enable;
	}

	private static async void SetSpellCheckLanguage(string language)
	{
		await CheckLanguageDicExist(language);
		TryDeleteCustomDict();
		LocalSettings.Settings.SpellCheckLanguage = language;
	}

	private static void TryDeleteCustomDict()
	{
		if (File.Exists(AppPaths.CustomDictionaryPath))
		{
			FileInfo fileInfo = new FileInfo(AppPaths.CustomDictionaryPath);
			fileInfo.Attributes = FileAttributes.Normal;
			fileInfo.Delete();
		}
	}

	private static async Task CheckLanguageDicExist(string language)
	{
		Downloading = true;
		await TryDownLoadLanguage(language);
		Downloading = false;
	}

	private static string GetRemotePath()
	{
		return "https://" + BaseUrl.GetPullDomain() + "/windows/dic/";
	}

	private static async Task TryDownLoadLanguage(string language)
	{
		if (!Directory.Exists(AppPaths.DictionariesDir))
		{
			Directory.CreateDirectory(AppPaths.DictionariesDir);
		}
		WebClient downloader = new WebClient();
		try
		{
			string text = Path.Combine(AppPaths.DictionariesDir, language + ".aff");
			if (!File.Exists(text) || new FileInfo(text).Length == 0L)
			{
				await downloader.DownloadFileTaskAsync(GetRemotePath() + language + ".aff", text);
			}
			string text2 = Path.Combine(AppPaths.DictionariesDir, language + ".dic");
			if (!File.Exists(text2) || new FileInfo(text2).Length == 0L)
			{
				await downloader.DownloadFileTaskAsync(GetRemotePath() + language + ".dic", text2);
			}
		}
		catch (Exception)
		{
		}
	}

	public static void TryInitSpellLanguage()
	{
		string text = LocalSettings.Settings.SpellCheckLanguage;
		if (string.IsNullOrEmpty(text))
		{
			text = "en-US";
			switch (LocalSettings.Settings.UserChooseLanguage)
			{
			case "fr-FR":
			case "ru-RU":
			case "pt-BR":
				text = LocalSettings.Settings.UserChooseLanguage;
				break;
			}
			LocalSettings.Settings.SettingsModel.SpellCheckLanguage = text;
		}
		TryDownLoadLanguage(text);
	}
}
