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
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.CustomControl;

namespace ticktick_WPF.Views.Filter;

public class ConditionSpanDateItem : UserControl, IComponentConnector
{
	private static readonly List<ComboBoxViewModel> FromDaysComboBoxItems;

	private static readonly List<ComboBoxViewModel> ToDaysComboBoxItems;

	internal Grid Container;

	internal CustomComboBox FromDayComboBox;

	internal CustomComboBox ToDayComboBox;

	private bool _contentLoaded;

	static ConditionSpanDateItem()
	{
		FromDaysComboBoxItems = new List<ComboBoxViewModel>();
		ToDaysComboBoxItems = new List<ComboBoxViewModel>();
		for (int i = -30; i < 31; i++)
		{
			string nthDayString = FilterUtils.GetNthDayString(i);
			FromDaysComboBoxItems.Add(new ComboBoxViewModel(i, nthDayString));
			ToDaysComboBoxItems.Add(new ComboBoxViewModel(i, nthDayString));
		}
		FromDaysComboBoxItems.Insert(0, new ComboBoxViewModel("-", "-"));
		ToDaysComboBoxItems.Add(new ComboBoxViewModel("-", "-"));
	}

	public ConditionSpanDateItem()
	{
		InitializeComponent();
	}

	private void OnItemClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is FilterListItemViewModel selectedItem)
		{
			Utils.FindParent<ConditionEditDialog>(this)?.OnItemSelected(selectedItem);
			e.Handled = true;
		}
	}

	private void OnComboBoxMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
	}

	private void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (base.DataContext is FilterListItemViewModel filterListItemViewModel)
		{
			SetComboBoxItems(FromDayComboBox, FromDaysComboBoxItems, filterListItemViewModel.DaysFrom, null);
			SetComboBoxItems(ToDayComboBox, ToDaysComboBoxItems, filterListItemViewModel.DaysTo, filterListItemViewModel.DaysFrom);
		}
	}

	private static void SetComboBoxItems(CustomComboBox comboBox, List<ComboBoxViewModel> data, int? selectedVal, int? minVal)
	{
		ObservableCollection<ComboBoxViewModel> observableCollection = new ObservableCollection<ComboBoxViewModel>();
		ComboBoxViewModel comboBoxViewModel = data.LastOrDefault();
		foreach (ComboBoxViewModel datum in data)
		{
			if (datum.Value is int num)
			{
				datum.IsEnable = !minVal.HasValue || num >= minVal;
				datum.Selected = selectedVal.HasValue && num == selectedVal;
			}
			else
			{
				datum.Selected = !selectedVal.HasValue;
			}
			comboBoxViewModel = (datum.Selected ? datum : comboBoxViewModel);
			observableCollection.Add(datum);
		}
		comboBox.Init(observableCollection, comboBoxViewModel);
	}

	private void OnFromDaySelected(object sender, ComboBoxViewModel e)
	{
		if (base.DataContext is FilterListItemViewModel filterListItemViewModel)
		{
			filterListItemViewModel.DaysFrom = e.Value as int?;
			if (filterListItemViewModel.DaysFrom.HasValue && filterListItemViewModel.DaysTo.HasValue && filterListItemViewModel.DaysTo < filterListItemViewModel.DaysFrom)
			{
				filterListItemViewModel.DaysTo = filterListItemViewModel.DaysFrom;
			}
			SetComboBoxItems(ToDayComboBox, ToDaysComboBoxItems, filterListItemViewModel.DaysTo, filterListItemViewModel.DaysFrom);
			filterListItemViewModel.Value = FilterUtils.GetSpanDateValue(filterListItemViewModel.DaysFrom, filterListItemViewModel.DaysTo);
			filterListItemViewModel.Selected = false;
			Utils.FindParent<ConditionEditDialog>(this)?.OnItemSelected(filterListItemViewModel);
		}
	}

	private void OnToDaySelected(object sender, ComboBoxViewModel e)
	{
		if (base.DataContext is FilterListItemViewModel filterListItemViewModel)
		{
			filterListItemViewModel.DaysTo = e.Value as int?;
			filterListItemViewModel.Value = FilterUtils.GetSpanDateValue(filterListItemViewModel.DaysFrom, filterListItemViewModel.DaysTo);
			filterListItemViewModel.Selected = false;
			Utils.FindParent<ConditionEditDialog>(this)?.OnItemSelected(filterListItemViewModel);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/filter/conditionspandateitem.xaml", UriKind.Relative);
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
			((ConditionSpanDateItem)target).DataContextChanged += OnDataBind;
			break;
		case 2:
			Container = (Grid)target;
			Container.MouseLeftButtonUp += OnItemClick;
			break;
		case 3:
			FromDayComboBox = (CustomComboBox)target;
			break;
		case 4:
			ToDayComboBox = (CustomComboBox)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
