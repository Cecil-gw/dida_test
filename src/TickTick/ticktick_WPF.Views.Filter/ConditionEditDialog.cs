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

namespace ticktick_WPF.Views.Filter;

public class ConditionEditDialog : UserControl, IComponentConnector
{
	private FilterConditionViewModel _viewModel;

	internal Grid AllItem;

	internal Grid LogicSelector;

	internal ListView ListView;

	internal Button SaveBtn;

	internal Button CancelBtn;

	private bool _contentLoaded;

	protected FilterConditionViewModel ViewModel
	{
		get
		{
			return _viewModel;
		}
		set
		{
			_viewModel = value;
			base.DataContext = value;
			if (value.ItemsSource != null)
			{
				ListView.ItemsSource = value.ItemsSource;
			}
		}
	}

	public bool IsSave { get; set; }

	private static bool IsUsedInSearch => false;

	public event EventHandler OnCancel;

	public event EventHandler<FilterConditionViewModel> OnSave;

	public event EventHandler<LogicType> OnLogicChanged;

	public event EventHandler<FilterConditionViewModel> OnSelectedValueChanged;

	public ConditionEditDialog()
	{
		InitializeComponent();
		CancelBtn.Click += delegate
		{
			Restore();
			this.OnCancel?.Invoke(this, null);
		};
		SaveBtn.Click += delegate
		{
			IsSave = true;
			if (CanSave())
			{
				SyncOriginal();
				this.OnSave?.Invoke(this, ViewModel);
			}
		};
		if (IsUsedInSearch)
		{
			SaveBtn.Content = Utils.GetString("OK");
		}
	}

	protected virtual bool CanSave()
	{
		if (ViewModel.IsAllSelected)
		{
			return ViewModel.ShowAll;
		}
		return false;
	}

	private void OnItemClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is ListView listView && e.OriginalSource is DependencyObject element && ItemsControl.ContainerFromElement(listView, element) is ListViewItem container && listView.ItemContainerGenerator.ItemFromContainer(container) is FilterListItemViewModel { DisplayType: FilterItemDisplayType.Normal } filterListItemViewModel && !HandleCheckedAble(filterListItemViewModel))
		{
			OnItemSelected(filterListItemViewModel);
		}
	}

	public void OnItemSelected(FilterListItemViewModel selectedItem)
	{
		selectedItem.Selected = !selectedItem.Selected;
		if (selectedItem.IsProjectGroup || (selectedItem.IsAssignee && string.IsNullOrEmpty(selectedItem.GroupId)))
		{
			SetChildrenSelectedRecursive(selectedItem, selectedItem.Selected);
			selectedItem.PartSelected = false;
		}
		if ((selectedItem.IsSecondLevel || selectedItem.IsAssignee) && !string.IsNullOrEmpty(selectedItem.GroupId))
		{
			FilterListItemViewModel filterListItemViewModel = _viewModel.ItemsSource.FirstOrDefault((FilterListItemViewModel model) => model.Value?.ToString() == selectedItem.GroupId);
			if (filterListItemViewModel != null)
			{
				if (selectedItem.Selected)
				{
					filterListItemViewModel.PartSelected = true;
				}
				else
				{
					filterListItemViewModel.Selected = false;
					filterListItemViewModel.PartSelected = filterListItemViewModel.Children?.Any((FilterListItemViewModel item) => item.Selected) ?? false;
				}
			}
		}
		if (selectedItem.Selected)
		{
			if (selectedItem.IsAllItem)
			{
				foreach (FilterListItemViewModel item in _viewModel.ItemsSource)
				{
					if (!item.IsAllItem)
					{
						item.Selected = false;
					}
				}
			}
			else
			{
				foreach (FilterListItemViewModel item2 in _viewModel.ItemsSource)
				{
					if (item2.IsAllItem)
					{
						item2.Selected = false;
						break;
					}
				}
			}
		}
		if (selectedItem.Selected)
		{
			_viewModel.IsAllSelected = false;
		}
		else
		{
			SetAllSelected();
		}
		this.OnSelectedValueChanged?.Invoke(this, _viewModel);
		SaveBtn.IsEnabled = CanSave();
	}

	public virtual void Restore()
	{
	}

	protected virtual void SyncOriginal()
	{
	}

	private void SetAllSelected()
	{
		FilterListItemViewModel filterListItemViewModel = _viewModel.ItemsSource.FirstOrDefault((FilterListItemViewModel x) => x.Selected);
		_viewModel.IsAllSelected = filterListItemViewModel == null;
	}

	protected virtual bool HandleCheckedAble(FilterListItemViewModel selectedItem)
	{
		return false;
	}

	private void OnLogicSelected(object sender, LogicType e)
	{
		this.OnLogicChanged?.Invoke(this, e);
	}

	private void OnAllToggle(object sender, MouseButtonEventArgs e)
	{
		_viewModel.IsAllSelected = !_viewModel.IsAllSelected;
		if (_viewModel.IsAllSelected)
		{
			foreach (FilterListItemViewModel item in _viewModel.ItemsSource)
			{
				item.Selected = false;
			}
		}
		this.OnSelectedValueChanged?.Invoke(this, null);
	}

	public void OnOpenClick(FilterListItemViewModel model)
	{
		model.Unfold = !model.Unfold;
		if (model.Unfold)
		{
			int index = _viewModel.ItemsSource.IndexOf(model);
			if (index >= 0)
			{
				InsertVisibleChildren(_viewModel.ItemsSource, model, ref index);
			}
		}
		else
		{
			RemoveDescendants(_viewModel.ItemsSource, model);
		}
	}

	protected static IEnumerable<FilterListItemViewModel> EnumerateDescendants(FilterListItemViewModel model)
	{
		if (model?.Children == null)
		{
			yield break;
		}
		foreach (FilterListItemViewModel child in model.Children)
		{
			yield return child;
			foreach (FilterListItemViewModel item in EnumerateDescendants(child))
			{
				yield return item;
			}
		}
	}

	protected static void SetChildrenSelectedRecursive(FilterListItemViewModel model, bool selected)
	{
		foreach (FilterListItemViewModel item in EnumerateDescendants(model))
		{
			item.Selected = selected;
			item.PartSelected = false;
		}
	}

	protected static void InsertVisibleChildren(ObservableCollection<FilterListItemViewModel> itemSource, FilterListItemViewModel model, ref int index)
	{
		if (model?.Children == null)
		{
			return;
		}
		foreach (FilterListItemViewModel child in model.Children)
		{
			if (!itemSource.Contains(child))
			{
				itemSource.Insert(++index, child);
			}
			if (child.Unfold)
			{
				InsertVisibleChildren(itemSource, child, ref index);
			}
		}
	}

	protected static void RemoveDescendants(ObservableCollection<FilterListItemViewModel> itemSource, FilterListItemViewModel model)
	{
		foreach (FilterListItemViewModel item in EnumerateDescendants(model).ToList())
		{
			itemSource.Remove(item);
		}
	}

	public void ValueChanged()
	{
		this.OnSelectedValueChanged?.Invoke(this, _viewModel);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/filter/conditioneditdialog.xaml", UriKind.Relative);
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
			AllItem = (Grid)target;
			AllItem.MouseLeftButtonUp += OnAllToggle;
			break;
		case 2:
			LogicSelector = (Grid)target;
			break;
		case 3:
			ListView = (ListView)target;
			ListView.MouseLeftButtonUp += OnItemClick;
			break;
		case 4:
			SaveBtn = (Button)target;
			break;
		case 5:
			CancelBtn = (Button)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
