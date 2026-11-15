using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Emoji.Wpf;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Misc.ColorSelector;

namespace ticktick_WPF.Views.Habit;

public class SetHabitIconControl : UserControl, IComponentConnector, IStyleConnector
{
	public static readonly DependencyProperty SelectedIconProperty = DependencyProperty.Register("SelectedIcon", typeof(string), typeof(SetHabitIconControl), new PropertyMetadata("habit_daily_check_in", null));

	public static readonly DependencyProperty IconColorProperty = DependencyProperty.Register("IconColor", typeof(string), typeof(SetHabitIconControl), new PropertyMetadata("#70C362", null));

	public static readonly DependencyProperty IconTextProperty = DependencyProperty.Register("IconText", typeof(string), typeof(SetHabitIconControl), new PropertyMetadata("A", null));

	public static readonly DependencyProperty TextColorProperty = DependencyProperty.Register("TextColor", typeof(string), typeof(SetHabitIconControl), new PropertyMetadata("", OnTextColorChanged));

	public static readonly DependencyProperty IsIconProperty = DependencyProperty.Register("IsIcon", typeof(bool), typeof(SetHabitIconControl), new PropertyMetadata(true, null));

	internal SetHabitIconControl Root;

	internal Image SelectedIconImage;

	internal Grid TransparentGrid;

	internal Border TextBackground;

	internal EmjTextBlock EmjTextBox;

	internal ItemsControl IconItems;

	internal TextBox IconTextBox;

	internal ColorSelector ColorItems;

	private bool _contentLoaded;

	public string SelectedIcon
	{
		get
		{
			return (string)GetValue(SelectedIconProperty);
		}
		set
		{
			SetValue(SelectedIconProperty, value);
		}
	}

	public string IconColor
	{
		get
		{
			return (string)GetValue(IconColorProperty);
		}
		set
		{
			SetValue(IconColorProperty, value);
		}
	}

	public string IconText
	{
		get
		{
			return (string)GetValue(IconTextProperty);
		}
		set
		{
			SetValue(IconTextProperty, value);
		}
	}

	public string TextColor
	{
		get
		{
			return (string)GetValue(TextColorProperty);
		}
		set
		{
			SetValue(TextColorProperty, value);
		}
	}

	public bool IsIcon
	{
		get
		{
			return (bool)GetValue(IsIconProperty);
		}
		set
		{
			SetValue(IsIconProperty, value);
		}
	}

	private List<HabitIconViewModel> Icons => IconItems.ItemsSource as List<HabitIconViewModel>;

	public event EventHandler OnIconSaved;

	public event EventHandler Closed;

	private static void OnTextColorChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is SetHabitIconControl setHabitIconControl && e.NewValue is string text)
		{
			setHabitIconControl.TransparentGrid.Visibility = ((!string.IsNullOrEmpty(text)) ? Visibility.Collapsed : Visibility.Visible);
			setHabitIconControl.EmjTextBox.Foreground = ((string.IsNullOrEmpty(text) || text.ToLower() == "transparent") ? ThemeUtil.GetColor("PrimaryColor") : Brushes.White);
		}
	}

	public SetHabitIconControl()
	{
		InitializeComponent();
		IconItems.ItemsSource = HabitIconViewModel.BuildItems();
		DataObject.AddPastingHandler(IconTextBox, OnPaste);
	}

	private void OnPaste(object sender, DataObjectPastingEventArgs e)
	{
		e.CancelCommand();
		if (e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText, autoConvert: true) && e.SourceDataObject.GetData(DataFormats.UnicodeText) is string text)
		{
			IconTextBox.TextChanged += OnIconTextChanged;
			IconTextBox.Text = text;
		}
	}

	private void OnSaveClick(object sender, RoutedEventArgs e)
	{
		this.OnIconSaved?.Invoke(this, null);
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		this.Closed?.Invoke(this, null);
	}

	public void Reset(string iconRes, string color)
	{
		iconRes = (string.IsNullOrWhiteSpace(iconRes) ? "habit_daily_check_in" : iconRes);
		if (iconRes.StartsWith("txt"))
		{
			TextColor = color;
			IconText = iconRes.Split('_').LastOrDefault() ?? (Utils.IsCn() ? "文" : "A");
			SelectedIconImage.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/Habits/habit_daily_check_in.png"));
			IconColor = HabitUtils.IconInfos["habit_daily_check_in"].IconOutlineColor;
			SelectedIcon = "habit_daily_check_in";
			IsIcon = false;
		}
		else
		{
			SelectedIcon = iconRes;
			IconColor = color;
			IconText = (Utils.IsCn() ? "文" : "A");
			IsIcon = true;
			SelectedIconImage.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/Habits/" + SelectedIcon.ToLower() + ".png"));
		}
		IconTextBox.Text = IconText;
		ColorItems.SetSelectedColor(TextColor);
		TransparentGrid.Visibility = ((!string.IsNullOrEmpty(TextColor)) ? Visibility.Collapsed : Visibility.Visible);
		EmjTextBox.Foreground = (string.IsNullOrEmpty(TextColor) ? ThemeUtil.GetColor("PrimaryColor") : Brushes.White);
		Icons.ForEach(delegate(HabitIconViewModel icon)
		{
			icon.Selected = SelectedIcon == icon.IconName;
		});
	}

	private void OnIconItemClick(object sender, MouseButtonEventArgs e)
	{
		if (!(sender is Grid { DataContext: var dataContext }))
		{
			return;
		}
		HabitIconViewModel iconModel = dataContext as HabitIconViewModel;
		if (iconModel != null)
		{
			Icons.ForEach(delegate(HabitIconViewModel icon)
			{
				icon.Selected = iconModel.IconName == icon.IconName;
			});
			SelectedIcon = iconModel.IconName;
			SelectedIconImage.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/Habits/" + SelectedIcon.ToLower() + ".png"));
			IconColor = iconModel.Color;
		}
	}

	private void SwitchIconClick(object sender, MouseButtonEventArgs e)
	{
		if (!IsIcon)
		{
			IsIcon = true;
		}
	}

	private void SwitchTextClick(object sender, MouseButtonEventArgs e)
	{
		if (IsIcon)
		{
			IsIcon = false;
		}
	}

	private void OnIconTextInput(object sender, TextCompositionEventArgs e)
	{
		string emojiIcon = EmojiHelper.GetEmojiIcon(e.Text);
		if (!string.IsNullOrEmpty(emojiIcon) && e.Text.StartsWith(emojiIcon))
		{
			IconTextBox.MaxLength = emojiIcon.Length;
		}
		else
		{
			IconTextBox.MaxLength = 1;
		}
		IconTextBox.TextChanged += OnIconTextChanged;
	}

	private void OnIconTextChanged(object sender, TextChangedEventArgs e)
	{
		string text = IconTextBox.Text.Trim();
		if (text.Length >= 1)
		{
			string emojiIcon = EmojiHelper.GetEmojiIcon(text);
			if (string.IsNullOrEmpty(emojiIcon) || !text.StartsWith(emojiIcon))
			{
				IconText = text.Substring(0, 1);
			}
			else
			{
				IconText = EmojiHelper.GetEmojiIcon(text);
			}
			IconTextBox.Text = IconText;
		}
		IconTextBox.TextChanged -= OnIconTextChanged;
	}

	private void OnTextColorSelect(object sender, string e)
	{
		if (e.ToLower() == "transparent")
		{
			e = null;
		}
		TextColor = e;
		TransparentGrid.Visibility = ((!string.IsNullOrEmpty(TextColor)) ? Visibility.Collapsed : Visibility.Visible);
		EmjTextBox.Foreground = (string.IsNullOrEmpty(TextColor) ? ThemeUtil.GetColor("PrimaryColor") : Brushes.White);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/habit/sethabiticoncontrol.xaml", UriKind.Relative);
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
			Root = (SetHabitIconControl)target;
			break;
		case 2:
			((Grid)target).MouseLeftButtonUp += SwitchIconClick;
			break;
		case 3:
			SelectedIconImage = (Image)target;
			break;
		case 4:
			((Grid)target).MouseLeftButtonUp += SwitchTextClick;
			break;
		case 5:
			TransparentGrid = (Grid)target;
			break;
		case 6:
			TextBackground = (Border)target;
			break;
		case 7:
			EmjTextBox = (EmjTextBlock)target;
			break;
		case 8:
			IconItems = (ItemsControl)target;
			break;
		case 10:
			IconTextBox = (TextBox)target;
			IconTextBox.PreviewTextInput += OnIconTextInput;
			break;
		case 11:
			ColorItems = (ColorSelector)target;
			break;
		case 12:
			((Button)target).Click += OnSaveClick;
			break;
		case 13:
			((Button)target).Click += OnCancelClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 9)
		{
			((Grid)target).MouseLeftButtonUp += OnIconItemClick;
		}
	}
}
