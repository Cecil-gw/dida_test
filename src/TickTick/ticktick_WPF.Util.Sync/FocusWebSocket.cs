using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TickTickModels;
using TickTickUtils.Lang;
using WebSocket4Net;
using ticktick_WPF.Dal;
using ticktick_WPF.Resource;
using ticktick_WPF.Views.Pomo;

namespace ticktick_WPF.Util.Sync;

public class FocusWebSocket : TickWebSocket
{
	private class WSFocusModel
	{
		public string type { get; set; }

		public FocusOptionModel data { get; set; }

		public long ctime { get; set; }
	}

	public FocusWebSocket(string pushUrl)
		: base(pushUrl)
	{
	}

	protected override bool IsConnectId(string msg)
	{
		return msg.StartsWith("{\"wsId\"");
	}

	protected override async Task Register(string msg)
	{
		int num = 0;
		if (num == 0)
		{
			TaskAwaiter taskAwaiter2 = default(TaskAwaiter);
			TaskAwaiter taskAwaiter = taskAwaiter2;
			taskAwaiter.GetResult();
		}
	}

	protected override List<KeyValuePair<string, string>> GetHeaders()
	{
		return new List<KeyValuePair<string, string>>
		{
			new KeyValuePair<string, string>("x-device", Utils.GetDeviceInfo()),
			new KeyValuePair<string, string>("x-timestamp", Utils.GetXTimeStamp()),
			new KeyValuePair<string, string>("hl", Utils.GetLanguage()),
			new KeyValuePair<string, string>("Authorization", "OAuth " + LocalSettings.Settings.LoginUserAuth)
		};
	}

	protected override async Task HandleMessage(string message)
	{
		try
		{
			WSFocusModel wSFocusModel = JsonConvert.DeserializeObject<WSFocusModel>(message);
			if (wSFocusModel?.data != null && !Utils.IsEmptyDate(wSFocusModel.data.time))
			{
				TickFocusManager.HandleRemoteOption(wSFocusModel.data);
			}
		}
		catch (Exception)
		{
		}
	}

	public override void SendHello()
	{
		SendTextAsync("{\"type\":\"ping\"}").ContinueWith(UtilRun.LogTask);
	}

	public void CheckConnected()
	{
		if (!_stopped)
		{
			WebSocket webSocket = _webSocket;
			if ((webSocket == null || webSocket.State != WebSocketState.Open) && UserDao.IsPro() && LocalSettings.Settings.EnableFocus && LocalSettings.Settings.FocusKeepInSync)
			{
				SendHello();
			}
		}
	}
}
