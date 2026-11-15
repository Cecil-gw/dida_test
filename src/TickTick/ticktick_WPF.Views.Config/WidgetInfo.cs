using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.AI;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Views.Config;

public class WidgetInfo : UserControl, IComponentConnector
{
	internal StackPanel CalendarWidgetPanel;

	internal StackPanel MatrixWidgetPanel;

	internal StackPanel AddHabitOperation;

	internal EscPopup HabitOptionPopup;

	internal StackPanel AddCountdownOperation;

	internal EscPopup CountdownOptionPopup;

	internal StackPanel AIWidgetRow;

	internal StackPanel AIWidgetPanel;

	private bool _contentLoaded;

	public WidgetInfo()
	{
		InitializeComponent();
		CalendarWidgetPanel.IsEnabled = CalendarWidgetHelper.CanAddProject();
		MatrixWidgetPanel.IsEnabled = MatrixWidgetHelper.CanAddWidget();
		UpdateAIWidgetEntrance();
		base.Loaded += delegate
		{
			GlobalEventManager.WidgetOpenChanged += OnWidgetopenChanged;
		};
		base.Unloaded += delegate
		{
			GlobalEventManager.WidgetOpenChanged -= OnWidgetopenChanged;
		};
	}

	private void OnWidgetopenChanged(object sender, EventArgs e)
	{
		CalendarWidgetPanel.IsEnabled = CalendarWidgetHelper.CanAddProject();
		MatrixWidgetPanel.IsEnabled = MatrixWidgetHelper.CanAddWidget();
		UpdateAIWidgetEntrance();
	}

	private void UpdateAIWidgetEntrance()
	{
		AIWidgetRow.Visibility = ((!LocalSettings.Settings.EnableAI) ? Visibility.Collapsed : Visibility.Visible);
		AIWidgetPanel.IsEnabled = AIWindow.DesktopWidget == null;
		AIWidgetPanel.Opacity = (AIWidgetPanel.IsEnabled ? 1.0 : 0.4);
	}

	private async void OnAIWidgetClick(object sender, MouseButtonEventArgs e)
	{
		AIWidgetPanel.IsEnabled = false;
		try
		{
			await AIWindow.ShowDesktopWidget();
		}
		finally
		{
			UpdateAIWidgetEntrance();
		}
	}

	private async void OnMatrixWidgetClick(object sender, MouseButtonEventArgs e)
	{
		if (ProChecker.CheckPro(ProType.MatrixWidget))
		{
			MatrixWidgetPanel.IsEnabled = false;
			if (MatrixWidgetHelper.CanAddWidget())
			{
				await MatrixWidgetHelper.AddWidget();
			}
		}
	}

	private async void OnCalendarWidgetClick(object sender, MouseButtonEventArgs e)
	{
		if (ProChecker.CheckPro(ProType.CalendarWidget))
		{
			CalendarWidgetPanel.IsEnabled = false;
			if (CalendarWidgetHelper.CanAddProject())
			{
				await CalendarWidgetHelper.AddWidget();
			}
		}
	}

	private void OnListWidgetClick(object sender, MouseButtonEventArgs e)
	{
		ProjectWidgetsHelper.AddDefaultWidget();
	}

	private void OnCountdownOptionClick(object sender, MouseButtonEventArgs e)
	{
		if (ProChecker.CheckPro(ProType.CountdownWidget))
		{
			List<CustomMenuItemViewModel> list = new List<CustomMenuItemViewModel>();
			CustomMenuItemViewModel item = new CustomMenuItemViewModel((object)WidgetType.CountdownItem, Utils.GetString("CardStyle"), (DrawingImage)null, "");
			CustomMenuItemViewModel item2 = new CustomMenuItemViewModel((object)WidgetType.CountdownList, Utils.GetString("ListView"), (DrawingImage)null, "")
			{
				IsEnable = CountdownWidgetsHelper.CanAddListWidget()
			};
			list.Add(item);
			list.Add(item2);
			CustomMenuList customMenuList = new CustomMenuList(list, CountdownOptionPopup);
			customMenuList.Operated += OnCountdownSelected;
			customMenuList.Show();
		}
	}

	private void OnCountdownSelected(object sender, object e)
	{
		CountdownWidgetsHelper.AddWidget((WidgetType)e);
	}

	private void OnHabitOptionClick(object sender, MouseButtonEventArgs e)
	{
		List<CustomMenuItemViewModel> list = new List<CustomMenuItemViewModel>();
		CustomMenuItemViewModel item = new CustomMenuItemViewModel((object)WidgetType.HabitItem, Utils.GetString("TodayHabit"), (DrawingImage)null, "");
		CustomMenuItemViewModel item2 = new CustomMenuItemViewModel((object)WidgetType.HabitList, Utils.GetString("HabitList"), (DrawingImage)null, "")
		{
			IsEnable = HabitWidgetHelper.CanAddListWidget()
		};
		CustomMenuItemViewModel item3 = new CustomMenuItemViewModel((object)WidgetType.HabitWeekItem, Utils.GetString("HabitWeekTracking"), (DrawingImage)null, "");
		list.Add(item);
		list.Add(item2);
		list.Add(item3);
		CustomMenuList customMenuList = new CustomMenuList(list, HabitOptionPopup);
		customMenuList.Operated += OnHabitSelected;
		customMenuList.Show();
	}

	private void OnHabitSelected(object sender, object e)
	{
		_ = (WidgetType)e;
		_ = 7;
		HabitWidgetHelper.AddWidget((WidgetType)e);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/config/widgetinfo.xaml", UriKind.Relative);
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
			((StackPanel)target).MouseLeftButtonUp += OnListWidgetClick;
			break;
		case 2:
			CalendarWidgetPanel = (StackPanel)target;
			CalendarWidgetPanel.MouseLeftButtonUp += OnCalendarWidgetClick;
			break;
		case 3:
			MatrixWidgetPanel = (StackPanel)target;
			MatrixWidgetPanel.MouseLeftButtonUp += OnMatrixWidgetClick;
			break;
		case 4:
			AddHabitOperation = (StackPanel)target;
			AddHabitOperation.MouseLeftButtonUp += OnHabitOptionClick;
			break;
		case 5:
			HabitOptionPopup = (EscPopup)target;
			break;
		case 6:
			AddCountdownOperation = (StackPanel)target;
			AddCountdownOperation.MouseLeftButtonUp += OnCountdownOptionClick;
			break;
		case 7:
			CountdownOptionPopup = (EscPopup)target;
			break;
		case 8:
			AIWidgetRow = (StackPanel)target;
			break;
		case 9:
			AIWidgetPanel = (StackPanel)target;
			AIWidgetPanel.MouseLeftButtonUp += OnAIWidgetClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
