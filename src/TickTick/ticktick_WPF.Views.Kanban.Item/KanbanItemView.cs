using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using TickTickUtils;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.DragAndDrop;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.CountDown.ViewModels;
using ticktick_WPF.Views.CountDown.Views;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Habit;
using ticktick_WPF.Views.MainListView;
using ticktick_WPF.Views.MainListView.TaskList;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.MarkDown.Colorizer;
using ticktick_WPF.Views.Tag;
using ticktick_WPF.Views.TaskList;

namespace ticktick_WPF.Views.Kanban.Item;

public class KanbanItemView : Border, ITaskOperation, IShowTaskDetailWindow
{
	private KanbanItemController _controller;

	private bool _previewLeftClick;

	private bool _isWaitingDoubleClick;

	private DateTime _lastClickTime;

	private bool _previewRightClick;

	private Point _startPoint;

	private Grid _root = new Grid();

	private TaskCheckIcon _checkIcon = new TaskCheckIcon
	{
		Width = 14.0,
		VerticalAlignment = VerticalAlignment.Top
	};

	private Border _bottomRect = new Border();

	private readonly Grid _container = new Grid();

	private Border _dragRect;

	private Border _titleBorder = new Border();

	private StackPanel _timeProjectPanel = new StackPanel
	{
		Orientation = Orientation.Horizontal,
		Margin = new Thickness(4.0, 1.0, 12.0, 0.0)
	};

	private KanbanItemIcons _itemIcons = new KanbanItemIcons
	{
		Margin = new Thickness(0.0, 0.0, 2.0, 4.0)
	};

	private Border _foldIcon;

	private TextBlock _contentText;

	private KanbanTagsControl _tagPanel;

	private ListItemProjectLabel _projectLabel;

	private Ellipse _avatarImage;

	private RowDefinition _firstRow;

	private KanbanColumnView _column;

	private bool _titleRowIconAlignmentPending;

	protected bool InPopup { get; set; }

	public KanbanItemView()
	{
		InitUi();
		base.DataContextChanged += OnDataBind;
		base.Loaded += delegate
		{
			AddEvent();
			BindColumnColorChanged();
		};
		base.Unloaded += delegate
		{
			ClearBinding();
		};
	}

	private void OnDraggingTaskChanged(object sender, PropertyChangedEventArgs e)
	{
		if (!(base.DataContext is DisplayItemModel displayItemModel))
		{
			return;
		}
		double opacity;
		if (!InPopup && !displayItemModel.Dragging)
		{
			HashSet<string> draggingTaskIds = TickDragHelper.DraggingTaskIds;
			if (draggingTaskIds != null && draggingTaskIds.Contains(displayItemModel.Id))
			{
				opacity = 0.4;
				goto IL_004d;
			}
		}
		opacity = 1.0;
		goto IL_004d;
		IL_004d:
		base.Opacity = opacity;
	}

	private void InitUi()
	{
		base.MinHeight = 42.0;
		base.Cursor = Cursors.Hand;
		Child = _bottomRect;
		_bottomRect.Child = _root;
		_root.Children.Add(_container);
		_firstRow = new RowDefinition
		{
			Height = GridLength.Auto
		};
		_firstRow.SetResourceReference(RowDefinition.MaxHeightProperty, "KanbanTitleMaxHeight");
		_container.RowDefinitions.Add(_firstRow);
		_container.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		_container.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		_container.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		_container.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(34.0)
		});
		_container.ColumnDefinitions.Add(new ColumnDefinition());
		base.MouseLeftButtonUp += OnDragMouseUp;
		base.MouseLeftButtonDown += OnLeftMouseDown;
		base.MouseRightButtonUp += OnRightClick;
		base.MouseRightButtonDown += OnRightMouseDown;
		base.MouseEnter += OnMouseEnter;
		base.MouseLeave += OnMouseLeave;
		base.MouseMove += OnItemDrag;
		base.SizeChanged += OnSizeChanged;
		base.RequestBringIntoView += OnBringIntoView;
		InitCheckIcon();
		InitIcons();
		InitTitle();
		SetResourceReference(Border.BackgroundProperty, "KanbanTaskItemBottomColor");
		_bottomRect.SetResourceReference(Border.BorderBrushProperty, "BaseColorOpacity10");
		UpdateCardShadow();
	}

	private void UpdateCardShadow()
	{
		if (!InPopup && LocalSettings.Settings.ExtraSettings.KbShowColumnColor)
		{
			base.Effect = null;
			return;
		}
		base.Effect = new DropShadowEffect
		{
			BlurRadius = (InPopup ? 12 : 5),
			ShadowDepth = (InPopup ? 2 : 2),
			Direction = (InPopup ? 300 : 270),
			Opacity = (InPopup ? 0.1 : 0.03)
		};
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (_tagPanel != null && Math.Abs(e.NewSize.Width - e.PreviousSize.Width) > 10.0 && base.DataContext is DisplayItemModel displayItemModel)
		{
			displayItemModel.SetKanbanDisplayTags();
		}
	}

	private void InitIcons()
	{
		_timeProjectPanel.SetValue(Grid.ColumnProperty, 1);
		_timeProjectPanel.SetValue(Grid.RowProperty, 3);
		_timeProjectPanel.Children.Add(_itemIcons);
		_itemIcons.SizeChanged += OnIconSizeChanged;
		_container.Children.Add(_timeProjectPanel);
	}

	private void InitTitle()
	{
		_titleBorder.SetValue(Grid.ColumnProperty, 1);
		_titleBorder.VerticalAlignment = VerticalAlignment.Top;
		_titleBorder.Margin = new Thickness(8.0, 12.0, 12.0, -2.0);
		_container.Children.Add(_titleBorder);
	}

	private void InitCheckIcon()
	{
		_checkIcon.SetResourceReference(FrameworkElement.MarginProperty, "KanbanItemCheckBoxMargin");
		_checkIcon.MouseRightButtonUp += OnCheckBoxRightMouseUp;
		_checkIcon.MouseLeftButtonUp += OnCheckBoxClick;
		_container.Children.Add(_checkIcon);
	}

	private void SetProject(DisplayItemModel model)
	{
		if (model.ShowProject)
		{
			if (_projectLabel == null)
			{
				_projectLabel = new ListItemProjectLabel
				{
					Margin = new Thickness(2.0, 2.0, 0.0, 6.0)
				};
				_projectLabel.ProjectTitle.MaxWidth = double.PositiveInfinity;
				_projectLabel.IsHitTestVisible = !model.IsCourse;
				_timeProjectPanel.Children.Add(_projectLabel);
			}
			_projectLabel.Opacity = ((model.Status == 0) ? 1.0 : 0.4);
		}
		else
		{
			_timeProjectPanel.Children.Remove(_projectLabel);
			_projectLabel = null;
		}
	}

	private void SetTagPanel(DisplayItemModel model)
	{
		if (model.ShowTag)
		{
			if (_tagPanel == null)
			{
				_tagPanel = new KanbanTagsControl
				{
					Margin = new Thickness(3.0, 2.0, 0.0, 2.0)
				};
				_tagPanel.SetValue(Grid.RowProperty, 2);
				_tagPanel.SetValue(Grid.ColumnProperty, 1);
				_container.Children.Add(_tagPanel);
			}
			_tagPanel.TagDelete -= OnTagDelete;
			_tagPanel.TagDelete += OnTagDelete;
			_tagPanel.Opacity = ((model.Status == 0) ? 1.0 : 0.4);
		}
		else
		{
			if (_tagPanel != null)
			{
				_tagPanel.TagDelete -= OnTagDelete;
			}
			_container.Children.Remove(_tagPanel);
			_tagPanel = null;
		}
	}

	private void SetTitle(string title, int status)
	{
		if (string.IsNullOrEmpty(title))
		{
			if (!(_titleBorder.Child is TextBlock))
			{
				TextBlock textBlock = new TextBlock
				{
					Margin = new Thickness(0.0, 0.0, 0.0, 6.0),
					Text = Utils.GetString("NoTitle"),
					TextDecorations = ((LocalSettings.Settings.ShowCompleteLine == 1 && status != 0) ? DeleteLineColorizer.HintStrikethrough : null)
				};
				textBlock.SetResourceReference(Control.FontSizeProperty, "Font14");
				textBlock.SetResourceReference(Control.ForegroundProperty, "BaseColorOpacity60");
				_titleBorder.Child = textBlock;
			}
			return;
		}
		if (!(_titleBorder.Child is TaskTitleBox))
		{
			TaskTitleBox taskTitleBox = new TaskTitleBox
			{
				Margin = new Thickness(0.0, 0.0, 4.0, 0.0),
				IsHitTestVisible = false,
				ReadOnly = true,
				VerticalAlignment = VerticalAlignment.Top,
				RenderLink = true,
				VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
			};
			taskTitleBox.SetWordWrap(wrap: true);
			taskTitleBox.SetBinding(LinkTextEditBox.TextStatusProperty, "Status");
			taskTitleBox.SetResourceReference(Control.FontSizeProperty, "Font14");
			_titleBorder.Child = taskTitleBox;
			taskTitleBox.LineSpacing = 4.0;
			taskTitleBox.SizeChanged += OnTitleSizeChanged;
		}
		TaskTitleBox taskTitleBox2 = (TaskTitleBox)_titleBorder.Child;
		if (taskTitleBox2.Text != title)
		{
			taskTitleBox2.SetText(title);
		}
		taskTitleBox2.SetTextForeground();
		UpdateTitleRowIconAlignment();
	}

	private void OnTitleSizeChanged(object sender, SizeChangedEventArgs e)
	{
		UpdateTitleRowIconAlignment();
	}

	private void UpdateTitleRowIconAlignment()
	{
		if (_titleRowIconAlignmentPending)
		{
			return;
		}
		UIElement child = _titleBorder.Child;
		TaskTitleBox title = child as TaskTitleBox;
		if (title == null)
		{
			return;
		}
		_titleRowIconAlignmentPending = true;
		base.Dispatcher.BeginInvoke(DispatcherPriority.Render, (Action)delegate
		{
			_titleRowIconAlignmentPending = false;
			if (_titleBorder.Child == title)
			{
				double defaultLineHeight = title.TextArea.TextView.DefaultLineHeight;
				if (!double.IsNaN(defaultLineHeight) && !double.IsInfinity(defaultLineHeight) && !(defaultLineHeight <= 0.0))
				{
					AlignIconToTitleLine(_checkIcon, defaultLineHeight);
					if (_foldIcon != null)
					{
						AlignIconToTitleLine(_foldIcon, defaultLineHeight);
					}
				}
			}
		});
	}

	private void AlignIconToTitleLine(FrameworkElement icon, double titleLineHeight)
	{
		Thickness margin = icon.Margin;
		double top = _titleBorder.Margin.Top + Math.Max(0.0, (titleLineHeight - icon.Height) / 2.0);
		icon.Margin = new Thickness(margin.Left, top, margin.Right, margin.Bottom);
	}

	private void SetContentText(DisplayItemModel model)
	{
		if (LocalSettings.Settings.ShowDetails)
		{
			DisplayItemModel.AssembleModelContent(model);
		}
		if (model.ShowContent)
		{
			if (_contentText == null)
			{
				_contentText = new TextBlock
				{
					TextWrapping = TextWrapping.NoWrap,
					TextTrimming = TextTrimming.CharacterEllipsis,
					LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
					Margin = new Thickness(8.0, 2.0, 0.0, 3.0)
				};
				_contentText.SetResourceReference(TextBlock.FontSizeProperty, "Font12");
				_contentText.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity40");
				_contentText.SetValue(Grid.RowProperty, 1);
				_contentText.SetValue(Grid.ColumnProperty, 1);
				_container.Children.Add(_contentText);
			}
			_contentText.Opacity = ((model.Status == 0) ? 1.0 : 0.6);
			string text = TaskUtils.GetContentDisplayText(model.Content);
			if (text.Length > 200)
			{
				text = text.Substring(0, 200);
			}
			if (text.Length > 50)
			{
				text = text.Substring(0, 50);
			}
			_contentText.Text = text;
		}
		else
		{
			_container.Children.Remove(_contentText);
			_contentText = null;
		}
	}

	private void SetFoldIcon(DisplayItemModel model)
	{
		if (model.HasChildren)
		{
			if (_foldIcon == null)
			{
				_foldIcon = new Border
				{
					Width = 14.0,
					Height = 14.0,
					Cursor = Cursors.Hand,
					VerticalAlignment = VerticalAlignment.Top,
					HorizontalAlignment = HorizontalAlignment.Left
				};
				_foldIcon.SetResourceReference(FrameworkElement.StyleProperty, "SmoothHoverBorderStyle40_60");
				_foldIcon.SetResourceReference(FrameworkElement.MarginProperty, "KanbanItemFoldIconMargin");
				_foldIcon.MouseLeftButtonUp += OnFoldClick;
				_container.Children.Add(_foldIcon);
				Path arrow = UiUtils.GetArrow(12.0, 0.0, "BaseColorOpacity100");
				arrow.RenderTransformOrigin = new Point(0.5, 0.5);
				_foldIcon.Child = arrow;
			}
			((Path)_foldIcon.Child).RenderTransform = new RotateTransform((!model.IsOpen) ? (-90) : 0);
			UpdateTitleRowIconAlignment();
		}
		else
		{
			if (_foldIcon != null)
			{
				_container.Children.Remove(_foldIcon);
				_foldIcon.MouseLeftButtonUp -= OnFoldClick;
				_foldIcon = null;
			}
			_checkIcon.SetResourceReference(FrameworkElement.MarginProperty, "KanbanItemCheckBoxMargin");
			UpdateTitleRowIconAlignment();
		}
	}

	private void SetDraggingBorder(bool show, int level)
	{
		if (show)
		{
			_container.Opacity = 0.0;
			if (_dragRect == null)
			{
				_dragRect = new Border
				{
					Background = ThemeUtil.GetColor("BaseColorOpacity3"),
					CornerRadius = base.CornerRadius
				};
				_root.Children.Add(_dragRect);
			}
			_dragRect.Margin = ((level > 0) ? new Thickness(16 + level * 24, 4.0, 4.0, 4.0) : new Thickness(0.0));
		}
		else
		{
			_container.Opacity = 1.0;
			if (_dragRect != null)
			{
				_root.Children.Remove(_dragRect);
				_dragRect = null;
			}
		}
	}

	public void SetContainerMargin(DisplayItemModel model)
	{
		int num = ((!InPopup && LocalSettings.Settings.ExtraSettings.KbShowColumnColor) ? (-4) : 0);
		_container.Margin = new Thickness((!InPopup) ? (model.Level * 24 + num) : 0, 0.0, num, 4.0);
	}

	private void SetCornerRadius(DisplayItemModel model)
	{
		int num = ((InPopup || model.ShowTopMargin) ? 1 : 0);
		int num2 = ((InPopup || model.ShowBottomMargin) ? 1 : 0);
		int num3 = 10;
		base.CornerRadius = new CornerRadius(num * num3, num * num3, num2 * num3, num2 * num3);
		_bottomRect.CornerRadius = base.CornerRadius;
		if (_dragRect != null)
		{
			_dragRect.CornerRadius = base.CornerRadius;
		}
		_bottomRect.BorderThickness = ((model.Dragging && model.Level == 0) ? new Thickness(0.0) : new Thickness(1.0, num, 1.0, num2));
		int num4 = (model.ShowTopMargin ? 2 : 0);
		bool kbShowColumnColor = LocalSettings.Settings.ExtraSettings.KbShowColumnColor;
		int num5 = (kbShowColumnColor ? 10 : 12);
		int num6 = (model.ShowBottomMargin ? (kbShowColumnColor ? 10 : 6) : 0);
		base.Margin = new Thickness(num5, InPopup ? 6 : num4, num5, InPopup ? 10 : num6);
	}

	private void SetBackGround(DisplayItemModel model)
	{
		if (model == null)
		{
			return;
		}
		if (model.Dragging && !InPopup && model.Level == 0)
		{
			_bottomRect.Background = Brushes.Transparent;
			base.Background = Brushes.Transparent;
			return;
		}
		if (TryGetColumnColor(out var color))
		{
			bool flag = model.InOperation || base.IsMouseOver;
			if (ThemeUtil.IsDark())
			{
				bool flag2 = ColorUtils.NeedsContrastBorder(color, isDark: true);
				int percent = ((!model.Selected) ? ((!flag) ? (flag2 ? 6 : 12) : (flag2 ? 8 : 16)) : (flag2 ? 10 : 20));
				_bottomRect.Background = ThemeUtil.GetAlphaColor(flag2 ? "#FFFFFF" : color, percent);
				if (flag2)
				{
					_bottomRect.BorderBrush = ThemeUtil.GetAlphaColor("#FFFFFF", 14);
				}
				else
				{
					_bottomRect.BorderBrush = ThemeUtil.GetAlphaColor(color, 24);
				}
			}
			else
			{
				_bottomRect.Background = (model.Selected ? ThemeUtil.GetBlendColor("#191919", color, 0.01f, 0.06f) : (flag ? ThemeUtil.GetBlendColor("#191919", color, 0.01f, 0.03f) : Brushes.Transparent));
				_bottomRect.SetResourceReference(Border.BorderBrushProperty, "BaseColorOpacity10");
			}
		}
		else
		{
			_bottomRect.SetResourceReference(Border.BackgroundProperty, (model.Dragging && !InPopup) ? "KanbanTaskItemBackground" : (model.Selected ? "ItemSelectedColor" : ((model.InOperation || base.IsMouseOver) ? "BaseColorOpacity3" : "KanbanTaskItemBackground")));
			_bottomRect.SetResourceReference(Border.BorderBrushProperty, "BaseColorOpacity10");
		}
		SetResourceReference(Border.BackgroundProperty, "KanbanTaskItemBottomColor");
	}

	private bool TryGetColumnColor(out string color)
	{
		color = null;
		if (InPopup || !LocalSettings.Settings.ExtraSettings.KbShowColumnColor)
		{
			return false;
		}
		color = (_column ?? Utils.FindParent<KanbanColumnView>(this))?.GetDisplayedColor();
		return KanbanColumnColorHelper.HasColor(color);
	}

	private void SetAvatar(DisplayItemModel model)
	{
		if (model.Avatar != null)
		{
			if (_avatarImage == null)
			{
				_avatarImage = new Ellipse
				{
					StrokeThickness = 1.0,
					Height = 20.0,
					Width = 20.0,
					VerticalAlignment = VerticalAlignment.Top,
					HorizontalAlignment = HorizontalAlignment.Right,
					Margin = new Thickness(0.0, 10.0, 8.0, 0.0)
				};
				_avatarImage.SetResourceReference(Shape.FillProperty, "BaseColorOpacity5");
				_avatarImage.SetValue(Grid.RowProperty, 0);
				_avatarImage.SetValue(Grid.ColumnProperty, 1);
				_container.Children.Add(_avatarImage);
				_titleBorder.Margin = new Thickness(8.0, 11.0, 30.0, 0.0);
			}
			_avatarImage.Fill = new ImageBrush
			{
				ImageSource = model.Avatar,
				Stretch = Stretch.Fill
			};
			_avatarImage.ToolTip = AvatarHelper.GetCacheUserName(model.Assignee, model.ProjectId);
		}
		else
		{
			_container.Children.Remove(_avatarImage);
			_avatarImage = null;
			_titleBorder.Margin = new Thickness(8.0, 11.0, 12.0, 0.0);
		}
	}

	private async void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		DisplayItemModel displayItemModel = e.NewValue as DisplayItemModel;
		DisplayItemModel oldVal = e.OldValue as DisplayItemModel;
		SetModel(oldVal, displayItemModel);
		if (displayItemModel != null && !string.IsNullOrEmpty(displayItemModel.Id))
		{
			PropertyChangedEventManager.AddHandler(displayItemModel, OnTaskPropertyChanged, string.Empty);
			if (!displayItemModel.ShowReminder.HasValue)
			{
				TaskItemLoadHelper.LoadShowReminder(displayItemModel);
			}
			if (_controller == null)
			{
				_controller = new KanbanItemController(this, displayItemModel);
				_itemIcons.SetController(_controller);
			}
			else
			{
				_controller.Reset(this, displayItemModel);
			}
			_checkIcon.SetIconColor(displayItemModel.Type, displayItemModel.Priority, displayItemModel.Status);
			SetTitle(displayItemModel.Title, displayItemModel.Status);
			SetBackGround(displayItemModel);
			displayItemModel.SetIcon();
			SetFoldIcon(displayItemModel);
			SetCornerRadius(displayItemModel);
			displayItemModel.TrySetAvatar();
			SetAvatar(displayItemModel);
			SetContentText(displayItemModel);
			SetTagPanel(displayItemModel);
			SetDraggingBorder(displayItemModel.Dragging && !InPopup, displayItemModel.Level);
			SetContainerMargin(displayItemModel);
			SetProject(displayItemModel);
			SetItemIconsMaxWidth(displayItemModel);
		}
	}

	private void SetModel(object oldVal, object newVal)
	{
		DisplayItemModel displayItemModel = oldVal as DisplayItemModel;
		DisplayItemModel displayItemModel2 = newVal as DisplayItemModel;
		displayItemModel2?.SetPropertyChangedEvent();
		if (displayItemModel != null)
		{
			PropertyChangedEventManager.RemoveHandler(displayItemModel, OnTaskPropertyChanged, string.Empty);
		}
		if (displayItemModel2 != null)
		{
			PropertyChangedEventManager.AddHandler(displayItemModel2, OnTaskPropertyChanged, string.Empty);
		}
	}

	private void OnIconSizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (base.DataContext is DisplayItemModel displayItemModel && _projectLabel != null)
		{
			_projectLabel.ProjectTitle.MaxWidth = Math.Max(20.0, (double)(200 - (displayItemModel.ShowProjectColor ? (-20) : 0)) - Math.Min(e.NewSize.Width, _itemIcons.MaxWidth));
		}
	}

	private void AddEvent()
	{
		PropertyChangedEventManager.AddHandler(StaticPropertiesModel.Instance, OnAltKeyDown, "AltKeyDown");
		PropertyChangedEventManager.AddHandler(LocalSettings.Settings, OnColumnColorChanged, "KbShowColumnColor");
		UpdateCardShadow();
	}

	private void ClearBinding()
	{
		if (base.DataContext is DisplayItemModel source)
		{
			PropertyChangedEventManager.RemoveHandler(source, OnTaskPropertyChanged, string.Empty);
		}
		if (_tagPanel != null)
		{
			_tagPanel.TagDelete -= OnTagDelete;
		}
		_controller?.Clear();
		_controller = null;
		PropertyChangedEventManager.RemoveHandler(StaticPropertiesModel.Instance, OnAltKeyDown, "AltKeyDown");
		PropertyChangedEventManager.RemoveHandler(LocalSettings.Settings, OnColumnColorChanged, "KbShowColumnColor");
		if (_column != null)
		{
			_column.DisplayColorChanged -= OnDisplayedColumnColorChanged;
			_column = null;
		}
	}

	private void OnColumnColorChanged(object sender, PropertyChangedEventArgs e)
	{
		UpdateCardShadow();
		RefreshColumnColorLayout();
	}

	private void RefreshColumnColorLayout()
	{
		if (base.DataContext is DisplayItemModel displayItemModel)
		{
			SetContainerMargin(displayItemModel);
			SetCornerRadius(displayItemModel);
			SetBackGround(displayItemModel);
		}
	}

	private void BindColumnColorChanged()
	{
		KanbanColumnView kanbanColumnView = Utils.FindParent<KanbanColumnView>(this);
		if (kanbanColumnView != _column)
		{
			if (_column != null)
			{
				_column.DisplayColorChanged -= OnDisplayedColumnColorChanged;
			}
			_column = kanbanColumnView;
			if (_column != null)
			{
				_column.DisplayColorChanged += OnDisplayedColumnColorChanged;
			}
		}
	}

	private void OnDisplayedColumnColorChanged(object sender, EventArgs e)
	{
		RefreshColumnColorLayout();
	}

	private void OnAltKeyDown(object sender, PropertyChangedEventArgs e)
	{
		if (base.DataContext is DisplayItemModel itemIconsMaxWidth)
		{
			SetItemIconsMaxWidth(itemIconsMaxWidth);
		}
	}

	private void OnTaskPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (!(base.DataContext is DisplayItemModel displayItemModel))
		{
			return;
		}
		string propertyName = e.PropertyName;
		if (propertyName == null)
		{
			return;
		}
		switch (propertyName.Length)
		{
		default:
			return;
		case 5:
			switch (propertyName[0])
			{
			case 'T':
				if (propertyName == "Title")
				{
					SetTitle(displayItemModel.Title, displayItemModel.Status);
				}
				break;
			case 'L':
				if (propertyName == "Level")
				{
					SetContainerMargin(displayItemModel);
					SetBackGround(displayItemModel);
					SetDraggingBorder(displayItemModel.Dragging && !InPopup, displayItemModel.Level);
				}
				break;
			}
			return;
		case 11:
		{
			char c = propertyName[0];
			if (c != 'D')
			{
				if (c != 'I')
				{
					if (c != 'P' || !(propertyName == "ProjectName"))
					{
						return;
					}
					goto IL_0257;
				}
				if (!(propertyName == "InOperation"))
				{
					return;
				}
				break;
			}
			if (propertyName == "DisplayTags")
			{
				SetTagPanel(displayItemModel);
			}
			return;
		}
		case 6:
			switch (propertyName[0])
			{
			case 'A':
				if (propertyName == "Avatar")
				{
					SetAvatar(displayItemModel);
				}
				break;
			case 'I':
				if (propertyName == "IsOpen")
				{
					SetFoldIcon(displayItemModel);
				}
				break;
			}
			return;
		case 16:
		{
			char c = propertyName[4];
			if (c != 'B')
			{
				if (c != 'P' || !(propertyName == "ShowProjectColor"))
				{
					return;
				}
				goto IL_0257;
			}
			if (!(propertyName == "ShowBottomMargin"))
			{
				return;
			}
			goto IL_028e;
		}
		case 8:
			switch (propertyName[0])
			{
			default:
				return;
			case 'D':
				if (propertyName == "Dragging")
				{
					SetBackGround(displayItemModel);
					SetCornerRadius(displayItemModel);
					SetDraggingBorder(displayItemModel.Dragging && !InPopup, displayItemModel.Level);
				}
				return;
			case 'S':
				break;
			case 'P':
				if (propertyName == "Priority")
				{
					_checkIcon.SetIconColor(displayItemModel.Type, displayItemModel.Priority, displayItemModel.Status);
				}
				return;
			}
			if (!(propertyName == "Selected"))
			{
				return;
			}
			break;
		case 9:
			if (propertyName == "ShowIcons")
			{
				_itemIcons.ResetIcons();
			}
			return;
		case 7:
			if (propertyName == "Content")
			{
				SetContentText(displayItemModel);
			}
			return;
		case 13:
			if (!(propertyName == "ShowTopMargin"))
			{
				return;
			}
			goto IL_028e;
		case 10:
		case 12:
		case 14:
		case 15:
			return;
			IL_028e:
			SetCornerRadius(displayItemModel);
			SetContainerMargin(displayItemModel);
			return;
			IL_0257:
			SetItemIconsMaxWidth(displayItemModel);
			return;
		}
		SetBackGround(displayItemModel);
	}

	private void SetItemIconsMaxWidth(DisplayItemModel model)
	{
		bool flag = StaticPropertiesModel.Instance.AltKeyDown && !LocalSettings.Settings.ShowCountDown;
		if (model.ShowProject && !flag)
		{
			double width = Utils.MeasureString(model.ProjectName, new Typeface(Utils.GetDefaultFontFamily(), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), 12.0).Width;
			_itemIcons.MaxWidth = Math.Max(124.0, (double)(200 - (model.ShowProjectColor ? 20 : 0)) - width);
			if (_projectLabel != null)
			{
				_projectLabel.ProjectTitle.MaxWidth = Math.Max(20.0, (double)(200 - (model.ShowProjectColor ? (-20) : 0)) - Math.Min(_itemIcons.ActualWidth, _itemIcons.MaxWidth));
			}
		}
		else
		{
			_itemIcons.MaxWidth = double.PositiveInfinity;
		}
	}

	private void OnFoldClick(object sender, MouseButtonEventArgs e)
	{
		_controller?.OnOpenPathClick(sender, e);
		e.Handled = true;
	}

	private void OnCheckBoxRightMouseUp(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		_controller?.OnCheckBoxRightMouseUp(_checkIcon);
	}

	private void OnCheckBoxClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		if (base.DataContext is DisplayItemModel { IsToggling: false } displayItemModel)
		{
			displayItemModel.IsToggling = true;
			_controller?.OnCheckBoxClick(displayItemModel);
		}
	}

	private async void OnRightClick(object sender, MouseButtonEventArgs e)
	{
		if (_previewRightClick)
		{
			_previewRightClick = false;
			if (_controller != null && !(await _controller.ShowOperationDialogSafely()))
			{
				await _controller.ShowTaskDetail();
			}
		}
	}

	private void OnTagDelete(object sender, string e)
	{
		_controller?.OnTagDelete(e.ToLower());
	}

	private void OnItemDrag(object sender, MouseEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed && _previewLeftClick && base.DataContext is DisplayItemModel { IsHabit: false, Enable: not false } && !TickDragHelper.Dragging)
		{
			Point position = e.GetPosition(this);
			if (Math.Abs(position.X - _startPoint.X) > 4.0 || Math.Abs(position.Y - _startPoint.Y) > 4.0)
			{
				_previewLeftClick = false;
				_controller?.OnKanbanItemDrag(e);
			}
		}
	}

	private async void OnDragMouseUp(object sender, MouseButtonEventArgs e)
	{
		object dataContext = base.DataContext;
		if (!(dataContext is DisplayItemModel model) || _controller == null)
		{
			return;
		}
		Utils.FindParent<ListViewContainer>(this)?.AddItemClickEvent();
		if (model.IsCountdown)
		{
			ShowCountdownDetail(model);
		}
		else if (model.IsEvent)
		{
			OpenEventDetailWindow(model);
		}
		else if (model.IsCourse)
		{
			ShowCourseDetail(model);
		}
		else if (model.IsHabit)
		{
			ShowHabitDetail(model, e);
		}
		else if (Utils.IfCtrlPressed() || Utils.IfShiftPressed())
		{
			_controller.OnBatchSelectMouseUp();
			_previewLeftClick = false;
		}
		else if (!model.Selected || model.InBatchSelected)
		{
			_isWaitingDoubleClick = false;
			bool firstClick = (DateTime.Now - _lastClickTime).TotalMilliseconds > 400.0;
			_lastClickTime = DateTime.Now;
			if (firstClick)
			{
				_isWaitingDoubleClick = true;
				await _controller.ShowDetailWindow();
			}
			if (!firstClick)
			{
				model.Selected = false;
				TaskDetailWindows.ShowTaskWindows(model.Id);
			}
			_previewLeftClick = false;
		}
	}

	private async void ShowCountdownDetail(DisplayItemModel model)
	{
		string id = model.Id;
		if (!string.IsNullOrEmpty(id))
		{
			CountdownModel countdownModel = await CountdownDao.GetCountdownById(id);
			if (countdownModel != null)
			{
				model.Selected = true;
				CountdownDetailWindow countdownDetailWindow = new CountdownDetailWindow();
				countdownDetailWindow.Disappear -= OnDetailClosed;
				countdownDetailWindow.Disappear += OnDetailClosed;
				countdownDetailWindow.Show(model: new CountDownViewModel(countdownModel, CountDownViewModel.FromSource.Kanban)
				{
					ScaleRatio = 1.2999999523162842
				}, target: this, targetWidth: base.ActualWidth + 6.0, addHeight: 0.0, byMouse: false);
			}
		}
	}

	private async void OpenEventDetailWindow(DisplayItemModel model)
	{
		string entityId = model.EntityId;
		if (!string.IsNullOrEmpty(entityId))
		{
			CalendarEventModel calendarEventModel = await CalendarEventDao.GetEventById(ArchivedDao.GetOriginalId(entityId));
			if (calendarEventModel != null)
			{
				calendarEventModel.DueStart = model.StartDate;
				calendarEventModel.DueEnd = model.DueDate;
				model.Selected = true;
				CalendarDetailWindow calendarDetailWindow = new CalendarDetailWindow();
				calendarDetailWindow.Disappear -= OnDetailClosed;
				calendarDetailWindow.Disappear += OnDetailClosed;
				calendarDetailWindow.Show(this, base.ActualWidth + 6.0, 0.0, byMouse: false, new CalendarDetailViewModel(calendarEventModel));
			}
		}
	}

	private async void ShowHabitDetail(DisplayItemModel model, MouseButtonEventArgs e)
	{
		HabitModel habitModel = await HabitDao.GetHabitById(model.Habit?.Id);
		if (habitModel != null)
		{
			HabitCheckInWindow habitCheckInWindow = new HabitCheckInWindow(habitModel, model.Status, model.StartDate, GetToastWindow());
			habitCheckInWindow.Show(this, base.ActualWidth, 0.0, byMouse: false);
			habitCheckInWindow.OnAction += delegate
			{
				model.Selected = false;
			};
			habitCheckInWindow.Closed += delegate
			{
				model.Selected = false;
			};
			model.Selected = true;
		}
	}

	private void OnCheckInAction(object sender, EventArgs e)
	{
		if (base.DataContext is DisplayItemModel displayItemModel)
		{
			displayItemModel.Selected = false;
		}
	}

	private IToastShowWindow GetToastWindow()
	{
		return Utils.FindParent<IToastShowWindow>(this);
	}

	private async void ShowCourseDetail(DisplayItemModel model)
	{
		if (model.Course != null)
		{
			CourseDetailViewModel viewModel = await CourseDetailViewModel.Build(model.Course);
			model.Selected = true;
			CourseDetailWindow courseDetailWindow = new CourseDetailWindow(viewModel);
			courseDetailWindow.Closed += async delegate
			{
				model.Selected = false;
				await Task.Delay(200);
			};
			courseDetailWindow.Show(this, base.ActualWidth, showByMouse: false);
		}
	}

	private void OnDetailClosed(object sender, string e)
	{
		if (base.DataContext is DisplayItemModel displayItemModel)
		{
			displayItemModel.Selected = false;
		}
	}

	private void OnMouseEnter(object sender, MouseEventArgs e)
	{
		_previewLeftClick = false;
		_previewRightClick = false;
		SetBackGround(base.DataContext as DisplayItemModel);
	}

	private void OnMouseLeave(object sender, MouseEventArgs e)
	{
		SetBackGround(base.DataContext as DisplayItemModel);
	}

	public void SetPriority(int priority)
	{
		_controller?.SetPriority(priority);
	}

	public void SetDate(string key)
	{
		_controller?.SetDate(key);
	}

	public void ClearDate()
	{
		_controller?.ClearDate();
	}

	public void SelectDate(bool relative)
	{
		_controller?.SelectDate();
	}

	public void ToggleTaskCompleted()
	{
		if (base.DataContext is DisplayItemModel model)
		{
			_controller?.ToggleItemCompleted(model);
		}
	}

	public void Delete()
	{
		_controller?.DeleteTask();
	}

	public bool ParsingDate()
	{
		return false;
	}

	public bool IsNewAdd()
	{
		return false;
	}

	public void PinOrUnpinTask()
	{
	}

	public void OpenSticky()
	{
	}

	public TaskListView GetParentList()
	{
		return _controller?.GetTaskListView();
	}

	private void OnLeftMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (_checkIcon.IsMouseOver)
		{
			return;
		}
		KanbanTagsControl tagPanel = _tagPanel;
		if ((tagPanel != null && tagPanel.IsMouseOver) || !(base.DataContext is DisplayItemModel displayItemModel))
		{
			return;
		}
		if (displayItemModel.IsEvent || displayItemModel.IsCourse)
		{
			KanbanColumnView kanbanColumnView = Utils.FindParent<KanbanColumnView>(this);
			if (kanbanColumnView != null && !kanbanColumnView.CanDragEvent())
			{
				return;
			}
		}
		if (displayItemModel.Enable)
		{
			_startPoint = e.GetPosition(this);
		}
		_previewLeftClick = true;
	}

	private void OnRightMouseDown(object sender, MouseButtonEventArgs e)
	{
		_previewRightClick = true;
	}

	public async Task SelectItem(bool focusTitle)
	{
		if (base.DataContext is DisplayItemModel model && _controller != null)
		{
			TaskDetailPopup window = await _controller.SelectKanbanItem(model, focusTitle);
			await Task.Delay(100);
			window?.TryShow();
		}
	}

	private void OnBringIntoView(object sender, RequestBringIntoViewEventArgs e)
	{
		if (Mouse.LeftButton == MouseButtonState.Pressed)
		{
			KanbanContainer kanbanContainer = Utils.FindParent<KanbanContainer>(this);
			if (kanbanContainer != null && kanbanContainer.IsInDragging())
			{
				e.Handled = true;
			}
		}
	}

	public void OnDetailKeyUp(Key key)
	{
	}
}
