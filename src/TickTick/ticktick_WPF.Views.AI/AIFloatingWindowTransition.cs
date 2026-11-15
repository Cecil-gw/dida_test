using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ticktick_WPF.Views.AI;

internal sealed class AIFloatingWindowTransition : IDisposable
{
	private const double MinimizedScale = 0.1;

	private readonly FrameworkElement _view;

	private readonly ScaleTransform _scale = new ScaleTransform(1.0, 1.0);

	private int _animationVersion;

	private bool _isHiding;

	internal static TimeSpan TransitionDuration => TimeSpan.FromMilliseconds(300.0);

	internal bool IsShowRequested { get; private set; }

	internal AIFloatingWindowTransition(FrameworkElement view)
	{
		_view = view;
		_view.RenderTransformOrigin = new Point(1.0, 1.0);
		_view.RenderTransform = _scale;
	}

	public void WarmUp()
	{
		_view.ApplyTemplate();
	}

	public void ShowImmediately()
	{
		_animationVersion++;
		IsShowRequested = true;
		_isHiding = false;
		Reset(1.0, 1.0);
		_view.Visibility = Visibility.Visible;
		_view.IsHitTestVisible = true;
	}

	public void ShowAnimated()
	{
		IsShowRequested = true;
		if (_view.Visibility != Visibility.Visible || _isHiding)
		{
			_animationVersion++;
			_isHiding = false;
			if (_view.Visibility != Visibility.Visible)
			{
				Reset(0.1, 0.0);
			}
			_view.Visibility = Visibility.Visible;
			_view.IsHitTestVisible = true;
			Animate(1.0, 1.0);
		}
	}

	public void HideAnimated(Action completed = null)
	{
		IsShowRequested = false;
		if (_view.Visibility == Visibility.Visible && !_isHiding)
		{
			_animationVersion++;
			_isHiding = true;
			_view.IsHitTestVisible = false;
			Animate(0.1, 0.0, delegate
			{
				HideImmediately();
				completed?.Invoke();
			});
		}
	}

	public void HideImmediately()
	{
		_animationVersion++;
		IsShowRequested = false;
		_isHiding = false;
		_view.IsHitTestVisible = false;
		_view.Visibility = Visibility.Hidden;
		Reset(1.0, 1.0);
	}

	private void Animate(double scale, double opacity, Action completed = null)
	{
		AnimateTo(_scale, ScaleTransform.ScaleXProperty, scale);
		AnimateTo(_scale, ScaleTransform.ScaleYProperty, scale);
		double opacity2 = _view.Opacity;
		_view.BeginAnimation(UIElement.OpacityProperty, null);
		_view.Opacity = opacity2;
		DoubleAnimationUsingKeyFrames doubleAnimationUsingKeyFrames = CreateWindowTransitionAnimation(opacity2, opacity);
		int version = _animationVersion;
		doubleAnimationUsingKeyFrames.Completed += delegate
		{
			if (version == _animationVersion)
			{
				Reset(scale, opacity);
				completed?.Invoke();
			}
		};
		_view.BeginAnimation(UIElement.OpacityProperty, doubleAnimationUsingKeyFrames);
	}

	private void Reset(double scale, double opacity)
	{
		ScaleTransform scale2 = _scale;
		double scaleX = (_scale.ScaleY = scale);
		scale2.ScaleX = scaleX;
		_scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
		_scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
		_view.Opacity = opacity;
		_view.BeginAnimation(UIElement.OpacityProperty, null);
	}

	public void Dispose()
	{
		HideImmediately();
	}

	internal static AnimationClock AnimateTo(IAnimatable target, DependencyProperty property, double targetValue)
	{
		DependencyObject obj = (DependencyObject)target;
		double num = (double)obj.GetValue(property);
		target.BeginAnimation(property, null);
		obj.SetValue(property, num);
		AnimationClock animationClock = CreateWindowTransitionAnimation(num, targetValue).CreateClock();
		target.ApplyAnimationClock(property, animationClock);
		return animationClock;
	}

	internal static DoubleAnimationUsingKeyFrames CreateWindowTransitionAnimation(double from, double to)
	{
		TimeSpan transitionDuration = TransitionDuration;
		return new DoubleAnimationUsingKeyFrames
		{
			Duration = transitionDuration,
			FillBehavior = FillBehavior.HoldEnd,
			KeyFrames = 
			{
				(DoubleKeyFrame)new DiscreteDoubleKeyFrame(from, KeyTime.FromTimeSpan(TimeSpan.Zero)),
				(DoubleKeyFrame)new SplineDoubleKeyFrame(to, KeyTime.FromTimeSpan(transitionDuration), new KeySpline(0.4, 0.0, 0.2, 1.0))
			}
		};
	}
}
