using System.Collections.Generic;
using System.Windows;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Config;

public class SubscribeTypeItemViewModel : BaseViewModel
{
	public const string Google = "Google";

	public const string Outlook = "Outlook";

	public const string Exchange = "Exchange";

	public const string iCloud = "iCloud";

	public const string WXWork = "WXWork";

	public const string DingTalk = "DingTalk";

	public const string FeiShu = "FeiShu";

	public const string CalDAV = "CalDAV";

	public const string Holiday = "Holiday";

	public const string URL = "URL";

	public const string WeChat = "WeChat";

	public const string Notion = "Notion";

	public const string TickCalendar = "TickCalendar";

	public const string EmailHelper = "EmailHelper";

	public const string Zapier = "Zapier";

	public const string IFTTT = "IFTTT";

	public const string Gmail = "Gmail";

	public const string Spark = "Spark";

	public const string OutlookEmail = "OutlookEmail";

	public const string Shortcut = "Shortcuts";

	public const string Alexa = "Alexa";

	public const string Chatmind = "Chatmind";

	public const string Slack = "Slack";

	public const string Telegram = "Telegram";

	public static List<string> RecommendTypes = new List<string> { "WeChat", "Google", "Notion" };

	public static List<string> SubscribeTypes = new List<string> { "Google", "Outlook", "Exchange", "iCloud", "WXWork", "DingTalk", "FeiShu", "CalDAV", "Holiday", "URL" };

	public static List<string> IntegrationTypes = new List<string> { "Google", "Notion" };

	public static List<string> OtherTypes = new List<string> { "TickCalendar", "EmailHelper" };

	public static List<string> EmailTypes = new List<string> { "Gmail", "Spark", "OutlookEmail" };

	public bool IsSubscribed { get; set; }

	public string Value { get; set; }

	public string ImagePath { get; set; }

	public string Name { get; set; }

	public string Desc { get; set; }

	public Visibility ShowDesc { get; set; } = Visibility.Collapsed;

	public Visibility ShowTips { get; set; } = Visibility.Collapsed;

	public TextWrapping DescWrap { get; set; } = TextWrapping.Wrap;

	public bool ShowExpired { get; set; }

	public SubscribeTypeItemViewModel()
	{
	}

	public SubscribeTypeItemViewModel(string type, bool isSubscribed = false)
	{
		IsSubscribed = isSubscribed;
		Value = type;
		Name = type;
		string text = "../../Assets/ImageSource";
		if (type == null)
		{
			return;
		}
		switch (type.Length)
		{
		case 6:
			switch (type[4])
			{
			case 'l':
				if (type == "Google")
				{
					Name = Utils.GetString("GoogleCalendar");
					ImagePath = text + "/googleCal_45.png";
				}
				break;
			case 'u':
				if (type == "iCloud")
				{
					Name = Utils.GetString("iCloudCalendar");
					ImagePath = text + "/iCloud_45.png";
				}
				break;
			case 'A':
				if (type == "CalDAV")
				{
					ImagePath = text + "/calDAV_45.png";
				}
				break;
			case 'a':
				if (type == "WeChat")
				{
					Name = Utils.GetString("Wechat");
					ImagePath = text + "/wechat_45.png";
				}
				break;
			case 'r':
				if (type == "WXWork")
				{
					Name = Utils.GetString("WeComCalendar");
					ImagePath = text + "/wxwork_45.png";
				}
				break;
			case 'h':
				if (type == "FeiShu")
				{
					Name = Utils.GetString("FeishuCalendar");
					ImagePath = text + "/feishu_45.png";
				}
				break;
			case 'o':
				if (type == "Notion")
				{
					ImagePath = text + "/notion_45.png";
				}
				break;
			case 'e':
				if (type == "Zapier")
				{
					ImagePath = text + "/zapier_45.png";
				}
				break;
			}
			break;
		case 7:
			switch (type[0])
			{
			case 'O':
				if (type == "Outlook")
				{
					Name = Utils.GetString("OutlookCalendar");
					ImagePath = text + "/outlook_45.png";
				}
				break;
			case 'H':
				if (type == "Holiday")
				{
					Name = Utils.GetString("holidays");
					ImagePath = text + "/holidayUrl_45.png";
				}
				break;
			}
			break;
		case 8:
			switch (type[0])
			{
			case 'E':
				if (type == "Exchange")
				{
					Name = Utils.GetString("ExchangeCalendar");
					ImagePath = text + "/exchange_45.png";
				}
				break;
			case 'D':
				if (type == "DingTalk")
				{
					Name = Utils.GetString("DingTalkCalendar");
					ImagePath = text + "/dindin_45.png";
				}
				break;
			case 'C':
				if (type == "Chatmind")
				{
					ImagePath = text + "/chatmind_45.png";
				}
				break;
			case 'T':
				if (type == "Telegram")
				{
					ImagePath = text + "/telegram_45.png";
				}
				break;
			}
			break;
		case 12:
			switch (type[0])
			{
			case 'T':
				if (type == "TickCalendar")
				{
					Name = string.Format(Utils.GetString("SubscribeTickTick"), Utils.GetAppName());
					Desc = string.Format(Utils.GetString("SubscribeTickTickDesc"), Utils.GetAppName());
					ShowDesc = Visibility.Visible;
					ImagePath = text + "/tickCal_45.png";
				}
				break;
			case 'O':
				if (type == "OutlookEmail")
				{
					Name = Utils.GetString("OutlookEmail");
					ImagePath = text + "/outlook_45.png";
				}
				break;
			}
			break;
		case 5:
			switch (type[3])
			{
			case 'T':
				if (type == "IFTTT")
				{
					ImagePath = text + "/ifttt_45.png";
				}
				break;
			case 'i':
				if (type == "Gmail")
				{
					ImagePath = text + "/gmail_45.png";
				}
				break;
			case 'r':
				if (type == "Spark")
				{
					ImagePath = text + "/spark_45.png";
				}
				break;
			case 'x':
				if (type == "Alexa")
				{
					Name = "Amazon Alexa";
					ImagePath = text + "/alexa_45.png";
				}
				break;
			case 'c':
				if (type == "Slack")
				{
					ImagePath = text + "/slack_45.png";
				}
				break;
			}
			break;
		case 3:
			if (type == "URL")
			{
				ImagePath = text + "/url_45.png";
				ShowTips = Visibility.Visible;
				Desc = Utils.GetString("UrlSubscribeSummary");
			}
			break;
		case 11:
			if (type == "EmailHelper")
			{
				Name = Utils.GetString("AddTasksViaEmail");
				Desc = string.Format(Utils.GetString("EmailCreatTaskDesc"), Utils.GetAppName());
				ShowDesc = Visibility.Visible;
				ImagePath = text + "/emailTask_45.png";
			}
			break;
		case 9:
			if (type == "Shortcuts")
			{
				ImagePath = text + "/shortcut_45.png";
			}
			break;
		case 4:
		case 10:
			break;
		}
	}

	public static List<SubscribeTypeItemViewModel> GetRecommendItems()
	{
		List<SubscribeTypeItemViewModel> list = new List<SubscribeTypeItemViewModel>();
		foreach (string recommendType in RecommendTypes)
		{
			list.Add(new SubscribeTypeItemViewModel(recommendType));
		}
		return list;
	}

	public static List<SubscribeTypeItemViewModel> GetSubscribeItems()
	{
		List<SubscribeTypeItemViewModel> list = new List<SubscribeTypeItemViewModel>();
		foreach (string subscribeType in SubscribeTypes)
		{
			if (!(subscribeType == "Holiday") || !Utils.IsWindows7())
			{
				list.Add(new SubscribeTypeItemViewModel(subscribeType));
			}
		}
		return list;
	}

	public static List<SubscribeTypeItemViewModel> GetIntegrationItems()
	{
		List<SubscribeTypeItemViewModel> list = new List<SubscribeTypeItemViewModel>();
		foreach (string integrationType in IntegrationTypes)
		{
			list.Add(new SubscribeTypeItemViewModel(integrationType));
		}
		return list;
	}

	public static List<SubscribeTypeItemViewModel> GetEmailItems()
	{
		List<SubscribeTypeItemViewModel> list = new List<SubscribeTypeItemViewModel>();
		foreach (string emailType in EmailTypes)
		{
			list.Add(new SubscribeTypeItemViewModel(emailType));
		}
		return list;
	}

	public static List<SubscribeTypeItemViewModel> GetOtherItems()
	{
		List<SubscribeTypeItemViewModel> list = new List<SubscribeTypeItemViewModel>();
		foreach (string otherType in OtherTypes)
		{
			list.Add(new SubscribeTypeItemViewModel(otherType));
		}
		return list;
	}
}
