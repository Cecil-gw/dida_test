using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TickTickUtils;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.KotlinUtils;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.CountDown.ViewModels;
using ticktick_WPF.Views.CountDown.Views;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Habit;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Pomo;
using ticktick_WPF.Views.Tag;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Views.Calendar.Agenda;

public class CalendarAgendaItem : Grid
{
	private bool _pressed;

	private readonly ColumnDefinition _firstColumn;

	private CalendarAgendaItemDateTitle _dateTitle;

	private TextBlock _yearText;

	private AgendaCheckIcon _checkIcon;

	private CalendarAgendaContent _content;

	private Border _dueTextBorder;

	private bool _isWaitingDoubleClick;

	private DateTime _lastClickTime;

	private Point _firstClickPoint;

	private Border _emptyTextBorder;

	private string _operatingTaskId;

	private bool _popupShowing;

	public bool ShowDateAtLeft { get; set; }

	public CalendarAgendaItem()
	{
		base.MinHeight = 40.0;
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		_firstColumn = new ColumnDefinition
		{
			Width = new GridLength(96.0)
		};
		base.ColumnDefinitions.Add(_firstColumn);
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			MaxWidth = 790.0
		});
		base.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		base.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto,
			MinHeight = 40.0
		});
		AddDateText();
		AddAgendaIcon();
		AddItemContent();
		_dateTitle = new CalendarAgendaItemDateTitle();
		base.Children.Add(_dateTitle);
		base.DataContextChanged += OnDataBind;
		base.SizeChanged += OnSizeChanged;
		base.Unloaded += OnUnloaded;
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is CalendarAgendaItemViewModel calendarAgendaItemViewModel)
		{
			PropertyChangedEventManager.RemoveHandler(calendarAgendaItemViewModel.SourceViewModel, OnModelPropertyChanged, string.Empty);
			PropertyChangedEventManager.RemoveHandler(calendarAgendaItemViewModel, OnModelPropertyChanged, string.Empty);
		}
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (Math.Abs(e.NewSize.Height - e.PreviousSize.Height) > 1.0 && base.DataContext is CalendarAgendaItemViewModel calendarAgendaItemViewModel)
		{
			if (calendarAgendaItemViewModel.IsEmpty && !calendarAgendaItemViewModel.ShowTitleDate && !calendarAgendaItemViewModel.ShowYearText)
			{
				calendarAgendaItemViewModel.Height = 48.0;
				return;
			}
			calendarAgendaItemViewModel.Height = e.NewSize.Height;
		}
		if (((e.NewSize.Width > 640.0 && !ShowDateAtLeft) || (e.NewSize.Width < 640.0 && ShowDateAtLeft)) && base.DataContext is CalendarAgendaItemViewModel calendarAgendaItemViewModel2)
		{
			SetDateTitlePosition(calendarAgendaItemViewModel2.ShowTitleDate);
		}
	}

	private async void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (e.OldValue is CalendarAgendaItemViewModel calendarAgendaItemViewModel)
		{
			PropertyChangedEventManager.RemoveHandler(calendarAgendaItemViewModel.SourceViewModel, OnModelPropertyChanged, string.Empty);
			PropertyChangedEventManager.RemoveHandler(calendarAgendaItemViewModel, OnModelPropertyChanged, string.Empty);
		}
		if (e.NewValue is CalendarAgendaItemViewModel model)
		{
			SetEmptyMode(model, model.IsEmpty && !model.IsLoadAfter && !model.IsLoadBefore, model.ShowYearText ? new DateTime?(model.Date) : ((DateTime?)null));
			SetDueMode(model.IsLoadAfter, model.IsLoadBefore, model.Date);
			SetDateTitlePosition(model.ShowTitleDate);
			_dateTitle.SetDateText(model.ShowTitleDate ? new DateTime?(model.Date) : ((DateTime?)null), model.IsRestDay, model.IsWorkDay, model.IsVirtual && model.Date.Day == 6);
			model.SetHeight(base.ActualHeight);
			_checkIcon.SetCheckable(model.Checkable);
			await model.SetIconsVisible();
			_content.SetIcons();
			SetEmptyText(model);
			_firstColumn.Width = new GridLength((LocalSettings.Settings.TimeFormat == "24Hour") ? 96 : 104);
			PropertyChangedEventManager.AddHandler(model.SourceViewModel, OnModelPropertyChanged, string.Empty);
			PropertyChangedEventManager.AddHandler(model, OnModelPropertyChanged, string.Empty);
		}
		SetDateTitleOpacity(1);
	}

	private void SetEmptyText(CalendarAgendaItemViewModel model)
	{
		_content.Visibility = ((!string.IsNullOrEmpty(model.EmptyText)) ? Visibility.Collapsed : Visibility.Visible);
		_emptyTextBorder.Visibility = (string.IsNullOrEmpty(model.EmptyText) ? Visibility.Collapsed : Visibility.Visible);
		if (string.IsNullOrEmpty(model.EmptyText))
		{
			return;
		}
		if (model.Date == DateTime.Today)
		{
			bool flag = DateTime.Now.Hour > 5 && DateTime.Now.Hour < 20;
			BitmapImage resourceImage = ImageUtils.GetResourceImage("pack://application:,,,/Assets/ImageSource/AgendaNoTask" + (flag ? "" : "Night") + ".png");
			ImageBrush background = new ImageBrush
			{
				ImageSource = resourceImage,
				AlignmentX = AlignmentX.Right,
				AlignmentY = AlignmentY.Top,
				Stretch = Stretch.UniformToFill
			};
			_emptyTextBorder.Background = background;
			if (_emptyTextBorder.Child is TextBlock textBlock)
			{
				textBlock.Foreground = ThemeUtil.GetColorInString(flag ? "#0D354F" : "#FFFFFF");
				textBlock.Opacity = 0.8;
				textBlock.Margin = new Thickness(20.0, -1.0, 0.0, 0.0);
			}
		}
		else
		{
			_emptyTextBorder.Background = null;
			if (_emptyTextBorder.Child is TextBlock textBlock2)
			{
				textBlock2.SetResourceReference(TextBlock.ForegroundProperty, "BaseSolidColorOpacity40");
				textBlock2.Opacity = 1.0;
				textBlock2.Margin = new Thickness(0.0, -1.0, 0.0, 0.0);
			}
		}
	}

	private void SetDueMode(bool isDueEnd, bool isDueStart, DateTime date)
	{
		if (isDueEnd || isDueStart)
		{
			if (_dueTextBorder == null)
			{
				_dueTextBorder = new Border
				{
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch,
					Height = 48.0
				};
				TextBlock textBlock = new TextBlock
				{
					FontSize = 12.0,
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center
				};
				_dueTextBorder.SetValue(Grid.RowSpanProperty, 2);
				_dueTextBorder.SetValue(Grid.ColumnProperty, 1);
				_dueTextBorder.SetValue(Grid.ColumnSpanProperty, 2);
				textBlock.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity40");
				_dueTextBorder.Child = textBlock;
				base.Children.Add(_dueTextBorder);
				textBlock.Inlines.Add(new Run());
				Run run = new Run(" " + Utils.GetString("ShowMore"))
				{
					Background = Brushes.Transparent,
					Cursor = Cursors.Hand
				};
				run.SetResourceReference(TextElement.ForegroundProperty, "PrimaryColor");
				run.MouseLeftButtonUp += OnLoadMoreClick;
				textBlock.Inlines.Add(run);
			}
			SetDateTitlePosition(showDate: false);
			if (_dueTextBorder.Child is TextBlock textBlock2 && textBlock2.Inlines.FirstInline is Run run2)
			{
				run2.Text = string.Format(Utils.GetString("ShowingDataUntil"), ticktick_WPF.Util.DateUtils.FormatYearMonthDay(date.AddDays(isDueEnd ? (-1) : 0), DateFormatLen.Middle));
			}
		}
		else if (_dueTextBorder != null)
		{
			base.Children.Remove(_dueTextBorder);
			_dueTextBorder = null;
		}
	}

	private void OnLoadMoreClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is CalendarAgendaItemViewModel calendarAgendaItemViewModel && (calendarAgendaItemViewModel.IsLoadAfter || calendarAgendaItemViewModel.IsLoadBefore))
		{
			Utils.FindParent<CalendarAgendaView>(this)?.LoadMore(calendarAgendaItemViewModel.IsLoadAfter, nextYear: true);
		}
	}

	private void SetEmptyMode(CalendarAgendaItemViewModel model, bool isEmpty, DateTime? date)
	{
		if (isEmpty)
		{
			if (_yearText == null)
			{
				_yearText = new TextBlock
				{
					Margin = new Thickness(0.0, 26.0, 0.0, 14.0),
					TextAlignment = TextAlignment.Center,
					FontSize = 20.0,
					FontWeight = FontWeights.Bold,
					HorizontalAlignment = HorizontalAlignment.Left
				};
				_yearText.SetValue(Grid.RowSpanProperty, 2);
				_yearText.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100");
				base.Children.Add(_yearText);
			}
			_yearText.LineHeight = ((!date.HasValue) ? 1 : 28);
			_yearText.FontSize = ((!date.HasValue) ? 1 : 20);
			TextBlock yearText = _yearText;
			CalendarAgendaItemViewModel frontItem = model.FrontItem;
			yearText.Margin = new Thickness(0.0, ((frontItem != null && frontItem.IsLoadBefore) || !date.HasValue) ? 26 : 60, 0.0, (!date.HasValue) ? 14 : 32);
			_yearText.Text = ((!date.HasValue) ? "" : ticktick_WPF.Util.DateUtils.FormatYearMonth(date.Value, DateFormatLen.Long));
			_dateTitle.Visibility = Visibility.Collapsed;
		}
		else
		{
			base.Children.Remove(_yearText);
			_yearText = null;
		}
	}

	private void SetDateTitlePosition(bool showDate)
	{
		ShowDateAtLeft = base.ActualWidth > 640.0;
		_dateTitle.SetValue(Grid.ColumnProperty, (!ShowDateAtLeft) ? 1 : 0);
		_dateTitle.SetValue(Grid.RowProperty, ShowDateAtLeft ? 1 : 0);
		_dateTitle.Opacity = (showDate ? 1 : 0);
		if (showDate || ShowDateAtLeft)
		{
			_dateTitle.Visibility = Visibility.Visible;
			_dateTitle.Margin = new Thickness(0.0, ShowDateAtLeft ? 10 : 0, 0.0, 0.0);
		}
		else
		{
			_dateTitle.Visibility = Visibility.Collapsed;
		}
	}

	private void AddDateText()
	{
		StackPanel stackPanel = new StackPanel
		{
			Margin = new Thickness(0.0, 21.0, 0.0, 0.0),
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top
		};
		TextBlock textBlock = new TextBlock
		{
			FontSize = 10.0,
			MinWidth = 32.0,
			LineHeight = 14.0,
			Height = 14.0,
			TextAlignment = TextAlignment.Left,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(0.0, 0.0, 0.0, 2.0)
		};
		textBlock.SetResourceReference(TextBlock.ForegroundProperty, "BaseSolidColorOpacity40");
		textBlock.SetBinding(TextBlock.TextProperty, "BeforeDateText");
		textBlock.SetBinding(UIElement.VisibilityProperty, "ShowBeforeDateText");
		TextBlock textBlock2 = new TextBlock
		{
			FontSize = 12.0,
			MinWidth = 32.0,
			LineHeight = 16.0,
			TextAlignment = TextAlignment.Left,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top
		};
		textBlock2.SetResourceReference(TextBlock.ForegroundProperty, "BaseSolidColorOpacity40");
		Run run = new Run();
		run.SetBinding(Run.TextProperty, "DateText");
		Run run2 = new Run
		{
			FontSize = 10.0
		};
		run2.SetBinding(Run.TextProperty, "AmPmText");
		run2.SetResourceReference(TextElement.ForegroundProperty, "BaseColorOpacity30");
		textBlock2.Inlines.Add(run);
		textBlock2.Inlines.Add(run2);
		stackPanel.Children.Add(textBlock);
		stackPanel.Children.Add(textBlock2);
		stackPanel.SetValue(Grid.ColumnProperty, 1);
		stackPanel.SetValue(Grid.RowProperty, 1);
		base.Children.Add(stackPanel);
	}

	private void AddAgendaIcon()
	{
		_checkIcon = new AgendaCheckIcon
		{
			HorizontalAlignment = HorizontalAlignment.Right,
			Margin = new Thickness(0.0, 0.0, 26.0, 0.0)
		};
		_checkIcon.SetValue(Grid.ColumnProperty, 1);
		_checkIcon.SetValue(Grid.RowProperty, 1);
		base.Children.Add(_checkIcon);
	}

	private void AddItemContent()
	{
		_content = new CalendarAgendaContent
		{
			MinHeight = 40.0
		};
		_content.SetValue(Grid.ColumnProperty, 2);
		_content.SetValue(Grid.RowProperty, 1);
		base.Children.Add(_content);
		_content.MouseLeftButtonUp += OnTaskClick;
		_content.MouseRightButtonUp += OnShowOperation;
		_emptyTextBorder = new Border
		{
			CornerRadius = new CornerRadius(8.0),
			Height = 60.0
		};
		TextBlock textBlock = new TextBlock
		{
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Left,
			FontSize = 14.0
		};
		textBlock.SetResourceReference(TextBlock.ForegroundProperty, "BaseSolidColorOpacity40");
		textBlock.SetBinding(TextBlock.TextProperty, "EmptyText");
		_emptyTextBorder.SetValue(Grid.ColumnProperty, 2);
		_emptyTextBorder.SetValue(Grid.RowProperty, 1);
		_emptyTextBorder.Child = textBlock;
		base.Children.Add(_emptyTextBorder);
	}

	public async void RefreshHabitItem()
	{
		object dataContext = base.DataContext;
		if (!(dataContext is CalendarAgendaItemViewModel vm))
		{
			return;
		}
		HabitCheckInModel habitCheckInModel = await HabitCheckInDao.GetHabitCheckInByHabitIdAndStamp(vm.Id, vm.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
		if (habitCheckInModel != null)
		{
			vm.HabitCheckIn = habitCheckInModel;
			bool num = habitCheckInModel.IsComplete();
			bool flag = habitCheckInModel.IsUnComplete();
			int status = (num ? 2 : (flag ? (-1) : 0));
			vm.SourceViewModel.Status = status;
			if (vm.SourceViewModel is HabitBaseViewModel habitBaseViewModel)
			{
				habitBaseViewModel.HabitCheckIn = habitCheckInModel;
				_content.SetIcons();
			}
		}
	}

	public void SetDateTitleOpacity(int opacity)
	{
		_dateTitle.Opacity = opacity;
	}

	public void SetDateTitle(DateTime date, bool isRestDay, bool isWorkDay)
	{
		_dateTitle.SetDateText(date, isRestDay, isWorkDay);
		_dateTitle.Opacity = 1.0;
	}

	private async void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (!(base.DataContext is CalendarAgendaItemViewModel calendarAgendaItemViewModel))
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
			default:
				return;
			case 'T':
				if (propertyName == "Title")
				{
					_content?.SetTitle(calendarAgendaItemViewModel.Title, calendarAgendaItemViewModel.Status);
				}
				return;
			case 'C':
				break;
			}
			if (!(propertyName == "Color"))
			{
				return;
			}
			goto IL_035a;
		case 8:
		{
			char c = propertyName[2];
			if ((uint)c <= 105u)
			{
				if (c != 'A')
				{
					if (c != 'i' || !(propertyName == "Priority"))
					{
						return;
					}
					goto IL_035a;
				}
				if (!(propertyName == "IsAllDay"))
				{
					return;
				}
				goto IL_02b5;
			}
			switch (c)
			{
			case 'o':
				if (propertyName == "Progress")
				{
					_content.SetIcons();
				}
				break;
			case 's':
				if (propertyName == "Assignee")
				{
					await calendarAgendaItemViewModel.ResetAvatars();
					_content.SetIcons();
				}
				break;
			case 'l':
				if (propertyName == "Selected")
				{
					_content.SetBackground(calendarAgendaItemViewModel);
				}
				break;
			}
			return;
		}
		case 4:
			switch (propertyName[0])
			{
			case 'T':
				if (propertyName == "Tags")
				{
					calendarAgendaItemViewModel.DisplayTags = null;
					calendarAgendaItemViewModel.NotifyPropertyChanged("DisplayTags");
					calendarAgendaItemViewModel.SetColor();
					calendarAgendaItemViewModel.SetIconColor();
				}
				break;
			case 'D':
				if (propertyName == "Date")
				{
					SetDueMode(calendarAgendaItemViewModel.IsLoadAfter, calendarAgendaItemViewModel.IsLoadBefore, calendarAgendaItemViewModel.Date);
				}
				break;
			}
			return;
		case 12:
			switch (propertyName[0])
			{
			default:
				return;
			case 'I':
				break;
			case 'S':
				if (propertyName == "ShowYearText")
				{
					SetEmptyMode(calendarAgendaItemViewModel, calendarAgendaItemViewModel.IsEmpty && !calendarAgendaItemViewModel.IsLoadAfter && !calendarAgendaItemViewModel.IsLoadBefore, calendarAgendaItemViewModel.ShowYearText ? new DateTime?(calendarAgendaItemViewModel.Date) : ((DateTime?)null));
				}
				return;
			}
			if (!(propertyName == "IsLoadBefore"))
			{
				return;
			}
			break;
		case 14:
			if (propertyName == "DisplayContent")
			{
				_content.SetIcons();
			}
			return;
		case 9:
			if (!(propertyName == "StartDate"))
			{
				return;
			}
			goto IL_02b5;
		case 7:
			if (!(propertyName == "DueDate"))
			{
				return;
			}
			goto IL_02b5;
		case 6:
			if (propertyName == "Status")
			{
				_content?.OnStatusChanged();
				calendarAgendaItemViewModel.SetColor();
				calendarAgendaItemViewModel.SetIcon();
				_content?.SetTextDecoration(calendarAgendaItemViewModel);
			}
			return;
		case 11:
			if (!(propertyName == "IsLoadAfter"))
			{
				return;
			}
			break;
		case 10:
		case 13:
			return;
			IL_035a:
			calendarAgendaItemViewModel.SetColor();
			calendarAgendaItemViewModel.SetIconColor();
			return;
			IL_02b5:
			calendarAgendaItemViewModel.NotifyPropertyChanged("ShowBeforeDateText");
			calendarAgendaItemViewModel.NotifyPropertyChanged("BeforeDateText");
			calendarAgendaItemViewModel.NotifyPropertyChanged("DateText");
			calendarAgendaItemViewModel.NotifyPropertyChanged("AmPmText");
			calendarAgendaItemViewModel.SetTimeSpan();
			return;
		}
		SetEmptyMode(calendarAgendaItemViewModel, calendarAgendaItemViewModel.IsEmpty && !calendarAgendaItemViewModel.IsLoadAfter && !calendarAgendaItemViewModel.IsLoadBefore, calendarAgendaItemViewModel.ShowYearText ? new DateTime?(calendarAgendaItemViewModel.Date) : ((DateTime?)null));
		SetDueMode(calendarAgendaItemViewModel.IsLoadAfter, calendarAgendaItemViewModel.IsLoadBefore, calendarAgendaItemViewModel.Date);
	}

	private async void OnTaskClick(object sender, MouseButtonEventArgs e)
	{
		if (((Utils.FindParent<CalendarControl>(this)?.IsLocked ?? false) && (!(base.DataContext is CalendarAgendaItemViewModel calendarAgendaItemViewModel) || (!calendarAgendaItemViewModel.IsLoadAfter && !calendarAgendaItemViewModel.IsLoadBefore))) || PopupStateManager.IsInSelection() || PopupStateManager.IsInAdd() || !_content.IsMouseOver)
		{
			return;
		}
		if (_popupShowing)
		{
			_popupShowing = false;
			return;
		}
		if (base.DataContext != null && base.DataContext is CalendarAgendaItemViewModel calendarAgendaItemViewModel2)
		{
			CalendarControl calendarControl = Utils.FindParent<CalendarControl>(this);
			calendarControl?.ClearCopiedTaskSelection(calendarAgendaItemViewModel2.Id);
			calendarControl?.AddItemClickEvent();
			if (calendarAgendaItemViewModel2.SourceViewModel.IsEvent)
			{
				await ShowEventDetail(e, calendarAgendaItemViewModel2);
			}
			else if (calendarAgendaItemViewModel2.SourceViewModel.IsHabit)
			{
				await ShowHabitDetail(e, calendarAgendaItemViewModel2);
			}
			else if (calendarAgendaItemViewModel2.SourceViewModel.IsPomo)
			{
				ShowPomoDetail(calendarAgendaItemViewModel2);
			}
			else if (calendarAgendaItemViewModel2.SourceViewModel.IsCourse)
			{
				ShowCourseDetail(calendarAgendaItemViewModel2);
			}
			else if (calendarAgendaItemViewModel2.SourceViewModel.IsCountdown)
			{
				ShowCountdownDetail(calendarAgendaItemViewModel2);
			}
			else
			{
				TryShowTaskDetail(calendarAgendaItemViewModel2);
			}
		}
		_pressed = false;
	}

	private async Task TryShowTaskDetail(CalendarAgendaItemViewModel model)
	{
		_isWaitingDoubleClick = false;
		bool firstClick = (DateTime.Now - _lastClickTime).TotalMilliseconds > 400.0;
		_lastClickTime = DateTime.Now;
		if (firstClick)
		{
			_isWaitingDoubleClick = true;
			_firstClickPoint = PopupLocationCalculator.GetMousePoint(isDefault: false);
			await ShowTaskDetail(model);
		}
		if (firstClick)
		{
			await Task.Delay(400);
			if (_isWaitingDoubleClick)
			{
				UserActCollectUtils.AddClickEvent("calendar", "action", "click_task");
				Utils.FindParent<CalendarAgendaItemCanvas>(this)?.SetEditingTaskId(model.Id, model.RepeatDiff);
				_popupShowing = true;
			}
		}
		else
		{
			UserActCollectUtils.AddClickEvent("calendar", "action", "double_click_task");
			model.Selected = false;
			Utils.FindParent<CalendarAgendaItemCanvas>(this)?.SetEditingTaskId(null, 0.0);
			_popupShowing = false;
			TaskDetailWindows.ShowTaskWindows(model.SourceViewModel.GetTaskId());
		}
	}

	private async Task<TaskDetailPopup> ShowTaskDetail(CalendarAgendaItemViewModel model)
	{
		if (model == null)
		{
			return null;
		}
		TaskDetailViewModel taskDetailViewModel = await TaskDetailViewModel.Build(model.SourceViewModel.GetTaskId());
		if (taskDetailViewModel != null)
		{
			taskDetailViewModel.RepeatDiff = (string.IsNullOrEmpty(model.RepeatFlag) ? ((double?)null) : new double?(model.RepeatDiff));
			TaskDetailPopup taskDetailPopup = new TaskDetailPopup
			{
				IsCalendarContext = true
			};
			IToastShowWindow toastShowWindow = Utils.FindParent<IToastShowWindow>(this);
			if (toastShowWindow != null)
			{
				if (PopupStateManager.LastTarget == this)
				{
					return null;
				}
				taskDetailPopup.DependentWindow = toastShowWindow;
			}
			model.Selected = true;
			taskDetailPopup.Disappear += delegate
			{
				model.Selected = false;
				HandleOnWindowClosed(model.Id);
			};
			taskDetailPopup.TaskDateSaved += delegate(object sender, string s)
			{
				Utils.FindParent<CalendarAgendaView>(this)?.CheckTaskDisplay(s, isAdd: false);
			};
			taskDetailPopup.Show(taskDetailViewModel, model.SourceViewModel.IsCheckItem ? model.SourceViewModel.Id : null, new TaskWindowDisplayArgs(this, base.ActualWidth, _firstClickPoint, 0.0));
			SetCalendarEditing(editing: true);
			return taskDetailPopup;
		}
		return null;
	}

	private async void HandleOnWindowClosed(string id)
	{
		Utils.FindParent<CalendarAgendaItemCanvas>(this)?.ClearEditingTaskId(id);
		CalendarControl calendarControl = Utils.FindParent<CalendarControl>(this);
		calendarControl?.SetEditting(editting: false);
		calendarControl?.GetFocus(force: true);
		await Task.Delay(50);
		_popupShowing = false;
	}

	private async Task ShowCountdownDetail(CalendarAgendaItemViewModel model)
	{
		CountdownModel countdownModel = (model.SourceViewModel as CountDownBaseViewModel)?.Countdown;
		if (countdownModel != null)
		{
			model.Selected = true;
			CountdownDetailWindow countdownDetailWindow = new CountdownDetailWindow();
			countdownDetailWindow.Disappear += async delegate
			{
				model.Selected = false;
				HandleOnWindowClosed(model.Id);
			};
			countdownDetailWindow.Show(model: new CountDownViewModel(countdownModel, model.StartDate ?? DateTime.Now)
			{
				ScaleRatio = 1.2999999523162842
			}, target: this, targetWidth: base.ActualWidth, addHeight: 5.0, byMouse: true);
		}
	}

	private async void ShowCourseDetail(CalendarAgendaItemViewModel model)
	{
		if (model.SourceViewModel.Course != null)
		{
			CourseDetailViewModel viewModel = await CourseDetailViewModel.Build(model.SourceViewModel.Course);
			model.Selected = true;
			CourseDetailWindow courseDetailWindow = new CourseDetailWindow(viewModel);
			courseDetailWindow.Closed += async delegate
			{
				model.Selected = false;
				HandleOnWindowClosed(model.Id);
			};
			courseDetailWindow.Show(this, base.ActualWidth, showByMouse: true);
			Utils.FindParent<CalendarAgendaItemCanvas>(this)?.SetEditingTaskId(model.Id, 0.0);
			_popupShowing = true;
			SetCalendarEditing(editing: true);
		}
	}

	private void SetEditingTaskId(string taskId, double diff)
	{
		Utils.FindParent<CalendarAgendaItemCanvas>(this)?.SetEditingTaskId(taskId, diff);
	}

	private void SetCalendarEditing(bool editing)
	{
		Utils.FindParent<CalendarControl>(this)?.SetEditting(editing);
	}

	private async void ShowPomoDetail(CalendarAgendaItemViewModel model)
	{
		PomodoroModel pomodoroModel = await PomoService.GetAdjustPomo(model.Id);
		if (pomodoroModel != null)
		{
			model.Selected = true;
			PomoDetailWindow pomoDetailWindow = new PomoDetailWindow(new PomoDisplayViewModel(pomodoroModel, pomodoroModel.Tasks), model);
			pomoDetailWindow.Closed += async delegate
			{
				model.Selected = false;
				HandleOnWindowClosed(model.Id);
			};
			pomoDetailWindow.Show(this, base.ActualWidth, showByMouse: true);
			Utils.FindParent<CalendarAgendaItemCanvas>(this)?.SetEditingTaskId(model.Id, 0.0);
			_popupShowing = true;
			SetCalendarEditing(editing: true);
		}
	}

	private async Task ShowHabitDetail(MouseButtonEventArgs e, CalendarAgendaItemViewModel model)
	{
		HabitModel habitModel = await HabitDao.GetHabitById(model.Id);
		if (habitModel != null)
		{
			HabitCheckInWindow habitCheckInWindow = new HabitCheckInWindow(habitModel, model.Status, model.StartDate, Utils.FindParent<IToastShowWindow>(this));
			habitCheckInWindow.Show(this, base.ActualWidth, 0.0, byMouse: true);
			SetCalendarEditing(editing: true);
			Utils.FindParent<CalendarAgendaItemCanvas>(this)?.SetEditingTaskId(model.Id, 0.0);
			_popupShowing = true;
			habitCheckInWindow.OnAction += delegate
			{
				model.Selected = false;
				HandleOnWindowClosed(model.Id);
				RefreshHabitItem();
			};
			habitCheckInWindow.Closed += delegate
			{
				model.Selected = false;
				HandleOnWindowClosed(model.Id);
			};
			model.Selected = true;
		}
	}

	private async Task ShowEventDetail(MouseButtonEventArgs sender, CalendarAgendaItemViewModel model)
	{
		string id = model.Id;
		if (string.IsNullOrEmpty(id))
		{
			return;
		}
		model.Selected = true;
		CalendarEventModel calendarEventModel = await CalendarEventDao.GetEventById(ArchivedDao.GetOriginalId(id));
		if (calendarEventModel != null)
		{
			calendarEventModel.DueStart = model.StartDate;
			calendarEventModel.DueEnd = model.DueDate;
			CalendarDetailWindow calendarDetailWindow = new CalendarDetailWindow(GetWidgetThemeId());
			calendarDetailWindow.Disappear += delegate
			{
				model.Selected = false;
				HandleOnWindowClosed(model.Id);
			};
			calendarDetailWindow.EventArchiveChanged += OnEventArchiveChanged;
			calendarDetailWindow.Show(this, base.ActualWidth, 0.0, byMouse: true, new CalendarDetailViewModel(calendarEventModel));
			Utils.FindParent<CalendarAgendaItemCanvas>(this)?.SetEditingTaskId(model.Id, 0.0);
			_popupShowing = true;
			SetCalendarEditing(editing: true);
		}
	}

	private void OnEventArchiveChanged(object sender, bool archive)
	{
		if (base.DataContext is CalendarAgendaItemViewModel calendarAgendaItemViewModel)
		{
			calendarAgendaItemViewModel.SourceViewModel.Status = (archive ? 2 : 0);
		}
	}

	private string GetWidgetThemeId()
	{
		CalendarWidget calendarWidget = Utils.FindParent<CalendarWidget>(this);
		if (calendarWidget != null)
		{
			return calendarWidget.ThemeId;
		}
		return string.Empty;
	}

	private CalendarControl GetCalendarParent()
	{
		return Utils.FindParent<CalendarControl>(this);
	}

	private CalendarAgendaItemCanvas GetParentCanvas()
	{
		return Utils.FindParent<CalendarAgendaItemCanvas>(this);
	}

	private async void OnShowOperation(object sender, MouseButtonEventArgs e)
	{
		if ((GetCalendarParent()?.IsLocked ?? false) || base.DataContext == null)
		{
			return;
		}
		object dataContext = base.DataContext;
		CalendarAgendaItemViewModel model = dataContext as CalendarAgendaItemViewModel;
		if (model == null)
		{
			return;
		}
		GetCalendarParent()?.ClearCopiedTaskSelection(model.Id);
		if (model.Type == DisplayType.Task || model.Type == DisplayType.Agenda || model.Type == DisplayType.Note || model.Type == DisplayType.Derivative)
		{
			model.Selected = true;
			_operatingTaskId = model.Id;
		}
		else
		{
			_operatingTaskId = null;
		}
		if (model.Type == DisplayType.Event)
		{
			if (!model.SourceViewModel.OutDate())
			{
				model.Selected = true;
				CalendarOperationDialog calendarOperationDialog = new CalendarOperationDialog(new EventArchiveArgs(model.SourceViewModel), model.Status != 0);
				calendarOperationDialog.SetPlaceTarget(this);
				calendarOperationDialog.Closed += OnDialogClosed;
				GetCalendarParent()?.SetEditting(editting: true);
				calendarOperationDialog.Operated += delegate(object arg, KeyValuePair<string, ActionType> obj)
				{
					model.SourceViewModel.Status = ((obj.Value == ActionType.Archive) ? 2 : 0);
				};
				calendarOperationDialog.Show();
			}
		}
		else if (model.Type == DisplayType.Course)
		{
			if (!(model.StartDate < DateTime.Today))
			{
				model.Selected = true;
				ArchiveOperationDialog archiveOperationDialog = new ArchiveOperationDialog(model.Course.UniqueId, ArchiveKind.Course, model.Course.Archived);
				archiveOperationDialog.SetPlaceTarget(this);
				archiveOperationDialog.Closed += OnDialogClosed;
				GetCalendarParent()?.SetEditting(editting: true);
				archiveOperationDialog.Operated += delegate(object arg, KeyValuePair<string, ActionType> obj)
				{
					model.Course.Archived = obj.Value == ActionType.Archive;
					model.SourceViewModel.Status = ((model.Course.Archived || model.Course.CourseStart.Date < DateTime.Today) ? 2 : 0);
				};
				archiveOperationDialog.Show();
			}
		}
		else
		{
			ProjectModel model2 = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == model.ProjectId);
			if ((model.Type == DisplayType.Habit && model.Status == 2) || TryToastUnableString(model2))
			{
				model.Selected = false;
			}
			else if (model.Type == DisplayType.Habit)
			{
				ShowHabitOperation(model);
			}
			else
			{
				await ShowTaskOperation(model);
			}
		}
	}

	private void ShowHabitOperation(CalendarAgendaItemViewModel model)
	{
		if (model.Status != 0)
		{
			return;
		}
		List<OperationItemViewModel> list = new List<OperationItemViewModel>
		{
			new OperationItemViewModel(ActionType.EditHabitLog),
			new OperationItemViewModel(ActionType.Skip),
			new OperationItemViewModel(ActionType.UnComplete)
		};
		HabitBaseViewModel habitBaseViewModel = model.SourceViewModel as HabitBaseViewModel;
		HabitModel habit = habitBaseViewModel?.Habit;
		HabitCheckInModel habitCheckInModel = habitBaseViewModel?.HabitCheckIn;
		if (habitCheckInModel != null && habitCheckInModel.Value > 0.0)
		{
			list.Add(new OperationItemViewModel(ActionType.Reset));
		}
		if (LocalSettings.Settings.EnableFocus)
		{
			list.Insert(0, new OperationItemViewModel(ActionType.StartFocus)
			{
				Selectable = false,
				SubActions = new List<OperationItemViewModel>
				{
					new OperationItemViewModel(ActionType.StartPomo)
					{
						Enable = (!TickFocusManager.Working || LocalSettings.Settings.PomoType == FocusConstance.Focus)
					},
					new OperationItemViewModel(ActionType.StartTiming)
					{
						Enable = (!TickFocusManager.Working || LocalSettings.Settings.PomoType == FocusConstance.Timing)
					}
				}
			});
		}
		OperationDialog operationDialog = new OperationDialog(model.Id, list);
		operationDialog.SetPlaceTarget(this);
		operationDialog.Operated += async delegate(object obj, KeyValuePair<string, ActionType> kv)
		{
			switch (kv.Value)
			{
			case ActionType.StartPomo:
				TickFocusManager.TryStartFocusHabit(kv.Key, isPomo: true);
				break;
			case ActionType.StartTiming:
				TickFocusManager.TryStartFocusHabit(kv.Key, isPomo: false);
				break;
			case ActionType.Skip:
				await HabitService.SkipHabit(model.Id);
				GetParentCanvas()?.RemoveItem(model);
				GetToastWindow()?.TryToastString(null, Utils.GetString("Skipped"));
				break;
			case ActionType.EditHabitLog:
				HabitUtils.ShowHabitRecordWindow(model.Id, habit?.Name, DateTime.Today, isEdit: true, model.Status != 2);
				break;
			case ActionType.UnComplete:
				await HabitService.UncompleteHabit(model.Id, DateTime.Today);
				break;
			case ActionType.Reset:
				await HabitService.ResetCheckInHabit(model.Id, DateTime.Today, habit?.Type, this);
				break;
			}
		};
		operationDialog.Show();
	}

	private async Task ShowTaskOperation(CalendarAgendaItemViewModel model)
	{
		if (string.IsNullOrEmpty(_operatingTaskId))
		{
			return;
		}
		GetToastWindow();
		OperationExtra operationExtra = await TaskOperationHelper.GetTaskAccessInfo(_operatingTaskId, canShowAddSubtask: false);
		if (operationExtra.TimeModel == null)
		{
			return;
		}
		operationExtra.TimeModel.StartDate = model.DisplayStartDate ?? model.StartDate;
		operationExtra.TimeModel.DueDate = model.DisplayDueDate ?? model.DueDate;
		TaskOperationDialog dialog = new TaskOperationDialog(operationExtra, this);
		if (Window.GetWindow(this) is WidgetWindow widgetWindow)
		{
			dialog.Resources = widgetWindow.Resources;
		}
		dialog.Closed += OnDialogClosed;
		dialog.PrioritySelect += SetPriority;
		dialog.Copied += CopyTask;
		dialog.LinkCopied += CopyTaskLink;
		dialog.Deleted += DeleteTask;
		dialog.AbandonOrReopen += OnAbandonOrReopenTask;
		dialog.ProjectSelect += OnProjectSelect;
		dialog.AssigneeSelect += OnAssigneeSelect;
		dialog.TagsSelect += OnTagsSelect;
		dialog.SkipCurrentRecurrence += OnSkipRecurrence;
		dialog.SwitchTaskOrNote += OnSwitchTaskOrNoteClick;
		dialog.Toast += OnTaskOperationToast;
		dialog.CompleteDateChanged += async delegate(object o, DateTime date)
		{
			await TaskService.ChangeCompleteDate(model.Id, date);
			SyncManager.TryDelaySync();
		};
		dialog.TimeClear += async delegate
		{
			dialog.Dismiss();
			await ClearDate();
		};
		dialog.TimeSelect += async delegate(object arg, TimeData data)
		{
			dialog.Dismiss();
			if (!string.IsNullOrEmpty(model.RepeatFlag) && model.StartDate.HasValue && model.Status == 0)
			{
				ModifyRepeatHandler.TryUpdateDueDate(model.Id, model.DisplayStartDate, model.DisplayDueDate, data, 1, 1);
			}
			else
			{
				await TaskService.SetDate(_operatingTaskId, data);
				Utils.FindParent<CalendarAgendaView>(this)?.CheckTaskDisplay(_operatingTaskId, isAdd: false);
			}
		};
		dialog.QuickDateSelect += async delegate(object arg, DateTime date)
		{
			dialog.Dismiss();
			if (!string.IsNullOrEmpty(model.RepeatFlag) && model.Status == 0)
			{
				TaskModel task = await TaskDao.GetThinTaskById(model.Id);
				if (task != null && model.StartDate.HasValue && task.startDate.HasValue)
				{
					List<TaskReminderModel> reminders = await TaskReminderDao.GetRemindersByTaskId(task.id);
					TimeData timeData = new TimeData
					{
						StartDate = model.StartDate,
						DueDate = model.DueDate,
						IsAllDay = (!model.StartDate.HasValue || (model.IsAllDay ?? true)),
						Reminders = reminders,
						RepeatFrom = model.RepeatFrom,
						RepeatFlag = model.RepeatFlag,
						ExDates = ((task.exDate != null) ? task.exDate.ToList() : new List<string>())
					};
					ModifyRepeatHandler.TryUpdateDueDateOnlyDate(model.Id, model.DisplayStartDate, model.DisplayDueDate, timeData, date, 1, 1);
				}
			}
			else
			{
				await TaskService.SetStartDate(_operatingTaskId, date);
				Utils.FindParent<CalendarAgendaView>(this)?.CheckTaskDisplay(_operatingTaskId, isAdd: false);
				SyncManager.TryDelaySync();
			}
		};
		dialog.Show();
		GetCalendarParent()?.SetEditting(editting: true);
	}

	private void OnTaskOperationToast(object sender, string e)
	{
		GetToastWindow()?.TryToastString(null, e);
	}

	private async void OnAbandonOrReopenTask(object sender, EventArgs e)
	{
		if (base.DataContext == null)
		{
			return;
		}
		object dataContext = base.DataContext;
		if (!(dataContext is CalendarAgendaItemViewModel { DisplayStartDate: var displayStartDate } model))
		{
			return;
		}
		if (displayStartDate.HasValue && model.StartDate != model.DisplayStartDate && model.Status == 0)
		{
			IToastShowWindow toastWindow = GetToastWindow();
			if (!(await ModifyRepeatHandler.CompleteOrSkipRecurrence(model.Id, model.DisplayStartDate, toastWindow)))
			{
				return;
			}
		}
		int num = ((model.Status != -1) ? (-1) : 0);
		await TaskService.SetTaskStatus(model.Id, num, num != 0, GetToastWindow());
	}

	private async void OnSwitchTaskOrNoteClick(object sender, EventArgs e)
	{
		if (base.DataContext != null && base.DataContext is CalendarAgendaItemViewModel calendarAgendaItemViewModel && await TaskService.SwitchTaskOrNote(calendarAgendaItemViewModel.Id, toast: false))
		{
			GetToastWindow()?.TryToastString(null, Utils.GetString("Converted"));
			SyncManager.TryDelaySync();
		}
	}

	private async void CopyTaskLink(object sender, EventArgs e)
	{
		TaskModel taskModel = await TaskDao.GetThinTaskById(_operatingTaskId);
		if (taskModel != null)
		{
			TaskUtils.CopyTaskLink(taskModel.id, taskModel.projectId, taskModel.title);
		}
	}

	private async Task ClearDate()
	{
		TaskModel taskModel = await TaskDao.GetThinTaskById(_operatingTaskId);
		if (taskModel != null)
		{
			if (string.IsNullOrEmpty(taskModel.attendId))
			{
				await TaskService.ClearDate(_operatingTaskId);
				SyncManager.TryDelaySync();
			}
			else if (await TaskOperationHelper.CheckIfTaskAllowClearDate(_operatingTaskId, this))
			{
				await TaskService.ClearAgendaDate(_operatingTaskId);
				SyncManager.TryDelaySync();
			}
		}
	}

	private async void OnSkipRecurrence(object sender, EventArgs e)
	{
		CalendarAgendaItemViewModel model = GetModel();
		if (model == null || !model.DisplayStartDate.HasValue || !(model.DisplayStartDate != model.StartDate))
		{
			await TaskService.SkipCurrentRecurrence(_operatingTaskId, notify: true, GetToastWindow());
		}
		else
		{
			await TaskService.SkipSelectRecurrence(_operatingTaskId, model.DisplayStartDate.Value.Date, GetToastWindow());
		}
		SyncManager.TryDelaySync();
	}

	private async void OnTagsSelect(object sender, TagSelectData tags)
	{
		await TaskService.SetTags(_operatingTaskId, tags.OmniSelectTags);
		SyncManager.TryDelaySync();
	}

	private async void OnAssigneeSelect(object sender, AvatarInfo assignee)
	{
		await TaskService.SetAssignee(_operatingTaskId, assignee.UserId);
		SyncManager.TryDelaySync();
	}

	private async void OnProjectSelect(object sender, SelectableItemViewModel e)
	{
		(string, string, string) projectAndColumnId = e.GetProjectAndColumnId();
		string projectId = projectAndColumnId.Item1;
		string item = projectAndColumnId.Item2;
		string operatingTaskId = _operatingTaskId;
		string columnId = item;
		await TaskService.MoveProject(operatingTaskId, projectId, null, columnId);
		SyncManager.TryDelaySync();
		CalendarControl calendarControl = Utils.FindParent<CalendarControl>(this);
		if (!string.IsNullOrEmpty(CalendarUtils.CheckTaskCanShown(_operatingTaskId, calendarControl != null && calendarControl.ViewSourceType == CalendarViewSourceType.WithList)))
		{
			GetToastWindow()?.ToastMoveProjectControl(projectId);
		}
	}

	private async void DeleteTask(object sender, EventArgs e)
	{
		PopupStateManager.OnViewPopupClosed(needWait: false);
		TaskModel task = await TaskDao.GetThinTaskById(_operatingTaskId);
		if (task == null)
		{
			return;
		}
		if (string.IsNullOrEmpty(task.attendId))
		{
			if (!string.IsNullOrEmpty(task.repeatFlag) && task.startDate.HasValue && task.status == 0)
			{
				CalendarAgendaItemViewModel model = GetModel();
				if (model != null && model.Id == task.id)
				{
					ModifyRepeatHandler.TryDeleteRecurrence(task.id, model.DisplayStartDate, model.DisplayDueDate, GetToastWindow());
					return;
				}
			}
			List<TaskModel> list = await TaskService.GetAllSubTasksByIdAsync(_operatingTaskId, task.projectId);
			if (list != null && list.Count > 0)
			{
				list.Add(task);
				GetToastWindow()?.BatchDeleteTask(list);
			}
			else
			{
				GetToastWindow()?.TaskDeleted(_operatingTaskId);
			}
		}
		else if (await TaskOperationHelper.CheckIfAllowDeleteAgenda(task, this))
		{
			if (!string.IsNullOrEmpty(task.repeatFlag) && task.startDate.HasValue && task.status == 0)
			{
				ModifyRepeatHandler.TryDeleteRecurrence(task.id, task.startDate, task.dueDate, GetToastWindow());
				return;
			}
			await TaskService.DeleteAgenda(_operatingTaskId, task.projectId, task.attendId);
		}
		if (base.DataContext != null && base.DataContext is CalendarAgendaItemViewModel model2)
		{
			GetParentCanvas()?.RemoveItem(model2);
		}
	}

	private async void CopyTask(object sender, EventArgs e)
	{
		TaskModel taskModel = await TaskService.CopyTask(_operatingTaskId, this);
		if (taskModel != null)
		{
			GetCalendarParent()?.SelectCopiedTask(taskModel.id);
		}
	}

	private async void SetPriority(object sender, int priority)
	{
		await TaskService.SetPriority(_operatingTaskId, priority);
	}

	private void OnDialogClosed(object sender, EventArgs e)
	{
		CalendarAgendaItemViewModel model = GetModel();
		if (model != null)
		{
			model.Selected = false;
		}
		GetCalendarParent()?.SetEditting(editting: false);
		PopupStateManager.OnViewPopupClosed();
	}

	private CalendarAgendaItemViewModel GetModel()
	{
		return base.DataContext as CalendarAgendaItemViewModel;
	}

	private bool TryToastUnableString(ProjectModel model)
	{
		if (model != null && !string.IsNullOrEmpty(model.permission))
		{
			IToastShowWindow toastWindow = GetToastWindow();
			string permission = model.permission;
			if (permission == "read")
			{
				string arg = Utils.GetString("ReadOnly");
				toastWindow?.TryToastString(null, string.Format(Utils.GetString("NoPermissionToEdit"), arg));
				return true;
			}
			if (permission == "comment")
			{
				string arg = Utils.GetString("CanComment");
				toastWindow?.TryToastString(null, string.Format(Utils.GetString("NoPermissionToEdit"), arg));
				return true;
			}
		}
		return false;
	}

	private IToastShowWindow GetToastWindow()
	{
		return Utils.FindParent<IToastShowWindow>(this);
	}
}
