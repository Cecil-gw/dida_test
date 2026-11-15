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

public class TaskCircleProgress : Border, IComponentConnector
{
	public static readonly DependencyProperty PercentageProperty = DependencyProperty.Register("Percentage", typeof(double), typeof(TaskCircleProgress), new PropertyMetadata(65.0, OnPercentageChanged));

	public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register("StrokeThickness", typeof(double), typeof(TaskCircleProgress), new PropertyMetadata(5.0));

	public static readonly DependencyProperty SegmentColorProperty = DependencyProperty.Register("SegmentColor", typeof(Brush), typeof(TaskCircleProgress), new PropertyMetadata(new SolidColorBrush(Colors.Red)));

	public static readonly DependencyProperty RadiusProperty = DependencyProperty.Register("Radius", typeof(double), typeof(TaskCircleProgress), new PropertyMetadata(25.0));

	internal TaskCircleProgress RootBd;

	internal Path PathRoot;

	internal PathFigure ProgressPath;

	internal LineSegment ProgressLine;

	internal ArcSegment ProgressArc;

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

	public TaskCircleProgress()
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
		(sender as TaskCircleProgress)?.RenderArc();
	}

	public void RenderArc()
	{
		double num = Radius + StrokeThickness;
		PathRoot.Width = num * 2.0;
		PathRoot.Height = num * 2.0;
		base.Width = num * 2.0 + 1.0;
		base.Height = num * 2.0 + 1.0;
		OutsidePath.StartPoint = new Point(num, num);
		OutsideLine.Point = new Point(num, 0.0);
		Point point = ComputeCartesianCoordinate(359.0, num);
		point.X += num;
		point.Y += num;
		OutsideArc.Point = point;
		OutsideArc.Size = new Size(num, num);
		OutsideArc.IsLargeArc = true;
		InsidePath.StartPoint = new Point(num, num);
		InsideLine.Point = new Point(num, StrokeThickness);
		Point point2 = ComputeCartesianCoordinate(359.0, Radius);
		point2.X += num;
		point2.Y += num;
		InsideArc.Point = point2;
		InsideArc.Size = new Size(Radius, Radius);
		InsideArc.IsLargeArc = true;
		double num2 = ((Percentage >= 100.0) ? 359.0 : (Percentage * 360.0 / 100.0));
		bool isLargeArc = num2 > 180.0;
		ProgressPath.StartPoint = new Point(num, num);
		ProgressLine.Point = new Point(num, StrokeThickness);
		Point point3 = ComputeCartesianCoordinate(num2, Radius);
		point3.X += num;
		point3.Y += num;
		ProgressArc.Point = point3;
		ProgressArc.Size = new Size(Radius, Radius);
		ProgressArc.IsLargeArc = isLargeArc;
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
			Uri resourceLocator = new Uri("/TickTick;component/views/taskcircleprogress.xaml", UriKind.Relative);
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
			RootBd = (TaskCircleProgress)target;
			break;
		case 2:
			PathRoot = (Path)target;
			break;
		case 3:
			ProgressPath = (PathFigure)target;
			break;
		case 4:
			ProgressLine = (LineSegment)target;
			break;
		case 5:
			ProgressArc = (ArcSegment)target;
			break;
		case 6:
			OutsidePath = (PathFigure)target;
			break;
		case 7:
			OutsideLine = (LineSegment)target;
			break;
		case 8:
			OutsideArc = (ArcSegment)target;
			break;
		case 9:
			InsidePath = (PathFigure)target;
			break;
		case 10:
			InsideLine = (LineSegment)target;
			break;
		case 11:
			InsideArc = (ArcSegment)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
