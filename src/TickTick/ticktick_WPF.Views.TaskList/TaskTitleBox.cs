using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.MainListView.DetailView;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.TaskList.Item;

namespace ticktick_WPF.Views.TaskList;

public sealed class TaskTitleBox : LinkTextEditBox
{
	private ListItemContent _parent;

	public event EventHandler<string> MultipleTextPaste;

	public TaskTitleBox()
	{
		ShowUrlNameOnly = true;
		base.PreviewMouseLeftButtonDown += OnMouseDown;
	}

	private void OnMouseDown(object sender, MouseButtonEventArgs e)
	{
		if ((Utils.IfCtrlPressed() || Utils.IfShiftPressed()) && !base.CurrentFocused)
		{
			e.Handled = true;
		}
		base.Options.DisableLinkOpen = !base.IsReadOnly && !base.TextArea.IsKeyboardFocused;
	}

	protected override void SetupGenerators()
	{
		base.TextArea.TextView.ElementGenerators.Add(new CustomLinkElementGenerator(this));
		base.TextArea.TextView.ElementGenerators.Add(new LinkNameGenerator(this, showUnderLine: true));
		base.TextArea.TextView.ElementGenerators.Add(new EmojiGenerator(this));
		base.TextArea.TextView.ElementGenerators.Add(new HideLinkBracketGenerator(this, LinkNameDict, isLeft: true));
		base.TextArea.TextView.ElementGenerators.Add(new HideLinkBracketGenerator(this, LinkNameDict, isLeft: false));
	}

	protected override async void OnPaste(object sender, DataObjectPastingEventArgs e)
	{
		if (this.MultipleTextPaste != null)
		{
			string text = string.Empty;
			try
			{
				text = e.SourceDataObject.GetData(DataFormats.UnicodeText) as string;
			}
			catch (Exception)
			{
				Utils.Toast(Utils.GetString("OperateFailed"));
			}
			if (!string.IsNullOrEmpty(text) && text.StartsWith("\r\n"))
			{
				text = " " + text.Substring(2);
			}
			if (!string.IsNullOrEmpty(text) && Utils.ShowBatchAdd(text))
			{
				e.CancelCommand();
				if (CanBatchAdd())
				{
					Utils.RunOnUiThread(base.Dispatcher, delegate
					{
						if (!ShowBatchAddWindow(text))
						{
							base.OnPaste(sender, e);
						}
					});
					return;
				}
			}
		}
		base.OnPaste(sender, e);
	}

	private bool CanBatchAdd()
	{
		return Utils.FindParent<IBatchAddable>(this)?.CanBatchAdd() ?? false;
	}

	private bool ShowBatchAddWindow(string text)
	{
		TaskDetailPopup.SetCanClose(value: false);
		Utils.FindParent<TaskDetailView>(this)?.SetPopupStatus(show: true);
		bool closeClick = false;
		CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("BatchAddTasks"), Utils.GetString("BatchAddTasksContent"), Utils.GetString("Add"), Utils.GetString("Cancel"));
		customerDialog.Owner = Window.GetWindow(this);
		customerDialog.CloseClick += delegate
		{
			closeClick = true;
		};
		bool? flag = customerDialog.ShowDialog();
		if (closeClick)
		{
			return false;
		}
		if (!flag.HasValue || !flag.Value)
		{
			return false;
		}
		this.MultipleTextPaste?.Invoke(this, text);
		Utils.FindParent<TaskDetailView>(this)?.SetPopupStatus(show: false);
		TaskDetailPopup.SetCanClose(value: true);
		return true;
	}

	public async void FocusOnMove()
	{
		Utils.Focus(this);
		base.CaretOffset = 0;
	}

	public void SetFocus(int offset)
	{
		Utils.Focus(this);
		base.CaretOffset = offset;
	}

	public void SetParent(ListItemContent content)
	{
		_parent = content;
	}

	protected override string GetAccountId()
	{
		return string.Empty;
	}

	private TaskBaseViewModel GetTaskBaseModel()
	{
		if (_parent?.DataContext is DisplayItemModel displayItemModel)
		{
			return displayItemModel.SourceViewModel;
		}
		return null;
	}

	protected override string GetProjectId()
	{
		return GetTaskBaseModel()?.ProjectId;
	}

	protected override string GetAssignee()
	{
		return GetTaskBaseModel()?.Assignee;
	}

	protected override int GetPriority()
	{
		return GetTaskBaseModel()?.Priority ?? (-1);
	}

	protected override List<string> GetTags()
	{
		return GetTaskBaseModel()?.Tags?.ToList();
	}

	protected override bool IsUsedInCalendar()
	{
		return false;
	}

	protected override bool IsQuickSetNote()
	{
		return GetTaskBaseModel()?.IsNote ?? false;
	}

	protected override bool ShowTemplateQuickSetItem()
	{
		return false;
	}
}
