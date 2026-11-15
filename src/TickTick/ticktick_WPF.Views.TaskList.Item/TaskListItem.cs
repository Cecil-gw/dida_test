using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.MainListView;
using ticktick_WPF.Views.MainListView.TaskList;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Pomo;
using ticktick_WPF.Views.Search;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Views.TaskList.Item;

public class TaskListItem : Canvas, ITaskOperation, IShowTaskDetailWindow
{
	private enum ControlType
	{
		Avatar,
		Icons,
		ProjectLabel,
		TagPanel
	}

	private class ControlInfo
	{
		public ControlType Type { get; set; }

		public double Width { get; set; }
	}

	private DisplayItemController _controller;

	private bool _startDrag;

	private bool _mouseDown;

	private Point _startPoint;

	private DisplayItemModel _model;

	private ListItemTagsControl _tagPanel;

	private TaskItemPomoSummaryControl _pomoSummary;

	private ListItemProjectLabel _projectLabel;

	private CommentSearchLabel _commentLabel;

	private DateTime _lastClickTime;

	private bool _isWaitingDoubleClick;

	private Point _firstClickPoint;

	private bool _isLocked;

	private int _startLeft;

	private bool _detailTagRefreshPending;

	private Border _dragBorder;

	private Border _moreBorder;

	private Border _backBorder;

	private static readonly Geometry DragBarIcon;

	private static readonly Geometry MoreIcon;

	private Canvas _itemContainer;

	private Rectangle _colorRect;

	private Rectangle _avatar;

	private Line _bottomLine;

	public ListItemContent TitleContent;

	private TextBlock _noteTimeText;

	private TextBlock ContentText;

	private TaskItemIcons Icons = new TaskItemIcons();

	private int _tagMaxCount = 2;

	private bool _showDetail;

	private double _originWidth;

	private const double leftMargin = 0.0;

	private const double rightMargin = 0.0;

	private const double minTitleWidth = 100.0;

	private CompleteStory _completeStoryView;

	private TextBlock _csTextBlock;

	private Border _csBackBorder;

	private bool _unloaded;

	private bool _showIcons = true;

	private bool InMatrix { get; set; }

	private bool InPreview { get; set; }

	private bool InList { get; set; }

	static TaskListItem()
	{
		DragBarIcon = Utils.GetIcon("DragBarIcon");
		MoreIcon = Utils.GetGeometry("Ic.TaskList.More");
	}

	public TaskListItem()
	{
		InList = true;
		InitControl();
		base.ClipToBounds = true;
	}

	private void OnAltKeyDown(object sender, PropertyChangedEventArgs e)
	{
		SetItemChildrenLeft();
	}

	public TaskListItem(bool inMatrix, bool inPreview)
	{
		InMatrix = inMatrix;
		InPreview = inPreview;
		InitControl();
	}

	private void LoadContent()
	{
		_itemContainer = new Canvas
		{
			Background = Brushes.Transparent
		};
		_itemContainer.ClipToBounds = true;
		_itemContainer.SetBinding(FrameworkElement.HeightProperty, new Binding
		{
			RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(TaskListItem), 1),
			Path = new PropertyPath(FrameworkElement.ActualHeightProperty)
		});
		base.Children.Add(_itemContainer);
		Canvas.SetLeft(_itemContainer, 12.0);
		_itemContainer.MouseEnter += OnContentMouseEnter;
		_itemContainer.MouseLeave += OnContentMouseLeave;
		TitleContent = new ListItemContent();
		TitleContent.Width = _itemContainer.Width - 4.0;
		_itemContainer.Children.Add(TitleContent);
		_itemContainer.SetValue(Panel.ZIndexProperty, 100);
		Canvas.SetLeft(TitleContent, 0.0);
		if (!InMatrix && !InPreview)
		{
			TitleContent.TitleTextBox.SetupSearchRender(isTitle: true, inSearchDetail: false);
		}
	}

	private void LoadIcons()
	{
		_itemContainer.Children.Add(Icons);
		Icons.SizeChanged += OnIconsSizeChanged;
		Icons.SetResourceReference(Canvas.TopProperty, "Double10Add3");
	}

	private void OnIconsSizeChanged(object sender, SizeChangedEventArgs e)
	{
		SetItemsLeft();
	}

	private void SetTitleWidth(double extraWidth = 0.0)
	{
		TitleContent.Width = Math.Max(0.0, _itemContainer.Width - extraWidth);
	}

	private void ShowOrHideDragBar(bool show)
	{
		DisplayItemModel model = GetModel();
		if (model == null)
		{
			return;
		}
		show = show && !_isLocked;
		if (show && !TaskDragHelpModel.DragHelp.IsDragging && model.Enable && model.ShowDragBar && !model.IsHabit && !InMatrix)
		{
			ShowDragBar();
		}
		else if (_dragBorder != null)
		{
			try
			{
				base.Children.Remove(_dragBorder);
				_dragBorder = null;
			}
			catch (Exception)
			{
			}
		}
	}

	private DisplayItemModel GetModel()
	{
		return base.DataContext as DisplayItemModel;
	}

	private void ShowDragBar()
	{
		if (_dragBorder == null)
		{
			_dragBorder = new Border
			{
				Width = 18.0,
				HorizontalAlignment = HorizontalAlignment.Left,
				Cursor = Cursors.SizeAll
			};
			_dragBorder.SetValue(Panel.ZIndexProperty, 10);
			_dragBorder.PreviewMouseLeftButtonDown += OnDragMouseDown;
			_dragBorder.MouseMove += OnMouseMove;
			_dragBorder.PreviewMouseLeftButtonUp += OnDragMouseUp;
			_dragBorder.SetResourceReference(FrameworkElement.StyleProperty, "SmoothHoverBorderStyle40_100");
			Path path = new Path
			{
				Width = 12.0,
				Height = 12.0,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Stretch = Stretch.Uniform
			};
			path.SetResourceReference(Shape.FillProperty, "BaseColorOpacity100_80");
			path.Data = DragBarIcon;
			_dragBorder.Child = path;
		}
		_dragBorder.Height = base.Height;
		if (!base.Children.Contains(_dragBorder))
		{
			base.Children.Add(_dragBorder);
		}
		Canvas.SetTop(_dragBorder, 0.0);
		Canvas.SetLeft(_dragBorder, _startLeft);
	}

	private void ShowOrHideMoreIcon(bool show)
	{
		DisplayItemModel model = GetModel();
		if (model != null)
		{
			show = show && !_isLocked;
			if (show && !TaskDragHelpModel.DragHelp.IsDragging && model.Enable && model.HitVisible && !InMatrix)
			{
				ShowMoreIcon();
			}
			else if (_moreBorder != null)
			{
				base.Children.Remove(_moreBorder);
				_moreBorder = null;
			}
		}
	}

	private void ShowMoreIcon()
	{
		if (_moreBorder == null)
		{
			_moreBorder = new Border
			{
				Width = 10.0,
				HorizontalAlignment = HorizontalAlignment.Right,
				Cursor = Cursors.Hand
			};
			_moreBorder.SetValue(Panel.ZIndexProperty, 10);
			_moreBorder.MouseLeftButtonUp += OperationClick;
			_moreBorder.SetResourceReference(FrameworkElement.StyleProperty, "SmoothHoverBorderStyle40_100");
			Path path = new Path
			{
				Width = 8.0,
				Height = 8.0,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Stretch = Stretch.Uniform
			};
			path.SetResourceReference(Shape.FillProperty, "BaseColorOpacity100_80");
			path.Data = MoreIcon;
			_moreBorder.Child = path;
			base.Children.Add(_moreBorder);
			Canvas.SetTop(_moreBorder, 0.0);
		}
		_moreBorder.Height = base.Height;
		Canvas.SetLeft(_moreBorder, base.ActualWidth - 16.0);
	}

	private void OnContentMouseEnter(object sender, MouseEventArgs e)
	{
		ShowBackground();
	}

	private void OnContentMouseLeave(object sender, MouseEventArgs e)
	{
		ShowBackground();
	}

	private void ShowBackground()
	{
		DisplayItemModel model = GetModel();
		if (model == null)
		{
			return;
		}
		if (model.IsCountdown)
		{
			ShowBottomLine();
			return;
		}
		string text = string.Empty;
		if (!TaskDragHelpModel.DragHelp.IsDragging)
		{
			if (model.InOperation || _itemContainer.IsMouseOver || model.Dragging)
			{
				text = "ListItemHover";
			}
			if (model.Selected)
			{
				text = "ItemSelectedColor";
			}
		}
		else if (model.Dragging)
		{
			text = "BaseColorOpacity3";
		}
		else if (model.Selected)
		{
			text = "ItemSelectedColor";
		}
		if (!string.IsNullOrEmpty(text) && !_isLocked)
		{
			if (_backBorder == null)
			{
				_backBorder = new Border();
				_backBorder.SetBinding(FrameworkElement.HeightProperty, new Binding
				{
					RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(TaskListItem), 1),
					Path = new PropertyPath(FrameworkElement.ActualHeightProperty)
				});
			}
			if (!base.Children.Contains(_backBorder))
			{
				base.Children.Add(_backBorder);
			}
			_backBorder.SetResourceReference(Panel.BackgroundProperty, text);
			_backBorder.Width = Math.Max(0.0, base.ActualWidth - (double)_startLeft - 36.0);
			Canvas.SetLeft(_backBorder, _startLeft + 18);
			Canvas.SetTop(_backBorder, 0.0);
			CornerRadius cornerRadius = new CornerRadius(10.0);
			if (text == "ItemSelectedColor")
			{
				if (!model.ShowTopMargin)
				{
					double topLeft = (cornerRadius.TopRight = 0.0);
					cornerRadius.TopLeft = topLeft;
				}
				if (!model.ShowBottomMargin)
				{
					double topLeft = (cornerRadius.BottomRight = 0.0);
					cornerRadius.BottomLeft = topLeft;
				}
			}
			_backBorder.CornerRadius = cornerRadius;
		}
		else if (_backBorder != null)
		{
			Border backBorder = _backBorder;
			_backBorder = null;
			base.Children.Remove(backBorder);
		}
		ShowBottomLine();
	}

	private async void ShowColorRect()
	{
		await Task.Delay(50);
		DisplayItemModel model = GetModel();
		if (!InMatrix && !string.IsNullOrEmpty(model?.Color))
		{
			if (_colorRect == null)
			{
				_colorRect = new Rectangle
				{
					Width = 3.0
				};
				_colorRect.SetBinding(FrameworkElement.HeightProperty, new Binding
				{
					RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(TaskListItem), 1),
					Path = new PropertyPath(FrameworkElement.ActualHeightProperty)
				});
				Canvas.SetLeft(_colorRect, 18.0);
				_colorRect.SetBinding(Shape.FillProperty, "Color");
				_colorRect.SetValue(Panel.ZIndexProperty, 100);
				base.Children.Add(_colorRect);
			}
			_colorRect.Opacity = ((!model.ShowProject) ? 0.0 : ((model.Status == 0) ? 1.0 : 0.36));
			_colorRect.Visibility = ((!LocalSettings.Settings.ExtraSettings.IsShowProjectListColor || model.Dragging) ? Visibility.Collapsed : Visibility.Visible);
		}
	}

	private void ShowBottomLine()
	{
		DisplayItemModel model = GetModel();
		if (model == null || model.IsCountdown || (!model.Dragging && !model.Selected && !_itemContainer.IsMouseOver))
		{
			if (_bottomLine == null)
			{
				_bottomLine = new Line
				{
					Width = Math.Max(0.0, _itemContainer.Width - 40.0),
					StrokeThickness = 1.0,
					X1 = 0.0,
					X2 = 1.0,
					Stretch = Stretch.Fill
				};
				_bottomLine.Margin = new Thickness(0.0, -1.0, 0.0, 0.0);
				_bottomLine.SetBinding(Canvas.TopProperty, new Binding
				{
					RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(TaskListItem), 1),
					Path = new PropertyPath(FrameworkElement.ActualHeightProperty)
				});
				_bottomLine.SetResourceReference(Shape.StrokeProperty, "BaseColorOpacity5");
				_itemContainer.Children.Add(_bottomLine);
				Canvas.SetLeft(_bottomLine, 36.0);
			}
		}
		else if (_bottomLine != null && _itemContainer.Children.Contains(_bottomLine))
		{
			_itemContainer.Children.Remove(_bottomLine);
			_bottomLine = null;
		}
	}

	private async void ShowAvatar()
	{
		DisplayItemModel model = GetModel();
		string text = model?.AvatarUrl;
		if (!string.IsNullOrEmpty(text))
		{
			if (_avatar == null)
			{
				_avatar = new Rectangle
				{
					Width = 18.0,
					Height = 18.0,
					RadiusX = 9.0,
					RadiusY = 9.0
				};
				_avatar.MouseLeftButtonUp += OnAvatarMouseUp;
				_avatar.Cursor = Cursors.Hand;
				_itemContainer.Children.Add(_avatar);
				_avatar.SetResourceReference(Canvas.TopProperty, "Double11Add2");
			}
			Canvas.SetLeft(_avatar, _itemContainer.ActualWidth - 32.0);
			_avatar.Visibility = Visibility.Visible;
			_avatar.Opacity = ((model.Status == 0) ? 1.0 : 0.4);
			Rectangle avatar = _avatar;
			ImageBrush imageBrush = new ImageBrush();
			ImageBrush imageBrush2 = imageBrush;
			imageBrush2.ImageSource = await AvatarHelper.GetAvatarByUrlAsync(text);
			avatar.Fill = imageBrush;
			_avatar.ToolTip = AvatarHelper.GetCacheUserName(model.Assignee, model.ProjectId);
		}
		else if (_avatar != null)
		{
			_avatar.Visibility = Visibility.Collapsed;
		}
	}

	private void ShowCreateTimeText()
	{
		DisplayItemModel model = GetModel();
		if (_showDetail && model != null && model.NoteDisplayDate.HasValue)
		{
			if (_noteTimeText == null)
			{
				_noteTimeText = new TextBlock
				{
					FontSize = 12.0
				};
				_noteTimeText.Margin = new Thickness(0.0, 2.0, 0.0, 0.0);
				_noteTimeText.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity40");
				_itemContainer.Children.Add(_noteTimeText);
				Canvas.SetLeft(_noteTimeText, 38.0);
			}
			_noteTimeText.Text = DateUtils.FormatYearMonthDay(model.NoteDisplayDate.Value, DateFormatLen.Middle);
		}
		else if (_noteTimeText != null)
		{
			_itemContainer.Children.Remove(_noteTimeText);
			_noteTimeText = null;
		}
	}

	private void InitControl()
	{
		base.Name = "ItemRoot";
		SetResourceReference(FrameworkElement.HeightProperty, "Height40");
		base.DataContextChanged += OnDataBind;
		base.Background = Brushes.Transparent;
		base.MouseEnter += OnMouseEnter;
		base.MouseLeave += OnMouseLeave;
		base.PreviewMouseLeftButtonUp += OnItemClick;
		base.MouseRightButtonUp += OnRightClick;
		base.MouseMove += OnItemMouseMove;
		base.PreviewMouseLeftButtonDown += OnMouseDown;
		LoadContent();
		LoadIcons();
		if (InPreview)
		{
			SetLocked(locked: true);
		}
		base.Unloaded += delegate
		{
			RemoveBinding();
			PropertyChangedEventManager.RemoveHandler(StaticPropertiesModel.Instance, OnAltKeyDown, "AltKeyDown");
			DataChangedNotifier.ProjectListColorVisChanged -= DataChangedNotifier_ProjectListColorVisChanged;
		};
		base.Loaded += delegate
		{
			ReloadData();
			PropertyChangedEventManager.AddHandler(StaticPropertiesModel.Instance, OnAltKeyDown, "AltKeyDown");
			DataChangedNotifier.ProjectListColorVisChanged += DataChangedNotifier_ProjectListColorVisChanged;
		};
	}

	private void DataChangedNotifier_ProjectListColorVisChanged(object sender, EventArgs e)
	{
		ShowColorRect();
	}

	protected override Size MeasureOverride(Size constraint)
	{
		if (Math.Abs(_originWidth - constraint.Width) > 1.0)
		{
			double width = Math.Min(10000.0, Math.Max(0.0, constraint.Width - (double)_startLeft - 36.0));
			_originWidth = constraint.Width;
			_itemContainer.Width = width;
			if (_backBorder != null)
			{
				_backBorder.Width = width;
			}
			base.Dispatcher.BeginInvoke((Action)delegate
			{
				SetItemChildrenLeft();
			}, DispatcherPriority.Background);
		}
		return base.MeasureOverride(constraint);
	}

	private void SetItemChildrenLeft()
	{
		bool flag = StaticPropertiesModel.Instance.AltKeyDown && !LocalSettings.Settings.ShowCountDown;
		DisplayItemModel model = GetModel();
		int num = (_showDetail ? _tagMaxCount : ((flag && model != null && model.StartDate.HasValue && (!model.IsNote || !(model.StartDate < DateTime.Today))) ? ((_originWidth > 520.0) ? 2 : ((!(_originWidth < 430.0)) ? 1 : 0)) : ((_originWidth > 480.0) ? 2 : ((!(_originWidth < 390.0)) ? 1 : 0))));
		if (_tagMaxCount != num)
		{
			_tagMaxCount = num;
			model?.SetDisplayTags(_tagMaxCount);
		}
		if (model != null)
		{
			bool flag2 = !model.StartDate.HasValue || (model.IsNote && model.StartDate < DateTime.Today) || !flag || _originWidth > 390.0;
			if (flag2 != _showIcons)
			{
				_showIcons = flag2;
				Icons.ResetIcons(flag2);
			}
			model.SetCanShowProject(!model.StartDate.HasValue || (model.IsNote && model.StartDate < DateTime.Today) || _showDetail || !flag || _originWidth >= 350.0);
		}
		if (_bottomLine != null)
		{
			_bottomLine.Width = Math.Max(0.0, _itemContainer.Width - 40.0);
		}
		SetItemsLeft();
	}

	private void SetItemsLeft()
	{
		if (GetModel() != null)
		{
			if (_showDetail)
			{
				SetDetailModeLayout();
			}
			else
			{
				SetNormalModeLayout();
			}
		}
	}

	private void SetNormalModeLayout()
	{
		DisplayItemModel model = GetModel();
		double actualWidth = _itemContainer.ActualWidth;
		if (actualWidth <= 0.0)
		{
			return;
		}
		bool flag = !string.IsNullOrEmpty(model?.AvatarUrl) && _avatar != null;
		bool flag2 = _showIcons && Icons.ActualWidth > 0.0;
		bool flag3 = _projectLabel != null && model.ShowProject;
		bool num = _tagPanel != null && model.ShowTag;
		double num2 = (flag ? (_avatar.Width + 2.0) : 0.0);
		double num3 = (flag2 ? (Icons.ActualWidth + 2.0) : 0.0);
		double num4 = (flag3 ? _projectLabel.ActualWidth : 0.0);
		double num5 = (num ? _tagPanel.ActualWidth : 0.0);
		double num6 = actualWidth - 8.0;
		List<ControlInfo> list = new List<ControlInfo>();
		if (flag)
		{
			list.Add(new ControlInfo
			{
				Type = ControlType.Avatar,
				Width = num2
			});
		}
		if (flag2)
		{
			list.Add(new ControlInfo
			{
				Type = ControlType.Icons,
				Width = num3
			});
		}
		if (flag3)
		{
			list.Add(new ControlInfo
			{
				Type = ControlType.ProjectLabel,
				Width = num4
			});
		}
		if (num)
		{
			list.Add(new ControlInfo
			{
				Type = ControlType.TagPanel,
				Width = num5
			});
		}
		double num7 = list.Sum((ControlInfo c) => c.Width);
		double num8 = actualWidth - 0.0 - 0.0 - num7;
		if (num8 < 100.0)
		{
			for (int num9 = list.Count - 1; num9 >= 0; num9--)
			{
				if (list[num9].Type == ControlType.TagPanel)
				{
					num7 -= list[num9].Width;
					list.RemoveAt(num9);
					num8 = actualWidth - 0.0 - 0.0 - num7;
					if (num8 >= 100.0)
					{
						break;
					}
				}
			}
			if (num8 < 100.0)
			{
				for (int num10 = list.Count - 1; num10 >= 0; num10--)
				{
					if (list[num10].Type == ControlType.ProjectLabel)
					{
						num7 -= list[num10].Width;
						list.RemoveAt(num10);
						num8 = actualWidth - 0.0 - 0.0 - num7;
						if (num8 >= 100.0)
						{
							break;
						}
					}
				}
			}
		}
		if (_projectLabel != null)
		{
			_projectLabel.Visibility = Visibility.Collapsed;
		}
		if (_tagPanel != null)
		{
			_tagPanel.Visibility = Visibility.Collapsed;
		}
		using (List<ControlInfo>.Enumerator enumerator = list.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				switch (enumerator.Current.Type)
				{
				case ControlType.Avatar:
					Canvas.SetLeft(_avatar, num6 - 18.0);
					num6 -= num2;
					break;
				case ControlType.Icons:
					Canvas.SetLeft(Icons, num6 - Icons.ActualWidth);
					num6 -= num3;
					break;
				case ControlType.ProjectLabel:
					_projectLabel.Visibility = Visibility.Visible;
					Canvas.SetLeft(_projectLabel, num6 - _projectLabel.ActualWidth);
					num6 -= num4;
					break;
				case ControlType.TagPanel:
					_tagPanel.Visibility = Visibility.Visible;
					Canvas.SetLeft(_tagPanel, num6 - _tagPanel.ActualWidth);
					num6 -= num5;
					break;
				}
			}
		}
		double width = Math.Max(100.0, num6 - 0.0 - 0.0);
		TitleContent.Width = width;
		UpdateClipRegions();
	}

	private void UpdateClipRegions()
	{
		double num = 0.0 + TitleContent.Width + 0.0;
		if (_projectLabel != null && _projectLabel.Visibility == Visibility.Visible)
		{
			double left = Canvas.GetLeft(_projectLabel);
			if (left < num)
			{
				double width = Math.Max(0.0, _projectLabel.ActualWidth - (num - left));
				_projectLabel.Clip = new RectangleGeometry
				{
					Rect = new Rect(0.0, 0.0, width, 40.0)
				};
			}
			else
			{
				_projectLabel.Clip = null;
			}
		}
		if (_tagPanel != null && _tagPanel.Visibility == Visibility.Visible)
		{
			double left2 = Canvas.GetLeft(_tagPanel);
			if (left2 < num)
			{
				double width2 = Math.Max(0.0, _tagPanel.ActualWidth - (num - left2));
				_tagPanel.Clip = new RectangleGeometry
				{
					Rect = new Rect(0.0, 0.0, width2, 40.0)
				};
			}
			else
			{
				_tagPanel.Clip = null;
			}
		}
	}

	private void SetDetailModeLayout()
	{
		if (_avatar != null)
		{
			Canvas.SetLeft(_avatar, _itemContainer.Width - 32.0);
		}
		DisplayItemModel model = GetModel();
		double num = ((!string.IsNullOrEmpty(model?.AvatarUrl)) ? 34 : 8);
		Canvas.SetLeft(Icons, _itemContainer.Width - Icons.ActualWidth - num);
		num += ((Icons.ActualWidth > 0.0) ? (Icons.ActualWidth + 2.0) : 0.0);
		if (_projectLabel != null)
		{
			double length = Math.Min((_tagPanel?.ActualWidth ?? 0.0) + 40.0, _itemContainer.Width - _projectLabel.ActualWidth - (_pomoSummary?.ActualWidth ?? 0.0) - 8.0);
			Canvas.SetLeft(_projectLabel, length);
		}
		if (_pomoSummary != null)
		{
			double length2 = Math.Min((_tagPanel?.ActualWidth ?? 0.0) + (_projectLabel?.ActualWidth ?? 0.0) + 40.0, _itemContainer.Width - _pomoSummary.ActualWidth - 8.0);
			Canvas.SetLeft(_pomoSummary, length2);
		}
		if (_tagPanel != null)
		{
			Canvas.SetLeft(_tagPanel, 36.0);
			double num2 = Math.Max(0.0, _itemContainer.Width - (_projectLabel?.ActualWidth ?? 0.0) - (_pomoSummary?.ActualWidth ?? 0.0) - 50.0);
			_tagPanel.MaxWidth = num2;
			int num3 = ListItemTagsControl.CalculateVisibleTagCount((from tag in DisplayItemModel.BuildTagModels(model.SourceViewModel.Tags, model.InKanban, model.InMatrix, int.MaxValue)
				select tag.Tag).ToList(), num2, _tagPanel.TagMaxWidth);
			if (_tagMaxCount != num3)
			{
				_tagMaxCount = num3;
				QueueDetailTagRefresh();
			}
			_tagPanel.Clip = null;
		}
		SetTitleWidth(num - 5.0);
		if (ContentText != null)
		{
			ContentText.Width = Math.Max(0.0, _itemContainer.Width - 44.0);
		}
	}

	private void QueueDetailTagRefresh()
	{
		if (_detailTagRefreshPending)
		{
			return;
		}
		_detailTagRefreshPending = true;
		base.Dispatcher.BeginInvoke((Action)delegate
		{
			_detailTagRefreshPending = false;
			if (_showDetail)
			{
				GetModel()?.SetDisplayTags(_tagMaxCount);
			}
		}, DispatcherPriority.Background);
	}

	private void SetLocked(bool locked)
	{
		if (_isLocked == locked)
		{
			return;
		}
		_isLocked = locked;
		foreach (UIElement child in base.Children)
		{
			if (!child.Equals(TitleContent))
			{
				child.IsHitTestVisible = !locked;
			}
		}
		TitleContent.TitleTextBox.IsHitTestVisible = !locked;
		TitleContent.CheckIcon.IsHitTestVisible = !locked;
	}

	public void SetPriority(int priority)
	{
		_controller?.SetPriority(priority);
	}

	public void SetDate(string key)
	{
		TitleContent.TryClearParseDate();
		_controller?.SetDate(key);
	}

	public void ClearDate()
	{
		TitleContent.TitleTextBox.SetCanParseDate(canParseDate: false);
		_controller?.ClearDate();
	}

	public void SelectDate(bool relative)
	{
		_controller?.SelectDate();
	}

	public async void ToggleTaskCompleted()
	{
		object dataContext = base.DataContext;
		if (dataContext is DisplayItemModel model)
		{
			TitleContent.RemoveFocus();
			bool soundPlayed = await TryPlayCompleteStory(model);
			_controller?.ToggleItemCompleted(model, soundPlayed);
		}
	}

	public void Delete()
	{
		TitleContent.RemoveFocus();
		_controller?.DeleteSelectedTasks();
	}

	public TaskListView GetParentList()
	{
		return _controller?.GetTaskListView();
	}

	public bool ParsingDate()
	{
		return TitleContent.TitleTextBox.ParsingDate;
	}

	public bool IsNewAdd()
	{
		if (base.DataContext is DisplayItemModel displayItemModel)
		{
			return displayItemModel.IsNewAdd;
		}
		return false;
	}

	public async void PinOrUnpinTask()
	{
		if (base.DataContext is DisplayItemModel displayItemModel)
		{
			string projectId = GetParentList()?.GetIdentity()?.CatId;
			string id = displayItemModel.Id;
			bool inDetail = displayItemModel.InDetail;
			await TaskService.TogglesStarred(id, projectId, null, null, inDetail);
		}
	}

	public void OpenSticky()
	{
		if (base.DataContext is DisplayItemModel displayItemModel)
		{
			TaskStickyWindow.ShowTaskSticky(new List<string> { displayItemModel.TaskId });
		}
	}

	private async void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		OnDataChanged(e.OldValue, e.NewValue);
	}

	private async void OnDataChanged(object oldValue, object newValue)
	{
		SetModel(oldValue, newValue);
		RemoveCompleteStory();
		if (newValue is DisplayItemModel displayItemModel && !string.IsNullOrEmpty(displayItemModel.Id))
		{
			SetStartLeft(displayItemModel);
			if (_controller == null)
			{
				_controller = new DisplayItemController(this, displayItemModel);
				Icons.SetController(_controller);
			}
			_showDetail = !displayItemModel.IsCountdown && !InMatrix && (LocalSettings.Settings.ShowDetails || LocalSettings.Settings.InSearch) && !displayItemModel.IsHabit;
			if (displayItemModel.DropHover)
			{
				SetResourceReference(Panel.BackgroundProperty, "BaseColorOpacity5");
			}
			else
			{
				base.Background = Brushes.Transparent;
			}
			_controller.Reset(this, displayItemModel);
			_itemContainer.Visibility = (displayItemModel.Dragging ? Visibility.Collapsed : Visibility.Visible);
			Icons.IsHitTestVisible = displayItemModel.Enable && !displayItemModel.IsCourse;
			if (InMatrix)
			{
				SetLocked(GetParentList()?.IsLocked ?? false);
			}
			TitleContent.TitleTextBox.IsHitTestVisible = displayItemModel.TitleHitVisible;
			ShowAvatar();
			InitContent(displayItemModel);
			SetContentText();
			TryGetSearchComment();
			ShowCreateTimeText();
			ShowBackground();
			SetTagAndProject(displayItemModel);
			ShowColorRect();
			SetShowDetailHeight();
			if (!displayItemModel.ShowReminder.HasValue)
			{
				TaskItemLoadHelper.LoadShowReminder(displayItemModel);
			}
		}
	}

	private void InitContent(DisplayItemModel model)
	{
		if (_showDetail && string.IsNullOrEmpty(model.Content))
		{
			if (model.IsTaskOrNote)
			{
				DisplayItemModel.AssembleModelContent(model);
			}
			else if (model.IsItem && model.SourceViewModel.OwnerTask != null)
			{
				model.Content = Utils.GetString("FromTask") + model.SourceViewModel.OwnerTask.Title;
			}
			else if (model.IsCourse)
			{
				model.Content = model.SourceViewModel.Content;
			}
		}
	}

	private async Task TryGetSearchComment()
	{
		DisplayItemModel model = GetModel();
		if (model == null)
		{
			return;
		}
		if (_showDetail && LocalSettings.Settings.InSearch && !string.IsNullOrEmpty(SearchHelper.SearchKey))
		{
			string text = string.Empty;
			List<CommentModel> taskComments = TaskCommentCache.GetTaskComments(model.TaskId);
			if (taskComments != null && taskComments.Count > 0)
			{
				foreach (CommentModel item in from c in taskComments
					orderby c.createdTime descending, c.modifiedTime descending
					select c)
				{
					Match match = SearchHelper.SearchRegex.Match(item.title?.ToLower() ?? string.Empty);
					if (item.title != null && match.Success)
					{
						text = ((match.Index > 8) ? ("..." + item.title.Substring(match.Index - 8).Replace("\r\n", " ").Replace("\r", " ")
							.Replace("\n", " ")) : item.title);
						break;
					}
				}
			}
			if (!string.IsNullOrEmpty(text))
			{
				if (_commentLabel == null)
				{
					_commentLabel = new CommentSearchLabel();
					_itemContainer.Children.Add(_commentLabel);
				}
				SetTaskContent(text, _commentLabel.CommentText);
				_commentLabel.Visibility = Visibility.Visible;
				Canvas.SetLeft(_commentLabel, 38.0);
				SetShowDetailHeight();
				model.SearchComment = true;
				return;
			}
		}
		model.SearchComment = false;
		if (_commentLabel != null)
		{
			_itemContainer.Children.Remove(_commentLabel);
			_commentLabel = null;
		}
	}

	private void SetShowDetailHeight()
	{
		DisplayItemModel model = GetModel();
		if (model == null)
		{
			return;
		}
		if (!_showDetail)
		{
			SetResourceReference(FrameworkElement.HeightProperty, "Height40");
			SetItemChildrenLeft();
			return;
		}
		int num = 40;
		int num2 = 4;
		if (!string.IsNullOrWhiteSpace(model.Content) && ContentText != null)
		{
			ContentText.SetResourceReference(Canvas.TopProperty, "ItemContentTop");
			num += 20;
			num2 += 2;
		}
		if (_noteTimeText != null)
		{
			_noteTimeText.SetResourceReference(Canvas.TopProperty, (num2 == 4) ? ("Height" + (num - 8)) : ("Double" + (num - 8) + "Add" + num2));
			num += 20;
		}
		if ((model.ShowTag && _tagPanel != null) || _projectLabel != null || _pomoSummary != null)
		{
			string name = ((num2 == 4) ? ("Height" + (num - 8)) : ("Double" + (num - 8) + "Add" + num2));
			_tagPanel?.SetResourceReference(Canvas.TopProperty, name);
			_projectLabel?.SetResourceReference(Canvas.TopProperty, name);
			_pomoSummary?.SetResourceReference(Canvas.TopProperty, name);
			num += 20;
			num2 += 2;
		}
		if (_commentLabel != null)
		{
			string name2 = ((num2 == 4) ? ("Height" + (num - 8)) : ("Double" + (num - 8) + "Add" + num2));
			_commentLabel.SetResourceReference(Canvas.TopProperty, name2);
			num += 20;
			num2 += 4;
		}
		SetResourceReference(FrameworkElement.HeightProperty, (num2 == 4) ? ("Height" + num) : ("Double" + num + "Add" + num2));
		SetItemChildrenLeft();
	}

	private void RemoveBinding()
	{
		DisplayItemModel model = GetModel();
		if (model != null)
		{
			PropertyChangedEventManager.RemoveHandler(model, OnModelPropertyChanged, string.Empty);
		}
		if (_tagPanel != null)
		{
			_itemContainer.Children.Remove(_tagPanel);
			_tagPanel.TagDelete -= OnTagDelete;
			_tagPanel = null;
		}
		if (_projectLabel != null)
		{
			_itemContainer.Children.Remove(_projectLabel);
			_projectLabel = null;
		}
		if (_pomoSummary != null)
		{
			_itemContainer.Children.Remove(_pomoSummary);
			_pomoSummary = null;
		}
		Icons.Children.Clear();
		_controller?.Clear();
		_controller = null;
		_unloaded = true;
	}

	private void ReloadData()
	{
		if (_unloaded)
		{
			DisplayItemModel model = GetModel();
			OnDataChanged(model, model);
			Icons.ResetIcons();
		}
	}

	private void SetModel(object oldVal, object newVal)
	{
		DisplayItemModel displayItemModel = oldVal as DisplayItemModel;
		DisplayItemModel displayItemModel2 = newVal as DisplayItemModel;
		displayItemModel2?.SetPropertyChangedEvent();
		if (displayItemModel != null)
		{
			PropertyChangedEventManager.RemoveHandler(displayItemModel, OnModelPropertyChanged, string.Empty);
		}
		if (displayItemModel2 != null)
		{
			PropertyChangedEventManager.AddHandler(displayItemModel2, OnModelPropertyChanged, string.Empty);
		}
	}

	private void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		base.Dispatcher.Invoke(delegate
		{
			if (!object.Equals(sender, base.DataContext))
			{
				PropertyChangedEventManager.RemoveHandler(sender as DisplayItemModel, OnModelPropertyChanged, string.Empty);
			}
			else
			{
				DisplayItemModel model = GetModel();
				if (model != null)
				{
					string propertyName = e.PropertyName;
					if (propertyName != null)
					{
						switch (propertyName.Length)
						{
						case 4:
							switch (propertyName[1])
							{
							default:
								return;
							case 'a':
								if (propertyName == "Tags")
								{
									model.SetDisplayTags(_tagMaxCount);
									SetTagPanel();
								}
								return;
							case 'y':
								break;
							}
							if (!(propertyName == "Type"))
							{
								break;
							}
							goto IL_050a;
						case 11:
							switch (propertyName[0])
							{
							default:
								return;
							case 'D':
								if (propertyName == "DisplayTags")
								{
									SetShowDetailHeight();
								}
								return;
							case 'S':
								if (propertyName == "ShowComment")
								{
									TryGetSearchComment();
									SetShowDetailHeight();
								}
								return;
							case 'I':
								break;
							}
							if (!(propertyName == "InOperation"))
							{
								break;
							}
							goto IL_046f;
						case 9:
							switch (propertyName[0])
							{
							case 'S':
								if (propertyName == "ShowIcons")
								{
									Icons.ResetIcons();
								}
								break;
							case 'D':
								if (propertyName == "DropHover")
								{
									if (model.DropHover)
									{
										SetResourceReference(Panel.BackgroundProperty, "BaseColorOpacity5");
									}
									else
									{
										base.Background = Brushes.Transparent;
									}
								}
								break;
							case 'A':
								if (propertyName == "AvatarUrl")
								{
									ShowAvatar();
									SetItemsLeft();
								}
								break;
							}
							break;
						case 8:
							switch (propertyName[1])
							{
							default:
								return;
							case 'h':
								if (propertyName == "ShowPomo")
								{
									SetPomoSummary();
									SetShowDetailHeight();
								}
								return;
							case 'r':
								if (propertyName == "Dragging")
								{
									SetStartLeft(model);
									_itemContainer.Visibility = (model.Dragging ? Visibility.Collapsed : Visibility.Visible);
									ShowOrHideMoreIcon(_itemContainer.IsMouseOver);
									ShowBackground();
								}
								return;
							case 'e':
								break;
							}
							if (!(propertyName == "Selected"))
							{
								break;
							}
							goto IL_046f;
						case 5:
							switch (propertyName[0])
							{
							case 'T':
								if (propertyName == "Title")
								{
									OnTaskTitleChanged();
								}
								break;
							case 'L':
								if (propertyName == "Level")
								{
									SetStartLeft(model);
								}
								break;
							}
							break;
						case 6:
							switch (propertyName[0])
							{
							default:
								return;
							case 'S':
								if (propertyName == "Status")
								{
									double opacity = ((model.Status == 0) ? 1.0 : 0.4);
									if (_tagPanel != null)
									{
										_tagPanel.Opacity = opacity;
									}
									if (_projectLabel != null)
									{
										_projectLabel.Opacity = opacity;
									}
									if (_avatar != null)
									{
										_avatar.Opacity = opacity;
									}
								}
								return;
							case 'E':
								break;
							}
							if (!(propertyName == "Enable"))
							{
								break;
							}
							goto IL_050a;
						case 7:
							if (propertyName == "Content")
							{
								SetContentText();
								SetShowDetailHeight();
							}
							break;
						case 15:
							if (propertyName == "TitleHitVisible")
							{
								TitleContent.TitleTextBox.IsHitTestVisible = model.TitleHitVisible;
							}
							break;
						case 13:
							if (!(propertyName == "ShowTopMargin"))
							{
								break;
							}
							goto IL_033c;
						case 16:
							if (!(propertyName == "ShowBottomMargin"))
							{
								break;
							}
							goto IL_033c;
						case 10:
						case 12:
						case 14:
							break;
							IL_033c:
							if (_backBorder != null && _backBorder.IsVisible)
							{
								CornerRadius cornerRadius = new CornerRadius(10.0);
								if (!model.ShowTopMargin)
								{
									double topLeft = (cornerRadius.TopRight = 0.0);
									cornerRadius.TopLeft = topLeft;
								}
								if (!model.ShowBottomMargin)
								{
									double topLeft = (cornerRadius.BottomRight = 0.0);
									cornerRadius.BottomLeft = topLeft;
								}
								_backBorder.CornerRadius = cornerRadius;
							}
							break;
							IL_046f:
							ShowBackground();
							break;
							IL_050a:
							Icons.IsHitTestVisible = model.Enable && !model.IsCourse;
							break;
						}
					}
				}
			}
		});
	}

	private void SetStartLeft(DisplayItemModel model)
	{
		_startLeft = model.Level * 20 + (model.Dragging ? 10 : 0);
		Canvas.SetLeft(_itemContainer, _startLeft + 18);
		_itemContainer.Width = Math.Max(0.0, base.ActualWidth - (double)_startLeft - 36.0);
		if (_backBorder != null)
		{
			_backBorder.Width = Math.Max(0.0, base.ActualWidth - (double)_startLeft - 36.0);
			Canvas.SetLeft(_backBorder, _startLeft + 18);
		}
		SetItemsLeft();
		ShowOrHideDragBar(_itemContainer.IsMouseOver);
	}

	private void OnTaskTitleChanged()
	{
		if (base.DataContext is DisplayItemModel displayItemModel && !TitleContent.TitleTextBox.KeyboardFocused && displayItemModel.Title != TitleContent.TitleTextBox.Text)
		{
			TitleContent.SetHintText(string.IsNullOrEmpty(displayItemModel.Title));
			TitleContent.TitleTextBox.SetTextOffset(displayItemModel.Title, restoreOffset: true, clearUndo: true);
		}
	}

	private void SetTagAndProject(DisplayItemModel model)
	{
		model.SetDisplayTags(_tagMaxCount);
		SetProjectLabel();
		SetTagPanel();
		SetPomoSummary();
	}

	private void SetProjectLabel()
	{
		DisplayItemModel model = GetModel();
		if (model != null && model.ShowProject)
		{
			if (_projectLabel == null)
			{
				_projectLabel = new ListItemProjectLabel();
				_projectLabel.SizeChanged += delegate(object o, SizeChangedEventArgs e)
				{
					if (e.NewSize.Width != e.PreviousSize.Width)
					{
						base.Dispatcher.BeginInvoke((Action)delegate
						{
							SetItemsLeft();
						}, DispatcherPriority.Background);
					}
				};
				_itemContainer.Children.Insert(1, _projectLabel);
			}
			_projectLabel.Opacity = ((model.Status == 0) ? 1.0 : 0.4);
			_projectLabel.SetResourceReference(Canvas.TopProperty, (!_showDetail) ? "Double8_12_13" : "Height38");
			_projectLabel.IsHitTestVisible = !model.IsCourse;
			_projectLabel.Visibility = Visibility.Visible;
		}
		else if (_projectLabel != null)
		{
			_itemContainer.Children.Remove(_projectLabel);
			_projectLabel = null;
		}
	}

	private void SetPomoSummary()
	{
		DisplayItemModel model = GetModel();
		if (_showDetail && model != null && model.ShowPomo)
		{
			if (_pomoSummary == null)
			{
				_pomoSummary = new TaskItemPomoSummaryControl
				{
					Margin = new Thickness(6.0, 4.0, 0.0, 0.0)
				};
				_pomoSummary.SizeChanged += delegate(object o, SizeChangedEventArgs e)
				{
					if (e.NewSize.Width != e.PreviousSize.Width)
					{
						base.Dispatcher.BeginInvoke(new Action(SetItemsLeft), DispatcherPriority.Background);
					}
				};
				_itemContainer.Children.Add(_pomoSummary);
			}
			_pomoSummary.SetData(model.PomoSummary);
		}
		else if (_pomoSummary != null)
		{
			_itemContainer.Children.Remove(_pomoSummary);
			_pomoSummary = null;
		}
	}

	private void SetTagPanel()
	{
		DisplayItemModel model = GetModel();
		if (model != null && model.ShowTag)
		{
			if (_tagPanel == null)
			{
				_tagPanel = new ListItemTagsControl();
				_tagPanel.TagDelete += OnTagDelete;
				_tagPanel.SizeChanged += delegate(object o, SizeChangedEventArgs e)
				{
					if (e.NewSize.Width != e.PreviousSize.Width)
					{
						base.Dispatcher.BeginInvoke((Action)delegate
						{
							SetItemsLeft();
						}, DispatcherPriority.Background);
					}
				};
				_itemContainer.Children.Add(_tagPanel);
			}
			_tagPanel.Opacity = ((model.Status == 0) ? 1.0 : 0.4);
			_tagPanel.TagMaxWidth = (_showDetail ? 168 : 72);
			_tagPanel.SetResourceReference(Canvas.TopProperty, (!_showDetail) ? "Double9_12_13" : "Height38");
			_tagPanel.Visibility = Visibility.Visible;
		}
		else if (_tagPanel != null)
		{
			_itemContainer.Children.Remove(_tagPanel);
			_tagPanel.TagDelete -= OnTagDelete;
			_tagPanel = null;
		}
		SetShowDetailHeight();
	}

	private async void OnItemClick(object sender, MouseButtonEventArgs e)
	{
		DisplayItemModel model = base.DataContext as DisplayItemModel;
		if (_isLocked || model == null || !model.HitVisible || TitleContent.IsIconMouseOver || (_moreBorder?.IsMouseOver ?? false))
		{
			return;
		}
		bool num = !Icons.IsMouseOver && !(_projectLabel?.IsMouseOver ?? false) && !(_tagPanel?.IsMouseOver ?? false) && !(_avatar?.IsMouseOver ?? false);
		ListViewContainer listViewContainer = Utils.FindParent<ListViewContainer>(this);
		listViewContainer?.AddItemClickEvent();
		bool flag = false;
		if (listViewContainer != null)
		{
			flag = !(listViewContainer?.CanShowDetailPage() ?? true);
		}
		else
		{
			TaskListWindow taskListWindow = Utils.FindParent<TaskListWindow>(this);
			if (taskListWindow != null)
			{
				flag = taskListWindow.CanShowPopup();
			}
		}
		if (!num || !(_mouseDown || model.Selected || InMatrix || flag))
		{
			return;
		}
		_mouseDown = false;
		bool flag2 = Utils.IfCtrlPressed() || Utils.IfShiftPressed();
		if (InList || flag2)
		{
			SelectItem(model, !flag2 && !flag);
			if (flag2)
			{
				return;
			}
		}
		double totalMilliseconds = (DateTime.Now - _lastClickTime).TotalMilliseconds;
		if (totalMilliseconds < 2000.0)
		{
			UtilLog.Info("ListItem.OnItemClick : " + totalMilliseconds);
		}
		bool firstClick = totalMilliseconds > 400.0;
		_lastClickTime = DateTime.Now;
		_isWaitingDoubleClick = false;
		if (firstClick)
		{
			_isWaitingDoubleClick = true;
			model.Selected = true;
			if (InMatrix || flag)
			{
				await ShowItemDetailWindow(model);
			}
		}
		if (!firstClick && TitleContent.TitleTextBox.SelectionLength == 0)
		{
			model.Selected = !InMatrix;
			TaskDetailWindows.ShowTaskWindows(model.TaskId);
		}
	}

	public async Task ShowItemDetailWindow(DisplayItemModel model, bool focusTitle = false)
	{
		if (!_isLocked)
		{
			await _controller.ShowDetailWindow(model, focusTitle);
		}
	}

	private async void SelectItem(DisplayItemModel model, bool canFocus = true)
	{
		_controller?.SelectItem();
		if (!model.InMatrix && canFocus && !TitleContent.TitleTextBox.CurrentFocused)
		{
			await Task.Delay(5);
			TitleContent.TitleTextBox.FocusEnd();
		}
	}

	private void OnRightClick(object sender, MouseButtonEventArgs e)
	{
		if (!_isLocked)
		{
			_controller?.ShowOperationDialogSafely();
			e.Handled = true;
		}
	}

	private void OnTagDelete(object sender, string tag)
	{
		DisplayItemModel model = GetModel();
		if (model != null && model.Enable)
		{
			_controller?.OnTagDelete(tag.ToLower());
		}
	}

	private void OperationClick(object sender, MouseButtonEventArgs e)
	{
		Mouse.Capture(null);
		_controller?.ShowOperationDialogSafely();
	}

	private void OnAvatarMouseUp(object sender, MouseEventArgs e)
	{
		e.Handled = true;
		Mouse.Capture(null);
		_controller?.ShowSelectAssignPopup();
	}

	private void NavigateTaskClick(object sender, RoutedEventArgs e)
	{
		_controller?.NavigateTaskClick();
	}

	private void OnDragMouseDown(object sender, MouseButtonEventArgs e)
	{
		_startDrag = true;
	}

	private void OnItemMouseMove(object sender, MouseEventArgs e)
	{
		if (_isLocked || TitleContent.TitleTextBox.SelectionPopupOpened)
		{
			return;
		}
		if (base.DataContext is DisplayItemModel displayItemModel && (!displayItemModel.ShowDragBar || !displayItemModel.Enable || displayItemModel.IsHabit || displayItemModel.IsCountdown || displayItemModel.IsSection))
		{
			_startDrag = false;
			return;
		}
		if (!TitleContent.TitleTextBox.CurrentFocused && _startDrag && e.LeftButton == MouseButtonState.Pressed)
		{
			Point position = e.GetPosition(this);
			if (!(Math.Abs(position.X - _startPoint.X) > 5.0) && !(Math.Abs(position.Y - _startPoint.Y) > 2.0))
			{
				return;
			}
			_startDrag = false;
			_controller?.OnDragMouseDown(sender, e);
		}
		_startDrag = false;
	}

	private void OnMouseMove(object sender, MouseEventArgs e)
	{
		if (!_isLocked && _startDrag && e.LeftButton == MouseButtonState.Pressed)
		{
			_controller?.OnDragMouseDown(sender, e);
		}
		_startDrag = false;
	}

	private void OnMouseDown(object sender, MouseButtonEventArgs e)
	{
		if ((_dragBorder != null && _dragBorder.IsMouseOver) || TitleContent.IsIconMouseOver)
		{
			return;
		}
		Rectangle avatar = _avatar;
		if ((avatar == null || !avatar.IsMouseOver) && (_moreBorder == null || !_moreBorder.IsMouseOver))
		{
			if (!Utils.IfCtrlPressed() && !Utils.IfShiftPressed())
			{
				_startPoint = e.GetPosition(this);
				_startDrag = true;
			}
			_mouseDown = true;
		}
	}

	private void OnDragMouseUp(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
	}

	private void SetContentText()
	{
		if (!_showDetail)
		{
			if (ContentText != null)
			{
				_itemContainer.Children.Remove(ContentText);
				ContentText = null;
			}
			return;
		}
		if (InMatrix || !LocalSettings.Settings.ShowDetails || !(base.DataContext is DisplayItemModel { IsHabit: false } displayItemModel) || string.IsNullOrWhiteSpace(displayItemModel.Content))
		{
			if (ContentText != null)
			{
				ContentText.Inlines.Clear();
				ContentText.Visibility = Visibility.Collapsed;
			}
			return;
		}
		if (ContentText == null)
		{
			ContentText = new TextBlock();
			ContentText.SetResourceReference(TextBlock.FontSizeProperty, "Font12");
			ContentText.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100_80");
			ContentText.Opacity = 0.4;
			ContentText.TextWrapping = TextWrapping.NoWrap;
			ContentText.TextTrimming = TextTrimming.CharacterEllipsis;
			_itemContainer.Children.Add(ContentText);
			Canvas.SetLeft(ContentText, 42.0);
		}
		ContentText.Visibility = Visibility.Visible;
		SetTaskContent(displayItemModel.Content, ContentText);
	}

	private void SetTaskContent(string content, TextBlock block)
	{
		block.Inlines.Clear();
		string contentDisplayText = TaskUtils.GetContentDisplayText(content ?? string.Empty);
		if (LocalSettings.Settings.InSearch && !string.IsNullOrEmpty(SearchHelper.SearchKey) && !string.IsNullOrEmpty(content) && !InPreview)
		{
			contentDisplayText = ((contentDisplayText.Length > 400) ? contentDisplayText.Substring(0, 399) : contentDisplayText);
			MatchCollection matchCollection = SearchHelper.SearchRegex.Matches(contentDisplayText.ToLower());
			if (matchCollection.Count > 0)
			{
				for (int i = 0; i < matchCollection.Count; i++)
				{
					int num = ((i > 0) ? (matchCollection[i - 1].Index + matchCollection[i - 1].Length) : 0);
					string text = contentDisplayText.Substring(num, matchCollection[i].Index - num);
					block.Inlines.Add(text);
					block.Inlines.Add(new Run(contentDisplayText.Substring(matchCollection[i].Index, matchCollection[i].Length))
					{
						Background = SearchHelper.GetSearchHighlightColor()
					});
				}
				int num2 = matchCollection[matchCollection.Count - 1].Index + matchCollection[matchCollection.Count - 1].Length;
				string text2 = contentDisplayText.Substring(num2, contentDisplayText.Length - num2);
				block.Inlines.Add(text2);
			}
			else
			{
				block.Inlines.Add(contentDisplayText);
			}
		}
		else
		{
			contentDisplayText = ((contentDisplayText.Length > 400) ? contentDisplayText.Substring(0, 399) : contentDisplayText);
			block.Inlines.Add(contentDisplayText);
		}
		block.Visibility = Visibility.Visible;
	}

	public async void ShowDetailWindow(bool focus, bool fromUpDown = false)
	{
		if (base.DataContext is DisplayItemModel model && _controller != null)
		{
			await _controller.ShowDetailWindow(model, focus, fromUpDown);
		}
	}

	private void OnMouseEnter(object sender, MouseEventArgs e)
	{
		ShowOrHideDragBar(show: true);
		ShowOrHideMoreIcon(show: true);
	}

	private void OnMouseLeave(object sender, MouseEventArgs e)
	{
		ShowOrHideDragBar(show: false);
		ShowOrHideMoreIcon(show: false);
	}

	public Point GetClickPoint()
	{
		return _firstClickPoint;
	}

	public async Task<bool> TryPlayCompleteStory(DisplayItemModel model)
	{
		if (InMatrix || (!model.IsTask && !model.IsItem))
		{
			return false;
		}
		bool flag = model.Status == 0 && _controller != null;
		string text = default(string);
		if (flag)
		{
			text = await _controller.GetCompleteStoryText(model);
			flag = text != null;
		}
		if (flag)
		{
			await PlayCompleteStory(text);
			return true;
		}
		return false;
	}

	private async Task PlayCompleteStory(string text)
	{
		bool flag = true;
		if (text == Utils.GetString("FirstTaskDone"))
		{
			flag = LocalSettings.Settings.ExtraSettings.CpltStoryTimes < 2;
			LocalSettings.Settings.ExtraSettings.CpltStoryTimes++;
		}
		double num = ((double)FindResource("Height40") - 40.0) / 2.0;
		_completeStoryView = new CompleteStory();
		base.Children.Add(_completeStoryView);
		Canvas.SetLeft(_completeStoryView, _startLeft + 19);
		Canvas.SetTop(_completeStoryView, num);
		DoubleAnimation animation = new DoubleAnimation
		{
			Duration = new Duration(TimeSpan.FromMilliseconds(300.0)),
			From = -40.0,
			To = 0.0
		};
		_csTextBlock = new TextBlock
		{
			Text = text,
			FontSize = 14.0
		};
		_csTextBlock.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100");
		TranslateTransform translateTransform = new TranslateTransform
		{
			Y = -40.0
		};
		_csTextBlock.RenderTransform = translateTransform;
		base.Children.Add(_csTextBlock);
		Canvas.SetLeft(_csTextBlock, _startLeft + 56);
		Canvas.SetTop(_csTextBlock, num + 11.0);
		DoubleAnimation animation2 = new DoubleAnimation
		{
			Duration = new Duration(TimeSpan.FromMilliseconds(300.0)),
			From = 0.0,
			To = Math.Max(0.0, base.ActualWidth - (double)_startLeft - 24.0)
		};
		_csBackBorder = new Border
		{
			Width = 0.0,
			Height = base.ActualHeight,
			Opacity = 0.2
		};
		_csBackBorder.SetResourceReference(Panel.BackgroundProperty, "TickYellow");
		Canvas.SetLeft(_csBackBorder, _startLeft + 12);
		base.Children.Add(_csBackBorder);
		TranslateTransform translateTransform2 = new TranslateTransform();
		_itemContainer.RenderTransform = translateTransform2;
		DoubleAnimation animation3 = new DoubleAnimation
		{
			Duration = new Duration(TimeSpan.FromMilliseconds(300.0)),
			From = 0.0,
			To = _itemContainer.ActualHeight
		};
		Utils.PlayCompletionSound();
		translateTransform2.BeginAnimation(TranslateTransform.YProperty, animation3);
		if (flag)
		{
			translateTransform.BeginAnimation(TranslateTransform.YProperty, animation);
		}
		_csBackBorder.BeginAnimation(FrameworkElement.WidthProperty, animation2);
		_completeStoryView.PlayStory();
		await Task.Delay(flag ? 900 : 500);
		RemoveCompleteStory();
	}

	private async Task RemoveCompleteStory()
	{
		if (_csTextBlock != null)
		{
			base.Children.Remove(_completeStoryView);
			base.Children.Remove(_csTextBlock);
			base.Children.Remove(_csBackBorder);
			_itemContainer.RenderTransform = null;
			_completeStoryView = null;
			_csTextBlock = null;
			_csBackBorder = null;
		}
	}

	public void OnDetailKeyUp(Key key)
	{
		switch (key)
		{
		case Key.Up:
			_controller.MoveUp(fromDetail: true);
			break;
		case Key.Down:
			_controller.MoveDown(fromDetail: true);
			break;
		}
	}
}
