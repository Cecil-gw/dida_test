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
using ticktick_WPF.Views.Misc.ColorSelector;

namespace ticktick_WPF.Views.MarkDown;

public class EditorMenuEx : UserControl, IComponentConnector
{
	private readonly HighlightColorMenuState _highlightColorState = new HighlightColorMenuState();

	private readonly HashSet<string> _activeActions = new HashSet<string>();

	private string _selectedText;

	private string _highlightContextColor;

	private bool _hasHighlightContextColor;

	private Window _colorPopupOwnerWindow;

	private string _action;

	public static readonly DependencyProperty HighlightBackgroundProperty = DependencyProperty.Register("HighlightBackground", typeof(string), typeof(EditorMenuEx));

	public static readonly DependencyProperty IsHighlightColorActiveProperty = DependencyProperty.Register("IsHighlightColorActive", typeof(bool), typeof(EditorMenuEx), new PropertyMetadata(false, null));

	internal EditorMenuEx Root;

	internal new ContentControl Content;

	internal StackPanel EditorPanel;

	internal ContentControl HeaderButton;

	internal Grid TextGrid;

	internal Border HeaderActiveBackground;

	internal StackPanel HeaderButtonGrid;

	internal Path StylePath;

	internal Popup HeaderPopup;

	internal StyleMenu StyleMenu;

	internal ContentControl BoldButton;

	internal Border BoldActiveBackground;

	internal Path BoldIconPath;

	internal ContentControl ItalicButton;

	internal Border ItalicActiveBackground;

	internal Path ItalicIconPath;

	internal ContentControl StrikeThroughButton;

	internal Border StrikeThroughActiveBackground;

	internal Path StrikeThroughIconPath;

	internal ContentControl UnderLineButton;

	internal Border UnderLineActiveBackground;

	internal Path UnderLineIconPath;

	internal ContentControl HighLightButton;

	internal Border HighLightActiveBackground;

	internal Path HighLightIconPath;

	internal Border HighlightColorHost;

	internal Border HighLightBorder;

	internal Border HighlightColorActiveBackground;

	internal Popup ColorSelectorPopup;

	internal HighlightColorMenu HighlightColorMenu;

	internal Border HighlightArrowBorder;

	internal ContentControl LinkButton;

	internal Border LinkActiveBackground;

	internal Path LinkIconPath;

	internal Grid DismissBar;

	private bool _contentLoaded;

	public string SelectedText
	{
		get
		{
			return _selectedText;
		}
		set
		{
			_selectedText = value;
			NotifyParseColor();
		}
	}

	public string Action
	{
		get
		{
			return _action;
		}
		set
		{
			_action = value;
			NotifySelectAction(value);
		}
	}

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

	public int MaxHeadingLevel { get; set; }

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

	public event EventHandler<string> EditorAction;

	public event EventHandler<ColorExtra> HighlightColorRequested;

	private void NotifySelectAction(string action)
	{
		StylePath.Data = Utils.GetGeometry(MarkDownAction.TryGetIconInEditor(action));
		UpdateActionVisuals();
	}

	public void UpdateContext(string selectedText, string action, int maxHeadingLevel, bool? addSubtaskEnabled = null, string highlightContextColor = null)
	{
		MaxHeadingLevel = maxHeadingLevel;
		_highlightContextColor = HighlightColorMenuState.NormalizeColor(highlightContextColor);
		_hasHighlightContextColor = true;
		SelectedText = selectedText;
		Action = action;
		if (addSubtaskEnabled.HasValue)
		{
			StyleMenu.AddSubtaskEnabled = addSubtaskEnabled.Value;
		}
	}

	public void UpdateContext(string selectedText, string action, IEnumerable<string> activeActions, int maxHeadingLevel, bool? addSubtaskEnabled = null, string highlightContextColor = null)
	{
		_activeActions.Clear();
		if (activeActions != null)
		{
			foreach (string item in activeActions.Where((string it) => !string.IsNullOrEmpty(it)))
			{
				_activeActions.Add(item);
			}
		}
		UpdateContext(selectedText, action, maxHeadingLevel, addSubtaskEnabled, highlightContextColor);
		UpdateActionVisuals();
	}

	public EditorMenuEx()
	{
		InitializeComponent();
		InitEvents();
		base.DataContext = this;
		base.Loaded += delegate
		{
			UpdateActionVisuals();
		};
		base.Unloaded += OnEditorMenuUnloaded;
		base.IsVisibleChanged += OnEditorMenuIsVisibleChanged;
	}

	private void InitEvents()
	{
		HighlightColorMenu.HighlightColorSelected -= OnHighlightColorSelected;
		HighlightColorMenu.HighlightColorSelected += OnHighlightColorSelected;
		HighlightColorMenu.MouseLeave -= OnHighlightColorMenuMouseLeave;
		HighlightColorMenu.MouseLeave += OnHighlightColorMenuMouseLeave;
		ColorSelectorPopup.Opened -= OnColorSelectorPopupOpened;
		ColorSelectorPopup.Opened += OnColorSelectorPopupOpened;
		ColorSelectorPopup.Closed -= OnColorSelectorPopupClosed;
		ColorSelectorPopup.Closed += OnColorSelectorPopupClosed;
		StyleMenu.EditorStyleAction -= OnEditorStyleClick;
		StyleMenu.EditorStyleAction += OnEditorStyleClick;
		base.PreviewMouseMove -= OnEditorMenuPreviewMouseMove;
		base.PreviewMouseMove += OnEditorMenuPreviewMouseMove;
		base.PreviewMouseDown -= OnEditorMenuPreviewMouseDown;
		base.PreviewMouseDown += OnEditorMenuPreviewMouseDown;
	}

	private void OnEditorStyleClick(object sender, string style)
	{
		HeaderPopup.IsOpen = false;
		DismissColorPopup();
		this.EditorAction?.Invoke(sender, style);
	}

	private void OnHighlightColorSelected(object sender, string color)
	{
		color = _highlightColorState.SelectColor(color);
		_highlightContextColor = color;
		_hasHighlightContextColor = true;
		NotifyHighlightColorRequested(sender, color);
		ColorSelectorPopup.IsOpen = false;
		NotifyParseColor();
	}

	private void OnStyleClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement { Tag: string tag })
		{
			e.Handled = true;
			HeaderPopup.IsOpen = false;
			DismissColorPopup();
			UserActCollectUtils.AddClickEvent("task_detail", "edit_menu", MarkDownAction.TryGetLabel(tag));
			this.EditorAction?.Invoke(sender, tag);
		}
	}

	private void UpdateActionVisuals()
	{
		if (base.IsLoaded)
		{
			bool isActive = !string.IsNullOrEmpty(_action) && _action != "Text";
			SetActionVisual(HeaderActiveBackground, StylePath, isActive);
			SetActionVisual(BoldActiveBackground, BoldIconPath, _activeActions.Contains("Bold"));
			SetActionVisual(ItalicActiveBackground, ItalicIconPath, _activeActions.Contains("Italic"));
			SetActionVisual(StrikeThroughActiveBackground, StrikeThroughIconPath, _activeActions.Contains("StrikeThrough"));
			SetActionVisual(UnderLineActiveBackground, UnderLineIconPath, _activeActions.Contains("UnderLine"));
			SetHighlightActionVisual(IsHighlightActive());
			SetActionVisual(LinkActiveBackground, LinkIconPath, _activeActions.Contains("Link"));
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
		SetActionVisual(HighLightActiveBackground, HighLightIconPath, isActive);
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

	private void ShowSelectColorPopup()
	{
		if (!ColorSelectorPopup.IsOpen)
		{
			_highlightColorState.IsPopupOpen = false;
			HighlightColorMenu.SelectedColor = _highlightColorState.GetMenuSelectedColor(GetHighlightContextColor());
			ColorSelectorPopup.IsOpen = true;
			NotifyParseColor();
		}
	}

	private void NotifyParseColor()
	{
		HighlightBackground = _highlightColorState.GetIconBackground(GetHighlightContextColor());
	}

	private string GetHighlightContextColor()
	{
		if (!_hasHighlightContextColor)
		{
			return HighlightColorMenuState.ExtractHighlightColor(SelectedText);
		}
		return _highlightContextColor;
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

	private void OnHeaderEnter(object sender, MouseEventArgs e)
	{
		StyleMenu.UpdateContext(_action, MaxHeadingLevel, StyleMenu.AddSubtaskEnabled);
		StyleMenu.SelectFirst();
		HeaderPopup.IsOpen = true;
		DismissColorPopup();
	}

	private void OnHighlightEnter(object sender, MouseEventArgs e)
	{
		DismissStylePopup();
		ShowSelectColorPopup();
	}

	private void OnHighlightArrowMouseUp(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		DismissStylePopup();
		ShowSelectColorPopup();
	}

	private void OnStyleEnter(object sender, MouseEventArgs e)
	{
		TryDismissSecondLevelPopup();
	}

	private void TryDismissSecondLevelPopup()
	{
		DismissColorPopup();
		DismissStylePopup();
	}

	public void DismissSecondLevelPopups()
	{
		DismissColorPopup();
		DismissStylePopup();
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

	private void DismissStylePopup()
	{
		if (HeaderPopup.IsOpen)
		{
			HeaderPopup.IsOpen = false;
		}
	}

	private void OnBorderLeave(object sender, MouseEventArgs e)
	{
		TryDismissSecondLevelPopup();
	}

	private void OnColorClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		NotifyHighlightColorRequested(sender, _highlightColorState.GetApplyColor(GetHighlightContextColor()));
		DismissColorPopup();
	}

	private void NotifyHighlightColorRequested(object sender, string color)
	{
		this.HighlightColorRequested?.Invoke(sender, HighlightColorMenuState.CreateHighlightColor(color));
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

	public bool IsInteractiveSource(DependencyObject source)
	{
		if (!IsSelfOrDescendant(source, this))
		{
			return IsColorPopupInteractiveSource(source);
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

	private void OnHeadPopupOpened(object sender, EventArgs e)
	{
		Popup popup = sender as Popup;
		if (popup?.PlacementTarget != null)
		{
			Point point = popup.Child.PointToScreen(new Point(0.0, 0.0));
			Point point2 = ((FrameworkElement)popup.PlacementTarget).PointToScreen(new Point(0.0, 0.0));
			DismissBar.VerticalAlignment = ((point.Y < point2.Y) ? VerticalAlignment.Bottom : VerticalAlignment.Top);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/markdown/editormenuex.xaml", UriKind.Relative);
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
			Root = (EditorMenuEx)target;
			break;
		case 2:
			Content = (ContentControl)target;
			break;
		case 3:
			EditorPanel = (StackPanel)target;
			break;
		case 4:
			HeaderButton = (ContentControl)target;
			break;
		case 5:
			TextGrid = (Grid)target;
			TextGrid.MouseEnter += OnHeaderEnter;
			break;
		case 6:
			HeaderActiveBackground = (Border)target;
			break;
		case 7:
			HeaderButtonGrid = (StackPanel)target;
			break;
		case 8:
			StylePath = (Path)target;
			break;
		case 9:
			HeaderPopup = (Popup)target;
			HeaderPopup.Opened += OnHeadPopupOpened;
			break;
		case 10:
			StyleMenu = (StyleMenu)target;
			break;
		case 11:
			BoldButton = (ContentControl)target;
			BoldButton.MouseEnter += OnStyleEnter;
			BoldButton.MouseLeftButtonUp += OnStyleClick;
			break;
		case 12:
			BoldActiveBackground = (Border)target;
			break;
		case 13:
			BoldIconPath = (Path)target;
			break;
		case 14:
			ItalicButton = (ContentControl)target;
			ItalicButton.MouseLeftButtonUp += OnStyleClick;
			ItalicButton.MouseEnter += OnStyleEnter;
			break;
		case 15:
			ItalicActiveBackground = (Border)target;
			break;
		case 16:
			ItalicIconPath = (Path)target;
			break;
		case 17:
			StrikeThroughButton = (ContentControl)target;
			StrikeThroughButton.MouseEnter += OnStyleEnter;
			StrikeThroughButton.MouseLeftButtonUp += OnStyleClick;
			break;
		case 18:
			StrikeThroughActiveBackground = (Border)target;
			break;
		case 19:
			StrikeThroughIconPath = (Path)target;
			break;
		case 20:
			UnderLineButton = (ContentControl)target;
			UnderLineButton.MouseLeftButtonUp += OnStyleClick;
			UnderLineButton.MouseEnter += OnStyleEnter;
			break;
		case 21:
			UnderLineActiveBackground = (Border)target;
			break;
		case 22:
			UnderLineIconPath = (Path)target;
			break;
		case 23:
			HighLightButton = (ContentControl)target;
			HighLightButton.MouseLeftButtonUp += OnStyleClick;
			HighLightButton.MouseEnter += OnStyleEnter;
			break;
		case 24:
			HighLightActiveBackground = (Border)target;
			break;
		case 25:
			HighLightIconPath = (Path)target;
			break;
		case 26:
			HighlightColorHost = (Border)target;
			HighlightColorHost.MouseLeave += OnHighlightColorHostMouseLeave;
			break;
		case 27:
			HighLightBorder = (Border)target;
			break;
		case 28:
			((Grid)target).MouseLeftButtonUp += OnColorClick;
			break;
		case 29:
			HighlightColorActiveBackground = (Border)target;
			break;
		case 30:
			ColorSelectorPopup = (Popup)target;
			break;
		case 31:
			HighlightColorMenu = (HighlightColorMenu)target;
			break;
		case 32:
			HighlightArrowBorder = (Border)target;
			HighlightArrowBorder.MouseEnter += OnHighlightEnter;
			HighlightArrowBorder.MouseUp += OnHighlightArrowMouseUp;
			break;
		case 33:
			LinkButton = (ContentControl)target;
			LinkButton.MouseLeftButtonUp += OnStyleClick;
			LinkButton.MouseEnter += OnStyleEnter;
			break;
		case 34:
			LinkActiveBackground = (Border)target;
			break;
		case 35:
			LinkIconPath = (Path)target;
			break;
		case 36:
			((Grid)target).MouseLeave += OnBorderLeave;
			break;
		case 37:
			DismissBar = (Grid)target;
			DismissBar.MouseLeave += OnBorderLeave;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
