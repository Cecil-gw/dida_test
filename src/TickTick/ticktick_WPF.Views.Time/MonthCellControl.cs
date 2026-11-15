using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;

namespace ticktick_WPF.Views.Time;

public class MonthCellControl : UserControl, IComponentConnector
{
	internal Ellipse HoverBackground;

	private bool _contentLoaded;

	public MonthViewModel Model { get; }

	public event EventHandler<DateTime> Select;

	public MonthCellControl(MonthViewModel model)
	{
		InitializeComponent();
		Model = model;
		base.DataContext = Model;
	}

	private void OnMouseEnter(object sender, MouseEventArgs e)
	{
		HoverBackground.Visibility = Visibility.Visible;
	}

	private void OnMouseLeave(object sender, MouseEventArgs e)
	{
		HoverBackground.Visibility = Visibility.Collapsed;
	}

	private void OnClick(object sender, MouseButtonEventArgs e)
	{
		this.Select?.Invoke(this, Model.Date);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/time/monthcellcontrol.xaml", UriKind.Relative);
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
			((MonthCellControl)target).MouseEnter += OnMouseEnter;
			((MonthCellControl)target).MouseLeftButtonUp += OnClick;
			((MonthCellControl)target).MouseLeave += OnMouseLeave;
			break;
		case 2:
			HoverBackground = (Ellipse)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
