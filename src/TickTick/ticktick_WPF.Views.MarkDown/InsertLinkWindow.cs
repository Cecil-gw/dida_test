using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Views.MarkDown;

public class InsertLinkWindow : Window, IComponentConnector
{
	private readonly ILinkTextEditor _editor;

	private readonly string _originalTitle;

	private readonly string _originalUrl;

	private bool _isNew;

	internal TextBlock TitleText;

	internal TextBox LinkNameText;

	internal TextBox LinkUrlText;

	internal Button SaveButton;

	internal Button DeleteLink;

	private bool _contentLoaded;

	public LinkLineInfoModel _originalModel { get; set; }

	public InsertLinkWindow(ILinkTextEditor editor, LinkLineInfoModel model)
	{
		InitializeComponent();
		_editor = editor;
		_originalTitle = model.Text;
		_originalUrl = model.Url;
		_isNew = model.IsNew;
		_originalModel = model;
		LinkNameText.Text = model.Text;
		LinkUrlText.Text = model.Url;
		if ((string.IsNullOrEmpty(model.Text) && string.IsNullOrEmpty(model.Url)) || model.Source == LinkInfoSource.Link)
		{
			LinkNameText.Focus();
		}
		else
		{
			LinkUrlText.CaretIndex = ((!string.IsNullOrEmpty(model.Url)) ? model.Url.Length : 0);
			LinkUrlText.Focus();
		}
		if (string.IsNullOrEmpty(model.Url))
		{
			TitleText.Text = Utils.GetString("AddLink");
			DeleteLink.Visibility = Visibility.Collapsed;
		}
		base.Loaded += OnLoaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		base.Loaded -= OnLoaded;
		double left = base.Left;
		double top = base.Top;
		WindowHelper.MoveTo(this, (int)left, (int)top);
		base.Left = left;
		base.Top = top;
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private string GetLinkText()
	{
		string text = LinkUrlText.Text;
		if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(LinkNameText.Text))
		{
			text = text + " \"" + LinkNameText.Text + "\"";
		}
		return text;
	}

	private async void OnSaveClick(object sender, RoutedEventArgs e)
	{
		_editor.UnRegisterCaretChanged();
		if (string.IsNullOrWhiteSpace(LinkNameText.Text))
		{
			ProjectTask projectTask = TaskUtils.ParseTaskUrlWithoutTitle(LinkUrlText.Text);
			if (projectTask != null)
			{
				TaskModel taskModel = await TaskDao.GetThinTaskById(projectTask.TaskId);
				if (taskModel != null)
				{
					LinkNameText.Text = taskModel.title;
				}
			}
			if (string.IsNullOrEmpty(LinkNameText.Text))
			{
				LinkNameText.Text = Utils.GetString("MyTask");
			}
		}
		if (!_isNew)
		{
			string original = ((_originalModel.Source == LinkInfoSource.Link) ? _originalUrl : ("[" + _originalTitle + "](" + _originalUrl + ")"));
			string revised = "[" + LinkNameText.Text + "](" + LinkUrlText.Text + ")";
			if (string.IsNullOrEmpty(LinkUrlText.Text))
			{
				revised = LinkNameText.Text ?? "";
			}
			if (_editor is MarkDownEditor markDownEditor)
			{
				markDownEditor.ReplaceLinkText(original, revised, _originalModel);
			}
			else
			{
				EditorUtilities.ReplaceLink(_editor.GetEditBox(), original, revised, _originalModel);
			}
		}
		else if (_editor is MarkDownEditor markDownEditor2)
		{
			markDownEditor2.InsertHyperlinkText(GetLinkText());
		}
		else
		{
			EditorUtilities.InsertHyperlink(_editor.GetEditBox(), GetLinkText());
		}
		_editor.RegisterCaretChanged();
		Close();
	}

	private void OnDeleteLinkClick(object sender, RoutedEventArgs e)
	{
		_editor.UnRegisterCaretChanged();
		string original = "[" + _originalTitle + "](" + _originalUrl + ")";
		string revised = LinkNameText.Text ?? "";
		if (_editor is MarkDownEditor markDownEditor)
		{
			markDownEditor.ReplaceLinkText(original, revised, _originalModel);
		}
		else
		{
			EditorUtilities.ReplaceLink(_editor.GetEditBox(), original, revised, _originalModel);
		}
		_editor.RegisterCaretChanged();
		Close();
	}

	private void OnLinkNameKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Return)
		{
			LinkUrlText.Focus();
		}
	}

	private void OnLinkUrlKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Return)
		{
			OnSaveClick(sender, e);
		}
	}

	public void TryShowDialog(double x, double y)
	{
		base.WindowStartupLocation = WindowStartupLocation.CenterScreen;
		ShowDialog();
		base.Left = x;
		base.Top = y;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/markdown/insertlinkwindow.xaml", UriKind.Relative);
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
			TitleText = (TextBlock)target;
			break;
		case 2:
			LinkNameText = (TextBox)target;
			LinkNameText.KeyDown += OnLinkNameKeyDown;
			break;
		case 3:
			LinkUrlText = (TextBox)target;
			LinkUrlText.KeyDown += OnLinkUrlKeyDown;
			break;
		case 4:
			SaveButton = (Button)target;
			SaveButton.Click += OnSaveClick;
			break;
		case 5:
			DeleteLink = (Button)target;
			DeleteLink.Click += OnDeleteLinkClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
