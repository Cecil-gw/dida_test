using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using ticktick_WPF.Views.CountDown.ViewModels;
using ticktick_WPF.Views.Misc.ColorSelector;

namespace ticktick_WPF.Views.CountDown.Views;

public class CountdownStyleColorPicker : UserControl, IComponentConnector
{
	internal ColorSelector NormalColorItems;

	internal ColorSelector FullscreenColors;

	internal ColorSelector CalendarColors;

	internal ColorSelector DopamineColors;

	internal ColorSelector TwoColorCard;

	internal ColorSelector CartoonColors;

	private bool _contentLoaded;

	private CountDownStyleSetViewModel viewModel { get; set; }

	public CountdownStyleColorPicker()
	{
		InitializeComponent();
	}

	private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (viewModel != null)
		{
			CountDownStyleSetViewModel countDownStyleSetViewModel = viewModel;
			countDownStyleSetViewModel.OnCountdownChanged = (Action)Delegate.Remove(countDownStyleSetViewModel.OnCountdownChanged, new Action(OnCountdownChanged));
		}
		viewModel = e.NewValue as CountDownStyleSetViewModel;
		if (viewModel != null)
		{
			CountDownStyleSetViewModel countDownStyleSetViewModel2 = viewModel;
			countDownStyleSetViewModel2.OnCountdownChanged = (Action)Delegate.Combine(countDownStyleSetViewModel2.OnCountdownChanged, new Action(OnCountdownChanged));
		}
	}

	private void UserControl_Unloaded(object sender, RoutedEventArgs e)
	{
		if (viewModel != null)
		{
			CountDownStyleSetViewModel countDownStyleSetViewModel = viewModel;
			countDownStyleSetViewModel.OnCountdownChanged = (Action)Delegate.Remove(countDownStyleSetViewModel.OnCountdownChanged, new Action(OnCountdownChanged));
		}
	}

	private void InitColorSelectors()
	{
		NormalColorItems.SetCustomColorList(viewModel._normalColors);
		NormalColorItems.SetSelectedColor("transparent");
		FullscreenColors.SetCustomColorList(viewModel._fullscreenColors);
		FullscreenColors.SetSelectedColor(viewModel._fullscreenColors[0]);
		CalendarColors.SetCustomColorList(viewModel._calendarColors);
		CalendarColors.SetSelectedColor("transparent");
		DopamineColors.SetCustomColorList(viewModel._dopamineColors);
		DopamineColors.SetSelectedColor(viewModel._dopamineColors[0]);
		TwoColorCard.SetCustomColorList(viewModel._twoColorColors);
		TwoColorCard.SetSelectedColor(viewModel._twoColorColors[0]);
		CartoonColors.SetCustomColorList(viewModel._cartoonColors);
		CartoonColors.SetSelectedColor(viewModel._cartoonColors[0]);
	}

	private void OnCountdownChanged()
	{
		if (viewModel?.Model == null)
		{
			return;
		}
		InitColorSelectors();
		if (viewModel.Model.StyleColors == null || !viewModel.Model.StyleColors.Any())
		{
			return;
		}
		string selectedColor = viewModel.Model.StyleColors.First();
		string style = viewModel.Model.Style;
		if (style == null)
		{
			return;
		}
		switch (style.Length)
		{
		case 8:
		{
			char c = style[0];
			if (c != 'c')
			{
				switch (c)
				{
				default:
					return;
				case 'p':
					break;
				case 'd':
					if (style == "dopamine")
					{
						DopamineColors.SetSelectedColor(selectedColor);
					}
					return;
				}
				if (!(style == "polaroid"))
				{
					break;
				}
				goto IL_0148;
			}
			if (!(style == "calendar"))
			{
				break;
			}
			goto IL_013b;
		}
		case 6:
			if (!(style == "normal"))
			{
				break;
			}
			goto IL_013b;
		case 16:
			if (!(style == "fullscreen_image"))
			{
				break;
			}
			goto IL_0148;
		case 9:
			if (style == "perpetual")
			{
				CalendarColors.SetSelectedColor(selectedColor);
			}
			break;
		case 14:
			if (style == "two_color_card")
			{
				TwoColorCard.SetSelectedColor(selectedColor);
			}
			break;
		case 7:
			if (style == "cartoon")
			{
				CartoonColors.SetSelectedColor(selectedColor);
			}
			break;
		case 10:
		case 11:
		case 12:
		case 13:
		case 15:
			break;
			IL_0148:
			FullscreenColors.SetSelectedColor(selectedColor);
			break;
			IL_013b:
			NormalColorItems.SetSelectedColor(selectedColor);
			break;
		}
	}

	private void OnNormalTextColorSelect(object sender, string color)
	{
		if (sender is FrameworkElement frameworkElement)
		{
			string styleType = frameworkElement.Tag.ToString();
			viewModel?.OnColorSelect(styleType, color);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/countdown/views/countdownstylecolorpicker.xaml", UriKind.Relative);
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
			((CountdownStyleColorPicker)target).DataContextChanged += UserControl_DataContextChanged;
			((CountdownStyleColorPicker)target).Unloaded += UserControl_Unloaded;
			break;
		case 2:
			NormalColorItems = (ColorSelector)target;
			break;
		case 3:
			FullscreenColors = (ColorSelector)target;
			break;
		case 4:
			CalendarColors = (ColorSelector)target;
			break;
		case 5:
			DopamineColors = (ColorSelector)target;
			break;
		case 6:
			TwoColorCard = (ColorSelector)target;
			break;
		case 7:
			CartoonColors = (ColorSelector)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
