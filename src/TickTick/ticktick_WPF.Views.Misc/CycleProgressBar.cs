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

public class CycleProgressBar : Grid, IComponentConnector
{
	public static readonly DependencyProperty TargetPercentProperty = DependencyProperty.Register("TargetPercent", typeof(double), typeof(CycleProgressBar), new PropertyMetadata(0.0, OnTargetPercentChanged));

	public int Duration = 180;

	public static readonly DependencyProperty PercentProperty = DependencyProperty.Register("Percent", typeof(double), typeof(CycleProgressBar), new PropertyMetadata(0.0, OnPercentChanged));

	public static readonly DependencyProperty AngleProperty = DependencyProperty.Register("Angle", typeof(double), typeof(CycleProgressBar), new PropertyMetadata(0.0, OnAngleChanged));

	public static readonly DependencyProperty ThicknessProperty = DependencyProperty.Register("Thickness", typeof(double), typeof(CycleProgressBar), new PropertyMetadata(8.0, null));

	public static readonly DependencyProperty UnderColorProperty = DependencyProperty.Register("UnderColor", typeof(SolidColorBrush), typeof(CycleProgressBar), new PropertyMetadata(Brushes.Transparent, null));

	public static readonly DependencyProperty TopColorProperty = DependencyProperty.Register("TopColor", typeof(Brush), typeof(CycleProgressBar), new PropertyMetadata(Brushes.Transparent, null));

	private DoubleAnimation _anim;

	internal CycleProgressBar Root;

	internal Path UnderPath;

	internal Path UnderHoverPath;

	internal Ellipse TopEll;

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

	public double TargetPercent
	{
		get
		{
			return (double)GetValue(TargetPercentProperty);
		}
		set
		{
			SetValue(TargetPercentProperty, value);
		}
	}

	public double Percent
	{
		get
		{
			return (double)GetValue(PercentProperty);
		}
		set
		{
			SetValue(PercentProperty, value);
		}
	}

	public double Thickness
	{
		get
		{
			return (double)GetValue(ThicknessProperty);
		}
		set
		{
			SetValue(ThicknessProperty, value);
		}
	}

	public SolidColorBrush UnderColor
	{
		get
		{
			return (SolidColorBrush)GetValue(UnderColorProperty);
		}
		set
		{
			SetValue(UnderColorProperty, value);
		}
	}

	public Brush TopColor
	{
		get
		{
			return (Brush)GetValue(TopColorProperty);
		}
		set
		{
			SetValue(TopColorProperty, value);
		}
	}

	private static void OnTargetPercentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is CycleProgressBar cycleProgressBar && e.NewValue is double num && Math.Abs(cycleProgressBar.Percent - num) > 0.1)
		{
			DoubleAnimation animation = new DoubleAnimation
			{
				From = cycleProgressBar.Percent,
				To = num,
				Duration = TimeSpan.FromMilliseconds(cycleProgressBar.Duration)
			};
			cycleProgressBar.BeginAnimation(PercentProperty, animation);
		}
	}

	private static void OnPercentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is CycleProgressBar cycleProgressBar)
		{
			object newValue = e.NewValue;
			if (newValue is double)
			{
				_ = (double)newValue;
				cycleProgressBar.DrawTopPath();
			}
		}
	}

	private static void OnAngleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is CycleProgressBar cycleProgressBar && e.NewValue is double num)
		{
			cycleProgressBar.Percent = num / 3.6;
		}
	}

	public CycleProgressBar()
	{
		InitializeComponent();
		base.SizeChanged += delegate
		{
			DrawUnderPath();
			DrawTopPath();
		};
	}

	private void DrawUnderPath()
	{
		CombinedGeometry data = new CombinedGeometry
		{
			GeometryCombineMode = GeometryCombineMode.Exclude,
			Geometry1 = new EllipseGeometry
			{
				RadiusX = base.Width / 2.0,
				RadiusY = base.Height / 2.0,
				Center = new Point(base.Width / 2.0, base.Height / 2.0)
			},
			Geometry2 = new EllipseGeometry
			{
				RadiusX = base.Width / 2.0 - Thickness,
				RadiusY = base.Height / 2.0 - Thickness,
				Center = new Point(base.Width / 2.0, base.Height / 2.0)
			}
		};
		UnderPath.Data = data;
	}

	private void DrawTopPath()
	{
		if (base.Width <= 0.0 || base.Height <= 0.0 || Thickness <= 0.0)
		{
			TopEll.Clip = null;
			return;
		}
		if (Percent <= 0.1)
		{
			TopEll.Clip = new RectangleGeometry
			{
				Rect = new Rect(0.0, 0.0, 0.0, 0.0)
			};
			return;
		}
		CombinedGeometry combinedGeometry = new CombinedGeometry
		{
			GeometryCombineMode = GeometryCombineMode.Union
		};
		PathGeometry pathGeometry = new PathGeometry();
		Point point = new Point(base.Width / 2.0, Thickness);
		PathFigure pathFigure = new PathFigure
		{
			StartPoint = point
		};
		pathFigure.Segments.Add(new ArcSegment
		{
			Point = new Point(base.Width / 2.0, 0.0),
			Size = new Size(Thickness / 2.0, Thickness / 2.0),
			SweepDirection = SweepDirection.Clockwise
		});
		pathFigure.Segments.Add(new ArcSegment
		{
			Point = new Point(base.Width / 2.0, Thickness),
			Size = new Size(Thickness / 2.0, Thickness / 2.0),
			SweepDirection = SweepDirection.Clockwise
		});
		pathGeometry.Figures.Add(pathFigure);
		PathGeometry pathGeometry2 = new PathGeometry();
		PathFigure pathFigure2 = new PathFigure
		{
			StartPoint = new Point(base.Width / 2.0, Thickness)
		};
		pathFigure2.Segments.Add(new LineSegment
		{
			Point = new Point(base.Width / 2.0, 0.0)
		});
		Point endPoint = GetEndPoint(outSide: true);
		Point endPoint2 = GetEndPoint(outSide: false);
		pathFigure2.Segments.Add(new ArcSegment
		{
			Size = new Size(base.Width / 2.0, base.Height / 2.0),
			Point = endPoint,
			SweepDirection = SweepDirection.Clockwise,
			IsLargeArc = (Percent > 50.0)
		});
		pathFigure2.Segments.Add(new ArcSegment
		{
			Size = new Size(Thickness / 2.0, Thickness / 2.0),
			Point = endPoint2,
			SweepDirection = SweepDirection.Clockwise
		});
		pathFigure2.Segments.Add(new ArcSegment
		{
			Size = new Size(base.Width / 2.0 - Thickness, base.Height / 2.0 - Thickness),
			Point = point,
			SweepDirection = SweepDirection.Counterclockwise,
			IsLargeArc = (Percent > 50.0)
		});
		pathGeometry2.Figures.Add(pathFigure2);
		combinedGeometry.Geometry1 = pathGeometry;
		combinedGeometry.Geometry2 = pathGeometry2;
		TopEll.Clip = combinedGeometry;
	}

	private Point GetEndPoint(bool outSide)
	{
		double num = ((Percent >= 100.0) ? 99.9 : Percent);
		double num2 = Math.PI * 2.0 * num / 100.0;
		double x = base.Width / 2.0 + Math.Sin(num2) * (base.Width / 2.0 - (outSide ? 0.0 : Thickness));
		double y = base.Width / 2.0 - Math.Cos(num2) * (base.Height / 2.0 - (outSide ? 0.0 : Thickness));
		return new Point(x, y);
	}

	public void SetHoverVisible(bool visible)
	{
		UnderHoverPath.Visibility = ((!visible) ? Visibility.Collapsed : Visibility.Visible);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/misc/cycleprogressbar.xaml", UriKind.Relative);
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
			Root = (CycleProgressBar)target;
			break;
		case 2:
			UnderPath = (Path)target;
			break;
		case 3:
			UnderHoverPath = (Path)target;
			break;
		case 4:
			TopEll = (Ellipse)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
