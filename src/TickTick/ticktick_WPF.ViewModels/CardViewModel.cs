using ticktick_WPF.Views.Filter;

namespace ticktick_WPF.ViewModels;

public class CardViewModel : BaseViewModel
{
	private string _content;

	private CardType _type;

	public int Version = 2;

	public LogicType LogicType { get; set; } = LogicType.Or;

	public int Index { get; set; }

	public CardType Type
	{
		get
		{
			return _type;
		}
		set
		{
			_type = value;
			OnPropertyChanged("Type");
		}
	}

	public string Content
	{
		get
		{
			return _content;
		}
		set
		{
			_content = value;
			OnPropertyChanged("Content");
		}
	}

	public string ConditionName { get; set; } = string.Empty;

	public bool ShowDropdown { get; set; }

	public virtual ConditionEditDialog GenerateEditDialog()
	{
		return null;
	}

	public virtual string ToCardText()
	{
		return string.Empty;
	}
}
