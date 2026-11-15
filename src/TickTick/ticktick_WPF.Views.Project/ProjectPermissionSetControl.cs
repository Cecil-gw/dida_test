using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Project;

public class ProjectPermissionSetControl : UserControl, IComponentConnector
{
	private bool _canModify = true;

	public static readonly DependencyProperty InUserItemProperty = DependencyProperty.Register("InUserItem", typeof(bool), typeof(ProjectPermissionSetControl), new PropertyMetadata(false, OnInUserItemChanged));

	public static readonly DependencyProperty SelectedPermissionProperty = DependencyProperty.Register("SelectedPermission", typeof(Constants.ProjectPermission), typeof(ProjectPermissionSetControl), new PropertyMetadata(Constants.ProjectPermission.write, null));

	public EventHandler<int> PermissionSelect;

	public EventHandler Transfer;

	public EventHandler Exit;

	public EventHandler Delete;

	public EventHandler<bool> NeedAuditChange;

	internal ProjectPermissionSetControl Root;

	internal Grid Container;

	internal StackPanel OptionPanel;

	internal TextBlock OptionText;

	internal Path Arrow;

	internal Image TransferImage;

	internal Image ExitProject;

	internal EscPopup OptionPopup;

	internal OptionCheckBox WriteButton;

	internal OptionCheckBox CommentButton;

	internal OptionCheckBox ReadOnlyButton;

	internal Line DividerLine;

	internal Border DeletePanel;

	internal DockPanel NeedAuditPanel;

	internal CheckBox NeedAuditCheckBox;

	internal Border ApprovalTooltipBorder;

	private bool _contentLoaded;

	public Constants.ProjectPermission SelectedPermission
	{
		get
		{
			return (Constants.ProjectPermission)GetValue(SelectedPermissionProperty);
		}
		set
		{
			SetValue(SelectedPermissionProperty, value);
		}
	}

	public bool InUserItem
	{
		get
		{
			return (bool)GetValue(InUserItemProperty);
		}
		set
		{
			SetValue(InUserItemProperty, value);
		}
	}

	public ProjectPermissionSetControl()
	{
		InitializeComponent();
	}

	private static void OnInUserItemChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is ProjectPermissionSetControl projectPermissionSetControl)
		{
			object newValue = e.NewValue;
			if (newValue is bool && (bool)newValue)
			{
				projectPermissionSetControl.DividerLine.Visibility = Visibility.Visible;
				projectPermissionSetControl.DeletePanel.Visibility = Visibility.Visible;
			}
		}
	}

	private void OnOptionButtonClick(object sender, RoutedEventArgs e)
	{
		if (OptionPanel.IsVisible)
		{
			OptionPopup.IsOpen = true;
		}
		if (TransferImage.IsVisible)
		{
			Transfer?.Invoke(this, null);
		}
		if (ExitProject.IsVisible)
		{
			Exit?.Invoke(this, null);
		}
	}

	private void OnOptionSelect(object sender, EventArgs e)
	{
		if (!_canModify)
		{
			Utils.Toast(Utils.GetString("CantModifyPermission"));
			return;
		}
		if (sender is FrameworkElement frameworkElement && int.TryParse((string)frameworkElement.Tag, out var result))
		{
			SwitchPermission(result);
			PermissionSelect?.Invoke(this, result);
		}
		OptionPopup.IsOpen = false;
	}

	public void SwitchPermission(int tag)
	{
		switch (tag)
		{
		case 1:
			OptionText.Text = WriteButton.Text;
			break;
		case 2:
			OptionText.Text = CommentButton.Text;
			break;
		case 3:
			OptionText.Text = ReadOnlyButton.Text;
			break;
		default:
			tag = 1;
			OptionText.Text = WriteButton.Text;
			break;
		}
		SelectedPermission = (Constants.ProjectPermission)tag;
	}

	private void OnDeleteClick(object sender, MouseButtonEventArgs e)
	{
		Delete?.Invoke(this, null);
		OptionPopup.IsOpen = false;
	}

	public void ShowTransfer()
	{
		OptionPanel.Visibility = Visibility.Collapsed;
		TransferImage.Visibility = Visibility.Visible;
	}

	public void ShowExit()
	{
		OptionPanel.Visibility = Visibility.Collapsed;
		ExitProject.Visibility = Visibility.Visible;
	}

	public void UnableChangePermission()
	{
		WriteButton.IsEnabled = false;
		CommentButton.IsEnabled = false;
		ReadOnlyButton.IsEnabled = false;
		_canModify = false;
	}

	public void SetTeamItem()
	{
		DividerLine.Visibility = Visibility.Collapsed;
		DeletePanel.Visibility = Visibility.Collapsed;
	}

	public void SetShowNeedAuditPanel(bool show, bool? needAudit = null, bool isTeam = false)
	{
		if (show)
		{
			NeedAuditPanel.Visibility = Visibility.Visible;
			DividerLine.Visibility = Visibility.Visible;
		}
		else
		{
			NeedAuditPanel.Visibility = Visibility.Collapsed;
			DividerLine.Visibility = Visibility.Collapsed;
		}
		if (needAudit.HasValue)
		{
			NeedAuditCheckBox.IsChecked = needAudit;
		}
		ApprovalTooltipBorder.ToolTip = Utils.GetString(isTeam ? "OwnerGuestHint" : "ApprovalByOwnerTooltip");
	}

	private void OnNeedAuditClick(object sender, MouseButtonEventArgs e)
	{
		Mouse.Capture(null);
		e.Handled = true;
		NeedAuditCheckBox.IsChecked = NeedAuditCheckBox.IsChecked != true;
		NeedAuditChange?.Invoke(this, NeedAuditCheckBox.IsChecked == true);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/project/projectpermissionsetcontrol.xaml", UriKind.Relative);
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
			Root = (ProjectPermissionSetControl)target;
			break;
		case 2:
			Container = (Grid)target;
			break;
		case 3:
			((Button)target).Click += OnOptionButtonClick;
			break;
		case 4:
			OptionPanel = (StackPanel)target;
			break;
		case 5:
			OptionText = (TextBlock)target;
			break;
		case 6:
			Arrow = (Path)target;
			break;
		case 7:
			TransferImage = (Image)target;
			break;
		case 8:
			ExitProject = (Image)target;
			break;
		case 9:
			OptionPopup = (EscPopup)target;
			break;
		case 10:
			WriteButton = (OptionCheckBox)target;
			break;
		case 11:
			CommentButton = (OptionCheckBox)target;
			break;
		case 12:
			ReadOnlyButton = (OptionCheckBox)target;
			break;
		case 13:
			DividerLine = (Line)target;
			break;
		case 14:
			DeletePanel = (Border)target;
			DeletePanel.PreviewMouseLeftButtonUp += OnDeleteClick;
			break;
		case 15:
			NeedAuditPanel = (DockPanel)target;
			break;
		case 16:
			NeedAuditCheckBox = (CheckBox)target;
			NeedAuditCheckBox.PreviewMouseLeftButtonUp += OnNeedAuditClick;
			break;
		case 17:
			ApprovalTooltipBorder = (Border)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
