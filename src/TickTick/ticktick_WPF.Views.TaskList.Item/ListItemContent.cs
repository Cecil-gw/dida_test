using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ticktick_WPF.Converter;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.DateParser;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.MainListView;
using ticktick_WPF.Views.MainListView.TaskList;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.MarkDown.Colorizer;
using ticktick_WPF.Views.QuickAdd;

namespace ticktick_WPF.Views.TaskList.Item;

public class ListItemContent : Canvas
{
	private const int TitleLeft = 42;

	private const int OpenIconLeft = 3;

	private const int DetailOpenIconLeft = 6;

	private DisplayItemController _controller;

	private string _id;

	private List<AvatarViewModel> _avatars;

	private string _projectId;

	private bool _checkBoxDown;

	private bool _inProjectList;

	public bool InSticky;

	public TaskCheckIcon CheckIcon = new TaskCheckIcon();

	private Border _openBorder;

	private Rectangle _virtualContent;

	public TaskTitleBox TitleTextBox = new TaskTitleBox
	{
		MaxLength = 2048
	};

	private TextBlock _hintText;

	private static readonly MultiBinding ReadOnlyMultiBinding;

	public bool TitleFocused => TitleTextBox.TextArea.IsKeyboardFocused;

	public bool IsIconMouseOver
	{
		get
		{
			Border openBorder = _openBorder;
			if (openBorder == null || !openBorder.IsMouseOver)
			{
				return CheckIcon.IsMouseOver;
			}
			return true;
		}
	}

	public bool BatchSelected => TitleTextBox.SelectionLength > 0;

	static ListItemContent()
	{
		ReadOnlyMultiBinding = new MultiBinding();
		BoolOrBoolConverter converter = (BoolOrBoolConverter)App.Instance.FindResource("BoolOrBoolConverter");
		ReadOnlyMultiBinding.Converter = converter;
		ReadOnlyMultiBinding.Bindings.Add(new Binding("ReadOnly"));
		ReadOnlyMultiBinding.Bindings.Add(new Binding("WindowAllowDrop")
		{
			Source = StaticPropertiesModel.Instance
		});
	}

	public ListItemContent()
	{
		AddTextControl();
		SetResourceReference(FrameworkElement.HeightProperty, "Height40");
		base.DataContextChanged += OnDataBind;
		base.SizeChanged += OnSizeChanged;
		CheckIcon.PreviewMouseLeftButtonUp += OnCheckBoxClick;
		CheckIcon.PreviewMouseRightButtonUp += CheckBoxRightMouseUp;
		CheckIcon.PreviewMouseLeftButtonDown += OnCheckBoxMouseDown;
		base.Children.Add(CheckIcon);
		Canvas.SetLeft(CheckIcon, 18.0);
		CheckIcon.SetResourceReference(Canvas.TopProperty, "Font12");
		TitleTextBox.PreviewTextInput += OnInput;
		base.Unloaded += delegate
		{
			_controller?.Clear();
			_controller = null;
		};
		TitleTextBox.SetBinding(LinkTextEditBox.ReadOnlyProperty, "ReadOnly");
	}

	private void OnInput(object sender, TextCompositionEventArgs e)
	{
		SetHintText(show: false);
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		TitleTextBox.Width = Math.Max(0.0, base.Width - 42.0 - 8.0);
		if (_virtualContent != null && base.DataContext is DisplayItemModel displayItemModel)
		{
			_virtualContent.Width = Math.Max(0.0, base.Width - 42.0 - (double)((displayItemModel.SortOrder == 4 || displayItemModel.SortOrder == 7) ? 58 : 28));
		}
	}

	private async void AddTextControl()
	{
		base.Children.Add(TitleTextBox);
		TitleTextBox.QuickItemSelected += OnQuickItemSelected;
		TitleTextBox.SetParent(this);
		TitleTextBox.PopOpened += OnQuickSetPopupOpened;
		TitleTextBox.PopClosed += OnQuickSetPopupClosed;
		Canvas.SetLeft(TitleTextBox, 42.0);
		TitleTextBox.SetResourceReference(Canvas.TopProperty, "Double11Add1");
		TitleTextBox.SetResourceReference(UIElement.IsHitTestVisibleProperty, "TitleHitVisible");
		TitleTextBox.SetResourceReference(Control.FontSizeProperty, "Font14");
		TitleTextBox.VerticalAlignment = VerticalAlignment.Top;
		TitleTextBox.TextLostFocus += TitleLostFocus;
		TitleTextBox.TextGotFocus += TitleGotFocus;
		TitleTextBox.LinkTextChange += TitleTextChanged;
		TitleTextBox.MoveUp += OnMoveUp;
		TitleTextBox.DateParsed += OnDateParsed;
		TitleTextBox.MoveDown += OnMoveDown;
		TitleTextBox.SplitText += OnSplit;
		TitleTextBox.MergeText += OnMergeText;
		TitleTextBox.IgnoreTokenChanged += OnIgnoreTokenChanged;
		TitleTextBox.MultipleTextPaste += OnMultipleTextPaste;
		TitleTextBox.CaretChanged += TitleCaretChanged;
		TitleTextBox.Navigate += OnNavigate;
		TitleTextBox.SelectDate += OnSelectDateKeyDown;
		TitleTextBox.RequestBringIntoView += EditorOnRequestBringIntoView;
		TitleTextBox.EscKeyUp += OnEscKeyUp;
		TitleTextBox.SetWordWrap(wrap: false);
		TitleTextBox.EnableSpellCheck = false;
	}

	private void OnEscKeyUp(object sender, EventArgs e)
	{
		RemoveFocus();
		Window window = Window.GetWindow(this);
		if (window != null && window.IsActive)
		{
			FocusManager.SetFocusedElement(window, window);
			Keyboard.Focus(window);
		}
	}

	public void SetHintText(bool show)
	{
		TextBlock hintText;
		object textDecorations;
		if (show)
		{
			if (_hintText == null)
			{
				_hintText = new TextBlock();
				Canvas.SetLeft(_hintText, 42.0);
				if (InSticky)
				{
					_hintText.FontSize = 13.0;
					Canvas.SetTop(_hintText, 11.0);
				}
				else
				{
					_hintText.SetResourceReference(Canvas.TopProperty, "Double11Add1");
					_hintText.SetResourceReference(TextBlock.FontSizeProperty, "Font14");
				}
				_hintText.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity20");
				_hintText.SetResourceReference(TextBlock.TextProperty, "NoTitle");
				_hintText.IsHitTestVisible = false;
				base.Children.Add(_hintText);
			}
			hintText = _hintText;
			if (LocalSettings.Settings.ExtraSettings.ShowCompleteLine == 1)
			{
				DisplayItemModel obj = base.DataContext as DisplayItemModel;
				if (obj == null || obj.Status != 0)
				{
					textDecorations = DeleteLineColorizer.HintStrikethrough;
					goto IL_0110;
				}
			}
			textDecorations = null;
			goto IL_0110;
		}
		if (_hintText != null)
		{
			base.Children.Remove(_hintText);
			_hintText = null;
		}
		return;
		IL_0110:
		hintText.TextDecorations = (TextDecorationCollection)textDecorations;
	}

	private void ShowOpenIcon(bool show, bool open)
	{
		if (show)
		{
			if (_openBorder == null)
			{
				_openBorder = new Border();
				base.Children.Add(_openBorder);
				_openBorder.SetResourceReference(Canvas.TopProperty, InSticky ? "StickyFont14" : "Font14");
				_openBorder.MouseLeftButtonUp += OnOpenPathClick;
				_openBorder.Cursor = Cursors.Hand;
				_openBorder.SetResourceReference(FrameworkElement.StyleProperty, "SmoothHoverBorderStyle40_100");
				Path path = new Path
				{
					Width = 12.0,
					Height = 12.0,
					RenderTransformOrigin = new Point(0.5, 0.5),
					Stretch = Stretch.Uniform,
					VerticalAlignment = VerticalAlignment.Center
				};
				path.SetResourceReference(Shape.FillProperty, "BaseColorOpacity100_80");
				path.Data = Utils.GetGeometry("Ic.Common.DownArrow");
				path.RenderTransform = new RotateTransform((!open) ? (-90) : 0);
				_openBorder.Child = path;
			}
			else if (_openBorder.Child is Path path2)
			{
				path2.RenderTransform = new RotateTransform((!open) ? (-90) : 0);
			}
			DisplayItemModel displayItemModel = base.DataContext as DisplayItemModel;
			Canvas.SetLeft(_openBorder, (displayItemModel != null && displayItemModel.InDetail && !InSticky) ? 6 : 3);
		}
		else if (_openBorder != null)
		{
			base.Children.Remove(_openBorder);
			_openBorder = null;
		}
	}

	private void OnQuickSetPopupOpened(object sender, EventArgs e)
	{
		if (sender is Popup popup && popup.Equals(TitleTextBox.SelectionPopup))
		{
			_controller?.SetQuickSetPopup(popup);
		}
		_controller?.SetParentInOperation(inOperate: true);
	}

	private async void OnQuickSetPopupClosed(object sender, EventArgs e)
	{
		_controller?.SetParentInOperation(inOperate: false);
		if (sender is Popup objA && object.Equals(objA, TitleTextBox.SelectionPopup))
		{
			_controller?.SetQuickSetPopup(null);
			await Task.Delay(100);
			if (!TitleFocused)
			{
				TitleLostFocus(null, null);
			}
		}
	}

	private async void OnQuickItemSelected(object sender, QuickSetModel e)
	{
		if (e != null && e.Type == QuickSetType.Date)
		{
			TryClearParseDate();
		}
		if (_controller != null)
		{
			await _controller.OnInputSelected(e, TitleTextBox.Text, !TitleTextBox.ParsingDate);
		}
		if (!TitleTextBox.ParsingDate)
		{
			return;
		}
		if (e == null || e.Type != QuickSetType.Project)
		{
			return;
		}
		object dataContext = base.DataContext;
		if (!(dataContext is DisplayItemModel model))
		{
			return;
		}
		TaskModel taskModel = await TaskDao.GetThinTaskById(model.Id);
		if (taskModel != null)
		{
			taskModel.startDate = model.StartDate;
			taskModel.dueDate = model.DueDate;
			if (!MoveToastHelper.CheckTaskMatched(_controller?.GetProjectIdentify(), taskModel))
			{
				string taskName = SaveParsedDate(model);
				Utils.GetToastWindow()?.ToastMoveProjectControl(taskModel.projectId, taskName);
			}
		}
	}

	public void TryClearParseDate()
	{
		TitleTextBox.ClearParseDate();
		if (_inProjectList)
		{
			Utils.FindParent<TaskView>(this)?.ClearDetailParseDate();
		}
		TaskListItemFocusHelper.ParseDateId = string.Empty;
		TaskListItemFocusHelper.ParseDateItem = null;
	}

	public async void TrySaveParseDate()
	{
		await Task.Delay(100);
		if (TitleTextBox.CanParseDate && base.DataContext is DisplayItemModel model)
		{
			SaveParsedDate(model);
		}
	}

	private void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		DisplayItemModel displayItemModel = e.NewValue as DisplayItemModel;
		if (e.OldValue is DisplayItemModel source)
		{
			PropertyChangedEventManager.RemoveHandler(source, OnPropertyChange, "");
		}
		if (displayItemModel == null)
		{
			return;
		}
		PropertyChangedEventManager.AddHandler(displayItemModel, OnPropertyChange, "");
		if (displayItemModel.IsNewAdd)
		{
			TrySetCanParse();
		}
		displayItemModel.SetIcon();
		CheckIcon.Cursor = ((!displayItemModel.Enable) ? Cursors.No : ((displayItemModel.IsEvent || displayItemModel.IsNote || displayItemModel.IsCourse || displayItemModel.IsCountdown) ? Cursors.Arrow : Cursors.Hand));
		if (_id != displayItemModel.Id)
		{
			_id = displayItemModel.Id;
			TitleTextBox.AutoGetUrlTitle = !displayItemModel.IsEvent;
			TitleTextBox.LinkTextChange -= TitleTextChanged;
			TitleTextBox.CaretChanged -= TitleCaretChanged;
			TitleTextBox.SetTextOffset(displayItemModel.Title, restoreOffset: true, clearUndo: true);
			TitleTextBox.LinkTextChange += TitleTextChanged;
			TitleTextBox.CaretChanged += TitleCaretChanged;
			TitleTextBox.SetMarkRegexText(displayItemModel != null && displayItemModel.Type == DisplayType.Event, inDetail: false);
		}
		else if (TitleTextBox.Text != displayItemModel.Title)
		{
			TitleTextBox.SetTextOffset(displayItemModel.Title, restoreOffset: true);
		}
		TitleTextBox.TextStatus = displayItemModel.Status;
		if (displayItemModel.CaretIndex.HasValue)
		{
			TitleTextBox.SetFocus(displayItemModel.CaretIndex.Value);
			displayItemModel.CaretIndex = null;
		}
		if (displayItemModel.IsTaskOrNote && _projectId != displayItemModel.ProjectId)
		{
			_projectId = displayItemModel.ProjectId;
			_avatars = null;
		}
		if (_controller == null)
		{
			_controller = new DisplayItemController(this, displayItemModel);
			_controller.SplashCommand += HandleSplashCommand;
		}
		else
		{
			_controller.Reset(this, displayItemModel);
		}
		ShowOpenIcon(displayItemModel.HasChildren && displayItemModel.Level < 4, displayItemModel.IsOpen);
		SetHintText(string.IsNullOrEmpty(displayItemModel.Title));
		if (displayItemModel.SourceViewModel.VirtualContent)
		{
			if (_virtualContent == null)
			{
				_virtualContent = new Rectangle
				{
					Height = 12.0
				};
				_virtualContent.SetResourceReference(Shape.FillProperty, "BaseColorOpacity5");
				base.Children.Add(_virtualContent);
				Canvas.SetLeft(_virtualContent, 42.0);
				Canvas.SetTop(_virtualContent, 13.0);
				_virtualContent.Width = Math.Max(0.0, base.Width - 42.0 - (double)((displayItemModel.SortOrder == 4 || displayItemModel.SortOrder == 7) ? 58 : 28));
			}
		}
		else
		{
			base.Children.Remove(_virtualContent);
		}
	}

	private async void HandleSplashCommand(object sender, string command)
	{
		if (string.IsNullOrEmpty(command))
		{
			return;
		}
		int splashIndexLength = Utils.GetSplashIndexLength(TitleTextBox.CaretOffset, TitleTextBox.Text ?? string.Empty);
		if (splashIndexLength >= 0)
		{
			int length = TitleTextBox.CaretOffset - splashIndexLength;
			TitleTextBox.Document.Remove(splashIndexLength, length);
		}
		if (command == "switch_note" || command == "switch_task")
		{
			TitleTextBox.CloseQuickSetPopup();
			_controller.OnSwitchClick(this, null);
			return;
		}
		await Task.Delay(50);
		if (TitleTextBox?.Document != null)
		{
			TitleTextBox.Document.Insert(TitleTextBox.CaretOffset, command);
		}
	}

	private void OnPropertyChange(object sender, PropertyChangedEventArgs e)
	{
		if (base.DataContext is DisplayItemModel displayItemModel)
		{
			switch (e.PropertyName)
			{
			case "Type":
			case "Priority":
				CheckIcon.SetIconColor(displayItemModel.Type, displayItemModel.Priority, displayItemModel.Status);
				break;
			case "Status":
				CheckIcon.SetIconColor(displayItemModel.Type, displayItemModel.Priority, displayItemModel.Status);
				TitleTextBox.TextStatus = displayItemModel.Status;
				SetHintText(string.IsNullOrEmpty(displayItemModel.Title));
				break;
			case "IsOpen":
			case "HasChildren":
				ShowOpenIcon(displayItemModel.HasChildren && displayItemModel.Level < 4, displayItemModel.IsOpen);
				break;
			}
		}
	}

	private void TitleLostFocus(object sender, EventArgs e)
	{
		DisplayItemModel displayItemModel = base.DataContext as DisplayItemModel;
		try
		{
			if (TitleTextBox.ReadOnly || (TitleTextBox.SelectionPopupOpened && TitleTextBox.ParsingDate))
			{
				return;
			}
			if (TitleTextBox.ParsedData == null)
			{
				TitleTextBox.SetCanParseDate(canParseDate: false);
				return;
			}
			if (displayItemModel == null || !displayItemModel.Enable)
			{
				return;
			}
			if (TitleTextBox.CanParseDate || displayItemModel.IsNewAdd)
			{
				if (_inProjectList)
				{
					TaskView taskView = Utils.FindParent<TaskView>(this);
					if (taskView != null && taskView.DetailTitleFocus())
					{
						goto IL_00a1;
					}
				}
				SaveParsedDate(displayItemModel);
				return;
			}
			goto IL_00a1;
			IL_00a1:
			if (!TitleTextBox.ParsingDate && !displayItemModel.IsNewAdd)
			{
				_controller?.TitleLostFocus(reload: false);
			}
		}
		finally
		{
			if (displayItemModel != null && displayItemModel.IsNewAdd)
			{
				displayItemModel.IsNewAdd = false;
				_controller?.TitleLostFocus(reload: true);
			}
			TitleTextBox.ScrollToHome();
			TaskUtils.TryGetTaskLinkTitle(displayItemModel?.Id, displayItemModel?.Title);
		}
	}

	private void TitleGotFocus(object sender, RoutedEventArgs e)
	{
		TrySetCanParse();
		if (Mouse.RightButton != MouseButtonState.Pressed)
		{
			_controller?.TitleGotFocus();
		}
	}

	private string SaveParsedDate(DisplayItemModel model)
	{
		model.IsNewAdd = false;
		IPaserDueDate parsedData = TitleTextBox.ParsedData;
		TitleTextBox.SetCanParseDate(canParseDate: false);
		string parsedText = TitleTextBox.GetParsedText();
		TaskListItemFocusHelper.ParseDateId = string.Empty;
		TaskListItemFocusHelper.ParseDateItem = null;
		_controller?.SetDateAndTitle(parsedText, parsedData);
		TitleTextBox.ScrollToHome();
		_controller?.TitleLostFocus(reload: true);
		model.ParseData = null;
		return parsedText;
	}

	private void TrySetCanParse()
	{
		if (!(base.DataContext is DisplayItemModel displayItemModel) || TitleTextBox.CanParseDate)
		{
			return;
		}
		bool flag = displayItemModel.IsNewAdd;
		if (!displayItemModel.IsTask)
		{
			flag = false;
		}
		else if (!flag)
		{
			TimeData timeData = _controller?.GetDefaultDate() ?? TimeData.BuildDefaultStartAndEnd();
			if (!displayItemModel.StartDate.HasValue)
			{
				goto IL_0126;
			}
			if (displayItemModel.StartDate == timeData.StartDate)
			{
				DateTime? dueDate = displayItemModel.DueDate;
				DateTime? dueDate2 = timeData.DueDate;
				if (dueDate.HasValue == dueDate2.HasValue && (!dueDate.HasValue || dueDate.GetValueOrDefault() == dueDate2.GetValueOrDefault()) && displayItemModel.IsAllDay == timeData.IsAllDay)
				{
					goto IL_0126;
				}
			}
		}
		goto IL_0128;
		IL_0126:
		flag = true;
		goto IL_0128;
		IL_0128:
		TaskListItemFocusHelper.FocusingItem = this;
		TitleTextBox.SetCanParseDate(flag);
		if (flag)
		{
			_inProjectList = _controller?.InProjectList() ?? false;
			if (_inProjectList)
			{
				TaskListItemFocusHelper.ParseDateId = displayItemModel.Id;
				TaskListItemFocusHelper.ParseDateItem = this;
			}
			else
			{
				TaskListItemFocusHelper.ParseDateId = string.Empty;
				TaskListItemFocusHelper.ParseDateItem = null;
			}
		}
		else
		{
			_inProjectList = false;
			TaskListItemFocusHelper.ParseDateId = string.Empty;
			TaskListItemFocusHelper.ParseDateItem = null;
		}
	}

	protected virtual void TitleTextChanged(object sender, EventArgs e)
	{
		string text = TitleTextBox.Text;
		SetHintText(string.IsNullOrEmpty(text));
		_controller?.TitleTextChanged(text);
	}

	private void OnMoveUp(object sender, EventArgs e)
	{
		TitleTextBox.ScrollToHome();
		_controller?.MoveUp();
	}

	private void OnMoveDown(object sender, EventArgs e)
	{
		TitleTextBox.ScrollToHome();
		_controller?.MoveDown();
	}

	private void OnMergeText(object sender, string text)
	{
		_controller?.OnMergeText(text);
	}

	private void OnMultipleTextPaste(object sender, string text)
	{
		_controller?.OnMultipleTextPaste(text);
	}

	private void TitleCaretChanged(object sender, int caretIndex)
	{
		if (TitleTextBox.TextArea.IsKeyboardFocused)
		{
			_controller?.TitleSelectionChanged(caretIndex);
		}
	}

	private async void OnCheckBoxClick(object sender, MouseButtonEventArgs e)
	{
		if (!_checkBoxDown)
		{
			return;
		}
		object dataContext = base.DataContext;
		if (dataContext is DisplayItemModel { IsToggling: false } model)
		{
			model.IsToggling = true;
			_checkBoxDown = false;
			bool soundPlayed = false;
			TaskListItem taskListItem = Utils.FindParent<TaskListItem>(this);
			if (taskListItem != null)
			{
				soundPlayed = await taskListItem.TryPlayCompleteStory(model);
			}
			_controller?.OnCheckBoxClick(model, soundPlayed);
			RemoveFocus();
		}
	}

	public void RemoveFocus()
	{
		TitleTextBox.TabIndex = -1;
		Keyboard.ClearFocus();
	}

	public void FocusTitle()
	{
		TitleTextBox.Focus();
	}

	public bool IsNote()
	{
		if (base.DataContext is DisplayItemModel displayItemModel)
		{
			return displayItemModel.IsNote;
		}
		return false;
	}

	private void OnNavigate(object sender, ProjectTask e)
	{
		Utils.FindParent<IListViewParent>(this)?.NavigateTask(e);
	}

	private void OnSplit(object sender, int e)
	{
		_controller?.SplitNewTask(null);
	}

	public string GetProjectId()
	{
		if (base.DataContext is DisplayItemModel { IsTaskOrNote: not false } displayItemModel)
		{
			return displayItemModel.ProjectId;
		}
		return null;
	}

	private void EditorOnRequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
	{
		e.Handled = true;
	}

	private void OnSelectDateKeyDown(object sender, EventArgs e)
	{
		if (!InSticky)
		{
			_controller?.SelectDate();
		}
	}

	private void OnCheckBoxMouseDown(object sender, MouseButtonEventArgs e)
	{
		_checkBoxDown = true;
	}

	private void OnDateParsed(object sender, IPaserDueDate e)
	{
		_controller?.SetParsedDate(e?.ToTimeData(addDefaultReminder: true));
	}

	private void OnIgnoreTokenChanged(object sender, List<string> e)
	{
		if (_inProjectList)
		{
			Utils.FindParent<TaskView>(this)?.GetTaskDetail()?.GetTitleText().SetIgnoreToken(e);
		}
	}

	public void SetIgnoreTokens(List<string> tokens)
	{
		TitleTextBox.SetIgnoreToken(tokens);
	}

	private void OnOpenPathClick(object sender, MouseButtonEventArgs e)
	{
		_controller?.OnOpenPathClick(sender, e);
		e.Handled = true;
	}

	private void CheckBoxRightMouseUp(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		_controller?.OnCheckBoxRightMouseUp(CheckIcon);
	}

	public void SetStickyMode()
	{
		InSticky = true;
		SetResourceReference(FrameworkElement.HeightProperty, "StickyHeight30");
		CheckIcon.SetResourceReference(Canvas.TopProperty, "StickyFont11");
		_openBorder?.SetResourceReference(Canvas.TopProperty, "StickyFont14");
		TitleTextBox.SetResourceReference(Canvas.TopProperty, "StickyFont11");
		TitleTextBox.SetBaseColor("StickyContentTextColor", "StickyContentTextColor", "StickyCompletedTextColor", "StickyTextColor20");
		TitleTextBox.SetLightTheme();
		TitleTextBox.SetResourceReference(Control.FontSizeProperty, "StickyFont13");
		if (_hintText != null)
		{
			_hintText.SetResourceReference(Control.FontSizeProperty, "StickyFont13");
			_hintText.SetResourceReference(Canvas.TopProperty, "StickyFont11");
		}
	}
}
