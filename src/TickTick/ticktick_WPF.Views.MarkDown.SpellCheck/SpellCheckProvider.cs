using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Framework.Markdown;
using ticktick_WPF.Util;
using ticktick_WPF.Views.MarkDown.Colorizer;

namespace ticktick_WPF.Views.MarkDown.SpellCheck;

public class SpellCheckProvider : ISpellCheckProvider
{
	private readonly Regex _mardownUri = new Regex("\\[([^\\[]+)\\]\\(([^\\)]+)\\)");

	private readonly SpellCheckBackgroundRenderer _spellCheckRenderer;

	private readonly ISpellingService _spellingService;

	private readonly Regex _uriFinderRegex = new Regex("(http|ftp|https|mailto):\\/\\/[\\w\\-_]+(\\.[\\w\\-_]+)+([\\w\\-\\.,@?^=%&amp;:/~\\+#]*[\\w\\-\\@?^=%&amp;/~\\+#])?", RegexOptions.Compiled);

	private readonly Regex _wordSeparatorRegex = new Regex("-[^\\w]+|^'[^\\w]+|[^\\w]+'[^\\w]+|[^\\w]+-[^\\w]+|[^\\w]+'$|[^\\w]+-$|^-$|^'$|[^\\w'-]", RegexOptions.Compiled);

	private ISpellChecker _editor;

	private bool _enabled = true;

	private bool _textChanged;

	private bool _delay;

	private string _uid;

	public bool Enabled
	{
		get
		{
			return _enabled;
		}
		set
		{
			_enabled = value;
			if (!_enabled)
			{
				ClearSpellCheckErrors();
			}
		}
	}

	public SpellCheckProvider(ISpellingService spellingService)
	{
		_uid = Utils.GetGuid();
		_spellingService = spellingService;
		_spellCheckRenderer = new SpellCheckBackgroundRenderer();
	}

	private void TryDoSpell(object sender, EventArgs e)
	{
		Utils.RunOnUiThread(Application.Current?.Dispatcher, delegate
		{
			try
			{
				DoSpellCheck();
			}
			catch (Exception)
			{
			}
		});
	}

	public void Initialize(ISpellChecker editor)
	{
		_editor = editor;
		_spellCheckRenderer.IsOffsetHidden = ((editor is MarkDownEditor markDownEditor) ? new Func<int, bool>(markDownEditor.IsDocumentOffsetHiddenByHeadingFold) : null);
		_editor.GetEditBox().TextArea.TextView.BackgroundRenderers.Add(_spellCheckRenderer);
		_editor.GetEditBox().TextArea.TextView.VisualLinesChanged += TextViewVisualLinesChanged;
	}

	public string CustomDictionaryFile()
	{
		return _spellingService.CustomDictionaryFile();
	}

	public void Disconnect()
	{
		if (_editor != null)
		{
			ClearSpellCheckErrors();
			_editor.GetEditBox().TextArea.TextView.BackgroundRenderers.Remove(_spellCheckRenderer);
			_editor.GetEditBox().TextArea.TextView.VisualLinesChanged -= TextViewVisualLinesChanged;
			_spellCheckRenderer.IsOffsetHidden = null;
			_editor = null;
		}
	}

	public void ForceRefresh(bool delay = false)
	{
		if (_editor != null && Enabled)
		{
			SetTextChanged(change: true, delay);
			_editor.GetEditBox().TextArea.TextView.Redraw();
			DelayActionHandlerCenter.TryDoAction(_uid, TryDoSpell, delay ? 300 : 100);
		}
	}

	public ISpellingService SpellingService()
	{
		return _spellingService;
	}

	public IEnumerable<TextSegment> GetSpellCheckErrors()
	{
		if (_spellCheckRenderer != null)
		{
			return _spellCheckRenderer.ErrorSegments;
		}
		return Enumerable.Empty<TextSegment>();
	}

	public IEnumerable<string> GetSpellCheckSuggestions(string word)
	{
		if (_spellCheckRenderer != null)
		{
			return _spellingService.Suggestions(word);
		}
		return Enumerable.Empty<string>();
	}

	public void Add(string word)
	{
		if (!string.IsNullOrWhiteSpace(word) && _spellingService != null && Enabled)
		{
			_spellingService.Add(word);
		}
	}

	private void TextViewVisualLinesChanged(object sender, EventArgs e)
	{
		if (Enabled)
		{
			DelayActionHandlerCenter.TryDoAction(_uid, TryDoSpell, _delay ? 300 : 100);
		}
	}

	private async Task DoSpellCheck()
	{
		if (_editor == null || !_textChanged || !_editor.GetEditBox().TextArea.TextView.VisualLinesValid)
		{
			return;
		}
		_spellCheckRenderer.ErrorSegments.Clear();
		IEnumerable<VisualLine> enumerable = _editor.GetEditBox().TextArea.TextView.VisualLines.AsParallel();
		ClearSpellCheckErrors();
		foreach (VisualLine currentLine in enumerable)
		{
			int startIndex = 0;
			int offset = currentLine.FirstDocumentLine.Offset;
			int length = currentLine.LastDocumentLine.EndOffset - offset;
			if (_editor is MarkDownEditor markDownEditor && markDownEditor.TryGetSemanticBlockAtOffset(offset, out var block) && block != null && block.Kind == MarkdownBlockKind.CodeBlock)
			{
				continue;
			}
			string originalText = _editor.GetEditBox().Document.GetText(offset, length);
			string textWithout;
			originalText = (textWithout = Regex.Replace(originalText, "[\\u2018\\u2019\\u201A\\u201B\\u2032\\u2035\\u3040-\\u309F\\u30A0-\\u30FF\\u3100-\\u312F\\u3100-\\u312F\\u31A0-\\u31BF\\u3400-\\u4DBF\\u4E00-\\u9FFF\\uAC00-\\uD7AF\\uF900-\\uFAFF\\uFF66-\\uFFDF]", " "));
			textWithout = _uriFinderRegex.Replace(textWithout, "");
			textWithout = _mardownUri.Replace(textWithout, "");
			string[] words = null;
			await Task.Run(delegate
			{
				IEnumerable<string> source = from s in _wordSeparatorRegex.Split(textWithout)
					where !string.IsNullOrEmpty(s)
					select s;
				words = source.Where((string w) => !Regex.Match(w, "\\d").Success).ToArray();
			});
			int num = 0;
			string[] array = words;
			foreach (string text in array)
			{
				if (num >= 20)
				{
					break;
				}
				string text2 = text.Trim('\'', '_', '-');
				int num3 = currentLine.FirstDocumentLine.Offset + originalText.IndexOf(text2, startIndex, StringComparison.InvariantCultureIgnoreCase);
				if (num3 < 0)
				{
					break;
				}
				if (!_spellingService.Spell(text2))
				{
					if (_editor is MarkDownEditor markDownEditor2 && markDownEditor2.IsDocumentOffsetHiddenByHeadingFold(num3))
					{
						startIndex = originalText.IndexOf(text, startIndex, StringComparison.InvariantCultureIgnoreCase) + text.Length;
						continue;
					}
					TextSegment item = new TextSegment
					{
						StartOffset = num3,
						Length = text.Length
					};
					_spellCheckRenderer.ErrorSegments.Add(item);
					num++;
				}
				startIndex = originalText.IndexOf(text, startIndex, StringComparison.InvariantCultureIgnoreCase) + text.Length;
			}
		}
		_editor.GetEditBox().TextArea.TextView.Redraw();
		_textChanged = false;
	}

	private void ClearSpellCheckErrors()
	{
		_spellCheckRenderer?.ErrorSegments.Clear();
	}

	public void SetTextChanged(bool change, bool delay = true)
	{
		_spellCheckRenderer.ErrorSegments.Clear();
		_textChanged = change;
		_delay = delay;
	}
}
