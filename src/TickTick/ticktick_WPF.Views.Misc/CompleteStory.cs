using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace ticktick_WPF.Views.Misc;

public class CompleteStory : Viewbox, IComponentConnector
{
	public static readonly DependencyProperty AngleProperty = DependencyProperty.Register("Angle", typeof(double), typeof(CompleteStory), new PropertyMetadata(0.0, OnAngelChanged));

	internal CompleteStory Root;

	internal Grid CheckGrid;

	internal Border CheckBorder;

	internal ArcSegment Arc;

	internal Line line;

	private bool _contentLoaded;

	public double Angle
	{
		get
		{
			return (double)GetValue(AngleProperty);
		}
		set
		{
			SetValue(AngleProperty, value);
		}
	}

	public CompleteStory()
	{
		InitializeComponent();
	}

	public void PlayStory()
	{
		((Storyboard)FindResource("Story")).Begin();
	}

	private static void OnAngelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is CompleteStory completeStory)
		{
			completeStory.SetAngle((double)e.NewValue);
		}
	}

	private void SetAngle(double val)
	{
		Angle = val;
		double num = ((val >= 360.0) ? 359.99 : val);
		double num2 = Math.PI * 2.0 * num / 360.0;
		double x = 24.0 - Math.Sin(num2) * 24.0;
		double y = 24.0 - Math.Cos(num2) * 24.0;
		Arc.Point = new Point(x, y);
		Arc.IsLargeArc = val >= 180.0;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/misc/completestory.xaml", UriKind.Relative);
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
			Root = (CompleteStory)target;
			break;
		case 2:
			CheckGrid = (Grid)target;
			break;
		case 3:
			CheckBorder = (Border)target;
			break;
		case 4:
			Arc = (ArcSegment)target;
			break;
		case 5:
			line = (Line)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
