using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.CustomControl;

namespace ticktick_WPF.Views.Search;

public class TimeFilterDialog : UserControl, IComponentConnector
{
	private List<ListItemData> _itemsSource;

	internal UpDownSelectListView Listview;

	internal SelectTimeSpanDialog SelectTimeDialog;

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

	public event EventHandler<DateFilterData> OnFilterSelect;

	public event EventHandler OnCancel;

	public event EventHandler OnEndEditDate;

	public TimeFilterDialog()
	{
		InitializeComponent();
	}

	public void SetStartDate(DateTime? startDate)
	{
		SelectTimeDialog.SetStartDate(startDate);
	}

	public void SetEndDate(DateTime? endDate)
	{
		SelectTimeDialog.SetEndDate(endDate);
	}

	public bool IsMouseOverNestedPopup()
	{
		return SelectTimeDialog.IsMouseOverDatePopup();
	}

	private async void OnItemClick(bool onEnter, UpDownSelectViewModel model)
	{
		if (model == null)
		{
			return;
		}
		if (model is ListItemData listItemData && (DateFilter)listItemData.Key != DateFilter.Custom)
		{
			this.OnFilterSelect?.Invoke(this, new DateFilterData
			{
				Type = (DateFilter)listItemData.Key
			});
			return;
		}
		Listview.Visibility = Visibility.Collapsed;
		SelectTimeDialog.Visibility = Visibility.Visible;
		SelectTimeDialog.OnEndEdit += delegate
		{
			this.OnEndEditDate?.Invoke(this, null);
		};
		SelectTimeDialog.Cancel += delegate
		{
			this.OnCancel?.Invoke(this, null);
		};
		SelectTimeDialog.OnSpanSelect += delegate(object obj, SelectTimeSpanViewModel date)
		{
			if (date.StartDate.HasValue && date.EndDate.HasValue)
			{
				this.OnFilterSelect?.Invoke(this, new DateFilterData
				{
					Type = DateFilter.Custom,
					StartDate = date.StartDate.Value,
					EndDate = date.EndDate.Value
				});
			}
		};
		await Task.Delay(10);
		HwndHelper.SetFocus(SelectTimeDialog);
		SelectTimeDialog.FocusBox.Focus();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/search/timefilterdialog.xaml", UriKind.Relative);
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
			Listview = (UpDownSelectListView)target;
			break;
		case 2:
			SelectTimeDialog = (SelectTimeSpanDialog)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
