using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Detail;

namespace ticktick_WPF.Views.Config;

public class StickyNoteConfig : UserControl, IComponentConnector, IStyleConnector
{
	internal ScrollViewer Scroller;

	internal Run LearnMoreText;

	internal StickyColorSelector ColorSelector;

	internal Slider OpacitySlider;

	internal CustomSimpleComboBox FontSizeComboBox;

	internal CheckBox PinDefault;

	internal CheckBox HideInTaskBarCheckbox;

	internal CheckBox ResetAfterSortCheckBox;

	internal ItemsControl StickySpaceItems;

	private bool _contentLoaded;

	public StickyNoteConfig()
	{
		InitializeComponent();
		InitControl();
		base.Loaded += delegate
		{
			Scroller.ScrollToTop();
		};
	}

	private void InitControl()
	{
		InitFontSize();
		InitStickySpacing();
		InitColor();
		InitOpacity();
		InitPin();
		InitSort();
		InitHideInTask();
	}

	private void InitOpacity()
	{
		if (LocalSettings.Settings.ExtraSettings.StickyOpacity >= 0.0 && LocalSettings.Settings.ExtraSettings.StickyOpacity <= 100.0)
		{
			OpacitySlider.Value = LocalSettings.Settings.ExtraSettings.StickyOpacity / 10.0;
			OpacitySlider.ToolTip = (int)(OpacitySlider.Value * 10.0) + "%";
			OpacitySlider.ValueChanged -= OnSlideValueChanged;
			OpacitySlider.ValueChanged += OnSlideValueChanged;
		}
	}

	private void InitStickySpacing()
	{
		List<StickySpacingSelectViewModel> list = new List<StickySpacingSelectViewModel>();
		list.Add(new StickySpacingSelectViewModel(Utils.GetString("none"), 3, 0, list));
		list.Add(new StickySpacingSelectViewModel(Utils.GetString("Normal"), 7, 20, list));
		list.Add(new StickySpacingSelectViewModel(Utils.GetString("Large"), 9, 30, list));
		list.Add(new StickySpacingSelectViewModel(Utils.GetString("ExtraLarge"), 13, 40, list));
		StickySpaceItems.ItemsSource = list;
	}

	private void InitHideInTask()
	{
		HideInTaskBarCheckbox.IsChecked = LocalSettings.Settings.ExtraSettings.StickyHideInTaskBar;
		HideInTaskBarCheckbox.Checked += OnCheckedChanged;
		HideInTaskBarCheckbox.Unchecked += OnCheckedChanged;
		void OnCheckedChanged(object sender, RoutedEventArgs e)
		{
			UserActCollectUtils.AddClickEvent("sticky_note", "not_show_in_the_taskbar", (HideInTaskBarCheckbox.IsChecked == true) ? "enable" : "disable");
			LocalSettings.Settings.ExtraSettings.StickyHideInTaskBar = HideInTaskBarCheckbox.IsChecked == true;
			TaskStickyWindow.SetShowInTaskBar();
		}
	}

	private void InitSort()
	{
		ResetAfterSortCheckBox.IsChecked = LocalSettings.Settings.ExtraSettings.ResetStickyWhenAlign;
		ResetAfterSortCheckBox.Checked += OnCheckedChanged;
		ResetAfterSortCheckBox.Unchecked += OnCheckedChanged;
		void OnCheckedChanged(object sender, RoutedEventArgs e)
		{
			UserActCollectUtils.AddClickEvent("sticky_note", "restore_default_size_when_arranging", (ResetAfterSortCheckBox.IsChecked == true) ? "enable" : "disable");
			LocalSettings.Settings.ExtraSettings.ResetStickyWhenAlign = ResetAfterSortCheckBox.IsChecked == true;
		}
	}

	private void InitPin()
	{
		PinDefault.IsChecked = LocalSettings.Settings.ExtraSettings.StickyDefaultPin;
		PinDefault.Checked += OnPinDefaultChanged;
		PinDefault.Unchecked += OnPinDefaultChanged;
		void OnPinDefaultChanged(object sender, RoutedEventArgs e)
		{
			UserActCollectUtils.AddClickEvent("sticky_note", "pin_by_default", (PinDefault.IsChecked == true) ? "enable" : "disable");
			LocalSettings.Settings.ExtraSettings.StickyDefaultPin = PinDefault.IsChecked == true;
		}
	}

	private void InitColor()
	{
		ColorSelector.SetSelectedColor(LocalSettings.Settings.ExtraSettings.StickyDefaultColor);
		ColorSelector.ColorSelect += delegate(object sender, string color)
		{
			LocalSettings.Settings.ExtraSettings.StickyDefaultColor = color;
		};
	}

	private void InitFontSize()
	{
		_ = LocalSettings.Settings.ExtraSettings.StickyFont;
		FontSizeComboBox.ItemsSource = new List<string>
		{
			Utils.GetString("Small"),
			Utils.GetString("Normal"),
			Utils.GetString("Large"),
			Utils.GetString("ExtraLarge")
		};
		FontSizeComboBox.SelectedIndex = LocalSettings.Settings.ExtraSettings.StickyFont;
	}

	private void OnFontSizeChanged(object sender, SimpleComboBoxViewModel e)
	{
		string data = "normal";
		switch (FontSizeComboBox.SelectedIndex)
		{
		case 0:
			data = "small";
			break;
		case 1:
			data = "normal";
			break;
		case 2:
			data = "large";
			break;
		case 3:
			data = "extra_large";
			break;
		}
		UserActCollectUtils.AddClickEvent("sticky_note", "font_size", data);
		LocalSettings.Settings.ExtraSettings.StickyFont = FontSizeComboBox.SelectedIndex;
		App.SetStickyFontSize();
	}

	private void OnSlideValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
	{
		LocalSettings.Settings.ExtraSettings.StickyOpacity = e.NewValue * 10.0;
		OpacitySlider.ToolTip = (int)(e.NewValue * 10.0) + "%";
		GlobalEventManager.OnStickyOpacityChanged();
	}

	private void OnSpaceItemSelected(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: StickySpacingSelectViewModel dataContext })
		{
			string data = "normal";
			switch (dataContext.Spacing)
			{
			case 0:
				data = "none";
				break;
			case 20:
				data = "normal";
				break;
			case 30:
				data = "large";
				break;
			case 40:
				data = "extra_large";
				break;
			}
			UserActCollectUtils.AddClickEvent("sticky_note", "default_grid_spacing", data);
			LocalSettings.Settings.ExtraSettings.StickySpacing = dataContext.Spacing;
			dataContext.SetSelected();
		}
	}

	private void LearnMoreClick(object sender, MouseButtonEventArgs e)
	{
		Utils.TryProcessStartUrl("https://help.dida365.com/articles/6986508444417130496");
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/config/stickynoteconfig.xaml", UriKind.Relative);
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
			Scroller = (ScrollViewer)target;
			break;
		case 2:
			LearnMoreText = (Run)target;
			LearnMoreText.MouseLeftButtonUp += LearnMoreClick;
			break;
		case 3:
			ColorSelector = (StickyColorSelector)target;
			break;
		case 4:
			OpacitySlider = (Slider)target;
			break;
		case 5:
			FontSizeComboBox = (CustomSimpleComboBox)target;
			break;
		case 6:
			PinDefault = (CheckBox)target;
			break;
		case 7:
			HideInTaskBarCheckbox = (CheckBox)target;
			break;
		case 8:
			ResetAfterSortCheckBox = (CheckBox)target;
			break;
		case 9:
			StickySpaceItems = (ItemsControl)target;
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
		if (connectionId == 10)
		{
			((Grid)target).MouseLeftButtonUp += OnSpaceItemSelected;
		}
	}
}
