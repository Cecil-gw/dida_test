using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ticktick_WPF.Entity.Enums;

namespace ticktick_WPF.Entity.Binding;

internal class AIRecommendGroup : INotifyPropertyChanged
{
	public string Title { get; set; }

	public bool IsShowMore
	{
		get
		{
			return _isShowMore;
		}
		set
		{
			if (_isShowMore != value)
			{
				_isShowMore = value;
				NotifyPropertyChanged("IsShowMore");
			}
		}
	}

	private bool _isShowMore { get; set; }

	public AIRecommendGroupType GroupType { get; set; }

	public RecommendTotalGroupType TotalType { get; set; }

	public ObservableCollection<AIRecommendItem> Items { get; set; } = new ObservableCollection<AIRecommendItem>();

	public List<AIRecommendTaskInfo> Tasks { get; set; }

	public event PropertyChangedEventHandler PropertyChanged;

	public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
