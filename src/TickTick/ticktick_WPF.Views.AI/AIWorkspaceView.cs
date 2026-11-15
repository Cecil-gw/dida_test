using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using Emoji.Wpf;
using Newtonsoft.Json.Linq;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Habit;
using ticktick_WPF.Views.MainListView.DetailView;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.AI;

public class AIWorkspaceView : UserControl, IDisposable, IComponentConnector, IStyleConnector
{
	internal sealed class ConversationPinDragHandler : IDragSortHandler
	{
		private readonly Func<AIWorkspaceView> _getWorkspace;

		private AIWorkspaceView _workspace;

		private IEnumerable _originalSource;

		private object _selected;

		private ObservableCollection<AIConversationListItem> _previewItems;

		private ListCollectionView _previewView;

		private AIConversationListItem _source;

		private AIConversationListItem _target;

		private bool _after;

		private bool _canDrop;

		private int _originalIndex;

		private bool _wasIgnoringSelection;

		private int _pinVersion;

		internal ConversationPinDragHandler(Func<AIWorkspaceView> getWorkspace)
		{
			_getWorkspace = getWorkspace;
		}

		public bool CanStart(object item, DependencyObject source)
		{
			AIWorkspaceView aIWorkspaceView = _getWorkspace();
			if (aIWorkspaceView != null && aIWorkspaceView.CanEditConversationPins && item is AIConversationListItem { IsPinned: not false, IsEditing: false } aIConversationListItem && !string.IsNullOrWhiteSpace(aIConversationListItem.EffectiveConversationId) && (source is Visual || source is ContentElement) && !(source is ButtonBase) && !(source is TextBox) && Utils.FindParent<ButtonBase>(source) == null)
			{
				return Utils.FindParent<TextBox>(source) == null;
			}
			return false;
		}

		public bool Start(ItemsControl control, object item)
		{
			if (!control.Items.Contains(item) || !CanStart(item, control))
			{
				return false;
			}
			_workspace = _getWorkspace();
			_source = (AIConversationListItem)item;
			_canDrop = false;
			_target = null;
			_originalSource = control.ItemsSource;
			_selected = ((control == _workspace.ConversationList && _workspace._isConversationListFloating) ? _workspace.CurrentConversation : ((ListBox)control).SelectedItem);
			_wasIgnoringSelection = _workspace._ignoreConversationSelection;
			_workspace._ignoreConversationSelection = true;
			_workspace._isDraggingConversations = true;
			_workspace._conversationEditVersion++;
			_pinVersion = _workspace._pinOperationVersion;
			_previewItems = new ObservableCollection<AIConversationListItem>(control.Items.Cast<AIConversationListItem>());
			_originalIndex = _previewItems.IndexOf(_source);
			_previewView = new ListCollectionView(_previewItems);
			_previewView.GroupDescriptions.Add(new PropertyGroupDescription("GroupTitle"));
			control.ItemsSource = _previewView;
			((ListBox)control).SelectedItem = _selected;
			return true;
		}

		public void Move(ItemsControl control, object item, object target, bool after)
		{
			if (_source == null)
			{
				return;
			}
			_canDrop = _source.IsPinned && target is AIConversationListItem aIConversationListItem && aIConversationListItem.IsPinned;
			if (!_canDrop)
			{
				_target = null;
				int num = _previewItems.IndexOf(_source);
				if (num != _originalIndex)
				{
					_previewItems.Move(num, _originalIndex);
				}
			}
			else if (target != _source)
			{
				AIConversationListItem item2 = (_target = (AIConversationListItem)target);
				_after = after;
				int num2 = _previewItems.IndexOf(item2) + (after ? 1 : 0);
				int num3 = _previewItems.IndexOf(_source);
				if (num3 < num2)
				{
					num2--;
				}
				if (num3 != num2)
				{
					_previewItems.Move(num3, num2);
				}
			}
		}

		public async void Complete(ItemsControl control, object item, bool cancelled)
		{
			await CompleteAsync(control, item, cancelled);
		}

		internal async Task CompleteAsync(ItemsControl control, object item, bool cancelled)
		{
			AIWorkspaceView workspace = _workspace;
			if (workspace == null)
			{
				return;
			}
			AIConversationListItem source = _source;
			AIConversationListItem target = _target;
			bool after = _after;
			bool canDrop = _canDrop;
			ListCollectionView previewView = _previewView;
			IEnumerable originalSource = _originalSource;
			object selected = _selected;
			bool wasIgnoringSelection = _wasIgnoringSelection;
			int pinVersion = _pinVersion;
			_workspace = null;
			try
			{
				if (!cancelled && canDrop && target != null && !workspace._isDisposed && workspace._pinOperationVersion == pinVersion)
				{
					await workspace.MoveConversationPinAsync(source, target, after);
				}
			}
			finally
			{
				try
				{
					if (control.ItemsSource == previewView)
					{
						control.ItemsSource = originalSource;
						((ListBox)control).SelectedItem = selected;
					}
				}
				finally
				{
					workspace._ignoreConversationSelection = wasIgnoringSelection;
					workspace._isDraggingConversations = false;
					_previewView = null;
					_previewItems = null;
					_originalSource = null;
					_source = null;
					_target = null;
					workspace.UpdateConversationInteractionState();
				}
			}
		}
	}

	private const double DefaultDetailPaneWidth = 360.0;

	private const double MinDetailPaneWidth = 312.0;

	private const double DefaultConversationListWidth = 260.0;

	private const double MinConversationListWidth = 230.0;

	private const double MaxConversationListWidth = 600.0;

	private const double MinConversationPaneWidth = 320.0;

	private const double ThreeColumnMinimumWidth = 896.0;

	private const double FloatingDetailPaneWidth = 310.0;

	private const double HeaderMenuPopupVerticalOffset = -4.0;

	private const double PopupBorderRightMargin = 10.0;

	private const double HeaderPopupVisibleRightOverflow = 2.0;

	private const string RenameConversationAction = "rename";

	private const string DeleteConversationAction = "delete";

	internal const string PinConversationAction = "pin";

	internal const int MaxPinnedConversations = 20;

	private bool _isUpdatingPins;

	private bool _isDraggingConversations;

	private DispatcherTimer _pinSyncTimer;

	private bool _isSyncingPins;

	private int _pinOperationVersion;

	private int _pinSessionVersion;

	private HashSet<string> _loadedConversationIds;

	private readonly AIConversationService _conversationService;

	private readonly ObservableCollection<AIConversationListItem> _conversations = new ObservableCollection<AIConversationListItem>();

	private readonly HashSet<string> _deletingConversationIds = new HashSet<string>(StringComparer.Ordinal);

	private readonly ListCollectionView _conversationView;

	private TaskDetailView _taskDetailView;

	private HabitDetailControl _habitDetailControl;

	private double _lastDetailPaneWidth = 360.0;

	private double _lastConversationListWidth = 260.0;

	private bool _isConversationListVisible = true;

	private bool _isConversationListFloating;

	private bool _isDetailPaneFloating;

	private bool _isFloatingDetailPaneHidden;

	private int _conversationListAnimationVersion;

	private string _currentConversationId;

	private bool _isAgentBridgeReady;

	private bool _isLoadingConversations;

	private bool _isLoadingMoreConversations;

	private bool _hasMoreConversations;

	private bool _conversationLoadFailed;

	private bool _hasRequestedInitialLoad;

	private bool _ignoreConversationSelection;

	private bool _isSwitchingConversation;

	private bool _isDisposed;

	private int _loadedServerConversationCount;

	private int _conversationLoadVersion;

	private EscPopup _conversationMenuPopup;

	private FrameworkElement _conversationMenuButtonPressedWhileOpen;

	private AIConversationListItem _renamingConversation;

	private string _renameOriginalTitle;

	private string _renameInitialText;

	private bool _isHeaderRename;

	private bool _isPrivacyAuthorizationRequired = true;

	private readonly DispatcherTimer _conversationRefreshTimer;

	private int _pendingRenameRequests;

	private int _conversationEditVersion;

	private int _conversationActivityVersion;

	private bool _isGenerating;

	private bool _isGenerationStateResolved;

	private int _generationStateVersion;

	private Func<Task<JObject>> _getGenerationStateAsync;

	internal ColumnDefinition ConversationListColumn;

	internal ColumnDefinition ConversationColumn;

	internal ColumnDefinition DetailColumn;

	internal Grid ConversationListPane;

	internal TextBlock AssistantTitle;

	internal Button NewConversationButton;

	internal DockPanel ConversationListPanel;

	internal TextBlock LoadingMoreConversationsText;

	internal ListBox ConversationList;

	internal StackPanel LoadingConversationPanel;

	internal StackPanel EmptyConversationPanel;

	internal StackPanel ConversationErrorPanel;

	internal Button ConversationListToggleButton;

	internal EmjTextBlock ConversationTitle;

	internal Button CurrentConversationMoreButton;

	internal Popup HeaderRenamePopup;

	internal ContentControl HeaderRenamePopupContent;

	internal AIConversationRenameEditor HeaderRenameTextBox;

	internal ContentControl ConversationContentHost;

	internal Border ConversationListDismissArea;

	internal Grid DetailPane;

	internal Grid DetailHost;

	internal Border ConversationListDivider;

	internal GridSplitter ConversationListSeparator;

	internal Border DetailDivider;

	internal GridSplitter DetailSeparator;

	internal AIPrivacyAuthorizationView PrivacyAuthorizationView;

	private bool _contentLoaded;

	private HashSet<string> LoadedConversationIds => _loadedConversationIds ?? (_loadedConversationIds = new HashSet<string>(StringComparer.Ordinal));

	internal ICollectionView ConversationItemsView => _conversationView;

	internal AIConversationListItem CurrentConversation => FindConversation(_currentConversationId);

	internal bool HasCurrentConversation => !string.IsNullOrWhiteSpace(_currentConversationId);

	internal bool CanSwitchConversation
	{
		get
		{
			if (_isAgentBridgeReady && !_isSwitchingConversation && !_isUpdatingPins)
			{
				return !_isDraggingConversations;
			}
			return false;
		}
	}

	internal bool CanEditConversationPins
	{
		get
		{
			if (!_isPrivacyAuthorizationRequired && !_isDisposed && !_isSwitchingConversation && !_isUpdatingPins)
			{
				return _renamingConversation == null;
			}
			return false;
		}
	}

	public event EventHandler NewConversationRequested;

	public event Func<string, string, Task<bool>> ConversationSelected;

	public event Action<string> CurrentConversationTitleChanged;

	public event EventHandler ConversationStateChanged;

	public event EventHandler PrivacyAuthorizationAccepted;

	public AIWorkspaceView()
		: this(new AIConversationService())
	{
	}

	internal AIWorkspaceView(AIConversationService conversationService)
	{
		_conversationService = conversationService ?? throw new ArgumentNullException("conversationService");
		InitializeComponent();
		AssistantTitle.Text = (Utils.IsTickPackage() ? "TickTick AI" : Utils.GetString("DidaAssistant"));
		_conversationView = CreateConversationItemsView(_conversations);
		ConversationList.ItemsSource = _conversationView;
		AttachConversationPinDrag(ConversationList, () => this);
		_conversationRefreshTimer = new DispatcherTimer(DispatcherPriority.Background, base.Dispatcher)
		{
			Interval = TimeSpan.FromMilliseconds(500.0)
		};
		_conversationRefreshTimer.Tick += OnConversationRefreshTimerTick;
		AIConversationService.ConversationsChanged += OnSharedConversationsChanged;
		NetworkChange.NetworkAvailabilityChanged += OnPinNetworkAvailabilityChanged;
		TimeChangeNotifier.DayChanged += OnDayChanged;
		PrivacyAuthorizationView.Accepted += OnPrivacyAuthorizationAccepted;
		base.Loaded += OnLoaded;
		base.SizeChanged += OnWorkspaceSizeChanged;
		UpdateConversationListState();
	}

	internal static ListCollectionView CreateConversationItemsView(IList conversations)
	{
		return new ListCollectionView(conversations)
		{
			SortDescriptions = 
			{
				new SortDescription("IsPinned", ListSortDirection.Descending),
				new SortDescription("PinSortOrder", ListSortDirection.Ascending),
				new SortDescription("PinTimeSortKey", ListSortDirection.Ascending),
				new SortDescription("SortTime", ListSortDirection.Descending)
			},
			GroupDescriptions = { (GroupDescription)new PropertyGroupDescription("GroupTitle") }
		};
	}

	internal void SetGenerating(bool generating)
	{
		_generationStateVersion++;
		bool isGenerationStateResolved = _isGenerationStateResolved;
		_isGenerationStateResolved = true;
		if (!isGenerationStateResolved || _isGenerating != generating)
		{
			_isGenerating = generating;
			_conversationActivityVersion++;
			if (_isAgentBridgeReady && !generating)
			{
				_conversationRefreshTimer.Start();
			}
		}
	}

	internal async Task SynchronizeGenerationStateAsync(Func<Task<JObject>> getState)
	{
		if (_isDisposed || !_isAgentBridgeReady)
		{
			return;
		}
		_getGenerationStateAsync = getState;
		int version = _generationStateVersion;
		try
		{
			JObject jObject = await getState();
			if (!_isDisposed && _isAgentBridgeReady && version == _generationStateVersion)
			{
				bool? flag = (bool?)jObject?["isGenerating"];
				if (flag.HasValue)
				{
					bool valueOrDefault = flag == true;
					SetGenerating(valueOrDefault);
				}
			}
		}
		catch (Exception ex)
		{
			UtilLog.Info("Agent generation state request failed: " + ex.Message);
		}
	}

	public void SetAgentBridgeReady(bool isReady)
	{
		if (_isAgentBridgeReady != isReady)
		{
			_generationStateVersion++;
			_conversationActivityVersion++;
			_isGenerationStateResolved = false;
			if (!isReady)
			{
				_isGenerating = false;
				_getGenerationStateAsync = null;
			}
		}
		_isAgentBridgeReady = isReady;
		NewConversationButton.IsEnabled = isReady && !_isPrivacyAuthorizationRequired;
		UpdateConversationInteractionState();
	}

	public void SetPrivacyAuthorizationRequired(bool isRequired)
	{
		bool flag = _isPrivacyAuthorizationRequired != isRequired;
		_isPrivacyAuthorizationRequired = isRequired;
		PrivacyAuthorizationView.Visibility = ((!isRequired) ? Visibility.Collapsed : Visibility.Visible);
		ConversationContentHost.IsEnabled = !isRequired;
		if (isRequired && flag)
		{
			_pinSyncTimer?.Stop();
			PrivacyAuthorizationView.Reset();
		}
		NewConversationButton.IsEnabled = _isAgentBridgeReady && !_isPrivacyAuthorizationRequired;
		UpdateConversationInteractionState();
		if (flag && !isRequired && ConversationContentHost.Content is AIConversationView aIConversationView)
		{
			aIConversationView.RequestComposerFocus();
		}
		if (!isRequired && base.IsLoaded)
		{
			EnsureConversationsLoadedAsync();
		}
	}

	public void SetConversationTitle(string title)
	{
		ConversationTitle.Text = (string.IsNullOrWhiteSpace(title) ? Utils.GetString("AIAgentNewChat") : title);
	}

	private void OnSharedConversationsChanged(object sender, EventArgs e)
	{
		if (sender == this)
		{
			return;
		}
		base.Dispatcher.BeginInvoke((Action)delegate
		{
			if (!_isDisposed && !_isPrivacyAuthorizationRequired)
			{
				_conversationRefreshTimer.Stop();
				_conversationRefreshTimer.Start();
			}
		});
	}

	private async void OnConversationRefreshTimerTick(object sender, EventArgs e)
	{
		if (_isDisposed || _isPrivacyAuthorizationRequired)
		{
			_conversationRefreshTimer.Stop();
		}
		else if (_renamingConversation == null && _pendingRenameRequests <= 0 && _deletingConversationIds.Count <= 0 && !_isLoadingConversations && !_isLoadingMoreConversations && !_isSwitchingConversation && !_isUpdatingPins && !_isDraggingConversations)
		{
			_conversationRefreshTimer.Stop();
			await RefreshConversationsAsync();
		}
	}

	private void SchedulePinSync(bool retry = false)
	{
		if (!_isDisposed && !_isPrivacyAuthorizationRequired && _conversationService.AutomaticallySyncPins)
		{
			if (_pinSyncTimer == null)
			{
				_pinSyncTimer = new DispatcherTimer(DispatcherPriority.Background, base.Dispatcher);
				_pinSyncTimer.Tick += OnPinSyncTimerTick;
			}
			_pinSyncTimer.Stop();
			_pinSyncTimer.Interval = TimeSpan.FromMilliseconds(retry ? 30000 : 500);
			_pinSyncTimer.Start();
		}
	}

	private void OnPinNetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
	{
		if (e.IsAvailable)
		{
			base.Dispatcher.BeginInvoke((Action)delegate
			{
				SchedulePinSync();
			});
		}
	}

	private async void OnPinSyncTimerTick(object sender, EventArgs e)
	{
		_pinSyncTimer.Stop();
		if (!_isDisposed && !_isPrivacyAuthorizationRequired)
		{
			if (_isDraggingConversations || _isUpdatingPins || _isLoadingConversations || _isSyncingPins)
			{
				SchedulePinSync();
			}
			else
			{
				await SyncConversationPinsAsync();
			}
		}
	}

	private void ApplyLocalPinState(AIConversationPinState state)
	{
		if (!state.Initialized)
		{
			return;
		}
		bool ignoreConversationSelection = _ignoreConversationSelection;
		_ignoreConversationSelection = true;
		try
		{
			foreach (AIConversationListItem item in _conversations.Where((AIConversationListItem row) => state.UnavailableIds.Contains(row.EffectiveConversationId)).ToList())
			{
				_conversations.Remove(item);
			}
			foreach (AIConversationListItem item2 in state.Items)
			{
				if (FindConversation(item2.EffectiveConversationId) == null)
				{
					_conversations.Add(item2);
				}
			}
			ApplyPinnedConversations(state.Items);
			RefreshPinView();
		}
		finally
		{
			_ignoreConversationSelection = ignoreConversationSelection;
		}
	}

	internal async Task SyncConversationPinsAsync()
	{
		if (_isDisposed || _isSyncingPins || _isPrivacyAuthorizationRequired)
		{
			return;
		}
		_isSyncingPins = true;
		int version = _pinSessionVersion;
		AIConversationPinStore store = null;
		try
		{
			store = _conversationService.PinStore;
			int editVersion = _conversationEditVersion;
			AIConversationPinState aIConversationPinState = await store.SyncAsync(_conversationService, IsCurrent);
			if (IsCurrent())
			{
				if (!_isDraggingConversations && !_isUpdatingPins && _renamingConversation == null && _pendingRenameRequests == 0 && editVersion == _conversationEditVersion)
				{
					ApplyLocalPinState(aIConversationPinState);
				}
				else
				{
					_conversationRefreshTimer.Start();
				}
				AIConversationService.NotifyConversationsChanged(this);
				if (aIConversationPinState.Pending.Count > 0)
				{
					SchedulePinSync(editVersion == _conversationEditVersion);
				}
			}
		}
		catch (Exception ex)
		{
			UtilLog.Info("Agent conversation pin sync deferred: " + ex.Message);
			if (store != null && IsCurrent())
			{
				SchedulePinSync(retry: true);
			}
		}
		finally
		{
			_isSyncingPins = false;
		}
		bool IsCurrent()
		{
			if (!_isDisposed && !_isPrivacyAuthorizationRequired && version == _pinSessionVersion)
			{
				return _conversationService.IsCurrentPinStore(store);
			}
			return false;
		}
	}

	public async Task RefreshConversationsAsync()
	{
		if (_isDisposed || _isPrivacyAuthorizationRequired || _isUpdatingPins || _isDraggingConversations)
		{
			return;
		}
		_hasRequestedInitialLoad = true;
		int loadVersion = ++_conversationLoadVersion;
		int editVersion = _conversationEditVersion;
		_isLoadingConversations = true;
		_isLoadingMoreConversations = false;
		_hasMoreConversations = false;
		_loadedServerConversationCount = 0;
		_conversationLoadFailed = false;
		UpdateConversationListState();
		AIConversationPinStore pinStore;
		try
		{
			pinStore = _conversationService.PinStore;
			AIConversationPinState aIConversationPinState = await TryReadLocalPinStateAsync(pinStore);
			if (!IsPinCurrent())
			{
				return;
			}
			if (_isSwitchingConversation || _isUpdatingPins || _isDraggingConversations || _renamingConversation != null || _pendingRenameRequests > 0 || editVersion != _conversationEditVersion)
			{
				_conversationRefreshTimer.Start();
				return;
			}
			if (aIConversationPinState != null)
			{
				ApplyLocalPinState(aIConversationPinState);
				if (aIConversationPinState.Pending.Count > 0)
				{
					SchedulePinSync();
				}
			}
			if (_isAgentBridgeReady && !_isGenerationStateResolved && _getGenerationStateAsync != null)
			{
				await SynchronizeGenerationStateAsync(_getGenerationStateAsync);
			}
			if (_isDisposed || _isPrivacyAuthorizationRequired || loadVersion != _conversationLoadVersion)
			{
				return;
			}
			int activityVersion = _conversationActivityVersion;
			string refreshedConversationId = _currentConversationId;
			bool wasLocalOnly = _conversations.Any((AIConversationListItem item) => item.IsLocalOnly && string.Equals(item.EffectiveConversationId, refreshedConversationId, StringComparison.Ordinal));
			Task<AIConversationListResponse> pageTask = _conversationService.GetConversationsAsync();
			Task<AIConversationPinState> pinnedTask = pinStore.RefreshAsync(_conversationService, IsPinCurrent);
			await Task.WhenAll(pageTask, pinnedTask);
			AIConversationListResponse response = await pageTask;
			AIConversationPinState pinState = await pinnedTask;
			if (_isDisposed || loadVersion != _conversationLoadVersion || !IsPinCurrent())
			{
				return;
			}
			Func<bool> isCurrent = () => !_isDisposed && !_isPrivacyAuthorizationRequired && loadVersion == _conversationLoadVersion && editVersion == _conversationEditVersion && activityVersion == _conversationActivityVersion && !_isGenerating && _isAgentBridgeReady && _isGenerationStateResolved && !_isSwitchingConversation && _renamingConversation == null && _pendingRenameRequests == 0;
			HashSet<string> missingLocalIds = await _conversationService.GetMissingLocalConversationIdsAsync(_conversations, response, isCurrent);
			if (_isDisposed || _isPrivacyAuthorizationRequired || loadVersion != _conversationLoadVersion)
			{
				return;
			}
			if (!pinState.IsTransient)
			{
				pinState = (await TryReadLocalPinStateAsync(pinStore)) ?? pinState;
			}
			if (!IsPinCurrent())
			{
				return;
			}
			if (_isSwitchingConversation || _isUpdatingPins || _isDraggingConversations || _renamingConversation != null || _pendingRenameRequests > 0 || activityVersion != _conversationActivityVersion || editVersion != _conversationEditVersion)
			{
				_conversationRefreshTimer.Start();
				return;
			}
			_ignoreConversationSelection = true;
			try
			{
				List<AIConversationListItem> list = _conversations.Where((AIConversationListItem item) => item.IsLocalOnly && !missingLocalIds.Contains(item.EffectiveConversationId) && response.Items.All((AIConversationListItem serverItem) => !string.Equals(serverItem?.EffectiveConversationId, item.EffectiveConversationId, StringComparison.Ordinal))).ToList();
				_conversations.Clear();
				LoadedConversationIds.Clear();
				foreach (AIConversationListItem item in list)
				{
					_conversations.Add(item);
				}
				foreach (AIConversationListItem item2 in response.Items.Where((AIConversationListItem item) => !string.IsNullOrWhiteSpace(item?.EffectiveConversationId)))
				{
					item2.IsLocalOnly = false;
					if (LoadedConversationIds.Add(item2.EffectiveConversationId))
					{
						_conversations.Add(item2);
					}
				}
				foreach (AIConversationListItem item3 in pinState.Items)
				{
					if (FindConversation(item3.EffectiveConversationId) == null)
					{
						_conversations.Add(item3);
					}
				}
				ApplyPinnedConversations(pinState.Items);
				_loadedServerConversationCount = response.Items.Count;
				_hasMoreConversations = _loadedServerConversationCount < response.Total && response.Items.Count > 0;
				_conversationView.Refresh();
				SelectCurrentConversation();
				RefreshCurrentConversationTitle(response.Items.Concat(pinState.Items));
			}
			finally
			{
				_ignoreConversationSelection = false;
			}
			Func<bool> isCurrent2 = () => !_isDisposed && !_isPrivacyAuthorizationRequired && loadVersion == _conversationLoadVersion && IsCurrentConversation(refreshedConversationId);
			bool flag = missingLocalIds.Contains(_currentConversationId);
			if (!flag)
			{
				bool flag2 = !string.IsNullOrWhiteSpace(refreshedConversationId) && isCurrent2() && !wasLocalOnly && !pinState.Items.Any((AIConversationListItem item) => item.IsPinned && item.EffectiveConversationId == refreshedConversationId);
				if (flag2)
				{
					flag2 = await _conversationService.IsConversationMissingAsync(refreshedConversationId, response, isCurrent2);
				}
				flag = flag2 && isCurrent2();
			}
			if (flag)
			{
				_currentConversationId = null;
				SetConversationTitle(null);
				this.CurrentConversationTitleChanged?.Invoke(null);
				HideDetail();
				SelectCurrentConversation();
				this.NewConversationRequested?.Invoke(this, EventArgs.Empty);
			}
		}
		catch (Exception ex)
		{
			if (!_isDisposed && loadVersion == _conversationLoadVersion)
			{
				_conversationLoadFailed = true;
				UtilLog.Info("Agent conversations request failed: " + ex.Message);
				SchedulePinSync(retry: true);
			}
		}
		finally
		{
			if (!_isDisposed && loadVersion == _conversationLoadVersion)
			{
				_isLoadingConversations = false;
				UpdateConversationListState();
			}
		}
		bool IsPinCurrent()
		{
			if (!_isDisposed && !_isPrivacyAuthorizationRequired && loadVersion == _conversationLoadVersion)
			{
				return _conversationService.IsCurrentPinStore(pinStore);
			}
			return false;
		}
	}

	private async Task<AIConversationPinState> TryReadLocalPinStateAsync(AIConversationPinStore store)
	{
		try
		{
			return await store.ReadAsync();
		}
		catch (Exception ex)
		{
			UtilLog.Info("Agent conversation pin cache read failed: " + ex.Message);
			return null;
		}
	}

	internal void RefreshCurrentConversationTitle(IEnumerable<AIConversationListItem> conversations)
	{
		if (!_isDisposed && !string.IsNullOrWhiteSpace(_currentConversationId) && !IsCurrentConversation(_renamingConversation?.EffectiveConversationId))
		{
			AIConversationListItem aIConversationListItem = conversations?.FirstOrDefault((AIConversationListItem item) => IsCurrentConversation(item?.EffectiveConversationId));
			if (aIConversationListItem != null)
			{
				string displayTitle = aIConversationListItem.DisplayTitle;
				SetConversationTitle(displayTitle);
				this.CurrentConversationTitleChanged?.Invoke(displayTitle);
			}
		}
	}

	public Task EnsureConversationsLoadedAsync()
	{
		if (_isPrivacyAuthorizationRequired || _hasRequestedInitialLoad)
		{
			return Task.CompletedTask;
		}
		_hasRequestedInitialLoad = true;
		return RefreshConversationsAsync();
	}

	public void ResetForAccountChange()
	{
		if (!_isDisposed)
		{
			_conversationLoadVersion++;
			_conversationActivityVersion++;
			_isGenerating = false;
			_isGenerationStateResolved = false;
			_generationStateVersion++;
			_getGenerationStateAsync = null;
			_isLoadingConversations = false;
			_isLoadingMoreConversations = false;
			_hasMoreConversations = false;
			_conversationLoadFailed = false;
			_hasRequestedInitialLoad = false;
			_loadedServerConversationCount = 0;
			_isSwitchingConversation = false;
			_deletingConversationIds.Clear();
			LoadedConversationIds.Clear();
			_pinOperationVersion++;
			_pinSessionVersion++;
			_pinSyncTimer?.Stop();
			_isUpdatingPins = false;
			CloseConversationMenu();
			CancelActiveRename();
			HeaderRenamePopup.IsOpen = false;
			_ignoreConversationSelection = true;
			try
			{
				_currentConversationId = null;
				_conversations.Clear();
				_conversationView.Refresh();
				ConversationList.SelectedItem = null;
			}
			finally
			{
				_ignoreConversationSelection = false;
			}
			SetConversationTitle(null);
			HideDetail();
			UpdateConversationListState();
		}
	}

	public void SetCurrentConversation(string conversationId)
	{
		_currentConversationId = (string.IsNullOrWhiteSpace(conversationId) ? null : conversationId.Trim());
		_ignoreConversationSelection = true;
		try
		{
			SelectCurrentConversation();
		}
		finally
		{
			_ignoreConversationSelection = false;
		}
		UpdateConversationInteractionState();
	}

	public void UpsertConversation(string conversationId, string title, bool isLocalOnly)
	{
		if (!string.IsNullOrWhiteSpace(conversationId))
		{
			string text = conversationId.Trim();
			_conversationActivityVersion++;
			AIConversationListItem aIConversationListItem = FindConversation(text);
			if (aIConversationListItem == null)
			{
				aIConversationListItem = new AIConversationListItem
				{
					Id = text,
					ConversationId = text,
					Title = title,
					CreatedAt = DateTimeOffset.Now,
					UpdatedAt = DateTimeOffset.Now,
					IsLocalOnly = isLocalOnly
				};
				_conversations.Insert(0, aIConversationListItem);
			}
			else if (!string.IsNullOrWhiteSpace(title))
			{
				aIConversationListItem.Title = title;
			}
			aIConversationListItem.IsLocalOnly = isLocalOnly;
			if (isLocalOnly)
			{
				aIConversationListItem.UpdatedAt = DateTimeOffset.Now;
			}
			_conversationView.Refresh();
			SetCurrentConversation(text);
			UpdateConversationListState();
		}
	}

	public void UpdateConversationTitle(string conversationId, string title)
	{
		if (!string.IsNullOrWhiteSpace(conversationId) && !string.IsNullOrWhiteSpace(title))
		{
			AIConversationListItem aIConversationListItem = FindConversation(conversationId);
			if (aIConversationListItem != null)
			{
				aIConversationListItem.Title = title;
			}
		}
	}

	public string GetConversationTitle(string conversationId)
	{
		string text = FindConversation(conversationId)?.Title;
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		return null;
	}

	public bool IsCurrentConversation(string conversationId)
	{
		if (!string.IsNullOrWhiteSpace(conversationId))
		{
			return string.Equals(_currentConversationId, conversationId.Trim(), StringComparison.Ordinal);
		}
		return false;
	}

	internal static CustomMenuItemViewModel CreatePinMenuItem(AIConversationListItem conversation)
	{
		if (conversation == null || conversation.IsLocalOnly || string.IsNullOrWhiteSpace(conversation.EffectiveConversationId))
		{
			return null;
		}
		return new CustomMenuItemViewModel("pin", Utils.GetString(conversation.IsPinned ? "Unpin" : "Pin"), Utils.GetGeometry(conversation.IsPinned ? "Ic.BatchEdit.CancelTopmost" : "Ic.BatchEdit.Topmost"))
		{
			ShowSelected = false
		};
	}

	private void ApplyPinnedConversations(IEnumerable<AIConversationListItem> pinned)
	{
		Dictionary<string, AIConversationListItem> dictionary = pinned.Where((AIConversationListItem item) => item != null && item.IsPinned && !item.IsArchived && !string.IsNullOrWhiteSpace(item.EffectiveConversationId)).GroupBy((AIConversationListItem item) => item.EffectiveConversationId, StringComparer.Ordinal).ToDictionary((IGrouping<string, AIConversationListItem> group) => group.Key, (IGrouping<string, AIConversationListItem> group) => group.First(), StringComparer.Ordinal);
		foreach (AIConversationListItem conversation in _conversations)
		{
			if (dictionary.TryGetValue(conversation.EffectiveConversationId, out var value))
			{
				conversation.IsLocalOnly = false;
				conversation.Metadata = value.Metadata;
			}
			else
			{
				conversation.Metadata = null;
			}
		}
		foreach (AIConversationListItem value2 in dictionary.Values)
		{
			if (FindConversation(value2.EffectiveConversationId) == null)
			{
				value2.IsLocalOnly = false;
				_conversations.Add(value2);
			}
		}
	}

	private void RefreshPinView()
	{
		bool ignoreConversationSelection = _ignoreConversationSelection;
		_ignoreConversationSelection = true;
		try
		{
			_conversationView.Refresh();
			SelectCurrentConversation();
		}
		finally
		{
			_ignoreConversationSelection = ignoreConversationSelection;
		}
		this.ConversationStateChanged?.Invoke(this, EventArgs.Empty);
	}

	internal Task<bool> SetConversationPinnedAsync(AIConversationListItem conversation, bool pinned)
	{
		return UpdateConversationPinAsync(conversation, pinned, null, after: false);
	}

	internal async void ToggleConversationPin(AIConversationListItem conversation)
	{
		if (conversation != null && !conversation.IsLocalOnly)
		{
			await SetConversationPinnedAsync(conversation, !conversation.IsPinned);
		}
	}

	internal Task<bool> MoveConversationPinAsync(AIConversationListItem source, AIConversationListItem target, bool after)
	{
		if (source == null || target == null || !source.IsPinned || !target.IsPinned || source.EffectiveConversationId == target.EffectiveConversationId)
		{
			return Task.FromResult(result: false);
		}
		return UpdateConversationPinAsync(source, target.IsPinned, target, after);
	}

	internal static bool TryGetPinSortOrder(IReadOnlyList<AIConversationListItem> others, int insertIndex, out long sortOrder)
	{
		sortOrder = 0L;
		if (insertIndex < 0 || insertIndex > others.Count)
		{
			return false;
		}
		decimal? num = ((insertIndex > 0) ? new decimal?(others[insertIndex - 1].PinSortOrder) : ((decimal?)null));
		decimal? num2 = ((insertIndex < others.Count) ? new decimal?(others[insertIndex].PinSortOrder) : ((decimal?)null));
		decimal num3 = ((!num.HasValue && !num2.HasValue) ? 0m : ((!num.HasValue) ? (num2.Value - 268435456m) : ((!num2.HasValue) ? (num.Value + 268435456m) : Math.Floor((num.Value + num2.Value) / 2m))));
		if (num3 < -9223372036854775808m || num3 > 9223372036854775807m || (num.HasValue && num3 <= num.Value) || (num2.HasValue && num3 >= num2.Value))
		{
			return false;
		}
		sortOrder = (long)num3;
		return true;
	}

	private async Task<bool> UpdateConversationPinAsync(AIConversationListItem conversation, bool pinned, AIConversationListItem target, bool after)
	{
		string text = conversation?.EffectiveConversationId;
		conversation = FindConversation(text);
		if (_isDisposed || _isPrivacyAuthorizationRequired || _isUpdatingPins || conversation == null || conversation.IsLocalOnly || string.IsNullOrWhiteSpace(text) || _renamingConversation != null)
		{
			return false;
		}
		_isUpdatingPins = true;
		int version = ++_pinOperationVersion;
		_conversationEditVersion++;
		UpdateConversationInteractionState();
		AIConversationPinStore store = null;
		try
		{
			store = _conversationService.PinStore;
			AIConversationPinState aIConversationPinState = await store.ChangeAsync(conversation, pinned, target, after, _conversations, IsCurrent);
			if (!IsCurrent())
			{
				return false;
			}
			if (aIConversationPinState == null)
			{
				if (pinned && target == null)
				{
					Utils.Toast(string.Format(Utils.GetString("AIPinLimitHint"), 20));
				}
				AIConversationPinState state = await store.ReadAsync();
				if (!IsCurrent())
				{
					return false;
				}
				ApplyLocalPinState(state);
				RefreshPinView();
				return false;
			}
			ApplyLocalPinState(aIConversationPinState);
			SchedulePinSync();
			AIConversationService.NotifyConversationsChanged(this);
			return true;
		}
		catch (Exception ex)
		{
			if (!_isDisposed && version == _pinOperationVersion)
			{
				UtilLog.Info("Agent conversation pin local save failed: " + ex.Message);
				if (base.IsLoaded)
				{
					Utils.Toast(Utils.GetString("SaveFailed"));
				}
				RefreshPinView();
			}
			return false;
		}
		finally
		{
			if (!_isDisposed && version == _pinOperationVersion)
			{
				_isUpdatingPins = false;
				UpdateConversationInteractionState();
			}
		}
		bool IsCurrent()
		{
			if (!_isDisposed && version == _pinOperationVersion)
			{
				return _conversationService.IsCurrentPinStore(store);
			}
			return false;
		}
	}

	internal static void AttachConversationPinDrag(ListBox list, Func<AIWorkspaceView> getWorkspace)
	{
		DragSortBehavior.SetHandler(list, new ConversationPinDragHandler(getWorkspace));
		DragSortBehavior.SetDragCursor(list, Cursors.SizeAll);
		DragSortBehavior.SetIsEnabled(list, value: true);
	}

	public void AttachConversationContent(UIElement content)
	{
		if (content == null)
		{
			throw new ArgumentNullException("content");
		}
		if (ConversationContentHost.Content != content)
		{
			ConversationContentHost.Content = content;
		}
	}

	public void DetachConversationContent(UIElement content)
	{
		if (ConversationContentHost.Content == content)
		{
			ConversationContentHost.Content = null;
		}
	}

	public async Task<bool> ShowTaskDetailAsync(string taskId)
	{
		if (string.IsNullOrWhiteSpace(taskId))
		{
			return false;
		}
		if (_taskDetailView == null)
		{
			_taskDetailView = new TaskDetailView(Constants.DetailMode.Page)
			{
				KeepDetailOnDelete = true,
				KeepDetailOnCopy = true
			};
			_taskDetailView.TaskDeleted += OnTaskDetailDeleted;
			_taskDetailView.TaskCopied += OnTaskDetailCopied;
			_taskDetailView.ShowUndoOnTaskDeleted += OnTaskDetailDeleteRequested;
			_taskDetailView.CheckItemsDeleted += OnTaskDetailCheckItemsDeleted;
			_taskDetailView.NavigateBack += OnTaskDetailCloseRequested;
			_taskDetailView.NavigateTask += OnTaskDetailNavigateTask;
			_taskDetailView.EnterImmersive += OnTaskDetailEnterImmersive;
			_taskDetailView.Visibility = Visibility.Collapsed;
			DetailHost.Children.Add(_taskDetailView);
		}
		await _taskDetailView.TaskSelect(taskId, string.Empty);
		if (!string.Equals(_taskDetailView.TaskId, taskId, StringComparison.Ordinal))
		{
			return false;
		}
		_taskDetailView.ShowBackMenu(hideImmersive: false);
		ShowDetail(_taskDetailView);
		return true;
	}

	public bool ShowHabitDetail(string habitId)
	{
		if (string.IsNullOrWhiteSpace(habitId))
		{
			return false;
		}
		if (_habitDetailControl == null)
		{
			_habitDetailControl = new HabitDetailControl
			{
				IsIndependent = true
			};
			_habitDetailControl.OuterContainer.Margin = new Thickness(0.0, 30.0, 0.0, 0.0);
			_habitDetailControl.HideDetail += OnHabitDetailHidden;
			_habitDetailControl.Visibility = Visibility.Collapsed;
			DetailHost.Children.Add(_habitDetailControl);
		}
		_habitDetailControl.Load(habitId.Trim(), DateTime.Today, pullRemote: true, force: true, useCardStyle: true, useDefaultThemeGrayBackground: true);
		_habitDetailControl.ScrollToTop();
		_habitDetailControl.ShowBackMenu();
		ShowDetail(_habitDetailControl);
		return true;
	}

	private void ShowDetail(UIElement detail)
	{
		if (_taskDetailView != null)
		{
			_taskDetailView.Visibility = ((detail != _taskDetailView) ? Visibility.Collapsed : Visibility.Visible);
		}
		if (_habitDetailControl != null)
		{
			_habitDetailControl.Visibility = ((detail != _habitDetailControl) ? Visibility.Collapsed : Visibility.Visible);
		}
		bool flag = DetailPane.Visibility == Visibility.Visible;
		bool animate = !flag || _isFloatingDetailPaneHidden;
		SetDetailPaneFloating(ShouldFloatDetailPane(base.ActualWidth));
		if (_isDetailPaneFloating)
		{
			DetailPane.Visibility = Visibility.Visible;
			ShowFloatingDetailPane(animate);
		}
		else
		{
			DetailColumn.MinWidth = 312.0;
			if (!flag || DetailColumn.ActualWidth < 312.0)
			{
				ApplyDockedDetailPaneWidth(ResolveDockedDetailPaneWidth(base.ActualWidth, _lastDetailPaneWidth, _isConversationListVisible));
			}
			DetailDivider.Visibility = Visibility.Visible;
			DetailSeparator.Visibility = Visibility.Visible;
			DetailPane.Visibility = Visibility.Visible;
		}
		UpdateConversationListWidthConstraint();
	}

	internal void PrepareForConversationHostChange()
	{
		CloseConversationMenu();
		HeaderRenamePopup.IsOpen = false;
		if (Mouse.Captured is DependencyObject descendant && ConversationContentHost.IsAncestorOf(descendant))
		{
			Mouse.Capture(null);
		}
	}

	public void HideDetail()
	{
		if (!_isDetailPaneFloating && DetailPane.Visibility == Visibility.Visible && DetailColumn.ActualWidth >= 312.0)
		{
			_lastDetailPaneWidth = Math.Max(312.0, DetailColumn.ActualWidth);
		}
		if (DetailPane.RenderTransform is TranslateTransform translateTransform)
		{
			translateTransform.BeginAnimation(TranslateTransform.XProperty, null);
			translateTransform.X = 0.0;
		}
		DetailPane.Effect = null;
		_isFloatingDetailPaneHidden = false;
		DetailPane.Visibility = Visibility.Collapsed;
		DetailDivider.Visibility = Visibility.Collapsed;
		DetailSeparator.Visibility = Visibility.Collapsed;
		DetailColumn.MinWidth = 0.0;
		DetailColumn.Width = new GridLength(0.0);
		UpdateConversationListWidthConstraint();
	}

	public void Dispose()
	{
		if (!_isDisposed)
		{
			_isDisposed = true;
			_pinOperationVersion++;
			_pinSessionVersion++;
			if (_pinSyncTimer != null)
			{
				_pinSyncTimer.Stop();
				_pinSyncTimer.Tick -= OnPinSyncTimerTick;
			}
			_getGenerationStateAsync = null;
			AIConversationService.ConversationsChanged -= OnSharedConversationsChanged;
			NetworkChange.NetworkAvailabilityChanged -= OnPinNetworkAvailabilityChanged;
			TimeChangeNotifier.DayChanged -= OnDayChanged;
			_conversationRefreshTimer.Stop();
			_conversationRefreshTimer.Tick -= OnConversationRefreshTimerTick;
			_conversationLoadVersion++;
			base.Loaded -= OnLoaded;
			base.SizeChanged -= OnWorkspaceSizeChanged;
			CloseConversationMenu();
			CancelActiveRename();
			HeaderRenamePopup.IsOpen = false;
			if (_taskDetailView != null)
			{
				_taskDetailView.TaskDeleted -= OnTaskDetailDeleted;
				_taskDetailView.TaskCopied -= OnTaskDetailCopied;
				_taskDetailView.ShowUndoOnTaskDeleted -= OnTaskDetailDeleteRequested;
				_taskDetailView.CheckItemsDeleted -= OnTaskDetailCheckItemsDeleted;
				_taskDetailView.NavigateBack -= OnTaskDetailCloseRequested;
				_taskDetailView.NavigateTask -= OnTaskDetailNavigateTask;
				_taskDetailView.EnterImmersive -= OnTaskDetailEnterImmersive;
				_taskDetailView.Dispose();
				_taskDetailView = null;
			}
			if (_habitDetailControl != null)
			{
				_habitDetailControl.HideDetail -= OnHabitDetailHidden;
				_habitDetailControl = null;
			}
			DetailHost.Children.Clear();
		}
	}

	private void OnDayChanged(object sender, EventArgs e)
	{
		if (_isDisposed)
		{
			return;
		}
		bool ignoreConversationSelection = _ignoreConversationSelection;
		_ignoreConversationSelection = true;
		try
		{
			_conversationView.Refresh();
			SelectCurrentConversation();
		}
		finally
		{
			_ignoreConversationSelection = ignoreConversationSelection;
		}
	}

	private async void OnLoaded(object sender, RoutedEventArgs e)
	{
		UpdateConversationListWidthConstraint();
		_conversationView.Refresh();
		if (!_isPrivacyAuthorizationRequired)
		{
			await EnsureConversationsLoadedAsync();
		}
	}

	private void OnWorkspaceSizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (e.WidthChanged)
		{
			UpdateDetailPaneLayout();
			UpdateConversationListWidthConstraint();
		}
	}

	private void OnConversationListSeparatorDragStarted(object sender, DragStartedEventArgs e)
	{
		UpdateConversationListWidthConstraint();
	}

	private void OnDetailSeparatorDragCompleted(object sender, DragCompletedEventArgs e)
	{
		if (!_isDetailPaneFloating && DetailPane.Visibility == Visibility.Visible && !(ConversationColumn.ActualWidth <= 0.0) && !(DetailColumn.ActualWidth < 312.0))
		{
			_lastDetailPaneWidth = DetailColumn.ActualWidth;
			double value = DetailColumn.ActualWidth / ConversationColumn.ActualWidth;
			ConversationColumn.Width = new GridLength(1.0, GridUnitType.Star);
			DetailColumn.Width = new GridLength(value, GridUnitType.Star);
			UpdateConversationListWidthConstraint();
		}
	}

	private void UpdateConversationListWidthConstraint()
	{
		if (base.ActualWidth <= 0.0)
		{
			return;
		}
		bool flag = base.ActualWidth - GetReservedDetailPaneWidth() < 550.0;
		if (flag != _isConversationListFloating)
		{
			_conversationListAnimationVersion++;
			_isConversationListFloating = flag;
			ConversationListColumn.BeginAnimation(ColumnDefinition.MaxWidthProperty, null);
			ConversationListPane.RenderTransform = new TranslateTransform();
			Grid.SetColumnSpan(ConversationListPane, (!flag) ? 1 : 3);
			ConversationListPane.HorizontalAlignment = ((!flag) ? HorizontalAlignment.Stretch : HorizontalAlignment.Left);
			Panel.SetZIndex(ConversationListPane, flag ? 1100 : 0);
			ConversationListPane.Effect = (flag ? new DropShadowEffect
			{
				Opacity = 0.08,
				BlurRadius = 12.0,
				ShadowDepth = 4.0,
				Direction = 0.0
			} : null);
			ConversationListPane.SetResourceReference(Panel.BackgroundProperty, flag ? "FoldAreaBackground" : "ShowAreaBottomColor");
			if (flag)
			{
				_isConversationListVisible = false;
			}
			ConversationListPane.Visibility = ((!_isConversationListVisible) ? Visibility.Collapsed : Visibility.Visible);
			ConversationListToggleButton.ToolTip = Utils.GetString(_isConversationListVisible ? "Collapse" : "Expand");
			ConversationListDismissArea.Visibility = Visibility.Collapsed;
			ConversationListColumn.MinWidth = ((!flag && _isConversationListVisible) ? 230.0 : 0.0);
			ConversationListColumn.Width = new GridLength((!flag && _isConversationListVisible) ? _lastConversationListWidth : 0.0);
			ConversationListColumn.MaxWidth = ((!flag && _isConversationListVisible) ? ResolveConversationListMaximumWidth(base.ActualWidth, GetReservedDetailPaneWidth()) : 0.0);
			Border conversationListDivider = ConversationListDivider;
			Visibility visibility = (ConversationListSeparator.Visibility = ((flag || !_isConversationListVisible) ? Visibility.Collapsed : Visibility.Visible));
			conversationListDivider.Visibility = visibility;
		}
		ConversationListPane.Width = (flag ? Math.Min(_lastConversationListWidth, Math.Max(0.0, base.ActualWidth - 40.0)) : double.NaN);
		if (!flag && _isConversationListVisible)
		{
			ConversationListColumn.MaxWidth = ResolveConversationListMaximumWidth(base.ActualWidth, GetReservedDetailPaneWidth());
		}
	}

	private void UpdateDetailPaneLayout()
	{
		if (DetailPane.Visibility != Visibility.Visible)
		{
			return;
		}
		bool flag = ShouldFloatDetailPane(base.ActualWidth);
		if (flag != _isDetailPaneFloating)
		{
			SetDetailPaneFloating(flag);
			if (_isDetailPaneFloating)
			{
				HideFloatingDetailPane();
			}
		}
	}

	private void SetDetailPaneFloating(bool floating)
	{
		_habitDetailControl?.SetSlideOutHost(floating);
		if (_isDetailPaneFloating == floating)
		{
			return;
		}
		if (floating)
		{
			if (DetailColumn.ActualWidth >= 312.0)
			{
				_lastDetailPaneWidth = DetailColumn.ActualWidth;
			}
			_isDetailPaneFloating = true;
			DetailColumn.MinWidth = 0.0;
			DetailColumn.Width = new GridLength(0.0);
			Grid.SetColumn(DetailPane, 1);
			Grid.SetColumnSpan(DetailPane, 2);
			DetailPane.Width = 310.0;
			DetailPane.MinWidth = 310.0;
			DetailPane.MaxWidth = 310.0;
			DetailPane.HorizontalAlignment = HorizontalAlignment.Right;
			Panel.SetZIndex(DetailPane, 100);
			DetailPane.SetResourceReference(Panel.BackgroundProperty, "FoldAreaBackground");
			DetailDivider.Visibility = Visibility.Collapsed;
			DetailSeparator.Visibility = Visibility.Collapsed;
			EnsureDetailPaneTransform().X = 0.0;
			return;
		}
		_isDetailPaneFloating = false;
		_isFloatingDetailPaneHidden = false;
		TranslateTransform translateTransform = EnsureDetailPaneTransform();
		translateTransform.BeginAnimation(TranslateTransform.XProperty, null);
		translateTransform.X = 0.0;
		DetailPane.Effect = null;
		Grid.SetColumn(DetailPane, 2);
		Grid.SetColumnSpan(DetailPane, 1);
		DetailPane.Width = double.NaN;
		DetailPane.MinWidth = 0.0;
		DetailPane.MaxWidth = double.PositiveInfinity;
		DetailPane.HorizontalAlignment = HorizontalAlignment.Stretch;
		Panel.SetZIndex(DetailPane, 0);
		DetailPane.SetResourceReference(Panel.BackgroundProperty, "ShowAreaBottomColor");
		if (DetailPane.Visibility == Visibility.Visible)
		{
			DetailColumn.MinWidth = 312.0;
			ApplyDockedDetailPaneWidth(ResolveDockedDetailPaneWidth(base.ActualWidth, _lastDetailPaneWidth, _isConversationListVisible));
			DetailDivider.Visibility = Visibility.Visible;
			DetailSeparator.Visibility = Visibility.Visible;
		}
	}

	private void ApplyDockedDetailPaneWidth(double detailPaneWidth)
	{
		double val = ((!_isConversationListVisible) ? 0.0 : ((ConversationListColumn.ActualWidth >= 230.0) ? ConversationListColumn.ActualWidth : _lastConversationListWidth));
		double val2 = ResolveConversationListMaximumWidth(base.ActualWidth, detailPaneWidth);
		val = Math.Min(val, val2);
		double num = Math.Max(320.0, base.ActualWidth - val - detailPaneWidth);
		double value = detailPaneWidth / num;
		ConversationColumn.Width = new GridLength(1.0, GridUnitType.Star);
		DetailColumn.Width = new GridLength(value, GridUnitType.Star);
	}

	private void ShowFloatingDetailPane(bool animate)
	{
		_isFloatingDetailPaneHidden = false;
		DetailDivider.Visibility = Visibility.Collapsed;
		DetailSeparator.Visibility = Visibility.Collapsed;
		if (DetailPane.Effect == null)
		{
			DetailPane.Effect = new DropShadowEffect
			{
				Opacity = 0.08,
				BlurRadius = 12.0,
				ShadowDepth = 4.0,
				Direction = 180.0
			};
		}
		TranslateTransform translateTransform = EnsureDetailPaneTransform();
		translateTransform.BeginAnimation(TranslateTransform.XProperty, null);
		if (!animate)
		{
			translateTransform.X = 0.0;
			return;
		}
		translateTransform.X = 310.0;
		translateTransform.BeginAnimation(TranslateTransform.XProperty, AnimationUtils.GetDoubleAnimation(310.0, 0.0, 240));
	}

	private void HideFloatingDetailPane()
	{
		_isFloatingDetailPaneHidden = true;
		DetailPane.Effect = null;
		TranslateTransform translateTransform = EnsureDetailPaneTransform();
		translateTransform.BeginAnimation(TranslateTransform.XProperty, null);
		translateTransform.BeginAnimation(TranslateTransform.XProperty, AnimationUtils.GetDoubleAnimation(translateTransform.X, 310.0, 240));
	}

	private TranslateTransform EnsureDetailPaneTransform()
	{
		if (DetailPane.RenderTransform is TranslateTransform result)
		{
			return result;
		}
		TranslateTransform translateTransform = new TranslateTransform();
		DetailPane.RenderTransform = translateTransform;
		return translateTransform;
	}

	private double GetReservedDetailPaneWidth()
	{
		if (_isDetailPaneFloating || DetailPane.Visibility != Visibility.Visible)
		{
			return 0.0;
		}
		if (!(DetailColumn.ActualWidth >= 312.0))
		{
			return ResolveDockedDetailPaneWidth(base.ActualWidth, _lastDetailPaneWidth, _isConversationListVisible);
		}
		return DetailColumn.ActualWidth;
	}

	internal static bool ShouldFloatDetailPane(double workspaceWidth)
	{
		if (workspaceWidth > 0.0)
		{
			return workspaceWidth < 896.0;
		}
		return false;
	}

	internal static double ResolveDockedDetailPaneWidth(double workspaceWidth, double preferredDetailPaneWidth, bool isConversationListVisible)
	{
		double num = (isConversationListVisible ? 230.0 : 0.0);
		double val = Math.Max(0.0, workspaceWidth) - num - 320.0;
		return Math.Max(312.0, Math.Min(Math.Max(312.0, preferredDetailPaneWidth), val));
	}

	internal static double ResolveConversationListMaximumWidth(double workspaceWidth, double detailPaneWidth)
	{
		double val = Math.Max(0.0, workspaceWidth) - Math.Max(0.0, detailPaneWidth) - 320.0;
		return Math.Max(230.0, Math.Min(600.0, val));
	}

	private void OnPrivacyAuthorizationAccepted(object sender, EventArgs e)
	{
		this.PrivacyAuthorizationAccepted?.Invoke(this, EventArgs.Empty);
	}

	private async void OnConversationListScrollChanged(object sender, ScrollChangedEventArgs e)
	{
		if (e.OriginalSource is ScrollViewer scrollViewer && !(scrollViewer.VerticalOffset < scrollViewer.ScrollableHeight - 24.0) && (!(e.VerticalChange <= 0.0) || !(e.ExtentHeightChange <= 0.0)))
		{
			await LoadMoreConversationsAsync();
		}
	}

	internal async Task LoadMoreConversationsAsync()
	{
		if (_isDisposed || _isUpdatingPins || _isDraggingConversations || _isLoadingConversations || _isLoadingMoreConversations || !_hasMoreConversations)
		{
			return;
		}
		int loadVersion = _conversationLoadVersion;
		int editVersion = _conversationEditVersion;
		int loadedServerConversationCount = _loadedServerConversationCount;
		_isLoadingMoreConversations = true;
		UpdateConversationListState();
		try
		{
			AIConversationListResponse aIConversationListResponse = await _conversationService.GetConversationsAsync(loadedServerConversationCount);
			if (_isDisposed || loadVersion != _conversationLoadVersion || editVersion != _conversationEditVersion)
			{
				return;
			}
			_ignoreConversationSelection = true;
			try
			{
				foreach (AIConversationListItem item in aIConversationListResponse.Items.Where((AIConversationListItem item) => !string.IsNullOrWhiteSpace(item?.EffectiveConversationId)))
				{
					item.IsLocalOnly = false;
					AIConversationListItem aIConversationListItem = FindConversation(item.EffectiveConversationId);
					if (aIConversationListItem == null)
					{
						_conversations.Add(item);
					}
					else if (aIConversationListItem.IsLocalOnly)
					{
						_conversations.Remove(aIConversationListItem);
						_conversations.Add(item);
					}
					else
					{
						aIConversationListItem.Title = item.Title;
						aIConversationListItem.CreatedAt = item.CreatedAt;
						aIConversationListItem.UpdatedAt = item.UpdatedAt;
					}
					LoadedConversationIds.Add(item.EffectiveConversationId);
				}
				_loadedServerConversationCount += aIConversationListResponse.Items.Count;
				_hasMoreConversations = aIConversationListResponse.Items.Count > 0 && _loadedServerConversationCount < aIConversationListResponse.Total;
				_conversationView.Refresh();
				SelectCurrentConversation();
				RefreshCurrentConversationTitle(aIConversationListResponse.Items);
			}
			finally
			{
				_ignoreConversationSelection = false;
			}
		}
		catch (Exception ex)
		{
			if (!_isDisposed && loadVersion == _conversationLoadVersion)
			{
				UtilLog.Info("Agent conversations next page request failed: " + ex.Message);
			}
		}
		finally
		{
			if (!_isDisposed && loadVersion == _conversationLoadVersion)
			{
				_isLoadingMoreConversations = false;
				UpdateConversationListState();
			}
		}
	}

	private async void OnConversationSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (!_ignoreConversationSelection && !_isSwitchingConversation && (!_isConversationListFloating || (!DragSortBehavior.IsDragPending(ConversationList) && !DragSortBehavior.GetIsDragging(ConversationList))) && ConversationList.SelectedItem is AIConversationListItem selected)
		{
			await ActivateConversationFromListAsync(selected);
		}
	}

	private async Task ActivateConversationFromListAsync(AIConversationListItem selected)
	{
		if (!(await SwitchConversationAsync(selected)))
		{
			RestoreCurrentSelection();
		}
		else if (!_isDisposed && _isConversationListFloating)
		{
			SetConversationListVisible(show: false);
		}
	}

	private async void OnConversationItemMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (_isConversationListFloating && !_ignoreConversationSelection && !_isSwitchingConversation && !_isDraggingConversations && _renamingConversation == null)
		{
			DependencyObject dependencyObject;
			for (dependencyObject = e.OriginalSource as DependencyObject; dependencyObject is ContentElement contentElement; dependencyObject = ContentOperations.GetParent(contentElement) ?? (contentElement as FrameworkContentElement)?.Parent)
			{
			}
			if (dependencyObject is Visual && !(dependencyObject is ButtonBase) && Utils.FindParent<ButtonBase>(dependencyObject) == null && (ItemsControl.ContainerFromElement(ConversationList, dependencyObject) as ListBoxItem)?.DataContext is AIConversationListItem selected)
			{
				await ActivateConversationFromListAsync(selected);
			}
		}
	}

	internal async Task<bool> SwitchConversationAsync(AIConversationListItem conversation)
	{
		string conversationId = conversation?.EffectiveConversationId;
		if (string.IsNullOrWhiteSpace(conversationId))
		{
			return false;
		}
		if (string.Equals(_currentConversationId, conversationId, StringComparison.Ordinal))
		{
			return true;
		}
		Func<string, string, Task<bool>> func = this.ConversationSelected;
		if (!CanSwitchConversation || func == null)
		{
			return false;
		}
		_isSwitchingConversation = true;
		_conversationActivityVersion++;
		UpdateConversationInteractionState();
		try
		{
			if (!(await func(conversationId, conversation.DisplayTitle)))
			{
				return false;
			}
			_currentConversationId = conversationId;
			RestoreCurrentSelection();
			return true;
		}
		catch (Exception ex)
		{
			UtilLog.Info("Agent conversation switch failed: " + ex.Message);
			return false;
		}
		finally
		{
			_isSwitchingConversation = false;
			UpdateConversationInteractionState();
		}
	}

	private async void OnRetryConversationsClick(object sender, RoutedEventArgs e)
	{
		await RefreshConversationsAsync();
	}

	private void OnConversationItemMoreClick(object sender, RoutedEventArgs e)
	{
		e.Handled = true;
		if (sender is FrameworkElement { DataContext: AIConversationListItem dataContext } frameworkElement && !ConsumeConversationMenuButtonClick(frameworkElement))
		{
			ShowConversationMenu(frameworkElement, dataContext, renameInHeader: false);
		}
	}

	private void OnConversationItemMouseRightButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (e.OriginalSource is DependencyObject element && ItemsControl.ContainerFromElement(ConversationList, element) is ListBoxItem { DataContext: AIConversationListItem { IsEditing: false } dataContext } listBoxItem)
		{
			e.Handled = true;
			Mouse.Capture(null);
			ShowConversationMenu(listBoxItem, dataContext, renameInHeader: false, openAtMouse: true);
		}
	}

	private void OnCurrentConversationMoreClick(object sender, RoutedEventArgs e)
	{
		e.Handled = true;
		AIConversationListItem aIConversationListItem = FindConversation(_currentConversationId);
		if (aIConversationListItem != null && sender is FrameworkElement frameworkElement && !ConsumeConversationMenuButtonClick(frameworkElement))
		{
			ShowConversationMenu(frameworkElement, aIConversationListItem, renameInHeader: true);
		}
	}

	private bool ConsumeConversationMenuButtonClick(FrameworkElement element)
	{
		EscPopup conversationMenuPopup = _conversationMenuPopup;
		int num;
		if (_conversationMenuButtonPressedWhileOpen != element)
		{
			if (conversationMenuPopup != null && conversationMenuPopup.IsOpen)
			{
				num = ((conversationMenuPopup.PlacementTarget == element) ? 1 : 0);
				if (num != 0)
				{
					goto IL_002d;
				}
			}
			else
			{
				num = 0;
			}
			goto IL_003f;
		}
		num = 1;
		goto IL_002d;
		IL_003f:
		ClearConversationMenuButtonSuppression();
		return (byte)num != 0;
		IL_002d:
		if (conversationMenuPopup != null && conversationMenuPopup.IsOpen)
		{
			conversationMenuPopup.IsOpen = false;
		}
		goto IL_003f;
	}

	private void OnConversationMenuPreProcessInput(object sender, PreProcessInputEventArgs e)
	{
		if (e.StagingItem.Input is MouseButtonEventArgs e2 && (e2.RoutedEvent == Mouse.PreviewMouseDownEvent || e2.RoutedEvent == Mouse.PreviewMouseDownOutsideCapturedElementEvent) && e2.ChangedButton == MouseButton.Left)
		{
			_conversationMenuButtonPressedWhileOpen = null;
			EscPopup conversationMenuPopup = _conversationMenuPopup;
			if (conversationMenuPopup != null && conversationMenuPopup.IsOpen && conversationMenuPopup.PlacementTarget is Button button && IsMouseWithin(button, e2))
			{
				_conversationMenuButtonPressedWhileOpen = button;
				InputManager.Current.PostProcessInput -= OnConversationMenuPostProcessInput;
				InputManager.Current.PostProcessInput += OnConversationMenuPostProcessInput;
			}
		}
	}

	private void OnConversationMenuPostProcessInput(object sender, ProcessInputEventArgs e)
	{
		if (e.StagingItem.Input is MouseButtonEventArgs e2 && e2.RoutedEvent == Mouse.MouseUpEvent && e2.ChangedButton == MouseButton.Left)
		{
			ClearConversationMenuButtonSuppression();
		}
	}

	private void ClearConversationMenuButtonSuppression()
	{
		InputManager.Current.PostProcessInput -= OnConversationMenuPostProcessInput;
		_conversationMenuButtonPressedWhileOpen = null;
	}

	private static bool IsMouseWithin(FrameworkElement element, MouseEventArgs mouseEvent)
	{
		if (element == null || element.ActualWidth <= 0.0 || element.ActualHeight <= 0.0)
		{
			return false;
		}
		Point position = mouseEvent.GetPosition(element);
		if (position.X >= 0.0 && position.X <= element.ActualWidth && position.Y >= 0.0)
		{
			return position.Y <= element.ActualHeight;
		}
		return false;
	}

	private void CloseConversationMenu()
	{
		InputManager.Current.PreProcessInput -= OnConversationMenuPreProcessInput;
		ClearConversationMenuButtonSuppression();
		EscPopup conversationMenuPopup = _conversationMenuPopup;
		_conversationMenuPopup = null;
		if (conversationMenuPopup != null && conversationMenuPopup.IsOpen)
		{
			conversationMenuPopup.IsOpen = false;
		}
	}

	internal static double ResolveHeaderRenamePopupHorizontalOffset(double targetWidth, double popupContentWidth)
	{
		return Math.Max(0.0, targetWidth) - Math.Max(0.0, popupContentWidth) + 10.0 + 2.0;
	}

	private void ShowConversationMenu(FrameworkElement placementTarget, AIConversationListItem conversation, bool renameInHeader, bool openAtMouse = false)
	{
		InputManager.Current.PreProcessInput -= OnConversationMenuPreProcessInput;
		ClearConversationMenuButtonSuppression();
		EscPopup conversationMenuPopup = _conversationMenuPopup;
		if (conversationMenuPopup != null && conversationMenuPopup.IsOpen)
		{
			_conversationMenuPopup.IsOpen = false;
		}
		EscPopup popup = new EscPopup
		{
			Placement = (openAtMouse ? PlacementMode.Mouse : PlacementMode.Bottom),
			PlacementTarget = placementTarget,
			HorizontalOffset = (openAtMouse ? (-5) : (-140)),
			VerticalOffset = (openAtMouse ? (-5.0) : (renameInHeader ? (-4.0) : 4.0)),
			StaysOpen = false,
			PopupAnimation = PopupAnimation.Fade,
			RestoreFocus = false
		};
		_conversationMenuPopup = popup;
		InputManager.Current.PreProcessInput += OnConversationMenuPreProcessInput;
		List<CustomMenuItemViewModel> list = new List<CustomMenuItemViewModel>
		{
			new CustomMenuItemViewModel("rename", Utils.GetString("Rename"), Utils.GetGeometry("Ic.CalendarOM.Edit"))
			{
				ShowSelected = false
			},
			new CustomMenuItemViewModel("delete", Utils.GetString("Delete"), Utils.GetGeometry("Ic.ProjectList.Delete"))
			{
				IsDelete = true,
				ShowSelected = false
			}
		};
		CustomMenuItemViewModel customMenuItemViewModel = CreatePinMenuItem(conversation);
		if (customMenuItemViewModel != null)
		{
			list.Insert(1, customMenuItemViewModel);
		}
		CustomMenuList obj = new CustomMenuList(list, popup)
		{
			MinWidth = 144.0
		};
		if (!renameInHeader)
		{
			conversation.IsMenuOpen = true;
		}
		obj.Closed += delegate
		{
			if (!renameInHeader)
			{
				conversation.IsMenuOpen = false;
			}
			if (_conversationMenuPopup == popup)
			{
				InputManager.Current.PreProcessInput -= OnConversationMenuPreProcessInput;
				_conversationMenuPopup = null;
			}
		};
		obj.Operated += delegate(object menuSender, object action)
		{
			if (object.Equals(action, "rename"))
			{
				base.Dispatcher.BeginInvoke(DispatcherPriority.Input, (Action)delegate
				{
					BeginRename(conversation, renameInHeader);
				});
			}
			else if (object.Equals(action, "pin"))
			{
				ToggleConversationPin(conversation);
			}
			else if (object.Equals(action, "delete"))
			{
				DeleteConversationAsync(conversation, Window.GetWindow(this));
			}
		};
		obj.Show();
	}

	private void BeginRename(AIConversationListItem conversation, bool inHeader)
	{
		if (conversation != null && !_deletingConversationIds.Contains(conversation.EffectiveConversationId))
		{
			CancelActiveRename();
			_renamingConversation = conversation;
			_conversationEditVersion++;
			_renameOriginalTitle = conversation.Title;
			_renameInitialText = conversation.DisplayTitle;
			_isHeaderRename = inHeader;
			conversation.Title = _renameInitialText;
			if (inHeader)
			{
				HeaderRenameTextBox.Text = _renameInitialText;
				double targetWidth = ((CurrentConversationMoreButton.ActualWidth > 0.0) ? CurrentConversationMoreButton.ActualWidth : CurrentConversationMoreButton.Width);
				HeaderRenamePopup.HorizontalOffset = ResolveHeaderRenamePopupHorizontalOffset(targetWidth, HeaderRenamePopupContent.Width);
				HeaderRenamePopup.VerticalOffset = -4.0;
				HeaderRenamePopup.IsOpen = true;
			}
			else
			{
				conversation.IsEditing = true;
			}
		}
	}

	private void OnHeaderRenamePopupOpened(object sender, EventArgs e)
	{
		AIConversationListItem conversation = _renamingConversation;
		base.Dispatcher.BeginInvoke(DispatcherPriority.Input, (Action)delegate
		{
			if (HeaderRenamePopup.IsOpen && _isHeaderRename && conversation != null && conversation == _renamingConversation)
			{
				HwndHelper.SetFocus(HeaderRenamePopup);
				HeaderRenameTextBox.Focus();
				HeaderRenameTextBox.SelectAll();
			}
		});
	}

	private void OnInlineRenameTextBoxIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		AIConversationRenameEditor textBox = sender as AIConversationRenameEditor;
		if (textBox == null)
		{
			return;
		}
		object dataContext = textBox.DataContext;
		AIConversationListItem conversation = dataContext as AIConversationListItem;
		if (conversation == null || !conversation.IsEditing || conversation != _renamingConversation)
		{
			return;
		}
		base.Dispatcher.BeginInvoke(DispatcherPriority.Input, (Action)delegate
		{
			if (textBox.IsVisible && conversation == _renamingConversation)
			{
				bool ignoreConversationSelection = _ignoreConversationSelection;
				_ignoreConversationSelection = true;
				try
				{
					textBox.Text = conversation.Title;
					textBox.Focus();
					textBox.SelectAll();
					SelectCurrentConversation();
				}
				finally
				{
					_ignoreConversationSelection = ignoreConversationSelection;
				}
			}
		});
	}

	private void OnInlineRenameTextChanged(object sender, EventArgs e)
	{
		if (sender is AIConversationRenameEditor { DataContext: AIConversationListItem dataContext } aIConversationRenameEditor && dataContext == _renamingConversation)
		{
			PreviewConversationTitle(dataContext, aIConversationRenameEditor.Text);
		}
	}

	private async void OnInlineRenamePreviewKeyDown(object sender, KeyEventArgs e)
	{
		if (!((sender as AIConversationRenameEditor)?.IsTextCompositionActive ?? false) && sender is AIConversationRenameEditor { DataContext: AIConversationListItem dataContext } aIConversationRenameEditor)
		{
			if (e.Key == Key.Return)
			{
				e.Handled = true;
				await FinishRenameAsync(dataContext, aIConversationRenameEditor.Text, save: true);
			}
			else if (e.Key == Key.Escape)
			{
				e.Handled = true;
				await FinishRenameAsync(dataContext, aIConversationRenameEditor.Text, save: false);
			}
		}
	}

	private async void OnWorkspacePreviewMouseDown(object sender, MouseButtonEventArgs e)
	{
		AIConversationRenameEditor aIConversationRenameEditor = AIConversationRenameEditor.FromFocusedElement();
		if (!_isHeaderRename && _renamingConversation != null && aIConversationRenameEditor != null && aIConversationRenameEditor.DataContext == _renamingConversation && !IsMouseWithin((FrameworkElement)(((object)(aIConversationRenameEditor.Parent as Border)) ?? ((object)aIConversationRenameEditor)), e))
		{
			await FinishRenameAsync(_renamingConversation, aIConversationRenameEditor.Text, save: true);
		}
	}

	private async void OnInlineRenameLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
	{
		if (sender is AIConversationRenameEditor { IsKeyboardFocusWithin: false, DataContext: AIConversationListItem dataContext } aIConversationRenameEditor)
		{
			await FinishRenameAsync(dataContext, aIConversationRenameEditor.Text, save: true);
		}
	}

	private void OnHeaderRenameTextChanged(object sender, EventArgs e)
	{
		if (_isHeaderRename && _renamingConversation != null)
		{
			PreviewConversationTitle(_renamingConversation, HeaderRenameTextBox.Text);
		}
	}

	private async void OnHeaderRenamePreviewKeyDown(object sender, KeyEventArgs e)
	{
		if (!HeaderRenameTextBox.IsTextCompositionActive && _renamingConversation != null && _isHeaderRename)
		{
			if (e.Key == Key.Return)
			{
				e.Handled = true;
				await FinishRenameAsync(_renamingConversation, HeaderRenameTextBox.Text, save: true);
			}
			else if (e.Key == Key.Escape)
			{
				e.Handled = true;
				await FinishRenameAsync(_renamingConversation, HeaderRenameTextBox.Text, save: false);
			}
		}
	}

	private async void OnHeaderRenamePopupClosed(object sender, EventArgs e)
	{
		if (_renamingConversation != null && _isHeaderRename)
		{
			await FinishRenameAsync(_renamingConversation, HeaderRenameTextBox.Text, save: true);
		}
	}

	private void PreviewConversationTitle(AIConversationListItem conversation, string title)
	{
		conversation.Title = title;
		if (IsCurrentConversation(conversation.EffectiveConversationId))
		{
			this.CurrentConversationTitleChanged?.Invoke(title);
			ConversationTitle.Text = title ?? string.Empty;
		}
	}

	private async Task FinishRenameAsync(AIConversationListItem conversation, string candidate, bool save)
	{
		if (conversation != _renamingConversation)
		{
			return;
		}
		string renameOriginalTitle = _renameOriginalTitle;
		string renameInitialText = _renameInitialText;
		bool isHeaderRename = _isHeaderRename;
		_renamingConversation = null;
		_renameOriginalTitle = null;
		_renameInitialText = null;
		_isHeaderRename = false;
		if (isHeaderRename)
		{
			if (HeaderRenamePopup.IsOpen)
			{
				HeaderRenamePopup.IsOpen = false;
			}
		}
		else
		{
			EndInlineRenameEditing(conversation);
		}
		string text = candidate?.Trim();
		if (!save || string.IsNullOrEmpty(text) || string.Equals(text, renameInitialText, StringComparison.Ordinal))
		{
			ApplyConversationTitle(conversation, renameOriginalTitle);
		}
		else
		{
			await RenameConversationAsync(conversation, text, renameOriginalTitle);
		}
	}

	internal Task<bool> RenameConversationAsync(AIConversationListItem conversation, string candidate)
	{
		return RenameConversationAsync(conversation, candidate, conversation?.Title);
	}

	private async Task<bool> RenameConversationAsync(AIConversationListItem conversation, string candidate, string originalTitle)
	{
		if (conversation == null || _deletingConversationIds.Contains(conversation.EffectiveConversationId))
		{
			return false;
		}
		string normalizedTitle = candidate?.Trim();
		if (string.IsNullOrEmpty(normalizedTitle))
		{
			return false;
		}
		if (string.Equals(normalizedTitle, originalTitle?.Trim(), StringComparison.Ordinal))
		{
			return true;
		}
		ApplyConversationTitle(conversation, normalizedTitle);
		_conversationEditVersion++;
		_pendingRenameRequests++;
		try
		{
			AIConversationListItem aIConversationListItem = await _conversationService.UpdateConversationTitleAsync(conversation.EffectiveConversationId, normalizedTitle);
			string title = (string.IsNullOrWhiteSpace(aIConversationListItem?.Title) ? normalizedTitle : aIConversationListItem.Title.Trim());
			conversation.UpdatedAt = aIConversationListItem?.UpdatedAt ?? conversation.UpdatedAt;
			conversation.IsLocalOnly = false;
			ApplyConversationTitle(conversation, title);
			_conversationView.Refresh();
			SelectCurrentConversation();
			AIConversationService.NotifyConversationsChanged(this);
			return true;
		}
		catch (Exception ex)
		{
			ApplyConversationTitle(conversation, originalTitle);
			Utils.Toast(Utils.GetString("LoadFailed"));
			UtilLog.Info("Agent conversation rename failed: " + ex.Message);
			return false;
		}
		finally
		{
			_pendingRenameRequests--;
		}
	}

	private void ApplyConversationTitle(AIConversationListItem conversation, string title)
	{
		conversation.Title = title;
		if (IsCurrentConversation(conversation.EffectiveConversationId))
		{
			string displayTitle = conversation.DisplayTitle;
			ConversationTitle.Text = displayTitle;
			this.CurrentConversationTitleChanged?.Invoke(displayTitle);
		}
		this.ConversationStateChanged?.Invoke(this, EventArgs.Empty);
	}

	private void CancelActiveRename()
	{
		if (_renamingConversation == null)
		{
			return;
		}
		AIConversationListItem renamingConversation = _renamingConversation;
		string renameOriginalTitle = _renameOriginalTitle;
		bool isHeaderRename = _isHeaderRename;
		_renamingConversation = null;
		_renameOriginalTitle = null;
		_renameInitialText = null;
		_isHeaderRename = false;
		if (isHeaderRename)
		{
			if (HeaderRenamePopup.IsOpen)
			{
				HeaderRenamePopup.IsOpen = false;
			}
		}
		else
		{
			EndInlineRenameEditing(renamingConversation);
		}
		ApplyConversationTitle(renamingConversation, renameOriginalTitle);
	}

	private void EndInlineRenameEditing(AIConversationListItem conversation)
	{
		bool ignoreConversationSelection = _ignoreConversationSelection;
		_ignoreConversationSelection = true;
		try
		{
			ConversationList.Focus();
			Keyboard.Focus(ConversationList);
			conversation.IsEditing = false;
			SelectCurrentConversation();
		}
		finally
		{
			_ignoreConversationSelection = ignoreConversationSelection;
		}
	}

	internal async Task<bool> DeleteConversationAsync(AIConversationListItem conversation, Window owner)
	{
		string conversationId = conversation?.EffectiveConversationId;
		if (string.IsNullOrWhiteSpace(conversationId) || !_deletingConversationIds.Add(conversationId))
		{
			return false;
		}
		try
		{
			if (new CustomerDialog(string.Format(Utils.GetString("AIAgentDeleteChatTitle"), conversation.DisplayTitle), Utils.GetString("AIAgentDeleteChatContent"), Utils.GetString("Delete"), Utils.GetString("Cancel"), owner).ShowDialog() != true)
			{
				return false;
			}
			bool flag;
			try
			{
				flag = await _conversationService.DeleteConversationAsync(conversationId);
			}
			catch (Exception ex)
			{
				UtilLog.Info("Agent conversation delete failed: " + ex.Message);
				flag = false;
			}
			if (!flag)
			{
				Utils.Toast(Utils.GetString("LoadFailed"));
				return false;
			}
			bool flag2 = IsCurrentConversation(conversationId);
			_ignoreConversationSelection = true;
			try
			{
				_conversations.Remove(conversation);
				if (LoadedConversationIds.Remove(conversationId) && _loadedServerConversationCount > 0)
				{
					_loadedServerConversationCount--;
				}
				if (flag2)
				{
					_currentConversationId = null;
					SetConversationTitle(null);
					HideDetail();
				}
				SelectCurrentConversation();
			}
			finally
			{
				_ignoreConversationSelection = false;
			}
			UpdateConversationListState();
			Utils.Toast(Utils.GetString("AIAgentChatdDeleted"));
			AIConversationService.NotifyConversationsChanged(this);
			if (flag2)
			{
				this.NewConversationRequested?.Invoke(this, EventArgs.Empty);
			}
			return true;
		}
		finally
		{
			_deletingConversationIds.Remove(conversationId);
		}
	}

	private AIConversationListItem FindConversation(string conversationId)
	{
		if (string.IsNullOrWhiteSpace(conversationId))
		{
			return null;
		}
		return _conversations.FirstOrDefault((AIConversationListItem item) => string.Equals(item.EffectiveConversationId, conversationId.Trim(), StringComparison.Ordinal));
	}

	private void SelectCurrentConversation()
	{
		ConversationList.SelectedItem = FindConversation(_currentConversationId);
	}

	private void RestoreCurrentSelection()
	{
		_ignoreConversationSelection = true;
		try
		{
			SelectCurrentConversation();
		}
		finally
		{
			_ignoreConversationSelection = false;
		}
	}

	private void UpdateConversationListState()
	{
		bool flag = _conversations.Count > 0;
		ConversationListPanel.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
		LoadingMoreConversationsText.Visibility = ((!flag || !_isLoadingMoreConversations) ? Visibility.Collapsed : Visibility.Visible);
		LoadingConversationPanel.Visibility = ((!_isLoadingConversations || flag) ? Visibility.Collapsed : Visibility.Visible);
		ConversationErrorPanel.Visibility = ((_isLoadingConversations || !_conversationLoadFailed || flag) ? Visibility.Collapsed : Visibility.Visible);
		EmptyConversationPanel.Visibility = ((_isLoadingConversations || _conversationLoadFailed || flag) ? Visibility.Collapsed : Visibility.Visible);
		UpdateConversationInteractionState();
	}

	private void UpdateConversationInteractionState()
	{
		bool flag = !string.IsNullOrWhiteSpace(_currentConversationId);
		ConversationList.IsEnabled = !_isPrivacyAuthorizationRequired && (_isAgentBridgeReady || _conversations.Any((AIConversationListItem item) => !item.IsLocalOnly)) && !_isSwitchingConversation && !_isUpdatingPins;
		ConversationTitle.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
		CurrentConversationMoreButton.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
		CurrentConversationMoreButton.IsEnabled = flag && !_isUpdatingPins && FindConversation(_currentConversationId) != null;
		this.ConversationStateChanged?.Invoke(this, EventArgs.Empty);
	}

	private void OnTaskDetailEnterImmersive(string taskId, int caretIndex)
	{
		Utils.FindParent<IListViewParent>(this)?.EnterImmersiveMode(taskId, caretIndex);
	}

	private async void OnTaskDetailNavigateTask(object sender, ProjectTask task)
	{
		await TaskDetailWindow.NavigateProjectTask(task, Utils.FindParent<IToastShowWindow>(this));
	}

	private void OnTaskDetailDeleted(object sender, string taskId)
	{
		HideDetail();
	}

	private void OnTaskDetailCopied(object sender, string taskId)
	{
		Utils.FindParent<IToastShowWindow>(this)?.TryToastString(null, Utils.GetString("CopyTask"));
	}

	private async void OnTaskDetailDeleteRequested(object sender, string taskId)
	{
		IToastShowWindow window = Utils.FindParent<IToastShowWindow>(this);
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		if (task != null && window != null)
		{
			List<TaskModel> list = await TaskService.GetAllSubTasksByIdAsync(taskId, task.projectId);
			if (list != null && list.Count > 0)
			{
				list.Add(task);
				await window.BatchDeleteTask(list);
			}
			else
			{
				window.TaskDeleted(taskId);
			}
		}
	}

	private void OnTaskDetailCheckItemsDeleted(object sender, TaskDetailItemModel model)
	{
		UndoToast undoToast = new UndoToast();
		undoToast.InitSubtaskUndo(model);
		Utils.FindParent<IToastShowWindow>(this)?.Toast(undoToast);
	}

	private void OnTaskDetailCloseRequested(object sender, EventArgs e)
	{
		HideDetail();
	}

	private void OnHabitDetailHidden(object sender, EventArgs e)
	{
		HideDetail();
	}

	private void OnNewConversationClick(object sender, RoutedEventArgs e)
	{
		this.NewConversationRequested?.Invoke(this, EventArgs.Empty);
		if (_isConversationListFloating)
		{
			SetConversationListVisible(show: false);
		}
	}

	private void OnToggleConversationListClick(object sender, RoutedEventArgs e)
	{
		SetConversationListVisible(!_isConversationListVisible);
	}

	private void OnConversationListDismissClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		SetConversationListVisible(show: false);
	}

	private void SetConversationListVisible(bool show)
	{
		if (_isConversationListVisible == show)
		{
			return;
		}
		_isConversationListVisible = show;
		ConversationListToggleButton.ToolTip = Utils.GetString(show ? "Collapse" : "Expand");
		int animationVersion = ++_conversationListAnimationVersion;
		if (_isConversationListFloating)
		{
			TranslateTransform transform = (TranslateTransform)ConversationListPane.RenderTransform;
			double value = ((ConversationListPane.Visibility == Visibility.Visible) ? transform.X : (0.0 - ConversationListPane.Width));
			ConversationListPane.Visibility = Visibility.Visible;
			ConversationListDismissArea.Visibility = ((!show) ? Visibility.Collapsed : Visibility.Visible);
			DoubleAnimation doubleAnimation = AnimationUtils.GetDoubleAnimation(value, show ? 0.0 : (0.0 - ConversationListPane.Width), 240);
			doubleAnimation.Completed += delegate
			{
				if (animationVersion == _conversationListAnimationVersion)
				{
					transform.BeginAnimation(TranslateTransform.XProperty, null);
					transform.X = 0.0;
					ConversationListPane.Visibility = ((!show) ? Visibility.Collapsed : Visibility.Visible);
				}
			};
			transform.BeginAnimation(TranslateTransform.XProperty, doubleAnimation);
			return;
		}
		double actualWidth = ConversationListColumn.ActualWidth;
		double num = Math.Min(_lastConversationListWidth, ResolveConversationListMaximumWidth(base.ActualWidth, GetReservedDetailPaneWidth()));
		ConversationListColumn.BeginAnimation(ColumnDefinition.MaxWidthProperty, null);
		if (show)
		{
			ConversationListPane.Visibility = Visibility.Visible;
			ConversationListDivider.Visibility = Visibility.Visible;
			ConversationListSeparator.Visibility = Visibility.Visible;
			ConversationListColumn.MinWidth = 0.0;
			ConversationListColumn.Width = new GridLength(_lastConversationListWidth);
			ConversationListColumn.MaxWidth = 0.0;
		}
		else
		{
			if (actualWidth >= 230.0)
			{
				_lastConversationListWidth = Math.Max(230.0, Math.Min(600.0, actualWidth));
			}
			ConversationListColumn.MinWidth = 0.0;
			ConversationListColumn.Width = new GridLength(_lastConversationListWidth);
			ConversationListColumn.MaxWidth = _lastConversationListWidth;
		}
		DoubleAnimation doubleAnimation2 = AnimationUtils.GetDoubleAnimation(show ? 0.0 : actualWidth, show ? num : 0.0, 240);
		doubleAnimation2.Completed += delegate
		{
			if (animationVersion == _conversationListAnimationVersion)
			{
				ConversationListColumn.BeginAnimation(ColumnDefinition.MaxWidthProperty, null);
				if (show)
				{
					ConversationListColumn.MinWidth = 230.0;
					ConversationListColumn.MaxWidth = ResolveConversationListMaximumWidth(base.ActualWidth, GetReservedDetailPaneWidth());
					ConversationListColumn.Width = new GridLength(_lastConversationListWidth);
				}
				else
				{
					ConversationListColumn.MinWidth = 0.0;
					ConversationListColumn.MaxWidth = 0.0;
					ConversationListColumn.Width = new GridLength(0.0);
					ConversationListPane.Visibility = Visibility.Collapsed;
					ConversationListDivider.Visibility = Visibility.Collapsed;
					ConversationListSeparator.Visibility = Visibility.Collapsed;
				}
			}
		};
		ConversationListColumn.BeginAnimation(ColumnDefinition.MaxWidthProperty, doubleAnimation2);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/ai/aiworkspaceview.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	internal Delegate _CreateDelegate(Type delegateType, string handler)
	{
		return Delegate.CreateDelegate(delegateType, this, handler);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			((AIWorkspaceView)target).PreviewMouseDown += OnWorkspacePreviewMouseDown;
			break;
		case 2:
			ConversationListColumn = (ColumnDefinition)target;
			break;
		case 3:
			ConversationColumn = (ColumnDefinition)target;
			break;
		case 4:
			DetailColumn = (ColumnDefinition)target;
			break;
		case 5:
			ConversationListPane = (Grid)target;
			break;
		case 6:
			AssistantTitle = (TextBlock)target;
			break;
		case 7:
			NewConversationButton = (Button)target;
			NewConversationButton.Click += OnNewConversationClick;
			break;
		case 8:
			ConversationListPanel = (DockPanel)target;
			break;
		case 9:
			LoadingMoreConversationsText = (TextBlock)target;
			break;
		case 10:
			ConversationList = (ListBox)target;
			ConversationList.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(OnConversationListScrollChanged));
			ConversationList.PreviewMouseRightButtonUp += OnConversationItemMouseRightButtonUp;
			ConversationList.MouseLeftButtonUp += OnConversationItemMouseLeftButtonUp;
			ConversationList.SelectionChanged += OnConversationSelectionChanged;
			break;
		case 12:
			LoadingConversationPanel = (StackPanel)target;
			break;
		case 13:
			EmptyConversationPanel = (StackPanel)target;
			break;
		case 14:
			ConversationErrorPanel = (StackPanel)target;
			break;
		case 15:
			((Button)target).Click += OnRetryConversationsClick;
			break;
		case 16:
			ConversationListToggleButton = (Button)target;
			ConversationListToggleButton.Click += OnToggleConversationListClick;
			break;
		case 17:
			ConversationTitle = (EmjTextBlock)target;
			break;
		case 18:
			CurrentConversationMoreButton = (Button)target;
			CurrentConversationMoreButton.Click += OnCurrentConversationMoreClick;
			break;
		case 19:
			HeaderRenamePopup = (Popup)target;
			HeaderRenamePopup.Opened += OnHeaderRenamePopupOpened;
			HeaderRenamePopup.Closed += OnHeaderRenamePopupClosed;
			break;
		case 20:
			HeaderRenamePopupContent = (ContentControl)target;
			break;
		case 21:
			HeaderRenameTextBox = (AIConversationRenameEditor)target;
			break;
		case 22:
			ConversationContentHost = (ContentControl)target;
			break;
		case 23:
			ConversationListDismissArea = (Border)target;
			ConversationListDismissArea.MouseLeftButtonDown += OnConversationListDismissClick;
			break;
		case 24:
			DetailPane = (Grid)target;
			break;
		case 25:
			DetailHost = (Grid)target;
			break;
		case 26:
			ConversationListDivider = (Border)target;
			break;
		case 27:
			ConversationListSeparator = (GridSplitter)target;
			ConversationListSeparator.DragStarted += OnConversationListSeparatorDragStarted;
			break;
		case 28:
			DetailDivider = (Border)target;
			break;
		case 29:
			DetailSeparator = (GridSplitter)target;
			DetailSeparator.DragCompleted += OnDetailSeparatorDragCompleted;
			break;
		case 30:
			PrivacyAuthorizationView = (AIPrivacyAuthorizationView)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 11)
		{
			((Button)target).Click += OnConversationItemMoreClick;
		}
	}
}
