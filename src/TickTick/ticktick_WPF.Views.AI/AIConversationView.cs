using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Newtonsoft.Json.Linq;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.AI;

public class AIConversationView : UserControl, IComponentConnector, IStyleConnector
{
	private const int StopPendingTimeoutMilliseconds = 10000;

	private const int FullScreenComposerMaxLines = 10;

	private const int FloatingComposerMaxLines = 5;

	private const double FullScreenComposerBorderMaxHeight = 286.0;

	private const double FloatingComposerBorderMaxHeight = 176.0;

	private const double FullScreenComposerTextMaxHeight = 252.0;

	private const double FloatingComposerTextMaxHeight = 142.0;

	private const double CompactComposerBottomMargin = 12.0;

	private const double ComposerButtonClearanceBottomMargin = 50.0;

	private const double CompactHomeLayoutBreakpoint = 500.0;

	public static readonly DependencyProperty SuggestionCornerRadiusProperty = DependencyProperty.RegisterAttached("SuggestionCornerRadius", typeof(CornerRadius), typeof(AIConversationView), new FrameworkPropertyMetadata(new CornerRadius(20.0)));

	private bool _isAgentBridgeReady;

	private bool _isGenerating;

	private bool _isSubmitting;

	private bool _isStopping;

	private int _stopRequestVersion;

	private int _greetingRefreshVersion;

	private bool _isTextCompositionActive;

	private bool _hasActiveConversation;

	private bool _isConversationStateResolved;

	private bool _isFloatingMode;

	private bool _isScrollToBottomRequestedVisible;

	private bool _isHostLayoutPending;

	private bool _isScrollingToBottom;

	private bool _isTokenLimitReached;

	private readonly Dictionary<string, bool> _appUpdateRequirements = new Dictionary<string, bool>(StringComparer.Ordinal);

	private readonly Dictionary<string, AgentApprovalSnapshot> _approvalPrompts = new Dictionary<string, AgentApprovalSnapshot>(StringComparer.Ordinal);

	private readonly AIComposerDraftCache _composerDrafts = new AIComposerDraftCache();

	private bool? _pendingAppUpdateRequirement;

	private bool _isApprovalRequestPending;

	private EscPopup _approvalMenuPopup;

	private int _approvalMenuHoverVersion;

	private AgentFeedbackRequest _feedbackRequest;

	private string _selectedFeedbackReason;

	private bool _isFeedbackSubmitting;

	private Window _feedbackWindow;

	private string _currentConversationId;

	private string _editingMessageId;

	private ContentControl _composerHost;

	private AIWebAppView _attachedWebView;

	private bool _shouldPresentWebStatus;

	private bool _isComposerButtonClearanceUpdateQueued;

	private DispatcherOperation _composerFocusOperation;

	internal Grid RootLayout;

	internal Grid MainHomeLayout;

	internal StackPanel MainHomeGreeting;

	internal TextBlock MainGreetingText;

	internal Grid MainComposerRow;

	internal Image HomeComposerGlow;

	internal ContentControl MainComposerHost;

	internal Grid ComposerSection;

	internal Border PremiumRestrictionBorder;

	internal Border TokenLimitNotice;

	internal Border ComposerBorder;

	internal TextBox ComposerTextBox;

	internal Button SubmitButton;

	internal Path SubmitArrow;

	internal Rectangle StopGlyph;

	internal Border AppUpdateRequiredBorder;

	internal Button DismissAppUpdateButton;

	internal Border ApprovalBorder;

	internal Grid ApprovalHeader;

	internal TextBlock ApprovalTitleText;

	internal TextBlock ApprovalProgressText;

	internal ScrollViewer ApprovalDetailsScrollViewer;

	internal ItemsControl ApprovalTaskTitles;

	internal TextBlock ApprovalDescriptionText;

	internal Border ApprovalDetailsFade;

	internal TextBlock ApprovalErrorText;

	internal Button ApprovalCancelButton;

	internal Button ApprovalConfirmButton;

	internal Button ApprovalMenuButton;

	internal RotateTransform ApprovalMenuArrowRotation;

	internal TextBlock GeneratedWarningText;

	internal ItemsControl MainHomeSuggestions;

	internal Grid FloatingHomeLayout;

	internal StackPanel FloatingHomeContent;

	internal TextBlock FloatingGreetingText;

	internal ItemsControl FloatingHomeSuggestions;

	internal ContentControl FloatingComposerHost;

	internal Grid ConversationLayout;

	internal ContentControl WebContentHost;

	internal Button ScrollToBottomButton;

	internal ContentControl ConversationComposerHost;

	internal Grid FeedbackOverlay;

	internal Button FeedbackNotHelpfulButton;

	internal Button FeedbackMismatchButton;

	internal Button FeedbackIncompleteButton;

	internal Button FeedbackOtherButton;

	internal TextBox FeedbackCommentTextBox;

	internal TextBlock FeedbackErrorText;

	internal Button SubmitFeedbackButton;

	private bool _contentLoaded;

	internal bool HasActiveConversation => _hasActiveConversation;

	public string DraftText
	{
		get
		{
			return ComposerTextBox.Text;
		}
		set
		{
			ComposerTextBox.Text = value ?? string.Empty;
			SaveCurrentComposerDraft();
		}
	}

	public event Func<string, string, Task<bool>> SubmitRequested;

	public event Func<Task<bool>> StopRequested;

	public event Func<Task<bool>> ScrollToBottomRequested;

	internal event Func<string, string, string, bool, Task<bool>> ApprovalResponseRequested;

	internal event Func<AgentRatingSubmission, Task<bool>> RatingSubmissionRequested;

	public event Action AppUpdateRequested;

	public static void SetSuggestionCornerRadius(DependencyObject element, CornerRadius value)
	{
		element.SetValue(SuggestionCornerRadiusProperty, value);
	}

	public static CornerRadius GetSuggestionCornerRadius(DependencyObject element)
	{
		return (CornerRadius)element.GetValue(SuggestionCornerRadiusProperty);
	}

	public AIConversationView()
	{
		InitializeComponent();
		NativeCandidateDecorationWorkaround.Attach(ComposerTextBox, delegate(string status)
		{
			UtilLog.Info("AI composer IME compatibility: " + status);
		});
		base.PreviewKeyDown += delegate(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.F12 && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && _attachedWebView != null)
			{
				e.Handled = true;
				_attachedWebView.ShowBackgroundCompatibilityMenu(this);
			}
		};
		_composerHost = MainComposerHost;
		AIHomeSuggestionItem[] itemsSource = CreateHomeSuggestions();
		MainHomeSuggestions.ItemsSource = itemsSource;
		FloatingHomeSuggestions.ItemsSource = itemsSource;
		ComposerTextBox.IsVisibleChanged += OnComposerIsVisibleChanged;
		TextCompositionManager.AddPreviewTextInputStartHandler(ComposerTextBox, OnComposerTextCompositionStarted);
		TextCompositionManager.AddPreviewTextInputUpdateHandler(ComposerTextBox, OnComposerTextCompositionUpdated);
		TextCompositionManager.AddPreviewTextInputHandler(ComposerTextBox, OnComposerTextCompositionCompleted);
		base.Loaded += OnLoaded;
		base.Loaded += OnThemeTrackingLoaded;
		base.Unloaded += OnThemeTrackingUnloaded;
		base.SizeChanged += OnViewSizeChanged;
		UpdatePresentation();
	}

	public void SetConversationActive(bool hasActiveConversation, string conversationId = null)
	{
		string text = (hasActiveConversation ? NormalizeConversationId(conversationId) : null);
		if (hasActiveConversation && string.IsNullOrEmpty(_currentConversationId) && !string.IsNullOrEmpty(text) && _pendingAppUpdateRequirement.HasValue)
		{
			SetAppUpdateRequirement(text, _pendingAppUpdateRequirement.Value);
			_pendingAppUpdateRequirement = null;
		}
		if (_hasActiveConversation != hasActiveConversation || !string.Equals(_currentConversationId, text, StringComparison.Ordinal) || !_isConversationStateResolved)
		{
			bool num = _hasActiveConversation != hasActiveConversation || !string.Equals(_currentConversationId, text, StringComparison.Ordinal);
			SaveCurrentComposerDraft();
			if (num)
			{
				_isApprovalRequestPending = false;
				CloseFeedback();
			}
			_hasActiveConversation = hasActiveConversation;
			_currentConversationId = text;
			_isConversationStateResolved = true;
			RestoreCurrentComposerDraft();
			if (!hasActiveConversation)
			{
				_isScrollToBottomRequestedVisible = false;
				_pendingAppUpdateRequirement = null;
			}
			UpdatePresentation();
			UpdateComposerState();
			if (num)
			{
				RequestComposerFocus();
			}
		}
	}

	private void OnComposerIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if ((bool)e.NewValue)
		{
			RequestComposerFocus();
		}
	}

	internal void RequestComposerFocus()
	{
		_composerFocusOperation?.Abort();
		_composerFocusOperation = base.Dispatcher.BeginInvoke(DispatcherPriority.Input, (Action)delegate
		{
			_composerFocusOperation = null;
			if (ComposerTextBox.IsVisible && ComposerTextBox.IsEnabled && ComposerTextBox.IsHitTestVisible && _feedbackWindow == null)
			{
				Window window = Window.GetWindow(ComposerTextBox);
				if (window != null && window.IsActive)
				{
					ComposerTextBox.Focus();
				}
			}
		});
	}

	public void SetConversationStateResolving()
	{
		if (_isConversationStateResolved)
		{
			_isConversationStateResolved = false;
			UpdatePresentation();
			UpdateComposerState();
		}
	}

	public void SetFloatingMode(bool isFloatingMode)
	{
		if (_isFloatingMode != isFloatingMode)
		{
			_isFloatingMode = isFloatingMode;
			_attachedWebView?.SetBackgroundHostMode(isFloatingMode);
			UpdatePresentation();
			QueueComposerButtonClearanceUpdate();
		}
	}

	public void SetAgentBridgeReady(bool isReady)
	{
		_isAgentBridgeReady = isReady;
		if (!isReady)
		{
			SetStopping(isStopping: false);
			_isScrollToBottomRequestedVisible = false;
			_isApprovalRequestPending = false;
			_isFeedbackSubmitting = false;
		}
		UpdateComposerState();
		UpdateScrollToBottomButton();
		UpdateApprovalPresentation();
		UpdateFeedbackState();
	}

	public void SetGenerating(bool isGenerating)
	{
		_isGenerating = isGenerating;
		if (!isGenerating)
		{
			SetStopping(isStopping: false);
		}
		UpdateComposerState();
		UpdateScrollToBottomButton();
		UpdateApprovalPresentation();
	}

	public void FillComposer(string content, string messageId)
	{
		_editingMessageId = (string.IsNullOrWhiteSpace(messageId) ? null : messageId);
		DraftText = content;
		ComposerTextBox.Focus();
		ComposerTextBox.CaretIndex = ComposerTextBox.Text.Length;
	}

	internal void FocusNewConversationComposer()
	{
		base.Dispatcher.BeginInvoke(DispatcherPriority.Input, (Action)delegate
		{
			if (!_hasActiveConversation && ComposerTextBox.IsVisible && ComposerTextBox.IsEnabled)
			{
				Window window = Window.GetWindow(this);
				if (window != null && window.IsActive)
				{
					ComposerTextBox.Focus();
					ComposerTextBox.CaretIndex = ComposerTextBox.Text.Length;
				}
			}
		});
	}

	public void SetScrollToBottomButtonVisible(bool visible)
	{
		if (_isScrollToBottomRequestedVisible != visible)
		{
			_isScrollToBottomRequestedVisible = visible;
			UpdateScrollToBottomButton();
		}
	}

	internal static bool ResolveScrollToBottomButtonVisibility(JToken payload)
	{
		if (payload != null && payload.Type == JTokenType.Boolean)
		{
			return payload.Value<bool>();
		}
		return (bool?)payload?["visible"] == true;
	}

	internal static bool? ResolveAppUpdateRequiredBlocking(JToken payload)
	{
		if (payload != null && payload.Type == JTokenType.Boolean)
		{
			return payload.Value<bool>();
		}
		JToken jToken = payload?["blocking"];
		if (jToken == null || jToken.Type != JTokenType.Boolean)
		{
			return null;
		}
		return jToken.Value<bool>();
	}

	public void SetAppUpdateRequired(string conversationId, bool blocking)
	{
		string text = NormalizeConversationId(conversationId) ?? _currentConversationId;
		if (string.IsNullOrEmpty(text))
		{
			if (_hasActiveConversation)
			{
				_pendingAppUpdateRequirement = _pendingAppUpdateRequirement == true || blocking;
			}
		}
		else
		{
			SetAppUpdateRequirement(text, blocking);
		}
		UpdatePresentation();
	}

	public void CompleteSubmission(string content, string messageId)
	{
		CompleteSubmission(content, messageId, _currentConversationId);
	}

	private void CompleteSubmission(string content, string messageId, string submissionConversationId)
	{
		if (_composerDrafts.ClearIfMatches(submissionConversationId, content, messageId) && string.Equals(_currentConversationId, submissionConversationId, StringComparison.Ordinal))
		{
			_editingMessageId = null;
			DraftText = string.Empty;
		}
	}

	public void AttachWebContent(UIElement content)
	{
		if (content == null)
		{
			throw new ArgumentNullException("content");
		}
		if (_attachedWebView != content)
		{
			DetachAttachedWebView();
			_attachedWebView = content as AIWebAppView;
			if (_attachedWebView != null)
			{
				_attachedWebView.StatusPresentationChanged += OnWebStatusPresentationChanged;
				_shouldPresentWebStatus = _attachedWebView.ShouldPresentStatusPanel;
			}
		}
		if (WebContentHost.Content != content)
		{
			WebContentHost.Content = content;
		}
		_attachedWebView?.SetBackgroundHostMode(_isFloatingMode);
		UpdatePresentation();
	}

	internal void SetApprovals(JToken payload)
	{
		AgentApprovalSnapshot agentApprovalSnapshot = AgentApprovalSnapshot.Parse(payload, _currentConversationId);
		if (agentApprovalSnapshot == null)
		{
			return;
		}
		string text = NormalizeConversationId(agentApprovalSnapshot.ConversationId);
		if (text == null)
		{
			text = _currentConversationId;
		}
		if (!string.IsNullOrEmpty(text))
		{
			if (agentApprovalSnapshot.CurrentApproval == null)
			{
				_approvalPrompts.Remove(text);
			}
			else
			{
				_approvalPrompts[text] = agentApprovalSnapshot;
			}
			if (string.Equals(text, _currentConversationId, StringComparison.Ordinal))
			{
				_isApprovalRequestPending = false;
				UpdatePresentation();
			}
		}
	}

	internal void ShowFeedback(JToken payload)
	{
		AgentFeedbackRequest agentFeedbackRequest = AgentFeedbackRequest.Parse(payload);
		if (agentFeedbackRequest == null || !string.Equals(agentFeedbackRequest.ConversationId, _currentConversationId, StringComparison.Ordinal))
		{
			return;
		}
		_feedbackRequest = agentFeedbackRequest;
		_selectedFeedbackReason = null;
		_isFeedbackSubmitting = false;
		FeedbackCommentTextBox.Text = string.Empty;
		FeedbackErrorText.Text = string.Empty;
		FeedbackErrorText.Visibility = Visibility.Collapsed;
		UpdateFeedbackReasonVisuals();
		UpdateFeedbackState();
		FeedbackOverlay.Visibility = Visibility.Visible;
		if (_feedbackWindow != null)
		{
			_feedbackWindow.Activate();
			FeedbackCommentTextBox.Focus();
			return;
		}
		Panel parent = (Panel)FeedbackOverlay.Parent;
		parent.Children.Remove(FeedbackOverlay);
		Window window = Window.GetWindow(this) ?? App.Window;
		Window window2 = new Window
		{
			Owner = window,
			WindowStartupLocation = ((window == null) ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner),
			SizeToContent = SizeToContent.Height,
			Width = 504.0,
			WindowStyle = WindowStyle.None,
			ResizeMode = ResizeMode.NoResize,
			AllowsTransparency = true,
			Background = Brushes.Transparent,
			ShowInTaskbar = false,
			Title = (string)FindResource("AIAgentFeedback"),
			Content = FeedbackOverlay
		};
		_feedbackWindow = window2;
		window2.ContentRendered += delegate
		{
			FeedbackCommentTextBox.Focus();
		};
		window2.PreviewKeyDown += delegate(object sender, KeyEventArgs args)
		{
			if (args.Key == Key.Escape)
			{
				args.Handled = true;
				CloseFeedback();
			}
		};
		window2.Closed += delegate
		{
			_feedbackWindow = null;
			window2.Content = null;
			parent.Children.Add(FeedbackOverlay);
			CloseFeedback();
		};
		window2.ShowDialog();
	}

	internal void ApplyRatingResult(JToken payload)
	{
		AgentFeedbackRequest feedbackRequest = _feedbackRequest;
		if (feedbackRequest != null && AgentRatingResult.Matches(payload, feedbackRequest.ConversationId, feedbackRequest.MessageId))
		{
			if ((bool?)payload?["success"] == true)
			{
				CloseFeedback();
			}
			else
			{
				ShowFeedbackSubmissionError((string?)payload?["error"]);
			}
		}
	}

	public void DetachWebContent(UIElement content)
	{
		if (_attachedWebView == content)
		{
			DetachAttachedWebView();
		}
		if (WebContentHost.Content == content)
		{
			WebContentHost.Content = null;
		}
		UpdatePresentation();
	}

	public void ResetForAccountChange()
	{
		_greetingRefreshVersion++;
		_isGenerating = false;
		SetStopping(isStopping: false);
		_isSubmitting = false;
		_hasActiveConversation = false;
		_isConversationStateResolved = true;
		_isScrollToBottomRequestedVisible = false;
		_isTokenLimitReached = false;
		_currentConversationId = null;
		_editingMessageId = null;
		_pendingAppUpdateRequirement = null;
		_appUpdateRequirements.Clear();
		_approvalPrompts.Clear();
		_isApprovalRequestPending = false;
		CloseFeedback();
		_composerDrafts.Clear();
		DraftText = string.Empty;
		UpdateGreeting(string.Empty);
		UpdatePresentation();
		UpdateComposerState();
	}

	public void SetTokenLimitReached(bool isTokenLimitReached)
	{
		if (_isTokenLimitReached != isTokenLimitReached)
		{
			_isTokenLimitReached = isTokenLimitReached;
			UpdatePresentation();
		}
	}

	public Task RefreshGreetingAsync()
	{
		return RefreshGreetingCoreAsync();
	}

	private void OnWebStatusPresentationChanged(object sender, EventArgs e)
	{
		if (sender == _attachedWebView)
		{
			_shouldPresentWebStatus = _attachedWebView.ShouldPresentStatusPanel;
			UpdatePresentation();
		}
	}

	private void DetachAttachedWebView()
	{
		if (_attachedWebView != null)
		{
			_attachedWebView.StatusPresentationChanged -= OnWebStatusPresentationChanged;
			_attachedWebView = null;
		}
		_shouldPresentWebStatus = false;
	}

	private void OnComposerTextChanged(object sender, TextChangedEventArgs e)
	{
		SaveCurrentComposerDraft();
		UpdateComposerState();
		QueueComposerButtonClearanceUpdate();
	}

	private void OnComposerTextBoxSizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (e.WidthChanged)
		{
			QueueComposerButtonClearanceUpdate();
		}
	}

	private void OnViewSizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (e.WidthChanged && ShouldUseCompactHomeLayout(e.PreviousSize.Width, _isFloatingMode) != ShouldUseCompactHomeLayout(e.NewSize.Width, _isFloatingMode))
		{
			UpdatePresentation();
			QueueComposerButtonClearanceUpdate();
		}
	}

	private async void OnLoaded(object sender, RoutedEventArgs e)
	{
		base.Loaded -= OnLoaded;
		QueueComposerButtonClearanceUpdate();
		await RefreshGreetingCoreAsync();
	}

	private void OnThemeTrackingLoaded(object sender, RoutedEventArgs e)
	{
		DataChangedNotifier.ThemeModeChanged -= OnThemeModeChanged;
		DataChangedNotifier.ThemeModeChanged += OnThemeModeChanged;
		PropertyChangedEventManager.RemoveHandler(LocalSettings.Settings, OnMembershipChanged, "IsPro");
		PropertyChangedEventManager.AddHandler(LocalSettings.Settings, OnMembershipChanged, "IsPro");
		UpdatePresentation();
	}

	private void OnThemeTrackingUnloaded(object sender, RoutedEventArgs e)
	{
		DataChangedNotifier.ThemeModeChanged -= OnThemeModeChanged;
		PropertyChangedEventManager.RemoveHandler(LocalSettings.Settings, OnMembershipChanged, "IsPro");
		CloseApprovalMenu();
		CloseFeedback();
	}

	private void OnMembershipChanged(object sender, PropertyChangedEventArgs e)
	{
		if (!base.Dispatcher.CheckAccess())
		{
			base.Dispatcher.BeginInvoke((Action)delegate
			{
				if (base.IsLoaded)
				{
					UpdatePresentation();
				}
			});
		}
		else if (base.IsLoaded)
		{
			UpdatePresentation();
		}
	}

	private void OnThemeModeChanged(object sender, EventArgs e)
	{
		UpdateHomeComposerGlowVisibility();
	}

	private void UpdateHomeComposerGlowVisibility()
	{
		HomeComposerGlow.Opacity = (LocalSettings.Settings.IsDarkTheme ? 0.4 : 0.52);
		bool isTokenLimitReached = _isTokenLimitReached;
		HomeComposerGlow.Visibility = ((!ShouldShowHomeComposerGlow(LocalSettings.Settings.ThemeId, UserDao.IsPro(), isTokenLimitReached)) ? Visibility.Collapsed : Visibility.Visible);
	}

	private async Task RefreshGreetingCoreAsync()
	{
		int refreshVersion = ++_greetingRefreshVersion;
		string displayName = ResolveFallbackDisplayName();
		UpdateGreeting(displayName);
		try
		{
			UserInfoModel userInfoModel = await UserManager.GetUserInfo();
			if (!string.IsNullOrWhiteSpace(userInfoModel?.name))
			{
				displayName = userInfoModel.name.Trim();
			}
			else if (!string.IsNullOrWhiteSpace(userInfoModel?.givenName))
			{
				displayName = userInfoModel.givenName.Trim();
			}
		}
		catch
		{
		}
		if (refreshVersion == _greetingRefreshVersion)
		{
			UpdateGreeting(displayName);
		}
	}

	private void UpdateGreeting(string displayName)
	{
		TimeZoneInfo timeZone = TimeZoneData.LocalTimeZoneModel?.TimeZone ?? TimeZoneInfo.Local;
		string text = Utils.GetString(GetGreetingResourceKey(GetHourInTimeZone(DateTime.UtcNow, timeZone)));
		string text2 = (string.IsNullOrWhiteSpace(displayName) ? text.Replace("{0}", string.Empty).Trim(' ', ',', '，') : string.Format(text, displayName));
		MainGreetingText.Text = text2;
		FloatingGreetingText.Text = text2;
	}

	private async void OnSuggestionClick(object sender, RoutedEventArgs e)
	{
		AIHomeSuggestionItem aIHomeSuggestionItem = (sender as Button)?.DataContext as AIHomeSuggestionItem;
		if (!string.IsNullOrWhiteSpace(aIHomeSuggestionItem?.Prompt))
		{
			UserActCollectUtils.AddClickEvent("ai_assistant", "suggestion", aIHomeSuggestionItem.TrackingLabel);
			if (aIHomeSuggestionItem.SubmitImmediately)
			{
				await SubmitTextAsync(aIHomeSuggestionItem.Prompt, null, clearComposerOnSuccess: false);
				return;
			}
			_editingMessageId = null;
			DraftText = aIHomeSuggestionItem.Prompt;
			ComposerTextBox.Focus();
			ComposerTextBox.CaretIndex = ComposerTextBox.Text.Length;
		}
	}

	private async void OnSubmitClick(object sender, RoutedEventArgs e)
	{
		if (_isGenerating)
		{
			await StopGeneratingAsync();
		}
		else
		{
			await SubmitComposerAsync();
		}
	}

	private async void OnPremiumUpgradeClick(object sender, RoutedEventArgs e)
	{
		await ShowPremiumUpgradeDialogAsync();
	}

	private async void OnScrollToBottomClick(object sender, RoutedEventArgs e)
	{
		Func<Task<bool>> func = this.ScrollToBottomRequested;
		if (!_isAgentBridgeReady || _isScrollingToBottom || func == null)
		{
			return;
		}
		_isScrollingToBottom = true;
		UpdateScrollToBottomButton();
		try
		{
			await func();
		}
		finally
		{
			_isScrollingToBottom = false;
			UpdateScrollToBottomButton();
		}
	}

	private async void OnComposerPreviewKeyDown(object sender, KeyEventArgs e)
	{
		if (ShouldSubmitOnEnter(e.Key, Keyboard.Modifiers, _isTextCompositionActive))
		{
			e.Handled = true;
			await SubmitComposerAsync();
		}
	}

	private void OnComposerLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
	{
		_isTextCompositionActive = false;
		QueueComposerButtonClearanceUpdate();
	}

	private void OnComposerTextCompositionStarted(object sender, TextCompositionEventArgs e)
	{
		_isTextCompositionActive = true;
	}

	private void OnComposerTextCompositionUpdated(object sender, TextCompositionEventArgs e)
	{
		_isTextCompositionActive = true;
	}

	private void OnComposerTextCompositionCompleted(object sender, TextCompositionEventArgs e)
	{
		_isTextCompositionActive = false;
		QueueComposerButtonClearanceUpdate();
	}

	internal static bool ShouldSubmitOnEnter(Key key, ModifierKeys modifiers, bool isTextCompositionActive)
	{
		if ((key == Key.Return || key == Key.Return) && modifiers == ModifierKeys.None)
		{
			return !isTextCompositionActive;
		}
		return false;
	}

	internal static string GetGreetingResourceKey(int hour)
	{
		if (hour >= 6 && hour < 12)
		{
			return "AIAgentGreetingMorning";
		}
		if (hour >= 12 && hour < 18)
		{
			return "AIAgentGreetingAfternoon";
		}
		if (hour >= 18 && hour < 24)
		{
			return "AIAgentGreetingEvening";
		}
		return "AIAgentGreetingLate";
	}

	internal static int GetHourInTimeZone(DateTime utcNow, TimeZoneInfo timeZone)
	{
		return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), timeZone ?? TimeZoneInfo.Local).Hour;
	}

	internal static int ResolveComposerMaxLines(bool isFloatingMode)
	{
		if (!isFloatingMode)
		{
			return 10;
		}
		return 5;
	}

	internal static bool ShouldReserveComposerButtonSpace(string text, int lineCount, double textEndRight, double buttonLeft)
	{
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		if (lineCount > 1 || text.IndexOf('\r') >= 0 || text.IndexOf('\n') >= 0)
		{
			return true;
		}
		if (!double.IsNaN(textEndRight) && !double.IsInfinity(textEndRight) && !double.IsNaN(buttonLeft) && !double.IsInfinity(buttonLeft) && buttonLeft > 0.0)
		{
			return textEndRight >= buttonLeft;
		}
		return false;
	}

	private void ApplyComposerLineLimit(bool useCompactLimits)
	{
		ComposerTextBox.MaxLines = ResolveComposerMaxLines(useCompactLimits);
		ComposerTextBox.MaxHeight = (useCompactLimits ? 142.0 : 252.0);
		ComposerBorder.MaxHeight = (useCompactLimits ? 176.0 : 286.0);
	}

	private void QueueComposerButtonClearanceUpdate()
	{
		if (!_isTextCompositionActive && !_isComposerButtonClearanceUpdateQueued)
		{
			_isComposerButtonClearanceUpdateQueued = true;
			base.Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)delegate
			{
				_isComposerButtonClearanceUpdateQueued = false;
				UpdateComposerButtonClearance();
			});
		}
	}

	private void UpdateComposerButtonClearance()
	{
		if (_isTextCompositionActive)
		{
			return;
		}
		double textEndRight = double.NaN;
		double buttonLeft = double.NaN;
		if (ComposerTextBox.IsLoaded && SubmitButton.IsLoaded)
		{
			Rect rectFromCharacterIndex = ComposerTextBox.GetRectFromCharacterIndex(ComposerTextBox.Text.Length, trailingEdge: true);
			if (!rectFromCharacterIndex.IsEmpty)
			{
				textEndRight = rectFromCharacterIndex.Right;
			}
			buttonLeft = SubmitButton.TranslatePoint(new Point(0.0, 0.0), ComposerTextBox).X;
		}
		double num = ResolveComposerBottomMargin(ShouldReserveComposerButtonSpace(ComposerTextBox.Text, ComposerTextBox.LineCount, textEndRight, buttonLeft));
		Thickness margin = ComposerTextBox.Margin;
		if (!(Math.Abs(margin.Bottom - num) < 0.1))
		{
			ComposerTextBox.Margin = new Thickness(margin.Left, margin.Top, margin.Right, num);
		}
	}

	internal static double ResolveComposerBottomMargin(bool reserveButtonSpace)
	{
		if (!reserveButtonSpace)
		{
			return 12.0;
		}
		return 50.0;
	}

	internal static AIHomeSuggestionItem[] CreateHomeSuggestions()
	{
		return new AIHomeSuggestionItem[3]
		{
			new AIHomeSuggestionItem(Utils.GetGeometry("Ic.TaskOM.Search"), Utils.GetString("AIAgentMyTaskToday"), Utils.GetString("AIAgentShowMeTaskToday"), submitImmediately: true, "today_task"),
			new AIHomeSuggestionItem(Utils.GetGeometry("Ic.CalendarType.Note"), Utils.GetString("AIAgentSummaryToday"), Utils.GetString("AIAgentSummaryTodayCompletion"), submitImmediately: true, "summarize_today"),
			new AIHomeSuggestionItem(Utils.GetGeometry("Ic.AI.TodayPlan"), Utils.GetString("AIAgentPlanMyDay"), Utils.GetString("AIAgentPlanBaseToday"), submitImmediately: true, "plan_my_day")
		};
	}

	private Task SubmitComposerAsync()
	{
		return SubmitTextAsync(ComposerTextBox.Text, _editingMessageId, clearComposerOnSuccess: true);
	}

	private async Task SubmitTextAsync(string content, string messageId, bool clearComposerOnSuccess)
	{
		string text = content.Trim();
		if (text.Length == 0 || _isGenerating || _isSubmitting)
		{
			return;
		}
		if (ShouldShowPremiumRestriction(UserDao.IsPro()))
		{
			await ShowPremiumUpgradeDialogAsync();
			return;
		}
		Func<string, string, Task<bool>> func = this.SubmitRequested;
		if (!_isAgentBridgeReady || !_isConversationStateResolved || func == null)
		{
			return;
		}
		string submissionConversationId = _currentConversationId;
		_isSubmitting = true;
		UpdateComposerState();
		try
		{
			UserActCollectUtils.AddClickEvent("ai_assistant", "user_message", "send");
			if (await func(text, messageId) && clearComposerOnSuccess)
			{
				CompleteSubmission(text, messageId, submissionConversationId);
			}
		}
		finally
		{
			_isSubmitting = false;
			UpdateComposerState();
		}
	}

	private async Task ShowPremiumUpgradeDialogAsync()
	{
		await ProChecker.ShowUpgradeDialog(ProType.AIAssistant, ResolvePremiumDialogOwner(App.Window, Window.GetWindow(this)));
		UpdatePresentation();
	}

	internal static Window ResolvePremiumDialogOwner(Window mainWindow, Window viewHostWindow)
	{
		return mainWindow ?? viewHostWindow;
	}

	private void OnDismissAppUpdateClick(object sender, RoutedEventArgs e)
	{
		if (GetCurrentAppUpdateRequirement() == false)
		{
			if (string.IsNullOrEmpty(_currentConversationId))
			{
				_pendingAppUpdateRequirement = null;
			}
			else
			{
				_appUpdateRequirements.Remove(_currentConversationId);
			}
			UpdatePresentation();
		}
	}

	private void OnAppUpdateClick(object sender, RoutedEventArgs e)
	{
		this.AppUpdateRequested?.Invoke();
	}

	private async void OnApprovalCancelClick(object sender, RoutedEventArgs e)
	{
		await SubmitApprovalDecisionAsync("rejected");
	}

	private async void OnApprovalConfirmClick(object sender, RoutedEventArgs e)
	{
		await SubmitApprovalDecisionAsync("approved");
	}

	private void OnApprovalMenuClick(object sender, RoutedEventArgs e)
	{
		e.Handled = true;
		ShowApprovalMenu(focusMenu: true);
	}

	private void OnApprovalMenuMouseEnter(object sender, MouseEventArgs e)
	{
		_approvalMenuHoverVersion++;
		ShowApprovalMenu(focusMenu: false);
	}

	private async void OnApprovalMenuMouseLeave(object sender, MouseEventArgs e)
	{
		EscPopup popup = _approvalMenuPopup;
		int version = ++_approvalMenuHoverVersion;
		await Task.Delay(250);
		if (version == _approvalMenuHoverVersion && popup != null && popup.StaysOpen && popup == _approvalMenuPopup && !ApprovalMenuButton.IsMouseOver)
		{
			UIElement child = popup.Child;
			if (child == null || !child.IsMouseOver)
			{
				CloseApprovalMenu();
			}
		}
	}

	private void ShowApprovalMenu(bool focusMenu)
	{
		if (_approvalMenuPopup != null)
		{
			if (focusMenu)
			{
				_approvalMenuPopup.StaysOpen = false;
				_approvalMenuPopup.TryFocus();
			}
			return;
		}
		AgentApproval approval = GetCurrentApproval();
		if (approval == null || !ApprovalMenuButton.IsEnabled || ApprovalMenuButton.Visibility != Visibility.Visible)
		{
			return;
		}
		EscPopup popup = new EscPopup
		{
			PlacementTarget = ApprovalMenuButton,
			Placement = PlacementMode.Custom,
			StaysOpen = !focusMenu,
			PopupAnimation = PopupAnimation.Fade,
			RestoreFocus = false,
			NeedFocus = focusMenu,
			CustomPopupPlacementCallback = (Size popupSize, Size targetSize, Point offset) => new CustomPopupPlacement[2]
			{
				new CustomPopupPlacement(new Point(targetSize.Width - popupSize.Width + 12.0, 0.0 - popupSize.Height + 6.0), PopupPrimaryAxis.Horizontal),
				new CustomPopupPlacement(new Point(targetSize.Width - popupSize.Width + 12.0, targetSize.Height), PopupPrimaryAxis.Horizontal)
			}
		};
		CustomMenuList customMenuList = new CustomMenuList(new CustomMenuItemViewModel[1]
		{
			new CustomMenuItemViewModel((object)"approved", approval.ConfirmAllText, (Geometry)null, "")
			{
				ShowSelected = false
			}
		}, popup);
		customMenuList.SetMinWidth(186.0);
		customMenuList.MouseEnter += delegate
		{
			_approvalMenuHoverVersion++;
		};
		customMenuList.MouseLeave += OnApprovalMenuMouseLeave;
		_approvalMenuPopup = popup;
		ApprovalMenuArrowRotation.Angle = 180.0;
		customMenuList.Closed += delegate
		{
			if (_approvalMenuPopup == popup)
			{
				_approvalMenuPopup = null;
				ApprovalMenuArrowRotation.Angle = 0.0;
			}
		};
		customMenuList.Operated += async delegate(object menuSender, object action)
		{
			if (object.Equals(action, "approved") && approval == GetCurrentApproval())
			{
				await SubmitApprovalDecisionAsync("approved", approveRemaining: true);
			}
		};
		customMenuList.Show();
	}

	private void CloseApprovalMenu()
	{
		_approvalMenuHoverVersion++;
		if (_approvalMenuPopup != null)
		{
			_approvalMenuPopup.IsOpen = false;
		}
	}

	internal bool TryCloseTopmostPopup()
	{
		EscPopup approvalMenuPopup = _approvalMenuPopup;
		if (approvalMenuPopup != null && approvalMenuPopup.IsOpen)
		{
			CloseApprovalMenu();
			return true;
		}
		if (FeedbackOverlay.Visibility == Visibility.Visible)
		{
			CloseFeedback();
			return true;
		}
		return false;
	}

	private async Task SubmitApprovalDecisionAsync(string decision, bool approveRemaining = false)
	{
		AgentApproval approval = GetCurrentApproval();
		Func<string, string, string, bool, Task<bool>> func = this.ApprovalResponseRequested;
		if (approval == null || !approval.CanSubmitDecision(decision))
		{
			return;
		}
		if (approveRemaining)
		{
			AgentApprovalSnapshot currentApprovalSnapshot = GetCurrentApprovalSnapshot();
			if ((currentApprovalSnapshot != null && currentApprovalSnapshot.Total <= 1) || approval.Status != "pending" || decision != "approved")
			{
				return;
			}
		}
		if (_isGenerating || !_isAgentBridgeReady || _isApprovalRequestPending || func == null)
		{
			return;
		}
		_isApprovalRequestPending = true;
		UpdateApprovalPresentation();
		bool accepted = false;
		try
		{
			accepted = await func(approval.ConversationId, approval.MessageId, decision, approveRemaining);
		}
		finally
		{
			if (!accepted && approval == GetCurrentApproval())
			{
				_isApprovalRequestPending = false;
			}
			UpdateApprovalPresentation();
		}
	}

	private void OnFeedbackReasonClick(object sender, RoutedEventArgs e)
	{
		if (!_isFeedbackSubmitting && sender is Button button)
		{
			string text = button.CommandParameter as string;
			if (AgentRatingSubmission.IsSupportedReason(text))
			{
				_selectedFeedbackReason = text;
				FeedbackErrorText.Visibility = Visibility.Collapsed;
				UpdateFeedbackReasonVisuals();
				UpdateFeedbackState();
			}
		}
	}

	private void OnFeedbackCommentTextChanged(object sender, TextChangedEventArgs e)
	{
		if (FeedbackErrorText != null)
		{
			FeedbackErrorText.Visibility = Visibility.Collapsed;
		}
	}

	private void OnCloseFeedbackClick(object sender, RoutedEventArgs e)
	{
		if (!_isFeedbackSubmitting)
		{
			CloseFeedback();
		}
	}

	private async void OnSubmitFeedbackClick(object sender, RoutedEventArgs e)
	{
		await SubmitFeedbackAsync();
	}

	private async Task SubmitFeedbackAsync()
	{
		AgentFeedbackRequest request = _feedbackRequest;
		Func<AgentRatingSubmission, Task<bool>> func = this.RatingSubmissionRequested;
		AgentRatingSubmission agentRatingSubmission = ((request == null) ? null : new AgentRatingSubmission(request.ConversationId, request.MessageId, request.Rating, _selectedFeedbackReason, FeedbackCommentTextBox.Text));
		if (agentRatingSubmission == null || !agentRatingSubmission.IsValid || !_isAgentBridgeReady || _isFeedbackSubmitting || func == null)
		{
			return;
		}
		_isFeedbackSubmitting = true;
		FeedbackErrorText.Visibility = Visibility.Collapsed;
		UpdateFeedbackState();
		try
		{
			if (!(await func(agentRatingSubmission)) && request == _feedbackRequest)
			{
				ShowFeedbackSubmissionError(null);
			}
		}
		catch
		{
			if (request == _feedbackRequest)
			{
				ShowFeedbackSubmissionError(null);
			}
		}
	}

	private void ShowFeedbackSubmissionError(string error)
	{
		_isFeedbackSubmitting = false;
		string text = error?.Trim();
		FeedbackErrorText.Text = (string.IsNullOrEmpty(text) ? (FeedbackErrorText.Tag as string) : text);
		FeedbackErrorText.Visibility = Visibility.Visible;
		UpdateFeedbackState();
	}

	private void UpdateFeedbackReasonVisuals()
	{
		Button[] feedbackReasonButtons = GetFeedbackReasonButtons();
		foreach (Button obj in feedbackReasonButtons)
		{
			bool flag = string.Equals(obj.CommandParameter as string, _selectedFeedbackReason, StringComparison.Ordinal);
			obj.SetResourceReference(Control.BorderBrushProperty, flag ? "PrimaryColor" : "BaseColorOpacity10");
			obj.SetResourceReference(Control.ForegroundProperty, flag ? "PrimaryColor" : "BaseColorOpacity80");
		}
	}

	private void UpdateFeedbackState()
	{
		if (SubmitFeedbackButton != null)
		{
			bool isEnabled = _feedbackRequest != null && _isAgentBridgeReady && !_isFeedbackSubmitting && !string.IsNullOrWhiteSpace(_selectedFeedbackReason);
			SubmitFeedbackButton.IsEnabled = isEnabled;
			Button[] feedbackReasonButtons = GetFeedbackReasonButtons();
			for (int i = 0; i < feedbackReasonButtons.Length; i++)
			{
				feedbackReasonButtons[i].IsEnabled = !_isFeedbackSubmitting;
			}
		}
	}

	private Button[] GetFeedbackReasonButtons()
	{
		return new Button[4] { FeedbackNotHelpfulButton, FeedbackMismatchButton, FeedbackIncompleteButton, FeedbackOtherButton };
	}

	private void CloseFeedback()
	{
		_feedbackRequest = null;
		_selectedFeedbackReason = null;
		_isFeedbackSubmitting = false;
		if (FeedbackOverlay != null)
		{
			FeedbackOverlay.Visibility = Visibility.Collapsed;
		}
		_feedbackWindow?.Close();
	}

	private void OnApprovalHeaderSizeChanged(object sender, SizeChangedEventArgs e)
	{
		UpdateApprovalHeaderLayout();
	}

	private void UpdateApprovalHeaderLayout()
	{
		if (ApprovalHeader != null && !(ApprovalHeader.ActualWidth <= 0.0))
		{
			TextBlock textBlock = new TextBlock
			{
				Text = ApprovalTitleText.Text,
				FontFamily = ApprovalTitleText.FontFamily,
				FontSize = ApprovalTitleText.FontSize,
				FontWeight = ApprovalTitleText.FontWeight
			};
			textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			TextBlock textBlock2 = new TextBlock
			{
				Text = ApprovalProgressText.Text,
				FontFamily = ApprovalProgressText.FontFamily,
				FontSize = ApprovalProgressText.FontSize
			};
			textBlock2.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			bool flag = ApprovalProgressText.Visibility == Visibility.Visible && textBlock.DesiredSize.Width + textBlock2.DesiredSize.Width + 8.0 > ApprovalHeader.ActualWidth;
			Grid.SetRow(ApprovalTitleText, flag ? 1 : 0);
			Grid.SetColumnSpan(ApprovalTitleText, (!flag) ? 1 : 2);
			Grid.SetColumn(ApprovalProgressText, (!flag) ? 1 : 0);
			ApprovalProgressText.Margin = (flag ? new Thickness(0.0, 0.0, 0.0, 8.0) : new Thickness(8.0, 0.0, 0.0, 0.0));
		}
	}

	private void OnApprovalDetailsScrollChanged(object sender, ScrollChangedEventArgs e)
	{
		if (ApprovalDetailsFade != null)
		{
			ApprovalDetailsFade.Visibility = ((ApprovalDetailsScrollViewer.Visibility != Visibility.Visible || !(ApprovalDetailsScrollViewer.ScrollableHeight - ApprovalDetailsScrollViewer.VerticalOffset > 0.5)) ? Visibility.Collapsed : Visibility.Visible);
		}
	}

	private void UpdateApprovalPresentation()
	{
		if (ApprovalBorder == null)
		{
			return;
		}
		AgentApproval currentApproval = GetCurrentApproval();
		if (currentApproval == null)
		{
			CloseApprovalMenu();
			ApprovalBorder.Visibility = Visibility.Collapsed;
			return;
		}
		ApprovalTitleText.Text = currentApproval.Title;
		AgentApprovalSnapshot currentApprovalSnapshot = GetCurrentApprovalSnapshot();
		ApprovalProgressText.Text = ((currentApprovalSnapshot.Total > 1) ? $"{currentApprovalSnapshot.CurrentIndex} / {currentApprovalSnapshot.Total}" : string.Empty);
		ApprovalProgressText.Visibility = ((currentApprovalSnapshot.Total <= 1) ? Visibility.Collapsed : Visibility.Visible);
		UpdateApprovalHeaderLayout();
		if (ApprovalTaskTitles.ItemsSource != currentApproval.TaskTitles)
		{
			CloseApprovalMenu();
			ApprovalDetailsScrollViewer.ScrollToTop();
			ApprovalTaskTitles.ItemsSource = currentApproval.TaskTitles;
		}
		ApprovalTaskTitles.Visibility = ((currentApproval.TaskTitles.Length == 0) ? Visibility.Collapsed : Visibility.Visible);
		ApprovalDescriptionText.Text = currentApproval.Description;
		ApprovalDescriptionText.Visibility = ((currentApproval.TaskTitles.Length != 0 || string.IsNullOrWhiteSpace(currentApproval.Description)) ? Visibility.Collapsed : Visibility.Visible);
		ApprovalDetailsScrollViewer.Visibility = ((currentApproval.TaskTitles.Length == 0 && string.IsNullOrWhiteSpace(currentApproval.Description)) ? Visibility.Collapsed : Visibility.Visible);
		if (ApprovalDetailsScrollViewer.Visibility != Visibility.Visible)
		{
			ApprovalDetailsFade.Visibility = Visibility.Collapsed;
		}
		ApprovalErrorText.Text = currentApproval.Error;
		ApprovalErrorText.Visibility = (string.IsNullOrWhiteSpace(currentApproval.Error) ? Visibility.Collapsed : Visibility.Visible);
		ApprovalConfirmButton.Content = currentApproval.ConfirmText;
		ApprovalCancelButton.Content = currentApproval.CancelText;
		bool flag = _isAgentBridgeReady && !_isApprovalRequestPending && !_isGenerating;
		bool flag2 = currentApprovalSnapshot.Total > 1 && currentApproval.Status == "pending";
		ApprovalMenuButton.Visibility = ((!flag2) ? Visibility.Collapsed : Visibility.Visible);
		ApprovalConfirmButton.Tag = (flag2 ? "10,0,0,10" : "10");
		ApprovalMenuButton.IsEnabled = flag && currentApproval.CanSubmitDecision("approved");
		if (!flag2 || !ApprovalMenuButton.IsEnabled)
		{
			CloseApprovalMenu();
		}
		ApprovalConfirmButton.IsEnabled = flag && currentApproval.CanSubmitDecision("approved");
		ApprovalCancelButton.IsEnabled = flag && currentApproval.CanSubmitDecision("rejected");
	}

	private void UpdateComposerState()
	{
		bool flag = _isAgentBridgeReady && _isConversationStateResolved;
		AIComposerActionState aIComposerActionState = ResolveComposerActionState(flag, _isGenerating, _isSubmitting, ComposerTextBox.Text);
		ApplyComposerActionVisual(aIComposerActionState);
		SubmitButton.IsEnabled = CanInvokeComposerAction(flag, _isSubmitting, _isStopping, aIComposerActionState);
		SubmitButton.ToolTip = ((aIComposerActionState == AIComposerActionState.Stop) ? Utils.GetString("Stop") : (flag ? Utils.GetString("Send") : Utils.GetString("Loading")));
	}

	internal static AIComposerActionState ResolveComposerActionState(bool isAgentBridgeReady, bool isGenerating, bool isSubmitting, string text)
	{
		if (isGenerating)
		{
			return AIComposerActionState.Stop;
		}
		if (!isAgentBridgeReady || isSubmitting || string.IsNullOrWhiteSpace(text))
		{
			return AIComposerActionState.Disabled;
		}
		return AIComposerActionState.Send;
	}

	internal static bool CanInvokeComposerAction(bool isAgentBridgeReady, bool isSubmitting, bool isStopping, AIComposerActionState actionState)
	{
		if (isAgentBridgeReady && !isSubmitting && !isStopping)
		{
			return actionState != AIComposerActionState.Disabled;
		}
		return false;
	}

	private void ApplyComposerActionVisual(AIComposerActionState actionState)
	{
		SubmitArrow.Visibility = ((actionState == AIComposerActionState.Stop) ? Visibility.Collapsed : Visibility.Visible);
		StopGlyph.Visibility = ((actionState != AIComposerActionState.Stop) ? Visibility.Collapsed : Visibility.Visible);
		switch (actionState)
		{
		case AIComposerActionState.Send:
			SubmitButton.SetResourceReference(Control.BackgroundProperty, "PrimaryColor");
			SubmitArrow.Fill = Brushes.White;
			break;
		case AIComposerActionState.Stop:
			SubmitButton.SetResourceReference(Control.BackgroundProperty, "BaseColorOpacity8");
			break;
		default:
			SubmitButton.SetResourceReference(Control.BackgroundProperty, "BaseColorOpacity4");
			SubmitArrow.SetResourceReference(Shape.FillProperty, "BaseColorOpacity20");
			break;
		}
	}

	private async Task StopGeneratingAsync()
	{
		Func<Task<bool>> func = this.StopRequested;
		if (!_isAgentBridgeReady || !_isConversationStateResolved || !_isGenerating || _isSubmitting || func == null)
		{
			return;
		}
		_isSubmitting = true;
		UpdateComposerState();
		try
		{
			bool flag = await func();
			UtilLog.Info("AI stop generating request returned: accepted=" + flag);
			if (flag && _isGenerating)
			{
				SetStopping(isStopping: true);
			}
		}
		finally
		{
			_isSubmitting = false;
			UpdateComposerState();
		}
	}

	private void SetStopping(bool isStopping)
	{
		if (_isStopping != isStopping)
		{
			_isStopping = isStopping;
			int requestVersion = ++_stopRequestVersion;
			if (isStopping)
			{
				ReleaseStopPendingAfterTimeoutAsync(requestVersion);
			}
		}
	}

	private async Task ReleaseStopPendingAfterTimeoutAsync(int requestVersion)
	{
		await Task.Delay(10000);
		if (_isStopping && requestVersion == _stopRequestVersion)
		{
			_isStopping = false;
			_stopRequestVersion++;
			UtilLog.Info("AI stop generating request timed out waiting for stateChanged.");
			UpdateComposerState();
		}
	}

	private void UpdatePresentation()
	{
		bool flag = !_isConversationStateResolved;
		bool flag2 = ShouldShowNativeHome(_isConversationStateResolved, _hasActiveConversation, _shouldPresentWebStatus, _attachedWebView != null && !_attachedWebView.HasPresentedBrowserContent);
		bool flag3 = ShouldUseCompactHomeLayout(base.ActualWidth, _isFloatingMode);
		bool flag4 = flag2 && !flag3;
		bool flag5 = flag2 && flag3;
		ApplyComposerLineLimit(_isFloatingMode || flag5);
		MoveComposerTo(flag4 ? MainComposerHost : (flag5 ? FloatingComposerHost : ConversationComposerHost));
		AgentApproval agentApproval = ((flag2 || flag) ? null : GetCurrentApproval());
		bool? flag6 = ((flag2 || flag || agentApproval != null) ? ((bool?)null) : GetCurrentAppUpdateRequirement());
		ComposerBorder.Visibility = ((flag6.HasValue || agentApproval != null) ? Visibility.Collapsed : Visibility.Visible);
		AppUpdateRequiredBorder.Visibility = ((!flag6.HasValue) ? Visibility.Collapsed : Visibility.Visible);
		ApprovalBorder.Visibility = ((agentApproval == null) ? Visibility.Collapsed : Visibility.Visible);
		UpdateApprovalPresentation();
		DismissAppUpdateButton.Visibility = ((flag6 != false) ? Visibility.Collapsed : Visibility.Visible);
		bool flag7 = ShouldShowPremiumRestriction(UserDao.IsPro());
		bool flag8 = _isTokenLimitReached && !flag7 && !flag6.HasValue && agentApproval == null;
		PremiumRestrictionBorder.Visibility = ((!flag7) ? Visibility.Collapsed : Visibility.Visible);
		TokenLimitNotice.Visibility = ((!flag8) ? Visibility.Collapsed : Visibility.Visible);
		MainHomeLayout.Visibility = ((!flag4) ? Visibility.Collapsed : Visibility.Visible);
		MainHomeLayout.IsHitTestVisible = flag4;
		FloatingHomeLayout.Visibility = ((!flag5) ? Visibility.Collapsed : Visibility.Visible);
		FloatingHomeLayout.IsHitTestVisible = flag5;
		MainHomeSuggestions.IsHitTestVisible = _isConversationStateResolved;
		FloatingHomeSuggestions.IsHitTestVisible = _isConversationStateResolved;
		_attachedWebView?.SetNativeHomePresented(flag2);
		ConversationLayout.Visibility = Visibility.Visible;
		ConversationLayout.Opacity = (flag2 ? 0.0 : 1.0);
		ConversationLayout.IsHitTestVisible = !flag2;
		WebContentHost.Visibility = Visibility.Visible;
		ConversationComposerHost.Visibility = Visibility.Visible;
		GeneratedWarningText.Visibility = ((!ShouldShowGeneratedWarning(flag4, flag5, flag)) ? Visibility.Collapsed : Visibility.Visible);
		UpdateHomeComposerGlowVisibility();
		UpdateScrollToBottomButton();
	}

	internal static bool ShouldShowNativeHome(bool isConversationStateResolved, bool hasActiveConversation, bool shouldPresentWebStatus, bool isWebViewFirstLoad = false)
	{
		if (!shouldPresentWebStatus)
		{
			if (!isWebViewFirstLoad)
			{
				return !hasActiveConversation;
			}
			return true;
		}
		return false;
	}

	internal static bool ShouldUseCompactHomeLayout(double availableWidth, bool isFloatingMode)
	{
		if (!isFloatingMode)
		{
			if (availableWidth > 0.0)
			{
				return availableWidth < 500.0;
			}
			return false;
		}
		return true;
	}

	internal static bool ShouldShowHomeComposerGlow(string themeId, bool isPremium, bool isTokenLimitReached)
	{
		if (!isPremium || isTokenLimitReached)
		{
			return false;
		}
		ThemeGroupType themeGroupType = ThemeKey.GetThemeGroupType(themeId);
		if (themeGroupType != ThemeGroupType.Photo && themeGroupType != ThemeGroupType.City && themeGroupType != ThemeGroupType.Season)
		{
			return themeGroupType != ThemeGroupType.Custom;
		}
		return false;
	}

	internal static bool ShouldShowGeneratedWarning(bool showMainHome, bool showFloatingHome, bool showResolving)
	{
		if (!showFloatingHome)
		{
			return !showMainHome;
		}
		return true;
	}

	internal static bool ShouldShowPremiumRestriction(bool isPremium)
	{
		return !isPremium;
	}

	private AgentApproval GetCurrentApproval()
	{
		return GetCurrentApprovalSnapshot()?.CurrentApproval;
	}

	private AgentApprovalSnapshot GetCurrentApprovalSnapshot()
	{
		if (!_hasActiveConversation || string.IsNullOrEmpty(_currentConversationId) || !_approvalPrompts.TryGetValue(_currentConversationId, out var value))
		{
			return null;
		}
		return value;
	}

	private bool? GetCurrentAppUpdateRequirement()
	{
		if (!_hasActiveConversation)
		{
			return null;
		}
		if (string.IsNullOrEmpty(_currentConversationId))
		{
			return _pendingAppUpdateRequirement;
		}
		if (!_appUpdateRequirements.TryGetValue(_currentConversationId, out var value))
		{
			return null;
		}
		return value;
	}

	private void SetAppUpdateRequirement(string conversationId, bool blocking)
	{
		if (_appUpdateRequirements.TryGetValue(conversationId, out var value))
		{
			_appUpdateRequirements[conversationId] = value || blocking;
		}
		else
		{
			_appUpdateRequirements[conversationId] = blocking;
		}
	}

	private void SaveCurrentComposerDraft()
	{
		if (ComposerTextBox != null)
		{
			_composerDrafts.Set(_currentConversationId, ComposerTextBox.Text, _editingMessageId);
		}
	}

	private void RestoreCurrentComposerDraft()
	{
		AIComposerDraft aIComposerDraft = _composerDrafts.Get(_currentConversationId);
		_editingMessageId = aIComposerDraft.EditingMessageId;
		ComposerTextBox.Text = aIComposerDraft.Text;
		ComposerTextBox.CaretIndex = ComposerTextBox.Text.Length;
	}

	private static string NormalizeConversationId(string conversationId)
	{
		if (!string.IsNullOrWhiteSpace(conversationId))
		{
			return conversationId.Trim();
		}
		return null;
	}

	private void UpdateScrollToBottomButton()
	{
		ScrollToBottomButton.Tag = _isGenerating;
		bool flag = !_isHostLayoutPending && _isAgentBridgeReady && _isConversationStateResolved && _hasActiveConversation && _isScrollToBottomRequestedVisible;
		ScrollToBottomButton.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
		ScrollToBottomButton.IsEnabled = flag && !_isScrollingToBottom;
	}

	internal void SetHostLayoutPending(bool pending)
	{
		_isHostLayoutPending = pending;
		UpdateScrollToBottomButton();
	}

	private void MoveComposerTo(ContentControl targetHost)
	{
		if (_composerHost != targetHost)
		{
			_composerHost.Content = null;
			targetHost.Content = ComposerSection;
			_composerHost = targetHost;
			QueueComposerButtonClearanceUpdate();
		}
	}

	private static string ResolveFallbackDisplayName()
	{
		string loginUserName = LocalSettings.Settings.LoginUserName;
		if (string.IsNullOrWhiteSpace(loginUserName))
		{
			return string.Empty;
		}
		return loginUserName.Trim();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/ai/aiconversationview.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			RootLayout = (Grid)target;
			break;
		case 2:
			MainHomeLayout = (Grid)target;
			break;
		case 3:
			MainHomeGreeting = (StackPanel)target;
			break;
		case 4:
			MainGreetingText = (TextBlock)target;
			break;
		case 5:
			MainComposerRow = (Grid)target;
			break;
		case 6:
			HomeComposerGlow = (Image)target;
			break;
		case 7:
			MainComposerHost = (ContentControl)target;
			break;
		case 8:
			ComposerSection = (Grid)target;
			break;
		case 9:
			PremiumRestrictionBorder = (Border)target;
			break;
		case 10:
			((Button)target).Click += OnPremiumUpgradeClick;
			break;
		case 11:
			TokenLimitNotice = (Border)target;
			break;
		case 12:
			ComposerBorder = (Border)target;
			break;
		case 13:
			ComposerTextBox = (TextBox)target;
			ComposerTextBox.PreviewKeyDown += OnComposerPreviewKeyDown;
			ComposerTextBox.LostKeyboardFocus += OnComposerLostKeyboardFocus;
			ComposerTextBox.TextChanged += OnComposerTextChanged;
			ComposerTextBox.SizeChanged += OnComposerTextBoxSizeChanged;
			break;
		case 14:
			SubmitButton = (Button)target;
			SubmitButton.Click += OnSubmitClick;
			break;
		case 15:
			SubmitArrow = (Path)target;
			break;
		case 16:
			StopGlyph = (Rectangle)target;
			break;
		case 17:
			AppUpdateRequiredBorder = (Border)target;
			break;
		case 18:
			((Button)target).Click += OnAppUpdateClick;
			break;
		case 19:
			DismissAppUpdateButton = (Button)target;
			DismissAppUpdateButton.Click += OnDismissAppUpdateClick;
			break;
		case 20:
			ApprovalBorder = (Border)target;
			break;
		case 21:
			ApprovalHeader = (Grid)target;
			ApprovalHeader.SizeChanged += OnApprovalHeaderSizeChanged;
			break;
		case 22:
			ApprovalTitleText = (TextBlock)target;
			break;
		case 23:
			ApprovalProgressText = (TextBlock)target;
			break;
		case 24:
			ApprovalDetailsScrollViewer = (ScrollViewer)target;
			ApprovalDetailsScrollViewer.ScrollChanged += OnApprovalDetailsScrollChanged;
			break;
		case 25:
			ApprovalTaskTitles = (ItemsControl)target;
			break;
		case 26:
			ApprovalDescriptionText = (TextBlock)target;
			break;
		case 27:
			ApprovalDetailsFade = (Border)target;
			break;
		case 28:
			ApprovalErrorText = (TextBlock)target;
			break;
		case 29:
			ApprovalCancelButton = (Button)target;
			ApprovalCancelButton.Click += OnApprovalCancelClick;
			break;
		case 30:
			ApprovalConfirmButton = (Button)target;
			ApprovalConfirmButton.Click += OnApprovalConfirmClick;
			break;
		case 31:
			ApprovalMenuButton = (Button)target;
			ApprovalMenuButton.Click += OnApprovalMenuClick;
			ApprovalMenuButton.MouseEnter += OnApprovalMenuMouseEnter;
			ApprovalMenuButton.MouseLeave += OnApprovalMenuMouseLeave;
			break;
		case 32:
			ApprovalMenuArrowRotation = (RotateTransform)target;
			break;
		case 33:
			GeneratedWarningText = (TextBlock)target;
			break;
		case 34:
			MainHomeSuggestions = (ItemsControl)target;
			break;
		case 36:
			FloatingHomeLayout = (Grid)target;
			break;
		case 37:
			FloatingHomeContent = (StackPanel)target;
			break;
		case 38:
			FloatingGreetingText = (TextBlock)target;
			break;
		case 39:
			FloatingHomeSuggestions = (ItemsControl)target;
			break;
		case 41:
			FloatingComposerHost = (ContentControl)target;
			break;
		case 42:
			ConversationLayout = (Grid)target;
			break;
		case 43:
			WebContentHost = (ContentControl)target;
			break;
		case 44:
			ScrollToBottomButton = (Button)target;
			ScrollToBottomButton.Click += OnScrollToBottomClick;
			break;
		case 45:
			ConversationComposerHost = (ContentControl)target;
			break;
		case 46:
			FeedbackOverlay = (Grid)target;
			break;
		case 47:
			((Button)target).Click += OnCloseFeedbackClick;
			break;
		case 48:
			FeedbackNotHelpfulButton = (Button)target;
			FeedbackNotHelpfulButton.Click += OnFeedbackReasonClick;
			break;
		case 49:
			FeedbackMismatchButton = (Button)target;
			FeedbackMismatchButton.Click += OnFeedbackReasonClick;
			break;
		case 50:
			FeedbackIncompleteButton = (Button)target;
			FeedbackIncompleteButton.Click += OnFeedbackReasonClick;
			break;
		case 51:
			FeedbackOtherButton = (Button)target;
			FeedbackOtherButton.Click += OnFeedbackReasonClick;
			break;
		case 52:
			FeedbackCommentTextBox = (TextBox)target;
			FeedbackCommentTextBox.TextChanged += OnFeedbackCommentTextChanged;
			break;
		case 53:
			FeedbackErrorText = (TextBlock)target;
			break;
		case 54:
			((Button)target).Click += OnCloseFeedbackClick;
			break;
		case 55:
			SubmitFeedbackButton = (Button)target;
			SubmitFeedbackButton.Click += OnSubmitFeedbackClick;
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
		switch (connectionId)
		{
		case 35:
			((Button)target).Click += OnSuggestionClick;
			break;
		case 40:
			((Button)target).Click += OnSuggestionClick;
			break;
		}
	}
}
