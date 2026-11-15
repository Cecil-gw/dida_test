using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.CountDown.ViewModels;
using ticktick_WPF.Views.CustomControl;

namespace ticktick_WPF.Views.Widget;

public class CountDownListWidget : WidgetBaseUserControl, IComponentConnector
{
	internal CountDownListWidget Root;

	internal Border WidgetBackground;

	internal Grid EmptyView;

	internal Image EmptyImage;

	internal new TextBlock AddText;

	internal SmoothScrollViewer MainScrollViewer;

	internal ItemsControl MainList;

	internal Grid DragGrid;

	internal Grid LockedOptionGrid;

	internal Button UnlockWidgetButton;

	internal Grid MoreGrid;

	internal Button MorePath;

	private bool _contentLoaded;

	private DateTime lastUpdateSizeTime { get; set; }

	private double lastHeight { get; set; }

	private bool loaded { get; set; }

	public ObservableCollection<CountDownListItem> Items { get; set; } = new ObservableCollection<CountDownListItem>();

	public CountDownListWidget(WidgetViewModel model)
		: base(model)
	{
		InitializeComponent();
		base.DataContext = (base.Model = model);
		MainList.ItemsSource = Items;
	}

	private void UserControl_Loaded(object sender, RoutedEventArgs e)
	{
		loaded = true;
		lastHeight = base.ActualHeight;
		LoadItems();
		LockWidget(base.Model.IsLocked);
		DataChangedNotifier.CountdownsChanged += DataChangedNotifier_CountdownsChanged;
		TimeChangeNotifier.DayChanged += TimeChangeNotifier_DayChanged;
		OnThemeChange(base.Model.ViewThemeId);
	}

	private void Root_Unloaded(object sender, RoutedEventArgs e)
	{
		DataChangedNotifier.CountdownsChanged -= DataChangedNotifier_CountdownsChanged;
		TimeChangeNotifier.DayChanged -= TimeChangeNotifier_DayChanged;
	}

	private void TimeChangeNotifier_DayChanged(object sender, EventArgs e)
	{
		LoadItems();
	}

	private void DayChanged(object sender, EventArgs e)
	{
		LoadItems();
	}

	private void DataChangedNotifier_CountdownsChanged(object sender, CountdownNotifier e)
	{
		if (loaded)
		{
			LoadItems();
		}
	}

	private async Task LoadItems()
	{
		List<CountDownListItem> items = new List<CountDownListItem>();
		List<CountdownModel> allCountdowns = await CountdownDao.GetActiveCountdowns();
		EmptyView.Visibility = ((allCountdowns.Count > 0) ? Visibility.Collapsed : Visibility.Visible);
		if (Utils.IsTickPackage())
		{
			AddText.Text = Utils.GetString("add_countdown");
		}
		else
		{
			AddText.Text = Utils.GetString("add_anniversary");
		}
		foreach (CountdownModel item2 in await Task.Run(() => MainCountDownViewModel.SortAllModels(allCountdowns, isArchivedOrder: false)))
		{
			item2.GetTargetDate(item2.TimerMode);
			DisplayTimerMode? timerMode = item2.TimerMode;
			int daysRemaining = item2.GetDaysRemaining(null, null, timerMode);
			CountDownListItem item = new CountDownListItem
			{
				CountdownId = item2.Id,
				Title = item2.Name,
				IsPassed = (daysRemaining < 0),
				Icon = CountdownService.GetIcon(item2.IconRes, item2.Color),
				IsShowDayUnit = (daysRemaining != 0 && base.Model.IsShowUnit),
				Days = ((daysRemaining == 0) ? Utils.GetString("Today") : Math.Abs(daysRemaining).ToString()),
				DayFontSize = ((daysRemaining == 0) ? 20 : 22),
				IsNumber = (daysRemaining != 0)
			};
			items.Add(item);
		}
		for (int num = 0; num < items.Count; num++)
		{
			if (num < Items.Count)
			{
				Items[num] = items[num];
			}
			else
			{
				Items.Add(items[num]);
			}
		}
		for (int num2 = Items.Count - 1; num2 >= items.Count; num2--)
		{
			Items.RemoveAt(num2);
		}
	}

	private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (lastHeight > base.ActualHeight && MainScrollViewer.VerticalOffset + MainScrollViewer.ViewportHeight == MainScrollViewer.ExtentHeight)
		{
			MainScrollViewer.ScrollToVerticalOffset(MainScrollViewer.VerticalOffset - 0.5);
		}
		lastHeight = base.ActualHeight;
	}

	public void OnShowUnitChange()
	{
		if (loaded)
		{
			LoadItems();
		}
	}

	internal override void OnThemeChange(string themeKey)
	{
		EmptyImage.Source = Utils.GetImageSource("EmptyCountdownWidgetDrawingImage", this);
	}

	internal override void CloseWidget()
	{
		CountdownWidgetsHelper.CloseListWidget();
		GetParentWindow()?.CloseWidget();
	}

	private void OnMoreClick(object sender, RoutedEventArgs e)
	{
		OnMoreClick(MorePath);
	}

	internal override void LockWidget(bool isLocked)
	{
		base.LockWidget(isLocked);
		DragGrid.IsHitTestVisible = !isLocked;
		LockedOptionGrid.Visibility = ((!isLocked) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void EmptyPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		App.NavigateCountdown();
	}

	private void MainList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		App.NavigateCountdown();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/widget/countdownlistwidget.xaml", UriKind.Relative);
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
			Root = (CountDownListWidget)target;
			break;
		case 2:
			WidgetBackground = (Border)target;
			break;
		case 3:
			EmptyView = (Grid)target;
			break;
		case 4:
			((StackPanel)target).MouseLeftButtonDown += EmptyPanel_MouseLeftButtonDown;
			break;
		case 5:
			EmptyImage = (Image)target;
			break;
		case 6:
			AddText = (TextBlock)target;
			break;
		case 7:
			MainScrollViewer = (SmoothScrollViewer)target;
			break;
		case 8:
			MainList = (ItemsControl)target;
			MainList.MouseLeftButtonUp += MainList_MouseLeftButtonUp;
			break;
		case 9:
			DragGrid = (Grid)target;
			DragGrid.PreviewMouseLeftButtonDown += base.OnDragBarMouseDown;
			DragGrid.MouseMove += base.OnDragMove;
			break;
		case 10:
			LockedOptionGrid = (Grid)target;
			break;
		case 11:
			UnlockWidgetButton = (Button)target;
			UnlockWidgetButton.Click += base.OnUnlockWidgetClick;
			break;
		case 12:
			MoreGrid = (Grid)target;
			break;
		case 13:
			MorePath = (Button)target;
			MorePath.Click += OnMoreClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
