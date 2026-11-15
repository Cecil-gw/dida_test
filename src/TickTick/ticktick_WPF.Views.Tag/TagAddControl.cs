using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Tag;

public class TagAddControl : UserControl, IComponentConnector
{
	private bool _tagSelected;

	private bool _showError;

	private bool _chineseInput;

	private TagSelectionControl TagItems;

	private Popup TagDisplayPopup;

	internal Border TextBorder;

	internal Grid AddTagIcon;

	internal Grid TextGrid;

	internal TextBox AddTagTextBox;

	internal TextBlock AddTagHint;

	internal Border ErrorBorder;

	internal Popup ErrorPopup;

	internal Run ErrorText;

	private bool _contentLoaded;

	public TagAddControl()
	{
		InitializeComponent();
		base.Unloaded += delegate
		{
			DelayActionHandlerCenter.RemoveAction("TagAddControlTextChanged");
			if (TagDisplayPopup != null)
			{
				TagDisplayPopup.IsOpen = false;
			}
		};
	}

	private TagDisplayControl FindParent()
	{
		return Utils.FindParent<TagDisplayControl>(this);
	}

	private void OnTagPanelChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		FindParent()?.NotifyTagPanelVisibleChanged(TagItems.Visibility);
	}

	private TagDisplayControl GetParent()
	{
		return Utils.FindParent<TagDisplayControl>(this);
	}

	private void OnTagSelected(object sender, string tag)
	{
		TagDisplayControl tagDisplayControl = FindParent();
		if (!string.IsNullOrEmpty(AddTagTextBox.Text))
		{
			AddTagTextBox.TextChanged -= OnTextChanged;
			AddTagTextBox.Text = string.Empty;
			AddTagTextBox.TextChanged += OnTextChanged;
		}
		ResetState();
		tagDisplayControl?.NotifyTagPanelVisibleChanged(Visibility.Collapsed);
		tagDisplayControl?.AddNewTag(tag.ToLower());
	}

	private void OnAddTagClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		if (AddTagIcon.Visibility == Visibility.Visible)
		{
			AddTagIcon.Visibility = Visibility.Collapsed;
			TextGrid.Visibility = Visibility.Visible;
			AddTagHint.Visibility = Visibility.Visible;
			AddTagTextBox.Focus();
			DisplaySelectTags(string.Empty);
		}
	}

	private void OnAddTagTextChanged()
	{
		if (!_chineseInput)
		{
			string text = AddTagTextBox.Text;
			AddTagHint.Visibility = ((!string.IsNullOrEmpty(text)) ? Visibility.Collapsed : Visibility.Visible);
			if (!NameUtils.IsValidName(text))
			{
				ShowTagInValid();
			}
			else if (CheckTagExisted(text))
			{
				ShowTagExisted();
			}
			else
			{
				DisplaySelectTags(text);
			}
		}
	}

	private void InitPopup()
	{
		if (TagDisplayPopup == null)
		{
			TagDisplayPopup = new Popup
			{
				StaysOpen = false,
				AllowsTransparency = true,
				Placement = PlacementMode.Bottom
			};
			TagDisplayPopup.PlacementTarget = this;
			TagDisplayPopup.Closed += OnTagPopupClosed;
			ContentControl contentControl = new ContentControl();
			contentControl.SetResourceReference(FrameworkElement.StyleProperty, "PopupContentStyle");
			TagItems = new TagSelectionControl
			{
				BatchMode = false
			};
			TagItems.TagSelected += OnTagSelected;
			TagItems.IsVisibleChanged += OnTagPanelChanged;
			contentControl.Content = TagItems;
			TagDisplayPopup.Child = contentControl;
		}
	}

	private void DisplaySelectTags(string prefix)
	{
		InitPopup();
		ErrorPopup.IsOpen = false;
		_showError = false;
		ErrorBorder.Visibility = Visibility.Collapsed;
		if (TagDisplayPopup != null)
		{
			TagDisplayPopup.IsOpen = true;
		}
		if (base.DataContext is TagLabelViewModel)
		{
			TagItems.Filter(prefix);
		}
	}

	private void OnTagPopupClosed(object sender, EventArgs e)
	{
		FindParent()?.NotifyTagPanelVisibleChanged(Visibility.Collapsed);
	}

	private bool CheckTagExisted(string prefix)
	{
		return GetParent()?.CheckIfTagExisted(prefix) ?? false;
	}

	private void ShowTagExisted()
	{
		ErrorText.Text = Utils.GetString("TagExisted");
		ShowErrorPopup();
	}

	private void ShowTagInValid()
	{
		ErrorText.Text = Utils.GetString("TagNotValid");
		ShowErrorPopup();
	}

	private void ShowErrorPopup()
	{
		ErrorPopup.IsOpen = true;
		ErrorBorder.Visibility = Visibility.Visible;
		if (TagDisplayPopup != null)
		{
			TagDisplayPopup.IsOpen = false;
		}
		_showError = true;
	}

	private void ResetState()
	{
		AddTagHint.Visibility = Visibility.Collapsed;
		TextGrid.Visibility = Visibility.Collapsed;
		AddTagIcon.Visibility = Visibility.Visible;
		AddTagTextBox.Text = string.Empty;
		_showError = false;
		if (TagDisplayPopup != null)
		{
			TagDisplayPopup.IsOpen = false;
		}
	}

	private void OnAddTagTextKeyDown(object sender, KeyEventArgs e)
	{
		switch (e.Key)
		{
		case Key.Escape:
			ResetState();
			break;
		case Key.Up:
			TagItems.MoveUp();
			break;
		case Key.Tab:
		case Key.Down:
			e.Handled = true;
			MoveTagSelectedItem();
			break;
		case Key.Return:
			TrySelectTag();
			break;
		}
		if ((e.ImeProcessedKey >= Key.A && e.ImeProcessedKey <= Key.Z) || e.ImeProcessedKey == Key.Back || e.ImeProcessedKey == Key.Left || e.ImeProcessedKey == Key.Right)
		{
			_chineseInput = true;
			AddTagHint.Visibility = Visibility.Collapsed;
		}
		else
		{
			_chineseInput = false;
		}
	}

	private void MoveTagSelectedItem()
	{
		if (!Utils.IfShiftPressed())
		{
			TagItems.MoveDown();
		}
		else
		{
			TagItems.MoveUp();
		}
	}

	private void TrySelectTag()
	{
		if (!NameUtils.IsValidName(AddTagTextBox.Text))
		{
			ShowTagInValid();
		}
		else if (CheckTagExisted(AddTagTextBox.Text))
		{
			ShowTagExisted();
		}
		else
		{
			TagItems.TrySelectFocusedTag();
		}
	}

	private void OnPopupClosed(object sender, EventArgs e)
	{
		if (!TextGrid.IsMouseOver && !_showError)
		{
			ResetState();
		}
	}

	private void OnTextBoxClick(object sender, MouseButtonEventArgs e)
	{
		AddTagTextBox.Focus();
		OnAddTagTextChanged();
	}

	private void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
	{
		if (!TextGrid.IsMouseOver && !TagItems.IsMouseOver)
		{
			ResetState();
		}
	}

	private async void OnTextChanged(object sender, TextChangedEventArgs e)
	{
		if (TextGrid.Visibility == Visibility.Visible)
		{
			DelayActionHandlerCenter.TryDoAction("TagAddControlTextChanged", OnTextChange, 20);
		}
	}

	private void OnTextChange(object sender, EventArgs e)
	{
		Utils.RunOnUiThread(base.Dispatcher, OnAddTagTextChanged);
	}

	private void OnAddTagTextKeyUp(object sender, KeyEventArgs e)
	{
		Key key = e.Key;
		if (key == Key.Up || key == Key.Down)
		{
			e.Handled = true;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/tag/tagaddcontrol.xaml", UriKind.Relative);
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
			((Grid)target).MouseLeftButtonUp += OnAddTagClick;
			break;
		case 2:
			TextBorder = (Border)target;
			break;
		case 3:
			AddTagIcon = (Grid)target;
			break;
		case 4:
			TextGrid = (Grid)target;
			TextGrid.PreviewMouseLeftButtonUp += OnTextBoxClick;
			break;
		case 5:
			AddTagTextBox = (TextBox)target;
			AddTagTextBox.LostFocus += OnTextBoxLostFocus;
			AddTagTextBox.PreviewKeyDown += OnAddTagTextKeyDown;
			AddTagTextBox.PreviewKeyUp += OnAddTagTextKeyUp;
			AddTagTextBox.TextChanged += OnTextChanged;
			break;
		case 6:
			AddTagHint = (TextBlock)target;
			break;
		case 7:
			ErrorBorder = (Border)target;
			break;
		case 8:
			ErrorPopup = (Popup)target;
			break;
		case 9:
			ErrorText = (Run)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
