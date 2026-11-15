using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ticktick_WPF.Notifier;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc;

public class EmptyView : Grid
{
	private Image _icon1;

	private Image _icon2;

	private Image _icon3;

	private Image _icon4;

	private Image _icon5;

	private Image _icon6;

	private Image _bottomIcon;

	private EmptyIconsSourceModel _model;

	private bool _setIcon5Color;

	public EmptyView()
		: this(200.0)
	{
	}

	public EmptyView(double size)
	{
		EmptyView emptyView = this;
		base.Width = size;
		base.Height = size;
		base.VerticalAlignment = VerticalAlignment.Center;
		base.HorizontalAlignment = HorizontalAlignment.Center;
		_bottomIcon = AddImage();
		_icon3 = AddImage();
		_icon5 = AddImage();
		_icon4 = AddImage();
		_icon6 = AddImage();
		_icon1 = AddImage();
		_icon2 = AddImage();
		base.Loaded += delegate
		{
			DataChangedNotifier.ThemeModeChanged -= emptyView.OnThemeChanged;
			DataChangedNotifier.ThemeModeChanged += emptyView.OnThemeChanged;
			emptyView.SetColor();
		};
		base.Unloaded += delegate
		{
			DataChangedNotifier.ThemeModeChanged -= emptyView.OnThemeChanged;
		};
		Image AddImage()
		{
			Image image = new Image
			{
				Width = size,
				Height = size,
				Stretch = Stretch.Uniform
			};
			emptyView.Children.Add(image);
			return image;
		}
	}

	private void OnThemeChanged(object sender, EventArgs e)
	{
		SetColor();
	}

	public void SetIconsSource(EmptyIconsSourceModel model, bool setIcon5Color = true)
	{
		if (model != null)
		{
			_model = model;
			_setIcon5Color = setIcon5Color;
			_bottomIcon.Source = model.Icon0;
			_icon1.Source = model.Icon1;
			_icon2.Source = model.Icon2;
			_icon3.Source = model.Icon3;
			_icon4.Source = model.Icon4;
			_icon5.Source = model.Icon5;
			_icon6.Source = model.Icon6;
			SetColor();
		}
	}

	private void SetColor()
	{
		if (_model != null)
		{
			SetImageColor(_model.Icon0?.Drawing, "EmptyIconColor0");
			SetImageColor(_model.Icon1?.Drawing, "EmptyIconColor1");
			SetImageColor(_model.Icon2?.Drawing, "EmptyIconColor2");
			SetImageColor(_model.Icon3?.Drawing, "EmptyIconColor3");
			SetImageColor(_model.Icon4?.Drawing, "PrimaryColor");
			if (_setIcon5Color)
			{
				SetImageColor(_model.Icon5?.Drawing, "EmptyIconColor5");
			}
			SetImageColor(_model.Icon6?.Drawing, "EmptyIconColor6");
		}
	}

	private void SetImageColor(Drawing drawing, string colorKey)
	{
		if (!(drawing is DrawingGroup drawingGroup))
		{
			if (drawing is GeometryDrawing geometryDrawing)
			{
				geometryDrawing.Brush = ThemeUtil.GetColor(colorKey);
			}
			return;
		}
		foreach (Drawing child in drawingGroup.Children)
		{
			SetImageColor(child, colorKey);
		}
	}

	public void Clear()
	{
		_bottomIcon.Source = null;
		_icon1.Source = null;
		_icon2.Source = null;
		_icon3.Source = null;
		_icon4.Source = null;
		_icon5.Source = null;
		_icon6.Source = null;
		_bottomIcon = null;
		_icon1 = null;
		_icon2 = null;
		_icon3 = null;
		_icon4 = null;
		_icon5 = null;
		_icon6 = null;
		DataChangedNotifier.ThemeModeChanged -= OnThemeChanged;
		base.Children.Clear();
	}
}
