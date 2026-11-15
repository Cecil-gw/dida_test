using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.CustomControl;

internal class MultiSelectComboViewModel : INotifyPropertyChanged
{
	private string? _title;

	public string? Title
	{
		get
		{
			return _title;
		}
		set
		{
			if (!(_title == value))
			{
				_title = value;
				NotifyPropertyChanged("Title");
			}
		}
	}

	public ObservableCollection<DataComboBoxViewModel> Items { get; set; } = new ObservableCollection<DataComboBoxViewModel>();

	public event PropertyChangedEventHandler? PropertyChanged;

	public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public void SetItems(List<DataComboBoxViewModel> items)
	{
		Items.Clear();
		foreach (DataComboBoxViewModel item in items)
		{
			Items.Add(item);
		}
		UpdateTitle();
	}

	public void UpdateTitle()
	{
		IEnumerable<string> enumerable = from x in Items
			where x.Selected
			select x.Title;
		if (enumerable.Any())
		{
			Title = string.Join(", ", enumerable);
		}
		else
		{
			Title = Utils.GetString("none");
		}
	}
}
