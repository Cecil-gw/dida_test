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
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Filter;

public class ConditionDateItem : Grid, IComponentConnector
{
	private bool _mouseDown;

	internal Grid Container;

	internal TextBlock BeforeText;

	internal TextBox InputNum;

	internal TextBlock AfterText;

	private bool _contentLoaded;

	public ConditionDateItem()
	{
		InitializeComponent();
	}

	private void OnItemClick(object sender, MouseButtonEventArgs e)
	{
		if (!_mouseDown)
		{
			return;
		}
		_mouseDown = false;
		if (base.DataContext is FilterListItemViewModel filterListItemViewModel)
		{
			Utils.FindParent<ConditionEditDialog>(this)?.OnItemSelected(filterListItemViewModel);
			if (filterListItemViewModel.Selected)
			{
				InputNum.Focus();
				InputNum.SelectAll();
			}
		}
	}

	private void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (base.DataContext is FilterListItemViewModel { DisplayType: var displayType })
		{
			List<string> list = Utils.GetString(displayType.ToString()).Replace("{0}", "|").Split('|')
				.ToList();
			list.Remove(string.Empty);
			if (list.Count == 2)
			{
				BeforeText.Text = list[0];
				AfterText.Text = list[1].Replace("{1}", "th");
			}
			else if (list.Count > 0)
			{
				BeforeText.Text = string.Empty;
				BeforeText.Margin = new Thickness(6.0, 0.0, 0.0, 0.0);
				AfterText.Text = list[0];
			}
		}
	}

	private async void OnIntervalTextChanged(object sender, TextChangedEventArgs e)
	{
		if (!int.TryParse(InputNum.Text, out var result))
		{
			return;
		}
		if (result > 30)
		{
			InputNum.Text = "30";
			InputNum.SelectAll();
			result = 30;
		}
		else if (result <= 0)
		{
			InputNum.Text = "1";
			InputNum.SelectAll();
			result = 1;
		}
		if (base.DataContext is FilterListItemViewModel filterListItemViewModel)
		{
			filterListItemViewModel.NDaysValue = result;
			switch (filterListItemViewModel.DisplayType)
			{
			case FilterItemDisplayType.AfterNDays:
				filterListItemViewModel.Value = result + "daysfromtoday";
				break;
			case FilterItemDisplayType.NDaysAgo:
				filterListItemViewModel.Value = "-" + result + "daysfromtoday";
				break;
			case FilterItemDisplayType.NDaysLater:
				filterListItemViewModel.Value = result + "dayslater";
				break;
			case FilterItemDisplayType.NextNDays:
				filterListItemViewModel.Value = result + "days";
				break;
			}
			Utils.FindParent<ConditionEditDialog>(this)?.ValueChanged();
		}
	}

	private void HandleNumberInput(object sender, TextCompositionEventArgs e)
	{
		if (e.Text.Length >= 1 && !char.IsDigit(e.Text, e.Text.Length - 1))
		{
			e.Handled = true;
		}
	}

	private void OnInputFocused(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is FilterListItemViewModel { Selected: false } filterListItemViewModel)
		{
			Utils.FindParent<ConditionEditDialog>(this)?.OnItemSelected(filterListItemViewModel);
		}
	}

	private void OnItemDown(object sender, MouseButtonEventArgs e)
	{
		_mouseDown = true;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/filter/conditiondateitem.xaml", UriKind.Relative);
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
			((ConditionDateItem)target).DataContextChanged += OnDataBind;
			break;
		case 2:
			Container = (Grid)target;
			Container.MouseLeftButtonUp += OnItemClick;
			Container.MouseLeftButtonDown += OnItemDown;
			break;
		case 3:
			BeforeText = (TextBlock)target;
			break;
		case 4:
			InputNum = (TextBox)target;
			InputNum.PreviewTextInput += HandleNumberInput;
			InputNum.TextChanged += OnIntervalTextChanged;
			InputNum.GotFocus += OnInputFocused;
			break;
		case 5:
			AfterText = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
