using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using TickTickUtils.Lang;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Pomo.FocusStatistics;

public class FocusStatisticsTitleView : DockPanel
{
	private EscPopup MorePopup { get; set; }

	public FocusStatisticsTitleView()
		: this(showBottomLine: false)
	{
	}

	public FocusStatisticsTitleView(bool showBottomLine)
	{
		if (showBottomLine)
		{
			Border border = new Border
			{
				Height = 7.0,
				BorderThickness = new Thickness(0.0, 0.0, 0.0, 1.0)
			};
			border.SetResourceReference(Border.BorderBrushProperty, "BaseColorOpacity5");
			base.Children.Add(border);
			border.SetValue(DockPanel.DockProperty, Dock.Bottom);
		}
		StackPanel buttonPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Margin = new Thickness(2.0, 0.0, 20.0, 0.0)
		};
		buttonPanel.SetValue(DockPanel.DockProperty, Dock.Right);
		base.Children.Add(buttonPanel);
		HoverIconButton addButton = new HoverIconButton();
		addButton.MouseLeftButtonUp += OnAddClick;
		buttonPanel.Children.Add(addButton);
		HoverIconButton moreButton = new HoverIconButton();
		moreButton.MouseLeftButtonUp += OnMoreClick;
		moreButton.Margin = new Thickness(2.0, 0.0, 0.0, 0.0);
		buttonPanel.Children.Add(moreButton);
		TextBlock textBlock = new TextBlock
		{
			FontSize = 18.0,
			Margin = new Thickness(22.0, 0.0, 0.0, 0.0)
		};
		textBlock.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100");
		textBlock.SetValue(DockPanel.DockProperty, Dock.Left);
		textBlock.SetBinding(TextBlock.TextProperty, "Title");
		base.Children.Add(textBlock);
		base.DataContextChanged += delegate(object o, DependencyPropertyChangedEventArgs e)
		{
			if (e.NewValue is FocusStatisticsTitleItemViewModel focusStatisticsTitleItemViewModel)
			{
				buttonPanel.Visibility = ((!focusStatisticsTitleItemViewModel.ShowButton) ? Visibility.Collapsed : Visibility.Visible);
				addButton.SetResourceReference(HoverIconButton.IconDataProperty, "Ic.ProjectList.Add");
				addButton.IsImage = false;
				moreButton.SetResourceReference(HoverIconButton.IconDataProperty, "Ic.ProjectList.More");
				moreButton.IsImage = false;
				moreButton.SetBinding(UIElement.VisibilityProperty, "MoreButtonVisibility");
			}
		};
	}

	private void OnAddClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is FocusStatisticsTitleItemViewModel)
		{
			AddFocusRecordWindow addFocusRecordWindow = new AddFocusRecordWindow(LocalSettings.Settings.PomoLocalSetting.AddRecordType);
			addFocusRecordWindow.Owner = Window.GetWindow(this);
			addFocusRecordWindow.ShowDialog();
		}
	}

	private void OnMoreClick(object sender, MouseButtonEventArgs e)
	{
		if (MorePopup != null)
		{
			MorePopup.IsOpen = false;
			return;
		}
		MorePopup = new EscPopup
		{
			PlacementTarget = (sender as UIElement),
			StaysOpen = false,
			Placement = PlacementMode.Bottom
		};
		MorePopup.Closed += MorePopup_Closed;
		Utils.FindParent<FocusStatisticsPanel>(this)?.Children.Add(MorePopup);
		CustomMenuList customMenuList = new CustomMenuList(new List<CustomMenuItemViewModel>
		{
			new CustomMenuItemViewModel("batchManage", Utils.GetString("BatchManagement"), Utils.GetGeometry("Ic.TaskOM.BatchEdit")),
			new CustomMenuItemViewModel("deleteAll", Utils.GetString("DeleteAll"), Utils.GetGeometry("Ic.ProjectList.DeleteForever"))
		}, MorePopup);
		customMenuList.Operated += async delegate(object o, object val)
		{
			switch (val as string)
			{
			case "batchManage":
			{
				BatchFocusRecWindow batchFocusRecWindow = new BatchFocusRecWindow();
				batchFocusRecWindow.Owner = Window.GetWindow(this);
				batchFocusRecWindow.ShowDialog();
				break;
			}
			case "deleteAll":
				if (new FocusRecDeleteAllWindow
				{
					Owner = Window.GetWindow(this)
				}.ShowDialog() == true)
				{
					UtilLog.Info("Delete all focus record result:" + await PomoService.DeleteAllPomoFocusRec());
				}
				break;
			}
		};
		customMenuList.Show();
	}

	private async void MorePopup_Closed(object sender, EventArgs e)
	{
		await Task.Delay(100);
		MorePopup.Closed -= MorePopup_Closed;
		Utils.FindParent<FocusStatisticsPanel>(this)?.Children.Remove(MorePopup);
		MorePopup = null;
	}
}
