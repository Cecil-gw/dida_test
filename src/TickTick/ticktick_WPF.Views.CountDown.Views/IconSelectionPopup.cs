using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using ticktick_WPF.Util;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.Misc.ColorSelector;

namespace ticktick_WPF.Views.CountDown.Views;

public class IconSelectionPopup : Window, IComponentConnector
{
	private string _original_icon;

	private Brush _original_background;

	private string _original_text;

	private IconType _original_type;

	internal RadioButton TabIcon;

	internal RadioButton TabEmoji;

	internal RadioButton TabText;

	internal ItemsControl IconsItemsControl;

	internal EmojiSelectControl EmojiSelector;

	internal EmojiEditor TextEditor;

	internal Grid ColorSelector;

	internal ColorSelector ColorItems;

	internal Button SaveButton;

	private bool _contentLoaded;

	public IconSelectionViewModel ViewModel { get; private set; }

	public event Action<string, string, Brush, IconType> IconSelected;

	public IconSelectionPopup(string name, string iconName, CountDownType type, string iconBackground)
	{
		if (iconBackground != null)
		{
			_original_background = ThemeUtil.GetColorInString(iconBackground);
		}
		if (iconName != null && iconName.StartsWith("txt_"))
		{
			_original_type = (EmojiHelper.StartWithEmoji(_original_text = iconName.Replace("txt_", string.Empty)) ? IconType.Emoji : IconType.Text);
			_original_icon = null;
		}
		else
		{
			_original_type = IconType.Icon;
			_original_icon = iconName;
			_original_text = null;
		}
		ViewModel = new IconSelectionViewModel(name, iconName, iconBackground);
		base.DataContext = ViewModel;
		InitializeComponent();
		ViewModel.LoadIcons(type);
		TabIcon.IsChecked = true;
		EmojiSelector.GetItems(focus: false, 11);
		ViewModel.OnSelectIconChanged += delegate(object sender, string icon)
		{
			this.IconSelected?.Invoke(icon, null, ViewModel.SelectedIconBackground, IconType.Icon);
		};
		if (iconName != null && iconName.StartsWith("txt_"))
		{
			ViewModel.CustomText = iconName;
			string text = iconName.Replace("txt_", string.Empty);
			if (EmojiHelper.StartWithEmoji(text))
			{
				TabEmoji.IsChecked = true;
			}
			else
			{
				TextEditor.Text = text;
				TabText.IsChecked = true;
			}
		}
		ColorItems.SetSelectedColor(iconBackground ?? null);
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		base.OnClosing(e);
	}

	private void TabIcon_Checked(object sender, RoutedEventArgs e)
	{
		ViewModel.CurrentTabIndex = 0;
		if (ColorSelector != null)
		{
			ColorSelector.Visibility = Visibility.Collapsed;
		}
		this.IconSelected?.Invoke(ViewModel.SelectedIconName, null, ViewModel.SelectedIconBackground, IconType.Icon);
	}

	private void TabEmoji_Checked(object sender, RoutedEventArgs e)
	{
		EmojiSelector.GetItems(focus: false, 11);
		ViewModel.CurrentTabIndex = 1;
		ColorSelector.Visibility = Visibility.Visible;
		ViewModel.SelectedIcon = null;
		this.IconSelected?.Invoke(null, ViewModel.CustomText, ViewModel.SelectedIconBackground, IconType.Emoji);
	}

	private void TabText_Checked(object sender, RoutedEventArgs e)
	{
		ViewModel.CurrentTabIndex = 2;
		ColorSelector.Visibility = Visibility.Visible;
		ViewModel.SelectedIconName = null;
		ViewModel.SelectedIcon = null;
		string arg = (EmojiHelper.StartWithEmoji(ViewModel.CustomText) ? string.Empty : ViewModel.CustomText);
		this.IconSelected?.Invoke(null, arg, ViewModel.SelectedIconBackground, IconType.Text);
	}

	private void CancelButton_Click(object sender, RoutedEventArgs e)
	{
		this.IconSelected?.Invoke(_original_icon, _original_text, _original_background, _original_type);
		Close();
	}

	private void OkButton_Click(object sender, RoutedEventArgs e)
	{
		base.Deactivated -= OnWindowDeactived;
		IconType arg = ViewModel.CurrentTabIndex switch
		{
			0 => IconType.Icon, 
			1 => IconType.Emoji, 
			2 => IconType.Text, 
			_ => IconType.Icon, 
		};
		this.IconSelected?.Invoke(ViewModel.SelectedIconName, ViewModel.CustomText?.Replace("txt_", string.Empty), ViewModel.SelectedIconBackground, arg);
		Close();
	}

	private void OnEmojiSelected(string emoji, bool closepopup)
	{
		ViewModel.CustomText = emoji;
		this.IconSelected?.Invoke(null, emoji, ViewModel.SelectedIconBackground, IconType.Emoji);
	}

	private void OnIconTextInput(object sender, TextCompositionEventArgs e)
	{
		ViewModel.CustomText = e.Text;
		ViewModel.SelectedIconName = null;
		this.IconSelected?.Invoke(null, e.Text, ViewModel.SelectedIconBackground, IconType.Text);
	}

	private void OnTextColorSelect(object sender, string e)
	{
		ViewModel.SelectedIconBackground = ((e == "transparent") ? null : ((Brush)new BrushConverter().ConvertFrom(e)));
		IconType arg = ViewModel.CurrentTabIndex switch
		{
			0 => IconType.Icon, 
			1 => IconType.Emoji, 
			2 => IconType.Text, 
			_ => IconType.Icon, 
		};
		this.IconSelected?.Invoke(ViewModel.SelectedIconName, ViewModel.CustomText?.Replace("txt_", string.Empty), ViewModel.SelectedIconBackground, arg);
	}

	private void OnEditTextChange(object sender, EventArgs e)
	{
		ViewModel.CustomText = TextEditor.Text;
		this.IconSelected?.Invoke(null, ViewModel.CustomText?.Replace("txt_", string.Empty), ViewModel.SelectedIconBackground, IconType.Text);
	}

	public async void OnWindowDeactived(object sender, EventArgs e)
	{
		if (!base.IsLoaded || !base.IsVisible || ColorItems.ColorPopup.IsOpen)
		{
			return;
		}
		try
		{
			await Task.Delay(100);
			CancelButton_Click(null, null);
		}
		catch (InvalidOperationException)
		{
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/countdown/views/iconselectionpopup.xaml", UriKind.Relative);
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
			((IconSelectionPopup)target).Deactivated += OnWindowDeactived;
			break;
		case 2:
			TabIcon = (RadioButton)target;
			TabIcon.Checked += TabIcon_Checked;
			break;
		case 3:
			TabEmoji = (RadioButton)target;
			TabEmoji.Checked += TabEmoji_Checked;
			break;
		case 4:
			TabText = (RadioButton)target;
			TabText.Checked += TabText_Checked;
			break;
		case 5:
			IconsItemsControl = (ItemsControl)target;
			break;
		case 6:
			EmojiSelector = (EmojiSelectControl)target;
			break;
		case 7:
			TextEditor = (EmojiEditor)target;
			break;
		case 8:
			ColorSelector = (Grid)target;
			break;
		case 9:
			((TextBlock)target).PreviewTextInput += OnIconTextInput;
			break;
		case 10:
			ColorItems = (ColorSelector)target;
			break;
		case 11:
			SaveButton = (Button)target;
			SaveButton.Click += OkButton_Click;
			break;
		case 12:
			((Button)target).Click += CancelButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
