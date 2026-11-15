using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Upgrade;

internal class ManageProSubscribeViewModel
{
	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand<ProSubscribeItem>? viewCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand<ProSubscribeItem>? cancelCommand;

	public Action OnClose { get; set; }

	public bool IsShowDate => Items.Count > 1;

	public ObservableCollection<ProSubscribeItem> Items { get; set; } = new ObservableCollection<ProSubscribeItem>();

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<ProSubscribeItem> ViewCommand => viewCommand ?? (viewCommand = new AsyncRelayCommand<ProSubscribeItem>(View));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<ProSubscribeItem> CancelCommand => cancelCommand ?? (cancelCommand = new AsyncRelayCommand<ProSubscribeItem>(Cancel));

	public ManageProSubscribeViewModel(List<SubscribeInfoModel> list)
	{
		LoadData(list);
	}

	public async Task LoadData(List<SubscribeInfoModel> list)
	{
		Items.Clear();
		string text;
		if (list != null)
		{
			foreach (SubscribeInfoModel item in list)
			{
				ProSubscribeItem proSubscribeItem = new ProSubscribeItem
				{
					Id = item.id,
					Paid = item.paid,
					Price = item.price,
					RenewDate = DateUtils.FormatYearMonthDay(item.proEndDate, DateFormatLen.Long),
					Freq = item.freq,
					RawSubsType = item.subscribeType
				};
				text = item.freq.ToLower();
				if (!(text == "month"))
				{
					if (text == "year")
					{
						proSubscribeItem.Plan = Utils.GetString("PremiumYearly");
					}
				}
				else
				{
					proSubscribeItem.Plan = Utils.GetString("PremiumMonthly");
				}
				proSubscribeItem.PayMethod = PremiumUtils.GetSubscribeTypeText(item.subscribeType);
				Items.Add(proSubscribeItem);
			}
			return;
		}
		UserModel userModel = await UserManager.GetCurrentUser(force: true);
		ProSubscribeItem proSubscribeItem2 = new ProSubscribeItem
		{
			Paid = (userModel.errorType > 0),
			Freq = userModel.subscribeFreq,
			RawSubsType = userModel.subscribeType
		};
		text = userModel.subscribeFreq.ToLower();
		if (!(text == "month"))
		{
			if (text == "year")
			{
				proSubscribeItem2.Plan = Utils.GetString("PremiumYearly");
			}
		}
		else
		{
			proSubscribeItem2.Plan = Utils.GetString("PremiumMonthly");
		}
		proSubscribeItem2.PayMethod = PremiumUtils.GetSubscribeTypeText(userModel.subscribeType);
		Items.Add(proSubscribeItem2);
	}

	[RelayCommand]
	public async Task View(ProSubscribeItem item)
	{
		string text = string.Empty;
		switch (item.RawSubsType)
		{
		case "paypal_subscribe":
			text = "https://www.paypal.com";
			break;
		case "google":
			text = "https://myaccount.google.com";
			break;
		case "stripe_subscribe":
			text = "https://stripe.com";
			break;
		}
		if (!string.IsNullOrEmpty(text))
		{
			Utils.TryProcessStartUrl(text);
		}
	}

	[RelayCommand]
	public async Task Cancel(ProSubscribeItem item)
	{
		if (item.RawSubsType == "apple")
		{
			new CustomerDialog(Utils.GetString("CancelAppleSubsTitle"), Utils.GetString("CancelAppleSubsDesc"), MessageBoxButton.OK).ShowDialog();
			return;
		}
		string desc = string.Format(Utils.GetString("CancelPremiumDesc"), item.PayMethod);
		if (!string.IsNullOrEmpty(item.Id))
		{
			SubscribePriceInfoModel subscribePriceInfoModel = await Communicator.GetSubscribeInfo(item.Id);
			if (ShouldShowPriceChangeDesc(subscribePriceInfoModel, item))
			{
				string subscribeType = subscribePriceInfoModel.subscribeType ?? item.RawSubsType;
				bool flag = string.Equals(subscribePriceInfoModel.subscribeFreq ?? item.Freq, "year", StringComparison.OrdinalIgnoreCase);
				string arg = FormatSubscribePrice(subscribePriceInfoModel.oldPrice.Value, subscribePriceInfoModel.currency);
				if (IsGoogleSubscribe(subscribeType))
				{
					string key = (flag ? "CancelProSubsChangeGooglePriceYearDesc" : "CancelProSubsChangeGooglePriceMonthDesc");
					desc = string.Format(Utils.GetString(key), arg);
				}
				else
				{
					string key2 = (flag ? "CancelProSubsChangePriceYearDesc" : "CancelProSubsChangePriceMonthDesc");
					string arg2 = FormatSubscribePrice(subscribePriceInfoModel.price.Value, subscribePriceInfoModel.currency);
					desc = string.Format(Utils.GetString(key2), arg, arg2);
				}
			}
		}
		if (new CustomerDialog(Utils.GetString("CancelPremiumTitle"), desc, Utils.GetString("CancelSubscription"), Utils.GetString("NotCancelNow")).ShowDialog() != true)
		{
			return;
		}
		if (!(await Communicator.CancelPaymentSubs(item.RawSubsType, item.Id)))
		{
			Utils.Toast(Utils.GetString("OperateFailed"));
			return;
		}
		new CustomerDialog(Utils.GetString("SubscriptionCanceledTitle"), Utils.GetString("SubscriptionCanceledDesc"), MessageBoxButton.OK).ShowDialog();
		List<SubscribeInfoModel> list = await Communicator.GetSubscribeList();
		if (list != null && list.Any())
		{
			LoadData(list);
		}
		else
		{
			OnClose?.Invoke();
		}
		DataChangedNotifier.NotifyPaymentUpdated();
	}

	private static bool ShouldShowPriceChangeDesc(SubscribePriceInfoModel info, ProSubscribeItem item)
	{
		if (info == null || !info.isPro || info.needSubscribe || !info.oldPrice.HasValue)
		{
			return false;
		}
		string text = info.subscribeType ?? item?.RawSubsType;
		if (string.Equals(text, "apple", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (IsGoogleSubscribe(text))
		{
			if (info.price.HasValue)
			{
				return info.oldPrice.Value != info.price.Value;
			}
			return true;
		}
		if (info.price.HasValue)
		{
			return info.oldPrice.Value != info.price.Value;
		}
		return false;
	}

	private static bool IsGoogleSubscribe(string subscribeType)
	{
		return string.Equals(subscribeType, "google", StringComparison.OrdinalIgnoreCase);
	}

	private static string FormatSubscribePrice(decimal price, string currency)
	{
		return GetCurrencySymbol(currency) + price.ToString("0.##", CultureInfo.CurrentCulture);
	}

	private static string GetCurrencySymbol(string currency)
	{
		string text = currency?.Trim();
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}
		switch (text)
		{
		case "$":
		case "￥":
		case "¥":
		case "€":
		case "£":
		case "₩":
			return text;
		default:
			return text.ToUpperInvariant() switch
			{
				"CNY" => "￥", 
				"USD" => "$", 
				"EUR" => "€", 
				"GBP" => "£", 
				"JPY" => "¥", 
				"KRW" => "₩", 
				"HKD" => "HK$", 
				"TWD" => "NT$", 
				_ => text.ToUpperInvariant() + " ", 
			};
		}
	}
}
