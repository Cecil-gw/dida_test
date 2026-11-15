using System;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Resource;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Pomo.MiniFocus;

public class PomoStatisticsSetView : UserControl, IComponentConnector, IStyleConnector
{
	private EscPopup _popup;

	private ObservableCollection<PomoStatisticsSetItemViewModel> _items;

	private int _selectIndex;

	internal ItemsControl Items;

	private bool _contentLoaded;

	public event EventHandler PopupClosed;

	public event EventHandler TypeChanged;

	public PomoStatisticsSetView(int selectedIndex, int anotherSelectedIndex)
	{
		InitializeComponent();
		_items = new ObservableCollection<PomoStatisticsSetItemViewModel>
		{
			new PomoStatisticsSetItemViewModel
			{
				Name = MiniFocusStatisticsView.MiniStatisticsTodayDuration
			},
			new PomoStatisticsSetItemViewModel
			{
				Name = MiniFocusStatisticsView.MiniStatisticsTodayPomo
			},
			new PomoStatisticsSetItemViewModel
			{
				Name = MiniFocusStatisticsView.MiniStatisticsWeekDuration
			},
			new PomoStatisticsSetItemViewModel
			{
				Name = MiniFocusStatisticsView.MiniStatisticsWeekPomo
			}
		};
		_items[selectedIndex].Selected = true;
		_items[anotherSelectedIndex].IsEnabled = false;
		_selectIndex = selectedIndex;
		Items.ItemsSource = _items;
	}

	private void OnItemClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: PomoStatisticsSetItemViewModel { Selected: false } dataContext })
		{
			int num = _items.IndexOf(dataContext);
			if (LocalSettings.Settings.PomoLocalSetting.MiniStatisticsTypes.Contains(_selectIndex.ToString()) && !LocalSettings.Settings.PomoLocalSetting.MiniStatisticsTypes.Contains(num.ToString()))
			{
				LocalSettings.Settings.PomoLocalSetting.MiniStatisticsTypes = LocalSettings.Settings.PomoLocalSetting.MiniStatisticsTypes.Replace(_selectIndex.ToString(), num.ToString());
				this.TypeChanged?.Invoke(this, null);
				_popup.IsOpen = false;
			}
		}
	}

	public void Show(UIElement element)
	{
		_popup = new EscPopup
		{
			StaysOpen = false,
			PlacementTarget = element,
			Placement = PlacementMode.Center,
			VerticalOffset = 74.0
		};
		_popup.Closed += delegate
		{
			this.PopupClosed?.Invoke(this, null);
			this.PopupClosed = null;
			this.TypeChanged = null;
		};
		_popup.Child = this;
		_popup.IsOpen = true;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/pomo/minifocus/pomostatisticssetview.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 1)
		{
			Items = (ItemsControl)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 2)
		{
			((ContentControl)target).MouseLeftButtonUp += OnItemClick;
		}
	}
}
