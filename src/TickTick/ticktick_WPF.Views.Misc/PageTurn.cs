using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using TickTickUtils;

namespace ticktick_WPF.Views.Misc;

public class PageTurn : UserControl, IComponentConnector
{
	private DoubleAnimation _da;

	private AxisAngleRotation3D _aar;

	private int _num;

	public static readonly DependencyProperty FontProperty = DependencyProperty.Register("Font", typeof(FontFamily), typeof(PageTurn), new PropertyMetadata(null));

	public static readonly DependencyProperty CurrentTenProperty = DependencyProperty.Register("CurrentTen", typeof(int), typeof(PageTurn), new PropertyMetadata(0));

	public static readonly DependencyProperty CurrentSingleProperty = DependencyProperty.Register("CurrentSingle", typeof(int), typeof(PageTurn), new PropertyMetadata(0));

	public static readonly DependencyProperty OriginTenProperty = DependencyProperty.Register("OriginTen", typeof(int), typeof(PageTurn), new PropertyMetadata(0));

	public static readonly DependencyProperty OriginSingleProperty = DependencyProperty.Register("OriginSingle", typeof(int), typeof(PageTurn), new PropertyMetadata(0));

	internal PageTurn Root;

	internal Grid View;

	internal Grid ShowGrid;

	internal Viewport3D Viewport;

	internal AxisAngleRotation3D aar;

	private bool _contentLoaded;

	public FontFamily Font
	{
		get
		{
			return (FontFamily)GetValue(FontProperty);
		}
		set
		{
			SetValue(FontProperty, value);
		}
	}

	public int CurrentTen
	{
		get
		{
			return (int)GetValue(CurrentTenProperty);
		}
		set
		{
			SetValue(CurrentTenProperty, value);
		}
	}

	public int CurrentSingle
	{
		get
		{
			return (int)GetValue(CurrentSingleProperty);
		}
		set
		{
			SetValue(CurrentSingleProperty, value);
		}
	}

	public int OriginTen
	{
		get
		{
			return (int)GetValue(OriginTenProperty);
		}
		set
		{
			SetValue(OriginTenProperty, value);
		}
	}

	public int OriginSingle
	{
		get
		{
			return (int)GetValue(OriginSingleProperty);
		}
		set
		{
			SetValue(OriginSingleProperty, value);
		}
	}

	public bool VisibleAlways { get; internal set; } = true;

	public PageTurn()
	{
		InitializeComponent();
		_da = new DoubleAnimation();
		_da.Duration = new Duration(TimeSpan.FromSeconds(1.0));
		_da.EasingFunction = new CubicEase
		{
			EasingMode = EasingMode.EaseInOut
		};
		_da.SetValue(System.Windows.Media.Animation.Timeline.DesiredFrameRateProperty, 60);
		_da.From = 0.0;
		_da.To = 180.0;
		_da.Completed += OnCompleted;
		Font = new FontFamily(AppPaths.ExeDir + "\\#D-DIN DIN");
		_aar = FindName("aar") as AxisAngleRotation3D;
	}

	public void SetText(int num, bool withAnimation = true)
	{
		if (_num != num)
		{
			_num = num;
			int currentTen = num / 10;
			int currentSingle = num % 10;
			OriginSingle = CurrentSingle;
			OriginTen = CurrentTen;
			CurrentSingle = currentSingle;
			CurrentTen = currentTen;
			if (!VisibleAlways)
			{
				ShowGrid.Visibility = Visibility.Collapsed;
				Viewport.Visibility = Visibility.Visible;
			}
			if (withAnimation)
			{
				_aar.BeginAnimation(AxisAngleRotation3D.AngleProperty, _da);
				return;
			}
			OriginSingle = CurrentSingle;
			OriginTen = CurrentTen;
		}
	}

	public void SetVisibleAlways(bool visible)
	{
		VisibleAlways = visible;
		ShowGrid.Visibility = (visible ? Visibility.Collapsed : Visibility.Visible);
		Viewport.Visibility = ((!visible) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void OnCompleted(object sender, EventArgs e)
	{
		if (!VisibleAlways)
		{
			ShowGrid.Visibility = Visibility.Visible;
			Viewport.Visibility = Visibility.Collapsed;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/misc/pageturn.xaml", UriKind.Relative);
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
			Root = (PageTurn)target;
			break;
		case 2:
			View = (Grid)target;
			break;
		case 3:
			ShowGrid = (Grid)target;
			break;
		case 4:
			Viewport = (Viewport3D)target;
			break;
		case 5:
			aar = (AxisAngleRotation3D)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
