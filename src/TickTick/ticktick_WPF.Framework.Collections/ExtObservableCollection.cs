using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace ticktick_WPF.Framework.Collections;

public class ExtObservableCollection<T> : ObservableCollection<T>
{
	protected override void ClearItems()
	{
		List<T> changedItems = new List<T>(this);
		base.ClearItems();
		base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, changedItems));
	}
}
