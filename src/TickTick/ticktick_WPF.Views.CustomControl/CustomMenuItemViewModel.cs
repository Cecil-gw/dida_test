using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.CustomControl;

public class CustomMenuItemViewModel : UpDownSelectViewModel
{
	private string _text;

	private string _rightText;

	private Geometry _icon;

	private SolidColorBrush _customIconColor;

	public bool IsDelete { get; set; }

	public object Value { get; set; }

	public List<CustomMenuItemViewModel> SubActions { get; set; }

	public string Text
	{
		get
		{
			return _text;
		}
		set
		{
			_text = value;
			OnPropertyChanged("Text");
		}
	}

	public string RightText
	{
		get
		{
			return _rightText;
		}
		set
		{
			_rightText = value;
			OnPropertyChanged("RightText");
		}
	}

	public double ImageWidth { get; set; } = 18.0;

	public double MinWidth { get; set; }

	public Thickness ImageMargin { get; set; } = new Thickness(12.0, 0.0, 0.0, 0.0);

	public DrawingImage Image { get; set; }

	public Thickness TextMargin { get; set; } = new Thickness(8.0, 1.0, 8.0, 0.0);

	public Geometry Icon
	{
		get
		{
			return _icon;
		}
		set
		{
			_icon = value;
			OnPropertyChanged("Icon");
		}
	}

	public Geometry ExtraIcon { get; set; }

	public SolidColorBrush ExtraIconColor { get; set; }

	public string ExtraIconTips { get; set; }

	public int ExtraIconAngle { get; set; }

	public int ExtraIconSize { get; set; } = 18;

	public bool IsImage { get; set; }

	public bool IsSplit { get; set; }

	public bool IsCenterText { get; set; }

	public bool UseCustomIconColor => CustomIconColor != null;

	public SolidColorBrush CustomIconColor
	{
		get
		{
			return _customIconColor;
		}
		set
		{
			_customIconColor = value;
			OnPropertyChanged("CustomIconColor");
			OnPropertyChanged("UseCustomIconColor");
		}
	}

	public override bool HasChildren
	{
		get
		{
			List<CustomMenuItemViewModel> subActions = SubActions;
			if ((subActions == null || subActions.Count <= 0) && SubControl == null)
			{
				return SubContent != null;
			}
			return true;
		}
	}

	public int FontSize { get; set; } = 13;

	public int TextLineHeight { get; set; } = 19;

	public bool IsMessage { get; set; }

	public bool IsStandaloneContent { get; set; }

	public ITabControl SubControl { get; set; }

	public FrameworkElement SubContent { get; set; }

	public double SubPopupWidth { get; set; } = double.NaN;

	public bool NeedSubContentStyle { get; set; } = true;

	public FrameworkElement UIContent { get; set; }

	public string UnableText { get; set; }

	public CustomMenuItemViewModel(object val, string title, DrawingImage image, string rightText = "")
	{
		Text = title;
		Image = image;
		Value = val;
		IsImage = image != null;
		ImageWidth = ((image != null) ? 16 : 0);
		RightText = rightText;
		MinWidth = GetTextWidth(title, rightText) + ImageWidth + 30.0;
		if (image == null)
		{
			TextMargin = new Thickness(12.0, 0.0, 8.0, 0.0);
			ImageMargin = new Thickness(0.0);
		}
	}

	private double GetTextWidth(string title, string rightText)
	{
		return Utils.MeasureString(title + rightText, new Typeface(Utils.GetDefaultFontFamily(), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), 13.0).Width;
	}

	public CustomMenuItemViewModel(object val, string title, Geometry icon, string rightText = "")
	{
		Text = title;
		_icon = icon;
		Value = val;
		RightText = rightText;
		MinWidth = GetTextWidth(title, rightText) + (double)((icon != null) ? 30 : 0) + 30.0;
		if (icon == null)
		{
			TextMargin = new Thickness(12.0, 0.0, 4.0, 0.0);
		}
	}

	public CustomMenuItemViewModel(object val)
	{
		IsSplit = true;
		base.IsEnable = false;
	}

	public void SetExtraIconAngle(int angle)
	{
		ExtraIconAngle = angle;
		OnPropertyChanged("ExtraIconAngle");
	}
}
