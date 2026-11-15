using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using TickTickUtils;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc.ColorSelector;

public class ColorSelector : UserControl, IComponentConnector, IStyleConnector
{
	public static readonly DependencyProperty WithNoColorProperty = DependencyProperty.Register("WithNoColor", typeof(bool), typeof(ColorSelector), new PropertyMetadata(true));

	public static readonly DependencyProperty WithCustomColorProperty = DependencyProperty.Register("WithCustomColor", typeof(bool), typeof(ColorSelector), new PropertyMetadata(true));

	public static readonly DependencyProperty OnlyShowSelectedProperty = DependencyProperty.Register("OnlyShowSelected", typeof(bool), typeof(ColorSelector), new PropertyMetadata(false, OnOnlyShowSelectedChange));

	public static readonly DependencyProperty OneColorSizeProperty = DependencyProperty.Register("OneColorSize", typeof(double), typeof(ColorSelector), new PropertyMetadata(20.0));

	public static readonly DependencyProperty OneColorEmptyDisplayProperty = DependencyProperty.Register("OneColorEmptyDisplay", typeof(Color?), typeof(ColorSelector), new PropertyMetadata(null, OnOneColorEmptyDisplayChanged));

	public static readonly DependencyProperty OneColorNeedsContrastBorderProperty = DependencyProperty.Register("OneColorNeedsContrastBorder", typeof(bool), typeof(ColorSelector), new PropertyMetadata(false));

	public static readonly DependencyProperty MoreColorSolutionProperty = DependencyProperty.Register("MoreColorSolution", typeof(int), typeof(ColorSelector), new PropertyMetadata(0));

	public static readonly DependencyProperty AllowPopupCancelSelectedProperty = DependencyProperty.Register("AllowPopupCancelSelected", typeof(bool), typeof(ColorSelector), new PropertyMetadata(true));

	public static readonly DependencyProperty SelectedColorProperty = DependencyProperty.Register("SelectedColor", typeof(Color), typeof(ColorSelector), new FrameworkPropertyMetadata(Colors.Transparent, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedColorChanged));

	public static readonly DependencyProperty SelectedColorStrProperty = DependencyProperty.Register("SelectedColorStr", typeof(string), typeof(ColorSelector), new FrameworkPropertyMetadata("transparent", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedColorStrChanged));

	private List<string> _colorList = new List<string> { "#FF6161", "#FFAC38", "#FFD324", "#E6EA49", "#35D870", "#4CA1FF", "#6E75F4" };

	private static readonly List<string> DefaultColorList = new List<string> { "#FF6161", "#FFAC38", "#FFD324", "#E6EA49", "#35D870", "#4CA1FF", "#6E75F4" };

	internal ColorSelector Root;

	internal Grid DefaultPanel;

	internal ItemsControl DefaultColorItems;

	internal Grid OneColorGrid;

	internal Rectangle OneSelectedColor;

	internal EscPopup ColorPopup;

	internal ContentControl MoreColorContainer;

	internal ColorItemSelector Selector;

	private bool _contentLoaded;

	private ObservableCollection<ItemColorViewModel> _defaultColors { get; set; } = new ObservableCollection<ItemColorViewModel>();

	public bool WithNoColor
	{
		get
		{
			return (bool)GetValue(WithNoColorProperty);
		}
		set
		{
			SetValue(WithNoColorProperty, value);
		}
	}

	public bool WithCustomColor
	{
		get
		{
			return (bool)GetValue(WithCustomColorProperty);
		}
		set
		{
			SetValue(WithCustomColorProperty, value);
		}
	}

	public bool OnlyShowSelected
	{
		get
		{
			return (bool)GetValue(OnlyShowSelectedProperty);
		}
		set
		{
			SetValue(OnlyShowSelectedProperty, value);
		}
	}

	public double OneColorSize
	{
		get
		{
			return (double)GetValue(OneColorSizeProperty);
		}
		set
		{
			SetValue(OneColorSizeProperty, value);
		}
	}

	public Color? OneColorEmptyDisplay
	{
		get
		{
			return (Color?)GetValue(OneColorEmptyDisplayProperty);
		}
		set
		{
			SetValue(OneColorEmptyDisplayProperty, value);
		}
	}

	public bool OneColorNeedsContrastBorder
	{
		get
		{
			return (bool)GetValue(OneColorNeedsContrastBorderProperty);
		}
		private set
		{
			SetValue(OneColorNeedsContrastBorderProperty, value);
		}
	}

	public int MoreColorSolution
	{
		get
		{
			return (int)GetValue(MoreColorSolutionProperty);
		}
		set
		{
			SetValue(MoreColorSolutionProperty, value);
		}
	}

	public bool AllowPopupCancelSelected
	{
		get
		{
			return (bool)GetValue(AllowPopupCancelSelectedProperty);
		}
		set
		{
			SetValue(AllowPopupCancelSelectedProperty, value);
		}
	}

	public Color SelectedColor
	{
		get
		{
			return (Color)GetValue(SelectedColorProperty);
		}
		set
		{
			SetValue(SelectedColorProperty, value);
		}
	}

	public string SelectedColorStr
	{
		get
		{
			return (string)GetValue(SelectedColorStrProperty);
		}
		set
		{
			SetValue(SelectedColorStrProperty, value);
		}
	}

	public event EventHandler<string> ColorSelect;

	private static void OnOnlyShowSelectedChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is ColorSelector colorSelector)
		{
			if ((bool)e.NewValue)
			{
				colorSelector.DefaultPanel.Visibility = Visibility.Collapsed;
				colorSelector.OneColorGrid.Visibility = Visibility.Visible;
			}
			else
			{
				colorSelector.DefaultPanel.Visibility = Visibility.Visible;
				colorSelector.OneColorGrid.Visibility = Visibility.Collapsed;
			}
		}
	}

	private static void OnOneColorEmptyDisplayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		ColorSelector obj = (ColorSelector)d;
		obj.UpdateOneColorNeedsContrastBorder(obj.CheckColor(obj.SelectedColorStr));
	}

	private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		ColorSelector obj = (ColorSelector)d;
		Color color = (Color)e.NewValue;
		string selectedColor = obj.ColorToString(color);
		obj.SetSelectedColor(selectedColor);
	}

	private static void OnSelectedColorStrChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		ColorSelector obj = (ColorSelector)d;
		string selectedColor = (string)e.NewValue;
		obj.SetSelectedColor(selectedColor);
	}

	public ColorSelector()
	{
		InitializeComponent();
		DefaultColorItems.ItemsSource = _defaultColors;
		Selector.ColorSelect += OnColorSelect;
		InitializeDefaultColors();
	}

	private void InitializeDefaultColors()
	{
		_defaultColors.Clear();
		if (WithNoColor)
		{
			_defaultColors.Add(new ItemColorViewModel
			{
				NoColor = true
			});
		}
		foreach (string color in _colorList)
		{
			_defaultColors.Add(new ItemColorViewModel
			{
				Color = color,
				IsContrastingColor = IsContrastingColor(color)
			});
		}
		_defaultColors.Add(new ItemColorViewModel
		{
			IsAddCustom = true
		});
	}

	private bool IsContrastingColor(string color)
	{
		return ColorUtils.NeedsContrastBorder(color, ThemeUtil.IsDark());
	}

	public void SetCustomColorList(List<string> colorList)
	{
		if (colorList != null && colorList.Count != 0)
		{
			_colorList = new List<string>(colorList);
			InitializeDefaultColors();
		}
	}

	public List<string> GetColorList()
	{
		return new List<string>(_colorList);
	}

	private void OnColorSelect(object sender, string color)
	{
		if (string.IsNullOrEmpty(color))
		{
			color = "transparent";
		}
		SetSelectedColor(color);
		this.ColorSelect?.Invoke(this, color);
	}

	private void ShowColorSelector(object sender, MouseButtonEventArgs e)
	{
		string text = null;
		if (OnlyShowSelected)
		{
			Brush fill = OneSelectedColor.Fill;
			text = ((fill == Brushes.Transparent) ? null : ThemeUtil.GetNoAlphaColorString(fill.ToString()));
		}
		else
		{
			text = _defaultColors.FirstOrDefault((ItemColorViewModel x) => x.IsAddCustom)?.Color;
		}
		Selector.SetItemSelected(text);
		ColorPopup.PlacementTarget = sender as FrameworkElement;
		if (OnlyShowSelected)
		{
			ColorPopup.VerticalOffset = 214.0 + OneColorSize;
		}
		else
		{
			ColorPopup.VerticalOffset = 200.0;
		}
		ColorPopup.IsOpen = true;
	}

	public string GetSelectedColor()
	{
		if (!OnlyShowSelected)
		{
			ItemColorViewModel itemColorViewModel = _defaultColors.FirstOrDefault((ItemColorViewModel m) => m.Selected);
			if (itemColorViewModel != null)
			{
				return itemColorViewModel.Color;
			}
		}
		string text = "transparent";
		if (OnlyShowSelected)
		{
			Brush fill = OneSelectedColor.Fill;
			if (fill == Brushes.Transparent)
			{
				return text;
			}
			text = fill.ToString();
			if (text.StartsWith("#") && text.Length == 9)
			{
				text = "#" + text.Substring(3);
			}
		}
		else
		{
			string text2 = _defaultColors.FirstOrDefault((ItemColorViewModel x) => x.IsAddCustom)?.Color;
			if (!string.IsNullOrEmpty(text2))
			{
				text = text2;
			}
		}
		return text;
	}

	private string CheckColor(string colorString)
	{
		if (string.IsNullOrEmpty(colorString) || (colorString.Length == 9 && colorString.StartsWith("#00")) || colorString == "transparent")
		{
			colorString = null;
		}
		if (!ColorUtils.IsAvailableColor(colorString))
		{
			colorString = null;
		}
		return colorString;
	}

	public void SetSelectedColor(string colorString)
	{
		colorString = CheckColor(colorString);
		UpdateOneColorNeedsContrastBorder(colorString);
		SolidColorBrush colorInString = ThemeUtil.GetColorInString(colorString);
		if (!OnlyShowSelected)
		{
			IEnumerable<ItemColorViewModel> enumerable = _defaultColors.Where((ItemColorViewModel x) => !x.IsAddCustom);
			foreach (ItemColorViewModel item in enumerable)
			{
				item.Selected = item.Color == colorString || (item.NoColor && ThemeUtil.IsEmptyColor(colorString));
			}
			ItemColorViewModel itemColorViewModel = _defaultColors.FirstOrDefault((ItemColorViewModel x) => x.IsAddCustom);
			if (itemColorViewModel != null)
			{
				if (enumerable.All((ItemColorViewModel m) => !m.Selected))
				{
					itemColorViewModel.Color = colorString;
					itemColorViewModel.Selected = true;
				}
				else
				{
					itemColorViewModel.Color = "transparent";
					itemColorViewModel.Selected = false;
				}
			}
		}
		else
		{
			OneSelectedColor.Fill = colorInString;
		}
		if (SelectedColor != colorInString.Color)
		{
			SelectedColor = colorInString.Color;
		}
		if (SelectedColorStr != colorString)
		{
			SelectedColorStr = colorString;
		}
	}

	private void UpdateOneColorNeedsContrastBorder(string selectedColor)
	{
		string text = selectedColor;
		if (string.IsNullOrEmpty(text) && OneColorEmptyDisplay.HasValue)
		{
			text = ColorToString(OneColorEmptyDisplay.Value);
		}
		OneColorNeedsContrastBorder = ColorUtils.NeedsContrastBorder(text, ThemeUtil.IsDark());
	}

	private void OnDefaultColorClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: ItemColorViewModel dataContext })
		{
			if (!dataContext.Selected)
			{
				string text = (dataContext.NoColor ? "transparent" : dataContext.Color);
				SetSelectedColor(text);
				this.ColorSelect?.Invoke(this, text);
			}
			else if (WithNoColor)
			{
				string text2 = "transparent";
				SetSelectedColor(text2);
				this.ColorSelect?.Invoke(this, text2);
			}
		}
	}

	private void OnColorPopupClosed(object sender, EventArgs e)
	{
		SettingsHelper.PushLocalPreference();
	}

	public static void TryAddClickEvent(string color)
	{
		string text = "";
		if (ThemeUtil.IsEmptyColor(color))
		{
			text = "none";
		}
		if (DefaultColorList.Contains(color))
		{
			text = "preset";
		}
		else
		{
			int num = ColorItemSelector.MoreColorList.IndexOf(color);
			if (num >= 0)
			{
				switch (num / 8)
				{
				case 0:
					text = "macaron";
					break;
				case 1:
					text = "morandi";
					break;
				case 2:
					text = "rococo";
					break;
				case 3:
					text = "classic";
					break;
				case 4:
					text = "memphis";
					break;
				}
			}
		}
		if (!string.IsNullOrEmpty(text))
		{
			UserActCollectUtils.AddClickEvent("project_list_ui", "list_color", text);
		}
	}

	private string ColorToString(Color color)
	{
		if (color == Colors.Transparent)
		{
			return "transparent";
		}
		string text = color.ToString();
		if (text.Length == 9 && text.StartsWith("#"))
		{
			return "#" + text.Substring(3);
		}
		return text;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/misc/colorselector/colorselector.xaml", UriKind.Relative);
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
			Root = (ColorSelector)target;
			break;
		case 2:
			DefaultPanel = (Grid)target;
			break;
		case 3:
			DefaultColorItems = (ItemsControl)target;
			break;
		case 5:
			OneColorGrid = (Grid)target;
			OneColorGrid.MouseLeftButtonUp += ShowColorSelector;
			break;
		case 6:
			OneSelectedColor = (Rectangle)target;
			break;
		case 7:
			ColorPopup = (EscPopup)target;
			break;
		case 8:
			MoreColorContainer = (ContentControl)target;
			break;
		case 9:
			Selector = (ColorItemSelector)target;
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
		if (connectionId == 4)
		{
			((Grid)target).MouseLeftButtonUp += ShowColorSelector;
		}
	}
}
