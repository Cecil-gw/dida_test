using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Config;

namespace ticktick_WPF.Views.MarkDown.SpellCheck;

public static class SpellCheckSuggestion
{
	private static bool IsAlternateAppsKeyShortcut
	{
		get
		{
			if (Keyboard.IsKeyDown(Key.F10))
			{
				if (!Keyboard.IsKeyDown(Key.LeftShift))
				{
					return Keyboard.IsKeyDown(Key.RightShift);
				}
				return true;
			}
			return false;
		}
	}

	public static void SpellCheckSuggestions(ISpellChecker editor, ContextMenu contextMenu)
	{
		if (editor.SpellCheckProvider == null)
		{
			return;
		}
		int offset;
		if (IsAlternateAppsKeyShortcut)
		{
			offset = editor.GetEditBox().SelectionStart;
		}
		else
		{
			TextViewPosition? positionFromPoint = editor.GetEditBox().GetPositionFromPoint(Mouse.GetPosition(editor.GetEditBox()));
			if (!positionFromPoint.HasValue)
			{
				return;
			}
			offset = editor.GetEditBox().Document.GetOffset(positionFromPoint.Value.Line, positionFromPoint.Value.Column);
		}
		TextSegment textSegment = editor.SpellCheckProvider.GetSpellCheckErrors().FirstOrDefault((TextSegment segment) => segment.StartOffset <= offset && segment.EndOffset >= offset && segment.EndOffset <= editor.GetEditBox().Text.Length);
		if (textSegment == null)
		{
			return;
		}
		DocumentLine lineByOffset = editor.GetEditBox().Document.GetLineByOffset(offset);
		if (offset == lineByOffset.Offset || offset == lineByOffset.EndOffset)
		{
			return;
		}
		string text = editor.GetEditBox().Document.GetText(textSegment);
		List<string> list = editor.SpellCheckProvider.GetSpellCheckSuggestions(text).ToList();
		if (list.Count > 0)
		{
			foreach (string item in list)
			{
				contextMenu.Items.Add(SpellSuggestMenuItem(editor, item, textSegment));
			}
		}
		else
		{
			contextMenu.Items.Add(new EditMenuItem
			{
				Title = Utils.GetString("NoSpellSuggest"),
				IsEnabled = false
			});
		}
		contextMenu.Items.Add(new EditMenuItem
		{
			Title = Utils.GetString("IgnoreSpelling"),
			Command = SpellCheckUtils.IgnoreSpellingError,
			CommandParameter = new SpellCheckHandler(editor, "", text, textSegment)
		});
	}

	private static MenuItem SpellSuggestMenuItem(ISpellChecker editor, string header, TextSegment segment)
	{
		return new EditMenuItem
		{
			Title = header,
			FontWeight = FontWeights.Bold,
			Command = SpellCheckUtils.CorrectSpellingError,
			CommandParameter = new SpellCheckHandler(editor, header, null, segment)
		};
	}

	public static void AddSpellCheckLanguageMenu(ContextMenu contextMenu)
	{
		bool spellCheckEnable = LocalSettings.Settings.SpellCheckEnable;
		contextMenu.Items.Add(new EditMenuItem
		{
			Title = Utils.GetString("SpellingCheck"),
			Items = 
			{
				(object)new EditMenuItem(184.0)
				{
					Title = Utils.GetString(spellCheckEnable ? "CloseSpellCheck" : "OpenSpellCheck"),
					Command = SpellCheckUtils.OpenOrClose,
					CommandParameter = !spellCheckEnable
				},
				(object)new EditMenuItem
				{
					Title = "English(US)",
					Command = SpellCheckUtils.LanguageSelected,
					CommandParameter = "en-US",
					IsChecked = (LocalSettings.Settings.SpellCheckLanguage == "en-US" || string.IsNullOrEmpty(LocalSettings.Settings.SpellCheckLanguage)),
					IsEnabled = (spellCheckEnable && !SpellCheckUtils.Downloading)
				},
				(object)new EditMenuItem
				{
					Title = "English(UK)",
					Command = SpellCheckUtils.LanguageSelected,
					CommandParameter = "en-GB",
					IsChecked = (LocalSettings.Settings.SpellCheckLanguage == "en-GB"),
					IsEnabled = (spellCheckEnable && !SpellCheckUtils.Downloading)
				},
				(object)new EditMenuItem
				{
					Title = "Fançais",
					Command = SpellCheckUtils.LanguageSelected,
					CommandParameter = "fr-FR",
					IsChecked = (LocalSettings.Settings.SpellCheckLanguage == "fr-FR"),
					IsEnabled = (spellCheckEnable && !SpellCheckUtils.Downloading)
				},
				(object)new EditMenuItem
				{
					Title = "Pусский",
					Command = SpellCheckUtils.LanguageSelected,
					CommandParameter = "ru-RU",
					IsChecked = (LocalSettings.Settings.SpellCheckLanguage == "ru-RU"),
					IsEnabled = (spellCheckEnable && !SpellCheckUtils.Downloading)
				},
				(object)new EditMenuItem
				{
					Title = "Deutsche",
					Command = SpellCheckUtils.LanguageSelected,
					CommandParameter = "de-DE",
					IsChecked = (LocalSettings.Settings.SpellCheckLanguage == "de-DE"),
					IsEnabled = (spellCheckEnable && !SpellCheckUtils.Downloading)
				},
				(object)new EditMenuItem
				{
					Title = "Español",
					Command = SpellCheckUtils.LanguageSelected,
					CommandParameter = "es-ES",
					IsChecked = (LocalSettings.Settings.SpellCheckLanguage == "es-ES"),
					IsEnabled = (spellCheckEnable && !SpellCheckUtils.Downloading)
				},
				(object)new EditMenuItem
				{
					Title = "Português (Brasil)",
					Command = SpellCheckUtils.LanguageSelected,
					CommandParameter = "pt-BR",
					IsChecked = (LocalSettings.Settings.SpellCheckLanguage == "pt-BR"),
					IsEnabled = (spellCheckEnable && !SpellCheckUtils.Downloading)
				},
				(object)new EditMenuItem
				{
					Title = "Português (Portugal)",
					Command = SpellCheckUtils.LanguageSelected,
					CommandParameter = "pt-PT",
					IsChecked = (LocalSettings.Settings.SpellCheckLanguage == "pt-PT"),
					IsEnabled = (spellCheckEnable && !SpellCheckUtils.Downloading)
				},
				(object)new EditMenuItem
				{
					Title = "Italiano",
					Command = SpellCheckUtils.LanguageSelected,
					CommandParameter = "it-IT",
					IsChecked = (LocalSettings.Settings.SpellCheckLanguage == "it-IT"),
					IsEnabled = (spellCheckEnable && !SpellCheckUtils.Downloading)
				}
			}
		});
	}
}
