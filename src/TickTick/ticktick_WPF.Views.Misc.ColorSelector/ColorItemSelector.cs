using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using TickTickUtils;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;

namespace ticktick_WPF.Views.Misc.ColorSelector;

public class ColorItemSelector : UserControl, IComponentConnector
{
	public static readonly DependencyProperty WithRecommendColorsProperty = DependencyProperty.Register("WithRecommendColors", typeof(bool), typeof(ColorItemSelector), new PropertyMetadata(false));

	public static readonly DependencyProperty AllowCancelSelectedProperty = DependencyProperty.Register("AllowCancelSelected", typeof(bool), typeof(ColorItemSelector), new PropertyMetadata(true));

	public static readonly DependencyProperty MoreColorSolutionProperty = DependencyProperty.Register("MoreColorSolution", typeof(int), typeof(ColorItemSelector), new PropertyMetadata(0, OnMoreColorSolutionChanged));

	public static readonly DependencyProperty EmptyDisplayColorProperty = DependencyProperty.Register("EmptyDisplayColor", typeof(Color?), typeof(ColorItemSelector), new PropertyMetadata(null));

	private IReadOnlyList<string> _recommendedColors;

	private bool _showNoColor = true;

	private string _selectedColor;

	internal ColorItemSelector Root;

	internal ItemsControl RecColorItems;

	internal ItemsControl McrColorItems;

	internal ItemsControl MrdColorItems;

	internal ItemsControl RccColorItems;

	internal ItemsControl ClassicColorItems;

	internal ItemsControl MphColorItems;

	internal ItemsControl CustomItems;

	private bool _contentLoaded;

	public bool WithRecommendColors
	{
		get
		{
			return (bool)GetValue(WithRecommendColorsProperty);
		}
		set
		{
			SetValue(WithRecommendColorsProperty, value);
		}
	}

	public bool AllowCancelSelected
	{
		get
		{
			return (bool)GetValue(AllowCancelSelectedProperty);
		}
		set
		{
			SetValue(AllowCancelSelectedProperty, value);
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

	public Color? EmptyDisplayColor
	{
		get
		{
			return (Color?)GetValue(EmptyDisplayColorProperty);
		}
		set
		{
			SetValue(EmptyDisplayColorProperty, value);
		}
	}

	public IEnumerable<string> RecommendedColors
	{
		get
		{
			return _recommendedColors;
		}
		set
		{
			_recommendedColors = value?.ToList();
			LoadData();
		}
	}

	public bool ShowNoColor
	{
		get
		{
			return _showNoColor;
		}
		set
		{
			if (_showNoColor != value)
			{
				_showNoColor = value;
				LoadData();
			}
		}
	}

	public static List<string> DefaultColors { get; } = new List<string> { "#FF6161", "#FFAC38", "#FFD324", "#E6EA49", "#35D870", "#4CA1FF", "#6E75F4" };

	public static List<string> DefaultCountdownColors { get; } = new List<string> { "#FFAE58", "#7BD866", "#54CBBB", "#67A2FA", "#C096FF", "#767DFD", "#FF7ABF", "#FF7070" };

	public static List<string> MoreColorList { get; } = new List<string>
	{
		"#F68F8A", "#F8C3B2", "#FCDEAA", "#C2DED0", "#63C5D2", "#6FAAF3", "#C09EEB", "#F678AC", "#D27762", "#F7C989",
		"#DCDE6B", "#A7C9B9", "#7BCAD3", "#86A9DE", "#AD94C7", "#C77B9B", "#BE7471", "#DD882E", "#E3CE7B", "#76A692",
		"#5DAF9C", "#4682B4", "#838BB0", "#CB8A90", "#EC6666", "#F2B04B", "#FFD966", "#DDE358", "#5DD1A8", "#4AA6EF",
		"#CF66F7", "#ED70A5", "#EF2D24", "#FF7D5D", "#FFD457", "#02DBB2", "#34BAC5", "#8C5EFA", "#CE89FE", "#F964C2"
	};

	public static List<string> MoreCountdownColorList { get; } = new List<string>
	{
		"#F68F8A", "#FFB499", "#FFCF7B", "#9AD9B9", "#63C5D2", "#6FAAF3", "#C09EEB", "#F678AC", "#D27762", "#F0C890",
		"#EDC287", "#92BFAA", "#7BCAD3", "#86A9DE", "#AD94C7", "#C77B9B", "#BE7471", "#DD882E", "#DCC25C", "#76A692",
		"#5DAF9C", "#4682B4", "#838BB0", "#CB8A90", "#EC6666", "#F2B04B", "#FDC928", "#CED806", "#5DD1A8", "#4AA6EF",
		"#CF66F7", "#ED70A5", "#EF2D24", "#FF7D5D", "#F6BF21", "#02DBB2", "#34BAC5", "#8C5EFA", "#CE89FE", "#F964C2"
	};

	private List<ItemColorViewModel> _colorItems { get; } = new List<ItemColorViewModel>();

	private ObservableCollection<ItemColorViewModel> _customItems { get; } = new ObservableCollection<ItemColorViewModel>();

	public event EventHandler<string> ColorSelect;

	private static void OnMoreColorSolutionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		((ColorItemSelector)d).LoadData();
	}

	public ColorItemSelector()
	{
		InitializeComponent();
		LoadData();
	}

	private void LoadData()
	{
		_colorItems.Clear();
		_customItems.Clear();
		ObservableCollection<ItemColorViewModel> observableCollection = new ObservableCollection<ItemColorViewModel>();
		ObservableCollection<ItemColorViewModel> observableCollection2 = new ObservableCollection<ItemColorViewModel>();
		ObservableCollection<ItemColorViewModel> observableCollection3 = new ObservableCollection<ItemColorViewModel>();
		ObservableCollection<ItemColorViewModel> observableCollection4 = new ObservableCollection<ItemColorViewModel>();
		ObservableCollection<ItemColorViewModel> observableCollection5 = new ObservableCollection<ItemColorViewModel>();
		ObservableCollection<ItemColorViewModel> observableCollection6 = new ObservableCollection<ItemColorViewModel>();
		RecColorItems.ItemsSource = observableCollection;
		McrColorItems.ItemsSource = observableCollection2;
		MrdColorItems.ItemsSource = observableCollection3;
		RccColorItems.ItemsSource = observableCollection4;
		ClassicColorItems.ItemsSource = observableCollection5;
		MphColorItems.ItemsSource = observableCollection6;
		CustomItems.ItemsSource = _customItems;
		if (MoreColorSolution == 0)
		{
			if (ShowNoColor)
			{
				observableCollection.Add(new ItemColorViewModel
				{
					NoColor = true
				});
			}
			foreach (string item2 in RecommendedColors ?? DefaultColors)
			{
				observableCollection.Add(new ItemColorViewModel
				{
					Color = item2
				});
			}
		}
		else
		{
			foreach (string defaultCountdownColor in DefaultCountdownColors)
			{
				observableCollection.Add(new ItemColorViewModel
				{
					Color = defaultCountdownColor
				});
			}
		}
		List<string> list = MoreColorList;
		if (MoreColorSolution == 1)
		{
			list = MoreCountdownColorList;
		}
		for (int i = 0; i < list.Count; i++)
		{
			ItemColorViewModel item = new ItemColorViewModel
			{
				Color = list[i]
			};
			switch (i / 8)
			{
			case 0:
				observableCollection2.Add(item);
				break;
			case 1:
				observableCollection3.Add(item);
				break;
			case 2:
				observableCollection4.Add(item);
				break;
			case 3:
				observableCollection5.Add(item);
				break;
			case 4:
				observableCollection6.Add(item);
				break;
			}
		}
		_customItems.Add(new ItemColorViewModel
		{
			IsAddCustom = true
		});
		List<string> list2 = LocalSettings.Settings.UserPreference?.RecentlyColors?.colors;
		if (list2 != null)
		{
			foreach (string item3 in list2)
			{
				_customItems.Add(CreateCustomColorItem(item3));
			}
		}
		_colorItems.AddRange(observableCollection);
		_colorItems.AddRange(observableCollection2);
		_colorItems.AddRange(observableCollection3);
		_colorItems.AddRange(observableCollection4);
		_colorItems.AddRange(observableCollection5);
		_colorItems.AddRange(observableCollection6);
	}

	public void SetItemSelected(string selectedColor)
	{
		string text = selectedColor;
		if (text == null && EmptyDisplayColor.HasValue)
		{
			text = ThemeUtil.GetNoAlphaColorString(EmptyDisplayColor.ToString());
		}
		foreach (ItemColorViewModel colorItem in _colorItems)
		{
			colorItem.Selected = string.Equals(colorItem.Color, text, StringComparison.OrdinalIgnoreCase);
		}
		foreach (ItemColorViewModel customItem in _customItems)
		{
			customItem.Selected = string.Equals(customItem.Color, text, StringComparison.OrdinalIgnoreCase);
		}
		_selectedColor = selectedColor;
	}

	private void OnItemClick(object sender, MouseButtonEventArgs e)
	{
		if (!(sender is ColorSelectItem { DataContext: ItemColorViewModel dataContext } colorSelectItem))
		{
			return;
		}
		e.Handled = true;
		if (!dataContext.IsAddCustom && dataContext.Selected)
		{
			if (AllowCancelSelected)
			{
				SetItemSelected(null);
				this.ColorSelect?.Invoke(null, "transparent");
			}
			else
			{
				this.ColorSelect?.Invoke(null, dataContext.Color);
			}
			return;
		}
		if (dataContext.IsAddCustom)
		{
			EscPopup obj = new EscPopup
			{
				PlacementTarget = colorSelectItem,
				StaysOpen = false,
				Placement = PlacementMode.Top,
				VerticalOffset = 10.0,
				HorizontalOffset = -4.0
			};
			ColorPicker colorPicker = (ColorPicker)(obj.Child = new ColorPicker(obj, _selectedColor));
			colorPicker.ColorSelected += delegate(object o, string brush)
			{
				OnCustomColorSelect(brush);
			};
			obj.IsOpen = true;
			return;
		}
		_colorItems.ForEach(delegate(ItemColorViewModel m)
		{
			m.Selected = false;
		});
		foreach (ItemColorViewModel customItem in _customItems)
		{
			customItem.Selected = customItem.Color == dataContext.Color;
		}
		dataContext.Selected = true;
		_selectedColor = dataContext.Color;
		this.ColorSelect?.Invoke(null, dataContext.Color);
	}

	private void OnCustomColorSelect(string color)
	{
		foreach (ItemColorViewModel customItem in _customItems)
		{
			customItem.Selected = false;
		}
		ItemColorViewModel exist = _customItems.FirstOrDefault((ItemColorViewModel m) => m.Color == color);
		if (exist != null)
		{
			_customItems.Remove(exist);
			_customItems.Insert(1, exist);
		}
		else
		{
			exist = CreateCustomColorItem(color);
			_customItems.Insert(1, exist);
			if (_customItems.Count > 8)
			{
				_customItems.RemoveAt(8);
			}
		}
		exist.Selected = true;
		_colorItems.ForEach(delegate(ItemColorViewModel m)
		{
			m.Selected = m.Color == exist.Color;
		});
		_selectedColor = exist.Color;
		this.ColorSelect?.Invoke(null, exist.Color);
	}

	private static ItemColorViewModel CreateCustomColorItem(string color)
	{
		return new ItemColorViewModel
		{
			Color = color,
			IsContrastingColor = ColorUtils.NeedsContrastBorder(color, ThemeUtil.IsDark())
		};
	}

	private void OnColorRightClick(object sender, MouseButtonEventArgs e)
	{
		if (!(sender is FrameworkElement { DataContext: var dataContext }))
		{
			return;
		}
		ItemColorViewModel model = dataContext as ItemColorViewModel;
		if (model != null)
		{
			List<CustomMenuItemViewModel> types = new List<CustomMenuItemViewModel>
			{
				new CustomMenuItemViewModel("delete", Utils.GetString("Delete"), Utils.GetGeometry("Ic.ProjectList.Delete"))
				{
					ShowSelected = false,
					IsDelete = true
				}
			};
			EscPopup popup = new EscPopup
			{
				StaysOpen = false,
				Placement = PlacementMode.MousePoint,
				PlacementTarget = this,
				VerticalOffset = -5.0,
				HorizontalOffset = -5.0
			};
			CustomMenuList customMenuList = new CustomMenuList(types, popup);
			customMenuList.SetMinWidth(96.0);
			customMenuList.Operated += delegate
			{
				_customItems.Remove(model);
				LocalSettings.Settings.RemoveCustomColor(model.Color);
			};
			customMenuList.Show();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/misc/colorselector/coloritemselector.xaml", UriKind.Relative);
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
			Root = (ColorItemSelector)target;
			break;
		case 2:
			RecColorItems = (ItemsControl)target;
			break;
		case 3:
			McrColorItems = (ItemsControl)target;
			break;
		case 4:
			MrdColorItems = (ItemsControl)target;
			break;
		case 5:
			RccColorItems = (ItemsControl)target;
			break;
		case 6:
			ClassicColorItems = (ItemsControl)target;
			break;
		case 7:
			MphColorItems = (ItemsControl)target;
			break;
		case 8:
			CustomItems = (ItemsControl)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
