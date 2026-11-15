using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace ticktick_WPF.Views.Misc;

public class PomoProgressBar : UserControl, IComponentConnector
{
	public static readonly DependencyProperty TargetAngelProperty = DependencyProperty.Register("TargetAngel", typeof(double), typeof(PomoProgressBar), new PropertyMetadata(0.0, OnTargetAngelChanged));

	public static readonly DependencyProperty AngleProperty = DependencyProperty.Register("Angle", typeof(double), typeof(PomoProgressBar), new PropertyMetadata(0.0, null));

	public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register("StrokeThickness", typeof(double), typeof(PomoProgressBar), new PropertyMetadata(2.0, null));

	public static readonly DependencyProperty IsStrokeModeProperty = DependencyProperty.Register("IsStrokeMode", typeof(bool), typeof(PomoProgressBar), new PropertyMetadata(true, null));

	public static readonly DependencyProperty TimingSizeProperty = DependencyProperty.Register("TimingSize", typeof(int), typeof(PomoProgressBar), new PropertyMetadata(0, null));

	public static readonly DependencyProperty UnderColorProperty = DependencyProperty.Register("UnderColor", typeof(SolidColorBrush), typeof(PomoProgressBar), new PropertyMetadata(Brushes.Transparent, null));

	public static readonly DependencyProperty TopColorProperty = DependencyProperty.Register("TopColor", typeof(Brush), typeof(PomoProgressBar), new PropertyMetadata(Brushes.Transparent, null));

	private bool _isPrimaryColor;

	private DoubleAnimation _anim;

	internal PomoProgressBar Root;

	internal CycleProgressBar CycleProgress;

	internal Grid TimingGrid;

	internal Path TimingPath;

	private bool _contentLoaded;

	public double TargetAngel
	{
		get
		{
			return (double)GetValue(TargetAngelProperty);
		}
		set
		{
			SetValue(TargetAngelProperty, value);
		}
	}

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

	public int TimingSize
	{
		get
		{
			return (int)GetValue(TimingSizeProperty);
		}
		set
		{
			SetValue(TimingSizeProperty, value);
		}
	}

	public bool IsStrokeMode
	{
		get
		{
			return (bool)GetValue(IsStrokeModeProperty);
		}
		set
		{
			if (IsStrokeMode != value)
			{
				SetValue(IsStrokeModeProperty, value);
				TimingPath.Data = ((!value) ? GetTimingData() : null);
				if (IsStrokeMode)
				{
					CycleProgress.SetBinding(CycleProgressBar.AngleProperty, new Binding("Angle")
					{
						Source = Root
					});
				}
				else
				{
					CycleProgress.Percent = 0.0;
				}
			}
		}
	}

	private static void OnTargetAngelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is PomoProgressBar pomoProgressBar && e.NewValue is double target)
		{
			pomoProgressBar.BeginAnim(target);
		}
	}

	private void BeginAnim(double target)
	{
		if (_anim == null)
		{
			_anim = new DoubleAnimation
			{
				Duration = TimeSpan.FromMilliseconds(1000.0)
			};
		}
		if (Math.Abs(Angle - target) > 0.1)
		{
			_anim.From = ((Angle > 300.0 && target < 60.0) ? 0.0 : Angle);
			_anim.To = target;
			BeginAnimation(AngleProperty, _anim);
		}
	}

	private Geometry GetTimingData()
	{
		return TimingSize switch
		{
			1 => Geometry.Parse(GetPath(base.Height / 2.0, base.Height / 2.0 - 4.0, 60, 1.0)), 
			2 => Geometry.Parse(GetPath(base.Height / 2.0, base.Height / 2.0 - 3.0, 30, 0.5)), 
			_ => Geometry.Parse(GetPath(base.Height / 2.0, base.Height / 2.0 - 12.0, 120, 1.5)), 
		};
	}

	public PomoProgressBar()
	{
		InitializeComponent();
		CycleProgress.Duration = 1000;
		CycleProgress.SetBinding(CycleProgressBar.AngleProperty, new Binding("Angle")
		{
			Source = Root
		});
	}

	private string GetPath(double oR, double iR, int num, double thick)
	{
		string text = "";
		for (int i = 0; i < num; i++)
		{
			double angle = (double)i * Math.PI * 2.0 / (double)num;
			text += GetRect(angle, oR, iR, thick);
		}
		return text;
	}

	private string GetRect(double angle, double oR, double iR, double thick)
	{
		double num = Math.Round(oR + Math.Sin(angle) * oR, 2);
		double num2 = Math.Round(oR - Math.Cos(angle) * oR, 2);
		double num3 = Math.Round(oR + Math.Sin(angle) * iR, 2);
		double num4 = Math.Round(oR - Math.Cos(angle) * iR, 2);
		double num5 = Math.Round(num + Math.Cos(angle) * thick, 2);
		double num6 = Math.Round(num2 + Math.Sin(angle) * thick, 2);
		double num7 = Math.Round(num3 + Math.Cos(angle) * thick, 2);
		double num8 = Math.Round(num4 + Math.Sin(angle) * thick, 2);
		return "M" + num.ToString(NumberFormatInfo.InvariantInfo) + "," + num2.ToString(NumberFormatInfo.InvariantInfo) + " L" + num.ToString(NumberFormatInfo.InvariantInfo) + "," + num2.ToString(NumberFormatInfo.InvariantInfo) + " L" + num3.ToString(NumberFormatInfo.InvariantInfo) + "," + num4.ToString(NumberFormatInfo.InvariantInfo) + " L" + num7.ToString(NumberFormatInfo.InvariantInfo) + "," + num8.ToString(NumberFormatInfo.InvariantInfo) + " L" + num5.ToString(NumberFormatInfo.InvariantInfo) + "," + num6.ToString(NumberFormatInfo.InvariantInfo) + " Z ";
	}

	public void Reset()
	{
		BeginAnimation(AngleProperty, null);
		Angle = 0.0;
		ImageBrush opacityMask = (ImageBrush)FindResource("MaskBrush");
		TimingGrid.OpacityMask = opacityMask;
	}

	public void HideLeftMask()
	{
		TimingGrid.OpacityMask = null;
	}

	public void SetAngle(double angle)
	{
		BeginAnimation(AngleProperty, null);
		Angle = angle;
	}

	public void SetSize(double width)
	{
		base.Height = width;
		base.Width = width;
		TimingPath.Data = ((!IsStrokeMode) ? GetTimingData() : null);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/misc/pomoprogressbar.xaml", UriKind.Relative);
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
			Root = (PomoProgressBar)target;
			break;
		case 2:
			CycleProgress = (CycleProgressBar)target;
			break;
		case 3:
			TimingGrid = (Grid)target;
			break;
		case 4:
			TimingPath = (Path)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
