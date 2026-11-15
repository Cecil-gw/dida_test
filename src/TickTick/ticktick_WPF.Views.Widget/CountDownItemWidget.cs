using System;
using System.CodeDom.Compiler;
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
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.CountDown.ViewModels;
using ticktick_WPF.Views.CountDown.Views;

namespace ticktick_WPF.Views.Widget;

public class CountDownItemWidget : WidgetBaseUserControl, IComponentConnector
{
	internal CountDownItemWidget Root;

	internal CountDownCardWidget CountdownView;

	internal Grid EmptyView;

	internal Image EmptyImage;

	internal new TextBlock AddText;

	internal Grid DragGrid;

	internal Grid LockedOptionGrid;

	internal Button UnlockWidgetButton;

	internal Grid MoreGrid;

	internal Button MorePath;

	private bool _contentLoaded;

	private CountDownViewModel CountDownVM { get; set; }

	public CountDownItemWidget(WidgetViewModel model)
		: base(model)
	{
		InitializeComponent();
		EmptyView.DataContext = (MoreGrid.DataContext = model);
		LoadData();
	}

	private void Root_Loaded(object sender, RoutedEventArgs e)
	{
		OnThemeChange(base.Model.ViewThemeId);
		LockWidget(base.Model.IsLocked);
		DataChangedNotifier.CountdownsChanged += DataChangedNotifier_CountdownsChanged;
		TimeChangeNotifier.DayChanged += TimeChangeNotifier_DayChanged;
	}

	private void Root_Unloaded(object sender, RoutedEventArgs e)
	{
		DataChangedNotifier.CountdownsChanged -= DataChangedNotifier_CountdownsChanged;
		TimeChangeNotifier.DayChanged -= TimeChangeNotifier_DayChanged;
	}

	private void TimeChangeNotifier_DayChanged(object sender, EventArgs e)
	{
		LoadData();
	}

	private void DataChangedNotifier_CountdownsChanged(object sender, CountdownNotifier e)
	{
		if (e?.Model?.Id == base.Model.CountdownId || string.IsNullOrEmpty(base.Model.CountdownId))
		{
			LoadData();
		}
	}

	public void UpdateOpacity()
	{
		if (CountDownVM != null && base.Model != null)
		{
			CountDownVM.WidgetOpacity = base.Model.Opacity;
		}
	}

	public async Task LoadData()
	{
		CountdownModel countdownModel;
		if (string.IsNullOrEmpty(base.Model.CountdownId))
		{
			countdownModel = await CountdownDao.GetNearestCountdown();
		}
		else
		{
			countdownModel = await CountdownDao.GetCountdownById(base.Model.CountdownId);
			if (countdownModel == null || countdownModel.Status == 1)
			{
				countdownModel = await CountdownDao.GetNearestCountdown();
			}
		}
		if (countdownModel != null)
		{
			CountdownView.Visibility = Visibility.Visible;
			EmptyView.Visibility = Visibility.Collapsed;
			base.Model.UpdateCountdownModel(countdownModel);
			CountDownVM = new CountDownViewModel(countdownModel, CountDownViewModel.FromSource.Widget);
			CountDownVM.WidgetId = base.Model.Id;
			UpdateOpacity();
			CountdownView.DataContext = CountDownVM;
		}
		else
		{
			if (Utils.IsTickPackage())
			{
				AddText.Text = Utils.GetString("add_countdown");
			}
			else
			{
				AddText.Text = Utils.GetString("add_anniversary");
			}
			CountdownView.Visibility = Visibility.Collapsed;
			EmptyView.Visibility = Visibility.Visible;
		}
	}

	internal override void OnThemeChange(string themeKey)
	{
		EmptyImage.Source = Utils.GetImageSource("EmptyCountdownWidgetDrawingImage", this);
	}

	internal override void CloseWidget()
	{
		CountdownWidgetsHelper.CloseSingleCountdown(base.Model.Id);
		GetParentWindow()?.CloseWidget();
	}

	internal override void OnOpacityChange(float opacity)
	{
		UpdateOpacity();
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

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/widget/countdownitemwidget.xaml", UriKind.Relative);
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
			Root = (CountDownItemWidget)target;
			break;
		case 2:
			CountdownView = (CountDownCardWidget)target;
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
			DragGrid = (Grid)target;
			DragGrid.PreviewMouseLeftButtonDown += base.OnDragBarMouseDown;
			DragGrid.MouseMove += base.OnDragMove;
			break;
		case 8:
			LockedOptionGrid = (Grid)target;
			break;
		case 9:
			UnlockWidgetButton = (Button)target;
			UnlockWidgetButton.Click += base.OnUnlockWidgetClick;
			break;
		case 10:
			MoreGrid = (Grid)target;
			break;
		case 11:
			MorePath = (Button)target;
			MorePath.Click += OnMoreClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
