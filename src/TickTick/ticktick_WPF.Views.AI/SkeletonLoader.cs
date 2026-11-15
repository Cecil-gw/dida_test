using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using ticktick_WPF.Notifier;

namespace ticktick_WPF.Views.AI;

public class SkeletonLoader : UserControl, IComponentConnector
{
	public static readonly DependencyProperty LengthsProperty = DependencyProperty.Register("Lengths", typeof(DoubleCollection), typeof(SkeletonLoader), new PropertyMetadata(null, OnLengthsChanged));

	public static readonly DependencyProperty IsAnimatingProperty = DependencyProperty.Register("IsAnimating", typeof(bool), typeof(SkeletonLoader), new PropertyMetadata(false, OnIsAnimatingChanged));

	private const double ITEM_HEIGHT = 24.0;

	private const double CORNER_RADIUS = 6.0;

	private static readonly Thickness ITEM_MARGIN = new Thickness(0.0, 5.0, 0.0, 5.0);

	private static readonly TimeSpan DELAY_STEP = TimeSpan.FromSeconds(0.1);

	private static readonly TimeSpan ANIMATION_DURATION = TimeSpan.FromSeconds(2.0);

	private const double GRADIENT_Y = 0.0;

	private const string START_COLOR_KEY = "ColorOpacity3";

	private const string MIDDLE_COLOR_KEY = "ColorOpacity6";

	private static readonly Color DEFAULT_START_COLOR = Color.FromArgb(51, 0, 0, 0);

	private static readonly Color DEFAULT_MIDDLE_COLOR = Color.FromArgb(102, 0, 0, 0);

	private Dictionary<Rectangle, Storyboard> _animations = new Dictionary<Rectangle, Storyboard>();

	internal SkeletonLoader RootControl;

	internal UniformGrid ItemsGrid;

	private bool _contentLoaded;

	public DoubleCollection Lengths
	{
		get
		{
			return (DoubleCollection)GetValue(LengthsProperty);
		}
		set
		{
			SetValue(LengthsProperty, value);
		}
	}

	public bool IsAnimating
	{
		get
		{
			return (bool)GetValue(IsAnimatingProperty);
		}
		set
		{
			SetValue(IsAnimatingProperty, value);
		}
	}

	public SkeletonLoader()
	{
		InitializeComponent();
	}

	private void RootControl_Loaded(object sender, RoutedEventArgs e)
	{
		DataChangedNotifier.ThemeModeChanged += UpdateColors;
	}

	private void RootControl_Unloaded(object sender, RoutedEventArgs e)
	{
		DataChangedNotifier.ThemeModeChanged -= UpdateColors;
	}

	private static void OnLengthsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		((SkeletonLoader)d).RecreateItems();
	}

	private static void OnIsAnimatingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		SkeletonLoader skeletonLoader = (SkeletonLoader)d;
		if ((bool)e.NewValue)
		{
			skeletonLoader.StartAllAnimations();
		}
		else
		{
			skeletonLoader.StopAllAnimations();
		}
	}

	private void RecreateItems()
	{
		StopAllAnimations();
		_animations.Clear();
		ItemsGrid.Children.Clear();
		if (Lengths != null && Lengths.Count != 0)
		{
			for (int i = 0; i < Lengths.Count; i++)
			{
				Rectangle element = CreateRectangle(Lengths[i], i);
				ItemsGrid.Children.Add(element);
			}
			if (IsAnimating)
			{
				StartAllAnimations();
			}
		}
	}

	private Rectangle CreateRectangle(double width, int index)
	{
		Rectangle rectangle = new Rectangle
		{
			Height = 24.0,
			RadiusX = 6.0,
			RadiusY = 6.0,
			Margin = ITEM_MARGIN,
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		rectangle.Clip = new RectangleGeometry(new Rect(0.0, 0.0, width, 24.0), 6.0, 6.0);
		rectangle.Fill = CreateBrush();
		Storyboard value = CreateAnimation(rectangle, index);
		_animations[rectangle] = value;
		return rectangle;
	}

	private LinearGradientBrush CreateBrush()
	{
		Color colorFromResource = GetColorFromResource("ColorOpacity3", DEFAULT_START_COLOR);
		Color colorFromResource2 = GetColorFromResource("ColorOpacity6", DEFAULT_MIDDLE_COLOR);
		return new LinearGradientBrush
		{
			StartPoint = new Point(-0.42, 0.0),
			EndPoint = new Point(0.58, 0.0),
			SpreadMethod = GradientSpreadMethod.Repeat,
			GradientStops = 
			{
				new GradientStop(colorFromResource, 0.2),
				new GradientStop(colorFromResource2, 0.58),
				new GradientStop(colorFromResource, 0.8)
			}
		};
	}

	private Storyboard CreateAnimation(Rectangle rect, int index)
	{
		PointAnimation pointAnimation = new PointAnimation
		{
			From = new Point(-0.42, 0.0),
			To = new Point(0.58, 0.0),
			Duration = new Duration(ANIMATION_DURATION),
			RepeatBehavior = RepeatBehavior.Forever,
			BeginTime = TimeSpan.FromSeconds((double)index * DELAY_STEP.TotalSeconds)
		};
		Storyboard.SetTarget(pointAnimation, rect);
		Storyboard.SetTargetProperty(pointAnimation, new PropertyPath("(Rectangle.Fill).(LinearGradientBrush.StartPoint)"));
		PointAnimation pointAnimation2 = new PointAnimation
		{
			From = new Point(0.58, 0.0),
			To = new Point(1.58, 0.0),
			Duration = new Duration(ANIMATION_DURATION),
			RepeatBehavior = RepeatBehavior.Forever,
			BeginTime = TimeSpan.FromSeconds((double)index * DELAY_STEP.TotalSeconds)
		};
		Storyboard.SetTarget(pointAnimation2, rect);
		Storyboard.SetTargetProperty(pointAnimation2, new PropertyPath("(Rectangle.Fill).(LinearGradientBrush.EndPoint)"));
		return new Storyboard
		{
			Children = 
			{
				(System.Windows.Media.Animation.Timeline)pointAnimation,
				(System.Windows.Media.Animation.Timeline)pointAnimation2
			}
		};
	}

	private void StartAllAnimations()
	{
		foreach (KeyValuePair<Rectangle, Storyboard> animation in _animations)
		{
			if (animation.Key != null && animation.Value != null)
			{
				animation.Value.Begin(animation.Key, isControllable: true);
			}
		}
	}

	private void StopAllAnimations()
	{
		foreach (KeyValuePair<Rectangle, Storyboard> animation in _animations)
		{
			if (animation.Key != null && animation.Value != null)
			{
				animation.Value.Stop(animation.Key);
			}
		}
	}

	private Color GetColorFromResource(string key, Color defaultColor)
	{
		object obj = TryFindResource(key) ?? Application.Current?.TryFindResource(key);
		if (obj is Color)
		{
			return (Color)obj;
		}
		if (obj is SolidColorBrush solidColorBrush)
		{
			return solidColorBrush.Color;
		}
		return defaultColor;
	}

	private void UpdateColors(object sender, EventArgs e)
	{
		foreach (object child in ItemsGrid.Children)
		{
			if (child is Rectangle { Fill: LinearGradientBrush fill })
			{
				Color colorFromResource = GetColorFromResource("ColorOpacity3", DEFAULT_START_COLOR);
				Color colorFromResource2 = GetColorFromResource("ColorOpacity6", DEFAULT_MIDDLE_COLOR);
				if (fill.GradientStops.Count >= 3)
				{
					fill.GradientStops[0].Color = colorFromResource;
					fill.GradientStops[1].Color = colorFromResource2;
					fill.GradientStops[2].Color = colorFromResource;
				}
			}
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/ai/skeletonloader.xaml", UriKind.Relative);
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
			RootControl = (SkeletonLoader)target;
			RootControl.Loaded += RootControl_Loaded;
			RootControl.Unloaded += RootControl_Unloaded;
			break;
		case 2:
			ItemsGrid = (UniformGrid)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
