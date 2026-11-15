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
using TickTickUtils.Lang;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc.ColorSelector;

public class ColorSelectorEx : UserControl, IComponentConnector
{
	private string _selectedColor;

	private string _selectedFontColor;

	private readonly List<ItemColorViewModel> _fontColors = new List<ItemColorViewModel>();

	private readonly List<ItemColorViewModel> _bgColors1 = new List<ItemColorViewModel>();

	private readonly List<ItemColorViewModel> _bgColors2 = new List<ItemColorViewModel>();

	private static readonly List<string> FontColorList = new List<string> { "#FFA3A3A3", "#FFE74941", "#FFFA9B35", "#FFFFC400", "#FF2BCBE1", "#FF3461ED", "#FFB748E1" };

	private static readonly List<string> ColorList1 = new List<string> { "#66A3A3A3", "#66FA6D68", "#66F7B268", "#66FFD64E", "#667FD9E5", "#667A9AFF", "#66DAA4EE" };

	private static readonly List<string> ColorList2 = new List<string> { "#D1D1D1", "#FFA3A3A3", "#FFFA6D68", "#FFF7B268", "#FFFFD64E", "#FF7FD9E5", "#FF7A9AFF", "#FFDAA4EE" };

	internal StackPanel Panel;

	internal ItemsControl FontColorItems;

	internal ItemsControl DefaultColorItems1;

	internal ItemsControl DefaultColorItems2;

	internal Button ResetBtn;

	private bool _contentLoaded;

	public string SelectedColor
	{
		get
		{
			return _selectedColor;
		}
		set
		{
			_selectedColor = value;
			UtilLog.Error("set selected color:" + value);
			NotifyColorSelected();
		}
	}

	public string SelectedFontColor
	{
		get
		{
			return _selectedFontColor;
		}
		set
		{
			_selectedFontColor = value;
			UtilLog.Error("set selected font color:" + value);
			NotifyFontColorSelected();
		}
	}

	public event EventHandler<ColorExtra> ColorSelect;

	private void NotifyResetVisible()
	{
		bool flag = true;
		if (_bgColors1 != null && _fontColors != null && _bgColors1.First().Selected && _fontColors.First().Selected)
		{
			flag = false;
		}
		ResetBtn.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
	}

	public ColorSelectorEx()
	{
		InitializeComponent();
		InitData();
	}

	private void InitData()
	{
		_bgColors1.Clear();
		_bgColors1.Add(new ItemColorViewModel
		{
			NoColor = true
		});
		foreach (string item in ColorList1)
		{
			_bgColors1.Add(new ItemColorViewModel
			{
				Color = item
			});
		}
		DefaultColorItems1.ItemsSource = _bgColors1;
		_bgColors2.Clear();
		foreach (string item2 in ColorList2)
		{
			_bgColors2.Add(new ItemColorViewModel
			{
				Color = item2
			});
		}
		DefaultColorItems2.ItemsSource = _bgColors2;
		_fontColors.Clear();
		_fontColors.Add(new ItemColorViewModel
		{
			NoColor = true,
			Color = (ThemeUtil.IsDark() ? "#FFFFFF" : "#191919")
		});
		foreach (string fontColor in FontColorList)
		{
			_fontColors.Add(new ItemColorViewModel
			{
				Color = fontColor
			});
		}
		FontColorItems.ItemsSource = _fontColors;
		NotifyFontColorSelected();
		NotifyColorSelected();
	}

	public void Reset()
	{
		_bgColors1.ForEach(delegate(ItemColorViewModel it)
		{
			it.Selected = false;
		});
		_bgColors2.ForEach(delegate(ItemColorViewModel it)
		{
			it.Selected = false;
		});
		_fontColors.ForEach(delegate(ItemColorViewModel it)
		{
			it.Selected = false;
		});
		if (_fontColors.Any())
		{
			_fontColors.First().Selected = true;
		}
		if (_bgColors1.Any())
		{
			_bgColors1.First().Selected = true;
		}
	}

	private void OnDefaultFontColorClick(object sender, MouseButtonEventArgs e)
	{
		_fontColors.ForEach(delegate(ItemColorViewModel it)
		{
			it.Selected = false;
		});
		if (sender is FrameworkElement { DataContext: ItemColorViewModel dataContext })
		{
			_selectedFontColor = ((!dataContext.NoColor) ? dataContext.Color : null);
			NotifyFontColorSelected();
			this.ColorSelect?.Invoke(this, new ColorExtra
			{
				Color = SelectedFontColor,
				Background = SelectedColor
			});
		}
	}

	private void OnDefaultColorClick(object sender, MouseButtonEventArgs e)
	{
		_bgColors1.ForEach(delegate(ItemColorViewModel it)
		{
			it.Selected = false;
		});
		_bgColors2.ForEach(delegate(ItemColorViewModel it)
		{
			it.Selected = false;
		});
		if (sender is FrameworkElement { DataContext: ItemColorViewModel dataContext })
		{
			_selectedColor = ((!dataContext.NoColor) ? dataContext.Color : null);
			NotifyColorSelected();
			this.ColorSelect?.Invoke(this, new ColorExtra
			{
				Color = SelectedFontColor,
				Background = SelectedColor
			});
		}
	}

	private void NotifyFontColorSelected()
	{
		_fontColors.ForEach(delegate(ItemColorViewModel it)
		{
			it.Selected = false;
		});
		ItemColorViewModel itemColorViewModel = _fontColors.FirstOrDefault((ItemColorViewModel it) => !it.NoColor && string.Equals(it.Color, _selectedFontColor, StringComparison.CurrentCultureIgnoreCase));
		if (itemColorViewModel != null)
		{
			_fontColors[0].Selected = false;
			itemColorViewModel.Selected = true;
		}
		else
		{
			_fontColors[0].Selected = true;
		}
		NotifyResetVisible();
	}

	private void NotifyColorSelected()
	{
		_bgColors1.ForEach(delegate(ItemColorViewModel it)
		{
			it.Selected = false;
		});
		_bgColors2.ForEach(delegate(ItemColorViewModel it)
		{
			it.Selected = false;
		});
		ItemColorViewModel itemColorViewModel = _bgColors1.FirstOrDefault((ItemColorViewModel it) => !it.NoColor && string.Equals(it.Color, _selectedColor, StringComparison.CurrentCultureIgnoreCase));
		ItemColorViewModel itemColorViewModel2 = _bgColors2.FirstOrDefault((ItemColorViewModel it) => !it.NoColor && string.Equals(it.Color, _selectedColor, StringComparison.CurrentCultureIgnoreCase));
		if (itemColorViewModel != null || itemColorViewModel2 != null)
		{
			if (itemColorViewModel != null)
			{
				itemColorViewModel.Selected = true;
			}
			if (itemColorViewModel2 != null)
			{
				itemColorViewModel2.Selected = true;
			}
		}
		else
		{
			_bgColors1[0].Selected = true;
		}
		NotifyResetVisible();
	}

	private void OnResetClick(object sender, RoutedEventArgs e)
	{
		SelectedColor = null;
		SelectedFontColor = null;
		this.ColorSelect?.Invoke(this, new ColorExtra
		{
			Color = SelectedFontColor,
			Background = SelectedColor
		});
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/misc/colorselector/colorselectorex.xaml", UriKind.Relative);
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
			Panel = (StackPanel)target;
			break;
		case 2:
			FontColorItems = (ItemsControl)target;
			break;
		case 3:
			DefaultColorItems1 = (ItemsControl)target;
			break;
		case 4:
			DefaultColorItems2 = (ItemsControl)target;
			break;
		case 5:
			ResetBtn = (Button)target;
			ResetBtn.Click += OnResetClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
