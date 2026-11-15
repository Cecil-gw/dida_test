using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using Microsoft.Win32;
using ticktick_WPF.Framework.Markdown;
using ticktick_WPF.Util;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Detail;

public class TaskExportWindow : Window, IComponentConnector
{
	private readonly string _taskTitle;

	private readonly string _downloadTitle;

	private readonly string _markdownText;

	private readonly string _plainText;

	internal Grid Container;

	internal AnimateGroupTitle FormatSwitch;

	internal TextBox Preview;

	internal Button DownloadButton;

	private bool _contentLoaded;

	private bool IsMarkdown => FormatSwitch.SelectedIndex == 0;

	public TaskExportWindow(string title, string description, bool markdown, string checklist = "", string plainTitle = null, string plainChecklist = null, string attachments = "")
	{
		InitializeComponent();
		Preview.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, delegate(object sender, ExecutedRoutedEventArgs args)
		{
			CopyText(Preview.SelectedText);
			args.Handled = true;
		}, delegate(object sender, CanExecuteRoutedEventArgs args)
		{
			args.CanExecute = Preview.SelectionLength > 0;
			args.Handled = true;
		}));
		base.PreviewKeyDown += delegate(object sender, KeyEventArgs args)
		{
			if (args.Key == Key.Escape)
			{
				Close();
				args.Handled = true;
			}
		};
		_taskTitle = title ?? string.Empty;
		_downloadTitle = plainTitle ?? LinkTextEditBox.GetPlainTextWithLinks(_taskTitle);
		string text = MarkdownUtils.RemoveBlockMetadataComments(description ?? string.Empty);
		_markdownText = BuildExportText(_taskTitle, text, checklist, attachments);
		_plainText = BuildExportText(plainTitle ?? _taskTitle, MarkdownUtils.RemoveBlockMetadataComments(MarkdownSemanticConverter.GetPlainTextWithBlockMarkers(text)), plainChecklist ?? checklist, MarkdownSemanticConverter.GetPlainTextWithBlockMarkers(attachments));
		FormatSwitch.SetSelectedIndex((!markdown) ? 1 : 0);
		FormatSwitch.SelectedTitleChanged += delegate
		{
			UpdatePreview();
		};
		UpdatePreview();
	}

	private static string BuildExportText(string title, string body, string checklist, string attachments)
	{
		return "# " + title + ((body.Length > 0) ? ("\n\n" + body) : string.Empty) + ((!string.IsNullOrEmpty(checklist)) ? ("\n\n" + checklist) : string.Empty) + ((!string.IsNullOrEmpty(attachments)) ? ("\n\n" + attachments) : string.Empty);
	}

	private void UpdatePreview()
	{
		Preview.Text = (IsMarkdown ? _markdownText : _plainText);
		Preview.ScrollToHome();
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private void CopyClick(object sender, RoutedEventArgs e)
	{
		CopyText(Preview.Text);
	}

	private void CopyText(string text)
	{
		try
		{
			Clipboard.SetDataObject(ClipboardRichTextHelper.CreateExportTextDataObject(NormalizeExportLineEndings(text)), copy: true);
		}
		catch (ExternalException)
		{
		}
		UiUtils.Toast(Container, Utils.GetString("Copied"));
	}

	private static string NormalizeExportLineEndings(string text)
	{
		return (text ?? string.Empty).Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
	}

	private void DownloadClick(object sender, RoutedEventArgs e)
	{
		string text = new string(_downloadTitle.Select((char c) => (!Path.GetInvalidFileNameChars().Contains(c)) ? c : '_').ToArray()).Trim().TrimEnd('.');
		if (text.Length > 120)
		{
			text = text.Substring(0, 120).TrimEnd('.');
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = Utils.GetString("Export");
		}
		string value = text.Split('.')[0].ToUpperInvariant();
		if (new string[22]
		{
			"CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6",
			"COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7",
			"LPT8", "LPT9"
		}.Contains(value))
		{
			text = "_" + text;
		}
		string text2 = (IsMarkdown ? ".md" : ".txt");
		SaveFileDialog saveFileDialog = new SaveFileDialog
		{
			FileName = text + text2,
			DefaultExt = text2,
			AddExtension = true,
			OverwritePrompt = true,
			Filter = (IsMarkdown ? "Markdown" : Utils.GetString("PlainText")) + "|*" + text2
		};
		if (saveFileDialog.ShowDialog(this) != true)
		{
			return;
		}
		try
		{
			File.WriteAllText(saveFileDialog.FileName, NormalizeExportLineEndings(Preview.Text), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			UiUtils.Toast(Container, Utils.GetString("DownloadSuccessful"));
		}
		catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is SecurityException)
		{
			UiUtils.Toast(Container, Utils.GetString("SaveFailed"));
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/detail/taskexportwindow.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	internal Delegate _CreateDelegate(Type delegateType, string handler)
	{
		return Delegate.CreateDelegate(delegateType, this, handler);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			Container = (Grid)target;
			break;
		case 2:
			FormatSwitch = (AnimateGroupTitle)target;
			break;
		case 3:
			Preview = (TextBox)target;
			break;
		case 4:
			((Button)target).Click += CopyClick;
			break;
		case 5:
			DownloadButton = (Button)target;
			DownloadButton.Click += DownloadClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
