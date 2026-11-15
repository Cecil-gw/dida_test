using System;
using System.ComponentModel;
using System.Windows;

namespace ticktick_WPF.ViewModels;

[Serializable]
public class BaseHidableViewModel : INotifyPropertyChanged
{
	private bool _isHide;

	public bool IsHide
	{
		get
		{
			return _isHide;
		}
		set
		{
			_isHide = value;
			OnPropertyChanged("IsHide");
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	protected void OnPropertyChanged(string propertyName = null)
	{
		if (this.PropertyChanged != null)
		{
			Application.Current?.Dispatcher?.InvokeAsync(delegate
			{
				this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			});
		}
	}

	public BaseHidableViewModel Clone()
	{
		return (BaseHidableViewModel)MemberwiseClone();
	}
}
