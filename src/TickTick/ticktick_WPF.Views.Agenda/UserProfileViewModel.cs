using System.Windows.Media.Imaging;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Agenda;

public class UserProfileViewModel : BaseViewModel
{
	public string AvatarUrl { get; set; }

	public BitmapImage Avatar { get; set; }

	public string DisplayName { get; set; }

	public string UserCode { get; set; }

	public string Email { get; set; }

	public bool IsOwner { get; set; }

	public bool IsMySelf { get; }

	public bool ShowDelete { get; set; }

	public UserProfileViewModel(ProfileModel model)
	{
		AvatarUrl = model.avatarUrl;
		DisplayName = GetDisplayName(model.name, model.username, model.isMyself);
		Email = model.email;
		UserCode = model.userCode;
		IsMySelf = model.isMyself;
		SetAvatar();
	}

	public UserProfileViewModel(UserInfoModel user, bool isMyself)
	{
		AvatarUrl = user.picture;
		DisplayName = GetDisplayName(user.name, user.username, isMyself);
		Email = user.username;
		UserCode = user.userCode;
		IsMySelf = isMyself;
		SetAvatar();
	}

	public UserProfileViewModel(AttendeeModel model)
	{
		AvatarUrl = model.user.avatarUrl;
		DisplayName = GetDisplayName(model.user.name, model.user.username, model.user.isMyself);
		Email = model.user.email;
		UserCode = model.user.userCode;
		IsMySelf = model.user.isMyself;
		SetAvatar();
	}

	private async void SetAvatar()
	{
		Avatar = await AvatarHelper.GetAvatarByUrlAsync(AvatarUrl);
		OnPropertyChanged("Avatar");
	}

	private static string GetDisplayName(string name, string userName, bool isMyself)
	{
		if (isMyself)
		{
			return Utils.GetString("Me");
		}
		if (!string.IsNullOrEmpty(name))
		{
			return name;
		}
		return userName;
	}
}
