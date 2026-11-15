using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using Newtonsoft.Json.Linq;
using TickTickDao;
using TickTickModels;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.MainListView.DetailView;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Views.AI;

public sealed class AIWindow : IndependentWindow, IListViewParent
{
	private const string DesktopWidgetId = "AIWidget";

	private const double DesktopBallSize = 76.0;

	private const double DesktopAutoHideTaskbarMargin = 48.0;

	private static bool _desktopOpening;

	private static Task _desktopClosing = Task.CompletedTask;

	private bool _isDesktopWidget;

	private Screen _desktopInitialScreen;

	private WidgetDesktopWindowBehavior _desktopWindowBehavior;

	private AIDesktopWindowState _desktopWindowState;

	private bool _changingDesktopLayout;

	private Vector _desktopExpandedOffset;

	private CalendarWidgetModel _desktopModel;

	private bool _desktopHiding;

	private AIFloatingWindow _desktopFloating;

	private Grid _desktopRoot;

	private Border _desktopSurface;

	private AIFloatingWindowTransition _desktopTransition;

	private bool _desktopCollapsing;

	private bool _desktopExpanding;

	private double? _desktopExpansionOpacity;

	private EventHandler _desktopExpansionRendering;

	private bool _desktopBallAlignmentPending;

	private bool _desktopBallRebasing;

	private bool? _desktopExpansionAfterAlignment;

	private bool _desktopSaveAfterAlignment;

	private System.Windows.Controls.Button _desktopBall;

	private readonly TranslateTransform _desktopBallHostPosition = new TranslateTransform();

	private readonly AIDesktopBallFade _desktopBallFade = new AIDesktopBallFade();

	private readonly AIDesktopBallFade _desktopBallRebase = new AIDesktopBallFade();

	private Grid _desktopBallHost;

	private readonly AIDesktopWidgetDragState _desktopDrag = new AIDesktopWidgetDragState();

	private UIElement _desktopDragTarget;

	private Matrix _desktopDragTransform = Matrix.Identity;

	private bool _desktopNativeDragging;

	private EscPopup _desktopEntryMenu;

	private WidgetSettings _desktopSettingsWindow;

	private string _appliedDesktopTheme;

	private string _appliedDesktopDisplayOption;

	private readonly Dictionary<string, KeyBinding> _desktopKeyBindings = new Dictionary<string, KeyBinding>();

	private const double DesktopDockSnapDistance = 12.0;

	private readonly TranslateTransform _desktopDockTranslate = new TranslateTransform();

	private DispatcherTimer _desktopDockTimer;

	private HideType _desktopDockEdge;

	private bool _desktopDockingDisposed;

	private readonly System.Windows.Media.Brush _desktopDockHitBackground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(1, 0, 0, 0));

	private static bool _opening;

	private const string WindowId = "AI";

	private readonly AIWebViewHostManager _webViews = new AIWebViewHostManager();

	private readonly AIWorkspaceView _workspace = new AIWorkspaceView();

	private readonly AIConversationView _conversation = new AIConversationView();

	private readonly Border _immersive = new Border
	{
		Visibility = Visibility.Collapsed
	};

	private TaskDetailView _immersiveSource;

	private bool _closed;

	public static AIWindow DesktopWidget { get; private set; }

	private Rect DesktopVisibleBounds
	{
		get
		{
			if (!_desktopModel.isExpanded)
			{
				return new Rect(base.Left + _desktopBallHostPosition.X, base.Top + _desktopBallHostPosition.Y, 76.0, 76.0);
			}
			return new Rect(base.Left, base.Top, base.Width, base.Height);
		}
	}

	public static AIWindow Current { get; private set; }

	public static bool IsShowing => Current != null;

	private AIWebAppView WebView => _webViews.ActiveWebView;

	private static bool HasConsent => LocalSettings.Settings.ExtraSettings.CheckHasFlag(HasSetFlag.AIAssistantPrivacyConsent);

	private AIWindow(CalendarWidgetModel model)
		: base(model)
	{
		InitializeAIWindow();
		InitializeDesktopWidget(model);
	}

	public static async Task ShowDesktopWidget(bool force = true)
	{
		if (_desktopOpening || WindowManager.AppLockOrExit || AppLockWindowManager.IsLocked || !LocalSettings.Settings.EnableAI || Utils.IsWindows7())
		{
			return;
		}
		_desktopOpening = true;
		try
		{
			string userId = LocalSettings.Settings.LoginUserId;
			Task desktopClosing = _desktopClosing;
			_desktopClosing = Task.CompletedTask;
			await desktopClosing;
			if (WindowManager.AppLockOrExit || AppLockWindowManager.IsLocked || !LocalSettings.Settings.EnableAI || userId != LocalSettings.Settings.LoginUserId)
			{
				return;
			}
			CalendarWidgetModel calendarWidgetModel = await SingleWidgetDao.TryGetWidget(UniqueWidgetMode.AI);
			if (WindowManager.AppLockOrExit || AppLockWindowManager.IsLocked || !LocalSettings.Settings.EnableAI || userId != LocalSettings.Settings.LoginUserId || (!force && calendarWidgetModel == null))
			{
				return;
			}
			Screen desktopInitialScreen = null;
			if (DesktopWidget == null)
			{
				if (calendarWidgetModel == null)
				{
					calendarWidgetModel = await SingleWidgetDao.CreateWidget(UniqueWidgetMode.AI);
					desktopInitialScreen = Screen.FromPoint(System.Windows.Forms.Cursor.Position);
				}
				if (WindowManager.AppLockOrExit || AppLockWindowManager.IsLocked || !LocalSettings.Settings.EnableAI || userId != LocalSettings.Settings.LoginUserId)
				{
					return;
				}
				calendarWidgetModel.NormalizeAIWidget();
				DesktopWidget = new AIWindow(calendarWidgetModel);
				DesktopWidget._desktopInitialScreen = desktopInitialScreen;
			}
			DesktopWidget.Show();
			if (force)
			{
				DesktopWidget.Activate();
			}
			RefreshDesktopWidgetEntrances();
		}
		catch (Exception ex)
		{
			UtilLog.Error("AI desktop widget open failed: " + ex);
			throw;
		}
		finally
		{
			_desktopOpening = false;
		}
	}

	public static void CloseDesktopWidget()
	{
		DesktopWidget?.Close();
	}

	public static void HideDesktopWidget()
	{
		if (DesktopWidget != null)
		{
			DesktopWidget._desktopHiding = true;
			DesktopWidget.Close();
		}
	}

	internal static void RefreshDesktopWidgetEntrances()
	{
		App.RefreshIconMenu();
		JumpHelper.InitJumpList();
		GlobalEventManager.NotifyWidgetOpenChanged();
	}

	private void InitializeDesktopWidget(CalendarWidgetModel model)
	{
		_isDesktopWidget = true;
		Id = "AIWidget";
		_desktopModel = model;
		_desktopModel.NormalizeAIWidget();
		base.WindowState = WindowState.Normal;
		base.WindowStyle = WindowStyle.None;
		base.AllowsTransparency = true;
		base.Background = System.Windows.Media.Brushes.Transparent;
		base.ShowInTaskbar = false;
		_desktopWindowState = new AIDesktopWindowState(this);
		_desktopWindowBehavior = WidgetDesktopWindowBehavior.Attach(this, () => UniqueWidgetMode.AI.ToString(), () => _desktopModel.displayOption);
		Container.Children.Remove(_workspace);
		Container.Children.Remove(_immersive);
		_workspace.DetachConversationContent(_conversation);
		_desktopRoot = new Grid
		{
			Background = System.Windows.Media.Brushes.Transparent
		};
		_desktopSurface = new Border
		{
			Margin = new Thickness(12.0),
			CornerRadius = new CornerRadius(20.0)
		};
		_desktopSurface.SetResourceReference(Border.BackgroundProperty, "WindowBackgroundPrimary");
		_desktopTransition = new AIFloatingWindowTransition(_desktopSurface);
		_desktopSurface.PreviewMouseLeftButtonDown += OnDesktopHeaderMouseDown;
		_desktopSurface.PreviewMouseMove += OnDesktopPointerMove;
		_desktopSurface.PreviewMouseLeftButtonUp += OnDesktopPointerUp;
		_desktopSurface.LostMouseCapture += OnDesktopPointerCaptureLost;
		_desktopSurface.SizeChanged += delegate(object sender, SizeChangedEventArgs args)
		{
			if (_desktopSurface.Child is FrameworkElement frameworkElement)
			{
				frameworkElement.Clip = new RectangleGeometry(new Rect(0.0, 0.0, args.NewSize.Width, args.NewSize.Height), 20.0, 20.0);
			}
		};
		_desktopFloating = new AIFloatingWindow
		{
			Width = double.NaN,
			Height = double.NaN,
			HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch
		};
		_desktopFloating.EnableDesktopWidgetMode();
		_desktopFloating.AttachConversationWorkspace(_workspace);
		_desktopFloating.CollapseRequested += delegate
		{
			SetDesktopExpanded(expanded: false);
		};
		_desktopFloating.NewConversationRequested += OnNewConversation;
		_desktopFloating.SettingsRequested += delegate
		{
			ShowDesktopSettings();
		};
		_desktopFloating.OpenMainWindowRequested += delegate
		{
			OpenDesktopMainWindow();
		};
		Grid grid = new Grid();
		grid.Children.Add(_desktopFloating);
		((System.Windows.Controls.Panel)ToastGrid.Parent).Children.Remove(ToastGrid);
		grid.Children.Add(ToastGrid);
		_desktopSurface.Child = grid;
		Canvas canvas = new Canvas();
		canvas.Children.Add(_desktopSurface);
		_desktopRoot.Children.Add(canvas);
		_desktopBall = new System.Windows.Controls.Button
		{
			Width = 52.0,
			Height = 52.0,
			BorderThickness = new Thickness(0.0),
			HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Bottom,
			Margin = new Thickness(12.0),
			Cursor = System.Windows.Input.Cursors.Hand,
			ToolTip = Utils.GetString("AIAssistant")
		};
		_desktopBall.Template = new ControlTemplate(typeof(System.Windows.Controls.Button))
		{
			VisualTree = new FrameworkElementFactory(typeof(ContentPresenter))
		};
		Grid grid2 = new Grid();
		Ellipse ellipse = new Ellipse();
		ellipse.SetResourceReference(Shape.FillProperty, "WindowBackgroundPrimary");
		grid2.Children.Add(ellipse);
		Path element = new Path
		{
			Width = 28.0,
			Height = 28.0,
			Stretch = Stretch.Uniform,
			Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(78, 129, byte.MaxValue)),
			Data = Utils.GetGeometry("Ic.Tabbar.AI")
		};
		grid2.Children.Add(element);
		_desktopBall.Content = grid2;
		_desktopBall.Click += async delegate
		{
			await ExpandDesktopWidgetAsync();
		};
		_desktopBall.PreviewMouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs args)
		{
			_desktopBall.Focus();
			BeginDesktopPointerDrag(_desktopBall, args);
		};
		_desktopBall.PreviewMouseMove += OnDesktopPointerMove;
		_desktopBall.PreviewMouseLeftButtonUp += OnDesktopPointerUp;
		_desktopBall.LostMouseCapture += OnDesktopPointerCaptureLost;
		_desktopBall.PreviewMouseRightButtonUp += delegate(object sender, MouseButtonEventArgs args)
		{
			args.Handled = true;
			ShowDesktopEntryMenu();
		};
		_desktopBallHost = new Grid
		{
			Width = 76.0,
			Height = 76.0,
			HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top
		};
		_desktopBallHost.Children.Add(_desktopBall);
		_desktopRoot.Children.Add(new Border
		{
			Width = 76.0,
			Height = 76.0,
			HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			RenderTransform = _desktopBallHostPosition,
			Child = _desktopBallHost
		});
		base.Content = _desktopRoot;
		InitializeDesktopDocking();
		base.KeyDown += OnDesktopKeyDown;
		base.Loaded += OnDesktopWidgetLoaded;
		base.SizeChanged += OnDesktopWidgetSizeChanged;
		base.DpiChanged += OnDesktopWidgetDpiChanged;
		base.LocationChanged += OnDesktopWidgetLocationChanged;
		AddDesktopKeyBinding("AINewConversation", new RelayCommand(delegate
		{
			UserActCollectUtils.AddShortCutEvent("AI", "new_chat");
			StartNewConversationAsync(focusComposer: true);
		}));
		AddDesktopKeyBinding("AIToggleFloatingWindow", new RelayCommand(delegate
		{
			UserActCollectUtils.AddShortCutEvent("AI", "toggle_floating_window");
			bool? desktopExpansionAfterAlignment = _desktopExpansionAfterAlignment;
			bool num;
			if (!desktopExpansionAfterAlignment.HasValue)
			{
				if (!_desktopModel.isExpanded)
				{
					goto IL_0043;
				}
				num = !_desktopCollapsing;
			}
			else
			{
				num = desktopExpansionAfterAlignment == true;
			}
			if (!num)
			{
				goto IL_0043;
			}
			goto IL_004b;
			IL_0043:
			if (!_desktopExpanding)
			{
				ExpandDesktopWidgetAsync();
				return;
			}
			goto IL_004b;
			IL_004b:
			SetDesktopExpanded(expanded: false);
		}));
		ApplyDesktopAppearance();
		SetDesktopExpanded(_desktopModel.isExpanded && HasConsent, save: false);
		AttachDesktopConversation();
	}

	private void OnDesktopWidgetLoaded(object sender, RoutedEventArgs args)
	{
		base.Loaded -= OnDesktopWidgetLoaded;
		if (_desktopInitialScreen != null)
		{
			_changingDesktopLayout = true;
			try
			{
				ApplyInitialDesktopPosition(this, _desktopInitialScreen, new System.Windows.Point(_desktopBallHostPosition.X, _desktopBallHostPosition.Y));
				_desktopInitialScreen = null;
			}
			finally
			{
				_changingDesktopLayout = false;
			}
		}
		SetDesktopExpanded(_desktopModel.isExpanded, save: false);
	}

	private void OnDesktopWidgetSizeChanged(object sender, SizeChangedEventArgs args)
	{
		if (_changingDesktopLayout)
		{
			return;
		}
		if (!_desktopModel.isExpanded)
		{
			ApplyDesktopBallRegion();
			return;
		}
		_desktopSurface.Width = Math.Max(360.0, args.NewSize.Width - 24.0);
		_desktopSurface.Height = Math.Max(400.0, args.NewSize.Height - 24.0);
		if (!_desktopCollapsing)
		{
			_desktopBallHostPosition.X = args.NewSize.Width - 76.0;
			_desktopBallHostPosition.Y = args.NewSize.Height - 76.0;
		}
	}

	private void OnDesktopWidgetDpiChanged(object sender, System.Windows.DpiChangedEventArgs args)
	{
		base.Dispatcher.BeginInvoke((Action)delegate
		{
			if (!_closed && !_desktopModel.isExpanded)
			{
				ApplyDesktopBallRegion();
			}
		}, DispatcherPriority.Loaded);
	}

	private void OnDesktopWidgetLocationChanged(object sender, EventArgs args)
	{
		if (!_changingDesktopLayout && !_desktopNativeDragging && !_desktopModel.isExpanded && !_desktopBallAlignmentPending && base.IsLoaded)
		{
			_desktopBallAlignmentPending = true;
			base.Dispatcher.BeginInvoke((Action)delegate
			{
				_desktopBallAlignmentPending = false;
				AlignDesktopBallWindow();
			}, DispatcherPriority.Loaded);
		}
	}

	private void AlignDesktopBallWindow()
	{
		if (_closed || _changingDesktopLayout || _desktopNativeDragging || _desktopModel.isExpanded || _desktopExpanding || _desktopBallRebasing || PresentationSource.FromVisual(this) == null)
		{
			return;
		}
		System.Windows.Point center = PointToScreen(new System.Windows.Point(_desktopBallHostPosition.X + 38.0, _desktopBallHostPosition.Y + 38.0));
		if (!AIDesktopWindowRegion.NeedsAlignment(this, center, new System.Windows.Size(76.0, 76.0)))
		{
			return;
		}
		_desktopBallRebasing = true;
		_desktopBall.IsHitTestVisible = false;
		_desktopBallRebase.Start(_desktopBallHost, 1.0, delegate
		{
			if (!_closed)
			{
				AIDesktopWindowRegion.Apply(this, Rect.Empty);
				RebaseDesktopBallWindow(center);
				_desktopRoot.UpdateLayout();
				_desktopBallRebase.WaitForRender(delegate
				{
					_desktopBallRebasing = false;
					_desktopBall.IsHitTestVisible = true;
					ApplyDesktopBallRegion();
					_desktopBallRebase.Dispose();
					bool? desktopExpansionAfterAlignment = _desktopExpansionAfterAlignment;
					_desktopExpansionAfterAlignment = null;
					if (desktopExpansionAfterAlignment == true)
					{
						SetDesktopExpanded(expanded: true, _desktopSaveAfterAlignment);
					}
					else
					{
						RefreshDesktopDocking();
						SaveLocation();
					}
				});
			}
		}, fade: false);
	}

	private void RebaseDesktopBallWindow(System.Windows.Point center)
	{
		_changingDesktopLayout = true;
		try
		{
			if (AIDesktopWindowRegion.AlignToScreen(this, center, new System.Windows.Size(76.0, 76.0)))
			{
				System.Windows.Point point = PointFromScreen(center);
				_desktopBallHostPosition.X = point.X - 38.0;
				_desktopBallHostPosition.Y = point.Y - 38.0;
				ApplyDesktopBallRegion();
			}
		}
		finally
		{
			_changingDesktopLayout = false;
		}
	}

	private void AttachDesktopConversation()
	{
		_workspace.DetachConversationContent(_conversation);
		_conversation.SetFloatingMode(isFloatingMode: true);
		_desktopFloating.AttachConversationContent(_conversation);
	}

	private async Task ExpandDesktopWidgetAsync()
	{
		if (!HasConsent)
		{
			RefreshDesktopDocking();
			OpenDesktopMainWindow();
			ScheduleDesktopDockHide();
		}
		else
		{
			_workspace.SetPrivacyAuthorizationRequired(isRequired: false);
			SetDesktopExpanded(expanded: true);
			await InitializeBrowserAsync();
			await _workspace.EnsureConversationsLoadedAsync();
		}
	}

	private void AddDesktopKeyBinding(string name, ICommand command)
	{
		KeyBinding keyBinding = new KeyBinding(command, new KeyGesture(Key.None));
		base.InputBindings.Add(keyBinding);
		_desktopKeyBindings.Add(name, keyBinding);
		KeyBindingManager.TryAddKeyBinding(name, keyBinding);
	}

	private void SetDesktopExpanded(bool expanded, bool save = true)
	{
		if (!expanded)
		{
			RestoreDesktopExpansionFrame();
		}
		if (_desktopBallRebasing)
		{
			_desktopExpansionAfterAlignment = expanded;
			_desktopSaveAfterAlignment = save;
		}
		else if (_desktopExpanding)
		{
			if (!expanded)
			{
				_desktopExpanding = false;
				_desktopBallFade.Dispose();
				_desktopBall.IsHitTestVisible = true;
			}
		}
		else
		{
			if (!expanded && save && _desktopCollapsing)
			{
				return;
			}
			if (!expanded && save && base.IsLoaded && _desktopModel.isExpanded)
			{
				_desktopCollapsing = true;
				EndDesktopPointerDrag();
				_desktopFloating.PrepareForHostChange();
				double? initialOpacity = _desktopBallFade.TakeOpacity();
				ResetDesktopDockPresentation();
				System.Windows.Point point = ResolveDesktopPosition(new System.Windows.Point(base.Left + base.Width, base.Top + base.Height) - _desktopExpandedOffset, new System.Windows.Size(76.0, 76.0), new Rect(base.Left, base.Top, base.Width, base.Height));
				_desktopBallHostPosition.X = point.X - base.Left;
				_desktopBallHostPosition.Y = point.Y - base.Top;
				SetDesktopBallVisible(visible: true, animate: true, initialOpacity);
				_desktopTransition.HideAnimated(delegate
				{
					_desktopCollapsing = false;
					_desktopModel.width = base.Width;
					_desktopModel.height = base.Height;
					_desktopModel.isExpanded = false;
					ConfigureDesktopResize(this, expanded: false);
					AlignDesktopBallWindow();
					ApplyDesktopBallRegion();
					_desktopFloating.HideImmediately();
					RefreshDesktopDocking();
					SaveLocation();
				});
			}
			else
			{
				_desktopCollapsing = false;
				if (expanded && save && base.IsLoaded && _desktopModel.isExpanded)
				{
					_desktopBall.IsHitTestVisible = false;
					AIFloatingWindowTransition.AnimateTo((Grid)_desktopBall.Content, UIElement.OpacityProperty, 0.0);
					_desktopTransition.ShowAnimated();
				}
				else
				{
					ApplyDesktopExpandedLayout(expanded, save);
				}
			}
		}
	}

	private void ApplyDesktopExpandedLayout(bool expanded, bool save)
	{
		if (expanded && save && base.IsLoaded && !_desktopModel.isExpanded && !_desktopExpanding)
		{
			_desktopExpanding = true;
			_desktopBall.IsHitTestVisible = false;
			_desktopBallFade.Start(_desktopBallHost, ((Grid)_desktopBall.Content).Opacity * _desktopBall.Opacity, delegate
			{
				if (!_desktopExpanding || _closed)
				{
					return;
				}
				try
				{
					_desktopExpansionOpacity = base.Opacity;
					if (!AIDesktopWindowFrame.Clear(this))
					{
						UtilLog.Error("AI desktop widget clear frame failed: " + Marshal.GetLastWin32Error());
					}
					AIDesktopWindowRegion.Apply(this, Rect.Empty);
					AIDesktopWindowFrame.Flush();
					base.Opacity = 0.0;
					ApplyDesktopExpandedLayout(expanded, save);
					WaitForDesktopExpansionFrame();
				}
				finally
				{
					_desktopExpanding = false;
				}
			});
			return;
		}
		_changingDesktopLayout = true;
		try
		{
			if (!_desktopExpanding)
			{
				_desktopBallFade.Dispose();
			}
			ResetDesktopDockPresentation();
			System.Windows.Point bottomRight = DesktopVisibleBounds.BottomRight;
			if (_desktopModel.isExpanded)
			{
				bottomRight -= _desktopExpandedOffset;
			}
			if (_desktopModel.isExpanded && save)
			{
				_desktopModel.width = base.Width;
				_desktopModel.height = base.Height;
			}
			_desktopModel.NormalizeAIWidget();
			_desktopSurface.Width = _desktopModel.width - 24.0;
			_desktopSurface.Height = _desktopModel.height - 24.0;
			System.Windows.Size size = new System.Windows.Size(_desktopModel.width, _desktopModel.height);
			System.Windows.Point point = new System.Windows.Point(bottomRight.X - size.Width, bottomRight.Y - size.Height);
			System.Windows.Point point2 = new System.Windows.Point(bottomRight.X - 76.0, bottomRight.Y - 76.0);
			SetDesktopBallVisible(visible: false);
			_desktopRoot.Clip = null;
			AIDesktopWindowRegion.Apply(this, _desktopExpansionOpacity.HasValue ? new Rect?(Rect.Empty) : ((Rect?)null));
			_desktopTransition.HideImmediately();
			_desktopRoot.UpdateLayout();
			if (PresentationSource.FromVisual(this) != null)
			{
				Rect desktopWorkingArea = GetDesktopWorkingArea(bottomRight);
				point = ResolveDesktopPosition(bottomRight, size, desktopWorkingArea);
				point2 = ResolveDesktopPosition(bottomRight, new System.Windows.Size(76.0, 76.0), desktopWorkingArea);
			}
			_desktopModel.isExpanded = expanded;
			System.Windows.Size value = WidgetConstants.GetDefaultSize(WidgetType.AI).Value;
			base.MinWidth = (expanded ? value.Width : 76.0);
			base.MinHeight = (expanded ? value.Height : 76.0);
			base.Width = size.Width;
			base.Height = size.Height;
			ConfigureDesktopResize(this, expanded);
			_desktopExpandedOffset = (expanded ? (point - new System.Windows.Point(bottomRight.X - size.Width, bottomRight.Y - size.Height)) : default(Vector));
			base.Left = point.X;
			base.Top = point.Y;
			_desktopBallHostPosition.X = (expanded ? (size.Width - 76.0) : (point2.X - base.Left));
			_desktopBallHostPosition.Y = (expanded ? (size.Height - 76.0) : (point2.Y - base.Top));
			_desktopRoot.UpdateLayout();
			if (expanded)
			{
				_desktopFloating.ShowAnimated();
			}
			else
			{
				_desktopFloating.HideImmediately();
			}
			if (expanded)
			{
				if (save && base.IsLoaded)
				{
					_desktopTransition.ShowAnimated();
				}
				else
				{
					_desktopTransition.ShowImmediately();
				}
			}
			SetDesktopBallVisible(!expanded, save && base.IsLoaded);
			if (!expanded)
			{
				ApplyDesktopBallRegion();
			}
		}
		finally
		{
			_changingDesktopLayout = false;
		}
		if (!expanded)
		{
			AlignDesktopBallWindow();
		}
		if (!_desktopCollapsing)
		{
			RefreshDesktopDocking(save);
		}
		if (save)
		{
			SaveLocation();
		}
	}

	private void WaitForDesktopExpansionFrame()
	{
		int frames = 0;
		TimeSpan? lastFrame = null;
		_desktopExpansionRendering = delegate(object sender, EventArgs args)
		{
			TimeSpan renderingTime = ((RenderingEventArgs)args).RenderingTime;
			if (!(lastFrame == renderingTime))
			{
				lastFrame = renderingTime;
				if (++frames >= 2)
				{
					RestoreDesktopExpansionFrame();
				}
			}
		};
		CompositionTarget.Rendering += _desktopExpansionRendering;
	}

	private void RestoreDesktopExpansionFrame()
	{
		if (_desktopExpansionRendering != null)
		{
			CompositionTarget.Rendering -= _desktopExpansionRendering;
		}
		_desktopExpansionRendering = null;
		if (!_desktopExpansionOpacity.HasValue)
		{
			return;
		}
		double value = _desktopExpansionOpacity.Value;
		_desktopExpansionOpacity = null;
		if (!_closed)
		{
			AIDesktopWindowFrame.Clear(this);
			base.Opacity = value;
			if (_desktopModel.isExpanded)
			{
				AIDesktopWindowRegion.Apply(this, null);
			}
			else
			{
				ApplyDesktopBallRegion();
			}
		}
	}

	private void ApplyDesktopBallRegion()
	{
		Rect rect = new Rect(_desktopBallHostPosition.X, _desktopBallHostPosition.Y, 76.0, 76.0);
		_desktopRoot.Clip = new RectangleGeometry(rect);
		if (!_desktopBallRebasing)
		{
			AIDesktopWindowRegion.Apply(this, rect);
		}
	}

	internal static void ConfigureDesktopResize(Window window, bool expanded)
	{
		window.ResizeMode = (expanded ? ResizeMode.CanResize : ResizeMode.NoResize);
		WindowUtils.SetWindowChromeBorderThickness(window, new Thickness(expanded ? 14 : 0));
	}

	private void SetDesktopBallVisible(bool visible, bool animate = false, double? initialOpacity = null)
	{
		Grid grid = (Grid)_desktopBall.Content;
		double num = initialOpacity ?? ((_desktopBall.Visibility == Visibility.Visible) ? grid.Opacity : 0.0);
		grid.BeginAnimation(UIElement.OpacityProperty, null);
		grid.Opacity = ((!visible) ? 0.0 : (animate ? num : 1.0));
		_desktopBall.IsHitTestVisible = visible;
		_desktopBall.Visibility = ((!visible) ? Visibility.Collapsed : Visibility.Visible);
		if (visible && animate)
		{
			AIFloatingEntry.AnimateEntryProperty(grid, UIElement.OpacityProperty, num, 1.0);
		}
	}

	internal static System.Windows.Point ResolveInitialDesktopPosition(Rect area, uint dpiX, uint dpiY, bool taskbarAutoHide)
	{
		return new System.Windows.Point(Math.Max(area.Left, area.Right - 100.0 * (double)dpiX / 96.0), Math.Max(area.Top, area.Bottom - (100.0 + (taskbarAutoHide ? 48.0 : 0.0)) * (double)dpiY / 96.0));
	}

	internal static void ApplyInitialDesktopPosition(Window window, Screen screen, System.Windows.Point ballOffset)
	{
		screen.GetDpi(DpiType.Effective, out var dpiX, out var dpiY);
		System.Drawing.Rectangle screenWorkingArea = NativeUtils.GetScreenWorkingArea(screen);
		System.Windows.Point point = ResolveInitialDesktopPosition(new Rect(screenWorkingArea.Left, screenWorkingArea.Top, screenWorkingArea.Width, screenWorkingArea.Height), dpiX, dpiY, NativeUtils.IsTaskbarAutoHideEnabled());
		IntPtr handle = new WindowInteropHelper(window).Handle;
		for (int i = 0; i < 2; i++)
		{
			if (!NativeUtils.SetWindowPos(handle, IntPtr.Zero, (int)Math.Round(point.X - ballOffset.X * (double)dpiX / 96.0), (int)Math.Round(point.Y - ballOffset.Y * (double)dpiY / 96.0), 0, 0, 21u))
			{
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}
			window.UpdateLayout();
		}
		System.Windows.Point point2 = window.PointToScreen(default(System.Windows.Point));
		DpiScale dpi = VisualTreeHelper.GetDpi(window);
		window.Left = point2.X / dpi.DpiScaleX;
		window.Top = point2.Y / dpi.DpiScaleY;
	}

	internal static System.Windows.Point ResolveDesktopPosition(System.Windows.Point anchor, System.Windows.Size size, Rect workingArea)
	{
		return new System.Windows.Point(Math.Max(workingArea.Left, Math.Min(anchor.X - size.Width, workingArea.Right - size.Width)), Math.Max(workingArea.Top, Math.Min(anchor.Y - size.Height, workingArea.Bottom - size.Height)));
	}

	protected override async Task SaveLocation()
	{
		if (!_isDesktopWidget)
		{
			await base.SaveLocation();
		}
		else if (!_changingDesktopLayout && !_closed && _desktopModel != null)
		{
			if (_desktopModel.isExpanded)
			{
				_desktopModel.width = base.Width;
				_desktopModel.height = base.Height;
			}
			Rect desktopVisibleBounds = DesktopVisibleBounds;
			_desktopModel.left = desktopVisibleBounds.Left;
			_desktopModel.top = desktopVisibleBounds.Top;
			await SingleWidgetDao.SaveWidget(_desktopModel);
		}
	}

	private void OnDesktopKeyDown(object sender, System.Windows.Input.KeyEventArgs args)
	{
		if (args.Key == Key.Escape && !args.Handled && !_desktopNativeDragging && (_desktopModel.isExpanded || _desktopExpanding || _desktopExpansionAfterAlignment == true))
		{
			args.Handled = true;
			if (!_desktopFloating.TryCloseTopmostPopup() && !_conversation.TryCloseTopmostPopup())
			{
				SetDesktopExpanded(expanded: false);
			}
		}
	}

	private void OnDesktopHeaderMouseDown(object sender, MouseButtonEventArgs args)
	{
		if (!(args.GetPosition(_desktopSurface).Y > 48.0) && !(args.OriginalSource is System.Windows.Controls.Primitives.ButtonBase) && Utils.FindParent<System.Windows.Controls.Primitives.ButtonBase>(args.OriginalSource as DependencyObject) == null)
		{
			BeginDesktopPointerDrag(_desktopSurface, args);
		}
	}

	private void BeginDesktopPointerDrag(UIElement target, MouseButtonEventArgs args)
	{
		if (args.ButtonState == MouseButtonState.Pressed && Mouse.LeftButton == MouseButtonState.Pressed)
		{
			_desktopDragTransform = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
			System.Drawing.Point mousePosition = System.Windows.Forms.Control.MousePosition;
			_desktopDrag.Begin(new System.Windows.Point(mousePosition.X, mousePosition.Y));
			_desktopDragTarget = target;
			if (!target.CaptureMouse())
			{
				_desktopDrag.Cancel();
				_desktopDragTarget = null;
				RefreshDesktopDocking();
				args.Handled = true;
			}
			else
			{
				ResetDesktopDockPresentation();
				args.Handled = true;
			}
		}
	}

	private void OnDesktopPointerMove(object sender, System.Windows.Input.MouseEventArgs args)
	{
		if (_desktopNativeDragging || sender != _desktopDragTarget || !_desktopDrag.IsPressed)
		{
			return;
		}
		if (Mouse.LeftButton != MouseButtonState.Pressed)
		{
			EndDesktopPointerDrag();
			RefreshDesktopDocking();
			return;
		}
		args.Handled = true;
		System.Drawing.Point mousePosition = System.Windows.Forms.Control.MousePosition;
		if (!_desktopDrag.TryStartDrag(new System.Windows.Point(mousePosition.X, mousePosition.Y), _desktopDragTransform, SystemParameters.MinimumHorizontalDragDistance, SystemParameters.MinimumVerticalDragDistance))
		{
			return;
		}
		_desktopNativeDragging = true;
		try
		{
			_desktopDragTarget.ReleaseMouseCapture();
			if (Mouse.LeftButton == MouseButtonState.Pressed)
			{
				DragMove();
			}
		}
		catch (InvalidOperationException) when (Mouse.LeftButton != MouseButtonState.Pressed)
		{
		}
		finally
		{
			_desktopNativeDragging = false;
			EndDesktopPointerDrag();
			AlignDesktopBallWindow();
			RefreshDesktopDocking();
			SaveLocation();
		}
	}

	private async void OnDesktopPointerUp(object sender, MouseButtonEventArgs args)
	{
		if (!_desktopNativeDragging && sender == _desktopDragTarget && _desktopDrag.IsPressed)
		{
			bool num = sender == _desktopBall && !_desktopDrag.HasDragged;
			EndDesktopPointerDrag();
			args.Handled = true;
			if (num)
			{
				await ExpandDesktopWidgetAsync();
				return;
			}
			RefreshDesktopDocking();
			SaveLocation();
		}
	}

	private void OnDesktopPointerCaptureLost(object sender, System.Windows.Input.MouseEventArgs args)
	{
		if (!_desktopNativeDragging && args.OriginalSource == _desktopDragTarget && !((UIElement)sender).IsMouseCaptured)
		{
			_desktopDrag.Cancel();
			_desktopDragTarget = null;
			RefreshDesktopDocking();
		}
	}

	private void EndDesktopPointerDrag()
	{
		UIElement desktopDragTarget = _desktopDragTarget;
		_desktopDrag.Cancel();
		_desktopDragTarget = null;
		if (desktopDragTarget != null && desktopDragTarget.IsMouseCaptured)
		{
			desktopDragTarget.ReleaseMouseCapture();
		}
	}

	private void SetDesktopGenerating(bool generating)
	{
		if (_desktopBall?.Content is Grid grid && grid.Children.Count > 1)
		{
			grid.Children[1].BeginAnimation(UIElement.OpacityProperty, generating ? new DoubleAnimation(0.45, 1.0, TimeSpan.FromMilliseconds(700.0))
			{
				AutoReverse = true,
				RepeatBehavior = RepeatBehavior.Forever
			} : null);
		}
	}

	private void ShowDesktopEntryMenu()
	{
		_desktopEntryMenu = new EscPopup
		{
			PlacementTarget = _desktopBall,
			Placement = PlacementMode.MousePoint,
			StaysOpen = false
		};
		_desktopEntryMenu.Closed += delegate
		{
			ScheduleDesktopDockHide();
		};
		UpdateDesktopDockPresentation(animate: true, keepVisible: true);
		CustomMenuList customMenuList = new CustomMenuList(new List<CustomMenuItemViewModel>
		{
			new CustomMenuItemViewModel("settings", Utils.GetString("Settings"), Utils.GetGeometry("Ic.Tabbar.SettingLinear")),
			new CustomMenuItemViewModel(null),
			new CustomMenuItemViewModel("main", Utils.GetString("OpenMainWindow"), Utils.GetGeometry("Ic.Focus.PopupWindow")),
			new CustomMenuItemViewModel("close", Utils.GetString("Close"), Utils.GetGeometry("Ic.Common.Power"))
		}, _desktopEntryMenu);
		customMenuList.Operated += delegate(object sender, object action)
		{
			if (object.Equals(action, "settings"))
			{
				ShowDesktopSettings();
			}
			else if (object.Equals(action, "main"))
			{
				OpenDesktopMainWindow();
			}
			else if (object.Equals(action, "close"))
			{
				Close();
			}
		};
		customMenuList.Show();
	}

	private void OpenDesktopMainWindow()
	{
		App.Window?.ShowWindow();
		App.Window?.SwitchModule(DisplayModule.AI, force: true, click: true);
	}

	private void ShowDesktopSettings()
	{
		if (_desktopSettingsWindow == null)
		{
			_desktopSettingsWindow = new WidgetSettings(_desktopModel, ApplyDesktopAppearance)
			{
				Owner = this
			};
			_desktopSettingsWindow.Closed += delegate
			{
				_desktopSettingsWindow = null;
				ScheduleDesktopDockHide();
			};
			_desktopSettingsWindow.Show();
		}
		UpdateDesktopDockPresentation(animate: true, keepVisible: true);
		_desktopSettingsWindow.Activate();
	}

	private void ApplyDesktopAppearance()
	{
		_desktopModel.NormalizeAIWidget();
		if (_appliedDesktopTheme != _desktopModel.themeId)
		{
			base.Resources.MergedDictionaries.Clear();
			ThemeUtil.SetTheme(_desktopModel.themeId, this);
			_appliedDesktopTheme = _desktopModel.themeId;
		}
		WebView?.SetThemeOverride(_desktopModel.themeId == "dark");
		if (_appliedDesktopDisplayOption != _desktopModel.displayOption)
		{
			_desktopWindowBehavior.UpdateDisplayOption();
			_appliedDesktopDisplayOption = _desktopModel.displayOption;
		}
		if (_desktopExpansionOpacity.HasValue)
		{
			_desktopExpansionOpacity = _desktopModel.opacity;
		}
		else
		{
			base.Opacity = _desktopModel.opacity;
		}
		bool flag = ShouldShowDesktopShadow(_desktopModel.opacity);
		_desktopSurface.Effect = (flag ? new DropShadowEffect
		{
			Color = Colors.Black,
			BlurRadius = 20.0,
			ShadowDepth = 2.0,
			Opacity = 0.22
		} : null);
		_desktopBall.Effect = (flag ? new DropShadowEffect
		{
			Color = Colors.Black,
			BlurRadius = 18.0,
			ShadowDepth = 2.0,
			Opacity = 0.2
		} : null);
		RefreshDesktopDocking();
		SingleWidgetDao.SaveWidget(_desktopModel);
	}

	internal static bool ShouldShowDesktopShadow(double opacity)
	{
		return opacity == 1.0;
	}

	private void DisposeDesktopWidget()
	{
		if (!_isDesktopWidget)
		{
			return;
		}
		_desktopWindowState?.Dispose();
		DisposeDesktopDocking();
		base.Loaded -= OnDesktopWidgetLoaded;
		base.SizeChanged -= OnDesktopWidgetSizeChanged;
		base.DpiChanged -= OnDesktopWidgetDpiChanged;
		base.LocationChanged -= OnDesktopWidgetLocationChanged;
		EndDesktopPointerDrag();
		SetDesktopGenerating(generating: false);
		_desktopExpanding = false;
		RestoreDesktopExpansionFrame();
		_desktopBallFade.Dispose();
		_desktopBallRebase.Dispose();
		_desktopBallRebasing = false;
		_desktopExpansionAfterAlignment = null;
		SetDesktopBallVisible(visible: false);
		_desktopTransition.Dispose();
		if (_desktopCollapsing)
		{
			_desktopModel.width = base.Width;
			_desktopModel.height = base.Height;
			_desktopModel.isExpanded = false;
			_desktopCollapsing = false;
		}
		_desktopClosing = ((!_desktopHiding && !WindowManager.AppLockOrExit) ? SingleWidgetDao.DeleteWidget(UniqueWidgetMode.AI) : SaveLocation());
		foreach (KeyValuePair<string, KeyBinding> desktopKeyBinding in _desktopKeyBindings)
		{
			KeyBindingManager.RemoveKeyBinding(desktopKeyBinding.Key, desktopKeyBinding.Value);
		}
		_desktopSettingsWindow?.Close();
		if (_desktopEntryMenu != null)
		{
			_desktopEntryMenu.IsOpen = false;
		}
		_desktopFloating.Dispose();
		DesktopWidget = null;
		RefreshDesktopWidgetEntrances();
	}

	private void InitializeDesktopDocking()
	{
		_desktopBall.RenderTransform = _desktopDockTranslate;
		_desktopDockTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(200.0)
		};
		_desktopDockTimer.Tick += OnDesktopDockTimerTick;
		base.MouseEnter += OnDesktopDockMouseEnter;
		base.MouseLeave += OnDesktopDockMouseLeave;
	}

	private Rect GetDesktopWorkingArea(System.Windows.Point anchor)
	{
		System.Windows.Point point = PointToScreen(new System.Windows.Point(anchor.X - base.Left - 1.0, anchor.Y - base.Top - 1.0));
		System.Drawing.Rectangle screenWorkingArea = NativeUtils.GetScreenWorkingArea(Screen.FromPoint(new System.Drawing.Point((int)point.X, (int)point.Y)));
		System.Windows.Point point2 = PointFromScreen(new System.Windows.Point(screenWorkingArea.Left, screenWorkingArea.Top));
		System.Windows.Point point3 = PointFromScreen(new System.Windows.Point(screenWorkingArea.Right, screenWorkingArea.Bottom));
		return new Rect(base.Left + point2.X, base.Top + point2.Y, point3.X - point2.X, point3.Y - point2.Y);
	}

	private void RefreshDesktopDocking(bool animate = true)
	{
		if (_desktopDockingDisposed || _desktopBallRebasing)
		{
			return;
		}
		ResetDesktopDockPresentation();
		if (!_desktopModel.isExpanded && _desktopModel.autoHide && base.Topmost && !_desktopDrag.IsPressed && !_changingDesktopLayout && PresentationSource.FromVisual(this) != null)
		{
			Rect desktopVisibleBounds = DesktopVisibleBounds;
			Rect desktopWorkingArea = GetDesktopWorkingArea(new System.Windows.Point(desktopVisibleBounds.Left + 38.0, desktopVisibleBounds.Top + 38.0));
			System.Windows.Point location = ResolveDesktopPosition(desktopVisibleBounds.BottomRight, new System.Windows.Size(76.0, 76.0), desktopWorkingArea);
			_desktopDockEdge = ResolveDesktopDockEdge(new Rect(location, new System.Windows.Size(76.0, 76.0)), desktopWorkingArea);
			if (_desktopDockEdge == HideType.Left)
			{
				location.X = desktopWorkingArea.Left;
			}
			else if (_desktopDockEdge == HideType.Right)
			{
				location.X = desktopWorkingArea.Right - 76.0;
			}
			else if (_desktopDockEdge == HideType.Top)
			{
				location.Y = desktopWorkingArea.Top;
			}
			base.Left = location.X - _desktopBallHostPosition.X;
			base.Top = location.Y - _desktopBallHostPosition.Y;
			UpdateDesktopDockPresentation(animate);
		}
	}

	internal static HideType ResolveDesktopDockEdge(Rect bounds, Rect area)
	{
		double num = Math.Abs(bounds.Left - area.Left);
		double num2 = Math.Abs(bounds.Right - area.Right);
		double num3 = Math.Min(Math.Abs(bounds.Top - area.Top), Math.Min(num, num2));
		if (num3 > 12.0)
		{
			return HideType.None;
		}
		if (num == num3)
		{
			return HideType.Left;
		}
		if (num2 != num3)
		{
			return HideType.Top;
		}
		return HideType.Right;
	}

	private void UpdateDesktopDockPresentation(bool animate, bool keepVisible = false)
	{
		if (_desktopDockingDisposed)
		{
			return;
		}
		bool flag = _desktopDockEdge != HideType.None && !_desktopModel.isExpanded && _desktopModel.autoHide && base.Topmost;
		int num;
		if (!keepVisible)
		{
			EscPopup desktopEntryMenu = _desktopEntryMenu;
			if (desktopEntryMenu == null || !desktopEntryMenu.IsOpen)
			{
				num = ((_desktopSettingsWindow?.IsVisible ?? false) ? 1 : 0);
				goto IL_0061;
			}
		}
		num = 1;
		goto IL_0061;
		IL_0061:
		bool isMenuOpen = (byte)num != 0;
		bool num2 = AIFloatingEntry.ShouldCollapseToEdge(flag, base.IsMouseOver, isMenuOpen, _desktopDrag.IsPressed);
		double num3 = (num2 ? AIFloatingEntry.ResolveDockedTranslation(12.0, _desktopBall.Width) : 0.0);
		double num4 = ((_desktopDockEdge == HideType.Left) ? (0.0 - num3) : ((_desktopDockEdge == HideType.Right) ? num3 : 0.0));
		double num5 = ((_desktopDockEdge == HideType.Top) ? (0.0 - num3) : 0.0);
		double num6 = (num2 ? 0.5 : 1.0);
		_desktopBallHost.ClipToBounds = flag;
		_desktopRoot.Background = (flag ? _desktopDockHitBackground : System.Windows.Media.Brushes.Transparent);
		if (animate)
		{
			AIFloatingEntry.AnimateTo(_desktopDockTranslate, TranslateTransform.XProperty, num4, 220);
			AIFloatingEntry.AnimateTo(_desktopDockTranslate, TranslateTransform.YProperty, num5, 220);
			AIFloatingEntry.AnimateTo(_desktopBall, UIElement.OpacityProperty, num6, 220);
			return;
		}
		_desktopDockTranslate.BeginAnimation(TranslateTransform.XProperty, null);
		_desktopDockTranslate.BeginAnimation(TranslateTransform.YProperty, null);
		_desktopDockTranslate.X = num4;
		_desktopDockTranslate.Y = num5;
		_desktopBall.BeginAnimation(UIElement.OpacityProperty, null);
		_desktopBall.Opacity = num6;
	}

	private void ResetDesktopDockPresentation()
	{
		_desktopDockTimer?.Stop();
		_desktopDockEdge = HideType.None;
		_desktopBallHost.ClipToBounds = false;
		_desktopRoot.Background = System.Windows.Media.Brushes.Transparent;
		_desktopDockTranslate.BeginAnimation(TranslateTransform.XProperty, null);
		_desktopDockTranslate.BeginAnimation(TranslateTransform.YProperty, null);
		_desktopBall.BeginAnimation(UIElement.OpacityProperty, null);
		_desktopDockTranslate.X = 0.0;
		_desktopDockTranslate.Y = 0.0;
		_desktopBall.Opacity = 1.0;
	}

	private void OnDesktopDockMouseEnter(object sender, System.Windows.Input.MouseEventArgs args)
	{
		_desktopDockTimer.Stop();
		UpdateDesktopDockPresentation(animate: true);
	}

	private void OnDesktopDockMouseLeave(object sender, System.Windows.Input.MouseEventArgs args)
	{
		ScheduleDesktopDockHide();
	}

	private void ScheduleDesktopDockHide()
	{
		if (!_desktopDockingDisposed && _desktopDockTimer != null)
		{
			_desktopDockTimer.Stop();
			if (_desktopDockEdge != HideType.None && !_desktopModel.isExpanded && !_desktopDrag.IsPressed)
			{
				_desktopDockTimer.Start();
			}
		}
	}

	private void OnDesktopDockTimerTick(object sender, EventArgs args)
	{
		_desktopDockTimer.Stop();
		UpdateDesktopDockPresentation(animate: true);
	}

	private void DisposeDesktopDocking()
	{
		_desktopDockingDisposed = true;
		_desktopDockTimer.Stop();
		_desktopDockTimer.Tick -= OnDesktopDockTimerTick;
		base.MouseEnter -= OnDesktopDockMouseEnter;
		base.MouseLeave -= OnDesktopDockMouseLeave;
		ResetDesktopDockPresentation();
	}

	private AIWindow(WindowModel model)
		: base(model)
	{
		InitializeAIWindow();
	}

	private void InitializeAIWindow()
	{
		Id = "AI";
		base.Title = (Utils.IsTickPackage() ? "TickTick AI" : Utils.GetString("DidaAssistant"));
		base.MinWidth = 600.0;
		base.MinHeight = 500.0;
		Container.Children.Add(_workspace);
		_immersive.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "WindowBackground");
		Container.Children.Add(_immersive);
		_workspace.AttachConversationContent(_conversation);
		_workspace.SetPrivacyAuthorizationRequired(!HasConsent);
		_workspace.PrivacyAuthorizationAccepted += OnPrivacyAccepted;
		_workspace.NewConversationRequested += OnNewConversation;
		_workspace.ConversationSelected += SelectConversationAsync;
		_workspace.CurrentConversationTitleChanged += SetConversationTitle;
		_conversation.SubmitRequested += SubmitAsync;
		_conversation.StopRequested += () => RunReadyAsync((AIWebAppView view) => view.StopGeneratingAsync());
		_conversation.ScrollToBottomRequested += () => RunReadyAsync((AIWebAppView view) => view.ScrollToBottomAsync());
		_conversation.ApprovalResponseRequested += (string id, string message, string decision, bool approveRemaining) => RunReadyAsync((AIWebAppView view) => view.RespondToApprovalAsync(id, message, decision, approveRemaining));
		_conversation.RatingSubmissionRequested += (AgentRatingSubmission submission) => RunReadyAsync((AIWebAppView view) => view.SubmitRatingAsync(submission));
		_conversation.AppUpdateRequested += delegate
		{
			App.Window?.CheckUpdate();
		};
		base.Loaded += OnWindowLoaded;
	}

	public static async Task ShowWindow(bool force = true)
	{
		if (_opening || Utils.IsWindows7())
		{
			return;
		}
		_opening = true;
		try
		{
			string userId = LocalSettings.Settings.LoginUserId;
			WindowModel model = await WindowModelDao.GetModelByIdAsync("AI");
			if (WindowManager.AppLockOrExit || userId != LocalSettings.Settings.LoginUserId || (!force && (model == null || model.Closed)))
			{
				return;
			}
			if (Current == null)
			{
				if (model != null)
				{
					await WindowModelDao.OpenWindow(model);
				}
				else
				{
					model = await IndependentWindow.GetNewWindowModel("AI");
				}
				if (WindowManager.AppLockOrExit || userId != LocalSettings.Settings.LoginUserId)
				{
					return;
				}
				Current = new AIWindow(model);
			}
			bool isVisible = Current.IsVisible;
			Current.Show();
			if (!isVisible)
			{
				UserActCollectUtils.AddClickEvent("ai_assistant", "show", "full_screen");
			}
			Current.Activate();
		}
		finally
		{
			_opening = false;
		}
	}

	public static void OpenOrCloseWindow(Window owner)
	{
		if (Current != null)
		{
			Current.Close();
		}
		else if (IndependentWindow.CheckCount(owner))
		{
			ShowWindow();
		}
	}

	private async void OnWindowLoaded(object sender, RoutedEventArgs e)
	{
		if (HasConsent && (!_isDesktopWidget || _desktopModel.isExpanded))
		{
			await InitializeBrowserAsync();
		}
	}

	internal Task RefreshConversationsAsync()
	{
		if (_closed || !HasConsent)
		{
			return Task.CompletedTask;
		}
		return _workspace.RefreshConversationsAsync();
	}

	private async void OnPrivacyAccepted(object sender, EventArgs e)
	{
		LocalSettings.Settings.ExtraSettings.SetHasFlag(HasSetFlag.AIAssistantPrivacyConsent);
		LocalSettings.Settings.Save();
		_workspace.SetPrivacyAuthorizationRequired(isRequired: false);
		if (_isDesktopWidget)
		{
			AttachDesktopConversation();
		}
		await InitializeBrowserAsync();
	}

	private async Task InitializeBrowserAsync()
	{
		if (_closed)
		{
			return;
		}
		if (WebView == null)
		{
			AIWebAppView webView = _webViews.Activate(AIWebViewHost.MainWindow).WebView;
			if (_isDesktopWidget)
			{
				webView.SetThemeOverride(_desktopModel.themeId == "dark");
			}
			webView.AgentBridgeReadyChanged += OnBridgeReady;
			webView.AgentNotificationReceived += OnNotification;
			_conversation.AttachWebContent(webView);
		}
		await WebView.EnsureInitializedAsync();
	}

	private async void OnBridgeReady(object sender, EventArgs e)
	{
		if (_closed || sender != WebView)
		{
			return;
		}
		AIWebAppView view = WebView;
		_workspace.SetAgentBridgeReady(view.IsAgentBridgeReady);
		_conversation.SetAgentBridgeReady(view.IsAgentBridgeReady);
		_desktopFloating?.SetAgentBridgeReady(view.IsAgentBridgeReady);
		if (view.IsAgentBridgeReady)
		{
			await _workspace.SynchronizeGenerationStateAsync(view.GetAgentStateAsync);
		}
		bool flag = view.IsAgentBridgeReady;
		if (flag)
		{
			flag = await _webViews.SynchronizeAsync(view);
		}
		if (flag && !_closed)
		{
			ApplyPage(_webViews.SharedPage, _webViews.SharedConversationId);
			JObject approvals = await view.GetApprovalsAsync();
			if (!_closed && view == WebView)
			{
				_conversation.SetApprovals(approvals);
			}
		}
	}

	private Task<bool> RunReadyAsync(Func<AIWebAppView, Task<bool>> action)
	{
		AIWebAppView webView = WebView;
		if (_closed || !HasConsent || webView == null || !webView.IsAgentBridgeReady)
		{
			return Task.FromResult(result: false);
		}
		return action(webView);
	}

	private async void OnNewConversation(object sender, EventArgs e)
	{
		await StartNewConversationAsync();
	}

	private async Task StartNewConversationAsync(bool focusComposer = false)
	{
		if (await RunReadyAsync((AIWebAppView view) => view.StartNewConversationAsync()) && !_closed)
		{
			ApplyPage("home", null);
			if (focusComposer)
			{
				_conversation.FocusNewConversationComposer();
			}
		}
	}

	private async Task<bool> SelectConversationAsync(string id, string title)
	{
		if (!(await RunReadyAsync((AIWebAppView view) => view.SwitchConversationAsync(id))) || _closed)
		{
			return false;
		}
		ApplyPage("conversation", id);
		SetConversationTitle(title);
		_workspace.HideDetail();
		return true;
	}

	private async Task<bool> SubmitAsync(string content, string messageId)
	{
		if (!(await RunReadyAsync((AIWebAppView view) => view.SubmitMessageAsync(content, messageId))) || _closed)
		{
			return false;
		}
		ApplyPage("conversation", _webViews.SharedConversationId);
		return true;
	}

	private void SetConversationTitle(string title)
	{
		_workspace.SetConversationTitle(title);
		_desktopFloating?.SetConversationTitle(title);
	}

	private void ApplyPage(string page, string id)
	{
		if (_closed)
		{
			return;
		}
		bool flag = string.Equals(page, "home", StringComparison.OrdinalIgnoreCase);
		if (flag || string.Equals(page, "conversation", StringComparison.OrdinalIgnoreCase))
		{
			_webViews.CompleteActivePageState(page, id);
			_conversation.SetConversationActive(!flag, _webViews.SharedConversationId);
			_workspace.SetCurrentConversation(flag ? null : _webViews.SharedConversationId);
			if (flag)
			{
				SetConversationTitle(null);
				_workspace.HideDetail();
			}
		}
	}

	private void OnNotification(object sender, AgentBridgeNativeEventArgs e)
	{
		if (_closed || sender != WebView)
		{
			return;
		}
		JToken payload = e.Payload;
		string methodName = e.MethodName;
		if (methodName == null)
		{
			return;
		}
		switch (methodName.Length)
		{
		case 18:
			switch (methodName[7])
			{
			case 't':
				if (methodName == "agent.stateChanged")
				{
					bool? flag2 = (bool?)payload?["isGenerating"];
					if (flag2.HasValue)
					{
						bool valueOrDefault2 = flag2 == true;
						_conversation.SetGenerating(valueOrDefault2);
						_workspace.SetGenerating(valueOrDefault2);
						SetDesktopGenerating(valueOrDefault2);
					}
					flag2 = (bool?)payload?["isQuotaExceeded"];
					if (flag2.HasValue)
					{
						bool valueOrDefault3 = flag2 == true;
						_conversation.SetTokenLimitReached(valueOrDefault3);
					}
					if (!_webViews.IsActiveSynchronizationPending && MainWindow.IsAIPageStateReady(payload) && !MainWindow.ShouldDeferRestoringAIHomeState(payload, WebView.IsAgentPageStateResolved, _conversation.HasActiveConversation))
					{
						ApplyPage((string?)payload?["page"], ((string?)payload?["currentConversationId"]) ?? ((string?)payload?["conversationId"]));
					}
				}
				break;
			case 'i':
				if (methodName == "agent.fillComposer")
				{
					_conversation.FillComposer((string?)payload?["content"], (string?)payload?["messageId"]);
				}
				break;
			case 'h':
				if (methodName == "agent.showFeedback")
				{
					_conversation.ShowFeedback(payload);
				}
				break;
			case 'a':
				if (methodName == "agent.ratingResult")
				{
					_conversation.ApplyRatingResult(payload);
				}
				break;
			}
			break;
		case 23:
			switch (methodName[6])
			{
			case 't':
				if (methodName == "agent.tokenLimitReached")
				{
					_conversation.SetTokenLimitReached(isTokenLimitReached: true);
				}
				break;
			case 'a':
				if (methodName == "agent.appUpdateRequired")
				{
					bool? flag = AIConversationView.ResolveAppUpdateRequiredBlocking(payload);
					if (flag.HasValue)
					{
						_conversation.SetAppUpdateRequired(_webViews.SharedConversationId, flag.Value);
					}
				}
				break;
			}
			break;
		case 17:
			if (methodName == "agent.pageChanged")
			{
				string text3 = (string?)payload?["page"];
				if (!_webViews.IsActiveSynchronizationPending && (WebView.IsAgentPageStateResolved || text3 == "conversation"))
				{
					ApplyPage(text3, (string?)payload?["conversationId"]);
				}
			}
			break;
		case 25:
			if (methodName == "agent.conversationCreated")
			{
				string text = ((string?)payload?["conversationId"]) ?? ((string?)payload?["id"]);
				string text2 = (string?)payload?["title"];
				bool valueOrDefault = (bool?)payload?["isLocalOnly"] == true;
				if (!_webViews.IsActiveSynchronizationPending)
				{
					ApplyPage("conversation", text);
				}
				SetConversationTitle(text2);
				_workspace.UpsertConversation(text, text2, valueOrDefault);
				if (!valueOrDefault)
				{
					_workspace.RefreshConversationsAsync();
				}
			}
			break;
		case 30:
			if (methodName == "agent.refreshConversationTitle")
			{
				string text4 = (string?)payload?["conversationId"];
				string text5 = (string?)payload?["title"];
				_workspace.UpdateConversationTitle(text4, text5);
				if (string.IsNullOrWhiteSpace(text4) || _workspace.IsCurrentConversation(text4))
				{
					SetConversationTitle(text5);
				}
				if (string.IsNullOrWhiteSpace(text5))
				{
					RefreshTitleAsync(text4);
				}
			}
			break;
		case 22:
			if (methodName == "agent.approvalsChanged")
			{
				_conversation.SetApprovals(payload);
			}
			break;
		case 43:
			if (methodName == "agent.scrollToBottomButtonVisibilityChanged")
			{
				_conversation.SetScrollToBottomButtonVisible(AIConversationView.ResolveScrollToBottomButtonVisibility(payload));
			}
			break;
		case 26:
			if (methodName == "agent.viewFullCardListMode")
			{
				_workspace.HideDetail();
			}
			break;
		case 15:
			if (methodName == "agent.cardClick")
			{
				OpenCardAsync(payload);
			}
			break;
		}
	}

	private async Task RefreshTitleAsync(string id)
	{
		await _workspace.RefreshConversationsAsync();
		if (!_closed && _workspace.IsCurrentConversation(id))
		{
			SetConversationTitle(_workspace.GetConversationTitle(id));
		}
	}

	private async Task OpenCardAsync(JToken payload)
	{
		if (_isDesktopWidget)
		{
			if (!_closed && App.Window != null)
			{
				await App.Window.HandleAgentCardClickAsync(payload, this);
			}
			return;
		}
		AgentCardType? type = MainWindow.ParseAgentCardType((string?)payload?["type"]);
		if (!MainWindow.CheckAgentCardFeatureEnabled(type, this) || !type.HasValue)
		{
			return;
		}
		switch (type.GetValueOrDefault())
		{
		case AgentCardType.Task:
		{
			string taskId = (string?)payload?["taskId"];
			if (!string.IsNullOrWhiteSpace(taskId))
			{
				await MainWindow.SyncAgentCardDataAsync();
				if (!_closed && await TaskUtils.TryLoadTask(taskId, (string?)payload?["projectId"], checkDeleted: true) != null && !_closed && await _workspace.ShowTaskDetailAsync(taskId) && !_closed)
				{
					UserActCollectUtils.AddClickEvent("ai_assistant", "result_card", "task_detail");
				}
			}
			break;
		}
		case AgentCardType.Habit:
		{
			string habitId = (string?)payload?["habitId"];
			if (string.IsNullOrWhiteSpace(habitId))
			{
				break;
			}
			await MainWindow.SyncAgentHabitCardDataAsync(habitId);
			if (_closed)
			{
				break;
			}
			if (!(await HabitService.IsHabitDeleted(habitId)) && !_closed)
			{
				if (_workspace.ShowHabitDetail(habitId))
				{
					UserActCollectUtils.AddClickEvent("ai_assistant", "result_card", "habit_detail");
				}
			}
			else if (!_closed)
			{
				TryToastString(null, Utils.GetString("HabitNotExistOrDeleted"));
			}
			break;
		}
		case AgentCardType.Project:
		{
			string projectId = (string?)payload?["projectId"];
			if (string.IsNullOrWhiteSpace(projectId))
			{
				break;
			}
			await MainWindow.SyncAgentCardDataAsync();
			if (_closed)
			{
				break;
			}
			ProjectModel projectModel = await ProjectDao.GetProjectById(projectId);
			if (!_closed)
			{
				if (projectModel == null || projectModel.delete_status)
				{
					TryToastString(null, Utils.GetString("ListNotExistOrDeleted"));
				}
				else if (TaskListWindow.Windows.ContainsKey(projectId) || IndependentWindow.CheckCount(this))
				{
					await TaskListWindow.ShowWindow(projectId, force: true, this);
					UserActCollectUtils.AddClickEvent("ai_assistant", "result_card", "project_list");
				}
			}
			break;
		}
		case AgentCardType.Focus:
		{
			string focusId = (string?)payload?["focusId"];
			if (string.IsNullOrWhiteSpace(focusId))
			{
				break;
			}
			await MainWindow.SyncAgentFocusCardDataAsync(focusId, (int?)payload?["focusType"]);
			if (_closed)
			{
				break;
			}
			PomodoroModel pomodoroModel = await PomoService.GetAdjustPomo(focusId);
			if (!_closed)
			{
				if (pomodoroModel == null)
				{
					TryToastString(null, Utils.GetString("FocusNotExistOrDeleted"));
					break;
				}
				PomoDetailWindow pomoDetailWindow = new PomoDetailWindow(new PomoDisplayViewModel(pomodoroModel, pomodoroModel.Tasks), (CalendarDisplayViewModel)null);
				pomoDetailWindow.SetCustomStyle();
				pomoDetailWindow.ShowCentered(this);
				UserActCollectUtils.AddClickEvent("ai_assistant", "result_card", "focus_record");
			}
			break;
		}
		}
	}

	protected override void OnClosing()
	{
		DisposeDesktopWidget();
		_closed = true;
		base.Loaded -= OnWindowLoaded;
		if (WebView != null)
		{
			WebView.AgentBridgeReadyChanged -= OnBridgeReady;
			WebView.AgentNotificationReceived -= OnNotification;
			_conversation.DetachWebContent(WebView);
		}
		_webViews.DisposeAsync();
		_immersive.Child = null;
		_immersiveSource = null;
		_workspace.Dispose();
		Container.Children.Clear();
		if (_isDesktopWidget)
		{
			DesktopWidget = null;
		}
		else
		{
			Current = null;
		}
	}

	public async void EnterImmersiveMode(string taskId, int caretIndex)
	{
		_immersiveSource = TaskDetailView.TakePendingImmersiveSource(taskId);
		ImmersiveContent content = new ImmersiveContent();
		_immersive.Child = content;
		_immersive.Visibility = Visibility.Visible;
		await content.LoadData(taskId, _immersiveSource);
		if (!_closed && _immersive.Child == content)
		{
			MarkDownEditor markDownEditor = content.TaskDetail.GetContentText() ?? content.TaskDetail.GetDescText();
			if (markDownEditor != null)
			{
				markDownEditor.EditBox.CaretOffset = Math.Max(0, Math.Min(caretIndex, markDownEditor.EditBox.Document.TextLength));
				markDownEditor.FocusEditBox();
				markDownEditor.EditBox.TextArea.Caret.BringCaretToView();
			}
		}
	}

	public void ExitImmersiveMode()
	{
		TaskDetailView source = _immersiveSource;
		source?.CopyEditorStateFrom((_immersive.Child as ImmersiveContent)?.TaskDetail);
		_immersive.Visibility = Visibility.Collapsed;
		_immersive.Child = null;
		_immersiveSource = null;
		string taskId = source?.TaskId;
		base.Dispatcher.BeginInvoke((Action)delegate
		{
			if (!_closed && base.IsActive)
			{
				TaskDetailView taskDetailView = source;
				if (taskDetailView != null && taskDetailView.IsVisible && !(source.TaskId != taskId) && _immersive.Visibility != Visibility.Visible)
				{
					MarkDownEditor obj = source.GetContentText() ?? source.GetDescText();
					obj?.FocusEditBox();
					obj?.EditBox.TextArea.Caret.BringCaretToView();
				}
			}
		}, DispatcherPriority.ContextIdle);
	}

	public Task NavigateTask(ProjectTask task)
	{
		return TaskDetailWindow.NavigateProjectTask(task, this);
	}

	public void OnProjectWidthChanged(double width)
	{
	}

	public void OnDetailWidthChanged(double width)
	{
	}

	public void SaveSelectedProject(string projectId)
	{
	}

	public double GetProjectWidth()
	{
		return 0.0;
	}

	public double GetDetailWidth()
	{
		return 1.0;
	}

	public string GetSelectedProject()
	{
		return null;
	}

	public double GetListContainerWidth()
	{
		return Container.ActualWidth;
	}

	public void SetMinSize(int width, int height)
	{
		base.MinWidth = width;
		base.MinHeight = height;
	}
}
