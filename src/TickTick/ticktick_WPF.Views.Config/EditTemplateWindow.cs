using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Config;

public class EditTemplateWindow : Window, IComponentConnector
{
	private readonly string _title;

	private readonly List<string> _exsitTitles;

	private bool _isAdd;

	private bool _appendNameIndex;

	internal TextBlock AddTemplateText;

	internal TextBox TitleTextBox;

	internal Button ClearButton;

	internal TextBlock WarningText;

	internal Button OkButton;

	private bool _contentLoaded;

	public event EventHandler<string> Save;

	public event EventHandler<string> Replace;

	public EditTemplateWindow(string title, List<string> exsitTitles, bool isAdd = false, string desc = "", bool appendNameIndex = false)
	{
		InitializeComponent();
		_title = title;
		if (isAdd)
		{
			_isAdd = true;
			AddTemplateText.Visibility = Visibility.Visible;
			AddTemplateText.Text = desc;
			_title = string.Empty;
		}
		AddTemplateText.Visibility = (string.IsNullOrEmpty(desc) ? Visibility.Collapsed : Visibility.Visible);
		_exsitTitles = exsitTitles;
		_appendNameIndex = appendNameIndex;
		TitleTextBox.Text = title;
		OkButton.IsEnabled = !string.IsNullOrEmpty(title.Trim());
		DataObject.AddPastingHandler(TitleTextBox, OnPaste);
		base.Closing += delegate
		{
			base.Owner?.Activate();
		};
	}

	private async void OnWindowLoaded(object sender, RoutedEventArgs e)
	{
		await Task.Delay(50);
		TitleTextBox.Focus();
		TitleTextBox.SelectAll();
	}

	private void OnPaste(object sender, DataObjectPastingEventArgs e)
	{
		e.CancelCommand();
		if (e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText, autoConvert: true) && e.SourceDataObject.GetData(DataFormats.UnicodeText) is string text)
		{
			InsertPasteContent(text.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " "));
		}
	}

	private void InsertPasteContent(string text)
	{
		string text2 = TitleTextBox.Text;
		int selectionStart = TitleTextBox.SelectionStart;
		try
		{
			text2 = text2.Remove(selectionStart, TitleTextBox.SelectionLength);
			text2 = text2.Insert(selectionStart, text);
			TitleTextBox.Text = text2;
			TitleTextBox.SelectionStart = selectionStart + text.Length;
		}
		catch (Exception)
		{
		}
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void OnSaveClick(object sender, RoutedEventArgs e)
	{
		SaveTemplate();
	}

	private void SaveTemplate()
	{
		string e = TitleTextBox.Text.Trim();
		if (_exsitTitles != null && _exsitTitles.Contains(TitleTextBox.Text.Trim()) && TitleTextBox.Text.Trim() != _title)
		{
			if (!_appendNameIndex)
			{
				if (_isAdd)
				{
					CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("SaveTemplate"), Utils.GetString("ReplaceTemplateContent"), Utils.GetString("Replace"), Utils.GetString("Cancel"));
					customerDialog.Owner = this;
					customerDialog.ShowDialog();
					if (customerDialog.DialogResult == true)
					{
						this.Replace?.Invoke(null, TitleTextBox.Text.Trim());
						Close();
					}
				}
				else
				{
					WarningText.Visibility = Visibility.Visible;
					OkButton.IsEnabled = false;
				}
				return;
			}
			e = GetNewTemplateName(TitleTextBox.Text.Trim(), _title, _exsitTitles);
		}
		this.Save?.Invoke(null, e);
		Close();
	}

	private string GetNewTemplateName(string newName, string title, List<string> names)
	{
		if (string.Equals(newName, title))
		{
			return newName;
		}
		while (names.Contains(newName))
		{
			newName += "(1)";
		}
		return newName;
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private void OnEscKeyDown(object sender, KeyEventArgs e)
	{
		switch (e.Key)
		{
		case Key.Escape:
			Close();
			break;
		case Key.Return:
			if (OkButton.IsEnabled)
			{
				SaveTemplate();
			}
			break;
		}
	}

	private void ClearText(object sender, RoutedEventArgs e)
	{
		TitleTextBox.Text = "";
	}

	private void OnTitleTextChanged(object sender, TextChangedEventArgs e)
	{
		WarningText.Visibility = Visibility.Collapsed;
		OkButton.IsEnabled = !string.IsNullOrEmpty(TitleTextBox.Text.Trim());
		if (!string.IsNullOrEmpty(TitleTextBox.Text))
		{
			ClearButton.Visibility = Visibility.Visible;
		}
		else
		{
			ClearButton.Visibility = Visibility.Collapsed;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/config/edittemplatewindow.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			((EditTemplateWindow)target).Loaded += OnWindowLoaded;
			break;
		case 2:
			AddTemplateText = (TextBlock)target;
			break;
		case 3:
			TitleTextBox = (TextBox)target;
			TitleTextBox.KeyDown += OnEscKeyDown;
			TitleTextBox.TextChanged += OnTitleTextChanged;
			break;
		case 4:
			ClearButton = (Button)target;
			ClearButton.Click += ClearText;
			break;
		case 5:
			WarningText = (TextBlock)target;
			break;
		case 6:
			OkButton = (Button)target;
			OkButton.Click += OnSaveClick;
			break;
		case 7:
			((Button)target).Click += OnCancelClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
