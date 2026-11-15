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

public class CustomSimpleComboBox : UserControl, IComponentConnector
{
	public static readonly DependencyProperty TabSelectedProperty = DependencyProperty.Register("TabSelected", typeof(bool), typeof(CustomSimpleComboBox), new PropertyMetadata(false, null));

	public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register("SelectedIndex", typeof(int), typeof(CustomSimpleComboBox), new PropertyMetadata(-1, SelectedIndexChanged));

	public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(List<string>), typeof(CustomSimpleComboBox), new PropertyMetadata(null, ItemsSourceChanged));

	private bool _mouseDown;

	internal CustomSimpleComboBox Root;

	internal StackPanel Panel;

	internal EmjTextBlock SelectedText;

	internal EscPopup ListPopup;

	internal UpDownSelectListView ListView;

	private bool _contentLoaded;

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

	public SimpleComboBoxViewModel SelectedItem { get; set; }

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

	public ObservableCollection<SimpleComboBoxViewModel> Items => ListView.ItemsSource as ObservableCollection<SimpleComboBoxViewModel>;

	public bool NeedClearCapture { get; set; }

	public HashSet<string> IgnoreValues { get; set; } = new HashSet<string>();

	public event EventHandler<SimpleComboBoxViewModel> ItemSelected;

	private static void ItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is CustomSimpleComboBox customSimpleComboBox && e.NewValue is List<string> items)
		{
			customSimpleComboBox.Init(items);
		}
	}

	private static void SelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is CustomSimpleComboBox customSimpleComboBox && e.NewValue is int selectedIndex)
		{
			customSimpleComboBox.SetSelectedIndex(selectedIndex);
		}
	}

	public CustomSimpleComboBox()
	{
		InitializeComponent();
	}

	public void Init(List<string> items)
	{
		ObservableCollection<SimpleComboBoxViewModel> observableCollection = new ObservableCollection<SimpleComboBoxViewModel>();
		foreach (string item2 in items)
		{
			SimpleComboBoxViewModel item = new SimpleComboBoxViewModel(item2);
			observableCollection.Add(item);
		}
		ListView.ItemsSource = observableCollection;
		if (SelectedIndex >= 0 && SelectedIndex < observableCollection.Count)
		{
			SimpleComboBoxViewModel simpleComboBoxViewModel = (SelectedItem = observableCollection[SelectedIndex]);
			SelectedItem.Selected = true;
			SelectedText.Text = simpleComboBoxViewModel.Title;
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
		ObservableCollection<SimpleComboBoxViewModel> items = Items;
		if (items == null || index >= items.Count)
		{
			return;
		}
		foreach (SimpleComboBoxViewModel item in Items)
		{
			item.Selected = false;
		}
		items[index].Selected = true;
		SelectedText.Text = items[index].Title;
	}

	public SimpleComboBoxViewModel GetSelectItemOrDefault()
	{
		SimpleComboBoxViewModel simpleComboBoxViewModel = SelectedItem;
		if (simpleComboBoxViewModel == null)
		{
			ObservableCollection<SimpleComboBoxViewModel> items = Items;
			if (items == null || items.Count <= 0)
			{
				return null;
			}
			simpleComboBoxViewModel = Items[0];
		}
		return simpleComboBoxViewModel;
	}

	private void OnItemSelected(bool onEnter, UpDownSelectViewModel e)
	{
		if (e is SimpleComboBoxViewModel simpleComboBoxViewModel)
		{
			if (simpleComboBoxViewModel.IsSplit)
			{
				return;
			}
			if (!IgnoreValues.Contains(simpleComboBoxViewModel.Title))
			{
				if (Items != null)
				{
					foreach (SimpleComboBoxViewModel item in Items)
					{
						item.Selected = false;
					}
				}
				simpleComboBoxViewModel.Selected = true;
				SelectedItem = simpleComboBoxViewModel;
				SelectedText.Text = simpleComboBoxViewModel.Title;
				SelectedIndex = Items.IndexOf(simpleComboBoxViewModel);
			}
			else
			{
				SetSelectedIndex(SelectedIndex);
			}
			this.ItemSelected?.Invoke(this, simpleComboBoxViewModel);
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
				SimpleComboBoxViewModel simpleComboBoxViewModel = SelectedItem ?? Items.First();
				int num = Items.IndexOf(simpleComboBoxViewModel);
				ListView.ScrollIntoView((num > 0) ? Items[num - 1] : simpleComboBoxViewModel);
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
				SimpleComboBoxViewModel simpleComboBoxViewModel = SelectedItem ?? Items.First();
				int num = Items.IndexOf(simpleComboBoxViewModel);
				ListView.ScrollIntoView((num > 0) ? Items[num - 1] : simpleComboBoxViewModel);
				ListView.ClearHover();
				simpleComboBoxViewModel.HoverSelected = true;
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
		SimpleComboBoxViewModel simpleComboBoxViewModel = Items.FirstOrDefault((SimpleComboBoxViewModel i) => i.Title == val);
		if (simpleComboBoxViewModel == null)
		{
			return;
		}
		int num = (SelectedIndex = Items.IndexOf(simpleComboBoxViewModel));
		ObservableCollection<SimpleComboBoxViewModel> items = Items;
		if (items == null || num >= items.Count)
		{
			return;
		}
		foreach (SimpleComboBoxViewModel item in Items)
		{
			item.Selected = false;
		}
		simpleComboBoxViewModel.Selected = true;
		SelectedText.Text = simpleComboBoxViewModel.Title;
	}

	public string GetSelectedValue()
	{
		return Items.FirstOrDefault((SimpleComboBoxViewModel i) => i.Selected)?.Title;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/customcontrol/customsimplecombobox.xaml", UriKind.Relative);
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
			Root = (CustomSimpleComboBox)target;
			break;
		case 2:
			((Border)target).PreviewMouseLeftButtonDown += MouseDown;
			((Border)target).PreviewMouseLeftButtonUp += ShowPopup;
			break;
		case 3:
			Panel = (StackPanel)target;
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
