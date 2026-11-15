using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace ticktick_WPF.ViewModels;

[Serializable]
public class BaseViewModel : INotifyPropertyChanged
{
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

	protected void ChangeAndNotify<T>(ref T value, T newValue, [CallerMemberName] string propertyName = null)
	{
		value = newValue;
		OnPropertyChanged(propertyName);
	}
}
