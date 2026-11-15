using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Subscribe;

public class EmailTaskCodeWindow : Window, IComponentConnector
{
	internal Grid TextGrid;

	internal TextBox CodeText;

	internal StackPanel OptionPanel;

	internal TextBlock MessageText;

	internal Run Run1;

	internal Run Email;

	internal Run Run2;

	internal Border ToastBorder;

	internal TextBlock ToastText;

	private bool _contentLoaded;

	public EmailTaskCodeWindow()
	{
		InitializeComponent();
		GetCode(isNew: false);
		SetMessageText();
	}

	private async void GetCode(bool isNew)
	{
		string text = await Communicator.GetMailTaskCode(isNew);
		if (!string.IsNullOrEmpty(text))
		{
			CodeText.Text = "todo+" + text + "@mail." + (Utils.IsDida() ? "dida365" : "ticktick") + ".com";
		}
	}

	private void SetMessageText()
	{
		string text = Utils.GetString("EmailTaskMessage").Replace("{0}", Utils.GetAppName());
		int num = text.IndexOf("{1}", StringComparison.Ordinal);
		if (num >= 0)
		{
			Run1.Text = text.Substring(0, num);
			Run2.Text = text.Substring(num + 3);
		}
		else
		{
			MessageText.Text = text;
		}
		Email.Text = (Utils.IsDida() ? " todo@mail.dida365.com " : " todo@mail.ticktick.com ");
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private void OnEmailClick(object sender, MouseButtonEventArgs e)
	{
		Process.Start("mailto:" + (Utils.IsDida() ? "todo@mail.dida365.com" : "todo@mail.ticktick.com"));
	}

	private void OnTextMouseEnter(object sender, MouseEventArgs e)
	{
		CodeText.Padding = new Thickness(10.0, 0.0, OptionPanel.ActualWidth, 0.0);
	}

	private void OnTextMouseLeave(object sender, MouseEventArgs e)
	{
		CodeText.Padding = new Thickness(10.0, 0.0, 10.0, 0.0);
	}

	private async void OnTextClick(object sender, MouseButtonEventArgs e)
	{
		CodeText.Select(0, CodeText.Text.Length);
		Mouse.Capture(null);
		await Task.Delay(40);
		OnCopyClick(null, null);
	}

	private void OnCopyClick(object sender, MouseButtonEventArgs e)
	{
		try
		{
			Clipboard.SetText(CodeText.Text);
			Toast(Utils.GetString("Copied"));
		}
		catch (Exception)
		{
		}
	}

	private void OnResetClick(object sender, MouseButtonEventArgs e)
	{
		GetCode(isNew: true);
	}

	public void Toast(string toastText)
	{
		ToastBorder.Visibility = Visibility.Visible;
		ToastText.Text = toastText;
		Storyboard storyboard = (Storyboard)FindResource("ShowToast");
		ToastBorder.BeginStoryboard(storyboard);
	}

	private void OnToasted(object sender, EventArgs e)
	{
		ToastBorder.Visibility = Visibility.Collapsed;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/subscribe/emailtaskcodewindow.xaml", UriKind.Relative);
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
			((Storyboard)target).Completed += OnToasted;
			break;
		case 2:
			TextGrid = (Grid)target;
			TextGrid.MouseEnter += OnTextMouseEnter;
			TextGrid.MouseLeave += OnTextMouseLeave;
			break;
		case 3:
			CodeText = (TextBox)target;
			CodeText.PreviewMouseLeftButtonUp += OnTextClick;
			break;
		case 4:
			OptionPanel = (StackPanel)target;
			break;
		case 5:
			((TextBlock)target).MouseLeftButtonUp += OnCopyClick;
			break;
		case 6:
			((TextBlock)target).MouseLeftButtonUp += OnResetClick;
			break;
		case 7:
			MessageText = (TextBlock)target;
			break;
		case 8:
			Run1 = (Run)target;
			break;
		case 9:
			Email = (Run)target;
			Email.MouseLeftButtonUp += OnEmailClick;
			break;
		case 10:
			Run2 = (Run)target;
			break;
		case 11:
			ToastBorder = (Border)target;
			break;
		case 12:
			ToastText = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
