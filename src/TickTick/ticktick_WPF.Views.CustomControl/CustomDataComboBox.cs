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
using Emoji.Wpf;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.CustomControl;

public class CustomDataComboBox : UserControl, IComponentConnector
{
	public static readonly DependencyProperty PopupMaxWidthProperty = DependencyProperty.Register("PopupMaxWidth", typeof(double), typeof(CustomDataComboBox), new PropertyMetadata(double.PositiveInfinity, null));

	public static readonly DependencyProperty TabSelectedProperty = DependencyProperty.Register("TabSelected", typeof(bool), typeof(CustomDataComboBox), new PropertyMetadata(false, null));

	public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register("SelectedIndex", typeof(int), typeof(CustomDataComboBox), new PropertyMetadata(-1, SelectedIndexChanged));

	public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(List<DataComboBoxViewModel>), typeof(CustomDataComboBox), new PropertyMetadata(null, ItemsSourceChanged));

	private bool _mouseDown;

	internal CustomDataComboBox Root;

	internal Grid Panel;

	internal EmjTextBlock SelectedText;

	internal EscPopup ListPopup;

	internal UpDownSelectListView ListView;

	private bool _contentLoaded;

	public double PopupMaxWidth
	{
		get
		{
			return (double)GetValue(PopupMaxWidthProperty);
		}
		set
		{
			SetValue(PopupMaxWidthProperty, value);
		}
	}

	public int SelectedIndex
	{
		get
		{
			return (int)GetValue(SelectedIndexProperty);
		}
		set
		{
			SetValue(SelectedIndexProperty, value);
		}
	}

	public List<string> ItemsSource
	{
		get
		{
			return (List<string>)GetValue(ItemsSourceProperty);
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

	public DataComboBoxViewModel SelectedItem { get; set; }

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

	public ObservableCollection<DataComboBoxViewModel> Items => ListView.ItemsSource as ObservableCollection<DataComboBoxViewModel>;

	public bool NeedClearCapture { get; set; }

	public HashSet<string> IgnoreValues { get; set; } = new HashSet<string>();

	public event EventHandler<DataComboBoxViewModel> ItemSelected;

	private static void ItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is CustomDataComboBox customDataComboBox && e.NewValue is List<DataComboBoxViewModel> items)
		{
			customDataComboBox.Init(items);
		}
	}

	private static void SelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is CustomDataComboBox customDataComboBox && e.NewValue is int selectedIndex)
		{
			customDataComboBox.SetSelectedIndex(selectedIndex);
		}
	}

	public CustomDataComboBox()
	{
		InitializeComponent();
	}

	public void Init(List<DataComboBoxViewModel> items)
	{
		ObservableCollection<DataComboBoxViewModel> observableCollection = new ObservableCollection<DataComboBoxViewModel>();
		foreach (DataComboBoxViewModel item in items)
		{
			observableCollection.Add(item);
		}
		ListView.ItemsSource = observableCollection;
		if (SelectedIndex >= 0 && SelectedIndex < observableCollection.Count)
		{
			DataComboBoxViewModel dataComboBoxViewModel = (SelectedItem = observableCollection[SelectedIndex]);
			SelectedItem.Selected = true;
			SelectedText.Text = dataComboBoxViewModel.Title;
		}
		else if (observableCollection.Count > 0)
		{
			SelectedItem = observableCollection[0];
			SelectedItem.Selected = true;
			SelectedText.Text = SelectedItem.Title;
		}
	}

	private void SetSelectedIndex(int index)
	{
		SelectedIndex = index;
		ObservableCollection<DataComboBoxViewModel> items = Items;
		if (items == null || index >= items.Count)
		{
			return;
		}
		foreach (DataComboBoxViewModel item in Items)
		{
			item.Selected = false;
		}
		items[index].Selected = true;
		SelectedText.Text = items[index].Title;
	}

	public DataComboBoxViewModel GetSelectItemOrDefault()
	{
		DataComboBoxViewModel dataComboBoxViewModel = SelectedItem;
		if (dataComboBoxViewModel == null)
		{
			ObservableCollection<DataComboBoxViewModel> items = Items;
			if (items == null || items.Count <= 0)
			{
				return null;
			}
			dataComboBoxViewModel = Items[0];
		}
		return dataComboBoxViewModel;
	}

	private void OnItemSelected(bool onEnter, UpDownSelectViewModel e)
	{
		if (e is DataComboBoxViewModel dataComboBoxViewModel)
		{
			if (dataComboBoxViewModel.IsSplit)
			{
				return;
			}
			if (!IgnoreValues.Contains(dataComboBoxViewModel.Title))
			{
				if (Items != null)
				{
					foreach (DataComboBoxViewModel item in Items)
					{
						item.Selected = false;
					}
				}
				dataComboBoxViewModel.Selected = true;
				SelectedItem = dataComboBoxViewModel;
				SelectedText.Text = dataComboBoxViewModel.Title;
				SelectedIndex = Items.IndexOf(dataComboBoxViewModel);
			}
			else
			{
				SetSelectedIndex(SelectedIndex);
			}
			this.ItemSelected?.Invoke(this, dataComboBoxViewModel);
		}
		ListPopup.IsOpen = false;
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
			if (Items.Count > 0)
			{
				ListView.ScrollIntoView(Items.Last());
				UpdateLayout();
				DataComboBoxViewModel dataComboBoxViewModel = SelectedItem ?? Items.First();
				int num = Items.IndexOf(dataComboBoxViewModel);
				ListView.ScrollIntoView((num > 0) ? Items[num - 1] : dataComboBoxViewModel);
			}
			e.Handled = true;
		}
	}

	public bool HandleEnter()
	{
		if (!ListPopup.IsOpen)
		{
			_mouseDown = true;
			ListPopup.IsOpen = true;
			if (Items.Count > 0)
			{
				ListView.ScrollIntoView(Items.Last());
				UpdateLayout();
				DataComboBoxViewModel dataComboBoxViewModel = SelectedItem ?? Items.First();
				int num = Items.IndexOf(dataComboBoxViewModel);
				ListView.ScrollIntoView((num > 0) ? Items[num - 1] : dataComboBoxViewModel);
				ListView.ClearHover();
				dataComboBoxViewModel.HoverSelected = true;
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

	private new void MouseDown(object sender, MouseButtonEventArgs e)
	{
		if (!ListPopup.IsOpen)
		{
			_mouseDown = true;
		}
	}

	public void SetSelectedValue(string val)
	{
		DataComboBoxViewModel dataComboBoxViewModel = Items.FirstOrDefault((DataComboBoxViewModel i) => i.Title == val);
		if (dataComboBoxViewModel == null)
		{
			return;
		}
		int num = (SelectedIndex = Items.IndexOf(dataComboBoxViewModel));
		ObservableCollection<DataComboBoxViewModel> items = Items;
		if (items == null || num >= items.Count)
		{
			return;
		}
		foreach (DataComboBoxViewModel item in Items)
		{
			item.Selected = false;
		}
		dataComboBoxViewModel.Selected = true;
		SelectedText.Text = dataComboBoxViewModel.Title;
	}

	public string GetSelectedValue()
	{
		return Items.FirstOrDefault((DataComboBoxViewModel i) => i.Selected)?.Title;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/customcontrol/customdatacombobox.xaml", UriKind.Relative);
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
			Root = (CustomDataComboBox)target;
			break;
		case 2:
			((Border)target).PreviewMouseLeftButtonDown += MouseDown;
			((Border)target).PreviewMouseLeftButtonUp += ShowPopup;
			break;
		case 3:
			Panel = (Grid)target;
			break;
		case 4:
			SelectedText = (EmjTextBlock)target;
			break;
		case 5:
			ListPopup = (EscPopup)target;
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
