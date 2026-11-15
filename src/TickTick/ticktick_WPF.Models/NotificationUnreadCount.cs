using Newtonsoft.Json;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Models;

public class NotificationUnreadCount : BaseViewModel
{
	private int _activity;

	private int _notification;

	[JsonProperty(PropertyName = "activity")]
	public int Activity
	{
		get
		{
			return _activity;
		}
		set
		{
			_activity = value;
			OnPropertyChanged("Activity");
		}
	}

	[JsonProperty(PropertyName = "notification")]
	public int Notification
	{
		get
		{
			return _notification;
		}
		set
		{
			_notification = value;
			OnPropertyChanged("Notification");
		}
	}

	public NotificationUnreadCount()
	{
	}

	public NotificationUnreadCount(int activity, int notification)
	{
		Activity = activity;
		Notification = notification;
	}
}
