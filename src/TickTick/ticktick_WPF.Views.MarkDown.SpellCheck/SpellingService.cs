using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NHunspell;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Views.MarkDown.SpellCheck;

public class SpellingService : ISpellingService
{
	private string _language;

	private Hunspell _speller;

	public static SpellingService CommonSpellingService = null;

	private static readonly string _dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Tick_Tick";

	public string Language
	{
		get
		{
			return _language;
		}
		set
		{
			SetLanguage(value);
		}
	}

	public SpellingService()
	{
		Language = LocalSettings.Settings.SpellCheckLanguage;
		LocalSettings.SpellCheckChanged += OnSpellLanguageChanged;
	}

	private void OnSpellLanguageChanged(object sender, EventArgs e)
	{
		if (LocalSettings.Settings.SpellCheckEnable)
		{
			Language = LocalSettings.Settings.SpellCheckLanguage;
		}
	}

	public bool Spell(string word)
	{
		if (_speller != null)
		{
			return _speller.Spell(word);
		}
		return true;
	}

	public IEnumerable<string> Suggestions(string word)
	{
		return _speller.Suggest(word);
	}

	public void Add(string word)
	{
		if (_speller != null)
		{
			_speller.Add(word);
			UpdateCustomDictionary(word);
		}
	}

	public void ClearLanguage()
	{
		_speller = null;
	}

	public string CustomDictionaryFile()
	{
		string text = Path.Combine(_dir, "custom_dictionary.txt");
		if (!File.Exists(text))
		{
			if (!Directory.Exists(_dir))
			{
				Directory.CreateDirectory(_dir);
			}
			Directory.CreateDirectory(_dir);
			File.WriteAllText(text, string.Empty);
		}
		return text;
	}

	public string[] Languages()
	{
		return Directory.GetFiles(SpellCheckFolder(), "*.dic").Select(Path.GetFileNameWithoutExtension).ToArray();
	}

	private static string SpellCheckFolder()
	{
		return _dir + "\\Dictionaries";
	}

	private void SetLanguage(string language)
	{
		if (string.IsNullOrEmpty(language))
		{
			return;
		}
		try
		{
			ClearLanguage();
			Hunspell hunspell = new Hunspell();
			string path = SpellCheckFolder();
			string text = Path.Combine(path, language + ".aff");
			string text2 = Path.Combine(path, language + ".dic");
			if (File.Exists(text) && File.Exists(text2))
			{
				hunspell.Load(text, text2);
				LoadCustomDictonary(hunspell);
				hunspell.Add("❓");
				_speller = hunspell;
				_language = language;
			}
		}
		catch (Exception)
		{
		}
	}

	private void LoadCustomDictonary(Hunspell speller)
	{
		try
		{
			string[] array = File.ReadAllLines(CustomDictionaryFile());
			foreach (string word in array)
			{
				speller.Add(word);
			}
		}
		catch (Exception)
		{
		}
	}

	private void UpdateCustomDictionary(string word)
	{
		try
		{
			File.AppendAllLines(CustomDictionaryFile(), new string[1] { word });
		}
		catch (Exception)
		{
		}
	}
}
