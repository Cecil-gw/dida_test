using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using ticktick_WPF.Converter;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Print;

public class TaskListPrintView : UserControl, IComponentConnector
{
	private readonly TaskListPrintViewModel _printModel;

	private bool _isDetail;

	private ResourceDictionary _resourceDictionary;

	private SolidColorBrush _primaryPrintColor = ThemeUtil.GetColor("PrintTextColorPrimary");

	private SolidColorBrush _secondaryPrintColor = ThemeUtil.GetColor("PrintTextColorSecondary");

	private ItemIconConverter _iconConvert = new ItemIconConverter();

	private ItemIconColorConverter _iconColorConvert = new ItemIconColorConverter();

	internal RichTextBox RichTextBox;

	internal FlowDocument Doc;

	internal Run ListName;

	private bool _contentLoaded;

	public TaskListPrintView(TaskListPrintViewModel model, bool isDetail)
	{
		_printModel = model;
		_isDetail = isDetail;
		InitializeComponent();
		List<ResourceDictionary> source = Application.Current?.Resources.MergedDictionaries.ToList();
		string requestedCulture = "Resource\\icons_light.xaml";
		_resourceDictionary = source.FirstOrDefault((ResourceDictionary d) => d.Source.OriginalString.Equals(requestedCulture));
		RenderDocument();
		RichTextBox.FontFamily = ((LocalSettings.Settings.ExtraSettings.AppFontFamily == "SourceHansansSC_CN") ? new FontFamily("Microsoft YaHei UI") : LocalSettings.Settings.FontFamily);
	}

	private void RenderDocument()
	{
		if (_printModel == null)
		{
			return;
		}
		ListName.Text = _printModel.ListName;
		if (_printModel.TaskPrintViewModels == null)
		{
			return;
		}
		foreach (TaskPrintViewModel taskPrintViewModel in _printModel.TaskPrintViewModels)
		{
			GetTable(taskPrintViewModel);
		}
	}

	private void GetTable(TaskPrintViewModel model)
	{
		Table table = new Table();
		TableColumn tableColumn = new TableColumn
		{
			Width = new GridLength(25 + model.Level * 16)
		};
		TableColumn item = new TableColumn
		{
			Width = GridLength.Auto
		};
		TableColumn tableColumn2 = new TableColumn();
		table.Columns.Add(tableColumn);
		table.Columns.Add(item);
		table.Columns.Add(tableColumn2);
		TableRowGroup tableRowGroup = new TableRowGroup();
		TableRow tableRow = new TableRow();
		TableCell item2;
		TableCell item3;
		TableCell item4;
		switch (model.Type)
		{
		case DisplayType.Section:
			item2 = new TableCell(new BlockUIContainer(new Path
			{
				Data = ((Path)FindResource("ic_svg_path_arrow")).Data,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Width = 9.0,
				Height = 6.0,
				Stretch = Stretch.Fill,
				Margin = new Thickness(0.0, 5.0, 0.0, 0.0),
				Fill = new SolidColorBrush(Colors.Black)
				{
					Opacity = 0.36
				},
				RenderTransformOrigin = new Point(0.5, 0.5),
				RenderTransform = new RotateTransform((!model.IsOpen) ? 270 : 0)
			}));
			item3 = new TableCell(new Paragraph(new Run(model.Title))
			{
				Foreground = _primaryPrintColor
			});
			tableColumn.Width = new GridLength(20.0);
			tableColumn2.Width = new GridLength(60.0);
			item4 = (model.IsOpen ? new TableCell() : new TableCell(new BlockUIContainer(new TextBlock
			{
				Text = model.Count.ToString(),
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Top
			})));
			table.Padding = new Thickness(0.0, 3.0, 0.0, 4.0);
			break;
		case DisplayType.LoadMore:
			item2 = new TableCell(new Paragraph(new Run(model.Title)
			{
				Foreground = _secondaryPrintColor
			}));
			tableColumn.Width = new GridLength(150.0);
			item3 = new TableCell();
			item4 = new TableCell();
			break;
		default:
		{
			Path checkboxPath = GetCheckboxPath(model);
			checkboxPath.Margin = new Thickness(model.Level * 16, 2.0, 0.0, 0.0);
			item2 = new TableCell(new BlockUIContainer(checkboxPath));
			TitleRightColumnView titleRightColumnView = new TitleRightColumnView(new RightColumnViewModel(model, _printModel.IsNormal));
			item3 = new TableCell(new Paragraph(new Run(model.Title))
			{
				Foreground = ((model.Status != 0) ? _secondaryPrintColor : _primaryPrintColor),
				FontSize = 13.0,
				LineHeight = 10.0,
				Padding = new Thickness(0.0, 1.0, 0.0, 4.0),
				TextDecorations = ((model.Status != 0) ? TextDecorations.Strikethrough : null)
			});
			item4 = new TableCell(new BlockUIContainer(titleRightColumnView));
			tableColumn2.Width = new GridLength(titleRightColumnView.NeedWidth);
			break;
		}
		}
		tableRow.Cells.Add(item2);
		tableRow.Cells.Add(item3);
		tableRow.Cells.Add(item4);
		tableRowGroup.Rows.Add(tableRow);
		if (_isDetail)
		{
			GetContentOrDesc(model, tableRowGroup);
		}
		if (model.Type != DisplayType.Section && model.Type != DisplayType.LoadMore && (!_isDetail || model.SubtaskPrintViewModels == null || model.SubtaskPrintViewModels.Count == 0))
		{
			TableRow lineRow = GetLineRow();
			tableRowGroup.Rows.Add(lineRow);
		}
		table.RowGroups.Add(tableRowGroup);
		Doc.Blocks.Add(table);
		List<SubtaskPrintViewModel> subtaskPrintViewModels = model.SubtaskPrintViewModels;
		if (subtaskPrintViewModels != null && subtaskPrintViewModels.Count > 0)
		{
			TryAddSubTaskTable(model);
		}
	}

	private TableRow GetLineRow()
	{
		TableRow tableRow = new TableRow();
		TableCell item = new TableCell(new BlockUIContainer(new Border
		{
			Height = 3.0,
			BorderBrush = ThemeUtil.GetAlphaColor("#191919", 8),
			BorderThickness = new Thickness(0.0, 0.0, 0.0, 1.0)
		}))
		{
			ColumnSpan = 2
		};
		tableRow.Cells.Add(new TableCell());
		tableRow.Cells.Add(item);
		return tableRow;
	}

	private void TryAddSubTaskTable(TaskPrintViewModel model)
	{
		Table table = new Table();
		TableColumn item = new TableColumn
		{
			Width = new GridLength(16 * model.Level + 50)
		};
		TableColumn item2 = new TableColumn
		{
			Width = GridLength.Auto
		};
		TableColumn item3 = new TableColumn
		{
			Width = new GridLength(100.0)
		};
		table.Columns.Add(item);
		table.Columns.Add(item2);
		table.Columns.Add(item3);
		TableRowGroup tableRowGroup = new TableRowGroup();
		foreach (SubtaskPrintViewModel subtaskPrintViewModel in model.SubtaskPrintViewModels)
		{
			GetItemRow(subtaskPrintViewModel, tableRowGroup, model.Level);
		}
		table.RowGroups.Add(tableRowGroup);
		table.Padding = new Thickness(0.0, 0.0, 0.0, 3.0);
		Doc.Blocks.Add(table);
	}

	private void GetItemRow(SubtaskPrintViewModel item, TableRowGroup rowGroup, int parentLevel)
	{
		TableRow tableRow = new TableRow();
		TableCell item2 = new TableCell(new BlockUIContainer(new Image
		{
			Source = ((item.Status == 0) ? Utils.GetImageSource("checkbox01DrawingImage", _resourceDictionary) : Utils.GetImageSource("checkbox05DrawingImage", _resourceDictionary)),
			Opacity = ((item.Status == 0) ? 0.36 : 0.18),
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = 14.0,
			Height = 14.0,
			Margin = new Thickness(16 * parentLevel + 24, 2.0, 0.0, 0.0)
		}));
		TableCell tableCell = new TableCell(new Paragraph(new Run(item.Title))
		{
			Foreground = ((item.Status != 0) ? _secondaryPrintColor : _primaryPrintColor),
			FontSize = 13.0,
			LineHeight = 20.0,
			Padding = new Thickness(0.0, 2.0, 0.0, 3.0),
			TextDecorations = ((item.Status != 0) ? TextDecorations.Strikethrough : null)
		});
		if (!item.StartDate.HasValue)
		{
			tableCell.ColumnSpan = 2;
			tableRow.Cells.Add(item2);
			tableRow.Cells.Add(tableCell);
		}
		else
		{
			TableCell item3 = new TableCell(new BlockUIContainer(new TextBlock
			{
				Text = PrintHelper.GetSubTaskTimeText(item),
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Top,
				Foreground = GetTimeTextColor(item.StartDate)
			}));
			tableRow.Cells.Add(item2);
			tableRow.Cells.Add(tableCell);
			tableRow.Cells.Add(item3);
		}
		rowGroup.Rows.Add(tableRow);
	}

	private Brush GetTimeTextColor(DateTime? startDate)
	{
		if (startDate.HasValue && startDate.Value.Date < DateTime.Today)
		{
			return ThemeUtil.GetColor("OutDateColor");
		}
		return _secondaryPrintColor;
	}

	private static void GetContentOrDesc(TaskPrintViewModel model, TableRowGroup rowGroup)
	{
		if (model.Type == DisplayType.Task || model.Type == DisplayType.Agenda || model.Type == DisplayType.Event || model.Type == DisplayType.Note)
		{
			string contentMarkdownDisplayText = TaskUtils.GetContentMarkdownDisplayText(string.IsNullOrEmpty(model.Content) ? model.Desc : model.Content);
			if (!string.IsNullOrEmpty(contentMarkdownDisplayText))
			{
				TableRow tableRow = new TableRow();
				TableCell item = new TableCell(new Paragraph(new Run(contentMarkdownDisplayText))
				{
					Foreground = ThemeUtil.GetColor("PrintTextColorSecondary"),
					FontSize = 13.0,
					LineHeight = 20.0,
					Padding = new Thickness(0.0, 2.0, 0.0, 3.0)
				})
				{
					ColumnSpan = 2
				};
				tableRow.Cells.Add(new TableCell());
				tableRow.Cells.Add(item);
				rowGroup.Rows.Add(tableRow);
			}
		}
	}

	private Path GetCheckboxPath(TaskPrintViewModel model)
	{
		object[] values = new object[4] { model.Type, model.Kind, model.Status, model.CalendarType };
		object[] values2 = new object[5] { model.Type, model.Priority, model.Status, false, this };
		Size size = new Size(14.0, 14.0);
		switch (model.Type)
		{
		case DisplayType.Course:
			size = new Size(15.0, 14.0);
			break;
		case DisplayType.Countdown:
			size = new Size(16.0, 16.0);
			break;
		}
		return new Path
		{
			Width = size.Width,
			Height = size.Height,
			Stretch = Stretch.Uniform,
			Data = (Geometry)_iconConvert.Convert(values, typeof(Geometry), null, null),
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Fill = (SolidColorBrush)_iconColorConvert.Convert(values2, typeof(SolidColorBrush), null, null)
		};
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/print/tasklistprintview.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			RichTextBox = (RichTextBox)target;
			break;
		case 2:
			Doc = (FlowDocument)target;
			break;
		case 3:
			ListName = (Run)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
