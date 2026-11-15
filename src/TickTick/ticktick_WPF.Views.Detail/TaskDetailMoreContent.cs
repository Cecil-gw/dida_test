using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.MainListView.DetailView;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Pomo;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Views.Detail;

public class TaskDetailMoreContent : StackPanel, ITabControl, IComponentConnector
{
	private bool _tagPopupShow;

	private bool _pomoPopupShow;

	private TaskDetailView _parent;

	public bool FirstOpenTag;

	public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register("SelectedIndex", typeof(int), typeof(TaskDetailMoreContent), new PropertyMetadata(-1, null));

	private EscPopup _popup;

	private Point _position;

	internal TaskDetailMoreContent Root;

	internal OptionCheckBox NewAddSubTaskItem;

	internal OptionCheckBox LinkParentTaskItem;

	internal OptionCheckBox PinButton;

	internal Grid AbandonedGrid;

	internal Grid NewTagItem;

	internal OptionCheckBox TagItem;

	internal EscPopup SetTagPopup;

	internal BatchSetTagControl BatchSetTagControl;

	internal OptionCheckBox NewUploadItem;

	internal OptionCheckBox NewInsetSummaryItem;

	internal Grid PomoItem;

	internal OptionCheckBox Pomo;

	internal EscPopup TaskPomoPopup;

	internal TaskPomoSetDialog TaskPomoSetDialog;

	internal OptionCheckBox UseTemplate;

	internal Line OMTopLine;

	internal OptionCheckBox ActivityItem;

	internal OptionCheckBox AddTemplate;

	internal OptionCheckBox CopyItem;

	internal OptionCheckBox CopyLinkItem;

	internal OptionCheckBox StickyItem;

	internal OptionCheckBox SwitchTaskNoteItem;

	internal OptionCheckBox ConvertToEventItem;

	internal OptionCheckBox ExportItem;

	internal OptionCheckBox Print;

	internal CommonDeleteIcon DeletePanel;

	private bool _contentLoaded;

	public int SelectedIndex
	{
		get
		{
			return (int)GetValue(SelectedIndexProperty);
		}
		set
		{
			SetValue(SelectedIndexProperty, value);
			SetItemTabSelected();
		}
	}

	public TaskDetailMoreContent()
	{
		InitializeComponent();
	}

	private void TryShowPopup(object sender, MouseEventArgs e)
	{
		Point position = e.GetPosition(this);
		if (Math.Abs(_position.Y - position.Y) > 4.0 || Math.Abs(_position.X - position.X) > 4.0)
		{
			_position = position;
			TryShowPomoPopup();
			TryShowTagPopup();
		}
	}

	private void TryShowTagPopup()
	{
		if (!TagItem.IsMouseOver && !SetTagPopup.IsMouseOver)
		{
			TryHideTagPopup();
		}
		else if (!_tagPopupShow && !SetTagPopup.IsOpen)
		{
			_tagPopupShow = true;
			DelayShowTagPopup();
		}
	}

	private async Task TryHideTagPopup()
	{
		_tagPopupShow = false;
		if (SetTagPopup.IsOpen)
		{
			await Task.Delay(300);
			if (!TagItem.IsMouseOver && !SetTagPopup.IsMouseOver && !BatchSetTagControl.CheckChange())
			{
				SetTagPopup.IsOpen = false;
			}
			else
			{
				_tagPopupShow = SetTagPopup.IsOpen;
			}
		}
	}

	private async Task DelayShowTagPopup(bool wait = true)
	{
		if (wait)
		{
			await Task.Delay(150);
			if (TaskPomoPopup.IsOpen)
			{
				_tagPopupShow = false;
				return;
			}
		}
		ShowTagPopup();
	}

	private void ShowTagPopup()
	{
		if (base.DataContext is TaskDetailViewModel taskDetailViewModel)
		{
			TagSelectData tags = new TagSelectData
			{
				OmniSelectTags = TagSerializer.ToTags(taskDetailViewModel.Tag)
			};
			if (_tagPopupShow)
			{
				BatchSetTagControl.Init(tags, FirstOpenTag);
				FirstOpenTag = false;
				SetTagPopup.IsOpen = true;
			}
		}
	}

	private void OnBatchSetTagClosed(object sender, EventArgs e)
	{
		SetTagPopup.IsOpen = false;
	}

	private async void OnTagsSelected(object sender, TagSelectData tags)
	{
		_parent.AddActionEvent("om", "tag");
		_parent.OnTagsSelected(tags, inBatch: false);
		SetTagPopup.IsOpen = false;
		await Task.Delay(200);
		_popup.IsOpen = false;
	}

	private async void OpenTaskSticky(object sender, MouseButtonEventArgs e)
	{
		_popup.IsOpen = false;
		object dataContext = base.DataContext;
		if (dataContext is TaskDetailViewModel viewModel)
		{
			string text = TaskUtils.ToastOnOpenSticky(viewModel.TaskId);
			if (!string.IsNullOrEmpty(text))
			{
				_parent.TryToast(text);
				return;
			}
			UserActCollectUtils.AddClickEvent("task_detail", "om", "open_as_sticky_note");
			_parent.TryForceHideWindow();
			await Task.Delay(200);
			TaskStickyWindow.ShowTaskSticky(new List<string> { viewModel.TaskId });
		}
	}

	private void TryShowPomoPopup()
	{
		if (!Pomo.IsMouseOver && !TaskPomoPopup.IsMouseOver)
		{
			TryHidePomoPopup();
		}
		else if (!_pomoPopupShow && !TaskPomoPopup.IsOpen)
		{
			_pomoPopupShow = true;
			DelayShowPomoPopup();
		}
	}

	private async Task TryHidePomoPopup()
	{
		_pomoPopupShow = false;
		if (TaskPomoPopup.IsOpen)
		{
			await Task.Delay(100);
			if (!Pomo.IsMouseOver && !TaskPomoSetDialog.CheckMouseMove())
			{
				TaskPomoPopup.IsOpen = false;
			}
			else
			{
				_pomoPopupShow = TaskPomoPopup.IsOpen;
			}
		}
	}

	private async Task DelayShowPomoPopup(bool wait = true)
	{
		if (wait)
		{
			await Task.Delay(150);
			if (SetTagPopup.IsOpen)
			{
				_tagPopupShow = false;
				return;
			}
		}
		ShowPomoPopup();
	}

	private void ShowPomoPopup()
	{
		if (_pomoPopupShow)
		{
			TaskPomoSetDialog.InitData((base.DataContext as TaskDetailViewModel)?.TaskId, force: true, "task_detail", "om");
			TaskPomoPopup.IsOpen = true;
		}
	}

	private void LinkParentTaskClick(object sender, MouseButtonEventArgs e)
	{
		_popup.IsOpen = false;
		if (base.DataContext is TaskDetailViewModel taskDetailViewModel)
		{
			_parent?.LinkParentTaskClick(sender, e, taskDetailViewModel.TaskId, _parent.Mode != Constants.DetailMode.Page);
		}
	}

	private void AddSubTaskClick(object sender, MouseButtonEventArgs e)
	{
		_popup.IsOpen = false;
		_parent?.AddSubTaskClick(sender, e);
	}

	private void OnAICompleteClick(object sender, MouseButtonEventArgs e)
	{
		_popup.IsOpen = false;
		_parent?.OnAICompleteClick();
	}

	private void OnUseTemplateClick(object sender, MouseButtonEventArgs e)
	{
		_popup.IsOpen = false;
		_parent?.OnUseTemplateClick();
	}

	private void OnStarClick(object sender, MouseButtonEventArgs e)
	{
		_popup.IsOpen = false;
		_parent?.OnStarClick();
	}

	private void OnAbandonedOrReopenClick(object sender, MouseButtonEventArgs e)
	{
		_popup.IsOpen = false;
		_parent?.OnAbandonOrReopenClick();
	}

	private async void OnAttachmentClick(object sender, MouseButtonEventArgs e)
	{
		if (e != null)
		{
			e.Handled = true;
		}
		_parent?.SetNeedFocusDetail(e: false);
		_popup.IsOpen = false;
		await Task.Delay(300);
		_tagPopupShow = false;
		_pomoPopupShow = false;
		_parent?.OnAddAttachmentClick();
	}

	private async void OnInsertSummaryClick(object sender, MouseButtonEventArgs e)
	{
		if (e != null)
		{
			e.Handled = true;
		}
		_parent?.SetNeedFocusDetail(e: false);
		_popup.IsOpen = false;
		await Task.Delay(300);
		_tagPopupShow = false;
		_pomoPopupShow = false;
		_parent?.TryInsertSummary();
	}

	private async void ClosePomoPopup(object sender, bool e)
	{
		_parent?.SetNeedFocusDetail(e: false);
		TaskPomoPopup.IsOpen = false;
		await Task.Delay(200);
		_popup.IsOpen = false;
	}

	private void OnActivityClick(object sender, MouseButtonEventArgs e)
	{
		_popup.IsOpen = false;
		_parent?.ShowTaskActivities();
	}

	private async void OnAddTemplateClick(object sender, MouseButtonEventArgs e)
	{
		_parent?.SetNeedFocusDetail(e: false);
		_popup.IsOpen = false;
		await Task.Delay(240);
		_parent?.OnAddTemplateClick();
	}

	private async void OnCopyClick(object sender, MouseButtonEventArgs e)
	{
		_popup.IsOpen = false;
		await Task.Delay(80);
		_parent?.OnTaskCopy();
	}

	private async void OnCopyLinkClick(object sender, MouseButtonEventArgs e)
	{
		_popup.IsOpen = false;
		_parent?.OnCopyLinkClick();
	}

	private async void SwitchTaskNoteClick(object sender, MouseButtonEventArgs e)
	{
		_popup.IsOpen = false;
		if (SwitchTaskNoteItem.Tag is string getString)
		{
			_parent?.TryToast(getString);
		}
		else
		{
			_parent?.SwitchTaskNoteClick();
		}
	}

	private async void ConvertToEventClick(object sender, MouseButtonEventArgs e)
	{
		_popup.IsOpen = false;
		await Task.Delay(80);
		_parent?.ConvertNewTaskToEventClick();
	}

	private async void OnPrintClick(object sender, MouseButtonEventArgs e)
	{
		_parent?.SetNeedFocusDetail(e: false);
		_popup.IsOpen = false;
		UserActCollectUtils.AddClickEvent("task_detail", "om", "print");
		_parent?.OnPrint();
	}

	private void OnDeleteClick(object sender, MouseButtonEventArgs e)
	{
		_popup.IsOpen = false;
		_parent?.OnDelete();
	}

	private void OnExportClick(object sender, MouseButtonEventArgs e)
	{
		_parent?.SetNeedFocusDetail(e: false);
		_parent?.SkipNextPopupClose();
		_popup.IsOpen = false;
		_parent?.OnExport(markdown: true);
		if (e != null)
		{
			e.Handled = true;
		}
	}

	public bool HandleTab(bool shift)
	{
		if (TaskPomoPopup.IsOpen)
		{
			TaskPomoSetDialog.HandleTab(shift);
			return true;
		}
		if (SetTagPopup.IsOpen)
		{
			BatchSetTagControl?.HandleTab(shift);
			return true;
		}
		return UpDownSelect(shift);
	}

	public bool HandleEnter()
	{
		if (SetTagPopup.IsOpen || TaskPomoPopup.IsOpen)
		{
			return false;
		}
		switch (SelectedIndex)
		{
		case 0:
			AddSubTaskClick(this, null);
			break;
		case 1:
			LinkParentTaskClick(this, null);
			break;
		case 2:
			OnStarClick(this, null);
			break;
		case 3:
			OnAbandonedOrReopenClick(this, null);
			break;
		case 4:
			_tagPopupShow = true;
			DelayShowTagPopup();
			break;
		case 5:
			OnAttachmentClick(this, null);
			break;
		case 6:
			OnInsertSummaryClick(this, null);
			break;
		case 7:
			_pomoPopupShow = true;
			ShowPomoPopup();
			break;
		case 8:
			OnUseTemplateClick(this, null);
			break;
		case 9:
			OnActivityClick(this, null);
			break;
		case 10:
			OnAddTemplateClick(this, null);
			break;
		case 11:
			OnCopyClick(this, null);
			break;
		case 12:
			OnCopyLinkClick(this, null);
			break;
		case 13:
			OpenTaskSticky(this, null);
			break;
		case 14:
			SwitchTaskNoteClick(this, null);
			break;
		case 15:
			ConvertToEventClick(this, null);
			break;
		case 16:
			OnExportClick(this, null);
			return true;
		case 17:
			OnPrintClick(this, null);
			break;
		case 18:
			OnDeleteClick(this, null);
			break;
		}
		return false;
	}

	public bool HandleEsc()
	{
		return false;
	}

	public bool UpDownSelect(bool isUp)
	{
		if (SetTagPopup.IsOpen || TaskPomoPopup.IsOpen)
		{
			return false;
		}
		if (SelectedIndex < 0)
		{
			SelectedIndex = 0;
			return true;
		}
		SelectedIndex += ((!isUp) ? 1 : 18);
		SelectedIndex %= 19;
		switch (SelectedIndex)
		{
		case 0:
			if (NewAddSubTaskItem.IsVisible)
			{
				break;
			}
			goto IL_0207;
		case 1:
			if (LinkParentTaskItem.IsVisible)
			{
				break;
			}
			goto IL_0207;
		case 2:
			if (PinButton.IsVisible)
			{
				break;
			}
			goto IL_0207;
		case 3:
			if (AbandonedGrid.IsVisible)
			{
				break;
			}
			goto IL_0207;
		case 4:
			if (NewTagItem.IsVisible)
			{
				break;
			}
			goto IL_0207;
		case 5:
			if (NewUploadItem.IsVisible)
			{
				break;
			}
			goto IL_0207;
		case 6:
			if (NewInsetSummaryItem.IsVisible)
			{
				break;
			}
			goto IL_0207;
		case 7:
			if (PomoItem.IsVisible)
			{
				break;
			}
			goto IL_0207;
		case 8:
			if (UseTemplate.IsVisible)
			{
				break;
			}
			goto IL_0207;
		case 9:
			if (ActivityItem.IsVisible)
			{
				break;
			}
			goto IL_0207;
		case 10:
			if (AddTemplate.IsVisible)
			{
				break;
			}
			goto IL_0207;
		case 11:
			if (CopyItem.IsVisible)
			{
				break;
			}
			goto IL_0207;
		case 12:
			if (CopyLinkItem.IsVisible)
			{
				break;
			}
			goto IL_0207;
		case 13:
			if (StickyItem.IsVisible)
			{
				break;
			}
			goto IL_0207;
		case 14:
			if (SwitchTaskNoteItem.IsVisible)
			{
				break;
			}
			goto IL_0207;
		case 15:
			if (ConvertToEventItem.IsVisible)
			{
				break;
			}
			goto IL_0207;
		case 16:
			if (ExportItem.IsVisible)
			{
				break;
			}
			goto IL_0207;
		case 17:
			if (Print.IsVisible)
			{
				break;
			}
			goto IL_0207;
		case 18:
			{
				if (DeletePanel.IsVisible)
				{
					break;
				}
				goto IL_0207;
			}
			IL_0207:
			UpDownSelect(isUp);
			break;
		}
		return true;
	}

	public bool LeftRightSelect(bool isLeft)
	{
		if (TaskPomoPopup.IsOpen)
		{
			if (!TaskPomoSetDialog.IsInputFocus())
			{
				TaskPomoPopup.IsOpen = false;
			}
			return false;
		}
		if (SetTagPopup.IsOpen)
		{
			return false;
		}
		if (!isLeft)
		{
			int selectedIndex = SelectedIndex;
			if (selectedIndex == 3 || selectedIndex == 6)
			{
				HandleEnter();
				return true;
			}
		}
		return false;
	}

	private void OnChildPopupOpened(object sender, EventArgs e)
	{
		if (object.Equals(sender, SetTagPopup))
		{
			TagItem.HoverSelected = true;
		}
		else if (object.Equals(sender, TaskPomoPopup))
		{
			Pomo.HoverSelected = true;
		}
	}

	private void OnChildPopupClosed(object sender, EventArgs e)
	{
		if (object.Equals(sender, SetTagPopup))
		{
			TagItem.HoverSelected = SelectedIndex == 4;
		}
		else if (object.Equals(sender, TaskPomoPopup))
		{
			Pomo.HoverSelected = SelectedIndex == 7;
		}
	}

	private void SetItemTabSelected()
	{
		TagItem.HoverSelected = SelectedIndex == 4 || SetTagPopup.IsOpen;
		Pomo.HoverSelected = SelectedIndex == 7 || TaskPomoPopup.IsOpen;
	}

	public void SetParent(TaskDetailView parent, EscPopup popup)
	{
		_parent = parent;
		_popup = popup;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/detail/taskdetailmorecontent.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	internal Delegate _CreateDelegate(Type delegateType, string handler)
	{
		return Delegate.CreateDelegate(delegateType, this, handler);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			Root = (TaskDetailMoreContent)target;
			Root.MouseMove += TryShowPopup;
			break;
		case 2:
			NewAddSubTaskItem = (OptionCheckBox)target;
			break;
		case 3:
			LinkParentTaskItem = (OptionCheckBox)target;
			break;
		case 4:
			PinButton = (OptionCheckBox)target;
			break;
		case 5:
			AbandonedGrid = (Grid)target;
			break;
		case 6:
			NewTagItem = (Grid)target;
			break;
		case 7:
			TagItem = (OptionCheckBox)target;
			break;
		case 8:
			SetTagPopup = (EscPopup)target;
			break;
		case 9:
			BatchSetTagControl = (BatchSetTagControl)target;
			break;
		case 10:
			NewUploadItem = (OptionCheckBox)target;
			break;
		case 11:
			NewInsetSummaryItem = (OptionCheckBox)target;
			break;
		case 12:
			PomoItem = (Grid)target;
			break;
		case 13:
			Pomo = (OptionCheckBox)target;
			break;
		case 14:
			TaskPomoPopup = (EscPopup)target;
			break;
		case 15:
			TaskPomoSetDialog = (TaskPomoSetDialog)target;
			break;
		case 16:
			UseTemplate = (OptionCheckBox)target;
			break;
		case 17:
			OMTopLine = (Line)target;
			break;
		case 18:
			ActivityItem = (OptionCheckBox)target;
			break;
		case 19:
			AddTemplate = (OptionCheckBox)target;
			break;
		case 20:
			CopyItem = (OptionCheckBox)target;
			break;
		case 21:
			CopyLinkItem = (OptionCheckBox)target;
			break;
		case 22:
			StickyItem = (OptionCheckBox)target;
			break;
		case 23:
			SwitchTaskNoteItem = (OptionCheckBox)target;
			break;
		case 24:
			ConvertToEventItem = (OptionCheckBox)target;
			break;
		case 25:
			ExportItem = (OptionCheckBox)target;
			break;
		case 26:
			Print = (OptionCheckBox)target;
			break;
		case 27:
			DeletePanel = (CommonDeleteIcon)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
