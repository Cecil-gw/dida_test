using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TickTickUtils.Lang;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Network;

namespace ticktick_WPF.Views.AI;

internal sealed class AIConversationService
{
	internal const int ConversationPageSize = 30;

	internal const int MaxConversationTitleLength = 64;

	private readonly Func<string, Task<AIConversationListResponse>> _getConversationsAsync;

	private readonly Func<string, AIConversationUpdateRequest, Task<AIConversationListItem>> _updateConversationAsync;

	private readonly Func<string, Task<bool>> _deleteConversationAsync;

	private readonly Func<string, Task<AIConversationListResponse>> _getPinnedConversationsAsync;

	private readonly Func<string, AIConversationPinBatchRequest, Task<AIConversationPinBatchResult>> _batchUpdatePinsAsync;

	private readonly bool _persistPins;

	private AIConversationPinStore _pinStore;

	internal bool AutomaticallySyncPins { get; }

	internal AIConversationPinStore PinStore
	{
		get
		{
			if (!_persistPins)
			{
				return _pinStore;
			}
			string currentPinScope = CurrentPinScope;
			if (currentPinScope == null)
			{
				throw new InvalidOperationException("No account for conversation pins.");
			}
			if (_pinStore?.Scope != currentPinScope)
			{
				_pinStore = new AIConversationPinStore(currentPinScope, new AISqlitePinStorage());
			}
			return _pinStore;
		}
	}

	private static string CurrentPinScope
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(LocalSettings.Settings.Common?.LoginUserId))
			{
				return JsonConvert.SerializeObject(new string[2]
				{
					BaseUrl.GetAIAgentUrl("/api/v1/conversations"),
					LocalSettings.Settings.Common.LoginUserId
				});
			}
			return null;
		}
	}

	internal static event EventHandler ConversationsChanged;

	internal static void NotifyConversationsChanged(object source)
	{
		AIConversationService.ConversationsChanged?.Invoke(source, EventArgs.Empty);
	}

	internal bool IsCurrentPinStore(AIConversationPinStore store)
	{
		if (store != null)
		{
			if (!_persistPins)
			{
				return store == _pinStore;
			}
			return store.Scope == CurrentPinScope;
		}
		return false;
	}

	public AIConversationService()
		: this((string uri) => ApiClient.GetJsonAsync<AIConversationListResponse>(uri), (string uri, AIConversationUpdateRequest request) => ApiClient.PostJsonAsync<AIConversationListItem>(uri, request), ApiClient.DeleteAsync)
	{
		_persistPins = true;
	}

	internal AIConversationService(Func<string, Task<AIConversationListResponse>> getConversationsAsync, Func<string, AIConversationUpdateRequest, Task<AIConversationListItem>> updateConversationAsync = null, Func<string, Task<bool>> deleteConversationAsync = null, Func<string, Task<AIConversationListResponse>> getPinnedConversationsAsync = null, Func<string, AIConversationPinBatchRequest, Task<AIConversationPinBatchResult>> batchUpdatePinsAsync = null, AIConversationPinStore pinStore = null, bool automaticallySyncPins = true)
	{
		_getConversationsAsync = getConversationsAsync ?? throw new ArgumentNullException("getConversationsAsync");
		_updateConversationAsync = updateConversationAsync ?? ((Func<string, AIConversationUpdateRequest, Task<AIConversationListItem>>)((string uri, AIConversationUpdateRequest request) => ApiClient.PostJsonAsync<AIConversationListItem>(uri, request)));
		_deleteConversationAsync = deleteConversationAsync ?? new Func<string, Task<bool>>(ApiClient.DeleteAsync);
		_getPinnedConversationsAsync = getPinnedConversationsAsync ?? ((Func<string, Task<AIConversationListResponse>>)((string uri) => ApiClient.GetJsonAsync<AIConversationListResponse>(uri)));
		_batchUpdatePinsAsync = batchUpdatePinsAsync ?? ((Func<string, AIConversationPinBatchRequest, Task<AIConversationPinBatchResult>>)((string uri, AIConversationPinBatchRequest request) => ApiClient.PostJsonAsync<AIConversationPinBatchResult>(uri, request)));
		_pinStore = pinStore ?? new AIConversationPinStore(Guid.NewGuid().ToString(), new AIMemoryPinStorage());
		AutomaticallySyncPins = automaticallySyncPins;
	}

	internal async Task<AIConversationPinBatchResult> BatchUpdatePinsAsync(AIConversationPinBatchRequest request)
	{
		AIConversationPinBatchResult aIConversationPinBatchResult = await _batchUpdatePinsAsync(BaseUrl.GetAIAgentUrl("/api/v1/conversations") + "/pins/batch", request);
		if (aIConversationPinBatchResult?.Items == null || aIConversationPinBatchResult.NotFound == null)
		{
			throw new InvalidOperationException("Pin batch response is incomplete.");
		}
		return aIConversationPinBatchResult;
	}

	public async Task<AIConversationListResponse> GetPinnedConversationsAsync()
	{
		AIConversationListResponse obj = (await _getPinnedConversationsAsync(BaseUrl.GetAIAgentUrl("/api/v1/conversations") + "/pinned")) ?? throw new InvalidOperationException("Pinned conversation list response is empty.");
		obj.Items = obj.Items ?? new List<AIConversationListItem>();
		return obj;
	}

	public async Task<AIConversationListResponse> GetConversationsAsync(int skip = 0, int limit = 30)
	{
		if (skip < 0)
		{
			throw new ArgumentOutOfRangeException("skip");
		}
		if (limit < 1 || limit > 100)
		{
			throw new ArgumentOutOfRangeException("limit");
		}
		AIConversationListResponse aIConversationListResponse = await _getConversationsAsync(BuildConversationsUri(skip, limit));
		if (aIConversationListResponse == null)
		{
			throw new InvalidOperationException("Agent conversation list response is empty.");
		}
		if (aIConversationListResponse.Items == null)
		{
			aIConversationListResponse.Items = new List<AIConversationListItem>();
		}
		return aIConversationListResponse;
	}

	internal async Task<bool> IsConversationMissingAsync(string conversationId, AIConversationListResponse firstPage, Func<bool> isCurrent)
	{
		int total = firstPage.Total;
		AIConversationListResponse aIConversationListResponse = firstPage;
		HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
		int skip = 0;
		while (isCurrent())
		{
			if (aIConversationListResponse.Total != total || total < 0)
			{
				return false;
			}
			if (aIConversationListResponse.Items.Any((AIConversationListItem item) => string.Equals(item?.EffectiveConversationId, conversationId, StringComparison.Ordinal)))
			{
				return false;
			}
			foreach (AIConversationListItem item in aIConversationListResponse.Items)
			{
				if (string.IsNullOrWhiteSpace(item?.EffectiveConversationId) || !ids.Add(item.EffectiveConversationId))
				{
					return false;
				}
			}
			skip += aIConversationListResponse.Items.Count;
			if (skip >= total)
			{
				return skip == total;
			}
			if (aIConversationListResponse.Items.Count == 0)
			{
				return false;
			}
			aIConversationListResponse = await GetConversationsAsync(skip);
		}
		return false;
	}

	internal async Task<HashSet<string>> GetMissingLocalConversationIdsAsync(IEnumerable<AIConversationListItem> conversations, AIConversationListResponse firstPage, Func<bool> isCurrent)
	{
		HashSet<string> missing = new HashSet<string>(StringComparer.Ordinal);
		foreach (AIConversationListItem item in conversations.Where((AIConversationListItem item) => item.IsLocalOnly).ToList())
		{
			if (isCurrent())
			{
				string id = item.EffectiveConversationId;
				bool flag = !string.IsNullOrWhiteSpace(id);
				if (flag)
				{
					flag = await IsConversationMissingAsync(id, firstPage, isCurrent);
				}
				if (flag && isCurrent())
				{
					missing.Add(id);
				}
				continue;
			}
			break;
		}
		return missing;
	}

	public async Task<AIConversationListItem> UpdateConversationTitleAsync(string conversationId, string title)
	{
		string normalizedId = NormalizeConversationId(conversationId);
		string normalizedTitle = title?.Trim();
		if (string.IsNullOrEmpty(normalizedTitle))
		{
			throw new ArgumentException("title");
		}
		if (normalizedTitle.Length > 64)
		{
			throw new ArgumentOutOfRangeException("title");
		}
		AIConversationPinStore pinStore = PinStore;
		AIConversationListItem response = await _updateConversationAsync(BuildConversationUri(normalizedId), new AIConversationUpdateRequest
		{
			Title = normalizedTitle
		});
		if (response == null)
		{
			response = new AIConversationListItem
			{
				Id = normalizedId,
				ConversationId = normalizedId,
				Title = normalizedTitle
			};
		}
		if (IsCurrentPinStore(pinStore))
		{
			try
			{
				await pinStore.UpdateTitleAsync(normalizedId, response.Title ?? normalizedTitle, response.UpdatedAt);
			}
			catch (Exception ex)
			{
				UtilLog.Info("Agent conversation pin title cache update failed: " + ex.Message);
			}
		}
		return response;
	}

	public async Task<bool> DeleteConversationAsync(string conversationId)
	{
		string id = NormalizeConversationId(conversationId);
		AIConversationPinStore pinStore = PinStore;
		bool deleted = await _deleteConversationAsync(BuildConversationUri(id));
		if (deleted && IsCurrentPinStore(pinStore))
		{
			try
			{
				await pinStore.RemoveAsync(id);
			}
			catch (Exception ex)
			{
				UtilLog.Info("Agent conversation pin cache removal failed: " + ex.Message);
			}
		}
		return deleted;
	}

	internal static string BuildConversationsUri(int skip, int limit)
	{
		return BaseUrl.GetAIAgentUrl("/api/v1/conversations") + "?skip=" + skip.ToString(CultureInfo.InvariantCulture) + "&limit=" + limit.ToString(CultureInfo.InvariantCulture) + "&include_archived=false&sort_by=updatedAt&sort_order=desc";
	}

	internal static string BuildConversationUri(string conversationId)
	{
		return BaseUrl.GetAIAgentUrl("/api/v1/conversations") + "/" + Uri.EscapeDataString(conversationId);
	}

	private static string NormalizeConversationId(string conversationId)
	{
		if (string.IsNullOrWhiteSpace(conversationId))
		{
			throw new ArgumentException("conversationId");
		}
		return conversationId.Trim();
	}
}
