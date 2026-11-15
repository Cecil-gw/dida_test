using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using Emoji.Wpf;
using TickTickUtils.Lang;
using ticktick_WPF.Converter;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.TaskList;

namespace ticktick_WPF.Views.Timeline;

public class TimelineGroup : Border, IComponentConnector
{
	public static readonly DependencyProperty PreviewDataProperty = DependencyProperty.Register("PreviewData", typeof(object), typeof(TimelineGroup), new PropertyMetadata((object)null));

	private Point _dragStartPoint;

	private Grid _container;

	private Border _foldBorder;

	private Border _moreButton;

	private EscPopup _morePopup;

	private TextBox _inputText;

	private Popup _errorPopup;

	private Grid _root;

	private EmjTextBlock _emjText;

	internal TimelineGroup Root;

	private bool _contentLoaded;

	public object PreviewData
	{
		get
		{
			return GetValue(PreviewDataProperty);
		}
		set
		{
			SetValue(PreviewDataProperty, value);
		}
	}

	public TimelineGroup()
	{
		InitializeComponent();
		LoadContent();
		base.DataContextChanged += OnDataBind;
	}

	private void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (e.NewValue is TimelineGroupViewModel timelineGroupViewModel)
		{
			if (timelineGroupViewModel.Editable && _moreButton == null)
			{
				LoadMoreButton();
				LoadEditContent();
			}
			if (_emjText != null)
			{
				_emjText.Margin = new Thickness((timelineGroupViewModel.Editable || !timelineGroupViewModel.IsAvatar || !TaskUtils.IsAvatarUrlEmpty(timelineGroupViewModel.AvatarUrl)) ? 2 : 8, 0.0, 0.0, 0.0);
			}
		}
	}

	private void LoadMoreButton()
	{
		_moreButton = new Border();
		_morePopup = new EscPopup
		{
			PlacementTarget = _moreButton,
			Placement = PlacementMode.Bottom,
			PopupAnimation = PopupAnimation.Fade,
			StaysOpen = false
		};
		_morePopup.Closed += OnPopupClosed;
		BoolOrVisibilityConverter converter = new BoolOrVisibilityConverter();
		MultiBinding multiBinding = new MultiBinding();
		multiBinding.Converter = converter;
		multiBinding.Bindings.Add(new Binding("IsMouseOver")
		{
			Source = _container
		});
		multiBinding.Bindings.Add(new Binding("IsOpen")
		{
			Source = _morePopup
		});
		_moreButton.SetBinding(UIElement.VisibilityProperty, multiBinding);
		_moreButton.SetResourceReference(FrameworkElement.StyleProperty, "SmoothHoverBorderStyle40_100");
		_moreButton.VerticalAlignment = VerticalAlignment.Center;
		_moreButton.MouseLeftButtonUp += OnMoreClick;
		_moreButton.SetValue(Grid.ColumnProperty, 3);
		Path path = new Path
		{
			Margin = new Thickness(0.0, 0.0, 5.0, 0.0)
		};
		path.SetResourceReference(Path.DataProperty, "Ic.ProjectList.More");
		path.SetResourceReference(FrameworkElement.StyleProperty, "TimeLineGroupMorePathStyle");
		_moreButton.Child = path;
		_container.Children.Add(_moreButton);
		_container.Children.Add(_morePopup);
	}

	private void LoadEditContent()
	{
		Grid grid = new Grid();
		grid.SetResourceReference(FrameworkElement.StyleProperty, "TimeLineGroupEditGridStyle");
		grid.IsVisibleChanged += OnEditBoxVisibleChanged;
		_inputText = new TextBox();
		_inputText.Margin = new Thickness(21.0, 3.0, 10.0, 0.0);
		_inputText.FontSize = 13.0;
		_inputText.Height = 20.0;
		_inputText.MaxLength = 64;
		_inputText.VerticalAlignment = VerticalAlignment.Top;
		_inputText.SetResourceReference(FrameworkElement.StyleProperty, "NoBorderTextBoxWithHint");
		_inputText.LostFocus += OnLostFocus;
		_inputText.TextChanged += OnNameTextChanged;
		_inputText.PreviewKeyDown += OnKeyDown;
		grid.Children.Add(_inputText);
		_errorPopup = new Popup
		{
			StaysOpen = false,
			AllowsTransparency = true,
			Placement = PlacementMode.Relative,
			PlacementTarget = _inputText,
			VerticalOffset = 30.0
		};
		ContentControl contentControl = new ContentControl();
		contentControl.SetResourceReference(FrameworkElement.StyleProperty, "PopupContentStyle");
		contentControl.Margin = new Thickness(0.0);
		TextBlock textBlock = new TextBlock
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(6.0),
			Width = 160.0,
			FontSize = 12.0
		};
		textBlock.SetResourceReference(TextBlock.TextProperty, "SectionNotValid");
		textBlock.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100_80");
		contentControl.Content = textBlock;
		_errorPopup.Child = contentControl;
		grid.Children.Add(_errorPopup);
		Border border = new Border
		{
			VerticalAlignment = VerticalAlignment.Top,
			Height = 22.0,
			BorderThickness = new Thickness(0.0, 0.0, 0.0, 1.0),
			Margin = new Thickness(20.0, 3.0, 10.0, 0.0)
		};
		border.SetResourceReference(Border.BorderBrushProperty, "PrimaryColor");
		grid.Children.Add(border);
		_root.Children.Add(grid);
	}

	private void LoadContent()
	{
		_root = new Grid
		{
			Height = 40.0,
			VerticalAlignment = VerticalAlignment.Top,
			Background = Brushes.Transparent
		};
		_root.MouseMove += OnDragMove;
		Child = _root;
		Border border = new Border();
		border.SetResourceReference(FrameworkElement.StyleProperty, "TimelineGroupTopLineStyle");
		_root.Children.Add(border);
		LoadContainer();
		LoadFoldIcon();
		Ellipse ellipse = new Ellipse();
		ellipse.SetResourceReference(FrameworkElement.StyleProperty, "TimeLineGroupAvatarStyle");
		_container.Children.Add(ellipse);
		LoadText();
	}

	private void LoadText()
	{
		_emjText = new EmjTextBlock();
		_emjText.SetResourceReference(FrameworkElement.StyleProperty, "TimeLineGroupTextStyle");
		_container.Children.Add(_emjText);
	}

	private void LoadContainer()
	{
		_container = new Grid
		{
			Background = Brushes.Transparent,
			Height = 25.0,
			Cursor = Cursors.Hand,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(0.0, 1.0, 0.0, 0.0)
		};
		_container.MouseLeftButtonUp += OnContainerMouseUp;
		_container.MouseLeftButtonDown += OnDragMouseDown;
		_container.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(0.0, GridUnitType.Auto)
		});
		_container.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(0.0, GridUnitType.Auto),
			MinWidth = 2.0
		});
		_container.ColumnDefinitions.Add(new ColumnDefinition());
		_container.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(0.0, GridUnitType.Auto)
		});
		_root.Children.Add(_container);
	}

	private void LoadFoldIcon()
	{
		_foldBorder = new Border();
		_foldBorder.VerticalAlignment = VerticalAlignment.Center;
		_foldBorder.SetValue(Grid.ColumnProperty, 0);
		_foldBorder.Margin = new Thickness(5.0, 0.0, 0.0, 0.0);
		_foldBorder.SetBinding(UIElement.VisibilityProperty, new Binding("ShowArrow"));
		_foldBorder.SetResourceReference(FrameworkElement.StyleProperty, "SmoothHoverBorderStyle40_100");
		_foldBorder.MouseLeftButtonUp += OnColumnOpenCloseMouseUp;
		Path path = new Path();
		path.SetResourceReference(FrameworkElement.StyleProperty, "TimelineGroupFoldPathStyle");
		_foldBorder.Child = path;
		_container.Children.Add(_foldBorder);
	}

	private async void OnColumnOpenCloseMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is TimelineGroupViewModel timelineGroupViewModel)
		{
			await timelineGroupViewModel.UpdateIsOpen(!timelineGroupViewModel.IsOpen);
		}
	}

	private void OnNameTextChanged(object sender, TextChangedEventArgs e)
	{
		if (!NameUtils.IsValidColumnName(_inputText.Text.Trim()))
		{
			_errorPopup.IsOpen = true;
			_inputText.SelectAll();
		}
		else
		{
			_errorPopup.IsOpen = false;
		}
	}

	private void OnMoreClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		Mouse.Capture(null);
		if (base.DataContext is TimelineGroupViewModel)
		{
			CustomSectionOption customSectionOption = new CustomSectionOption(_morePopup);
			SetOptionAction(customSectionOption);
			customSectionOption.Show();
		}
	}

	protected virtual void SetOptionAction(CustomSectionOption option)
	{
		option.SetAction(AddSectionUpper, AddSectionUnder, Rename, Delete);
	}

	private void AddSectionUpper()
	{
		_morePopup.IsOpen = false;
		if (base.DataContext is TimelineGroupViewModel timelineGroupViewModel)
		{
			timelineGroupViewModel.Parent?.AddNewColumn(timelineGroupViewModel, below: false);
		}
	}

	private void AddSectionUnder()
	{
		_morePopup.IsOpen = false;
		if (base.DataContext is TimelineGroupViewModel timelineGroupViewModel)
		{
			timelineGroupViewModel.Parent?.AddNewColumn(timelineGroupViewModel, below: true);
		}
	}

	private async void Rename()
	{
		_morePopup.IsOpen = false;
		if (base.DataContext is TimelineGroupViewModel timelineGroupViewModel)
		{
			timelineGroupViewModel.Editing = true;
			_inputText.Text = timelineGroupViewModel.Title;
		}
	}

	private void OnLostFocus(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is TimelineGroupViewModel { Editing: not false } timelineGroupViewModel)
		{
			string text = _inputText.Text.Trim();
			timelineGroupViewModel.Editing = false;
			if (!string.IsNullOrEmpty(text) && NameUtils.IsValidColumnName(text))
			{
				SaveSectionName(timelineGroupViewModel, text);
			}
			else if (string.IsNullOrEmpty(timelineGroupViewModel.Id))
			{
				timelineGroupViewModel.Parent.RemoveGroup(timelineGroupViewModel);
			}
		}
		_errorPopup.IsOpen = false;
	}

	private async Task SaveSectionName(TimelineGroupViewModel columnModel, string saveName)
	{
		if (string.IsNullOrEmpty(saveName) || saveName == columnModel.Title)
		{
			return;
		}
		if (await CheckIfColumnNameExisted(columnModel, saveName))
		{
			Utils.Toast(Utils.GetString("SectionNameExisted"));
			if (string.IsNullOrEmpty(columnModel.Id))
			{
				columnModel.Parent.RemoveGroup(columnModel);
			}
			return;
		}
		if (!string.IsNullOrEmpty(columnModel.Id))
		{
			await ColumnDao.SaveColumnName(saveName, columnModel.Id);
		}
		else
		{
			columnModel.Id = (await ColumnDao.AddColumn(saveName, columnModel.ProjectId, columnModel.SortOrder, columnModel.Id)).id;
		}
		columnModel.Title = saveName;
		SyncManager.TryDelaySync();
	}

	private async Task<bool> CheckIfColumnNameExisted(TimelineGroupViewModel model, string name)
	{
		if (name == model.Title)
		{
			return false;
		}
		return (await ColumnDao.GetColumnsByProjectId(model.ProjectId))?.Exists((ColumnModel c) => c.name == name) ?? false;
	}

	private async void Delete()
	{
		_morePopup.IsOpen = false;
		object dataContext = base.DataContext;
		if (!(dataContext is TimelineGroupViewModel { Editable: not false } model))
		{
			return;
		}
		string text = await ColumnDao.GetProjectDefaultColumnId(model.ProjectId);
		string targetColumnId = null;
		if ((await TaskDao.GetTasksInProjectOrColumn(model.ProjectId, model.Id, text == model.Id)).Count > 0)
		{
			DeleteColumnWindow deleteColumnWindow = new DeleteColumnWindow(model.ProjectId, model.Id)
			{
				Owner = Window.GetWindow(this)
			};
			bool? flag = deleteColumnWindow.ShowDialog();
			App.Window.Activate();
			if (flag != true)
			{
				return;
			}
			targetColumnId = deleteColumnWindow.TargetColumnId;
		}
		await PerformDeleteColumn(model.Id, targetColumnId);
	}

	private async Task PerformDeleteColumn(string columnId, string targetColumnId = null)
	{
		object dataContext = base.DataContext;
		if (dataContext is TimelineGroupViewModel { Editable: not false } model)
		{
			await ColumnDao.DeleteColumn(model.Id);
			string primaryColumnId = await ColumnDao.GetProjectDefaultColumnId(model.ProjectId);
			UtilLog.Info("TimelineGroup.Delete : " + model.Id);
			if (targetColumnId != null)
			{
				await TaskService.MoveColumnAsync(columnId, model.ProjectId, model.ProjectId, targetColumnId);
			}
			else
			{
				await TaskService.BatchDeleteTaskInColumn(model.ProjectId, model.Id, primaryColumnId);
			}
			Utils.Toast(Utils.GetString("Deleted"));
			SyncManager.TryDelaySync();
			model.Parent.RemoveGroup(model);
		}
	}

	private void OnPopupClosed(object sender, EventArgs e)
	{
	}

	private void OnDragMouseDown(object sender, MouseButtonEventArgs e)
	{
		if ((_foldBorder == null || !_foldBorder.IsMouseOver) && (_moreButton == null || !_moreButton.IsMouseOver))
		{
			_dragStartPoint = e.GetPosition(this);
		}
	}

	private void OnDragMove(object sender, MouseEventArgs e)
	{
		if (_dragStartPoint != default(Point) && e.LeftButton == MouseButtonState.Pressed)
		{
			if (base.DataContext is TimelineGroupViewModel { Editable: not false } timelineGroupViewModel)
			{
				Point position = e.GetPosition(this);
				if (Math.Abs(position.X - _dragStartPoint.X) > 4.0 || Math.Abs(position.Y - _dragStartPoint.Y) > 4.0)
				{
					Utils.FindParent<TimelineContainer>(this)?.ShowDragColumnPopup(timelineGroupViewModel, e);
				}
			}
		}
		else
		{
			_dragStartPoint = default(Point);
		}
	}

	private void OnContainerMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (_dragStartPoint != default(Point))
		{
			_dragStartPoint = default(Point);
			if (base.DataContext is TimelineGroupViewModel { ShowArrow: Visibility.Visible } timelineGroupViewModel)
			{
				timelineGroupViewModel.UpdateIsOpen(!timelineGroupViewModel.IsOpen);
			}
		}
	}

	private async void OnEditBoxVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (base.DataContext is TimelineGroupViewModel { Editing: not false } timelineGroupViewModel)
		{
			_inputText.Tag = (string.IsNullOrEmpty(timelineGroupViewModel.Id) ? Utils.GetString("NewColumn") : null);
			await Task.Delay(20);
			_inputText.Focus();
			_inputText.SelectAll();
		}
	}

	private void OnKeyDown(object sender, KeyEventArgs e)
	{
		switch (e.Key)
		{
		case Key.Escape:
			if (base.DataContext is TimelineGroupViewModel { Editing: not false } timelineGroupViewModel2)
			{
				timelineGroupViewModel2.Editing = false;
				if (string.IsNullOrEmpty(timelineGroupViewModel2.Id))
				{
					timelineGroupViewModel2.Parent.RemoveGroup(timelineGroupViewModel2);
				}
			}
			_errorPopup.IsOpen = false;
			break;
		case Key.Return:
			if (base.DataContext is TimelineGroupViewModel { Editing: not false } timelineGroupViewModel)
			{
				string text = _inputText.Text.Trim();
				timelineGroupViewModel.Editing = false;
				if (!string.IsNullOrEmpty(text) && NameUtils.IsValidColumnName(text))
				{
					SaveSectionName(timelineGroupViewModel, text);
				}
				else if (string.IsNullOrEmpty(timelineGroupViewModel.Id))
				{
					timelineGroupViewModel.Parent.RemoveGroup(timelineGroupViewModel);
				}
			}
			_errorPopup.IsOpen = false;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/timeline/timelinegroup.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 1)
		{
			Root = (TimelineGroup)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
