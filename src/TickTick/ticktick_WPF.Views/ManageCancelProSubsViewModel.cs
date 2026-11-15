using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ticktick_WPF.Views;

public class ManageCancelProSubsViewModel : INotifyPropertyChanged
{
	public enum StepPage
	{
		Manage,
		CancelSubs,
		Apple,
		Canceled
	}

	private string _title;

	private StepPage _step;

	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			_title = value;
			NotifyPropertyChanged("Title");
		}
	}

	public StepPage Step
	{
		get
		{
			return _step;
		}
		set
		{
			_step = value;
			NotifyPropertyChanged("Step");
		}
	}

	public string FreqPlan { get; set; }

	public string SubsTypeText { get; set; }

	public string CancelPremiumDesc { get; set; }

	public string RawSubsType { get; set; }

	public event PropertyChangedEventHandler PropertyChanged;

	public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
