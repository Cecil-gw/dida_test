using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Detail;

public class DetailHintPanel : StackPanel, IComponentConnector
{
	private DrawingImage _imageSource;

	private bool _isTask;

	private bool _ignoreTheme;

	private bool _visible;

	internal ImageBrush ImageBrush;

	private bool _contentLoaded;

	public DetailHintPanel(bool isTask)
	{
		InitializeComponent();
		_isTask = isTask;
		SetImage();
		SetColor();
		base.Loaded += delegate
		{
			DataChangedNotifier.ThemeModeChanged -= OnThemeChanged;
			DataChangedNotifier.ThemeModeChanged += OnThemeChanged;
			SetColor();
		};
		base.Unloaded += delegate
		{
			DataChangedNotifier.ThemeModeChanged -= OnThemeChanged;
		};
	}

	private void OnThemeChanged(object sender, EventArgs e)
	{
		SetColor();
	}

	private void SetColor()
	{
		string themeId = LocalSettings.Settings.ThemeId;
		_visible = _ignoreTheme || ThemeKey.IsColorTheme(themeId) || ThemeKey.IsDarkTheme(themeId);
		SolidColorBrush color = (_visible ? ThemeUtil.GetColor("DetailEmptyColor") : Brushes.Transparent);
		SetImageColor(_imageSource?.Drawing, color);
	}

	private void SetImageColor(Drawing drawing, SolidColorBrush color)
	{
		if (!(drawing is DrawingGroup drawingGroup))
		{
			if (drawing is GeometryDrawing geometryDrawing)
			{
				if (geometryDrawing.Pen != null)
				{
					geometryDrawing.Pen.Brush = color;
				}
				else
				{
					geometryDrawing.Brush = color;
				}
			}
			return;
		}
		foreach (Drawing child in drawingGroup.Children)
		{
			SetImageColor(child, color);
		}
	}

	private void SetImage()
	{
		if (_isTask)
		{
			_imageSource = Utils.GetImageSource("TaskDetailEmptyImage");
			_imageSource = _imageSource.Clone();
			ImageBrush.ImageSource = _imageSource;
		}
		else
		{
			_imageSource = Utils.GetImageSource("HabitDetailEmptyImage");
			_imageSource = _imageSource.Clone();
			ImageBrush.ImageSource = _imageSource;
		}
	}

	public void SetIgnoreTheme(bool ignore)
	{
		_ignoreTheme = ignore;
		SetColor();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/detail/detailhintpanel.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 1)
		{
			ImageBrush = (ImageBrush)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
