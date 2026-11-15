using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ticktick_WPF.Views.AI;

internal sealed class AIDesktopBallFade : IDisposable
{
	private double _contentOpacity;

	private double _windowOpacity;

	private readonly SolidColorBrush _opacity = new SolidColorBrush();

	private DispatcherOperation _readyOperation;

	private EventHandler _fadeRendering;

	private EventHandler _rendering;

	internal AIDesktopBallSnapshot ActiveSnapshot { get; private set; }

	internal void Start(FrameworkElement host, double contentOpacity = 1.0, Action ready = null, bool fade = true)
	{
		Dispose();
		_contentOpacity = contentOpacity;
		_windowOpacity = Window.GetWindow(host)?.Opacity ?? 1.0;
		_opacity.Opacity = _windowOpacity;
		DpiScale dpi = VisualTreeHelper.GetDpi(host);
		RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(Math.Max(1, (int)Math.Ceiling(host.ActualWidth * dpi.DpiScaleX)), Math.Max(1, (int)Math.Ceiling(host.ActualHeight * dpi.DpiScaleY)), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
		renderTargetBitmap.Render(host);
		renderTargetBitmap.Freeze();
		AIDesktopBallSnapshot snapshot = new AIDesktopBallSnapshot(renderTargetBitmap, host.PointToScreen(default(Point)), _windowOpacity);
		ActiveSnapshot = snapshot;
		Action action = delegate
		{
			_readyOperation = null;
			if (ActiveSnapshot == snapshot)
			{
				snapshot.Show();
				if (fade)
				{
					BeginFade(snapshot);
				}
				ready?.Invoke();
			}
		};
		if (ready != null)
		{
			_readyOperation = host.Dispatcher.BeginInvoke(action, DispatcherPriority.Render);
		}
		else
		{
			action();
		}
	}

	private void BeginFade(AIDesktopBallSnapshot snapshot)
	{
		AnimationClock animationClock = AIFloatingWindowTransition.AnimateTo(_opacity, Brush.OpacityProperty, 0.0);
		_fadeRendering = delegate
		{
			snapshot.SetOpacity(_opacity.Opacity);
		};
		CompositionTarget.Rendering += _fadeRendering;
		animationClock.Completed += delegate
		{
			if (ActiveSnapshot == snapshot)
			{
				Dispose();
			}
		};
	}

	internal void WaitForRender(Action ready)
	{
		if (_rendering != null)
		{
			CompositionTarget.Rendering -= _rendering;
		}
		int frames = 0;
		TimeSpan? lastFrame = null;
		AIDesktopBallSnapshot snapshot = ActiveSnapshot;
		_rendering = delegate(object sender, EventArgs args)
		{
			if (ActiveSnapshot == snapshot)
			{
				TimeSpan renderingTime = ((RenderingEventArgs)args).RenderingTime;
				if (!(lastFrame == renderingTime))
				{
					lastFrame = renderingTime;
					if (++frames >= 2)
					{
						CompositionTarget.Rendering -= _rendering;
						_rendering = null;
						ready();
					}
				}
			}
		};
		CompositionTarget.Rendering += _rendering;
	}

	internal double? TakeOpacity()
	{
		if (ActiveSnapshot == null)
		{
			return null;
		}
		double val = ((_windowOpacity > 0.0) ? (ActiveSnapshot.Opacity / _windowOpacity * _contentOpacity) : 0.0);
		Dispose();
		return Math.Max(0.0, Math.Min(1.0, val));
	}

	public void Dispose()
	{
		_readyOperation?.Abort();
		_readyOperation = null;
		if (_rendering != null)
		{
			CompositionTarget.Rendering -= _rendering;
		}
		if (_fadeRendering != null)
		{
			CompositionTarget.Rendering -= _fadeRendering;
		}
		_rendering = null;
		_fadeRendering = null;
		AIDesktopBallSnapshot activeSnapshot = ActiveSnapshot;
		ActiveSnapshot = null;
		activeSnapshot?.Dispose();
		_opacity.Opacity = 0.0;
		_opacity.BeginAnimation(Brush.OpacityProperty, null);
	}
}
