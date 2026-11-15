using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.TaskList;

namespace ticktick_WPF.Views.MainListView.DetailView;

public class TaskDetailHeadView : Grid
{
	private EscPopup _popup = new EscPopup
	{
		StaysOpen = false,
		HorizontalOffset = 45.0,
		VerticalOffset = 25.0,
		Placement = PlacementMode.Left
	};

	private readonly HoverIconButton _backBt = new HoverIconButton
	{
		Margin = new Thickness(-4.0, 0.0, 8.0, 0.0),
		Visibility = Visibility.Collapsed,
		HorizontalAlignment = HorizontalAlignment.Left,
		ToolTip = Utils.GetString("Close"),
		IsImage = false,
		ImageWidth = 16.0
	};

	private readonly TaskCheckIcon _checkIcon = new TaskCheckIcon
	{
		Margin = new Thickness(3.0, 0.0, 12.0, 0.0),
		VerticalAlignment = VerticalAlignment.Center,
		HorizontalAlignment = HorizontalAlignment.Left,
		Cursor = Cursors.Hand
	};

	private readonly Grid _dateGrid = new Grid
	{
		Height = 40.0,
		Margin = new Thickness(0.0, 0.0, 0.0, 0.0)
	};

	private TextBlock _dateText;

	private StackPanel _remindTimePanel;

	private StackPanel _rightIcons;

	private AssignIcon _assignIcon;

	private TaskProgressControl _progress;

	private bool _showPopupMouseDown;

	private bool isPopupOpen { get; set; }

	public TaskDetailHeadView()
	{
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
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
		base.RowDefinitions.Add(new RowDefinition());
		base.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		base.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(8.0)
		});
		InitUi();
	}

	private void InitUi()
	{
		_backBt.SetIconColor("BaseColorOpacity40");
		_backBt.MouseLeftButtonUp += OnBackClick;
		base.Children.Add(_backBt);
		_checkIcon.PreviewMouseLeftButtonUp += OnCheckBoxClick;
		_checkIcon.PreviewMouseRightButtonUp += CheckBoxRightMouseUp;
		_checkIcon.SetValue(Grid.ColumnProperty, 1);
		_checkIcon.SetBinding(UIElement.VisibilityProperty, "ShowCheckIcon");
		_checkIcon.SetSize(17);
		base.Children.Add(_checkIcon);
		Border border = new Border
		{
			Height = 14.0,
			Width = 4.0,
			BorderThickness = new Thickness(0.0, 0.0, 1.0, 0.0),
			Margin = new Thickness(28.0, 0.0, 8.0, 0.0)
		};
		border.SetValue(Grid.ColumnProperty, 1);
		border.SetResourceReference(Border.BorderBrushProperty, "BaseColorOpacity20");
		border.SetBinding(UIElement.VisibilityProperty, "ShowCheckIcon");
		base.Children.Add(border);
		InitDateGrid();
		InitRightIcons();
		_remindTimePanel = new StackPanel
		{
			Margin = new Thickness(28.0, -8.0, 5.0, 4.0),
			HorizontalAlignment = HorizontalAlignment.Left
		};
		_remindTimePanel.SetValue(Grid.ColumnProperty, 2);
		_remindTimePanel.SetValue(Grid.RowProperty, 1);
		base.Children.Add(_remindTimePanel);
		InitRemindText();
		_progress = new TaskProgressControl
		{
			VerticalAlignment = VerticalAlignment.Bottom,
			Margin = new Thickness(-19.0, 0.0, -19.0, 4.0)
		};
		_progress.ProgressChanged += OnProgressChanged;
		_progress.SetValue(Panel.ZIndexProperty, 1000);
		_progress.SetValue(Grid.ColumnSpanProperty, 4);
		_progress.SetValue(Grid.RowSpanProperty, 3);
		_progress.SetBinding(UIElement.IsEnabledProperty, "Enable");
		_progress.SetBinding(UIElement.VisibilityProperty, "ProgressVisibility");
		base.Children.Add(_progress);
		Line line = new Line
		{
			X1 = 0.0,
			X2 = 1.0,
			Stretch = Stretch.Fill,
			VerticalAlignment = VerticalAlignment.Bottom,
			Margin = new Thickness(-20.0, 0.0, -20.0, 4.0),
			StrokeThickness = 1.0
		};
		line.SetResourceReference(Shape.StrokeProperty, "BaseColorOpacity5");
		line.SetValue(Grid.ColumnSpanProperty, 4);
		line.SetValue(Grid.RowProperty, 2);
		base.Children.Add(line);
		_popup.Opened += PopupOpened;
		_popup.Closed += PopupClosed;
	}

	private void InitRemindText()
	{
		TextBlock textBlock = new TextBlock
		{
			FontSize = 11.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			TextTrimming = TextTrimming.CharacterEllipsis,
			Margin = new Thickness(0.0, 0.0, 0.0, 4.0),
			Background = Brushes.Transparent
		};
		textBlock.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity40");
		textBlock.SetBinding(TextBlock.TextProperty, "RemindTimeText");
		textBlock.SetBinding(UIElement.VisibilityProperty, "ShowSnoozeText");
		_remindTimePanel.Children.Add(textBlock);
		TextBlock textBlock2 = new TextBlock
		{
			FontSize = 11.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			TextTrimming = TextTrimming.CharacterEllipsis,
			Margin = new Thickness(0.0, 0.0, 0.0, 4.0),
			Background = Brushes.Transparent
		};
		textBlock2.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity40");
		textBlock2.SetBinding(TextBlock.TextProperty, "TimeZoneText");
		textBlock2.SetBinding(UIElement.VisibilityProperty, "ShowTimeZoneText");
		_remindTimePanel.Children.Add(textBlock2);
	}

	public void SetIconColor(int priority, int status)
	{
		_checkIcon.SetIconColor(DisplayType.Task, priority, status);
	}

	private void InitRightIcons()
	{
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal
		};
		stackPanel.SetBinding(UIElement.IsEnabledProperty, "Enable");
		stackPanel.SetValue(Grid.ColumnProperty, 3);
		_rightIcons = stackPanel;
		base.Children.Add(_rightIcons);
	}

	public void SetRightIcons(bool newAdd, bool isNote)
	{
		_rightIcons.Children.Clear();
		if (isNote)
		{
			if (!newAdd)
			{
				Border border = new Border
				{
					Width = 24.0,
					Cursor = Cursors.Hand
				};
				border.SetResourceReference(FrameworkElement.StyleProperty, "SmoothHoverBorderStyle60_80");
				border.MouseLeftButtonDown += OnShowPopupMouseDown;
				border.PreviewMouseLeftButtonUp += delegate(object o, MouseButtonEventArgs e)
				{
					ShowNoteMessage(o);
				};
				Path path = UiUtils.CreatePath("Ic.Setting.About", "BaseColorOpacity100", "", -1.0, geoKeyIsGeo: true);
				path.Width = 20.0;
				path.Height = 20.0;
				path.VerticalAlignment = VerticalAlignment.Center;
				path.Stretch = Stretch.Uniform;
				border.Child = path;
				_rightIcons.Children.Insert(0, border);
			}
		}
		else
		{
			HoverIconButton hoverIconButton = new HoverIconButton
			{
				ImageOpacity = 1.0,
				Margin = new Thickness(2.0, 0.0, -1.0, 0.0)
			};
			hoverIconButton.SetBinding(HoverIconButton.ImageSourceProperty, "PriorityImage");
			hoverIconButton.SetResourceReference(FrameworkElement.ToolTipProperty, "priority");
			hoverIconButton.MouseLeftButtonDown += OnShowPopupMouseDown;
			hoverIconButton.MouseLeftButtonUp += OnPriorityClick;
			_rightIcons.Children.Insert(0, hoverIconButton);
		}
	}

	private void InitDateGrid()
	{
		_dateGrid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		_dateGrid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		_dateGrid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		_dateGrid.SetValue(Grid.ColumnProperty, 2);
		base.Children.Add(_dateGrid);
		_dateGrid.MouseLeftButtonUp += SelectDateClick;
		_dateGrid.MouseLeftButtonDown += OnSelectTimeMouseDown;
		_dateGrid.SetBinding(UIElement.IsEnabledProperty, "Enable");
		Border border = new Border
		{
			Margin = new Thickness(0.0, 7.0, 0.0, 7.0),
			Cursor = Cursors.Hand
		};
		border.SetValue(Grid.ColumnSpanProperty, 3);
		border.SetResourceReference(FrameworkElement.StyleProperty, "SmoothHoverBorderStyle");
		_dateGrid.Children.Add(border);
		Path path = new Path
		{
			Width = 20.0,
			Height = 20.0,
			Stretch = Stretch.Uniform,
			IsHitTestVisible = false,
			Margin = new Thickness(4.0, 0.0, 0.0, 0.0),
			Data = Utils.GetGeometry("Ic.TaskOM.DateRemind")
		};
		path.SetBinding(Shape.FillProperty, "DateIconColor");
		_dateGrid.Children.Add(path);
		_dateText = new TextBlock
		{
			Margin = new Thickness(4.0, 0.0, 4.0, 0.0),
			Background = Brushes.Transparent,
			TextTrimming = TextTrimming.CharacterEllipsis,
			IsHitTestVisible = false
		};
		_dateText.SetBinding(TextBlock.ForegroundProperty, "DateIconColor");
		_dateText.SetResourceReference(FrameworkElement.StyleProperty, "Body01");
		_dateText.SetBinding(TextBlock.TextProperty, "DateText");
		_dateText.SetValue(Grid.ColumnProperty, 1);
		_dateGrid.Children.Add(_dateText);
		Image image = new Image
		{
			Width = 14.0,
			Height = 14.0,
			Cursor = Cursors.Hand,
			Margin = new Thickness(0.0, 4.0, 4.0, 4.0),
			Opacity = 0.4
		};
		image.SetBinding(FrameworkElement.ToolTipProperty, "RepeatText");
		image.SetValue(ToolTipService.InitialShowDelayProperty, 500);
		image.SetValue(ToolTipService.BetweenShowDelayProperty, 0);
		image.SetValue(Grid.ColumnProperty, 2);
		image.SetResourceReference(Image.SourceProperty, "RepeatDrawingImage");
		image.SetResourceReference(FrameworkElement.StyleProperty, "Icon01");
		image.SetBinding(UIElement.VisibilityProperty, "ShowRepeatIcon");
		_dateGrid.Children.Add(image);
	}

	private ContentControl GetNoteMessageContent()
	{
		if (base.DataContext is TaskDetailViewModel taskDetailViewModel)
		{
			ContentControl contentControl = new ContentControl();
			contentControl.SetResourceReference(FrameworkElement.StyleProperty, "PopupContentStyle");
			StackPanel stackPanel = (StackPanel)(contentControl.Content = new StackPanel());
			Style style = (Style)FindResource("Tag05");
			Style style2 = (Style)FindResource("Tag01");
			MarkdownTextStatisticsResult markdownTextStatisticsResult = MarkdownTextStatistics.Count(taskDetailViewModel.TaskContent);
			TextBlock element = new TextBlock
			{
				Text = Utils.GetString("Words"),
				LineHeight = 16.0,
				Margin = new Thickness(16.0, 4.0, 16.0, 0.0),
				Style = style
			};
			TextBlock element2 = new TextBlock
			{
				Text = Utils.IntToStringWithDivide(markdownTextStatisticsResult.WordCount),
				LineHeight = 16.0,
				Margin = new Thickness(16.0, 2.0, 16.0, 4.0),
				Style = style2,
				FontSize = 11.0
			};
			TextBlock element3 = new TextBlock
			{
				Text = Utils.GetString("Characters"),
				LineHeight = 16.0,
				Margin = new Thickness(16.0, 4.0, 16.0, 0.0),
				Style = style
			};
			TextBlock element4 = new TextBlock
			{
				Text = Utils.IntToStringWithDivide(markdownTextStatisticsResult.CharacterCount),
				LineHeight = 16.0,
				Margin = new Thickness(16.0, 2.0, 16.0, 8.0),
				Style = style2,
				FontSize = 11.0
			};
			TextBlock element5 = new TextBlock
			{
				Text = Utils.GetString("CreatedAt"),
				LineHeight = 16.0,
				Margin = new Thickness(16.0, 4.0, 16.0, 0.0),
				Style = style
			};
			TextBlock element6 = new TextBlock
			{
				Text = (taskDetailViewModel.CreateDate.HasValue ? DateUtils.FormatYearMonthDay(taskDetailViewModel.CreateDate.Value, DateFormatLen.Long, withWeek: true) : string.Empty),
				LineHeight = 16.0,
				Margin = new Thickness(16.0, 2.0, 16.0, 8.0),
				Style = style2,
				FontSize = 11.0
			};
			TextBlock element7 = new TextBlock
			{
				Text = Utils.GetString("ModifiedAt"),
				LineHeight = 16.0,
				Margin = new Thickness(16.0, 4.0, 16.0, 0.0),
				Style = style
			};
			TextBlock element8 = new TextBlock
			{
				Text = (taskDetailViewModel.ModifiedDate.HasValue ? DateUtils.FormatYearMonthDay(taskDetailViewModel.ModifiedDate.Value, DateFormatLen.Long, withWeek: true) : string.Empty),
				LineHeight = 16.0,
				Margin = new Thickness(16.0, 2.0, 16.0, 8.0),
				Style = style2,
				FontSize = 11.0
			};
			stackPanel.Children.Add(element);
			stackPanel.Children.Add(element2);
			stackPanel.Children.Add(element3);
			stackPanel.Children.Add(element4);
			stackPanel.Children.Add(element5);
			stackPanel.Children.Add(element6);
			stackPanel.Children.Add(element7);
			stackPanel.Children.Add(element8);
			return contentControl;
		}
		return null;
	}

	private TaskDetailView GetParent()
	{
		return Utils.FindParent<TaskDetailView>(this);
	}

	private void OnShowPopupMouseDown(object sender, MouseButtonEventArgs e)
	{
		_showPopupMouseDown = true;
	}

	private async void PopupClosed(object sender, EventArgs e)
	{
		GetParent()?.PopupClosed(sender, e);
		await Task.Delay(100);
		isPopupOpen = false;
	}

	private void PopupOpened(object sender, EventArgs e)
	{
		GetParent()?.PopupOpened(sender, e);
	}

	private void OnPriorityClick(object sender, MouseButtonEventArgs e)
	{
		TaskDetailView parent = GetParent();
		if (parent == null || !parent.CheckEnable())
		{
			return;
		}
		if (_showPopupMouseDown)
		{
			if (isPopupOpen)
			{
				return;
			}
			isPopupOpen = true;
			if (base.DataContext is TaskDetailViewModel taskDetailViewModel)
			{
				SetPriorityDialog setPriorityDialog = new SetPriorityDialog(taskDetailViewModel.Priority);
				_popup.Child = setPriorityDialog;
				_popup.PlacementTarget = sender as FrameworkElement;
				setPriorityDialog.PrioritySelect += OnPrioritySelected;
				Window window = Window.GetWindow(this);
				_popup.HorizontalOffset = ((window != null && window.WindowState == WindowState.Maximized) ? 32 : 45);
				_popup.IsOpen = true;
			}
		}
		_showPopupMouseDown = false;
		void OnPrioritySelected(object o, int i)
		{
			_popup.IsOpen = false;
			_popup.Child = null;
			GetParent()?.PrioritySelect(i);
		}
	}

	private void ShowNoteMessage(object sender)
	{
		if (_showPopupMouseDown)
		{
			ContentControl noteMessageContent = GetNoteMessageContent();
			_popup.Child = noteMessageContent;
			_popup.PlacementTarget = sender as FrameworkElement;
			Window window = Window.GetWindow(this);
			_popup.HorizontalOffset = ((window != null && window.WindowState == WindowState.Maximized) ? 32 : 45);
			_popup.IsOpen = true;
		}
		_showPopupMouseDown = false;
	}

	private void SelectDateClick(object sender, MouseButtonEventArgs e)
	{
		if (_showPopupMouseDown)
		{
			GetParent()?.SelectDateClick(e);
		}
		_showPopupMouseDown = false;
	}

	private void OnSetAssigneeMouseUp(object sender, MouseButtonEventArgs e)
	{
		TaskDetailView parent = GetParent();
		if (parent == null || !parent.CheckEnable() || !_showPopupMouseDown)
		{
			return;
		}
		_showPopupMouseDown = false;
		if (!(base.DataContext is TaskDetailViewModel taskDetailViewModel))
		{
			return;
		}
		if (Utils.IsNetworkAvailable())
		{
			_popup.PlacementTarget = sender as FrameworkElement;
			SetAssigneeDialog setAssigneeDialog = new SetAssigneeDialog(taskDetailViewModel.ProjectId, _popup, taskDetailViewModel.Assignee);
			parent.AddActionEvent("action", "assignee");
			setAssigneeDialog.AssigneeSelect += async delegate(object o, AvatarInfo avatar)
			{
				await parent.SetAssignee(avatar.UserId, inBatch: false);
			};
			setAssigneeDialog.Show();
		}
		else
		{
			parent.TryToast(Utils.GetString("NoNetwork"));
		}
	}

	private void OnSelectTimeMouseDown(object sender, MouseButtonEventArgs e)
	{
		TaskDetailView parent = GetParent();
		if (parent != null && parent.DatePopupShow)
		{
			parent.DatePopupShow = false;
		}
		else
		{
			_showPopupMouseDown = true;
		}
	}

	private void OnBackClick(object sender, MouseButtonEventArgs e)
	{
		GetParent()?.OnBackClick();
	}

	private void CheckBoxRightMouseUp(object sender, MouseButtonEventArgs e)
	{
		GetParent()?.OnCheckBoxMouseRightUp(_checkIcon);
	}

	private void OnCheckBoxClick(object sender, MouseButtonEventArgs e)
	{
		GetParent()?.OnCheckBoxClick();
	}

	public void SetBackIcon(bool show, bool isNavigate = false)
	{
		if (show)
		{
			_backBt.SetResourceReference(HoverIconButton.IconDataProperty, isNavigate ? "Ic.Common.LeftArrow" : "Ic.Common.Close");
			_backBt.Visibility = Visibility.Visible;
		}
		else
		{
			_backBt.Visibility = Visibility.Collapsed;
		}
	}

	private void OnProgressChanged(int pointerProgress, int currentProgress)
	{
		Utils.FindParent<TaskDetailView>(this)?.ProgressClick(pointerProgress, currentProgress);
	}

	public async Task SetProgress(int progress, bool withAnim = false)
	{
		_progress.SetProgress(progress, withAnim);
	}

	public void CheckProgress()
	{
		if (base.DataContext is TaskDetailViewModel taskDetailViewModel && _progress.GetProgress() != taskDetailViewModel.Progress)
		{
			_progress.SetProgress(taskDetailViewModel.Progress.GetValueOrDefault());
		}
	}

	public async Task SetAvatarVisible(bool showAssign, string assignee = null, string projectId = null)
	{
		if (showAssign)
		{
			string avatar = null;
			if (!string.IsNullOrEmpty(assignee) && assignee != "-1")
			{
				avatar = await AvatarHelper.GetAvatarUrl(assignee, projectId);
			}
			if (_assignIcon == null)
			{
				_assignIcon = new AssignIcon
				{
					Cursor = Cursors.Hand,
					Margin = new Thickness(0.0, 0.0, 4.0, 0.0)
				};
				_assignIcon.MouseLeftButtonUp += OnSetAssigneeMouseUp;
				_assignIcon.MouseLeftButtonDown += OnShowPopupMouseDown;
			}
			if (!_rightIcons.Children.Contains(_assignIcon))
			{
				_rightIcons.Children.Insert(0, _assignIcon);
			}
			_assignIcon.ToolTip = (string.IsNullOrEmpty(assignee) ? Utils.GetString("AssignTo") : AvatarHelper.GetCacheUserName(assignee, projectId));
			_assignIcon.SetAvatar(avatar);
		}
		else if (_assignIcon != null)
		{
			_rightIcons.Children.Remove(_assignIcon);
			_assignIcon = null;
		}
	}

	public UIElement GetDateDropTarget()
	{
		return _dateText;
	}
}
