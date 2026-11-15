using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Misc.ColorSelector.Media;

namespace ticktick_WPF.Views.Misc.ColorSelector;

public class ColorPicker : UserControl, IComponentConnector
{
	private double H;

	private double S = 1.0;

	private double B = 1.0;

	private SolidColorBrush _selectColor = Brushes.Transparent;

	private int _r = 255;

	private int _g = 255;

	private int _b = 255;

	private Popup _popup;

	internal ColorPicker root;

	internal Border viewSelectColor;

	internal Rectangle viewSelectColor1;

	internal Rectangle viewSelectColor2;

	internal ThumbPro thumbSB;

	internal GradientStop viewLine1;

	internal GradientStop viewLine2;

	internal GradientStop viewLine3;

	internal GradientStop viewLine4;

	internal GradientStop viewLine5;

	internal GradientStop viewLine6;

	internal GradientStop viewLine7;

	internal ThumbPro thumbH;

	internal Ellipse DisplayEll;

	internal TextBox TextR;

	internal TextBox TextG;

	internal TextBox TextB;

	internal TextBox TextHex;

	internal Button SaveButton;

	private bool _contentLoaded;

	public SolidColorBrush SelectColor
	{
		get
		{
			return _selectColor;
		}
		set
		{
			_selectColor = value;
			DisplayEll.Fill = SelectColor;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectColor"));
		}
	}

	public event EventHandler<string> ColorSelected;

	public event PropertyChangedEventHandler PropertyChanged;

	public ColorPicker(Popup popup, string selectedColor)
	{
		InitializeComponent();
		_popup = popup;
		Init(selectedColor);
	}

	private async Task Init(string selectedColor)
	{
		if (ThemeUtil.IsEmptyColor(selectedColor))
		{
			selectedColor = (ThemeUtil.IsDark() ? "#000000" : "#FFFFFF");
		}
		RgbaColor rColor = new RgbaColor(selectedColor);
		TextHex.Text = ThemeUtil.GetNoAlphaColorString(rColor.HexString);
		SelectColor = rColor.SolidColorBrush;
		TextR.Text = rColor.R.ToString();
		TextG.Text = rColor.G.ToString();
		TextB.Text = rColor.B.ToString();
		_r = rColor.R;
		_g = rColor.G;
		_b = rColor.B;
		while (base.ActualWidth < 10.0)
		{
			await Task.Delay(5);
		}
		RColorChange(rColor);
	}

	private void ThumbPro_ValueChanged(double xpercent, double ypercent)
	{
		H = 360.0 * xpercent;
		HsbaColor hsbaColor = new HsbaColor(H, 1.0, 1.0);
		viewSelectColor.Background = hsbaColor.SolidColorBrush;
		thumbH.Background = hsbaColor.SolidColorBrush;
		hsbaColor = new HsbaColor(H, S, B);
		SelectColor = hsbaColor.SolidColorBrush;
		HColorChange(hsbaColor);
	}

	private void ThumbPro_ValueChanged_1(double xpercent, double ypercent)
	{
		S = xpercent;
		B = 1.0 - ypercent;
		HsbaColor hsbaColor = new HsbaColor(H, S, B);
		SelectColor = hsbaColor.SolidColorBrush;
		HColorChange(hsbaColor);
	}

	private void HColorChange(HsbaColor hColor)
	{
		RgbaColor rgbaColor = hColor.RgbaColor;
		TextR.Text = rgbaColor.R.ToString();
		TextG.Text = rgbaColor.G.ToString();
		TextB.Text = rgbaColor.B.ToString();
		_r = rgbaColor.R;
		_g = rgbaColor.G;
		_b = rgbaColor.B;
		TextHex.Text = ThemeUtil.GetNoAlphaColorString(rgbaColor.HexString);
	}

	private void RColorChange(RgbaColor rColor)
	{
		HsbaColor hsbaColor = rColor.HsbaColor;
		H = hsbaColor.H;
		S = hsbaColor.S;
		B = hsbaColor.B;
		thumbH.SetTopLeftByPercent(H / 360.0, 0.0);
		thumbSB.SetTopLeftByPercent(S, 1.0 - B);
		hsbaColor.S = 1.0;
		hsbaColor.B = 1.0;
		viewSelectColor.Background = hsbaColor.SolidColorBrush;
		thumbH.Background = hsbaColor.SolidColorBrush;
	}

	private void OnNumPreviewInput(object sender, TextCompositionEventArgs e)
	{
		if (e.Text.Length >= 1 && !char.IsDigit(e.Text, e.Text.Length - 1))
		{
			e.Handled = true;
		}
	}

	private void OnTextChanged(object sender, TextChangedEventArgs e)
	{
		if (sender is TextBox textBox && string.IsNullOrEmpty(textBox.Text))
		{
			return;
		}
		if (TextHex.Equals(sender))
		{
			string text = TextHex.Text;
			if (text.Length < 6)
			{
				return;
			}
			try
			{
				if (!text.StartsWith("#"))
				{
					text = "#" + text;
				}
				Color color = (Color)ColorConverter.ConvertFromString(text);
				RgbaColor rgbaColor = new RgbaColor(color.R, color.G, color.B, color.A);
				string noAlphaColorString = ThemeUtil.GetNoAlphaColorString(rgbaColor.HexString);
				if (TextHex.Text != noAlphaColorString)
				{
					TextHex.TextChanged -= OnTextChanged;
					TextHex.Text = noAlphaColorString;
					TextHex.TextChanged += OnTextChanged;
				}
				SelectColor = rgbaColor.SolidColorBrush;
				TextR.Text = rgbaColor.R.ToString();
				TextG.Text = rgbaColor.G.ToString();
				TextB.Text = rgbaColor.B.ToString();
				_r = rgbaColor.R;
				_g = rgbaColor.G;
				_b = rgbaColor.B;
				RColorChange(rgbaColor);
				return;
			}
			catch (Exception)
			{
				return;
			}
		}
		if (TextR.Equals(sender))
		{
			if (int.TryParse(TextR.Text, out var result))
			{
				if (result > 255 || result < 0)
				{
					result = ((result > 255) ? 255 : 0);
					TextR.Text = result.ToString();
				}
				_r = result;
			}
			else
			{
				TextR.Text = _r.ToString();
			}
		}
		if (TextG.Equals(sender))
		{
			if (int.TryParse(TextG.Text, out var result2))
			{
				if (result2 > 255 || result2 < 0)
				{
					result2 = ((result2 > 255) ? 255 : 0);
					TextG.Text = result2.ToString();
				}
				_g = result2;
			}
			else
			{
				TextG.Text = _g.ToString();
			}
		}
		if (TextB.Equals(sender))
		{
			if (int.TryParse(TextB.Text, out var result3))
			{
				if (result3 > 255 || result3 < 0)
				{
					result3 = ((result3 > 255) ? 255 : 0);
					TextB.Text = result3.ToString();
				}
				_b = result3;
			}
			else
			{
				TextB.Text = _b.ToString();
			}
		}
		RgbaColor rgbaColor2 = new RgbaColor(_r, _g, _b);
		SelectColor = rgbaColor2.SolidColorBrush;
		TextHex.Text = ThemeUtil.GetNoAlphaColorString(rgbaColor2.HexString);
		RColorChange(rgbaColor2);
	}

	private void OnSaveClick(object sender, RoutedEventArgs e)
	{
		string noAlphaColorString = ThemeUtil.GetNoAlphaColorString(new RgbaColor(_r, _g, _b).SolidColorBrush.Color.ToString());
		LocalSettings.Settings.SaveCustomColor(noAlphaColorString);
		this.ColorSelected?.Invoke(this, noAlphaColorString);
		_popup.IsOpen = false;
		UserActCollectUtils.AddClickEvent("project_list_ui", "list_color", "custom");
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		_popup.IsOpen = false;
	}

	private async void OnGotFocus(object sender, RoutedEventArgs e)
	{
		if (sender is TextBox t)
		{
			t.TextChanged -= OnTextChanged;
			t.TextChanged += OnTextChanged;
			await Task.Delay(200);
			t.SelectAll();
		}
	}

	private void OnLostFocus(object sender, RoutedEventArgs e)
	{
		if (sender is TextBox textBox)
		{
			textBox.Select(0, 0);
			textBox.TextChanged -= OnTextChanged;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/misc/colorselector/colorpicker.xaml", UriKind.Relative);
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
			root = (ColorPicker)target;
			break;
		case 2:
			viewSelectColor = (Border)target;
			break;
		case 3:
			viewSelectColor1 = (Rectangle)target;
			break;
		case 4:
			viewSelectColor2 = (Rectangle)target;
			break;
		case 5:
			thumbSB = (ThumbPro)target;
			break;
		case 6:
			viewLine1 = (GradientStop)target;
			break;
		case 7:
			viewLine2 = (GradientStop)target;
			break;
		case 8:
			viewLine3 = (GradientStop)target;
			break;
		case 9:
			viewLine4 = (GradientStop)target;
			break;
		case 10:
			viewLine5 = (GradientStop)target;
			break;
		case 11:
			viewLine6 = (GradientStop)target;
			break;
		case 12:
			viewLine7 = (GradientStop)target;
			break;
		case 13:
			thumbH = (ThumbPro)target;
			break;
		case 14:
			DisplayEll = (Ellipse)target;
			break;
		case 15:
			TextR = (TextBox)target;
			TextR.PreviewTextInput += OnNumPreviewInput;
			TextR.GotFocus += OnGotFocus;
			TextR.LostFocus += OnLostFocus;
			break;
		case 16:
			TextG = (TextBox)target;
			TextG.PreviewTextInput += OnNumPreviewInput;
			TextG.GotFocus += OnGotFocus;
			TextG.LostFocus += OnLostFocus;
			break;
		case 17:
			TextB = (TextBox)target;
			TextB.PreviewTextInput += OnNumPreviewInput;
			TextB.GotFocus += OnGotFocus;
			TextB.LostFocus += OnLostFocus;
			break;
		case 18:
			TextHex = (TextBox)target;
			TextHex.GotFocus += OnGotFocus;
			TextHex.LostFocus += OnLostFocus;
			break;
		case 19:
			SaveButton = (Button)target;
			SaveButton.Click += OnSaveClick;
			break;
		case 20:
			((Button)target).Click += OnCancelClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
