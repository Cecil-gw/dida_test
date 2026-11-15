using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TickTickUtils;
using ticktick_WPF.Cache;
using ticktick_WPF.Converter;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Attachment;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.MarkDown;

namespace ticktick_WPF.Views.Print;

public class TaskDetailPrintView : UserControl, IComponentConnector
{
	private readonly SolidColorBrush _primaryPrintColor = ThemeUtil.GetColor("PrintTextColorPrimary");

	private readonly SolidColorBrush _secondaryPrintColor = ThemeUtil.GetColor("PrintTextColorSecondary");

	private TaskDetailPrintViewModel _model;

	private double _height;

	private double _width;

	private MarkDownEditor _markDown;

	private BlockUIContainer _markDownLastBlockUiContainer;

	private ItemIconColorConverter _iconColorConvert = new ItemIconColorConverter();

	internal FlowDocumentPageViewer PageViewer;

	internal FlowDocument Doc;

	internal BlockUIContainer Head;

	internal StackPanel CheckIcon;

	internal Border AvatarGrid;

	internal Border AvatarImage;

	internal Image AssignImage;

	internal Grid PriorityGrid;

	internal ColumnDefinition FColumn;

	internal ColumnDefinition SColumn;

	internal Paragraph TitleText;

	internal Paragraph DescText;

	internal TableRowGroup ItemRowGroup;

	internal BlockUIContainer TagBlock;

	internal WrapPanel TagContainer;

	internal TableRowGroup SubTaskGroup;

	internal Table CommentTable;

	internal TableRowGroup CommentRowGroup;

	internal ScrollViewer MarkDownViewer;

	private bool _contentLoaded;

	public bool IsMdLoaded { get; private set; }

	public TaskDetailPrintView()
	{
		InitializeComponent();
	}

	public TaskDetailPrintView(TaskDetailPrintViewModel model, double width, double height)
	{
		_model = model;
		base.DataContext = model;
		_height = height;
		_width = width;
		InitializeComponent();
		Doc.FontFamily = ((LocalSettings.Settings.ExtraSettings.AppFontFamily == "SourceHansansSC_CN") ? new FontFamily("Microsoft YaHei UI") : LocalSettings.Settings.FontFamily);
		_markDown = new MarkDownEditor(inPrint: true);
		_markDown.ImageMode = model.ImageMode;
		if (!string.IsNullOrEmpty(_model.Title))
		{
			_model.ContentOrDesc = PrependTitleForPrint(_model.ContentOrDesc, _model.Title);
			_model.Title = string.Empty;
		}
		_markDown.SetImageGeneratorTaskId(model.TaskId);
		_markDown.Width = _width - 70.0;
		_markDown.Text = MarkdownUtils.RemoveFoldedMetadataComments(_model.ContentOrDesc);
		MarkDownViewer.Content = _markDown;
		base.Loaded += OnLoaded;
		base.SizeChanged += delegate(object sender, SizeChangedEventArgs args)
		{
			if (IsMdLoaded)
			{
				_markDown.Width = args.NewSize.Width - 70.0;
				_markDown.Text = _model.ContentOrDesc;
				_width = args.NewSize.Width;
				_height = args.NewSize.Height;
				ReloadContent();
			}
		};
	}

	private static string PrependTitleForPrint(string content, string title)
	{
		string rawText = content ?? string.Empty;
		List<MarkdownInlineDisplaySegment> list = MarkdownInlineSemanticMetadata.Extract(ref rawText);
		if (list.Count > 0)
		{
			int num = title.Length + 1;
			foreach (MarkdownInlineDisplaySegment item in list)
			{
				item.SourcePosition += num;
			}
			rawText += MarkdownInlineSemanticMetadata.Serialize(list);
		}
		return "## " + title + "\n" + rawText;
	}

	private void OnInitialized(object sender, EventArgs eventArgs)
	{
		FColumn.Width = new GridLength(_model.Progress.GetValueOrDefault(), GridUnitType.Star);
		SColumn.Width = new GridLength((100 - _model.Progress).GetValueOrDefault(), GridUnitType.Star);
		if (_model.Avatar != null)
		{
			AvatarGrid.Visibility = Visibility.Visible;
			AvatarImage.Background = new ImageBrush
			{
				ImageSource = _model.Avatar
			};
		}
		else
		{
			Image assignImage = AssignImage;
			ProjectModel projectById = CacheManager.GetProjectById(_model.ProjectId);
			assignImage.Visibility = ((projectById == null || !projectById.IsShareList()) ? Visibility.Collapsed : Visibility.Visible);
		}
		if (_model.Kind == "NOTE")
		{
			PriorityGrid.Visibility = Visibility.Collapsed;
			CheckIcon.Visibility = Visibility.Collapsed;
		}
	}

	private void TryAddContent()
	{
		if (TitleText.Parent == Doc)
		{
			Doc.Blocks.Remove(TitleText);
		}
		EnsureMarkdownAnchor();
		GetMarkDown();
	}

	private void EnsureMarkdownAnchor()
	{
		if (DescText.Parent == null)
		{
			Doc.Blocks.InsertAfter(Head, DescText);
		}
	}

	private void TryRemoveContent()
	{
		IsMdLoaded = false;
		_markDownLastBlockUiContainer = null;
		List<Block> list = new List<Block>();
		foreach (Block block in Doc.Blocks)
		{
			if ("md".Equals(block.Tag))
			{
				list.Add(block);
			}
		}
		list.ForEach(delegate(Block i)
		{
			Doc.Blocks.Remove(i);
		});
	}

	private void TryAddItems()
	{
		if (!(_model.Kind == "CHECKLIST"))
		{
			return;
		}
		List<SubtaskPrintViewModel> subtaskPrintViewModels = _model.SubtaskPrintViewModels;
		if (subtaskPrintViewModels == null || subtaskPrintViewModels.Count <= 0)
		{
			return;
		}
		foreach (SubtaskPrintViewModel subtaskPrintViewModel in _model.SubtaskPrintViewModels)
		{
			TableRow tableRow = new TableRow();
			Path checkboxPath = GetCheckboxPath(subtaskPrintViewModel);
			checkboxPath.Margin = new Thickness(0.0, 2.0, 0.0, 0.0);
			TableCell item = new TableCell(new BlockUIContainer(checkboxPath));
			TableCell tableCell = new TableCell(new Paragraph(new Run(subtaskPrintViewModel.Title))
			{
				Foreground = ((subtaskPrintViewModel.Status != 0) ? _secondaryPrintColor : _primaryPrintColor),
				FontSize = 13.0,
				Padding = new Thickness(0.0, 0.0, 0.0, 2.0),
				TextDecorations = ((subtaskPrintViewModel.Status != 0) ? TextDecorations.Strikethrough : null)
			});
			if (!subtaskPrintViewModel.StartDate.HasValue)
			{
				tableCell.ColumnSpan = 2;
				tableRow.Cells.Add(item);
				tableRow.Cells.Add(tableCell);
			}
			else
			{
				TableCell item2 = new TableCell(new BlockUIContainer(new TextBlock
				{
					Text = PrintHelper.GetSubTaskTimeText(subtaskPrintViewModel),
					HorizontalAlignment = HorizontalAlignment.Right,
					VerticalAlignment = VerticalAlignment.Bottom,
					FontSize = 13.0,
					Margin = new Thickness(0.0, 0.0, 0.0, 2.0),
					Foreground = GetTimeTextColor(subtaskPrintViewModel.StartDate)
				}));
				tableRow.Cells.Add(item);
				tableRow.Cells.Add(tableCell);
				tableRow.Cells.Add(item2);
			}
			ItemRowGroup.Rows.Add(tableRow);
			TableRow lineRow = GetLineRow();
			ItemRowGroup.Rows.Add(lineRow);
		}
	}

	private void GetMarkDown()
	{
		double num = _height - 150.0;
		if (_markDownLastBlockUiContainer == null)
		{
			num = _height - 190.0;
		}
		if (_markDown.ActualHeight > num)
		{
			string persistedText = _markDown.GetPersistedText();
			int pageLastIndex = _markDown.GetPageLastIndex(num);
			int persistedOffset = _markDown.GetPersistedOffset(pageLastIndex);
			persistedOffset = Math.Max(0, Math.Min(persistedOffset, persistedText.Length));
			if (persistedOffset == 0 && persistedText.Length > 0)
			{
				persistedOffset = Math.Min(Math.Max(1, pageLastIndex), persistedText.Length);
			}
			string text = persistedText.Substring(0, persistedOffset);
			MarkDownEditor obj = new MarkDownEditor(inPrint: true)
			{
				Width = _width - 70.0,
				HorizontalAlignment = HorizontalAlignment.Left,
				EnableSpellCheck = false,
				ImageMode = _markDown.ImageMode,
				Height = num
			};
			BlockUIContainer blockUIContainer = new BlockUIContainer(obj);
			obj.SetImageGeneratorTaskId(_model.TaskId);
			obj.Text = text;
			blockUIContainer.Tag = "md";
			if (_markDownLastBlockUiContainer != null)
			{
				Doc.Blocks.InsertAfter(_markDownLastBlockUiContainer, blockUIContainer);
				_markDownLastBlockUiContainer = blockUIContainer;
			}
			else
			{
				Doc.Blocks.InsertBefore(DescText, blockUIContainer);
				_markDownLastBlockUiContainer = blockUIContainer;
			}
			obj.UpdateLayout();
			_markDown.Text = persistedText.Substring(persistedOffset);
			_markDown.UpdateLayout();
			if (!string.IsNullOrEmpty(text))
			{
				GetMarkDown();
			}
			else
			{
				IsMdLoaded = true;
			}
		}
		else
		{
			MarkDownEditor markDownEditor = new MarkDownEditor(inPrint: true);
			markDownEditor.Width = _width - 70.0;
			markDownEditor.MinHeight = _markDown.ActualHeight + 10.0;
			markDownEditor.Height = _markDown.ActualHeight + 10.0;
			markDownEditor.HorizontalAlignment = HorizontalAlignment.Left;
			markDownEditor.EnableSpellCheck = false;
			markDownEditor.ImageMode = _markDown.ImageMode;
			markDownEditor.SetImageGeneratorTaskId(_model.TaskId);
			markDownEditor.Text = _markDown.GetPersistedText();
			BlockUIContainer newItem = new BlockUIContainer(markDownEditor)
			{
				Tag = "md"
			};
			if (_markDownLastBlockUiContainer != null)
			{
				Doc.Blocks.InsertAfter(_markDownLastBlockUiContainer, newItem);
			}
			else
			{
				Doc.Blocks.InsertBefore(DescText, newItem);
			}
			markDownEditor.UpdateLayout();
			markDownEditor.Redraw();
			markDownEditor.UpdateLayout();
			IsMdLoaded = true;
		}
	}

	private TableRow GetLineRow(Thickness margin = default(Thickness))
	{
		TableRow tableRow = new TableRow();
		TableCell item = new TableCell(new BlockUIContainer(new Border
		{
			Height = 6.0,
			BorderBrush = ThemeUtil.GetAlphaColor("#191919", 10),
			BorderThickness = new Thickness(0.0, 0.0, 0.0, 1.0),
			Margin = margin
		}))
		{
			ColumnSpan = 2
		};
		tableRow.Cells.Add(new TableCell());
		tableRow.Cells.Add(item);
		return tableRow;
	}

	private void TryAddSubTask()
	{
		List<SubtaskPrintViewModel> realSubtaskPrintViewModels = _model.RealSubtaskPrintViewModels;
		if (realSubtaskPrintViewModels == null || realSubtaskPrintViewModels.Count <= 0)
		{
			return;
		}
		BlockUIContainer blockUIContainer = new BlockUIContainer(new Border
		{
			Height = 2.0,
			BorderBrush = ThemeUtil.GetAlphaColor("#05080F", 8),
			BorderThickness = new Thickness(0.0, 1.0, 0.0, 0.0)
		});
		blockUIContainer.Margin = new Thickness(0.0, 15.0, 0.0, 15.0);
		Doc.Blocks.InsertAfter(TagBlock, blockUIContainer);
		foreach (SubtaskPrintViewModel realSubtaskPrintViewModel in _model.RealSubtaskPrintViewModels)
		{
			TableRow tableRow = new TableRow();
			StackPanel obj = new StackPanel
			{
				Orientation = Orientation.Horizontal
			};
			Path checkboxPath = GetCheckboxPath(realSubtaskPrintViewModel);
			obj.Children.Add(checkboxPath);
			obj.Children.Add(new TextBlock
			{
				Text = realSubtaskPrintViewModel.Title,
				Foreground = ((realSubtaskPrintViewModel.Status != 0) ? _secondaryPrintColor : _primaryPrintColor),
				FontSize = 13.0,
				VerticalAlignment = VerticalAlignment.Bottom,
				Padding = new Thickness(12.0, 0.0, 0.0, 0.0),
				TextDecorations = ((realSubtaskPrintViewModel.Status != 0) ? TextDecorations.Strikethrough : null)
			});
			TableCell item = new TableCell(new BlockUIContainer(obj))
			{
				ColumnSpan = 2
			};
			tableRow.Cells.Add(item);
			if (realSubtaskPrintViewModel.StartDate.HasValue)
			{
				TableCell item2 = new TableCell(new BlockUIContainer(new TextBlock
				{
					Text = PrintHelper.GetSubTaskTimeText(realSubtaskPrintViewModel),
					HorizontalAlignment = HorizontalAlignment.Right,
					VerticalAlignment = VerticalAlignment.Bottom,
					FontSize = 13.0,
					Margin = new Thickness(0.0, 0.0, 0.0, 0.0),
					Foreground = GetTimeTextColor(realSubtaskPrintViewModel.StartDate)
				}));
				tableRow.Cells.Add(item2);
			}
			SubTaskGroup.Rows.Add(tableRow);
			TableRow lineRow = GetLineRow(new Thickness((realSubtaskPrintViewModel.Level - 1) * 15, 0.0, 0.0, 0.0));
			SubTaskGroup.Rows.Add(lineRow);
		}
	}

	private Path GetCheckboxPath(SubtaskPrintViewModel model)
	{
		DisplayType displayType = DisplayType.Task;
		object[] values = new object[5] { displayType, model.Priority, model.Status, false, this };
		Geometry taskIconGeometry = ThemeUtil.GetTaskIconGeometry(DisplayType.Task, model.Status, model.Kind, 0);
		return new Path
		{
			Width = 14.0,
			Height = 14.0,
			Stretch = Stretch.UniformToFill,
			Data = taskIconGeometry,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Bottom,
			Fill = (SolidColorBrush)_iconColorConvert.Convert(values, typeof(SolidColorBrush), null, null),
			Margin = new Thickness((model.Level - 1) * 15, 0.0, 0.0, 1.0)
		};
	}

	private Brush GetTimeTextColor(DateTime? startDate)
	{
		if (startDate.HasValue && startDate.Value.Date < DateTime.Today)
		{
			return ThemeUtil.GetColor("OutDateColor");
		}
		return _secondaryPrintColor;
	}

	private void TryAddComments()
	{
		List<CommentViewModel> comments = _model.Comments;
		if (comments == null || comments.Count <= 0)
		{
			return;
		}
		BlockUIContainer blockUIContainer = new BlockUIContainer();
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Margin = new Thickness(1.0, 30.0, 0.0, 0.0)
		};
		stackPanel.Children.Add(new TextBlock
		{
			Text = Utils.GetString("Comment"),
			FontSize = 14.0,
			Foreground = _primaryPrintColor,
			Margin = new Thickness(0.0, 0.0, 10.0, 0.0)
		});
		stackPanel.Children.Add(new TextBlock
		{
			Text = _model.Comments.Count.ToString(),
			FontSize = 13.0,
			VerticalAlignment = VerticalAlignment.Center,
			Foreground = _secondaryPrintColor
		});
		blockUIContainer.Child = stackPanel;
		Doc.Blocks.InsertBefore(CommentTable, blockUIContainer);
		foreach (CommentViewModel comment in _model.Comments)
		{
			AddComment(comment);
		}
	}

	private void AddComment(CommentViewModel comment)
	{
		TableRow tableRow = new TableRow();
		TableCell item = new TableCell(new BlockUIContainer(new Ellipse
		{
			Height = 30.0,
			Width = 30.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			StrokeThickness = 1.0,
			Stroke = ThemeUtil.GetAlphaColor("#191919", 8),
			Fill = new ImageBrush(AvatarHelper.GetAvatarByUrl(comment.AvatarUrl))
		}))
		{
			RowSpan = 2
		};
		TableCell item2 = new TableCell(new BlockUIContainer(new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Children = 
			{
				(UIElement)new TextBlock
				{
					Text = comment.UserName,
					FontSize = 14.0,
					FontWeight = FontWeights.SemiBold,
					Foreground = _primaryPrintColor,
					Margin = new Thickness(0.0, 0.0, 10.0, 0.0)
				},
				(UIElement)new TextBlock
				{
					Text = ticktick_WPF.Util.DateUtils.FormatCommentTime(comment.CreateDate),
					FontSize = 13.0,
					Foreground = _secondaryPrintColor
				}
			}
		}));
		tableRow.Cells.Add(item);
		tableRow.Cells.Add(item2);
		CommentRowGroup.Rows.Add(tableRow);
		TableRow tableRow2 = new TableRow();
		TableCell item3 = new TableCell(new Paragraph(new Run(comment.Content)
		{
			FontSize = 14.0,
			Foreground = _primaryPrintColor
		})
		{
			Padding = new Thickness(0.0, 2.0, 0.0, 15.0)
		});
		tableRow2.Cells.Add(item3);
		CommentRowGroup.Rows.Add(tableRow2);
	}

	private void TryAddAttachments()
	{
		try
		{
			List<AttachmentViewModel> attachments = _model.Attachments;
			if (attachments == null || attachments.Count <= 0 || !(_model.Kind == "CHECKLIST"))
			{
				return;
			}
			BlockUIContainer previousSibling = TagBlock;
			bool flag = true;
			foreach (AttachmentViewModel attachment in _model.Attachments)
			{
				int num = (flag ? 30 : 6);
				flag = false;
				if (attachment.FileType == "IMAGE" && !string.IsNullOrEmpty(attachment.LocalPath))
				{
					BlockUIContainer blockUIContainer = new BlockUIContainer();
					double maxWidth = _width - 80.0;
					int num2 = ((_model.ImageMode == 1) ? 120 : 800);
					Size imageSize = ThemeUtil.GetImageSize(attachment.LocalPath);
					Size smallRect = ImageUtils.GetSmallRect(maxWidth, num2, imageSize.Width, imageSize.Height);
					Grid grid = new Grid
					{
						Margin = new Thickness(10.0, num, 0.0, 6.0),
						Height = smallRect.Height
					};
					Rectangle element = new Rectangle
					{
						Height = smallRect.Height,
						Width = smallRect.Width,
						RadiusX = 4.0,
						RadiusY = 4.0,
						HorizontalAlignment = HorizontalAlignment.Left,
						Fill = new ImageBrush
						{
							ImageSource = ThemeUtil.GetImage(attachment.LocalPath, (int)(smallRect.Width * 1.5)),
							Stretch = Stretch.Uniform
						}
					};
					Border element2 = new Border();
					grid.Children.Add(element);
					grid.Children.Add(element2);
					blockUIContainer.Child = grid;
					Doc.Blocks.InsertAfter(previousSibling, blockUIContainer);
					previousSibling = blockUIContainer;
				}
				else
				{
					BlockUIContainer blockUIContainer2 = new BlockUIContainer();
					Border border = new Border
					{
						Height = 50.0,
						Margin = new Thickness(10.0, num, 30.0, 6.0),
						Background = AttachmentResourceHelper.GetFileTypeColor(attachment.FileType),
						CornerRadius = new CornerRadius(4.0),
						BorderThickness = new Thickness(1.0),
						BorderBrush = AttachmentResourceHelper.GetFileTypeBorderColor(attachment.FileType)
					};
					Grid grid2 = new Grid
					{
						ColumnDefinitions = 
						{
							new ColumnDefinition
							{
								Width = GridLength.Auto
							},
							new ColumnDefinition(),
							new ColumnDefinition
							{
								Width = GridLength.Auto
							}
						}
					};
					Image element3 = new Image
					{
						Height = 30.0,
						Margin = new Thickness(12.0, 0.0, 0.0, 0.0),
						Source = new BitmapImage(new Uri(AttachmentResourceHelper.GetFileTypeImage(attachment.FileType)))
					};
					TextBlock textBlock = new TextBlock
					{
						Text = attachment.FileName,
						FontSize = 14.0,
						HorizontalAlignment = HorizontalAlignment.Left,
						TextTrimming = TextTrimming.CharacterEllipsis,
						Foreground = _primaryPrintColor,
						LineHeight = 14.0,
						LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
						VerticalAlignment = VerticalAlignment.Center,
						Margin = new Thickness(12.0, 0.0, 12.0, 0.0)
					};
					textBlock.SetValue(Grid.ColumnProperty, 1);
					TextBlock textBlock2 = new TextBlock
					{
						Margin = new Thickness(12.0, 0.0, 12.0, 0.0),
						Text = ((attachment.Size > 1024000) ? ((attachment.Size / 1024000).ToString("f2") + "MB") : ((attachment.Size > 1024) ? ((attachment.Size / 1024).ToString("f2") + "KB") : (attachment.Size + "B"))),
						Foreground = ThemeUtil.GetAlphaColor("#191919", 85),
						FontSize = 12.0,
						LineHeight = 12.0,
						LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
						VerticalAlignment = VerticalAlignment.Center
					};
					textBlock2.SetValue(Grid.ColumnProperty, 2);
					grid2.Children.Add(element3);
					grid2.Children.Add(textBlock);
					grid2.Children.Add(textBlock2);
					border.Child = grid2;
					blockUIContainer2.Child = border;
					Doc.Blocks.InsertAfter(previousSibling, blockUIContainer2);
					previousSibling = blockUIContainer2;
				}
			}
		}
		catch (Exception)
		{
		}
	}

	private void TryAddTags()
	{
		List<TagPrintModel> tags = _model.Tags;
		if (tags == null || tags.Count <= 0)
		{
			return;
		}
		foreach (TagPrintModel tag in _model.Tags)
		{
			Border border = new Border
			{
				Background = tag.TagBackColor,
				CornerRadius = new CornerRadius(2.0),
				Margin = new Thickness(0.0, 0.0, 5.0, 4.0)
			};
			TextBlock child = new TextBlock
			{
				Text = tag.TagTitle,
				Foreground = tag.TagTextColor,
				FontSize = 12.0,
				Padding = new Thickness(4.0, 2.0, 4.0, 2.0)
			};
			border.Child = child;
			TagContainer.Children.Add(border);
		}
	}

	private void ReloadContent()
	{
		TryRemoveContent();
		TryAddContent();
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		UpdateLayout();
		_markDown.SetTheme();
		_markDown.UpdateLayout();
		_markDown.Redraw();
		_markDown.UpdateLayout();
		TryAddContent();
		TryAddItems();
		TryAddTags();
		TryAddSubTask();
		TryAddAttachments();
		TryAddComments();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/print/taskdetailprintview.xaml", UriKind.Relative);
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
			((TaskDetailPrintView)target).Initialized += OnInitialized;
			break;
		case 2:
			PageViewer = (FlowDocumentPageViewer)target;
			break;
		case 3:
			Doc = (FlowDocument)target;
			break;
		case 4:
			Head = (BlockUIContainer)target;
			break;
		case 5:
			CheckIcon = (StackPanel)target;
			break;
		case 6:
			AvatarGrid = (Border)target;
			break;
		case 7:
			AvatarImage = (Border)target;
			break;
		case 8:
			AssignImage = (Image)target;
			break;
		case 9:
			PriorityGrid = (Grid)target;
			break;
		case 10:
			FColumn = (ColumnDefinition)target;
			break;
		case 11:
			SColumn = (ColumnDefinition)target;
			break;
		case 12:
			TitleText = (Paragraph)target;
			break;
		case 13:
			DescText = (Paragraph)target;
			break;
		case 14:
			ItemRowGroup = (TableRowGroup)target;
			break;
		case 15:
			TagBlock = (BlockUIContainer)target;
			break;
		case 16:
			TagContainer = (WrapPanel)target;
			break;
		case 17:
			SubTaskGroup = (TableRowGroup)target;
			break;
		case 18:
			CommentTable = (Table)target;
			break;
		case 19:
			CommentRowGroup = (TableRowGroup)target;
			break;
		case 20:
			MarkDownViewer = (ScrollViewer)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
