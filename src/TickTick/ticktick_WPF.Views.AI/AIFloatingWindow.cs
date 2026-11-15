using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using System.Windows.Threading;
using Emoji.Wpf;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.AI;

public class AIFloatingWindow : UserControl, IDisposable, IComponentConnector
{
	private const string RenameConversationAction = "rename";

	private const string DeleteConversationAction = "delete";

	private readonly AIFloatingWindowTransition _transition;

	private bool _isAgentBridgeReady;

	private AIWorkspaceView _conversationWorkspace;

	private AIConversationListItem _renamingConversation;

	private bool _conversationSelectorPressedWhileOpen;

	private EscPopup _conversationMenuPopup;

	private FrameworkElement _conversationMenuButtonPressedWhileOpen;

	private bool _desktopWidgetMode;

	private bool _isInitialWebViewLoading;

	internal AIFloatingWindow Root;

	internal Grid FloatingHeader;

	internal Button ConversationSelectorButton;

	internal EmjTextBlock ConversationTitle;

	internal Path ConversationDropDownArrow;

	internal Button NewConversationButton;

	internal Button ConversationMoreButton;

	internal Popup ConversationListPopup;

	internal ListBox FloatingConversationList;

	internal Popup ConversationRenamePopup;

	internal AIConversationRenameEditor ConversationRenameTextBox;

	internal ContentControl ConversationContentHost;

	private bool _contentLoaded;

	internal static TimeSpan TransitionDuration => AIFloatingWindowTransition.TransitionDuration;

	public event EventHandler CollapseRequested;

	public event EventHandler NewConversationRequested;

	public event EventHandler DockRequested;

	public event EventHandler SettingsRequested;

	public event EventHandler OpenMainWindowRequested;

	public AIFloatingWindow()
	{
		InitializeComponent();
		AIWorkspaceView.AttachConversationPinDrag(FloatingConversationList, () => _conversationWorkspace);
		_transition = new AIFloatingWindowTransition(this);
	}

	public void EnableDesktopWidgetMode()
	{
		_desktopWidgetMode = true;
		FrameworkElementFactory frameworkElementFactory = new FrameworkElementFactory(typeof(AdornerDecorator));
		frameworkElementFactory.AppendChild(new FrameworkElementFactory(typeof(ContentPresenter)));
		base.Template = new ControlTemplate(typeof(UserControl))
		{
			VisualTree = frameworkElementFactory
		};
		UpdateConversationControls();
	}

	internal bool TryCloseTopmostPopup()
	{
		EscPopup conversationMenuPopup = _conversationMenuPopup;
		if (conversationMenuPopup != null && conversationMenuPopup.IsOpen)
		{
			CloseConversationMenu();
			return true;
		}
		if (ConversationRenamePopup.IsOpen)
		{
			if (_renamingConversation != null)
			{
				FinishConversationRenameAsync(save: false);
			}
			else
			{
				ConversationRenamePopup.IsOpen = false;
			}
			return true;
		}
		if (ConversationListPopup.IsOpen)
		{
			ConversationListPopup.IsOpen = false;
			return true;
		}
		return false;
	}

	public void SetAgentBridgeReady(bool isReady)
	{
		_isAgentBridgeReady = isReady;
		UpdateConversationControls();
	}

	public void SetConversationTitle(string title)
	{
		ConversationTitle.Text = ((_isInitialWebViewLoading || string.IsNullOrWhiteSpace(title)) ? Utils.GetString("AIAgentNewChat") : title);
	}

	internal void SetInitialWebViewLoading(bool isLoading)
	{
		_isInitialWebViewLoading = isLoading;
		UpdateConversationControls();
	}

	public void AttachConversationWorkspace(AIWorkspaceView workspace)
	{
		if (workspace == null)
		{
			throw new ArgumentNullException("workspace");
		}
		if (_conversationWorkspace != workspace)
		{
			DetachConversationWorkspace();
			_conversationWorkspace = workspace;
			_conversationWorkspace.ConversationStateChanged += OnConversationWorkspaceStateChanged;
			FloatingConversationList.ItemsSource = _conversationWorkspace.ConversationItemsView;
		}
		UpdateConversationControls();
		_conversationWorkspace.EnsureConversationsLoadedAsync();
	}

	public void WarmUp()
	{
		_transition.WarmUp();
	}

	public void DetachConversationWorkspace()
	{
		if (_conversationWorkspace != null)
		{
			_conversationWorkspace.ConversationStateChanged -= OnConversationWorkspaceStateChanged;
			_conversationWorkspace = null;
		}
		FloatingConversationList.ItemsSource = null;
		ConversationListPopup.IsOpen = false;
		ConversationRenamePopup.IsOpen = false;
		_renamingConversation = null;
		UpdateConversationControls();
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

	public void ShowAnimated()
	{
		if (_desktopWidgetMode)
		{
			_transition.ShowImmediately();
		}
		else
		{
			_transition.ShowAnimated();
		}
	}

	public void HideAnimated()
	{
		PrepareForHostChange();
		_transition.HideAnimated();
	}

	public void HideImmediately()
	{
		PrepareForHostChange();
		_transition.HideImmediately();
	}

	public void PrepareForHostChange()
	{
		CloseConversationMenu();
		ConversationListPopup.IsOpen = false;
		ConversationRenamePopup.IsOpen = false;
		InputManager.Current.PreProcessInput -= OnConversationSelectorPreProcessInput;
		ClearConversationSelectorButtonSuppression();
		if (Mouse.Captured is DependencyObject descendant && IsAncestorOf(descendant))
		{
			Mouse.Capture(null);
		}
	}

	public void Dispose()
	{
		PrepareForHostChange();
		_transition.Dispose();
		DetachConversationWorkspace();
	}

	private void OnConversationWorkspaceStateChanged(object sender, EventArgs e)
	{
		UpdateConversationControls();
	}

	private void UpdateConversationControls()
	{
		AIWorkspaceView conversationWorkspace = _conversationWorkspace;
		bool flag = conversationWorkspace != null && conversationWorkspace.ConversationItemsView?.IsEmpty == false;
		bool flag2 = conversationWorkspace?.HasCurrentConversation ?? false;
		AIConversationListItem aIConversationListItem = conversationWorkspace?.CurrentConversation;
		ConversationSelectorButton.IsEnabled = flag;
		ConversationDropDownArrow.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
		NewConversationButton.Visibility = ((!flag2) ? Visibility.Collapsed : Visibility.Visible);
		ConversationMoreButton.Visibility = ((!flag2 && !_desktopWidgetMode) ? Visibility.Collapsed : Visibility.Visible);
		NewConversationButton.IsEnabled = flag2 && _isAgentBridgeReady;
		ConversationMoreButton.IsEnabled = _desktopWidgetMode || (flag2 && _isAgentBridgeReady && conversationWorkspace != null && conversationWorkspace.CanSwitchConversation && aIConversationListItem != null);
		FloatingConversationList.IsEnabled = conversationWorkspace?.CanEditConversationPins ?? false;
		FloatingConversationList.SelectedItem = aIConversationListItem;
		if (aIConversationListItem != null)
		{
			ConversationTitle.Text = aIConversationListItem.DisplayTitle;
		}
		else if (!flag2)
		{
			ConversationTitle.Text = Utils.GetString("AIAgentNewChat");
		}
		if (!flag)
		{
			ConversationListPopup.IsOpen = false;
		}
		if (_isInitialWebViewLoading)
		{
			ConversationTitle.Text = Utils.GetString("AIAgentNewChat");
			NewConversationButton.Visibility = Visibility.Collapsed;
			ConversationMoreButton.Visibility = Visibility.Collapsed;
		}
	}

	private void OnConversationSelectorClick(object sender, RoutedEventArgs e)
	{
		if (!ConsumeConversationSelectorButtonClick())
		{
			AIWorkspaceView conversationWorkspace = _conversationWorkspace;
			if (conversationWorkspace != null && conversationWorkspace.ConversationItemsView?.IsEmpty == false)
			{
				UpdateConversationControls();
				ConversationListPopup.IsOpen = true;
				InputManager.Current.PreProcessInput -= OnConversationSelectorPreProcessInput;
				InputManager.Current.PreProcessInput += OnConversationSelectorPreProcessInput;
			}
		}
	}

	private void OnConversationListPopupClosed(object sender, EventArgs e)
	{
		InputManager.Current.PreProcessInput -= OnConversationSelectorPreProcessInput;
		UpdateConversationControls();
	}

	private bool ConsumeConversationSelectorButtonClick()
	{
		bool num = ShouldSuppressConversationSelectorPopupReopen(_conversationSelectorPressedWhileOpen, ConversationListPopup.IsOpen);
		if (num && ConversationListPopup.IsOpen)
		{
			ConversationListPopup.IsOpen = false;
		}
		ClearConversationSelectorButtonSuppression();
		return num;
	}

	private void OnConversationSelectorPreProcessInput(object sender, PreProcessInputEventArgs e)
	{
		if (e.StagingItem.Input is MouseButtonEventArgs e2 && (e2.RoutedEvent == Mouse.PreviewMouseDownEvent || e2.RoutedEvent == Mouse.PreviewMouseDownOutsideCapturedElementEvent) && e2.ChangedButton == MouseButton.Left)
		{
			_conversationSelectorPressedWhileOpen = false;
			if (ConversationListPopup.IsOpen && IsMouseWithin(ConversationSelectorButton, e2))
			{
				_conversationSelectorPressedWhileOpen = true;
				InputManager.Current.PostProcessInput -= OnConversationSelectorPostProcessInput;
				InputManager.Current.PostProcessInput += OnConversationSelectorPostProcessInput;
			}
		}
	}

	private void OnConversationSelectorPostProcessInput(object sender, ProcessInputEventArgs e)
	{
		if (e.StagingItem.Input is MouseButtonEventArgs e2 && e2.RoutedEvent == Mouse.MouseUpEvent && e2.ChangedButton == MouseButton.Left)
		{
			ClearConversationSelectorButtonSuppression();
		}
	}

	private void ClearConversationSelectorButtonSuppression()
	{
		InputManager.Current.PostProcessInput -= OnConversationSelectorPostProcessInput;
		_conversationSelectorPressedWhileOpen = false;
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

	internal static bool ShouldSuppressConversationSelectorPopupReopen(bool wasOpenOnMouseDown, bool isOpenAtClick)
	{
		return wasOpenOnMouseDown || isOpenAtClick;
	}

	private async void OnConversationListPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (!DragSortBehavior.IsDragging(FloatingConversationList) && ((!(e.OriginalSource is DependencyObject element)) ? null : (ItemsControl.ContainerFromElement(FloatingConversationList, element) as ListBoxItem))?.DataContext is AIConversationListItem conversation)
		{
			e.Handled = true;
			await SelectConversationFromPopupAsync(conversation);
		}
	}

	private async void OnConversationListPreviewKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Escape)
		{
			e.Handled = true;
			ConversationListPopup.IsOpen = false;
		}
		else if (e.Key == Key.Return && FloatingConversationList.SelectedItem is AIConversationListItem conversation)
		{
			e.Handled = true;
			await SelectConversationFromPopupAsync(conversation);
		}
	}

	private async Task SelectConversationFromPopupAsync(AIConversationListItem conversation)
	{
		ConversationListPopup.IsOpen = false;
		if (_conversationWorkspace != null)
		{
			await _conversationWorkspace.SwitchConversationAsync(conversation);
		}
		UpdateConversationControls();
	}

	private async void OnConversationListScrollChanged(object sender, ScrollChangedEventArgs e)
	{
		ScrollViewer scrollViewer = e.OriginalSource as ScrollViewer;
		if (_conversationWorkspace != null && scrollViewer != null && !(scrollViewer.VerticalOffset < scrollViewer.ScrollableHeight - 24.0) && (!(e.VerticalChange <= 0.0) || !(e.ExtentHeightChange <= 0.0)))
		{
			await _conversationWorkspace.LoadMoreConversationsAsync();
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

	private void OnConversationMoreClick(object sender, RoutedEventArgs e)
	{
		e.Handled = true;
		AIConversationListItem conversation = _conversationWorkspace?.CurrentConversation;
		if ((!_desktopWidgetMode && conversation == null) || !(sender is FrameworkElement frameworkElement) || ConsumeConversationMenuButtonClick(frameworkElement))
		{
			return;
		}
		CloseConversationMenu();
		EscPopup popup = new EscPopup
		{
			Placement = PlacementMode.Bottom,
			PlacementTarget = frameworkElement,
			HorizontalOffset = -140.0,
			VerticalOffset = 4.0,
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
		if (conversation == null)
		{
			list.Clear();
		}
		if (_desktopWidgetMode)
		{
			if (list.Count > 0)
			{
				list.Add(new CustomMenuItemViewModel(null));
			}
			list.Add(new CustomMenuItemViewModel("widgetSettings", Utils.GetString("Settings"), Utils.GetGeometry("Ic.Tabbar.SettingLinear")));
			list.Add(new CustomMenuItemViewModel("openMainWindow", Utils.GetString("OpenMainWindow"), Utils.GetGeometry("Ic.Focus.PopupWindow")));
		}
		CustomMenuItemViewModel customMenuItemViewModel = AIWorkspaceView.CreatePinMenuItem(conversation);
		if (customMenuItemViewModel != null)
		{
			list.Insert(1, customMenuItemViewModel);
		}
		CustomMenuList customMenuList = new CustomMenuList(list, popup);
		customMenuList.MinWidth = 144.0;
		customMenuList.Closed += delegate
		{
			if (_conversationMenuPopup == popup)
			{
				InputManager.Current.PreProcessInput -= OnConversationMenuPreProcessInput;
				_conversationMenuPopup = null;
			}
		};
		customMenuList.Operated += delegate(object menuSender, object action)
		{
			if (object.Equals(action, "widgetSettings"))
			{
				this.SettingsRequested?.Invoke(this, EventArgs.Empty);
			}
			else if (object.Equals(action, "openMainWindow"))
			{
				this.OpenMainWindowRequested?.Invoke(this, EventArgs.Empty);
			}
			else if (object.Equals(action, "rename"))
			{
				base.Dispatcher.BeginInvoke(DispatcherPriority.Input, (Action)delegate
				{
					BeginConversationRename(conversation);
				});
			}
			else if (object.Equals(action, "pin"))
			{
				_conversationWorkspace?.ToggleConversationPin(conversation);
			}
			else if (object.Equals(action, "delete"))
			{
				DeleteConversationAsync(conversation);
			}
		};
		customMenuList.Show();
	}

	private void BeginConversationRename(AIConversationListItem conversation)
	{
		if (conversation != null && _conversationWorkspace != null)
		{
			_renamingConversation = conversation;
			ConversationRenameTextBox.Text = conversation.DisplayTitle;
			ConversationRenamePopup.IsOpen = true;
		}
	}

	private void OnConversationRenamePopupOpened(object sender, EventArgs e)
	{
		AIConversationListItem conversation = _renamingConversation;
		base.Dispatcher.BeginInvoke(DispatcherPriority.Input, (Action)delegate
		{
			if (ConversationRenamePopup.IsOpen && conversation != null && _renamingConversation == conversation)
			{
				HwndHelper.SetFocus(ConversationRenamePopup);
				ConversationRenameTextBox.Focus();
				ConversationRenameTextBox.SelectAll();
			}
		});
	}

	private async void OnConversationRenamePreviewKeyDown(object sender, KeyEventArgs e)
	{
		if (!ConversationRenameTextBox.IsTextCompositionActive)
		{
			if (e.Key == Key.Return)
			{
				e.Handled = true;
				await FinishConversationRenameAsync(save: true);
			}
			else if (e.Key == Key.Escape)
			{
				e.Handled = true;
				await FinishConversationRenameAsync(save: false);
			}
		}
	}

	private async void OnConversationRenamePopupClosed(object sender, EventArgs e)
	{
		if (_renamingConversation != null)
		{
			await FinishConversationRenameAsync(save: true);
		}
	}

	private async Task FinishConversationRenameAsync(bool save)
	{
		AIConversationListItem renamingConversation = _renamingConversation;
		if (renamingConversation != null)
		{
			_renamingConversation = null;
			string text = ConversationRenameTextBox.Text;
			if (ConversationRenamePopup.IsOpen)
			{
				ConversationRenamePopup.IsOpen = false;
			}
			if (save && _conversationWorkspace != null)
			{
				await _conversationWorkspace.RenameConversationAsync(renamingConversation, text);
			}
			UpdateConversationControls();
		}
	}

	private async Task DeleteConversationAsync(AIConversationListItem conversation)
	{
		if (_conversationWorkspace != null)
		{
			await _conversationWorkspace.DeleteConversationAsync(conversation, Window.GetWindow(this));
			UpdateConversationControls();
		}
	}

	private void OnCollapseClick(object sender, RoutedEventArgs e)
	{
		this.CollapseRequested?.Invoke(this, EventArgs.Empty);
	}

	private void OnNewConversationClick(object sender, RoutedEventArgs e)
	{
		this.NewConversationRequested?.Invoke(this, EventArgs.Empty);
	}

	private void OnDockClick(object sender, RoutedEventArgs e)
	{
		this.DockRequested?.Invoke(this, EventArgs.Empty);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/ai/aifloatingwindow.xaml", UriKind.Relative);
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
			Root = (AIFloatingWindow)target;
			break;
		case 2:
			FloatingHeader = (Grid)target;
			break;
		case 3:
			ConversationSelectorButton = (Button)target;
			ConversationSelectorButton.Click += OnConversationSelectorClick;
			break;
		case 4:
			ConversationTitle = (EmjTextBlock)target;
			break;
		case 5:
			ConversationDropDownArrow = (Path)target;
			break;
		case 6:
			NewConversationButton = (Button)target;
			NewConversationButton.Click += OnNewConversationClick;
			break;
		case 7:
			((Button)target).Click += OnDockClick;
			break;
		case 8:
			ConversationMoreButton = (Button)target;
			ConversationMoreButton.Click += OnConversationMoreClick;
			break;
		case 9:
			((Button)target).Click += OnCollapseClick;
			break;
		case 10:
			ConversationListPopup = (Popup)target;
			ConversationListPopup.Closed += OnConversationListPopupClosed;
			break;
		case 11:
			FloatingConversationList = (ListBox)target;
			FloatingConversationList.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(OnConversationListScrollChanged));
			FloatingConversationList.PreviewMouseLeftButtonUp += OnConversationListPreviewMouseLeftButtonUp;
			FloatingConversationList.PreviewKeyDown += OnConversationListPreviewKeyDown;
			break;
		case 12:
			ConversationRenamePopup = (Popup)target;
			ConversationRenamePopup.Opened += OnConversationRenamePopupOpened;
			ConversationRenamePopup.Closed += OnConversationRenamePopupClosed;
			break;
		case 13:
			ConversationRenameTextBox = (AIConversationRenameEditor)target;
			break;
		case 14:
			ConversationContentHost = (ContentControl)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
