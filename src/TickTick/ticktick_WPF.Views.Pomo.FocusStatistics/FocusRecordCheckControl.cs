using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using Emoji.Wpf;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar;

namespace ticktick_WPF.Views.Pomo.FocusStatistics;

public class FocusRecordCheckControl : UserControl, IComponentConnector
{
	internal ItemsControl TitleItemsControl;

	private bool _contentLoaded;

	public FocusRecordCheckControl()
	{
		InitializeComponent();
		base.DataContextChanged += delegate
		{
			ResetTitles();
		};
	}

	private void ResetTitles()
	{
		TitleItemsControl.Items.Clear();
		if (base.DataContext is FocusRecordItemViewModel focusRecordItemViewModel)
		{
			for (int i = 0; i < focusRecordItemViewModel.Titles.Count; i++)
			{
				EmjTextBlock emjTextBlock = new EmjTextBlock
				{
					Text = focusRecordItemViewModel.Titles[i],
					FontSize = 14.0,
					TextTrimming = TextTrimming.CharacterEllipsis,
					Margin = new Thickness(0.0, 4.0, 0.0, 4.0)
				};
				emjTextBlock.SetResourceReference(TextBlock.ForegroundProperty, focusRecordItemViewModel.UnlinkedTitleIndexes.Contains(i) ? "BaseColorOpacity40" : "BaseColorOpacity100");
				TitleItemsControl.Items.Add(emjTextBlock);
			}
		}
	}

	private async void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (!(sender is Border border))
		{
			return;
		}
		FocusRecordItemViewModel viewModel = base.DataContext as FocusRecordItemViewModel;
		if (viewModel == null)
		{
			return;
		}
		UserActCollectUtils.AddClickEvent("focus", "focus_tab", "focus_record");
		PomodoroModel pomodoroModel = await PomoService.GetAdjustPomo(viewModel.FocusId);
		if (pomodoroModel != null)
		{
			PomoDetailWindow pomoDetailWindow = new PomoDetailWindow(new PomoDisplayViewModel(pomodoroModel, pomodoroModel.Tasks), (CalendarDisplayViewModel)null);
			pomoDetailWindow.SetCustomStyle();
			pomoDetailWindow.Closed += async delegate
			{
				await Task.Delay(200);
				await (viewModel?.ResetTitle());
				ResetTitles();
			};
			pomoDetailWindow.Show(border, border.Width, showByMouse: true);
		}
	}

	private void CheckBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		CheckBox checkBox = sender as CheckBox;
		if (checkBox.IsChecked == true)
		{
			checkBox.IsChecked = false;
			e.Handled = true;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/pomo/focusstatistics/focusrecordcheckcontrol.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			((CheckBox)target).PreviewMouseLeftButtonDown += CheckBox_PreviewMouseLeftButtonDown;
			break;
		case 2:
			((Border)target).MouseLeftButtonUp += Border_MouseLeftButtonUp;
			break;
		case 3:
			TitleItemsControl = (ItemsControl)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
