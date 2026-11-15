using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Timers;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;

namespace ticktick_WPF.Util.Sync;

public static class WebSocketService
{
	private const int AutoSendPingInterval = 30;

	private const int OpenTimeout = 10000;

	private static int _reconnectCount;

	private static string PushUrl = "wss://wss.dida365.com/pc";

	private static string FocusPushUrl = "wss://wssp.dida365.com/pc";

	private static SyncWebSocket _webSocket;

	private static FocusWebSocket _focusSocket;

	private static readonly Timer SyncTimer = new Timer();

	public static async Task InitAsync()
	{
		string apiDomain = BaseUrl.GetApiDomain(getDefault: true);
		PushUrl = apiDomain.Replace("https", "wss").Replace("api", "wss") + "/pc";
		if (apiDomain.Contains("-"))
		{
			FocusPushUrl = apiDomain.Replace("https", "wss").Replace("api", "wss-pomodoro") + "/pc";
		}
		SyncTimer.Interval = 600000.0;
		SyncTimer.Elapsed += delegate
		{
			UtilRun.Watch("websocket.SyncTimer() watch sync", delegate
			{
				Utils.RunOnUiThread(App.Window.Dispatcher, delegate
				{
					HabitCheckinCache.Reset();
					SyncManager.Sync();
					ReminderCalculator.AssembleReminders();
				});
			}, ignoreException: true);
		};
		SyncTimer.Start();
		_webSocket = new SyncWebSocket(PushUrl);
		_focusSocket = new FocusWebSocket(FocusPushUrl);
		_webSocket.InitAsync();
		_focusSocket.InitAsync();
		NetworkChange.NetworkAvailabilityChanged += delegate(object sender, NetworkAvailabilityEventArgs args)
		{
			bool flag = args?.IsAvailable ?? false;
			UtilLog.Info("WebSocketService NetworkAvailabilityChanged.IsAvailable=" + flag);
			if (flag)
			{
				_webSocket.SendHello();
				_focusSocket.SendHello();
			}
		};
	}

	public static void DisposeWs()
	{
		SyncTimer.Stop();
		_webSocket.DisposeWs();
		_focusSocket.DisposeWs();
	}

	public static void CheckFocusSocket()
	{
		_focusSocket?.CheckConnected();
	}
}
