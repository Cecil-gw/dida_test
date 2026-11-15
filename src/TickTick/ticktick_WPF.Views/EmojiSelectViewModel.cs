using System.Collections.Generic;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views;

public class EmojiSelectViewModel : BaseViewModel
{
	private bool _folded;

	public string Text { get; set; }

	public string Key { get; set; }

	public bool IsSection { get; set; }

	public bool IsEmojiItems
	{
		get
		{
			List<EmojiSelectViewModel> children = Children;
			if (children == null)
			{
				return false;
			}
			return children.Count > 0;
		}
	}

	public bool Folded
	{
		get
		{
			return _folded;
		}
		set
		{
			_folded = value;
			OnPropertyChanged("Folded");
		}
	}

	public List<EmojiSelectViewModel> Children { get; set; }
}
