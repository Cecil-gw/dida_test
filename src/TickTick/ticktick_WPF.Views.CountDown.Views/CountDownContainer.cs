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
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CountDown.ViewModels;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.CountDown.Views;

public class CountDownContainer : UserControl, IComponentConnector
{
	private bool _isLoadingMore;

	internal StackPanel TitlePanel;

	internal TextBlock Title;

	internal HoverIconButton AddButton;

	internal EscPopup AddActionPopup;

	internal HoverIconButton MoreButton;

	internal EscPopup MorePopup;

	internal GroupTitle CountdownGroupTitle;

	internal SmoothScrollViewer MainScrollViewer;

	internal ItemsControl CardsItemsControl;

	internal ContentControl EmptyGrid;

	internal Grid ToastGrid;

	private bool _contentLoaded;

	private static Action OnShowSectionChange { get; set; }

	private MainCountDownViewModel viewModel { get; set; }

	public CountDownContainer()
	{
		InitializeComponent();
		base.DataContext = (viewModel = new MainCountDownViewModel());
		MainCountDownViewModel mainCountDownViewModel2 = viewModel;
		mainCountDownViewModel2.OnGroupTitleUpdate = (Action<string>)Delegate.Combine(mainCountDownViewModel2.OnGroupTitleUpdate, new Action<string>(OnGroupTitleUpdate));
		MainCountDownViewModel mainCountDownViewModel3 = viewModel;
		mainCountDownViewModel3.OnShowGroupType = (Action<CountDownType>)Delegate.Combine(mainCountDownViewModel3.OnShowGroupType, new Action<CountDownType>(OnShowGroupType));
		InitEmptyView();
		MainScrollViewer.ScrollChanged += MainScrollViewer_ScrollChanged;
		TimeChangeNotifier.DayChanged += async delegate
		{
			await viewModel.LoadCountdownsWithLock();
		};
		DataChangedNotifier.CountdownsChanged += async delegate(object sender, CountdownNotifier args)
		{
			if (args?.Model == null)
			{
				await viewModel.LoadCountdownsWithLock();
			}
			else
			{
				await viewModel.UpdateItemWithSort(args.Model, args.Type);
			}
			TaskCountCache.TryReloadSmartCount();
		};
		LocalSettings settings = LocalSettings.Settings;
		settings.OnPreferenceChanged = (Action)Delegate.Combine(settings.OnPreferenceChanged, new Action(OnInitPerference));
		OnShowSectionChange = (Action)Delegate.Combine(OnShowSectionChange, new Action(OnInitPerference));
		DataChangedNotifier.CountdownShowRemarkChanged += DataChangedNotifier_CountdownShowRemarkChanged;
	}

	private void OnInitPerference()
	{
		viewModel.InitConfig();
	}

	public void OnShowGroupType(CountDownType type)
	{
		CountdownGroupTitle.SelectedTitle = CountdownModel.GetTypeResourceKey(type);
		viewModel.UpdateFilter(type);
	}

	public async Task OnShowGroupTypeRefresh(CountDownType type)
	{
		OnShowGroupType(type);
		await viewModel.LoadCountdownsWithLock(clear: true, noTimeLimit: true);
	}

	private void OnGroupTitleUpdate(string text)
	{
		CountdownGroupTitle.Titles = text;
	}

	private void DataChangedNotifier_CountdownShowRemarkChanged(object sender, EventArgs e)
	{
		viewModel.SwitchShowRemark(StaticPropertiesModel.Instance.AltKeyDown);
	}

	private void MainScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
	{
		if (!_isLoadingMore && sender is ScrollViewer scrollViewer && scrollViewer.VerticalOffset > scrollViewer.ScrollableHeight * 0.95 && viewModel.HasMoreItems)
		{
			_isLoadingMore = true;
			Task.Run(async delegate
			{
				await viewModel.LoadMoreItemsAsync();
				_isLoadingMore = false;
			});
		}
	}

	private void InitEmptyView()
	{
		EmptyWithTextView emptyWithTextView = new EmptyWithTextView();
		if (!viewModel.ShowArchived)
		{
			emptyWithTextView.SetData(Utils.GetString("empty_title_no_countdowns"), Utils.GetString("empty_desc_no_countdowns"), new EmptyIconsSourceModel("CountDownEmpty", new List<int> { 1, 2, 3, 4 }), setIcon5Color: false);
			EmptyGrid.Content = emptyWithTextView;
		}
		else
		{
			emptyWithTextView.SetData(Utils.GetString("empty_title_no_archived_countdowns"), Utils.GetString("empty_desc_no_archived_countdowns"), new EmptyIconsSourceModel("HabitListArchiveEmpty", new List<int> { 2, 4, 5 }), setIcon5Color: false);
			EmptyGrid.Content = emptyWithTextView;
		}
		DataChangedNotifier.ThemeModeChanged -= OnAppThemeChanged;
		DataChangedNotifier.ThemeModeChanged += OnAppThemeChanged;
	}

	private void OnAppThemeChanged(object sender, EventArgs e)
	{
		viewModel.LoadCountdownsWithLock();
	}

	private void OnMoreClick(object sender, MouseButtonEventArgs e)
	{
		List<CustomMenuItemViewModel> list = new List<CustomMenuItemViewModel>();
		if (viewModel.IsShowGroup)
		{
			list.Add(new CustomMenuItemViewModel("ShowGroup", Utils.GetString("HideGroup"), Utils.GetGeometry("Ic.Countdown.HideGroup"))
			{
				ShowSelected = false
			});
		}
		else
		{
			list.Add(new CustomMenuItemViewModel("ShowGroup", Utils.GetString("ShowGroup"), Utils.GetGeometry("Ic.Countdown.ShowGrouping"))
			{
				ShowSelected = false
			});
		}
		CustomMenuList customMenuList = new CustomMenuList(list, MorePopup);
		customMenuList.Operated += OnMoreItemSelected;
		customMenuList.Show();
	}

	private void OnMoreItemSelected(object sender, object e)
	{
		if (e.ToString() == "ShowGroup")
		{
			viewModel.IsShowGroup = !viewModel.IsShowGroup;
			LocalSettings.Settings.SaveShowCountdownSection(viewModel.IsShowGroup);
			OnShowSectionChange?.Invoke();
		}
	}

	private async void OnAddClick(object sender, MouseButtonEventArgs e)
	{
		UserActCollectUtils.AddClickEvent("countdown", "add", "add");
		List<CountdownModel> list = await CountdownDao.GetActiveCountdowns();
		long limitByKey = LimitCache.GetLimitByKey(Constants.LimitKind.CountdownNumber);
		if (!UserDao.IsPro())
		{
			if (list.Count >= limitByKey)
			{
				ProChecker.ShowUpgradeDialog(Utils.IsDida() ? ProType.MoreAnniversaries : ProType.MoreCountdowns, Window.GetWindow(this));
				return;
			}
		}
		else if (list.Count >= limitByKey)
		{
			new CustomerDialog(Utils.GetString("LimitTips"), string.Format(Utils.GetString(Utils.IsDida() ? "ProAnniversaryLimit" : "ProCountdownLimit"), limitByKey), MessageBoxButton.OK, Utils.GetToastWindow() as Window).ShowDialog();
			return;
		}
		List<CustomMenuItemViewModel> types = new List<CustomMenuItemViewModel>
		{
			new CustomMenuItemViewModel(CountDownType.Anniversary, Utils.GetString("type_anniversary"), Utils.GetGeometry("Ic.Countdown.Anniversary")),
			new CustomMenuItemViewModel(CountDownType.Countdown, Utils.GetString("type_countdown"), Utils.GetGeometry("Ic.Countdown.Countdown")),
			new CustomMenuItemViewModel(CountDownType.Birthday, Utils.GetString("type_birthday"), Utils.GetGeometry("Ic.Countdown.Birthday")),
			new CustomMenuItemViewModel(CountDownType.Holiday, Utils.GetString("type_holiday"), Utils.GetGeometry("Ic.Countdown.Festival"))
		};
		if (Utils.IsTickPackage())
		{
			types = new List<CustomMenuItemViewModel>
			{
				new CustomMenuItemViewModel(CountDownType.Countdown, Utils.GetString("type_countdown"), Utils.GetGeometry("Ic.Countdown.Countdown")),
				new CustomMenuItemViewModel(CountDownType.Anniversary, Utils.GetString("type_anniversary"), Utils.GetGeometry("Ic.Countdown.Anniversary")),
				new CustomMenuItemViewModel(CountDownType.Birthday, Utils.GetString("type_birthday"), Utils.GetGeometry("Ic.Countdown.Birthday")),
				new CustomMenuItemViewModel(CountDownType.Holiday, Utils.GetString("type_holiday"), Utils.GetGeometry("Ic.Countdown.Festival"))
			};
		}
		CustomMenuList customMenuList = new CustomMenuList(types, AddActionPopup);
		customMenuList.Operated += OnAddItemSelected;
		customMenuList.Show();
	}

	private void OnAddItemSelected(object sender, object countdownType)
	{
		AddCountDown((countdownType is CountDownType countDownType) ? countDownType : CountDownType.Countdown);
	}

	private void AddCountDown(CountDownType countdownType)
	{
		UserActCollectUtils.AddClickEvent("countdown", "add", "show_add_page");
		EditCountDownDialog countDownDialog = new EditCountDownDialog(null, countdownType)
		{
			Owner = Window.GetWindow(this)
		};
		countDownDialog.Closed += delegate
		{
			Window.GetWindow(this)?.Activate();
			if (countDownDialog.ChangedType.HasValue && viewModel.IsShowGroup && countDownDialog.ChangedType != viewModel.lastGroupType && viewModel.lastGroupType != CountDownType.All)
			{
				App.Window.Toast(new MoveToastControl(string.Format(Utils.GetString("AddedTo"), CountdownModel.GetTypeText(countDownDialog.ChangedType.Value)), delegate
				{
					OnShowGroupTypeRefresh(countDownDialog.ChangedType.Value);
				}));
			}
		};
		countDownDialog.ShowDialog();
	}

	private void OnArchiveToggleClick(object sender, MouseButtonEventArgs e)
	{
		EscPopup popup = new EscPopup
		{
			StaysOpen = false,
			PlacementTarget = TitlePanel,
			VerticalOffset = -8.0,
			Placement = PlacementMode.Bottom,
			HorizontalOffset = -8.0
		};
		CustomMenuList customMenuList = new CustomMenuList(new List<CustomMenuItemViewModel>
		{
			new CustomMenuItemViewModel((object)"Keeping", Utils.GetString("countdown_recording"), (Geometry)null, "")
			{
				Selected = !viewModel.ShowArchived
			},
			new CustomMenuItemViewModel((object)"Archived", Utils.GetString("Archived"), (Geometry)null, "")
			{
				Selected = viewModel.ShowArchived
			}
		}, popup);
		customMenuList.Operated += async delegate(object o, object val)
		{
			if (val is string text)
			{
				bool showArchived = text == "Archived";
				viewModel.ShowArchived = showArchived;
				InitEmptyView();
				Title.Text = (viewModel.ShowArchived ? Utils.GetString("ArchivedCountdowns") : Utils.GetString("CountdownTitle"));
			}
		};
		customMenuList.Show();
	}

	private async void GroupTitle_SelectedTitleChanged(object sender, GroupTitleViewModel e)
	{
		if (viewModel.UpdateFilter(e.Title))
		{
			await viewModel.LoadCountdownsWithLock(clear: true, noTimeLimit: true);
		}
		MainScrollViewer.ScrollToTop();
		UserActCollectUtils.AddClickEvent("countdown", "group", viewModel.lastGroupType.ToString().ToLower());
	}

	public void Clear()
	{
		viewModel.Clear();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/countdown/views/countdowncontainer.xaml", UriKind.Relative);
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
			TitlePanel = (StackPanel)target;
			TitlePanel.MouseLeftButtonUp += OnArchiveToggleClick;
			break;
		case 2:
			Title = (TextBlock)target;
			break;
		case 3:
			AddButton = (HoverIconButton)target;
			break;
		case 4:
			AddActionPopup = (EscPopup)target;
			break;
		case 5:
			MoreButton = (HoverIconButton)target;
			break;
		case 6:
			MorePopup = (EscPopup)target;
			break;
		case 7:
			CountdownGroupTitle = (GroupTitle)target;
			break;
		case 8:
			MainScrollViewer = (SmoothScrollViewer)target;
			break;
		case 9:
			CardsItemsControl = (ItemsControl)target;
			break;
		case 10:
			EmptyGrid = (ContentControl)target;
			break;
		case 11:
			ToastGrid = (Grid)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
