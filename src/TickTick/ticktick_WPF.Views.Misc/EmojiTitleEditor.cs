using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using Emoji.Wpf;
using ticktick_WPF.Views.MarkDown;

namespace ticktick_WPF.Views.Misc;

public class EmojiTitleEditor : UserControl, IComponentConnector
{
	internal Border Container;

	internal EmojiEditor Editor;

	internal EmjTextBlock Display;

	internal Grid ErrorGrid;

	internal TextBlock ErrorText;

	private bool _contentLoaded;

	public Func<string, string> CheckFunc { get; set; }

	public event EventHandler<string> TextChanged;

	public EmojiTitleEditor()
	{
		InitializeComponent();
		Editor.EditBox.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
		Editor.EditBox.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
		Editor.EditBox.FontWeight = FontWeights.Bold;
	}

	public void SetCheckFunc(Func<string, string> func)
	{
		CheckFunc = func;
	}

	public void SetText(string text)
	{
		Editor.TextChanged -= OnTextChanged;
		Editor.Text = string.Empty;
		Editor.Visibility = Visibility.Collapsed;
		ErrorGrid.Visibility = Visibility.Collapsed;
		Display.Text = text;
		Display.Visibility = Visibility.Visible;
	}

	public void SetMaxWidth(double width)
	{
		if (width > 0.0)
		{
			base.MaxWidth = width;
		}
	}

	public void SetEnable(bool projectCanEdit)
	{
		Container.IsHitTestVisible = projectCanEdit;
	}

	private async void OnClick(object sender, MouseButtonEventArgs e)
	{
		if (Display.IsVisible)
		{
			e.Handled = true;
			Editor.TextChanged -= OnTextChanged;
			Editor.EditBox.LostFocus -= OnLostFocus;
			Editor.Text = Display.Text;
			Editor.Visibility = Visibility.Visible;
			Editor.Width = Math.Min(Display.ActualWidth + 80.0, base.MaxWidth);
			Display.Visibility = Visibility.Collapsed;
			await Task.Delay(100);
			Editor.FocusEnd();
			Editor.EditBox.LostFocus += OnLostFocus;
			Editor.TextChanged += OnTextChanged;
		}
	}

	private void OnTextChanged(object sender, EventArgs e)
	{
		string text = CheckFunc(Editor.Text);
		ErrorGrid.Visibility = (string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible);
		ErrorText.Text = text;
	}

	private async void OnLostFocus(object sender, RoutedEventArgs e)
	{
		if (!ErrorGrid.IsVisible)
		{
			Editor.EditBox.LostFocus -= OnLostFocus;
			TrySaveText();
		}
	}

	private void TrySaveText()
	{
		Editor.TextChanged -= OnTextChanged;
		Editor.Visibility = Visibility.Collapsed;
		if (!string.IsNullOrEmpty(Editor.Text.Trim()) && Editor.Text.Trim() != Display.Text)
		{
			Display.Text = Editor.Text.Trim();
			this.TextChanged?.Invoke(this, Editor.Text.Trim());
		}
		Display.Visibility = Visibility.Visible;
		Editor.Text = string.Empty;
	}

	private void OnEnterUp(object sender, EventArgs e)
	{
		if (!ErrorGrid.IsVisible)
		{
			Editor.EditBox.LostFocus -= OnLostFocus;
			TrySaveText();
		}
	}

	private void OnKeyUp(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Escape)
		{
			if (ErrorGrid.IsVisible)
			{
				ErrorGrid.Visibility = Visibility.Collapsed;
			}
			SetText(Display.Text);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/misc/emojititleeditor.xaml", UriKind.Relative);
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
			Container = (Border)target;
			Container.PreviewMouseLeftButtonUp += OnClick;
			break;
		case 2:
			Editor = (EmojiEditor)target;
			break;
		case 3:
			Display = (EmjTextBlock)target;
			break;
		case 4:
			ErrorGrid = (Grid)target;
			break;
		case 5:
			ErrorText = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
