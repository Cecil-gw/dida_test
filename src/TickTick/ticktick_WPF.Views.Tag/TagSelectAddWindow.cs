using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Tag;

public class TagSelectAddWindow : MyWindow, IComponentConnector
{
	private double _left;

	private double _top;

	internal Border ShadowBorder;

	internal Border TagBorder;

	internal TextBox TitleText;

	internal Grid TagPanel;

	internal TagSelectionControl TagItems;

	internal Border ErrorBorder;

	internal Popup ErrorPopup;

	private bool _contentLoaded;

	public event EventHandler<string> TagExit;

	public TagSelectAddWindow(bool canCreateTag = true, string text = null)
	{
		InitializeComponent();
		TagItems.CanCreateTag = canCreateTag;
		if (text != null)
		{
			TagBorder.Visibility = Visibility.Hidden;
		}
	}

	public void Show(double left, double top)
	{
		_left = left;
		_top = top;
		InitEvents();
		Show();
		FocusTextEnd();
	}

	private void InitEvents()
	{
		TagItems.TagSelected -= OnTagSelected;
		TagItems.TagSelected += OnTagSelected;
		TagItems.TagsCountChanged -= OnTagsCountChanged;
		TagItems.TagsCountChanged += OnTagsCountChanged;
	}

	private void OnTagsCountChanged(object sender, int count)
	{
		TagPanel.Visibility = ((count <= 0) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void OnTagSelected(object sender, string tag)
	{
		Hide();
	}

	private async void FocusTextEnd()
	{
		TitleText.Focus();
		await Task.Delay(10);
		TitleText.Select(TitleText.Text.Length, 0);
	}

	private void OnTitleKeyUp(object sender, KeyEventArgs e)
	{
		switch (e.Key)
		{
		case Key.Escape:
			ExitTagMode();
			break;
		case Key.Up:
			TagItems.MoveUp();
			break;
		case Key.Down:
			TagItems.MoveDown();
			break;
		case Key.Tab:
			if (Utils.IfShiftPressed())
			{
				TagItems.MoveUp();
			}
			else
			{
				TagItems.MoveDown();
			}
			break;
		case Key.Return:
			TryAddTag();
			e.Handled = true;
			break;
		}
	}

	private void TryAddTag()
	{
		if (CheckIfTextValid(TitleText.Text))
		{
			TagItems.TrySelectFocusedTag();
		}
		else
		{
			Utils.Toast(Utils.GetString("TagNotValid"));
		}
	}

	private static bool CheckIfTextValid(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		if (text.StartsWith("#") && !NameUtils.IsValidName(text.Substring(1)))
		{
			return false;
		}
		return true;
	}

	private void ExitTagMode()
	{
		this.TagExit?.Invoke(this, TitleText.Text);
		Hide();
	}

	private void InitLocation()
	{
		if (double.IsNaN(_left) || double.IsInfinity(_left))
		{
			_left = 0.0;
		}
		if (double.IsNaN(_top) || double.IsInfinity(_top))
		{
			_top = 0.0;
		}
		base.Left = _left;
		base.Top = _top;
	}

	private void OnTitleTextChanged(object sender, TextChangedEventArgs e)
	{
		if (TitleText.Text == "# ")
		{
			ExitTagMode();
			return;
		}
		string text = (string.IsNullOrEmpty(TitleText.Text) ? string.Empty : TitleText.Text);
		if (!Utils.IsWindows7() && text != "#" && text.EndsWith("#"))
		{
			ExitTagMode();
			TitleText.TextChanged -= OnTitleTextChanged;
			TitleText.Text = string.Empty;
			TitleText.TextChanged += OnTitleTextChanged;
		}
		if (text.Contains(" "))
		{
			ExitTagMode();
		}
		if (text == "#")
		{
			TagBorder.Background = new SolidColorBrush(Colors.Transparent);
			ShadowBorder.Background = new SolidColorBrush(Colors.Transparent);
			TitleText.Foreground = new SolidColorBrush(Colors.Transparent);
			TagItems?.Filter(string.Empty);
			return;
		}
		if (!string.IsNullOrEmpty(text) && text.StartsWith("#"))
		{
			text = text.Substring(1);
		}
		ShadowBorder.Background = new SolidColorBrush(Colors.White);
		TagBorder.Background = ThemeUtil.GetPrimaryColor(0.18);
		TitleText.Foreground = ThemeUtil.GetColor("PrimaryColor");
		ShowErrorHint(!NameUtils.IsValidName(text, checkSpace: false));
		if (!string.IsNullOrEmpty(text))
		{
			if (text.EndsWith(" "))
			{
				this.TagExit?.Invoke(this, " ");
				Hide();
			}
			else
			{
				TagItems?.Filter(text);
			}
		}
		else
		{
			ExitTagMode();
		}
	}

	private void ShowErrorHint(bool show)
	{
		if (ErrorBorder != null)
		{
			ErrorBorder.Visibility = ((!show) ? Visibility.Collapsed : Visibility.Visible);
			ErrorPopup.IsOpen = show;
		}
	}

	private void OnWindowDeactivated(object sender, EventArgs e)
	{
		Close();
	}

	private void OnWindowLoaded(object sender, RoutedEventArgs e)
	{
		InitLocation();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/tag/tagselectaddwindow.xaml", UriKind.Relative);
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
			ShadowBorder = (Border)target;
			break;
		case 2:
			TagBorder = (Border)target;
			break;
		case 3:
			TitleText = (TextBox)target;
			TitleText.PreviewKeyUp += OnTitleKeyUp;
			TitleText.TextChanged += OnTitleTextChanged;
			break;
		case 4:
			TagPanel = (Grid)target;
			break;
		case 5:
			TagItems = (TagSelectionControl)target;
			break;
		case 6:
			ErrorBorder = (Border)target;
			break;
		case 7:
			ErrorPopup = (Popup)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
