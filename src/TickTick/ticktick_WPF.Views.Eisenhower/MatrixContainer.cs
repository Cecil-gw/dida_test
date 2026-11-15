using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.DragAndDrop;
using ticktick_WPF.Util.Sync.Model;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Views.Eisenhower;

public class MatrixContainer : UserControl, IComponentConnector
{
	private readonly DelayActionHandler _delayLoadHandler = new DelayActionHandler(200);

	private readonly object _loadTaskGate = new object();

	private readonly HashSet<QuadrantControl> _pendingLoadQuadrants = new HashSet<QuadrantControl>();

	private const double QuadrantPadding = 6.0;

	private Task _currentLoadTask = Task.CompletedTask;

	private bool _loadTaskRunning;

	private bool _loadTaskPending;

	private bool _pendingLoadAll;

	private bool _pendingRestore = true;

	private List<QuadrantControl> _quadrants;

	private Rect[] _quadrantLocations = new Rect[4];

	private Point _dragStartPosition;

	private Point _popupStartPoint;

	private QuadrantControl _dragQuadrant;

	private int _dragHoverIndex = -1;

	private int _dragStartIndex;

	private WidgetMorePopup _widgetMorePopup;

	private bool _clear;

	private CustomMenuList _viewMenu;

	private bool _isViewPopupOpen;

	private Popup _guidePopup;

	private bool _showingGuide;

	private WidgetViewModel _widgetModel;

	internal BlurEffect BlurEffect;

	internal RowDefinition HeadRow;

	internal Border DragPanel;

	internal Grid TitleHost;

	internal Border TitleBorder;

	internal Grid TitlePanel;

	internal TextBlock TitleName;

	internal Path TitleDropIcon;

	internal HoverIconButton UnLockGrid;

	internal HoverIconButton SyncGrid;

	internal HoverIconButton MoreGrid;

	internal EscPopup MorePopup;

	internal EscPopup ViewPopup;

	internal Canvas Container;

	internal QuadrantControl Quadrant1;

	internal QuadrantControl Quadrant2;

	internal QuadrantControl Quadrant3;

	internal QuadrantControl Quadrant4;

	internal Popup DragQuadrantPopup;

	internal VisualBrush DragVisual;

	private bool _contentLoaded;

	private bool isShowPopup { get; set; }

	private CustomMenuList moreMenu { get; set; }

	public bool IsLocked { get; set; }

	public bool InOperation { get; set; }

	internal bool IsWidgetMode => _widgetModel != null;

	private string WidgetActiveViewId => _widgetModel?.ActiveViewId;

	public event EventHandler<WidgetMoreAction> MoreAction;

	public MatrixContainer()
	{
		InitializeComponent();
		_delayLoadHandler.SetAction(DelayLoad);
		base.Loaded += delegate
		{
			BindEvents();
			LoadTask();
			TryShowMatrixGuide();
		};
		base.Unloaded += delegate
		{
			_guidePopup?.SetCurrentValue(Popup.IsOpenProperty, false);
			UnbindEvents();
		};
		InitQuadrants();
	}

	private void InitQuadrants()
	{
		_quadrants = new List<QuadrantControl> { Quadrant1, Quadrant2, Quadrant3, Quadrant4 };
		foreach (QuadrantControl quadrant in _quadrants)
		{
			quadrant.SetIdentity();
		}
		_quadrants.Sort((QuadrantControl a, QuadrantControl b) => (a.Quadrant?.sortOrder).GetValueOrDefault().CompareTo((b.Quadrant?.sortOrder).GetValueOrDefault()));
	}

	private void UnbindEvents()
	{
		TimeChangeNotifier.DayChanged -= OnDayChanged;
		DataChangedNotifier.TaskDefaultChanged -= OnTaskDefaultChanged;
		DataChangedNotifier.MatrixQuadrantChanged -= OnMatrixQuadrantChanged;
		DataChangedNotifier.SyncDone -= OnSyncChanged;
		TasksChangeEventManager.RemoveHandler(TaskChangeNotifier.Notifier, OnTasksChanged);
		DataChangedNotifier.ThemeModeChanged -= OnThemeModeChanged;
		GlobalEventManager.QuadrantSortChanged -= OnQuadrantSortChanged;
		_delayLoadHandler.StopAndClear();
	}

	private void BindEvents()
	{
		TimeChangeNotifier.DayChanged += OnDayChanged;
		DataChangedNotifier.MatrixQuadrantChanged += OnMatrixQuadrantChanged;
		DataChangedNotifier.TaskDefaultChanged += OnTaskDefaultChanged;
		DataChangedNotifier.SyncDone += OnSyncChanged;
		TasksChangeEventManager.AddHandler(TaskChangeNotifier.Notifier, OnTasksChanged);
		DataChangedNotifier.ThemeModeChanged += OnThemeModeChanged;
		GlobalEventManager.QuadrantSortChanged += OnQuadrantSortChanged;
		_delayLoadHandler.SetAction(DelayLoad);
	}

	private void OnTasksChanged(object sender, TasksChangeEventArgs e)
	{
		bool reloadRequested = false;
		Action reloadAction = delegate
		{
			reloadRequested = true;
		};
		foreach (QuadrantControl item in _quadrants.ToList())
		{
			item.TaskList.OnTasksChanged(e, reloadAction);
		}
		if (reloadRequested)
		{
			LoadTask();
		}
	}

	private void OnSyncChanged(object sender, SyncResult e)
	{
		if (!base.Dispatcher.CheckAccess())
		{
			base.Dispatcher.InvokeAsync(delegate
			{
				OnSyncChanged(sender, e);
			});
			return;
		}
		RefreshViewTitle();
		if (base.IsVisible && (e.RemoteProjectsChanged || e.RemoteTasksChanged || e.RemoteTagChanged || e.MatrixProfilesChanged))
		{
			LoadTask();
		}
	}

	private void OnQuadrantSortChanged(object sender, EventArgs e)
	{
		if (sender != null && sender.Equals(this))
		{
			return;
		}
		foreach (QuadrantControl quadrant in _quadrants)
		{
			quadrant.SetIdentity();
		}
		_quadrants.Sort((QuadrantControl a, QuadrantControl b) => (a.Quadrant?.sortOrder).GetValueOrDefault().CompareTo((b.Quadrant?.sortOrder).GetValueOrDefault()));
		SetQuadrantLocation();
	}

	private void OnThemeModeChanged(object sender, EventArgs e)
	{
		if (base.IsVisible)
		{
			LoadTask();
		}
	}

	private void OnTaskDefaultChanged(object sender, EventArgs e)
	{
		foreach (QuadrantControl quadrant in _quadrants)
		{
			if (!quadrant.TaskList.Equals(sender))
			{
				quadrant.SetIdentity();
			}
		}
	}

	private void DelayLoad(object sender, EventArgs e)
	{
		Utils.RunOnBackgroundThread(base.Dispatcher, Load);
	}

	private void OnDayChanged(object sender, object e)
	{
		if (base.IsVisible)
		{
			LoadTask();
		}
	}

	private void OnMatrixQuadrantChanged(object sender, int e)
	{
		Utils.RunOnBackgroundThread(base.Dispatcher, delegate
		{
			RefreshViewTitle();
			switch (e)
			{
			case 1:
				LoadQuadrant(Quadrant1);
				break;
			case 2:
				LoadQuadrant(Quadrant2);
				break;
			case 3:
				LoadQuadrant(Quadrant3);
				break;
			case 4:
				LoadQuadrant(Quadrant4);
				break;
			default:
				LoadTask();
				break;
			}
		});
	}

	private void Load()
	{
		LoadTask();
	}

	public Task LoadTask(bool restore = true)
	{
		return QueueLoad(null, restore);
	}

	internal Task LoadQuadrant(QuadrantControl quadrant, bool restore = true)
	{
		if (quadrant != null)
		{
			return QueueLoad(new List<QuadrantControl> { quadrant }, restore);
		}
		return Task.CompletedTask;
	}

	private Task QueueLoad(List<QuadrantControl> quadrants, bool restore)
	{
		if (!base.Dispatcher.CheckAccess())
		{
			return base.Dispatcher.InvokeAsync(() => QueueLoadOnUiThread(quadrants, restore)).Task.Unwrap();
		}
		return QueueLoadOnUiThread(quadrants, restore);
	}

	private Task QueueLoadOnUiThread(List<QuadrantControl> quadrants, bool restore)
	{
		lock (_loadTaskGate)
		{
			if (_loadTaskRunning)
			{
				_loadTaskPending = true;
				if (quadrants == null)
				{
					_pendingLoadAll = true;
					_pendingLoadQuadrants.Clear();
				}
				else if (!_pendingLoadAll)
				{
					foreach (QuadrantControl quadrant in quadrants)
					{
						_pendingLoadQuadrants.Add(quadrant);
					}
				}
				_pendingRestore &= restore;
				return _currentLoadTask;
			}
			_loadTaskRunning = true;
			_currentLoadTask = LoadTaskLoopAsync(quadrants, restore);
			return _currentLoadTask;
		}
	}

	private async Task LoadTaskLoopAsync(List<QuadrantControl> quadrants, bool restore)
	{
		try
		{
			while (true)
			{
				RefreshViewTitle();
				try
				{
					await LoadQuadrantsAsync(quadrants, restore);
				}
				finally
				{
					RefreshViewTitle();
				}
				lock (_loadTaskGate)
				{
					if (!_loadTaskPending)
					{
						_loadTaskRunning = false;
						break;
					}
					quadrants = (_pendingLoadAll ? null : _pendingLoadQuadrants.ToList());
					restore = _pendingRestore;
					_pendingLoadAll = false;
					_pendingLoadQuadrants.Clear();
					_pendingRestore = true;
					_loadTaskPending = false;
				}
			}
		}
		catch
		{
			lock (_loadTaskGate)
			{
				_loadTaskRunning = false;
				_loadTaskPending = false;
				_pendingLoadAll = false;
				_pendingLoadQuadrants.Clear();
				_pendingRestore = true;
			}
			throw;
		}
	}

	private async Task LoadQuadrantsAsync(List<QuadrantControl> requestedQuadrants, bool restore)
	{
		if (requestedQuadrants == null)
		{
			while (TickDragHelper.Dragging || DragQuadrantPopup.IsOpen)
			{
				await Task.Delay(100);
			}
			foreach (QuadrantControl quadrant in _quadrants)
			{
				quadrant.SetIdentity();
			}
			_quadrants.Sort((QuadrantControl a, QuadrantControl b) => (a.Quadrant?.sortOrder).GetValueOrDefault().CompareTo((b.Quadrant?.sortOrder).GetValueOrDefault()));
			SetQuadrantLocation();
		}
		HashSet<QuadrantControl> requested = ((requestedQuadrants == null) ? null : new HashSet<QuadrantControl>(requestedQuadrants));
		await Task.WhenAll(((IEnumerable<QuadrantControl>)_quadrants.Where((QuadrantControl quadrant) => requested == null || requested.Contains(quadrant)).ToList()).Select((Func<QuadrantControl, Task>)async delegate(QuadrantControl quadrant)
		{
			_ = 1;
			try
			{
				while (TickDragHelper.Dragging)
				{
					await Task.Delay(100);
				}
				await quadrant.LoadFromMatrixAsync(restore);
			}
			catch (Exception arg)
			{
				UtilLog.Warn($"MatrixContainer.LoadQuadrantsAsync level={quadrant.Level}, ex: {arg}");
			}
		}));
	}

	public void Toast(string toast)
	{
		GetToastParent()?.TryToastString(null, toast);
	}

	private IToastShowWindow GetToastParent()
	{
		return Utils.FindParent<IToastShowWindow>(this);
	}

	private void OnMouseUp(object sender, MouseButtonEventArgs e)
	{
		QuadrantControl mousePointElement = Utils.GetMousePointElement<QuadrantControl>(e, this);
		if (mousePointElement == null)
		{
			return;
		}
		foreach (QuadrantControl quadrant in _quadrants)
		{
			if (!quadrant.Equals(mousePointElement))
			{
				quadrant.ClearSelected();
			}
		}
	}

	internal bool AddButtonMoveOver()
	{
		return _quadrants.Any((QuadrantControl quad) => quad.AddButton.IsMouseOver);
	}

	public void Reload(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			LoadTask();
			return;
		}
		foreach (QuadrantControl quadrant in _quadrants)
		{
			if (quadrant.Quadrant?.id == id)
			{
				quadrant.LoadAsync();
			}
		}
	}

	internal MatrixProfileModel GetActiveProfileSync()
	{
		if (!IsWidgetMode)
		{
			return MatrixProfileDao.GetActiveProfileSync();
		}
		MatrixProfileDao.EnsureMigratedSync();
		List<MatrixProfileModel> profilesSync = MatrixProfileDao.GetProfilesSync();
		return profilesSync.FirstOrDefault((MatrixProfileModel p) => string.Equals(p.sid, WidgetActiveViewId, StringComparison.Ordinal)) ?? MatrixProfileDao.GetActiveProfileSync() ?? profilesSync.FirstOrDefault();
	}

	internal List<QuadrantModel> GetActiveQuadrantsSync()
	{
		if (!IsWidgetMode)
		{
			return MatrixProfileDao.GetActiveQuadrantsSync();
		}
		MatrixProfileModel activeProfileSync = GetActiveProfileSync();
		if (activeProfileSync != null)
		{
			return MatrixProfileDao.GetProfileQuadrantsSync(activeProfileSync.sid);
		}
		return MatrixModel.GetDefault().quadrants;
	}

	internal QuadrantModel GetQuadrantByLevel(int level)
	{
		if (!IsWidgetMode)
		{
			return MatrixManager.GetQuadrantByLevel(level);
		}
		return GetActiveQuadrantsSync()?.FirstOrDefault((QuadrantModel q) => q.id == "quadrant" + level) ?? QuadrantModel.GetDefault(level);
	}

	internal string GetQuadrantNameByLevel(int level)
	{
		QuadrantModel quadrantByLevel = GetQuadrantByLevel(level);
		if (string.IsNullOrEmpty(quadrantByLevel.name))
		{
			return Utils.GetString("MatrixTitleQuadrant" + level);
		}
		return quadrantByLevel.name;
	}

	internal async void SaveQuadrant(int level, string rule, SortOption sort, string name)
	{
		if (!IsWidgetMode)
		{
			LocalSettings.Settings.SaveMatrixQuadrant(level, rule, sort, name);
			DataChangedNotifier.NotifyMatrixQuadrantChanged(level);
			return;
		}
		MatrixProfileModel profile = GetActiveProfileSync();
		if (profile != null)
		{
			await MatrixProfileDao.UpdateProfileQuadrantAsync(profile.sid, level, rule, sort, name);
			if (profile != null && string.Equals(LocalSettings.Settings.MatrixActiveViewId, profile.sid, StringComparison.Ordinal))
			{
				LocalSettings.Settings.OnMatrixQuadrantChanged();
				DataChangedNotifier.NotifyMatrixQuadrantChanged(level);
			}
			else
			{
				OnMatrixQuadrantChanged(this, level);
			}
		}
	}

	public async Task BatchPinTask()
	{
		foreach (QuadrantControl quadrant in _quadrants)
		{
			quadrant.BatchPinTask().ContinueWith(UtilRun.LogTask);
		}
	}

	public void BatchOpenSticky()
	{
		foreach (QuadrantControl quadrant in _quadrants)
		{
			quadrant.BatchOpenSticky();
		}
	}

	public void TryBatchSetDate(DateTime? date)
	{
		foreach (QuadrantControl quadrant in _quadrants)
		{
			quadrant.BatchSetDate(date).ContinueWith(UtilRun.LogTask);
		}
	}

	public void TryBatchSetPriority(int priority)
	{
		foreach (QuadrantControl quadrant in _quadrants)
		{
			quadrant.BatchSetPriority(priority).ContinueWith(UtilRun.LogTask);
		}
	}

	private void RefreshViewTitle()
	{
		try
		{
			MatrixProfileDao.EnsureMigratedSync();
			List<MatrixProfileModel> profilesSync = MatrixProfileDao.GetProfilesSync();
			MatrixProfileModel activeProfileSync = GetActiveProfileSync();
			TitleName.Text = activeProfileSync?.DisplayName ?? Utils.GetString("Matrix");
			SetViewSwitchState(profilesSync.Count > 1);
		}
		catch (Exception ex)
		{
			UtilLog.Warn("MatrixContainer.RefreshViewTitle " + ex.Message);
			TitleName.Text = Utils.GetString("Matrix");
			SetViewSwitchState(canSwitchView: false);
		}
	}

	private void SetViewSwitchState(bool canSwitchView)
	{
		TitleDropIcon.Visibility = ((!canSwitchView) ? Visibility.Collapsed : Visibility.Visible);
		if (canSwitchView)
		{
			TitleBorder.SetResourceReference(FrameworkElement.StyleProperty, "ClickIconBorderStyle");
		}
		else
		{
			TitleBorder.Style = null;
		}
	}

	private async void TryShowMatrixGuide(int delayMilliseconds = 800)
	{
		if (_showingGuide || Window.GetWindow(this) is WidgetWindow)
		{
			return;
		}
		_showingGuide = true;
		try
		{
			if (delayMilliseconds > 0)
			{
				await Task.Delay(delayMilliseconds);
			}
			if (!base.IsVisible)
			{
				return;
			}
			MatrixProfileDao.EnsureMigratedSync();
			List<MatrixProfileModel> profilesSync = MatrixProfileDao.GetProfilesSync();
			ExtraSettings extraSettings = LocalSettings.Settings.ExtraSettings;
			if (extraSettings == null)
			{
				return;
			}
			bool flag = false;
			if (!extraSettings.CheckHasFlag(HasSetFlag.MatrixManageViewsHint))
			{
				flag = await ShowMatrixGuide(MoreGrid, Utils.GetString("ManageMultipleViews"), HasSetFlag.MatrixManageViewsHint);
				profilesSync = MatrixProfileDao.GetProfilesSync();
				RefreshViewTitle();
			}
			if (profilesSync.Count > 1 && !extraSettings.CheckHasFlag(HasSetFlag.MatrixSwitchViewsHint))
			{
				if (flag)
				{
					await Task.Delay(100);
				}
				await ShowMatrixGuide(TitleBorder, Utils.GetString("ClickToSwitchViews"), HasSetFlag.MatrixSwitchViewsHint, alignLeftToTarget: true);
			}
		}
		catch (Exception ex)
		{
			UtilLog.Warn("MatrixContainer.TryShowMatrixGuide " + ex.Message);
		}
		finally
		{
			_showingGuide = false;
		}
	}

	private async Task<bool> ShowMatrixGuide(UIElement target, string text, HasSetFlag flag, bool alignLeftToTarget = false)
	{
		ExtraSettings extraSettings = LocalSettings.Settings.ExtraSettings;
		if (target == null || extraSettings == null || extraSettings.CheckHasFlag(flag) || !base.IsVisible || !target.IsVisible)
		{
			return false;
		}
		_guidePopup?.SetCurrentValue(Popup.IsOpenProperty, false);
		extraSettings.SetHasFlag(flag);
		LocalSettings.Settings.SetChanged();
		LocalSettings.Settings.Save(force: true).ContinueWith(UtilRun.LogTask);
		ArrowHintUserControl arrowHintUserControl = new ArrowHintUserControl
		{
			Text = text,
			ArrowDirection = ArrowHintUserControl.ArrowDirectionEnum.Up,
			ContentPadding = new Thickness(10.0, 6.0, 10.0, 6.0),
			MaxWidth = Math.Max(80.0, GetGuideRootWidth() - 16.0)
		};
		arrowHintUserControl.Measure(new Size(arrowHintUserControl.MaxWidth, double.PositiveInfinity));
		double guideTargetLeft = GetGuideTargetLeft(target);
		double guideTargetWidth = GetGuideTargetWidth(target);
		double width = arrowHintUserControl.DesiredSize.Width;
		double num = guideTargetLeft + guideTargetWidth / 2.0;
		double val = (alignLeftToTarget ? guideTargetLeft : (num - width / 2.0));
		double num2 = 8.0;
		double num3 = Math.Max(0.0, GetGuideRootWidth() - width - num2);
		val = ((num3 < num2) ? num3 : Math.Max(num2, Math.Min(val, num3)));
		if (alignLeftToTarget)
		{
			arrowHintUserControl.ArrowHorizontalAlignment = HorizontalAlignment.Left;
			arrowHintUserControl.ArrowOffset = 17.0;
		}
		else
		{
			double val2 = num - val;
			double num4 = 10.0;
			val2 = Math.Max(num4, Math.Min(val2, width - num4));
			if (Math.Abs(val2 - width / 2.0) <= 1.0)
			{
				arrowHintUserControl.ArrowHorizontalAlignment = HorizontalAlignment.Center;
			}
			else if (val2 < width / 2.0)
			{
				arrowHintUserControl.ArrowHorizontalAlignment = HorizontalAlignment.Left;
				arrowHintUserControl.ArrowOffset = Math.Max(0.0, val2 - 10.0);
			}
			else
			{
				arrowHintUserControl.ArrowHorizontalAlignment = HorizontalAlignment.Right;
				arrowHintUserControl.ArrowOffset = Math.Max(0.0, width - val2 - 10.0);
			}
		}
		Popup popup = new Popup
		{
			AllowsTransparency = true,
			StaysOpen = false,
			PlacementTarget = target,
			Placement = PlacementMode.Bottom,
			HorizontalOffset = val - guideTargetLeft,
			VerticalOffset = 4.0,
			Child = arrowHintUserControl
		};
		_guidePopup = popup;
		TaskCompletionSource<object> closedTaskSource = new TaskCompletionSource<object>();
		EventHandler closedHandler = null;
		closedHandler = delegate
		{
			popup.Closed -= closedHandler;
			closedTaskSource.TrySetResult(null);
		};
		popup.Closed += closedHandler;
		popup.IsOpen = true;
		await closedTaskSource.Task;
		if (object.Equals(_guidePopup, popup))
		{
			_guidePopup = null;
		}
		return true;
	}

	private double GetGuideRootWidth()
	{
		double val = ((base.ActualWidth > 0.0) ? base.ActualWidth : base.RenderSize.Width);
		return Math.Max(80.0, val);
	}

	private double GetGuideTargetLeft(UIElement target)
	{
		try
		{
			return target.TransformToAncestor(this).Transform(new Point(0.0, 0.0)).X;
		}
		catch (Exception)
		{
			return 0.0;
		}
	}

	private double GetGuideTargetWidth(UIElement target)
	{
		if (target is FrameworkElement frameworkElement)
		{
			frameworkElement.UpdateLayout();
			if (frameworkElement.ActualWidth > 0.0)
			{
				return frameworkElement.ActualWidth;
			}
		}
		return Math.Max(12.0, target.RenderSize.Width);
	}

	private void OnTitleClick(object sender, MouseButtonEventArgs e)
	{
		MatrixProfileDao.EnsureMigratedSync();
		List<MatrixProfileModel> profilesSync = MatrixProfileDao.GetProfilesSync();
		if (profilesSync.Count > 1 && !_isViewPopupOpen)
		{
			string activeSid = GetActiveProfileSync()?.sid;
			List<CustomMenuItemViewModel> types = profilesSync.Select((MatrixProfileModel profile) => new CustomMenuItemViewModel((object)profile.sid, profile.DisplayName, (Geometry)null, "")
			{
				Selected = string.Equals(activeSid, profile.sid, StringComparison.Ordinal)
			}).ToList();
			_viewMenu = new CustomMenuList(types, ViewPopup);
			_viewMenu.Operated += OnViewSelected;
			_viewMenu.Closed += ViewMenuClosed;
			_viewMenu.Show();
			_isViewPopupOpen = true;
		}
	}

	private async void OnViewSelected(object sender, object e)
	{
		if (e is string text)
		{
			if (string.Equals(GetActiveProfileSync()?.sid, text, StringComparison.Ordinal))
			{
				RefreshViewTitle();
			}
			else if (!ProChecker.CheckPro(ProType.Matrix, Window.GetWindow(this)))
			{
				RefreshViewTitle();
			}
			else if (IsWidgetMode)
			{
				await SaveWidgetActiveProfileAsync(text);
				await LoadTask(restore: false);
			}
			else
			{
				await MatrixProfileDao.SetActiveProfileAsync(text);
				RefreshViewTitle();
			}
		}
	}

	private async Task SaveWidgetActiveProfileAsync(string sid)
	{
		if (_widgetModel != null && !string.Equals(_widgetModel.ActiveViewId, sid, StringComparison.Ordinal))
		{
			_widgetModel.ActiveViewId = sid;
			await _widgetModel.Save();
		}
	}

	private async void ViewMenuClosed(object sender, EventArgs e)
	{
		if (_viewMenu != null)
		{
			_viewMenu.Operated -= OnViewSelected;
			_viewMenu.Closed -= ViewMenuClosed;
		}
		await Task.Delay(100);
		_isViewPopupOpen = false;
	}

	private void OnMoreClick(object sender, MouseButtonEventArgs e)
	{
		if (Window.GetWindow(this) is WidgetWindow)
		{
			if (_widgetMorePopup == null)
			{
				_widgetMorePopup = new WidgetMorePopup(OnMorePopupClosed);
				_widgetMorePopup.SetPlaceTarget(MoreGrid);
				_widgetMorePopup.MoreAction += OnMoreMoreAction;
			}
			_widgetMorePopup.Show(new Point(-5.0, -10.0), WidgetType.Matrix);
			SetInOperation(inOperation: true);
		}
		else
		{
			if (isShowPopup)
			{
				return;
			}
			MatrixProfileDao.EnsureMigratedSync();
			List<MatrixProfileModel> profilesSync = MatrixProfileDao.GetProfilesSync();
			List<CustomMenuItemViewModel> list = new List<CustomMenuItemViewModel>();
			if (profilesSync.Count > 1)
			{
				list.Add(new CustomMenuItemViewModel("edit", Utils.GetString("Edit"), Utils.GetGeometry("Ic.TaskOM.Edit")));
			}
			list.AddRange(new CustomMenuItemViewModel[3]
			{
				new CustomMenuItemViewModel("manage", Utils.GetString("ManageView"), Utils.GetGeometry("Ic.TaskOM.ManageGroup")),
				new CustomMenuItemViewModel("showComplete", Utils.GetString(LocalSettings.Settings.MatrixShowCompleted ? "HideCompleted" : "ShowCompleted"), Utils.GetGeometry(LocalSettings.Settings.MatrixShowCompleted ? "Ic.TaskOM.HideCompleted" : "Ic.TaskOM.ShowCompleted")),
				new CustomMenuItemViewModel("guide", Utils.GetString("UserGuide"), Utils.GetGeometry("Ic.Matrix.UserGuide") ?? Utils.GetGeometry("Ic.Common.Help"))
			});
			moreMenu = new CustomMenuList(list, MorePopup);
			moreMenu.Operated += async delegate(object o, object c)
			{
				switch (c as string)
				{
				case "edit":
					OpenEditView();
					break;
				case "manage":
					OpenManageViews();
					break;
				case "showComplete":
					try
					{
						await LocalSettings.Settings.SaveMatrixShowCompleted(!LocalSettings.Settings.MatrixShowCompleted);
						DataChangedNotifier.NotifyMatrixQuadrantChanged(-1);
						break;
					}
					catch (Exception ex)
					{
						UtilLog.Warn("MatrixContainer.SaveMatrixShowCompleted " + ex.Message);
						break;
					}
				case "guide":
					ShowUserGuide();
					break;
				}
			};
			moreMenu.Closed += MoreMenu_Closed;
			moreMenu.Show();
			isShowPopup = true;
		}
	}

	private async void MoreMenu_Closed(object sender, EventArgs e)
	{
		if (moreMenu != null)
		{
			moreMenu.Closed -= MoreMenu_Closed;
		}
		await Task.Delay(100);
		isShowPopup = false;
	}

	private void OpenEditView()
	{
		MatrixProfileModel activeProfileSync = GetActiveProfileSync();
		if (activeProfileSync != null && new EditMatrixViewWindow(activeProfileSync)
		{
			Owner = Window.GetWindow(this)
		}.ShowDialog() == true)
		{
			RefreshViewTitle();
			TryShowMatrixGuide(0);
		}
	}

	private void OpenManageViews()
	{
		ManageMatrixViewsWindow manageMatrixViewsWindow = new ManageMatrixViewsWindow();
		manageMatrixViewsWindow.Owner = Window.GetWindow(this);
		manageMatrixViewsWindow.ShowDialog();
		RefreshViewTitle();
		TryShowMatrixGuide(0);
	}

	private void ShowUserGuide()
	{
		if (Utils.IsDida())
		{
			Utils.TryProcessStartUrl("https://help.dida365.com/articles/6950398710203285504");
		}
		else
		{
			Utils.TryProcessStartUrl("https://help.ticktick.com/articles/7055782055577124864");
		}
	}

	private void OnMoreMoreAction(object sender, WidgetMoreAction e)
	{
		this.MoreAction?.Invoke(this, e);
	}

	private void OnContainerSizeChanged(object sender, SizeChangedEventArgs e)
	{
		double num = e.NewSize.Width / 2.0 - 6.0;
		double num2 = e.NewSize.Height / 2.0 - 6.0;
		for (int i = 0; i < 4; i++)
		{
			_quadrantLocations[i] = new Rect((double)(i % 2) * (num + 12.0), (i < 2) ? 0.0 : (num2 + 12.0), num, num2);
		}
		if (!DragQuadrantPopup.IsOpen)
		{
			SetQuadrantLocation();
		}
	}

	private void SetQuadrantLocation()
	{
		for (int i = 0; i < 4; i++)
		{
			QuadrantControl quadrantControl = _quadrants[i];
			quadrantControl.Width = _quadrantLocations[i].Width;
			quadrantControl.Height = _quadrantLocations[i].Height;
			quadrantControl.BeginAnimation(Canvas.LeftProperty, null);
			quadrantControl.BeginAnimation(Canvas.TopProperty, null);
			quadrantControl.SetValue(Canvas.LeftProperty, _quadrantLocations[i].X);
			quadrantControl.SetValue(Canvas.TopProperty, _quadrantLocations[i].Y);
		}
	}

	public void StartDragQuadrant(QuadrantControl quadrant, MouseEventArgs args)
	{
		if (!DragQuadrantPopup.IsOpen)
		{
			_dragStartIndex = _quadrants.IndexOf(quadrant);
			if (_dragStartIndex >= 0)
			{
				_dragStartPosition = args.GetPosition(Container);
				_dragQuadrant = quadrant;
				_popupStartPoint = new Point((double)quadrant.GetValue(Canvas.LeftProperty), (double)quadrant.GetValue(Canvas.TopProperty));
				DragQuadrantPopup.HorizontalOffset = _popupStartPoint.X;
				DragQuadrantPopup.VerticalOffset = _popupStartPoint.Y;
				DragVisual.Visual = quadrant;
				DragQuadrantPopup.IsOpen = true;
				quadrant.BeginAnimation(Canvas.LeftProperty, null);
				quadrant.SetValue(Canvas.LeftProperty, Container.ActualWidth * -1.0);
				SetupDragEvent();
			}
		}
	}

	private void SetupDragEvent()
	{
		CaptureMouse();
		base.MouseMove -= OnDragMouseMove;
		base.MouseMove += OnDragMouseMove;
		base.MouseLeftButtonUp -= OnDragDrop;
		base.MouseLeftButtonUp += OnDragDrop;
	}

	private void OnDragDrop(object sender, MouseButtonEventArgs e)
	{
		OnQuadrantDrop();
	}

	private void OnDragMouseMove(object sender, MouseEventArgs e)
	{
		if (DragQuadrantPopup.IsOpen && e.LeftButton == MouseButtonState.Pressed)
		{
			Point position = e.GetPosition(Container);
			DragQuadrantPopup.HorizontalOffset = _popupStartPoint.X + (position - _dragStartPosition).X;
			DragQuadrantPopup.VerticalOffset = _popupStartPoint.Y + (position - _dragStartPosition).Y;
			for (int i = 0; i < 4; i++)
			{
				Rect rect = _quadrantLocations[i];
				if (i != _dragStartIndex && IsInRect(i, rect, position))
				{
					if (i != _dragHoverIndex)
					{
						SetLocationAnimation(_dragHoverIndex, _dragHoverIndex);
						_dragHoverIndex = i;
						TryMoveHoverQuadrant(i);
					}
					return;
				}
			}
			SetLocationAnimation(_dragHoverIndex, _dragHoverIndex);
			_dragHoverIndex = -1;
		}
		else
		{
			OnQuadrantDrop();
		}
		static bool IsInRect(int num, Rect rect2, Point point)
		{
			if ((point.X > rect2.Right + 6.0 && num % 2 == 0) || (point.X < rect2.Left - 6.0 && num % 2 == 1))
			{
				return false;
			}
			if ((point.Y > rect2.Bottom + 6.0 && num < 2) || (point.Y < rect2.Top - 6.0 && num >= 2))
			{
				return false;
			}
			return true;
		}
	}

	private async Task TryMoveHoverQuadrant(int index)
	{
		await Task.Delay(100);
		if (index == _dragHoverIndex)
		{
			SetLocationAnimation(index, _dragStartIndex);
		}
	}

	private void OnQuadrantDrop()
	{
		if (!DragQuadrantPopup.IsOpen)
		{
			return;
		}
		DragQuadrantPopup.IsOpen = false;
		if (_dragStartIndex >= 0)
		{
			_quadrants[_dragStartIndex].OnHeadMouseUp();
		}
		if (_dragHoverIndex >= 0)
		{
			_quadrants[_dragStartIndex] = _quadrants[_dragHoverIndex];
			_quadrants[_dragHoverIndex] = _dragQuadrant;
			Dictionary<string, long> dictionary = new Dictionary<string, long>();
			for (int i = 0; i < 4; i++)
			{
				QuadrantControl quadrantControl = _quadrants[i];
				dictionary[quadrantControl.Quadrant.id] = (long)i * 268435456L;
			}
			UpdateQuadrantsSortOrder(dictionary);
		}
		SetQuadrantLocation();
		ReleaseMouseCapture();
		base.MouseMove -= OnDragMouseMove;
		base.MouseLeftButtonUp -= OnDragDrop;
		_dragStartIndex = -1;
		_dragHoverIndex = -1;
		_dragQuadrant = null;
		DragVisual.Visual = null;
	}

	private void UpdateQuadrantsSortOrder(Dictionary<string, long> orders)
	{
		if (!IsWidgetMode)
		{
			MatrixManager.UpdateQuadrantsSortOrder(orders, this);
			return;
		}
		MatrixProfileModel activeProfileSync = GetActiveProfileSync();
		if (activeProfileSync != null)
		{
			MatrixProfileDao.UpdateProfileQuadrantOrdersSync(activeProfileSync.sid, orders);
			GlobalEventManager.NotifyQuadrantSortChanged(this);
			if (string.Equals(LocalSettings.Settings.MatrixActiveViewId, activeProfileSync.sid, StringComparison.Ordinal))
			{
				LocalSettings.Settings.OnMatrixQuadrantChanged();
			}
		}
	}

	private void SetLocationAnimation(int from, int to)
	{
		if (from >= 0 && from != _dragStartIndex)
		{
			QuadrantControl value = _quadrants[from];
			Storyboard obj = (Storyboard)FindResource("DragMoveStory");
			if (obj.Children[0] is DoubleAnimation doubleAnimation)
			{
				doubleAnimation.To = _quadrantLocations[to].X;
				Storyboard.SetTarget(doubleAnimation, value);
			}
			if (obj.Children[1] is DoubleAnimation doubleAnimation2)
			{
				doubleAnimation2.To = _quadrantLocations[to].Y;
				Storyboard.SetTarget(doubleAnimation2, value);
			}
			obj.Begin();
		}
	}

	public void BeginSyncStory()
	{
		Storyboard obj = ((MoreGrid.Visibility == Visibility.Visible) ? ((Storyboard)FindResource("UnlockedSyncStory")) : ((Storyboard)FindResource("LockedSyncStory")));
		SyncGrid.Visibility = Visibility.Visible;
		obj.Begin();
		SyncGrid.IsHitTestVisible = false;
	}

	private void SyncStoryCompleted(object sender, EventArgs e)
	{
		if (MoreGrid.Visibility != Visibility.Visible)
		{
			SyncGrid.IsHitTestVisible = true;
		}
		else
		{
			SyncGrid.Visibility = Visibility.Collapsed;
		}
	}

	public void SetLocked(bool isLocked)
	{
		IsLocked = isLocked;
		DragPanel.IsEnabled = !isLocked;
		UnLockGrid.Visibility = ((!isLocked) ? Visibility.Collapsed : Visibility.Visible);
		MoreGrid.Visibility = (isLocked ? Visibility.Collapsed : Visibility.Visible);
		SyncGrid.BeginAnimation(UIElement.OpacityProperty, null);
		SyncGrid.Opacity = (isLocked ? 1 : 0);
		SyncGrid.Visibility = ((!isLocked) ? Visibility.Collapsed : Visibility.Visible);
		SyncGrid.IsHitTestVisible = isLocked;
		foreach (QuadrantControl quadrant in _quadrants)
		{
			quadrant.SetLocked(isLocked);
		}
	}

	public void SetQuadrantBackOpacity(double opacity)
	{
		foreach (QuadrantControl quadrant in _quadrants)
		{
			quadrant.SetBackBorderOpacity(opacity);
		}
	}

	public void SetWidgetMode(WidgetViewModel model)
	{
		_widgetModel = model;
		TitleHost.Margin = new Thickness(0.0, 0.0, 0.0, 10.0);
		TitleName.FontSize = 18.0;
		HeadRow.Height = new GridLength(44.0);
		DragPanel.Cursor = Cursors.SizeAll;
		Quadrant1.SetWidgetBackColor(model.Opacity);
		Quadrant2.SetWidgetBackColor(model.Opacity);
		Quadrant3.SetWidgetBackColor(model.Opacity);
		Quadrant4.SetWidgetBackColor(model.Opacity);
	}

	public void SetBlur()
	{
		BlurEffect.Radius = ((!UserDao.IsPro()) ? 8 : 0);
		base.IsEnabled = UserDao.IsPro();
	}

	public void SetInOperation(bool inOperation)
	{
		InOperation = inOperation;
	}

	private void OnMorePopupClosed()
	{
		SetInOperation(inOperation: false);
	}

	public void ToastMoveProjectControl(string taskProjectId, string taskTitle, MoveToastType type)
	{
		GetToastParent()?.ToastMoveProjectControl(taskProjectId, taskTitle, type);
	}

	public void BatchDeleteTask()
	{
		foreach (QuadrantControl quadrant in _quadrants)
		{
			quadrant.BatchDeleteTask();
		}
	}

	public void ExpandOrFoldAllSection()
	{
		bool flag = _quadrants.All((QuadrantControl q) => q.TaskList.IsAllSectionOpen());
		foreach (QuadrantControl quadrant in _quadrants)
		{
			quadrant.ToggleAllSection(!flag);
		}
	}

	public void ExpandOrFoldAllTask()
	{
		bool flag = _quadrants.All((QuadrantControl q) => q.TaskList.IsAllTaskOpen());
		foreach (QuadrantControl quadrant in _quadrants)
		{
			quadrant.ExpandOrFoldAllTask(!flag);
		}
	}

	public void OnQuadrantTaskDragOver(QuadrantControl control)
	{
		foreach (QuadrantControl quadrant in _quadrants)
		{
			quadrant.SetDragOver(quadrant == control);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/eisenhower/matrixcontainer.xaml", UriKind.Relative);
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
			((MatrixContainer)target).PreviewMouseLeftButtonUp += OnMouseUp;
			break;
		case 2:
			((Storyboard)target).Completed += SyncStoryCompleted;
			break;
		case 3:
			((Storyboard)target).Completed += SyncStoryCompleted;
			break;
		case 4:
			BlurEffect = (BlurEffect)target;
			break;
		case 5:
			HeadRow = (RowDefinition)target;
			break;
		case 6:
			DragPanel = (Border)target;
			break;
		case 7:
			TitleHost = (Grid)target;
			break;
		case 8:
			TitleBorder = (Border)target;
			TitleBorder.MouseLeftButtonUp += OnTitleClick;
			break;
		case 9:
			TitlePanel = (Grid)target;
			break;
		case 10:
			TitleName = (TextBlock)target;
			break;
		case 11:
			TitleDropIcon = (Path)target;
			break;
		case 12:
			UnLockGrid = (HoverIconButton)target;
			break;
		case 13:
			SyncGrid = (HoverIconButton)target;
			break;
		case 14:
			MoreGrid = (HoverIconButton)target;
			break;
		case 15:
			MorePopup = (EscPopup)target;
			break;
		case 16:
			ViewPopup = (EscPopup)target;
			break;
		case 17:
			Container = (Canvas)target;
			Container.SizeChanged += OnContainerSizeChanged;
			break;
		case 18:
			Quadrant1 = (QuadrantControl)target;
			break;
		case 19:
			Quadrant2 = (QuadrantControl)target;
			break;
		case 20:
			Quadrant3 = (QuadrantControl)target;
			break;
		case 21:
			Quadrant4 = (QuadrantControl)target;
			break;
		case 22:
			DragQuadrantPopup = (Popup)target;
			break;
		case 23:
			DragVisual = (VisualBrush)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
