using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc;

public class ProCarouselControl : UserControl, IComponentConnector, IStyleConnector
{
	private CarouselItemViewModel _selectedItem;

	private Timer _timer = new Timer(5000.0);

	private List<CarouselItemViewModel> _items;

	private bool _timerStopped;

	internal ColumnDefinition FirstColumn;

	internal Image FrontImage;

	internal Image NextImage;

	internal ItemsControl SelectableItems;

	internal Border SlideButton;

	internal TranslateTransform DotTransform;

	internal Grid LeftArrow;

	internal Grid RightArrow;

	private bool _contentLoaded;

	public CarouselItemViewModel SelectedItem => _selectedItem;

	public event EventHandler<CarouselItemViewModel> CurrentChanged;

	public ProCarouselControl()
	{
		InitializeComponent();
		_timer.Elapsed += TryStartScroll;
		InitData(GetCalendarCarouselModels());
	}

	private void TryStartScroll(object sender, ElapsedEventArgs e)
	{
		Utils.RunOnBackgroundThread(base.Dispatcher, StartScroll);
	}

	private void StartScroll()
	{
		int num = _items.IndexOf(_selectedItem);
		num = ((num < _items.Count - 1) ? (num + 1) : 0);
		CarouselItemViewModel carouselItemViewModel = _items[num];
		_selectedItem.Selected = false;
		carouselItemViewModel.Selected = true;
		_selectedItem = carouselItemViewModel;
		MoveToNext(carouselItemViewModel, num * 8);
	}

	public async void InitData(List<CarouselItemViewModel> models)
	{
		if (models != null && models.Count != 0)
		{
			int[] array = new int[models.Count + 1];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = i;
			}
			SelectableItems.ItemsSource = array;
			_items = models;
			CarouselItemViewModel selected = models.FirstOrDefault((CarouselItemViewModel x) => x.Value == "Month") ?? models[0];
			int num = models.IndexOf(selected);
			if (num != 0)
			{
				MoveToNext(selected, num * 8);
			}
			selected.Selected = true;
			_selectedItem = selected;
			if (!File.Exists(selected.ImageUrl))
			{
				await ProChecker.TryDownloadBackground(Path.GetFileName(selected.ImageUrl));
			}
			if (File.Exists(selected.ImageUrl))
			{
				FrontImage.Source = new BitmapImage(new Uri(selected.ImageUrl));
			}
			SetTimerEnable(b: true);
			TryDownLoadImages(models.Select((CarouselItemViewModel m) => m.ImageUrl).ToList());
		}
	}

	private void SetTimerEnable(bool b)
	{
		try
		{
			if (b)
			{
				_timer?.Start();
				return;
			}
			_timerStopped = true;
			_timer?.Stop();
		}
		catch (Exception)
		{
		}
	}

	private async void TryDownLoadImages(List<string> select)
	{
		foreach (string item in select)
		{
			if (!File.Exists(item))
			{
				await ProChecker.TryDownloadBackground(Path.GetFileName(item));
			}
		}
	}

	private void OnItemClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: var dataContext } && dataContext is int num)
		{
			_selectedItem.Selected = false;
			CarouselItemViewModel selectedItem = _selectedItem;
			int num2 = _items.IndexOf(selectedItem);
			if (num > num2)
			{
				num--;
			}
			CarouselItemViewModel next = (_selectedItem = _items[num]);
			SetTimerEnable(b: false);
			MoveToNext(next, num * 8);
		}
	}

	private void MoveToFront(CarouselItemViewModel current, CarouselItemViewModel next, double x)
	{
		if (File.Exists(next.ImageUrl))
		{
			FrontImage.Source = new BitmapImage(new Uri(next.ImageUrl));
		}
		DotTransform.X = x;
		this.CurrentChanged?.Invoke(this, _selectedItem);
	}

	private void MoveToNext(CarouselItemViewModel next, double x)
	{
		try
		{
			if (File.Exists(next.ImageUrl))
			{
				FrontImage.Source = new BitmapImage(new Uri(next.ImageUrl));
			}
			DotTransform.X = x;
			this.CurrentChanged?.Invoke(this, _selectedItem);
		}
		catch (Exception)
		{
			UtilLog.Info("CarouselControl.MoveNext " + next.ImageUrl);
		}
	}

	public List<CarouselItemViewModel> GetCalendarCarouselModels()
	{
		string text = (Utils.IsZhCn() ? "_cn" : "_en");
		return new List<CarouselItemViewModel>
		{
			new CarouselItemViewModel
			{
				ImageUrl = AppPaths.UpgradeProDir + "CalendarProYear" + text + ".png",
				Title = Utils.GetString("YearView"),
				Desc = Utils.GetString("YearViewDesc"),
				Value = "Year"
			},
			new CarouselItemViewModel
			{
				ImageUrl = AppPaths.UpgradeProDir + "CalendarProMonth" + text + ".png",
				Title = Utils.GetString("MonthView"),
				Desc = Utils.GetString("MonthViewDesc"),
				Value = "Month"
			},
			new CarouselItemViewModel
			{
				ImageUrl = AppPaths.UpgradeProDir + "CalendarProWeek" + text + ".png",
				Title = Utils.GetString("WeekView"),
				Desc = Utils.GetString("WeekViewDesc"),
				Value = "Week"
			},
			new CarouselItemViewModel
			{
				ImageUrl = AppPaths.UpgradeProDir + "CalendarProDay" + text + ".png",
				Title = Utils.GetString("DayView"),
				Desc = Utils.GetString("DayViewDesc"),
				Value = "Day"
			},
			new CarouselItemViewModel
			{
				ImageUrl = AppPaths.UpgradeProDir + "CalendarProAgenda" + text + ".png",
				Title = Utils.GetString("AgendaView"),
				Desc = Utils.GetString("AgendaViewDesc"),
				Value = "Agenda"
			},
			new CarouselItemViewModel
			{
				ImageUrl = AppPaths.UpgradeProDir + "CalendarProMultiDays" + text + ".png",
				Title = Utils.GetString("MultiDayView"),
				Desc = Utils.GetString("MultiDayViewDesc"),
				Value = "MultiDays"
			},
			new CarouselItemViewModel
			{
				ImageUrl = AppPaths.UpgradeProDir + "CalendarProMultiWeeks" + text + ".png",
				Title = Utils.GetString("MultiWeekView"),
				Desc = Utils.GetString("MultiWeekViewDesc"),
				Value = "MultiWeeks"
			}
		};
	}

	private void OnStoryCompleted(object sender, EventArgs e)
	{
		this.CurrentChanged?.Invoke(this, _selectedItem);
	}

	public void Dispose()
	{
		this.CurrentChanged = null;
		Timer timer = _timer;
		_timer = null;
		timer.Dispose();
	}

	private void OnLeftClick(object sender, MouseButtonEventArgs e)
	{
		int num = _items.IndexOf(_selectedItem);
		num = ((num > 0) ? (num - 1) : (_items.Count - 1));
		CarouselItemViewModel carouselItemViewModel = _items[num];
		_selectedItem.Selected = false;
		carouselItemViewModel.Selected = true;
		_selectedItem = carouselItemViewModel;
		SetTimerEnable(b: false);
		MoveToNext(carouselItemViewModel, num * 8);
	}

	private void OnRightClick(object sender, MouseButtonEventArgs e)
	{
		int num = _items.IndexOf(_selectedItem);
		num = ((num < _items.Count - 1) ? (num + 1) : 0);
		CarouselItemViewModel carouselItemViewModel = _items[num];
		_selectedItem.Selected = false;
		_ = _selectedItem;
		carouselItemViewModel.Selected = true;
		_selectedItem = carouselItemViewModel;
		SetTimerEnable(b: false);
		MoveToNext(carouselItemViewModel, num * 8);
	}

	public bool IsButtonMouseOver()
	{
		if (!SelectableItems.IsMouseOver && !LeftArrow.IsMouseOver)
		{
			return RightArrow.IsMouseOver;
		}
		return true;
	}

	public string CurrentDisplayValue()
	{
		return _selectedItem.Value;
	}

	public bool IsTimerStop()
	{
		return _timerStopped;
	}

	public void StopTimer()
	{
		SetTimerEnable(b: false);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/misc/procarouselcontrol.xaml", UriKind.Relative);
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
			((Storyboard)target).Completed += OnStoryCompleted;
			break;
		case 2:
			((Storyboard)target).Completed += OnStoryCompleted;
			break;
		case 3:
			FirstColumn = (ColumnDefinition)target;
			break;
		case 4:
			FrontImage = (Image)target;
			break;
		case 5:
			NextImage = (Image)target;
			break;
		case 6:
			SelectableItems = (ItemsControl)target;
			break;
		case 8:
			SlideButton = (Border)target;
			break;
		case 9:
			DotTransform = (TranslateTransform)target;
			break;
		case 10:
			LeftArrow = (Grid)target;
			LeftArrow.MouseLeftButtonUp += OnLeftClick;
			break;
		case 11:
			RightArrow = (Grid)target;
			RightArrow.MouseLeftButtonUp += OnRightClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 7)
		{
			((Border)target).PreviewMouseLeftButtonUp += OnItemClick;
		}
	}
}
