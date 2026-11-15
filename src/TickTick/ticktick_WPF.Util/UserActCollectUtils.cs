using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Util;

public static class UserActCollectUtils
{
	public static void StartPost() { }

	public static void PostEventNow() { }

	public static Task OnDeviceDataChanged() { return Task.CompletedTask; }

	public static async void AddSortOptionEvent(string type, string ctype, string groupBy, string orderBy) { }

	public static async void SendUpgradePurchaseSuccessData(string from, string productId) { }

	public static async void SendUpgradePurchaseData(string from, string productId) { }

	public static async void SendUpgradeShowData(string from) { }

	public static async void SendUpgradePromptData(string from) { }

	public static Task SendUserDataWithGroup(string ctype, string label, string group) { return Task.CompletedTask; }

	public static async void AddTaskLabelEvent(string type, string ctype, string label, long time) { }

	public static async void AddClickEventWithData(string type, string ctype, JObject data) { }

	public static async void AddClickEvent(string type, string ctype, string data) { }

	public static Task SendCustomException(string custom) { return Task.CompletedTask; }

	public static Task SendException(Exception e, ExceptionType type) { return Task.CompletedTask; }

	public static void OnAppExit() { }

	public static void AddShortCutEvent(string ctype, string label) { }
}
