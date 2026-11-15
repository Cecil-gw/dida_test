using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TickTickUtils;
using TickTickUtils.Lang;

namespace ticktick_WPF.Views.AI;

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
[ComDefaultInterface(typeof(IAgentDsBridgeHost))]
public sealed class AgentDsBridgeHost : IAgentDsBridgeHost, IDisposable
{
	private sealed class PendingWebCall
	{
		public int CallbackId { get; }

		public string MethodName { get; }

		public object Payload { get; }

		public TaskCompletionSource<JToken> Completion { get; }

		public PendingWebCall(int callbackId, string methodName, object payload)
		{
			CallbackId = callbackId;
			MethodName = methodName;
			Payload = payload;
			Completion = new TaskCompletionSource<JToken>(TaskCreationOptions.RunContinuationsAsynchronously);
		}
	}

	private static readonly TimeSpan DefaultWebCallTimeout = TimeSpan.FromSeconds(15.0);

	private const string HasNativeMethod = "_dsb.hasNativeMethod";

	private const string Initialize = "_dsb.dsinit";

	private const string ReturnValue = "_dsb.returnValue";

	private const string DisableJavascriptDialogBlock = "_dsb.disableJavascriptDialogBlock";

	private readonly object _gate = new object();

	private readonly Func<string, Task<string>> _executeScriptAsync;

	private readonly TimeSpan _webCallTimeout;

	private readonly Dictionary<string, Func<JToken, object>> _nativeMethods = new Dictionary<string, Func<JToken, object>>(StringComparer.Ordinal);

	private readonly Dictionary<int, PendingWebCall> _pendingWebCalls = new Dictionary<int, PendingWebCall>();

	private readonly Queue<PendingWebCall> _startupQueue = new Queue<PendingWebCall>();

	private int _nextCallbackId;

	private bool _isDisposed;

	private bool _isWebReady;

	internal event EventHandler WebReady;

	internal AgentDsBridgeHost(Func<string, Task<string>> executeScriptAsync, TimeSpan? webCallTimeout = null)
	{
		_executeScriptAsync = executeScriptAsync ?? throw new ArgumentNullException("executeScriptAsync");
		_webCallTimeout = webCallTimeout ?? DefaultWebCallTimeout;
	}

	internal void RegisterNativeMethod(string methodName, Func<JToken, object> handler)
	{
		if (!AgentBridgeEvents.CanRegisterNativeMethod(methodName))
		{
			throw new ArgumentException("Unknown Agent Bridge native method.", "methodName");
		}
		if (handler == null)
		{
			throw new ArgumentNullException("handler");
		}
		lock (_gate)
		{
			ThrowIfDisposed();
			_nativeMethods[methodName] = handler;
		}
	}

	public string invoke(string methodName, string argumentsJson)
	{
		try
		{
			string text = (methodName ?? string.Empty).Trim();
			JToken jToken = NormalizePayload(ParseArguments(argumentsJson)["data"]);
			switch (text)
			{
			case "_dsb.hasNativeMethod":
				return CreateSuccessResponse(HasRegisteredNativeMethod(jToken));
			case "_dsb.dsinit":
				UtilLog.Info("AgentDsBridge received _dsb.dsinit.");
				MarkWebReady();
				return CreateSuccessResponse(null);
			case "_dsb.returnValue":
				AcceptWebReturnValue(jToken);
				return CreateSuccessResponse(null);
			case "_dsb.disableJavascriptDialogBlock":
				return CreateSuccessResponse(null);
			default:
			{
				Func<JToken, object> value;
				lock (_gate)
				{
					if (_isDisposed || !_nativeMethods.TryGetValue(text, out value))
					{
						UtilLog.Info("AgentDsBridge rejected unregistered native method: " + text);
						return CreateErrorResponse();
					}
				}
				object data = value(jToken);
				UtilLog.Info("AgentDsBridge handled native method: " + text);
				return CreateSuccessResponse(data);
			}
			}
		}
		catch (Exception ex)
		{
			UtilLog.Info("AgentDsBridge native call failed: " + ExceptionUtils.BuildExceptionMessage(ex));
			return CreateErrorResponse();
		}
	}

	internal Task<JToken> CallWebAsync(string methodName, object payload = null)
	{
		if (!AgentBridgeEvents.CanCallWeb(methodName))
		{
			throw new ArgumentException("Unknown Agent Bridge web method.", "methodName");
		}
		bool flag = false;
		PendingWebCall pendingWebCall;
		lock (_gate)
		{
			ThrowIfDisposed();
			int num = ++_nextCallbackId;
			pendingWebCall = new PendingWebCall(num, methodName, payload);
			_pendingWebCalls.Add(num, pendingWebCall);
			if (_isWebReady)
			{
				flag = true;
			}
			else
			{
				_startupQueue.Enqueue(pendingWebCall);
			}
		}
		if (flag)
		{
			DispatchWebCallAsync(pendingWebCall);
		}
		UtilLog.Info("AgentDsBridge queued Web call: " + methodName + ", callbackId=" + pendingWebCall.CallbackId + ", webReady=" + flag);
		return WaitForWebCallAsync(pendingWebCall);
	}

	private async Task<JToken> WaitForWebCallAsync(PendingWebCall call)
	{
		Task task = Task.Delay(_webCallTimeout);
		if (await Task.WhenAny(call.Completion.Task, task) == call.Completion.Task)
		{
			return await call.Completion.Task;
		}
		lock (_gate)
		{
			_pendingWebCalls.Remove(call.CallbackId);
		}
		TimeoutException exception = new TimeoutException("Agent Web method did not return in time: " + call.MethodName);
		call.Completion.TrySetException(exception);
		UtilLog.Info("AgentDsBridge Web call timed out: " + call.MethodName + ", callbackId=" + call.CallbackId);
		return await call.Completion.Task;
	}

	public void Dispose()
	{
		PendingWebCall[] array;
		lock (_gate)
		{
			if (_isDisposed)
			{
				return;
			}
			_isDisposed = true;
			_isWebReady = false;
			array = _pendingWebCalls.Values.ToArray();
			_pendingWebCalls.Clear();
			_startupQueue.Clear();
			_nativeMethods.Clear();
		}
		PendingWebCall[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].Completion.TrySetCanceled();
		}
	}

	private bool HasRegisteredNativeMethod(JToken payload)
	{
		JObject obj = payload as JObject;
		string text = (string?)obj?["name"];
		string a = (((string?)obj?["type"]) ?? "all").Trim();
		if (string.Equals(a, "asyn", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (!string.Equals(a, "all", StringComparison.OrdinalIgnoreCase) && !string.Equals(a, "syn", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		lock (_gate)
		{
			return !_isDisposed && text != null && _nativeMethods.ContainsKey(text.Trim());
		}
	}

	private void MarkWebReady()
	{
		bool flag = false;
		PendingWebCall[] queuedCalls;
		lock (_gate)
		{
			if (_isDisposed)
			{
				return;
			}
			if (!_isWebReady)
			{
				_isWebReady = true;
				flag = true;
			}
			queuedCalls = _startupQueue.ToArray();
			_startupQueue.Clear();
		}
		if (flag)
		{
			UtilLog.Info("AgentDsBridge Web initialization completed.");
			this.WebReady?.Invoke(this, EventArgs.Empty);
		}
		if (queuedCalls.Length == 0)
		{
			return;
		}
		Task.Run(async delegate
		{
			PendingWebCall[] array = queuedCalls;
			foreach (PendingWebCall call in array)
			{
				await DispatchWebCallAsync(call);
			}
		});
	}

	private void AcceptWebReturnValue(JToken payload)
	{
		JObject jObject = payload as JObject;
		int? num = (int?)jObject?["id"];
		bool flag = ((bool?)jObject?["complete"]) ?? true;
		if (!num.HasValue || !flag)
		{
			return;
		}
		PendingWebCall value;
		lock (_gate)
		{
			if (!_pendingWebCalls.TryGetValue(num.Value, out value))
			{
				return;
			}
			_pendingWebCalls.Remove(num.Value);
		}
		value.Completion.TrySetResult(jObject?["data"] ?? JValue.CreateNull());
		UtilLog.Info("AgentDsBridge received Web return value: " + value.MethodName + ", callbackId=" + value.CallbackId);
	}

	private async Task DispatchWebCallAsync(PendingWebCall call)
	{
		try
		{
			lock (_gate)
			{
				if (_isDisposed || !_pendingWebCalls.ContainsKey(call.CallbackId))
				{
					return;
				}
			}
			await _executeScriptAsync(BuildWebCallScript(call));
			UtilLog.Info("AgentDsBridge dispatched Web call: " + call.MethodName + ", callbackId=" + call.CallbackId);
		}
		catch (Exception exception)
		{
			lock (_gate)
			{
				_pendingWebCalls.Remove(call.CallbackId);
			}
			call.Completion.TrySetException(exception);
		}
	}

	private static string BuildWebCallScript(PendingWebCall call)
	{
		JArray jArray = new JArray();
		if (call.Payload != null)
		{
			jArray.Add((call.Payload as JToken) ?? JToken.FromObject(call.Payload));
		}
		JObject jObject = new JObject
		{
			["method"] = call.MethodName,
			["callbackId"] = call.CallbackId,
			["data"] = jArray.ToString(Formatting.None)
		};
		string text = "window._handleMessageFromNative(" + jObject.ToString(Formatting.None) + ");";
		if (call.MethodName != "agent.getApprovals")
		{
			return text;
		}
		return "(function () {\r\n    function hasApprovals(registry) {\r\n        return !!registry &&\r\n            (typeof registry['agent.getApprovals'] === 'function' ||\r\n             !!registry._obs && !!registry._obs.agent &&\r\n             typeof registry._obs.agent.getApprovals === 'function');\r\n    }\r\n    if (!hasApprovals(window._dsf) && !hasApprovals(window._dsaf)) {\r\n        window._dsbridge.call('_dsb.returnValue', JSON.stringify({data: {\r\n            id: " + call.CallbackId + ", complete: true, data: null\r\n        }}));\r\n        return;\r\n    }\r\n    " + text + "\r\n})();";
	}

	private static JObject ParseArguments(string argumentsJson)
	{
		if (!string.IsNullOrWhiteSpace(argumentsJson))
		{
			return JObject.Parse(argumentsJson);
		}
		return new JObject();
	}

	private static JToken NormalizePayload(JToken payload)
	{
		if (payload == null || payload.Type != JTokenType.String)
		{
			return payload;
		}
		string text = (string?)payload;
		if (string.IsNullOrWhiteSpace(text))
		{
			return payload;
		}
		try
		{
			return JToken.Parse(text);
		}
		catch (JsonReaderException)
		{
			return payload;
		}
	}

	private static string CreateSuccessResponse(object data)
	{
		return CreateResponse(0, data);
	}

	private static string CreateErrorResponse()
	{
		return CreateResponse(-1, null);
	}

	private static string CreateResponse(int code, object data)
	{
		return new JObject
		{
			["code"] = code,
			["data"] = ((data == null) ? JValue.CreateNull() : ((data as JToken) ?? JToken.FromObject(data)))
		}.ToString(Formatting.None);
	}

	private void ThrowIfDisposed()
	{
		if (_isDisposed)
		{
			throw new ObjectDisposedException("AgentDsBridgeHost");
		}
	}
}
