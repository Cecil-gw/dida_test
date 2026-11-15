using ICSharpCode.AvalonEdit.Document;

namespace ticktick_WPF.Views.MarkDown.SpellCheck;

public class SpellCheckHandler
{
	private string _correct;

	private TextSegment _errorSegment;

	private ISpellChecker _editor;

	private string _error;

	public SpellCheckHandler(ISpellChecker editor, string correct, string errorText, TextSegment errorSegment)
	{
		_editor = editor;
		_correct = correct;
		_error = errorText;
		_errorSegment = errorSegment;
	}

	public void CorrectSpellingError()
	{
		_editor?.CorrectSpellingError(_correct, _errorSegment);
	}

	public void IgnoreSpellingError()
	{
		_editor?.AddErrorToDict(_error, _errorSegment);
	}
}
