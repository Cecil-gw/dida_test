using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ticktick_WPF.Views.MarkDown.SpellCheck;

namespace ticktick_WPF.Views.MarkDown;

public interface ISpellChecker
{
	SpellCheckProvider SpellCheckProvider { get; set; }

	TextEditor GetEditBox();

	void CorrectSpellingError(string correct, TextSegment errorSegment);

	void AddErrorToDict(string error, TextSegment errorSegment);
}
