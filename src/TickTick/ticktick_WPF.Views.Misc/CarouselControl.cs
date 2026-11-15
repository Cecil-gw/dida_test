using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using TickTickUtils.Lang;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc;

public class CarouselControl : Grid, IComponentConnector, IStyleConnector
{
	private CarouselItemViewModel _selectedItem;

	private Timer _timer = new Timer(3000.0);

	private List<CarouselItemViewModel> _items;

	private bool _isDark;

	internal ColumnDefinition FirstColumn;

	internal Image FrontImage;

	internal Image NextImage;

	internal TextBlock Title;

	internal ItemsControl SelectableItems;

	internal Border SlideButton;

	private bool _contentLoaded;

	public CarouselControl()
	{
		InitializeComponent();
		_timer.Elapsed += TryStartScroll;
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
		CarouselItemViewModel selectedItem = _selectedItem;
		carouselItemViewModel.Selected = true;
		_selectedItem = carouselItemViewModel;
		MoveToNext(selectedItem, carouselItemViewModel, num * 8);
	}

	public void InitData(List<CarouselItemViewModel> models)
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
			CarouselItemViewModel carouselItemViewModel = models[0];
			carouselItemViewModel.Selected = true;
			_selectedItem = carouselItemViewModel;
			_isDark = App.Instance?.FindResource("IsDarkTheme") as bool? == true;
			FrontImage.Source = new BitmapImage(new Uri(string.Format(carouselItemViewModel.ImageUrl, _isDark ? "dark" : "light")));
			Title.Text = carouselItemViewModel.Title;
			_timer.Start();
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
			_timer.Stop();
			if (num2 > num)
			{
				MoveToFront(selectedItem, next, num * 8);
			}
			else
			{
				MoveToNext(selectedItem, next, num * 8);
			}
			_timer.Start();
		}
	}

	private void MoveToFront(CarouselItemViewModel current, CarouselItemViewModel next, double x)
	{
		FrontImage.Source = new BitmapImage(new Uri(string.Format(next.ImageUrl, _isDark ? "dark" : "light")));
		NextImage.Source = new BitmapImage(new Uri(string.Format(current.ImageUrl, _isDark ? "dark" : "light")));
		Storyboard obj = (Storyboard)FindResource("MoveToFrontStory");
		((DoubleAnimation)obj.Children[3]).To = x;
		obj.Begin();
		Title.Text = next.Title;
	}

	private void MoveToNext(CarouselItemViewModel current, CarouselItemViewModel next, double x)
	{
		try
		{
			FrontImage.Source = new BitmapImage(new Uri(string.Format(current.ImageUrl, _isDark ? "dark" : "light")));
			NextImage.Source = new BitmapImage(new Uri(string.Format(next.ImageUrl, _isDark ? "dark" : "light")));
			Storyboard obj = (Storyboard)FindResource("MoveToNextStory");
			((DoubleAnimation)obj.Children[3]).To = x;
			obj.Begin();
			Title.Text = next.Title;
		}
		catch (Exception)
		{
			UtilLog.Info("CarouselControl.MoveNext " + current.ImageUrl + "  " + next.ImageUrl);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/misc/carouselcontrol.xaml", UriKind.Relative);
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
			FirstColumn = (ColumnDefinition)target;
			break;
		case 2:
			FrontImage = (Image)target;
			break;
		case 3:
			NextImage = (Image)target;
			break;
		case 4:
			Title = (TextBlock)target;
			break;
		case 5:
			SelectableItems = (ItemsControl)target;
			break;
		case 7:
			SlideButton = (Border)target;
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
		if (connectionId == 6)
		{
			((Border)target).MouseLeftButtonUp += OnItemClick;
		}
	}
}
