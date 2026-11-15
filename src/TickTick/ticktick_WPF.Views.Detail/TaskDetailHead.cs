using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.MainListView.DetailView;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Detail;

public class TaskDetailHead : Grid, IComponentConnector
{
	private bool _selectTimeMouseDown;

	internal Border BackBtn;

	internal Grid CheckIcon;

	internal Path CheckPath;

	internal Grid DateGrid;

	internal Grid ChooseTimeGrid;

	internal Path DatePath;

	internal Grid AssignGrid;

	internal Border AvatarImageRectangle;

	internal ImageBrush AvatarImage;

	internal HoverIconButton AssignOtherGrid;

	internal EscPopup SetAssignPopup;

	internal HoverIconButton SetPriorityGrid;

	internal EscPopup SetPriorityPopup;

	internal Border NoteMessageGrid;

	internal Popup NoteMessagePopup;

	internal TextBlock TextCount;

	internal TextBlock CreatedAt;

	internal TextBlock ModifiedAt;

	internal TaskProgressControl TaskProgressGrid;

	internal Grid ProgressPreviewGrid;

	private bool _contentLoaded;

	public TaskDetailHead(TaskDetailView detailControl)
	{
		InitializeComponent();
	}

	public void SetBackIconEnabled(bool enable)
	{
		BackBtn.IsEnabled = enable;
		BackBtn.Visibility = ((!enable) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void OnBackClick(object sender, MouseButtonEventArgs e)
	{
		BackBtn.Visibility = Visibility.Collapsed;
		e.Handled = true;
	}

	private void OnCheckBoxClick(object sender, MouseButtonEventArgs e)
	{
	}

	private void OnCheckBoxMouseRightUp(object sender, MouseButtonEventArgs e)
	{
	}

	private void SelectDateClick(object sender, MouseButtonEventArgs e)
	{
		_selectTimeMouseDown = false;
	}

	private async void OnSetAssigneeMouseUp(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
	}

	private void SetPriorityMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (SetPriorityPopup.IsOpen)
		{
			SetPriorityPopup.IsOpen = false;
		}
		else if (base.DataContext is TaskDetailViewModel taskDetailViewModel)
		{
			SetPriorityDialog setPriorityDialog = new SetPriorityDialog(taskDetailViewModel.Priority);
			SetPriorityPopup.Child = setPriorityDialog;
			SetPriorityPopup.IsOpen = true;
			setPriorityDialog.PrioritySelect += OnPrioritySelected;
		}
		void OnPrioritySelected(object o, int i)
		{
			SetPriorityPopup.IsOpen = false;
			SetPriorityPopup.Child = null;
		}
	}

	private void NoteMessageClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is TaskDetailViewModel taskDetailViewModel)
		{
			TextCount.Text = Utils.IntToStringWithDivide((!string.IsNullOrEmpty(taskDetailViewModel.TaskContent)) ? taskDetailViewModel.TaskContent.Replace("\r", "").Replace("\n", "").Replace(" ", "")
				.Length : 0);
			CreatedAt.Text = taskDetailViewModel.CreateDate?.ToString("D", App.Ci);
			ModifiedAt.Text = taskDetailViewModel.ModifiedDate?.ToString("D", App.Ci);
			NoteMessagePopup.IsOpen = true;
		}
	}

	private async void ProgressPreviewMouseMove(object sender, MouseEventArgs e)
	{
		if (base.DataContext is TaskDetailViewModel taskDetailViewModel)
		{
			_ = taskDetailViewModel.Enable;
		}
	}

	private void ProgressMouseLeave(object sender, MouseEventArgs e)
	{
		DelayActionHandlerCenter.RemoveAction("DetailShowProgressPointer");
		TaskProgressGrid.HidePointer();
	}

	private void ProgressClick(object sender, MouseButtonEventArgs e)
	{
		TaskProgressGrid.GetPointerProgress();
		TaskProgressGrid.GetProgress();
	}

	public async Task SetProgress(int pointerProgress, bool withAnimal = false)
	{
		TaskProgressGrid.SetProgress(pointerProgress, withAnimal);
	}

	public async Task SetAvatarVisible(bool showAssignOther, bool showAvatarImage, string avatarUrl)
	{
		ImageBrush avatarImage = AvatarImage;
		avatarImage.ImageSource = await AvatarHelper.GetAvatarByUrlAsync(avatarUrl);
		AvatarImageRectangle.Visibility = ((!showAvatarImage) ? Visibility.Collapsed : Visibility.Visible);
		AssignOtherGrid.Visibility = ((!showAssignOther) ? Visibility.Collapsed : Visibility.Visible);
	}

	public void CheckProgress()
	{
		if (base.DataContext is TaskDetailViewModel taskDetailViewModel && TaskProgressGrid.GetProgress() != taskDetailViewModel.Progress)
		{
			TaskProgressGrid.SetProgress(taskDetailViewModel.Progress.GetValueOrDefault());
		}
	}

	private void OnSelectTimeMouseDown(object sender, MouseButtonEventArgs e)
	{
	}

	public void SetPriorityIcon(int priority)
	{
		DrawingImage imageSource = null;
		switch (priority)
		{
		case 0:
			imageSource = Utils.GetImageSource("NonePriorityDrawingImage", this);
			break;
		case 1:
			imageSource = Utils.GetImageSource("LowPriorityDrawingImage");
			break;
		case 3:
			imageSource = Utils.GetImageSource("MidPriorityDrawingImage");
			break;
		case 5:
			imageSource = Utils.GetImageSource("HighPriorityDrawingImage");
			break;
		}
		SetPriorityGrid.ImageSource = imageSource;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/detail/taskdetailhead.xaml", UriKind.Relative);
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
			BackBtn = (Border)target;
			BackBtn.MouseLeftButtonUp += OnBackClick;
			break;
		case 2:
			CheckIcon = (Grid)target;
			CheckIcon.MouseLeftButtonUp += OnCheckBoxClick;
			CheckIcon.MouseRightButtonUp += OnCheckBoxMouseRightUp;
			break;
		case 3:
			CheckPath = (Path)target;
			break;
		case 4:
			DateGrid = (Grid)target;
			break;
		case 5:
			((Border)target).MouseLeftButtonUp += SelectDateClick;
			((Border)target).MouseLeftButtonDown += OnSelectTimeMouseDown;
			break;
		case 6:
			ChooseTimeGrid = (Grid)target;
			break;
		case 7:
			DatePath = (Path)target;
			break;
		case 8:
			AssignGrid = (Grid)target;
			AssignGrid.MouseLeftButtonUp += OnSetAssigneeMouseUp;
			break;
		case 9:
			AvatarImageRectangle = (Border)target;
			break;
		case 10:
			AvatarImage = (ImageBrush)target;
			break;
		case 11:
			AssignOtherGrid = (HoverIconButton)target;
			break;
		case 12:
			SetAssignPopup = (EscPopup)target;
			break;
		case 13:
			SetPriorityGrid = (HoverIconButton)target;
			break;
		case 14:
			SetPriorityPopup = (EscPopup)target;
			break;
		case 15:
			NoteMessageGrid = (Border)target;
			NoteMessageGrid.PreviewMouseLeftButtonUp += NoteMessageClick;
			break;
		case 16:
			NoteMessagePopup = (Popup)target;
			break;
		case 17:
			TextCount = (TextBlock)target;
			break;
		case 18:
			CreatedAt = (TextBlock)target;
			break;
		case 19:
			ModifiedAt = (TextBlock)target;
			break;
		case 20:
			((StackPanel)target).MouseLeftButtonUp += SelectDateClick;
			break;
		case 21:
			TaskProgressGrid = (TaskProgressControl)target;
			break;
		case 22:
			ProgressPreviewGrid = (Grid)target;
			ProgressPreviewGrid.PreviewMouseMove += ProgressPreviewMouseMove;
			ProgressPreviewGrid.MouseLeave += ProgressMouseLeave;
			ProgressPreviewGrid.MouseLeftButtonDown += ProgressClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
