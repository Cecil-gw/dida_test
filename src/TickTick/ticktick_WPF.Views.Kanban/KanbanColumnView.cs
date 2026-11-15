using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Emoji.Wpf;
using TickTickUtils;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.DragAndDrop;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Kanban.Item;
using ticktick_WPF.Views.MainListView.TaskList;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Misc.ColorSelector;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.QuickAdd;

namespace ticktick_WPF.Views.Kanban;

public class KanbanColumnView : Grid, IKanbanColumn
{
	private const double ColumnCornerRadius = 14.0;

	private static readonly TranslateTransform ColoredContentTransform;

	private static LinearGradientBrush _emptyBrush;

	private readonly Border _columnColorBackground = new Border
	{
		CornerRadius = new CornerRadius(14.0),
		IsHitTestVisible = false,
		Background = Brushes.Transparent
	};

	private readonly Grid _titleGrid = new Grid
	{
		Background = Brushes.Transparent,
		Cursor = Cursors.Hand,
		Height = 36.0,
		VerticalAlignment = VerticalAlignment.Bottom,
		Margin = new Thickness(14.0, 0.0, 10.0, 0.0)
	};

	private TaskListView _taskList;

	private KanbanContainer _kanban;

	private string _originalColumnName;

	private QuickAddView _quickAddView;

	private bool _quickAddFocusPending;

	private TextBox _editNameText;

	private Border _editNameBackground;

	private Popup _errorPopup;

	private Rectangle _emptyBorder;

	private HoverIconButton _moreButton;

	private bool _popupOpen;

	private HoverIconButton _addButton;

	private Rectangle _draggingBorder;

	private Rectangle _dropBorder;

	private string _currentColumnColor;

	private bool _canEnter = true;

	private TextBlock _postPoneText;

	public bool IsMouseDragOver
	{
		get
		{
			if (_titleGrid.IsMouseOver && (_addButton == null || !_addButton.IsMouseOver))
			{
				if (_moreButton != null)
				{
					return !_moreButton.IsMouseOver;
				}
				return true;
			}
			return false;
		}
	}

	public bool DragMouseDown { get; set; }

	public string ColumnId { get; set; }

	public bool TaskChanged { get; set; }

	public event EventHandler DisplayColorChanged;

	public KanbanColumnView(ColumnViewModel model)
	{
		base.VerticalAlignment = VerticalAlignment.Stretch;
		base.Margin = new Thickness(0.0, 0.0, 0.0, 4.0);
		base.RenderTransformOrigin = new Point(0.5, 0.5);
		base.DataContextChanged += OnDataBind;
		base.DataContext = model;
		base.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(40.0)
		});
		base.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		base.RowDefinitions.Add(new RowDefinition());
		SetResourceReference(FrameworkElement.WidthProperty, "KanbanColumnWidth");
		InitColumnColorBackground();
		InitTitleGrid();
		SetShowAdd(withAnim: false);
		InitView();
		base.Loaded += OnLoaded;
		base.Unloaded += OnUnload;
	}

	private void OnUnload(object sender, RoutedEventArgs e)
	{
		UnbindEvents();
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		BindEvents();
	}

	static KanbanColumnView()
	{
		ColoredContentTransform = new TranslateTransform(0.0, 2.0);
		_emptyBrush = new LinearGradientBrush
		{
			StartPoint = new Point(1.0, 0.0),
			EndPoint = new Point(1.0, 1.0)
		};
		SetEmptyBrushColor();
	}

	private void InitTitleGrid()
	{
		_titleGrid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		_titleGrid.ColumnDefinitions.Add(new ColumnDefinition());
		_titleGrid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		_titleGrid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		_titleGrid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		Border border = new Border
		{
			Margin = new Thickness(-4.0, 0.0, 0.0, 0.0),
			CornerRadius = new CornerRadius(4.0),
			VerticalAlignment = VerticalAlignment.Center
		};
		border.SetBinding(UIElement.IsHitTestVisibleProperty, "Enable");
		border.SetResourceReference(FrameworkElement.StyleProperty, "HoverBorderStyle10");
		EmjTextBlock emjTextBlock = new EmjTextBlock
		{
			MaxWidth = base.Width - 84.0,
			ClipToBounds = true,
			IsHitTestVisible = false,
			TextWrapping = TextWrapping.Wrap,
			Height = 18.0,
			FontSize = 14.0,
			Margin = new Thickness(4.0),
			TextTrimming = TextTrimming.CharacterEllipsis,
			FontWeight = FontWeights.Bold
		};
		emjTextBlock.SetBinding(EmjTextBlock.TextProperty, "Name");
		emjTextBlock.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100");
		border.Child = emjTextBlock;
		border.MouseLeftButtonUp += OnTitleClick;
		_titleGrid.Children.Add(border);
		TextBlock textBlock = new TextBlock
		{
			FontSize = 12.0,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(4.0, 0.0, 0.0, 0.0)
		};
		textBlock.SetValue(Grid.ColumnProperty, 1);
		textBlock.SetBinding(TextBlock.TextProperty, "TaskCount");
		textBlock.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity40");
		_titleGrid.Children.Add(textBlock);
		base.Children.Add(_titleGrid);
	}

	private void InitColumnColorBackground()
	{
		_columnColorBackground.SetValue(Grid.RowSpanProperty, 3);
		_columnColorBackground.SetValue(Panel.ZIndexProperty, -10);
		base.Children.Add(_columnColorBackground);
		SetColumnColor(GetModel());
	}

	private void OnTitleClick(object sender, MouseButtonEventArgs e)
	{
		if (DragMouseDown)
		{
			SetNewName(rename: true);
		}
		DragMouseDown = false;
	}

	private void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (e.OldValue is ColumnViewModel source)
		{
			PropertyChangedEventManager.RemoveHandler(source, OnNotifyReload, "Reload");
			PropertyChangedEventManager.RemoveHandler(source, OnCanDropChanged, "CanDrop");
			PropertyChangedEventManager.RemoveHandler(source, OnColumnColorChanged, "Color");
		}
		if (e.NewValue is ColumnViewModel columnViewModel)
		{
			ColumnId = columnViewModel.ColumnId;
			SetTaskList(columnViewModel);
			PropertyChangedEventManager.AddHandler(columnViewModel, OnNotifyReload, "Reload");
			PropertyChangedEventManager.AddHandler(columnViewModel, OnCanDropChanged, "CanDrop");
			PropertyChangedEventManager.AddHandler(columnViewModel, OnColumnColorChanged, "Color");
			SetColumnColor(columnViewModel);
			SetDropBorder(columnViewModel);
			SetMoreButton(columnViewModel);
			SetAddButton(columnViewModel);
			SetPostponeButton(columnViewModel);
			InitView();
			SetShowAdd(withAnim: false);
			if (columnViewModel.NewAdd)
			{
				SetNewName(rename: false);
			}
		}
	}

	private void OnColumnColorChanged(object sender, PropertyChangedEventArgs e)
	{
		RefreshColumnColor();
	}

	public void RefreshColumnColor()
	{
		SetColumnColor(GetModel());
	}

	private void SetColumnColor(ColumnViewModel model)
	{
		UpdateColumnContentOffset(model);
		UpdateEmptyColumnPlaceholder(model);
		if (!LocalSettings.Settings.ExtraSettings.KbShowColumnColor || model == null || model.NewAdd)
		{
			_currentColumnColor = null;
			_columnColorBackground.Background = Brushes.Transparent;
			_columnColorBackground.BorderBrush = Brushes.Transparent;
			_columnColorBackground.BorderThickness = new Thickness(0.0);
			SetDraggingBackground();
			this.DisplayColorChanged?.Invoke(this, EventArgs.Empty);
			return;
		}
		_currentColumnColor = GetColumnColor(model);
		int num = (ThemeUtil.IsDark() ? 8 : 10);
		if (_emptyBorder == null)
		{
			_columnColorBackground.Background = ThemeUtil.GetAlphaColor(_currentColumnColor, num);
		}
		else
		{
			_columnColorBackground.Background = CreateEmptyColumnBackground(_currentColumnColor, num);
		}
		SetColumnColorBorder();
		SetDraggingBackground();
		this.DisplayColorChanged?.Invoke(this, EventArgs.Empty);
	}

	private void UpdateColumnContentOffset(ColumnViewModel model)
	{
		bool kbShowColumnColor = LocalSettings.Settings.ExtraSettings.KbShowColumnColor;
		Transform transform = ((kbShowColumnColor && model != null && !model.NewAdd) ? ColoredContentTransform : Transform.Identity);
		_titleGrid.RenderTransform = transform;
		_quickAddView?.SetValue(UIElement.RenderTransformProperty, transform);
		_taskList?.SetValue(UIElement.RenderTransformProperty, transform);
		_emptyBorder?.SetValue(UIElement.RenderTransformProperty, transform);
		_editNameBackground?.SetValue(UIElement.RenderTransformProperty, transform);
		_editNameText?.SetValue(UIElement.RenderTransformProperty, transform);
		_titleGrid.Margin = (kbShowColumnColor ? new Thickness(14.0, 0.0, 10.0, 0.0) : new Thickness(12.0, 0.0, 12.0, 0.0));
		_quickAddView?.SetValue(FrameworkElement.MarginProperty, kbShowColumnColor ? new Thickness(10.0, 4.0, 10.0, 8.0) : new Thickness(12.0, 0.0, 12.0, 0.0));
		_emptyBorder?.SetValue(FrameworkElement.MarginProperty, kbShowColumnColor ? new Thickness(10.0, 5.0, 10.0, 0.0) : new Thickness(12.0, 8.0, 12.0, 0.0));
		Thickness thickness = (kbShowColumnColor ? new Thickness(10.0, 0.0, 10.0, 0.0) : new Thickness(12.0, 0.0, 12.0, 0.0));
		double num = (kbShowColumnColor ? 32.0 : 36.0);
		_editNameBackground?.SetValue(FrameworkElement.MarginProperty, thickness);
		_editNameBackground?.SetValue(FrameworkElement.HeightProperty, num);
		_editNameText?.SetValue(FrameworkElement.MarginProperty, thickness);
		_editNameText?.SetValue(FrameworkElement.HeightProperty, num);
		UpdateTaskListTopMargin(model);
	}

	private void UpdateTaskListTopMargin(ColumnViewModel model = null)
	{
		if (_taskList != null)
		{
			if (!LocalSettings.Settings.ExtraSettings.KbShowColumnColor)
			{
				_taskList.Margin = new Thickness(0.0, 6.0, 0.0, 8.0);
				return;
			}
			bool flag = _taskList.DisplayModels.FirstOrDefault()?.IsSection ?? WillTaskListStartWithSection(model ?? GetModel());
			int num = ((_quickAddView == null && !flag) ? 4 : 2);
			_taskList.Margin = new Thickness(0.0, num, 0.0, -4.0);
		}
	}

	private static bool WillTaskListStartWithSection(ColumnViewModel model)
	{
		if (model != null && model.SourceItems?.Any((TaskBaseViewModel item) => item.IsPinned) == true)
		{
			return true;
		}
		switch (model?.Identity?.SortOption?.groupByType)
		{
		case Constants.SortType.project:
		case Constants.SortType.dueDate:
		case Constants.SortType.priority:
		case Constants.SortType.assignee:
		case Constants.SortType.tag:
		case Constants.SortType.createdTime:
			return true;
		default:
			return false;
		}
	}

	private async void ObserveTaskListLoad(TaskListView taskList, ColumnViewModel model, Task loadTask)
	{
		try
		{
			await loadTask;
		}
		catch (Exception)
		{
			return;
		}
		if (_taskList == taskList && GetModel() == model)
		{
			UpdateTaskListTopMargin(model);
		}
	}

	private void SetColumnColorBorder()
	{
		if (ColorUtils.NeedsContrastBorder(_currentColumnColor, ThemeUtil.IsDark()))
		{
			_columnColorBackground.SetResourceReference(Border.BorderBrushProperty, "BaseColorOpacity10");
			_columnColorBackground.BorderThickness = new Thickness(1.0);
		}
		else
		{
			_columnColorBackground.BorderBrush = Brushes.Transparent;
			_columnColorBackground.BorderThickness = new Thickness(0.0);
		}
	}

	private static LinearGradientBrush CreateEmptyColumnBackground(string color, int opacity)
	{
		return new LinearGradientBrush
		{
			StartPoint = new Point(0.5, 0.0),
			EndPoint = new Point(0.5, 1.0),
			GradientStops = new GradientStopCollection
			{
				new GradientStop(ThemeUtil.GetAlphaColor(color, opacity).Color, 0.0),
				new GradientStop(Colors.Transparent, 1.0)
			}
		};
	}

	private void UpdateEmptyColumnPlaceholder(ColumnViewModel model)
	{
		if (_emptyBorder != null)
		{
			_emptyBorder.Visibility = ((LocalSettings.Settings.ExtraSettings.KbShowColumnColor || (model != null && model.NewAdd)) ? Visibility.Collapsed : Visibility.Visible);
		}
	}

	private static string GetColumnColor(ColumnViewModel model)
	{
		if (KanbanColumnColorHelper.FollowsThemeColor(model.ColumnId))
		{
			return ThemeUtil.GetColorValue("ColorPrimary").ToString();
		}
		_ = model.Identity;
		ColumnProjectIdentity identity = model.Identity;
		bool isWeekDateView = identity != null && identity.Project is WeekProjectIdentity;
		string text = KanbanColumnColorHelper.GetFixedColor(model.ColumnId, isWeekDateView);
		if (!KanbanColumnColorHelper.HasColor(text))
		{
			text = model.Color;
		}
		if (!KanbanColumnColorHelper.HasColor(text))
		{
			return ThemeUtil.GetColorValue("ColorPrimary").ToString();
		}
		return text;
	}

	private void SetDraggingBackground()
	{
		if (_draggingBorder == null)
		{
			return;
		}
		bool flag = ThemeUtil.IsDark();
		string color = (flag ? "#202020" : "#FFFFFF");
		if (LocalSettings.Settings.ExtraSettings.KbShowColumnColor && KanbanColumnColorHelper.HasColor(_currentColumnColor))
		{
			_draggingBorder.Fill = ThemeUtil.GetBlendColor(color, _currentColumnColor, 1f, 0.16f);
			if (ColorUtils.NeedsContrastBorder(_currentColumnColor, flag))
			{
				_draggingBorder.Stroke = ThemeUtil.GetAlphaColor(flag ? "#FFFFFF" : "#191919", flag ? 60 : 100);
			}
			else
			{
				_draggingBorder.Stroke = ThemeUtil.GetAlphaColor(_currentColumnColor, flag ? 60 : 100);
			}
		}
		else
		{
			_draggingBorder.Fill = ThemeUtil.GetBlendColor(color, flag ? "#FFFFFF" : "#191919", 1f, 0.03f);
			_draggingBorder.SetResourceReference(Shape.StrokeProperty, "BaseColorOpacity10");
		}
		_draggingBorder.StrokeThickness = 1.0;
	}

	private void SetPostponeButton(ColumnViewModel model)
	{
		if (model.ColumnId == "date:-1" && (model.Identity.Project is TodayProjectIdentity || model.Identity.Project is WeekProjectIdentity || model.Identity.Project is AllProjectIdentity))
		{
			if (_postPoneText == null)
			{
				_postPoneText = new TextBlock
				{
					Text = Utils.GetString("Postpone"),
					FontSize = 12.0,
					Background = Brushes.Transparent,
					VerticalAlignment = VerticalAlignment.Center
				};
				_postPoneText.SetResourceReference(TextBlock.ForegroundProperty, "ThemeBlue");
				_postPoneText.MouseLeftButtonUp += PostPoneClick;
				_postPoneText.SetValue(Grid.ColumnProperty, 2);
				_titleGrid.Children.Add(_postPoneText);
			}
		}
		else if (_postPoneText != null)
		{
			_titleGrid.Children.Remove(_postPoneText);
			_postPoneText = null;
		}
	}

	private void PostPoneClick(object sender, MouseButtonEventArgs e)
	{
		if (new CustomerDialog(Utils.GetString("PostponeToTodayTitle"), Utils.GetString("PostponeToTodayMsg"), Utils.GetString("Postpone"), Utils.GetString("Cancel"), App.Window).ShowDialog() == true)
		{
			TaskUtils.PostPoneTasks(_taskList);
		}
	}

	private void OnCanDropChanged(object sender, PropertyChangedEventArgs e)
	{
		ColumnViewModel model = GetModel();
		if (model != null)
		{
			SetDropBorder(model);
		}
	}

	private void SetDropBorder(ColumnViewModel model)
	{
		if (model.CanDrop)
		{
			if (_dropBorder == null)
			{
				_dropBorder = new Rectangle
				{
					RadiusX = 14.0,
					RadiusY = 14.0,
					StrokeThickness = 1.0
				};
				_dropBorder.SetResourceReference(Shape.StrokeProperty, "PrimaryColor");
				if (LocalSettings.Settings.ExtraSettings.KbShowColumnColor)
				{
					_dropBorder.Fill = Brushes.Transparent;
				}
				else
				{
					_dropBorder.SetResourceReference(Shape.FillProperty, "PrimaryColor10");
				}
				_dropBorder.SetValue(Panel.ZIndexProperty, -5);
				_dropBorder.SetValue(Grid.RowSpanProperty, 3);
				base.Children.Add(_dropBorder);
			}
		}
		else
		{
			base.Children.Remove(_dropBorder);
			_dropBorder = null;
		}
	}

	private void SetAddButton(ColumnViewModel model)
	{
		if (model != null)
		{
			if (!model.CanAdd || model.NewAdd || LocalSettings.Settings.ExtraSettings.KbShowAdd)
			{
				_titleGrid.Children.Remove(_addButton);
				_addButton = null;
			}
			else if (_addButton == null)
			{
				_addButton = new HoverIconButton
				{
					Margin = new Thickness(2.0, 0.0, 0.0, 0.0),
					Focusable = false
				};
				_addButton.SetResourceReference(HoverIconButton.IconDataProperty, "Ic.ProjectList.Add");
				_addButton.IsImage = false;
				_addButton.SetResourceReference(FrameworkElement.ToolTipProperty, "AddaTask");
				_addButton.MouseLeftButtonUp += AddTaskClick;
				_addButton.SetValue(Grid.ColumnProperty, 3);
				_titleGrid.Children.Add(_addButton);
			}
		}
	}

	private void AddTaskClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		InitQuickAddView();
	}

	private void OnNotifyReload(object sender, PropertyChangedEventArgs e)
	{
		ColumnViewModel model = GetModel();
		if (model != null)
		{
			SetTaskList(model);
		}
	}

	private void SetMoreButton(ColumnViewModel model)
	{
		if (!model.Enable || model.NewAdd)
		{
			_titleGrid.Children.Remove(_moreButton);
			_moreButton = null;
		}
		else if (_moreButton == null)
		{
			_moreButton = new HoverIconButton
			{
				Margin = new Thickness(2.0, 0.0, 0.0, 0.0)
			};
			_moreButton.SetResourceReference(HoverIconButton.IconDataProperty, "Ic.ProjectList.More");
			_moreButton.IsImage = false;
			_moreButton.SetResourceReference(FrameworkElement.ToolTipProperty, "More");
			_moreButton.MouseLeftButtonUp += MoreGridClick;
			_moreButton.SetValue(Grid.ColumnProperty, 4);
			_titleGrid.Children.Add(_moreButton);
		}
	}

	private void MoreGridClick(object sender, MouseButtonEventArgs e)
	{
		ColumnViewModel model = GetModel();
		ColumnViewModel columnViewModel = model;
		if (columnViewModel == null || columnViewModel.Enable)
		{
			List<CustomMenuItemViewModel> list = new List<CustomMenuItemViewModel>
			{
				new CustomMenuItemViewModel("rename", Utils.GetString("Rename"), Utils.GetGeometry("Ic.TaskOM.Edit")),
				new CustomMenuItemViewModel("addToLeft", Utils.GetString("AddColumnToTheLeft"), Utils.GetGeometry("Ic.TaskOM.AddGroupLeft")),
				new CustomMenuItemViewModel("addToRight", Utils.GetString("AddColumnToTheRight"), Utils.GetGeometry("Ic.TaskOM.AddGroupRight"))
			};
			KanbanContainer kanban = _kanban;
			if (kanban != null && kanban.GetColumnCount() > 1)
			{
				list.Add(new CustomMenuItemViewModel("delete", Utils.GetString("Delete"), Utils.GetGeometry("Ic.ProjectList.Delete"))
				{
					IsDelete = true
				});
			}
			EscPopup popup = new EscPopup
			{
				StaysOpen = false,
				PlacementTarget = _moreButton,
				Placement = PlacementMode.Bottom,
				VerticalOffset = -5.0,
				HorizontalOffset = -102.0
			};
			CustomMenuList customMenuList = new CustomMenuList(list, popup);
			bool flag = AddColumnColorMenuItem(list, customMenuList, model);
			ProjectOrGroupPopup projectOrGroupPopup = new ProjectOrGroupPopup(customMenuList.SubPopup, new ProjectExtra
			{
				ProjectIds = new List<string> { model?.ProjectId }
			}, new ProjectSelectorExtra
			{
				showAll = false,
				batchMode = false,
				canSelectGroup = false,
				CanSearch = true,
				onlyShowPermission = true,
				ShowColumn = false
			});
			list.Insert(flag ? 4 : 3, new CustomMenuItemViewModel("move", Utils.GetString("MoveTo"), Utils.GetGeometry("Ic.ProjectList.MoveTo"))
			{
				SubControl = projectOrGroupPopup,
				SubPopupWidth = customMenuList.SubPopup.Width,
				NeedSubContentStyle = false
			});
			projectOrGroupPopup.ItemSelect += delegate(object o, SelectableItemViewModel viewModel)
			{
				popup.IsOpen = false;
				OnMoveColumnItemSelected(model, viewModel);
			};
			popup.Opened += delegate
			{
				_popupOpen = true;
			};
			popup.Closed += delegate
			{
				_popupOpen = false;
			};
			customMenuList.Operated += OnMoreItemSelected;
			customMenuList.Show();
		}
	}

	private async void OnMoveColumnItemSelected(ColumnViewModel model, SelectableItemViewModel e)
	{
		await Task.Delay(50);
		string id = e.Id;
		if (id != model.ProjectId)
		{
			TaskService.MoveColumnAsync(model.ColumnId, model.ProjectId, id);
		}
	}

	private void OnMoreItemSelected(object sender, object e)
	{
		switch (e as string)
		{
		case "rename":
			SetNewName(rename: true);
			break;
		case "addToLeft":
			_kanban?.AddColumn(GetModel()?.ColumnId, HorizontalDirection.Left);
			break;
		case "addToRight":
			_kanban?.AddColumn(GetModel()?.ColumnId, HorizontalDirection.Right);
			break;
		case "delete":
			_kanban?.DeleteColumn(GetModel()?.ColumnId, prompt: true);
			break;
		}
	}

	private bool AddColumnColorMenuItem(IList<CustomMenuItemViewModel> menuItems, CustomMenuList moreMenu, ColumnViewModel model)
	{
		if (model == null || !model.IsCustomColumn || !LocalSettings.Settings.ExtraSettings.KbShowColumnColor)
		{
			return false;
		}
		ColorItemSelector colorItemSelector = new ColorItemSelector
		{
			WithRecommendColors = true,
			AllowCancelSelected = false,
			RecommendedColors = KanbanColumnColorHelper.GetRecommendedCustomColors(),
			ShowNoColor = false
		};
		colorItemSelector.SetItemSelected(KanbanColumnColorHelper.HasColor(model.Color) ? model.Color : null);
		colorItemSelector.ColorSelect += async delegate(object sender, string color)
		{
			string selectedColor = (KanbanColumnColorHelper.HasColor(color) ? color : "transparent");
			if (string.Equals(model.Color, selectedColor, StringComparison.OrdinalIgnoreCase))
			{
				moreMenu.SubPopup.RestoreFocus = false;
				moreMenu.CloseSubPopup(null, e: false);
			}
			else
			{
				await ColumnDao.SaveColumnColor(selectedColor, model.ColumnId);
				model.Color = selectedColor;
				SyncManager.TryDelaySync();
				moreMenu.SubPopup.RestoreFocus = false;
				moreMenu.CloseSubPopup(null, e: false);
			}
		};
		menuItems.Insert(1, new CustomMenuItemViewModel("color", Utils.GetString("Color"), Utils.GetGeometry("Ic.Countdown.Appearance"))
		{
			SubContent = colorItemSelector,
			ShowSelected = false
		});
		return true;
	}

	private void SetTaskList(ColumnViewModel model)
	{
		if ((LocalSettings.Settings.HideComplete ? model.SourceItems.Count((TaskBaseViewModel i) => i.Status == 0) : model.SourceItems.Count) > 0)
		{
			if (_taskList == null)
			{
				_taskList = new TaskListView
				{
					Margin = new Thickness(0.0, 2.0, 0.0, -4.0),
					VerticalAlignment = VerticalAlignment.Top
				};
				_taskList.SetValue(Grid.RowProperty, 2);
				base.Children.Add(_taskList);
				try
				{
					if (_kanban != null)
					{
						_taskList.ViewModel.SetKanbanParent(_kanban.GetViewModel());
					}
				}
				catch (Exception)
				{
				}
				_taskList.ItemsCountChanged += OnTaskCountChanged;
			}
			if (model.Identity != null)
			{
				SectionAddTaskViewModel sectionAddTaskViewModel = _kanban?.AddingModel;
				_taskList.SetIdentity(model.Identity, TaskListDisplayType.Kanban);
				_taskList.SetAddingTask(adding: false);
				UpdateTaskListTopMargin(model);
				Task loadTask = _taskList.SortItemsAndLoad(model.SourceItems, (sectionAddTaskViewModel != null && sectionAddTaskViewModel.ColumnId == model.ColumnId && !sectionAddTaskViewModel.Invalid) ? sectionAddTaskViewModel : null);
				ObserveTaskListLoad(_taskList, model, loadTask);
			}
			_taskList.ViewModel.BatchEditor = _kanban;
			model.SetTaskListViewModel(_taskList.ViewModel);
			base.Children.Remove(_emptyBorder);
			_emptyBorder = null;
			SetColumnColor(model);
			base.Height = double.NaN;
		}
		else
		{
			base.Children.Remove(_taskList);
			_taskList = null;
			model.SetTaskListViewModel(null);
			if (_taskList != null)
			{
				_taskList.ItemsCountChanged -= OnTaskCountChanged;
			}
			if (_emptyBorder == null)
			{
				_emptyBorder = new Rectangle
				{
					RadiusX = 10.0,
					RadiusY = 10.0,
					Margin = new Thickness(10.0, 5.0, 10.0, 0.0),
					VerticalAlignment = VerticalAlignment.Stretch,
					HorizontalAlignment = HorizontalAlignment.Stretch,
					IsHitTestVisible = false,
					Fill = _emptyBrush
				};
				_emptyBorder.SetValue(Grid.RowProperty, 2);
				base.Children.Add(_emptyBorder);
				UpdateEmptyColumnPlaceholder(model);
			}
			SetColumnColor(model);
			if (!double.IsInfinity(base.MaxHeight))
			{
				base.Height = base.MaxHeight;
			}
		}
	}

	public static void SetEmptyBrushColor()
	{
		Color color = (ThemeUtil.IsDark() ? Colors.White : ((LocalSettings.Settings.ThemeId == "White") ? Colors.Black : ThemeUtil.GetColorValue("ColorPrimary")));
		double opacity = (ThemeUtil.IsDark() ? 0.02 : ((LocalSettings.Settings.ThemeId == "White") ? 0.04 : 0.05));
		Color color2 = Color.FromArgb(byte.MaxValue, color.R, color.G, color.B);
		Color color3 = Color.FromArgb(0, color.R, color.G, color.B);
		_emptyBrush.Opacity = opacity;
		_emptyBrush.GradientStops.Clear();
		_emptyBrush.GradientStops.Add(new GradientStop(color2, 0.0));
		_emptyBrush.GradientStops.Add(new GradientStop(color3, 1.0));
	}

	public void UnbindEvents()
	{
		if (_taskList != null)
		{
			_taskList.ItemsCountChanged -= OnTaskCountChanged;
		}
		if (_kanban != null)
		{
			_kanban.SetSelected -= OnSetSelected;
			_kanban = null;
		}
	}

	private void BindEvents()
	{
		_kanban = Utils.FindParent<KanbanContainer>(this);
		if (_kanban != null)
		{
			_kanban.SetSelected -= OnSetSelected;
			_kanban.SetSelected += OnSetSelected;
			if (_taskList != null)
			{
				_taskList.ViewModel.BatchEditor = _kanban;
			}
		}
	}

	public void OnDragMouseMove(MouseEventArgs e, string dragFromColumnId)
	{
		object dragData = TickDragHelper.GetDragData();
		ColumnViewModel model = GetModel();
		DisplayItemModel displayItemModel = dragData as DisplayItemModel;
		if (displayItemModel == null && dragData is CalendarDisplayViewModel calendarDisplayViewModel)
		{
			displayItemModel = new DisplayItemModel
			{
				SourceViewModel = calendarDisplayViewModel.SourceViewModel
			};
		}
		if (model != null && displayItemModel != null && (!displayItemModel.IsNote || !model.ColumnId.StartsWith("priority:")))
		{
			if (dragFromColumnId != model.ColumnId)
			{
				model.CanDrop = (displayItemModel.IsTaskOrNote && dragFromColumnId != "note") || (displayItemModel.IsItem && model.Identity.GetRealSortOption().groupByType == Constants.SortType.dueDate);
			}
			model.MouseOver = true;
		}
	}

	private void OnSetSelected(object sender, List<string> e)
	{
		_taskList?.SetSelected(e);
	}

	private async void OnTaskCountChanged(object sender, EventArgs e)
	{
		await Task.Delay(100);
		ColumnViewModel model = GetModel();
		if (model != null)
		{
			model.TaskCount = _taskList?.ViewModel.SourceModels.Count((TaskBaseViewModel i) => i.Status == 0) ?? 0;
			UpdateTaskListTopMargin();
		}
	}

	private void InitView(bool focus = false, bool resetQuickAdd = false)
	{
		ColumnViewModel model = GetModel();
		if (model != null)
		{
			_originalColumnName = model.Name;
			if (_quickAddView != null && (resetQuickAdd || _quickAddView.Model.ColumnId != model.ColumnId || string.IsNullOrEmpty(_quickAddView.TitleText.EditBox.Text)))
			{
				_quickAddView.Reset(AddTaskViewModel.Build(new ColumnProjectIdentity(model.Identity, model.ColumnId), null, useInKanban: true), QuickAddView.Scenario.Kanban, focus);
			}
		}
	}

	private async void InitQuickAddView(bool withAnim = true, bool focus = true)
	{
		ColumnViewModel model = GetModel();
		if (model == null)
		{
			return;
		}
		if (_addButton != null)
		{
			_addButton.IsHitTestVisible = false;
		}
		if (_quickAddView == null)
		{
			_quickAddView = new QuickAddView(new ColumnProjectIdentity(model.Identity.Project, model.ColumnId), QuickAddView.Scenario.Kanban)
			{
				Margin = new Thickness(10.0, 4.0, 10.0, 8.0),
				MaxHeight = 200.0,
				InputCornerRadius = new CornerRadius(10.0)
			};
			int num = (_quickAddView.Model.UseDetailStyle ? 200 : 108);
			_quickAddView.TaskAdded += OnTaskAdded;
			_quickAddView.BatchTaskAdded += OnBatchTaskAdded;
			_quickAddView.SetValue(Grid.RowProperty, 1);
			base.Children.Add(_quickAddView);
			UpdateColumnContentOffset(model);
			UpdateTaskListTopMargin();
			if (withAnim)
			{
				_quickAddView.BeginAnimation(FrameworkElement.MaxHeightProperty, AnimationUtils.GetDoubleAnimation(0.0, 200.0, 120));
			}
			else
			{
				_quickAddView.BeginAnimation(FrameworkElement.MaxHeightProperty, null);
				_quickAddView.MaxHeight = num;
			}
			_quickAddView.TitleText.EditBox.LostFocus += OnQuickAddLostFocus;
		}
		if (focus)
		{
			QuickAddView quickAddView = _quickAddView;
			_quickAddFocusPending = true;
			try
			{
				await quickAddView.FocusEndAsync();
			}
			finally
			{
				if (quickAddView == _quickAddView)
				{
					_quickAddFocusPending = false;
				}
			}
		}
		if (LocalSettings.Settings.ExtraSettings.KbShowAdd)
		{
			_quickAddView.TitleText.EditBox.LostFocus -= OnQuickAddLostFocus;
			await Task.Delay(120);
			_quickAddView?.BeginAnimation(FrameworkElement.MaxHeightProperty, null);
			int num2 = 108;
			if (_quickAddView != null)
			{
				num2 = (_quickAddView.Model.UseDetailStyle ? 200 : 108);
			}
			_quickAddView?.SetResourceReference(FrameworkElement.MaxHeightProperty, num2);
		}
	}

	private async void OnQuickAddLostFocus(object sender, RoutedEventArgs e)
	{
		if (!_quickAddFocusPending && _quickAddView != null && _quickAddView.IsLostFocus())
		{
			await (_quickAddView?.TryAddTaskOnLostFocus());
			HideAddView();
		}
	}

	private void HideAddView()
	{
		QuickAddView quickAddView = _quickAddView;
		_quickAddView = null;
		_quickAddFocusPending = false;
		if (quickAddView != null)
		{
			quickAddView.TaskAdded -= OnTaskAdded;
			quickAddView.BatchTaskAdded -= OnBatchTaskAdded;
			quickAddView.TitleText.LostFocus -= OnQuickAddLostFocus;
			if (_addButton != null)
			{
				_addButton.IsHitTestVisible = true;
			}
			base.Children.Remove(quickAddView);
			UpdateTaskListTopMargin();
		}
	}

	public void ResetQuickAddView(bool keepText = true)
	{
		QuickAddView quickAddView = _quickAddView;
		if (base.IsVisible && quickAddView != null)
		{
			ColumnViewModel model = GetModel();
			if (model?.Identity != null)
			{
				quickAddView.ResetView(model.Identity, focus: false, keepText);
			}
		}
	}

	private void OnBatchTaskAdded(object sender, List<TaskModel> e)
	{
		SyncManager.TryDelaySync();
	}

	private void OnTaskAdded(object sender, TaskModel model)
	{
		List<TaskBaseViewModel> matchedTasks = TaskViewModelHelper.GetMatchedTasks(GetModel().Identity.Project, new List<string> { model.id });
		if (matchedTasks == null || matchedTasks.Count == 0)
		{
			if (CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == model.projectId) != null)
			{
				Utils.FindParent<IToastShowWindow>(this)?.ToastMoveProjectControl(model.projectId, null, MoveToastType.Add);
			}
			return;
		}
		ColumnViewModel model2 = GetModel();
		if (model2 != null)
		{
			model2.CanDrop = false;
			if (model.projectId == model2.ProjectId && (model.columnId == model2.ColumnId || (model2.IsPinned && model.pinnedTimeStamp > 0)))
			{
				model2.TaskCount++;
			}
		}
	}

	public void TryFocus()
	{
		_editNameText.Focus();
	}

	private async void SetNewName(bool rename)
	{
		_titleGrid.Visibility = Visibility.Collapsed;
		if (_editNameText == null)
		{
			_editNameBackground = new Border
			{
				Height = 32.0,
				Margin = new Thickness(10.0, 0.0, 10.0, 0.0),
				VerticalAlignment = VerticalAlignment.Bottom,
				CornerRadius = new CornerRadius(8.0),
				IsHitTestVisible = false
			};
			_editNameBackground.SetResourceReference(Panel.BackgroundProperty, "WindowBackground");
			base.Children.Add(_editNameBackground);
			_editNameText = new TextBox
			{
				Height = 32.0,
				Margin = new Thickness(10.0, 0.0, 10.0, 0.0),
				VerticalAlignment = VerticalAlignment.Bottom
			};
			base.Children.Add(_editNameText);
			_editNameText.SetResourceReference(FrameworkElement.StyleProperty, "HintEditTextStyle");
			_editNameText.LostFocus += OnNameLostFocus;
			_editNameText.TextChanged += OnNameTextChanged;
			_editNameText.PreviewKeyUp += OnEditNameKeyUp;
			_editNameText.PreviewKeyDown += OnEditNameKeyDown;
			SetEditing();
			_editNameText.Focus();
		}
		UpdateColumnContentOffset(GetModel());
		if (rename)
		{
			_editNameText.Text = GetModel()?.Name ?? string.Empty;
			_editNameText.SelectAll();
			return;
		}
		await Task.Delay(20);
		_editNameText.Text = string.Empty;
		_editNameText.Focus();
		_editNameText.SelectAll();
	}

	private void OnEditNameKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.ImeProcessed)
		{
			_canEnter = false;
		}
	}

	private void OnEditNameKeyUp(object sender, KeyEventArgs e)
	{
		Key key = e.Key;
		if (key != Key.Return)
		{
			if (key == Key.Escape)
			{
				goto IL_001a;
			}
		}
		else if (_canEnter)
		{
			goto IL_001a;
		}
		goto IL_0029;
		IL_0029:
		_canEnter = true;
		return;
		IL_001a:
		OnNameLostFocus(null, null);
		e.Handled = true;
		goto IL_0029;
	}

	private void SetEditing()
	{
		ColumnViewModel model = GetModel();
		if (model != null)
		{
			model.Editing = true;
		}
	}

	private async void OnNameLostFocus(object sender, RoutedEventArgs e)
	{
		ColumnViewModel model = GetModel();
		if (model != null)
		{
			model.Editing = false;
			if (!model.NewAdd || !string.IsNullOrEmpty(_editNameText?.Text))
			{
				await SaveColumnName();
			}
			else if (string.IsNullOrEmpty(_editNameText?.Text) && !base.IsMouseOver)
			{
				_kanban?.DeleteColumn(this);
			}
		}
	}

	private async Task SaveColumnName()
	{
		ColumnViewModel model = GetModel();
		if (model == null)
		{
			return;
		}
		TextBox editText = _editNameText;
		KanbanContainer kanban = _kanban ?? Utils.FindParent<KanbanContainer>(this);
		if (editText == null)
		{
			return;
		}
		string saveName = editText.Text.Trim();
		if (!NameUtils.IsValidColumnName(saveName))
		{
			editText.SelectAll();
			SetErrorPopup(show: true);
			return;
		}
		if (string.IsNullOrEmpty(saveName))
		{
			editText.Text = _originalColumnName;
			model.Name = _originalColumnName;
			editText.LostFocus -= OnNameLostFocus;
			editText.TextChanged -= OnNameTextChanged;
			RemoveNameEditor();
			Utils.Toast(Utils.GetString("SectionNameCannotBeEmpty"));
			if (GetModel().NewAdd)
			{
				kanban?.DeleteColumn(this);
			}
			return;
		}
		bool flag = saveName != model.Name;
		if (flag)
		{
			flag = await CheckIfColumnNameExisted(saveName);
		}
		if (flag)
		{
			editText.Text = _originalColumnName;
			GetModel().Name = _originalColumnName;
			editText.LostFocus -= OnNameLostFocus;
			editText.TextChanged -= OnNameTextChanged;
			RemoveNameEditor();
			Utils.Toast(Utils.GetString("SectionNameExisted"));
			if (GetModel().NewAdd)
			{
				kanban?.DeleteColumn(this);
			}
			return;
		}
		if (!model.NewAdd)
		{
			await ColumnDao.SaveColumnName(saveName, model.ColumnId, model.NewAdd);
		}
		else
		{
			await ColumnDao.AddColumn(saveName, model.ProjectId, model.SortOrder);
		}
		_originalColumnName = saveName;
		GetModel().Name = saveName;
		editText.LostFocus -= OnNameLostFocus;
		editText.TextChanged -= OnNameTextChanged;
		RemoveNameEditor();
		if (model.NewAdd)
		{
			model.NewAdd = false;
			model.Editing = false;
			kanban?.Reload(forceReload: true);
		}
		SyncManager.TryDelaySync();
	}

	private void RemoveNameEditor()
	{
		base.Children.Remove(_editNameText);
		base.Children.Remove(_editNameBackground);
		_editNameText = null;
		_editNameBackground = null;
		_titleGrid.Visibility = Visibility.Visible;
	}

	private void SetErrorPopup(bool show)
	{
		if (show)
		{
			if (_errorPopup == null && _editNameText != null)
			{
				_errorPopup = new Popup
				{
					StaysOpen = true,
					PlacementTarget = _editNameText,
					Placement = PlacementMode.Bottom,
					AllowsTransparency = true
				};
				ContentControl contentControl = new ContentControl();
				contentControl.SetResourceReference(FrameworkElement.StyleProperty, "PopupContentStyle");
				TextBlock textBlock = new TextBlock
				{
					Text = Utils.GetString("SectionNotValid"),
					FontSize = 11.0,
					Margin = new Thickness(6.0)
				};
				textBlock.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100");
				contentControl.Content = textBlock;
				_errorPopup.Child = contentControl;
				_errorPopup.IsOpen = true;
			}
		}
		else if (_errorPopup != null)
		{
			_errorPopup.IsOpen = false;
			_errorPopup = null;
		}
	}

	private async Task<bool> CheckIfColumnNameExisted(string name)
	{
		if (name == _originalColumnName)
		{
			return false;
		}
		if (_kanban != null)
		{
			return (await _kanban.GetColumnNames()).Contains(name);
		}
		return false;
	}

	private void OnNameTextChanged(object sender, TextChangedEventArgs e)
	{
		if (!string.IsNullOrEmpty(_editNameText.Text.TrimEnd()))
		{
			CheckIfColumnNameValid();
		}
	}

	private void CheckIfColumnNameValid()
	{
		if (!NameUtils.IsValidColumnName(_editNameText.Text))
		{
			SetErrorPopup(show: true);
			_editNameText.SelectAll();
		}
		else
		{
			SetErrorPopup(show: false);
		}
	}

	public bool QuickAddPopupOpened()
	{
		return _quickAddView?.IsInOperation ?? false;
	}

	public bool CloseQuickAddActionPopups()
	{
		QuickAddView quickAddView = _quickAddView;
		if (quickAddView == null || !quickAddView.CloseActionPopups())
		{
			return Utils.FindSingleVisualChildren<ListItemAddView>(this)?.CloseQuickAddActionPopups() ?? false;
		}
		return true;
	}

	public async void OnTaskDrop(DisplayItemModel model)
	{
		ColumnViewModel model2 = GetModel();
		if (model2 != null && model.ColumnId == model2.ColumnId && _taskList != null)
		{
			await _taskList.OnTaskDrop(model);
			ReloadColumn();
		}
	}

	public string GetColumnId()
	{
		return GetModel()?.ColumnId;
	}

	public void ReloadColumn()
	{
		_taskList?.ReLoad(null);
	}

	public async void TryShowTaskDetail(string taskModelId, bool focusTitle = false)
	{
		if (_kanban == null)
		{
			return;
		}
		await Task.Delay(120);
		DisplayItemModel item = _taskList?.ViewModel.Items.FirstOrDefault((DisplayItemModel i) => i.Id == taskModelId);
		if (item != null)
		{
			KanbanItemView kanbanItemView = Utils.FindSingleVisualChildren<KanbanItemView>(_taskList?.GetItemByModel(item));
			if (kanbanItemView != null)
			{
				kanbanItemView.SelectItem(focusTitle);
				return;
			}
			_taskList?.ScrollToItemById(item.Id);
			await Task.Delay(120);
			Utils.FindSingleVisualChildren<KanbanItemView>(_taskList?.GetItemByModel(item))?.SelectItem(focusTitle);
		}
		else
		{
			TaskDetailWindows.ShowTaskWindows(taskModelId);
		}
	}

	public ColumnViewModel GetModel()
	{
		if (base.DataContext != null && base.DataContext is ColumnViewModel result)
		{
			return result;
		}
		return null;
	}

	public string GetDisplayedColor()
	{
		if (!KanbanColumnColorHelper.HasColor(_currentColumnColor))
		{
			return GetColumnColor(GetModel());
		}
		return _currentColumnColor;
	}

	public bool IsPopupOpen()
	{
		return QuickAddPopupOpened();
	}

	public async Task ExpandOrFoldAllTask(bool isOpen)
	{
		if (_taskList != null)
		{
			await _taskList.ExpandOrFoldAllTask();
		}
	}

	public void SetMaxHeight(double maxHeight)
	{
		base.MaxHeight = maxHeight;
		if (_emptyBorder != null)
		{
			base.Height = base.MaxHeight;
		}
	}

	public void SetColumnDragging(bool dragging)
	{
		if (dragging)
		{
			if (_draggingBorder == null)
			{
				_draggingBorder = new Rectangle
				{
					RadiusX = 14.0,
					RadiusY = 14.0
				};
				SetDraggingBackground();
				_draggingBorder.SetValue(Panel.ZIndexProperty, -5);
				_draggingBorder.SetValue(Grid.RowSpanProperty, 3);
				_draggingBorder.Effect = new DropShadowEffect
				{
					Opacity = 0.08,
					BlurRadius = 18.0,
					ShadowDepth = 0.0,
					Direction = 270.0
				};
				base.Children.Add(_draggingBorder);
			}
		}
		else
		{
			base.Children.Remove(_draggingBorder);
			_draggingBorder = null;
		}
	}

	public bool CanDragEvent()
	{
		ColumnViewModel model = GetModel();
		if (model != null)
		{
			if (model.ColumnId != "calendar")
			{
				return model.ColumnId != "course";
			}
			return false;
		}
		return false;
	}

	public void SetShowAdd(bool withAnim = true)
	{
		if (LocalSettings.Settings.ExtraSettings.KbShowAdd)
		{
			ColumnViewModel model = GetModel();
			if (model != null && model.CanAdd && !model.NewAdd)
			{
				InitQuickAddView(withAnim, focus: false);
			}
			else
			{
				HideAddView();
			}
		}
		else if (_quickAddView != null && _quickAddView.IsLostFocus())
		{
			HideAddView();
		}
		SetAddButton(GetModel());
	}
}
