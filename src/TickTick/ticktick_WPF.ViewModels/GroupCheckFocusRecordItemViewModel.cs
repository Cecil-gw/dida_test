using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ticktick_WPF.ViewModels;

internal class GroupCheckFocusRecordItemViewModel : INotifyPropertyChanged
{
	private CheckFocusRecordItemViewModel _DateTitle;

	public CheckFocusRecordItemViewModel DateTitle
	{
		get
		{
			return _DateTitle;
		}
		set
		{
			if (_DateTitle != null)
			{
				value.SelectedChanged = (Action<CheckFocusRecordItemViewModel>)Delegate.Remove(value.SelectedChanged, new Action<CheckFocusRecordItemViewModel>(OnTitleCheckChanged));
			}
			_DateTitle = value;
			NotifyPropertyChanged("DateTitle");
			value.SelectedChanged = (Action<CheckFocusRecordItemViewModel>)Delegate.Combine(value.SelectedChanged, new Action<CheckFocusRecordItemViewModel>(OnTitleCheckChanged));
		}
	}

	public ObservableCollection<CheckFocusRecordItemViewModel> Items { get; set; } = new ObservableCollection<CheckFocusRecordItemViewModel>();

	public int SelectedCount => Items.Count((CheckFocusRecordItemViewModel x) => x.IsSelected == true);

	public Action OnCheckedCountChanged { get; set; }

	private bool isChanging { get; set; }

	public event PropertyChangedEventHandler PropertyChanged;

	public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public GroupCheckFocusRecordItemViewModel()
	{
		Items.CollectionChanged += Items_CollectionChanged;
	}

	private void OnTitleCheckChanged(CheckFocusRecordItemViewModel model)
	{
		if (isChanging)
		{
			return;
		}
		isChanging = true;
		try
		{
			if (model.IsSelected.HasValue)
			{
				bool? isSelected = model.IsSelected;
				foreach (CheckFocusRecordItemViewModel item in Items)
				{
					item.IsSelected = isSelected.Value;
				}
				OnCheckedCountChanged?.Invoke();
			}
		}
		catch (Exception)
		{
		}
		isChanging = false;
	}

	private void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		if (e.NewItems != null)
		{
			foreach (object newItem in e.NewItems)
			{
				if (newItem is CheckFocusRecordItemViewModel checkFocusRecordItemViewModel)
				{
					checkFocusRecordItemViewModel.SelectedChanged = (Action<CheckFocusRecordItemViewModel>)Delegate.Combine(checkFocusRecordItemViewModel.SelectedChanged, new Action<CheckFocusRecordItemViewModel>(OnItemSelectionChanged));
				}
			}
		}
		if (e.OldItems == null)
		{
			return;
		}
		foreach (object oldItem in e.OldItems)
		{
			if (oldItem is CheckFocusRecordItemViewModel checkFocusRecordItemViewModel2)
			{
				checkFocusRecordItemViewModel2.SelectedChanged = (Action<CheckFocusRecordItemViewModel>)Delegate.Remove(checkFocusRecordItemViewModel2.SelectedChanged, new Action<CheckFocusRecordItemViewModel>(OnItemSelectionChanged));
			}
		}
		if (isChanging)
		{
			return;
		}
		isChanging = true;
		try
		{
			if (!Items.Any())
			{
				DateTitle.IsSelected = false;
			}
			else if (Items.All((CheckFocusRecordItemViewModel x) => x.IsSelected == true))
			{
				DateTitle.IsSelected = true;
			}
			else if (Items.All((CheckFocusRecordItemViewModel x) => x.IsSelected == false))
			{
				DateTitle.IsSelected = false;
			}
			else
			{
				DateTitle.IsSelected = null;
			}
		}
		catch (Exception)
		{
		}
		isChanging = false;
	}

	private void OnItemSelectionChanged(CheckFocusRecordItemViewModel model)
	{
		if (isChanging)
		{
			return;
		}
		isChanging = true;
		try
		{
			bool isSelected = model.IsSelected.Value;
			if (Items.All((CheckFocusRecordItemViewModel x) => x.IsSelected == isSelected))
			{
				DateTitle.IsSelected = isSelected;
			}
			else
			{
				DateTitle.IsSelected = null;
			}
			OnCheckedCountChanged?.Invoke();
		}
		catch (Exception)
		{
		}
		isChanging = false;
	}
}
