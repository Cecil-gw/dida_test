using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TickTickUtils.Lang;

namespace ticktick_WPF.Views.AI;

internal sealed class AIConversationPinStore
{
	private sealed class StoreLocks
	{
		public readonly SemaphoreSlim State = new SemaphoreSlim(1, 1);

		public readonly SemaphoreSlim Sync = new SemaphoreSlim(1, 1);
	}

	private static readonly ConcurrentDictionary<string, StoreLocks> Locks = new ConcurrentDictionary<string, StoreLocks>();

	private readonly StoreLocks _locks;

	private readonly IAIPinStorage _storage;

	internal string Scope { get; }

	internal AIConversationPinStore(string scope, IAIPinStorage storage)
	{
		Scope = scope;
		_storage = storage;
		_locks = Locks.GetOrAdd(scope, (string _) => new StoreLocks());
	}

	private async Task<AIConversationPinState> ReadCoreAsync()
	{
		AIConversationPinState aIConversationPinState = JsonConvert.DeserializeObject<AIConversationPinState>((await _storage.ReadAsync(Scope)) ?? "{}");
		if (aIConversationPinState?.Items == null || aIConversationPinState.Pending == null || aIConversationPinState.Items.Any((AIConversationListItem item) => string.IsNullOrWhiteSpace(item?.EffectiveConversationId)) || aIConversationPinState.Pending.Any((KeyValuePair<string, AIPendingPinChange> entry) => string.IsNullOrWhiteSpace(entry.Key) || entry.Value == null || !Enum.IsDefined(typeof(AIPinChangeKind), entry.Value.Kind) || string.IsNullOrWhiteSpace(entry.Value.Revision)))
		{
			throw new InvalidOperationException("Invalid conversation pin cache.");
		}
		return aIConversationPinState;
	}

	private Task SaveAsync(AIConversationPinState state)
	{
		return _storage.WriteAsync(Scope, JsonConvert.SerializeObject(state));
	}

	private static AIConversationListItem Copy(AIConversationListItem item)
	{
		return JsonConvert.DeserializeObject<AIConversationListItem>(JsonConvert.SerializeObject(item));
	}

	private static List<AIConversationListItem> Ordered(IEnumerable<AIConversationListItem> items)
	{
		return (from item in items
			where item.IsPinned
			orderby item.PinSortOrder, item.PinTimeSortKey
			select item).ToList();
	}

	private static void SetPendingSortOrder(AIConversationPinState state, AIConversationListItem item, long sortOrder)
	{
		string effectiveConversationId = item.EffectiveConversationId;
		if (!state.Pending.TryGetValue(effectiveConversationId, out var value))
		{
			Dictionary<string, AIPendingPinChange> pending = state.Pending;
			AIPendingPinChange obj = new AIPendingPinChange
			{
				Kind = AIPinChangeKind.Reorder,
				SortOrder = sortOrder
			};
			value = obj;
			pending[effectiveConversationId] = obj;
		}
		else if (value.SortOrder != sortOrder)
		{
			value.SortOrder = sortOrder;
			value.Revision = Guid.NewGuid().ToString();
		}
		item.Metadata = new AIConversationPinMetadata
		{
			PinnedTime = item.Metadata.PinnedTime,
			SortOrder = sortOrder
		};
	}

	private static void RebasePendingOrder(AIConversationPinState state, Dictionary<string, AIConversationListItem> byId, List<AIConversationListItem> localOrder)
	{
		HashSet<string> hashSet = new HashSet<string>(from entry in state.Pending
			where entry.Value.Kind != AIPinChangeKind.Unpin
			select entry.Key, StringComparer.Ordinal);
		if (hashSet.Count == 0)
		{
			return;
		}
		List<AIConversationListItem> list = Ordered(byId.Values);
		AIConversationListItem value;
		List<string> list2 = (from aIConversationListItem in localOrder
			select aIConversationListItem.EffectiveConversationId into id
			where byId.TryGetValue(id, out value) && value.IsPinned
			select id).ToList();
		for (int num = list2.Count - 1; num >= 0; num--)
		{
			string text = list2[num];
			if (hashSet.Contains(text))
			{
				AIConversationListItem item = byId[text];
				list.Remove(item);
				string anchorId = state.Pending[text].AnchorId;
				int num2 = ((anchorId == null) ? (-1) : list2.IndexOf(anchorId));
				int index = ((num2 < 0 || hashSet.Contains(anchorId) || Math.Abs(num2 - num) != 1) ? ((num + 1 < list2.Count) ? list.IndexOf(byId[list2[num + 1]]) : list.Count) : (list.IndexOf(byId[anchorId]) + ((num2 < num) ? 1 : 0)));
				list.Insert(index, item);
			}
		}
		int num3 = 0;
		while (num3 < list.Count)
		{
			if (!hashSet.Contains(list[num3].EffectiveConversationId))
			{
				num3++;
				continue;
			}
			int num4;
			for (num4 = num3 + 1; num4 < list.Count && hashSet.Contains(list[num4].EffectiveConversationId); num4++)
			{
			}
			int num5 = num4 - num3;
			decimal? num6 = ((num3 > 0) ? new decimal?(list[num3 - 1].PinSortOrder) : ((decimal?)null));
			decimal? num7 = ((num4 < list.Count) ? new decimal?(list[num4].PinSortOrder) : ((decimal?)null));
			List<long> list3 = new List<long>();
			decimal? num8 = num6;
			for (int num9 = 0; num9 < num5; num9++)
			{
				decimal num10 = ((num6.HasValue && num7.HasValue) ? Math.Floor(num6.Value + (num7.Value - num6.Value) * (decimal)(num9 + 1) / (decimal)(num5 + 1)) : (num6.HasValue ? (num6.Value + 268435456m * (decimal)(num9 + 1)) : (num7.HasValue ? (num7.Value - 268435456m * (decimal)(num5 - num9)) : (268435456m * (decimal)num9))));
				if (num10 < -9223372036854775808m || num10 > 9223372036854775807m || (num8.HasValue && num10 <= num8.Value) || (num7.HasValue && num10 >= num7.Value))
				{
					for (int num11 = 0; num11 < list.Count; num11++)
					{
						SetPendingSortOrder(state, list[num11], (long)num11 * 268435456L);
					}
					return;
				}
				list3.Add((long)num10);
				num8 = num10;
			}
			for (int num12 = 0; num12 < num5; num12++)
			{
				SetPendingSortOrder(state, list[num3 + num12], list3[num12]);
			}
			num3 = num4;
		}
	}

	internal async Task<AIConversationPinState> ReadAsync()
	{
		await _locks.State.WaitAsync();
		try
		{
			return await ReadCoreAsync();
		}
		finally
		{
			_locks.State.Release();
		}
	}

	internal async Task<AIConversationPinState> ChangeAsync(AIConversationListItem conversation, bool pinned, AIConversationListItem target, bool after, IEnumerable<AIConversationListItem> seed, Func<bool> isCurrent)
	{
		await _locks.State.WaitAsync();
		try
		{
			AIConversationPinState state = await ReadCoreAsync();
			if (!isCurrent())
			{
				return null;
			}
			if (!state.Initialized)
			{
				state.Items = Ordered(seed).Select(Copy).ToList();
				state.Initialized = true;
			}
			string id = conversation.EffectiveConversationId;
			AIConversationListItem aIConversationListItem = state.Items.FirstOrDefault((AIConversationListItem row) => row.EffectiveConversationId == id);
			List<AIConversationListItem> list = Ordered(state.Items);
			if (target != null && (aIConversationListItem == null || !aIConversationListItem.IsPinned || !list.Any((AIConversationListItem row) => row.EffectiveConversationId == target.EffectiveConversationId)))
			{
				return null;
			}
			if (pinned && target == null && aIConversationListItem != null && aIConversationListItem.IsPinned)
			{
				return state;
			}
			if (pinned && (aIConversationListItem == null || !aIConversationListItem.IsPinned) && list.Count >= 20)
			{
				return null;
			}
			if (aIConversationListItem == null)
			{
				aIConversationListItem = Copy(conversation);
				state.Items.Add(aIConversationListItem);
			}
			aIConversationListItem.Title = conversation.Title;
			AIPinChangeKind aIPinChangeKind = ((!pinned) ? AIPinChangeKind.Unpin : ((target != null) ? AIPinChangeKind.Reorder : AIPinChangeKind.Pin));
			if (!pinned)
			{
				aIConversationListItem.Metadata = null;
				state.Pending[id] = new AIPendingPinChange
				{
					Kind = aIPinChangeKind
				};
			}
			else
			{
				list.Remove(aIConversationListItem);
				int num = ((target == null) ? list.Count : (list.FindIndex((AIConversationListItem row) => row.EffectiveConversationId == target.EffectiveConversationId) + (after ? 1 : 0)));
				if (state.Pending.TryGetValue(id, out var value) && value.Kind == AIPinChangeKind.Pin)
				{
					aIPinChangeKind = AIPinChangeKind.Pin;
				}
				JToken pinnedTime = aIConversationListItem.Metadata?.PinnedTime ?? new JValue(DateTimeOffset.UtcNow);
				if (AIWorkspaceView.TryGetPinSortOrder(list, num, out var sortOrder))
				{
					aIConversationListItem.Metadata = new AIConversationPinMetadata
					{
						PinnedTime = pinnedTime,
						SortOrder = sortOrder
					};
					state.Pending[id] = new AIPendingPinChange
					{
						Kind = aIPinChangeKind,
						SortOrder = sortOrder,
						AnchorId = target?.EffectiveConversationId
					};
				}
				else
				{
					list.Insert(num, aIConversationListItem);
					aIConversationListItem.Metadata = new AIConversationPinMetadata
					{
						PinnedTime = pinnedTime
					};
					foreach (AIConversationListItem item in list)
					{
						string effectiveConversationId = item.EffectiveConversationId;
						AIPendingPinChange value2;
						AIPinChangeKind kind = ((effectiveConversationId == id) ? aIPinChangeKind : ((!state.Pending.TryGetValue(effectiveConversationId, out value2) || value2.Kind != AIPinChangeKind.Pin) ? AIPinChangeKind.Reorder : AIPinChangeKind.Pin));
						long num2 = (long)list.IndexOf(item) * 268435456L;
						item.Metadata = new AIConversationPinMetadata
						{
							PinnedTime = item.Metadata.PinnedTime,
							SortOrder = num2
						};
						state.Pending[effectiveConversationId] = new AIPendingPinChange
						{
							Kind = kind,
							SortOrder = num2,
							AnchorId = ((effectiveConversationId == id) ? target?.EffectiveConversationId : null)
						};
					}
				}
			}
			await SaveAsync(state);
			return state;
		}
		finally
		{
			_locks.State.Release();
		}
	}

	private static void MergeRemote(AIConversationPinState state, IEnumerable<AIConversationListItem> remote)
	{
		List<AIConversationListItem> localOrder = Ordered(state.Items);
		Dictionary<string, AIConversationListItem> dictionary = (from item in remote
			where item != null && item.IsPinned && !item.IsArchived && !string.IsNullOrWhiteSpace(item.EffectiveConversationId)
			group item by item.EffectiveConversationId).ToDictionary((IGrouping<string, AIConversationListItem> group) => group.Key, (IGrouping<string, AIConversationListItem> group) => Copy(group.First()), StringComparer.Ordinal);
		HashSet<string> remoteIds = new HashSet<string>(dictionary.Keys, StringComparer.Ordinal);
		foreach (KeyValuePair<string, AIPendingPinChange> item in state.Pending.ToList())
		{
			string id = item.Key;
			AIPendingPinChange value = item.Value;
			AIConversationListItem aIConversationListItem = state.Items.FirstOrDefault((AIConversationListItem item) => item.EffectiveConversationId == id);
			if (value.Kind == AIPinChangeKind.Reorder && (!remoteIds.Contains(id) || (value.AnchorId != null && !AnchorAvailable(value.AnchorId))))
			{
				state.Pending.Remove(id);
			}
			else if (value.Kind == AIPinChangeKind.Unpin)
			{
				dictionary.Remove(id);
				if (aIConversationListItem != null)
				{
					aIConversationListItem.Metadata = null;
					dictionary[id] = aIConversationListItem;
				}
			}
			else if (aIConversationListItem != null)
			{
				AIConversationListItem value2;
				AIConversationListItem aIConversationListItem2 = (dictionary.TryGetValue(id, out value2) ? value2 : aIConversationListItem);
				aIConversationListItem2.Metadata = new AIConversationPinMetadata
				{
					PinnedTime = (aIConversationListItem2.Metadata?.PinnedTime ?? aIConversationListItem.Metadata?.PinnedTime ?? new JValue(DateTimeOffset.UtcNow)),
					SortOrder = (aIConversationListItem2.Metadata?.SortOrder ?? value.SortOrder)
				};
				dictionary[id] = aIConversationListItem2;
			}
		}
		RebasePendingOrder(state, dictionary, localOrder);
		state.Items = dictionary.Values.ToList();
		state.Initialized = true;
		bool AnchorAvailable(string text)
		{
			if (!state.Pending.TryGetValue(text, out var value3))
			{
				return remoteIds.Contains(text);
			}
			if (value3.Kind != AIPinChangeKind.Pin)
			{
				if (value3.Kind == AIPinChangeKind.Reorder)
				{
					return remoteIds.Contains(text);
				}
				return false;
			}
			return true;
		}
	}

	internal async Task<AIConversationPinState> MergeRemoteAsync(IEnumerable<AIConversationListItem> remote, Func<bool> isCurrent)
	{
		await _locks.State.WaitAsync();
		try
		{
			AIConversationPinState state = await ReadCoreAsync();
			if (!isCurrent())
			{
				return state;
			}
			MergeRemote(state, remote);
			await SaveAsync(state);
			return state;
		}
		finally
		{
			_locks.State.Release();
		}
	}

	internal async Task<AIConversationPinState> SyncAsync(AIConversationService service, Func<bool> isCurrent)
	{
		await _locks.Sync.WaitAsync();
		try
		{
			if (!isCurrent())
			{
				return await ReadAsync();
			}
			AIConversationListResponse remote = await service.GetPinnedConversationsAsync();
			if (!isCurrent())
			{
				return await ReadAsync();
			}
			AIConversationPinState aIConversationPinState = await MergeRemoteAsync(remote.Items, isCurrent);
			Dictionary<string, AIPendingPinChange> sent = aIConversationPinState.Pending.ToDictionary((KeyValuePair<string, AIPendingPinChange> keyValuePair) => keyValuePair.Key, (KeyValuePair<string, AIPendingPinChange> keyValuePair) => keyValuePair.Value, StringComparer.Ordinal);
			HashSet<string> remoteIds = new HashSet<string>(from item in remote.Items
				where item.IsPinned
				select item.EffectiveConversationId);
			int num = remoteIds.Count - sent.Count((KeyValuePair<string, AIPendingPinChange> keyValuePair) => keyValuePair.Value.Kind == AIPinChangeKind.Unpin && remoteIds.Contains(keyValuePair.Key));
			foreach (KeyValuePair<string, AIPendingPinChange> item in sent.Where((KeyValuePair<string, AIPendingPinChange> keyValuePair) => keyValuePair.Value.Kind == AIPinChangeKind.Pin && !remoteIds.Contains(keyValuePair.Key)).ToList())
			{
				if (num >= 20)
				{
					sent.Remove(item.Key);
				}
				else
				{
					num++;
				}
			}
			if (sent.Count == 0 || !isCurrent())
			{
				return aIConversationPinState;
			}
			AIConversationPinBatchRequest request = new AIConversationPinBatchRequest
			{
				Update = (from keyValuePair in sent
					where keyValuePair.Value.Kind != AIPinChangeKind.Unpin
					select new AIConversationPinUpsert
					{
						ConversationId = keyValuePair.Key,
						SortOrder = keyValuePair.Value.SortOrder
					}).ToList(),
				Delete = (from keyValuePair in sent
					where keyValuePair.Value.Kind == AIPinChangeKind.Unpin
					select keyValuePair.Key).ToList()
			};
			AIConversationPinBatchResult result = await service.BatchUpdatePinsAsync(request);
			if (!isCurrent())
			{
				return await ReadAsync();
			}
			await _locks.State.WaitAsync();
			try
			{
				AIConversationPinState state = await ReadCoreAsync();
				if (!isCurrent())
				{
					return state;
				}
				foreach (KeyValuePair<string, AIPendingPinChange> entry in sent)
				{
					if (!state.Pending.TryGetValue(entry.Key, out var value) || value.Revision != entry.Value.Revision)
					{
						continue;
					}
					bool flag = result.NotFound.Contains(entry.Key);
					AIConversationListItem aIConversationListItem = result.Items.FirstOrDefault((AIConversationListItem item) => item?.EffectiveConversationId == entry.Key);
					if (flag || (aIConversationListItem != null && !((entry.Value.Kind == AIPinChangeKind.Unpin) ? aIConversationListItem.IsPinned : (!aIConversationListItem.IsPinned || aIConversationListItem.PinSortOrder != entry.Value.SortOrder))))
					{
						state.Pending.Remove(entry.Key);
						state.Items.RemoveAll((AIConversationListItem item) => item.EffectiveConversationId == entry.Key);
						if (flag)
						{
							state.UnavailableIds.Add(entry.Key);
						}
						if (!flag && aIConversationListItem.IsPinned)
						{
							state.Items.Add(Copy(aIConversationListItem));
						}
					}
				}
				await SaveAsync(state);
				return state;
			}
			finally
			{
				_locks.State.Release();
			}
		}
		finally
		{
			_locks.Sync.Release();
		}
	}

	internal async Task<AIConversationPinState> RefreshAsync(AIConversationService service, Func<bool> isCurrent)
	{
		await _locks.Sync.WaitAsync();
		try
		{
			if (!isCurrent())
			{
				return await ReadAsync();
			}
			AIConversationListResponse response = await service.GetPinnedConversationsAsync();
			try
			{
				return await MergeRemoteAsync(response.Items, isCurrent);
			}
			catch (Exception ex)
			{
				UtilLog.Info("Agent conversation pin cache refresh failed: " + ex.Message);
				try
				{
					AIConversationPinState aIConversationPinState = await ReadAsync();
					if (isCurrent())
					{
						MergeRemote(aIConversationPinState, response.Items);
					}
					aIConversationPinState.IsTransient = true;
					return aIConversationPinState;
				}
				catch (Exception ex2)
				{
					UtilLog.Info("Agent conversation pin cache fallback read failed: " + ex2.Message);
				}
				return new AIConversationPinState
				{
					Initialized = true,
					Items = response.Items,
					IsTransient = true
				};
			}
		}
		finally
		{
			_locks.Sync.Release();
		}
	}

	internal async Task RemoveAsync(string id)
	{
		await _locks.State.WaitAsync();
		try
		{
			AIConversationPinState aIConversationPinState = await ReadCoreAsync();
			if (aIConversationPinState.Items.Any((AIConversationListItem item) => item.EffectiveConversationId == id) || aIConversationPinState.Pending.ContainsKey(id))
			{
				aIConversationPinState.Items.RemoveAll((AIConversationListItem item) => item.EffectiveConversationId == id);
				aIConversationPinState.Pending.Remove(id);
				await SaveAsync(aIConversationPinState);
			}
		}
		finally
		{
			_locks.State.Release();
		}
	}

	internal async Task UpdateTitleAsync(string id, string title, DateTimeOffset? updatedAt)
	{
		await _locks.State.WaitAsync();
		try
		{
			AIConversationPinState aIConversationPinState = await ReadCoreAsync();
			AIConversationListItem aIConversationListItem = aIConversationPinState.Items.FirstOrDefault((AIConversationListItem row) => row.EffectiveConversationId == id);
			if (aIConversationListItem != null)
			{
				aIConversationListItem.Title = title;
				aIConversationListItem.UpdatedAt = updatedAt ?? aIConversationListItem.UpdatedAt;
				await SaveAsync(aIConversationPinState);
			}
		}
		finally
		{
			_locks.State.Release();
		}
	}
}
