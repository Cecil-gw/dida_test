using System.Collections.Generic;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Search;

public class SearchHistoryViewModel : BaseViewModel
{
	private bool _selected;

	public string SearchKey { get; set; }

	public List<string> Tags { get; set; }

	public string FirstTag { get; set; }

	public string SecondTag { get; set; }

	public int MoreNum { get; set; }

	public bool Selected
	{
		get
		{
			return _selected;
		}
		set
		{
			_selected = value;
			OnPropertyChanged("Selected");
		}
	}

	public SearchHistoryViewModel(SearchExtra model)
	{
		SearchKey = model.SearchKey;
		Tags = model.Tags;
		FirstTag = ((Tags != null && Tags.Count > 0) ? Tags[0] : null);
		SecondTag = ((Tags != null && Tags.Count > 1) ? Tags[1] : null);
		MoreNum = ((Tags != null && Tags.Count > 2) ? (Tags.Count - 2) : 0);
		Selected = false;
	}
}
