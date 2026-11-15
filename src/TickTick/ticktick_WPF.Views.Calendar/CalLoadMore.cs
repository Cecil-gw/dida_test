using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Calendar.Month;

namespace ticktick_WPF.Views.Calendar;

public class CalLoadMore : Grid, IComponentConnector
{
	private bool _pressed;

	internal Rectangle MoreBorder;

	internal Rectangle InnerBorder;

	internal TextBlock TitleText;

	private bool _contentLoaded;

	public CalLoadMore(bool isDragItem = false)
	{
		InitializeComponent();
		if (isDragItem)
		{
			MoreBorder.SetResourceReference(Shape.FillProperty, "TextInverseColor");
			InnerBorder.StrokeThickness = 1.0;
			InnerBorder.SetResourceReference(Shape.FillProperty, "PrimaryColor10");
			InnerBorder.SetResourceReference(Shape.StrokeProperty, "PrimaryColor");
			TitleText.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryColor");
		}
	}

	private IDragBarEvent GetDragTarget()
	{
		return Utils.FindParent<IDragBarEvent>(this);
	}

	private async void OnClick(object sender, MouseButtonEventArgs e)
	{
		if ((GetDragTarget()?.OnSelection() ?? false) || PopupStateManager.IsInSelection() || PopupStateManager.IsInAdd())
		{
			return;
		}
		if (base.DataContext != null)
		{
			object dataContext = base.DataContext;
			if (dataContext is CalendarDisplayViewModel { IsLoadMore: not false } model)
			{
				await Task.Delay(150);
				ShowLoadMore(CalendarGeoHelper.GetRealColumn(model.Column), model.Children);
			}
		}
		_pressed = false;
	}

	private CalendarControl GetCalendarParent()
	{
		return Utils.FindParent<CalendarControl>(this);
	}

	private void ShowLoadMore(int column, List<CalendarDisplayViewModel> children)
	{
		bool locked = GetCalendarParent()?.IsLocked ?? false;
		Utils.FindParent<MultiWeekWeekControl>(this)?.ShowLoadMore(column, locked, children);
	}

	private void OnMouseDown(object sender, MouseButtonEventArgs e)
	{
		_pressed = true;
	}

	public void SetMoreBorderMargin(bool isFull)
	{
		MoreBorder.Margin = new Thickness(isFull ? 3 : (-2), 1.0, 4.0, 1.0);
		InnerBorder.Margin = new Thickness(isFull ? 3 : (-2), 1.0, 4.0, 1.0);
		TitleText.Margin = new Thickness(isFull ? 5 : 0, 1.0, 6.0, 1.0);
	}

	public void SetFontSize()
	{
		base.Height = CalendarGeoHelper.TaskBarHeight;
		TitleText.FontSize = CalendarGeoHelper.CalTitleFontSize - 1.0;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/calendar/calloadmore.xaml", UriKind.Relative);
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
			((CalLoadMore)target).MouseLeftButtonDown += OnMouseDown;
			((CalLoadMore)target).MouseLeftButtonUp += OnClick;
			break;
		case 2:
			MoreBorder = (Rectangle)target;
			break;
		case 3:
			InnerBorder = (Rectangle)target;
			break;
		case 4:
			TitleText = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
