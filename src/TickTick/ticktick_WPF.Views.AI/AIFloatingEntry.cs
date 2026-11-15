using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.AI;

public class AIFloatingEntry : UserControl, IComponentConnector
{
	private const string DockToEdgeAction = "dockToEdge";

	private const string RestoreAction = "restore";

	private const double DockedVisibleWidth = 25.0;

	private const double DefaultRightInset = 24.0;

	private const double DockMenuWidth = 180.0;

	internal const int DockAnimationDurationMilliseconds = 220;

	private const int AutoHideDelayMilliseconds = 0;

	internal const double DockedOpacity = 0.5;

	public static readonly DependencyProperty IsGeneratingProperty = DependencyProperty.Register("IsGenerating", typeof(bool), typeof(AIFloatingEntry), new PropertyMetadata(false, OnIsGeneratingChanged));

	public static readonly RoutedEvent ClickEvent = EventManager.RegisterRoutedEvent("Click", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(AIFloatingEntry));

	private readonly DispatcherTimer _autoHideTimer;

	private readonly TranslateTransform _positionTranslate = new TranslateTransform();

	private FrameworkElement _positionHost;

	private Point? _savedPosition;

	private AIFloatingEntryPlacement _savedPlacement;

	private Point _pointerStart;

	private Point _dragStartPosition;

	private bool _isPointerDown;

	private bool _isDragging;

	private bool _collapseRequested;

	private Popup _dockMenuPopup;

	private bool _isDocked;

	private bool _isDockMenuOpen;

	private bool _isEntryHiding;

	private int _entryAnimationVersion;

	internal AIFloatingEntry Root;

	internal Grid InteractionRoot;

	internal Border InteractionSurface;

	internal Border GuideAnchor;

	internal Grid EntryRoot;

	internal TranslateTransform DockTranslate;

	internal Button EntryButton;

	internal RotateTransform LoadingRotate;

	private bool _contentLoaded;

	public bool IsGenerating
	{
		get
		{
			return (bool)GetValue(IsGeneratingProperty);
		}
		set
		{
			SetValue(IsGeneratingProperty, value);
		}
	}

	public FrameworkElement GuidePlacementTarget => GuideAnchor;

	internal bool IsDocked => _isDocked;

	public event RoutedEventHandler Click
	{
		add
		{
			AddHandler(ClickEvent, value);
		}
		remove
		{
			RemoveHandler(ClickEvent, value);
		}
	}

	public AIFloatingEntry()
	{
		InitializeComponent();
		_autoHideTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(0.0), DispatcherPriority.Background, OnAutoHideTimerTick, base.Dispatcher);
		_autoHideTimer.Stop();
		_isDocked = LocalSettings.Settings.ExtraSettings.AIFloatingEntryDocked;
		_savedPlacement = LocalSettings.Settings.ExtraSettings.AIFloatingEntryPlacement;
		base.RenderTransform = _positionTranslate;
		base.Loaded += OnEntryLoaded;
		base.Unloaded += OnEntryUnloaded;
		base.SizeChanged += OnPositionHostSizeChanged;
		InteractionSurface.IsHitTestVisible = _isDocked;
		UpdateDockPresentation(animate: false);
	}

	public void ShowAnimated()
	{
		if (base.Visibility != Visibility.Visible || _isEntryHiding)
		{
			double num = ((base.Visibility == Visibility.Visible) ? InteractionRoot.Opacity : 0.0);
			_entryAnimationVersion++;
			_isEntryHiding = false;
			base.Visibility = Visibility.Visible;
			base.IsHitTestVisible = true;
			InteractionSurface.IsHitTestVisible = _isDocked;
			UpdateDockPresentation(animate: false);
			AnimateEntryProperty(InteractionRoot, UIElement.OpacityProperty, num, 1.0);
		}
	}

	public void HideAnimated()
	{
		if (base.Visibility != Visibility.Visible || _isEntryHiding)
		{
			return;
		}
		_isEntryHiding = true;
		int version = ++_entryAnimationVersion;
		base.IsHitTestVisible = false;
		CloseEntryMenu();
		AnimateEntryProperty(InteractionRoot, UIElement.OpacityProperty, InteractionRoot.Opacity, 0.0, delegate
		{
			if (version == _entryAnimationVersion && _isEntryHiding)
			{
				HideEntry();
			}
		});
	}

	public void HideEntry()
	{
		_entryAnimationVersion++;
		_isEntryHiding = false;
		base.IsHitTestVisible = false;
		CloseEntryMenu();
		InteractionRoot.BeginAnimation(UIElement.OpacityProperty, null);
		InteractionRoot.Opacity = 0.0;
		EntryRoot.BeginAnimation(UIElement.OpacityProperty, null);
		DockTranslate.BeginAnimation(TranslateTransform.XProperty, null);
		base.Visibility = Visibility.Collapsed;
	}

	private void CloseEntryMenu()
	{
		FinishDrag();
		Popup dockMenuPopup = _dockMenuPopup;
		if (dockMenuPopup != null && dockMenuPopup.IsOpen)
		{
			_dockMenuPopup.IsOpen = false;
		}
		_autoHideTimer.Stop();
	}

	internal static AnimationClock AnimateEntryProperty(IAnimatable target, DependencyProperty property, double from, double to, Action completed = null)
	{
		target.BeginAnimation(property, null);
		((DependencyObject)target).SetValue(property, from);
		AnimationClock animationClock = AIFloatingWindowTransition.AnimateTo(target, property, to);
		if (completed != null)
		{
			animationClock.Completed += delegate
			{
				completed();
			};
		}
		return animationClock;
	}

	private void OnEntryButtonClick(object sender, RoutedEventArgs e)
	{
		RaiseEvent(new RoutedEventArgs(ClickEvent, this));
	}

	private void OnEntryLoaded(object sender, RoutedEventArgs e)
	{
		_positionHost = base.Parent as FrameworkElement;
		if (_positionHost != null)
		{
			_positionHost.SizeChanged += OnPositionHostSizeChanged;
		}
		UpdateEntryPosition();
	}

	private void OnEntryUnloaded(object sender, RoutedEventArgs e)
	{
		FinishDrag();
		if (_positionHost != null)
		{
			_positionHost.SizeChanged -= OnPositionHostSizeChanged;
			_positionHost = null;
		}
	}

	private void OnPositionHostSizeChanged(object sender, SizeChangedEventArgs e)
	{
		UpdateEntryPosition();
		UpdateDockPresentation(animate: false);
	}

	private Point GetDefaultPosition()
	{
		return new Point(_positionHost.ActualWidth - base.ActualWidth - base.Margin.Right + GuideAnchor.Margin.Left, _positionHost.ActualHeight - base.ActualHeight - base.Margin.Bottom + (base.ActualHeight - EntryRoot.Height) / 2.0);
	}

	private Point GetEntryPosition()
	{
		Size bounds = new Size(_positionHost.ActualWidth, _positionHost.ActualHeight);
		Size entrySize = new Size(EntryRoot.Width, EntryRoot.Height);
		if (!_isDragging)
		{
			return ResolveAnchoredEntryPosition(_savedPlacement, bounds, entrySize, GetDefaultPosition(), _isDocked);
		}
		return ResolveEntryPosition(_savedPosition, bounds, entrySize, GetDefaultPosition(), useDefaultX: false);
	}

	private void RememberEntryPlacement(Point position, bool preserveVerticalAnchor = false)
	{
		AIFloatingEntryPlacement aIFloatingEntryPlacement = CaptureEntryPlacement(position, new Size(_positionHost.ActualWidth, _positionHost.ActualHeight), new Size(EntryRoot.Width, EntryRoot.Height));
		if (preserveVerticalAnchor && _savedPlacement != null)
		{
			aIFloatingEntryPlacement.AnchorBottom = _savedPlacement.AnchorBottom;
			aIFloatingEntryPlacement.VerticalInset = _savedPlacement.VerticalInset;
		}
		_savedPlacement = aIFloatingEntryPlacement;
		LocalSettings.Settings.ExtraSettings.AIFloatingEntryPlacement = aIFloatingEntryPlacement;
	}

	private void UpdateEntryPosition()
	{
		if (_positionHost != null && !(base.ActualWidth <= 0.0) && !(base.ActualHeight <= 0.0))
		{
			Point entryPosition = GetEntryPosition();
			Point defaultPosition = GetDefaultPosition();
			_positionTranslate.X = entryPosition.X - defaultPosition.X;
			_positionTranslate.Y = entryPosition.Y - defaultPosition.Y;
		}
	}

	internal static Point ResolveEntryPosition(Point? savedPosition, Size bounds, Size entrySize, Point defaultPosition, bool useDefaultX)
	{
		Point point = savedPosition ?? defaultPosition;
		double val = ((useDefaultX || double.IsNaN(point.X) || double.IsInfinity(point.X)) ? defaultPosition.X : point.X);
		double val2 = ((double.IsNaN(point.Y) || double.IsInfinity(point.Y)) ? defaultPosition.Y : point.Y);
		return new Point(Math.Max(0.0, Math.Min(Math.Max(0.0, bounds.Width - entrySize.Width), val)), Math.Max(0.0, Math.Min(Math.Max(0.0, bounds.Height - entrySize.Height), val2)));
	}

	internal static AIFloatingEntryPlacement CaptureEntryPlacement(Point position, Size bounds, Size entrySize)
	{
		position = ResolveEntryPosition(position, bounds, entrySize, default(Point), useDefaultX: false);
		double num = Math.Max(0.0, bounds.Width - entrySize.Width) - position.X;
		double num2 = Math.Max(0.0, bounds.Height - entrySize.Height) - position.Y;
		return new AIFloatingEntryPlacement
		{
			AnchorRight = (num <= position.X),
			AnchorBottom = (num2 <= position.Y),
			HorizontalInset = Math.Min(position.X, num),
			VerticalInset = Math.Min(position.Y, num2)
		};
	}

	internal static Point ResolveAnchoredEntryPosition(AIFloatingEntryPlacement placement, Size bounds, Size entrySize, Point defaultPosition, bool useDefaultX)
	{
		return ResolveEntryPosition((placement == null) ? ((Point?)null) : new Point?(new Point(placement.AnchorRight ? (bounds.Width - entrySize.Width - placement.HorizontalInset) : placement.HorizontalInset, placement.AnchorBottom ? (bounds.Height - entrySize.Height - placement.VerticalInset) : placement.VerticalInset)), bounds, entrySize, defaultPosition, useDefaultX);
	}

	private void OnEntryButtonMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (_positionHost != null)
		{
			_isPointerDown = true;
			_pointerStart = e.GetPosition(_positionHost);
			_dragStartPosition = GetEntryPosition();
		}
	}

	private void OnEntryButtonMouseMove(object sender, MouseEventArgs e)
	{
		if (!_isPointerDown || _positionHost == null || e.LeftButton != MouseButtonState.Pressed)
		{
			return;
		}
		Vector vector = e.GetPosition(_positionHost) - _pointerStart;
		if (!_isDragging)
		{
			if (Math.Abs(vector.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(vector.Y) < SystemParameters.MinimumVerticalDragDistance)
			{
				return;
			}
			_isDragging = true;
			_collapseRequested = false;
			_autoHideTimer.Stop();
			EntryButton.CaptureMouse();
			UpdateDockPresentation(animate: false);
		}
		_savedPosition = ResolveEntryPosition(_dragStartPosition + vector, new Size(_positionHost.ActualWidth, _positionHost.ActualHeight), new Size(EntryRoot.Width, EntryRoot.Height), GetDefaultPosition(), useDefaultX: false);
		UpdateEntryPosition();
		e.Handled = true;
	}

	private void OnEntryButtonMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (_isDragging)
		{
			e.Handled = true;
		}
		FinishDrag();
	}

	private void OnEntryButtonLostMouseCapture(object sender, MouseEventArgs e)
	{
		FinishDrag();
	}

	private void FinishDrag()
	{
		_isPointerDown = false;
		if (_isDragging)
		{
			Point entryPosition = GetEntryPosition();
			_isDragging = false;
			_collapseRequested = _isDocked;
			_savedPosition = ResolveEntryPosition(entryPosition, new Size(_positionHost.ActualWidth, _positionHost.ActualHeight), new Size(EntryRoot.Width, EntryRoot.Height), GetDefaultPosition(), _isDocked);
			RememberEntryPlacement(_savedPosition.Value);
			UpdateEntryPosition();
			LocalSettings.Settings.Save(force: true);
			if (EntryButton.IsMouseCaptured)
			{
				EntryButton.ReleaseMouseCapture();
			}
			UpdateDockPresentation(animate: true);
		}
	}

	private void OnEntryButtonMouseRightButtonUp(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		ShowDockMenu();
	}

	private void ShowDockMenu()
	{
		Popup dockMenuPopup = _dockMenuPopup;
		if (dockMenuPopup == null || !dockMenuPopup.IsOpen)
		{
			_autoHideTimer.Stop();
			_isDockMenuOpen = true;
			_collapseRequested = false;
			UpdateDockPresentation(animate: false);
			EscPopup popup = (EscPopup)(_dockMenuPopup = new EscPopup
			{
				Placement = PlacementMode.Top,
				PlacementTarget = EntryRoot,
				HorizontalOffset = EntryRoot.ActualWidth - 180.0,
				VerticalOffset = 0.0,
				StaysOpen = false,
				PopupAnimation = PopupAnimation.Fade,
				RestoreFocus = false
			});
			string val = (_isDocked ? "restore" : "dockToEdge");
			string title = Utils.GetString(_isDocked ? "AIAgentRestore" : "AIAgentDockToEdge");
			Geometry dockMenuIcon = GetDockMenuIcon();
			CustomMenuList customMenuList = new CustomMenuList(new List<CustomMenuItemViewModel>
			{
				new CustomMenuItemViewModel(val, title, dockMenuIcon)
				{
					ShowSelected = false
				}
			}, popup);
			customMenuList.SetMinWidth(180.0);
			customMenuList.Operated += OnDockMenuOperated;
			customMenuList.Closed += OnDockMenuClosed;
			customMenuList.Show();
		}
	}

	private Geometry GetDockMenuIcon()
	{
		if (!_isDocked)
		{
			return Utils.GetGeometry("Ic.AI.Collapse");
		}
		Geometry geometry = Utils.GetGeometry("Ic.AI.Restore")?.Clone();
		if (geometry == null)
		{
			return null;
		}
		PathGeometry pathGeometry = PathGeometry.CreateFromGeometry(geometry);
		for (int num = pathGeometry.Figures.Count - 1; num >= 0; num--)
		{
			if (pathGeometry.Figures[num].Segments.Count == 0)
			{
				pathGeometry.Figures.RemoveAt(num);
			}
		}
		Rect bounds = pathGeometry.Bounds;
		if (bounds.IsEmpty || bounds.Width <= 0.0 || bounds.Height <= 0.0)
		{
			return geometry;
		}
		pathGeometry.Transform = new MatrixTransform(new Matrix(-1.2857142857142858, 0.0, 0.0, 1.2857142857142858, 36.0, 0.0));
		PathGeometry pathGeometry2 = new PathGeometry();
		pathGeometry2.Figures.Add(new PathFigure
		{
			StartPoint = new Point(0.0, 0.0),
			IsClosed = true
		});
		pathGeometry2.Figures.Add(new PathFigure
		{
			StartPoint = new Point(18.0, 18.0),
			IsClosed = true
		});
		return new GeometryGroup
		{
			Children = 
			{
				(Geometry)pathGeometry2,
				(Geometry)pathGeometry
			}
		};
	}

	private void OnDockMenuOperated(object sender, object action)
	{
		_isDockMenuOpen = false;
		if (object.Equals(action, "dockToEdge"))
		{
			SetDocked(isDocked: true);
		}
		else if (object.Equals(action, "restore"))
		{
			SetDocked(isDocked: false);
		}
	}

	private void OnDockMenuClosed(object sender, EventArgs e)
	{
		bool isDockMenuOpen = _isDockMenuOpen;
		_dockMenuPopup = null;
		_isDockMenuOpen = false;
		if (isDockMenuOpen)
		{
			ScheduleAutoHide();
		}
	}

	private void SetDocked(bool isDocked)
	{
		if (_isDocked != isDocked)
		{
			UserActCollectUtils.AddClickEvent("ai_assistant", "mini_ball", isDocked ? "dock_to_edge" : "restore");
			Point? savedPosition = ((_positionHost != null && base.ActualWidth > 0.0) ? new Point?(GetEntryPosition()) : ((Point?)null));
			_isDocked = isDocked;
			_collapseRequested = isDocked && InteractionRoot.IsMouseOver;
			if (savedPosition.HasValue)
			{
				_savedPosition = ResolveEntryPosition(savedPosition, new Size(_positionHost.ActualWidth, _positionHost.ActualHeight), new Size(EntryRoot.Width, EntryRoot.Height), GetDefaultPosition(), useDefaultX: true);
				RememberEntryPlacement(_savedPosition.Value, preserveVerticalAnchor: true);
				UpdateEntryPosition();
			}
			LocalSettings.Settings.ExtraSettings.AIFloatingEntryDocked = isDocked;
			LocalSettings.Settings.Save(force: true);
			InteractionSurface.IsHitTestVisible = _isDocked;
			_autoHideTimer.Stop();
			UpdateDockPresentation(animate: true);
		}
	}

	private void OnInteractionRootMouseEnter(object sender, MouseEventArgs e)
	{
		_autoHideTimer.Stop();
		if (_isDocked && !_collapseRequested)
		{
			UpdateDockPresentation(animate: true);
		}
	}

	private void OnInteractionRootMouseLeave(object sender, MouseEventArgs e)
	{
		if (!_isDragging)
		{
			_collapseRequested = false;
		}
		ScheduleAutoHide();
	}

	private void ScheduleAutoHide()
	{
		_autoHideTimer.Stop();
		if (_isDocked && !_isDragging && !_isDockMenuOpen && !InteractionRoot.IsMouseOver)
		{
			_autoHideTimer.Start();
		}
	}

	private void OnAutoHideTimerTick(object sender, EventArgs e)
	{
		_autoHideTimer.Stop();
		if (ShouldCollapseToEdge(_isDocked, InteractionRoot.IsMouseOver, _isDockMenuOpen, _isDragging, _collapseRequested))
		{
			UpdateDockPresentation(animate: true);
		}
	}

	private void UpdateDockPresentation(bool animate)
	{
		bool num = ShouldCollapseToEdge(_isDocked, InteractionRoot.IsMouseOver, _isDockMenuOpen, _isDragging, _collapseRequested);
		double num2 = (num ? ResolveDockedTranslation() : 0.0);
		double num3 = (num ? 0.5 : 1.0);
		InteractionSurface.IsHitTestVisible = _isDocked && !_isDragging;
		if (!animate)
		{
			DockTranslate.BeginAnimation(TranslateTransform.XProperty, null);
			DockTranslate.X = num2;
			EntryRoot.BeginAnimation(UIElement.OpacityProperty, null);
			EntryRoot.Opacity = num3;
		}
		else
		{
			AnimateTo(DockTranslate, TranslateTransform.XProperty, num2, 220);
			AnimateTo(EntryRoot, UIElement.OpacityProperty, num3, 220);
		}
	}

	private double ResolveDockedTranslation()
	{
		double rightInset = 24.0;
		double num = ((EntryRoot.ActualWidth > 0.0) ? EntryRoot.ActualWidth : 50.0);
		if (base.ActualWidth > 0.0)
		{
			double x = GuideAnchor.TranslatePoint(new Point(num, 0.0), this).X;
			if (!double.IsNaN(x) && !double.IsInfinity(x))
			{
				rightInset = Math.Max(0.0, base.ActualWidth - x);
			}
		}
		return ResolveDockedTranslation(rightInset, num);
	}

	internal static bool ShouldCollapseToEdge(bool isDocked, bool isMouseOver, bool isMenuOpen, bool isDragging = false, bool collapseRequested = false)
	{
		if (isDocked && !isDragging && !isMenuOpen)
		{
			if (!collapseRequested)
			{
				return !isMouseOver;
			}
			return true;
		}
		return false;
	}

	internal static double ResolveDockedTranslation(double rightInset, double entryWidth)
	{
		return Math.Max(0.0, rightInset) + Math.Max(0.0, entryWidth - 25.0);
	}

	internal static void AnimateTo(IAnimatable target, DependencyProperty property, double value, int durationMilliseconds)
	{
		DependencyObject obj = (DependencyObject)target;
		double num = (double)obj.GetValue(property);
		target.BeginAnimation(property, null);
		obj.SetValue(property, num);
		target.BeginAnimation(property, new DoubleAnimation(num, value, TimeSpan.FromMilliseconds(durationMilliseconds))
		{
			EasingFunction = new QuadraticEase
			{
				EasingMode = EasingMode.EaseOut
			},
			FillBehavior = FillBehavior.HoldEnd
		});
	}

	private static void OnIsGeneratingChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
	{
		((AIFloatingEntry)dependencyObject).UpdateLoadingAnimation((bool)e.NewValue);
	}

	private void UpdateLoadingAnimation(bool isGenerating)
	{
		if (LoadingRotate != null)
		{
			LoadingRotate.BeginAnimation(RotateTransform.AngleProperty, null);
			LoadingRotate.Angle = 0.0;
			if (isGenerating)
			{
				LoadingRotate.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(0.0, 360.0, TimeSpan.FromMilliseconds(1200.0))
				{
					RepeatBehavior = RepeatBehavior.Forever
				});
			}
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/ai/aifloatingentry.xaml", UriKind.Relative);
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
			Root = (AIFloatingEntry)target;
			break;
		case 2:
			InteractionRoot = (Grid)target;
			InteractionRoot.MouseEnter += OnInteractionRootMouseEnter;
			InteractionRoot.MouseLeave += OnInteractionRootMouseLeave;
			break;
		case 3:
			InteractionSurface = (Border)target;
			break;
		case 4:
			GuideAnchor = (Border)target;
			break;
		case 5:
			EntryRoot = (Grid)target;
			break;
		case 6:
			DockTranslate = (TranslateTransform)target;
			break;
		case 7:
			EntryButton = (Button)target;
			EntryButton.PreviewMouseLeftButtonDown += OnEntryButtonMouseLeftButtonDown;
			EntryButton.PreviewMouseMove += OnEntryButtonMouseMove;
			EntryButton.PreviewMouseLeftButtonUp += OnEntryButtonMouseLeftButtonUp;
			EntryButton.LostMouseCapture += OnEntryButtonLostMouseCapture;
			EntryButton.Click += OnEntryButtonClick;
			EntryButton.PreviewMouseRightButtonUp += OnEntryButtonMouseRightButtonUp;
			break;
		case 8:
			LoadingRotate = (RotateTransform)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
