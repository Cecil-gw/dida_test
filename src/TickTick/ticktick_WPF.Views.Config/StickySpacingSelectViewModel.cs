using System.Collections.Generic;
using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Config;

public class StickySpacingSelectViewModel : BaseViewModel
{
	private bool _selected;

	public int Spacing;

	private readonly List<StickySpacingSelectViewModel> _parentList;

	public string Title { get; set; }

	public int DisplayMargin { get; set; }

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

	public StickySpacingSelectViewModel(string title, int margin, int spacing, List<StickySpacingSelectViewModel> parent)
	{
		Title = title;
		Spacing = spacing;
		DisplayMargin = margin;
		_parentList = parent;
		_selected = LocalSettings.Settings.ExtraSettings.StickySpacing == spacing;
	}

	public void SetSelected()
	{
		_parentList?.ForEach(delegate(StickySpacingSelectViewModel m)
		{
			m.Selected = false;
		});
		Selected = true;
	}
}
