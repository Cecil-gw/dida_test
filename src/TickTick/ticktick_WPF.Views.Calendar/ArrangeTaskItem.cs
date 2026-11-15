using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using TickTickUtils;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Calendar;

public class ArrangeTaskItem : UserControl, IComponentConnector
{
	private ArrangeTaskPanel _parent;

	private SolidColorBrush _outDateBrush = ThemeUtil.GetColor("OutDateColor");

	private SolidColorBrush _dateBrush = ThemeUtil.GetColor("ThemeBlue");

	internal TaskBar TaskBar;

	internal StackPanel ArrangeSection;

	internal Path OpenIndicator;

	internal RotateTransform rt;

	internal TextBlock CountText;

	private bool _contentLoaded;

	public ArrangeTaskItem()
	{
		InitializeComponent();
		base.DataContextChanged += OnDataContextChanged;
		TaskBar.TimeText.Foreground = _outDateBrush;
		TaskBar.TimeText.Opacity = 1.0;
		TaskBar.Mode = TaskbarMode.Arrange;
	}

	private void UserControl_Loaded(object sender, RoutedEventArgs e)
	{
		ArrangeSection.MouseLeftButtonUp -= FoldingClick;
		ArrangeSection.MouseLeftButtonUp += FoldingClick;
		CalendarEventChangeNotifier.FontSizeChanged += OnFontSizeChanged;
		DataChangedNotifier.CalendarShowIconChanged += DataChangedNotifier_CalendarShowIconChanged;
		DataChangedNotifier.CalendarTaskBarStyleChanged += DataChangedNotifier_CalendarTaskBarStyleChanged;
		TaskBar.SetFontSize();
	}

	private void DataChangedNotifier_CalendarTaskBarStyleChanged(object sender, EventArgs e)
	{
		TaskBar.GetModel()?.SetColor();
	}

	private void DataChangedNotifier_CalendarShowIconChanged(object sender, EventArgs e)
	{
		if (base.DataContext is CalendarDisplayViewModel calendarDisplayViewModel)
		{
			TaskBar.SetShowIcon(calendarDisplayViewModel.IsConfigShowIcon(), calendarDisplayViewModel.ForceShowIcon);
		}
	}

	private void OnFontSizeChanged(object sender, EventArgs e)
	{
		TaskBar.SetFontSize();
	}

	private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (!(base.DataContext is CalendarDisplayViewModel calendarDisplayViewModel))
		{
			return;
		}
		if (calendarDisplayViewModel.IsSection)
		{
			rt.BeginAnimation(RotateTransform.AngleProperty, null);
			rt.Angle = ((!calendarDisplayViewModel.IsOpened) ? (-90) : 0);
			CountText.Text = (calendarDisplayViewModel.Children?.Count).ToString() ?? "";
			return;
		}
		TaskBar.SetShowIcon(calendarDisplayViewModel.IsConfigShowIcon(), calendarDisplayViewModel.ForceShowIcon);
		if (calendarDisplayViewModel.SourceViewModel.OutDate())
		{
			TaskBar.TimeText.Foreground = _outDateBrush;
		}
		else
		{
			TaskBar.TimeText.Foreground = _dateBrush;
		}
	}

	private ArrangeTaskPanel GetParent()
	{
		ArrangeTaskPanel obj = _parent ?? Utils.FindParent<ArrangeTaskPanel>(this);
		ArrangeTaskPanel result = obj;
		_parent = obj;
		return result;
	}

	private async void FoldingClick(object sender, MouseButtonEventArgs e)
	{
		ArrangeSection.MouseLeftButtonUp -= FoldingClick;
		if (base.DataContext is CalendarDisplayViewModel calendarDisplayViewModel)
		{
			calendarDisplayViewModel.IsOpened = !calendarDisplayViewModel.IsOpened;
			rt.BeginAnimation(RotateTransform.AngleProperty, AnimationUtils.GetDoubleAnimation(null, (!calendarDisplayViewModel.IsOpened) ? (-90) : 0, 120));
			string text = calendarDisplayViewModel.SectionStatusName ?? calendarDisplayViewModel.Title;
			if (calendarDisplayViewModel.IsOpened)
			{
				ArrangeSectionStatusDao.DeleteSectionStatusModel(new ArrangeSectionStatusModel
				{
					Name = text,
					Type = LocalSettings.Settings.ArrangeDisplayType
				});
				if (text != calendarDisplayViewModel.Title)
				{
					ArrangeSectionStatusDao.DeleteSectionStatusModel(new ArrangeSectionStatusModel
					{
						Name = calendarDisplayViewModel.Title,
						Type = LocalSettings.Settings.ArrangeDisplayType
					});
				}
			}
			else
			{
				ArrangeSectionStatusDao.AddSectionStatusModel(new ArrangeSectionStatusModel
				{
					Name = text,
					Type = LocalSettings.Settings.ArrangeDisplayType,
					UserId = LocalSettings.Settings.LoginUserId
				});
			}
			if (GetParent() != null)
			{
				await _parent.OnSectionStatusChanged(calendarDisplayViewModel.IsOpened, calendarDisplayViewModel);
			}
		}
		ArrangeSection.MouseLeftButtonUp -= FoldingClick;
		ArrangeSection.MouseLeftButtonUp += FoldingClick;
	}

	private void UserControl_Unloaded(object sender, RoutedEventArgs e)
	{
		ArrangeSection.MouseLeftButtonUp -= FoldingClick;
		DataChangedNotifier.CalendarShowIconChanged -= DataChangedNotifier_CalendarShowIconChanged;
		CalendarEventChangeNotifier.FontSizeChanged -= OnFontSizeChanged;
		DataChangedNotifier.CalendarTaskBarStyleChanged -= DataChangedNotifier_CalendarTaskBarStyleChanged;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/calendar/arrangetaskitem.xaml", UriKind.Relative);
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
			((ArrangeTaskItem)target).Loaded += UserControl_Loaded;
			((ArrangeTaskItem)target).Unloaded += UserControl_Unloaded;
			break;
		case 2:
			TaskBar = (TaskBar)target;
			break;
		case 3:
			ArrangeSection = (StackPanel)target;
			break;
		case 4:
			OpenIndicator = (Path)target;
			break;
		case 5:
			rt = (RotateTransform)target;
			break;
		case 6:
			CountText = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
