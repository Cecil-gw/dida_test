using System.Collections.ObjectModel;

namespace ticktick_WPF.Entity.Binding;

internal class AIRecommendAllGroup
{
	public ObservableCollection<AIRecommendGroup> DefaultGroups { get; set; }

	public ObservableCollection<AIRecommendGroup> AIRecommendGroups { get; set; }

	public ObservableCollection<AIRecommendGroup> DetailGroups { get; set; }
}
