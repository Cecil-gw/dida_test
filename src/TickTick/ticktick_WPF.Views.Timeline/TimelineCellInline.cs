using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Emoji.Wpf;
using ticktick_WPF.Cache;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Detail;

namespace ticktick_WPF.Views.Timeline;

public class TimelineCellInline : TimelineCellBase
{
	private Path _icon;

	private Ellipse _avatar;

	private Grid _contentPanel;

	private Border _border;

	private Rectangle _rect;

	private bool _inline = true;

	private Grid _infoTile;

	private EmjTextBlock _emjEmjText;

	private int _dragStatus;

	private Point? _point;

	private DoubleAnimation _leftAni;

	private DoubleAnimation _topAni;

	private bool _topAnimating;

	private TaskCircleProgress _progress;

	private async void LoadStyle()
	{
		base.Margin = new Thickness(0.0, 3.0, 0.0, 0.0);
		_infoTile = new Grid();
		base.Content = _infoTile;
		_infoTile.Margin = new Thickness(2.0, 3.0, 2.0, 3.0);
		_infoTile.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		_infoTile.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		_infoTile.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		_infoTile.SetBinding(FrameworkElement.WidthProperty, new Binding("Width"));
		_border = new Border();
		_border.SetResourceReference(FrameworkElement.StyleProperty, "TimeLineInlineBorder");
		_rect = new Rectangle();
		_rect.SetResourceReference(FrameworkElement.StyleProperty, "TimeLineItemRectStyle");
		_border.Child = _rect;
		_infoTile.Children.Add(_border);
		_border.MouseEnter += OnMouseEnter;
		_border.MouseLeave += OnMouseLeave;
		_contentPanel = new Grid();
		_contentPanel.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		_contentPanel.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		_contentPanel.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		_contentPanel.SetResourceReference(FrameworkElement.StyleProperty, "TimeLineItemStackPanelStyle");
		_infoTile.Children.Add(_contentPanel);
		_emjEmjText = new EmjTextBlock
		{
			IsHitTestVisible = false
		};
		_emjEmjText.SetResourceReference(FrameworkElement.StyleProperty, "TimeLineItemEmjTextStyle");
		_contentPanel.Children.Add(_emjEmjText);
		_contentPanel.MouseLeftButtonDown += OnTextMouseDown;
		_contentPanel.MouseLeftButtonUp += OnTextMouseUp;
		_contentPanel.MouseRightButtonUp += OnRightClick;
		_contentPanel.MouseEnter += OnTextMouseEnter;
		_contentPanel.MouseLeave += OnTextMouseLeave;
	}

	private void OnRightClick(object sender, MouseButtonEventArgs e)
	{
		TimelineCellViewModel timelineCellViewModel = cellModel;
		if (timelineCellViewModel != null)
		{
			if (timelineCellViewModel.DisplayModel.IsTaskOrNote && !timelineCellViewModel.Editable)
			{
				base.Container?.TryToastString(Utils.GetString("NoEditingPermission"));
			}
			else if (timelineCellViewModel.DisplayModel.Type == DisplayType.CheckItem)
			{
				base.Container?.TryToastString(Utils.GetString("OptionNotSupport"));
			}
			else
			{
				base.Container?.ShowMenuPopup(timelineCellViewModel);
			}
		}
	}

	public TimelineCellInline()
	{
		base.DataContextChanged += OnTimelineCellDataContextChanged;
		LoadStyle();
		SetBinding(UIElement.VisibilityProperty, new Binding("Visibility"));
		SetBinding(FrameworkElement.HeightProperty, new Binding("Parent.OneLineHeight"));
		SetBinding(FrameworkElement.MaxWidthProperty, new Binding("MaxWidth"));
		SetBinding(UIElement.IsHitTestVisibleProperty, StaticPropertiesModel.HitTestBinding);
		base.ClipToBounds = true;
	}

	private async void OnTimelineCellDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		bool oldInline = _border != null && (int)_border.GetValue(Grid.ColumnProperty) == 1;
		if (e.OldValue is TimelineCellViewModel source)
		{
			PropertyChangedEventManager.RemoveHandler(source, OnModelPropertyChanged, string.Empty);
		}
		bool newInline = oldInline;
		if (e.NewValue is TimelineCellViewModel newVm)
		{
			newInline = newVm.Inline;
			CheckIcon(newVm);
			CheckTextProgressAndAvatar(newVm);
			PropertyChangedEventManager.AddHandler(newVm, OnModelPropertyChanged, string.Empty);
			double left = newVm.Left;
			if (!double.IsInfinity(left))
			{
				SetValue(Canvas.LeftProperty, left);
			}
			if (newVm.Top >= 0.0)
			{
				BeginAnimation(Canvas.TopProperty, null);
				_topAnimating = false;
				SetValue(Canvas.TopProperty, newVm.Top);
			}
			SetValue(Panel.ZIndexProperty, 10);
			SetBatchDragStyle(newVm);
			if (newVm.IsNew)
			{
				UserActCollectUtils.AddClickEvent("timeline", "task_action", "add");
				TaskDetailPopup popup = new TaskDetailPopup();
				SetValue(Panel.ZIndexProperty, 100);
				popup.TaskSaved -= OnTaskSaved;
				popup.TaskSaved += OnTaskSaved;
				popup.Disappear -= OnNewWindowDisappear;
				popup.Disappear += OnNewWindowDisappear;
				TaskDetailViewModel taskDetailViewModel = new TaskDetailViewModel(newVm.DisplayModel)
				{
					IsNewAdd = true,
					Reminders = (newVm.IsAllDay ? TimeData.GetDefaultAllDayReminders().ToArray() : TimeData.GetDefaultTimeReminders().ToArray())
				};
				await Task.Delay(50);
				popup.Show(taskDetailViewModel, string.Empty, new TaskWindowDisplayArgs(this, 3.0, default(Point), focus: false, base.ActualHeight + 3.0));
				popup.FocusTitle();
				newVm.Operation = newVm.Operation.Add(TimelineCellOperation.Edit);
				base.timelineModel.Editing = true;
			}
		}
		if (oldInline != newInline && _border != null)
		{
			_border.SetValue(Grid.ColumnProperty, newInline ? 1 : 0);
			_border.SetValue(Grid.ColumnSpanProperty, (!newInline) ? 1 : 2);
			_infoTile.SetBinding(FrameworkElement.WidthProperty, newInline ? new Binding("Width") : new Binding());
		}
		_inline = newInline;
	}

	private void CheckTextProgressAndAvatar(TimelineCellViewModel model)
	{
		if (model == null)
		{
			return;
		}
		int num = (model.Inline ? 40 : 50);
		int num2 = (model.Inline ? 70 : 80);
		double num3 = (model.Inline ? 14.0 : (base.timelineModel.OneDayWidth + 2.0));
		double num4 = (model.Inline ? model.Width : model.MaxWidth);
		model.CheckAvatarUrl();
		bool flag = model.AvatarUrl != "-1" && !string.IsNullOrEmpty(model.AvatarUrl);
		bool flag2 = model.Progress > 0 && model.Status == 0;
		string title = model.Title;
		if (title != null && title.Length < 10)
		{
			double width = Utils.MeasureString(model.Title, new Typeface(Utils.GetDefaultFontFamily(), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), 12.0).Width;
			if (flag2 && num4 <= (double)num)
			{
				flag2 = num4 - num3 - width >= 16.0;
			}
			if (flag && num4 <= (double)num2)
			{
				flag = num4 - num3 - width - (double)(flag2 ? 16 : 0) >= 22.0;
			}
		}
		else
		{
			flag2 = flag2 && num4 > (double)num;
			flag = flag && num4 > (double)num2;
		}
		num3 += (double)(flag2 ? 16 : 0);
		num3 += (double)(flag ? 22 : 0);
		if (!double.IsPositiveInfinity(num4))
		{
			num4 -= num3;
			if (num4 < 6.0)
			{
				num4 = 0.0;
			}
		}
		num4 = ((!model.Inline) ? Math.Min(num4, 120.0) : num4);
		_emjEmjText.MaxWidth = (double.IsNaN(num4) ? 100.0 : num4);
		_emjEmjText.TextTrimming = ((!model.Inline) ? TextTrimming.CharacterEllipsis : TextTrimming.None);
		CheckProgress(model, flag2);
		CheckAvatar(model, flag);
	}

	private void SetBatchDragStyle(TimelineCellViewModel model)
	{
		if (_dragStatus == model.DragStatus)
		{
			return;
		}
		switch (model.DragStatus)
		{
		case 2:
			base.Opacity = 0.0;
			break;
		case 1:
			base.Opacity = 1.0;
			_rect.Fill = ThemeUtil.GetColor("BaseColorOpacity5");
			_rect.StrokeThickness = 0.0;
			_contentPanel.Visibility = Visibility.Collapsed;
			if (_avatar != null)
			{
				_avatar.Visibility = Visibility.Collapsed;
			}
			_border.Background = Brushes.Transparent;
			SetValue(Panel.ZIndexProperty, 0);
			break;
		case 0:
			base.Opacity = 1.0;
			_rect.SetBinding(Shape.FillProperty, "BackgroundBrush");
			_rect.SetBinding(Shape.StrokeThicknessProperty, "BorderThickness");
			_border.SetResourceReference(Control.BackgroundProperty, "TimelineContainerBackground");
			_contentPanel.Visibility = Visibility.Visible;
			if (_avatar != null)
			{
				_avatar.Visibility = Visibility.Visible;
			}
			SetValue(Panel.ZIndexProperty, 20);
			break;
		}
		_dragStatus = model.DragStatus;
	}

	private void CheckIcon(TimelineCellViewModel model)
	{
		if (model.Icon != null && _icon == null)
		{
			_icon = new Path();
			_icon.SetResourceReference(FrameworkElement.StyleProperty, "TimeLineItemIconStyle");
			_contentPanel?.Children.Insert(0, _icon);
		}
		if (model.Icon == null && _icon != null)
		{
			_contentPanel?.Children.Remove(_icon);
			_icon = null;
		}
	}

	private void CheckProgress(TimelineCellViewModel model, bool showProgress)
	{
		if (showProgress && _progress == null)
		{
			_progress = new TaskCircleProgress
			{
				Percentage = model.Progress,
				Radius = 4.5,
				StrokeThickness = 1.0,
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(2.0, 0.0, -2.0, -1.0),
				IsHitTestVisible = false,
				Opacity = 0.4
			};
			_progress.SetValue(Grid.ColumnProperty, 2);
			_progress.SetBinding(TaskCircleProgress.PercentageProperty, "Progress");
			_progress.SetBinding(TaskCircleProgress.SegmentColorProperty, "ForegroundBrush");
			_contentPanel?.Children.Add(_progress);
		}
		if (!showProgress && _progress != null)
		{
			_contentPanel?.Children.Remove(_progress);
			_progress = null;
		}
	}

	private void CheckAvatar(TimelineCellViewModel model, bool showAvatar)
	{
		if (showAvatar && _avatar == null)
		{
			_avatar = new Ellipse();
			_avatar.SetResourceReference(FrameworkElement.StyleProperty, "TimeLineItemAvatarStyle");
			_infoTile.Children.Add(_avatar);
		}
		if (!showAvatar && _avatar != null)
		{
			_infoTile.Children.Remove(_avatar);
			_avatar = null;
		}
	}

	private void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (sender != cellModel)
		{
			return;
		}
		TimelineCellViewModel timelineCellViewModel = cellModel;
		string propertyName = e.PropertyName;
		if (propertyName == null)
		{
			return;
		}
		switch (propertyName.Length)
		{
		case 4:
			switch (propertyName[0])
			{
			case 'L':
				if (propertyName == "Left" && !double.IsInfinity(timelineCellViewModel.Left))
				{
					SetValue(Canvas.LeftProperty, timelineCellViewModel.Left);
				}
				break;
			case 'I':
				if (propertyName == "Icon")
				{
					CheckIcon(timelineCellViewModel);
				}
				break;
			}
			break;
		case 8:
			switch (propertyName[0])
			{
			default:
				return;
			case 'P':
				if (propertyName == "Progress")
				{
					CheckTextProgressAndAvatar(timelineCellViewModel);
				}
				return;
			case 'M':
				break;
			}
			if (!(propertyName == "MaxWidth"))
			{
				break;
			}
			goto IL_0375;
		case 3:
		{
			if (!(propertyName == "Top"))
			{
				break;
			}
			double top = timelineCellViewModel.Top;
			if (_topAnimating && Math.Abs((_topAni?.To).GetValueOrDefault() - top) < 0.1)
			{
				break;
			}
			double num = (double)GetValue(Canvas.TopProperty);
			if (!timelineCellViewModel.BatchSelected && top >= 0.0 && num >= 0.0 && Math.Abs(top - num) > 0.1)
			{
				if (_topAni == null)
				{
					_topAni = new DoubleAnimation
					{
						Duration = TimeSpan.FromMilliseconds(200.0),
						EasingFunction = new CubicEase
						{
							EasingMode = EasingMode.EaseOut
						}
					};
					_topAni.Completed += delegate
					{
						_topAnimating = false;
					};
				}
				_topAni.From = num;
				_topAni.To = top;
				_topAnimating = true;
				BeginAnimation(Canvas.TopProperty, _topAni);
			}
			else if (timelineCellViewModel.Top >= 0.0)
			{
				BeginAnimation(Canvas.TopProperty, null);
				SetValue(Canvas.TopProperty, timelineCellViewModel.Top);
			}
			break;
		}
		case 9:
			if (propertyName == "AvatarUrl")
			{
				CheckTextProgressAndAvatar(timelineCellViewModel);
			}
			break;
		case 6:
			if (propertyName == "Inline")
			{
				_border.SetValue(Grid.ColumnProperty, timelineCellViewModel.Inline ? 1 : 0);
				_border.SetValue(Grid.ColumnSpanProperty, (!timelineCellViewModel.Inline) ? 1 : 2);
				_infoTile.SetBinding(FrameworkElement.WidthProperty, timelineCellViewModel.Inline ? new Binding("Width") : new Binding());
				_inline = timelineCellViewModel.Inline;
				CheckTextProgressAndAvatar(timelineCellViewModel);
			}
			break;
		case 13:
			if (propertyName == "BatchSelected")
			{
				SetValue(Panel.ZIndexProperty, timelineCellViewModel.BatchSelected ? 20 : 10);
			}
			break;
		case 10:
			if (propertyName == "DragStatus")
			{
				SetBatchDragStyle(timelineCellViewModel);
			}
			break;
		case 5:
			if (!(propertyName == "Width"))
			{
				break;
			}
			goto IL_0375;
		case 7:
		case 11:
		case 12:
			break;
			IL_0375:
			CheckTextProgressAndAvatar(timelineCellViewModel);
			break;
		}
	}

	private async void OnNewWindowDisappear(object sender, string e)
	{
		if (sender is TaskDetailPopup taskDetailPopup)
		{
			taskDetailPopup.Disappear -= OnNewWindowDisappear;
			taskDetailPopup.TaskSaved -= OnTaskSaved;
		}
		SetValue(Panel.ZIndexProperty, 0);
		TimelineCellViewModel model = cellModel;
		if (ModelInvalid())
		{
			base.timelineModel.Editing = false;
			return;
		}
		model.Operation = model.Operation.Remove(TimelineCellOperation.Edit);
		if (model.IsNew)
		{
			base.timelineModel.RemoveCell(model);
			TaskCache.DeleteTask(model.Id);
		}
		else
		{
			TaskBaseViewModel taskById = TaskCache.GetTaskById(model.Id);
			model.ResetBaseVm(taskById);
			base.timelineModel.UpdateCellTime(model);
			if (base.timelineModel.GroupByEnum != Constants.SortType.sortOrder)
			{
				await base.timelineModel.UpdateGroupAsync();
			}
			await base.timelineModel.UpdateCellLineAsync();
			if (!base.timelineModel.CheckTaskMatched(model.Id))
			{
				base.Container?.TryToastString(Utils.GetString("TaskHasBeenFiltered"));
			}
		}
		base.timelineModel.Editing = false;
	}

	private void OnTaskSaved(object sender, string e)
	{
		if (cellModel != null)
		{
			cellModel.IsNew = false;
		}
	}

	private void OnMouseLeave(object sender, MouseEventArgs e)
	{
		OnCellMouseLeave(sender, e);
	}

	private void OnMouseEnter(object sender, MouseEventArgs e)
	{
		OnCellMouseEnter(sender, e);
	}

	private void OnTextMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (!_inline)
		{
			e.Handled = true;
		}
	}

	private async void OnTextMouseUp(object sender, MouseButtonEventArgs e)
	{
		TimelineCellViewModel timelineCellViewModel = cellModel;
		if (!_inline && timelineCellViewModel != null)
		{
			e.Handled = true;
			base.Container?.ClearCopiedTaskSelection(timelineCellViewModel.Id);
			if (Utils.IfCtrlPressed())
			{
				base.timelineModel.ChangeBatchSelectStatus(timelineCellViewModel);
			}
			else
			{
				await OpenDetailWindow();
			}
		}
	}

	private void OnTextMouseLeave(object sender, MouseEventArgs e)
	{
		if (!_inline)
		{
			base.Container?.TryCloseToolTips(force: true);
		}
	}

	private void OnTextMouseEnter(object sender, MouseEventArgs e)
	{
		if (_inline)
		{
			return;
		}
		FrameworkElement ui = sender as FrameworkElement;
		if (ui == null)
		{
			return;
		}
		Task.Delay(200).ContinueWith(delegate
		{
			ThreadUtil.DetachedRunOnUiThread(delegate
			{
				if (ui.IsMouseOver)
				{
					base.Container?.OpenToolTips(this);
				}
			});
		});
	}

	public void Hide()
	{
		base.Visibility = Visibility.Collapsed;
	}

	public void ResetVisibility()
	{
		SetBinding(UIElement.VisibilityProperty, new Binding("Visibility"));
	}

	public void SetModel(TimelineCellViewModel model)
	{
		if (_emjEmjText != null)
		{
			_emjEmjText.Text = model.Title;
		}
		base.Visibility = Visibility.Visible;
	}

	public void ShowDetail()
	{
		if (cellModel != null)
		{
			ShowTaskDetail(cellModel, byMouse: false);
		}
	}
}
