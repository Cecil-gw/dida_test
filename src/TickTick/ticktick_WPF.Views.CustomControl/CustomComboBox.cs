using System;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using Emoji.Wpf;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.CustomControl;

public class CustomComboBox : Grid, IComponentConnector
{
	public static readonly DependencyProperty TabSelectedProperty = DependencyProperty.Register("TabSelected", typeof(bool), typeof(CustomComboBox), new PropertyMetadata(false, null));

	public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(ObservableCollection<ComboBoxViewModel>), typeof(CustomComboBox), new PropertyMetadata(null, ItemsSourceChanged));

	private bool _mouseDown;

	public static readonly DependencyProperty CustomTitleProperty = DependencyProperty.Register("CustomTitle", typeof(string), typeof(CustomComboBox), new PropertyMetadata(null, OnCustomTitleChange));

	public static readonly DependencyProperty PopupMinWidthProperty = DependencyProperty.Register("PopupMinWidth", typeof(double), typeof(CustomComboBox), new PropertyMetadata(0.0, null));

	public static readonly DependencyProperty AutoPopupWidthProperty = DependencyProperty.Register("AutoPopupWidth", typeof(bool), typeof(CustomComboBox), new PropertyMetadata(false, null));

	public static readonly DependencyProperty SelectedTextFontSizeProperty = DependencyProperty.Register("SelectedTextFontSize", typeof(double), typeof(CustomComboBox), new PropertyMetadata(13.0, null));

	public static readonly DependencyProperty RadiusProperty = DependencyProperty.Register("Radius", typeof(double), typeof(CustomComboBox), new PropertyMetadata(8.0));

	private bool popupGotFocus;

	internal CustomComboBox Root;

	internal Border BackBorder;

	internal EmjTextBlock SelectedText;

	internal EscPopup ListPopup;

	internal ContentControl PopupContent;

	internal UpDownSelectListView ListView;

	private bool _contentLoaded;

	public ObservableCollection<ComboBoxViewModel> ItemsSource
	{
		get
		{
			return (ObservableCollection<ComboBoxViewModel>)GetValue(ItemsSourceProperty);
		}
		set
		{
			SetValue(ItemsSourceProperty, value);
		}
	}

	public bool TabSelected
	{
		get
		{
			return (bool)GetValue(TabSelectedProperty);
		}
		set
		{
			SetValue(TabSelectedProperty, value);
		}
	}

	public ComboBoxViewModel SelectedItem { get; set; }

	public bool IsOpen
	{
		get
		{
			return ListPopup.IsOpen;
		}
		set
		{
			ListPopup.IsOpen = value;
		}
	}

	public string CustomTitle
	{
		get
		{
			return (string)GetValue(CustomTitleProperty);
		}
		set
		{
			SetValue(CustomTitleProperty, value);
		}
	}

	public double PopupMinWidth
	{
		get
		{
			return (double)GetValue(PopupMinWidthProperty);
		}
		set
		{
			SetValue(PopupMinWidthProperty, value);
		}
	}

	public bool AutoPopupWidth
	{
		get
		{
			return (bool)GetValue(AutoPopupWidthProperty);
		}
		set
		{
			SetValue(AutoPopupWidthProperty, value);
		}
	}

	public double SelectedTextFontSize
	{
		get
		{
			return (double)GetValue(SelectedTextFontSizeProperty);
		}
		set
		{
			SetValue(SelectedTextFontSizeProperty, value);
		}
	}

	public double Radius
	{
		get
		{
			return (double)GetValue(RadiusProperty);
		}
		set
		{
			SetValue(RadiusProperty, value);
		}
	}

	public ObservableCollection<ComboBoxViewModel> Items => ListView.ItemsSource as ObservableCollection<ComboBoxViewModel>;

	public bool NeedClearCapture { get; set; }

	public int SelectedIndex
	{
		get
		{
			if (SelectedItem != null)
			{
				return Items?.IndexOf(SelectedItem) ?? 0;
			}
			return 0;
		}
	}

	public event EventHandler<ComboBoxViewModel> ItemSelected;

	private static void ItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is CustomComboBox customComboBox && e.NewValue is ObservableCollection<ComboBoxViewModel> items)
		{
			customComboBox.Init(items, null);
		}
	}

	private static void OnCustomTitleChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is CustomComboBox customComboBox)
		{
			customComboBox.SelectedText.Text = e.NewValue as string;
		}
	}

	public CustomComboBox()
	{
		InitializeComponent();
	}

	public void Init<T>(ObservableCollection<T> items, T selected) where T : ComboBoxViewModel
	{
		if (selected == null && items.Any())
		{
			selected = items.FirstOrDefault((T i) => i.Selected) ?? items[0];
			selected.Selected = true;
		}
		ListView.ItemsSource = items;
		SelectedItem = selected;
		if (string.IsNullOrEmpty(CustomTitle) && selected != null)
		{
			SelectedText.Text = selected.SelectedTitle;
		}
	}

	public void Init<T>(ObservableCollection<T> items) where T : ComboBoxViewModel
	{
		ListView.ItemsSource = items;
	}

	public ComboBoxViewModel GetSelectItemOrDefault()
	{
		ComboBoxViewModel comboBoxViewModel = SelectedItem;
		if (comboBoxViewModel == null)
		{
			ObservableCollection<ComboBoxViewModel> items = Items;
			if (items == null || items.Count <= 0)
			{
				return null;
			}
			comboBoxViewModel = Items[0];
		}
		return comboBoxViewModel;
	}

	public void SetSelected(object selected)
	{
		if (Items == null)
		{
			return;
		}
		foreach (ComboBoxViewModel item in Items)
		{
			item.Selected = item.Value.Equals(selected);
			if (item.Selected)
			{
				SelectedItem = item;
				if (string.IsNullOrEmpty(CustomTitle))
				{
					SelectedText.Text = item.SelectedTitle;
				}
			}
		}
	}

	public void ClearSelected()
	{
		if (Items != null)
		{
			foreach (ComboBoxViewModel item in Items)
			{
				item.Selected = false;
			}
		}
		SelectedItem = null;
	}

	private void OnItemSelected(bool onEnter, UpDownSelectViewModel e)
	{
		if (e is ComboBoxViewModel comboBoxViewModel)
		{
			if (comboBoxViewModel.CanSelect)
			{
				if (Items != null)
				{
					foreach (ComboBoxViewModel item in Items)
					{
						item.Selected = false;
					}
				}
				comboBoxViewModel.Selected = true;
				SelectedItem = comboBoxViewModel;
				if (string.IsNullOrEmpty(CustomTitle))
				{
					SelectedText.Text = comboBoxViewModel.SelectedTitle;
				}
			}
			this.ItemSelected?.Invoke(this, comboBoxViewModel);
		}
		ListPopup.IsOpen = false;
	}

	private new void MouseDown(object sender, MouseButtonEventArgs e)
	{
		if (!ListPopup.IsOpen)
		{
			_mouseDown = true;
		}
	}

	private void ShowPopup(object sender, MouseButtonEventArgs e)
	{
		if (_mouseDown)
		{
			_mouseDown = false;
			if (NeedClearCapture)
			{
				Mouse.Capture(null);
			}
			ListPopup.IsOpen = true;
			ObservableCollection<ComboBoxViewModel> items = Items;
			if (items != null && items.Count > 0)
			{
				ListView.ScrollIntoView(Items.Last());
				UpdateLayout();
				ComboBoxViewModel comboBoxViewModel = SelectedItem ?? Items.First();
				int num = Items.IndexOf(comboBoxViewModel);
				ListView.ScrollIntoView((num > 0) ? Items[num - 1] : comboBoxViewModel);
			}
			e.Handled = true;
		}
	}

	public bool HandleEnter()
	{
		if (!ListPopup.IsOpen)
		{
			ListPopup.IsOpen = true;
			if (Items.Count > 0)
			{
				ListView.ScrollIntoView(Items.Last());
				UpdateLayout();
				ComboBoxViewModel comboBoxViewModel = SelectedItem ?? Items.First();
				int num = Items.IndexOf(comboBoxViewModel);
				ListView.ScrollIntoView((num > 0) ? Items[num - 1] : comboBoxViewModel);
				ListView.ClearHover();
				comboBoxViewModel.HoverSelected = true;
			}
			return true;
		}
		return ListPopup.HandleEnter();
	}

	public void UpDownSelect(bool isUp)
	{
		ListView.UpDownSelect(isUp);
	}

	public void Close()
	{
		ListPopup.IsOpen = false;
	}

	public void SetPopupPosition(PlacementMode placement, int h, int v)
	{
		ListPopup.Placement = placement;
		ListPopup.VerticalOffset = v;
		ListPopup.HorizontalOffset = h;
	}

	private void ListPopup_GotFocus(object sender, RoutedEventArgs e)
	{
		popupGotFocus = true;
	}

	private void ListPopup_LostFocus(object sender, RoutedEventArgs e)
	{
		popupGotFocus = false;
	}

	private async void Root_LostFocus(object sender, RoutedEventArgs e)
	{
		await Task.Delay(100);
		if (!popupGotFocus)
		{
			ListPopup.IsOpen = false;
		}
	}

	private void Root_Loaded(object sender, RoutedEventArgs e)
	{
		UpdatePopupWidth();
	}

	private void ListPopup_Opened(object sender, EventArgs e)
	{
		UpdatePopupWidth();
	}

	private void UpdatePopupWidth()
	{
		double num = Math.Max(PopupMinWidth, base.ActualWidth);
		PopupContent.MinWidth = num;
		PopupContent.MaxWidth = (AutoPopupWidth ? double.PositiveInfinity : 1000.0);
		PopupContent.Width = (AutoPopupWidth ? double.NaN : (num + 16.0));
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/customcontrol/customcombobox.xaml", UriKind.Relative);
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
			Root = (CustomComboBox)target;
			Root.LostFocus += Root_LostFocus;
			Root.Loaded += Root_Loaded;
			break;
		case 2:
			BackBorder = (Border)target;
			BackBorder.PreviewMouseLeftButtonDown += MouseDown;
			BackBorder.PreviewMouseLeftButtonUp += ShowPopup;
			break;
		case 3:
			SelectedText = (EmjTextBlock)target;
			break;
		case 4:
			ListPopup = (EscPopup)target;
			break;
		case 5:
			PopupContent = (ContentControl)target;
			break;
		case 6:
			ListView = (UpDownSelectListView)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
