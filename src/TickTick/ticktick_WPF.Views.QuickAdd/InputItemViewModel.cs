using System.Windows.Media;
using System.Windows.Media.Imaging;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.QuickAdd;

public class InputItemViewModel<T> : InputItemViewModel
{
	public T Entity { get; }

	public InputItemViewModel(string title, string value, string url, T entity)
	{
		base.Title = title;
		base.Value = value;
		base.ImageUrl = url;
		Entity = entity;
		base.Pinyin = PinyinUtils.ToPinyin(title);
	}

	public InputItemViewModel(string title, string value, T entity, bool needPy = true, Geometry icon = null)
	{
		base.Title = title;
		base.Value = value;
		Entity = entity;
		base.Icon = icon;
		if (needPy)
		{
			base.Pinyin = PinyinUtils.ToPinyin(title);
		}
	}

	public InputItemViewModel(string title, T entity)
	{
		base.Title = title;
		base.Value = title;
		Entity = entity;
		base.Pinyin = PinyinUtils.ToPinyin(title);
	}
}
public class InputItemViewModel : BaseViewModel
{
	private string _imageUrl;

	private bool _selected;

	private bool _highLightSelected;

	public string Title { get; set; }

	public string Value { get; set; }

	public Pinyin Pinyin { get; set; }

	public Geometry Icon { get; set; }

	public BitmapImage Avatar { get; set; }

	public bool ShowIcon => Icon != null;

	public string ImageUrl
	{
		get
		{
			return _imageUrl;
		}
		set
		{
			_imageUrl = value;
			SetAvatar();
		}
	}

	public bool IsNoAvatar { get; set; }

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

	public bool HighLightSelected
	{
		get
		{
			return _highLightSelected;
		}
		set
		{
			_highLightSelected = value;
			OnPropertyChanged("HighLightSelected");
		}
	}

	private async void SetAvatar()
	{
		Avatar = await AvatarHelper.GetAvatarByUrlAsync(ImageUrl);
		OnPropertyChanged("Avatar");
	}
}
