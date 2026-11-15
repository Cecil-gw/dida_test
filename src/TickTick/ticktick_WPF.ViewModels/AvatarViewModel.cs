using System;

namespace ticktick_WPF.ViewModels;

public class AvatarViewModel : BaseViewModel
{
	public string AvatarUrl { get; set; }

	public string Name { get; set; }

	public string UserId { get; set; }

	public string UserCode { get; set; }

	public bool IsNoAvatar { get; set; }

	public DateTime Date { get; set; }
}
