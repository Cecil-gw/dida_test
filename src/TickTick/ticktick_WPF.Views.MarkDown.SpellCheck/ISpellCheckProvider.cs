using System.Collections.Generic;
using ICSharpCode.AvalonEdit.Document;

namespace ticktick_WPF.Views.MarkDown.SpellCheck;

public interface ISpellCheckProvider
{
	bool Enabled { get; set; }

	void Initialize(ISpellChecker editor);

	IEnumerable<TextSegment> GetSpellCheckErrors();

	IEnumerable<string> GetSpellCheckSuggestions(string word);

	void Add(string word);

	string CustomDictionaryFile();

	void Disconnect();

	void ForceRefresh(bool delay = false);

	ISpellingService SpellingService();
}
