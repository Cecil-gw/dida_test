using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.Views.MainListView.DetailView;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Detail;

public class TaskDetailBatchControl : Grid, IComponentConnector
{
	private BatchDetailView _parent;

	private object _mouseDownItem;

	public static readonly DependencyProperty UserCardStyleProperty = DependencyProperty.Register("UserCardStyle", typeof(bool), typeof(TaskDetailBatchControl), new PropertyMetadata(true));

	public static readonly DependencyProperty PopupShowIndexProperty = DependencyProperty.Register("PopupShowIndex", typeof(int), typeof(TaskDetailBatchControl), new PropertyMetadata(-1));

	internal TaskDetailBatchControl Root;

	internal Border CloseButton;

	internal TextBlock BatchTaskText;

	internal TextBlock CancelButton;

	internal Border TopBorder;

	internal OptionItemWithImageIcon BatchRestoreProjectButton;

	internal OptionItemWithImageIcon BatchDeleteFromTrashButton;

	internal Grid BatchSelectDateButton;

	internal Grid DelayGrid;

	internal Grid BatchTaskPriorityButton;

	internal TextBlock PriorityText;

	internal EscPopup SetPriorityPopup;

	internal DropIconButton BatchTaskMoveProjectButton;

	internal EscPopup SetProjectPopup;

	internal Grid BatchAssigneeGrid;

	internal Rectangle AvatarImage;

	internal ImageBrush Avatar;

	internal EscPopup BatchAssigneePopup;

	internal Grid BatchSetTagsButton;

	internal Path BatchTagIcon;

	internal TextBlock BatchTagsText;

	internal Border BatchTagsContainer;

	internal Border BatchBlockWidthTarget;

	internal WrapPanel BatchTaskBlocks;

	internal Border BatchCompleteOrUndoneBorder;

	internal Path BatchTaskDoneIcon;

	internal Path BatchTaskUndoneIcon;

	internal CheckBox HasAbandonCheck;

	internal Border BatchTaskStarBorder;

	internal Path BatchTaskStartPinIcon;

	internal Path BatchTaskStartUnPinIcon;

	internal Border BatchAbandonBorder;

	internal Border BatchSetParentTaskBorder;

	internal Border BatchTaskMergeBorder;

	internal Border BatchTaskCopyBorder;

	internal Border BatchSwitchTaskNoteBorder;

	internal Path SwitchPath;

	internal TextBlock SwitchTaskToNoteText;

	internal TextBlock SwitchNoteToTaskText;

	internal Border BatchOpenStickyBorder;

	internal Border BatchCopyTextBorder;

	internal Border BatchDeleteBorder;

	private bool _contentLoaded;

	public bool UserCardStyle
	{
		get
		{
			return (bool)GetValue(UserCardStyleProperty);
		}
		set
		{
			SetValue(UserCardStyleProperty, value);
		}
	}

	public int PopupShowIndex
	{
		get
		{
			return (int)GetValue(PopupShowIndexProperty);
		}
		set
		{
			SetValue(PopupShowIndexProperty, value);
		}
	}

	public TaskDetailBatchControl(BatchDetailView parent)
	{
		InitializeComponent();
		_parent = parent;
		BatchTaskMoveProjectButton.TitleText.Height = 18.0;
	}

	public void SetUseCardStyle(bool userCardStyle, bool showClose)
	{
		if (UserCardStyle != userCardStyle)
		{
			UserCardStyle = userCardStyle;
			TopBorder.SetResourceReference(Panel.BackgroundProperty, userCardStyle ? "BatchSelectBackground" : "BaseColorOpacity2");
		}
		CloseButton.Visibility = ((!showClose) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void RestoreProjectClick(object sender, MouseButtonEventArgs e)
	{
		SetProjectPopup.PlacementTarget = BatchRestoreProjectButton;
		_parent?.OnRestoreProject();
	}

	private void DeleteFromTrashClick(object sender, MouseButtonEventArgs e)
	{
		_parent?.OnBatchDeleteTaskForever();
	}

	private void SelectDateClick(object sender, MouseButtonEventArgs e)
	{
		if (object.Equals(_mouseDownItem, sender))
		{
			_parent?.SelectDateClick(e);
		}
		_mouseDownItem = null;
	}

	private void SetPriorityClick(object sender, MouseButtonEventArgs e)
	{
		if (object.Equals(_mouseDownItem, sender))
		{
			_mouseDownItem = null;
			if (SetPriorityPopup.IsOpen)
			{
				SetPriorityPopup.IsOpen = false;
				return;
			}
			SetPriorityDialog setPriorityDialog = new SetPriorityDialog(_parent?.GetCurrentPriority() ?? (-1));
			SetPriorityPopup.Child = setPriorityDialog;
			SetPriorityPopup.IsOpen = true;
			setPriorityDialog.PrioritySelect += OnPrioritySelected;
		}
		void OnPrioritySelected(object o, int i)
		{
			SetPriorityPopup.IsOpen = false;
			SetPriorityPopup.Child = null;
			_parent?.PrioritySelect(inBatch: true, i);
		}
	}

	private void MoveProjectMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (object.Equals(_mouseDownItem, sender))
		{
			_parent?.OnMoveProject(SetProjectPopup, b: true);
		}
		_mouseDownItem = null;
	}

	private void OnSetAssigneeMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (object.Equals(_mouseDownItem, sender))
		{
			_parent?.OnSetAssigneeMouseUp(inBatch: true, BatchAssigneePopup);
		}
		_mouseDownItem = null;
	}

	private void OnTagMouseUp(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		if (object.Equals(_mouseDownItem, sender))
		{
			_parent?.OnTag(BatchSetTagsButton);
		}
		_mouseDownItem = null;
	}

	private void OnBatchCompleteMouseUp(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		_parent?.OnBatchCompleteMouseUp(sender);
	}

	private void OnBatchAbandonMouseUp(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		_parent?.OnBatchAbandonMouseUp(sender);
	}

	private void OnStarMouseUp(object sender, MouseButtonEventArgs e)
	{
		_parent?.OnBatchPinClick(sender, BatchTaskStartPinIcon.IsVisible);
	}

	private void OnMergeMouseUp(object sender, MouseButtonEventArgs e)
	{
		_parent?.OnMergeMouseUp(sender);
	}

	private void OnLinkMouseUp(object sender, MouseButtonEventArgs e)
	{
		_parent?.OnLinkParentMouseUp(sender);
	}

	private void BatchCopyMouseUp(object sender, MouseButtonEventArgs e)
	{
		_parent?.OnCopy();
	}

	private void SwitchTaskNoteMouseUp(object sender, MouseButtonEventArgs e)
	{
		_parent?.BatchSwitchTaskNoteMouseUp(sender);
	}

	private void OnBatchCopyTextMouseUp(object sender, MouseButtonEventArgs e)
	{
		_parent?.OnBatchCopyTextMouseUp(sender);
	}

	private void OnDeleteMouseUp(object sender, MouseButtonEventArgs e)
	{
		_parent?.OnBatchDelete(sender);
	}

	public async void SetBatchAssignVisible(bool show, string avatarUrl)
	{
		BatchAssigneeGrid.Visibility = ((!show) ? Visibility.Collapsed : Visibility.Visible);
		ImageBrush avatar = Avatar;
		avatar.ImageSource = await AvatarHelper.GetAvatarByUrlAsync(avatarUrl);
		AvatarImage.Visibility = (string.IsNullOrEmpty(avatarUrl) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void OnBatchOpenStickyMouseUp(object sender, MouseButtonEventArgs e)
	{
		_parent?.BatchOpenSticky();
	}

	public async void SetAvatar(string assignee, string projectId)
	{
		if (CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.ProjectId == projectId)?.IsShareList() ?? false)
		{
			string avatarUrl = null;
			if (!string.IsNullOrEmpty(assignee) && assignee != "-1")
			{
				avatarUrl = await AvatarHelper.GetAvatarUrl(assignee, projectId);
			}
			SetBatchAssignVisible(show: true, avatarUrl);
		}
		else
		{
			SetBatchAssignVisible(show: false, null);
		}
	}

	private void OnCancelClick(object sender, MouseButtonEventArgs e)
	{
		_parent?.OnCancel();
	}

	private void OnCloseClick(object sender, MouseButtonEventArgs e)
	{
		_parent?.TryCloseDetail();
	}

	private void OnDelayMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (object.Equals(_mouseDownItem, sender))
		{
			_mouseDownItem = null;
			EscPopup escPopup = new EscPopup
			{
				PlacementTarget = DelayGrid,
				Placement = PlacementMode.MousePoint,
				StaysOpen = false,
				HorizontalOffset = 10.0,
				VerticalOffset = -5.0
			};
			escPopup.Closed += delegate
			{
				PopupShowIndex = -1;
			};
			DelayOrAdvanceDialog delayOrAdvanceDialog = new DelayOrAdvanceDialog();
			delayOrAdvanceDialog.DelayOrAdvanced += delegate(object o, long l)
			{
				_parent?.DelayOrAdvancedTasks(l);
			};
			delayOrAdvanceDialog.ShowDialog(escPopup, placeTop: false, _parent?.GetBatchArgs()?.CanPostponeTime == true);
			PopupShowIndex = 1;
		}
	}

	private void OnItemMouseDown(object sender, MouseButtonEventArgs e)
	{
		_mouseDownItem = sender;
	}

	public void SetPopupShowIndex(int index)
	{
		PopupShowIndex = index;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/detail/taskdetailbatchcontrol.xaml", UriKind.Relative);
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
			Root = (TaskDetailBatchControl)target;
			break;
		case 2:
			CloseButton = (Border)target;
			CloseButton.MouseLeftButtonUp += OnCloseClick;
			break;
		case 3:
			BatchTaskText = (TextBlock)target;
			break;
		case 4:
			CancelButton = (TextBlock)target;
			CancelButton.MouseLeftButtonUp += OnCancelClick;
			break;
		case 5:
			TopBorder = (Border)target;
			break;
		case 6:
			BatchRestoreProjectButton = (OptionItemWithImageIcon)target;
			break;
		case 7:
			BatchDeleteFromTrashButton = (OptionItemWithImageIcon)target;
			break;
		case 8:
			BatchSelectDateButton = (Grid)target;
			BatchSelectDateButton.MouseLeftButtonUp += SelectDateClick;
			BatchSelectDateButton.PreviewMouseLeftButtonDown += OnItemMouseDown;
			break;
		case 9:
			DelayGrid = (Grid)target;
			DelayGrid.MouseLeftButtonUp += OnDelayMouseUp;
			DelayGrid.PreviewMouseLeftButtonDown += OnItemMouseDown;
			break;
		case 10:
			BatchTaskPriorityButton = (Grid)target;
			BatchTaskPriorityButton.PreviewMouseLeftButtonDown += OnItemMouseDown;
			BatchTaskPriorityButton.MouseLeftButtonUp += SetPriorityClick;
			break;
		case 11:
			PriorityText = (TextBlock)target;
			break;
		case 12:
			SetPriorityPopup = (EscPopup)target;
			break;
		case 13:
			BatchTaskMoveProjectButton = (DropIconButton)target;
			break;
		case 14:
			SetProjectPopup = (EscPopup)target;
			break;
		case 15:
			BatchAssigneeGrid = (Grid)target;
			BatchAssigneeGrid.MouseLeftButtonUp += OnSetAssigneeMouseUp;
			BatchAssigneeGrid.PreviewMouseLeftButtonDown += OnItemMouseDown;
			break;
		case 16:
			AvatarImage = (Rectangle)target;
			break;
		case 17:
			Avatar = (ImageBrush)target;
			break;
		case 18:
			BatchAssigneePopup = (EscPopup)target;
			break;
		case 19:
			BatchSetTagsButton = (Grid)target;
			BatchSetTagsButton.MouseLeftButtonUp += OnTagMouseUp;
			BatchSetTagsButton.PreviewMouseLeftButtonDown += OnItemMouseDown;
			break;
		case 20:
			BatchTagIcon = (Path)target;
			break;
		case 21:
			BatchTagsText = (TextBlock)target;
			break;
		case 22:
			BatchTagsContainer = (Border)target;
			break;
		case 23:
			BatchBlockWidthTarget = (Border)target;
			break;
		case 24:
			BatchTaskBlocks = (WrapPanel)target;
			break;
		case 25:
			BatchCompleteOrUndoneBorder = (Border)target;
			BatchCompleteOrUndoneBorder.MouseLeftButtonUp += OnBatchCompleteMouseUp;
			break;
		case 26:
			BatchTaskDoneIcon = (Path)target;
			break;
		case 27:
			BatchTaskUndoneIcon = (Path)target;
			break;
		case 28:
			HasAbandonCheck = (CheckBox)target;
			break;
		case 29:
			BatchTaskStarBorder = (Border)target;
			BatchTaskStarBorder.MouseLeftButtonUp += OnStarMouseUp;
			break;
		case 30:
			BatchTaskStartPinIcon = (Path)target;
			break;
		case 31:
			BatchTaskStartUnPinIcon = (Path)target;
			break;
		case 32:
			BatchAbandonBorder = (Border)target;
			BatchAbandonBorder.MouseLeftButtonUp += OnBatchAbandonMouseUp;
			break;
		case 33:
			BatchSetParentTaskBorder = (Border)target;
			BatchSetParentTaskBorder.MouseLeftButtonUp += OnLinkMouseUp;
			break;
		case 34:
			BatchTaskMergeBorder = (Border)target;
			BatchTaskMergeBorder.MouseLeftButtonUp += OnMergeMouseUp;
			break;
		case 35:
			BatchTaskCopyBorder = (Border)target;
			BatchTaskCopyBorder.MouseLeftButtonUp += BatchCopyMouseUp;
			break;
		case 36:
			BatchSwitchTaskNoteBorder = (Border)target;
			BatchSwitchTaskNoteBorder.MouseLeftButtonUp += SwitchTaskNoteMouseUp;
			break;
		case 37:
			SwitchPath = (Path)target;
			break;
		case 38:
			SwitchTaskToNoteText = (TextBlock)target;
			break;
		case 39:
			SwitchNoteToTaskText = (TextBlock)target;
			break;
		case 40:
			BatchOpenStickyBorder = (Border)target;
			BatchOpenStickyBorder.MouseLeftButtonUp += OnBatchOpenStickyMouseUp;
			break;
		case 41:
			BatchCopyTextBorder = (Border)target;
			BatchCopyTextBorder.MouseLeftButtonUp += OnBatchCopyTextMouseUp;
			break;
		case 42:
			BatchDeleteBorder = (Border)target;
			BatchDeleteBorder.MouseLeftButtonUp += OnDeleteMouseUp;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
