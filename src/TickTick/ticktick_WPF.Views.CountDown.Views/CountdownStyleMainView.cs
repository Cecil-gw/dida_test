using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CountDown.ViewModels;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.CountDown.Views;

public class CountdownStyleMainView : UserControl, IComponentConnector
{
	internal Border PreviewCard;

	internal ContentControl StylePreviewCard;

	internal Button ChooseImageButton;

	internal Border ImageButton;

	internal FrameworkElement OptionHolder;

	internal EscPopup OptionPopup;

	private bool _contentLoaded;

	private CountDownStyleSetViewModel viewModel { get; set; }

	public CountdownStyleMainView()
	{
		InitializeComponent();
	}

	private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		viewModel = e.NewValue as CountDownStyleSetViewModel;
	}

	private void OnChangeImageClick(object sender, RoutedEventArgs routedEventArgs)
	{
		if (OptionPopup.IsOpen)
		{
			OptionPopup.IsOpen = false;
		}
		else
		{
			ShowContextMenu(OptionPopup);
		}
	}

	private void ShowContextMenu(Popup popupControl)
	{
		CustomMenuList customMenuList = new CustomMenuList(new List<CustomMenuItemViewModel>
		{
			new CustomMenuItemViewModel("Crop", Utils.GetString("crop"), Utils.GetGeometry("Ic.Countdown.Cut"))
			{
				ShowSelectedIcon = false
			},
			new CustomMenuItemViewModel("ChangeImage", Utils.GetString("change_image"), Utils.GetGeometry("Ic.Countdown.Picture"))
			{
				ShowSelectedIcon = false
			}
		}.Where((CustomMenuItemViewModel item) => item != null).ToList(), popupControl);
		customMenuList.Operated += OnItemSelected;
		customMenuList.Show();
	}

	private async void OnItemSelected(object sender, object key)
	{
		switch (key as string)
		{
		case "ChangeImage":
			OptionPopup.IsOpen = false;
			viewModel?.ShowSelectImageWindow();
			break;
		case "Crop":
			viewModel?.ShowCropWindow();
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/countdown/views/countdownstylemainview.xaml", UriKind.Relative);
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
			((CountdownStyleMainView)target).DataContextChanged += UserControl_DataContextChanged;
			break;
		case 2:
			PreviewCard = (Border)target;
			break;
		case 3:
			StylePreviewCard = (ContentControl)target;
			break;
		case 4:
			ChooseImageButton = (Button)target;
			ChooseImageButton.Click += OnChangeImageClick;
			break;
		case 5:
			ImageButton = (Border)target;
			break;
		case 6:
			OptionHolder = (FrameworkElement)target;
			break;
		case 7:
			OptionPopup = (EscPopup)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
