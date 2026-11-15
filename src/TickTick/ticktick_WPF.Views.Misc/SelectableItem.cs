using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Emoji.Wpf;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc;

public class SelectableItem : UserControl
{
	private bool _inCal;

	private Grid _rootGrid;

	private ContentControl _subPopupPlacement;

	private Grid _contentGrid;

	private Border _openBorder;

	private Path _openPath;

	private Rectangle _subSpan;

	private ContentControl _iconContent;

	private Path _icon;

	private EmjTextBlock _emj;

	private EmjTextBlock _title;

	private StackPanel _textStack;

	private ContentControl _rightContent;

	private RadioButton _radio;

	private Path _linkPath;

	private Border _tipBorder;

	private Path _tipPath;

	private Border _checkBorder;

	private Path _checkBox;

	private Grid _circleCheckGrid;

	private Ellipse _circleCheckEllipse;

	private Path _circleCheckPath;

	private Polygon _rightArrow;

	public ContentControl Container { get; set; }

	public Line SplitLine { get; set; }

	public SelectableItem()
	{
		base.Background = Brushes.Transparent;
		base.DataContextChanged += OnDataContextChanged;
		InitUI();
		base.MouseEnter += OnMouseEnter;
		base.MouseLeave += OnMouseLeave;
		base.Unloaded += OnUnloaded;
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is SelectableItemViewModel source)
		{
			PropertyChangedEventManager.RemoveHandler(source, OnItemPropertyChanged, "");
		}
	}

	private ItemSelection GetParent()
	{
		return Utils.FindParent<ItemSelection>(this);
	}

	private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		bool flag = false;
		if (e.OldValue is SelectableItemViewModel selectableItemViewModel)
		{
			PropertyChangedEventManager.RemoveHandler(selectableItemViewModel, OnItemPropertyChanged, "");
			flag = selectableItemViewModel.IsBold;
		}
		if (!(base.DataContext is SelectableItemViewModel selectableItemViewModel2))
		{
			return;
		}
		if (selectableItemViewModel2.InCalFilter && (!_inCal || (flag && !selectableItemViewModel2.IsBold)))
		{
			SetInCal();
		}
		if (selectableItemViewModel2.IsSplit)
		{
			base.Content = SplitLine;
			return;
		}
		base.Content = Container;
		PropertyChangedEventManager.AddHandler(selectableItemViewModel2, OnItemPropertyChanged, "");
		Container.Visibility = (selectableItemViewModel2.IsSplit ? Visibility.Collapsed : Visibility.Visible);
		SetOpenBorder(selectableItemViewModel2.CanExpand && ((selectableItemViewModel2.IsParent && !selectableItemViewModel2.IsSectionGroup) || (selectableItemViewModel2.IsParent && selectableItemViewModel2.IsSectionGroup && base.IsMouseOver)), selectableItemViewModel2.Open);
		SetListIcon(selectableItemViewModel2.ShowIcon, selectableItemViewModel2.Icon != null);
		SetSubItemSpan(selectableItemViewModel2.IsSubItem);
		SetIconColor(selectableItemViewModel2);
		SetTextColor(selectableItemViewModel2);
		SetRightContent(selectableItemViewModel2);
		SetTipIcon(selectableItemViewModel2);
		if (!string.IsNullOrEmpty(selectableItemViewModel2.Desc))
		{
			SetDescText();
		}
		_title.Padding = new Thickness(selectableItemViewModel2.ShowIcon ? 8 : 0, 0.0, 20.0, 0.0);
		_title.FontWeight = (selectableItemViewModel2.IsBold ? FontWeights.Bold : FontWeights.Normal);
		if (selectableItemViewModel2.IsBold && selectableItemViewModel2.InCalFilter)
		{
			_title.FontSize = 12.0;
			_title.MaxHeight = 15.0;
		}
	}

	private void OnOpenClick(object sender, MouseButtonEventArgs e)
	{
		if ((_contentGrid != null && e.GetPosition(_contentGrid).X > 18.0) || base.DataContext is SelectableItemViewModel { CanExpand: false })
		{
			return;
		}
		if (base.DataContext is ListGroupViewModel listGroupViewModel)
		{
			e.Handled = true;
			listGroupViewModel.Open = !listGroupViewModel.Open;
			GetParent()?.OnListGroupChanged(listGroupViewModel);
		}
		else if (base.DataContext is CalendarGroupViewModel calendarGroupViewModel)
		{
			e.Handled = true;
			calendarGroupViewModel.Open = !calendarGroupViewModel.Open;
			GetParent()?.OnCalendarGroupChanged(calendarGroupViewModel);
		}
		else if (!(base.DataContext is TagGroupViewModel) && !(base.DataContext is TeamSectionViewModel) && !(base.DataContext is FilterGroupViewModel))
		{
			e.Handled = true;
			if (base.DataContext is SelectableItemViewModel model)
			{
				GetParent()?.OnFoldChildren(this, model);
			}
		}
	}

	private void OnCheckBoxClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is SelectableItemViewModel { IsSectionGroup: not false } selectableItemViewModel)
		{
			e.Handled = true;
			selectableItemViewModel.Selected = !selectableItemViewModel.Selected;
			selectableItemViewModel.PartSelected = false;
			GetParent()?.OnSectionGroupSelected(selectableItemViewModel);
		}
	}

	private void InitUI()
	{
		Container = new ContentControl
		{
			Margin = new Thickness(0.0, 0.0, 2.0, 0.0)
		};
		Container.SetResourceReference(FrameworkElement.StyleProperty, "ListHoverSelectedItemStyle");
		SplitLine = new Line
		{
			Margin = new Thickness(0.0, 4.0, 0.0, 4.0)
		};
		SplitLine.SetResourceReference(FrameworkElement.StyleProperty, "SplitLineStyle");
		_rootGrid = new Grid();
		Container.Content = _rootGrid;
		_rootGrid.SetResourceReference(FrameworkElement.MinHeightProperty, "Height36");
		_contentGrid = new Grid
		{
			Margin = new Thickness(0.0, 5.0, 6.0, 5.0),
			HorizontalAlignment = HorizontalAlignment.Left
		};
		_contentGrid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(12.0)
		});
		_contentGrid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		_contentGrid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		_contentGrid.ColumnDefinitions.Add(new ColumnDefinition());
		_rootGrid.Children.Add(_contentGrid);
		InitText();
		_rightContent = new ContentControl
		{
			HorizontalAlignment = HorizontalAlignment.Right,
			Width = 32.0,
			Background = Brushes.Transparent
		};
		_rootGrid.Children.Add(_rightContent);
	}

	private void SetInCal()
	{
		_inCal = true;
		_rootGrid.MinHeight = 36.0;
		_title.FontSize = 13.0;
		_title.MaxHeight = 20.0;
		_rightContent.Margin = new Thickness(0.0, 0.0, 8.0, 0.0);
		Container.Margin = new Thickness(1.0, 0.0, 0.0, 0.0);
	}

	private void InitText()
	{
		_textStack = new StackPanel
		{
			VerticalAlignment = VerticalAlignment.Center
		};
		_textStack.SetValue(Grid.ColumnProperty, 3);
		_contentGrid.Children.Add(_textStack);
		_title = new EmjTextBlock
		{
			TextWrapping = TextWrapping.Wrap,
			ClipToBounds = true,
			VerticalAlignment = VerticalAlignment.Center,
			TextTrimming = TextTrimming.CharacterEllipsis,
			Margin = new Thickness(0.0, 0.0, 8.0, 0.0)
		};
		_textStack.Children.Add(_title);
		_title.SetBinding(EmjTextBlock.TextProperty, "Title");
		_title.SetResourceReference(TextBlock.FontSizeProperty, "Font13");
		_title.SetResourceReference(FrameworkElement.MaxHeightProperty, "Height18");
		_title.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100_80");
	}

	private void SetDescText()
	{
		if (_textStack.Children.Count == 1)
		{
			EmjTextBlock emjTextBlock = new EmjTextBlock
			{
				TextWrapping = TextWrapping.Wrap,
				ClipToBounds = true,
				Height = 14.0,
				Margin = new Thickness(8.0, 2.0, 50.0, 0.0),
				TextTrimming = TextTrimming.CharacterEllipsis,
				FontSize = 11.0
			};
			emjTextBlock.SetBinding(EmjTextBlock.TextProperty, "Desc");
			emjTextBlock.SetBinding(UIElement.VisibilityProperty, "DescVisibility");
			emjTextBlock.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity40");
			_textStack.Children.Add(emjTextBlock);
		}
	}

	public ContentControl GetSubPopupPlacement()
	{
		if (_subPopupPlacement == null)
		{
			_subPopupPlacement = new ContentControl
			{
				Margin = new Thickness(2.0, 0.0, 0.0, 0.0)
			};
			_rootGrid.Children.Insert(0, _subPopupPlacement);
		}
		return _subPopupPlacement;
	}

	private void OnMouseEnter(object sender, MouseEventArgs e)
	{
		if (base.DataContext is SelectableItemViewModel { CanExpand: not false, IsParent: not false, IsSectionGroup: not false } selectableItemViewModel)
		{
			SetOpenBorder(show: true, selectableItemViewModel.Open);
		}
	}

	private void OnMouseLeave(object sender, MouseEventArgs e)
	{
		if (base.DataContext is SelectableItemViewModel { CanExpand: not false, IsParent: not false, IsSectionGroup: not false } selectableItemViewModel)
		{
			SetOpenBorder(show: false, selectableItemViewModel.Open);
		}
	}

	private void SetOpenBorder(bool show, bool isOpen)
	{
		if (show)
		{
			if (_openBorder == null)
			{
				_openBorder = new Border
				{
					Width = 18.0,
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Stretch,
					Margin = new Thickness(-1.0, 0.0, 0.0, 0.0),
					Background = Brushes.Transparent
				};
				_openBorder.PreviewMouseLeftButtonUp += OnOpenClick;
				_contentGrid.Children.Add(_openBorder);
				_openPath = UiUtils.GetArrow(12.0, (!isOpen) ? (-90) : 0, "BaseColorOpacity40");
				_openPath.RenderTransformOrigin = new Point(0.5, 0.5);
				_openPath.VerticalAlignment = VerticalAlignment.Center;
				_openPath.Stretch = Stretch.Fill;
				_openPath.Margin = new Thickness(0.0);
				_openBorder.Child = _openPath;
			}
			_openPath.RenderTransform = new RotateTransform((!isOpen) ? (-90) : 0);
			_openBorder.Visibility = Visibility.Visible;
		}
		else if (_openBorder != null)
		{
			_openBorder.Visibility = Visibility.Collapsed;
		}
	}

	private void SetListIcon(bool show, bool isIcon)
	{
		if (show)
		{
			if (_iconContent == null)
			{
				_iconContent = new ContentControl
				{
					Width = 18.0,
					Background = Brushes.Transparent
				};
				_iconContent.SetValue(Grid.ColumnProperty, 2);
				_contentGrid.Children.Add(_iconContent);
				_icon = new Path
				{
					Width = 18.0,
					Height = 18.0,
					VerticalAlignment = VerticalAlignment.Center,
					Stretch = Stretch.Uniform
				};
				_icon.SetBinding(Path.DataProperty, "Icon");
				_icon.SetResourceReference(Shape.FillProperty, "BaseColorOpacity100");
			}
			if (isIcon)
			{
				_iconContent.Content = _icon;
			}
			else
			{
				if (_emj == null)
				{
					_emj = new EmjTextBlock
					{
						HorizontalAlignment = HorizontalAlignment.Left,
						Margin = new Thickness(-1.0, 0.0, 0.0, 0.0)
					};
					_emj.SetResourceReference(FrameworkElement.StyleProperty, "Body01");
					_emj.SetBinding(EmjTextBlock.TextProperty, "Emoji");
				}
				_iconContent.Content = _emj;
			}
			_iconContent.Visibility = Visibility.Visible;
		}
		else if (_iconContent != null)
		{
			_iconContent.Visibility = Visibility.Collapsed;
		}
	}

	private void SetSubItemSpan(bool show)
	{
		if (show)
		{
			if (_subSpan == null)
			{
				_subSpan = new Rectangle
				{
					Width = 12.0
				};
				_subSpan.SetValue(Grid.ColumnProperty, 1);
				_contentGrid.Children.Add(_subSpan);
			}
			_subSpan.Visibility = Visibility.Visible;
		}
		else if (_subSpan != null)
		{
			_subSpan.Visibility = Visibility.Collapsed;
		}
	}

	private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (!(base.DataContext is SelectableItemViewModel selectableItemViewModel))
		{
			return;
		}
		switch (e.PropertyName)
		{
		case "Open":
			if (_openPath != null)
			{
				_openPath.RenderTransform = new RotateTransform((!selectableItemViewModel.Open) ? (-90) : 0);
			}
			break;
		case "Highlighted":
			SetSelectIconColor(selectableItemViewModel);
			SetIconColor(selectableItemViewModel);
			SetTextColor(selectableItemViewModel);
			break;
		case "ShowCheckBox":
			SetRightContent(selectableItemViewModel);
			break;
		}
	}

	private void SetSelectIconColor(SelectableItemViewModel model)
	{
		_checkBox?.SetResourceReference(Shape.FillProperty, model.Highlighted ? "TextAccentColor" : "BaseColorOpacity40");
		_rightArrow?.SetResourceReference(Shape.FillProperty, model.Highlighted ? "TextAccentColor" : "BaseColorOpacity40");
	}

	private void SetIconColor(SelectableItemViewModel model)
	{
		if (model.IsGray)
		{
			_icon?.SetResourceReference(Shape.FillProperty, "BaseSolidColorOpacity40");
		}
		else if (model.Highlighted && !model.InCalFilter)
		{
			_icon?.SetResourceReference(Shape.FillProperty, "TextAccentColor");
		}
		else
		{
			_icon?.SetResourceReference(Shape.FillProperty, "BaseColorOpacity100");
		}
	}

	private void SetTextColor(SelectableItemViewModel model)
	{
		if (model.IsGray)
		{
			_title?.SetResourceReference(TextBlock.ForegroundProperty, "BaseSolidColorOpacity40");
		}
		else if (model.IsTeam)
		{
			_title?.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity60");
		}
		else if (model.Highlighted && !model.InCalFilter)
		{
			_title?.SetResourceReference(TextBlock.ForegroundProperty, "TextAccentColor");
		}
		else
		{
			_title?.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100");
		}
	}

	private void SetRightContent(SelectableItemViewModel model)
	{
		if (model.InCalFilter && !model.CanMultiSelect)
		{
			_rightContent.Content = GetRadioButton();
		}
		else if (model.Linked)
		{
			_rightContent.Content = GetLinkPath();
		}
		else if (model.ShowSubOnSide)
		{
			_rightContent.Content = GetRightArrow();
		}
		else if (model.ShowCheckBox)
		{
			_rightContent.Content = GetCheckBox(model);
		}
		else
		{
			_rightContent.Content = null;
		}
		SetSelectIconColor(model);
		SetCircleCheckState(model);
	}

	private void SetTipIcon(SelectableItemViewModel model)
	{
		if (string.IsNullOrEmpty(model.ToolTip))
		{
			if (_tipBorder != null)
			{
				_tipBorder.Visibility = Visibility.Collapsed;
			}
			return;
		}
		if (_tipBorder == null)
		{
			_tipBorder = new Border
			{
				Width = 18.0,
				Height = 18.0,
				Background = Brushes.Transparent,
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(0.0, 0.0, 32.0, 0.0)
			};
			_tipPath = new Path
			{
				Width = 14.0,
				Height = 14.0,
				Stretch = Stretch.Uniform,
				Data = Utils.GetGeometry("Ic.Common.Help")
			};
			_tipPath.SetResourceReference(Shape.FillProperty, "BaseColorOpacity40");
			_tipBorder.Child = _tipPath;
			_rootGrid.Children.Add(_tipBorder);
		}
		_tipBorder.ToolTip = model.ToolTip;
		_tipBorder.Visibility = Visibility.Visible;
	}

	private Polygon GetRightArrow()
	{
		if (_rightArrow == null)
		{
			_rightArrow = new Polygon
			{
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Right,
				Margin = new Thickness(0.0, 0.0, 8.0, 0.0),
				RenderTransformOrigin = new Point(0.5, 0.5),
				Width = 12.0,
				Height = 12.0,
				Points = (App.Instance.FindResource("ArrowPolygon") as Polygon)?.Points
			};
			_rightArrow.RenderTransform = new RotateTransform
			{
				Angle = -90.0
			};
			_rightArrow.SetResourceReference(Shape.FillProperty, "BaseColorOpacity40");
		}
		return _rightArrow;
	}

	private Path GetLinkPath()
	{
		if (_linkPath == null)
		{
			_linkPath = new Path
			{
				Width = 14.0,
				Height = 14.0,
				Stretch = Stretch.Uniform,
				Data = Utils.GetGeometry("Ic.Focus.LinkToTask"),
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Center
			};
			_linkPath.SetResourceReference(Shape.FillProperty, "BaseColorOpacity40");
		}
		return _linkPath;
	}

	private Border GetCheckBox(SelectableItemViewModel model)
	{
		if (_checkBorder == null)
		{
			_checkBorder = new Border
			{
				Background = Brushes.Transparent,
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Center
			};
			_checkBorder.PreviewMouseLeftButtonUp += OnCheckBoxClick;
			_checkBox = new Path
			{
				Stretch = Stretch.Uniform
			};
			_checkBox.SetResourceReference(Shape.FillProperty, "BaseColorOpacity40");
			_checkBox.SetBinding(Path.DataProperty, "CheckBox");
		}
		if (model.UseCircleCheck)
		{
			_checkBorder.Child = GetCircleCheck();
		}
		else
		{
			_checkBox.Width = (model.InCalFilter ? 17 : 14);
			_checkBox.Height = 17.0;
			_checkBorder.Child = _checkBox;
		}
		return _checkBorder;
	}

	private Grid GetCircleCheck()
	{
		if (_circleCheckGrid == null)
		{
			_circleCheckGrid = new Grid
			{
				Width = 19.0,
				Height = 19.0,
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Center
			};
			_circleCheckEllipse = new Ellipse
			{
				Width = 19.0,
				Height = 19.0,
				StrokeThickness = 1.5
			};
			_circleCheckEllipse.SetResourceReference(Shape.StrokeProperty, "ThemeCheckBorderColor");
			_circleCheckEllipse.SetResourceReference(Shape.FillProperty, "TextAccentColor");
			_circleCheckGrid.Children.Add(_circleCheckEllipse);
			_circleCheckPath = new Path
			{
				Width = 19.0,
				Height = 19.0,
				Fill = Brushes.White,
				Data = Utils.GetGeometry("Ic.Common.CheckedInside"),
				Stretch = Stretch.Uniform
			};
			_circleCheckGrid.Children.Add(_circleCheckPath);
		}
		return _circleCheckGrid;
	}

	private void SetCircleCheckState(SelectableItemViewModel model)
	{
		if (model.UseCircleCheck && _circleCheckGrid != null)
		{
			if (model.Selected)
			{
				_circleCheckEllipse.SetResourceReference(Shape.StrokeProperty, "ThemeCheckBorderColor");
				_circleCheckEllipse.SetResourceReference(Shape.FillProperty, "TextAccentColor");
				_circleCheckPath.Visibility = Visibility.Visible;
			}
			else
			{
				_circleCheckEllipse.SetResourceReference(Shape.StrokeProperty, "BaseColorOpacity40");
				_circleCheckEllipse.Fill = Brushes.Transparent;
				_circleCheckPath.Visibility = Visibility.Collapsed;
			}
		}
	}

	private RadioButton GetRadioButton()
	{
		if (_radio == null)
		{
			_radio = new RadioButton
			{
				IsHitTestVisible = false,
				Width = 18.0,
				Height = 18.0,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};
			_radio.SetBinding(ToggleButton.IsCheckedProperty, "Selected");
			_radio.SetResourceReference(FrameworkElement.StyleProperty, "SelectableItemRadioButtonStyle");
		}
		return _radio;
	}
}
