using System.Windows.Media.Imaging;
using ticktick_WPF.Util;

namespace ticktick_WPF.ViewModels;

public class AvatarComboBoxViewModel : AvatarViewModel
{
	public new string AvatarUrl
	{
		set
		{
			SetAvatar(value);
		}
	}

	public new string Name { get; set; }

	public new string UserId { get; set; }

	public new string UserCode { get; set; }

	public BitmapImage Avatar { get; set; }

	private async void SetAvatar(string avatarUrl)
	{
		Avatar = await AvatarHelper.GetAvatarByUrlAsync(avatarUrl);
		OnPropertyChanged("Avatar");
	}
}
