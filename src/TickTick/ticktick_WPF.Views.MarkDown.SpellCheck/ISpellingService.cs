using System.Collections.Generic;

namespace ticktick_WPF.Views.MarkDown.SpellCheck;

public interface ISpellingService
{
	string Language { get; set; }

	void ClearLanguage();

	bool Spell(string word);

	IEnumerable<string> Suggestions(string word);

	void Add(string word);

	string CustomDictionaryFile();

	string[] Languages();
}
