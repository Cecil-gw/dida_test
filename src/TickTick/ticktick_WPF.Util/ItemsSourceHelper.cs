using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util;

public class ItemsSourceHelper
{
	public static void SetHidableItemsSource<T>(ItemsControl itemsControl, List<T> items) where T : BaseHidableViewModel
	{
		if (itemsControl.ItemsSource is ObservableCollection<T> observableCollection)
		{
			int num = Math.Min(observableCollection.Count, items.Count);
			if (observableCollection.Count != items.Count)
			{
				int num2 = Math.Abs(observableCollection.Count - items.Count);
				for (int i = num; i < num + num2; i++)
				{
					if (observableCollection.Count > items.Count)
					{
						observableCollection[i].IsHide = true;
					}
					else if (observableCollection.Count < items.Count)
					{
						observableCollection.Add(items[i]);
					}
				}
			}
			for (int j = 0; j < num; j++)
			{
				observableCollection[j] = items[j];
			}
		}
		else
		{
			itemsControl.ItemsSource = new ObservableCollection<T>(items);
		}
	}

	public static void SetItemsSource<T>(ItemsControl itemsControl, List<T> items) where T : INotifyPropertyChanged
	{
		if (items == null)
		{
			items = new List<T>();
		}
		if (itemsControl.ItemsSource is ObservableCollection<T> observableCollection)
		{
			int num = Math.Min(observableCollection.Count, items.Count);
			if (observableCollection.Count != items.Count)
			{
				int num2 = Math.Abs(observableCollection.Count - items.Count);
				ObservableCollection<T> observableCollection2 = new ObservableCollection<T>();
				for (int i = num; i < num + num2; i++)
				{
					if (observableCollection.Count > items.Count)
					{
						observableCollection2.Add(observableCollection[i]);
					}
					else if (observableCollection.Count < items.Count)
					{
						observableCollection.Add(items[i]);
					}
				}
				foreach (T item in observableCollection2)
				{
					observableCollection.Remove(item);
				}
			}
			for (int j = 0; j < num; j++)
			{
				observableCollection[j] = items[j];
			}
		}
		else
		{
			itemsControl.ItemsSource = new ObservableCollection<T>(items);
		}
	}

	public static void CopyTo<T>(List<T> source, ObservableCollection<T> target)
	{
		lock (target)
		{
			int num = Math.Min(source.Count, target.Count);
			if (source.Count != target.Count)
			{
				int num2 = Math.Abs(source.Count - target.Count);
				List<T> list = new List<T>();
				for (int i = num; i < num + num2; i++)
				{
					if (target.Count > source.Count)
					{
						list.Add(target[i]);
					}
					else if (target.Count < source.Count)
					{
						target.Add(source[i]);
					}
				}
				foreach (T item in list)
				{
					target.Remove(item);
				}
			}
			for (int j = 0; j < num; j++)
			{
				T val = target[j];
				T val2 = source[j];
				if (val == null || val2 == null || !val.Equals(val2))
				{
					target[j] = val2;
				}
			}
		}
	}

	public static void RemoveMissing<T>(List<T> source, ObservableCollection<T> target)
	{
		HashSet<T> hashSet = new HashSet<T>(source);
		lock (target)
		{
			for (int num = target.Count - 1; num >= 0; num--)
			{
				if (!hashSet.Contains(target[num]))
				{
					target.RemoveAt(num);
				}
			}
		}
	}
}
