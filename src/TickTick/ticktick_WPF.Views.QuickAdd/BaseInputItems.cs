using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace ticktick_WPF.Views.QuickAdd;

public abstract class BaseInputItems<T> : QuickInputItems
{
	private string _inputText;

	private InputItemViewModel<T> _selectedItem;

	public event EventHandler<InputItemViewModel<T>> OnItemSelected;

	protected void LoadData(bool selectFirst = true)
	{
		ObservableCollection<InputItemViewModel<T>> observableCollection = InitData();
		if (observableCollection != null && observableCollection.Count > 0)
		{
			if (selectFirst)
			{
				observableCollection[0].Selected = true;
			}
			Items.ItemsSource = observableCollection;
		}
	}

	private bool IsExactlyMatched()
	{
		InputItemViewModel<T> selectedItem = GetSelectedItem();
		if (selectedItem != null)
		{
			if (!string.Equals(selectedItem.Title, _inputText, StringComparison.CurrentCultureIgnoreCase))
			{
				return selectedItem.Pinyin.Text == _inputText;
			}
			return true;
		}
		return false;
	}

	public override bool Filter(string filter, List<string> selected = null)
	{
		try
		{
			string key = filter.ToLower();
			_inputText = key;
			ObservableCollection<InputItemViewModel<T>> observableCollection = InitData();
			if (string.IsNullOrEmpty(key))
			{
				observableCollection[0].Selected = true;
				Items.ItemsSource = observableCollection;
				return true;
			}
			List<InputItemViewModel<T>> list = observableCollection.Where((InputItemViewModel<T> item) => item != null && ((!string.IsNullOrEmpty(item.Title) && item.Title.ToLower().Contains(key)) || item.Pinyin.Contains(key))).ToList();
			if (list.Count > 0)
			{
				ObservableCollection<InputItemViewModel<T>> observableCollection2 = new ObservableCollection<InputItemViewModel<T>>(list);
				observableCollection2[0].Selected = true;
				ObservableCollection<InputItemViewModel<T>> itemsSource = observableCollection2;
				Items.ItemsSource = itemsSource;
				return true;
			}
			if (IsTag())
			{
				Items.ItemsSource = new ObservableCollection<InputItemViewModel<T>>();
				return true;
			}
			Items.ItemsSource = new ObservableCollection<InputItemViewModel<T>>();
			return false;
		}
		catch (Exception)
		{
			return false;
		}
	}

	protected abstract ObservableCollection<InputItemViewModel<T>> InitData();

	protected override void OnItemClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is Grid { DataContext: InputItemViewModel<T> dataContext })
		{
			this.OnItemSelected?.Invoke(this, dataContext);
			e.Handled = true;
		}
	}

	public override void TrySelectItem(bool exactly = false)
	{
		InputItemViewModel<T> selectedItem = GetSelectedItem();
		if (selectedItem != null && (!exactly || IsExactlyMatched()))
		{
			this.OnItemSelected?.Invoke(this, selectedItem);
		}
	}

	public override bool TrySelectIteWithSpace(string content)
	{
		if (Items.HasItems && Items.ItemsSource is ObservableCollection<InputItemViewModel<T>> observableCollection)
		{
			if (observableCollection.Count != 1)
			{
				return false;
			}
			InputItemViewModel<T> inputItemViewModel = observableCollection.FirstOrDefault((InputItemViewModel<T> model) => model.Selected);
			if (inputItemViewModel != null)
			{
				if (!string.Equals(content, inputItemViewModel.Title, StringComparison.CurrentCultureIgnoreCase))
				{
					return false;
				}
				this.OnItemSelected?.Invoke(this, inputItemViewModel);
				return true;
			}
		}
		return false;
	}

	protected override void OnItemEnter(object sender, MouseEventArgs e)
	{
		if (sender is Grid { DataContext: InputItemViewModel<T> dataContext })
		{
			if (_selectedItem == null)
			{
				_selectedItem = dataContext;
			}
			else
			{
				_selectedItem.Selected = false;
				_selectedItem = dataContext;
			}
			dataContext.Selected = true;
		}
	}

	private InputItemViewModel<T> GetSelectedItem()
	{
		if (Items.HasItems && Items.ItemsSource is ObservableCollection<InputItemViewModel<T>> source)
		{
			return source.FirstOrDefault((InputItemViewModel<T> model) => model.Selected);
		}
		return null;
	}

	public override void Move(bool forward)
	{
		if (!Items.HasItems || !(Items.ItemsSource is ObservableCollection<InputItemViewModel<T>> observableCollection))
		{
			return;
		}
		InputItemViewModel<T> selectedItem = GetSelectedItem();
		if (selectedItem == null)
		{
			observableCollection[0].Selected = true;
			return;
		}
		int num = observableCollection.TakeWhile((InputItemViewModel<T> model) => !model.Selected).Count();
		int num2 = (forward ? (num - 1) : (num + 1));
		if (num2 < 0)
		{
			num2 = observableCollection.Count - 1;
		}
		else if (num2 >= observableCollection.Count)
		{
			num2 = 0;
		}
		selectedItem.Selected = false;
		_selectedItem = observableCollection[num2];
		observableCollection[num2].Selected = true;
		Items.ScrollIntoView(observableCollection[num2]);
	}
}
