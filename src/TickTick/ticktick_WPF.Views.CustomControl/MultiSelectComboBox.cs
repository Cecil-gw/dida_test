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
using Emoji.Wpf;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.CustomControl;

public class MultiSelectComboBox : UserControl, IComponentConnector, IStyleConnector
{
	internal MultiSelectComboBox Root;

	internal Grid Panel;

	internal EmjTextBlock SelectedText;

	internal EscPopup ListPopup;

	internal ListView MainList;

	private bool _contentLoaded;

	private bool _mouseDown { get; set; }

	private bool _isConfirm { get; set; }

	private MultiSelectComboViewModel viewModel { get; set; } = new MultiSelectComboViewModel();

	private List<object> lastSelectedList { get; set; } = new List<object>();

	public event EventHandler<List<DataComboBoxViewModel>> ItemSelected;

	public MultiSelectComboBox()
	{
		InitializeComponent();
		base.DataContext = viewModel;
	}

	public void Init(List<DataComboBoxViewModel> items)
	{
		viewModel.SetItems(items);
		lastSelectedList = (from x in viewModel.Items
			where x.Selected
			select x.Data).ToList();
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
		if (!_mouseDown)
		{
			return;
		}
		_mouseDown = false;
		_isConfirm = false;
		ListPopup.IsOpen = true;
		if (viewModel.Items.Count > 0)
		{
			DataComboBoxViewModel dataComboBoxViewModel = viewModel.Items.FirstOrDefault((DataComboBoxViewModel x) => x.Selected) ?? viewModel.Items.First();
			int num = viewModel.Items.IndexOf(dataComboBoxViewModel);
			MainList.ScrollIntoView((num > 0) ? viewModel.Items[num - 1] : dataComboBoxViewModel);
		}
		e.Handled = true;
	}

	private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: DataComboBoxViewModel dataContext })
		{
			dataContext.Selected = !dataContext.Selected;
		}
	}

	private void OkButton_Click(object sender, RoutedEventArgs e)
	{
		_isConfirm = true;
		ListPopup.IsOpen = false;
		lastSelectedList = (from x in viewModel.Items
			where x.Selected
			select x.Data).ToList();
		this.ItemSelected?.Invoke(this, viewModel.Items.Where((DataComboBoxViewModel x) => x.Selected).ToList());
	}

	private void CancelButton_Click(object sender, RoutedEventArgs e)
	{
		ListPopup.IsOpen = false;
	}

	private void ListPopup_Closed(object sender, EventArgs e)
	{
		if (!_isConfirm)
		{
			foreach (DataComboBoxViewModel item in viewModel.Items)
			{
				item.Selected = lastSelectedList.Contains(item.Data);
			}
		}
		viewModel.UpdateTitle();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/customcontrol/multiselectcombobox.xaml", UriKind.Relative);
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
			Root = (MultiSelectComboBox)target;
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
			MainList = (ListView)target;
			break;
		case 8:
			((Button)target).Click += OkButton_Click;
			break;
		case 9:
			((Button)target).Click += CancelButton_Click;
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
		if (connectionId == 7)
		{
			((Grid)target).MouseLeftButtonUp += Grid_MouseLeftButtonUp;
		}
	}
}
