using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc.ColorSelector;

namespace ticktick_WPF.Views.MarkDown;

public class EditorMenu : UserControl, IComponentConnector
{
	private readonly HighlightColorMenuState _highlightColorState = new HighlightColorMenuState();

	private static readonly HashSet<string> NonSelectableActions = new HashSet<string> { "NumberedList", "BulletList", "CheckItem", "Quote" };

	public Func<string> GetCurrentStyle;

	public static readonly DependencyProperty HighlightBackgroundProperty = DependencyProperty.Register("HighlightBackground", typeof(string), typeof(EditorMenu));

	public static readonly DependencyProperty IsImmersiveModeProperty = DependencyProperty.Register("IsImmersiveMode", typeof(bool), typeof(EditorMenu), new PropertyMetadata(false, null));

	public static readonly DependencyProperty IsFlatModeProperty = DependencyProperty.Register("IsFlatMode", typeof(bool), typeof(EditorMenu), new PropertyMetadata(true, null));

	public static readonly DependencyProperty HeadingProperty = DependencyProperty.Register("Heading", typeof(string), typeof(EditorMenu), new PropertyMetadata(string.Empty, null));

	public static readonly DependencyProperty CurrentActionProperty = DependencyProperty.Register("CurrentAction", typeof(string), typeof(EditorMenu), new PropertyMetadata(string.Empty, null));

	public static readonly DependencyProperty IsHighlightColorActiveProperty = DependencyProperty.Register("IsHighlightColorActive", typeof(bool), typeof(EditorMenu), new PropertyMetadata(false, null));

	private readonly List<FrameworkElement> _moreOptionElements = new List<FrameworkElement>();

	private readonly HashSet<string> _activeActions = new HashSet<string>();

	private DateTime? _headerClosedTime;

	private string _highlightContextColor;

	private Window _colorPopupOwnerWindow;

	private bool _isPopMode = true;

	internal EditorMenu Root;

	internal new ContentControl Content;

	internal ItemsControl EditorPanel;

	internal ContentControl NavigateBackBtn;

	internal ContentControl ExitImmersiveBtn;

	internal ContentControl EnterImmersiveBtn;

	internal Grid ImmersiveSeparator;

	internal ContentControl HeaderButton;

	internal Border HeaderActiveBackground;

	internal Path HeaderIconPath;

	internal Popup HeaderPopup;

	internal Border Heading1Grid;

	internal Border Heading2Grid;

	internal Border Heading3Grid;

	internal ContentControl BoldButton;

	internal Border BoldActiveBackground;

	internal Path BoldIconPath;

	internal ContentControl HighlightButton;

	internal Border HighlightActiveBackground;

	internal Path HighlightIconPath;

	internal Border HighlightColorHost;

	internal Border HighLightBorder;

	internal Border HighlightColorActiveBackground;

	internal Popup ColorSelectorPopup;

	internal HighlightColorMenu HighlightColorMenu;

	internal Border HighlightArrowBorder;

	internal ContentControl CheckItemButton;

	internal Border CheckItemActiveBackground;

	internal Path CheckItemIconPath;

	internal ContentControl BulletListButton;

	internal Border BulletListActiveBackground;

	internal Path BulletListIconPath;

	internal ContentControl NumberedListButton;

	internal Border NumberedListActiveBackground;

	internal Path NumberedListIconPath;

	internal ContentControl ItalicButton;

	internal Border ItalicActiveBackground;

	internal Path ItalicIconPath;

	internal ContentControl UnderLineButton;

	internal Border UnderLineActiveBackground;

	internal Path UnderLineIconPath;

	internal ContentControl StrikeThroughButton;

	internal Border StrikeThroughActiveBackground;

	internal Path StrikeThroughIconPath;

	internal ContentControl DateItem;

	internal Popup DatePopup;

	internal ContentControl LinkButton;

	internal Border LinkActiveBackground;

	internal Path LinkIconPath;

	internal ContentControl CodeButton;

	internal Border CodeActiveBackground;

	internal Path CodeIconPath;

	internal ContentControl QuoteButton;

	internal Border QuoteActiveBackground;

	internal Path QuoteIconPath;

	internal Grid Split;

	internal ContentControl Attachment;

	internal Grid MoreOptionGrid;

	internal ContentControl MoreGrid;

	internal Popup MoreOptionPopup;

	internal ContentControl MoreItalicItem;

	internal Border MoreItalicActiveBackground;

	internal Path MoreItalicIconPath;

	internal ContentControl MoreUnderLineItem;

	internal Border MoreUnderLineActiveBackground;

	internal Path MoreUnderLineIconPath;

	internal ContentControl MoreStrikeThroughItem;

	internal Border MoreStrikeThroughActiveBackground;

	internal Path MoreStrikeThroughIconPath;

	internal ContentControl MoreLineItem;

	internal ContentControl ExtraDateItem;

	internal Popup ExtraDatePopup;

	internal Grid MoreSplit;

	internal ContentControl MoreLink;

	internal Border MoreLinkActiveBackground;

	internal Path MoreLinkIconPath;

	internal ContentControl MoreCodeItem;

	internal Border MoreCodeActiveBackground;

	internal Path MoreCodeIconPath;

	internal ContentControl MoreQuote;

	internal Border MoreQuoteActiveBackground;

	internal Path MoreQuoteIconPath;

	internal Grid MoreSplit2;

	internal ContentControl MoreAttachment;

	private bool _contentLoaded;

	public string HighlightBackground
	{
		get
		{
			return (string)GetValue(HighlightBackgroundProperty);
		}
		set
		{
			SetValue(HighlightBackgroundProperty, value);
		}
	}

	public bool UseTaskDetailToolbarTracking { get; set; }

	public string Heading
	{
		get
		{
			return (string)GetValue(HeadingProperty);
		}
		set
		{
			SetValue(HeadingProperty, value);
			UpdateActionVisuals();
		}
	}

	public string CurrentAction
	{
		get
		{
			return (string)GetValue(CurrentActionProperty);
		}
		set
		{
			SetValue(CurrentActionProperty, value);
			UpdateActionVisuals();
		}
	}

	public bool IsHighlightColorActive
	{
		get
		{
			return (bool)GetValue(IsHighlightColorActiveProperty);
		}
		private set
		{
			SetValue(IsHighlightColorActiveProperty, value);
		}
	}

	public bool IsFlatMode
	{
		get
		{
			return (bool)GetValue(IsFlatModeProperty);
		}
		set
		{
			SetValue(IsFlatModeProperty, value);
		}
	}

	public bool IsImmersiveMode
	{
		get
		{
			return (bool)GetValue(IsImmersiveModeProperty);
		}
		set
		{
			SetValue(IsImmersiveModeProperty, value);
		}
	}

	public bool IsPopMode
	{
		get
		{
			return _isPopMode;
		}
		set
		{
			_isPopMode = value;
			OnPopModeSet(value);
		}
	}

	public event EventHandler EnterImmersive;

	public event EventHandler ExitImmersive;

	public event EventHandler<string> EditorAction;

	public event EventHandler<ColorExtra> HighlightColorRequested;

	public event EventHandler NavigateBack;

	private void TrackToolbarClick(string label, string legacyLabel = null, bool taskDetailOnly = false)
	{
		if (UseTaskDetailToolbarTracking)
		{
			UserActCollectUtils.AddClickEvent("task_detail", "md_toolbar_v2", label);
		}
		else if (!taskDetailOnly)
		{
			UserActCollectUtils.AddClickEvent("description", "md_toolbar", legacyLabel ?? label);
		}
	}

	public EditorMenu()
	{
		InitializeComponent();
		HighlightBackground = _highlightColorState.GetIconBackground();
		base.Loaded += OnEditorMenuLoaded;
		base.Unloaded += OnEditorMenuUnloaded;
		base.IsVisibleChanged += OnEditorMenuIsVisibleChanged;
	}

	private void OnEditorMenuLoaded(object sender, RoutedEventArgs e)
	{
		base.Loaded -= OnEditorMenuLoaded;
		InitMoreOptions();
		InitEvents();
		UpdateActionVisuals();
	}

	public void UpdateActiveActions(IEnumerable<string> actions)
	{
		_activeActions.Clear();
		if (actions != null)
		{
			foreach (string item in actions.Where(ShouldShowActiveState))
			{
				_activeActions.Add(item);
			}
		}
		UpdateActionVisuals();
	}

	private static bool ShouldShowActiveState(string action)
	{
		if (!string.IsNullOrEmpty(action))
		{
			return !NonSelectableActions.Contains(action);
		}
		return false;
	}

	public void ApplyEditorState(string action, IEnumerable<string> activeActions, string highlightContextColor = null)
	{
		_highlightContextColor = HighlightColorMenuState.NormalizeColor(highlightContextColor);
		HighlightBackground = _highlightColorState.GetIconBackground(_highlightContextColor);
		UpdateActiveActions(activeActions);
		CurrentAction = action ?? string.Empty;
		Heading = (IsHeadingAction(action) ? action : string.Empty);
	}

	private void OnPopModeSet(bool value)
	{
		if (!value)
		{
			Content.Style = null;
			NavigateBackBtn.Visibility = Visibility.Collapsed;
			ExitImmersiveBtn.Visibility = Visibility.Collapsed;
			EnterImmersiveBtn.Visibility = Visibility.Collapsed;
			ImmersiveSeparator.Visibility = Visibility.Collapsed;
			MoreAttachment.Visibility = Visibility.Collapsed;
			MoreSplit2.Visibility = Visibility.Collapsed;
			Attachment.Visibility = Visibility.Collapsed;
			Split.Visibility = Visibility.Collapsed;
		}
	}

	private void InitMoreOptions()
	{
		_moreOptionElements.Add(MoreItalicItem);
		_moreOptionElements.Add(MoreUnderLineItem);
		_moreOptionElements.Add(MoreStrikeThroughItem);
		_moreOptionElements.Add(MoreLineItem);
		_moreOptionElements.Add(ExtraDateItem);
		_moreOptionElements.Add(MoreSplit);
		_moreOptionElements.Add(MoreLink);
		_moreOptionElements.Add(MoreCodeItem);
		_moreOptionElements.Add(MoreQuote);
		if (_isPopMode)
		{
			_moreOptionElements.Add(MoreSplit2);
			_moreOptionElements.Add(MoreAttachment);
		}
		SetMoreIcons();
		SetupTooltips();
	}

	private void SetupTooltips()
	{
		Heading1Grid.ToolTip = new ToolTip
		{
			Content = "#" + Utils.GetString("heading_1") + " (Alt+Ctrl+1)",
			Placement = PlacementMode.Right
		};
		Heading2Grid.ToolTip = new ToolTip
		{
			Content = "#" + Utils.GetString("heading_2") + " (Alt+Ctrl+2)",
			Placement = PlacementMode.Right
		};
		Heading3Grid.ToolTip = new ToolTip
		{
			Content = "#" + Utils.GetString("heading_3") + " (Alt+Ctrl+3)",
			Placement = PlacementMode.Right
		};
		Heading1Grid.DataContext = this;
		Heading2Grid.DataContext = this;
		Heading3Grid.DataContext = this;
	}

	private void InitEvents()
	{
		HeaderPopup.Closed -= OnHeaderOpened;
		HeaderPopup.Closed += OnHeaderOpened;
		DatePopup.Closed -= OnHeaderOpened;
		DatePopup.Closed += OnHeaderOpened;
		HighlightColorMenu.HighlightColorSelected -= OnHighlightColorSelected;
		HighlightColorMenu.HighlightColorSelected += OnHighlightColorSelected;
		HighlightColorMenu.MouseLeave -= OnHighlightColorMenuMouseLeave;
		HighlightColorMenu.MouseLeave += OnHighlightColorMenuMouseLeave;
		ColorSelectorPopup.Opened -= OnColorSelectorPopupOpened;
		ColorSelectorPopup.Opened += OnColorSelectorPopupOpened;
		ColorSelectorPopup.Closed -= OnColorSelectorPopupClosed;
		ColorSelectorPopup.Closed += OnColorSelectorPopupClosed;
		base.PreviewMouseMove -= OnEditorMenuPreviewMouseMove;
		base.PreviewMouseMove += OnEditorMenuPreviewMouseMove;
		base.PreviewMouseDown -= OnEditorMenuPreviewMouseDown;
		base.PreviewMouseDown += OnEditorMenuPreviewMouseDown;
	}

	private void OnHeaderOpened(object sender, EventArgs e)
	{
		_headerClosedTime = DateTime.Now;
	}

	private void OnExitClick(object sender, MouseButtonEventArgs e)
	{
		TrackToolbarClick("fullscreen", "full-screen");
		this.ExitImmersive?.Invoke(sender, e);
	}

	private void OnStyleClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement { Tag: string tag })
		{
			e.Handled = true;
			DismissColorPopup();
			MoreOptionPopup.IsOpen = false;
			HeaderPopup.IsOpen = false;
			TrackToolbarClick(MarkDownAction.TryGetLabel(tag));
			this.EditorAction?.Invoke(sender, tag);
			if (ShouldApplyClickedActionToHeading(tag))
			{
				Heading = tag;
				UpdateActionVisuals();
			}
		}
	}

	private static bool ShouldApplyClickedActionToHeading(string action)
	{
		if (!IsHeadingAction(action))
		{
			return action == "Text";
		}
		return true;
	}

	private static bool IsHeadingAction(string action)
	{
		if (!(action == "Heading1") && !(action == "Heading2"))
		{
			return action == "Heading3";
		}
		return true;
	}

	private void UpdateActionVisuals()
	{
		if (base.IsLoaded)
		{
			bool isActive = IsHeadingAction(Heading);
			SetActionVisual(HeaderActiveBackground, HeaderIconPath, isActive);
			SetActionVisual(BoldActiveBackground, BoldIconPath, _activeActions.Contains("Bold"));
			SetHighlightActionVisual(IsHighlightActive());
			SetActionVisual(CheckItemActiveBackground, CheckItemIconPath, _activeActions.Contains("CheckItem"));
			SetActionVisual(BulletListActiveBackground, BulletListIconPath, _activeActions.Contains("BulletList"));
			SetActionVisual(NumberedListActiveBackground, NumberedListIconPath, _activeActions.Contains("NumberedList"));
			SetActionVisual(ItalicActiveBackground, ItalicIconPath, _activeActions.Contains("Italic"));
			SetActionVisual(MoreItalicActiveBackground, MoreItalicIconPath, _activeActions.Contains("Italic"));
			SetActionVisual(UnderLineActiveBackground, UnderLineIconPath, _activeActions.Contains("UnderLine"));
			SetActionVisual(MoreUnderLineActiveBackground, MoreUnderLineIconPath, _activeActions.Contains("UnderLine"));
			SetActionVisual(StrikeThroughActiveBackground, StrikeThroughIconPath, _activeActions.Contains("StrikeThrough"));
			SetActionVisual(MoreStrikeThroughActiveBackground, MoreStrikeThroughIconPath, _activeActions.Contains("StrikeThrough"));
			SetActionVisual(LinkActiveBackground, LinkIconPath, _activeActions.Contains("Link"));
			SetActionVisual(MoreLinkActiveBackground, MoreLinkIconPath, _activeActions.Contains("Link"));
			SetActionVisual(CodeActiveBackground, CodeIconPath, _activeActions.Contains("Code"));
			SetActionVisual(MoreCodeActiveBackground, MoreCodeIconPath, _activeActions.Contains("Code"));
			SetActionVisual(QuoteActiveBackground, QuoteIconPath, _activeActions.Contains("Quote"));
			SetActionVisual(MoreQuoteActiveBackground, MoreQuoteIconPath, _activeActions.Contains("Quote"));
		}
	}

	private bool IsHighlightActive()
	{
		if (!_activeActions.Contains("HighLight"))
		{
			return _activeActions.Contains("HighLightEx");
		}
		return true;
	}

	private void SetHighlightActionVisual(bool isActive)
	{
		IsHighlightColorActive = isActive;
		SetActionVisual(HighlightActiveBackground, HighlightIconPath, isActive);
	}

	private static void SetActionVisual(Border background, Shape icon, bool isActive)
	{
		SetBackgroundVisibility(background, isActive);
		icon?.SetResourceReference(Shape.FillProperty, "BaseColorOpacity100");
	}

	private static void SetBackgroundVisibility(Border background, bool isActive)
	{
		if (background != null)
		{
			background.Visibility = ((!isActive) ? Visibility.Collapsed : Visibility.Visible);
		}
	}

	private void OnExtraDateClick(object sender, MouseButtonEventArgs e)
	{
		DismissColorPopup();
		ShowDatePopup(ExtraDatePopup);
		e.Handled = true;
	}

	private void OnDateClick(object sender, MouseButtonEventArgs e)
	{
		DismissColorPopup();
		ShowDatePopup(DatePopup);
		e.Handled = true;
	}

	private void ShowDatePopup(Popup datePopup)
	{
		if (datePopup.IsOpen)
		{
			datePopup.IsOpen = false;
		}
		else if (!_headerClosedTime.HasValue || (DateTime.Now - _headerClosedTime.Value).TotalMilliseconds > 200.0)
		{
			DateTime now = DateTime.Now;
			string text = DateUtils.FormatYearMonthDay(now, DateFormatLen.Middle);
			string timeText = DateUtils.GetTimeText(now);
			string text2 = text + " " + timeText;
			string text3 = DateUtils.FormatYearMonthDay(now, DateFormatLen.Short);
			CustomMenuList customMenuList = new CustomMenuList(new List<CustomMenuItemViewModel>
			{
				new CustomMenuItemViewModel((object)text2, text2, (Geometry)null, "")
				{
					FontSize = 11
				},
				new CustomMenuItemViewModel((object)text, text, (Geometry)null, "")
				{
					FontSize = 11
				},
				new CustomMenuItemViewModel((object)text3, text3, (Geometry)null, "")
				{
					FontSize = 11
				},
				new CustomMenuItemViewModel((object)timeText, timeText, (Geometry)null, "")
				{
					FontSize = 11
				}
			}, datePopup);
			customMenuList.Operated += delegate(object o, object tag)
			{
				TrackToolbarClick("time");
				this.EditorAction?.Invoke(null, "DateTime_" + tag);
			};
			customMenuList.Show();
		}
	}

	private void OnHeaderClick(object sender, MouseButtonEventArgs e)
	{
		TrackToolbarClick("header", null, taskDetailOnly: true);
		DismissColorPopup();
		if (HeaderPopup.IsOpen)
		{
			HeaderPopup.IsOpen = false;
		}
		else if (!_headerClosedTime.HasValue || (DateTime.Now - _headerClosedTime.Value).TotalMilliseconds > 200.0)
		{
			if (GetCurrentStyle != null)
			{
				Heading = GetCurrentStyle?.Invoke();
			}
			HeaderPopup.IsOpen = true;
		}
		e.Handled = true;
	}

	private void OnMenuSizeChanged(object sender, SizeChangedEventArgs e)
	{
		SetMoreIcons();
	}

	private void SetMoreIcons()
	{
		string itemsInFirstRow = GetItemsInFirstRow(EditorPanel);
		MoreGrid.Visibility = (((Attachment.Visibility == Visibility.Visible && itemsInFirstRow == "Attachment") || (Attachment.Visibility != Visibility.Visible && itemsInFirstRow == "Quote")) ? Visibility.Collapsed : Visibility.Visible);
		if (!MoreGrid.IsVisible)
		{
			return;
		}
		bool flag = true;
		for (int num = _moreOptionElements.Count - 1; num >= 0; num--)
		{
			if (_moreOptionElements[num].Tag.ToString() == itemsInFirstRow || (_moreOptionElements[num].Tag.ToString() == "SplitSplitLine" && itemsInFirstRow == "DateTime") || (_moreOptionElements[num].Tag.ToString() == "SplitQuote" && itemsInFirstRow == "Quote"))
			{
				flag = false;
			}
			_moreOptionElements[num].Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
		}
	}

	private void OnMoreClick(object sender, MouseButtonEventArgs e)
	{
		DismissColorPopup();
		MoreOptionPopup.IsOpen = !MoreOptionPopup.IsOpen;
	}

	private void OnImmersiveClick(object sender, MouseButtonEventArgs e)
	{
		TrackToolbarClick("fullscreen", "full-screen");
		this.EnterImmersive?.Invoke(this, null);
	}

	private static string GetItemsInFirstRow(ItemsControl itemsControl)
	{
		double num = -1.0;
		string result = string.Empty;
		for (int i = 8; i < itemsControl.Items.Count; i++)
		{
			FrameworkElement frameworkElement = (FrameworkElement)itemsControl.ItemContainerGenerator.ContainerFromIndex(i);
			if (frameworkElement == null)
			{
				break;
			}
			double x = frameworkElement.TranslatePoint(default(Point), itemsControl).X;
			if (x <= num && x > 0.0)
			{
				break;
			}
			if (frameworkElement.Tag is string text && text != "SplitSplitLine" && text != "SplitQuote" && frameworkElement.IsVisible)
			{
				result = text;
			}
			num = x;
		}
		return result;
	}

	public void HideImmersive()
	{
		EnterImmersiveBtn.Visibility = Visibility.Collapsed;
		ImmersiveSeparator.Visibility = Visibility.Collapsed;
	}

	public void ShowImmersive()
	{
		EnterImmersiveBtn.Visibility = Visibility.Visible;
		ImmersiveSeparator.Visibility = Visibility.Visible;
	}

	private void OnNavigateBackClick(object sender, MouseButtonEventArgs e)
	{
		this.NavigateBack?.Invoke(this, null);
	}

	public void SetReadOnly(bool readOnly)
	{
		for (int i = 2; i < EditorPanel.Items.Count; i++)
		{
			UIElement obj = (UIElement)EditorPanel.ItemContainerGenerator.ContainerFromIndex(i);
			obj.IsEnabled = !readOnly;
			obj.Opacity = (readOnly ? 0.3f : 1f);
		}
	}

	private void OnHighlightEnter(object sender, MouseEventArgs e)
	{
		ShowSelectColorPopup();
	}

	private void OnHighlightArrowMouseUp(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		ShowSelectColorPopup();
	}

	private void OnHighlightColorSelected(object sender, string color)
	{
		TrackToolbarClick("highlight", null, taskDetailOnly: true);
		color = _highlightColorState.SelectColor(color);
		_highlightContextColor = color;
		HighlightBackground = _highlightColorState.GetIconBackground();
		NotifyHighlightColorRequested(sender, color);
		ColorSelectorPopup.IsOpen = false;
	}

	private void OnColorClick(object sender, MouseButtonEventArgs e)
	{
		TrackToolbarClick("highlight", null, taskDetailOnly: true);
		e.Handled = true;
		NotifyHighlightColorRequested(sender, _highlightColorState.GetApplyColor(_highlightContextColor));
		DismissColorPopup();
	}

	private void NotifyHighlightColorRequested(object sender, string color)
	{
		this.HighlightColorRequested?.Invoke(sender, HighlightColorMenuState.CreateHighlightColor(color));
	}

	private void ShowSelectColorPopup()
	{
		if (!ColorSelectorPopup.IsOpen)
		{
			_highlightColorState.IsPopupOpen = false;
			HighlightColorMenu.SelectedColor = _highlightColorState.GetMenuSelectedColor(_highlightContextColor);
			ColorSelectorPopup.IsOpen = true;
		}
	}

	private void OnColorSelectorPopupOpened(object sender, EventArgs e)
	{
		_highlightColorState.IsPopupOpen = true;
		AttachColorPopupDismissHandlers();
	}

	private void OnColorSelectorPopupClosed(object sender, EventArgs e)
	{
		_highlightColorState.IsPopupOpen = false;
		DetachColorPopupDismissHandlers();
	}

	private void OnHighlightColorMenuMouseLeave(object sender, MouseEventArgs e)
	{
		DismissColorPopup();
	}

	private void OnHighlightColorHostMouseLeave(object sender, MouseEventArgs e)
	{
		if (ColorSelectorPopup.IsOpen && sender is FrameworkElement relativeTo && e.GetPosition(relativeTo).Y < 0.0)
		{
			DismissColorPopup();
		}
	}

	private void OnEditorMenuPreviewMouseMove(object sender, MouseEventArgs e)
	{
		if (ColorSelectorPopup.IsOpen && ShouldDismissColorPopupForMenuHover(e.OriginalSource as DependencyObject))
		{
			DismissColorPopup();
		}
	}

	private void OnEditorMenuPreviewMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (ColorSelectorPopup.IsOpen && !IsColorPopupInteractiveSource(e.OriginalSource as DependencyObject))
		{
			DismissColorPopup();
		}
	}

	private void OnColorPopupOwnerPreviewMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (ColorSelectorPopup.IsOpen && !IsColorPopupInteractiveSource(e.OriginalSource as DependencyObject))
		{
			DismissColorPopup();
		}
	}

	private void OnColorPopupOwnerDeactivated(object sender, EventArgs e)
	{
		DismissColorPopup();
	}

	private void OnColorPopupOwnerStateChanged(object sender, EventArgs e)
	{
		if (sender is Window { WindowState: WindowState.Minimized })
		{
			DismissColorPopup();
		}
	}

	private void OnEditorMenuUnloaded(object sender, RoutedEventArgs e)
	{
		DismissColorPopup();
		DetachColorPopupDismissHandlers();
	}

	private void OnEditorMenuIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (object.Equals(e.NewValue, false))
		{
			DismissColorPopup();
		}
	}

	private void DismissColorPopup()
	{
		if (ColorSelectorPopup.IsOpen)
		{
			ColorSelectorPopup.IsOpen = false;
		}
		_highlightColorState.IsPopupOpen = false;
		DetachColorPopupDismissHandlers();
	}

	private void AttachColorPopupDismissHandlers()
	{
		DetachColorPopupDismissHandlers();
		_colorPopupOwnerWindow = ResolveColorPopupOwnerWindow();
		if (_colorPopupOwnerWindow != null)
		{
			_colorPopupOwnerWindow.PreviewMouseDown += OnColorPopupOwnerPreviewMouseDown;
			_colorPopupOwnerWindow.Deactivated += OnColorPopupOwnerDeactivated;
			_colorPopupOwnerWindow.StateChanged += OnColorPopupOwnerStateChanged;
		}
	}

	private void DetachColorPopupDismissHandlers()
	{
		if (_colorPopupOwnerWindow != null)
		{
			_colorPopupOwnerWindow.PreviewMouseDown -= OnColorPopupOwnerPreviewMouseDown;
			_colorPopupOwnerWindow.Deactivated -= OnColorPopupOwnerDeactivated;
			_colorPopupOwnerWindow.StateChanged -= OnColorPopupOwnerStateChanged;
			_colorPopupOwnerWindow = null;
		}
	}

	private Window ResolveColorPopupOwnerWindow()
	{
		object obj = Window.GetWindow(this);
		if (obj == null)
		{
			obj = Window.GetWindow(ColorSelectorPopup.PlacementTarget);
			if (obj == null)
			{
				obj = Application.Current?.Windows.OfType<Window>().FirstOrDefault((Window window) => window.IsActive);
				if (obj == null)
				{
					Application current = Application.Current;
					if (current == null)
					{
						return null;
					}
					obj = current.MainWindow;
				}
			}
		}
		return (Window)obj;
	}

	private bool IsColorPopupInteractiveSource(DependencyObject source)
	{
		if (!IsSelfOrDescendant(source, HighlightColorHost) && !IsSelfOrDescendant(source, ColorSelectorPopup.Child))
		{
			return IsSelfOrDescendant(source, HighlightColorMenu);
		}
		return true;
	}

	private bool ShouldDismissColorPopupForMenuHover(DependencyObject source)
	{
		if (IsColorPopupInteractiveSource(source))
		{
			return false;
		}
		if (TryFindMenuActionTag(source, out var action) && action != "HighLight")
		{
			return action != "HighLightEx";
		}
		return false;
	}

	private static bool TryFindMenuActionTag(DependencyObject source, out string action)
	{
		while (source != null)
		{
			if (source is FrameworkElement { Tag: string tag } && !string.IsNullOrEmpty(tag))
			{
				action = tag;
				return true;
			}
			source = GetParent(source);
		}
		action = null;
		return false;
	}

	private static bool IsSelfOrDescendant(DependencyObject source, DependencyObject ancestor)
	{
		while (source != null)
		{
			if (source == ancestor)
			{
				return true;
			}
			source = GetParent(source);
		}
		return false;
	}

	private static DependencyObject GetParent(DependencyObject element)
	{
		if (element is FrameworkElement { Parent: not null } frameworkElement)
		{
			return frameworkElement.Parent;
		}
		if (element is FrameworkContentElement { Parent: not null } frameworkContentElement)
		{
			return frameworkContentElement.Parent;
		}
		try
		{
			return VisualTreeHelper.GetParent(element);
		}
		catch (InvalidOperationException)
		{
			return null;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/markdown/editormenu.xaml", UriKind.Relative);
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
			Root = (EditorMenu)target;
			Root.SizeChanged += OnMenuSizeChanged;
			break;
		case 2:
			Content = (ContentControl)target;
			break;
		case 3:
			EditorPanel = (ItemsControl)target;
			break;
		case 4:
			NavigateBackBtn = (ContentControl)target;
			NavigateBackBtn.MouseLeftButtonUp += OnNavigateBackClick;
			break;
		case 5:
			ExitImmersiveBtn = (ContentControl)target;
			ExitImmersiveBtn.MouseLeftButtonUp += OnExitClick;
			break;
		case 6:
			EnterImmersiveBtn = (ContentControl)target;
			EnterImmersiveBtn.MouseLeftButtonUp += OnImmersiveClick;
			break;
		case 7:
			ImmersiveSeparator = (Grid)target;
			break;
		case 8:
			HeaderButton = (ContentControl)target;
			HeaderButton.MouseLeftButtonUp += OnHeaderClick;
			break;
		case 9:
			HeaderActiveBackground = (Border)target;
			break;
		case 10:
			HeaderIconPath = (Path)target;
			break;
		case 11:
			HeaderPopup = (Popup)target;
			break;
		case 12:
			Heading1Grid = (Border)target;
			Heading1Grid.MouseLeftButtonUp += OnStyleClick;
			break;
		case 13:
			Heading2Grid = (Border)target;
			Heading2Grid.MouseLeftButtonUp += OnStyleClick;
			break;
		case 14:
			Heading3Grid = (Border)target;
			Heading3Grid.MouseLeftButtonUp += OnStyleClick;
			break;
		case 15:
			BoldButton = (ContentControl)target;
			BoldButton.MouseLeftButtonUp += OnStyleClick;
			break;
		case 16:
			BoldActiveBackground = (Border)target;
			break;
		case 17:
			BoldIconPath = (Path)target;
			break;
		case 18:
			HighlightButton = (ContentControl)target;
			HighlightButton.MouseLeftButtonUp += OnStyleClick;
			break;
		case 19:
			HighlightActiveBackground = (Border)target;
			break;
		case 20:
			HighlightIconPath = (Path)target;
			break;
		case 21:
			HighlightColorHost = (Border)target;
			HighlightColorHost.MouseLeave += OnHighlightColorHostMouseLeave;
			break;
		case 22:
			HighLightBorder = (Border)target;
			break;
		case 23:
			((Grid)target).MouseLeftButtonUp += OnColorClick;
			break;
		case 24:
			HighlightColorActiveBackground = (Border)target;
			break;
		case 25:
			ColorSelectorPopup = (Popup)target;
			break;
		case 26:
			HighlightColorMenu = (HighlightColorMenu)target;
			break;
		case 27:
			HighlightArrowBorder = (Border)target;
			HighlightArrowBorder.MouseEnter += OnHighlightEnter;
			HighlightArrowBorder.MouseUp += OnHighlightArrowMouseUp;
			break;
		case 28:
			CheckItemButton = (ContentControl)target;
			CheckItemButton.MouseLeftButtonUp += OnStyleClick;
			break;
		case 29:
			CheckItemActiveBackground = (Border)target;
			break;
		case 30:
			CheckItemIconPath = (Path)target;
			break;
		case 31:
			BulletListButton = (ContentControl)target;
			BulletListButton.MouseLeftButtonUp += OnStyleClick;
			break;
		case 32:
			BulletListActiveBackground = (Border)target;
			break;
		case 33:
			BulletListIconPath = (Path)target;
			break;
		case 34:
			NumberedListButton = (ContentControl)target;
			NumberedListButton.MouseLeftButtonUp += OnStyleClick;
			break;
		case 35:
			NumberedListActiveBackground = (Border)target;
			break;
		case 36:
			NumberedListIconPath = (Path)target;
			break;
		case 37:
			ItalicButton = (ContentControl)target;
			ItalicButton.MouseLeftButtonUp += OnStyleClick;
			break;
		case 38:
			ItalicActiveBackground = (Border)target;
			break;
		case 39:
			ItalicIconPath = (Path)target;
			break;
		case 40:
			UnderLineButton = (ContentControl)target;
			UnderLineButton.MouseLeftButtonUp += OnStyleClick;
			break;
		case 41:
			UnderLineActiveBackground = (Border)target;
			break;
		case 42:
			UnderLineIconPath = (Path)target;
			break;
		case 43:
			StrikeThroughButton = (ContentControl)target;
			StrikeThroughButton.MouseLeftButtonUp += OnStyleClick;
			break;
		case 44:
			StrikeThroughActiveBackground = (Border)target;
			break;
		case 45:
			StrikeThroughIconPath = (Path)target;
			break;
		case 46:
			((ContentControl)target).MouseLeftButtonUp += OnStyleClick;
			break;
		case 47:
			DateItem = (ContentControl)target;
			DateItem.MouseLeftButtonUp += OnDateClick;
			break;
		case 48:
			DatePopup = (Popup)target;
			break;
		case 49:
			LinkButton = (ContentControl)target;
			LinkButton.MouseLeftButtonUp += OnStyleClick;
			break;
		case 50:
			LinkActiveBackground = (Border)target;
			break;
		case 51:
			LinkIconPath = (Path)target;
			break;
		case 52:
			CodeButton = (ContentControl)target;
			CodeButton.MouseLeftButtonUp += OnStyleClick;
			break;
		case 53:
			CodeActiveBackground = (Border)target;
			break;
		case 54:
			CodeIconPath = (Path)target;
			break;
		case 55:
			QuoteButton = (ContentControl)target;
			QuoteButton.MouseLeftButtonUp += OnStyleClick;
			break;
		case 56:
			QuoteActiveBackground = (Border)target;
			break;
		case 57:
			QuoteIconPath = (Path)target;
			break;
		case 58:
			Split = (Grid)target;
			break;
		case 59:
			Attachment = (ContentControl)target;
			Attachment.MouseLeftButtonUp += OnStyleClick;
			break;
		case 60:
			MoreOptionGrid = (Grid)target;
			break;
		case 61:
			MoreGrid = (ContentControl)target;
			break;
		case 62:
			((Border)target).MouseLeftButtonUp += OnMoreClick;
			break;
		case 63:
			MoreOptionPopup = (Popup)target;
			break;
		case 64:
			MoreItalicItem = (ContentControl)target;
			MoreItalicItem.MouseLeftButtonUp += OnStyleClick;
			break;
		case 65:
			MoreItalicActiveBackground = (Border)target;
			break;
		case 66:
			MoreItalicIconPath = (Path)target;
			break;
		case 67:
			MoreUnderLineItem = (ContentControl)target;
			MoreUnderLineItem.MouseLeftButtonUp += OnStyleClick;
			break;
		case 68:
			MoreUnderLineActiveBackground = (Border)target;
			break;
		case 69:
			MoreUnderLineIconPath = (Path)target;
			break;
		case 70:
			MoreStrikeThroughItem = (ContentControl)target;
			MoreStrikeThroughItem.MouseLeftButtonUp += OnStyleClick;
			break;
		case 71:
			MoreStrikeThroughActiveBackground = (Border)target;
			break;
		case 72:
			MoreStrikeThroughIconPath = (Path)target;
			break;
		case 73:
			MoreLineItem = (ContentControl)target;
			MoreLineItem.MouseLeftButtonUp += OnStyleClick;
			break;
		case 74:
			ExtraDateItem = (ContentControl)target;
			ExtraDateItem.MouseLeftButtonUp += OnExtraDateClick;
			break;
		case 75:
			ExtraDatePopup = (Popup)target;
			break;
		case 76:
			MoreSplit = (Grid)target;
			break;
		case 77:
			MoreLink = (ContentControl)target;
			MoreLink.MouseLeftButtonUp += OnStyleClick;
			break;
		case 78:
			MoreLinkActiveBackground = (Border)target;
			break;
		case 79:
			MoreLinkIconPath = (Path)target;
			break;
		case 80:
			MoreCodeItem = (ContentControl)target;
			MoreCodeItem.MouseLeftButtonUp += OnStyleClick;
			break;
		case 81:
			MoreCodeActiveBackground = (Border)target;
			break;
		case 82:
			MoreCodeIconPath = (Path)target;
			break;
		case 83:
			MoreQuote = (ContentControl)target;
			MoreQuote.MouseLeftButtonUp += OnStyleClick;
			break;
		case 84:
			MoreQuoteActiveBackground = (Border)target;
			break;
		case 85:
			MoreQuoteIconPath = (Path)target;
			break;
		case 86:
			MoreSplit2 = (Grid)target;
			break;
		case 87:
			MoreAttachment = (ContentControl)target;
			MoreAttachment.MouseLeftButtonUp += OnStyleClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
