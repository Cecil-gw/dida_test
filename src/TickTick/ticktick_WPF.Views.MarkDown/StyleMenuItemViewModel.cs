using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.MarkDown;

public class StyleMenuItemViewModel : BaseViewModel
{
	private bool _showSplitLine;

	private bool _isVisible = true;

	private bool _isChecked;

	private bool _isSelected;

	public string Action { get; set; }

	public string Text { get; set; }

	public string Icon { get; set; }

	public bool ShowSplitLine
	{
		get
		{
			return _showSplitLine;
		}
		set
		{
			_showSplitLine = value;
			OnPropertyChanged("ShowSplitLine");
		}
	}

	public bool Enabled { get; set; } = true;

	public bool OriginalVisible { get; set; }

	public bool IsVisible
	{
		get
		{
			return _isVisible;
		}
		set
		{
			_isVisible = value;
			OnPropertyChanged("IsVisible");
		}
	}

	public bool IsChecked
	{
		get
		{
			return _isChecked;
		}
		set
		{
			_isChecked = value;
			OnPropertyChanged("IsChecked");
		}
	}

	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			_isSelected = value;
			OnPropertyChanged("IsSelected");
		}
	}

	public StyleMenuItemViewModel(string action, string text, bool originalVisible = true)
	{
		Action = action;
		Text = text;
		Icon = MarkDownAction.TryGetIcon(action);
		IsVisible = originalVisible;
		OriginalVisible = originalVisible;
	}

	public string GetIcon(string name)
	{
		return string.Empty;
	}
}
