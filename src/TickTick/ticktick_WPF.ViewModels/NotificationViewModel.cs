using System;
using System.Windows.Media.Imaging;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.ViewModels;

public class NotificationViewModel : BaseViewModel
{
	private string acceptButtonContent = Utils.GetString("Accept");

	public int actionStatus { get; set; }

	public string type { get; set; }

	public string teamId { get; set; }

	public string teamName { get; set; }

	private string userDisplayName { get; set; }

	private string title { get; set; }

	private string avatarUrl { get; set; }

	private DateTime createdTime { get; set; }

	private string actionText { get; set; }

	private string actionVisibility { get; set; }

	private string buttonVisibility { get; set; }

	private string acceptButtonVisibility { get; set; }

	private string declineButtonVisibility { get; set; }

	private string isButtonEnable { get; set; }

	public string userCode { get; set; }

	public string projectid { get; set; }

	public string id { get; set; }

	public string url { get; set; }

	public NotificationModel notification { get; set; }

	public string UserDisplayName
	{
		get
		{
			return userDisplayName;
		}
		set
		{
			userDisplayName = value;
			OnPropertyChanged("UserDisplayName");
		}
	}

	public string Title
	{
		get
		{
			return title;
		}
		set
		{
			title = value;
			OnPropertyChanged("Title");
		}
	}

	public string AvatarUrl
	{
		get
		{
			if (!string.IsNullOrEmpty(avatarUrl) && !(avatarUrl == "-1"))
			{
				return avatarUrl;
			}
			return "avatar-new.png";
		}
		set
		{
			avatarUrl = value;
			SetAvatar();
		}
	}

	public BitmapImage Avatar { get; set; }

	public DateTime CreatedTime
	{
		get
		{
			return createdTime;
		}
		set
		{
			createdTime = value;
			OnPropertyChanged("CreatedTime");
		}
	}

	public string ActionText
	{
		get
		{
			return actionText;
		}
		set
		{
			actionText = value;
			OnPropertyChanged("ActionText");
		}
	}

	public string ActionVisibility
	{
		get
		{
			if (actionVisibility == null)
			{
				return "Collapsed";
			}
			return actionVisibility;
		}
		set
		{
			actionVisibility = value;
			OnPropertyChanged("ActionVisibility");
		}
	}

	public string ButtonVisibility
	{
		get
		{
			if (buttonVisibility == null)
			{
				return "Collapsed";
			}
			return buttonVisibility;
		}
		set
		{
			buttonVisibility = value;
			OnPropertyChanged("ButtonVisibility");
		}
	}

	public string AcceptButtonVisibility
	{
		get
		{
			if (acceptButtonVisibility == null)
			{
				return "Collapsed";
			}
			return acceptButtonVisibility;
		}
		set
		{
			acceptButtonVisibility = value;
			OnPropertyChanged("AcceptButtonVisibility");
		}
	}

	public string DeclineButtonVisibility
	{
		get
		{
			if (declineButtonVisibility == null)
			{
				return "Collapsed";
			}
			return declineButtonVisibility;
		}
		set
		{
			declineButtonVisibility = value;
			OnPropertyChanged("DeclineButtonVisibility");
		}
	}

	public string AcceptButtonContent
	{
		get
		{
			return acceptButtonContent;
		}
		set
		{
			acceptButtonContent = value;
			OnPropertyChanged("AcceptButtonContent");
		}
	}

	public string IsButtonEnable
	{
		get
		{
			if (isButtonEnable == null)
			{
				return "True";
			}
			return isButtonEnable;
		}
		set
		{
			isButtonEnable = value;
			OnPropertyChanged("IsButtonEnable");
		}
	}

	public string Url
	{
		get
		{
			return url;
		}
		set
		{
			url = value;
			OnPropertyChanged("Url");
		}
	}

	public NotificationViewModel(NotificationModel notification)
	{
		id = notification.id;
		teamId = notification.notificationUserData.teamId;
		teamName = notification.notificationUserData.teamName;
		actionStatus = notification.actionStatus;
		type = notification.type;
		projectid = notification.notificationUserData.entityId;
		CreatedTime = notification.createdTime;
		userCode = notification.notificationUserData.userCode ?? notification.notificationUserData.fromUserCode;
		GetActionStatus(notification);
		UserDisplayName = ((userCode == null) ? Utils.GetAppName() : (notification.notificationUserData.userDisplayName ?? notification.notificationUserData.name ?? notification.notificationUserData.fromUserDisplayName));
		this.notification = notification;
		Title = notification.title;
	}

	private async void SetAvatar()
	{
		Avatar = await AvatarHelper.GetAvatarByUrlAsync(AvatarUrl);
		OnPropertyChanged("Avatar");
	}

	private void GetActionStatus(NotificationModel notification)
	{
		switch (notification.type)
		{
		case "attend":
		case "task":
			SetVisible(0);
			break;
		case "team":
			switch (notification.actionStatus)
			{
			case 1:
			case 20:
			case 21:
				SetVisible(1);
				break;
			case 15:
			case 22:
				SetVisible(2);
				break;
			case 16:
			case 23:
				SetVisible(3);
				break;
			default:
				SetVisible(0);
				break;
			}
			break;
		case "projectPermission":
			switch (notification.actionStatus)
			{
			case 1:
				SetVisible(1);
				AcceptButtonContent = Utils.GetString("Agree");
				break;
			case 4:
				SetVisible(2);
				break;
			case 5:
				SetVisible(3);
				break;
			default:
				SetVisible(0);
				break;
			}
			break;
		case "share":
			switch (notification.actionStatus)
			{
			case 0:
			case 9:
				SetVisible(1);
				break;
			case 1:
			case 10:
				SetVisible(2);
				break;
			case 2:
			case 11:
				SetVisible(3);
				break;
			case 3:
				SetVisible(4);
				break;
			default:
				SetVisible(0);
				break;
			}
			break;
		case "PayReminder":
		{
			int num = notification.actionStatus;
			if (num == 0 || num == 9)
			{
				actionVisibility = "Collapsed";
				ButtonVisibility = "Visible";
				AcceptButtonVisibility = "Visible";
				DeclineButtonVisibility = "Collapsed";
				if (!notification.notificationUserData.expiryDate.HasValue || notification.notificationUserData.expiryDate.Value.Year == 1)
				{
					AcceptButtonContent = Utils.GetString("UpdateNow");
				}
				else
				{
					AcceptButtonContent = Utils.GetString("RenewNow");
				}
			}
			else
			{
				SetVisible(0);
			}
			break;
		}
		default:
			SetVisible(0);
			break;
		}
	}

	private void SetVisible(int num)
	{
		switch (num)
		{
		case 0:
			actionVisibility = "Collapsed";
			ButtonVisibility = "Collapsed";
			AcceptButtonVisibility = "Collapsed";
			DeclineButtonVisibility = "Collapsed";
			break;
		case 1:
			actionVisibility = "Collapsed";
			ButtonVisibility = "Visible";
			AcceptButtonVisibility = "Visible";
			DeclineButtonVisibility = "Visible";
			break;
		case 2:
			actionVisibility = "Visible";
			ButtonVisibility = "Collapsed";
			AcceptButtonVisibility = "Collapsed";
			DeclineButtonVisibility = "Collapsed";
			actionText = Utils.GetString("Accepted");
			break;
		case 3:
			actionVisibility = "Visible";
			ButtonVisibility = "Collapsed";
			AcceptButtonVisibility = "Collapsed";
			DeclineButtonVisibility = "Collapsed";
			actionText = Utils.GetString("Declined");
			break;
		case 4:
			actionVisibility = "Visible";
			ButtonVisibility = "Collapsed";
			AcceptButtonVisibility = "Collapsed";
			DeclineButtonVisibility = "Collapsed";
			actionText = Utils.GetString("Cancelled");
			break;
		}
	}
}
