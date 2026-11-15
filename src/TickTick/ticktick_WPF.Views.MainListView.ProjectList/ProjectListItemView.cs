using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Emoji.Wpf;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Config;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.MainListView.ProjectList;

public class ProjectListItemView : Grid
{
	private bool _canSelect;

	private readonly ColumnDefinition _secondColumn;

	private readonly ContentControl _backBorder = new ContentControl
	{
		Margin = new Thickness(8.0, 0.0, 8.0, 0.0)
	};

	private readonly ContentControl _iconContent = new ContentControl
	{
		HorizontalAlignment = HorizontalAlignment.Left,
		Width = 20.0,
		Height = 22.0,
		VerticalAlignment = VerticalAlignment.Center
	};

	private readonly EmjTextBlock _title = new EmjTextBlock
	{
		Margin = new Thickness(28.0, 0.0, 0.0, 0.0),
		TextTrimming = TextTrimming.CharacterEllipsis,
		TextWrapping = TextWrapping.Wrap,
		ClipToBounds = true
	};

	private readonly ContentControl _rightControl = new ContentControl
	{
		HorizontalAlignment = HorizontalAlignment.Right,
		Margin = new Thickness(0.0, 0.0, 16.0, 0.0)
	};

	private Grid _colorIndicator;

	private Point _mouseDownPosition;

	public ProjectListItemView()
	{
		base.Cursor = Cursors.Hand;
		base.Background = Brushes.Transparent;
		SetResourceReference(FrameworkElement.HeightProperty, "Height36");
		base.Margin = new Thickness(0.0, 1.0, 0.0, 1.0);
		base.MouseEnter += OnMouseEnter;
		base.MouseLeave += OnMouseLeave;
		base.MouseLeftButtonUp += OnProjectClick;
		base.MouseRightButtonUp += OnRightClick;
		base.PreviewMouseLeftButtonDown += OnPreviewMouseDown;
		base.DataContextChanged += OnDataBind;
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(20.0)
		});
		_secondColumn = new ColumnDefinition();
		base.ColumnDefinitions.Add(_secondColumn);
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		InitElements();
	}

	private void InitElements()
	{
		InitBackBorder();
		InitIcon();
		InitTitle();
		InitColorAndCount();
	}

	private void InitColorAndCount()
	{
		base.Children.Add(_rightControl);
		_rightControl.SetValue(Grid.ColumnProperty, 3);
		_rightControl.SetResourceReference(FrameworkElement.MinWidthProperty, "Height24");
	}

	private void InitTitle()
	{
		base.Children.Add(_title);
		_title.SetValue(Grid.ColumnProperty, 1);
		_title.SetResourceReference(TextBlock.FontSizeProperty, "Font14");
		_title.SetResourceReference(TextBlock.ForegroundProperty, "ProjectMenuColorOpacity100_80");
		_title.SetResourceReference(FrameworkElement.StyleProperty, "Body01");
		_title.SetResourceReference(FrameworkElement.MaxHeightProperty, "Double20Add2");
		_title.SetBinding(EmjTextBlock.TextProperty, "TitleText");
		_title.SetBinding(FrameworkElement.ToolTipProperty, "Title");
		_title.SetResourceReference(UIElement.EffectProperty, "ProjectTitleEffect");
		_title.SetValue(ToolTipService.InitialShowDelayProperty, 300);
		_title.ToolTipOpening += OnTitleToolTipOpening;
	}

	private void InitIcon()
	{
		base.Children.Add(_iconContent);
		_iconContent.SetValue(Grid.ColumnProperty, 1);
	}

	private Border GetFoldBorder()
	{
		foreach (object child in base.Children)
		{
			if (child is Border border && (int)border.GetValue(Grid.ColumnProperty) == 0 && (int)border.GetValue(Grid.ColumnSpanProperty) == 1)
			{
				return border;
			}
		}
		return null;
	}

	private void ShowOrHideFoldIcon(bool show, bool open)
	{
		Border foldBorder = GetFoldBorder();
		if (!show)
		{
			if (foldBorder != null)
			{
				base.Children.Remove(foldBorder);
			}
		}
		else if (foldBorder == null)
		{
			foldBorder = new Border
			{
				Width = 20.0,
				Margin = new Thickness(6.0, 0.0, 0.0, 0.0)
			};
			foldBorder.SetResourceReference(FrameworkElement.StyleProperty, "SmoothHoverBorderStyle60_100");
			foldBorder.MouseLeftButtonUp += OnCollapseClick;
			Path path = UiUtils.CreatePath("Ic.TaskList.DownArrow", "ProjectMenuColorOpacity100_80", "Path01", -1.0, geoKeyIsGeo: true);
			path.Width = 12.0;
			path.Height = 12.0;
			path.RenderTransformOrigin = new Point(0.5, 0.5);
			RotateTransform renderTransform = new RotateTransform
			{
				Angle = ((!open) ? (-90) : 0)
			};
			path.RenderTransform = renderTransform;
			foldBorder.Child = path;
			base.Children.Add(foldBorder);
		}
		else if (foldBorder.Child is Path path2)
		{
			RotateTransform renderTransform2 = new RotateTransform
			{
				Angle = ((!open) ? (-90) : 0)
			};
			path2.RenderTransform = renderTransform2;
		}
	}

	private void ShowOrHideColor(bool show)
	{
		if (!show)
		{
			if (_colorIndicator != null)
			{
				base.Children.Remove(_colorIndicator);
			}
			return;
		}
		if (_colorIndicator == null)
		{
			_colorIndicator = new Grid
			{
				Width = 8.0,
				Height = 8.0
			};
			Ellipse ellipse = new Ellipse();
			ellipse.SetBinding(Shape.FillProperty, "Color");
			Style style = new Style(typeof(Ellipse));
			style.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(0.0)));
			DataTrigger dataTrigger = new DataTrigger
			{
				Binding = new Binding("ColorNeedsContrastBorder"),
				Value = true
			};
			dataTrigger.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(1.0)));
			style.Triggers.Add(dataTrigger);
			ellipse.Style = style;
			Border border = new Border
			{
				CornerRadius = new CornerRadius(4.0),
				BorderThickness = new Thickness(1.0)
			};
			border.SetResourceReference(Border.BorderBrushProperty, "BaseColorOpacity10");
			Style style2 = new Style(typeof(Border));
			style2.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Collapsed));
			DataTrigger dataTrigger2 = new DataTrigger
			{
				Binding = new Binding("ColorNeedsContrastBorder"),
				Value = true
			};
			dataTrigger2.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Visible));
			style2.Triggers.Add(dataTrigger2);
			border.Style = style2;
			_colorIndicator.Children.Add(ellipse);
			_colorIndicator.Children.Add(border);
			_colorIndicator.SetValue(Grid.ColumnProperty, 2);
		}
		if (!base.Children.Contains(_colorIndicator))
		{
			base.Children.Add(_colorIndicator);
		}
	}

	private void InitBackBorder()
	{
		base.Children.Add(_backBorder);
		_backBorder.SetValue(Grid.ColumnProperty, 0);
		_backBorder.SetValue(Grid.ColumnSpanProperty, 4);
	}

	private void SetBackground()
	{
		if (base.DataContext is ProjectItemViewModel projectItemViewModel)
		{
			CornerRadius cornerRadius = new CornerRadius(10.0);
			Border border = null;
			if (projectItemViewModel.DropSelected || projectItemViewModel.IsTabSelected)
			{
				border = new Border
				{
					CornerRadius = cornerRadius,
					BorderThickness = new Thickness(1.0),
					Background = (projectItemViewModel.DropSelected ? ThemeUtil.GetColorInString("#19FFFFFF") : Brushes.Transparent)
				};
				border.SetResourceReference(Border.BorderBrushProperty, projectItemViewModel.DropSelected ? "ProjectMenuColorOpacity40" : "PrimaryColor");
			}
			else if (projectItemViewModel.Selected)
			{
				border = new Border
				{
					CornerRadius = cornerRadius,
					BorderThickness = new Thickness(0.0)
				};
				border.SetResourceReference(Border.BackgroundProperty, "ProjectSelectedBackground");
			}
			else
			{
				border = new Border
				{
					CornerRadius = cornerRadius
				};
				border.SetResourceReference(FrameworkElement.StyleProperty, "ProjectItemHoverBorderStyle");
			}
			_backBorder.Content = border;
		}
	}

	private void SetRightContent(ProjectItemViewModel model)
	{
		if (model?.InfoIcon != null)
		{
			if (!(_rightControl.Content is Border { Name: "InvalidPath" }))
			{
				Border border2 = new Border
				{
					Name = "InvalidPath",
					Width = 18.0,
					Height = 18.0,
					Background = Brushes.Transparent,
					Cursor = Cursors.Hand
				};
				border2.MouseLeftButtonUp += OnInvalidInfoClick;
				border2.SetBinding(FrameworkElement.ToolTipProperty, "Info");
				Path path = UiUtils.CreatePath("", "", "ProjectIconStyle");
				path.Width = 16.0;
				path.Data = model.InfoIcon;
				path.Fill = ThemeUtil.GetColorInString("#FFB000");
				border2.Child = path;
				_rightControl.Content = border2;
			}
		}
		else if (base.IsMouseOver && !(model is ClosedProjectGroupViewModel))
		{
			if (!(_rightControl.Content is Button { Name: "MoreButton" }))
			{
				Button button2 = new Button
				{
					Name = "MoreButton",
					Cursor = Cursors.Hand,
					Width = 16.0,
					HorizontalAlignment = HorizontalAlignment.Right
				};
				button2.SetResourceReference(FrameworkElement.StyleProperty, "OpacityHoverButtonStyle");
				button2.Click += OnMoreClick;
				Path path2 = UiUtils.CreatePath("Ic.TaskList.More", "ProjectMenuColorOpacity100_80", "Path01", -1.0, geoKeyIsGeo: true);
				path2.Width = 16.0;
				path2.Height = 16.0;
				button2.Content = path2;
				_rightControl.Content = button2;
			}
		}
		else if (model != null && model.Count > 0)
		{
			if (!(_rightControl.Content is TextBlock))
			{
				TextBlock textBlock = new TextBlock
				{
					HorizontalAlignment = HorizontalAlignment.Right,
					Opacity = 0.8
				};
				textBlock.SetResourceReference(FrameworkElement.StyleProperty, "Tag01");
				textBlock.SetResourceReference(TextBlock.ForegroundProperty, "ProjectMenuColorOpacity80");
				textBlock.SetBinding(TextBlock.TextProperty, "Count");
				textBlock.SetResourceReference(UIElement.EffectProperty, "ProjectTitleEffect");
				_rightControl.Content = textBlock;
			}
		}
		else
		{
			_rightControl.Content = null;
		}
	}

	private void SetIcon(ProjectItemViewModel origin, ProjectItemViewModel model)
	{
		if (origin == null || origin.ShowIcon != model.ShowIcon || (origin.ShowIcon && string.IsNullOrEmpty(origin.IconText) != string.IsNullOrEmpty(model.IconText)))
		{
			if (model.ShowIcon)
			{
				Path path = UiUtils.CreatePath("", "ProjectMenuIconColor", "Path01");
				path.Width = 20.0;
				path.Height = 20.0;
				path.HorizontalAlignment = HorizontalAlignment.Center;
				path.VerticalAlignment = VerticalAlignment.Center;
				path.SetBinding(Path.DataProperty, "Icon");
				_iconContent.Content = path;
			}
			else
			{
				EmjTextBlock emjTextBlock = new EmjTextBlock
				{
					HorizontalAlignment = HorizontalAlignment.Center,
					TextAlignment = TextAlignment.Center,
					Width = 20.0,
					FontSize = 16.0,
					Margin = new Thickness(0.0, -1.0, 0.0, 0.0)
				};
				emjTextBlock.SetResourceReference(FrameworkElement.StyleProperty, "Body01");
				emjTextBlock.SetResourceReference(TextBlock.ForegroundProperty, "ProjectMenuColorOpacity80");
				emjTextBlock.SetBinding(EmjTextBlock.TextProperty, "EmojiText");
				_iconContent.Content = emjTextBlock;
			}
		}
	}

	private void OnMoreClick(object sender, RoutedEventArgs e)
	{
		e.Handled = true;
		Mouse.Capture(null);
		ShowActionPopup();
	}

	private void OnInvalidInfoClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is BindCalendarAccountProjectViewModel bindCalendarAccountProjectViewModel)
		{
			try
			{
				SubscribeExpiredWindow.TryShowWindow(new List<BindCalendarAccountModel> { bindCalendarAccountProjectViewModel.Account }, null, null, setCheckTime: false);
			}
			catch (Exception ex)
			{
				UtilLog.Error(ex.Message);
			}
		}
		if (base.DataContext is SubscribeCalendarProjectViewModel subscribeCalendarProjectViewModel)
		{
			try
			{
				SubscribeExpiredWindow.TryShowWindow(null, new List<CalendarSubscribeProfileModel> { subscribeCalendarProjectViewModel.Profile }, null, setCheckTime: false);
			}
			catch (Exception ex2)
			{
				UtilLog.Error(ex2.Message);
			}
		}
	}

	private void OnProjectClick(object sender, MouseButtonEventArgs e)
	{
		if (!_canSelect)
		{
			return;
		}
		if (base.DataContext is ProjectItemViewModel projectItemViewModel)
		{
			if (projectItemViewModel is ClosedProjectGroupViewModel)
			{
				Utils.FindParent<ProjectListView>(this)?.CollapseItem(projectItemViewModel);
			}
			else
			{
				Utils.FindParent<ProjectListView>(this)?.OnProjectClick(projectItemViewModel, manual: true);
			}
		}
		_canSelect = false;
	}

	private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
	{
		Border foldBorder = GetFoldBorder();
		if ((foldBorder == null || !foldBorder.IsMouseOver) && !_rightControl.IsMouseOver)
		{
			_canSelect = true;
		}
		ProjectListView projectListView = Utils.FindParent<ProjectListView>(this);
		if (projectListView == null || !projectListView.InPopup)
		{
			_mouseDownPosition = e.GetPosition(this);
			base.MouseMove += OnMouseMove;
		}
	}

	private void OnMouseMove(object sender, MouseEventArgs e)
	{
		if (e.LeftButton != MouseButtonState.Pressed)
		{
			base.MouseMove -= OnMouseMove;
			return;
		}
		Point position = e.GetPosition(this);
		if ((position - _mouseDownPosition).Length > 4.0)
		{
			base.MouseMove -= OnMouseMove;
			GetParent()?.OnItemStartDrag(GetModel<ProjectItemViewModel>(), position);
		}
	}

	private void OnMouseEnter(object sender, MouseEventArgs e)
	{
		ProjectListView projectListView = Utils.FindParent<ProjectListView>(this);
		if ((projectListView == null || !projectListView.InPopup) && base.DataContext is ProjectItemViewModel projectItemViewModel)
		{
			SetRightContent(projectItemViewModel);
			PtfType ptfType = projectItemViewModel.GetPtfType();
			ProjectMenuItemHoverModel.Model.HoverType = ptfType;
		}
	}

	private void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		ProjectItemViewModel projectItemViewModel = e.OldValue as ProjectItemViewModel;
		ProjectItemViewModel projectItemViewModel2 = e.NewValue as ProjectItemViewModel;
		if (projectItemViewModel != null)
		{
			PropertyChangedEventManager.RemoveHandler(projectItemViewModel, OnModelPropertyChanged, "");
		}
		if (projectItemViewModel2 != null)
		{
			PropertyChangedEventManager.AddHandler(projectItemViewModel2, OnModelPropertyChanged, "");
			SetIcon(projectItemViewModel, projectItemViewModel2);
			ShowOrHideFoldIcon(projectItemViewModel2.IsGroupItem, projectItemViewModel2.Open);
			ShowOrHideColor(!string.IsNullOrEmpty(projectItemViewModel2.Color) && projectItemViewModel2.Color.ToLower() != "transparent");
			SetRightContent(projectItemViewModel2);
			base.Margin = new Thickness((projectItemViewModel2.InSubSection ? 18 : 0) + (projectItemViewModel2.IsSubItem ? 10 : 0), 1.0, 0.0, 1.0);
			SetBackground();
			base.Opacity = ((!projectItemViewModel2.DragSelected) ? 1 : 0);
			if (projectItemViewModel2.Count <= 0 && LocalSettings.Settings.ExtraSettings.NumDisplayType != 2)
			{
				projectItemViewModel2.EnsureCountLoaded();
			}
		}
	}

	private void OnTitleToolTipOpening(object sender, ToolTipEventArgs e)
	{
		if (base.DataContext is ProjectItemViewModel projectItemViewModel && _title.ActualWidth > 0.0)
		{
			string text = projectItemViewModel.TitleText ?? string.Empty;
			e.Handled = string.IsNullOrEmpty(text) || Utils.MeasureStringWidth(text, _title.FontSize) <= _title.ActualWidth;
		}
		else
		{
			e.Handled = true;
		}
	}

	private void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == "Count")
		{
			SetRightContent(base.DataContext as ProjectItemViewModel);
		}
		if (e.PropertyName == "Color" && base.DataContext is ProjectItemViewModel projectItemViewModel)
		{
			ShowOrHideColor(!string.IsNullOrEmpty(projectItemViewModel.Color) && !projectItemViewModel.Color.Equals("transparent", StringComparison.OrdinalIgnoreCase));
		}
		if ((e.PropertyName == "ShowIcon" || e.PropertyName == "ShowEmoji") && base.DataContext is ProjectItemViewModel model)
		{
			SetIcon(null, model);
		}
		if (e.PropertyName == "Selected" || e.PropertyName == "DropSelected")
		{
			SetBackground();
		}
		if (e.PropertyName == "Open")
		{
			BeginFoldAnimation();
		}
		if (e.PropertyName == "Level" && base.DataContext is ProjectItemViewModel projectItemViewModel2)
		{
			base.Margin = new Thickness((projectItemViewModel2.InSubSection ? 18 : 0) + (projectItemViewModel2.IsSubItem ? 10 : 0), 1.0, 0.0, 1.0);
		}
		if (e.PropertyName == "DragSelected" && base.DataContext is ProjectItemViewModel projectItemViewModel3)
		{
			base.Opacity = ((!projectItemViewModel3.DragSelected) ? 1 : 0);
		}
	}

	private void BeginFoldAnimation()
	{
		Border foldBorder = GetFoldBorder();
		if (foldBorder != null && foldBorder.Child is Path { RenderTransform: RotateTransform renderTransform } && base.DataContext is ProjectItemViewModel projectItemViewModel)
		{
			renderTransform.BeginAnimation(RotateTransform.AngleProperty, AnimationUtils.GetDoubleAnimation(null, (!projectItemViewModel.Open) ? (-90) : 0, 120));
		}
	}

	private void OnMouseLeave(object sender, MouseEventArgs e)
	{
		if (base.DataContext is ProjectItemViewModel rightContent)
		{
			SetRightContent(rightContent);
		}
	}

	private void OnCollapseClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is ProjectItemViewModel model)
		{
			Utils.FindParent<ProjectListView>(this)?.CollapseItem(model);
		}
	}

	private void OnRightClick(object sender, MouseButtonEventArgs e)
	{
		ProjectListView projectListView = Utils.FindParent<ProjectListView>(this);
		if (projectListView == null || !projectListView.InPopup)
		{
			e.Handled = true;
			Mouse.Capture(null);
			ShowActionPopup();
		}
	}

	private async Task ShowActionPopup()
	{
		if (!(base.DataContext is ProjectItemViewModel model))
		{
			return;
		}
		if (TeamDao.IsTeamExpired(model.TeamId))
		{
			Utils.Toast(Utils.GetString("TeamExpiredOperate"));
			return;
		}
		List<ContextAction> list = (await model.GetContextActions())?.ToList();
		if (list == null)
		{
			return;
		}
		List<CustomMenuItemViewModel> list2 = new List<CustomMenuItemViewModel>();
		foreach (ContextAction item2 in list)
		{
			CustomMenuItemViewModel customMenuItemViewModel = new CustomMenuItemViewModel((object)item2.ActionKey, item2.Title, (Geometry)null, "");
			switch (item2.ActionKey)
			{
			case ContextActionKey.Delete:
				customMenuItemViewModel.IsDelete = true;
				break;
			case ContextActionKey.Exit:
				if (CacheManager.GetProjectById(model.Id).OpenToTeam)
				{
					customMenuItemViewModel.IsEnable = false;
					customMenuItemViewModel.IsMessage = true;
					customMenuItemViewModel.ExtraIcon = Utils.GetGeometry("Ic.Common.Help");
					customMenuItemViewModel.ExtraIconColor = ThemeUtil.GetColor("BaseColorOpacity100");
					customMenuItemViewModel.ExtraIconTips = Utils.GetString("CannotExitOpenProject");
				}
				break;
			}
			List<ContextAction> subActions = item2.SubActions;
			if (subActions != null && subActions.Count() > 0)
			{
				customMenuItemViewModel.SubActions = new List<CustomMenuItemViewModel>();
				foreach (ContextAction subAction in item2.SubActions)
				{
					CustomMenuItemViewModel item = new CustomMenuItemViewModel((object)subAction.ActionKey, subAction.Title, (Geometry)null, "")
					{
						Selected = subAction.Selected,
						ShowSelected = true
					};
					customMenuItemViewModel.SubActions.Add(item);
				}
				customMenuItemViewModel.ShowSelected = false;
			}
			list2.Add(customMenuItemViewModel);
		}
		EscPopup popup = new EscPopup
		{
			StaysOpen = false,
			Placement = PlacementMode.Mouse,
			PlacementTarget = this,
			HorizontalOffset = -5.0,
			VerticalOffset = -5.0
		};
		CustomMenuList customMenuList = new CustomMenuList(list2, popup);
		customMenuList.Operated += OnActionSelected;
		customMenuList.Show();
	}

	private void OnActionSelected(object sender, object e)
	{
		if (!(e is ContextActionKey contextActionKey))
		{
			return;
		}
		if ((uint)(contextActionKey - 18) <= 1u)
		{
			TaskListWindow.OpenOrCloseWindow(GetModel<ProjectItemViewModel>().GetSaveIdentity(), Window.GetWindow(this));
		}
		else
		{
			if (base.DataContext == null)
			{
				return;
			}
			object dataContext = base.DataContext;
			if (!(dataContext is FilterProjectViewModel))
			{
				if (!(dataContext is TagProjectViewModel tagProjectViewModel))
				{
					if (!(dataContext is SubscribeCalendarProjectViewModel))
					{
						if (!(dataContext is BindCalendarAccountProjectViewModel))
						{
							if (!(dataContext is ticktick_WPF.ViewModels.SmartProjectViewModel smart))
							{
								if (dataContext is ticktick_WPF.ViewModels.ProjectGroupViewModel projectGroupViewModel)
								{
									HandleGroupProjectAction(projectGroupViewModel, contextActionKey);
								}
								else
								{
									HandleProjectAction(contextActionKey);
								}
							}
							else
							{
								HandleSmartProjectAction(contextActionKey, smart);
							}
						}
						else
						{
							HandleBindAccountAction(contextActionKey);
						}
					}
					else
					{
						HandleCalendarAction(contextActionKey);
					}
				}
				else
				{
					HandleTagAction(tagProjectViewModel.TagModel, contextActionKey);
				}
			}
			else
			{
				HandleFilterAction(contextActionKey);
			}
		}
	}

	private ProjectListView GetParent()
	{
		return Utils.FindParent<ProjectListView>(this);
	}

	private void HandleGroupProjectAction(ticktick_WPF.ViewModels.ProjectGroupViewModel group, ContextActionKey action)
	{
		switch (action)
		{
		case ContextActionKey.AddList:
			UserActCollectUtils.AddClickEvent("project_list_ui", "folder", "cm.add_list");
			GetParent()?.OnAddProjectClick(group.ProjectGroup.id, group.ProjectGroup.teamId);
			break;
		case ContextActionKey.Pin:
		case ContextActionKey.Unpin:
			UserActCollectUtils.AddClickEvent("project_list_ui", "folder", (action == ContextActionKey.Pin) ? "cm.pin" : "cm.unpin");
			GetParent()?.OnPinClick(group.ProjectGroup.id, 6, action == ContextActionKey.Pin);
			break;
		case ContextActionKey.Edit:
			UserActCollectUtils.AddClickEvent("project_list_ui", "folder", "cm.rename");
			GetParent()?.OnRenameGroup(group.ProjectGroup);
			break;
		case ContextActionKey.Ungroup:
			UserActCollectUtils.AddClickEvent("project_list_ui", "folder", "cm.ungroup");
			GetParent()?.OnDeleteProjectGroup(group.ProjectGroup);
			break;
		}
	}

	private void HandleSmartProjectAction(ContextActionKey action, ticktick_WPF.ViewModels.SmartProjectViewModel smart)
	{
		switch (action)
		{
		case ContextActionKey.Show:
		case ContextActionKey.Hide:
		case ContextActionKey.ShowIfNotEmpty:
			GetParent().ShowOrHideProject(GetSmartProject(), action);
			break;
		case ContextActionKey.Edit:
			GetParent().EditInboxProject();
			break;
		case ContextActionKey.Pin:
		case ContextActionKey.Unpin:
			UserActCollectUtils.AddClickEvent("project_list_ui", "tag", (action == ContextActionKey.Pin) ? "cm.pin" : "cm.unpin");
			GetParent()?.OnPinClick(smart.Project.UserEventId, 11, action == ContextActionKey.Pin);
			break;
		}
	}

	private void HandleBindAccountAction(ContextActionKey action)
	{
		BindCalendarAccountModel bindCalendar = GetBindCalendar();
		if (bindCalendar == null)
		{
			return;
		}
		switch (action)
		{
		case ContextActionKey.Edit:
			UserActCollectUtils.AddClickEvent("project_list_ui", "event", "cm.edit");
			GetParent().EditBindCalendar(bindCalendar.Id);
			break;
		case ContextActionKey.Pin:
		case ContextActionKey.Unpin:
			GetParent()?.OnPinClick(bindCalendar.Id, 9, action == ContextActionKey.Pin);
			UserActCollectUtils.AddClickEvent("project_list_ui", "event", (action == ContextActionKey.Pin) ? "cm.pin" : "cm.unpin");
			break;
		case ContextActionKey.Reauthorize:
		{
			BindCalendarAccountProjectViewModel model = GetModel<BindCalendarAccountProjectViewModel>();
			if (model.Account.IsBindAccountPassword())
			{
				BindAccountWindow bindAccountWindow = new BindAccountWindow(model.Account);
				bindAccountWindow.Owner = Utils.GetParentWindow(this);
				bindAccountWindow.ShowDialog();
			}
			else if (model.Account.Site == "outlook")
			{
				BindOutlookWindow bindOutlookWindow = new BindOutlookWindow();
				bindOutlookWindow.SetOriginAccount(model.Account?.Id);
				bindOutlookWindow.ShowDialog();
			}
			else if (model.Account.Site == "google")
			{
				BindGoogleAccount instance = BindGoogleAccount.GetInstance();
				instance.Start(model.Account?.Id);
				instance.ToastText += delegate(object sender, string s)
				{
					Utils.Toast(s);
				};
			}
			else if (model.Account.Kind == "icloud")
			{
				BindICloudWindow bindICloudWindow = new BindICloudWindow(null, model.Account);
				bindICloudWindow.Owner = Window.GetWindow(this);
				bindICloudWindow.Topmost = false;
				bindICloudWindow.ShowDialog();
			}
			break;
		}
		case ContextActionKey.Unsubscribe:
			GetParent().UnbindCalendar(bindCalendar.Id);
			break;
		}
	}

	private void HandleCalendarAction(ContextActionKey action)
	{
		CalendarSubscribeProfileModel calendarModel = GetCalendarModel();
		if (calendarModel != null)
		{
			switch (action)
			{
			case ContextActionKey.Edit:
				UserActCollectUtils.AddClickEvent("project_list_ui", "event", "cm.edit");
				GetParent().EditSubscribeCalendar(calendarModel);
				break;
			case ContextActionKey.Pin:
			case ContextActionKey.Unpin:
				UserActCollectUtils.AddClickEvent("project_list_ui", "event", (action == ContextActionKey.Pin) ? "cm.pin" : "cm.unpin");
				GetParent()?.OnPinClick(calendarModel.Id, 9, action == ContextActionKey.Pin);
				break;
			case ContextActionKey.Unsubscribe:
				GetParent()?.UnsubscribeCalendar(calendarModel);
				break;
			}
		}
	}

	private async void HandleTagAction(TagModel tag, ContextActionKey action)
	{
		bool flag = await CheckCanEditTag(tag.name);
		switch (action)
		{
		case ContextActionKey.Edit:
			UserActCollectUtils.AddClickEvent("project_list_ui", "tag", "cm.edit");
			GetParent()?.EditTag(tag, flag);
			break;
		case ContextActionKey.Delete:
			UserActCollectUtils.AddClickEvent("project_list_ui", "tag", "cm.delete");
			if (!flag)
			{
				Utils.Toast(Utils.GetString("NoNetwork"));
			}
			else
			{
				GetParent()?.DeleteTag(tag);
			}
			break;
		case ContextActionKey.AddSubTag:
			if (!flag)
			{
				Utils.Toast(Utils.GetString("NoNetwork"));
			}
			else
			{
				GetParent()?.AddSubTag(tag);
			}
			break;
		case ContextActionKey.MergeTags:
			UserActCollectUtils.AddClickEvent("project_list_ui", "tag", "cm.merge");
			if (!flag)
			{
				Utils.Toast(Utils.GetString("NoNetwork"));
			}
			else
			{
				GetParent()?.MergeTag(tag);
			}
			break;
		case ContextActionKey.Pin:
		case ContextActionKey.Unpin:
			UserActCollectUtils.AddClickEvent("project_list_ui", "tag", (action == ContextActionKey.Pin) ? "cm.pin" : "cm.unpin");
			GetParent()?.OnPinClick(tag.name, 7, action == ContextActionKey.Pin);
			break;
		case ContextActionKey.ToShareTag:
		case ContextActionKey.ToPersonalTag:
			TagService.SwitchTagType(tag.name);
			break;
		}
	}

	private static async Task<bool> CheckCanEditTag(string tag)
	{
		if (Utils.IsNetworkAvailable())
		{
			return true;
		}
		return (await TagDao.GetTagByName(tag)).status != 2;
	}

	private void HandleProjectAction(ContextActionKey action)
	{
		switch (action)
		{
		case ContextActionKey.Edit:
			UserActCollectUtils.AddClickEvent("project_list_ui", "list", "cm.edit");
			GetParent()?.EditProject(GetProjectModel());
			break;
		case ContextActionKey.Delete:
			UserActCollectUtils.AddClickEvent("project_list_ui", "list", "cm.delete");
			GetParent()?.DeleteOrExitProject(GetProjectModel());
			break;
		case ContextActionKey.Share:
			UserActCollectUtils.AddClickEvent("project_list_ui", "list", "cm.share");
			GetParent()?.ShareProject(GetProjectModel());
			break;
		case ContextActionKey.Exit:
			UserActCollectUtils.AddClickEvent("project_list_ui", "list", "cm.delete");
			GetParent()?.DeleteOrExitProject(GetProjectModel(), isDelete: false);
			break;
		case ContextActionKey.Duplicate:
			UserActCollectUtils.AddClickEvent("project_list_ui", "list", "cm.duplicate");
			GetParent()?.DuplicateProject(GetProjectModel());
			break;
		case ContextActionKey.Archive:
			UserActCollectUtils.AddClickEvent("project_list_ui", "list", "cm.archive");
			GetParent()?.CloseOrOpenProject(GetProjectModel());
			break;
		case ContextActionKey.UnArchiveList:
			UserActCollectUtils.AddClickEvent("project_list_ui", "list", "cm.unarchive");
			GetParent()?.CloseOrOpenProject(GetProjectModel());
			break;
		case ContextActionKey.Pin:
		case ContextActionKey.Unpin:
		{
			ProjectModel projectModel = GetProjectModel();
			UserActCollectUtils.AddClickEvent("project_list_ui", "list", (action == ContextActionKey.Pin) ? "cm.pin" : "cm.unpin");
			GetParent()?.OnPinClick(projectModel.id, 5, action == ContextActionKey.Pin);
			break;
		}
		}
	}

	private void HandleFilterAction(ContextActionKey action)
	{
		switch (action)
		{
		case ContextActionKey.Edit:
			UserActCollectUtils.AddClickEvent("project_list_ui", "filter", "cm.edit");
			GetParent()?.EditFilter(GetFilterModel());
			break;
		case ContextActionKey.Duplicate:
			if (ProChecker.CheckPro(ProType.Filter))
			{
				UserActCollectUtils.AddClickEvent("project_list_ui", "filter", "cm.duplicate");
				GetParent()?.DuplicateFilter(GetFilterModel());
			}
			break;
		case ContextActionKey.Delete:
			UserActCollectUtils.AddClickEvent("project_list_ui", "filter", "cm.delete");
			GetParent()?.DeleteFilter(GetFilterModel());
			break;
		case ContextActionKey.Pin:
		case ContextActionKey.Unpin:
		{
			FilterModel filterModel = GetFilterModel();
			UserActCollectUtils.AddClickEvent("project_list_ui", "filter", (action == ContextActionKey.Pin) ? "cm.pin" : "cm.unpin");
			GetParent()?.OnPinClick(filterModel.id, 8, action == ContextActionKey.Pin);
			break;
		}
		}
	}

	private ProjectModel GetProjectModel()
	{
		if (base.DataContext != null)
		{
			if (base.DataContext is NormalProjectViewModel normalProjectViewModel)
			{
				return normalProjectViewModel.Project;
			}
			if (base.DataContext is ticktick_WPF.ViewModels.SubProjectViewModel subProjectViewModel)
			{
				return subProjectViewModel.Project;
			}
		}
		return null;
	}

	private SmartProject GetSmartProject()
	{
		return GetModel<ticktick_WPF.ViewModels.SmartProjectViewModel>().Project;
	}

	private BindCalendarAccountModel GetBindCalendar()
	{
		return GetModel<BindCalendarAccountProjectViewModel>().Account;
	}

	private CalendarSubscribeProfileModel GetCalendarModel()
	{
		return GetModel<SubscribeCalendarProjectViewModel>().Profile;
	}

	private FilterModel GetFilterModel()
	{
		return GetModel<FilterProjectViewModel>().Filter;
	}

	private T GetModel<T>()
	{
		if (base.DataContext != null)
		{
			object dataContext = base.DataContext;
			if (dataContext is T)
			{
				return (T)dataContext;
			}
		}
		return default(T);
	}
}
