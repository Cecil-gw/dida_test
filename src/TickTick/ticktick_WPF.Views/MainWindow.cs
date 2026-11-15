using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using KotlinModels;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.SyncServices;
using ticktick_WPF.Util;
using ticktick_WPF.Util.DragAndDrop;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.Util.TouchPad;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.AI;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Config;
using ticktick_WPF.Views.CountDown.Views;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Eisenhower;
using ticktick_WPF.Views.Habit;
using ticktick_WPF.Views.Kanban;
using ticktick_WPF.Views.Lock;
using ticktick_WPF.Views.MainListView;
using ticktick_WPF.Views.MainListView.DetailView;
using ticktick_WPF.Views.MainListView.ProjectList;
using ticktick_WPF.Views.MainListView.TaskList;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.NewUser;
using ticktick_WPF.Views.Pomo;
using ticktick_WPF.Views.QuickAdd;
using ticktick_WPF.Views.Search;
using ticktick_WPF.Views.Summary;
using ticktick_WPF.Views.TabBar;
using ticktick_WPF.Views.TaskList;
using ticktick_WPF.Views.TaskList.Item;
using ticktick_WPF.Views.Theme;
using ticktick_WPF.Views.Timeline;
using ticktick_WPF.Views.Undo;
using ticktick_WPF.Views.Update;
using ticktick_WPF.Views.Upgrade;
using ticktick_WPF.Views.User;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Views;

[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant, InstanceContextMode = InstanceContextMode.Single)]
public class MainWindow : MyWindow, IListViewParent, IKeyBinding, IThemeable, IToastShowWindow, INavigateProject, IComponentConnector
{
	private bool _goDown;

	private bool _viewDown;

	private bool _tabMenuFirst = true;

	private bool _thirdColumnFolded;

	private bool _projectFloat;

	private bool _needHideDetail;

	private const int WM_MOUSEHWHEEL = 526;

	private const int WM_MOUSEMOVE = 512;

	private static bool _isSpliteDragging;

	private static bool _checkShowTutorial;

	private static bool _proDialogShowed;

	private static bool _updateVersionShowed;

	private static bool _versionOutdated;

	private static DateTime _lastSyncTime;

	private bool _canDragToNormal;

	private System.Windows.Point _dragStartPoint;

	private string _currentMode = "list";

	private bool _goToPremium;

	private DateTime _headerClickOldTime = DateTime.Now;

	private DateTime? _lastShowUpdateTime;

	private DateTime? _lastStateChangeSyncTime;

	private Storyboard _leftProjectMenu;

	private System.Timers.Timer _lockHandler;

	private TaskDetailWindow _navigateWindow;

	private readonly AIClientHostState _aiClientHostState = new AIClientHostState();

	private readonly AIWebViewHostManager _aiWebViews = new AIWebViewHostManager();

	private readonly SemaphoreSlim _aiWebViewHostTransitionGate = new SemaphoreSlim(1, 1);

	private int _aiWebViewHostAttachmentVersion;

	private AIFloatingWindow _aiFloatingWindow;

	private AIConversationView _aiConversationView;

	private AIWorkspaceView _aiWorkspaceView;

	private AIWebAppView _mainAIWebViewAwaitingMaximizedRestore;

	private int _mainAIWebViewRestoreVersion;

	private WindowState _lastNonMinimizedWindowState;

	private bool _isAIFloatingEntryGuidePending;

	private bool _needShowTemplateGuide;

	private System.Windows.Controls.ContextMenu _notifyIconMenu;

	private DateTime? _refreshSizeTime;

	private bool _upgradeShowing;

	private int _windowPresentationVersion;

	private Task _showWindowTask;

	private double _width = -1.0;

	private DateTime? _calendarInListDate;

	public CalendarControl MainCalendar;

	public HabitContainer HabitView;

	public SummaryControl Summary;

	public MatrixContainer MatrixContainer;

	public CountDownContainer CountDownContainer;

	private SearchDialog SearchDialog;

	private SearchOperationDialog SearchOperationDialog;

	private bool _hideMenu;

	private double _listDetailScale;

	public new bool IsLoaded;

	private bool _showed;

	private DisplayModule _previousModule;

	private bool _ignoreFocus;

	private bool _mouseDown;

	private bool _inPrimaryScreen;

	private bool _enableHandleMinMax = true;

	private int _saveModule = -1;

	private bool _iconMenuShowing;

	private AIClientPresentation _trackedAIPresentation;

	private int _resetClickTimes;

	private bool _altKeyDown;

	private WeakReference<TaskDetailView> _immersiveSourceDetail;

	internal MainWindow Window;

	internal Grid Container;

	internal ColumnDefinition ColumnOneDefine;

	internal Border WindowBackground;

	internal ImageBrush WindowBackgroundImage;

	internal Grid CLGrid;

	internal ColumnDefinition LCFirstColumn;

	internal ColumnDefinition LCSecondColumn;

	internal Border ClBorder;

	internal Border CalendarBorder;

	internal RectangleGeometry CalendarBorderClip;

	internal ScaleTransform CalendarTransform;

	internal Border CalendarThicknessBorder;

	internal Border ListBorder;

	internal RectangleGeometry ListBorderClip;

	internal ScaleTransform ListTransform;

	internal Border ListThicknessBorder;

	internal GridSplitter LCSplitter;

	internal Border FocusBorder;

	internal RectangleGeometry FocusBorderClip;

	internal Border HabitBorder;

	internal RectangleGeometry HabitBorderClip;

	internal Border EisenhoverBorder;

	internal RectangleGeometry MatrixBorderClip;

	internal Border CountDownBorder;

	internal RectangleGeometry CountDownBorderClip;

	internal Border AIBorder;

	internal RectangleGeometry AIBorderClip;

	internal Border ThicknessBorder;

	internal LeftMenuBar LeftTabBar;

	internal ContentControl TutorialContent;

	internal StackPanel ResizeButtons;

	internal System.Windows.Controls.Button UpgradeButton;

	internal System.Windows.Controls.Button MinButton;

	internal System.Windows.Shapes.Rectangle MinPath;

	internal System.Windows.Controls.Button MaxButton;

	internal System.Windows.Shapes.Path MaxPath;

	internal System.Windows.Controls.Button NormalButton;

	internal System.Windows.Shapes.Path NormalPath;

	internal System.Windows.Controls.Button CloseButton;

	internal Polygon X;

	internal ContentControl AIFloatingLayer;

	internal AIFloatingEntry AIFloatingEntry;

	internal Popup AIFloatingEntryGuidePopup;

	internal ArrowHintUserControl AIFloatingEntryGuideHint;

	internal Border ImmersiveGrid;

	internal Border CoachMarks;

	internal Grid MaskPanel;

	internal Border ShortCutMask;

	internal Grid ToastGrid;

	internal ContentControl GuideContent;

	internal ContentControl BottomContent;

	private bool _contentLoaded;

	private static Thickness noMargin { get; }

	private static Thickness border { get; }

	private static CornerRadius noRadius { get; }

	private static CornerRadius withRadius { get; }

	private static Thickness cardNTopMargin { get; }

	private static Thickness cardMargin { get; }

	private static GridLength flatLeftBarWidth { get; }

	private static GridLength cardLeftBarWidth { get; }

	private CalendarProView calendarProView { get; set; }

	public bool IsInTutorial { get; set; }

	public bool ShowCalendar => LocalSettings.Settings.MainWindowDisplayModule == 1;

	public bool ShowList => LocalSettings.Settings.MainWindowDisplayModule == 0;

	public bool ShowHabit => LocalSettings.Settings.MainWindowDisplayModule == 3;

	public bool ShowMatrix => LocalSettings.Settings.MainWindowDisplayModule == 2;

	public bool ShowFocus => LocalSettings.Settings.MainWindowDisplayModule == 4;

	public bool ShowCountdown => LocalSettings.Settings.MainWindowDisplayModule == 5;

	public bool ShowAI => LocalSettings.Settings.MainWindowDisplayModule == 6;

	public ListViewContainer ListView { get; set; }

	public Action<DisplayModule> OnModuleSwitch { get; set; }

	public static NewVersionReminderWindow NewVersionReminderWindow { get; set; }

	public static bool isWaitingForUpdate { get; set; }

	public event EventHandler<SearchExtra> Search;

	private void OnInterfaceStyleChanged(object sender, EventArgs e)
	{
		if (IsLoaded)
		{
			SwitchDisplayModule();
		}
	}

	private void OnThemeChanged(object sender, EventArgs e)
	{
		OnSwitchTheme();
	}

	private void SetInterfaceStyle()
	{
		try
		{
			bool flag = false;
			bool flag2 = false;
			if ((ShowCalendar || ShowList) && !string.IsNullOrEmpty(LocalSettings.Settings.ExtraSettings.ListCalendarSettings))
			{
				ListCalendarSettings listCalendarSettings = JsonConvert.DeserializeObject<ListCalendarSettings>(LocalSettings.Settings.ExtraSettings.ListCalendarSettings);
				if (listCalendarSettings != null)
				{
					flag = listCalendarSettings.ShowCInL;
					flag2 = listCalendarSettings.ShowLInC;
				}
			}
			switch (LocalSettings.Settings.ExtraSettings.InterfaceStyle)
			{
			case InterfaceStyle.Flat:
			{
				RectangleGeometry listBorderClip3 = ListBorderClip;
				RectangleGeometry listBorderClip4 = ListBorderClip;
				RectangleGeometry calendarBorderClip3 = CalendarBorderClip;
				RectangleGeometry calendarBorderClip4 = CalendarBorderClip;
				RectangleGeometry matrixBorderClip3 = MatrixBorderClip;
				RectangleGeometry matrixBorderClip4 = MatrixBorderClip;
				RectangleGeometry focusBorderClip3 = FocusBorderClip;
				RectangleGeometry focusBorderClip4 = FocusBorderClip;
				RectangleGeometry habitBorderClip3 = HabitBorderClip;
				RectangleGeometry habitBorderClip4 = HabitBorderClip;
				RectangleGeometry countDownBorderClip3 = CountDownBorderClip;
				RectangleGeometry countDownBorderClip4 = CountDownBorderClip;
				RectangleGeometry aIBorderClip2 = AIBorderClip;
				double num = (AIBorderClip.RadiusY = 0.0);
				double num3 = (aIBorderClip2.RadiusX = num);
				double num5 = (countDownBorderClip4.RadiusY = num3);
				double num7 = (countDownBorderClip3.RadiusX = num5);
				double num9 = (habitBorderClip4.RadiusY = num7);
				double num11 = (habitBorderClip3.RadiusX = num9);
				double num13 = (focusBorderClip4.RadiusY = num11);
				double num15 = (focusBorderClip3.RadiusX = num13);
				double num17 = (matrixBorderClip4.RadiusY = num15);
				double num19 = (matrixBorderClip3.RadiusX = num17);
				double num21 = (calendarBorderClip4.RadiusY = num19);
				double num23 = (calendarBorderClip3.RadiusX = num21);
				double radiusX = (listBorderClip4.RadiusY = num23);
				listBorderClip3.RadiusX = radiusX;
				Border calendarThicknessBorder2 = CalendarThicknessBorder;
				Border listThicknessBorder2 = ListThicknessBorder;
				CornerRadius cornerRadius = (ThicknessBorder.CornerRadius = noRadius);
				CornerRadius cornerRadius3 = (listThicknessBorder2.CornerRadius = cornerRadius);
				calendarThicknessBorder2.CornerRadius = cornerRadius3;
				if ((!flag2 || !ShowCalendar) && (!flag || !ShowList))
				{
					Border listBorder2 = ListBorder;
					Border calendarBorder2 = CalendarBorder;
					Border eisenhoverBorder2 = EisenhoverBorder;
					Border focusBorder2 = FocusBorder;
					Border habitBorder2 = HabitBorder;
					Border countDownBorder2 = CountDownBorder;
					Thickness thickness = (AIBorder.Margin = noMargin);
					Thickness thickness3 = (countDownBorder2.Margin = thickness);
					Thickness thickness5 = (habitBorder2.Margin = thickness3);
					Thickness thickness7 = (focusBorder2.Margin = thickness5);
					Thickness thickness9 = (eisenhoverBorder2.Margin = thickness7);
					Thickness margin = (calendarBorder2.Margin = thickness9);
					listBorder2.Margin = margin;
					if (ListView != null)
					{
						ListView.Margin = noMargin;
					}
					if (MainCalendar != null)
					{
						MainCalendar.Margin = noMargin;
					}
					if (MatrixContainer != null)
					{
						MatrixContainer.Margin = noMargin;
					}
					if (TickFocusManager.MainFocus != null)
					{
						TickFocusManager.MainFocus.Margin = noMargin;
					}
					if (HabitView != null)
					{
						HabitView.Margin = noMargin;
					}
					if (CountDownContainer != null)
					{
						CountDownContainer.Margin = noMargin;
					}
					if (_aiWorkspaceView != null)
					{
						_aiWorkspaceView.Margin = noMargin;
					}
				}
				break;
			}
			case InterfaceStyle.Card:
			{
				RectangleGeometry listBorderClip = ListBorderClip;
				RectangleGeometry listBorderClip2 = ListBorderClip;
				RectangleGeometry calendarBorderClip = CalendarBorderClip;
				RectangleGeometry calendarBorderClip2 = CalendarBorderClip;
				RectangleGeometry matrixBorderClip = MatrixBorderClip;
				RectangleGeometry matrixBorderClip2 = MatrixBorderClip;
				RectangleGeometry focusBorderClip = FocusBorderClip;
				RectangleGeometry focusBorderClip2 = FocusBorderClip;
				RectangleGeometry habitBorderClip = HabitBorderClip;
				RectangleGeometry habitBorderClip2 = HabitBorderClip;
				RectangleGeometry countDownBorderClip = CountDownBorderClip;
				RectangleGeometry countDownBorderClip2 = CountDownBorderClip;
				RectangleGeometry aIBorderClip = AIBorderClip;
				double num = (AIBorderClip.RadiusY = 14.0);
				double num3 = (aIBorderClip.RadiusX = num);
				double num5 = (countDownBorderClip2.RadiusY = num3);
				double num7 = (countDownBorderClip.RadiusX = num5);
				double num9 = (habitBorderClip2.RadiusY = num7);
				double num11 = (habitBorderClip.RadiusX = num9);
				double num13 = (focusBorderClip2.RadiusY = num11);
				double num15 = (focusBorderClip.RadiusX = num13);
				double num17 = (matrixBorderClip2.RadiusY = num15);
				double num19 = (matrixBorderClip.RadiusX = num17);
				double num21 = (calendarBorderClip2.RadiusY = num19);
				double num23 = (calendarBorderClip.RadiusX = num21);
				double radiusX = (listBorderClip2.RadiusY = num23);
				listBorderClip.RadiusX = radiusX;
				Border calendarThicknessBorder = CalendarThicknessBorder;
				Border listThicknessBorder = ListThicknessBorder;
				CornerRadius cornerRadius = (ThicknessBorder.CornerRadius = withRadius);
				CornerRadius cornerRadius3 = (listThicknessBorder.CornerRadius = cornerRadius);
				calendarThicknessBorder.CornerRadius = cornerRadius3;
				Border listBorder = ListBorder;
				Border calendarBorder = CalendarBorder;
				Border eisenhoverBorder = EisenhoverBorder;
				Border focusBorder = FocusBorder;
				Border habitBorder = HabitBorder;
				Border countDownBorder = CountDownBorder;
				Thickness thickness = (AIBorder.Margin = cardMargin);
				Thickness thickness3 = (countDownBorder.Margin = thickness);
				Thickness thickness5 = (habitBorder.Margin = thickness3);
				Thickness thickness7 = (focusBorder.Margin = thickness5);
				Thickness thickness9 = (eisenhoverBorder.Margin = thickness7);
				Thickness margin = (calendarBorder.Margin = thickness9);
				listBorder.Margin = margin;
				if (ListView != null)
				{
					ListView.Margin = cardNTopMargin;
				}
				if (MainCalendar != null)
				{
					MainCalendar.Margin = cardNTopMargin;
				}
				if (MatrixContainer != null)
				{
					MatrixContainer.Margin = cardNTopMargin;
				}
				if (TickFocusManager.MainFocus != null)
				{
					TickFocusManager.MainFocus.Margin = cardNTopMargin;
				}
				if (HabitView != null)
				{
					HabitView.Margin = cardNTopMargin;
				}
				if (CountDownContainer != null)
				{
					CountDownContainer.Margin = cardNTopMargin;
				}
				if (_aiWorkspaceView != null)
				{
					_aiWorkspaceView.Margin = cardNTopMargin;
				}
				CLGrid.Background = System.Windows.Media.Brushes.Transparent;
				ClBorder.Visibility = Visibility.Collapsed;
				break;
			}
			}
			UpdateCLGridSize();
		}
		catch (Exception)
		{
		}
		OnSwitchTheme();
	}

	private void OnSwitchTheme()
	{
		switch (LocalSettings.Settings.ExtraSettings.InterfaceStyle)
		{
		case InterfaceStyle.Flat:
			SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "MainWindowBrush");
			break;
		case InterfaceStyle.Card:
			if (LocalSettings.Settings.ThemeId == "White" || ThemeKey.IsDarkTheme(LocalSettings.Settings.ThemeId))
			{
				SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "MainWindowCardBrush");
			}
			else
			{
				SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "MainWindowBrush");
			}
			break;
		}
		if (!IsLoaded)
		{
			return;
		}
		if (ShowCalendar || ShowList)
		{
			ThicknessBorder.Visibility = Visibility.Collapsed;
			if ((LocalSettings.Settings.ThemeId == "White" || ThemeKey.IsDarkTheme(LocalSettings.Settings.ThemeId)) && LocalSettings.Settings.ExtraSettings.InterfaceStyle == InterfaceStyle.Card)
			{
				Border calendarThicknessBorder = CalendarThicknessBorder;
				Thickness borderThickness = (ListThicknessBorder.BorderThickness = border);
				calendarThicknessBorder.BorderThickness = borderThickness;
			}
			else
			{
				Border calendarThicknessBorder2 = CalendarThicknessBorder;
				Thickness borderThickness = (ListThicknessBorder.BorderThickness = noMargin);
				calendarThicknessBorder2.BorderThickness = borderThickness;
			}
		}
		else
		{
			ThicknessBorder.Visibility = Visibility.Visible;
			if ((LocalSettings.Settings.ThemeId == "White" || ThemeKey.IsDarkTheme(LocalSettings.Settings.ThemeId)) && LocalSettings.Settings.ExtraSettings.InterfaceStyle == InterfaceStyle.Card)
			{
				ThicknessBorder.BorderThickness = border;
			}
			else
			{
				ThicknessBorder.BorderThickness = noMargin;
			}
		}
		ThemeUtil.GetColor("SystemButtonBrush");
	}

	private void LoadCalendar()
	{
		SetViewVisible(CalendarBorder);
		MainCalendar?.BindEvents();
		if (MainCalendar == null)
		{
			InitCalendar(CalendarViewSourceType.Main);
		}
		SetMainWindowsMinSize(isList: false);
		UpdateListCalendarDisplay(isCal: true);
		CalendarBorder.Child = MainCalendar;
		CalendarEventLoader.Reset();
		MainCalendar.CheckPro();
		MainCalendar.GotoCalDate();
		MainCalendar.GetFocus();
		MainCalendar.CollectUserEvent();
	}

	private void LoadCalInTaskView()
	{
		CalendarBorder.Visibility = Visibility.Visible;
		MainCalendar?.BindEvents();
		if (MainCalendar == null)
		{
			InitCalendar(CalendarViewSourceType.WithList);
		}
		CalendarBorder.Child = MainCalendar;
		CalendarEventLoader.Reset();
		MainCalendar.ArrangePanel.TryReloadTasks();
	}

	private void UpdateListCalendarDisplay(bool isCal, bool inSearch = false, bool addEvent = true)
	{
		if (LocalSettings.Settings.ExtraSettings.ListCalendarSettings != null && UserDao.IsPro())
		{
			try
			{
				ListCalendarSettings listCalendarSettings = JsonConvert.DeserializeObject<ListCalendarSettings>(LocalSettings.Settings.ExtraSettings.ListCalendarSettings);
				if (listCalendarSettings != null)
				{
					bool showLInC = listCalendarSettings.ShowLInC;
					bool flag = listCalendarSettings.ShowCInL && !inSearch;
					base.MinWidth = 680.0;
					CalendarBorder.SetValue(Grid.ColumnProperty, (!isCal && flag) ? 1 : 0);
					CalendarBorder.SetValue(Grid.ColumnSpanProperty, (!isCal || showLInC) ? 1 : 2);
					ListBorder.SetValue(Grid.ColumnProperty, (isCal && showLInC) ? 1 : 0);
					ListBorder.SetValue(Grid.ColumnSpanProperty, (isCal || flag) ? 1 : 2);
					if (isCal && showLInC)
					{
						LoadTaskViewInCalView();
						LCSplitter.Visibility = Visibility.Visible;
						CLGrid.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "ParallelBackground");
						CalendarBorder.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "ParallelPartBackground");
						ListBorder.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "ParallelPartBackground");
						CalendarBorder.Margin = new Thickness(10.0, 28.0, 0.0, 10.0);
						ListBorder.Margin = new Thickness(10.0, 28.0, 10.0, 10.0);
						ClBorder.Visibility = Visibility.Visible;
						CalendarBorderClip.RadiusX = 12.0;
						CalendarBorderClip.RadiusY = 12.0;
						CalendarBorder.CornerRadius = new CornerRadius(12.0);
						ListBorderClip.RadiusX = 12.0;
						ListBorderClip.RadiusY = 12.0;
						ListView.Margin = new Thickness(0.0, -12.0, 0.0, 0.0);
						ListView.SetParallelShow(showWithCal: true, inCal: true);
						MainCalendar.Margin = new Thickness(0.0, -12.0, 0.0, 0.0);
						MainCalendar.SetParallelShow(parallel: true, inTask: false);
						ListView?.LoadSavedProject();
						base.MinWidth = 680.0;
						if (addEvent)
						{
							UserActCollectUtils.AddClickEvent("split_view", "calendar+task", "view_show");
						}
						SetParallelShown();
					}
					else if (!isCal && flag)
					{
						LoadCalInTaskView();
						LCSplitter.Visibility = Visibility.Visible;
						CLGrid.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "ParallelBackground");
						CalendarBorder.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "ParallelPartBackground");
						ListBorder.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "ParallelPartBackground");
						CalendarBorder.Margin = new Thickness(10.0, 28.0, 10.0, 10.0);
						ListBorder.Margin = new Thickness(10.0, 28.0, 0.0, 10.0);
						ClBorder.Visibility = Visibility.Visible;
						CalendarBorderClip.RadiusX = 12.0;
						CalendarBorderClip.RadiusY = 12.0;
						CalendarBorder.CornerRadius = new CornerRadius(12.0);
						ListBorderClip.RadiusX = 12.0;
						ListBorderClip.RadiusY = 12.0;
						ListView.Margin = new Thickness(0.0, -12.0, 0.0, 0.0);
						ListView.SetParallelShow(showWithCal: true, inCal: false);
						MainCalendar.Margin = new Thickness(0.0, -12.0, 0.0, 0.0);
						MainCalendar.SetParallelShow(parallel: true, inTask: true);
						MainCalendar.GotoListCalDate();
						base.MinWidth = 680.0;
						if (addEvent)
						{
							UserActCollectUtils.AddClickEvent("split_view", "task+calendar", "view_show");
						}
						SetParallelShown();
					}
					if ((isCal && showLInC) || (!isCal && flag))
					{
						base.MinWidth = 680.0;
						double num = (isCal ? listCalendarSettings.CL_Divide : listCalendarSettings.LC_Divide);
						double num2 = Math.Max(50.0, ColumnOneDefine.ActualWidth);
						double num3 = (base.ActualWidth - num2) * num / (1.0 + num);
						if (num3 < 320.0)
						{
							num3 = 320.0;
						}
						double num4 = base.ActualWidth - num2 - num3;
						if (num4 < 310.0)
						{
							num4 = 310.0;
						}
						LCFirstColumn.Width = new GridLength(1.0, GridUnitType.Star);
						LCSecondColumn.Width = new GridLength(num3 / num4, GridUnitType.Star);
						LCSplitter.IsHitTestVisible = true;
					}
					else
					{
						ClearParallelShow(isCal);
					}
				}
			}
			catch (Exception)
			{
				CalendarBorder.SetValue(Grid.ColumnProperty, 0);
				CalendarBorder.SetValue(Grid.ColumnSpanProperty, 2);
				ListBorder.SetValue(Grid.ColumnProperty, 0);
				ListBorder.SetValue(Grid.ColumnSpanProperty, 2);
				LCSplitter.Visibility = Visibility.Collapsed;
			}
		}
		else
		{
			CalendarBorder.SetValue(Grid.ColumnProperty, 0);
			CalendarBorder.SetValue(Grid.ColumnSpanProperty, 2);
			ListBorder.SetValue(Grid.ColumnProperty, 0);
			ListBorder.SetValue(Grid.ColumnSpanProperty, 2);
			ClearParallelShow(isCal);
		}
		CalendarThicknessBorder.SetValue(Grid.ColumnProperty, CalendarBorder.GetValue(Grid.ColumnProperty));
		CalendarThicknessBorder.SetValue(Grid.ColumnSpanProperty, CalendarBorder.GetValue(Grid.ColumnSpanProperty));
		ListThicknessBorder.SetValue(Grid.ColumnProperty, ListBorder.GetValue(Grid.ColumnProperty));
		ListThicknessBorder.SetValue(Grid.ColumnSpanProperty, ListBorder.GetValue(Grid.ColumnSpanProperty));
		ChangeHeaderButtonColor();
		SetInterfaceStyle();
		void ClearParallelShow(bool resetCal)
		{
			ListView?.SetParallelShow(showWithCal: false, inCal: false);
			MainCalendar?.SetParallelShow(parallel: false, !resetCal);
			LCSecondColumn.Width = new GridLength(0.0);
			LCSplitter.Visibility = Visibility.Collapsed;
			if (isCal)
			{
				ListBorder.Visibility = Visibility.Collapsed;
			}
			else
			{
				CalendarBorder.Visibility = Visibility.Collapsed;
			}
			CLGrid.Background = System.Windows.Media.Brushes.Transparent;
			CalendarBorder.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "ShowAreaBottomColor");
			ListBorder.Background = System.Windows.Media.Brushes.Transparent;
			CalendarBorder.Margin = new Thickness(0.0);
			ListBorder.Margin = new Thickness(0.0);
			ClBorder.Visibility = Visibility.Collapsed;
			CalendarBorderClip.RadiusX = 0.0;
			CalendarBorderClip.RadiusY = 0.0;
			CalendarBorder.CornerRadius = new CornerRadius(0.0);
			ListBorderClip.RadiusX = 0.0;
			ListBorderClip.RadiusY = 0.0;
			if (ListView != null)
			{
				ListView.Margin = new Thickness(0.0, 0.0, 0.0, 0.0);
			}
			if (MainCalendar != null)
			{
				MainCalendar.ViewSourceType = CalendarViewSourceType.Main;
				MainCalendar.Margin = new Thickness(0.0, 0.0, 0.0, 0.0);
			}
		}
	}

	private void SetParallelShown()
	{
		if (LocalSettings.Settings.UserPreference?.desktopSplitView == null || LocalSettings.Settings.UserPreference.desktopSplitView.showGuide)
		{
			LocalSettings.SetParallelGuideShow();
		}
	}

	private async void ShowParallelFeatureWindow()
	{
		if (LocalSettings.Settings.UserPreference?.desktopSplitView == null || LocalSettings.Settings.UserPreference.desktopSplitView.showGuide)
		{
			await Task.Delay(200);
			LocalSettings.SetParallelGuideShow();
			UserActCollectUtils.AddClickEvent("timeline", "user_guide", "feature_introduction");
			await FeatureInfoWindow.Show(Utils.GetString("TaskShowWithCalendar"), Utils.GetString("ParallelFeatureDesc"), null, "TaskAndCalendar" + (Utils.IsCn() ? "_cn" : "_en"), Utils.GetString("EnjoyNow"), null, isGif: false);
		}
	}

	private void InitCalendar(CalendarViewSourceType type)
	{
		MainCalendar = new CalendarControl(type);
	}

	public async Task LoadMatrix()
	{
		SetViewVisible(EisenhoverBorder);
		if (MatrixContainer == null)
		{
			MatrixContainer = new MatrixContainer();
		}
		EisenhoverBorder.Child = MatrixContainer;
		SetInterfaceStyle();
		SetMainWindowsMinSize(isList: false);
		await Task.Delay(100);
		MatrixContainer?.LoadTask(restore: false);
	}

	private void SwitchCountDown()
	{
		SetViewVisible(CountDownBorder);
		if (CountDownContainer == null)
		{
			CountDownContainer = new CountDownContainer();
		}
		CountDownBorder.Child = CountDownContainer;
		SetInterfaceStyle();
		SetMainWindowsMinSize(isList: true);
		base.MinWidth = 600.0;
	}

	private void SwitchFocus()
	{
		SetViewVisible(FocusBorder);
		TickFocusManager.InitFocusControl();
		FocusBorder.Child = TickFocusManager.MainFocus;
		SetInterfaceStyle();
		base.MinWidth = 450.0;
		base.MinHeight = 600.0;
	}

	private void SwitchHabit()
	{
		SetViewVisible(HabitBorder);
		if (HabitView == null || HabitBorder.Child != HabitView)
		{
			HabitView = new HabitContainer(isMainHabit: true);
			HabitBorder.Child = HabitView;
		}
		SetInterfaceStyle();
		SetMainWindowsMinSize(isList: true);
		base.MinWidth = 550.0;
		base.MinHeight = 550.0;
		HabitView.Reload();
	}

	private async void SwitchAI()
	{
		SetViewVisible(AIBorder);
		System.Windows.Controls.Panel.SetZIndex(AIBorder, 10);
		SetInterfaceStyle();
		SetMainWindowsMinSize(isList: true);
		await Task.Delay(50);
		LocalAIChatPanel.ShowIn(AIBorder);
	}

	private void SetViewVisible(Border border)
	{
		System.Windows.Controls.Panel.SetZIndex(AIBorder, (border == AIBorder) ? 10 : 0);
		FocusBorder.Visibility = ((FocusBorder != border) ? Visibility.Hidden : Visibility.Visible);
		CalendarBorder.Visibility = ((CalendarBorder != border) ? Visibility.Hidden : Visibility.Visible);
		EisenhoverBorder.Visibility = ((EisenhoverBorder != border) ? Visibility.Hidden : Visibility.Visible);
		ListBorder.Visibility = ((ListBorder != border) ? Visibility.Hidden : Visibility.Visible);
		HabitBorder.Visibility = ((HabitBorder != border) ? Visibility.Hidden : Visibility.Visible);
		CountDownBorder.Visibility = ((CountDownBorder != border) ? Visibility.Hidden : Visibility.Visible);
		AIBorder.Visibility = ((AIBorder != border) ? Visibility.Hidden : Visibility.Visible);
	}

	private async void LoadTaskView(bool isReload)
	{
		SetViewVisible(ListBorder);
		bool flag = true;
		if (ListBorder.Child == null)
		{
			ListView = ListViewContainer.GetListView("MainWindow", ListBorder);
			ListBorder.Child = ListView;
			flag = false;
		}
		UpdateListCalendarDisplay(isCal: false, LocalSettings.Settings.InSearch);
		ListViewContainer listView = ListView;
		bool fromSearch = listView != null && listView.Mode == ListMode.Search;
		if (fromSearch && !LocalSettings.Settings.InSearch)
		{
			ListView?.StopSearch();
		}
		if (!isReload)
		{
			ListView?.HideDetail();
		}
		if (!LocalSettings.Settings.InSearch)
		{
			if (flag && !CalendarBorder.IsVisible)
			{
				await Task.Delay(100);
			}
			OnModuleSwitch?.Invoke(DisplayModule.Task);
			if (isReload)
			{
				ListView?.ReloadView(fromSearch);
				return;
			}
			UpdateLayout();
			await Task.Delay(100);
			ListView?.LoadSavedProject(delay: true);
		}
	}

	private void LoadTaskViewInCalView()
	{
		ListBorder.Visibility = Visibility.Visible;
		if (ListBorder.Child == null)
		{
			ListView = ListViewContainer.GetListView("MainWindow", ListBorder);
			ListBorder.Child = ListView;
		}
		if (ListView.Mode == ListMode.Search && !LocalSettings.Settings.InSearch)
		{
			ListView.StopSearch();
		}
		ListView?.HideDetail();
	}

	public void OnProjectWidthChanged(double width)
	{
		LocalSettings.Settings.ProjectPanelWidth = width;
	}

	public void OnDetailWidthChanged(double width)
	{
		LocalSettings.Settings.DetailListDivide = width;
	}

	public void SaveSelectedProject(string saveProjectId)
	{
		if (!ShowCalendar)
		{
			LocalSettings.Settings.SelectProjectId = saveProjectId;
		}
		else
		{
			LocalSettings.Settings.ExtraSettings.ProjectParallelSelectProject = saveProjectId;
		}
	}

	public double GetProjectWidth()
	{
		return LocalSettings.Settings.ProjectPanelWidth;
	}

	public double GetDetailWidth()
	{
		return LocalSettings.Settings.DetailListDivide;
	}

	public string GetSelectedProject()
	{
		if (!ShowList)
		{
			return LocalSettings.Settings.ExtraSettings.ProjectParallelSelectProject;
		}
		return LocalSettings.Settings.SelectProjectId;
	}

	public void SetMinSize(int width, int height)
	{
		base.MinWidth = width;
		base.MinHeight = height;
	}

	public double GetListContainerWidth()
	{
		return ListBorder.ActualWidth;
	}

	public static void ShowCalendarInTaskChanged()
	{
		ListCalendarSettings listCalendarSettings;
		try
		{
			listCalendarSettings = JsonConvert.DeserializeObject<ListCalendarSettings>(LocalSettings.Settings.ExtraSettings.ListCalendarSettings) ?? new ListCalendarSettings();
		}
		catch (Exception)
		{
			listCalendarSettings = new ListCalendarSettings();
		}
		listCalendarSettings.ShowCInL = !listCalendarSettings.ShowCInL;
		LocalSettings.Settings.ExtraSettings.ListCalendarSettings = JsonConvert.SerializeObject(listCalendarSettings);
		if (listCalendarSettings.ShowCInL)
		{
			MainWindowManager.Window.ShowParallelFeatureWindow();
		}
		if (LocalSettings.Settings.MainWindowDisplayModule == 0)
		{
			MainWindowManager.Window.UpdateListCalendarDisplay(isCal: false, inSearch: false, addEvent: false);
		}
	}

	public static void ShowTaskInCalendarChanged()
	{
		ListCalendarSettings listCalendarSettings;
		try
		{
			listCalendarSettings = JsonConvert.DeserializeObject<ListCalendarSettings>(LocalSettings.Settings.ExtraSettings.ListCalendarSettings) ?? new ListCalendarSettings();
		}
		catch (Exception)
		{
			listCalendarSettings = new ListCalendarSettings();
		}
		listCalendarSettings.ShowLInC = !listCalendarSettings.ShowLInC;
		LocalSettings.Settings.ExtraSettings.ListCalendarSettings = JsonConvert.SerializeObject(listCalendarSettings);
		if (LocalSettings.Settings.MainWindowDisplayModule == 1)
		{
			MainWindowManager.Window.UpdateListCalendarDisplay(isCal: true, inSearch: false, addEvent: false);
		}
	}

	private void OnLCSplitDragComplete(object sender, DragCompletedEventArgs e)
	{
		Border border = ((!ShowList) ? ListBorder : CalendarBorder);
		ScaleTransform obj = ((!ShowList) ? ListTransform : CalendarTransform);
		border.Opacity = 1.0;
		border.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
		obj.ScaleX = 1.0;
		obj.ScaleY = 1.0;
		ListCalendarSettings listCalendarSettings;
		try
		{
			listCalendarSettings = JsonConvert.DeserializeObject<ListCalendarSettings>(LocalSettings.Settings.ExtraSettings.ListCalendarSettings);
		}
		catch (Exception)
		{
			listCalendarSettings = new ListCalendarSettings();
		}
		if (LocalSettings.Settings.MainWindowDisplayModule == 0)
		{
			if (LCSecondColumn.ActualWidth < 260.0)
			{
				listCalendarSettings.ShowCInL = false;
				UserActCollectUtils.AddClickEvent("split_view", "task+calendar", "close_from_om");
				ShowCalendarInTaskChanged();
			}
			else if (LCSecondColumn.ActualWidth < 320.0)
			{
				double actualWidth = LCFirstColumn.ActualWidth;
				double actualWidth2 = LCSecondColumn.ActualWidth;
				actualWidth = actualWidth + actualWidth2 - 320.0;
				listCalendarSettings.LC_Divide = 320.0 / actualWidth;
				LCFirstColumn.Width = new GridLength(1.0, GridUnitType.Star);
				LCSecondColumn.Width = new GridLength(listCalendarSettings.LC_Divide, GridUnitType.Star);
			}
			else
			{
				listCalendarSettings.LC_Divide = LCSecondColumn.ActualWidth / LCFirstColumn.ActualWidth;
				LCFirstColumn.Width = new GridLength(1.0, GridUnitType.Star);
				LCSecondColumn.Width = new GridLength(listCalendarSettings.LC_Divide, GridUnitType.Star);
			}
		}
		else if (LCSecondColumn.ActualWidth < 260.0)
		{
			listCalendarSettings.ShowLInC = false;
			UserActCollectUtils.AddClickEvent("split_view", "calendar+task", "close_from_om");
			ShowTaskInCalendarChanged();
		}
		else if (LCSecondColumn.ActualWidth < 320.0)
		{
			double actualWidth3 = LCFirstColumn.ActualWidth;
			double actualWidth4 = LCSecondColumn.ActualWidth;
			actualWidth3 = actualWidth3 + actualWidth4 - 320.0;
			listCalendarSettings.CL_Divide = 320.0 / actualWidth3;
			LCFirstColumn.Width = new GridLength(1.0, GridUnitType.Star);
			LCSecondColumn.Width = new GridLength(listCalendarSettings.CL_Divide, GridUnitType.Star);
		}
		else
		{
			listCalendarSettings.CL_Divide = LCSecondColumn.ActualWidth / LCFirstColumn.ActualWidth;
			LCFirstColumn.Width = new GridLength(1.0, GridUnitType.Star);
			LCSecondColumn.Width = new GridLength(listCalendarSettings.CL_Divide, GridUnitType.Star);
		}
		LocalSettings.Settings.ExtraSettings.ListCalendarSettings = JsonConvert.SerializeObject(listCalendarSettings);
	}

	private void OnCalendarBorderSizeChanged(object sender, SizeChangedEventArgs e)
	{
		CalendarBorderClip.Rect = new Rect(0.0, 0.0, CalendarBorder.ActualWidth, CalendarBorder.ActualHeight);
	}

	private void OnListBorderSizeChanged(object sender, SizeChangedEventArgs e)
	{
		ListBorderClip.Rect = new Rect(0.0, 0.0, ListBorder.ActualWidth, ListBorder.ActualHeight);
	}

	private void OnFocusBorderSizeChanged(object sender, SizeChangedEventArgs e)
	{
		FocusBorderClip.Rect = new Rect(0.0, 0.0, FocusBorder.ActualWidth, FocusBorder.ActualHeight);
	}

	private void OnMatrixBorderSizeChanged(object sender, SizeChangedEventArgs e)
	{
		MatrixBorderClip.Rect = new Rect(0.0, 0.0, EisenhoverBorder.ActualWidth, EisenhoverBorder.ActualHeight);
	}

	private void OnHabitBorderSizeChanged(object sender, SizeChangedEventArgs e)
	{
		HabitBorderClip.Rect = new Rect(0.0, 0.0, HabitBorder.ActualWidth, HabitBorder.ActualHeight);
	}

	private void OnCountDownBorderSizeChanged(object sender, SizeChangedEventArgs e)
	{
		CountDownBorderClip.Rect = new Rect(0.0, 0.0, CountDownBorder.ActualWidth, CountDownBorder.ActualHeight);
	}

	private void OnAIBorderSizeChanged(object sender, SizeChangedEventArgs e)
	{
		AIBorderClip.Rect = new Rect(0.0, 0.0, AIBorder.ActualWidth, AIBorder.ActualHeight);
	}

	public void ChangeHeaderButtonColor()
	{
		switch (LocalSettings.Settings.ExtraSettings.InterfaceStyle)
		{
		case InterfaceStyle.Flat:
		{
			string themeId = LocalSettings.Settings.ThemeId;
			if (!ThemeKey.IsColorTheme(themeId) && !ThemeKey.IsDarkTheme(themeId) && !ThemeKey.IsLightColorImageTheme(themeId) && ListBorder.IsVisible && CalendarBorder.IsVisible)
			{
				MaxPath.Fill = System.Windows.Media.Brushes.White;
				MinPath.Fill = System.Windows.Media.Brushes.White;
				X.Fill = System.Windows.Media.Brushes.White;
				NormalPath.Fill = System.Windows.Media.Brushes.White;
				NormalButton.SetResourceReference(FrameworkElement.StyleProperty, "normalButtonWhiteStyle");
				MaxButton.SetResourceReference(FrameworkElement.StyleProperty, "normalButtonWhiteStyle");
				MinButton.SetResourceReference(FrameworkElement.StyleProperty, "normalButtonWhiteStyle");
				CloseButton.SetResourceReference(FrameworkElement.StyleProperty, "normalButtonWhiteStyle");
			}
			else
			{
				MaxPath.SetResourceReference(Shape.FillProperty, "BaseColorOpacity100");
				MinPath.SetResourceReference(Shape.FillProperty, "BaseColorOpacity100");
				X.SetResourceReference(Shape.FillProperty, "BaseColorOpacity100");
				NormalPath.SetResourceReference(Shape.FillProperty, "BaseColorOpacity100");
				NormalButton.SetResourceReference(FrameworkElement.StyleProperty, "normalButtonStyle");
				MaxButton.SetResourceReference(FrameworkElement.StyleProperty, "normalButtonStyle");
				MinButton.SetResourceReference(FrameworkElement.StyleProperty, "normalButtonStyle");
				CloseButton.SetResourceReference(FrameworkElement.StyleProperty, "normalButtonStyle");
			}
			break;
		}
		case InterfaceStyle.Card:
			MaxPath.SetResourceReference(Shape.FillProperty, "SystemButtonBrush");
			MinPath.SetResourceReference(Shape.FillProperty, "SystemButtonBrush");
			X.SetResourceReference(Shape.FillProperty, "SystemButtonBrush");
			NormalPath.SetResourceReference(Shape.FillProperty, "SystemButtonBrush");
			break;
		}
	}

	public void ShowCalendarProView(string current = null, bool autoSwitch = true)
	{
		if (CalendarBorder.IsVisible)
		{
			MainCalendar.ShowProToast();
			CloseVirtualParallel();
			calendarProView = new CalendarProView(current, autoSwitch)
			{
				Margin = new Thickness(50.0, 0.0, 0.0, 0.0)
			};
			BottomContent.Content = calendarProView;
			calendarProView.ItemSelected += delegate(object sender, string s)
			{
				SwitchTo(s);
			};
			if (!string.IsNullOrEmpty(current))
			{
				SwitchTo(current);
			}
		}
		void SwitchTo(string s1)
		{
			MainCalendar.SwitchVirtualView(s1);
		}
	}

	public void TryStopCalendarProTimer()
	{
		if (calendarProView != null)
		{
			calendarProView.Titles.StopTimer();
		}
	}

	private void CloseVirtualParallel()
	{
		if (ListBorder.IsVisible)
		{
			ListView?.SetParallelShow(showWithCal: false, inCal: false);
			MainCalendar?.SetParallelShow(parallel: false, inTask: false);
			LCSecondColumn.Width = new GridLength(0.0);
			LCSplitter.Visibility = Visibility.Collapsed;
			ListBorder.Visibility = Visibility.Collapsed;
			CLGrid.Background = System.Windows.Media.Brushes.Transparent;
			CalendarBorder.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "ShowAreaBottomColor");
			ListBorder.Background = System.Windows.Media.Brushes.Transparent;
			CalendarBorder.Margin = new Thickness(0.0);
			ListBorder.Margin = new Thickness(0.0);
			ClBorder.Visibility = Visibility.Collapsed;
			CalendarBorderClip.RadiusX = 0.0;
			CalendarBorderClip.RadiusY = 0.0;
			CalendarBorder.CornerRadius = new CornerRadius(0.0);
			ListBorderClip.RadiusX = 0.0;
			ListBorderClip.RadiusY = 0.0;
			if (ListView != null)
			{
				ListView.Margin = new Thickness(0.0, 0.0, 0.0, 0.0);
			}
			if (MainCalendar != null)
			{
				MainCalendar.ViewSourceType = CalendarViewSourceType.Main;
				MainCalendar.Margin = new Thickness(0.0, 0.0, 0.0, 0.0);
			}
		}
	}

	private void ShowVirtualParallel(bool showContent)
	{
		CalendarBorder.SetValue(Grid.ColumnProperty, 0);
		CalendarBorder.SetValue(Grid.ColumnSpanProperty, 1);
		ListBorder.SetValue(Grid.ColumnProperty, 1);
		ListBorder.SetValue(Grid.ColumnSpanProperty, 1);
		LoadTaskViewInCalView();
		LCSplitter.Visibility = Visibility.Visible;
		CLGrid.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "ParallelBackground");
		CalendarBorder.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "ParallelPartBackground");
		ListBorder.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "ParallelPartBackground");
		CalendarBorder.Margin = new Thickness(10.0, 28.0, 0.0, 10.0);
		ListBorder.Margin = new Thickness(10.0, 28.0, 10.0, 10.0);
		ClBorder.Visibility = Visibility.Visible;
		CalendarBorderClip.RadiusX = 12.0;
		CalendarBorderClip.RadiusY = 12.0;
		CalendarBorder.CornerRadius = new CornerRadius(12.0);
		ListBorderClip.RadiusX = 12.0;
		ListBorderClip.RadiusY = 12.0;
		ListView.Margin = new Thickness(0.0, -12.0, 0.0, 0.0);
		ListView.SetParallelShow(showWithCal: true, inCal: true);
		MainCalendar.Margin = new Thickness(0.0, -12.0, 0.0, 0.0);
		MainCalendar.SetParallelShow(parallel: true, inTask: false);
		ListView?.LoadVirtualData(showContent);
		LCSplitter.IsHitTestVisible = false;
		base.MinWidth = 680.0;
		double num = 0.4;
		double num2 = Math.Max(50.0, ColumnOneDefine.ActualWidth);
		double num3 = (base.ActualWidth - num2) * num / (1.0 + num);
		if (num3 < 320.0)
		{
			num3 = 320.0;
		}
		double num4 = base.ActualWidth - num2 - num3;
		if (num4 < 310.0)
		{
			num4 = 310.0;
		}
		LCFirstColumn.Width = new GridLength(1.0, GridUnitType.Star);
		LCSecondColumn.Width = new GridLength(num3 / num4, GridUnitType.Star);
	}

	public void GetVirtualData()
	{
	}

	public void OnCalendarProModelSwitch(CarouselItemViewModel model)
	{
		if (CalendarBorder.IsVisible && !UserDao.IsPro())
		{
			CloseVirtualParallel();
			MainCalendar.SwitchVirtualView(model.Value);
		}
	}

	public void InputCommand()
	{
		KeyBindingCommand.InputCommand(this, ListView?.GetSelectedProject());
	}

	public void CloseCommand()
	{
		OnCloseButtonClick(null, null);
	}

	public void ExitImmersiveCommand()
	{
		ExitImmersiveMode();
	}

	public void JumpCalendar()
	{
		if (LocalSettings.Settings.MainWindowDisplayModule != 1)
		{
			SwitchModule(DisplayModule.Calendar);
		}
	}

	public void JumpSmartProject(SmartListType smartListType)
	{
		SwitchModule(DisplayModule.Task);
		ListViewContainer listView = ListView;
		if (listView != null && listView.Mode == ListMode.Search)
		{
			StopSearch();
		}
		ListView?.ProjectList?.TrySelectSmartProject(smartListType);
	}

	public void JumpHabit()
	{
		if (LocalSettings.Settings.ShowHabit && LocalSettings.Settings.MainWindowDisplayModule != 3)
		{
			SwitchModule(DisplayModule.Habit);
		}
	}

	public void ToggleParallelView()
	{
		if (!LocalSettings.Settings.InSearch && (ShowList || ShowCalendar))
		{
			ListCalendarSettings listCalendarSettings2;
			try
			{
				string listCalendarSettings = LocalSettings.Settings.ExtraSettings.ListCalendarSettings;
				listCalendarSettings2 = (string.IsNullOrWhiteSpace(listCalendarSettings) ? new ListCalendarSettings() : (JsonConvert.DeserializeObject<ListCalendarSettings>(listCalendarSettings) ?? new ListCalendarSettings()));
			}
			catch (JsonException)
			{
				listCalendarSettings2 = new ListCalendarSettings();
			}
			if (ShowList && (listCalendarSettings2.ShowCInL || ProChecker.CheckPro(ProType.ParallelShowInTask)))
			{
				ShowCalendarInTaskChanged();
			}
			else if (ShowCalendar && (listCalendarSettings2.ShowLInC || ProChecker.CheckPro(ProType.ParallelShowInCalendar)))
			{
				ShowTaskInCalendarChanged();
			}
		}
	}

	public void ToggleAIFloatingWindow()
	{
		if (LocalSettings.Settings.EnableAI && !_aiClientHostState.IsTemporarilySuppressed && _aiClientHostState.Presentation != AIClientPresentation.FullScreen)
		{
			UserActCollectUtils.AddShortCutEvent("AI", "toggle_floating_window");
			if (_aiClientHostState.Presentation == AIClientPresentation.Floating)
			{
				OnAIFloatingWindowCollapseRequested(_aiFloatingWindow, EventArgs.Empty);
				return;
			}
			_aiClientHostState.RestoreFloatingEntry();
			OnAIFloatingEntryClick(null, null);
		}
	}

	public void JumpTab(int index)
	{
		if (index >= 1 && index <= 9)
		{
			UserActCollectUtils.AddShortCutEvent("navigate", "switch_the_tab_bar");
			List<TabBarModel> navigationTabBars = TabBarModel.GetNavigationTabBars();
			if (navigationTabBars.Count >= index && Enum.TryParse<DisplayModule>(TabBarModel.GetModuleName(navigationTabBars[index - 1].name), ignoreCase: true, out var result))
			{
				SwitchModule(result);
			}
		}
	}

	public void SearchCommand()
	{
		StartSearch();
	}

	public void SearchOperationCommand()
	{
		UserActCollectUtils.AddShortCutEvent("general", "open_command_menu");
		ShowSearchOperation();
	}

	public void Print(bool isDetail = false)
	{
		ListView?.TryPrint(isDetail);
	}

	private ITaskOperation GetFocusedTaskItem()
	{
		IInputElement focusedElement = FocusManager.GetFocusedElement(this);
		if (!(focusedElement is ITaskOperation result))
		{
			if (focusedElement is TextArea child)
			{
				ITaskOperation taskOperation = Utils.FindParent<ITaskOperation>(child);
				if (taskOperation != null)
				{
					return taskOperation;
				}
			}
			return null;
		}
		return result;
	}

	public void BatchSetPriorityCommand(int priority)
	{
		ListView?.TryBatchSetPriority(priority);
		if (MatrixContainer != null && MatrixContainer.IsVisible)
		{
			MatrixContainer.TryBatchSetPriority(priority);
		}
	}

	public void BatchOpenStickyCommand()
	{
		ListView?.BatchOpenSticky();
		if (MatrixContainer != null && MatrixContainer.IsVisible)
		{
			MatrixContainer.BatchOpenSticky();
		}
	}

	public void BatchSetDateCommand(DateTime? date)
	{
		ListView?.TryBatchSetDate(date);
		if (MatrixContainer != null && MatrixContainer.IsVisible)
		{
			MatrixContainer.TryBatchSetDate(date);
		}
	}

	public void BatchPinTaskCommand()
	{
		ListView?.BatchPinTask();
		if (MatrixContainer != null && MatrixContainer.IsVisible)
		{
			MatrixContainer.BatchPinTask();
		}
	}

	public void SelectDate(bool relative)
	{
		GetFocusedTaskItem()?.SelectDate(relative);
	}

	public void ToggleTaskCompleted()
	{
		ListView?.GetTaskView()?.GetTaskListView()?.ToggleSelectedTaskCompleted();
	}

	public void BatchDeleteCommand()
	{
		ListView?.BatchDeleteTask();
		if (MatrixContainer != null && MatrixContainer.IsVisible)
		{
			MatrixContainer.BatchDeleteTask();
		}
	}

	public void TaskDelete()
	{
		ITaskOperation focusedTaskItem = GetFocusedTaskItem();
		if (focusedTaskItem != null)
		{
			focusedTaskItem.Delete();
		}
		else
		{
			BatchDeleteCommand();
		}
	}

	private void InitShortcut()
	{
		KeyBindingManager.TryAddKeyBinding("SyncTask", GetKeyBinding(MainWindowCommands.SyncCommand));
		KeyBindingManager.TryAddKeyBinding("SearchTask", GetKeyBinding(MainWindowCommands.SearchCommand));
		KeyBindingManager.TryAddKeyBinding("AddTask", GetKeyBinding(MainWindowCommands.InputCommand));
		KeyBindingManager.TryAddKeyBinding("JumpAll", GetKeyBinding(MainWindowCommands.JumpAllList));
		KeyBindingManager.TryAddKeyBinding("JumpToday", GetKeyBinding(MainWindowCommands.JumpTodayList));
		KeyBindingManager.TryAddKeyBinding("JumpTomorrow", GetKeyBinding(MainWindowCommands.JumpTomorrowList));
		KeyBindingManager.TryAddKeyBinding("JumpWeek", GetKeyBinding(MainWindowCommands.JumpWeekList));
		KeyBindingManager.TryAddKeyBinding("JumpAssign", GetKeyBinding(MainWindowCommands.JumpAssignMeList));
		KeyBindingManager.TryAddKeyBinding("JumpInbox", GetKeyBinding(MainWindowCommands.JumpInboxList));
		KeyBindingManager.TryAddKeyBinding("JumpComplete", GetKeyBinding(MainWindowCommands.JumpCompleteList));
		KeyBindingManager.TryAddKeyBinding("JumpTrash", GetKeyBinding(MainWindowCommands.JumpTrashList));
		KeyBindingManager.TryAddKeyBinding("JumpSummary", GetKeyBinding(MainWindowCommands.JumpSummary));
		KeyBindingManager.TryAddKeyBinding("Print", GetKeyBinding(MainWindowCommands.PrintCommand));
		KeyBindingManager.TryAddKeyBinding("PrintDetail", GetKeyBinding(MainWindowCommands.PrintDetailCommand));
		KeyBindingManager.TryAddKeyBinding("SetNoPriority", GetKeyBinding(MainWindowCommands.SetPriorityNoneCommand));
		KeyBindingManager.TryAddKeyBinding("SetLowPriority", GetKeyBinding(MainWindowCommands.SetPriorityLowCommand));
		KeyBindingManager.TryAddKeyBinding("SetMediumPriority", GetKeyBinding(MainWindowCommands.SetPriorityMediumCommand));
		KeyBindingManager.TryAddKeyBinding("SetHighPriority", GetKeyBinding(MainWindowCommands.SetPriorityHighCommand));
		KeyBindingManager.TryAddKeyBinding("ClearDate", GetKeyBinding(MainWindowCommands.ClearDateCommand));
		KeyBindingManager.TryAddKeyBinding("SetToday", GetKeyBinding(MainWindowCommands.SetTodayCommand));
		KeyBindingManager.TryAddKeyBinding("SetTomorrow", GetKeyBinding(MainWindowCommands.SetTomorrowCommand));
		KeyBindingManager.TryAddKeyBinding("SetNextWeek", GetKeyBinding(MainWindowCommands.SetNextWeekCommand));
		KeyBindingManager.TryAddKeyBinding("SetDate", GetKeyBinding(MainWindowCommands.SelectDateCommand));
		KeyBindingManager.TryAddKeyBinding("CompleteTask", GetKeyBinding(MainWindowCommands.CompleteCommand));
		KeyBindingManager.TryAddKeyBinding("DeleteTask", GetKeyBinding(MainWindowCommands.DeleteCommand));
		KeyBindingManager.TryAddKeyBinding("ExpandAllTask", GetKeyBinding(MainWindowCommands.ExpandAllTaskCommand));
		KeyBindingManager.TryAddKeyBinding("ExpandAllSection", GetKeyBinding(MainWindowCommands.ExpandAllSectionCommand));
		KeyBindingManager.TryAddKeyBinding("PinTask", GetKeyBinding(MainWindowCommands.PinTaskCommand));
		KeyBindingManager.TryAddKeyBinding("JumpAbandon", GetKeyBinding(MainWindowCommands.JumpAbandonList));
		KeyBindingManager.TryAddKeyBinding("OpenSetting", GetKeyBinding(MainWindowCommands.OpenSettingCommand));
		KeyBindingManager.TryAddKeyBinding("JumpCalendar", GetKeyBinding(MainWindowCommands.JumpCalendar));
		KeyBindingManager.TryAddKeyBinding("JumpHabit", GetKeyBinding(MainWindowCommands.JumpHabitList));
		KeyBindingManager.TryAddKeyBinding("ToggleParallelView", GetKeyBinding(MainWindowCommands.ToggleParallelView));
		KeyBindingManager.TryAddKeyBinding("AINewConversation", GetKeyBinding(MainWindowCommands.AINewConversation));
		KeyBindingManager.TryAddKeyBinding("AIToggleFloatingWindow", GetKeyBinding(MainWindowCommands.AIToggleFloatingWindow));
		KeyBindingManager.TryAddKeyBinding("OpenSticky", GetKeyBinding(MainWindowCommands.OpenStickyCommand));
		KeyBindingManager.TryAddKeyBinding("OmListView", GetKeyBinding(MainWindowCommands.ListViewCommand));
		KeyBindingManager.TryAddKeyBinding("OmKanbanView", GetKeyBinding(MainWindowCommands.KanbanViewCommand));
		KeyBindingManager.TryAddKeyBinding("OmTimelineView", GetKeyBinding(MainWindowCommands.TimelineViewCommand));
		bool flag = KeyBindingManager.HasCtrlNumKeyBinding();
		for (int i = 1; i <= 9; i++)
		{
			if (!flag)
			{
				LocalSettings.Settings.ShortCutModel.SetPropertyValue($"Tab0{i}", "Ctrl+D" + i);
			}
			KeyBindingManager.TryAddKeyBinding($"Tab0{i}", GetKeyBinding(GetCommand($"Tab0{i}Command")));
		}
		static ICommand GetCommand(string commandName)
		{
			return typeof(MainWindowCommands).GetField(commandName).GetValue(null) as ICommand;
		}
		KeyBinding GetKeyBinding(ICommand command)
		{
			if (command != null)
			{
				KeyBinding keyBinding = new KeyBinding(command, new KeyGesture(Key.None))
				{
					CommandParameter = this
				};
				base.InputBindings.Add(keyBinding);
				return keyBinding;
			}
			return null;
		}
	}

	public bool TryTabListAndDetail()
	{
		IInputElement focusedElement = FocusManager.GetFocusedElement(this);
		if (focusedElement == null || focusedElement.Equals(this))
		{
			return false;
		}
		return ListView?.TabListAndDetail(focusedElement) ?? false;
	}

	public void ShowOrHideWindow()
	{
		if (LocalSettings.Settings["LoginUserId"].ToString() != "")
		{
			if (base.IsActive && base.Visibility == Visibility.Visible)
			{
				HideWindow();
			}
			else
			{
				TryShowMainWindow();
			}
		}
	}

	public void TryUndo()
	{
		if (ToastGrid.Children.Count > 0 && ToastGrid.Children[0] is UndoToast { Visibility: Visibility.Visible } undoToast)
		{
			undoToast.OnUndo();
		}
	}

	public async void ExpandOrFoldAllTask(bool isOpen)
	{
		ListView?.ExpandOrFoldAllTask(isOpen);
		MatrixContainer?.ExpandOrFoldAllTask();
	}

	public async void ExpandOrFoldAllSection()
	{
		ListView?.ExpandOrFoldAllSection();
		MatrixContainer?.ExpandOrFoldAllSection();
	}

	private void DoOperationOnKeyUp(Key e)
	{
		if (e == Key.Oem2 && Utils.IfShiftPressed())
		{
			ShowShortCutPanel();
			return;
		}
		if (_viewDown && e >= Key.A && e <= Key.Z)
		{
			DoViewAction(e);
		}
		_viewDown = e == Key.V && !_goDown;
		if (_goDown && e >= Key.A && e <= Key.Z && !Utils.IfShiftPressed() && !Utils.IfCtrlPressed())
		{
			DoGoAction(e);
		}
		_goDown = e == Key.G && !_goDown && !Utils.IfShiftPressed() && !Utils.IfCtrlPressed();
	}

	private void DoViewAction(Key key)
	{
		if (LocalSettings.Settings.MainWindowDisplayModule == 0)
		{
			ProjectIdentity projectIdentity = ListView?.GetSelectedProject();
			string text = string.Empty;
			switch (key)
			{
			case Key.L:
				UserActCollectUtils.AddShortCutEvent("switch_views", "list_view");
				text = "list";
				break;
			case Key.K:
				UserActCollectUtils.AddShortCutEvent("switch_views", "kanban_view");
				text = "kanban";
				break;
			case Key.T:
				UserActCollectUtils.AddShortCutEvent("switch_views", "timeline_view");
				text = "timeline";
				break;
			}
			if (!string.IsNullOrEmpty(text))
			{
				projectIdentity.SwitchViewMode(text, ShowCalendar);
			}
		}
	}

	private void DoGoAction(Key key)
	{
		switch (key)
		{
		case Key.S:
			UserActCollectUtils.AddShortCutEvent("navigate", "go_to_settings");
			LeftTabBar.ShowSettingDialog();
			break;
		case Key.A:
			UserActCollectUtils.AddShortCutEvent("navigate", "go_to_all");
			JumpSmartProject(SmartListType.All);
			break;
		case Key.T:
			UserActCollectUtils.AddShortCutEvent("navigate", "go_to_tod");
			JumpSmartProject(SmartListType.Today);
			break;
		case Key.R:
			UserActCollectUtils.AddShortCutEvent("navigate", "go_to_tom");
			JumpSmartProject(SmartListType.Tomorrow);
			break;
		case Key.N:
			UserActCollectUtils.AddShortCutEvent("navigate", "go_to_7D");
			JumpSmartProject(SmartListType.Week);
			break;
		case Key.M:
			UserActCollectUtils.AddShortCutEvent("navigate", "go_to_assign");
			JumpSmartProject(SmartListType.Assign);
			break;
		case Key.I:
			UserActCollectUtils.AddShortCutEvent("navigate", "go_to_inbox");
			JumpSmartProject(SmartListType.Inbox);
			break;
		case Key.C:
			UserActCollectUtils.AddShortCutEvent("navigate", "go_to_completed");
			JumpSmartProject(SmartListType.Completed);
			break;
		case Key.W:
			UserActCollectUtils.AddShortCutEvent("navigate", "go_to_wont_do");
			JumpSmartProject(SmartListType.Abandoned);
			break;
		case Key.G:
			UserActCollectUtils.AddShortCutEvent("navigate", "go_to_trash");
			JumpSmartProject(SmartListType.Trash);
			break;
		case Key.B:
			UserActCollectUtils.AddShortCutEvent("navigate", "go_to_summary");
			JumpSmartProject(SmartListType.Summary);
			break;
		}
	}

	public void OnTabKeyUp()
	{
		if (!MaskPanel.IsVisible && ListView != null && ListView.IsVisible)
		{
			if (!TryTabListAndDetail())
			{
				ListView.TabSelectQuickAddItem();
			}
			else
			{
				ListView.TabKanbanSelect();
			}
		}
	}

	public void SwitchViewMode(string mode)
	{
		ListView?.GetSelectedProject()?.SwitchViewMode(mode, ShowCalendar);
	}

	private void OnLCSplitDragging(object sender, DragDeltaEventArgs e)
	{
		Border obj = ((!ShowList) ? ListBorder : CalendarBorder);
		ScaleTransform scaleTransform = ((!ShowList) ? ListTransform : CalendarTransform);
		double actualWidth = LCSecondColumn.ActualWidth;
		obj.HorizontalAlignment = ((actualWidth > 244.0) ? System.Windows.HorizontalAlignment.Stretch : System.Windows.HorizontalAlignment.Right);
		obj.Opacity = ((actualWidth < 260.0) ? (actualWidth / 500.0) : 1.0);
		scaleTransform.ScaleX = ((actualWidth < 260.0) ? (1.0 - (1.0 - actualWidth / 260.0) / 26.0) : 1.0);
		scaleTransform.ScaleY = ((actualWidth < 260.0) ? (1.0 - (1.0 - actualWidth / 260.0) / 26.0) : 1.0);
	}

	private void OnClSplitDragOver(object sender, System.Windows.Input.MouseEventArgs e)
	{
		ListViewContainer listView = ListView;
		if (listView == null || TaskDetailPopup.IsDragOver(e) || !LCSplitter.IsVisible || !base.IsMouseCaptured)
		{
			return;
		}
		System.Windows.Point position = e.GetPosition(LCSplitter);
		if (position.X < 0.0 || position.X > LCSplitter.ActualWidth || position.Y < 0.0 || position.Y > LCSplitter.ActualHeight)
		{
			return;
		}
		double itemWidth = listView.GetItemWidth();
		object dragData = TickDragHelper.GetDragData();
		if (!(dragData is DisplayItemModel))
		{
			if (!(dragData is TimelineCellViewModel timelineCellViewModel))
			{
				if (dragData is CalendarDisplayViewModel && ListView != null)
				{
					string displayMode = ListView.GetTaskView().GetDisplayMode();
					if (displayMode == "list")
					{
						TickDragHelper.ChangeDragContent(0, itemWidth);
					}
					if (displayMode == "kanban")
					{
						TickDragHelper.ChangeDragContent(1, itemWidth);
					}
				}
			}
			else
			{
				timelineCellViewModel.SetWidth();
				TickDragHelper.ChangeDragContent(timelineCellViewModel, timelineCellViewModel.Width);
			}
		}
		else
		{
			TickDragHelper.ChangeDragContent(dragData, itemWidth);
		}
	}

	private void OnMainWindowSizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (_showed)
		{
			NotifyWindowSizeChanged(e);
		}
		UpdateCLGridSize(e.NewSize.Width < e.PreviousSize.Width);
	}

	private void UpdateCLGridSize(bool reduceWidth = false)
	{
		double num = Math.Max(50.0, ColumnOneDefine.Width.Value);
		CLGrid.Width = Math.Max(310.0, Container.ActualWidth - num);
		if (!(CLGrid.IsVisible && reduceWidth) || !CalendarBorder.IsVisible || !ListBorder.IsVisible)
		{
			return;
		}
		double num2 = (base.ActualWidth - num) * LCFirstColumn.Width.Value / (LCFirstColumn.Width.Value + LCSecondColumn.Width.Value);
		double num3 = num2 * LCSecondColumn.Width.Value / LCFirstColumn.Width.Value;
		if (num2 > 0.0 && num3 > 0.0)
		{
			if (num3 <= 320.0)
			{
				num3 = 320.0;
				num2 = base.ActualWidth - 320.0 - num;
			}
			LCSecondColumn.Width = new GridLength(num3 / num2, GridUnitType.Star);
			LCFirstColumn.Width = new GridLength(1.0, GridUnitType.Star);
		}
	}

	public void InitRestoreLocation()
	{
		RestoreWindowSize();
		RestoreLastPosition();
	}

	private void RestoreWindowSize()
	{
		base.SizeChanged -= OnMainWindowSizeChanged;
		base.SizeChanged += OnMainWindowSizeChanged;
		if (LocalSettings.Settings.Maxmized)
		{
			base.WindowState = WindowState.Maximized;
			DelaySetSize();
		}
		else
		{
			SafeSetWindowSize();
		}
	}

	private void SafeSetWindowSize(bool refresh = false, bool delayRefresh = false)
	{
		if (!_isSpliteDragging)
		{
			double mainWindowWidth = LocalSettings.Settings.MainWindowWidth;
			double mainWindowHeight = LocalSettings.Settings.MainWindowHeight;
			try
			{
				base.Width = mainWindowWidth;
				base.Height = mainWindowHeight;
			}
			catch (Exception)
			{
				base.Width = 1020.0;
				base.Height = 680.0;
			}
			if (refresh)
			{
				RefreshUi(delayRefresh ? 1000 : 0);
			}
		}
	}

	private async void RefreshUi(int delayRefresh = 0)
	{
		if (delayRefresh > 0)
		{
			await Task.Delay(delayRefresh);
		}
		InvalidateVisual();
		InvalidateArrange();
		UpdateLayout();
	}

	private async Task RestoreLastPosition()
	{
		double windowLeft = LocalSettings.Settings.WindowLeft;
		double windowTop = LocalSettings.Settings.WindowTop;
		base.Left = windowLeft;
		base.Top = windowTop;
		if (windowLeft > SystemParameters.PrimaryScreenWidth || windowLeft < -1.0 * LocalSettings.Settings.MainWindowWidth / 2.0 || windowTop < -1.0 * LocalSettings.Settings.MainWindowHeight / 2.0 || windowTop > SystemParameters.PrimaryScreenHeight)
		{
			WindowHelper.MoveTo(this, (int)windowLeft, (int)windowTop);
			base.Left = windowLeft;
			base.Top = windowTop;
		}
		await Task.Delay(2000);
		EnsureWindowVisible(new System.Windows.Point(100.0, 100.0));
	}

	private void NotifyWindowSizeChanged(SizeChangedEventArgs e)
	{
		if (base.WindowState != WindowState.Maximized)
		{
			DelaySaveLocation();
		}
	}

	private void DelaySaveLocation()
	{
		DelayActionHandlerCenter.TryDoAction("MainWindowSaveLocation", SaveLocation);
	}

	private bool EnsureWindowVisible(System.Windows.Point? defaultPoint = null)
	{
		CompositionTarget compositionTarget = PresentationSource.FromVisual(this)?.CompositionTarget;
		if (compositionTarget == null)
		{
			return false;
		}
		double left = base.Left;
		double top = base.Top;
		Matrix transformToDevice = compositionTarget.TransformToDevice;
		System.Windows.Point point = transformToDevice.Transform(new System.Windows.Point(left, top));
		System.Windows.Point point2 = transformToDevice.Transform(new System.Windows.Point(left + base.Width, top + base.Height));
		Rect rect = new Rect(point, point2);
		Screen[] allScreens = Screen.AllScreens;
		foreach (Screen screen in allScreens)
		{
			Rect rect2 = new Rect(screen.Bounds.X, screen.Bounds.Y, screen.Bounds.Width, screen.Bounds.Height);
			Rect rect3 = Rect.Intersect(rect, rect2);
			if (!rect3.IsEmpty && rect3.Width > 1.0 && rect3.Height > 1.0)
			{
				return false;
			}
		}
		System.Windows.Point defaultPoint2 = defaultPoint ?? new System.Windows.Point((SystemParameters.PrimaryScreenWidth > base.Width) ? ((SystemParameters.PrimaryScreenWidth - base.Width) / 2.0) : 0.0, (SystemParameters.PrimaryScreenHeight > base.Height) ? ((SystemParameters.PrimaryScreenHeight - base.Height) / 2.0) : 0.0);
		System.Windows.Point pomoLocationSafely = WidgetLocationHelper.GetPomoLocationSafely(left, top, base.Width, base.Height, compositionTarget.TransformFromDevice, defaultPoint2);
		bool num = Math.Abs(pomoLocationSafely.X - left) > 1.0;
		bool flag = Math.Abs(pomoLocationSafely.Y - top) > 1.0;
		if (!num && !flag)
		{
			return false;
		}
		base.Left = pomoLocationSafely.X;
		base.Top = pomoLocationSafely.Y;
		DelaySaveLocation();
		return true;
	}

	private void OnMouseDown(object sender, MouseButtonEventArgs e)
	{
		_mouseDown = true;
	}

	private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
	{
		AIFloatingWindow aiFloatingWindow = _aiFloatingWindow;
		if (aiFloatingWindow != null && aiFloatingWindow.IsMouseOver)
		{
			return;
		}
		if (SearchDialog != null && SearchDialog.Showing)
		{
			SearchDialog.TryClose();
		}
		if (SearchOperationDialog != null && SearchOperationDialog.Showing)
		{
			SearchOperationDialog.Close();
		}
		ListView?.OnWindowMouseDown(e);
		HabitView?.OnWindowMouseDown(e);
		if (FocusBorder.Visibility == Visibility.Visible)
		{
			TickFocusManager.MainFocus?.TryFoldDetail();
		}
		if (MainCalendar != null && MainCalendar.IsVisible)
		{
			System.Windows.Point position = e.GetPosition(MainCalendar);
			if (!(position.X > 0.0) || !(position.X < MainCalendar.ActualWidth) || !(position.Y > 0.0) || !(position.Y < MainCalendar.ActualHeight))
			{
				MainCalendar.ExitBatchSelect();
			}
		}
	}

	public void SetMainWindowsMinSize(bool isList)
	{
		if (isList)
		{
			base.MinWidth = 390.0;
			base.MinHeight = 400.0;
		}
		else
		{
			base.MinWidth = 680.0;
			base.MinHeight = 462.0;
		}
	}

	static MainWindow()
	{
		noMargin = default(Thickness);
		border = new Thickness(1.0);
		noRadius = default(CornerRadius);
		withRadius = new CornerRadius(14.0);
		cardNTopMargin = new Thickness(0.0, -12.0, 0.0, 0.0);
		cardMargin = new Thickness(0.0, 28.0, 6.0, 6.0);
		flatLeftBarWidth = new GridLength(50.0);
		cardLeftBarWidth = new GridLength(64.0);
		_lastSyncTime = DateTime.Now;
		_updateVersionShowed = false;
	}

	public MainWindow()
	{
		InitializeComponent();
		_aiWebViews.WebViewCreated += OnAIWebViewCreated;
		_aiWebViews.WebViewReleasing += OnAIWebViewReleasing;
		_aiClientHostState.SetMainModuleIsAI(ShowAI);
		_aiClientHostState.Changed += OnAIClientHostStateChanged;
		UpdateAIClientPresentation();
		Utils.SetWindowChrome(this, new Thickness(5.0, 5.0, 3.0, 5.0));
		OnWindowInitialized();
		TickDragHelper.DragMoving += OnClSplitDragOver;
		DataChangedNotifier.CalOrListBatchSelected += OnCalOrListBatchSelected;
		DataChangedNotifier.InterfaceStyleChanged += OnInterfaceStyleChanged;
		DataChangedNotifier.ThemeModeChanged += OnThemeChanged;
		SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "MainWindowBrush");
	}

	private void OnAIClientHostStateChanged(object sender, EventArgs e)
	{
		if (!base.Dispatcher.CheckAccess())
		{
			base.Dispatcher.BeginInvoke(new Action(UpdateAIClientPresentation));
		}
		else
		{
			UpdateAIClientPresentation();
		}
	}

	private async void UpdateAIClientPresentation()
	{
		try
		{
			if (!LocalSettings.Settings.EnableAI)
			{
				HideAIFloatingEntryGuide();
				AIFloatingEntry.HideEntry();
				_aiFloatingWindow?.HideImmediately();
				return;
			}
			AIClientPresentation presentation = _aiClientHostState.Presentation;
			if (_trackedAIPresentation != presentation)
			{
				_trackedAIPresentation = presentation;
				if (presentation != AIClientPresentation.Background)
				{
					UserActCollectUtils.AddClickEvent("ai_assistant", "show", (presentation == AIClientPresentation.FullScreen) ? "full_screen" : "floating");
				}
			}
			bool num = _aiClientHostState.ShouldShowFloatingWindow && HasAIAssistantPrivacyConsent();
			if (!num)
			{
				if (_aiClientHostState.Presentation == AIClientPresentation.FullScreen)
				{
					_aiFloatingWindow?.HideImmediately();
				}
				else
				{
					_aiFloatingWindow?.HideAnimated();
				}
			}
			UpdateAIFloatingEntryVisibility();
			if (num)
			{
				EnsureAIFloatingWindow();
				await AttachAIWebViewToFloatingWindowAsync(initializeBrowser: true);
			}
		}
		catch (Exception ex)
		{
			UtilLog.Info("Failed to update Agent WebView presentation. " + ExceptionUtils.BuildExceptionMessage(ex));
		}
	}

	internal void RefreshAIFeatureAvailability()
	{
		if (!LocalSettings.Settings.EnableAI)
		{
			_isAIFloatingEntryGuidePending = false;
			HideAIFloatingEntryGuide();
			_aiClientHostState.CloseFloatingWindow();
			AIWindow.CloseDesktopWidget();
		}
		UpdateAIClientPresentation();
		AIWindow.RefreshDesktopWidgetEntrances();
	}

	internal void RequestAIFloatingEntryGuide()
	{
		if (LocalSettings.Settings.EnableAI && !LocalSettings.Settings.ExtraSettings.CheckHasFlag(HasSetFlag.AIAssistantFloatingEntryGuide))
		{
			_isAIFloatingEntryGuidePending = true;
			TryShowAIFloatingEntryGuide();
		}
	}

	private void UpdateAIFloatingEntryVisibility()
	{
		AIFloatingEntry.IsGenerating = _aiClientHostState.ShouldAnimateFloatingEntry;
		if (!_aiClientHostState.ShouldShowFloatingEntry)
		{
			HideAIFloatingEntryGuide();
			if (_aiClientHostState.ShouldShowFloatingWindow && HasAIAssistantPrivacyConsent())
			{
				AIFloatingEntry.HideAnimated();
			}
			else
			{
				AIFloatingEntry.HideEntry();
			}
		}
		else
		{
			AIFloatingEntry.ShowAnimated();
			TryShowAIFloatingEntryGuide();
		}
	}

	private void TryShowAIFloatingEntryGuide()
	{
		if (_isAIFloatingEntryGuidePending && LocalSettings.Settings.EnableAI && _aiClientHostState.ShouldShowFloatingEntry && AIFloatingEntry.Visibility == Visibility.Visible)
		{
			_isAIFloatingEntryGuidePending = false;
			AIFloatingEntryGuideHint.Opacity = 0.0;
			AIFloatingEntryGuideHint.ArrowOffset = 22.0;
			AIFloatingEntryGuidePopup.HorizontalOffset = 0.0;
			AIFloatingEntryGuidePopup.IsOpen = true;
			LocalSettings.Settings.ExtraSettings.SetHasFlag(HasSetFlag.AIAssistantFloatingEntryGuide);
			LocalSettings.Settings.Save();
		}
	}

	private void HideAIFloatingEntryGuide()
	{
		AIFloatingEntryGuidePopup.IsOpen = false;
	}

	private CustomPopupPlacement[] PlaceAIFloatingEntryGuide(System.Windows.Size popupSize, System.Windows.Size targetSize, System.Windows.Point offset)
	{
		double x = targetSize.Width / 2.0 - popupSize.Width + 32.0 + 28.0 + offset.X;
		double y = 0.0 - popupSize.Height - 14.0 + offset.Y;
		return new CustomPopupPlacement[1]
		{
			new CustomPopupPlacement(new System.Windows.Point(x, y), PopupPrimaryAxis.Horizontal)
		};
	}

	private void OnAIFloatingEntryGuideOpened(object sender, EventArgs e)
	{
		base.Dispatcher.BeginInvoke((Action)delegate
		{
			if (AIFloatingEntryGuidePopup.IsOpen)
			{
				System.Windows.Point point = AIFloatingEntry.PointToScreen(new System.Windows.Point(AIFloatingEntry.ActualWidth / 2.0, 0.0));
				System.Windows.Point arrowCenterOnScreen = AIFloatingEntryGuideHint.GetArrowCenterOnScreen();
				System.Windows.Point point2 = AIFloatingEntryGuideHint.PointFromScreen(point);
				System.Windows.Point point3 = AIFloatingEntryGuideHint.PointFromScreen(arrowCenterOnScreen);
				double val = AIFloatingEntryGuideHint.ArrowOffset + point2.X - point3.X;
				double val2 = Math.Max(0.0, AIFloatingEntryGuideHint.ActualWidth - 20.0);
				AIFloatingEntryGuideHint.ArrowOffset = Math.Max(0.0, Math.Min(val, val2));
				base.Dispatcher.BeginInvoke(DispatcherPriority.Render, (Action)delegate
				{
					if (AIFloatingEntryGuidePopup.IsOpen)
					{
						AIFloatingEntryGuideHint.Opacity = 1.0;
					}
				});
			}
		});
	}

	private void OnAIFloatingEntryClick(object sender, RoutedEventArgs e)
	{
		HideAIFloatingEntryGuide();
		if (LocalSettings.Settings.EnableAI)
		{
			if (!HasAIAssistantPrivacyConsent())
			{
				SwitchModule(DisplayModule.AI, force: true, click: true);
			}
			else if (_aiClientHostState.TryOpenFloatingWindow())
			{
				GetOrCreateAIWorkspaceView().RefreshConversationsAsync();
				GetOrCreateAIConversationView().RefreshGreetingAsync();
			}
		}
	}

	private void OnAIFloatingEntryGuideClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		OnAIFloatingEntryClick(sender, e);
	}

	private static bool HasAIAssistantPrivacyConsent()
	{
		return LocalSettings.Settings.ExtraSettings.CheckHasFlag(HasSetFlag.AIAssistantPrivacyConsent);
	}

	private void OnAIWebViewCreated(AIWebAppView aiWebView)
	{
		aiWebView.AgentBridgeReadyChanged += OnAgentBridgeReadyChanged;
		aiWebView.AgentNotificationReceived += OnAgentNotificationReceived;
		aiWebView.StatusPresentationChanged += OnAIWebStatusPresentationChanged;
	}

	private void OnAIWebViewReleasing(AIWebAppView aiWebView)
	{
		_aiConversationView?.DetachWebContent(aiWebView);
		aiWebView.AgentBridgeReadyChanged -= OnAgentBridgeReadyChanged;
		aiWebView.AgentNotificationReceived -= OnAgentNotificationReceived;
		aiWebView.StatusPresentationChanged -= OnAIWebStatusPresentationChanged;
	}

	private void OnAIWebStatusPresentationChanged(object sender, EventArgs e)
	{
		if (_aiWebViews.IsActive(sender) && _aiWebViews.ActiveHost == AIWebViewHost.FloatingWindow && _aiClientHostState.ShouldShowFloatingWindow && sender is AIWebAppView aIWebAppView)
		{
			_aiFloatingWindow?.SetInitialWebViewLoading(!aIWebAppView.HasPresentedBrowserContent);
			if (aIWebAppView.ShouldPresentStatusPanel)
			{
				_aiFloatingWindow?.ShowAnimated();
			}
		}
	}

	private void OnAgentBridgeReadyChanged(object sender, EventArgs e)
	{
		if (_aiWebViews.IsActive(sender))
		{
			AIWebAppView activeWebView = _aiWebViews.ActiveWebView;
			bool isAgentBridgeReady = activeWebView.IsAgentBridgeReady;
			_aiFloatingWindow?.SetAgentBridgeReady(isAgentBridgeReady);
			_aiWorkspaceView?.SetAgentBridgeReady(isAgentBridgeReady);
			_aiConversationView?.SetAgentBridgeReady(isAgentBridgeReady);
			if (isAgentBridgeReady)
			{
				SynchronizeAIConversationStateAsync(activeWebView);
			}
		}
	}

	private async Task SynchronizeAIConversationStateAsync(AIWebAppView aiWebView)
	{
		if (_aiWorkspaceView != null)
		{
			await _aiWorkspaceView.SynchronizeGenerationStateAsync(aiWebView.GetAgentStateAsync);
		}
		if (await _aiWebViews.SynchronizeAsync(aiWebView) && aiWebView == _aiWebViews.ActiveWebView && aiWebView.IsAgentBridgeReady && _aiWebViews.MatchesSharedPage(aiWebView))
		{
			string page = _aiWebViews.SharedPage;
			string conversationId = _aiWebViews.SharedConversationId;
			ApplyAIPageState(page, conversationId);
			JObject approvals = await aiWebView.GetApprovalsAsync();
			if (aiWebView == _aiWebViews.ActiveWebView && aiWebView.IsAgentBridgeReady && _aiWebViews.MatchesSharedPage(aiWebView) && string.Equals(page, _aiWebViews.SharedPage, StringComparison.Ordinal) && string.Equals(conversationId, _aiWebViews.SharedConversationId, StringComparison.Ordinal))
			{
				_aiConversationView?.SetApprovals(approvals);
			}
		}
	}

	private void OnAgentNotificationReceived(object sender, AgentBridgeNativeEventArgs e)
	{
		if (!_aiWebViews.IsActive(sender))
		{
			return;
		}
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
					bool? flag = (bool?)e.Payload?["isGenerating"];
					if (flag.HasValue)
					{
						_aiClientHostState.SetGenerating(flag.Value);
						_aiConversationView?.SetGenerating(flag.Value);
						_aiWorkspaceView?.SetGenerating(flag.Value);
					}
					bool? flag2 = (bool?)e.Payload?["isQuotaExceeded"];
					if (flag2.HasValue)
					{
						_aiConversationView?.SetTokenLimitReached(flag2.Value);
					}
					TryApplyResolvedAIPageState(e.Payload);
				}
				break;
			case 'i':
				if (methodName == "agent.fillComposer")
				{
					_aiConversationView?.FillComposer((string?)e.Payload?["content"], (string?)e.Payload?["messageId"]);
				}
				break;
			case 'h':
				if (methodName == "agent.showFeedback")
				{
					_aiConversationView?.ShowFeedback(e.Payload);
				}
				break;
			case 'a':
				if (methodName == "agent.ratingResult")
				{
					_aiConversationView?.ApplyRatingResult(e.Payload);
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
					_aiConversationView?.SetTokenLimitReached(isTokenLimitReached: true);
				}
				break;
			case 'a':
				if (methodName == "agent.appUpdateRequired")
				{
					bool? flag3 = AIConversationView.ResolveAppUpdateRequiredBlocking(e.Payload);
					if (flag3.HasValue)
					{
						_aiConversationView?.SetAppUpdateRequired(_aiWebViews.SharedConversationId, flag3.Value);
					}
				}
				break;
			}
			break;
		case 22:
			if (methodName == "agent.approvalsChanged")
			{
				_aiConversationView?.SetApprovals(e.Payload);
			}
			break;
		case 25:
			if (methodName == "agent.conversationCreated")
			{
				string conversationId = ((string?)e.Payload?["conversationId"]) ?? ((string?)e.Payload?["id"]);
				string text = (string?)e.Payload?["title"];
				bool valueOrDefault = (bool?)e.Payload?["isLocalOnly"] == true;
				if (!_aiWebViews.IsActiveSynchronizationPending)
				{
					ApplyAIPageState("conversation", conversationId);
				}
				SetAIConversationTitle(text);
				_aiWorkspaceView?.UpsertConversation(conversationId, text, valueOrDefault);
				if (!valueOrDefault && _aiWorkspaceView != null)
				{
					_aiWorkspaceView.RefreshConversationsAsync();
				}
			}
			break;
		case 30:
			if (methodName == "agent.refreshConversationTitle")
			{
				string text2 = (string?)e.Payload?["conversationId"];
				string text3 = (string?)e.Payload?["title"];
				if (_aiWorkspaceView == null || string.IsNullOrWhiteSpace(text2) || _aiWorkspaceView.IsCurrentConversation(text2))
				{
					SetAIConversationTitle(text3);
				}
				_aiWorkspaceView?.UpdateConversationTitle(text2, text3);
				if (string.IsNullOrWhiteSpace(text3) && _aiWorkspaceView != null)
				{
					RefreshAIConversationTitleAsync(text2);
				}
			}
			break;
		case 17:
		{
			if (!(methodName == "agent.pageChanged"))
			{
				break;
			}
			string text4 = (string?)e.Payload?["page"];
			if (!_aiWebViews.IsActiveSynchronizationPending)
			{
				AIWebAppView activeWebView = _aiWebViews.ActiveWebView;
				if ((activeWebView != null && activeWebView.IsAgentPageStateResolved) || string.Equals(text4, "conversation", StringComparison.OrdinalIgnoreCase))
				{
					ApplyAIPageState(text4, (string?)e.Payload?["conversationId"]);
				}
			}
			break;
		}
		case 15:
			if (methodName == "agent.cardClick")
			{
				HandleAgentCardClickAsync(e.Payload);
			}
			break;
		case 43:
			if (methodName == "agent.scrollToBottomButtonVisibilityChanged")
			{
				_aiConversationView?.SetScrollToBottomButtonVisible(AIConversationView.ResolveScrollToBottomButtonVisibility(e.Payload));
			}
			break;
		case 26:
			if (methodName == "agent.viewFullCardListMode")
			{
				_aiWorkspaceView?.HideDetail();
			}
			break;
		}
	}

	internal static bool IsAIPageStateReady(JToken state)
	{
		return AIWebViewHostManager.IsPageStateReady(state);
	}

	internal static bool ShouldDeferRestoringAIHomeState(JToken state, bool isPageStateResolved, bool hasActiveConversation)
	{
		if (!isPageStateResolved && hasActiveConversation)
		{
			return string.Equals((string?)state?["page"], "home", StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private bool TryApplyResolvedAIPageState(JToken state)
	{
		if (_aiWebViews.IsActiveSynchronizationPending || !IsAIPageStateReady(state) || ShouldDeferRestoringAIHomeState(state, _aiWebViews.ActiveWebView?.IsAgentPageStateResolved ?? false, _aiConversationView?.HasActiveConversation ?? false))
		{
			return false;
		}
		ApplyAIPageState((string?)state["page"], ((string?)state["currentConversationId"]) ?? ((string?)state["conversationId"]));
		return true;
	}

	private void ApplyAIPageState(string page, string conversationId)
	{
		bool flag = string.Equals(page, "home", StringComparison.OrdinalIgnoreCase);
		if (flag || string.Equals(page, "conversation", StringComparison.OrdinalIgnoreCase))
		{
			_aiWebViews.CompleteActivePageState(page, conversationId);
			string sharedConversationId = _aiWebViews.SharedConversationId;
			_aiConversationView?.SetConversationActive(!flag, flag ? null : sharedConversationId);
			_aiWorkspaceView?.SetCurrentConversation(flag ? null : sharedConversationId);
			if (flag)
			{
				SetAIConversationTitle(null);
				_aiWorkspaceView?.HideDetail();
			}
			if (_aiClientHostState.ShouldShowFloatingWindow && _aiWebViews.ActiveHost == AIWebViewHost.FloatingWindow)
			{
				_aiFloatingWindow?.ShowAnimated();
			}
		}
	}

	internal async Task HandleAgentCardClickAsync(JToken payload, Window floatingOwner = null)
	{
		AgentCardType? agentCardType = ParseAgentCardType((string?)payload?["type"]);
		if (CheckAgentCardFeatureEnabled(agentCardType, floatingOwner ?? this))
		{
			UtilLog.Info("Agent card click received: type=" + (agentCardType?.ToString() ?? "null"));
			switch (agentCardType)
			{
			case AgentCardType.Task:
				await HandleAgentTaskCardClickAsync(payload, floatingOwner);
				break;
			case AgentCardType.Project:
				await HandleAgentProjectCardClickAsync(payload);
				break;
			case AgentCardType.Habit:
				await HandleAgentHabitCardClickAsync(payload, floatingOwner);
				break;
			case AgentCardType.Focus:
				await HandleAgentFocusCardClickAsync(payload, floatingOwner);
				break;
			default:
				UtilLog.Info("Agent card click ignored: unsupported card type.");
				break;
			}
		}
	}

	internal static bool CheckAgentCardFeatureEnabled(AgentCardType? type, Window owner)
	{
		string text = null;
		if (type == AgentCardType.Habit && !LocalSettings.Settings.ShowHabit)
		{
			text = "HabitView";
		}
		else if (type == AgentCardType.Focus && !LocalSettings.Settings.EnableFocus)
		{
			text = "PomoFocus";
		}
		if (text == null)
		{
			return true;
		}
		if (new CustomerDialog(Utils.GetString("FeatureDisabled"), string.Format(Utils.GetString("FeatureDisabledContent"), Utils.GetString(text)), Utils.GetString("FeatureDisabledToEnable"), Utils.GetString("Cancel"), owner).ShowDialog() == true)
		{
			SettingDialog.ShowSettingDialog(SettingsType.Feature, owner);
		}
		return false;
	}

	internal static AgentCardType? ParseAgentCardType(string type)
	{
		return type?.Trim().ToLowerInvariant() switch
		{
			"task" => AgentCardType.Task, 
			"project" => AgentCardType.Project, 
			"habit" => AgentCardType.Habit, 
			"focus" => AgentCardType.Focus, 
			_ => null, 
		};
	}

	internal static string ResolveAgentCardNotFoundResourceKey(AgentCardType type)
	{
		return type switch
		{
			AgentCardType.Task => "TaskNotExistOrDeleted", 
			AgentCardType.Project => "ListNotExistOrDeleted", 
			AgentCardType.Habit => "HabitNotExistOrDeleted", 
			AgentCardType.Focus => "FocusNotExistOrDeleted", 
			_ => throw new ArgumentOutOfRangeException("type", type, null), 
		};
	}

	private static void ShowAgentCardNotFoundToast(AgentCardType type)
	{
		Utils.Toast(Utils.GetString(ResolveAgentCardNotFoundResourceKey(type)));
	}

	private async Task HandleAgentTaskCardClickAsync(JToken payload, Window floatingOwner)
	{
		string taskId = ((string?)payload?["taskId"])?.Trim();
		string projectId = ((string?)payload?["projectId"])?.Trim();
		if (string.IsNullOrEmpty(taskId))
		{
			UtilLog.Info("Agent task card click ignored: missing taskId.");
			return;
		}
		await SyncAgentCardDataAsync();
		TaskModel taskModel = await TaskUtils.TryLoadTask(taskId, projectId, checkDeleted: true, ResolveAgentCardNotFoundResourceKey(AgentCardType.Task));
		if (taskModel == null)
		{
			UtilLog.Info("Agent task card click failed: task not found.");
		}
		else if (floatingOwner == null && _aiClientHostState.Presentation == AIClientPresentation.FullScreen)
		{
			if (!(await GetOrCreateAIWorkspaceView().ShowTaskDetailAsync(taskId)))
			{
				ShowAgentCardNotFoundToast(AgentCardType.Task);
				UtilLog.Info("Agent task card detail failed to load.");
			}
			else
			{
				UserActCollectUtils.AddClickEvent("ai_assistant", "result_card", "task_detail");
				UtilLog.Info("Agent task card detail opened in third column.");
			}
		}
		else
		{
			await NavigateAgentTaskInMainWindowAsync(taskModel);
			if (ListView != null)
			{
				UserActCollectUtils.AddClickEvent("ai_assistant", "result_card", "task_detail");
			}
		}
	}

	private async Task NavigateAgentTaskInMainWindowAsync(TaskModel task)
	{
		if (LocalSettings.Settings.InSearch)
		{
			StopSearch(forceSwitch: false);
		}
		SwitchModule(DisplayModule.Task);
		ListView?.NavigateTask(task);
		if (await AppLockCache.GetAppLocked())
		{
			AppUnlockWindow.TryUnlockApp(ShowWindow);
		}
		else
		{
			ShowWindow();
		}
		UtilLog.Info("Agent task card opened in main task list.");
	}

	private async Task HandleAgentProjectCardClickAsync(JToken payload)
	{
		string projectId = ((string?)payload?["projectId"])?.Trim();
		if (string.IsNullOrEmpty(projectId))
		{
			UtilLog.Info("Agent project card click ignored: missing projectId.");
			return;
		}
		await SyncAgentCardDataAsync();
		ProjectModel projectModel = await ProjectDao.GetProjectById(projectId);
		if (projectModel == null && !Utils.IsNetworkAvailable())
		{
			Utils.Toast(Utils.GetString("NoNetwork"));
			UtilLog.Info("Agent project card click failed: project is not local and network is unavailable.");
			return;
		}
		if (projectModel == null || projectModel.delete_status)
		{
			ShowAgentCardNotFoundToast(AgentCardType.Project);
			UtilLog.Info("Agent project card click failed: project not found.");
			return;
		}
		await NavigateProject("project", projectId);
		if (ListView != null)
		{
			UserActCollectUtils.AddClickEvent("ai_assistant", "result_card", "project_list");
		}
	}

	internal static async Task SyncAgentCardDataAsync()
	{
		if (!Utils.IsNetworkAvailable())
		{
			return;
		}
		try
		{
			await SyncManager.Sync();
		}
		catch (Exception ex)
		{
			UtilLog.Info("Agent card synchronization failed: " + ExceptionUtils.BuildExceptionMessage(ex));
		}
	}

	internal static async Task SyncAgentHabitCardDataAsync(string habitId)
	{
		if (string.IsNullOrWhiteSpace(habitId) || !Utils.IsNetworkAvailable())
		{
			return;
		}
		try
		{
			await HabitService.PullRemoteHabits();
			HabitModel habitModel = await HabitDao.GetHabitById(habitId);
			bool flag = habitModel != null && habitModel.Status != -1;
			if (flag)
			{
				flag = await HabitService.PullRemoteCheckinAndRecordData(habitModel);
			}
			if (flag)
			{
				await HabitDao.UpdateHabitInfo(habitId, isUpdateSyncStatus: false);
				DataChangedNotifier.NotifyHabitsChanged();
			}
		}
		catch (Exception ex)
		{
			UtilLog.Info("Agent habit card synchronization failed: " + ExceptionUtils.BuildExceptionMessage(ex));
		}
	}

	internal static async Task SyncAgentFocusCardDataAsync(string focusId, int? focusType)
	{
		if (!Utils.IsNetworkAvailable())
		{
			return;
		}
		try
		{
			await PomoSyncService.PullForCardAsync(focusId, focusType);
		}
		catch (Exception ex)
		{
			UtilLog.Info("Agent focus card synchronization failed: " + ExceptionUtils.BuildExceptionMessage(ex));
		}
	}

	private async Task HandleAgentHabitCardClickAsync(JToken payload, Window floatingOwner)
	{
		string habitId = ((string?)payload?["habitId"])?.Trim();
		if (string.IsNullOrEmpty(habitId))
		{
			UtilLog.Info("Agent habit card click ignored: missing habitId.");
			return;
		}
		await SyncAgentHabitCardDataAsync(habitId);
		if (await HabitService.IsHabitDeleted(habitId))
		{
			ShowAgentCardNotFoundToast(AgentCardType.Habit);
			UtilLog.Info("Agent habit card click failed: habit not found.");
			return;
		}
		if (floatingOwner == null && _aiClientHostState.Presentation == AIClientPresentation.FullScreen)
		{
			if (GetOrCreateAIWorkspaceView().ShowHabitDetail(habitId))
			{
				UserActCollectUtils.AddClickEvent("ai_assistant", "result_card", "habit_detail");
				UtilLog.Info("Agent habit card detail opened in third column.");
			}
			return;
		}
		SwitchModule(DisplayModule.Habit);
		if (floatingOwner != null)
		{
			await TryShowMainWindow();
		}
		if (HabitView != null)
		{
			HabitView.NavigateItem(habitId);
			UserActCollectUtils.AddClickEvent("ai_assistant", "result_card", "habit_detail");
		}
	}

	private async Task HandleAgentFocusCardClickAsync(JToken payload, Window floatingOwner)
	{
		string focusId = ((string?)payload?["focusId"])?.Trim();
		if (string.IsNullOrEmpty(focusId))
		{
			UtilLog.Info("Agent focus card click ignored: missing focusId.");
			return;
		}
		await SyncAgentFocusCardDataAsync(focusId, (int?)payload?["focusType"]);
		PomodoroModel pomodoroModel = await PomoService.GetAdjustPomo(focusId);
		if (pomodoroModel == null)
		{
			ShowAgentCardNotFoundToast(AgentCardType.Focus);
			UtilLog.Info("Agent focus card click failed: focus record not found.");
			return;
		}
		UtilLog.Info("Agent focus card click opening detail: focusType=" + (((int?)payload?["focusType"])?.ToString() ?? "null"));
		PomoDetailWindow pomoDetailWindow = new PomoDetailWindow(new PomoDisplayViewModel(pomodoroModel, pomodoroModel.Tasks), (CalendarDisplayViewModel)null);
		pomoDetailWindow.SetCustomStyle();
		UIElement target = floatingOwner ?? this;
		if (floatingOwner == null && _aiClientHostState.Presentation == AIClientPresentation.Floating && _aiFloatingWindow != null)
		{
			target = _aiFloatingWindow;
		}
		pomoDetailWindow.ShowCentered(target);
		UserActCollectUtils.AddClickEvent("ai_assistant", "result_card", "focus_record");
	}

	private AIConversationView GetOrCreateAIConversationView()
	{
		if (_aiConversationView != null)
		{
			return _aiConversationView;
		}
		_aiConversationView = new AIConversationView();
		_aiConversationView.SubmitRequested += OnAIConversationSubmitRequested;
		_aiConversationView.StopRequested += OnAIConversationStopRequested;
		_aiConversationView.ScrollToBottomRequested += OnAIScrollToBottomRequested;
		_aiConversationView.ApprovalResponseRequested += OnAIApprovalResponseRequested;
		_aiConversationView.RatingSubmissionRequested += OnAIRatingSubmissionRequested;
		_aiConversationView.AppUpdateRequested += OnAIAppUpdateRequested;
		_aiConversationView.SetAgentBridgeReady(_aiWebViews.ActiveWebView?.IsAgentBridgeReady ?? false);
		return _aiConversationView;
	}

	private AIWorkspaceView GetOrCreateAIWorkspaceView()
	{
		if (_aiWorkspaceView != null)
		{
			return _aiWorkspaceView;
		}
		_aiWorkspaceView = new AIWorkspaceView();
		_aiWorkspaceView.NewConversationRequested += OnAINewConversationRequested;
		_aiWorkspaceView.ConversationSelected += OnAIConversationSelected;
		_aiWorkspaceView.CurrentConversationTitleChanged += OnAICurrentConversationTitleChanged;
		_aiWorkspaceView.PrivacyAuthorizationAccepted += OnAIPrivacyAuthorizationAccepted;
		_aiWorkspaceView.SetPrivacyAuthorizationRequired(!HasAIAssistantPrivacyConsent());
		_aiWorkspaceView.SetAgentBridgeReady(_aiWebViews.ActiveWebView?.IsAgentBridgeReady ?? false);
		AIWebAppView activeWebView = _aiWebViews.ActiveWebView;
		if (activeWebView != null && activeWebView.IsAgentBridgeReady)
		{
			_aiWorkspaceView.SynchronizeGenerationStateAsync(_aiWebViews.ActiveWebView.GetAgentStateAsync);
		}
		return _aiWorkspaceView;
	}

	private async void OnAIPrivacyAuthorizationAccepted(object sender, EventArgs e)
	{
		if (!HasAIAssistantPrivacyConsent())
		{
			LocalSettings.Settings.ExtraSettings.SetHasFlag(HasSetFlag.AIAssistantPrivacyConsent);
			LocalSettings.Settings.Save();
		}
		_aiWorkspaceView?.SetPrivacyAuthorizationRequired(isRequired: false);
		if (_aiClientHostState.Presentation == AIClientPresentation.FullScreen)
		{
			await AttachAIWebViewToMainWindowAsync(initializeBrowser: true);
		}
	}

	private Task<AIWebAppView> AttachAIWebViewToMainWindowAsync(bool initializeBrowser)
	{
		return AttachAIWebViewAsync(AIWebViewHost.MainWindow, initializeBrowser);
	}

	private Task<AIWebAppView> AttachAIWebViewToFloatingWindowAsync(bool initializeBrowser)
	{
		return AttachAIWebViewAsync(AIWebViewHost.FloatingWindow, initializeBrowser);
	}

	private async Task<AIWebAppView> AttachAIWebViewAsync(AIWebViewHost targetHost, bool initializeBrowser)
	{
		AIWebAppView aiWebView = null;
		int attachmentVersion = 0;
		bool requiresStateSynchronization = false;
		await RunAIHostTransitionAsync(_aiWebViewHostTransitionGate, async delegate
		{
			AIWebViewActivation aIWebViewActivation = await AttachAIWebViewHostAsync(targetHost);
			aiWebView = aIWebViewActivation?.WebView;
			requiresStateSynchronization = aIWebViewActivation?.RequiresPageSynchronization ?? false;
			attachmentVersion = _aiWebViewHostAttachmentVersion;
		}, async delegate
		{
			if (IsCurrent())
			{
				if (initializeBrowser)
				{
					if (targetHost == AIWebViewHost.FloatingWindow && aiWebView.LifecycleState == AIWebViewLifecycleState.NotInitialized)
					{
						await Task.Delay(AIFloatingWindow.TransitionDuration);
						if (!IsCurrent())
						{
							return;
						}
					}
					await aiWebView.EnsureInitializedAsync();
					if (!IsCurrent())
					{
						return;
					}
				}
				if (aiWebView.IsAgentBridgeReady && requiresStateSynchronization)
				{
					await SynchronizeAIConversationStateAsync(aiWebView);
				}
			}
		});
		return aiWebView;
		bool IsCurrent()
		{
			if (aiWebView != null && attachmentVersion == _aiWebViewHostAttachmentVersion && aiWebView == _aiWebViews.ActiveWebView && _aiWebViews.ActiveHost == targetHost)
			{
				return IsAIWebViewHostRequested(targetHost);
			}
			return false;
		}
	}

	internal static async Task RunAIHostTransitionAsync(SemaphoreSlim gate, Func<Task> attachHost, Func<Task> completeWebState)
	{
		await gate.WaitAsync();
		try
		{
			await attachHost();
		}
		finally
		{
			gate.Release();
		}
		await completeWebState();
	}

	private async Task<AIWebViewActivation> AttachAIWebViewHostAsync(AIWebViewHost targetHost)
	{
		AIWebAppView suspendedWebView = null;
		try
		{
			if (!IsAIWebViewHostRequested(targetHost))
			{
				return null;
			}
			AIConversationView conversationView = GetOrCreateAIConversationView();
			_aiWebViewHostAttachmentVersion++;
			AIWorkspaceView workspaceView = GetOrCreateAIWorkspaceView();
			AIWebAppView previousWebView = _aiWebViews.ActiveWebView;
			string previousConversationId = _aiWebViews.SharedConversationId;
			string previousPage = _aiWebViews.SharedPage;
			bool preserveScroll = previousWebView != null && _aiWebViews.ActiveHost != targetHost && !string.Equals(previousPage, "home", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(previousConversationId);
			if (preserveScroll)
			{
				if (targetHost == AIWebViewHost.FloatingWindow)
				{
					_aiFloatingWindow.ShowAnimated();
				}
				await previousWebView.PreserveHostScrollPositionAsync(restore: false);
				if (!IsAIWebViewHostRequested(targetHost))
				{
					return null;
				}
				suspendedWebView = previousWebView;
				suspendedWebView.SetHostSwitchPresentationSuspended(suspended: true);
				conversationView.SetHostLayoutPending(pending: true);
			}
			AIWebViewActivation activation = _aiWebViews.Activate(targetHost);
			AIWebAppView aiWebView = activation.WebView;
			bool requiresPageSynchronization = activation.RequiresPageSynchronization;
			if (requiresPageSynchronization)
			{
				conversationView.SetConversationStateResolving();
			}
			if (activation.PreviousWebView != aiWebView)
			{
				_aiConversationView?.DetachWebContent(activation.PreviousWebView);
				_ = activation.PreviousWebView;
			}
			if (!IsAIWebViewHostRequested(targetHost))
			{
				return null;
			}
			if (targetHost == AIWebViewHost.MainWindow)
			{
				_aiFloatingWindow?.PrepareForHostChange();
				_aiFloatingWindow?.HideImmediately();
				_aiFloatingWindow?.DetachConversationContent(conversationView);
				conversationView.SetFloatingMode(isFloatingMode: false);
				workspaceView.AttachConversationContent(conversationView);
				if (AIBorder.Child != workspaceView)
				{
					AIBorder.Child = workspaceView;
				}
			}
			else
			{
				workspaceView.PrepareForConversationHostChange();
				workspaceView.DetachConversationContent(conversationView);
				conversationView.SetFloatingMode(isFloatingMode: true);
				_aiFloatingWindow.SetInitialWebViewLoading(!aiWebView.HasPresentedBrowserContent);
				_aiFloatingWindow.AttachConversationWorkspace(workspaceView);
				_aiFloatingWindow.AttachConversationContent(conversationView);
			}
			conversationView.AttachWebContent(aiWebView);
			bool isAgentBridgeReady = aiWebView.IsAgentBridgeReady;
			_aiFloatingWindow?.SetAgentBridgeReady(isAgentBridgeReady);
			workspaceView.SetAgentBridgeReady(isAgentBridgeReady);
			conversationView.SetAgentBridgeReady(isAgentBridgeReady);
			if (!requiresPageSynchronization)
			{
				ApplyAIPageState(_aiWebViews.SharedPage, _aiWebViews.SharedConversationId);
				if (!string.Equals(_aiWebViews.SharedPage, "home", StringComparison.OrdinalIgnoreCase))
				{
					SetAIConversationTitle(workspaceView.GetConversationTitle(_aiWebViews.SharedConversationId));
				}
			}
			UtilLog.Info("Agent WebView attached to retained host: " + targetHost);
			if (targetHost == AIWebViewHost.FloatingWindow && IsAIWebViewHostRequested(targetHost))
			{
				_aiFloatingWindow.ShowAnimated();
			}
			if (preserveScroll && !requiresPageSynchronization && previousWebView == aiWebView && IsAIWebViewHostRequested(targetHost))
			{
				conversationView.UpdateLayout();
				await base.Dispatcher.InvokeAsync(delegate
				{
				}, DispatcherPriority.Loaded);
				if (IsAIWebViewHostRequested(targetHost) && previousConversationId == _aiWebViews.SharedConversationId && previousPage == _aiWebViews.SharedPage)
				{
					await aiWebView.PreserveHostScrollPositionAsync(restore: true);
				}
			}
			if (suspendedWebView != null && IsAIWebViewHostRequested(targetHost))
			{
				conversationView.UpdateLayout();
				await aiWebView.WaitForHostSwitchLayoutAsync();
			}
			return activation;
		}
		finally
		{
			suspendedWebView?.SetHostSwitchPresentationSuspended(suspended: false);
			_aiConversationView?.SetHostLayoutPending(pending: false);
		}
	}

	private bool IsAIWebViewHostRequested(AIWebViewHost targetHost)
	{
		switch (targetHost)
		{
		case AIWebViewHost.MainWindow:
			return _aiClientHostState.Presentation == AIClientPresentation.FullScreen;
		case AIWebViewHost.FloatingWindow:
			if (_aiClientHostState.ShouldShowFloatingWindow)
			{
				return _aiFloatingWindow != null;
			}
			return false;
		default:
			return false;
		}
	}

	private void EnsureAIFloatingWindow()
	{
		if (_aiFloatingWindow == null)
		{
			_aiFloatingWindow = new AIFloatingWindow();
			AIFloatingLayer.Content = _aiFloatingWindow;
			_aiFloatingWindow.SetBinding(FrameworkElement.MaxWidthProperty, new System.Windows.Data.Binding("ActualWidth")
			{
				Source = Container
			});
			_aiFloatingWindow.SetBinding(FrameworkElement.MaxHeightProperty, new System.Windows.Data.Binding("ActualHeight")
			{
				Source = Container
			});
			_aiFloatingWindow.CollapseRequested += OnAIFloatingWindowCollapseRequested;
			_aiFloatingWindow.NewConversationRequested += OnAIFloatingWindowNewConversationRequested;
			_aiFloatingWindow.DockRequested += OnAIFloatingWindowDockRequested;
			_aiFloatingWindow.SetAgentBridgeReady(_aiWebViews.ActiveWebView?.IsAgentBridgeReady ?? false);
		}
	}

	private void WarmUpAIFloatingClient()
	{
		if (!LocalSettings.Settings.EnableAI || !HasAIAssistantPrivacyConsent() || _aiClientHostState.ShouldShowFloatingWindow)
		{
			return;
		}
		try
		{
			EnsureAIFloatingWindow();
			_aiFloatingWindow.WarmUp();
			GetOrCreateAIWorkspaceView();
			GetOrCreateAIConversationView();
			_aiWebViews.EnsureRetained(AIWebViewHost.FloatingWindow);
		}
		catch (Exception ex)
		{
			UtilLog.Info("Failed to warm up Agent floating client. " + ExceptionUtils.BuildExceptionMessage(ex));
		}
	}

	private void OnAIFloatingWindowCollapseRequested(object sender, EventArgs e)
	{
		(sender as AIFloatingWindow)?.HideAnimated();
		_aiClientHostState.CloseFloatingWindow();
	}

	private void OnAIFloatingWindowNewConversationRequested(object sender, EventArgs e)
	{
		StartNewAIConversationAsync();
	}

	private void OnAINewConversationRequested(object sender, EventArgs e)
	{
		StartNewAIConversationAsync();
	}

	public void OnAINewConversationShortcut()
	{
		if (LocalSettings.Settings.EnableAI && !_aiClientHostState.IsTemporarilySuppressed && (_aiClientHostState.Presentation == AIClientPresentation.FullScreen || _aiClientHostState.ShouldShowFloatingWindow))
		{
			UserActCollectUtils.AddShortCutEvent("AI", "new_chat");
			StartNewAIConversationAsync(focusComposer: true);
		}
	}

	private void OnAICurrentConversationTitleChanged(string title)
	{
		SetAIConversationTitle(title);
	}

	private async Task StartNewAIConversationAsync(bool focusComposer = false)
	{
		if (!HasAIAssistantPrivacyConsent())
		{
			return;
		}
		AIWebAppView activeWebView = _aiWebViews.ActiveWebView;
		bool flag = activeWebView?.IsAgentBridgeReady ?? false;
		if (flag)
		{
			flag = await activeWebView.StartNewConversationAsync();
		}
		if (flag)
		{
			ApplyAIPageState("home", null);
			if (focusComposer)
			{
				_aiConversationView?.FocusNewConversationComposer();
			}
		}
	}

	private async Task<bool> OnAIConversationSelected(string conversationId, string title)
	{
		if (!HasAIAssistantPrivacyConsent())
		{
			return false;
		}
		AIWebAppView aiWebView = _aiWebViews.ActiveWebView;
		if (aiWebView == null)
		{
			return false;
		}
		await aiWebView.EnsureInitializedAsync();
		bool flag = !aiWebView.IsAgentBridgeReady;
		if (!flag)
		{
			flag = !(await aiWebView.SwitchConversationAsync(conversationId));
		}
		if (flag)
		{
			return false;
		}
		SetAIConversationTitle(title);
		ApplyAIPageState("conversation", conversationId);
		_aiWorkspaceView?.HideDetail();
		return true;
	}

	private async Task RefreshAIConversationTitleAsync(string conversationId)
	{
		AIWorkspaceView workspace = _aiWorkspaceView;
		if (workspace != null && !string.IsNullOrWhiteSpace(conversationId))
		{
			await workspace.RefreshConversationsAsync();
			string conversationTitle = workspace.GetConversationTitle(conversationId);
			if (workspace.IsCurrentConversation(conversationId) && !string.IsNullOrWhiteSpace(conversationTitle))
			{
				SetAIConversationTitle(conversationTitle);
			}
		}
	}

	private void OnAIFloatingWindowDockRequested(object sender, EventArgs e)
	{
		SwitchModule(DisplayModule.AI, force: true, click: true);
	}

	private async Task<bool> OnAIConversationSubmitRequested(string content, string messageId)
	{
		if (!HasAIAssistantPrivacyConsent())
		{
			return false;
		}
		AIWebAppView aiWebView = _aiWebViews.ActiveWebView;
		if (aiWebView == null)
		{
			return false;
		}
		await aiWebView.EnsureInitializedAsync();
		bool flag = aiWebView.IsAgentBridgeReady;
		if (flag)
		{
			flag = await aiWebView.SubmitMessageAsync(content, messageId);
		}
		bool num = flag;
		if (num)
		{
			ApplyAIPageState("conversation", _aiWebViews.SharedConversationId);
		}
		return num;
	}

	private async Task<bool> OnAIConversationStopRequested()
	{
		if (!HasAIAssistantPrivacyConsent())
		{
			return false;
		}
		AIWebAppView activeWebView = _aiWebViews.ActiveWebView;
		bool flag = activeWebView?.IsAgentBridgeReady ?? false;
		if (flag)
		{
			flag = await activeWebView.StopGeneratingAsync();
		}
		return flag;
	}

	private async Task<bool> OnAIScrollToBottomRequested()
	{
		AIWebAppView activeWebView = _aiWebViews.ActiveWebView;
		bool flag = activeWebView?.IsAgentBridgeReady ?? false;
		if (flag)
		{
			flag = await activeWebView.ScrollToBottomAsync();
		}
		return flag;
	}

	private async Task<bool> OnAIApprovalResponseRequested(string conversationId, string messageId, string decision, bool approveRemaining)
	{
		AIWebAppView activeWebView = _aiWebViews.ActiveWebView;
		bool flag = activeWebView?.IsAgentBridgeReady ?? false;
		if (flag)
		{
			flag = await activeWebView.RespondToApprovalAsync(conversationId, messageId, decision, approveRemaining);
		}
		return flag;
	}

	private async Task<bool> OnAIRatingSubmissionRequested(AgentRatingSubmission submission)
	{
		AIWebAppView activeWebView = _aiWebViews.ActiveWebView;
		bool flag = activeWebView?.IsAgentBridgeReady ?? false;
		if (flag)
		{
			flag = await activeWebView.SubmitRatingAsync(submission);
		}
		return flag;
	}

	private void OnAIAppUpdateRequested()
	{
		CheckUpdate();
	}

	private void SetAIConversationTitle(string title)
	{
		_aiWorkspaceView?.SetConversationTitle(title);
		_aiFloatingWindow?.SetConversationTitle(title);
	}

	private async void OnWindowInitialized()
	{
		base.Loaded += OnMainWindowLoaded;
		await Task.Delay(10);
		InitViews();
		InitShortcut();
	}

	public async Task HandlePipelineMsg(string text)
	{
		if (await AppLockCache.GetAppLocked())
		{
			AppUnlockWindow.TryUnlockApp(Navigate);
		}
		else
		{
			Navigate();
		}
		void Navigate()
		{
			switch (text)
			{
			case "Show":
				TryShowMainWindow();
				break;
			case "add_task":
				OpenAddTaskWindow();
				break;
			case "task_show_calendar":
				ShowWindow();
				SwitchModule(DisplayModule.Calendar);
				break;
			case "task_show_today":
				SwitchModule(DisplayModule.Task);
				ListView?.ProjectList.SelectToday();
				ShowWindow();
				break;
			case "task_show_inbox":
				SwitchModule(DisplayModule.Task);
				ListView?.ProjectList.SelectInbox();
				ShowWindow();
				break;
			case "other_add_list_widget":
				App.AddProjectWidget();
				break;
			case "other_add_calendar_widget":
				App.Instance.AddCalendarWidget();
				break;
			case "other_remove_calendar_widget":
				CalendarWidgetHelper.CloseWidget();
				break;
			case "other_add_matrix_widget":
				App.Instance.AddMatrixWidget();
				break;
			case "other_remove_matrix_widget":
				MatrixWidgetHelper.CloseWidget();
				break;
			case "other_more_widgets":
				SettingDialog.ShowSettingDialog(SettingsType.Widget);
				break;
			case "other_add_ai_widget":
				AIWindow.ShowDesktopWidget();
				break;
			case "other_remove_ai_widget":
				AIWindow.CloseDesktopWidget();
				break;
			case "other_open_focus_widget":
				TickFocusManager.HideOrShowFocusWidget(force: true);
				break;
			case "other_show_pomo":
			case "other_start_pomo":
				UserActCollectUtils.AddClickEvent("focus", "start_from", "dock");
				TickFocusManager.BeginFocus(isPomo: true, forceShowWidget: true);
				break;
			case "other_start_timing":
			case "other_show_timing":
				UserActCollectUtils.AddClickEvent("focus", "start_from", "dock");
				TickFocusManager.BeginFocus(isPomo: false, forceShowWidget: true);
				break;
			default:
				NavigateProject(text);
				break;
			}
		}
	}

	private void NavigateProject(string text)
	{
		if (text != null && text.Contains(":"))
		{
			string[] array = text.Split(':');
			if (array.Length == 2)
			{
				string type = array[0];
				string id = array[1];
				NavigateProject(type, id);
			}
		}
		ShowWindow();
	}

	public void NavigateTodayProject()
	{
		TryShowMainWindow();
		SwitchModule(DisplayModule.Task);
		ListView?.ProjectList.TrySelectSmartProject(SmartListType.Today);
	}

	public void NavigateTomorrowProject()
	{
		TryShowMainWindow();
		SwitchModule(DisplayModule.Task);
		ListView?.ProjectList.TrySelectSmartProject(SmartListType.Tomorrow);
	}

	public void NavigateSmartListProject(SmartListType type)
	{
		TryShowMainWindow();
		SwitchModule(DisplayModule.Task);
		ListView?.ProjectList.TrySelectSmartProject(type);
	}

	public void OpenAddTaskWindow()
	{
		AddTaskWindow.ShowWindow(null, ProjectIdentity.GetDefaultProject());
	}

	public string GetThemeId()
	{
		return "light";
	}

	public async void TaskDeleted(string taskId)
	{
		TaskModel taskModel = await TaskDao.GetTaskById(taskId);
		if (taskModel != null)
		{
			List<TaskModel> taskModels = new List<TaskModel> { taskModel };
			List<TaskModel> list = await TaskService.GetAllSubTasksByIdAsync(taskModel.id, taskModel.projectId);
			if (list != null && list.Any())
			{
				taskModels.AddRange(list);
			}
			await TaskService.BatchDeleteTasks(taskModels);
		}
		TryFocus();
	}

	public void ToastDeleteRecUndo(List<TaskDeleteRecurrenceUndoEntity> undoModels)
	{
		if (undoModels != null && undoModels.Any())
		{
			UndoToast undoToast = new UndoToast();
			undoToast.InitTaskUndo(undoModels);
			Toast(undoToast);
		}
	}

	public void Toast(FrameworkElement uiElement)
	{
		WindowToastHelper.ShowAndHideToast(ToastGrid, uiElement);
		TryFocus();
	}

	public void TryToastString(object obj, string toastString)
	{
		WindowToastHelper.ToastString(ToastGrid, toastString);
	}

	public async Task<bool> BatchDeleteTask(List<TaskModel> tasks)
	{
		if (await TaskService.BatchDeleteTasks(tasks))
		{
			HideDetailOnTasksDelete(tasks?.Select((TaskModel t) => t.id).ToList());
			return true;
		}
		return false;
	}

	public void TaskComplete(CloseUndoToast undo)
	{
		WindowToastHelper.ShowAndHideToast(ToastGrid, undo);
		TryFocus();
	}

	public void TryHideToast()
	{
		if (ToastGrid.Children.Count > 0)
		{
			if (ToastGrid.Children[0] is UndoToast undoToast)
			{
				undoToast.OnFinished();
			}
			ToastGrid.Children[0].Visibility = Visibility.Collapsed;
		}
	}

	private void HideDetailOnTasksDelete(List<string> taskIds)
	{
		if (taskIds != null && taskIds.Count != 0)
		{
			ListView?.HideDetailOnTasksDelete(taskIds);
		}
	}

	private async void DelaySetSize()
	{
		await Task.Delay(200);
		SafeSetWindowSize(refresh: true, delayRefresh: true);
	}

	private async void InitViews()
	{
		HotKeyUtils.ResignHotKey();
		InitNotifyIcon();
		CheckResetPassword();
		base.Title = Utils.GetAppName();
		SystemEvents.SessionSwitch -= OnPowerModeChanged;
		SystemEvents.SessionSwitch += OnPowerModeChanged;
		SystemEvents.UserPreferenceChanged -= OnSystemUserPreferenceChanged;
		SystemEvents.UserPreferenceChanged += OnSystemUserPreferenceChanged;
		SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
		SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
	}

	private async void OnDisplaySettingsChanged(object sender, EventArgs e)
	{
		await Task.Delay(5000);
		try
		{
			string text = "OnSystemDisplayChanged ";
			Screen[] allScreens = Screen.AllScreens;
			foreach (Screen screen in allScreens)
			{
				text = text + screen.Bounds.ToString() + ",";
			}
			UtilLog.Info(text + "left : " + base.Left + ",top " + base.Top);
			EnsureWindowVisible();
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
		}
	}

	private void OnSystemUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
	{
		if (LocalSettings.Settings.ExtraSettings.UseSystemTheme)
		{
			ThemeUtil.TrySetSystemTheme();
		}
	}

	private async void OnPowerModeChanged(object sender, SessionSwitchEventArgs e)
	{
		try
		{
			App.Instance.SetNotifyIcon(isLock: false);
		}
		catch (Exception ex)
		{
			UtilLog.Info("OnPowerModeChanged:" + ex.Message);
		}
		await Task.Delay(3000);
		RefreshUi();
		UpdateLayout();
		await Task.Delay(2000);
		RefreshUi();
		UpdateLayout();
	}

	private static void CheckResetPassword()
	{
		if (LocalSettings.Settings.NeedResetPassword && LocalSettings.Settings.NeedResetUserId == LocalSettings.Settings.LoginUserId)
		{
			LocalSettings.Settings.NeedResetPassword = false;
			LocalSettings.Settings.NeedResetUserId = string.Empty;
			new SetPasswordWindow(SetPasswordMode.Reset).ShowDialog();
		}
	}

	private async void InitNotifyIcon()
	{
		bool notifyIcon = await AppLockCache.GetAppLocked(isStart: true);
		App.NotifyIcon.Text = Utils.GetAppName();
		App.Instance.SetNotifyIcon(notifyIcon);
		App.NotifyIcon.Visible = true;
		App.NotifyIcon.MouseClick += OnAppIconClick;
		System.Windows.Forms.MenuItem[] menuItems = new System.Windows.Forms.MenuItem[0];
		App.NotifyIcon.ContextMenu = new System.Windows.Forms.ContextMenu(menuItems);
		App.NotifyIcon.ContextMenu.Collapse += OnMenuClosed;
		App.NotifyIcon.ContextMenu.Popup += OnMenuShowed;
	}

	private async void OnMenuShowed(object sender, EventArgs e)
	{
		await Task.Delay(150);
		if (_iconMenuShowing)
		{
			_iconMenuShowing = false;
		}
		else
		{
			ShowIconMenu();
		}
	}

	private async void ShowIconMenu()
	{
		if (_notifyIconMenu == null || !_notifyIconMenu.IsOpen)
		{
			_notifyIconMenu = await CreateNotifyMenu();
			_notifyIconMenu.IsOpen = true;
		}
	}

	private async Task<System.Windows.Controls.ContextMenu> CreateNotifyMenu()
	{
		System.Windows.Controls.ContextMenu menu = new System.Windows.Controls.ContextMenu
		{
			StaysOpen = false,
			Width = 240.0,
			MaxWidth = 300.0,
			VerticalOffset = -10.0,
			HorizontalOffset = -120.0,
			UseLayoutRounding = true
		};
		menu.SetResourceReference(FrameworkElement.StyleProperty, "ContextMenuStyle");
		menu.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "PopupBackground");
		if (!((await AppLockCache.GetModel())?.Locked ?? false))
		{
			AppOptionItem appOptionItem = GetItem("AddDesktopWidgets", AppOption.Null, "Ic.Setting.Widget");
			appOptionItem.PreviewMouseLeftButtonUp += TryResetWindowPosition;
			AppOptionItem appOptionItem2 = GetItem("Calendar", AppOption.AddCalendarWidget, null);
			appOptionItem2.IsEnabled = CalendarWidgetHelper.CanAddProject();
			AppOptionItem appOptionItem3 = GetItem("Matrix", AppOption.AddMatrixWidget, null);
			appOptionItem3.IsEnabled = MatrixWidgetHelper.CanAddWidget();
			AppOptionItem appOptionItem4 = GetItem("CountdownTitle", AppOption.Null, null);
			AppOptionItem newItem = GetItem("CardStyle", AppOption.AddSingleCountdownWidget, null);
			AppOptionItem appOptionItem5 = GetItem("ListView", AppOption.AddListCountdownWidget, null);
			appOptionItem5.IsEnabled = CountdownWidgetsHelper.CanAddListWidget();
			appOptionItem4.Items.Add(newItem);
			appOptionItem4.Items.Add(appOptionItem5);
			AppOptionItem appOptionItem6 = GetItem("Habit", AppOption.Null, null);
			AppOptionItem newItem2 = GetItem("TodayHabit", AppOption.AddHabitItemWidget, null);
			AppOptionItem appOptionItem7 = GetItem("HabitList", AppOption.AddHabitListWidget, null);
			AppOptionItem newItem3 = GetItem("HabitWeekTracking", AppOption.AddHabitWeekWidget, null);
			appOptionItem7.IsEnabled = HabitWidgetHelper.CanAddListWidget();
			appOptionItem6.Items.Add(newItem2);
			appOptionItem6.Items.Add(appOptionItem7);
			appOptionItem6.Items.Add(newItem3);
			menu.Items.Add(appOptionItem);
			appOptionItem.Items.Add(GetItem("Task", AppOption.AddListWidget, null));
			appOptionItem.Items.Add(appOptionItem2);
			appOptionItem.Items.Add(appOptionItem3);
			appOptionItem.Items.Add(appOptionItem6);
			appOptionItem.Items.Add(appOptionItem4);
			if (LocalSettings.Settings.EnableAI)
			{
				AppOptionItem appOptionItem8 = GetItem("AIAssistant", AppOption.AddAIWidget, null);
				appOptionItem8.IsEnabled = AIWindow.DesktopWidget == null;
				appOptionItem.Items.Add(appOptionItem8);
			}
			AddSeparator();
			if (LocalSettings.Settings.EnableFocus)
			{
				AppOptionItem appOptionItem9 = GetItem("BeginFocus", AppOption.StartFocus, "Ic.ProjectList.Focus");
				appOptionItem9.Shortcut = LocalSettings.Settings.PomoShortcut;
				menu.Items.Add(appOptionItem9);
				if (TickFocusManager.Working)
				{
					AppOptionItem newItem4 = GetItem((LocalSettings.Settings.PomoType == FocusConstance.Focus) ? "ShowPomo" : "ShowTiming", (LocalSettings.Settings.PomoType == FocusConstance.Focus) ? AppOption.StartPomo : AppOption.StartTiming, null);
					appOptionItem9.Items.Add(newItem4);
				}
				else
				{
					appOptionItem9.Items.Add(GetItem("StartPomo", AppOption.StartPomo, null));
					appOptionItem9.Items.Add(GetItem("StartTiming", AppOption.StartTiming, null));
				}
				AddSeparator();
			}
			AppOptionItem appOptionItem10 = (LocalSettings.Settings.MainWindowTopmost ? GetItem("CancelAboveOthers", AppOption.Unpin, "Ic.ProjectList.CancelTopmost") : GetItem("KeepAboveOthers", AppOption.Pin, "Ic.ProjectList.Topmost"));
			appOptionItem10.Shortcut = LocalSettings.Settings.ShortcutPin;
			menu.Items.Add(appOptionItem10);
			AppOptionItem appOptionItem11 = GetItem("LockApp", AppOption.Lock, "Ic.Common.Lock");
			appOptionItem11.Shortcut = LocalSettings.Settings.LockShortcut;
			menu.Items.Add(appOptionItem11);
			AddSeparator();
		}
		else
		{
			AppOptionItem appOptionItem12 = GetItem("UnLockApp", AppOption.Unlock, "Ic.Common.Unlock");
			appOptionItem12.Shortcut = LocalSettings.Settings.LockShortcut;
			menu.Items.Add(appOptionItem12);
		}
		menu.Items.Add(GetItem("Exit", AppOption.Exit, "Ic.ProjectList.Exit"));
		return menu;
		void AddSeparator()
		{
			Separator separator = new Separator();
			separator.SetResourceReference(FrameworkElement.StyleProperty, "MenuSeparatorStyle");
			menu.Items.Add(separator);
		}
		AppOptionItem GetItem(string titleKey, AppOption tag, string iconKey)
		{
			AppOptionItem appOptionItem13 = new AppOptionItem();
			appOptionItem13.Title = Utils.GetString(titleKey);
			appOptionItem13.Tag = tag;
			appOptionItem13.PreviewMouseLeftButtonUp += OnOptionClick;
			appOptionItem13.SetIcon(iconKey);
			return appOptionItem13;
		}
	}

	private async void TryResetWindowPosition(object sender, MouseButtonEventArgs e)
	{
		_resetClickTimes++;
		if (_resetClickTimes >= 4)
		{
			base.Left = 100.0;
			base.Top = 100.0;
			CalendarWidgetHelper.ResetPosition();
			MatrixWidgetHelper.ResetPosition();
			ProjectWidgetsHelper.ResetPosition();
			CountdownWidgetsHelper.ResetPosition();
			HabitWidgetHelper.ResetPosition();
			_resetClickTimes = 0;
		}
		await Task.Delay(1600);
		_resetClickTimes = 0;
	}

	private void OnMenuClosed(object sender, EventArgs e)
	{
		if (_notifyIconMenu != null)
		{
			_notifyIconMenu.IsOpen = false;
			_notifyIconMenu = null;
		}
	}

	private async void OnOptionClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is AppOptionItem { Tag: var tag } && tag is AppOption appOption)
		{
			if (_notifyIconMenu != null && appOption != AppOption.Null)
			{
				_notifyIconMenu.IsOpen = false;
				_notifyIconMenu = null;
			}
			switch (appOption)
			{
			case AppOption.AddListWidget:
				App.AddProjectWidget();
				break;
			case AppOption.AddCalendarWidget:
				App.Instance.AddCalendarWidget();
				break;
			case AppOption.CloseCalendarWidget:
				App.Instance.CloseCalendarWidget();
				break;
			case AppOption.AddMatrixWidget:
				App.Instance.AddMatrixWidget();
				break;
			case AppOption.AddAIWidget:
				AIWindow.ShowDesktopWidget();
				break;
			case AppOption.CloseAIWidget:
				AIWindow.CloseDesktopWidget();
				break;
			case AppOption.CloseMatrixWidget:
				App.Instance.CloseMatrixWidget();
				break;
			case AppOption.AddSingleCountdownWidget:
				App.Instance.AddSingleCountdownWidget();
				break;
			case AppOption.AddListCountdownWidget:
				App.Instance.AddListCountdownWidget();
				break;
			case AppOption.CloseListCountdownWidget:
				App.Instance.CloseListCountdownWidget();
				break;
			case AppOption.AddHabitItemWidget:
				App.Instance.AddHabitItemWidget();
				break;
			case AppOption.AddHabitListWidget:
				App.Instance.AddHabitListWidget();
				break;
			case AppOption.AddHabitWeekWidget:
				App.Instance.AddHabitWeekWidget();
				break;
			case AppOption.CloseHabitListWidget:
				App.Instance.CloseListHabitWidget();
				break;
			case AppOption.StartFocus:
				TickFocusManager.HideOrShowFocusWidget(force: true);
				break;
			case AppOption.StartPomo:
				UserActCollectUtils.AddClickEvent("focus", "start_from", "dock");
				TickFocusManager.BeginFocus(isPomo: true);
				break;
			case AppOption.StartTiming:
				UserActCollectUtils.AddClickEvent("focus", "start_from", "dock");
				TickFocusManager.BeginFocus(isPomo: false);
				break;
			case AppOption.Pin:
				LocalSettings.Settings.MainWindowTopmost = true;
				break;
			case AppOption.Unpin:
				LocalSettings.Settings.MainWindowTopmost = false;
				break;
			case AppOption.Lock:
				App.Instance.TryLockApp();
				break;
			case AppOption.Unlock:
				TryShowMainWindow();
				break;
			case AppOption.Exit:
				SaveSettingsBeforeExit();
				await LocalSettings.Settings.Save();
				await Task.Delay(120);
				App.Instance.ExitApplication();
				break;
			case AppOption.ShowFocusWidget:
			case AppOption.Null:
				break;
			}
		}
	}

	private void SaveSettingsBeforeExit()
	{
		SmartListTaskFoldHelper.Save();
	}

	private void SaveLocation(object sender, EventArgs e)
	{
		Utils.RunOnUiThread(base.Dispatcher, delegate
		{
			if (base.WindowState != WindowState.Maximized)
			{
				LocalSettings.Settings.WindowLeft = base.Left;
				LocalSettings.Settings.WindowTop = base.Top;
				if (base.ActualWidth >= base.MinWidth)
				{
					LocalSettings.Settings.MainWindowWidth = base.ActualWidth;
				}
				if (base.ActualHeight > 0.0 && base.ActualWidth > 0.0)
				{
					LocalSettings.Settings.MainWindowHeight = base.ActualHeight;
				}
			}
		});
	}

	public void OnEnterImmersive(string taskId, int caretIndex)
	{
		EnterImmersiveMode(taskId, caretIndex);
	}

	public async Task NavigateTask(ProjectTask task)
	{
		if (await TaskUtils.TryLoadTask(task.TaskId, task.ProjectId) != null)
		{
			if (_navigateWindow == null)
			{
				_navigateWindow = new TaskDetailWindow();
				_navigateWindow.ShowInNavigate(task.TaskId, App.Window);
				_navigateWindow.DependentWindow = this;
			}
			else
			{
				_navigateWindow?.OnNavigateTask(task);
			}
			_ignoreFocus = true;
			await Task.Delay(200);
			_ignoreFocus = false;
		}
	}

	public void NavigateCourse(string courseId)
	{
		if (!string.IsNullOrEmpty(courseId))
		{
			SwitchModule(DisplayModule.Task);
			ListView?.NavigateTodayProject();
			ListView?.GetTaskView()?.TaskSelect(new ListItemSelectModel(courseId, string.Empty, DisplayType.Course, TaskSelectType.Click));
		}
	}

	public async void NavigateHabit(string habitId, bool showRecord = false)
	{
		if (!string.IsNullOrEmpty(habitId) && await HabitDao.GetHabitById(habitId) != null)
		{
			SwitchModule(DisplayModule.Habit);
			HabitView?.NavigateItem(habitId);
			if (showRecord)
			{
				HabitView?.CheckInHabit(habitId);
			}
		}
	}

	public async void NavigateEvent(string eventId)
	{
		if (string.IsNullOrEmpty(eventId))
		{
			return;
		}
		CalendarEventModel model = await CalendarEventDao.GetEventByEventId(eventId);
		if (model == null)
		{
			model = await CalendarEventDao.GetEventById(eventId);
		}
		if (model == null)
		{
			return;
		}
		CalendarSubscribeProfileModel calendarSubscribeProfileModel = CacheManager.GetSubscribeCalendars().FirstOrDefault((CalendarSubscribeProfileModel cal) => cal.Id == model.CalendarId);
		if (calendarSubscribeProfileModel != null)
		{
			SwitchModule(DisplayModule.Task);
			SubscribeCalendarProjectIdentity identity = new SubscribeCalendarProjectIdentity(calendarSubscribeProfileModel);
			ListView?.SelectProject(identity);
			await Task.Delay(100);
			ListView?.GetTaskView()?.NavigateEvent(eventId);
			return;
		}
		BindCalendarModel bindCalendar = CacheManager.GetBindCalendars().FirstOrDefault((BindCalendarModel cal) => cal.Id == model.CalendarId);
		if (bindCalendar != null)
		{
			BindCalendarAccountModel bindCalendarAccountModel = CacheManager.GetBindCalendarAccounts().FirstOrDefault((BindCalendarAccountModel acc) => acc.Id == bindCalendar.AccountId);
			if (bindCalendarAccountModel != null)
			{
				SwitchModule(DisplayModule.Task);
				BindAccountCalendarProjectIdentity identity2 = new BindAccountCalendarProjectIdentity(bindCalendarAccountModel);
				ListView?.SelectProject(identity2);
				await Task.Delay(100);
				ListView?.GetTaskView()?.NavigateEvent(eventId);
			}
		}
	}

	public async Task NavigateNormalProject(string projectId)
	{
		SaveSelectedProject("project:" + projectId);
		SwitchModule(DisplayModule.Task);
		ListView?.SelectProject(ProjectIdHelper.GetProjectIdentity(projectId));
		if (await AppLockCache.GetAppLocked())
		{
			AppUnlockWindow.TryUnlockApp(ShowWindow);
		}
		else
		{
			ShowWindow();
		}
	}

	public async Task NavigateColumn(string projectId, string columnId)
	{
		if (!string.IsNullOrEmpty(projectId) && !string.IsNullOrEmpty(columnId))
		{
			SaveSelectedProject("project:" + projectId);
			SwitchModule(DisplayModule.Task);
			ListView?.SelectTaskGroup(projectId, columnId);
			if (await AppLockCache.GetAppLocked())
			{
				AppUnlockWindow.TryUnlockApp(ShowWindow);
			}
			else
			{
				ShowWindow();
			}
		}
	}

	public async Task EditProject(string projectId)
	{
		ListView?.EditProject(projectId);
	}

	public async Task NavigateFilter(string filterId)
	{
		SwitchModule(DisplayModule.Task);
		ListView?.SelectFilter(filterId);
		if (await AppLockCache.GetAppLocked())
		{
			AppUnlockWindow.TryUnlockApp(ShowWindow);
		}
		else
		{
			ShowWindow();
		}
	}

	public async Task NavigateGroup(string groupId)
	{
		SwitchModule(DisplayModule.Task);
		ListView?.SelectGroup(groupId);
		if (await AppLockCache.GetAppLocked())
		{
			AppUnlockWindow.TryUnlockApp(ShowWindow);
		}
		else
		{
			ShowWindow();
		}
	}

	public async Task NavigateProject(string type, string id)
	{
		if (LocalSettings.Settings.InSearch)
		{
			App.Window.StopSearch(forceSwitch: false);
		}
		LocalSettings.Settings.SelectProjectId = type + ":" + id;
		SwitchModule(DisplayModule.Task);
		ListView?.NavigateProject(type, id);
		if (await AppLockCache.GetAppLocked())
		{
			AppUnlockWindow.TryUnlockApp(ShowWindow);
		}
		else
		{
			ShowWindow();
		}
	}

	public async void NavigateTask(string taskId, string itemId)
	{
		TaskModel taskModel = await TaskDao.GetThinTaskById(taskId);
		if (taskModel != null && taskModel.deleted != 2)
		{
			SwitchModule(DisplayModule.Task);
			ListView?.NavigateTask(taskModel);
		}
	}

	public void ReloadView(bool restore = false)
	{
		if (restore)
		{
			SwitchDisplayModule(isReload: true);
		}
		else if (ShowList)
		{
			ListViewContainer listView = ListView;
			if (listView != null && listView.Mode == ListMode.Normal)
			{
				ListView?.ReloadView();
			}
		}
	}

	public void Reload()
	{
		SwitchDisplayModule(isReload: true);
	}

	private void SwitchDisplayModule(bool isReload = false)
	{
		DisplayModule displayModule = (DisplayModule)LocalSettings.Settings.MainWindowDisplayModule;
		if (displayModule == DisplayModule.AI && Utils.IsWindows7())
		{
			displayModule = DisplayModule.Task;
			LocalSettings.Settings.MainWindowDisplayModule = (int)displayModule;
		}
		_aiClientHostState.SetMainModuleIsAI(displayModule == DisplayModule.AI);
		LeftTabBar.SelectedModule = displayModule.ToString();
		OnSwitchModule();
		if (displayModule != DisplayModule.Task)
		{
			OnModuleSwitch?.Invoke(displayModule);
		}
		switch (displayModule)
		{
		case DisplayModule.Task:
			CLGrid.Visibility = Visibility.Visible;
			LoadTaskView(isReload);
			break;
		case DisplayModule.Calendar:
			CLGrid.Visibility = Visibility.Visible;
			LoadCalendar();
			if (!UserDao.IsUserValid())
			{
				ProChecker.ShowUpgradeDialog(ProType.CalendarView, App.Window);
			}
			else
			{
				UserActCollectUtils.AddClickEvent("calendar", "view_show", LocalSettings.Settings.CalendarDisplaySettings.MainCal.GetSwitchText());
			}
			switch (LocalSettings.Settings?.UserPreference?.CalendarConfig?.ItemStyleType)
			{
			case CalendarConfig.CalendarItemStyle.Classic:
				UserActCollectUtils.AddClickEvent("calendar", "item_style", "classic");
				break;
			case CalendarConfig.CalendarItemStyle.Modern:
				UserActCollectUtils.AddClickEvent("calendar", "item_style", "modern");
				break;
			}
			break;
		case DisplayModule.Matrix:
			CLGrid.Visibility = Visibility.Collapsed;
			LoadMatrix();
			break;
		case DisplayModule.Habit:
			CLGrid.Visibility = Visibility.Collapsed;
			SwitchHabit();
			break;
		case DisplayModule.Pomo:
			CLGrid.Visibility = Visibility.Collapsed;
			SwitchFocus();
			break;
		case DisplayModule.CountDown:
			CLGrid.Visibility = Visibility.Collapsed;
			SwitchCountDown();
			CountdownSyncService.SyncCountdowns();
			break;
		case DisplayModule.AI:
			CLGrid.Visibility = Visibility.Collapsed;
			SwitchAI();
			break;
		}
		ChangeHeaderButtonColor();
	}

	private void OnSwitchModule()
	{
		ProChecker.TryCloseCalendarProDialog();
		BottomContent.Content = null;
		if (MainCalendar != null)
		{
			MainCalendar.IsVirtual = false;
		}
		ListView?.GetTaskView()?.ExitVirtual();
	}

	private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
	{
		IsLoaded = true;
		base.Loaded -= OnMainWindowLoaded;
		base.Width += 1.0;
		CheckUpdateStart();
		UserActCollectUtils.AddClickEvent("dotnet", "version", Utils.GetDotNetVersion());
		PresentationSource presentationSource = PresentationSource.FromVisual(this);
		((HwndSource)presentationSource)?.AddHook(Hook);
		if (TouchPadHelper.TouchpadHelper.Exists())
		{
			TouchPadHelper.TouchpadHelper.RegisterInput(((HwndSource)presentationSource).Handle);
		}
		Utils.LogActionTimes(delegate
		{
			OnWindowLoaded();
		}, 300, null, "MainWindow.OnMainWindowLoaded.OnWindowLoaded");
		base.Width -= 1.0;
		base.Dispatcher.BeginInvoke(new Action(WarmUpAIFloatingClient), DispatcherPriority.ApplicationIdle);
	}

	private async Task OnWindowLoaded()
	{
		AvatarHelper.GetAllProjectAvatars();
		LimitCache.InitLimitCache();
		LeftTabBar.SelectedModule = ((DisplayModule)LocalSettings.Settings.MainWindowDisplayModule/*cast due to .constrained prefix*/).ToString();
		CheckVersionValid();
	}

	private async Task CheckVersionValid(bool showPrompt = true)
	{
		if (!Utils.IsTickPackage() && DateTime.Today <= new DateTime(2026, 5, 30) && LocalSettings.Settings.ExtraSettings.WeiboLoginWarnDate < DateTime.Today)
		{
			LocalSettings.Settings.ExtraSettings.WeiboLoginWarnDate = DateTime.Today;
			UserInfoModel userInfoModel = await UserManager.GetUserInfo();
			if (userInfoModel.fakedEmail)
			{
				UserModel userModel = await UserDao.GetUserModelByName(userInfoModel.username);
				if (userModel != null && string.IsNullOrEmpty(userModel.phone))
				{
					List<ThirdSiteBindType> list = await Communicator.GetAllThirdSiteBindTypes();
					if (list.Count == 1 && list.Contains(ThirdSiteBindType.Weibo))
					{
						new MessageInfoWindow(Utils.GetString("WeiboLoginDiscontinued"), Utils.GetString("WeiboLoginDiscontinuedDesc"), Utils.GetString("LinkBind"), Utils.GetString("GotIt"), this, showClose: true, delegate
						{
							Utils.TryProcessStartUrlWithToken("https://dida365.com/webapp/#settings/profile");
						}, delegate
						{
							Activate();
						}).Show();
					}
				}
			}
			LocalSettings.Settings.Save();
		}
		if (UserDao.IsPro())
		{
			if (!showPrompt)
			{
				return;
			}
			if (UserManager.InGraceDate())
			{
				bool flag = false;
				if (UserManager.IsLastGraceDate() && LocalSettings.Settings.ExtraSettings.GraceDateNotifyDate < ticktick_WPF.Util.DateUtils.GetDateNum(DateTime.Today))
				{
					flag = true;
				}
				else if (LocalSettings.Settings.ExtraSettings.GraceDateNotifyDate <= ticktick_WPF.Util.DateUtils.GetDateNum(DateTime.Today.AddDays(-7.0)))
				{
					flag = true;
				}
				if (flag)
				{
					LocalSettings.Settings.ExtraSettings.GraceDateNotifyDate = ticktick_WPF.Util.DateUtils.GetDateNum(DateTime.Today);
					await Task.Delay(2000);
					ShowGraceWindow();
				}
			}
			if (!(LocalSettings.Settings.ExtraSettings.DuplicateSubsNotifyDate != DateTime.Today))
			{
				return;
			}
			UserModel user = await UserManager.GetCurrentUser();
			if (user.errorType != 2)
			{
				return;
			}
			string text = Utils.GetString("DuplicateSubscriptionContent");
			if (App.Ci.TwoLetterISOLanguageName == "zh")
			{
				text = text.Replace(" {0} ", "{0}");
			}
			IconTextButtonWindow iconTextButtonWindow = new IconTextButtonWindow(Utils.GetString("DuplicateSubscriptions"), "DuplicateSubsDrawingImage", string.Format(text, Utils.GetString("ManageSubscriptions")), Utils.GetString("ManageSubscriptions"), async delegate
			{
				if (user != null && user.subscribeType != "order")
				{
					ManageProSubscribeWindow manageProSubscribeWindow = new ManageProSubscribeWindow(await Communicator.GetSubscribeList());
					manageProSubscribeWindow.Owner = this;
					manageProSubscribeWindow.ShowDialog();
				}
				else
				{
					Utils.TryProcessStartUrlWithToken("webapp/#settings/premium");
				}
			});
			iconTextButtonWindow.Owner = this;
			iconTextButtonWindow.Show();
			LocalSettings.Settings.ExtraSettings.DuplicateSubsNotifyDate = DateTime.Today;
			return;
		}
		await Task.Delay(2000);
		if (_updateVersionShowed || !(UserManager.ProExpired() && showPrompt))
		{
			return;
		}
		try
		{
			if (DateTime.ParseExact(LocalSettings.Settings.LastProRemindeTime, "yyyy MM dd", null) < DateTime.Today)
			{
				ShowProExpired();
			}
		}
		catch (Exception)
		{
			ShowProExpired();
		}
	}

	private async Task ShowCountdownIntro()
	{
		await FeatureInfoWindow.Show(Utils.GetString("CountdownTitle"), Utils.GetString("countdown_guide_desc"), null, "MoreCountdowns" + (Utils.IsCn() ? "_cn" : "_en"), Utils.GetString("EnjoyNow"), delegate
		{
			LocalSettings.Settings.ShowOrHideTabBar("COUNTDOWN", show: true);
			CountdownSyncService.SyncCountdowns();
			App.NavigateCountdown();
		}, isGif: false);
	}

	private void ShowGraceWindow()
	{
		DateTime? userGraceDate = UserManager.GetUserGraceDate();
		if (userGraceDate.HasValue)
		{
			int val = (int)(DateTime.Now.Date - userGraceDate.Value.Date).TotalDays;
			SubscriptionGraceDateWindow subscriptionGraceDateWindow = new SubscriptionGraceDateWindow((UserManager.GetSubscribeType() == "apple") ? "Apple" : "Google", Math.Max(1, val));
			subscriptionGraceDateWindow.Owner = this;
			subscriptionGraceDateWindow.Show();
		}
	}

	public async Task TryShowTutorial()
	{
		string abTest = await ABTestManager.GetABTestGroupResult("newuser3_202403");
		IsInTutorial = true;
		if (!ABTestManager.IsAGroup(abTest))
		{
			await TaskService.PullGuideTask();
			ShowTutorial();
			return;
		}
		ShowNewUserGuide();
		string text = await ProjectDao.PullGuideProject(abTest);
		if (!string.IsNullOrEmpty(text))
		{
			LocalSettings.Settings.SelectProjectId = "project:" + text;
			ListView?.ProjectList?.LoadSavedProject();
		}
	}

	public void ShowNewUserGuide()
	{
		WindowUtils.SetWindowChromeBorderThickness(this, new Thickness(0.0));
		base.ResizeMode = ResizeMode.NoResize;
		ResizeButtons.Visibility = Visibility.Hidden;
		base.WindowState = WindowState.Normal;
		base.Width = 1140.0;
		base.Height = 780.0;
		NewUserGuide newUserGuide = new NewUserGuide();
		newUserGuide.SetValue(Grid.ColumnSpanProperty, 2);
		newUserGuide.SetValue(Grid.RowSpanProperty, 2);
		newUserGuide.SetValue(System.Windows.Controls.Panel.ZIndexProperty, 5);
		Container.Children.Add(newUserGuide);
	}

	public void RemoveNewUserGuide(NewUserGuide guide)
	{
		LeftTabBar.SetItems();
		Container.Children.Remove(guide);
		WindowUtils.SetWindowChromeBorderThickness(this, new Thickness(5.0, 5.0, 2.0, 5.0));
		base.ResizeMode = ResizeMode.CanResize;
		ResizeButtons.Visibility = Visibility.Visible;
		IsInTutorial = false;
		Communicator.PullAppConfig(fromLogin: false, ignoreEtag: true);
	}

	private void ShowProExpired()
	{
		if (_upgradeShowing)
		{
			return;
		}
		if (!_lastShowUpdateTime.HasValue)
		{
			_lastShowUpdateTime = DateTime.Now;
		}
		else
		{
			if ((DateTime.Now - _lastShowUpdateTime.Value).TotalSeconds < 10.0)
			{
				return;
			}
			_lastShowUpdateTime = DateTime.Now;
		}
		_upgradeShowing = true;
		ShowProExpiredDialog();
		_upgradeShowing = false;
	}

	private async void ShowProExpiredDialog()
	{
		if (!UserDao.IsPro() && !UserManager.IsTeamActive() && !LocalSettings.Settings.DontShowProWindow)
		{
			List<ProjectModel> projects = (from p in CacheManager.GetProjects()
				where !p.closed.HasValue || !p.closed.Value
				select p).ToList();
			ListOverLimitsModel listOverLimitsModel = new ListOverLimitsModel
			{
				ProjectNum = projects.Count
			};
			ListOverLimitsModel listOverLimitsModel2 = listOverLimitsModel;
			listOverLimitsModel2.TaskNum = await ProjectDao.GetTaskOverLimitProjectNum();
			listOverLimitsModel.ShareNum = projects.Where((ProjectModel p) => p.IsShareList() && p.isOwner).ToList().Count;
			new ProReminderWindow(listOverLimitsModel).Show();
			_proDialogShowed = true;
			LocalSettings.Settings.LastProRemindeTime = DateTime.Today.ToString("yyyy MM dd");
		}
	}

	private void InitLock()
	{
		_lockHandler = new System.Timers.Timer(5000.0);
		_lockHandler.Elapsed += CheckLocked;
	}

	private async void CheckLocked(object sender, ElapsedEventArgs e)
	{
		AppLockModel model = await AppLockCache.GetModel();
		if (model != null && await AppLockCache.IsLockValid())
		{
			int num = model.LockInterval * 1000 * 60;
			if (IdleTimeFinder.GetIdleTime() >= num)
			{
				App.Instance.LockAndHide();
			}
		}
	}

	public void ReleaseLock()
	{
		StopLock();
		_lockHandler = null;
	}

	public async void TryLock()
	{
		if (!SettingDialog.SettingShow())
		{
			if (_lockHandler == null)
			{
				InitLock();
			}
			_lockHandler.Stop();
			_lockHandler.Start();
		}
	}

	public void StopLock()
	{
		_lockHandler?.Stop();
	}

	public async void UnlockApp()
	{
		JumpHelper.InitJumpList();
		AppLockModel appLockModel = await AppLockCache.GetModel();
		WindowManager.AppLockOrExit = false;
		if (appLockModel != null)
		{
			if (appLockModel.LockWidget)
			{
				CalendarWidgetHelper.TryLoadWidget();
				MatrixWidgetHelper.TryLoadWidget();
				ProjectWidgetsHelper.LoadWidgets();
				CountdownWidgetsHelper.LoadAllWidgets();
				HabitWidgetHelper.LoadAllWidgets();
				TaskStickyWindow.TryLoadSavedStickies();
			}
			AIWindow.ShowDesktopWidget(force: false);
			IndependentWindow.Restore();
			if (appLockModel.LockAfter)
			{
				TryLock();
			}
		}
		App.Instance.SetNotifyIcon(isLock: false);
	}

	public async Task TryFocus()
	{
		await Task.Delay(10);
		Keyboard.ClearFocus();
		if (_ignoreFocus)
		{
			return;
		}
		if (ShowCalendar && !LocalSettings.Settings.InSearch)
		{
			MainCalendar?.GetFocus();
			return;
		}
		if (!PopupStateManager.CanShowAddPopup())
		{
			await Task.Delay(300);
			if (!PopupStateManager.CanShowAddPopup())
			{
				return;
			}
		}
		if (base.IsActive)
		{
			FocusManager.SetFocusedElement(this, this);
			Keyboard.Focus(this);
		}
	}

	public async Task TryShowMainWindow(bool force = false, bool delay = false, bool isStart = false)
	{
		if (force)
		{
			await ShowWindow(delay);
		}
		else if (await AppLockCache.GetAppLocked(isStart))
		{
			HideInInit();
			AppUnlockWindow.TryUnlockApp(ShowWindow);
		}
		else
		{
			await ShowWindow(delay);
		}
	}

	public async Task UnlockAndNavigateTask(string taskId, string itemId)
	{
		if (await AppLockCache.GetAppLocked())
		{
			AppUnlockWindow.TryUnlockApp(Navigate);
		}
		else
		{
			Navigate();
		}
		void Navigate()
		{
			ShowWindow();
			NavigateTask(taskId, itemId);
		}
	}

	public async Task ShowUndoToast(UndoToast undoToast)
	{
		WindowToastHelper.ShowAndHideToast(ToastGrid, undoToast);
		TryFocus();
	}

	public void HandleNeedUpgradeClient(string forceText)
	{
		Utils.RunOnUiThread(base.Dispatcher, delegate
		{
			NeedUpgradeClient(forceText);
		});
	}

	private async Task NeedUpgradeClient(string forceText)
	{
		if (!_versionOutdated)
		{
			_versionOutdated = true;
			UpdateModel updateModel = await UpdateHelper.CheckUpdate();
			if (updateModel != null && updateModel.versionNum > Utils.VersionNumToDouble())
			{
				ShowNewVersionWindow(updateModel, force: true, this, forceText);
			}
		}
	}

	public async Task CheckLoginStatus(bool force = false)
	{
		bool isPro = UserDao.IsPro();
		await LeftTabBar.LoadUserInfo(force);
		await base.Dispatcher.InvokeAsync((Func<Task>)async delegate
		{
			if (_aiConversationView != null)
			{
				await _aiConversationView.RefreshGreetingAsync();
			}
		}).Task.Unwrap();
		bool isTeamPro = await TeamDao.IsTeamPro();
		bool isFreeUser = UserManager.IsFreeUser();
		int num = ((!isFreeUser) ? (await UserManager.GetProExpireDays()) : (-1));
		int proEndDays = num;
		bool currentIsPro = UserDao.IsPro();
		await base.Dispatcher.InvokeAsync(delegate
		{
			if (isTeamPro)
			{
				UpgradeButton.Visibility = Visibility.Collapsed;
			}
			else if (isFreeUser)
			{
				UpgradeButton.Visibility = Visibility.Collapsed;
			}
			else if (proEndDays <= 7 && proEndDays >= 0 && currentIsPro)
			{
				UpgradeButton.Visibility = Visibility.Collapsed;
			}
			else
			{
				UpgradeButton.Visibility = Visibility.Collapsed;
			}
			if (isPro != currentIsPro)
			{
				MatrixWidgetHelper.CheckProEnable();
			}
			DataChangedNotifier.NotifyPaymentUpdated();
		});
	}

	protected override void OnActivated(EventArgs e)
	{
		base.OnActivated(e);
		if (App.NotifyIcon != null)
		{
			App.NotifyIcon.Visible = true;
		}
		Utils.ToastWindow = this;
		if (base.WindowState == WindowState.Maximized)
		{
			ListView?.TryShowThreeColumns();
		}
		CheckUserStatus().ContinueWith(UtilRun.LogTask);
		App.CheckLastActiveTime();
		ListView?.GetTaskView()?.InValidDetailMargin();
		if (!Utils.IfAltPressed())
		{
			SetAltKeyDown(down: false);
		}
	}

	public async Task CheckUserStatus()
	{
		bool navigatePayStatus = PayProWindow.NavigatePayProcess;
		DateTime payTime = PayProWindow.PayTime;
		try
		{
			if (LeftTabBar.LoadTime < DateTime.Now.AddMinutes(-10.0) || navigatePayStatus)
			{
				await PerformCheckUserStatus(navigatePayStatus);
			}
		}
		catch (Exception ex)
		{
			UtilLog.Warn("MainWindow.CheckUserStatus PerformCheckUserStatus ex: " + ex);
		}
		if (navigatePayStatus)
		{
			if (payTime == PayProWindow.PayTime)
			{
				PayProWindow.NavigatePayProcess = false;
			}
			PollingCheckPayment(payTime).ContinueWith(UtilRun.LogTask);
		}
	}

	public async Task PollingCheckPayment(DateTime startTime)
	{
		await Task.Delay(2000);
		List<int> times = new List<int> { 10, 20, 30, 40, 40, 40, 50, 50, 50 };
		int i = 0;
		bool? isPaymentCompleted = null;
		while (startTime == PayProWindow.PayTime && i < times.Count && !(startTime != PayProWindow.PayTime))
		{
			isPaymentCompleted = await Communicator.CheckPaymentCompleted();
			if (isPaymentCompleted.HasValue)
			{
				break;
			}
			await Task.Delay(times[i] * 1000);
			i++;
		}
		if (isPaymentCompleted ?? false)
		{
			await HandlePaymentUpdated();
		}
	}

	public async Task PerformCheckUserStatus(bool force = false)
	{
		await CheckLoginStatus(force);
	}

	public async Task HandlePaymentUpdated()
	{
		try
		{
			await PerformCheckUserStatus(force: true);
		}
		catch (Exception ex)
		{
			UtilLog.Warn("MainWindow.HandlePaymentUpdated PerformCheckUserStatus ex: " + ex);
		}
		finally
		{
			SyncManager.TryDelaySync(1000);
		}
	}

	protected override void OnDeactivated(EventArgs e)
	{
		base.OnDeactivated(e);
		LocalSettings.Settings.Save();
		CheckToLock();
		ListView?.GetTaskView()?.InValidDetailMargin();
		CheckAltKeyDown();
	}

	private async Task CheckAltKeyDown()
	{
		if (StaticPropertiesModel.Instance.AltKeyDown)
		{
			while (Utils.IfAltPressed())
			{
				await Task.Delay(100);
			}
			SetAltKeyDown(down: false);
		}
	}

	private async void CheckToLock()
	{
		AppLockModel appLockModel = await AppLockCache.GetModel();
		if (appLockModel != null && appLockModel.LockAfter)
		{
			TryLock();
		}
	}

	protected override async void OnClosing(CancelEventArgs e)
	{
		if (base.Visibility == Visibility.Visible)
		{
			InvalidateWindowRestore();
			Hide();
			e.Cancel = true;
			if (await AppLockCache.IsMinLock())
			{
				App.Instance.LockApp();
			}
			return;
		}
		if (_aiConversationView != null)
		{
			_aiConversationView.SubmitRequested -= OnAIConversationSubmitRequested;
			_aiConversationView.StopRequested -= OnAIConversationStopRequested;
			_aiConversationView.ScrollToBottomRequested -= OnAIScrollToBottomRequested;
			_aiConversationView.ApprovalResponseRequested -= OnAIApprovalResponseRequested;
			_aiConversationView.RatingSubmissionRequested -= OnAIRatingSubmissionRequested;
		}
		_aiFloatingWindow?.Dispose();
		await _aiWebViews.DisposeAsync();
		_aiWebViews.WebViewCreated -= OnAIWebViewCreated;
		_aiWebViews.WebViewReleasing -= OnAIWebViewReleasing;
		if (_aiWorkspaceView != null)
		{
			_aiWorkspaceView.NewConversationRequested -= OnAINewConversationRequested;
			_aiWorkspaceView.ConversationSelected -= OnAIConversationSelected;
			_aiWorkspaceView.CurrentConversationTitleChanged -= OnAICurrentConversationTitleChanged;
			_aiWorkspaceView.Dispose();
		}
		if (App.NotifyIcon != null)
		{
			App.NotifyIcon.MouseClick -= OnAppIconClick;
		}
	}

	public void ShowWindow()
	{
		ShowWindow(delay: false);
	}

	public void HideInInit()
	{
		InvalidateWindowRestore();
		base.WindowState = WindowState.Minimized;
		Show();
		Hide();
	}

	private Task ShowWindow(bool delay)
	{
		if (_showWindowTask != null)
		{
			return _showWindowTask;
		}
		TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		_showWindowTask = taskCompletionSource.Task;
		RestoreWindowAsync(delay, ++_windowPresentationVersion, taskCompletionSource);
		return taskCompletionSource.Task;
	}

	internal void InvalidateWindowRestore()
	{
		_windowPresentationVersion++;
		_showWindowTask = null;
	}

	private void LogWindowRestore(int version, string stage)
	{
		UtilLog.Info("WindowRestore id=" + version + ", stage=" + stage + ", visibility=" + base.Visibility.ToString() + ", state=" + base.WindowState.ToString() + ", taskbar=" + base.ShowInTaskbar + ", bounds=" + base.Left + "," + base.Top + "," + base.Width + "," + base.Height + ", aiHost=" + _aiWebViews.ActiveHost.ToString() + ", aiVisible=" + AIBorder.IsVisible);
	}

	private async Task RestoreWindowAsync(bool delay, int version, TaskCompletionSource<bool> completion)
	{
		string stage = "begin";
		try
		{
			bool needReload = !base.ShowInTaskbar;
			bool num = LocalSettings.Settings.Maxmized || base.WindowState == WindowState.Maximized;
			WindowState state = (LocalSettings.Settings.Maxmized ? WindowState.Maximized : base.WindowState);
			if (num && base.Visibility != Visibility.Visible)
			{
				Step("minimizeBeforeShow", delegate
				{
					base.WindowState = WindowState.Minimized;
				});
				await Task.Delay(10);
				if (!IsCurrent())
				{
					return;
				}
			}
			Step("show", base.Show);
			if (!IsCurrent())
			{
				return;
			}
			if (!base.ShowInTaskbar)
			{
				Step("showInTaskbar", delegate
				{
					base.ShowInTaskbar = true;
				});
				await Task.Delay(50);
				if (!IsCurrent())
				{
					return;
				}
			}
			Step("activate", delegate
			{
				base.Visibility = Visibility.Visible;
				Activate();
			});
			if (!IsCurrent())
			{
				return;
			}
			if (state == WindowState.Maximized && !_showed)
			{
				await CheckTaskBarAutoHide();
				if (!IsCurrent())
				{
					return;
				}
			}
			Step("restoreState", delegate
			{
				base.WindowState = ((state != WindowState.Minimized) ? state : WindowState.Normal);
			});
			if (!IsCurrent())
			{
				return;
			}
			Step("ensurePosition", delegate
			{
				EnsureWindowVisible();
			});
			if (!IsCurrent())
			{
				return;
			}
			RefreshRestoredWindow(version);
			TryFocus();
			stage = "reloadContent";
			if (!IsLoaded || LeftTabBar.SelectedModule != ((DisplayModule)LocalSettings.Settings.MainWindowDisplayModule/*cast due to .constrained prefix*/).ToString())
			{
				Utils.LogActionTimes(delegate
				{
					SwitchDisplayModule();
				}, 300, null, "MainWindow.ShowWindow.SwitchDisplayModule");
			}
			if (!IsCurrent())
			{
				return;
			}
			if (MainWindowManager.IsReSignIn())
			{
				Utils.LogActionTimes(delegate
				{
					OnWindowLoaded();
				}, 300, null, "MainWindow.ShowWindow.OnWindowLoaded");
			}
			if (!_showed || needReload)
			{
				_showed = true;
				Utils.LogActionTimes(delegate
				{
					ReloadView(restore: true);
				}, 300, null, "MainWindow.ShowWindow.ReloadView");
			}
			if (delay)
			{
				await Task.Delay(300);
				if (!IsCurrent())
				{
					return;
				}
				Step("delayedActivate", delegate
				{
					Activate();
				});
			}
			if (IsCurrent())
			{
				ShowParallelGuide();
				LogWindowRestore(version, "complete");
			}
		}
		catch (Exception ex)
		{
			UtilLog.Error("WindowRestore id=" + version + ", failedStage=" + stage + ": " + ex);
		}
		finally
		{
			if (!IsCurrent())
			{
				LogWindowRestore(version, "superseded");
			}
			if (_showWindowTask == completion.Task)
			{
				_showWindowTask = null;
			}
			completion.TrySetResult(result: true);
		}
		bool IsCurrent()
		{
			return version == _windowPresentationVersion;
		}
		void Step(string name, Action action)
		{
			stage = name;
			LogWindowRestore(version, name + ".begin");
			action();
			LogWindowRestore(version, name + ".end");
		}
	}

	private async void RefreshRestoredWindow(int version)
	{
		_ = 1;
		try
		{
			await Task.Delay(200);
			if (version == _windowPresentationVersion && base.IsVisible && base.WindowState != WindowState.Minimized)
			{
				LogWindowRestore(version, "resize.begin");
				SafeSetWindowSize();
				LogWindowRestore(version, "resize.end");
				await Task.Delay(1000);
				if (version == _windowPresentationVersion && base.IsVisible && base.WindowState != WindowState.Minimized)
				{
					LogWindowRestore(version, "layout.begin");
					InvalidateVisual();
					InvalidateArrange();
					UpdateLayout();
					LogWindowRestore(version, "layout.end");
				}
			}
		}
		catch (Exception ex)
		{
			UtilLog.Error("WindowRestore id=" + version + ", delayedRefresh failed: " + ex);
			throw;
		}
	}

	private async Task CheckTaskBarAutoHide()
	{
		Screen screen = Screen.FromHandle(new WindowInteropHelper(this).Handle);
		if (!(screen.WorkingArea.Size != screen.Bounds.Size) && screen.Primary)
		{
			await Task.Delay(400);
		}
	}

	public async void HideWindow(bool checkLock = true)
	{
		InvalidateWindowRestore();
		int version = _windowPresentationVersion;
		Hide();
		await Task.Delay(1);
		if (version == _windowPresentationVersion)
		{
			base.ShowInTaskbar = false;
			DataChangedNotifier.NotifyMainWindowHidden();
			MemoryHelper.FlushMemory();
		}
		if (checkLock && await AppLockCache.IsMinLock())
		{
			App.Instance.LockApp();
		}
	}

	private async void OnAppIconClick(object sender, System.Windows.Forms.MouseEventArgs e)
	{
		switch (e.Button)
		{
		case MouseButtons.Left:
			TryShowMainWindow();
			break;
		case MouseButtons.Right:
			_iconMenuShowing = true;
			ShowIconMenu();
			break;
		}
	}

	private void MaxButtonClick(object sender, RoutedEventArgs e)
	{
		ToggleWindowState();
	}

	private void ToggleWindowState()
	{
		base.WindowState = ((base.WindowState != WindowState.Maximized) ? WindowState.Maximized : WindowState.Normal);
		LocalSettings.Settings.Maxmized = base.WindowState == WindowState.Maximized;
		SafeSetWindowSize();
	}

	private void NormalButtonClick(object sender, RoutedEventArgs e)
	{
		ToggleWindowState();
	}

	private void MinButton_Click(object sender, RoutedEventArgs e)
	{
		base.WindowState = WindowState.Minimized;
	}

	private async void OnCloseButtonClick(object sender, RoutedEventArgs e)
	{
		HideWindow();
	}

	private async void HeaderGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		var source = e.OriginalSource as System.Windows.DependencyObject;
		while (source != null)
		{
			if (source is System.Windows.Controls.Button)
			{
				return;
			}
			source = System.Windows.Media.VisualTreeHelper.GetParent(source);
		}
		_mouseDown = true;
		GetVirtualData();
		System.Windows.Point position = Mouse.GetPosition((UIElement)sender);
		if (base.WindowState == WindowState.Maximized)
		{
			_dragStartPoint = position;
		}
		if (e.LeftButton == MouseButtonState.Pressed)
		{
			DragMove();
			e.Handled = false;
		}
		_headerClickOldTime = DateTime.Now;
	}

	public async void SyncButtonClick()
	{
		try
		{
			await CheckLoginStatus();
		}
		catch (Exception ex)
		{
			UtilLog.Warn("MainWindow.SyncButtonClick CheckLoginStatus ex: " + ex);
		}
		CheckProValid();
		AppConfigManager.PerformPullAppConfig();
		string syncUserId = LocalSettings.Settings.LoginUserId;
		await SyncManager.Sync(1);
		if (syncUserId == LocalSettings.Settings.LoginUserId && HasAIAssistantPrivacyConsent())
		{
			await Task.WhenAll(_aiWorkspaceView?.RefreshConversationsAsync() ?? Task.CompletedTask, AIWindow.Current?.RefreshConversationsAsync() ?? Task.CompletedTask, AIWindow.DesktopWidget?.RefreshConversationsAsync() ?? Task.CompletedTask);
		}
	}

	private async void CheckProValid()
	{
		if (!UserManager.IsTeamUser() && CacheManager.GetTeams().Any())
		{
			ListView?.ReloadView();
		}
		CheckVersionValid(showPrompt: false);
	}

	public async void OpenChangeLog()
	{
		Task.Run(async delegate
		{
			ReleaseNoteModel releaseNoteModel = await Utils.GetReleaseNote();
			string key = ((App.Ci.ToString() == "zh-CN") ? "zh_cn" : "en");
			NoteItemModel note = releaseNoteModel?.data.FirstOrDefault((NoteItemModel item) => item.lang == key);
			UpdateModel updateModel = await UpdateHelper.CheckUpdate();
			if (note != null && updateModel != null)
			{
				string version = Utils.ToVersion(updateModel.versionNum);
				string message = note?.content;
				base.Dispatcher.Invoke(delegate
				{
					ChangeLogWindow changeLogWindow = new ChangeLogWindow(version, message);
					changeLogWindow.Owner = this;
					changeLogWindow.Show();
				});
			}
			else
			{
				UtilLog.Info(((note == null) ? "note is null," : "") + ((updateModel == null) ? "updateModel is null" : ""));
			}
		});
	}

	public static async void ViewOnWeb()
	{
		string urlTemple = BaseUrl.GetDomainUrl() + "/sign/autoSignOn?token={0}";
		Utils.TryProcessStartUrl(string.Format(urlTemple, await Communicator.GetSignOnToken()));
	}

	public void CheckUpdate()
	{
		if (!Utils.IsNetworkAvailable())
		{
			Utils.Toast(Utils.GetString("NoNetwork"));
			return;
		}
		SettingDialog.ShowSettingDialog(SettingsType.About, this);
		App.Window?.LeftTabBar?.SetUpgradeDisplay(0);
	}

	public static async Task ShowNewVersionWindow(UpdateModel update, bool force = false, Window owner = null, string forceText = null, Action onShown = null)
	{
		Task.Run(async delegate
		{
			ReleaseNoteModel releaseNoteModel = update.releaseNote;
			if (releaseNoteModel == null)
			{
				releaseNoteModel = await Utils.GetReleaseNote();
			}
			ReleaseNoteModel releaseNoteModel2 = releaseNoteModel;
			string key = ((App.Ci.ToString() == "zh-CN") ? "zh_cn" : "en");
			NoteItemModel noteItemModel = releaseNoteModel2?.data.FirstOrDefault((NoteItemModel item) => item.lang == key);
			NewVersonViewModel model = new NewVersonViewModel
			{
				OldVersion = System.Windows.Application.ResourceAssembly.GetName().Version.ToString(),
				NewVersion = Utils.ToVersion(update.versionNum),
				ReleaseNote = noteItemModel?.content,
				DownloadPath = update.downLoadUri,
				SizeModel = releaseNoteModel2?.size,
				ForceVersions = update.forceVersions,
				UpdateModel = update
			};
			System.Windows.Application.Current.Dispatcher.Invoke(delegate
			{
				if (!NewVersionWindow.ForceUpdating)
				{
					int upgradeDisplay = 1;
					if (LocalSettings.Settings.ExtraSettings.LastUpdateRedDotVersion == Utils.GetVersion() && LocalSettings.Settings.ExtraSettings.LastUpdateRedDotNewVersion == update.versionNum.ToString())
					{
						upgradeDisplay = 0;
					}
					App.Window?.LeftTabBar.SetUpgradeDisplay(upgradeDisplay);
					_updateVersionShowed = true;
					force = force || (update.forceVersions != null && update.forceVersions.Contains(Utils.GetVersion()));
					if (!force)
					{
						if (NewVersionReminderWindow == null)
						{
							NewVersionReminderWindow = new NewVersionReminderWindow(model, force, forceText);
						}
						try
						{
							NewVersionReminderWindow.Owner = owner;
							NewVersionReminderWindow.UpdateVisibility(onShown);
							return;
						}
						catch (Exception)
						{
							NewVersionReminderWindow.Owner = null;
							NewVersionReminderWindow.UpdateVisibility(onShown);
							return;
						}
					}
					if (!isWaitingForUpdate)
					{
						isWaitingForUpdate = true;
						NewVersionDetailWindow newVersionDetailWindow = new NewVersionDetailWindow(model, force, forceText);
						newVersionDetailWindow.ContentRendered += delegate
						{
							Action action = onShown;
							onShown = null;
							action?.Invoke();
						};
						try
						{
							newVersionDetailWindow.Owner = owner;
							newVersionDetailWindow.ShowDialog();
						}
						catch (Exception)
						{
							newVersionDetailWindow.Owner = null;
							newVersionDetailWindow.ShowDialog();
						}
					}
				}
			});
		});
	}

	private async void CheckUpdateStart()
	{
		if (!Utils.IsNetworkAvailable())
		{
			return;
		}
		UpdateHelper.TryDeleteOriginPackage();
		if (ticktick_WPF.Util.DateUtils.GetDateNum(DateTime.Today) >= LocalSettings.Settings.GrayVersionDate)
		{
			UpdateModel updateModel = await UpdateHelper.CheckUpdate();
			if (updateModel != null)
			{
				TryShowUpdate(updateModel);
			}
		}
	}

	private async Task TryShowUpdate(UpdateModel updateModel)
	{
		int num = (int)updateModel.versionNum;
		int num2 = (int)Utils.VersionNumToDouble();
		if (!UpdateHelper.NeedUpgradeVersion(updateModel.versionNum, Utils.VersionNumToDouble()))
		{
			return;
		}
		if (updateModel.forceVersions != null && updateModel.forceVersions.Contains(num2.ToString()))
		{
			ShowNewVersionWindow(updateModel, force: false, this);
			return;
		}
		if (!updateModel.forceUpdate)
		{
			if (ticktick_WPF.Util.DateUtils.GetDateNum(updateModel.publishDate) > LocalSettings.Settings.GrayVersionDate)
			{
				LocalSettings.Settings.GrayVersionDate = UpdateHelper.GetGreyDate(updateModel.publishDate);
			}
			if (LocalSettings.Settings.GrayVersionDate > ticktick_WPF.Util.DateUtils.GetDateNum(DateTime.Today))
			{
				return;
			}
		}
		string skipVersion = LocalSettings.Settings.SkipVersion;
		string value = Utils.ToVersion(updateModel.versionNum);
		if (skipVersion?.Contains(value) ?? false)
		{
			LeftTabBar.SetUpgradeDisplay(0);
			return;
		}
		LeftMenuBar.CanUpdate = true;
		int upgradeDisplay = 1;
		if (LocalSettings.Settings.ExtraSettings.LastUpdateRedDotVersion == Utils.GetVersion() && LocalSettings.Settings.ExtraSettings.LastUpdateRedDotNewVersion == updateModel.versionNum.ToString())
		{
			upgradeDisplay = 0;
		}
		App.Window?.LeftTabBar.SetUpgradeDisplay(upgradeDisplay);
		if (num / 10 - num2 / 10 == 0 && num - num2 >= 2)
		{
			await Task.Delay(2000);
			ShowNewVersionWindow(updateModel, force: false, this);
		}
		else if (!_proDialogShowed && updateModel.greyForced)
		{
			ShowNewVersionWindow(updateModel, force: false, this);
		}
	}

	private async void BackDownLoad(string uri, string path)
	{
		if (!Utils.IsNetworkAvailable())
		{
			return;
		}
		using WebClient webClient = new WebClient();
		if (!Directory.Exists(AppPaths.PackageDir))
		{
			Directory.CreateDirectory(AppPaths.PackageDir);
		}
		try
		{
			webClient.DownloadProgressChanged += OnDownloadChanged;
			webClient.DownloadFileCompleted += OnDownloadCompleted;
			await webClient.DownloadFileTaskAsync(uri, path);
		}
		catch (Exception)
		{
		}
	}

	private async void OnDownloadCompleted(object sender, AsyncCompletedEventArgs e)
	{
		bool notifyIcon = await AppLockCache.GetAppLocked();
		App.NotifyIcon.Text = Utils.GetAppName();
		App.NotifyIcon.BalloonTipTitle = Utils.GetString("DownloadCompleted");
		App.NotifyIcon.BalloonTipText = Utils.GetString("ClickToUpdate");
		App.NotifyIcon.BalloonTipClicked += ShowUpdateWindow;
		App.NotifyIcon.ShowBalloonTip(20);
		App.Instance.SetNotifyIcon(notifyIcon);
		LeftMenuBar.CanUpdate = true;
	}

	private void ShowUpdateWindow(object sender, EventArgs e)
	{
	}

	private void OnDownloadChanged(object sender, DownloadProgressChangedEventArgs e)
	{
		if (App.NotifyIcon != null)
		{
			App.NotifyIcon.Text = string.Format(Utils.GetString("DownloadingPackage"), e?.ProgressPercentage);
		}
	}

	public async Task Logout()
	{
		if (await App.CheckTwitterLogin(logOut: true))
		{
			await ForceLogout();
		}
	}

	public async Task ForceLogout()
	{
		await SyncManager.Sync();
		if (await LoginManager.Logout())
		{
			await ResetAIAssistantForLogoutAsync();
			InvalidateWindowRestore();
			Hide();
			new LoginDialog().Show();
		}
	}

	private async Task ResetAIAssistantForLogoutAsync()
	{
		_aiWebViewHostAttachmentVersion++;
		_aiClientHostState.SetGenerating(isGenerating: false);
		_aiFloatingWindow?.HideImmediately();
		_aiFloatingWindow?.SetAgentBridgeReady(isReady: false);
		_aiWorkspaceView?.SetAgentBridgeReady(isReady: false);
		_aiWorkspaceView?.ResetForAccountChange();
		_aiConversationView?.SetAgentBridgeReady(isReady: false);
		_aiConversationView?.ResetForAccountChange();
		await _aiWebViewHostTransitionGate.WaitAsync();
		try
		{
			_aiFloatingWindow?.DetachConversationWorkspace();
			_aiFloatingWindow?.DetachConversationContent(_aiConversationView);
			await _aiWebViews.ResetAsync();
			_aiWorkspaceView?.ResetForAccountChange();
			_aiConversationView?.ResetForAccountChange();
		}
		catch (Exception ex)
		{
			UtilLog.Info("Failed to reset Agent state after logout. " + ExceptionUtils.BuildExceptionMessage(ex));
		}
		finally
		{
			_aiWebViewHostTransitionGate.Release();
		}
	}

	public async Task OnSearch(SearchExtra searchExtra, bool restore)
	{
		if (!LocalSettings.Settings.InSearch)
		{
			_saveModule = LocalSettings.Settings.MainWindowDisplayModule;
		}
		if (searchExtra.LinkTaskIds == null)
		{
			SearchHelper.InSearch = true;
			SwitchModule(DisplayModule.Task, force: true);
			ListView?.StartSearch(searchExtra, restore);
			TryFocus();
			return;
		}
		bool flag = searchExtra.SearchKey.Length == 0;
		UserActCollectUtils.AddClickEvent("link_parent_task", "task_search_page", flag ? "recently_created" : "search");
		int taskLevel = TaskCache.GetTaskLevel(searchExtra.SearchId);
		int num = 0;
		if (searchExtra.LinkTaskIds != null && searchExtra.LinkTaskIds.Any())
		{
			num = (from taskId in searchExtra.LinkTaskIds
				let task = TaskCache.GetTaskById(taskId)
				select TaskCache.GetMaxTaskDepth(taskId, task.ProjectId)).Concat(new int[1] { num }).Max();
		}
		if (taskLevel + num >= 5)
		{
			SearchDialog?.Close();
			if (new CustomerDialog(Utils.GetString("task_level_max_limit_title"), Utils.GetString("task_level_max_limit_desc"), Utils.GetString("OK"), Utils.GetString("Cancel"))
			{
				Owner = System.Windows.Window.GetWindow(this),
				Topmost = false
			}.ShowDialog() != true)
			{
				return;
			}
		}
		string toastType = "auto";
		string text = searchExtra.from;
		if (!(text == "calendar"))
		{
			if (text == "detail")
			{
				toastType = "hide";
			}
		}
		else
		{
			toastType = "show";
		}
		await TaskDao.BatchUpdateParent(searchExtra.LinkTaskIds, searchExtra.SearchId, null, canUndo: true, toastType);
		await TaskDragHelper.CheckChildrenLevelAndSortOrder(searchExtra.SearchId);
		TaskChangeNotifier.NotifyTaskParentChanged(searchExtra.LinkTaskIds);
	}

	private void WindowLocationChanged(object sender, EventArgs e)
	{
		DelaySaveLocation();
	}

	public async void SelectTagProject(string tag)
	{
		await Task.Delay(120);
		SwitchModule(DisplayModule.Task);
		ListView?.SelectTagProject(tag.ToLower(), scroll: true);
		ExitImmersiveMode();
		if (await AppLockCache.GetAppLocked())
		{
			AppUnlockWindow.TryUnlockApp(ShowWindow);
		}
		else
		{
			ShowWindow();
		}
	}

	public void StartSearch()
	{
		StartSearch(reopen: false);
	}

	public void StartSearch(bool reopen)
	{
		StartSearch(reopen, SearchKind.Normal);
	}

	public void StartLinkSearch(List<string> linkTaskIds = null, string from = "list")
	{
		if (SearchOperationDialog != null && SearchOperationDialog.Showing)
		{
			SearchOperationDialog.Close();
		}
		if (SearchDialog == null || !SearchDialog.Showing)
		{
			SearchDialog = new SearchDialog(restore: false, SearchKind.Task, linkTaskIds, from);
			if (base.IsVisible)
			{
				SearchDialog.Owner = this;
			}
			SearchDialog?.Show();
		}
	}

	public void StartSearch(bool reopen, SearchKind kind, List<string> linkTaskIds = null)
	{
		if (SearchOperationDialog != null && SearchOperationDialog.Showing)
		{
			SearchOperationDialog.Close();
		}
		if (SearchDialog == null || !SearchDialog.Showing)
		{
			LeftTabBar.SelectedModule = "Search";
			SearchDialog = new SearchDialog(reopen, kind, linkTaskIds)
			{
				Owner = this
			};
			SearchDialog?.Show();
		}
	}

	private void ShowSearchOperation()
	{
		if (SearchDialog != null && SearchDialog.Showing)
		{
			SearchDialog.Close();
		}
		if (SearchOperationDialog == null || !SearchOperationDialog.Showing)
		{
			SearchOperationDialog = new SearchOperationDialog
			{
				Owner = this
			};
			SearchOperationDialog?.Show();
		}
	}

	private async void OnWindowKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (_aiFloatingWindow?.IsKeyboardFocusWithin ?? false)
		{
			return;
		}
		if (!Utils.IfAltPressed())
		{
			SetAltKeyDown(down: false);
		}
		if (e.Key == Key.System && e.SystemKey == Key.LeftAlt)
		{
			e.Handled = true;
		}
		if (!base.IsActive)
		{
			return;
		}
		IInputElement focusedElement = FocusManager.GetFocusedElement(this);
		if (CalendarBorder.Visibility == Visibility.Visible && !ImmersiveGrid.IsVisible && (!ListBorder.IsVisible || Utils.FindParent<TaskView>(focusedElement as DependencyObject) == null))
		{
			MainCalendar?.OnKeyUp(sender, e);
		}
		if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Z)
		{
			TryUndo();
			return;
		}
		if (focusedElement == null || focusedElement.Equals(this) || focusedElement is ScrollViewer || Utils.FindParent<ProjectListView>(focusedElement as DependencyObject) != null)
		{
			DoOperationOnKeyUp(e.Key);
			ListView?.OnWindowKeyUp(e);
		}
		if (e.Key == Key.Escape)
		{
			await Task.Delay(50);
			if (!e.Handled && MaskPanel.IsVisible)
			{
				CloseShortCut();
			}
		}
	}

	private async void SetAltKeyDown(bool down)
	{
		if (_altKeyDown == down)
		{
			return;
		}
		_altKeyDown = down;
		string text = ((!ShowList) ? ((!ShowCalendar) ? (ShowMatrix ? "in_matrix_tab" : null) : ((ListView?.IsVisible ?? false) ? "in_split_view" : "in_calendar_tab")) : ((MainCalendar?.IsVisible ?? false) ? "in_split_view" : "in_task_tab"));
		if (text == null && !ShowCountdown)
		{
			return;
		}
		if (down && !StaticPropertiesModel.Instance.AltKeyDown)
		{
			IInputElement focusedElement = Keyboard.FocusedElement;
			if (focusedElement is System.Windows.Controls.TextBox && MaskPanel.IsVisible)
			{
				return;
			}
			if (focusedElement is TextArea)
			{
				await Task.Delay(500);
				if (!_altKeyDown)
				{
					return;
				}
				UserActCollectUtils.AddClickEvent("shortcut", "alt", "in_task_tab_editing");
			}
			else if (!string.IsNullOrEmpty(text))
			{
				UserActCollectUtils.AddClickEvent("shortcut", "alt", text);
			}
		}
		StaticPropertiesModel.Instance.AltKeyDown = down;
	}

	private void UpgradeProClick(object sender, RoutedEventArgs e)
	{
	}

	public void HandleDateChanged()
	{
		Utils.RunOnUiThread(base.Dispatcher, async delegate
		{
			ProjectAndTaskIdsCache.ResetIds();
			TaskStickyWindow.ReloadStickies();
			MainCalendar?.OnCurrentDateChanged();
		});
	}

	public void SyncVersionStatus()
	{
		Utils.RunOnUiThread(base.Dispatcher, delegate
		{
			CheckVersionValid(showPrompt: false);
		});
	}

	public void HideBackground()
	{
		WindowBackground.Visibility = Visibility.Collapsed;
		MainWindowManager.BackImageSource = null;
		IndependentWindow.SetBackGround(null);
	}

	public void SetBackgroundImage(BitmapImage background, Rect viewBox)
	{
		MainWindowManager.BackImageSource = background;
		WindowBackgroundImage.ImageSource = background;
		WindowBackgroundImage.Viewbox = viewBox;
		IndependentWindow.SetBackGround(WindowBackgroundImage);
		if (WindowBackground.Visibility != Visibility.Visible)
		{
			WindowBackground.Visibility = Visibility.Visible;
		}
	}

	public void FinishTutorial()
	{
		TutorialContent.Content = null;
		ListView?.GetTaskView()?.FocusQuickAdd();
	}

	private void ShowTutorial()
	{
		TutorialContent.Content = new TutorialPopup();
	}

	private async void OnWindowMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (MaskPanel.IsVisible || (!_mouseDown && !CalendarBorder.IsMouseOver))
		{
			return;
		}
		_mouseDown = false;
		if ((AIBorder.IsVisible && AIBorder.IsMouseOver) || (_aiFloatingWindow?.IsMouseOver ?? false))
		{
			return;
		}
		if (!ShowCalendar || LocalSettings.Settings.InSearch)
		{
			if (ListView?.GetTaskView()?.DetailMouseOver() != true && (Summary == null || Summary.RemoveInputFocus()) && Utils.GetMousePointVisibleItem<QuickAddView>(e, this) == null && Utils.GetMousePointVisibleItem<System.Windows.Controls.TextBox>(e, this) == null && (MatrixContainer == null || !MatrixContainer.IsVisible || (!MatrixContainer.AddButtonMoveOver() && Utils.GetMousePointElement<TaskListItem>(e, MatrixContainer) == null)) && base.IsActive && !e.Handled)
			{
				TryFocus();
			}
			return;
		}
		if (!base.IsActive)
		{
			Activate();
		}
		await Task.Delay(100);
		if (base.IsActive && !e.Handled)
		{
			MainCalendar?.GetFocus();
			TaskOperationDialog.TryCloseLastOne();
		}
	}

	public async void EnterImmersiveMode(string taskId, int caretOffset = -1)
	{
		TryShowMainWindow();
		base.MinWidth = 595.0;
		TaskDetailView taskDetailView = ListView?.GetTaskView()?.GetTaskDetail();
		TaskDetailView taskDetailView2 = TaskDetailView.TakePendingImmersiveSource(taskId) ?? taskDetailView;
		_immersiveSourceDetail = ((taskDetailView2 == null) ? null : new WeakReference<TaskDetailView>(taskDetailView2));
		MarkDownEditor markDownEditor = taskDetailView2?.GetContentText() ?? taskDetailView2?.GetDescText();
		int sourceCaretOffset = ((caretOffset >= 0) ? caretOffset : (markDownEditor?.EditBox.CaretOffset ?? 0));
		ImmersiveContent editor = new ImmersiveContent
		{
			Margin = new Thickness(0.0, 24.0, 0.0, 12.0),
			VerticalAlignment = VerticalAlignment.Stretch
		};
		ImmersiveGrid.Child = editor;
		ImmersiveGrid.Visibility = Visibility.Visible;
		CardThemeDictService.OnImmerseChanged(enable: true);
		await editor.LoadData(taskId, taskDetailView2);
		MarkDownEditor markDownEditor2 = editor.TaskDetail.GetContentText() ?? editor.TaskDetail.GetDescText();
		if (markDownEditor2 != null && sourceCaretOffset >= 0)
		{
			RestoreImmersiveCaret(editor, markDownEditor2, sourceCaretOffset);
		}
	}

	private void RestoreImmersiveCaret(ImmersiveContent immersiveContent, MarkDownEditor editor, int caretOffset)
	{
		if (ImmersiveGrid.Child == immersiveContent && caretOffset >= 0)
		{
			int num = Math.Min(caretOffset, editor.EditBox.Text.Length);
			editor.EditBox.CaretOffset = num;
			editor.FocusEditBox();
			TextLocation location = editor.EditBox.Document.GetLocation(num);
			editor.EditBox.ScrollTo(location.Line, location.Column);
		}
	}

	public void ExitImmersiveMode()
	{
		if (ImmersiveGrid.Visibility == Visibility.Visible)
		{
			TaskDetailImmerseView taskDetailImmerseView = (ImmersiveGrid.Child as ImmersiveContent)?.TaskDetail;
			TaskDetailView taskDetailView = ListView?.GetTaskView()?.GetTaskDetail();
			TaskDetailView detail = taskDetailView;
			taskDetailView?.CopyEditorStateFrom(taskDetailImmerseView);
			if (_immersiveSourceDetail != null && _immersiveSourceDetail.TryGetTarget(out var target))
			{
				detail = target;
				if (target != taskDetailView)
				{
					target.CopyEditorStateFrom(taskDetailImmerseView);
				}
			}
			base.MinWidth = 370.0;
			ImmersiveGrid.Visibility = Visibility.Collapsed;
			ImmersiveGrid.Child = null;
			RestoreDetailFocusAfterImmersive(detail, taskDetailImmerseView?.TaskId);
		}
		_immersiveSourceDetail = null;
		CardThemeDictService.OnImmerseChanged(enable: false);
	}

	private void RestoreDetailFocusAfterImmersive(TaskDetailView detail, string taskId)
	{
		MarkDownEditor editor = detail?.GetContentText() ?? detail?.GetDescText();
		if (editor == null || string.IsNullOrEmpty(taskId) || detail.TaskId != taskId)
		{
			return;
		}
		base.Dispatcher.BeginInvoke((Action)delegate
		{
			if (base.IsActive && ImmersiveGrid.Visibility != Visibility.Visible && detail.IsVisible && !(detail.TaskId != taskId) && editor.IsVisible && editor == (detail.GetContentText() ?? detail.GetDescText()))
			{
				editor.FocusEditBox();
				editor.EditBox.TextArea.Caret.BringCaretToView();
			}
		}, DispatcherPriority.ContextIdle);
	}

	private IntPtr Hook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
	{
		switch (msg)
		{
		case 255:
		{
			TouchpadContact[] array = TouchPadHelper.TouchpadHelper.ParseInput(lParam);
			if (array != null && array.Any((TouchpadContact c) => c.ContactId == 1))
			{
				OnDoubleFingerTouch();
			}
			break;
		}
		case 526:
		{
			int num = 0;
			num = LOWORD(wParam);
			OnMouseTilt(num);
			return (IntPtr)1;
		}
		case 36:
			if (Utils.IsWindows7())
			{
				return IntPtr.Zero;
			}
			if (base.WindowState != WindowState.Maximized || _enableHandleMinMax)
			{
				if (base.WindowState == WindowState.Maximized)
				{
					_enableHandleMinMax = false;
					DelaySetEnableHandleMinMax();
				}
				else
				{
					_enableHandleMinMax = true;
				}
				Screen screen = Screen.FromRectangle(new System.Drawing.Rectangle((int)base.Left, (int)base.Top, (int)base.Width, (int)base.Height));
				if (screen.WorkingArea.Size != screen.Bounds.Size || !screen.Primary)
				{
					SetInPrimary(inPrimary: false);
					return IntPtr.Zero;
				}
				if (Mouse.LeftButton == MouseButtonState.Pressed && base.WindowState == WindowState.Normal)
				{
					return IntPtr.Zero;
				}
				if (WindowSizing.WmGetMinMaxInfo(hwnd, lParam, base.MinWidth, base.MinHeight))
				{
					SetInPrimary(inPrimary: true);
					handled = true;
					return IntPtr.Zero;
				}
				SetInPrimary(inPrimary: false);
			}
			break;
		}
		return IntPtr.Zero;
		async void DelaySetEnableHandleMinMax()
		{
			await Task.Delay(500);
			_enableHandleMinMax = true;
		}
		void SetInPrimary(bool inPrimary)
		{
			_inPrimaryScreen = inPrimary;
			if (!inPrimary && base.WindowState == WindowState.Maximized)
			{
				((Grid)base.Content).Margin = new Thickness(7.0, 5.0, 7.0, 5.0);
			}
			else if (inPrimary)
			{
				((Grid)base.Content).Margin = new Thickness(0.0);
			}
		}
	}

	private static int HIWORD(IntPtr ptr)
	{
		return (ptr.ToInt32() >> 16) & 0xFFFF;
	}

	private static int LOWORD(IntPtr ptr)
	{
		return ((int)ptr.ToInt64() >> 16) % 256;
	}

	private void OnMouseTilt(int offset)
	{
		if (CalendarBorder.IsMouseOver)
		{
			if (base.IsActive)
			{
				MainCalendar?.OnTouchScroll(offset);
			}
		}
		else if (ListBorder.IsMouseOver)
		{
			ListView.OnScroll(offset);
		}
	}

	private void OnDoubleFingerTouch()
	{
		if (ShowCalendar)
		{
			MainCalendar?.OnDoubleFingerTouch();
		}
	}

	private void StopDragMove(object sender, MouseButtonEventArgs e)
	{
		_dragStartPoint = default(System.Windows.Point);
	}

	private void OnDragMove(object sender, System.Windows.Input.MouseEventArgs e)
	{
		if (base.WindowState != WindowState.Maximized || e.LeftButton != MouseButtonState.Pressed || !(_dragStartPoint != default(System.Windows.Point)))
		{
			return;
		}
		UIElement uIElement = (UIElement)sender;
		System.Windows.Point position = Mouse.GetPosition(uIElement);
		if (!((position - _dragStartPoint).Length < 4.0))
		{
			System.Windows.Point point = uIElement.PointToScreen(new System.Windows.Point(0.0, 0.0));
			CompositionTarget compositionTarget = PresentationSource.FromVisual(uIElement)?.CompositionTarget;
			if (compositionTarget != null)
			{
				point = compositionTarget.TransformFromDevice.Transform(point);
			}
			base.Top = point.Y;
			double num = 0.0;
			if (!MathUtil.TryParseString2Double(LocalSettings.Settings.MainWindowWidth.ToString(CultureInfo.InvariantCulture), out var db))
			{
				db = 1020.0;
			}
			if (position.X > db / 2.0)
			{
				num = position.X - db / 2.0;
			}
			base.Left = point.X + num;
			base.WindowState = WindowState.Normal;
			LocalSettings.Settings.Maxmized = false;
			_dragStartPoint = default(System.Windows.Point);
			try
			{
				DragMove();
			}
			catch (Exception)
			{
			}
			e.Handled = true;
		}
	}

	public void HandleOnNeedPro()
	{
	}

	public void TryReloadCalendar()
	{
		if (CalendarBorder.Visibility == Visibility.Visible)
		{
			MainCalendar?.Reload(force: false, reloadArrange: false);
		}
	}

	private async void OnStateChanged(object sender, EventArgs e)
	{
		if (base.WindowState == WindowState.Minimized)
		{
			MainCalendar?.ClearCopiedTaskSelection();
			ListView?.GetTaskView()?.ClearCopiedTaskSelectionOnWindowMinimized();
			if (_lastNonMinimizedWindowState == WindowState.Maximized)
			{
				SuspendMainAIWebViewForMaximizedRestore();
			}
		}
		if (base.WindowState == WindowState.Maximized && !_inPrimaryScreen)
		{
			((Grid)base.Content).Margin = new Thickness(7.0, 5.0, 7.0, 5.0);
		}
		else
		{
			((Grid)base.Content).Margin = new Thickness(0.0);
		}
		if (base.WindowState == WindowState.Normal)
		{
			SafeSetWindowSize();
		}
		if (base.WindowState != WindowState.Minimized)
		{
			_lastNonMinimizedWindowState = base.WindowState;
			await ResumeMainAIWebViewAfterWindowRestoreAsync();
		}
	}

	private void SuspendMainAIWebViewForMaximizedRestore()
	{
		if (_aiWebViews.ActiveHost != AIWebViewHost.MainWindow || AIBorder.Visibility != Visibility.Visible)
		{
			return;
		}
		AIWebAppView activeWebView = _aiWebViews.ActiveWebView;
		if (activeWebView != null)
		{
			if (_mainAIWebViewAwaitingMaximizedRestore != null && _mainAIWebViewAwaitingMaximizedRestore != activeWebView)
			{
				_mainAIWebViewAwaitingMaximizedRestore.ResumeBrowserPresentationAfterHostLayout();
			}
			_mainAIWebViewAwaitingMaximizedRestore = activeWebView;
			_mainAIWebViewRestoreVersion++;
			activeWebView.SuspendBrowserPresentationForHostLayout();
		}
	}

	private async Task ResumeMainAIWebViewAfterWindowRestoreAsync()
	{
		AIWebAppView webView = _mainAIWebViewAwaitingMaximizedRestore;
		if (webView == null)
		{
			return;
		}
		int restoreVersion = _mainAIWebViewRestoreVersion;
		if (base.WindowState == WindowState.Maximized && _aiWebViews.ActiveHost == AIWebViewHost.MainWindow && _aiWebViews.ActiveWebView == webView)
		{
			await base.Dispatcher.InvokeAsync(delegate
			{
				UpdateLayout();
				AIBorder.UpdateLayout();
				webView.UpdateLayout();
			}, DispatcherPriority.Loaded);
			await WaitForNextRenderingPassAsync();
			await WaitForNextRenderingPassAsync();
		}
		if (base.WindowState != WindowState.Minimized && restoreVersion == _mainAIWebViewRestoreVersion && _mainAIWebViewAwaitingMaximizedRestore == webView)
		{
			_mainAIWebViewAwaitingMaximizedRestore = null;
			webView.ResumeBrowserPresentationAfterHostLayout();
		}
	}

	private static Task WaitForNextRenderingPassAsync()
	{
		TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();
		EventHandler handler = null;
		handler = delegate
		{
			CompositionTarget.Rendering -= handler;
			completion.TrySetResult(result: true);
		};
		CompositionTarget.Rendering += handler;
		return completion.Task;
	}

	public void ToastMoveProjectControl(string projectId, string taskName = null, MoveToastType moveType = MoveToastType.Move)
	{
		WindowToastHelper.ShowAndHideToast(ToastGrid, new MoveToastControl(projectId, this, taskName, moveType));
	}

	public async void TryToastMoveControl(TaskModel task, bool isToday, bool hideTitle = false)
	{
		if (task != null && (string.IsNullOrEmpty(task.parentId) || ListView?.GetTaskView()?.CheckTaskInList(task.parentId) != true) && !MoveToastHelper.CheckTaskMatched(ListView?.GetSelectedProject(), task))
		{
			WindowToastHelper.ShowAndHideToast(ToastGrid, new MoveToastControl(isToday, this, hideTitle ? string.Empty : task.title));
		}
	}

	public void NavigateProjectById(string projectId)
	{
		TryShowMainWindow();
		NavigateNormalProject(projectId);
	}

	public void SwitchModule(DisplayModule module, bool force = false, bool click = false)
	{
		if (module == DisplayModule.AI && !LocalSettings.Settings.EnableAI)
		{
			return;
		}
		if (LocalSettings.Settings.MainWindowDisplayModule != (int)module || force)
		{
			_previousModule = (DisplayModule)LocalSettings.Settings.MainWindowDisplayModule;
			LocalSettings.Settings.MainWindowDisplayModule = (int)module;
			if (_previousModule != module)
			{
				ClearPreviousViewData();
			}
			SwitchDisplayModule();
			ThemeUtil.TryClearImageCached(force: true);
			return;
		}
		LeftTabBar.SelectedModule = module.ToString();
		if (click)
		{
			if (module == DisplayModule.Pomo)
			{
				TimerSyncService.PullRemoteTimers();
			}
			if (module == DisplayModule.CountDown)
			{
				CountdownSyncService.SyncCountdowns();
			}
			if (module == DisplayModule.Calendar)
			{
				MainCalendar.GotoToday();
			}
		}
	}

	private async void ClearPreviousViewData()
	{
		switch (_previousModule)
		{
		case DisplayModule.Calendar:
			MainCalendar?.UnbindEvents();
			break;
		case DisplayModule.Task:
			ListView?.ExitBatchSelect();
			break;
		case DisplayModule.Matrix:
			EisenhoverBorder.Child = null;
			break;
		case DisplayModule.Habit:
			HabitBorder.Child = null;
			HabitView = null;
			break;
		case DisplayModule.Pomo:
			FocusBorder.Child = null;
			TickFocusManager.SetFocusInitPanel();
			TickFocusManager.MainFocus = null;
			break;
		}
		DelayActionHandlerCenter.TryDoAction("DelayCollectGC", delegate
		{
			GC.Collect();
		}, 4000);
	}

	public void ResetLeftSelected()
	{
		LeftTabBar.SelectedModule = ((DisplayModule)LocalSettings.Settings.MainWindowDisplayModule/*cast due to .constrained prefix*/).ToString();
	}

	public void SetCustomTheme(BitmapImage image, Rect rect)
	{
		if (image != null)
		{
			WindowBackground.Visibility = Visibility.Visible;
			WindowBackgroundImage.ImageSource = image;
		}
		WindowBackgroundImage.Viewbox = rect;
	}

	public void SwitchPreviousModule()
	{
		SwitchModule(_previousModule);
	}

	public async void BeginCoach(UIElement ui)
	{
		CoachMarks.Child = ui;
		CoachMarks.IsHitTestVisible = false;
		CoachMarks.Visibility = Visibility.Hidden;
		await Task.Delay(500);
		CoachMarks.IsHitTestVisible = true;
		CoachMarks.Visibility = Visibility.Visible;
	}

	public void EndCoach()
	{
		CoachMarks.Child = null;
		CoachMarks.Visibility = Visibility.Collapsed;
	}

	public async Task ShowShortCutPanel()
	{
		UserActCollectUtils.AddShortCutEvent("general", "shortcuts");
		await SettingsHelper.PullRemoteSettings();
		Border border = new Border
		{
			Child = new ShortcutsConfig(inSettings: false)
			{
				Width = ((ShortCutMask.ActualWidth <= 72.0 || ShortCutMask.ActualWidth >= 512.0) ? 440.0 : (ShortCutMask.ActualWidth - 72.0))
			},
			HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
			Margin = new Thickness(36.0),
			CornerRadius = new CornerRadius(10.0)
		};
		border.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "PopupBackground");
		ShortCutMask.Child = border;
		MaskPanel.Visibility = Visibility.Visible;
	}

	private void CloseShortCut(object sender, MouseButtonEventArgs e)
	{
		UIElement child = ShortCutMask.Child;
		if (child == null || !child.IsMouseOver)
		{
			CloseShortCut();
		}
	}

	private async Task CloseShortCut()
	{
		ShortCutMask.Child = null;
		MaskPanel.Visibility = Visibility.Collapsed;
		if (SettingsHelper.ShortCutChanged)
		{
			await SettingsHelper.PushLocalSettings();
			SettingsHelper.ShortCutChanged = false;
		}
		TryFocus();
	}

	public void ClearListView()
	{
		ListView?.HideDetail();
		ListBorder.Visibility = Visibility.Collapsed;
		ListBorder.Child = null;
		ListView = null;
	}

	public void ClearChildren()
	{
		ClearListView();
		LeftTabBar.SelectedModule = null;
	}

	public void StopSearch(bool forceSwitch = true)
	{
		SearchHelper.InSearch = false;
		if (_saveModule >= 0)
		{
			SwitchModule((DisplayModule)_saveModule, force: true);
			_saveModule = -1;
		}
		else if (forceSwitch)
		{
			SwitchDisplayModule();
		}
	}

	private async void OnWindowKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (_aiFloatingWindow?.IsKeyboardFocusWithin ?? false)
		{
			return;
		}
		if (e.Key == Key.Escape && LocalSettings.Settings.InSearch)
		{
			await Task.Delay(50);
			if (!e.Handled && ListView?.GetTaskView()?.TaskDetailFocus() != true)
			{
				StopSearch();
			}
		}
		if (Utils.IfAltPressed() && e.Key == Key.System)
		{
			SetAltKeyDown(down: true);
		}
		else
		{
			SetAltKeyDown(down: false);
		}
	}

	public async void ShowParallelGuide()
	{
		await Task.Delay(1000);
		CommonOtherSettings otherSettings = LocalSettings.Settings.Common.GetOtherSettings();
		if (!otherSettings.ShowParallelGuide.HasValue || otherSettings.ShowParallelGuide == true)
		{
			return;
		}
		otherSettings.ShowParallelGuide = true;
		LocalSettings.Settings.Common.SetOtherSettings(otherSettings);
		LocalSettings.Settings.Save(force: true);
		try
		{
			ListCalendarSettings listCalendarSettings = JsonConvert.DeserializeObject<ListCalendarSettings>(LocalSettings.Settings.ExtraSettings.ListCalendarSettings);
			if (listCalendarSettings != null && (listCalendarSettings.ShowCInL || listCalendarSettings.ShowLInC))
			{
				return;
			}
		}
		catch (Exception)
		{
		}
		ParallelGuideControl parallelGuideControl = new ParallelGuideControl(ThemeUtil.IsDark());
		int listIndex = LeftTabBar.GetListIndex();
		parallelGuideControl.ParallelGuideGrid.Margin = new Thickness(52.0, 67 + listIndex * 48, 0.0, 0.0);
		GuideContent.Content = parallelGuideControl;
	}

	public async void CloseParallelGuide(bool openParallel)
	{
		GuideContent.Content = null;
		await Task.Delay(10);
		if (openParallel && ProChecker.CheckPro(ProType.ParallelShowInTask))
		{
			SwitchModule(DisplayModule.Task);
			ShowCalendarInTaskChanged();
		}
	}

	private void OnCalOrListBatchSelected(object sender, bool calBatchSelected)
	{
		if (calBatchSelected)
		{
			ListView?.ExitBatchSelect();
		}
		else
		{
			MainCalendar?.ExitBatchSelect(ignorePopup: true, ignoreCtrlPress: true);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/mainwindow.xaml", UriKind.Relative);
			System.Windows.Application.LoadComponent(this, resourceLocator);
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
			Window = (MainWindow)target;
			break;
		case 2:
			Container = (Grid)target;
			break;
		case 3:
			ColumnOneDefine = (ColumnDefinition)target;
			break;
		case 4:
			WindowBackground = (Border)target;
			break;
		case 5:
			WindowBackgroundImage = (ImageBrush)target;
			break;
		case 6:
			CLGrid = (Grid)target;
			break;
		case 7:
			LCFirstColumn = (ColumnDefinition)target;
			break;
		case 8:
			LCSecondColumn = (ColumnDefinition)target;
			break;
		case 9:
			ClBorder = (Border)target;
			break;
		case 10:
			CalendarBorder = (Border)target;
			CalendarBorder.SizeChanged += OnCalendarBorderSizeChanged;
			break;
		case 11:
			CalendarBorderClip = (RectangleGeometry)target;
			break;
		case 12:
			CalendarTransform = (ScaleTransform)target;
			break;
		case 13:
			CalendarThicknessBorder = (Border)target;
			break;
		case 14:
			ListBorder = (Border)target;
			ListBorder.SizeChanged += OnListBorderSizeChanged;
			break;
		case 15:
			ListBorderClip = (RectangleGeometry)target;
			break;
		case 16:
			ListTransform = (ScaleTransform)target;
			break;
		case 17:
			ListThicknessBorder = (Border)target;
			break;
		case 18:
			LCSplitter = (GridSplitter)target;
			LCSplitter.DragCompleted += OnLCSplitDragComplete;
			LCSplitter.DragDelta += OnLCSplitDragging;
			break;
		case 19:
			FocusBorder = (Border)target;
			FocusBorder.SizeChanged += OnFocusBorderSizeChanged;
			break;
		case 20:
			FocusBorderClip = (RectangleGeometry)target;
			break;
		case 21:
			HabitBorder = (Border)target;
			HabitBorder.SizeChanged += OnHabitBorderSizeChanged;
			break;
		case 22:
			HabitBorderClip = (RectangleGeometry)target;
			break;
		case 23:
			EisenhoverBorder = (Border)target;
			EisenhoverBorder.SizeChanged += OnMatrixBorderSizeChanged;
			break;
		case 24:
			MatrixBorderClip = (RectangleGeometry)target;
			break;
		case 25:
			CountDownBorder = (Border)target;
			CountDownBorder.SizeChanged += OnCountDownBorderSizeChanged;
			break;
		case 26:
			CountDownBorderClip = (RectangleGeometry)target;
			break;
		case 27:
			AIBorder = (Border)target;
			AIBorder.SizeChanged += OnAIBorderSizeChanged;
			break;
		case 28:
			AIBorderClip = (RectangleGeometry)target;
			break;
		case 29:
			ThicknessBorder = (Border)target;
			break;
		case 30:
			LeftTabBar = (LeftMenuBar)target;
			break;
		case 31:
			TutorialContent = (ContentControl)target;
			break;
		case 32:
			((Border)target).MouseLeftButtonDown += HeaderGrid_MouseLeftButtonDown;
			((Border)target).MouseLeftButtonUp += StopDragMove;
			((Border)target).MouseMove += OnDragMove;
			break;
		case 33:
			ResizeButtons = (StackPanel)target;
			break;
		case 34:
			UpgradeButton = (System.Windows.Controls.Button)target;
			UpgradeButton.Click += UpgradeProClick;
			break;
		case 35:
			MinButton = (System.Windows.Controls.Button)target;
			MinButton.Click += MinButton_Click;
			break;
		case 36:
			MinPath = (System.Windows.Shapes.Rectangle)target;
			break;
		case 37:
			MaxButton = (System.Windows.Controls.Button)target;
			MaxButton.Click += MaxButtonClick;
			break;
		case 38:
			MaxPath = (System.Windows.Shapes.Path)target;
			break;
		case 39:
			NormalButton = (System.Windows.Controls.Button)target;
			NormalButton.Click += NormalButtonClick;
			break;
		case 40:
			NormalPath = (System.Windows.Shapes.Path)target;
			break;
		case 41:
			CloseButton = (System.Windows.Controls.Button)target;
			CloseButton.Click += OnCloseButtonClick;
			break;
		case 42:
			X = (Polygon)target;
			break;
		case 43:
			AIFloatingLayer = (ContentControl)target;
			break;
		case 44:
			AIFloatingEntry = (AIFloatingEntry)target;
			break;
		case 45:
			AIFloatingEntryGuidePopup = (Popup)target;
			AIFloatingEntryGuidePopup.Opened += OnAIFloatingEntryGuideOpened;
			break;
		case 46:
			AIFloatingEntryGuideHint = (ArrowHintUserControl)target;
			break;
		case 47:
			ImmersiveGrid = (Border)target;
			break;
		case 48:
			CoachMarks = (Border)target;
			break;
		case 49:
			MaskPanel = (Grid)target;
			MaskPanel.MouseLeftButtonUp += CloseShortCut;
			break;
		case 50:
			ShortCutMask = (Border)target;
			break;
		case 51:
			((Border)target).MouseLeftButtonUp += CloseShortCut;
			break;
		case 52:
			ToastGrid = (Grid)target;
			break;
		case 53:
			GuideContent = (ContentControl)target;
			break;
		case 54:
			BottomContent = (ContentControl)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}

