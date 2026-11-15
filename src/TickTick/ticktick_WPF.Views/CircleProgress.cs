using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ticktick_WPF.Views;

public class CircleProgress : Border, IComponentConnector
{
	public static readonly DependencyProperty PercentageProperty = DependencyProperty.Register("Percentage", typeof(double), typeof(CircleProgress), new PropertyMetadata(65.0, OnPercentageChanged));

	public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register("StrokeThickness", typeof(double), typeof(CircleProgress), new PropertyMetadata(5.0));

	public static readonly DependencyProperty SegmentColorProperty = DependencyProperty.Register("SegmentColor", typeof(Brush), typeof(CircleProgress), new PropertyMetadata(new SolidColorBrush(Colors.Red)));

	public static readonly DependencyProperty RadiusProperty = DependencyProperty.Register("Radius", typeof(double), typeof(CircleProgress), new PropertyMetadata(25.0));

	internal CircleProgress RootBd;

	internal Path PathRoot;

	internal PathFigure OutsidePath;

	internal LineSegment OutsideLine;

	internal ArcSegment OutsideArc;

	internal PathFigure InsidePath;

	internal LineSegment InsideLine;

	internal ArcSegment InsideArc;

	private bool _contentLoaded;

	public double Radius
	{
		get
		{
			return (double)GetValue(RadiusProperty);
		}
		set
		{
			SetValue(RadiusProperty, value);
		}
	}

	public Brush SegmentColor
	{
		get
		{
			return (Brush)GetValue(SegmentColorProperty);
		}
		set
		{
			SetValue(SegmentColorProperty, value);
		}
	}

	public double StrokeThickness
	{
		get
		{
			return (double)GetValue(StrokeThicknessProperty);
		}
		set
		{
			SetValue(StrokeThicknessProperty, value);
		}
	}

	public double Percentage
	{
		get
		{
			return (double)GetValue(PercentageProperty);
		}
		set
		{
			SetValue(PercentageProperty, value);
		}
	}

	public CircleProgress()
	{
		InitializeComponent();
		base.Loaded += OnLoaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		RenderArc();
	}

	private static void OnPercentageChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		(sender as CircleProgress)?.RenderArc();
	}

	public void RenderArc()
	{
		double num = ((Percentage >= 100.0) ? 359.0 : (Percentage * 360.0 / 100.0));
		bool isLargeArc = num > 180.0;
		double num2 = Radius + StrokeThickness;
		PathRoot.Width = num2 * 2.0;
		PathRoot.Height = num2 * 2.0;
		OutsidePath.StartPoint = new Point(num2, num2);
		OutsideLine.Point = new Point(num2, 0.0);
		Point point = ComputeCartesianCoordinate(num, num2);
		point.X += num2;
		point.Y += num2;
		OutsideArc.Point = point;
		OutsideArc.Size = new Size(num2, num2);
		OutsideArc.IsLargeArc = isLargeArc;
		InsidePath.StartPoint = new Point(num2, num2);
		InsideLine.Point = new Point(num2, StrokeThickness);
		Point point2 = ComputeCartesianCoordinate(num, Radius);
		point2.X += num2;
		point2.Y += num2;
		InsideArc.Point = point2;
		InsideArc.Size = new Size(Radius, Radius);
		InsideArc.IsLargeArc = isLargeArc;
	}

	private Point ComputeCartesianCoordinate(double angle, double radius)
	{
		double num = Math.PI / 180.0 * (angle - 90.0);
		double x = radius * Math.Cos(num);
		double y = radius * Math.Sin(num);
		return new Point(x, y);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/circleprogress.xaml", UriKind.Relative);
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
			RootBd = (CircleProgress)target;
			break;
		case 2:
			PathRoot = (Path)target;
			break;
		case 3:
			OutsidePath = (PathFigure)target;
			break;
		case 4:
			OutsideLine = (LineSegment)target;
			break;
		case 5:
			OutsideArc = (ArcSegment)target;
			break;
		case 6:
			InsidePath = (PathFigure)target;
			break;
		case 7:
			InsideLine = (LineSegment)target;
			break;
		case 8:
			InsideArc = (ArcSegment)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
