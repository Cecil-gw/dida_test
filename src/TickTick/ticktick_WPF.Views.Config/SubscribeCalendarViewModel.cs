using System.Windows;
using TickTickModels;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Config;

public class SubscribeCalendarViewModel : BaseViewModel
{
	private bool _isExpried;

	public readonly BindCalendarAccountModel Account;

	public string CalendarId { get; set; }

	public bool IsConnect { get; set; }

	public bool ShowConnect
	{
		get
		{
			if (IsConnect)
			{
				return Site != "notion";
			}
			return false;
		}
	}

	public string Kind { get; set; }

	public string Site { get; set; }

	public Visibility ImageBorderVisible { get; set; }

	public bool ReminderEnable { get; set; }

	public string ImageUrl => Kind switch
	{
		"api" => Site switch
		{
			"outlook" => "../../Assets/ImageSource/outlook_45.png", 
			"google" => "../../Assets/ImageSource/googleCal_45.png", 
			"notion" => "../../Assets/ImageSource/notion_45.png", 
			_ => "../../Assets/ImageSource/url_45.png", 
		}, 
		"url" => "../../Assets/ImageSource/url_45.png", 
		"holiday" => "../../Assets/ImageSource/holidayUrl_45.png", 
		"caldav" => "../../Assets/ImageSource/calDAV_45.png", 
		"exchange" => "../../Assets/ImageSource/exchange_45.png", 
		"icloud" => "../../Assets/ImageSource/iCloud_45.png", 
		_ => null, 
	};

	public SubscribeCalendarType SubType { get; set; }

	public string Title { get; set; }

	public string Color { get; set; }

	public string Content { get; set; }

	public CalendarSubsShowType ShowStatus { get; set; }

	public string Tips { get; set; }

	public bool ShowTips { get; set; }

	public bool IsExpired
	{
		get
		{
			return _isExpried;
		}
		set
		{
			_isExpried = value;
			OnPropertyChanged("IsExpired");
		}
	}

	public long SortOrder { get; set; }

	public SubscribeCalendarViewModel(BindCalendarAccountModel account)
	{
		Account = account;
		CalendarId = account.Id;
		SubType = SubscribeCalendarType.Account;
		if (Account.Site == "feishu")
		{
			Title = Utils.GetString("FeishuCalendar");
		}
		else
		{
			Title = account.DisplayName;
			Content = ((!string.IsNullOrEmpty(account.Description)) ? account.Description : Content);
		}
		Kind = account.Kind;
		Site = account.Site;
		IsExpired = account.Expired;
		SortOrder = account.Order.GetValueOrDefault();
	}

	public SubscribeCalendarViewModel(CalendarSubscribeProfileModel profile)
	{
		CalendarId = profile.Id;
		SubType = SubscribeCalendarType.Url;
		Title = profile.Name;
		Color = profile.Color;
		Content = profile.Url;
		_isExpried = profile.Expired;
		ShowStatus = profile.ShowType;
		Kind = ((profile.Kind == 2) ? "holiday" : "url");
		ReminderEnable = profile.ReminderEnable == true;
		SortOrder = profile.Order.GetValueOrDefault();
	}

	public SubscribeCalendarViewModel(CalendarConnectInfo connect)
	{
		CalendarId = connect.id;
		IsExpired = connect.status == 1;
		SubType = SubscribeCalendarType.Account;
		IsConnect = true;
		Title = connect.account + ((connect.site == "notion") ? (" (" + connect.workspaceName + ")") : null);
		Kind = "api";
		Site = connect.site;
		if (connect.site == "notion")
		{
			ImageBorderVisible = Visibility.Collapsed;
		}
	}

	public void NavigateNotionUrl()
	{
		Utils.TryProcessToNotionConnect(CalendarId);
	}
}
