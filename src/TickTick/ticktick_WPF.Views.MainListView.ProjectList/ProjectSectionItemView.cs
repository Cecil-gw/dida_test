using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using TickTickUtils;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Project;

namespace ticktick_WPF.Views.MainListView.ProjectList;

public class ProjectSectionItemView : Grid
{
	private Border _messageText;

	private bool _popupShow;

	private bool _mouseDown;

	private Point _mouseDownPosition;

	public ProjectSectionItemView()
	{
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(22.0)
		});
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		base.ColumnDefinitions.Add(new ColumnDefinition());
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		base.Cursor = Cursors.Hand;
		base.Background = Brushes.Transparent;
		SetResourceReference(FrameworkElement.HeightProperty, "Height30");
		InitTitle();
		base.MouseEnter += OnMouseEnter;
		base.MouseLeave += OnMouseLeave;
		base.Loaded += OnLoaded;
		base.Unloaded += OnUnLoaded;
		base.DataContextChanged += OnDataBind;
		base.PreviewMouseLeftButtonUp += OnItemClick;
		base.PreviewMouseLeftButtonDown += OnItemMouseDown;
	}

	private void OnItemMouseDown(object sender, MouseButtonEventArgs e)
	{
		StackPanel rightOptions = GetRightOptions();
		if (rightOptions != null && rightOptions.IsMouseOver)
		{
			return;
		}
		Border messageText = _messageText;
		if (messageText == null || !messageText.IsMouseOver)
		{
			_mouseDown = true;
			ProjectListView projectListView = Utils.FindParent<ProjectListView>(this);
			if (projectListView == null || !projectListView.InPopup)
			{
				_mouseDownPosition = e.GetPosition(this);
				base.MouseMove += OnMouseMove;
			}
		}
	}

	private void OnMouseMove(object sender, MouseEventArgs e)
	{
		if (e.LeftButton != MouseButtonState.Pressed)
		{
			base.MouseMove -= OnMouseMove;
		}
		else if ((e.GetPosition(this) - _mouseDownPosition).Length > 4.0)
		{
			base.MouseMove -= OnMouseMove;
			_mouseDown = false;
			Utils.FindParent<ProjectListView>(this)?.OnItemStartDrag(base.DataContext as PtfAllViewModel, _mouseDownPosition);
		}
	}

	private void OnUnLoaded(object sender, RoutedEventArgs e)
	{
		PropertyChangedEventManager.RemoveHandler(ProjectMenuItemHoverModel.Model, OnHoverPropertyChanged, "HoverType");
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		PropertyChangedEventManager.AddHandler(ProjectMenuItemHoverModel.Model, OnHoverPropertyChanged, "HoverType");
	}

	private void InitTitle()
	{
		TextBlock textBlock = new TextBlock
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Center,
			IsHitTestVisible = false,
			TextTrimming = TextTrimming.CharacterEllipsis
		};
		textBlock.SetBinding(TextBlock.TextProperty, "Title");
		textBlock.SetResourceReference(TextBlock.FontSizeProperty, "Font12");
		textBlock.SetResourceReference(TextBlock.FontWeightProperty, "BoldToMedium");
		textBlock.SetResourceReference(TextBlock.ForegroundProperty, "ProjectSectionColor");
		textBlock.SetValue(Panel.ZIndexProperty, 10);
		textBlock.SetValue(Grid.ColumnProperty, 1);
		base.Children.Add(textBlock);
	}

	private Border GetFoldBorder()
	{
		foreach (object child in base.Children)
		{
			if (child is Border { Name: "FoldBorder" } border)
			{
				return border;
			}
		}
		return null;
	}

	private void ShowOrHideFoldBorder(bool show, bool open, bool showAdd)
	{
		ProjectListView projectListView = Utils.FindParent<ProjectListView>(this);
		if (projectListView != null && projectListView.InPopup)
		{
			showAdd = false;
		}
		Border foldBorder = GetFoldBorder();
		if (!show)
		{
			if (foldBorder != null)
			{
				base.Children.Remove(foldBorder);
			}
			return;
		}
		if (foldBorder == null)
		{
			foldBorder = new Border
			{
				Name = "FoldBorder",
				Margin = new Thickness(8.0, 0.0, 8.0, 0.0),
				CornerRadius = new CornerRadius(4.0)
			};
			foldBorder.SetResourceReference(Panel.BackgroundProperty, "ProjectHoverBackGround");
			foldBorder.SetValue(Panel.ZIndexProperty, 0);
			foldBorder.SetValue(Grid.ColumnSpanProperty, 4);
			Border border = new Border
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new Thickness(2.0, 0.0, 0.0, 0.0)
			};
			border.SetResourceReference(FrameworkElement.StyleProperty, "SmoothHoverBorderStyle80_100");
			Path path = UiUtils.CreatePath("Ic.Common.DownArrow", "ProjectSectionColor", "Path01", -1.0, geoKeyIsGeo: true);
			path.Width = 12.0;
			path.Height = 12.0;
			path.RenderTransformOrigin = new Point(0.5, 0.5);
			RotateTransform renderTransform = new RotateTransform
			{
				Angle = ((!open) ? (-90) : 0)
			};
			path.RenderTransform = renderTransform;
			foldBorder.Child = border;
			border.Child = path;
			base.Children.Add(foldBorder);
		}
		ShowOrHideAddBorder(showAdd);
	}

	private StackPanel GetRightOptions()
	{
		foreach (object child in base.Children)
		{
			if (child is StackPanel { Name: "RightPanel" } stackPanel)
			{
				return stackPanel;
			}
		}
		return null;
	}

	private void ShowOrHideAddBorder(bool show)
	{
		PtfAllViewModel ptfAllViewModel = base.DataContext as PtfAllViewModel;
		if (ptfAllViewModel == null)
		{
			show = false;
		}
		else if (ptfAllViewModel.IsProject && LocalSettings.Settings.ExtraSettings.ShowProjectTimes > 0)
		{
			show = true;
		}
		ProjectListView projectListView = Utils.FindParent<ProjectListView>(this);
		if (projectListView != null && projectListView.InPopup)
		{
			show = false;
		}
		StackPanel rightOptions = GetRightOptions();
		if (!show)
		{
			if (rightOptions != null)
			{
				base.Children.Remove(rightOptions);
			}
		}
		else if (rightOptions == null)
		{
			rightOptions = new StackPanel
			{
				Name = "RightPanel",
				Margin = new Thickness(0.0, 0.0, 10.0, 0.0),
				HorizontalAlignment = HorizontalAlignment.Right,
				Orientation = Orientation.Horizontal
			};
			rightOptions.SetValue(Panel.ZIndexProperty, 10);
			if (ptfAllViewModel.IsFilter || ptfAllViewModel.IsTag)
			{
				Button button = new Button
				{
					Width = 22.0,
					Height = 22.0
				};
				button.Click += OnMoreClick;
				button.SetResourceReference(FrameworkElement.StyleProperty, "OpacityHoverButtonStyle");
				Path path = UiUtils.CreatePath("Ic.TaskList.More", "ProjectMenuColorOpacity100_80", "Path01", -1.0, geoKeyIsGeo: true);
				path.Width = 16.0;
				path.Height = 16.0;
				button.Content = path;
				rightOptions.Children.Add(button);
			}
			Button button2 = new Button
			{
				Width = 22.0,
				Height = 22.0
			};
			button2.Click += OnAddClick;
			button2.SetResourceReference(FrameworkElement.StyleProperty, "OpacityHoverButtonStyle");
			Path path2 = UiUtils.CreatePath("Ic.ProjectList.Add", "ProjectMenuColorOpacity100_80", "Path01", -1.0, geoKeyIsGeo: true);
			path2.Width = 16.0;
			path2.Height = 16.0;
			button2.Content = path2;
			rightOptions.Children.Add(button2);
			rightOptions.SetValue(Grid.ColumnProperty, 3);
			base.Children.Add(rightOptions);
		}
	}

	private async void OnMoreClick(object sender, RoutedEventArgs e)
	{
		e.Handled = true;
		Mouse.Capture(null);
		object dataContext = base.DataContext;
		if (dataContext is PtfAllViewModel model && (model.IsTag || model.IsFilter))
		{
			List<ContextAction> list = (await model.GetContextActions())?.ToList();
			if (list != null)
			{
				int num = (model.IsTag ? LocalSettings.Settings.SmartListTag : LocalSettings.Settings.ShowCustomSmartList);
				List<CustomMenuItemViewModel> list2 = new List<CustomMenuItemViewModel>();
				foreach (ContextAction item in list)
				{
					CustomMenuItemViewModel customMenuItemViewModel = new CustomMenuItemViewModel((object)item.ActionKey, item.Title, (Geometry)null, "");
					customMenuItemViewModel.Selected = (item.ActionKey == ContextActionKey.Show && num == 0) || (item.ActionKey == ContextActionKey.Hide && num == 1) || (item.ActionKey == ContextActionKey.ShowIfNotEmpty && num == 2);
					list2.Add(customMenuItemViewModel);
				}
				EscPopup escPopup = new EscPopup
				{
					StaysOpen = false,
					Placement = PlacementMode.Mouse,
					PlacementTarget = this,
					HorizontalOffset = -5.0,
					VerticalOffset = -5.0
				};
				escPopup.Closed += delegate
				{
					_popupShow = false;
					if (!base.IsMouseOver)
					{
						ProjectMenuItemHoverModel.Model.HoverType = PtfType.Null;
						OnMouseLeave();
					}
				};
				CustomMenuList customMenuList = new CustomMenuList(list2, escPopup);
				customMenuList.Operated += OnActionSelected;
				_popupShow = true;
				customMenuList.Show();
				OnMouseEnter();
			}
		}
		ShowOrHideAddBorder(show: true);
	}

	private void OnActionSelected(object sender, object e)
	{
		if (e is ContextActionKey contextActionKey && base.DataContext is PtfAllViewModel model)
		{
			switch (contextActionKey)
			{
			case ContextActionKey.Show:
				Utils.FindParent<ProjectListView>(this)?.ShowOrHidePtf(model, 0);
				break;
			case ContextActionKey.Hide:
				Utils.FindParent<ProjectListView>(this)?.ShowOrHidePtf(model, 1);
				break;
			case ContextActionKey.ShowIfNotEmpty:
				Utils.FindParent<ProjectListView>(this)?.ShowOrHidePtf(model, 2);
				break;
			}
		}
	}

	private void OnItemClick(object sender, MouseButtonEventArgs e)
	{
		if (!_mouseDown)
		{
			return;
		}
		_mouseDown = false;
		if (!(base.DataContext is ProjectItemViewModel projectItemViewModel))
		{
			return;
		}
		Utils.FindParent<ProjectListView>(this)?.OnPtfAllClick(projectItemViewModel);
		if (projectItemViewModel is TeamGroupViewModel)
		{
			if (projectItemViewModel.Id != "c1a7e08345e444dea187e21a692f0d7a")
			{
				TeamDao.SaveOpenStatus(projectItemViewModel.Id, projectItemViewModel.Open);
			}
			else
			{
				LocalSettings.Settings.ExpandPersonalSection = projectItemViewModel.Open;
			}
		}
	}

	private void OnAddClick(object sender, RoutedEventArgs e)
	{
		e.Handled = true;
		Mouse.Capture(null);
		if (base.DataContext == null || !(base.DataContext is PtfAllViewModel ptfAllViewModel))
		{
			return;
		}
		switch (ptfAllViewModel.Type)
		{
		case PtfType.Project:
			if (LocalSettings.Settings.ExtraSettings.ShowProjectTimes > 0)
			{
				LocalSettings.Settings.ExtraSettings.ShowProjectTimes--;
			}
			Utils.FindParent<ProjectListView>(this)?.OnAddProjectClick(string.Empty, ptfAllViewModel.TeamId);
			break;
		case PtfType.Tag:
			Utils.FindParent<ProjectListView>(this)?.OnAddTagClick();
			break;
		case PtfType.Filter:
			Utils.FindParent<ProjectListView>(this)?.OnAddFilterClick();
			break;
		case PtfType.Subscribe:
			Mouse.Capture(null);
			new AddSubscribeDialog(this).Show();
			break;
		}
	}

	private void OnMouseEnter(object sender, MouseEventArgs e)
	{
		OnMouseEnter();
	}

	private void OnMouseEnter()
	{
		if (base.DataContext is PtfAllViewModel ptfAllViewModel)
		{
			ShowOrHideFoldBorder(show: true, ptfAllViewModel.Open, !ptfAllViewModel.InSubSection);
			ProjectMenuItemHoverModel.Model.HoverType = ptfAllViewModel.Type;
		}
	}

	private void OnMouseLeave(object sender, MouseEventArgs e)
	{
		OnMouseLeave();
	}

	private void OnMouseLeave()
	{
		if (base.DataContext is ProjectItemViewModel projectItemViewModel && !_popupShow)
		{
			ShowOrHideFoldBorder(show: false, projectItemViewModel.Open, showAdd: false);
		}
	}

	private void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		ProjectItemViewModel projectItemViewModel = e.OldValue as ProjectItemViewModel;
		PtfAllViewModel ptfAllViewModel = e.NewValue as PtfAllViewModel;
		if (projectItemViewModel != null)
		{
			PropertyChangedEventManager.RemoveHandler(projectItemViewModel, OnOpenChanged, "Open");
			PropertyChangedEventManager.RemoveHandler(projectItemViewModel, OnDragSelectChanged, "DragSelected");
		}
		if (ptfAllViewModel != null)
		{
			base.Opacity = ((!ptfAllViewModel.DragSelected) ? 1 : 0);
			PropertyChangedEventManager.AddHandler(ptfAllViewModel, OnOpenChanged, "Open");
			PropertyChangedEventManager.AddHandler(ptfAllViewModel, OnDragSelectChanged, "DragSelected");
			SetNotifyMessage(ptfAllViewModel);
			base.Margin = new Thickness(ptfAllViewModel.InSubSection ? 18 : 0, ptfAllViewModel.InSubSection ? 4 : 8, 0.0, 0.0);
			if (ptfAllViewModel.IsProject && LocalSettings.Settings.ExtraSettings.ShowProjectTimes > 0)
			{
				ShowOrHideAddBorder(show: true);
			}
		}
	}

	private void SetNotifyMessage(ProjectItemViewModel model)
	{
		if (!string.IsNullOrEmpty(model.NotifyMessage))
		{
			if (_messageText == null)
			{
				_messageText = new Border
				{
					HorizontalAlignment = HorizontalAlignment.Left,
					Height = 16.0,
					CornerRadius = new CornerRadius(4.0),
					Margin = new Thickness(6.0, 1.0, 0.0, 0.0)
				};
				_messageText.SetValue(Grid.ColumnProperty, 2);
				_messageText.SetResourceReference(Panel.BackgroundProperty, "BaseColorOpacity10");
				base.Children.Add(_messageText);
				TextBlock textBlock = new TextBlock
				{
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Center,
					IsHitTestVisible = false,
					FontSize = 10.0,
					TextTrimming = TextTrimming.CharacterEllipsis,
					Margin = new Thickness(4.0, 0.0, 4.0, 0.0)
				};
				textBlock.SetBinding(TextBlock.TextProperty, "NotifyMessage");
				textBlock.SetResourceReference(TextBlock.ForegroundProperty, "ProjectSectionColor");
				_messageText.SetValue(Panel.ZIndexProperty, 100);
				_messageText.Child = textBlock;
				_messageText.PreviewMouseLeftButtonUp += OnMessageMouseUp;
			}
			_messageText.Visibility = Visibility.Visible;
		}
		else if (_messageText != null)
		{
			_messageText.Visibility = Visibility.Collapsed;
		}
	}

	private void OnMessageMouseUp(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		if (!UserDao.IsPro())
		{
			ProChecker.CheckPro((CacheManager.GetProjectsWithoutInbox().Count((ProjectModel p) => !p.delete_status) >= 9) ? ProType.MoreLists : ProType.MoreListsUnlimited);
		}
	}

	private void OnDragSelectChanged(object sender, PropertyChangedEventArgs e)
	{
		if (base.DataContext is ProjectItemViewModel projectItemViewModel)
		{
			base.Opacity = ((!projectItemViewModel.DragSelected) ? 1 : 0);
		}
	}

	private void OnOpenChanged(object sender, PropertyChangedEventArgs e)
	{
		Border foldBorder = GetFoldBorder();
		if (foldBorder != null && foldBorder.Child is Border { Child: Path { RenderTransform: RotateTransform renderTransform } } && base.DataContext is ProjectItemViewModel projectItemViewModel)
		{
			renderTransform.BeginAnimation(RotateTransform.AngleProperty, AnimationUtils.GetDoubleAnimation(null, (!projectItemViewModel.Open) ? (-90) : 0, 120));
		}
	}

	private void OnHoverPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (base.DataContext is PtfAllViewModel { InSubSection: false } ptfAllViewModel)
		{
			ShowOrHideAddBorder(ProjectMenuItemHoverModel.Model.HoverType == ptfAllViewModel.Type);
		}
	}
}
