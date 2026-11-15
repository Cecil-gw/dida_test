using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Emoji.Wpf;
using ticktick_WPF.Resource;
using ticktick_WPF.Service.SortOrder;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.MainListView.ProjectList;

public class ProjectPinItemView : Border
{
	private bool _mouseDown;

	public bool InPopup { get; set; }

	public ProjectPinItemView()
	{
		base.Height = 56.0;
		SetBinding(FrameworkElement.WidthProperty, "ItemWidth");
		base.Background = Brushes.Transparent;
		base.CornerRadius = new CornerRadius(6.0);
		base.Margin = new Thickness(1.0, 2.0, 1.0, 2.0);
		base.Cursor = Cursors.Hand;
		base.MouseEnter += OnMouseEnter;
		base.MouseLeave += OnMouseLeave;
		base.DataContextChanged += OnDataBind;
		base.MouseLeftButtonDown += OnMouseDown;
		base.MouseLeftButtonUp += OnMouseUp;
		base.MouseRightButtonUp += OnItemRightClick;
		base.Unloaded += OnUnload;
	}

	private void OnItemRightClick(object sender, MouseButtonEventArgs e)
	{
		ProjectListView projectListView = Utils.FindParent<ProjectListView>(this);
		if ((projectListView != null && projectListView.InPopup) || !(sender is Border { DataContext: var dataContext }))
		{
			return;
		}
		ProjectPinItemViewModel model = dataContext as ProjectPinItemViewModel;
		if (model != null)
		{
			CustomMenuList customMenuList = new CustomMenuList(new List<CustomMenuItemViewModel>
			{
				new CustomMenuItemViewModel((object)"unpin", Utils.GetString("Unpin"), (DrawingImage)null, "")
				{
					ImageWidth = 0.0
				}
			}, new EscPopup
			{
				PlacementTarget = this,
				Placement = PlacementMode.Mouse,
				StaysOpen = false
			});
			customMenuList.Operated += async delegate
			{
				await ProjectPinSortOrderService.Delete(model.SyncEntityId, model.Type);
				Utils.FindParent<ProjectPinView>(this)?.OnItemUnpin(model);
			};
			customMenuList.Show();
		}
	}

	private void OnMouseUp(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		if (_mouseDown)
		{
			_mouseDown = false;
			if (base.DataContext is ProjectPinItemViewModel model)
			{
				Utils.FindParent<ProjectPinView>(this)?.OnItemSelect(model);
			}
		}
	}

	private void OnMouseDown(object sender, MouseButtonEventArgs e)
	{
		_mouseDown = true;
		if (base.DataContext is ProjectPinItemViewModel model)
		{
			Utils.FindParent<ProjectPinView>(this)?.OnItemMouseDown(model);
		}
	}

	private void OnUnload(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is ProjectPinItemViewModel source)
		{
			PropertyChangedEventManager.RemoveHandler(source, OnModelPropertyChanged, "");
		}
	}

	private void OnMouseLeave(object sender, MouseEventArgs e)
	{
		SetBackGround();
	}

	private void OnMouseEnter(object sender, MouseEventArgs e)
	{
		SetBackGround();
	}

	private void SetBackGround()
	{
		if (base.DataContext is ProjectPinItemViewModel projectPinItemViewModel)
		{
			if (projectPinItemViewModel.IsMore)
			{
				return;
			}
			if (projectPinItemViewModel.Selected)
			{
				if (LocalSettings.Settings.ExtraSettings.InterfaceStyle == InterfaceStyle.Card)
				{
					SetResourceReference(Border.BackgroundProperty, "ProjectPinSelectedBackground");
				}
				else
				{
					SetResourceReference(Border.BackgroundProperty, "ProjectSelectedBackground");
				}
				return;
			}
		}
		if (base.IsMouseOver || InPopup)
		{
			SetResourceReference(Border.BackgroundProperty, "ProjectHoverBackGround");
		}
		else
		{
			base.Background = Brushes.Transparent;
		}
	}

	private void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		ProjectPinItemViewModel projectPinItemViewModel = e.OldValue as ProjectPinItemViewModel;
		if (projectPinItemViewModel != null)
		{
			PropertyChangedEventManager.RemoveHandler(projectPinItemViewModel, OnModelPropertyChanged, "");
		}
		if (e.NewValue is ProjectPinItemViewModel projectPinItemViewModel2)
		{
			PropertyChangedEventManager.AddHandler(projectPinItemViewModel2, OnModelPropertyChanged, "");
			SetBackGround();
			if (projectPinItemViewModel2.IsMore)
			{
				base.ToolTip = null;
				SetMoreContent();
			}
			else if (projectPinItemViewModel == null || projectPinItemViewModel.IsMore || projectPinItemViewModel.Icon == null != (projectPinItemViewModel2.Icon == null))
			{
				ToolTip toolTip = new ToolTip();
				toolTip.SetResourceReference(FrameworkElement.StyleProperty, "PinProjectTooltipStyle");
				toolTip.SetBinding(ContentControl.ContentProperty, "Title");
				base.ToolTip = toolTip;
				SetItemContent(projectPinItemViewModel2);
			}
			base.Opacity = ((InPopup || !projectPinItemViewModel2.Dragging) ? 1 : 0);
		}
	}

	private void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == "Selected")
		{
			SetBackGround();
		}
		if (!(e.PropertyName == "Dragging"))
		{
			return;
		}
		int num;
		if (!InPopup)
		{
			ProjectPinItemViewModel obj = base.DataContext as ProjectPinItemViewModel;
			if (obj != null && obj.Dragging)
			{
				num = 0;
				goto IL_0050;
			}
		}
		num = 1;
		goto IL_0050;
		IL_0050:
		base.Opacity = num;
	}

	private void SetItemContent(ProjectPinItemViewModel model)
	{
		Grid grid = new Grid();
		Border border = new Border
		{
			Height = 28.0,
			Width = 28.0,
			CornerRadius = new CornerRadius(6.0),
			VerticalAlignment = VerticalAlignment.Top,
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 4.0, 0.0, 0.0)
		};
		border.SetBinding(Border.BackgroundProperty, "IconBackground");
		grid.Children.Add(border);
		if (model.Icon != null)
		{
			Path path = new Path
			{
				VerticalAlignment = VerticalAlignment.Top
			};
			path.SetResourceReference(FrameworkElement.StyleProperty, "Path01");
			path.Width = 18.0;
			path.Height = 18.0;
			path.SetBinding(Path.DataProperty, "Icon");
			path.SetBinding(Shape.FillProperty, "IconColor");
			path.Margin = new Thickness(0.0, 9.0, 0.0, 0.0);
			grid.Children.Add(path);
		}
		else
		{
			EmjTextBlock emjTextBlock = new EmjTextBlock
			{
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Top,
				Margin = new Thickness(0.0, 9.0, 0.0, 0.0)
			};
			emjTextBlock.SetResourceReference(FrameworkElement.StyleProperty, "Body01");
			emjTextBlock.SetBinding(EmjTextBlock.TextProperty, "Emoji");
			grid.Children.Add(emjTextBlock);
		}
		EmjTextBlock emjTextBlock2 = new EmjTextBlock
		{
			Name = "PTitle",
			TextTrimming = TextTrimming.WordEllipsis,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Bottom,
			Margin = new Thickness(0.0, 0.0, 0.0, 6.0),
			FontSize = 10.0
		};
		emjTextBlock2.SetResourceReference(FrameworkElement.StyleProperty, "Body01");
		emjTextBlock2.SetResourceReference(TextBlock.ForegroundProperty, "ProjectMenuColorOpacity80");
		emjTextBlock2.SetBinding(EmjTextBlock.TextProperty, "DisplayTitle");
		grid.Children.Add(emjTextBlock2);
		Child = grid;
	}

	private void SetMoreContent()
	{
		Grid grid = new Grid();
		Border border = new Border
		{
			Height = 28.0,
			Width = 28.0,
			VerticalAlignment = VerticalAlignment.Top,
			Background = Brushes.Transparent,
			Margin = new Thickness(0.0, 4.0, 0.0, 0.0)
		};
		border.SetResourceReference(FrameworkElement.StyleProperty, "SmoothHoverBorderStyle60_100");
		Rectangle rectangle = new Rectangle
		{
			Height = 28.0,
			Width = 28.0,
			RadiusX = 6.0,
			RadiusY = 6.0,
			StrokeThickness = 1.0
		};
		Grid grid2 = new Grid
		{
			Width = 28.0,
			Height = 28.0
		};
		grid2.Children.Add(rectangle);
		rectangle.SetResourceReference(Shape.StrokeProperty, "ProjectMenuColorOpacity40");
		Path path = new Path
		{
			Width = 12.0,
			Height = 12.0,
			Data = Utils.GetGeometry("Ic.Common.DownArrow")
		};
		path.SetResourceReference(Shape.FillProperty, "ProjectMenuColorOpacity60");
		path.SetResourceReference(FrameworkElement.StyleProperty, "Path01");
		grid2.Children.Add(path);
		border.Child = grid2;
		TextBlock textBlock = new TextBlock
		{
			TextTrimming = TextTrimming.WordEllipsis,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Bottom,
			Margin = new Thickness(0.0, 0.0, 0.0, 6.0),
			FontSize = 10.0,
			Text = Utils.GetString("Expand")
		};
		textBlock.SetResourceReference(FrameworkElement.StyleProperty, "Body01");
		textBlock.SetResourceReference(TextBlock.ForegroundProperty, "ProjectMenuColorOpacity80");
		grid.Children.Add(border);
		grid.Children.Add(textBlock);
		Child = grid;
	}
}
