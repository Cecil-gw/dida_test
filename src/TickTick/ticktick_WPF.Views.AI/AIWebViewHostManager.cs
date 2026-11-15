using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TickTickUtils;
using TickTickUtils.Lang;

namespace ticktick_WPF.Views.AI;

internal sealed class AIWebViewHostManager
{
	private const int PageSynchronizationVerificationAttempts = 20;

	private const int PageSynchronizationVerificationDelayMilliseconds = 100;

	private readonly Dictionary<AIWebAppView, Task<bool>> _synchronizationTasks = new Dictionary<AIWebAppView, Task<bool>>();

	private AIWebAppView _webView;

	private AIWebAppView _pendingSynchronizationWebView;

	private int _sharedPageVersion;

	public AIWebAppView ActiveWebView { get; private set; }

	public AIWebViewHost ActiveHost { get; private set; }

	public string SharedPage { get; private set; } = "home";

	public string SharedConversationId { get; private set; }

	public int RetainedCount => (_webView != null) ? 1 : 0;

	public bool IsActiveSynchronizationPending => _pendingSynchronizationWebView == ActiveWebView;

	public event Action<AIWebAppView> WebViewCreated;

	public event Action<AIWebAppView> WebViewReleasing;

	public bool IsActive(object sender)
	{
		return sender == ActiveWebView;
	}

	public AIWebAppView GetRetained(AIWebViewHost host)
	{
		if (!AIWebViewHostPolicy.CanRetainWebView(host))
		{
			return null;
		}
		return _webView;
	}

	public AIWebAppView EnsureRetained(AIWebViewHost host)
	{
		if (!AIWebViewHostPolicy.CanRetainWebView(host))
		{
			throw new ArgumentOutOfRangeException("host");
		}
		AIWebAppView retained = GetRetained(host);
		if (retained != null)
		{
			return retained;
		}
		retained = (_webView = new AIWebAppView());
		this.WebViewCreated?.Invoke(retained);
		return retained;
	}

	public AIWebViewActivation Activate(AIWebViewHost host, bool createIfMissing = true)
	{
		if (!AIWebViewHostPolicy.CanRetainWebView(host))
		{
			throw new ArgumentOutOfRangeException("host");
		}
		AIWebAppView aIWebAppView = (createIfMissing ? EnsureRetained(host) : GetRetained(host));
		if (aIWebAppView == null)
		{
			return null;
		}
		AIWebAppView activeWebView = ActiveWebView;
		ActiveWebView = aIWebAppView;
		ActiveHost = host;
		bool flag = !MatchesSharedPage(aIWebAppView);
		if (flag)
		{
			aIWebAppView.MarkAgentPageStateResolving();
			_pendingSynchronizationWebView = aIWebAppView;
		}
		else if (_pendingSynchronizationWebView == aIWebAppView)
		{
			_pendingSynchronizationWebView = null;
		}
		return new AIWebViewActivation(aIWebAppView, activeWebView, flag);
	}

	public void CompleteActivePageState(string page, string conversationId)
	{
		string text = (string.Equals(page, "home", StringComparison.OrdinalIgnoreCase) ? "home" : "conversation");
		string text2 = ((text == "home" || string.IsNullOrWhiteSpace(conversationId)) ? null : conversationId.Trim());
		if (!string.Equals(SharedPage, text, StringComparison.Ordinal) || !string.Equals(SharedConversationId, text2, StringComparison.Ordinal))
		{
			SharedPage = text;
			SharedConversationId = text2;
			_sharedPageVersion++;
		}
		_pendingSynchronizationWebView = null;
		ActiveWebView?.MarkAgentPageStateResolved(text, text2);
	}

	public bool MatchesSharedPage(AIWebAppView webView)
	{
		if (webView == null || !webView.IsAgentPageStateResolved || !string.Equals(webView.ResolvedAgentPage, SharedPage, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (!string.Equals(SharedPage, "home", StringComparison.OrdinalIgnoreCase))
		{
			if (!string.IsNullOrWhiteSpace(SharedConversationId))
			{
				return string.Equals(webView.ResolvedConversationId, SharedConversationId, StringComparison.Ordinal);
			}
			return false;
		}
		return true;
	}

	public async Task<bool> SynchronizeAsync(AIWebAppView webView)
	{
		if (webView == null || !webView.IsAgentBridgeReady)
		{
			return false;
		}
		if (_synchronizationTasks.TryGetValue(webView, out var value))
		{
			return await value;
		}
		Task<bool> task = SynchronizeCoreAsync(webView);
		_synchronizationTasks[webView] = task;
		try
		{
			return await task;
		}
		finally
		{
			if (_synchronizationTasks.TryGetValue(webView, out var value2) && value2 == task)
			{
				_synchronizationTasks.Remove(webView);
			}
		}
	}

	public async Task ReleaseAsync(AIWebViewHost host)
	{
		AIWebAppView retained = GetRetained(host);
		if (retained == null)
		{
			return;
		}
		this.WebViewReleasing?.Invoke(retained);
		_synchronizationTasks.Remove(retained);
		if (_pendingSynchronizationWebView == retained)
		{
			_pendingSynchronizationWebView = null;
		}
		_webView = null;
		if (ActiveWebView == retained)
		{
			ActiveWebView = null;
			ActiveHost = AIWebViewHost.None;
		}
		try
		{
			await retained.DisposeAsync();
		}
		catch (Exception ex)
		{
			UtilLog.Info("Failed to dispose retained Agent WebView for " + host.ToString() + ". " + ExceptionUtils.BuildExceptionMessage(ex));
		}
	}

	public async Task DisposeAsync()
	{
		await ReleaseAsync(AIWebViewHost.MainWindow);
	}

	public async Task ResetAsync()
	{
		_sharedPageVersion++;
		_pendingSynchronizationWebView = null;
		await DisposeAsync();
		SharedPage = "home";
		SharedConversationId = null;
		ActiveWebView = null;
		ActiveHost = AIWebViewHost.None;
	}

	internal static bool IsPageStateReady(JToken state)
	{
		if (state == null || (bool?)state["isLoading"] != false)
		{
			return false;
		}
		string a = (string?)state["page"];
		if (!string.Equals(a, "home", StringComparison.OrdinalIgnoreCase))
		{
			return string.Equals(a, "conversation", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private async Task<bool> SynchronizeCoreAsync(AIWebAppView webView)
	{
		int sharedPageVersion = _sharedPageVersion;
		string sharedPage = SharedPage;
		string sharedConversationId = SharedConversationId;
		JObject state = await webView.GetAgentStateAsync();
		if (!DoesPageStateMatch(sharedPage, sharedConversationId, state))
		{
			bool flag;
			if (string.Equals(sharedPage, "home", StringComparison.OrdinalIgnoreCase))
			{
				flag = await webView.StartNewConversationAsync();
			}
			else
			{
				if (string.IsNullOrWhiteSpace(sharedConversationId))
				{
					return false;
				}
				flag = await webView.SwitchConversationAsync(sharedConversationId);
			}
			if (!flag)
			{
				UtilLog.Info("Agent WebView failed to synchronize shared conversation state.");
				return false;
			}
			for (int attempt = 0; attempt < 20; attempt++)
			{
				if (sharedPageVersion != _sharedPageVersion)
				{
					return false;
				}
				state = await webView.GetAgentStateAsync();
				if (DoesPageStateMatch(sharedPage, sharedConversationId, state))
				{
					break;
				}
				if (attempt < 19)
				{
					await Task.Delay(100);
				}
			}
		}
		if (sharedPageVersion != _sharedPageVersion || !DoesPageStateMatch(sharedPage, sharedConversationId, state))
		{
			return false;
		}
		webView.MarkAgentPageStateResolved(sharedPage, sharedConversationId);
		if (webView == ActiveWebView)
		{
			_pendingSynchronizationWebView = null;
		}
		return true;
	}

	private static bool DoesPageStateMatch(string expectedPage, string expectedConversationId, JToken state)
	{
		if (!IsPageStateReady(state) || !string.Equals((string?)state["page"], expectedPage, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (string.Equals(expectedPage, "home", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		string a = ((string?)state["currentConversationId"]) ?? ((string?)state["conversationId"]);
		if (!string.IsNullOrWhiteSpace(expectedConversationId))
		{
			return string.Equals(a, expectedConversationId, StringComparison.Ordinal);
		}
		return false;
	}
}
