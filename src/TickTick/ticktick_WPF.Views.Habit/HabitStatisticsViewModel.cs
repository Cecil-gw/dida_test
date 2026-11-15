using System.ComponentModel;
using System.Runtime.CompilerServices;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Habit;

public class HabitStatisticsViewModel : INotifyPropertyChanged
{
	private bool _isShowGrayBack;

	public bool IsShowGrayBack
	{
		get
		{
			return _isShowGrayBack;
		}
		set
		{
			if (_isShowGrayBack != value)
			{
				_isShowGrayBack = value;
				NotifyPropertyChanged("IsShowGrayBack");
			}
		}
	}

	public int MonthCheckinCount { get; set; }

	public int TotalCheckinCount { get; set; }

	public string Unit { get; set; }

	public int MonthCheckinRate { get; set; }

	public double MonthCompletion { get; set; }

	public double TotalCompletion { get; set; }

	public int CurrentStreak { get; set; }

	public int BestStreak { get; set; }

	public bool UseCardStyle { get; set; }

	public string MonthCheckUnit => GetDayOrDays(MonthCheckinCount);

	public string TotalCheckUnit => GetDayOrDays(TotalCheckinCount);

	public string CurrentStreakUnit => GetDayOrDays(CurrentStreak);

	public string BestStreakUnit => GetDayOrDays(BestStreak);

	public event PropertyChangedEventHandler PropertyChanged;

	public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public string GetDayOrDays(int day)
	{
		return Utils.GetString((day > 1) ? "PublicUpDays" : "PublicUpDay");
	}
}
