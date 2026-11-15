using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.CustomControl;

namespace ticktick_WPF.Views;

public class DropdownDialog : UserControl, IComponentConnector
{
	private List<ListItemData> _itemsSource;

	internal UpDownSelectListView Listview;

	private bool _contentLoaded;

	public List<ListItemData> ItemsSource
	{
		get
		{
			return _itemsSource;
		}
		set
		{
			_itemsSource = value;
			Listview.ItemsSource = _itemsSource;
		}
	}

	public event EventHandler<ListItemData> OnItemSelected;

	public DropdownDialog()
	{
		InitializeComponent();
	}

	private void OnItemClick(bool onEnter, UpDownSelectViewModel e)
	{
		this.OnItemSelected?.Invoke(this, e as ListItemData);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/dropdowndialog.xaml", UriKind.Relative);
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
		if (connectionId == 1)
		{
			Listview = (UpDownSelectListView)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
