using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using SuperSocket.ClientEngine;
using TickTickUtils.Lang;
using WebSocket4Net;
using ticktick_WPF.Dal;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Util.Sync;

public class TickWebSocket
{
	private const int AutoSendPingInterval = 540;

	private const int OpenTimeout = 10000;

	protected WebSocket _webSocket;

	private readonly System.Timers.Timer HeartTimer = new System.Timers.Timer();

	private readonly SemaphoreSlim Locker = new SemaphoreSlim(1);

	protected volatile bool _stopped;

	private string _pushUrl;

	public TickWebSocket(string pushUrl)
	{
		_pushUrl = pushUrl;
		HeartTimer.Interval = 55000.0;
		HeartTimer.Elapsed += delegate
		{
			SendHello();
		};
	}

	public async Task InitAsync()
	{
		UtilLog.Info(GetType().Name + ".InitAsync()");
		_stopped = false;
		HeartTimer.Start();
		await OpenAsync();
	}

	public virtual void SendHello()
	{
		SendTextAsync("hello").ContinueWith(UtilRun.LogTask);
	}

	public async Task SendTextAsync(string text)
	{
		if (text != null)
		{
			(await OpenAsync())?.Send(text);
		}
	}

	private async Task<WebSocket> OpenAsync()
	{
		if (this is FocusWebSocket && (!UserDao.IsPro() || !LocalSettings.Settings.EnableFocus || !LocalSettings.Settings.FocusKeepInSync))
		{
			return null;
		}
		Stopwatch watch = Stopwatch.StartNew();
		await Locker.WaitAsync();
		try
		{
			WebSocket webSocket = _webSocket;
			if (!_stopped && (webSocket == null || webSocket.State != WebSocketState.Open))
			{
				long waitTime = watch.ElapsedMilliseconds;
				if (webSocket != null)
				{
					UtilLog.Info(GetType().Name + ".OpenAsync() dispose ws: " + webSocket?.ToString() + ", stopped: " + _stopped);
					UtilRun.Dispose(webSocket);
					_webSocket = null;
				}
				webSocket = (_webSocket = await Task.Run((Func<WebSocket>)CreateOpenedWebSocket));
				UtilLog.Info(GetType().Name + ".OpenAsync() elapsed: " + watch.Elapsed.ToString() + "(wait: " + waitTime + "ms, open: " + (watch.ElapsedMilliseconds - waitTime) + "ms) Opened ws: " + webSocket?.ToString() + ", stopped: " + _stopped);
			}
			if (_stopped)
			{
				UtilRun.Dispose(_webSocket);
				_webSocket = null;
				throw new Exception(GetType().Name + " is stopped");
			}
			return webSocket;
		}
		catch (Exception ex)
		{
			watch.Stop();
			UtilLog.Debug(GetType().Name + ".OpenAsync() elapsed: " + watch.Elapsed.ToString() + ", ex: " + ex?.ToString() + ", stopped: " + _stopped);
			UtilLog.Warn(GetType().Name + ".OpenAsync() Error: " + ex.Message);
			return null;
		}
		finally
		{
			Locker.Release();
		}
	}

	private WebSocket CreateOpenedWebSocket()
	{
		List<KeyValuePair<string, string>> headers = GetHeaders();
		WebSocket ws = new WebSocket(_pushUrl, "", null, headers, "", "", WebSocketVersion.None, null, SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12);
		ws.EnableAutoSendPing = true;
		ws.AutoSendPingInterval = 540;
		ws.Security.EnabledSslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
		ws.MessageReceived += OnMessageReceived;
		Exception cause = null;
		CountdownEvent openedLocker;
		try
		{
			openedLocker = new CountdownEvent(1);
			try
			{
				ws.Error += OnOpenError;
				ws.Opened += OnOpened;
				ws.Open();
				openedLocker.Wait(10000);
				if (ws.State == WebSocketState.Open)
				{
					return ws;
				}
			}
			finally
			{
				if (openedLocker != null)
				{
					((IDisposable)openedLocker).Dispose();
				}
			}
			if (cause == null)
			{
				cause = new Exception(GetType().Name + ".open error timeout: " + 10000 + "ms");
			}
		}
		catch (Exception ex)
		{
			cause = ex;
		}
		UtilRun.Dispose(ws);
		throw cause ?? new Exception();
		void OnOpenError(object sender, ErrorEventArgs args)
		{
			WebSocket webSocket = (WebSocket)sender;
			webSocket.Error -= OnOpenError;
			UtilLog.Error(GetType().Name + ".CreateOpenedWebSocket()  OpenError ws: " + webSocket?.ToString() + ", state: " + (webSocket?.State).ToString() + ", ex: " + args.Exception);
			cause = args.Exception;
			openedLocker.Signal();
		}
		void OnOpened(object sender, EventArgs e)
		{
			UtilLog.Info(GetType().Name + ".CreateOpenedWebSocket() Opened ws:" + sender?.ToString() + ", event: " + e);
			ws.Error -= OnOpenError;
			ws.Opened -= OnOpened;
			ws.Error += OnError;
			ws.Closed += OnClosed;
			openedLocker.Signal();
		}
	}

	protected virtual List<KeyValuePair<string, string>> GetHeaders()
	{
		return null;
	}

	private async void OnMessageReceived(object sender, MessageReceivedEventArgs e)
	{
		_ = 1;
		try
		{
			string message = e.Message;
			if (message != null && !message.Contains("pong"))
			{
				UtilLog.Info(GetType().Name + ".OnMessageReceived(): " + message);
			}
			if (!string.IsNullOrEmpty(e.Message) && !_stopped)
			{
				if (IsConnectId(message))
				{
					await Register(message);
				}
				else
				{
					await HandleMessage(message);
				}
			}
		}
		catch (Exception ex)
		{
			UtilLog.Warn(GetType().Name + ".OnMessageReceived() ex: " + ex);
		}
	}

	protected virtual bool IsConnectId(string msg)
	{
		return false;
	}

	protected virtual async Task Register(string msg)
	{
	}

	private void OnError(object sender, ErrorEventArgs error)
	{
		WebSocket webSocket = (WebSocket)sender;
		UtilLog.Error(GetType().Name + ".OnError() ws: " + webSocket?.ToString() + ", state: " + (webSocket?.State).ToString() + ", ex: " + error.Exception);
	}

	private void OnClosed(object sender, EventArgs e)
	{
		WebSocket webSocket = sender as WebSocket;
		if (webSocket != null)
		{
			webSocket.Error -= OnError;
			webSocket.Closed -= OnClosed;
		}
		UtilLog.Info(GetType().Name + ".OnClosed() ws: " + webSocket);
	}

	protected virtual async Task HandleMessage(string message)
	{
	}

	public void DisposeWs()
	{
		_stopped = true;
		HeartTimer.Stop();
		WebSocket webSocket = _webSocket;
		UtilLog.Info(GetType().Name + ".DisposeWs() dispose ws: " + webSocket?.ToString() + ", stopped: " + _stopped);
		if (webSocket != null)
		{
			_webSocket = null;
			UtilRun.Dispose(webSocket);
		}
	}
}
