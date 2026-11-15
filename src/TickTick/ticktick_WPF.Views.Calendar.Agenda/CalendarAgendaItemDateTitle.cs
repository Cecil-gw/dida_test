using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Calendar.Agenda;

public class CalendarAgendaItemDateTitle : Canvas
{
	private TextBlock _dayText;

	private TextBlock _monthText;

	private TextBlock _lunarText;

	private Image _image;

	private DateTime? _date;

	private bool _isRest;

	private bool _isWork;

	public CalendarAgendaItemDateTitle()
	{
		base.Height = 38.0;
		base.Width = 130.0;
		base.HorizontalAlignment = HorizontalAlignment.Left;
		base.VerticalAlignment = VerticalAlignment.Top;
	}

	private void InitUi()
	{
		_dayText = new TextBlock
		{
			FontSize = 24.0,
			FontWeight = FontWeights.Bold,
			LineHeight = 32.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		_dayText.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity80");
		base.Children.Add(_dayText);
		_monthText = new TextBlock
		{
			FontSize = 11.0,
			LineHeight = 16.0
		};
		_monthText.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity60");
		base.Children.Add(_monthText);
		_lunarText = new TextBlock
		{
			FontSize = 11.0,
			LineHeight = 16.0
		};
		_lunarText.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity60");
		base.Children.Add(_lunarText);
	}

	public void SetDateText(DateTime? date, bool isRest = false, bool isWork = false, bool isVirtualToday = false)
	{
		DateTime? date2 = _date;
		DateTime? dateTime = date;
		if (date2.HasValue == dateTime.HasValue && (!date2.HasValue || date2.GetValueOrDefault() == dateTime.GetValueOrDefault()) && _isRest == isRest && _isWork == isWork)
		{
			return;
		}
		_date = date;
		_isRest = isRest;
		_isWork = isWork;
		if (!date.HasValue)
		{
			if (_dayText != null)
			{
				_dayText.Text = "";
				_monthText.Text = "";
				_lunarText.Text = "";
				if (_image != null)
				{
					_image.Source = null;
				}
			}
			return;
		}
		if (_dayText == null)
		{
			InitUi();
		}
		DateTime value = date.Value;
		string text = value.Day.ToString() ?? "";
		double num = Utils.MeasureStringWidth(text, 24.0, FontWeights.Bold);
		_dayText.Text = text;
		_monthText.Text = value.ToString("ddd", App.Ci);
		Canvas.SetTop(_dayText, 3.0);
		Canvas.SetLeft(_monthText, num + 9.0);
		string lunarText = DateUtils.GetLunarText(value, showWeek: false);
		if (!string.IsNullOrEmpty(lunarText))
		{
			Canvas.SetLeft(_lunarText, num + 9.0);
			Canvas.SetTop(_monthText, 3.0);
			Canvas.SetTop(_lunarText, 19.0);
			_lunarText.Text = lunarText;
		}
		else
		{
			Canvas.SetTop(_monthText, 14.0);
			_lunarText.Text = "";
		}
		bool flag = isVirtualToday || date.Value.Date == DateTime.Today;
		_dayText.SetResourceReference(TextBlock.ForegroundProperty, flag ? "PrimaryColor" : "BaseSolidColorOpacity80");
		_monthText.SetResourceReference(TextBlock.ForegroundProperty, flag ? "PrimaryColor" : "BaseSolidColorOpacity60");
		_lunarText.SetResourceReference(TextBlock.ForegroundProperty, flag ? "PrimaryColor" : "BaseSolidColorOpacity60");
		if (isRest || isWork)
		{
			if (_image == null)
			{
				_image = new Image
				{
					Height = 10.0,
					Width = 10.0,
					Stretch = Stretch.Uniform
				};
				base.Children.Add(_image);
			}
			Canvas.SetLeft(_image, num - 3.0);
			_image.SetResourceReference(Image.SourceProperty, isRest ? "RestDrawingImage" : "WorkDrawingImage");
		}
		else if (_image != null)
		{
			_image.Source = null;
		}
	}

	public void SetNoTaskText()
	{
	}
}
